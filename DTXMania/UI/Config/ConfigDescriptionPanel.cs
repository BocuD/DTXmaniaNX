using System.Numerics;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using DTXMania.UI.Item;
using DTXMania.UI.Skin;

namespace DTXMania.UI.Config;

internal sealed class ConfigDescriptionPanel : UIGroup
{
    //found once the component has loaded; nothing is built here, or a layout would load a second copy
    private UIText? text;

    // text-vs-background offset (matches the old config layout)
    private const float TextInset = 19f;
    private const float TextTop = 18f;

    //4_Description Panel.png, so the text below knows what it is wrapping inside
    private static readonly Vector2 PanelSize = new(280, 360);

    public ConfigDescriptionPanel() : base("ConfigDescriptionPanel")
    {
        isVisible = false;
        size = PanelSize;

        MakeComponent("ConfigDescriptionPanel", DescriptionPanelDefault);
    }

    protected override void OnContentLoaded()
    {
        text = GetChild<UIText>("DescriptionText");
    }

    private static UIGroup DescriptionPanelDefault()
    {
        UIGroup root = new("ConfigDescriptionPanel");

        root.AddChild(new UIImage
        {
            name = "Background",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\4_Description Panel.png"),
            size = PanelSize,
            renderOrder = 0
        });

        //the panel is narrow and tall, so a description that does not fit gains lines rather than
        //running off the side. The text sits inset, so it wraps to the panel less both margins.
        UIText description = root.AddChild(new UIText("", 17));
        description.name = "DescriptionText";
        description.fillColor = Color4.Black;
        description.outlineWidth = 0;
        description.renderOrder = 1;
        description.position = new Vector3(TextInset, TextTop, 0);
        description.wrap = true;
        description.size.X = MathF.Max(PanelSize.X - TextInset * 2f, 1f);

        return root;
    }

    public void Update(CItemBase? item, bool visible)
    {
        if (visible && text != null)
        {
            text.SetText(item?.formatDescription?.Invoke() ?? item?.strDescription ?? "");
        }

        isVisible = visible;
    }
}
