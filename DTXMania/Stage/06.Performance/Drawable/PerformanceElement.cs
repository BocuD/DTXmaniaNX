using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Inspector;
using DTXMania.UI.Skin;
using DTXMania.UI.Text;
using Hexa.NET.ImGui;

namespace DTXMania;

public abstract class PerformanceElement : UIGroup
{
    [Themable] public EPlayer player;

    protected PerformanceElement(string name, EPlayer player) : base(name)
    {
        this.player = player;
        MakeComponent(ComponentName(player), () => BuildDefault(player));
    }

    protected abstract string ComponentName(EPlayer player);
    protected abstract UIGroup BuildDefault(EPlayer player);

    public override void OnDeserialize()
    {
        base.OnDeserialize();
        MakeComponent(ComponentName(player), () => BuildDefault(player));
    }

    protected static UIText LaneNumber(string name, string source, Vector2 position) => new(string.Empty, 17)
    {
        name = name,
        font = SkinResource.System("texgyreadventor-bold.otf"),
        style = UiTextStyle.Regular,
        fillColor = Color4.White,
        outlineWidth = 1.5f,
        position = new Vector3(position, 0),
        pivot = new Vector2(1, 0),
        bindings =
        {
            new UIBinding("text", source),
            new UIBinding("isVisible", "Config.ShowPerformanceInformation")
        }
    };

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (ImGui.CollapsingHeader("Performance"))
        {
            Inspector.Inspect("Player", ref player);
        }
    }
}
