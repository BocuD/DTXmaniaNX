using System.Numerics;
using System.Diagnostics;
using System.Drawing;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using FDK;

namespace DTXMania;

internal partial class CStagePerfDrumsScreen : CStagePerfCommonScreen
{
    private DrumsPlayfield playfield;

    public CStagePerfDrumsScreen()
    {
        eStageID = EStage.Performance_6;
        ePhaseID = EPhase.Common_DefaultState;
        bActivated = false;
        listChildActivities.Add(actCombo = new CActPerfDrumsComboDGB(partData));

        listChildActivities.Add(actGraph = new CActPerfSkillMeter());
        listChildActivities.Add(actJudgeString = new CActPerfDrumsJudgementString());
        listChildActivities.Add(actLaneFlushD = new CActPerfDrumsLaneFlushD());
        listChildActivities.Add(actScore = new CActPerfDrumsScore(partData));
        listChildActivities.Add(actScrollSpeed = new CActPerfScrollSpeed());
        listChildActivities.Add(video = new CActPerfVideo());
        listChildActivities.Add(actBGA = new CActPerfBGA());
        listChildActivities.Add(actStageFailed = new CActPerfStageFailure());
        listChildActivities.Add(actPlayInfo = new CActPerformanceInformation());
        listChildActivities.Add(actProgressBar = new CActPerfProgressBar());
    }

    public override void OnLayoutReady()
    {
        base.OnLayoutReady();
        
        //before anything that reads lane positions
        playfield = FindDrumsPlayfield();
        playfield.SetParts(actScore, actCombo, actProgressBar, actJudgeString);
        playfield.Setup(actLaneFlushD);
        playfield.SetChart(timeline);
        playfields = [playfield];
        RefreshPlayfields();

        AddLegacyPart(playfield, "FillInEffect", LegacyLanesLayer, _ => DrawFillInEffect());

        UIGroup hud = LayoutGroup(HudGroup);
        AddLegacyPart(hud, "Graph", LegacyOverlayLayer, _ => Probed(probeGraph, tUpdateAndDraw_Graph));
        AddLegacyPart(hud, "PerformanceInformation", LegacyOverlayLayer, _ => Probed(probeOverlay, tUpdateAndDraw_PerformanceInformation));
    }

    // Methods

    public void tStorePerfResults( out CScoreIni.CPerformanceEntry Drums, out CScoreIni.CPerformanceEntry Guitar, out CScoreIni.CPerformanceEntry Bass, out CChip[] r空打ちドラムチップ, out bool bIsTrainingMode)
    {
        tStorePerfResults( out Drums, out Guitar, out Bass, out bIsTrainingMode );

        r空打ちドラムチップ = new CChip[ 12 ];
        for ( int i = 0; i < 12; i++ )
        {
            r空打ちドラムチップ[ i ] = drums!.FindEmptyHitSound( (EPad) i );
            if ( r空打ちドラムチップ[ i ] == null )
            {
                r空打ちドラムチップ[ i ] = timeline.NearestDrum(CDTXMania.Timer.nCurrentTime + CDTXMania.ConfigIni.nInputAdjustTimeMs.Drums, DrumsPerformance.PadChannels[i], HitState.DontCare);
            }
        }
    }


    // CStage 実装

    public override void OnActivate()
    {
        bInFillIn = false;
        base.OnActivate();
        ct登場用 = new CCounter(0, 12, 16, CDTXMania.Timer);

        actPlayInfo.jl = (CDTXMania.ConfigIni.bReverse.Drums ? nJudgeLinePosY.Drums - 159 : nJudgeLineMaxPosY - nJudgeLinePosY.Drums);

        if ( CDTXMania.bCompactMode )
        {
            var score = new CChartData();
            actGraph.dbGraphValue_Goal = score.SongInformation.HighCompletionRate[ 0 ];
        }
        else
        {
            actGraph.dbGraphValue_Goal = CDTXMania.chosenChartData.SongInformation.HighCompletionRate[ 0 ];	// #24074 2011.01.23 add ikanick
            actGraph.dbGraphValue_PersonalBest = CDTXMania.chosenChartData.SongInformation.HighCompletionRate[ 0 ];

            // #35411 2015.08.21 chnmr0 add
            // ゴースト利用可のなとき、0で初期化
            if (CDTXMania.ConfigIni.eTargetGhost.Drums != ETargetGhostData.NONE)
            {
                if (CDTXMania.listTargetGhsotLag[(int)EInstrumentPart.DRUMS] != null)
                {
                    actGraph.dbGraphValue_Goal = 0;
                }
            }
        }
    }
    
