using DTXMania.Core;

namespace DTXMania;

internal enum EDrumsEvent
{
    AutoNote,
    Bonus,
    Judged
}

internal struct DrumsEvent
{
    public EDrumsEvent Kind;

    public CChip Chip;

    public long Time;

    public EJudgement Judgement;

    public bool Auto;

    public bool CountsCombo;
}

internal class DrumsPerformance
{
    //by channel from HiHatClose to LeftBassDrum; the lane is an ELane, the pad one of the pad art's cells
    public static readonly int[] ChannelLanes = [1, 2, 3, 4, 5, 7, 6, 1, 9, 0, 8, 8];
    public static readonly int[] ChannelPads = [1, 2, 3, 4, 5, 7, 6, 1, 8, 0, 9, 9];

    public readonly PerformanceData Data;
    public readonly List<DrumsEvent> Events = [];

    private readonly STAUTOPLAY auto;

    public DrumsPerformance(PerformanceData data, STAUTOPLAY auto)
    {
        Data = data;
        this.auto = auto;
    }

    private static int InputAdjustMs => CDTXMania.ConfigIni.nInputAdjustTimeMs.Drums;

    public static int LaneOf(CChip chip) => ChannelLanes[chip.nChannelNumber - EChannel.HiHatClose];

    public static int PadOf(CChip chip) => ChannelPads[chip.nChannelNumber - EChannel.HiHatClose];

    private bool IsAuto(CChip chip) => auto[LaneOf(chip)];

    //correctLane false is a miss without judging the timing
    public EJudgement ApplyHit(CChip chip, long time, bool correctLane)
    {
        chip.bHit = true;
        bool isAuto = IsAuto(chip);
        chip.bIsAutoPlayed = isAuto;
        EJudgement judgement = correctLane ? Data.JudgeChip(time, chip, isAuto ? 0 : InputAdjustMs) : EJudgement.Miss;

        //before ApplyJudgement, so the last perfect's top-up to 1,000,000 includes the bonus score
        if (judgement is EJudgement.Great or EJudgement.Perfect && chip.bBonusChip)
        {
            Events.Add(new DrumsEvent { Kind = EDrumsEvent.Bonus, Chip = chip });
            if (CDTXMania.ConfigIni.nSkillMode == 1 && (!CDTXMania.ConfigIni.bAllDrumsAreAutoPlay || CDTXMania.ConfigIni.bAutoAddGage))
            {
                Data.AddScore(500L);
            }
        }

        bool countsCombo = CDTXMania.ConfigIni.bAllDrumsAreAutoPlay || !isAuto;
        Data.ApplyJudgement(judgement, isAuto, chip.nLag, countsCombo);
        Events.Add(new DrumsEvent { Kind = EDrumsEvent.Judged, Chip = chip, Judgement = judgement, Auto = isAuto, CountsCombo = countsCombo });
        return judgement;
    }

    public void UpdateNote(CChip chip)
    {
        long now = AudioMixer.Timer.nCurrentTime;
        bool isAuto = IsAuto(chip);
        if (!chip.bHit && chip.nDistanceFromBar.Drums < 0 && Data.JudgeChip(now, chip, isAuto ? 0 : InputAdjustMs) == EJudgement.Miss)
        {
            ApplyHit(chip, now, true);
        }

        if (chip.bHit || !isAuto)
        {
            return;
        }

        long ghostLag = 0;
        List<int>? ghost = CDTXMania.listAutoGhostLag.Drums;
        if (CDTXMania.ConfigIni.eAutoGhost.Drums != EAutoGhostData.PERFECT && ghost != null &&
            0 <= chip.n楽器パートでの出現順 && chip.n楽器パートでの出現順 < ghost.Count)
        {
            ghostLag = (ghost[chip.n楽器パートでの出現順] & 255) - 128 - InputAdjustMs;
            if (chip.nPlaybackTimeMs + ghostLag > now)
            {
                return;
            }
        }
        else if (chip.nDistanceFromBar.Drums >= 0)
        {
            return;
        }

        chip.bHit = true;
        long time = chip.nPlaybackTimeMs + ghostLag;
        Events.Add(new DrumsEvent { Kind = EDrumsEvent.AutoNote, Chip = chip, Time = time });
        ApplyHit(chip, time, true);
    }
}
