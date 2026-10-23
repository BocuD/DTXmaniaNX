using DTXMania.Core;

namespace DTXMania;

//stores performance related state, owned / updated by stage
public class PerformanceSession
{
    public STDGBVALUE<HitCounts> HitCountExclAuto;
    public STDGBVALUE<HitCounts> HitCountIncAuto;
    public STDGBVALUE<TimingCounts> TimingHitCount;
    public STDGBVALUE<double> AchievementRate;
    public STDGBVALUE<int> Combo;
    public STDGBVALUE<int> MaxCombo;

    //from holding long notes, and allowed on top of the 1,000,000 maximum
    public STDGBVALUE<int> LongNoteBonusScore;

    //target ghost replayed alongside the play
    public STDGBVALUE<HitCounts> GhostHitCount;
    public STDGBVALUE<int> GhostCombo;
    public STDGBVALUE<int> GhostMaxCombo;

    public PerformanceSession()
    {
        Reset();
    }

    public void Reset()
    {
        for (int i = 0; i < 3; i++)
        {
            HitCountExclAuto[i] = new HitCounts();
            HitCountIncAuto[i] = new HitCounts();
            TimingHitCount[i] = new TimingCounts();
            GhostHitCount[i] = new HitCounts();
        }

        AchievementRate = default;
        Combo = default;
        MaxCombo = default;
        LongNoteBonusScore = default;
        GhostCombo = default;
        GhostMaxCombo = default;
    }
}

public class HitCounts
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

public class TimingCounts
{
    public int Late;
    public int Early;
}