    public override void OnManagedCreateResources()
    {
        if ( bActivated )
        {
            bChorusSection = false;


            base.OnManagedCreateResources();
        }
    }

    public override void FirstUpdate()
    {
        AudioMixer.Timer.tReset();
        CDTXMania.Timer.tReset();
        playfield.StartFire(ELane.HH, false, false, false, 0, false); // #31554 2013.6.12 yyagi

        double UnitTime;
        UnitTime = ((60.0 / (CDTXMania.stagePerfDrumsScreen.actPlayInfo.dbBPM) / 14.0));
        ctBPMBar = new CCounter(1, 14, (int)(UnitTime * 1000.0), CDTXMania.Timer);

        ctComboTimer = new CCounter(1, 16,
            (int)((60.0 / (CDTXMania.stagePerfDrumsScreen.actPlayInfo.dbBPM) / 16) * 1000.0), CDTXMania.Timer);

        ctWailingChipPatternAnimation = new CCounter(0, 4, 50, CDTXMania.Timer);
        ePhaseID = EPhase.Common_DefaultState;

        ct登場用.tUpdate();
        
        // display presence now that the initial timer reset has been performed
        tDisplayPresence();
    }

    public override int OnUpdateAndDraw()
    {
        //base draws the UI, which is gone once deactivated
        if (!bActivated) return 0;

        base.OnUpdateAndDraw();

        if (CanLeave)
        {
            if (!CDTXMania.Skin.soundStageClear.bIsPlaying && !CDTXMania.Skin.soundSTAGEFAILED音.bIsPlaying)
            {
                nNumberOfMistakes = DrumsData.HitCountExclAuto.Miss + DrumsData.HitCountExclAuto.Poor;
                switch (nNumberOfMistakes)
                {
                    case 0:
                    {
                        nNumberPerfects = DrumsData.HitCountExclAuto.Perfect;
                        if (CDTXMania.ConfigIni.bAllDrumsAreAutoPlay)
                        {
                            nNumberPerfects = DrumsData.HitCountIncAuto.Perfect;
                        }

                        //EXC
                        if (nNumberPerfects == CDTXMania.DTX.nVisibleChipsCount.Drums)
                        {
                            bExc = true;
                            if (CDTXMania.ConfigIni.nSkillMode == 1)
                                DrumsData.AddClearBonus(true);
                            break;
                        }

                        //FULL COMBO
                        bFullCom = true;
                        if (CDTXMania.ConfigIni.nSkillMode == 1)
                            DrumsData.AddClearBonus(false);
                        break;
                    }
                }

                return (int)eReturnValue;
            }
        }

        if (ePhaseID == EPhase.PERFORMANCE_STAGE_RESTART)
        {
            Trace.TraceInformation("Restarting");
            return (int)eReturnValue;
        }
        
        return 0;
    }

    private static readonly int probeVideo = AllocationProbe.Register("video");
    private static readonly int probeChips = AllocationProbe.Register("chips");
    private static readonly int probeGraph = AllocationProbe.Register("graph");
    private static readonly int probeOverlay = AllocationProbe.Register("debug overlay");
    private static readonly int probeKeyInput = AllocationProbe.Register("key input");

    private static void Probed(int probe, Action draw)
    {
        AllocationProbe.Begin(probe);
        draw();
        AllocationProbe.End(probe);
    }

