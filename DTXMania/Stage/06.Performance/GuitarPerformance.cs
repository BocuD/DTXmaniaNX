using DTXMania.Core;

namespace DTXMania;

internal enum EGuitarEvent
{
    Note,
    Wail
}

internal struct GuitarEvent
{
    public EGuitarEvent Kind;

    public CChip? Chip;

    public long Time;

    public bool Miss;

    //bit per lane, R G B Y P from bit 0
    public int FireLanes;
}

internal class GuitarPerformance
{
    private const long WailingWindowMs = 1000;

    //1s past the line at 234px per second
    private const int WailingMarkerOutDistance = -234;

    private const int AllLanes = 0b11111;

    public readonly EInstrument Player;
    public readonly EChartPart Chart;
    public readonly PerformanceData Data;

    public readonly List<GuitarEvent> Events = [];

    private readonly ChipTimeline timeline;
    private readonly bool autoPick;
    private readonly bool autoWail;
    private readonly int autoLanes;

    private readonly Queue<CChip> armedWailing = new();

    public GuitarPerformance(EInstrument player, EChartPart chart, PerformanceData data, ChipTimeline timeline, STAUTOPLAY auto)
    {
        Player = player;
        Chart = chart;
        Data = data;
        this.timeline = timeline;

        bool guitarSide = player == EInstrument.Guitar1;
        autoPick = guitarSide ? auto.GtPick : auto.BsPick;
        autoWail = guitarSide ? auto.GtW : auto.BsW;
        autoLanes = Lanes(guitarSide ? auto.GtR : auto.BsR, guitarSide ? auto.GtG : auto.BsG, guitarSide ? auto.GtB : auto.BsB,
            guitarSide ? auto.GtY : auto.BsY, guitarSide ? auto.GtP : auto.BsP);
    }

    //the player's settings, pads and lanes are still stored as guitar and bass
    public EInstrumentPart Side => Player == EInstrument.Guitar1 ? EInstrumentPart.GUITAR : EInstrumentPart.BASS;

    private EInstrumentPart Part => (EInstrumentPart)Chart;

    private static int Lanes(bool r, bool g, bool b, bool y, bool p) => (r ? 1 : 0) | (g ? 2 : 0) | (b ? 4 : 0) | (y ? 8 : 0) | (p ? 16 : 0);

    private static int LanesOf(CChip chip)
    {
        bool[] lanes = EnumConverter.GetArrayBoolFromEChannel(chip.nChannelNumber);
        return Lanes(lanes[0], lanes[1], lanes[2], lanes[3], lanes[4]);
    }

    public void UpdateNote(CChip chip)
    {
        if (chip.bHit || !autoPick)
        {
            return;
        }

        int side = (int)Side;
        long ghostLag = 0;
        List<int>? ghost = CDTXMania.listAutoGhostLag[(int)Part];
        if (CDTXMania.ConfigIni.eAutoGhost[(int)Part] != EAutoGhostData.PERFECT && ghost != null &&
            0 <= chip.n楽器パートでの出現順 && chip.n楽器パートでの出現順 < ghost.Count)
        {
            //low byte is the lag + 128, anything above it breaks the combo
            int recorded = ghost[chip.n楽器パートでの出現順];
            ghostLag = (recorded & 255) - 128 - CDTXMania.ConfigIni.nInputAdjustTimeMs[side];
            if (chip.nPlaybackTimeMs + ghostLag > AudioMixer.Timer.nCurrentTime)
            {
                return;
            }
            if (recorded > 255)
            {
                Data.BreakCombo();
            }
        }
        else if (chip.nDistanceFromBar[side] >= 0)
        {
            return;
        }

        int chipLanes = LanesOf(chip);
        int pushed = Lanes(CDTXMania.Pad.bPressing(Side, EPad.R), CDTXMania.Pad.bPressing(Side, EPad.G), CDTXMania.Pad.bPressing(Side, EPad.B),
            CDTXMania.Pad.bPressing(Side, EPad.Y), CDTXMania.Pad.bPressing(Side, EPad.P));

        bool openPlayed = chip.bChipIsOpenNote && (pushed & ~autoLanes) == 0;
        bool miss = chipLanes != autoLanes && ((chipLanes ^ pushed) & ~autoLanes) != 0 && !openPlayed;

        chip.bHit = true;
        long time = chip.nPlaybackTimeMs + ghostLag;
        Events.Add(new GuitarEvent
        {
            Kind = EGuitarEvent.Note,
            Chip = chip,
            Time = time,
            Miss = miss,
            FireLanes = openPlayed ? AllLanes : chipLanes & (autoLanes | pushed)
        });

        if (!miss)
        {
            ArmWailing(time + CDTXMania.ConfigIni.nInputAdjustTimeMs[side]);
        }
    }

