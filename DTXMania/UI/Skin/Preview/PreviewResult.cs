using DTXMania.Core;

namespace DTXMania.UI.Skin.Preview;

/// <summary>
/// Fake scores for the skin editor. Written into the stage's own performance entry, so the result layout
/// reads them the same way it reads a real one.
/// </summary>
public static class PreviewResult
{
    public static void Apply(int instrument) => Apply(instrument, Preset.Clear);

    public enum Preset
    {
        Excellent,
        FullCombo,
        Clear,
        Failed
    }

    private const int TotalChips = 850;

    private readonly record struct Score(
        int Perfect, int Great, int Good, int Poor, int Miss, int MaxCombo, long Points);

    //the counts must add up to TotalChips, and MaxCombo must equal that total to count as a full combo.
    //Rate, skill and rank are not listed: they follow from the counts, and the game works them out
    private static readonly Dictionary<Preset, Score> Presets = new()
    {
        [Preset.Excellent] = new(850, 0, 0, 0, 0, MaxCombo: 850, Points: 1_000_000),
        [Preset.FullCombo] = new(792, 58, 0, 0, 0, MaxCombo: 850, Points: 941_200),
        [Preset.Clear] = new(604, 171, 48, 15, 12, MaxCombo: 318, Points: 703_450),
        [Preset.Failed] = new(210, 168, 190, 121, 161, MaxCombo: 47, Points: 214_900)
    };

    public static void Apply(int instrument, Preset preset)
    {
        EnsureChart();

        CStageResult stage = CDTXMania.StageManager.stageResult;
        CScoreIni.CPerformanceEntry entry = stage.stPerformanceEntry[instrument];
        Score score = Presets[preset];

        entry.nTotalChipsCount = TotalChips;

        entry.nPerfectCount = entry.nPerfectCount_ExclAuto = score.Perfect;
        entry.nGreatCount = entry.nGreatCount_ExclAuto = score.Great;
        entry.nGoodCount = entry.nGoodCount_ExclAuto = score.Good;
        entry.nPoorCount = entry.nPoorCount_ExclAuto = score.Poor;
        entry.nMissCount = entry.nMissCount_ExclAuto = score.Miss;

        entry.nMaxCombo = score.MaxCombo;
        entry.nScore = score.Points;

        Recalculate(instrument);
    }

    public static void Recalculate(int instrument)
    {
        CStageResult stage = CDTXMania.StageManager.stageResult;
        CScoreIni.CPerformanceEntry entry = stage.stPerformanceEntry[instrument];

        entry.dbPerformanceSkill = CScoreIni.tCalculatePlayingSkill(
            entry.nTotalChipsCount, entry.nPerfectCount, entry.nGreatCount, entry.nGoodCount,
            entry.nPoorCount, entry.nMissCount, entry.nMaxCombo,
            (EInstrumentPart)instrument, CDTXMania.ConfigIni.bAutoPlay);

        CChartData? chart = CDTXMania.chosenChartData;
        double level = chart?.SongInformation.GetLevel(instrument) ?? 0.0;
        int levelDec = chart?.SongInformation.LevelDec[instrument] ?? 0;

        entry.dbGameSkill = CScoreIni.tCalculateGameSkillFromPlayingSkill(
            level, levelDec, entry.dbPerformanceSkill);

        stage.nRankValue[instrument] = CScoreIni.tCalculateRank(
            entry.nTotalChipsCount, entry.nPerfectCount, entry.nGreatCount, entry.nGoodCount,
            entry.nPoorCount, entry.nMissCount, entry.nMaxCombo);
    }

    //keeps an existing score, so coming back to the stage shows the same numbers
    public static void EnsureScore()
    {
        EnsureChart();

        int instrument = CDTXMania.GetCurrentInstrument();

        if (CDTXMania.StageManager.stageResult.stPerformanceEntry[instrument].nTotalChipsCount == 0)
        {
            Apply(instrument);
        }
    }

    //the result layout reads the chart, so one has to exist. Header only: it needs the folder and the
    //jacket, not the chips
    public static void EnsureChart()
    {
        string path = CDTXMania.chosenChartData?.FileInformation.AbsoluteFilePath ?? string.Empty;

        if (path.Length > 0 && File.Exists(path)
            && CDTXMania.DTX?.strFileNameFullPath != path)
        {
            CDTXMania.DTX = new CDTX(path, true);
            return;
        }

        CDTXMania.DTX ??= new CDTX();
    }
}
