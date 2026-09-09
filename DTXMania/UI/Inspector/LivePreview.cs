using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;

namespace DTXMania.UI.Inspector;

/// <summary>
/// Shows an edited component with the values a running instance of it is seeing. Each of the editor's
/// slots is matched to the same slot of the live list, so a list of rows shows the rows the game has
/// rather than the first one repeated.
/// </summary>
public sealed class LivePreview(IUIDataContext borrowed, Func<UIGroup?> target) : IUIPreview
{
    public IUIDataContext Root => borrowed;

    public IUIDataContext? ScopeFor(UIGroup group)
    {
        if (group is not UIItemSlot slot || slot.parent is not UIItemsGroup list || target() is not { } live)
        {
            return null;
        }

        //lists are matched by name, the same way a slot's saved sample values are keyed
        return FindList(live, list.name) is { } liveList ? SlotAt(liveList, slot.index)?.dataContext : null;
    }

    /// <summary>The editor's own slot for one index, which is what the panel resolves a live value
    /// through: pushed as the preview, it lands on the matching live slot.</summary>
    public static UIItemSlot? SlotAt(UIItemsGroup list, int index)
    {
        foreach (UIDrawable child in list.children)
        {
            if (child is UIItemSlot slot && slot.index == index)
            {
                return slot;
            }
        }

        return null;
    }

    private static UIItemsGroup? FindList(UIGroup group, string name)
    {
        foreach (UIDrawable child in group.children)
        {
            if (child is UIItemsGroup list && list.name == name)
            {
                return list;
            }

            if (child is UIGroup nested && FindList(nested, name) is { } found)
            {
                return found;
            }
        }

        return null;
    }
}
