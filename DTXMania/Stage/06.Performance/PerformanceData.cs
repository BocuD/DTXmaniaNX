using DTXMania.Core;

namespace DTXMania;

public class PerformanceData
{
    public readonly EInstrumentPart Part;

    private HitCounts hitCountExclAuto;
    private HitCounts hitCountIncAuto;
    private TimingCounts timingHitCount;
    private double achievementRate;
    private int combo;
    private int maxCombo;
    private double score;
    private int longNoteBonusScore;
    private HitCounts ghostHitCount;
    private int ghostCombo;
    private int ghostMaxCombo;
    private STAUTOPLAY autoPlay;
    private double gauge;
    private int riskyTimesInitial;
    private int riskyTimes;

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

    public HitCounts HitCountExclAuto => hitCountExclAuto;
    public HitCounts HitCountIncAuto => hitCountIncAuto;
    public TimingCounts TimingHitCount => timingHitCount;
    public double AchievementRate => achievementRate;
    public int Combo => combo;
    public int MaxCombo => maxCombo;
    public double Score => score;
    public double Gauge => gauge;

    //score for holding long notes, added on top of the 1,000,000 maximum
    public int LongNoteBonusScore => longNoteBonusScore;

    //target ghost replayed alongside the play
    public HitCounts GhostHitCount => ghostHitCount;
    public int GhostMaxCombo => ghostMaxCombo;

    public int ChartChips => Part switch
    {
        EInstrumentPart.DRUMS => CDTXMania.DTX.nVisibleChipsCount.Drums,
        EInstrumentPart.GUITAR => CDTXMania.DTX.nVisibleChipsCount.Guitar,
        _ => CDTXMania.DTX.nVisibleChipsCount.Bass
    };

    public PerformanceData(EInstrumentPart part)
    {
        Part = part;
    }

    //risky is how many misses the play may have until stage failed, 0 for a normal gauge
    public void Reset(STAUTOPLAY autoPlayed, int risky)
    {
        autoPlay = autoPlayed;
        riskyTimesInitial = risky;
        riskyTimes = risky;
        gauge = risky <= 0 ? 2.0 / 3 : risky == 1 ? 0.0 : GaugeMax;
        hitCountExclAuto = default;
        hitCountIncAuto = default;
        timingHitCount = default;
        achievementRate = 0;
        combo = 0;
        maxCombo = 0;
        score = 0;
        longNoteBonusScore = 0;
        ghostHitCount = default;
        ghostCombo = 0;
        ghostMaxCombo = 0;
    }

    //keeps the counts with auto included, so the result screen can still show a fully auto play
    public void ResetForLoop()
    {
        hitCountExclAuto = default;
        timingHitCount = default;
        combo = 0;
        maxCombo = 0;
        score = 0;
    }

    public void CountJudgement(EJudgement judgement, bool auto)
    {
        hitCountIncAuto[(int)judgement]++;
        if (!auto)
        {
            hitCountExclAuto[(int)judgement]++;
        }
    }

    public void CountTiming(bool late)
    {
        if (late)
        {
            timingHitCount.Late++;
        }
        else
        {
            timingHitCount.Early++;
        }
    }

    public void AddCombo()
    {
        combo++;
        maxCombo = Math.Max(maxCombo, combo);
    }

    public void BreakCombo()
    {
        combo = 0;
    }

    //auto-played guitar and bass score half for auto pick and half again for auto neck. A fully auto part only
    //scores with AutoAddGage
    public void AddScore(long delta)
    {
        double factor = Part switch
        {
            EInstrumentPart.GUITAR => AutoFactor(CDTXMania.ConfigIni.bAllGuitarsAreAutoPlay, autoPlay.GtPick,
                autoPlay.GtR || autoPlay.GtG || autoPlay.GtB || autoPlay.GtY || autoPlay.GtP),
            EInstrumentPart.BASS => AutoFactor(CDTXMania.ConfigIni.bAllBassAreAutoPlay, autoPlay.BsPick,
                autoPlay.BsR || autoPlay.BsG || autoPlay.BsB || autoPlay.BsY || autoPlay.BsP),
            _ => 1.0
        };
        score += delta * factor;
    }

