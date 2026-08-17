using DTXMania.UI.DynamicElements;
using DTXMania.UI.Item;

namespace DTXMania.UI.Config;

/// <summary>
/// What one settings row displays, exposed as <c>"Item"</c> to the ConfigRow component. The row's look is
/// the component's business; this is only what it shows.
/// </summary>
internal sealed class ConfigRowData
{
    [DataField] public string Name { get; private set; } = string.Empty;

    [DataField] public string Value { get; private set; } = string.Empty;

    //which of the ConfigRow component's panel arts this row uses; the arts themselves belong to the layout
    [DataField] public int PanelIndex { get; private set; }

    [DataField] public bool HasPanel => Item != null;

    //the value is drawn in one style or the other, never both. A text-input row shows its value like any
    //other until it is being typed in, when the field it renders itself takes that space
    [DataField] public bool ShowValue => hasValue && !IsEditing;

    [DataField] public bool ShowEditedValue => hasValue && IsEditing && !rendersOwnField;

    public bool IsEditing { get; set; }

    public CItemBase? Item { get; private set; }

    private bool hasValue;
    private bool rendersOwnField;

    public void SetItem(CItemBase? item)
    {
        Item = item;
        IsEditing = false;
        Name = item?.strItemName ?? string.Empty;
        PanelIndex = PanelFor(item);

        hasValue = item != null;
        rendersOwnField = item is CItemTextInput;

        RefreshValue();
    }

    //the order the ConfigRow component lists its panel arts in
    private static int PanelFor(CItemBase? item) => item?.ePanelType switch
    {
        CItemBase.EPanelType.Folder => 1,
        CItemBase.EPanelType.Other or CItemBase.EPanelType.Return => 2,
        _ => 0
    };

    /// <summary>Re-reads the item's displayed value, which its action may have changed.</summary>
    public void RefreshValue()
    {
        Value = hasValue && Item != null
            ? Item.formatValue?.Invoke() ?? Item.GetStringValue()
            : string.Empty;
    }
}

