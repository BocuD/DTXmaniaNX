namespace DTXMania.UI.Drawable;

/// <summary>
/// A group of selectable children (e.g. the config left-menu buttons) with a single highlighted
/// selection that can be moved next/previous and activated.
/// </summary>
public class UISelectList : UIGroup
{
    private readonly List<IUISelectable> selectables = [];

    public UISelectList(string name) : base(name)
    {
    }

    public int currentlySelectedIndex { get; private set; }
    public int SelectableCount => selectables.Count;

    public T AddSelectableChild<T>(T child, int index = -1) where T : UIDrawable, IUISelectable
    {
        if (index >= 0)
        {
            children.Insert(index, child);
            selectables.Insert(index, child);
        }
        else
        {
            children.Add(child);
            selectables.Add(child);
        }

        return child;
    }

    public void RemoveSelectableChild(IUISelectable child)
    {
        if (child is UIDrawable drawable)
        {
            children.Remove(drawable);
        }

        selectables.Remove(child);
    }

    public void SetSelectedIndex(int i)
    {
        if (i < 0 || i >= selectables.Count)
        {
            return;
        }

        selectables[currentlySelectedIndex].SetSelected(false);
        currentlySelectedIndex = i;
        selectables[currentlySelectedIndex].SetSelected(true);
    }

    public void SelectNext()
    {
        if (selectables.Count == 0) return;
        SetSelectedIndex((currentlySelectedIndex + 1) % selectables.Count);
    }

    public void SelectPrevious()
    {
        if (selectables.Count == 0) return;
        SetSelectedIndex((currentlySelectedIndex - 1 + selectables.Count) % selectables.Count);
    }

    public void RunAction()
    {
        if (selectables.Count == 0) return;
        selectables[currentlySelectedIndex].RunAction();
    }

    public void UpdateLayout(int spacing = 32)
    {
        for (int i = 0; i < selectables.Count; i++)
        {
            ((UIDrawable)selectables[i]).position.Y = i * spacing;
        }
    }
}
