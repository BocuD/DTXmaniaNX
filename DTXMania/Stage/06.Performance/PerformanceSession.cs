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
    private STDGBVALUE<double> gauge;
    private int riskyTimesInitial;
    private STDGBVALUE<int> riskyTimes;

    private const double GaugeMax = 1.0;
    private const double GaugeFailed = -0.1;

    //by EJudgement, then drums, guitar, bass
    private static readonly float[,] GaugeDelta =
    {
        { 0.004f, 0.006f, 0.006f },
        { 0.002f, 0.003f, 0.003f },
        { 0.000f, 0.000f, 0.000f },
        { -0.020f, -0.030f, -0.030f },
        { -0.050f, -0.050f, -0.050f }
    };
    //XG skill mode has its own drums column
    private static readonly float[] XgDrumsGaugeDelta = [0.005f, 0.001f, 0f, -0.017f, -0.041f];
    //by EDamageLevel, applied to misses
    private static readonly float[] MissDamageFactor = [0.25f, 0.5f, 0.75f];

    public STDGBVALUE<HitCounts> HitCountExclAuto => hitCountExclAuto;
    public STDGBVALUE<HitCounts> HitCountIncAuto => hitCountIncAuto;
    public STDGBVALUE<TimingCounts> TimingHitCount => timingHitCount;
    public STDGBVALUE<double> AchievementRate => achievementRate;
    public STDGBVALUE<int> Combo => combo;
    public STDGBVALUE<int> MaxCombo => maxCombo;
    public STDGBVALUE<double> Score => score;
    public STDGBVALUE<double> Gauge => gauge;

    //from holding long notes, and allowed on top of the 1,000,000 maximum
    public STDGBVALUE<int> LongNoteBonusScore => longNoteBonusScore;

    //target ghost replayed alongside the play
    public STDGBVALUE<HitCounts> GhostHitCount => ghostHitCount;
    public STDGBVALUE<int> GhostMaxCombo => ghostMaxCombo;

    //risky is how many misses the play may have, 0 for a normal gauge
    public void Reset(STAUTOPLAY autoPlayed, int risky)
    {
        autoPlay = autoPlayed;
        riskyTimesInitial = risky;
        riskyTimes = new STDGBVALUE<int> { Drums = risky, Guitar = risky, Bass = risky };
        double startGauge = risky <= 0 ? 2.0 / 3 : risky == 1 ? 0.0 : GaugeMax;
        gauge = new STDGBVALUE<double> { Drums = startGauge, Guitar = startGauge, Bass = startGauge };
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

    public void Damage(EInstrumentPart part, EJudgement judgement)
    {
        bool risky = riskyTimesInitial > 0;
        bool hazard = CDTXMania.ConfigIni.bHAZARD;
        double damage = judgement switch
        {
            EJudgement.Perfect => risky ? 0 : GaugeDeltaFor(judgement, part),
            EJudgement.Great or EJudgement.Good when hazard => risky ? RiskyMiss(part) : GaugeDeltaFor(EJudgement.Miss, part),
            EJudgement.Great or EJudgement.Good => risky ? 0 : GaugeDeltaFor(judgement, part),
            EJudgement.Poor => risky ? RiskyMiss(part) : GaugeDeltaFor(judgement, part),
            EJudgement.Miss => risky ? RiskyMiss(part) : GaugeDeltaFor(judgement, part) * MissDamageFactor[(int)CDTXMania.ConfigIni.eDamageLevel],
            _ => 0
        };

        gauge[(int)part] = Math.Min(gauge[(int)part] + damage, GaugeMax);
    }

    public bool IsFailed(EInstrumentPart part)
    {
        return riskyTimesInitial > 0 ? riskyTimes[(int)part] <= 0 : gauge[(int)part] <= GaugeFailed;
    }

    public bool IsDanger(EInstrumentPart part)
    {
        if (riskyTimesInitial <= 0)
        {
            return gauge[(int)part] <= 0.3;
        }

        return riskyTimesInitial switch
        {
            1 => false,
            2 or 3 => riskyTimes[(int)part] <= 1,
            _ => riskyTimes[(int)part] <= 2
        };
    }

    private static double GaugeDeltaFor(EJudgement judgement, EInstrumentPart part)
    {
        if (part == EInstrumentPart.DRUMS && CDTXMania.ConfigIni.nSkillMode == 1)
        {
            return XgDrumsGaugeDelta[(int)judgement];
        }

        return GaugeDelta[(int)judgement, (int)part];
    }

    private double RiskyMiss(EInstrumentPart part)
    {
        int i = (int)part;
        double damage = riskyTimes[i] == 1 ? 0 : -GaugeMax / (riskyTimesInitial - 1);
        if (riskyTimes[i] >= 0)
        {
            riskyTimes[i]--;
        }

        return damage;
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
