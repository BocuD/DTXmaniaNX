using System.Diagnostics;
using DTXMania.UI.Drawable.Serialization;
using DTXMania.UI.Inspector;
using Newtonsoft.Json;
using SharpDX;

namespace DTXMania.UI.Drawable;

public class UIGroup : UIDrawable
{
    public List<UIDrawable> children = [];

    [AddChildMenu]
    public static UIDrawable Create()
    {
        return new UIGroup("New UIGroup");
    }
    
    public UIGroup() : this("New UIGroup")
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
        
    public override void Draw(Matrix parentMatrix)
    {
        if (!isVisible) return;
            
        UpdateLocalTransformMatrix();
        
        Matrix combinedMatrix = localTransformMatrix * parentMatrix;
            
        //sort by draw priority
        children.Sort((a, b) => a.renderOrder.CompareTo(b.renderOrder));

        for (int index = 0; index < children.Count; index++)
        {
            UIDrawable element = children[index];
            if (element.isVisible)
            {
                try
                {
                    //draw elements at their position relative to the group
                    element.Draw(combinedMatrix);
                }
                catch (Exception e)
                {
                    string stackTrace = e.StackTrace ?? "No stack trace";
                    Trace.TraceError($"Error drawing {element.name}: {e} Stacktrace: {stackTrace}");
                }
            }
        }
    }

    public override void OnDeserialize()
    {
        base.OnDeserialize();
        
        foreach (var child in children)
        {
            if (child == null) continue;
            
            child.SetParent(this, false);
        }
        
        //remove null children
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
        //create a copy of the group to avoid modifying the original
        var groupCopy = new UIGroup(name);
        groupCopy.children = new List<UIDrawable>(children);
            
        //remove any base elements
        //todo: actually implement a better way of not serializing certain children
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
            Trace.TraceError($"Failed to save stage skin: {e} Stacktrace: {stackTrace}");
            return "";
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
                return null;
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
}