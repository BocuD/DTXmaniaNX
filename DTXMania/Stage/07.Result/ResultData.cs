using DTXMania.Core;
using DTXMania.UI.DynamicElements;

namespace DTXMania;

/// <summary>
/// The result screen's view-model, exposed as <c>"Result"</c> on the stage's data context so panels bind
/// to it (e.g. <c>"Result.LevelInt"</c>) instead of reading score statics directly. Values are computed
/// live for the current instrument, which is safe because the result data is stable while the screen is up.
/// </summary>
public sealed class ResultData
{
    private static int Instrument => CDTXMania.GetCurrentInstrument();
    private static CScoreIni.CPerformanceEntry Entry => CDTXMania.StageManager.stageResult.stPerformanceEntry[Instrument];
    [DataField] public double Level => LevelParts().intPart + LevelParts().deci / 100.0;
    [DataField] public double Rate => Entry.dbPerformanceSkill;
    [DataField] public double Skill => Entry.dbGameSkill;
    [DataField] public double MaxSkill => CDTXMania.chosenChartData?.SongInformation.GetMaxSkill(Instrument) ?? 0.0;

    [DataField] public string LevelInt => NumberParts.Whole(Level);
    [DataField] public string LevelFraction => NumberParts.Fraction(Level);

    [DataField] public string RateInt => NumberParts.Whole(Rate);
    [DataField] public string RateFraction => NumberParts.Fraction(Rate) + "%";

    [DataField] public string SkillInt => NumberParts.Whole(Skill);
    [DataField] public string SkillFraction => NumberParts.Fraction(Skill);

    [DataField] public bool ShowSkillBar => MaxSkill > 0.0;

    [DataField] public double SkillOfMax => MaxSkill > 0.0 ? Entry.dbGameSkill / MaxSkill : 0.0;

    //mutually exclusive; drives which result badge is shown
    [DataField] public bool IsExcellent => Entry.nPerfectCount == Entry.nTotalChipsCount;
    [DataField] public bool IsFullCombo => !IsExcellent && Entry.bIsFullCombo;
    [DataField] public bool IsClear => !IsExcellent && !Entry.bIsFullCombo;

    [DataField] public bool ShowLagCounts => CDTXMania.ConfigIni.bShowLagHitCount;
    [DataField] public int FastCount => Entry.nFastCount;
    [DataField] public int SlowCount => Entry.nSlowCount;

    //level is stored as either xx.y (LEVEL<=99 plus LEVELDEC) or xxx (LEVEL>99), split here into a whole
    //part and a 2-digit fraction
    private static (int intPart, int deci) LevelParts()
    {
        int level = CDTXMania.DTX?.LEVEL[Instrument] ?? 0;
        int intPart;
        int deci;

        if (level > 99)
        {
            intPart = level / 100;
            deci = level - intPart * 100;
        }
        else
        {
            intPart = level / 10;
            deci = (level - intPart * 10) * 10 + (CDTXMania.DTX?.LEVELDEC[Instrument] ?? 0);
        }

        if (deci < 10)
        {
            deci *= 10;
        }

        return (intPart, deci);
    }

}