    public override void OnUpdate()
    {
        if (!bActivated) return;

        bExc = false;
        bFullCom = false;

        if (CDTXMania.ConfigIni.bSTAGEFAILEDEnabled && !bIsTrainingMode && !StageManager.dropStageChanges && DrumsData.IsFailed()
            && ePhaseID == EPhase.Common_DefaultState)
        {
            FailStage();
        }

        AllocationProbe.Begin(probeVideo);
        UpdateVideo();
        AllocationProbe.End(probeVideo);

        UpdateScrollSpeed();
        UpdateChipAnimation();

        AllocationProbe.Begin(probeChips);
        bIsFinishedPlaying = UpdateChips();
        AllocationProbe.End(probeChips);

        //loop instead of going to the result screen
        if (bIsFinishedPlaying && ePhaseID == EPhase.Common_DefaultState && StageManager.dropStageChanges)
        {
            Restart();
        }
        else if (bIsFinishedPlaying && (ePhaseID == EPhase.Common_DefaultState))
        {
            if ((DrumsData.IsFailed()) && (ePhaseID == EPhase.Common_DefaultState))
            {
                FailStage();
            }
            else
            {
                eReturnValue = EPerfScreenReturnValue.StageClear;
                ePhaseID = EPhase.PERFORMANCE_STAGE_CLEAR;
                tStartResultDelay();
            }
        }

        UpdateResultDelay();

        UpdateStageFailed();

        if (LoopEndMs != -1 && AudioMixer.Timer.nCurrentTime > LoopEndMs)
        {
            Trace.TraceInformation("Reached end of loop");
            tJumpInSong(LoopBeginMs == -1 ? 0 : LoopBeginMs);

            //Reset hit counts and scores, so that the displayed score reflects the looped part only
            DrumsData.ResetForLoop();
        }

        AllocationProbe.Begin(probeKeyInput);
        tHandleKeyInput();
        AllocationProbe.End(probeKeyInput);

        actScore.Update();

        UpdateLoopMarkers();
    }

    // Other

    #region [ private ]
    //-----------------
    public bool bExc;
    public bool bFullCom;
    public int nNumberOfMistakes;
    public int nNumberPerfects;
    public bool bInFillIn;
    public bool bEndFillIn;
    public bool bChorusSection;
    public double UnitTime;
    //-----------------
    protected override void UpdateGraph(EInstrumentPart part)
    {
        actGraph.dbグラフ値現在_渡 = DrumsData.CalculateSkill();
        int chips = DrumsData.ChartChips;
        if (CDTXMania.listTargetGhsotLag.Drums != null && CDTXMania.ConfigIni.eTargetGhost.Drums == ETargetGhostData.ONLINE && chips > 0)
        {
            actGraph.dbグラフ値現在_渡 = 100 * (DrumsData.HitCountExclAuto.Perfect * 17 + DrumsData.HitCountExclAuto.Great * 7 + DrumsData.MaxCombo * 3) / (20.0 * chips);
        }

        DrumsData.SetAchievementRate(bIsTrainingMode ? 0 : actGraph.dbグラフ値現在_渡);
    }

