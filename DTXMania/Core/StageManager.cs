using System.Diagnostics;
using System.Numerics;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;

namespace DTXMania.Core;

internal class StageManager
{
    public CStageStartup stageStartup { get; }
    public CStageTitle stageTitle { get; }
    public CStageConfig stageConfig { get; }
    public CStageSongSelectionNew stageSongSelectionNew { get; }
    public CStageSongLoading stageSongLoading { get; }
    public CStagePerfGuitarScreen stagePerfGuitarScreen { get; }
    public CStagePerfDrumsScreen stagePerfDrumsScreen { get; }
    public CStageResult stageResult { get; }
    public CStageChangeSkin stageChangeSkin { get; }
    public CStageEnd stageEnd { get; }
    public CStageUITest stageUITest { get; } //dev only json layout test bed

    public CStagePerfCommonScreen stagePerfScreen =>
        CDTXMania.ConfigIni.bGuitarRevolutionMode ? stagePerfGuitarScreen : stagePerfDrumsScreen;
    
    public CStage rCurrentStage;
    public CStage rPreviousStage;

    //fade transition
    private BaseTexture? _stageSnapshot;
    private bool _inFadeTransition;
    private float _fadeAlpha;
    private long _fadeStartMs;
    private float FadeDurationMs = 1500f;

    public StageManager()
    {
        stageStartup = new CStageStartup();
        stageTitle = new CStageTitle();
        stageConfig = new CStageConfig();
        stageSongSelectionNew = new CStageSongSelectionNew();
        stageSongLoading = new CStageSongLoading();
        stagePerfDrumsScreen = new CStagePerfDrumsScreen();
        stagePerfGuitarScreen = new CStagePerfGuitarScreen();
        stageResult = new CStageResult();
        stageChangeSkin = new CStageChangeSkin();
        stageEnd = new CStageEnd();
        stageUITest = new CStageUITest();
    }
    
    public void LoadInitialStage()
    {
        if (CDTXMania.bCompactMode)
        {
            CDTXMania.SelectCompactModeChart();
        }

        //startup moves on to song loading itself in compact mode
        rCurrentStage = stageStartup;
        rCurrentStage.OnActivate();
    }

    public void UpdateStage()
    {
        if (rCurrentStage == null) return;

        //dev shortcuts: F9 opens the json layout test stage from the title, F10 dumps the current
        //stage's code-built layout into the active skin
        if (CDTXMania.InputManager != null)
        {
            if (rCurrentStage.eStageID == CStage.EStage.Title_2
                && CDTXMania.InputManager.Keyboard.bKeyPressed((int)SlimDX.DirectInput.Key.F9))
            {
                tChangeStage(stageUITest);
            }

            if (CDTXMania.InputManager.Keyboard.bKeyPressed((int)SlimDX.DirectInput.Key.F10))
            {
                UI.Skin.StageLayoutGenerator.GenerateForCurrentStage();
            }
        }

        rCurrentStage.Update();
    }

    public void DrawStage()
    {
        if (rCurrentStage == null) return;

        int nUpdateAndDrawReturnValue = rCurrentStage.OnUpdateAndDraw();

        //composite our stage snapshot while it fades out
        DrawFadeOverlay();

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

            case CStage.EStage.UITest_10:
                if (nUpdateAndDrawReturnValue == (int)CStageUITest.EReturnValue.ReturnToTitle)
                {
                    tChangeStage(stageTitle);
                }

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
                    case (int)CStageSongSelectionNew.EReturnValue.ReturnToTitle:
                        tChangeStage(stageTitle);
                        break;

                    case (int)CStageSongSelectionNew.EReturnValue.Selected:
                        tChangeStage(stageSongLoading);
                        break;

                    case (int)CStageSongSelectionNew.EReturnValue.CallConfig:
                        tChangeStage(stageConfig);
                        break;

                    case (int)CStageSongSelectionNew.EReturnValue.ChangeSking:
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
                        break;
                    }

                    #endregion

