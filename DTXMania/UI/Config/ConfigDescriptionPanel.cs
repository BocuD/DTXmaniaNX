using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using DTXMania.UI.Item;

namespace DTXMania.UI.Config;

internal sealed class ConfigDescriptionPanel : UIGroup
{
    private readonly UIText text;

    // text-vs-background offset (matches the old config layout)
    private const float TextInset = 19f;
    private const float TextTop = 18f;

    public ConfigDescriptionPanel() : base("ConfigDescriptionPanel")
    {
        dontSerialize = true;
        isVisible = false;

        UIImage background = AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\4_Description Panel.png"))));
        background.renderOrder = 0;

        size = background.size;

        text = AddChild(new UIText("", 17));
        text.name = "DescriptionText";
        text.fillColor = Color4.Black;
        text.outlineWidth = 0;
        text.renderOrder = 1;
        text.position = new Vector3(TextInset, TextTop, 0);

        //the panel is narrow and tall, so a description that does not fit gains lines rather than
        //running off the side. The text sits inset, so it wraps to the panel less both margins.
        text.wrap = true;
        text.size.X = MathF.Max(size.X - TextInset * 2f, 1f);
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
