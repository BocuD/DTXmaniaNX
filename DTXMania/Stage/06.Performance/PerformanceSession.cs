using DTXMania.Core;

namespace DTXMania;

//stores performance related state, owned by the stage. Everything changes through the methods below
public class PerformanceSession
{
    private STDGBVALUE<HitCounts> hitCountExclAuto;
    private STDGBVALUE<HitCounts> hitCountIncAuto;
    private STDGBVALUE<TimingCounts> timingHitCount;
    private STDGBVALUE<double> achievementRate;
    private STDGBVALUE<int> combo;
    private STDGBVALUE<int> maxCombo;
    private STDGBVALUE<double> score;
    private STDGBVALUE<int> longNoteBonusScore;
    private STDGBVALUE<HitCounts> ghostHitCount;
    private STDGBVALUE<int> ghostCombo;
    private STDGBVALUE<int> ghostMaxCombo;
    private STAUTOPLAY autoPlay;

    public STDGBVALUE<HitCounts> HitCountExclAuto => hitCountExclAuto;
    public STDGBVALUE<HitCounts> HitCountIncAuto => hitCountIncAuto;
    public STDGBVALUE<TimingCounts> TimingHitCount => timingHitCount;
    public STDGBVALUE<double> AchievementRate => achievementRate;
    public STDGBVALUE<int> Combo => combo;
    public STDGBVALUE<int> MaxCombo => maxCombo;
    public STDGBVALUE<double> Score => score;

    //from holding long notes, and allowed on top of the 1,000,000 maximum
    public STDGBVALUE<int> LongNoteBonusScore => longNoteBonusScore;

    //target ghost replayed alongside the play
    public STDGBVALUE<HitCounts> GhostHitCount => ghostHitCount;
    public STDGBVALUE<int> GhostMaxCombo => ghostMaxCombo;

    public void Reset(STAUTOPLAY autoPlayed)
    {
        autoPlay = autoPlayed;
        hitCountExclAuto = default;
        hitCountIncAuto = default;
        timingHitCount = default;
        achievementRate = default;
        combo = default;
        maxCombo = default;
        score = default;
        longNoteBonusScore = default;
        ghostHitCount = default;
        ghostCombo = default;
        ghostMaxCombo = default;
    }

    //keeps the counts with auto included, so the result screen can still show a fully auto play
    public void ResetForLoop(EInstrumentPart part)
    {
        int i = (int)part;
        hitCountExclAuto[i] = default;
        timingHitCount[i] = default;
        combo[i] = 0;
        maxCombo[i] = 0;
        score[i] = 0;
    }

    public void CountJudgement(EInstrumentPart part, EJudgement judgement, bool auto)
    {
        Count(ref hitCountIncAuto, part, judgement);
        if (!auto)
        {
            Count(ref hitCountExclAuto, part, judgement);
        }
    }

    public void CountTiming(EInstrumentPart part, bool late)
    {
        TimingCounts counts = timingHitCount[(int)part];
        if (late)
        {
            counts.Late++;
        }
        else
        {
            counts.Early++;
        }
        timingHitCount[(int)part] = counts;
    }

    public void AddCombo(EInstrumentPart part)
    {
        int i = (int)part;
        combo[i]++;
        maxCombo[i] = Math.Max(maxCombo[i], combo[i]);
    }

    public void BreakCombo(EInstrumentPart part)
    {
        combo[(int)part] = 0;
    }

    //auto-played guitar and bass score half for auto pick and half again for auto neck. A fully auto part only
    //scores with AutoAddGage
    public void AddScore(EInstrumentPart part, long delta)
    {
        double factor = part switch
        {
            EInstrumentPart.GUITAR => AutoFactor(CDTXMania.ConfigIni.bAllGuitarsAreAutoPlay, autoPlay.GtPick,
                autoPlay.GtR || autoPlay.GtG || autoPlay.GtB || autoPlay.GtY || autoPlay.GtP),
            EInstrumentPart.BASS => AutoFactor(CDTXMania.ConfigIni.bAllBassAreAutoPlay, autoPlay.BsPick,
                autoPlay.BsR || autoPlay.BsG || autoPlay.BsB || autoPlay.BsY || autoPlay.BsP),
            _ => 1.0
        };
        score[(int)part] += delta * factor;
    }

    //100 for each sixth of a long note held
    public void AddLongNoteBonus(EInstrumentPart part)
    {
        AddScore(part, 100);
        longNoteBonusScore[(int)part] += 100;
    }

    public void ResetLongNoteBonus()
    {
        longNoteBonusScore = default;
    }

    public void AddClearBonus(EInstrumentPart part, bool excellent)
    {
        score[(int)part] += excellent ? 30000 : 15000;
    }

    public void SetAchievementRate(EInstrumentPart part, double rate)
    {
        achievementRate[(int)part] = rate;
    }

    public void ClearAchievementRates()
    {
        achievementRate = default;
    }

    //the max is taken before the combo moves, so it trails the ghost's combo by one hit
    public void CountGhost(EInstrumentPart part, EJudgement judgement)
    {
        int i = (int)part;
        Count(ref ghostHitCount, part, judgement);
        ghostMaxCombo[i] = Math.Max(ghostMaxCombo[i], ghostCombo[i]);
        if (judgement is EJudgement.Miss or EJudgement.Poor)
        {
            ghostCombo[i] = 0;
        }
        else
        {
            ghostCombo[i]++;
        }
    }

    public void BreakGhostCombo(EInstrumentPart part)
    {
        ghostCombo[(int)part] = 0;
    }

    private static void Count(ref STDGBVALUE<HitCounts> counts, EInstrumentPart part, EJudgement judgement)
    {
        HitCounts c = counts[(int)part];
        c[(int)judgement]++;
        counts[(int)part] = c;
    }

    private static double AutoFactor(bool allAuto, bool autoPick, bool autoNeck)
    {
        if (allAuto)
        {
            return CDTXMania.ConfigIni.bAutoAddGage ? 1.0 : 0.0;
        }

        return (autoPick ? 0.5 : 1.0) * (autoNeck ? 0.5 : 1.0);
    }
}

public struct HitCounts
{
    public int Perfect;
    public int Great;
    public int Good;
    public int Poor;
    public int Miss;

    //by EJudgement, Perfect to Miss
    public int this[int index]
    {
        get => index switch
        {
            0 => Perfect,
            1 => Great,
            2 => Good,
            3 => Poor,
            4 => Miss,
            _ => throw new IndexOutOfRangeException()
        };
        set
        {
            switch (index)
            {
                case 0: Perfect = value; return;
                case 1: Great = value; return;
                case 2: Good = value; return;
                case 3: Poor = value; return;
                case 4: Miss = value; return;
                default: throw new IndexOutOfRangeException();
            }
        }
    }
}

public struct TimingCounts
{
    public int Late;
    public int Early;
}
