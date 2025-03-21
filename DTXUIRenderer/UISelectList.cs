﻿using System.Collections.Generic;

namespace DTXUIRenderer;

public class UISelectList : UIGroup
{
    public List<IUISelectable> selectableChildren = new List<IUISelectable>();
    public List<UIDrawable> selectableChildrenDrawables = new List<UIDrawable>();

    public UISelectList(string name) : base(name)
    {
        
    }

    public int currentlySelectedIndex { get; private set; } = 0;
        
    public T AddSelectableChild<T>(T child, int index = -1) where T : UIDrawable, IUISelectable
    {
        if (index >= 0)
        {
            children.Insert(index, child);
            selectableChildren.Insert(index, child);
            selectableChildrenDrawables.Insert(index, child);
            return child;
        }
            
        children.Add(child);
        selectableChildren.Add(child);
        selectableChildrenDrawables.Add(child);
            
        return child;
    }
        
    public void RemoveSelectableChild(IUISelectable child)
    {
        children.Remove(child as UIDrawable);
        selectableChildren.Remove(child);
    }
        
    public void SetSelectedIndex(int i)
    {
        if (i < 0 || i >= selectableChildren.Count)
        {
            return;
        }
            
        selectableChildren[currentlySelectedIndex].SetSelected(false);
        currentlySelectedIndex = i;
        selectableChildren[currentlySelectedIndex].SetSelected(true);
    }
        
    public void SelectNext()
    {
        selectableChildren[currentlySelectedIndex].SetSelected(false);
            
        currentlySelectedIndex++;
        if (currentlySelectedIndex >= selectableChildren.Count)
        {
            currentlySelectedIndex = 0;
        }
            
        selectableChildren[currentlySelectedIndex].SetSelected(true);
    }
        
    public void SelectPrevious()
    {
        selectableChildren[currentlySelectedIndex].SetSelected(false);
            
        currentlySelectedIndex--;
        if (currentlySelectedIndex < 0)
        {
            currentlySelectedIndex = selectableChildren.Count - 1;
        }
            
        selectableChildren[currentlySelectedIndex].SetSelected(true);
    }
        
    public void RunAction()
    {
        selectableChildren[currentlySelectedIndex].RunAction();
    }
        
    public void UpdateLayout(int spacing = 32)
    {
        for (int i = 0; i < selectableChildren.Count; i++)
        {
            selectableChildrenDrawables[i].position.Y = i * spacing;
        }
    }
}