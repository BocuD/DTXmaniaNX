using DTXMania.Core;
using FDK;

namespace DTXMania;

internal enum EGuitarEvent
{
    AutoNote,
    PickedNote,
    EmptyPick,
    HoldFire,
    HoldBonus,
    HoldRelease,
    Judged,
    Wail
}

internal struct GuitarEvent
{
    public EGuitarEvent Kind;

    public CChip? Chip;

    public long Time;

    public bool Miss;

    public bool ShiftPitch;

    public EJudgement Judgement;

    public bool Auto;

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

    public CChip? EmptyPickSound;
    public CChip? HeldNote { get; private set; }
    public int LitLanes { get; private set; }

    private readonly ChipTimeline timeline;
    private readonly bool autoPick;
    private readonly bool autoWail;
    private readonly int autoLanes;

    private readonly Queue<CChip> armedWailing = new();
    private readonly List<STInputEvent> padEvents = [];

    private int holdDurationMs;
    private int holdBonuses;

    public GuitarPerformance(EInstrument player, EChartPart chart, PerformanceData data, ChipTimeline timeline, STAUTOPLAY auto)
    {
        Player = player;
        Chart = chart;
        Data = data;
        this.timeline = timeline;

        bool guitarSide = player == EInstrument.Guitar1;
        autoPick = guitarSide ? auto.GtPick : auto.BsPick;
        autoWail = guitarSide ? auto.GtW : auto.BsW;
        autoLanes = ToLaneMask(guitarSide ? auto.GtR : auto.BsR, guitarSide ? auto.GtG : auto.BsG, guitarSide ? auto.GtB : auto.BsB,
            guitarSide ? auto.GtY : auto.BsY, guitarSide ? auto.GtP : auto.BsP);
    }

    //the player's settings, pads and lanes are still stored as guitar and bass
    public EInstrumentPart Side => Player == EInstrument.Guitar1 ? EInstrumentPart.GUITAR : EInstrumentPart.BASS;

    private EInstrumentPart Part => (EInstrumentPart)Chart;

    private int InputAdjustMs => CDTXMania.ConfigIni.nInputAdjustTimeMs[(int)Side];

    private static int ToLaneMask(bool r, bool g, bool b, bool y, bool p) => (r ? 1 : 0) | (g ? 2 : 0) | (b ? 4 : 0) | (y ? 8 : 0) | (p ? 16 : 0);

    private static int GetLanes(CChip chip)
    {
        bool[] lanes = EnumConverter.GetArrayBoolFromEChannel(chip.nChannelNumber);
        return ToLaneMask(lanes[0], lanes[1], lanes[2], lanes[3], lanes[4]);
    }

    private int GetPressedLanes() => ToLaneMask(CDTXMania.Pad.bPressing(Side, EPad.R), CDTXMania.Pad.bPressing(Side, EPad.G),
        CDTXMania.Pad.bPressing(Side, EPad.B), CDTXMania.Pad.bPressing(Side, EPad.Y), CDTXMania.Pad.bPressing(Side, EPad.P));

    private EJudgement JudgeChip(long time, CChip? chip, int inputAdjustMs)
    {
        if (chip == null)
        {
            return EJudgement.Miss;
        }

        chip.nLag = (int)(time + inputAdjustMs - chip.nPlaybackTimeMs);
        if (chip.eInstrumentPart != EInstrumentPart.UNKNOWN)
        {
            chip.nCurrentComboForGhost = Data.Combo;
        }
        return ChipJudge.GetJudgementForLag(chip, chip.nLag);
    }

    private bool IsAuto(CChip chip) =>
        autoPick && (GetLanes(chip) & ~autoLanes) == 0 && (!chip.bChipIsWailingNote || autoWail) && (!chip.bChipIsOpenNote || autoLanes == AllLanes);

