using System.Diagnostics;
using DTXMania.UI.Drawable;
using FDK;

namespace DTXMania.Core;

internal class StageManager
{
    public CStageStartup stageStartup { get; }
    public CStageTitle stageTitle { get; }
    public CStageConfig stageConfig { get; }
    public CStageSongSelection stageSongSelection { get; }
    public CStageSongSelectionNew stageSongSelectionNew { get; }
    public CStageSongLoading stageSongLoading { get; }
    public CStagePerfGuitarScreen stagePerfGuitarScreen { get; }
    public CStagePerfDrumsScreen stagePerfDrumsScreen { get; }
    public CStageResult stageResult { get; }
    public CStageChangeSkin stageChangeSkin { get; }
    public CStageEnd stageEnd { get; }
    
    public CStage rCurrentStage;
    public CStage rPreviousStage;

    public StageManager()
    {
        stageStartup = new CStageStartup();
        stageTitle = new CStageTitle();
        stageConfig = new CStageConfig();
        stageSongSelection = new CStageSongSelection();
        stageSongSelectionNew = new CStageSongSelectionNew();
        stageSongLoading = new CStageSongLoading();
        stagePerfDrumsScreen = new CStagePerfDrumsScreen();
        stagePerfGuitarScreen = new CStagePerfGuitarScreen();
        stageResult = new CStageResult();
        stageChangeSkin = new CStageChangeSkin();
        stageEnd = new CStageEnd();
    }
    
    public void LoadInitialStage()
    {
        rCurrentStage = CDTXMania.bCompactMode ? stageSongLoading : stageStartup;
        rCurrentStage.OnActivate();
    }