    //100 for each sixth of a long note held
    public void AddLongNoteBonus()
    {
        AddScore(100);
        longNoteBonusScore += 100;
    }

    public void ResetLongNoteBonus()
    {
        longNoteBonusScore = 0;
    }

    public void AddClearBonus(bool excellent)
    {
        score += excellent ? 30000 : 15000;
    }

    public void SetAchievementRate(double rate)
    {
        achievementRate = rate;
    }

    //sets the chip's lag; the combo is kept for recording a ghost
    public EJudgement JudgeChip(long time, CChip? chip, int inputAdjustMs)
    {
        if (chip == null)
        {
            return EJudgement.Miss;
        }

        chip.nLag = (int)(time + inputAdjustMs - chip.nPlaybackTimeMs);
        if (chip.eInstrumentPart != EInstrumentPart.UNKNOWN)
        {
            chip.nCurrentComboForGhost = combo;
        }
        return ChipJudge.GetJudgementForLag(chip, chip.nLag);
    }

    public bool HasTargetGhost => CDTXMania.ConfigIni.eTargetGhost[(int)Part] != ETargetGhostData.NONE && CDTXMania.listTargetGhsotLag[(int)Part] != null;

    public double CalculateTargetGhostGoal()
    {
        int chips = ChartChips;
        HitCounts ghost = ghostHitCount;
        double goal;
        if (CDTXMania.ConfigIni.eTargetGhost[(int)Part] == ETargetGhostData.ONLINE)
        {
            goal = chips > 0 ? 100 * (ghost.Perfect * 17 + ghost.Great * 7 + ghostMaxCombo * 3) / (20.0 * chips) : 0;
        }
        else if (CDTXMania.ConfigIni.nSkillMode == 0)
        {
            goal = CScoreIni.tCalculatePlayingSkillOld(chips, ghost.Perfect, ghost.Great, ghost.Good, ghost.Poor, ghost.Miss,
                ghostMaxCombo, Part, new STAUTOPLAY());
        }
        else
        {
            goal = CScoreIni.tCalculatePlayingSkill(chips, ghost.Perfect, ghost.Great, ghost.Good, ghost.Poor, ghost.Miss,
                ghostMaxCombo, Part, new STAUTOPLAY());
        }

        return Math.Clamp(goal, 0, 100);
    }

    public void ApplyJudgement(EJudgement judgement, bool auto, int lag, bool countsCombo)
    {
        if (CDTXMania.ConfigIni.bAutoAddGage || !auto)
        {
            Damage(judgement);
        }
        if (!auto)
        {
            CountTiming(lag > 0);
        }
        CountJudgement(judgement, auto);

        if (countsCombo)
        {
            if (judgement is EJudgement.Perfect or EJudgement.Great or EJudgement.Good)
            {
                AddCombo();
            }
            else
            {
                BreakCombo();
            }
        }

        //guitar and bass score auto chips in skill mode 1 even without AutoAddGage
        bool scoresAuto = CDTXMania.ConfigIni.bAutoAddGage || (CDTXMania.ConfigIni.nSkillMode == 1 && Part != EInstrumentPart.DRUMS);
        if (judgement != EJudgement.Miss && (scoresAuto || !auto))
        {
            AddScore(CDTXMania.ConfigIni.nSkillMode == 1 ? SkillModeScore(judgement) : ClassicScore(judgement));
        }
    }

    //a full perfect play adds up to 1,000,000. The combo multiplier ramps up to 50, and the last chip tops the score up
    private long SkillModeScore(EJudgement judgement)
    {
        float maxChips = ChartChips;

        //drum bonus chips score 500 each, out of the same 1,000,000
        float bonusChips = Part == EInstrumentPart.DRUMS ? 500.0f * CDTXMania.DTX.nボーナスチップ数 : 0f;
        float perfect = (1000000.0f - bonusChips) / (1275.0f + 50.0f * (maxChips - 50.0f));

        float delta = judgement switch
        {
            EJudgement.Perfect when combo < maxChips => perfect,
            EJudgement.Perfect when hitCountIncAuto.Perfect >= maxChips => 1000000.0f - (float)score + longNoteBonusScore,
            EJudgement.Great => perfect * 0.5f,
            EJudgement.Good => perfect * 0.2f,
            _ => 0f
        };

        if (combo < 50)
        {
            delta *= combo;
        }
        else if (combo != maxChips && hitCountExclAuto.Perfect != maxChips)
        {
            delta *= 50.0f;
        }

        return (long)delta;
    }

