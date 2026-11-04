using System.Numerics;
using DTXMania.Core;
using DTXMania.Drawable;
using DTXMania.UI.Drawable;
using DTXMania.UI.Inspector;
using DTXMania.UI.Skin;
using Hexa.NET.ImGui;

namespace DTXMania;

//one per guitar player
public class GuitarPlayfield : Playfield
{
    [AddChildMenu("Game/Performance/Guitar Playfield")]
    public new static UIDrawable Create() => new GuitarPlayfield();

    private const string ComponentName = "GuitarPlayfield";

    [Themable] public EPlayer player;

    public GuitarPlayfield() : this(EPlayer.Guitar1)
    {
    }

    public GuitarPlayfield(EPlayer player) : base(player)
    {
        this.player = player;
        name = player.ToString();
        MakeComponent(ComponentName, BuildDefault);
    }

    public override void OnDeserialize()
    {
        base.OnDeserialize();
        stats.Player = player;
        MakeComponent(ComponentName, BuildDefault);
    }

    private LaneGroup lanes = null!;
    private CActPerfGuitarLaneFlushGB laneFlush = null!;
    private CActPerfGuitarWailingBonus wailingBonus = null!;

    private GuitarChipRenderer? guitarChips;
    private ActPerfNewFire? fire;
    private WailingEffect? wailing;

    //the judgement text numbers guitar 13 and bass 14
    private int judgementLane;

    internal void StartFire(int lane, CChip chip)
    {
        fire?.Start(lane, chip);
    }

    internal void Wail(CChip chip)
    {
        wailingBonus.Start(player, chip);
        wailing?.Play();
    }

    //this player has one judgement lane, unlike a drums playfield
    internal void Judged(EJudgement judgement, int lag)
    {
        ShowJudgement(judgementLane, judgement, lag);
    }

    internal void Setup(CActPerfGuitarLaneFlushGB flush, CActPerfGuitarWailingBonus wailing)
    {
        part = CStagePerfCommonScreen.PartOf(player);
        judgementLane = 12 + (int)player;
        laneFlush = flush;
        wailingBonus = wailing;
        EnsureContent();

        lanes.order = Frets(CDTXMania.ConfigIni.bLeft[(int)player]);
        lanes.Arrange();

        AddLegacyPart("LaneFlush", CStagePerfCommonScreen.LegacyLanesLayer, probeLanes, origin => laneFlush.Draw(origin, player));
        AddLegacyPart("WailingBonus", CStagePerfCommonScreen.LegacyLanesLayer, probeLanes, origin => wailingBonus.Draw(origin, player));
        AddLegacyPart("ProgressBar", CStagePerfCommonScreen.LegacyLanesLayer, probeLanes, origin => progressBar.Draw(origin, part, part));

        loopMarkers = AddPart(new LoopMarkers(), "LoopMarkers", CStagePerfCommonScreen.ChipsLayer);
        loopMarkers.size.Y = 720;

        guitarChips = AddPart(new GuitarChipRenderer(player, part), "Chips", CStagePerfCommonScreen.ChipsLayer);
        guitarChips.lanes = layout;
        chipRenderer = guitarChips;

        AddLegacyPart("JudgementStrings", CStagePerfCommonScreen.LegacyOverlayLayer, probeJudgement,
            _ => judgements.DrawLanes(judgementLane, judgementLane));

        //the right player's score and combo sit beside its panel, not 870 to the right
        bool right = player == EPlayer.Guitar2;
        SetUpScore(right ? -578 : 0, right ? -585 : 0);

        fire = AddPart(new ActPerfNewFire(right ? 2 : 1), "ChipFire", CStagePerfCommonScreen.EffectsLayer);
        this.wailing = AddPart(new WailingEffect(), "WailingEffect", CStagePerfCommonScreen.LaneArtLayer);
        this.wailing.position = new Vector3(242, 58, 0);

        judgements.InitUI(this, judgementLane, judgementLane, position.X);
    }

