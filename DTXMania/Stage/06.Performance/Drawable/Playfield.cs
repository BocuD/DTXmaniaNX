using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;

namespace DTXMania;

//Lanes, chips and effects for one player; restricted to rendering and display
//Context and state is passed through the playfield
public abstract class Playfield : UIGroup
{
    protected EInstrumentPart part;
    
    protected readonly PerformanceStats stats = new();
    private readonly UIDataContext data = new();

    //child elements reference these
    protected readonly LaneLayout layout = new();
    protected ChipRenderer? chipRenderer;
    protected LoopMarkers? loopMarkers;
    protected ScoreDisplay scoreDisplay = null!;

    //the chart does not change while the song plays
    private ChipTimeline? timeline;

    //activities the stage keeps alive, drawn here because they belong to this player
    private CActPerfCommonScore score = null!;
    private CActPerfCommonCombo combo = null!;
    internal CActPerfProgressBar progressBar = null!;
    internal CActPerfCommonJudgementString judgements = null!;

    protected static readonly int probeLanes = AllocationProbe.Register("lanes");
    protected static readonly int probeJudgement = AllocationProbe.Register("judgement string");

    protected Playfield(EPlayer player) : base("Playfield")
    {
        size = new Vector2(1280, 720);

        stats.Player = player;
        data.RegisterObject("Stats", () => stats);
        dataContext = data;
    }

    //LaneDisp: 0 all on, 1 lane off, 2 line off, 3 all off
    protected bool ShowsLaneArt => CDTXMania.ConfigIni.nLaneDisp[(int)part] is 0 or 2;
    protected bool ShowsLines => CDTXMania.ConfigIni.nLaneDisp[(int)part] is 0 or 1;

    //the player moves the judge line while the song plays, so the parts take it again every frame
    protected abstract int JudgeLineY { get; }

    internal ScoreDisplay Score => scoreDisplay;

    internal void SetParts(CActPerfCommonScore playerScore, CActPerfCommonCombo playerCombo,
        CActPerfProgressBar bar, CActPerfCommonJudgementString judgementStrings)
    {
        score = playerScore;
        combo = playerCombo;
        progressBar = bar;
        judgements = judgementStrings;
        judgements.lanes = layout;
    }

    protected void ShowJudgement(int lane, EJudgement judgement, int lag)
    {
        judgements.Start(lane, judgement, lag);
    }

    //marks where on the progress bar this player hit
    internal void Progress(int timeMs, EJudgement judgement)
    {
        progressBar.Hit(part, timeMs, judgement);
    }

    internal void SetChart(ChipTimeline chart)
    {
        timeline = chart;
        ApplyChart();
    }

    //the chip renderer and the markers are made again when the layout reloads
    protected void ApplyChart()
    {
        if (chipRenderer != null)
        {
            chipRenderer.timeline = timeline;
        }

        if (loopMarkers != null)
        {
            loopMarkers.part = part;
        }
    }

    //can be null if there are no loop points set; these behave and scroll like chips
    internal void SetLoop(CChip? begin, CChip? end)
    {
        if (loopMarkers != null)
        {
            loopMarkers.begin = begin;
            loopMarkers.end = end;
        }
    }

    //the markers span the lanes. Their text sits at a fixed place on screen
    //chipCentreY: where a chip at distance 0 is centred
    protected void PlaceLoopMarkers(int screenTextX, int chipCentreY, bool scrollsDown)
    {
        if (loopMarkers == null)
        {
            return;
        }

        loopMarkers.position.X = layout.SpanLeft;
        loopMarkers.size.X = layout.SpanWidth;
        loopMarkers.textX = screenTextX - (int)layout.SpanLeft;
        loopMarkers.showsLines = ShowsLines;
        loopMarkers.judgeLineY = chipCentreY;
        loopMarkers.scrollsDown = scrollsDown;
    }

    //parts made at run time. They are not saved with the skin
    protected T AddPart<T>(T made, string partName, int renderOrder) where T : UIDrawable
    {
        made.name = partName;
        made.renderOrder = renderOrder;
        made.dontSerialize = true;
        return AddChild(made);
    }

    //a part an activity still draws
    protected LegacyDrawable AddLegacyPart(string partName, int renderOrder, int probe, Action<Matrix4x4> draw)
    {
        return AddPart(new LegacyDrawable(partName, origin =>
        {
            AllocationProbe.Begin(probe);
            draw(origin);
            AllocationProbe.End(probe);
        }), partName, renderOrder);
    }

    protected void SetUpScore(float scoreX, float comboX)
    {
        scoreDisplay.Setup(score, combo, part, scoreX, comboX);
    }

    protected static ScoreDisplay DefaultScore(ScoreBonus bonus)
    {
        ScoreDisplay display = new() { renderOrder = CStagePerfCommonScreen.ScoreLayer };
        display.AddChild(bonus);
        return display;
    }

    public static UIBinding OnRightSide(string target, double right, double left) => new(target, "Stats.Player")
    {
        condition = BindingCondition.Equal, value = (int)EPlayer.Guitar2, whenTrue = right, whenFalse = left
    };

    protected static PerformanceElement Add(UIGroup parent, PerformanceElement element, string name, float x, float y, int renderOrder, string shownWhen)
    {
        parent.AddChild(element);
        element.name = name;
        element.position = new Vector3(x, y, 0);
        element.renderOrder = renderOrder;
        element.bindings.Add(new UIBinding("isVisible", shownWhen));
        return element;
    }
}