    //perfect, great, good, poor
    private static readonly long[] ClassicScorePerCombo = [350L, 200L, 50L, 0L];

    private long ClassicScore(EJudgement judgement)
    {
        if (combo <= 500 || judgement == EJudgement.Good)
        {
            return ClassicScorePerCombo[(int)judgement] * combo;
        }

        return judgement == EJudgement.Perfect ? ClassicScorePerCombo[(int)judgement] * 500L : 0L;
    }

    public double CalculateSkill()
    {
        int chips = ChartChips;
        HitCounts hits = hitCountExclAuto;
        return CDTXMania.ConfigIni.nSkillMode == 0
            ? CScoreIni.tCalculatePlayingSkillOld(chips, hits.Perfect, hits.Great, hits.Good, hits.Poor, hits.Miss, maxCombo, Part, autoPlay)
            : CScoreIni.tCalculatePlayingSkill(chips, hits.Perfect, hits.Great, hits.Good, hits.Poor, hits.Miss, maxCombo, Part, autoPlay);
    }

    //the max is taken before the combo moves, so it trails the ghost's combo by one hit
    public void CountGhost(EJudgement judgement)
    {
        ghostHitCount[(int)judgement]++;
        ghostMaxCombo = Math.Max(ghostMaxCombo, ghostCombo);
        if (judgement is EJudgement.Miss or EJudgement.Poor)
        {
            ghostCombo = 0;
        }
        else
        {
            ghostCombo++;
        }
    }

    public void BreakGhostCombo()
    {
        ghostCombo = 0;
    }

    public void Damage(EJudgement judgement)
    {
        bool risky = riskyTimesInitial > 0;
        bool hazard = CDTXMania.ConfigIni.bHAZARD;
        double damage = judgement switch
        {
            EJudgement.Perfect => risky ? 0 : GaugeDeltaFor(judgement),
            EJudgement.Great or EJudgement.Good when hazard => risky ? RiskyMiss() : GaugeDeltaFor(EJudgement.Miss),
            EJudgement.Great or EJudgement.Good => risky ? 0 : GaugeDeltaFor(judgement),
            EJudgement.Poor => risky ? RiskyMiss() : GaugeDeltaFor(judgement),
            EJudgement.Miss => risky ? RiskyMiss() : GaugeDeltaFor(judgement) * MissDamageFactor[(int)CDTXMania.ConfigIni.eDamageLevel],
            _ => 0
        };

        gauge = Math.Min(gauge + damage, GaugeMax);
    }

    public bool IsFailed()
    {
        return riskyTimesInitial > 0 ? riskyTimes <= 0 : gauge <= GaugeFailed;
    }

    public bool IsDanger()
    {
        if (riskyTimesInitial <= 0)
        {
            return gauge <= 0.3;
        }

        return riskyTimesInitial switch
        {
            1 => false,
            2 or 3 => riskyTimes <= 1,
            _ => riskyTimes <= 2
        };
    }

    private double GaugeDeltaFor(EJudgement judgement)
    {
        if (Part == EInstrumentPart.DRUMS && CDTXMania.ConfigIni.nSkillMode == 1)
        {
            return XgDrumsGaugeDelta[(int)judgement];
        }

        return GaugeDelta[(int)judgement, (int)Part];
    }

    private double RiskyMiss()
    {
        double damage = riskyTimes == 1 ? 0 : -GaugeMax / (riskyTimesInitial - 1);
        if (riskyTimes >= 0)
        {
            riskyTimes--;
        }

        return damage;
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
