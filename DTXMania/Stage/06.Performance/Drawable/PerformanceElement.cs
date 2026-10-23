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
    [Themable] public EInstrumentPart instrument;

    protected readonly PerformanceStats stats = new();
    private readonly UIDataContext data = new();

    protected PerformanceElement(string name, EInstrumentPart instrument) : base(name)
    {
        this.instrument = instrument;
        stats.Instrument = (int)instrument;

        data.RegisterObject("Stats", () => stats);
        dataContext = data;

        MakeComponent(ComponentName(instrument), () => BuildDefault(instrument));
    }

    protected abstract string ComponentName(EInstrumentPart part);
    protected abstract UIGroup BuildDefault(EInstrumentPart part);

    protected virtual bool IsShown => stats.HasLane;

    public override void OnDeserialize()
    {
        base.OnDeserialize();
        stats.Instrument = (int)instrument;
        MakeComponent(ComponentName(instrument), () => BuildDefault(instrument));
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        stats.Instrument = (int)instrument;

        //not isVisible: a hidden element never draws again to notice it should show
        if (!IsShown)
        {
            return;
        }

        base.Draw(parentMatrix);
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
            Inspector.Inspect("Instrument", ref instrument);
        }
    }
}
