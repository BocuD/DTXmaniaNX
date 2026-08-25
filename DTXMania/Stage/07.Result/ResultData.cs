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
    private const double SkillBarFullWidth = 286.0;

    private static int Instrument => CDTXMania.GetCurrentInstrument();
    private static CScoreIni.CPerformanceEntry Entry => CDTXMania.StageManager.stageResult.stPerformanceEntry[Instrument];
    private static double MaxSkill => CDTXMania.chosenChartData?.SongInformation.GetMaxSkill(Instrument) ?? 0.0;

    [DataField] public string SongTitle
    {
        get
        {
            if (!CDTXMania.bCompactMode && CDTXMania.ConfigIni.b曲名表示をdefのものにする)
            {
                return CDTXMania.chosenSong?.title ?? string.Empty;
            }

            return CDTXMania.DTX?.TITLE ?? string.Empty;
        }
    }

    [DataField] public string Artist => CDTXMania.DTX?.ARTIST ?? string.Empty;

    [DataField] public string StageNumber => InfoBox.GetStageNumberText();

    [DataField] public string LevelInt => LevelParts().intPart.ToString();
    [DataField] public string LevelFraction => "." + LevelParts().deci;

    [DataField] public string RateInt => ((int)Entry.dbPerformanceSkill).ToString();
    [DataField] public string RateFraction => "." + RateFractionValue() + "%";

    [DataField] public string SkillInt => ((int)Entry.dbGameSkill).ToString();
    [DataField] public string SkillFraction => "." + SkillFractionValue().ToString("N0");

    [DataField] public bool ShowSkillBar => MaxSkill > 0.0;

    [DataField] public double SkillBarWidth =>
        MaxSkill > 0.0 ? SkillBarFullWidth * (Entry.dbGameSkill / MaxSkill) : 0.0;

    //mutually exclusive; drives which result badge is shown
    [DataField] public bool IsExcellent => Entry.nPerfectCount == Entry.nTotalChipsCount;
    [DataField] public bool IsFullCombo => !IsExcellent && Entry.bIsFullCombo;
    [DataField] public bool IsClear => !IsExcellent && !Entry.bIsFullCombo;

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

    private static int RateFractionValue()
    {
        double rate = Entry.dbPerformanceSkill;
        int fraction = (int)((rate - (int)rate) * 100);
        if (fraction < 10)
        {
            fraction *= 10;
        }

        return fraction;
    }

    private static int SkillFractionValue()
    {
        double skill = Entry.dbGameSkill;
        return (int)((skill - (int)skill) * 100);
    }
}
