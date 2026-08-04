using System.Diagnostics;
using System.Numerics;
using DTXMania.UI.Animation;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Inspector;
using Hexa.NET.ImGui;
using Newtonsoft.Json;

namespace DTXMania.UI.Drawable;

public class UIGroup : UIDrawable
{
    [Themable] public bool sortByRenderOrder = true;
    public List<UIDrawable> children = [];

    private bool dirty = false;

    //drawables already reported as failing to draw, so one broken element does not fill the log
    [JsonIgnore] private static readonly HashSet<string> reportedDrawFailures = [];

    //clips are part of what a skin describes: a cursor that pulses is animation, not code. Replace rather
    //than populate, so a type that builds an animator in its constructor does not end up with the loaded
    //clips appended to the ones it made
    [SkinSerialize]
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public Animator? animator;

    //per-instance data source for this subtree, runtime-only; descendants resolve their binding keys
    //against it before falling back to ancestors and the global context. See UIDrawable.DataContexts
    [JsonIgnore] public IUIDataContext? dataContext;

    [AddChildMenu]
    public static UIDrawable Create()
    {
        return new UIGroup("New UIGroup");
    }

    public UIGroup()
        : this("New UIGroup")
    {
    }

    public UIGroup(string name)
    {
        this.name = name;
    }

    public T AddChild<T>(T element, bool setParent = true) where T : UIDrawable
    {
        children.Add(element);
        if (setParent)
        {
            element.SetParent(this, false);
        }

        animator?.InvalidateBindings();

        dirty = true;
        
        return element;
    }

    public T GetChild<T>(int i) where T : UIDrawable
    {
        return (T)children[i];
    }

    public T? GetChild<T>(string name) where T : UIDrawable
    {
        //names come from json, so a mismatched type is a layout authoring error, not a crash
        return children.FirstOrDefault(x => x.name == name) as T;
    }

    public UIDrawable GetChild(int i)
    {
        return children[i];
    }

    public void RemoveChild(UIDrawable element)
    {
        children.Remove(element);
        animator?.InvalidateBindings();
        
        dirty = true;
    }

    public void ClearChildren()
    {
        foreach (UIDrawable element in children)
        {
            element.Dispose();
        }

        children.Clear();
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        if (!isVisible)
        {
            return;
        }

        //bindings first, then the animator, so an animation targeting a bound member wins for this frame
        for (int index = 0; index < children.Count; index++)
        {
            children[index].ApplyBindings();
        }

        animator?.TickAuto(this);

        UpdateLocalTransformMatrix();
        Matrix4x4 combinedMatrix = localTransformMatrix * parentMatrix;

        if (sortByRenderOrder && dirty)
        {
            children.Sort((a, b) => a.renderOrder.CompareTo(b.renderOrder));
            dirty = false;
        }

        for (int index = 0; index < children.Count; index++)
        {
            UIDrawable element = children[index];
            if (!element.isVisible)
            {
                continue;
            }

            try
            {
                element.Draw(combinedMatrix);
            }
            catch (Exception e)
            {
                //a drawable that throws once throws every frame, so it is reported the first time and
                //then left alone: the log stays readable and the rest of the tree still draws
                if (reportedDrawFailures.Add(element.id))
                {
                    Trace.TraceError($"Error drawing {element.name}: {e} Stacktrace: {e.StackTrace ?? "No stack trace"}");
                }
            }
        }
    }

    public override void OnDeserialize()
    {
        base.OnDeserialize();

        foreach (UIDrawable? child in children)
        {
            child?.SetParent(this, false);
        }

        children.RemoveAll(x => x == null);
    }

    public override void Dispose()
    {
        base.Dispose();

        foreach (UIDrawable element in children)
        {
            element.Dispose();
        }

        children.Clear();
    }

    public int GetChildIndex(UIDrawable node)
    {
        return children.IndexOf(node);
    }

    public void SetChildIndex(UIDrawable node, int index)
    {
        if (index < 0 || index >= children.Count)
        {
            Trace.TraceError($"Index {index} is out of bounds for children list of size {children.Count}");
            return;
        }

        int currentIndex = GetChildIndex(node);
        if (currentIndex != -1)
        {
            children.RemoveAt(currentIndex);
            children.Insert(index, node);
        }
    }

    public override void DrawInspector()
    {
        base.DrawInspector();
        ImGui.Checkbox("Sort by Render Order", ref sortByRenderOrder);

        if (ImGui.CollapsingHeader("Animator"))
        {
            if (animator == null)
            {
                if (ImGui.Button("Add Animator"))
                {
                    animator = new Animator();
                }
            }
            else
            {
                animator.DrawInspector(this);

                ImGui.Separator();
                if (ImGui.Button("Remove Animator"))
                {
                    animator = null;
                }
            }
        }
    }
}
