using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using DTXMania.UI.Item;
using Color = System.Drawing.Color;

namespace DTXMania.UI.Config;

/// <summary>
/// One reusable row in a <see cref="ConfigList"/>: a background panel (chosen by the item's panel
/// type) plus its name and value text. Mirrors the song-select element pattern - a single instance
/// is reused and re-bound to different <see cref="CItemBase"/>s as the list scrolls.
/// </summary>
internal class ConfigItemElement : UIGroup
{
    private static BaseTexture boxNormal;
    private static BaseTexture boxFolder;
    private static BaseTexture boxOther;

    public static void LoadAssets()
    {
        boxNormal = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\4_itembox.png"));
        boxFolder = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\4_itembox folder.png"));
        boxOther = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\4_itembox other.png"));
    }

    public static void DisposeAssets()
    {
        boxNormal?.Dispose();
        boxFolder?.Dispose();
        boxOther?.Dispose();
    }

    private readonly UIImage panel;
    private readonly UIText nameText;
    private readonly UIText valueText;

    public CItemBase? item { get; private set; }

    public ConfigItemElement() : base("ConfigItem")
    {
        panel = AddChild(new UIImage(boxNormal));
        panel.renderOrder = 0;
        panel.name = "panel";

        nameText = AddChild(new UIText("", 16));
        nameText.fillColor = Color4.White;
        nameText.outlineWidth = 0;
        nameText.position = new Vector3(30, 30, 0);
        nameText.anchor = new Vector2(0, 0);
        nameText.renderOrder = 1;
        nameText.name = "name";

        valueText = AddChild(new UIText("", 16));
        valueText.fillColor = Color4.FromColor(Color.Black);
        valueText.outlineWidth = 0;
        valueText.position = new Vector3(265, 30, 0);
        valueText.anchor = new Vector2(0, 0);
        valueText.renderOrder = 1;
        valueText.name = "value";
    }

    public void Bind(CItemBase? newItem)
    {
        item = newItem;

        panel.SetTexture(GetPanelTexture(newItem), false, false);

        nameText.SetText(newItem?.strItemName ?? "");
        nameText.isVisible = newItem != null;

        RefreshValue();
    }

    //re-reads the item's display value (call after the value changed)
    public void RefreshValue()
    {
        if (item is CItemTextInput textInput)
        {
            textInput.drawableTextInput.position = valueText.position;
        }
        
        //folders / back buttons have no value to show, CItemTextInput will render it on its own
        bool showValue = item is not CItemTextInput;
        valueText.isVisible = showValue;

        if (showValue)
        {
            valueText.SetText(item!.formatValue?.Invoke() ?? item.GetStringValue());
        }
    }

    //called when resolution changes to force a re-render; todo: make this a global list
    public void ForceReRenderText()
    {
        nameText.MarkDirty();
        valueText.MarkDirty();
    }

    //switches the value text to the highlighted style used while editing
    public void SetEditing(bool editing)
    {
        if (editing)
        {
            valueText.fillColor = Color4.White;
            valueText.outlineColor = Color4.FromColor(Color.OrangeRed);
            valueText.outlineWidth = 4;
        }
        else
        {
            valueText.fillColor = Color4.FromColor(Color.Black);
            valueText.outlineWidth = 0;
        }

        valueText.MarkDirty();
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        base.Draw(parentMatrix);

        if (item is CItemTextInput textInput)
        {
            textInput.drawableTextInput.Draw(localTransformMatrix * parentMatrix);
        }
    }

    private static BaseTexture GetPanelTexture(CItemBase? item)
    {
        return item?.ePanelType switch
        {
            CItemBase.EPanelType.Folder => boxFolder,
            CItemBase.EPanelType.Other or CItemBase.EPanelType.Return => boxOther,
            _ => boxNormal
        };
    }
}
