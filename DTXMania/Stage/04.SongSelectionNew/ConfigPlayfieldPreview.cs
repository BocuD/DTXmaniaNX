using System.Numerics;
using DTXMania.Core;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using FDK;

namespace DTXMania;

internal sealed class ConfigPlayfieldPreview : UIGroup
{
    public float drumsX;
    public float? guitarX;

    private const int PassMs = 30000;

    private const int LeadInMs = 1500;

    private readonly ChipTimeline timeline = new();
    private readonly List<CActivity> activities = [];

    public string? chartPath;

    private CDTX? chart;
    private bool chartRead;
    private string? readPath;

    private EPlayer? shown;
    private EInstrumentPart part;
    private Playfield? playfield;
    private DrumsPlayfield? drums;
    private GuitarPlayfield? guitar;
    private CActPerfGuitarLaneFlushGB? guitarFlush;
    private int firstFlushLane;

    private long firstNoteMs;
    private long startMs;
    private long timeMs;
    private long lastTick;

    public ConfigPlayfieldPreview() : base("Playfield Preview")
    {
        dontSerialize = true;
        renderOrder = 60;
        parentAnchor = UICanvas.Center;
        position = UICanvas.FromCenter(0, 0);
        size = new Vector2(1280, 720);
        isVisible = false;
    }

    public void Show(EPlayer? player)
    {
        if (player == shown)
        {
            return;
        }

        Tear();
        shown = null;
        isVisible = false;

        if (player == null)
        {
            return;
        }

        Read();
        Build(player.Value);
        shown = player;
        isVisible = true;
        Restart();
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        Update();
        base.Draw(parentMatrix);
    }

    private void Update()
    {
        if (shown == null || playfield == null)
        {
            return;
        }

        playfield.position.X = drums != null ? drumsX : guitarX ?? guitar!.SlotX;

        EPlayer player = shown.Value;
        if (LayoutChanged(player))
        {
            Rebuild(player);
            return;
        }

        long now = CDTXMania.Timer.nCurrentTime;
        timeMs += Math.Clamp(now - lastTick, 0, 100);
        lastTick = now;

        if (timeMs >= startMs + PassMs || timeline.TopChip < 0 || timeline.TopChip >= timeline.Chips.Count)
        {
            Restart();
        }

        STDGBVALUE<double> scroll = new();
        for (int i = 0; i < 3; i++)
        {
            scroll[i] = CDTXMania.ConfigIni.nScrollSpeed[i];
        }

        int partIndex = (int)part;
        for (int i = timeline.TopChip; i >= 0 && i < timeline.Chips.Count; i++)
        {
            CChip chip = timeline.Chips[i];
            chip.ComputeDistanceFromBar(timeMs, scroll);
            if (chip.nDistanceFromBar[partIndex] > ChipRenderer.DrawRange)
            {
                break;
            }

            if (i == timeline.TopChip && timeline.TryAdvance())
            {
                continue;
            }

            if (chip.bHit || chip.nDistanceFromBar[partIndex] > 0)
            {
                continue;
            }

            if (IsNote(chip))
            {
                Hit(chip);
            }
            else
            {
                chip.bHit = true;
            }
        }
    }

    private bool IsNote(CChip chip) => part switch
    {
        EInstrumentPart.DRUMS => chip.bDrums可視チップ,
        EInstrumentPart.GUITAR => chip.bGuitar可視チップ,
        _ => chip.bBass可視チップ
    };

    private void Hit(CChip chip)
    {
        chip.bHit = true;

        if (drums != null)
        {
            ELane lane = (ELane)DrumsPerformance.LaneOf(chip);
            drums.Hit(lane, DrumsPerformance.PadOf(chip), CInputManager.nDefaultVelocity / 127f);
            drums.Judged((int)lane, EJudgement.Perfect, 0);
            drums.StartFire(lane, false, false);
            return;
        }

        if (guitar == null)
        {
            return;
        }

        for (int lane = 0; lane < 5; lane++)
        {
            if ((chip.frets & (1 << lane)) != 0)
            {
                guitar.StartFire(lane, chip);
                guitarFlush?.Start(firstFlushLane + lane);
            }
        }

        guitar.Judged(EJudgement.Perfect, 0);
    }