    protected override int JudgeLineY => CDTXMania.ConfigIni.bReverse[(int)player]
        ? 611 - CDTXMania.ConfigIni.nJudgeLine[(int)player]
        : 154 + CDTXMania.ConfigIni.nJudgeLine[(int)player];

    protected override void OnContentLoaded()
    {
        lanes = GetChild<LaneGroup>("Lanes") ?? AddChild(DefaultLanes());
        lanes.layout = layout;
        scoreDisplay = GetChild<ScoreDisplay>("Score") ?? AddChild(DefaultScore(DefaultBonus()));
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        EnsureContent();
        lanes.Arrange();

        //guitar chips are centred 10 below the judge line, or 1 below it in reverse
        bool reverse = CDTXMania.ConfigIni.bReverse[(int)player];
        PlaceLoopMarkers(60, JudgeLineY + (reverse ? 1 : 10), reverse);

        if (guitarChips != null)
        {
            guitarChips.judgeLineY = JudgeLineY;
        }

        base.Draw(parentMatrix);
    }

    private static ELane[] Frets(bool lefty) => lefty
        ? [ELane.GtP, ELane.GtY, ELane.GtB, ELane.GtG, ELane.GtR]
        : [ELane.GtR, ELane.GtG, ELane.GtB, ELane.GtY, ELane.GtP];

    //layout only: the lane art is one image, drawn by the Lane element
    private static LaneGroup DefaultLanes()
    {
        LaneGroup laneGroup = new()
        {
            position = new Vector3(88, 0, 0),
            size = new Vector2(195, 720),
            renderOrder = CStagePerfCommonScreen.LaneArtLayer,
            lineOverlap = 0,
            lanes =
            {
                Fret(ELane.GtR), Fret(ELane.GtG), Fret(ELane.GtB), Fret(ELane.GtY), Fret(ELane.GtP)
            }
        };

        return laneGroup;

        static Lane Fret(ELane lane) => new() { lane = lane, width = 39 };
    }

    //the hold bonus rises over its half second
    private static ScoreBonus DefaultBonus()
    {
        ScoreBonus bonus = new()
        {
            position = new Vector3(333, 45, 0),
            image = SkinResource.System(@"Graphics\7_Bonus_100.png"),
            durationMs = 500,
            riseBy = 20
        };

        bonus.bindings.Add(OnRightSide("position.X", 15, 333));
        return bonus;
    }

    private static UIGroup BuildDefault()
    {
        UIGroup root = new(ComponentName);
        root.AddChild(DefaultLanes());
        root.AddChild(DefaultScore(DefaultBonus()));
        Add(root, new PerformanceLane(EPlayer.Guitar1), "Lane", 67, 42, CStagePerfCommonScreen.LaneArtLayer, "Stats.HasLane");
        Add(root, new PerformanceLaneCaps(EPlayer.Guitar1), "LaneCaps", 67, 42, CStagePerfCommonScreen.LaneOverlayLayer, "Stats.HasLane");
        Add(root, new PerformanceShutters(EPlayer.Guitar1), "Shutters", 80, 0, CStagePerfCommonScreen.LaneOverlayLayer, "Stats.HasLane");
        Add(root, new PerformanceJudgeLine(EPlayer.Guitar1), "JudgeLine", 0, 0, CStagePerfCommonScreen.LaneOverlayLayer, "Stats.HasLane");

        PerformanceElement status = Add(root, new PerformanceStatusPanel(EPlayer.Guitar1), "Status", 373, 254, CStagePerfCommonScreen.PanelLayer, "Stats.ShowsPanel");
        status.bindings.Add(OnRightSide("position.X", -205, 373));

        Add(root, new PerformanceGauge(EPlayer.Guitar1), "Gauge", 79, 0, CStagePerfCommonScreen.PanelLayer, "Stats.HasLane");
        return root;
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