    //correctLane false is a miss without judging the timing
    private void ApplyHit(CChip chip, long time, bool correctLane)
    {
        chip.bHit = true;
        if (chip.bIsLongNote)
        {
            chip.bIsHittingLongNote = true;
            StartHold(chip);
        }

        bool auto = IsAuto(chip);
        chip.bIsAutoPlayed = auto;
        EJudgement judgement = correctLane ? JudgeChip(time, chip, auto ? 0 : InputAdjustMs) : EJudgement.Miss;
        Data.ApplyJudgement(judgement, auto, chip.nLag, countsCombo: true);
        if (judgement == EJudgement.Miss)
        {
            chip.bIsHittingLongNote = false;
            EndHold();
        }

        Events.Add(new GuitarEvent { Kind = EGuitarEvent.Judged, Chip = chip, Judgement = judgement, Auto = auto });
    }

    public void UpdateNote(CChip chip)
    {
        long now = AudioMixer.Timer.nCurrentTime;
        if (!chip.bHit && chip.nDistanceFromBar[(int)Side] < 0 && JudgeChip(now, chip, IsAuto(chip) ? 0 : InputAdjustMs) == EJudgement.Miss)
        {
            ApplyHit(chip, now, true);
        }

        if (chip.bIsLongNote && chip.bVisible && chip.chipLongNoteEndPosition.nDistanceFromBar[(int)Side] <= 0)
        {
            return;
        }

        if (chip.bHit || !autoPick)
        {
            return;
        }

        long ghostLag = 0;
        List<int>? ghost = CDTXMania.listAutoGhostLag[(int)Part];
        if (CDTXMania.ConfigIni.eAutoGhost[(int)Part] != EAutoGhostData.PERFECT && ghost != null &&
            0 <= chip.n楽器パートでの出現順 && chip.n楽器パートでの出現順 < ghost.Count)
        {
            //low byte is the lag + 128, anything above it breaks the combo
            int recorded = ghost[chip.n楽器パートでの出現順];
            ghostLag = (recorded & 255) - 128 - InputAdjustMs;
            if (chip.nPlaybackTimeMs + ghostLag > now)
            {
                return;
            }
            if (recorded > 255)
            {
                Data.BreakCombo();
            }
        }
        else if (chip.nDistanceFromBar[(int)Side] >= 0)
        {
            return;
        }

        int chipLanes = GetLanes(chip);
        int pressed = GetPressedLanes();
        bool openPlayed = chip.bChipIsOpenNote && (pressed & ~autoLanes) == 0;
        bool miss = chipLanes != autoLanes && ((chipLanes ^ pressed) & ~autoLanes) != 0 && !openPlayed;

        chip.bHit = true;
        long time = chip.nPlaybackTimeMs + ghostLag;
        Events.Add(new GuitarEvent
        {
            Kind = EGuitarEvent.AutoNote,
            Chip = chip,
            Time = time,
            ShiftPitch = miss && CDTXMania.ConfigIni.bSpecialist[(int)Side],
            FireLanes = openPlayed ? AllLanes : chipLanes & (autoLanes | pressed)
        });

        if (miss)
        {
            chip.nLag = 0;
        }
        ApplyHit(chip, time, !miss);

        if (!miss)
        {
            ArmWailing(time + InputAdjustMs);
        }
    }

    public void UpdateInput()
    {
        int pressed = GetPressedLanes();
        CChip? shown = HeldNote ?? FindNextNote();
        int shownLanes = shown != null ? GetLanes(shown) : 0;
        LitLanes = pressed | (shownLanes & autoLanes);

        if (HeldNote != null)
        {
            UpdateHold(pressed, shownLanes);
        }

        CDTXMania.Pad.GetEvents(Side, EPad.Pick, padEvents);
        foreach (STInputEvent pick in padEvents)
        {
            if (pick.b押された)
            {
                Pick(pick.nTimeStamp - AudioMixer.Timer.nResetAtMs, pressed);
            }
        }

        CDTXMania.Pad.GetEvents(Side, EPad.Wail, padEvents);
        foreach (STInputEvent wail in padEvents)
        {
            if (wail.b押された)
            {
                Wail(wail.nTimeStamp - AudioMixer.Timer.nResetAtMs);
            }
        }
    }