    private void Restart()
    {
        startMs = Math.Max(0, firstNoteMs - LeadInMs);
        timeMs = startMs;
        lastTick = CDTXMania.Timer.nCurrentTime;
        timeline.Seek(startMs);
    }

    private void Build(EPlayer player)
    {
        part = CStagePerfCommonScreen.PartOf(player);

        CActPerfProgressBar progress = Activated(new CActPerfProgressBar(true));

        if (player == EPlayer.Drums)
        {
            CActPerfDrumsJudgementString judgements = Activated(new CActPerfDrumsJudgementString());
            CActPerfDrumsLaneFlushD flush = Activated(new CActPerfDrumsLaneFlushD());

            drums = AddChild(new DrumsPlayfield());
            drums.Preview();
            drums.SetParts(null, null, progress, judgements);
            drums.Setup(flush);
            playfield = drums;
        }
        else
        {
            CActPerfGuitarJudgementString judgements = Activated(new CActPerfGuitarJudgementString());
            guitarFlush = Activated(new CActPerfGuitarLaneFlushGB());
            firstFlushLane = player == EPlayer.Guitar1 ? 0 : 5;

            guitar = AddChild(new GuitarPlayfield(player));
            guitar.Preview();
            guitar.SetParts(null, null, progress, judgements);
            guitar.Setup(guitarFlush, Activated(new CActPerfGuitarWailingBonus()));
            playfield = guitar;
        }

        playfield.dontSerialize = true;
        playfield.SetChart(timeline);

        RememberLayout(player);

        firstNoteMs = 0;
        foreach (CChip chip in timeline.Chips)
        {
            if (IsNote(chip))
            {
                firstNoteMs = chip.nPlaybackTimeMs;
                break;
            }
        }
    }

    private EType laneType;
    private EType laneCount;
    private bool laneGaps;
    private ERDPosition ridePosition;
    private bool classicFire;
    private bool lefty;

    private bool LayoutChanged(EPlayer player)
    {
        CConfigIni config = CDTXMania.ConfigIni;
        return laneType != config.eLaneType.Drums
               || laneCount != config.eNumOfLanes.Drums
               || laneGaps != config.bDrumsLaneGaps
               || ridePosition != config.eRDPosition
               || classicFire != config.bClassicDrumsFire
               || lefty != config.bLeft[(int)player];
    }

    private void RememberLayout(EPlayer player)
    {
        CConfigIni config = CDTXMania.ConfigIni;
        laneType = config.eLaneType.Drums;
        laneCount = config.eNumOfLanes.Drums;
        laneGaps = config.bDrumsLaneGaps;
        ridePosition = config.eRDPosition;
        classicFire = config.bClassicDrumsFire;
        lefty = config.bLeft[(int)player];
    }

    private void Rebuild(EPlayer player)
    {
        long at = timeMs;
        Tear();
        Build(player);
        timeMs = at;
        lastTick = CDTXMania.Timer.nCurrentTime;
        timeline.Seek(at);
    }

    private T Activated<T>(T activity) where T : CActivity
    {
        activity.OnActivate();
        activities.Add(activity);
        return activity;
    }

    private void Tear()
    {
        foreach (CActivity activity in activities)
        {
            activity.OnDeactivate();
        }

        activities.Clear();
        ClearChildren();

        playfield = null;
        drums = null;
        guitar = null;
        guitarFlush = null;
    }

    private void Read()
    {
        if (chartRead && readPath == chartPath)
        {
            return;
        }

        chartRead = true;
        readPath = chartPath;
        chart = null;

        if (string.IsNullOrEmpty(chartPath))
        {
            timeline.Load(null);
            return;
        }

        chart = new CDTX(chartPath, false, 1.0, 0);
        CDTXMania.DTX = chart;
        timeline.Load(chart.listChip);
    }
}
