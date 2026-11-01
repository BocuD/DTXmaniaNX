using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Inspector;
using DTXMania.UI.Skin;

namespace DTXMania;

public class DrumsPlayfield : Playfield
{
    [AddChildMenu("Game/Performance/Drums Playfield")]
    public new static UIDrawable Create() => new DrumsPlayfield();

    private const string ComponentName = "DrumsPlayfield";

    private LaneGroup lanes = null!;
    private DrumPads pads = null!;
    private CActPerfDrumsLaneFlushD laneFlush = null!;

    private DrumsChipRenderer? drumChips;
    private IPerfFire? fire;
    private bool setUp;

    public DrumsPlayfield() : base(EInstrument.Drums)
    {
        name = "Drums";
        part = EInstrumentPart.DRUMS;
        MakeComponent(ComponentName, BuildDefault);
    }

    public override void OnDeserialize()
    {
        base.OnDeserialize();
        MakeComponent(ComponentName, BuildDefault);
    }

    internal void Setup(CActPerfDrumsLaneFlushD flush)
    {
        laneFlush = flush;
        laneFlush.lanes = layout;
        EnsureContent();
        setUp = true;
        ApplyConfig();
    }

    //the skin's parts come from a saved layout or the default one. A reload replaces all of them
    protected override void OnContentLoaded()
    {
        lanes = GetChild<LaneGroup>("Lanes") ?? AddChild(DefaultLanes());
        pads = GetChild<DrumPads>("Pads") ?? AddChild(DefaultPads());
        scoreDisplay = GetChild<ScoreDisplay>("Score") ?? AddChild(DefaultScore(DefaultBonus()));

        lanes.layout = layout;
        stats.lanes = layout;
        pads.lanes = layout;

        if (setUp)
        {
            ApplyConfig();
        }
    }

    private void ApplyConfig()
    {
        lanes.order = Columns();
        foreach (ELane lane in lanes.order)
        {
            lanes.SetEnabled(lane, IsUsed(lane));
        }

        lanes.Arrange();

        loopMarkers = AddPart(new LoopMarkers(), "LoopMarkers", CStagePerfCommonScreen.ChipsLayer);
        loopMarkers.size.Y = 720;

        drumChips = AddPart(new DrumsChipRenderer(), "Chips", CStagePerfCommonScreen.ChipsLayer);
        drumChips.lanes = layout;
        chipRenderer = drumChips;
        ApplyChart();

        AddLegacyPart("LaneFlush", CStagePerfCommonScreen.LegacyLanesLayer, probeLanes, origin => laneFlush.Draw(origin));
        AddLegacyPart("ProgressBar", CStagePerfCommonScreen.LegacyLanesLayer, probeLanes, origin => progressBar.Draw(origin, part, part));
        AddLegacyPart("JudgementStrings", CStagePerfCommonScreen.LegacyOverlayLayer, probeJudgement, _ => judgements.OnUpdateAndDraw());
        SetUpScore(0, 0);

        if (CDTXMania.ConfigIni.bClassicDrumsFire)
        {
            fire = AddPart(new ClassicDrumsChipFire { lanes = layout }, "ChipFire", CStagePerfCommonScreen.EffectsLayer);
        }
        else
        {
            fire = AddPart(new ActPerfNewFire(0) { lanes = layout }, "ChipFire", CStagePerfCommonScreen.EffectsLayer);
        }

        //the judgement text shares the effects layer with the fire, and draws over it
        judgements.InitUI(this, 0, int.MaxValue, 0);
    }

    //the player moves it with PageUp and PageDown, which keeps JudgeLine in step
    protected override int JudgeLineY => CDTXMania.ConfigIni.bReverse.Drums
        ? 159 + CDTXMania.ConfigIni.nJudgeLine.Drums
        : 561 - CDTXMania.ConfigIni.nJudgeLine.Drums;

    //pad: LC HH SD BD HT LT FT CY RD LP
    internal void Hit(ELane lane, int pad, float velocity)
    {
        pads.Hit(pad);
        laneFlush.Start(lane, velocity);
    }

    internal void Judged(int lane, EJudgement judgement, int lag)
    {
        ShowJudgement(lane, judgement, lag);
    }

    internal void StartFire(ELane lane, bool fillIn, bool fillInEnd)
    {
        fire?.Start(lane, fillIn, fillInEnd);
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        EnsureContent();

        lanes.showsArt = ShowsLaneArt;
        //the lanes fade so the video behind them shows through
        lanes.opacity = (255 - (int)(CDTXMania.ConfigIni.nMovieAlpha * 255 / 10f)) / 255f;
        lanes.Arrange();

        bool reverse = CDTXMania.ConfigIni.bReverse.Drums;
        PlaceLoopMarkers(830, JudgeLineY, !reverse);

        if (drumChips != null)
        {
            drumChips.judgeLineY = JudgeLineY;
            //the pattern runs through 8 frames, one every 70ms
            drumChips.patternFrame = PerformanceClock.Frame(70, 8);
        }

        if (fire != null)
        {
            int offset = CDTXMania.ConfigIni.nJudgeLinePosOffset.Drums;
            fire.iPosY = (reverse ? JudgeLineY - 183 : JudgeLineY - 186) + offset;
        }

        base.Draw(parentMatrix);
    }