    private void Pick(long time, int pressed)
    {
        int poorRangeMs = Chart == EChartPart.Guitar ? CDTXMania.stGuitarHitRanges.nPoorSizeMs : CDTXMania.stBassHitRanges.nPoorSizeMs;
        CChip? note = timeline.NearestNote(time + InputAdjustMs, Part, poorRangeMs, HitState.NotHit, preferPast: true);
        EJudgement judgement = JudgeChip(time, note, InputAdjustMs);
        bool specialist = CDTXMania.ConfigIni.bSpecialist[(int)Side];
        bool light = CDTXMania.ConfigIni.bLight[(int)Side];

        if (note != null && judgement != EJudgement.Miss)
        {
            int noteLanes = GetLanes(note);
            if ((noteLanes & ~autoLanes) == (pressed & ~autoLanes))
            {
                bool openPlayed = note.bChipIsOpenNote && (pressed & ~autoLanes) == 0;
                Events.Add(new GuitarEvent
                {
                    Kind = EGuitarEvent.PickedNote,
                    Chip = note,
                    Time = time,
                    ShiftPitch = judgement == EJudgement.Poor && specialist,
                    FireLanes = openPlayed ? AllLanes : noteLanes & (autoLanes | pressed)
                });
                ApplyHit(note, time, true);
                ArmWailing(time + InputAdjustMs);
                return;
            }
        }

        Events.Add(new GuitarEvent
        {
            Kind = EGuitarEvent.EmptyPick,
            Chip = EmptyPickSound ?? timeline.NearestNote(time + InputAdjustMs, Part, 0, HitState.DontCare, preferPast: false),
            Miss = !light,
            ShiftPitch = specialist
        });

        if (!light)
        {
            Data.Damage(EJudgement.Miss);
            Data.BreakCombo();
        }
    }

    private void StartHold(CChip note)
    {
        HeldNote = note;
        holdDurationMs = note.chipLongNoteEndPosition.nPlaybackTimeMs - note.nPlaybackTimeMs;
        holdBonuses = 0;
    }

    public void EndHold()
    {
        if (HeldNote != null)
        {
            HeldNote.bIsHittingLongNote = false;
        }

        HeldNote = null;
        holdDurationMs = 0;
        holdBonuses = 0;
    }

    private void UpdateHold(int pressed, int noteLanes)
    {
        CChip note = HeldNote!;
        if ((noteLanes & ~autoLanes) != (pressed & ~autoLanes))
        {
            if (JudgeChip(AudioMixer.Timer.nCurrentTime, note.chipLongNoteEndPosition, InputAdjustMs) >= EJudgement.Miss)
            {
                EndHold();
                Events.Add(new GuitarEvent { Kind = EGuitarEvent.HoldRelease });
            }
            return;
        }

        if ((noteLanes & autoLanes) != noteLanes)
        {
            Data.Damage(EJudgement.Good);
        }

        int fireLanes = noteLanes & (autoLanes | pressed);
        if (fireLanes != 0)
        {
            Events.Add(new GuitarEvent { Kind = EGuitarEvent.HoldFire, Chip = note, FireLanes = fireLanes });
        }

        if (holdBonuses < 5 && AudioMixer.Timer.nCurrentTime >= note.nPlaybackTimeMs + (holdBonuses + 1) * holdDurationMs / 6)
        {
            Data.AddLongNoteBonus();
            Events.Add(new GuitarEvent { Kind = EGuitarEvent.HoldBonus });
            holdBonuses++;
        }
    }

    private void ArmWailing(long time)
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

    private void Wail(long time)
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
            Events.Add(new GuitarEvent { Kind = EGuitarEvent.Wail, Chip = FindWailingSound(marker) });
            if (!autoWail)
            {
                AddWailingScore();
            }

            return;
        }
    }

    private CChip? FindNextNote()
    {
        int adjust = autoPick ? 0 : InputAdjustMs;
        return timeline.NearestNote(AudioMixer.Timer.nCurrentTime + adjust, Part, 800, HitState.NotHit, preferPast: true);
    }

    private CChip? FindWailingSound(CChip marker) =>
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

    public double CalculateTargetGhostGoal()
    {
        int notes = Data.ChartChips;
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
