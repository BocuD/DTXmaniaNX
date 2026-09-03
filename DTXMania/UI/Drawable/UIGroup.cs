using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
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
    [JsonIgnore] private static readonly ConditionalWeakTable<UIDrawable, object> reportedDrawFailures = [];

    //an animating property is written every frame for as long as it animates, so a per-write cost here
    //scales with the frame rate
    [JsonIgnore] private static readonly int probeAnimators =
        Core.Framework.AllocationProbe.Register("Animators (whole tree)");

    //clips are part of what a skin describes: a cursor that pulses is animation, not code. Replace rather
    //than populate, so a type that builds an animator in its constructor does not end up with the loaded
    //clips appended to the ones it made
    [SkinSerialize]
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public Animator? animator;

    //per-instance data source for this subtree, runtime-only; descendants resolve their binding keys
    //against it before falling back to ancestors and the global context. See UIDrawable.DataContexts
    [JsonIgnore] public IUIDataContext? dataContext;

    //what this component's keys resolved to when it was last saved from a running instance. Written on a
    //component file so it can be edited on its own with values that make sense; nothing at runtime reads it
    [SkinSerialize]
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public Dictionary<string, string>? sampleContext;

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

    /// <summary>Asks for the children to be sorted again, after something changed a child's renderOrder.</summary>
    public void InvalidateOrder()
    {
        dirty = true;
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

        if (animator != null)
        {
            //TickAuto does not descend into child groups, so these never nest
            Core.Framework.AllocationProbe.Begin(probeAnimators);
            animator.TickAuto(this);
            Core.Framework.AllocationProbe.End(probeAnimators);
        }

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
                if (reportedDrawFailures.TryAdd(element, null!))
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
