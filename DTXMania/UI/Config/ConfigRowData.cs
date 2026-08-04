using DTXMania.Core;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Item;
using System.Numerics;

namespace DTXMania.UI.Config;

/// <summary>
/// What one settings row displays, exposed as <c>"Item"</c> to the ConfigRow component. The row's look is
/// the component's business; this is only what it shows.
/// </summary>
internal sealed class ConfigRowData
{
    [DataField] public string Name { get; private set; } = string.Empty;

    [DataField] public string Value { get; private set; } = string.Empty;

    [DataField] public BaseTexture? Panel { get; private set; }

    //the value is drawn in one style or the other, never both, and a text-input row draws neither because
    //it renders its own field
    [DataField] public bool ShowValue => hasValue && !IsEditing;

    [DataField] public bool ShowEditedValue => hasValue && IsEditing;

    public bool IsEditing { get; set; }

    public CItemBase? Item { get; private set; }

    private bool hasValue;

    public void SetItem(CItemBase? item, ConfigRowAssets assets)
    {
        Item = item;
        IsEditing = false;
        Name = item?.strItemName ?? string.Empty;
        Panel = assets.PanelFor(item);

        //a text input renders its own value, so the row leaves that space alone
        hasValue = item != null && item is not CItemTextInput;

        RefreshValue();
    }

    /// <summary>Re-reads the item's displayed value, which its action may have changed.</summary>
    public void RefreshValue()
    {
        Value = hasValue && Item != null
            ? Item.formatValue?.Invoke() ?? Item.GetStringValue()
            : string.Empty;
    }
}

/// <summary>The panel art behind a row, which is picked by what kind of row it is.</summary>
internal sealed class ConfigRowAssets : IDisposable
{
    private readonly BaseTexture normal = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\4_itembox.png"));
    private readonly BaseTexture folder = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\4_itembox folder.png"));
    private readonly BaseTexture other = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\4_itembox other.png"));

    /// <summary>How big a row's panel is; a dynamic image keeps the size its layout gives it.</summary>
    public Vector2 PanelSize => new(normal.Width, normal.Height);

    public BaseTexture? PanelFor(CItemBase? item) => item?.ePanelType switch
    {
        null => null,
        CItemBase.EPanelType.Folder => folder,
        CItemBase.EPanelType.Other or CItemBase.EPanelType.Return => other,
        _ => normal
    };

    public void Dispose()
    {
        normal.Dispose();
        folder.Dispose();
        other.Dispose();
    }
}