    private void ApplyDrumsEvents()
    {
        foreach (DrumsEvent e in drums!.Events)
        {
            switch (e.Kind)
            {
                case EDrumsEvent.AutoNote:
                    playfield.Hit((ELane)e.Lane, e.Pad, e.Velocity);
                    StartChipFire(e.Lane, e.Chip);
                    sequencer.PlaySound(e.Chip, AudioMixer.Timer.nResetAtMs + e.Time, EInstrumentPart.DRUMS, CDTXMania.DTX.nモニタを考慮した音量(EInstrumentPart.DRUMS));
                    break;

                case EDrumsEvent.Hit:
                    playfield.Hit((ELane)e.Lane, e.Pad, e.Velocity);
                    if (e.Fire)
                    {
                        StartChipFire(e.Lane, e.Chip);
                    }
                    PlayHitSound(e.Sound);
                    break;

                case EDrumsEvent.EmptyHit:
                    playfield.Hit((ELane)e.Lane, e.Pad, e.Velocity);
                    PlayHitSound(e.Sound);
                    if (e.Miss)
                    {
                        playfield.Judged(e.Lane, e.Auto ? EJudgement.Auto : EJudgement.Miss, 999);
                    }
                    break;

                case EDrumsEvent.Bonus:
                    playfield.Score?.AddBonus();
                    if (CDTXMania.ConfigIni.DisplayBonusEffects)
                    {
                        CDTXMania.Skin.soundAudience.tPlay();
                    }
                    break;

                case EDrumsEvent.Judged:
                    playfield.Judged(e.Lane, e.Auto ? EJudgement.Auto : e.Judgement, e.Chip.nLag);
                    bool hit = e.Judgement is EJudgement.Perfect or EJudgement.Great or EJudgement.Good;
                    if (!e.Auto && hit)
                    {
                        playfield.Progress(e.Chip.nPlaybackTimeMs, e.Judgement);
                    }
                    if (e.Judgement == EJudgement.Miss)
                    {
                        actCombo.tコンボリセット処理();
                    }
                    if (e.CountsCombo && hit)
                    {
                        actCombo.tComboAnime(EInstrumentPart.DRUMS);
                    }
                    UpdateGraph(EInstrumentPart.DRUMS);
                    break;
            }
        }
        drums.Events.Clear();
    }

    private void StartChipFire(int lane, CChip chip)
    {
        bool lastInFillIn = bInFillIn && bフィルイン区間の最後のChipである(chip);
        playfield.StartFire((ELane)lane, bInFillIn, lastInFillIn, lastInFillIn, nJudgeLinePosY_delta.Drums);
    }

    private void PlayHitSound(CChip? sound)
    {
        if (sound != null)
        {
            sequencer.PlaySound(sound, AudioMixer.Timer.nSystemTimeMs, EInstrumentPart.DRUMS, CDTXMania.ConfigIni.n手動再生音量, CDTXMania.ConfigIni.b演奏音を強調する.Drums);
        }
    }

    protected override void tHandleInput_Drums()
    {
        drums!.UpdateInput();
        if (drums.HadInput)
        {
            drums.Data.RecordInputDevices();
        }
        ApplyDrumsEvents();
    }

    protected override void tJudgeLineMovingUpandDown()
    {
        CDTXMania.stagePerfDrumsScreen.actPlayInfo.jl = (CDTXMania.ConfigIni.bReverse.Drums ? 0 : nJudgeLineMaxPosY - nJudgeLinePosY.Drums);
    }

    protected override void tUpdateAndDraw_WailingFrame()
    {
        base.tUpdateAndDraw_WailingFrame( 587, 478,
            (int)(CDTXMania.ConfigIni.bReverse.Guitar ? ( 400 - txWailingFrame.Height ) : 69),
            (int)(CDTXMania.ConfigIni.bReverse.Bass ? ( 400 - txWailingFrame.Height ) : 69)
        );
    }
    
    private void tUpdateAndDraw_Graph()  // t進行描画_グラフ
    {
        if ( CDTXMania.ConfigIni.bGraph有効.Drums )
        {
            actGraph.OnUpdateAndDraw();
        }
    }


    protected override void tUpdateAndDraw_PerformanceInformation()
    {
        base.tUpdateAndDraw_PerformanceInformation( 1000, 257 );
    }

    protected override void UpdateChip_BarLine( CConfigIni configIni, ref CDTX dTX, ref CChip pChip )
    {
        int n小節番号plus1 = pChip.nPlaybackPosition / 384;
        if ( !pChip.bHit && ( pChip.nDistanceFromBar.Drums < 0 ) )
        {
            pChip.bHit = true;

            if ( CDTXMania.ConfigIni.bMetronome )
            {
                CDTXMania.Skin.soundMetronome.tPlay();
            }

            actPlayInfo.n小節番号 = n小節番号plus1 - 1;
            if ( configIni.bWave再生位置自動調整機能有効 && bNeedsDriftCorrection )
            {
                dTX.tAutoCorrectWavPlaybackPosition();
            }
        }
    }

    #endregion
}