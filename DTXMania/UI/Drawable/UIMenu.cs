using DTXMania.Core;
using DTXMania.UI.DynamicElements;
using FDK;

namespace DTXMania.UI.Drawable;

/// <summary>
/// One entry of a <see cref="UIMenu"/>, exposed as <c>"Item"</c> to the entry component. Whether an entry
/// runs a delegate or a named stage action is the caller's choice: code-built menus hand over a delegate,
/// a menu described by a skin names an action the stage registered.
/// </summary>
public sealed class UIMenuItem
{
    [DataField] public string Label { get; }

    [DataField] public string ActionId { get; }

    //for a menu whose entries are rows of one sheet rather than text: where this entry's art starts
    [DataField] public double ClipY { get; init; }

    //what choosing this entry sounds like, where the usual decide sound is not what it should sound like
    internal CSystemSound? Sound { get; init; }

    public Action? Run { get; }

    public UIMenuItem(string label, Action run)
    {
        Label = label;
        ActionId = string.Empty;
        Run = run;
    }

    public UIMenuItem(string label, string actionId)
    {
        Label = label;
        ActionId = actionId;
    }
}

/// <summary>
/// A list of entries with one selected, which is what every menu in the game reduces to. The entries are
/// data and the look is a component, so the same class serves the modal's options, a stage's menu and
/// anything a skin describes — none of them need code that positions a cursor or tracks an index.
/// </summary>
public class UIMenu : UIItemsGroup, IUIItemSource
{
    private readonly List<UIMenuItem> entries = [];

    //runs when cancel is pressed with this menu focused, for menus that can be backed out of
    public Action? onCancel;

    //runs when the user moves onto another entry, for a menu whose entries preview themselves
    public Action<UIMenuItem>? onSelectionChanged;

    //replaces running the entry, for a menu where choosing means something else — the config menu's
    //categories load on selection and choosing moves focus into the page
    public Action<UIMenuItem>? onDecide;

    public UIMenu() : this("Menu")
    {
    }

    public UIMenu(string name) : base(name)
    {
        SetSource(this);
    }

    public int ItemCount => entries.Count;

    public object? GetItem(int index) => index >= 0 && index < entries.Count ? entries[index] : null;

    public UIMenuItem? SelectedEntry => GetItem(SelectedItem) as UIMenuItem;

    public void SetEntries(IEnumerable<UIMenuItem> items, int selection = 0)
    {
        entries.Clear();
        entries.AddRange(items);

        SetSource(this);
        SelectedItem = selection;
    }

    protected override void OnSelectionMoved()
    {
        if (SelectedEntry is { } entry)
        {
            onSelectionChanged?.Invoke(entry);
        }
    }

    protected override void Decide()
    {
        if (SelectedEntry is not { } entry)
        {
            return;
        }

        (entry.Sound ?? CDTXMania.Skin.soundDecide).tPlay();

        if (onDecide != null)
        {
            onDecide(entry);
            return;
        }

        if (entry.Run != null)
        {
            entry.Run();
            return;
        }

        if (!string.IsNullOrEmpty(entry.ActionId)
            && CDTXMania.StageManager.rCurrentStage?.dynamicActions.TryGetValue(entry.ActionId, out Action? action) == true)
        {
            action();
        }
    }

    protected override void Cancel()
    {
        if (onCancel == null)
        {
            return;
        }

        CDTXMania.Skin.soundCancel.tPlay();
        onCancel();
    }
}
