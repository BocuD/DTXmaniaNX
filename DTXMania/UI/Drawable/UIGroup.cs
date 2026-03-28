using System.Diagnostics;
using System.Numerics;
using DTXMania.UI.Drawable.Serialization;
using DTXMania.UI.Inspector;
using Hexa.NET.ImGui;
using Newtonsoft.Json;

namespace DTXMania.UI.Drawable;

public class UIGroup : UIDrawable
{
    public bool sortByRenderOrder = true;
    public List<UIDrawable> children = [];

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

        return element;
    }

    public T GetChild<T>(int i) where T : UIDrawable
    {
        return (T)children[i];
    }

    public T? GetChild<T>(string name) where T : UIDrawable
    {
        return (T?)children.FirstOrDefault(x => x.name == name);
    }

    public UIDrawable GetChild(int i)
    {
        return children[i];
    }

    public void RemoveChild(UIDrawable element)
    {
        children.Remove(element);
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

        UpdateLocalTransformMatrix();
        Matrix4x4 combinedMatrix = localTransformMatrix * parentMatrix;

        if (sortByRenderOrder)
        {
            children.Sort((a, b) => a.renderOrder.CompareTo(b.renderOrder));
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
                string stackTrace = e.StackTrace ?? "No stack trace";
                Trace.TraceError($"Error drawing {element.name}: {e} Stacktrace: {stackTrace}");
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

    public string SerializeToJSON()
    {
        UIGroup groupCopy = new(name)
        {
            children = new List<UIDrawable>(children)
        };

        groupCopy.children.RemoveAll(x => x.dontSerialize);

        try
        {
            string json = JsonConvert.SerializeObject(groupCopy, Formatting.Indented);
            groupCopy.Dispose();
            return json;
        }
        catch (Exception e)
        {
            string stackTrace = e.StackTrace ?? "No stack trace";
            Trace.TraceError($"Failed to save UIGroup: {e} Stacktrace: {stackTrace}");
            return string.Empty;
        }
    }

    public static UIGroup? DeserializeFromJSON(string json)
    {
        try
        {
            UIGroup? loadedGroup = JsonConvert.DeserializeObject<UIGroup>(json, new UIDrawableConverter());
            if (loadedGroup == null)
            {
                Trace.TraceError("Deserialization returned null, possibly due to an empty or invalid JSON.");
            }

            return loadedGroup;
        }
        catch (Exception e)
        {
            string stackTrace = e.StackTrace ?? "No stack trace";
            Trace.TraceError($"Failed to deserialize UIGroup: {e} Stacktrace: {stackTrace}");
            return null;
        }
    }

    public override void DrawInspector()
    {
        base.DrawInspector();
        ImGui.Checkbox("Sort by Render Order", ref sortByRenderOrder);
    }
}
