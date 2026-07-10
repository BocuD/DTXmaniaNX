using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using DTXMania.UI.Item;

namespace DTXMania.UI.Config;

internal sealed class ConfigDescriptionPanel : UIGroup
{
    private readonly UIText text;

    public ConfigDescriptionPanel() : base("ConfigDescriptionPanel")
    {
        dontSerialize = true;
        isVisible = false;

        UIImage background = AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\4_Description Panel.png"))));
        background.renderOrder = 0;

        text = AddChild(new UIText("", 17));
        text.name = "DescriptionText";
        text.fillColor = Color4.Black;
        text.outlineWidth = 0;
        text.renderOrder = 1;
        text.position = new Vector3(19, 18, 0); // text-vs-background offset (matches the old config layout)
    }

    public void Update(CItemBase? item, bool visible)
    {
        if (visible)
        {
            text.SetText(item?.formatDescription?.Invoke() ?? item?.strDescription ?? "");
        }
        isVisible = visible;
    }
}
