using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Inspector;
using Hexa.NET.ImGui;

namespace DTXMania;

public class PlayerSection : UIGroup
{
    [AddChildMenu("Game/Performance/Player Section")]
    public new static UIDrawable Create() => new PlayerSection();

    [Themable] public EInstrument player;

    private readonly PerformanceStats stats = new();
    private readonly UIDataContext data = new();

    public PlayerSection() : this(EInstrument.Drums)
    {
    }

    public PlayerSection(EInstrument player) : base("PlayerSection")
    {
        this.player = player;
        size = new Vector2(1280, 720);

        stats.Player = player;
        data.RegisterObject("Stats", () => stats);
        dataContext = data;

        MakeComponent(ComponentName(player), () => BuildDefault(player));
    }

    private static string ComponentName(EInstrument player) => player == EInstrument.Drums ? "DrumsPlayer" : "GuitarPlayer";

    public override void OnDeserialize()
    {
        base.OnDeserialize();
        stats.Player = player;
        MakeComponent(ComponentName(player), () => BuildDefault(player));
    }

    private static UIGroup BuildDefault(EInstrument player)
    {
        UIGroup root = new(ComponentName(player));

        if (player == EInstrument.Drums)
        {
            Add(root, new PerformanceShutters(EInstrumentPart.DRUMS), "Shutters", 295, 0, CStagePerfCommonScreen.LaneOverlayLayer, "Stats.HasLane");
            Add(root, new PerformanceJudgeLine(EInstrumentPart.DRUMS), "JudgeLine", 295, 0, CStagePerfCommonScreen.LaneOverlayLayer, "Stats.HasLane");

            UIGroup perspective = root.AddChild(new UIGroup("Perspective"));
            perspective.size = new Vector2(1280, 720);
            perspective.parentAnchor = new Vector2(0.5f, 0.5f);
            perspective.pivot = new Vector2(0.5f, 0.5f);
            perspective.perspective = 1410;
            perspective.vanishingPoint = new Vector2(640, 360);
            perspective.renderOrder = CStagePerfCommonScreen.PanelLayer;

            PerformanceStatusPanel status = perspective.AddChild(new PerformanceStatusPanel(EInstrumentPart.DRUMS));
            status.name = "Status";
            status.bindings.Add(new UIBinding("isVisible", "Stats.ShowsPanel"));

            //3d version
            //status.position = new Vector3(86, 186, -39);
            //status.rotation = new Vector3(0, -61f * MathF.PI / 180, 0);

            //2d version
            status.position = new Vector3(14, 237, 0);

            PerformanceElement gauge = Add(root, new PerformanceGauge(EInstrumentPart.DRUMS), "Gauge", 294, 626, CStagePerfCommonScreen.PanelLayer, "Stats.HasLane");
            gauge.bindings.Add(new UIBinding("position.Y", "Config.Reverse.Drums")
            {
                condition = BindingCondition.IsTrue, whenTrue = 28, whenFalse = 626
            });

            return root;
        }

        Add(root, new PerformanceLane(EInstrumentPart.GUITAR), "Lane", 67, 42, CStagePerfCommonScreen.LaneArtLayer, "Stats.HasLane");
        Add(root, new PerformanceLaneCaps(EInstrumentPart.GUITAR), "LaneCaps", 67, 42, CStagePerfCommonScreen.LaneOverlayLayer, "Stats.HasLane");
        Add(root, new PerformanceShutters(EInstrumentPart.GUITAR), "Shutters", 80, 0, CStagePerfCommonScreen.LaneOverlayLayer, "Stats.HasLane");
        Add(root, new PerformanceJudgeLine(EInstrumentPart.GUITAR), "JudgeLine", 80, 0, CStagePerfCommonScreen.LaneOverlayLayer, "Stats.HasLane");

        PerformanceElement guitarStatus = Add(root, new PerformanceStatusPanel(EInstrumentPart.GUITAR), "Status", 373, 254, CStagePerfCommonScreen.PanelLayer, "Stats.ShowsPanel");
        guitarStatus.bindings.Add(OnRightSide("position.X", -205, 373));

        Add(root, new PerformanceGauge(EInstrumentPart.GUITAR), "Gauge", 80, 0, CStagePerfCommonScreen.PanelLayer, "Stats.HasLane");

        return root;
    }

    public static UIBinding OnRightSide(string target, double right, double left) => new(target, "Stats.Player")
    {
        condition = BindingCondition.Equal, value = (int)EInstrument.Guitar2, whenTrue = right, whenFalse = left
    };

    private static PerformanceElement Add(UIGroup parent, PerformanceElement element, string name, float x, float y, int renderOrder, string shownWhen)
    {
        parent.AddChild(element);
        element.name = name;
        element.position = new Vector3(x, y, 0);
        element.renderOrder = renderOrder;
        element.bindings.Add(new UIBinding("isVisible", shownWhen));
        return element;
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (ImGui.CollapsingHeader("Player"))
        {
            if (Inspector.Inspect("Player", ref player))
            {
                stats.Player = player;
            }
        }
    }
}