    public void DrawStage()
    {
        if (rCurrentStage == null) return;

        int nUpdateAndDrawReturnValue = rCurrentStage.OnUpdateAndDraw();

        //handle stage changes
        switch (rCurrentStage.eStageID)
        {
            case CStage.EStage.DoNothing_0:
                break;

            case CStage.EStage.Startup_1:
                if (nUpdateAndDrawReturnValue != 0)
                {
                    tChangeStage(CDTXMania.bCompactMode ? stageSongLoading : stageTitle);
                }

                break;

            case CStage.EStage.Title_2:

                #region [ *** ]

                //-----------------------------
                if (nUpdateAndDrawReturnValue != 0)
                {
                    switch (nUpdateAndDrawReturnValue)
                    {
                        case (int)CStageTitle.EReturnResult.GAMESTART:
                            tChangeStage(stageSongSelectionNew);
                            break;

                        case (int)CStageTitle.EReturnResult.CONFIG:
                            tChangeStage(stageConfig);
                            break;

                        case (int)CStageTitle.EReturnResult.EXIT:
                            tChangeStage(stageEnd);
                            break;
                    }
                }

                //-----------------------------

                #endregion

                break;

            case CStage.EStage.Config_3:

                #region [ *** ]

                //-----------------------------
                if (nUpdateAndDrawReturnValue != 0)
                {
                    switch (rPreviousStage.eStageID)
                    {
                        case CStage.EStage.Title_2:
                            tChangeStage(stageTitle);
                            break;

                        case CStage.EStage.SongSelection_4:
                            tChangeStage(stageSongSelectionNew);
                            break;
                    }
                }

                //-----------------------------

                #endregion

                break;

            case CStage.EStage.SongSelection_4:

                #region [ *** ]

                //-----------------------------
                switch (nUpdateAndDrawReturnValue)
                {
                    case (int)CStageSongSelection.EReturnValue.ReturnToTitle:
                        tChangeStage(stageTitle);
                        break;

                    case (int)CStageSongSelection.EReturnValue.Selected:
                        tChangeStage(stageSongLoading);
                        break;

                    case (int)CStageSongSelection.EReturnValue.CallConfig:
                        tChangeStage(stageConfig);
                        break;

                    case (int)CStageSongSelection.EReturnValue.ChangeSking:
                        tChangeStage(stageChangeSkin);
                        break;
                }

                //-----------------------------

                #endregion

                break;

            case CStage.EStage.SongLoading_5:

                #region [ *** ]

                //-----------------------------
                if (nUpdateAndDrawReturnValue != 0)
                {
                    CDTXMania.Pad.stDetectedDevice.Clear(); // 入力デバイスフラグクリア(2010.9.11)

                    #region [ ESC押下時は、曲の読み込みを中止して選曲画面に戻る ]

                    if (nUpdateAndDrawReturnValue == (int)ESongLoadingScreenReturnValue.LoadingStopped)
                    {
                        //DTX.tStopPlayingAllChips();
                        CDTXMania.DTX.OnDeactivate();
                        Trace.TraceInformation("曲の読み込みを中止しました。");
                        CDTXMania.tRunGarbageCollector();

                        GitaDoraTransition.Close(10, () => tChangeStage(stageSongSelectionNew));
                        break;
                    }

                    #endregion


                    if (!CDTXMania.ConfigIni.bGuitarRevolutionMode)
                    {
                        tChangeStage(stagePerfDrumsScreen);
                    }
                    else
                    {
                        tChangeStage(stagePerfGuitarScreen);
                    }
                }

                //-----------------------------

                #endregion

                break;

            case CStage.EStage.Performance_6:

                #region [ *** ]

                //-----------------------------

                #region [ DTXVモード中にDTXCreatorから指示を受けた場合の処理 ]

                if (CDTXMania.DTXVmode.Enabled && CDTXMania.DTXVmode.Refreshed)
                {
                    CDTXMania.DTXVmode.Refreshed = false;

                    if (CDTXMania.DTXVmode.Command == CDTXVmode.ECommand.Stop)
                    {
                        ((CStagePerfCommonScreen)rCurrentStage).t停止();

                        //if (previewSound != null)
                        //{
                        //    this.previewSound.tサウンドを停止する();
                        //    this.previewSound.Dispose();
                        //    this.previewSound = null;
                        //}
                    }
                    else if (CDTXMania.DTXVmode.Command == CDTXVmode.ECommand.Play)
                    {
                        if (CDTXMania.DTXVmode.NeedReload)
                        {
                            ((CStagePerfCommonScreen)rCurrentStage).t再読込();
                            if (CDTXMania.DTXVmode.GRmode)
                            {
                                CDTXMania.ConfigIni.bDrumsEnabled = false;
                                CDTXMania.ConfigIni.bGuitarEnabled = true;
                            }
                            else
                            {
                                //Both in Original DTXMania, but we don't support that
                                CDTXMania.ConfigIni.bDrumsEnabled = true;
                                CDTXMania.ConfigIni.bGuitarEnabled = false;
                            }

                            CDTXMania.ConfigIni.bTimeStretch = CDTXMania.DTXVmode.TimeStretch;
                            CSoundManager.bIsTimeStretch = CDTXMania.DTXVmode.TimeStretch;
                            if (CDTXMania.ConfigIni.bVerticalSyncWait != CDTXMania.DTXVmode.VSyncWait)
                            {
                                CDTXMania.ConfigIni.bVerticalSyncWait = CDTXMania.DTXVmode.VSyncWait;
                                //CDTXMania.b次のタイミングで垂直帰線同期切り替えを行う = true;
                            }
                        }
                        else
                        {
                            ((CStagePerfCommonScreen)rCurrentStage).tJumpInSongToBar(CDTXMania.DTXVmode.nStartBar);
                        }
                    }
                }

                #endregion

                CScoreIni scoreIni = null;
                switch (nUpdateAndDrawReturnValue)
                {
                    case (int)EPerfScreenReturnValue.Continue:
                        break;

                    case (int)EPerfScreenReturnValue.Interruption:
                    case (int)EPerfScreenReturnValue.Restart:

                        #region [ Cancel performance ]

                        //-----------------------------
                        if (!CDTXMania.DTXVmode.Enabled && !CDTXMania.DTX2WAVmode.Enabled)
                        {
                            scoreIni = CDTXMania.tScoreIniへBGMAdjustとHistoryとPlayCountを更新("Play cancelled");
                        }

                        CDTXMania.DTX.tStopPlayingAllChips();
                        CDTXMania.DTX.OnDeactivate();
                        rCurrentStage.OnDeactivate();
                        if (CDTXMania.bCompactMode && !CDTXMania.DTXVmode.Enabled && !CDTXMania.DTX2WAVmode.Enabled)
                        {
                            CDTXMania.app.Window.Close();
                        }
                        else if (nUpdateAndDrawReturnValue == (int)EPerfScreenReturnValue.Restart)
                        {
                            tChangeStage(stageSongLoading, true, false);
                        }
                        else
                        {
                            tChangeStage(stageSongSelectionNew, true, false);
                        }

                        break;
                    //-----------------------------

                    #endregion

                    case (int)EPerfScreenReturnValue.StageFailure:

                        #region [ 演奏失敗(StageFailed) ]

                        //-----------------------------
                        {
                            //New extract performance record
                            CScoreIni.CPerformanceEntry cPerf_Drums, cPerf_Guitar, cPerf_Bass;
                            bool bTrainingMode = false;
                            CChip[] chipsArray = new CChip[10];
                            if (CDTXMania.ConfigIni.bGuitarRevolutionMode)
                            {
                                stagePerfGuitarScreen.tStorePerfResults(out cPerf_Drums, out cPerf_Guitar,
                                    out cPerf_Bass, out bTrainingMode);
                            }
                            else
                            {
                                stagePerfDrumsScreen.tStorePerfResults(out cPerf_Drums, out cPerf_Guitar,
                                    out cPerf_Bass, out chipsArray, out bTrainingMode);
                            }
                            //Original
                            //scoreIni = this.tScoreIniへBGMAdjustとHistoryとPlayCountを更新("Stage failed");

                            //Save Performance Records if necessary
                            if (!bTrainingMode)
                            {
                                //Swap if required
                                if (CDTXMania.ConfigIni
                                    .bIsSwappedGuitarBass) // #24063 2011.1.24 yyagi Gt/Bsを入れ替えていたなら、演奏結果も入れ替える
                                {
                                    CScoreIni.CPerformanceEntry t;
                                    t = cPerf_Guitar;
                                    cPerf_Guitar = cPerf_Bass;
                                    cPerf_Bass = t;
                                }

                                string strInstrument = "";
                                string strPerfSkill = "";
                                //STDGBVALUE<string> strCurrProgressBars;
                                STDGBVALUE<bool> bToSaveProgressBarRecord;
                                bToSaveProgressBarRecord.Drums = false;
                                bToSaveProgressBarRecord.Guitar = false;
                                bToSaveProgressBarRecord.Bass = false;
                                STDGBVALUE<bool> bNewProgressBarRecord;
                                bNewProgressBarRecord.Drums = false;
                                bNewProgressBarRecord.Guitar = false;
                                bNewProgressBarRecord.Bass = false;
                                bool bGuitarAndBass = false;
                                if (!cPerf_Drums.bHasAnyAutoAtAll && cPerf_Drums.nTotalChipsCount > 0)
                                {
                                    //Drums played
                                    strInstrument = " Drums";
                                    bToSaveProgressBarRecord.Drums = true;
                                }
                                else if (!cPerf_Guitar.bHasAnyAutoAtAll && cPerf_Guitar.nTotalChipsCount > 0)
                                {
                                    if (!cPerf_Bass.bHasAnyAutoAtAll && cPerf_Bass.nTotalChipsCount > 0)
                                    {
                                        // Guitar and bass played together
                                        bGuitarAndBass = true;
                                        strInstrument = " G+B";
                                        bToSaveProgressBarRecord.Guitar = true;
                                        bToSaveProgressBarRecord.Bass = true;
                                    }
                                    else
                                    {
                                        // Guitar only played
                                        strInstrument = " Guitar";
                                        bToSaveProgressBarRecord.Guitar = true;
                                    }
                                }
                                else
                                {
                                    //Bass only played
                                    strInstrument = " Bass";
                                    bToSaveProgressBarRecord.Bass = true;
                                }

                                string str = "";
                                string strSpeed = "";
                                if (CDTXMania.ConfigIni.nPlaySpeed != 20)
                                {
                                    double d = CDTXMania.ConfigIni.nPlaySpeed / 20.0;
                                    strSpeed = (bGuitarAndBass ? " x" : " Speed x") + d.ToString("0.00");
                                }

                                str = $"Stage failed{strInstrument} {strSpeed}";

                                scoreIni = CDTXMania.tScoreIniへBGMAdjustとHistoryとPlayCountを更新(str);

                                CScore cScore = CDTXMania.confirmedChart;

                                if (bToSaveProgressBarRecord.Drums)
                                {
                                    scoreIni.stSection.LastPlayDrums.strProgress = cPerf_Drums.strProgress;

                                    if (CScoreIni.tCheckIfUpdateProgressBarRecordOrNot(
                                            cScore.SongInformation.progress.Drums, cPerf_Drums.strProgress))
                                    {
                                        scoreIni.stSection.HiSkillDrums.strProgress = cPerf_Drums.strProgress;
                                        bNewProgressBarRecord.Drums = true;
                                    }
                                }

                                if (bToSaveProgressBarRecord.Guitar)
                                {
                                    scoreIni.stSection.LastPlayGuitar.strProgress = cPerf_Guitar.strProgress;
                                    if (CScoreIni.tCheckIfUpdateProgressBarRecordOrNot(
                                            cScore.SongInformation.progress.Guitar, cPerf_Guitar.strProgress))
                                    {
                                        scoreIni.stSection.HiSkillGuitar.strProgress = cPerf_Guitar.strProgress;
                                        bNewProgressBarRecord.Guitar = true;
                                    }
                                }

                                if (bToSaveProgressBarRecord.Bass)
                                {
                                    scoreIni.stSection.LastPlayBass.strProgress = cPerf_Bass.strProgress;
                                    if (CScoreIni.tCheckIfUpdateProgressBarRecordOrNot(
                                            cScore.SongInformation.progress.Bass, cPerf_Bass.strProgress))
                                    {
                                        scoreIni.stSection.HiSkillBass.strProgress = cPerf_Bass.strProgress;
                                        bNewProgressBarRecord.Bass = true;
                                    }
                                }

                                scoreIni.tExport(CDTXMania.DTX.strFileNameFullPath + ".score.ini");

                                if (!CDTXMania.bCompactMode)
                                {
                                    if (bNewProgressBarRecord.Drums)
                                    {
                                        // New Song Progress
                                        cScore.SongInformation.progress.Drums = cPerf_Drums.strProgress;
                                    }

                                    if (bNewProgressBarRecord.Guitar)
                                    {
                                        // New Song Progress
                                        cScore.SongInformation.progress.Guitar = cPerf_Guitar.strProgress;
                                    }

                                    if (bNewProgressBarRecord.Bass)
                                    {
                                        // New Song Progress
                                        cScore.SongInformation.progress.Bass = cPerf_Bass.strProgress;
                                    }
                                }
                            }
                        }

                        CDTXMania.DTX.tStopPlayingAllChips();
                        CDTXMania.DTX.OnDeactivate();
                        if (CDTXMania.bCompactMode)
                        {
                            CDTXMania.app.Window.Close();
                        }
                        else
                        {
                            tChangeStage(stageSongSelectionNew);
                        }

                        break;

                    #endregion

                    case (int)EPerfScreenReturnValue.StageClear:

                        #region [ 演奏クリア ]

                        //-----------------------------
                        CScoreIni.CPerformanceEntry cPerfEntry_Drums, cPerfEntry_Guitar, cPerfEntry_Bass;
                        bool bIsTrainingMode = false;
                        CChip[] chipArray = new CChip[10];
                        if (CDTXMania.ConfigIni.bGuitarRevolutionMode)
                        {
                            stagePerfGuitarScreen.tStorePerfResults(out cPerfEntry_Drums, out cPerfEntry_Guitar,
                                out cPerfEntry_Bass, out bIsTrainingMode);
                            //Transfer nTimingHitCount to stageResult
                            stageResult.nTimingHitCount = stagePerfGuitarScreen.nTimingHitCount;
                        }
                        else
                        {
                            stagePerfDrumsScreen.tStorePerfResults(out cPerfEntry_Drums, out cPerfEntry_Guitar,
                                out cPerfEntry_Bass, out chipArray, out bIsTrainingMode);
                            //Transfer nTimingHitCount to stageResult
                            stageResult.nTimingHitCount = stagePerfDrumsScreen.nTimingHitCount;
                        }

                        if (!bIsTrainingMode)
                        {
                            if (CDTXMania.ConfigIni
                                .bIsSwappedGuitarBass) // #24063 2011.1.24 yyagi Gt/Bsを入れ替えていたなら、演奏結果も入れ替える
                            {
                                CScoreIni.CPerformanceEntry t;
                                t = cPerfEntry_Guitar;
                                cPerfEntry_Guitar = cPerfEntry_Bass;
                                cPerfEntry_Bass = t;

                                CDTXMania.DTX.SwapGuitarBassInfos(); // 譜面情報も元に戻す
                                CDTXMania.ConfigIni.SwapGuitarBassInfos_AutoFlags(); // #24415 2011.2.27 yyagi
                                // リザルト集計時のみ、Auto系のフラグも元に戻す。
                                // これを戻すのは、リザルト集計後。
                            } // "case CStage.EStage.Result:"のところ。

                            double ps = 0.0;
                            int nRank = 0;
                            string strInstrument = "";
                            string strPerfSkill = "";
                            bool bGuitarAndBass = false;
                            if (cPerfEntry_Drums is { bHasAnyAutoAtAll: false, nTotalChipsCount: > 0 })
                            {
                                //Drums played
                                strPerfSkill = $" {cPerfEntry_Drums.dbPerformanceSkill:F2}";
                                nRank = (CDTXMania.ConfigIni.nSkillMode == 0)
                                    ? CScoreIni.tCalculateRankOld(cPerfEntry_Drums)
                                    : CScoreIni.tCalculateRank(0, cPerfEntry_Drums.dbPerformanceSkill);
                            }
                            else if (cPerfEntry_Guitar is { bHasAnyAutoAtAll: false, nTotalChipsCount: > 0 })
                            {
                                if (cPerfEntry_Bass is { bHasAnyAutoAtAll: false, nTotalChipsCount: > 0 })
                                {
                                    // Guitar and bass played together
                                    bGuitarAndBass = true;
                                    strPerfSkill = String.Format("{0:F2}/{1:F2}",
                                        cPerfEntry_Guitar.dbPerformanceSkill, cPerfEntry_Bass.dbPerformanceSkill);
                                    nRank = CScoreIni.tCalculateOverallRankValue(cPerfEntry_Drums,
                                        cPerfEntry_Guitar, cPerfEntry_Bass);
                                    strInstrument = " G+B";
                                }
                                else
                                {
                                    // Guitar only played
                                    strPerfSkill = $" {cPerfEntry_Guitar.dbPerformanceSkill:F2}";
                                    nRank = (CDTXMania.ConfigIni.nSkillMode == 0)
                                        ? CScoreIni.tCalculateRankOld(cPerfEntry_Guitar)
                                        : CScoreIni.tCalculateRank(0, cPerfEntry_Guitar.dbPerformanceSkill);
                                    strInstrument = " Guitar";
                                }
                            }
                            else
                            {
                                //Bass only played
                                strPerfSkill = $" {cPerfEntry_Bass.dbPerformanceSkill:F2}";
                                nRank = (CDTXMania.ConfigIni.nSkillMode == 0)
                                    ? CScoreIni.tCalculateRankOld(cPerfEntry_Bass)
                                    : CScoreIni.tCalculateRank(0, cPerfEntry_Bass.dbPerformanceSkill);
                                strInstrument = " Bass";
                            }

                            string str = "";
                            if (nRank == (int)CScoreIni.ERANK.UNKNOWN)
                            {
                                str = "Cleared (No chips)";
                            }
                            else
                            {
                                string strSpeed = "";
                                if (CDTXMania.ConfigIni.nPlaySpeed != 20)
                                {
                                    double d = (double)(CDTXMania.ConfigIni.nPlaySpeed / 20.0);
                                    strSpeed = (bGuitarAndBass ? " x" : " Speed x") + d.ToString("0.00");
                                }

                                str = string.Format("Cleared{0} ({1}:{2}{3})", strInstrument,
                                    Enum.GetName(typeof(CScoreIni.ERANK), nRank), strPerfSkill, strSpeed);
                            }

                            scoreIni = CDTXMania.tScoreIniへBGMAdjustとHistoryとPlayCountを更新(str);
                        }

                        stageResult.stPerformanceEntry.Drums = cPerfEntry_Drums;
                        stageResult.stPerformanceEntry.Guitar = cPerfEntry_Guitar;
                        stageResult.stPerformanceEntry.Bass = cPerfEntry_Bass;
                        stageResult.rEmptyDrumChip = chipArray;
                        stageResult.bIsTrainingMode = bIsTrainingMode;

                        tChangeStage(stageResult);
                        break;
                    //-----------------------------

                    #endregion
                }

                //-----------------------------

                #endregion

                break;

            case CStage.EStage.Result_7:

                #region [ *** ]

                //-----------------------------
                if (nUpdateAndDrawReturnValue != 0)
                {
                    if (CDTXMania.ConfigIni
                        .bIsSwappedGuitarBass) // #24415 2011.2.27 yyagi Gt/Bsを入れ替えていたなら、Auto状態をリザルト画面終了後に元に戻す
                    {
                        CDTXMania.ConfigIni.SwapGuitarBassInfos_AutoFlags(); // Auto入れ替え
                    }

                    CDTXMania.DTX.tPausePlaybackForAllChips();
                    CDTXMania.DTX.OnDeactivate();
                    rCurrentStage.OnDeactivate();
                    if (!CDTXMania.bCompactMode)
                    {
                        tChangeStage(stageSongSelectionNew);
                    }
                    else
                    {
                        CDTXMania.app.Window.Close();
                    }
                }

                //-----------------------------

                #endregion

                break;

            case CStage.EStage.ChangeSkin_9:

                #region [ *** ]

                //-----------------------------
                if (nUpdateAndDrawReturnValue != 0)
                {
                    tChangeStage(stageSongSelectionNew);
                }

                //-----------------------------

                #endregion

                break;

            case CStage.EStage.End_8:

                #region [ *** ]

                //-----------------------------
                if (nUpdateAndDrawReturnValue != 0)
                {
                    CDTXMania.app.Exit();
                }

                //-----------------------------

                #endregion

                break;
        }
    }

    public static bool preventStageChanges;

    public void tChangeStage(CStage newStage, bool activateNewStage = true, bool deactivateOldStage = true)
    {
        nextStage = newStage;
        this.activateNewStage = activateNewStage;
        this.deactivateOldStage = deactivateOldStage;
        stageChangeRequested = true;
    }

    private bool stageChangeRequested = false;
    private CStage nextStage;
    private bool activateNewStage;
    private bool deactivateOldStage;
    public void HandleStageChanges()
    {
        if (preventStageChanges) return;
        if (!stageChangeRequested) return;
        
        if (deactivateOldStage)
        {
            rCurrentStage.OnDeactivate();
        }

        Trace.TraceInformation("----------------------");
        Trace.TraceInformation($"■ {nextStage.eStageID}");
        Trace.TraceInformation($"Changing Stage from {rCurrentStage.GetType().Name} to {nextStage.GetType().Name}");

        if (activateNewStage)
        {
            nextStage.OnActivate();
        }

        rPreviousStage = rCurrentStage;
        rCurrentStage = nextStage;
        
        stageChangeRequested = false;

        CDTXMania.tRunGarbageCollector();
    }
}