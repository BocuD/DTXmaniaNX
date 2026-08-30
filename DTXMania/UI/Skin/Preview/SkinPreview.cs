using DTXMania.Core;
using DTXMania.SongDb;

namespace DTXMania.UI.Skin.Preview;

/// <summary>
/// Holds the game on one stage so its layout can be edited, and stands in for anything that stage would
/// normally need a real song or score for.
/// </summary>
public static class SkinPreview
{
    private static PreviewSongDb? songDb;

    public static bool IsActive => CStage.previewMode;

    public static SongDb.SongDb SongDb => (songDb ??= new PreviewSongDb()).Database;

    //never turned off to move the game; use ForceChangeStage for that
    public static bool HoldStage
    {
        get => StageManager.dropStageChanges;
        set => StageManager.dropStageChanges = value;
    }

    public static void RegenerateSongDb()
    {
        songDb = null;
    }

    public static void Enter()
    {
        if (IsActive)
        {
            return;
        }

        CStage.previewMode = true;
        HoldStage = true;

        InspectorManager.inspectorEnabled = true;
        InspectorManager.gameWindow.enabled = true;

        InspectorManager.ShowWindow("Skin Editor");
        InspectorManager.ShowWindow("Hierarchy");
        InspectorManager.ShowWindow("Inspector");
    }

    public static void Exit()
    {
        if (!IsActive)
        {
            return;
        }

        CStage.previewMode = false;
        HoldStage = false;
        CDTXMania.StageManager.handOverTo = null;

        CDTXMania.StageManager.CancelPendingStageChange();
    }

    //no skin change stage: it has nothing to draw
    public static readonly CStage.EStage[] Stages =
    [
        CStage.EStage.Startup_1,
        CStage.EStage.Title_2,
        CStage.EStage.Config_3,
        CStage.EStage.SongSelection_4,
        CStage.EStage.SongLoading_5,
        CStage.EStage.Performance_6,
        CStage.EStage.Result_7,
        CStage.EStage.End_8,
        CStage.EStage.UITest_10
    ];

    //these three read the chart off disk
    public static bool RequiresSong(CStage.EStage stage) => stage
        is CStage.EStage.SongLoading_5
        or CStage.EStage.Performance_6
        or CStage.EStage.Result_7;

    //does nothing if the stage needs a chart and none was passed
    public static void LoadStage(CStage.EStage stage,
        SongNode? song = null, CChartData? chart = null, int difficulty = 0)
    {
        Enter();
        StopPlayback();

        if (RequiresSong(stage))
        {
            //the button is greyed out too, but a missing chart would NRE in OnActivate
            if (song == null || chart == null)
            {
                return;
            }

            CDTXMania.UpdateSelection(song, chart, difficulty);
        }

        //the performance screen needs loading to run first
        if (stage == CStage.EStage.Performance_6)
        {
            StageManager stages = CDTXMania.StageManager;

            stages.ForceChangeStage(stages.stageSongLoading);
            stages.handOverTo = StageFor(CStage.EStage.Performance_6);
            return;
        }

        if (stage == CStage.EStage.Result_7)
        {
            PreviewResult.EnsureScore();
        }

        CStage? target = StageFor(stage);
        if (target == null || target == CDTXMania.StageManager.rCurrentStage)
        {
            return;
        }

        //do not lift the hold; the current stage would get its own change in first
        CDTXMania.StageManager.ForceChangeStage(target);
    }

    //jumping out of the performance screen skips the exit that stops the chart
    private static void StopPlayback()
    {
        CDTXMania.DTX?.tStopPlayingAllChips();
    }

    public static CStage? StageFor(CStage.EStage stage)
    {
        StageManager stages = CDTXMania.StageManager;

        return stage switch
        {
            CStage.EStage.Startup_1 => stages.stageStartup,
            CStage.EStage.Title_2 => stages.stageTitle,
            CStage.EStage.Config_3 => stages.stageConfig,
            CStage.EStage.SongSelection_4 => stages.stageSongSelectionNew,
            CStage.EStage.SongLoading_5 => stages.stageSongLoading,
            //must match what StageManager picks when loading hands over
            CStage.EStage.Performance_6 => CDTXMania.ConfigIni.bGuitarRevolutionMode
                ? stages.stagePerfGuitarScreen
                : stages.stagePerfDrumsScreen,
            CStage.EStage.Result_7 => stages.stageResult,
            CStage.EStage.End_8 => stages.stageEnd,
            CStage.EStage.UITest_10 => stages.stageUITest,
            _ => null
        };
    }
}