    private static ELane[] Columns()
    {
        ELane[] columns = CDTXMania.ConfigIni.eLaneType.Drums switch
        {
            EType.B => [ELane.LC, ELane.HH, ELane.SD, ELane.LP, ELane.BD, ELane.HT, ELane.LT, ELane.FT, ELane.CY, ELane.RD],
            EType.C => [ELane.LC, ELane.HH, ELane.LP, ELane.SD, ELane.BD, ELane.HT, ELane.LT, ELane.FT, ELane.CY, ELane.RD],
            EType.D => [ELane.LC, ELane.HH, ELane.SD, ELane.HT, ELane.LP, ELane.BD, ELane.LT, ELane.FT, ELane.CY, ELane.RD],
            _ => [ELane.LC, ELane.HH, ELane.LP, ELane.SD, ELane.HT, ELane.BD, ELane.LT, ELane.FT, ELane.CY, ELane.RD]
        };

        if (CDTXMania.ConfigIni.eRDPosition == ERDPosition.RDRC)
        {
            columns[8] = ELane.RD;
            columns[9] = ELane.CY;
        }

        return columns;
    }

    //the chart is folded into fewer lanes at load, eg: with 9 lanes RD chips become CY or LC
    private static bool IsUsed(ELane lane) => CDTXMania.ConfigIni.eNumOfLanes.Drums switch
    {
        EType.B => lane != ELane.RD,
        EType.C => lane is ELane.HH or ELane.SD or ELane.BD or ELane.HT or ELane.LT or ELane.CY,
        _ => true
    };

    private static UIGroup BuildDefault()
    {
        UIGroup root = new(ComponentName);
        root.AddChild(DefaultLanes());
        root.AddChild(DefaultPads());
        root.AddChild(DefaultScore(DefaultBonus()));
        Add(root, new PerformanceShutters(EInstrumentPart.DRUMS), "Shutters", 295, 0, CStagePerfCommonScreen.LaneOverlayLayer, "Stats.HasLane");
        Add(root, new PerformanceJudgeLine(EInstrumentPart.DRUMS), "JudgeLine", 0, 0, CStagePerfCommonScreen.LaneOverlayLayer, "Stats.HasLane");

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

        PerformanceElement gauge = Add(root, new PerformanceGauge(EInstrumentPart.DRUMS), "Gauge", 296, 626, CStagePerfCommonScreen.PanelLayer, "Stats.HasLane");
        gauge.bindings.Add(new UIBinding("position.Y", "Config.Reverse.Drums")
        {
            condition = BindingCondition.IsTrue, whenTrue = 28, whenFalse = 626
        });

        return root;
    }

    private static LaneGroup DefaultLanes()
    {
        LaneGroup laneGroup = new()
        {
            position = new Vector3(296, 0, 0),
            size = new Vector2(557, 720),
            renderOrder = CStagePerfCommonScreen.LaneArtLayer,
            bindings = { new UIBinding("isVisible", "Stats.HasLane") },
            lanes =
            {
                Column(0, 68, 0, 4, ELane.LC),
                Column(1, 50, 0, 0, ELane.HH),
                Column(2, 52, 0, 0, ELane.LP),
                Column(3, 58, 0, 0, ELane.SD),
                Column(4, 50, 0, 0, ELane.HT),
                Column(5, 64, 5, 0, ELane.BD),
                Column(6, 50, 0, 0, ELane.LT),
                Column(7, 50, 0, 0, ELane.FT),
                Column(8, 68, 4, 0, ELane.CY),
                Column(9, 40, 0, 0, ELane.RD)
            }
        };

        return laneGroup;

        static Lane Column(int art, float width, float spaceLeft, float spaceRight, ELane lane) => new()
        {
            lane = lane,
            background = SkinResource.System($@"Graphics\Performance\Lane\lane{art}.png"),
            width = width,
            spaceLeft = spaceLeft,
            spaceRight = spaceRight
        };
    }

    private static ScoreBonus DefaultBonus() => new()
    {
        position = new Vector3(160, 80, 0)
    };

    //CY draws last, so with RD RC the ride sits under it
    private static DrumPads DefaultPads()
    {
        DrumPads drumPads = new()
        {
            position = new Vector3(0, 570, 0),
            renderOrder = CStagePerfCommonScreen.LegacyOverlayLayer,
            bindings =
            {
                new UIBinding("position.Y", "Config.Reverse.Drums") { condition = BindingCondition.IsTrue, whenTrue = 70, whenFalse = 570 }
            },
            pads =
            {
                Pad(ELane.LC, 0, -2.5f, 3, new Color4(1.16f, 0.74f, 0.99f)),
                Pad(ELane.HH, 1, -2, 4, new Color4(0.5f, 0.85f, 1.16f)),
                Pad(ELane.SD, 3, -1, 11, new Color4(1.16f, 1.06f, 0.52f)),
                Pad(ELane.BD, 5, -1, 7, new Color4(0.89f, 0.63f, 1.16f)),
                Pad(ELane.HT, 4, -1, 15, new Color4(0.53f, 0.96f, 0.61f)),
                Pad(ELane.LT, 6, 1, 18, new Color4(1.16f, 0.8f, 0.76f)),
                Pad(ELane.FT, 7, 1, 11, new Color4(1.16f, 0.92f, 0.54f)),
                Pad(ELane.RD, 9, 14, 2, new Color4(0.5f, 0.85f, 1.16f)),
                Pad(ELane.LP, 2, -2, 7, new Color4(1.16f, 0.77f, 0.99f)),
                Pad(ELane.CY, 8, 4, 2, new Color4(0.5f, 0.85f, 1.16f))
            }
        };

        return drumPads;

        static DrumPad Pad(ELane lane, int art, float offsetX, float offsetY, Color4 litColour) => new()
        {
            lane = lane,
            image = SkinResource.System($@"Graphics\Pad\pad{art}.png"),
            offset = new Vector2(offsetX, offsetY),
            litColour = litColour
        };
    }
}