    public void ArmWailing(long time)
    {
        EChannel marker = Chart == EChartPart.Guitar ? EChannel.Guitar_Wailing : EChannel.Bass_Wailing;
        CChip? chip = timeline.Nearest(time, marker, 140, HitState.NotHit);
        if (chip != null)
        {
            armedWailing.Enqueue(chip);
        }
    }

    public void UpdateWailingMarker(CChip marker)
    {
        float distance = marker.nDistanceFromBar[(int)Side];
        if (marker.bHit || distance >= 0)
        {
            return;
        }

        if (distance < WailingMarkerOutDistance)
        {
            marker.bHit = true;
        }
        if (autoWail)
        {
            Wail(marker.nPlaybackTimeMs);
        }
    }

    public void Wail(long time)
    {
        while (armedWailing.Count > 0)
        {
            CChip marker = armedWailing.Peek();
            long since = time - marker.nPlaybackTimeMs;

            if (since < 0)
            {
                return;
            }

            armedWailing.Dequeue();

            if (since > WailingWindowMs)
            {
                continue;
            }

            marker.bHit = true;
            Events.Add(new GuitarEvent { Kind = EGuitarEvent.Wail, Chip = SoundForWailing(marker) });
            if (!autoWail)
            {
                AddWailingScore();
            }

            return;
        }
    }

    public CChip? NextNote()
    {
        int adjust = autoPick ? 0 : CDTXMania.ConfigIni.nInputAdjustTimeMs[(int)Side];
        return timeline.NearestNote(AudioMixer.Timer.nCurrentTime + adjust, Part, 800, HitState.NotHit, preferPast: true);
    }

    private CChip? SoundForWailing(CChip marker) =>
        Chart == EChartPart.Guitar ? timeline.Nearest(marker.nPlaybackTimeMs, EChannel.Guitar_WailingSound, 0, HitState.DontCare) : null;

    private void AddWailingScore()
    {
        if (CDTXMania.ConfigIni.nSkillMode == 0)
        {
            Data.AddScore(Math.Min(Data.Combo, 500) * 3000L);
            return;
        }

        Data.AddScore(Data.Combo > 500 ? 50000 : Data.Combo * 100);
    }

    public bool HasTargetGhost => CDTXMania.ConfigIni.eTargetGhost[(int)Part] != ETargetGhostData.NONE && CDTXMania.listTargetGhsotLag[(int)Part] != null;

    public double TargetGhostGoal()
    {
        int notes = CDTXMania.DTX.nVisibleChipsCount[(int)Part];
        HitCounts ghost = Data.GhostHitCount;
        double goal;
        if (CDTXMania.ConfigIni.eTargetGhost[(int)Part] == ETargetGhostData.ONLINE)
        {
            goal = notes > 0 ? 100 * (ghost.Perfect * 17 + ghost.Great * 7 + Data.GhostMaxCombo * 3) / (20.0 * notes) : 0;
        }
        else if (CDTXMania.ConfigIni.nSkillMode == 0)
        {
            goal = CScoreIni.tCalculatePlayingSkillOld(notes, ghost.Perfect, ghost.Great, ghost.Good, ghost.Poor, ghost.Miss,
                Data.GhostMaxCombo, Part, new STAUTOPLAY());
        }
        else
        {
            goal = CScoreIni.tCalculatePlayingSkill(notes, ghost.Perfect, ghost.Great, ghost.Good, ghost.Poor, ghost.Miss,
                Data.GhostMaxCombo, Part, new STAUTOPLAY());
        }

        return Math.Clamp(goal, 0, 100);
    }
}
