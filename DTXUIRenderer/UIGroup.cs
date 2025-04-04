using System.Collections.Generic;
using System.Linq;
using SharpDX;

namespace DTXUIRenderer;

public class UIGroup : UIDrawable
{
    internal readonly List<UIDrawable> children = [];
    
    //parameterless constructor required to create the object in the inspector
    [AddChildMenu]
    public UIGroup() : this("New UIGroup")
    {
    }
    
    public UIGroup(string name)
    {
        this.name = name;
    }
    
    public T AddChild<T>(T element) where T : UIDrawable
    {
        children.Add(element);
        return element;
    }
        
    public T GetChild<T>(int i) where T : UIDrawable
    {
        return (T)children[i];
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
                //draw elements at their position relative to the group
                element.Draw(combinedMatrix);
            }
        }
    }

    public override void Dispose()
    {
        foreach (UIDrawable element in children)
        {
            element.Dispose();
        }
            
        children.Clear();
    }
}