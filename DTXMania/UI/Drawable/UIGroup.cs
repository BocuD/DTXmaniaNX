using DTXMania.UI.Inspector;
using DTXUIRenderer;
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
    
    public UIDrawable? GetChild(string name)
    {
        return children.FirstOrDefault(x => x.name == name);
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
        var sortedChildren = children.OrderBy(x => x.renderOrder);
        
        foreach (UIDrawable element in sortedChildren)
        {
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
                    Console.WriteLine($"Error drawing {element.name}: {e} Stacktrace: {stackTrace}");
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
            Console.WriteLine($"Index {index} is out of bounds for children list of size {children.Count}");
            return;
        }

        int currentIndex = GetChildIndex(node);
        if (currentIndex != -1)
        {
            children.RemoveAt(currentIndex);
            children.Insert(index, node);
        }
    }
}