                    BeginFadeTransition(stagePerfScreen);
                }

                //-----------------------------

                #endregion

                break;

            case CStage.EStage.Performance_6:

                #region [ *** ]

                //-----------------------------
                
                switch (nUpdateAndDrawReturnValue)
                {
                    case (int)EPerfScreenReturnValue.Continue:
                        break;

                    case (int)EPerfScreenReturnValue.Interruption:
                    case (int)EPerfScreenReturnValue.Restart:

                        #region [ Cancel performance ]

                        CDTXMania.UpdateBGMAdjustHistoryPlayCountIntScoreIni("Play cancelled");

                        CDTXMania.DTX.tStopPlayingAllChips();
                        CDTXMania.DTX.OnDeactivate();
                        rCurrentStage.OnDeactivate();
                        if (CDTXMania.bCompactMode)
                        {
                            DTXManiaGL.instance.RequestExit();
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
                        PerformanceResults.SaveStageFailed(stagePerfScreen);

                        CDTXMania.DTX.tStopPlayingAllChips();
                        CDTXMania.DTX.OnDeactivate();
                        if (CDTXMania.bCompactMode)
                        {
                            DTXManiaGL.instance.RequestExit();
                        }
                        else
                        {
                            GitaDoraTransition.Close(0, () => tChangeStage(stageSongSelectionNew));
                        }

                        break;

                    #endregion

                    case (int)EPerfScreenReturnValue.StageClear:

                        #region [ 演奏クリア ]

                        PerformanceResults.SaveStageClear(stagePerfScreen,
                            out CScoreIni.CPerformanceEntry cPerfEntry_Drums, out CScoreIni.CPerformanceEntry cPerfEntry_Guitar,
                            out CScoreIni.CPerformanceEntry cPerfEntry_Bass, out CChip[] chipArray, out bool bIsTrainingMode);
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
                        DTXManiaGL.instance.RequestExit();
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
                    CDTXMania.app.tTerminate();

                    if (CDTXMania.app.isUpdateReady)
                    {
                        try { CDTXMania.app.updateService.ApplyAndRestart(CDTXMania.app.stagedUpdate); }
                        catch (Exception ex) { Trace.TraceError("Failed to launch updater: " + ex); }
                    }
                    
                    DTXManiaGL.instance.RequestExit();
                }

                //-----------------------------

                #endregion

                break;
        }
    }

    public static bool pauseStageChanges;

    //dropped changes are gone for good, so only the skin editor should set this
    public static bool dropStageChanges;

    //lets the loading screen hand over to the performance screen while everything else is held
    public CStage? handOverTo;

    public void tChangeStage(CStage newStage, bool activateNewStage = true, bool deactivateOldStage = true)
    {
        if (dropStageChanges && newStage != handOverTo)
        {
            return;
        }

        handOverTo = null;
        Request(newStage, activateNewStage, deactivateOldStage);
    }

    //ignores dropStageChanges
    public void ForceChangeStage(CStage newStage) => Request(newStage, true, true);

    private void Request(CStage newStage, bool activateNewStage, bool deactivateOldStage)
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
    //drop a queued change instead of running it later
    public void CancelPendingStageChange()
    {
        stageChangeRequested = false;
    }

    public void HandleStageChanges()
    {
        if (pauseStageChanges) return;
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
    
    //Helpers for fade transition
    private void BeginFadeTransition(CStage newStage)
    {
        var rt = CDTXMania.GameRenderTarget;
        if (rt != null)
        {
            try
            {
                byte[] pixels = rt.ReadPixels();
                _stageSnapshot?.Dispose();
                _stageSnapshot = BaseTexture.LoadFromMemory(pixels, rt.Width, rt.Height, "LoadingSnapshot");
            }
            catch (Exception e)
            {
                Trace.TraceWarning($"StageManager: could not capture loading snapshot: {e.Message}");
                _stageSnapshot = null;
            }
        }

        tChangeStage(newStage);

        if (_stageSnapshot != null)
        {
            _inFadeTransition = true;
            _fadeAlpha        = 1.0f;
            _fadeStartMs      = 0;   // will be latched after perf stage's first frame
        }
    }
    
    private void DrawFadeOverlay()
    {
        if (!_inFadeTransition || _stageSnapshot == null) return;

        // Wait until the performance stage has completed its first frame before
        // starting the fade, so the activation spike doesn't show through
        if (rCurrentStage.bJustStartedUpdate)
        {
            // _stageSnapshot.tDraw2D(0f, 0f, new RectangleF(0, 0, _stageSnapshot.Width, _stageSnapshot.Height),
            //     new Color4(1f, 1f, 1f, 1f), new Vector2(1280, 720));
            _stageSnapshot.tDraw2DMatrix(Matrix4x4.Identity, new Color4(1f, 1f, 1f));
            return;
        }

        // Latch the start time on the first frame after the spike
        if (_fadeStartMs == 0)
        {
            _fadeStartMs = CDTXMania.Timer.nCurrentTime;
        }

        long elapsed = CDTXMania.Timer.nCurrentTime - _fadeStartMs;
        _fadeAlpha = Math.Clamp(1f - (float)elapsed / FadeDurationMs, 0f, 1f);

        _stageSnapshot.tDraw2DMatrix(Matrix4x4.Identity, new Color4(1f, 1f, 1f, _fadeAlpha));
        //_stageSnapshot.tDraw2D(0f, 0f, new RectangleF(0, 0, _stageSnapshot.Width, _stageSnapshot.Height), new Color4(1f, 1f, 1f, _fadeAlpha), new Vector2(1280, 720));

        if (_fadeAlpha <= 0f)
        {
            _stageSnapshot.Dispose();
            _stageSnapshot = null;
            _inFadeTransition = false;
            _fadeStartMs = 0;
        }
    }
}