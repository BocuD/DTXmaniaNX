using System.Diagnostics;
using System.Drawing;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using FDK;

namespace DTXMania;

internal partial class CStagePerfDrumsScreen : CStagePerfCommonScreen
{
    private bool useNewDrumsFire = true;

    public CStagePerfDrumsScreen()
    {
        eStageID = EStage.Performance_6;
        ePhaseID = EPhase.Common_DefaultState;
        bActivated = false;
        listChildActivities.Add(actPad = new CActPerfDrumsPad());
        listChildActivities.Add(actCombo = new CActPerfDrumsComboDGB());
        listChildActivities.Add(actDANGER = new CActPerfDrumsDanger());

        listChildActivities.Add(actGauge = new CActPerfCommonGauge());
        listChildActivities.Add(actGraph = new CActPerfSkillMeter());
        listChildActivities.Add(actJudgeString = new CActPerfDrumsJudgementString());
        listChildActivities.Add(actLaneFlushD = new CActPerfDrumsLaneFlushD());
        listChildActivities.Add(actScore = new CActPerfDrumsScore());
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
        
        LegacyDrawable lanes = ui.AddChild(new LegacyDrawable(DrawDrumsLanes));
        lanes.name = "drumsLanes";
        lanes.renderOrder = LegacyLanesLayer;

        LegacyDrawable overlay = ui.AddChild(new LegacyDrawable(DrawDrumsOverlay));
        overlay.name = "drumsOverlay";
        overlay.renderOrder = LegacyOverlayLayer;

        UIDrawable chipFire = ui.AddChild<UIDrawable>(useNewDrumsFire ? new ActPerfNewFire(0) : new ClassicDrumsChipFire());
        chipFire.name = "drumChipsFire";
        chipFire.renderOrder = EffectsLayer;

        actChipFireD = (IPerfFire)chipFire;
        actChipFireD.iPosY = CDTXMania.ConfigIni.bReverse.Drums ? nJudgeLinePosY.Drums - 183 : nJudgeLinePosY.Drums - 186;

        actJudgeString.InitUI(ui);
    }

    // Methods

    public void tStorePerfResults( out CScoreIni.CPerformanceEntry Drums, out CScoreIni.CPerformanceEntry Guitar, out CScoreIni.CPerformanceEntry Bass, out CChip[] r空打ちドラムチップ, out bool bIsTrainingMode)
    {
        tStorePerfResults_Drums( out Drums );
        tStorePerfResults_Guitar( out Guitar );
        tStorePerfResultsBass( out Bass );

        r空打ちドラムチップ = new CChip[ 12 ];
        for ( int i = 0; i < 12; i++ )
        {
            r空打ちドラムチップ[ i ] = r空うちChip( EInstrumentPart.DRUMS, (EPad) i );
            if ( r空打ちドラムチップ[ i ] == null )
            {
                r空打ちドラムチップ[ i ] = r指定時刻に一番近いChip_ヒット未済問わず不可視考慮( CDTXMania.Timer.nCurrentTime, nパッド0Atoチャンネル0A[ i ], nInputAdjustTimeMs.Drums );
            }
        }
        bIsTrainingMode = this.bIsTrainingMode;
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
            bBonus = false;
            txChip = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_chips_drums.png"));
            txLaneCover = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_lanes_Cover_cls.png"));


            base.OnManagedCreateResources();
        }
    }

    public override void FirstUpdate()
    {
        AudioMixer.Timer.tReset();
        CDTXMania.Timer.tReset();
        actChipFireD.Start(ELane.HH, false, false, false, 0, false); // #31554 2013.6.12 yyagi

        ctChipPatternAnimation.Drums = new CCounter(0, 7, 70, CDTXMania.Timer);
        double UnitTime;
        UnitTime = ((60.0 / (CDTXMania.stagePerfDrumsScreen.actPlayInfo.dbBPM) / 14.0));
        ctBPMBar = new CCounter(1, 14, (int)(UnitTime * 1000.0), CDTXMania.Timer);

        ctComboTimer = new CCounter(1, 16,
            (int)((60.0 / (CDTXMania.stagePerfDrumsScreen.actPlayInfo.dbBPM) / 16) * 1000.0), CDTXMania.Timer);

        ctChipPatternAnimation.Guitar = new CCounter(0, 0x17, 20, CDTXMania.Timer);
        ctChipPatternAnimation.Bass = new CCounter(0, 0x17, 20, CDTXMania.Timer);
        ctWailingChipPatternAnimation = new CCounter(0, 4, 50, CDTXMania.Timer);
        ePhaseID = EPhase.Common_FadeIn;

        ct登場用.tUpdate();
        
        // display presence now that the initial timer reset has been performed
        tDisplayPresence();
    }

    public override int OnUpdateAndDraw()
    {
        //base draws the UI, which is gone once deactivated
        if (!bActivated) return 0;

        base.OnUpdateAndDraw();

        if (bIsFinishedFadeout)
        {
            if (!CDTXMania.Skin.soundStageClear.bIsPlaying && !CDTXMania.Skin.soundSTAGEFAILED音.bIsPlaying)
            {
                Debug.WriteLine("Total OnUpdateAndDraw=" + sw.ElapsedMilliseconds + "ms");
                nNumberOfMistakes = nHitCount_ExclAuto.Drums.Miss + nHitCount_ExclAuto.Drums.Poor;
                switch (nNumberOfMistakes)
                {
                    case 0:
                    {
                        nNumberPerfects = nHitCount_ExclAuto.Drums.Perfect;
                        if (CDTXMania.ConfigIni.bAllDrumsAreAutoPlay)
                        {
                            nNumberPerfects = nHitCount_IncAuto.Drums.Perfect;
                        }

                        //EXC
                        if (nNumberPerfects == CDTXMania.DTX.nVisibleChipsCount.Drums)
                        {
                            bExc = true;
                            if (CDTXMania.ConfigIni.nSkillMode == 1)
                                actScore.nCurrentTrueScore.Drums += 30000;
                            break;
                        }

                        //FULL COMBO
                        bFullCom = true;
                        if (CDTXMania.ConfigIni.nSkillMode == 1)
                            actScore.nCurrentTrueScore.Drums += 15000;
                        break;
                    }
                }

                return (int)eReturnValueAfterFadeOut;
            }
        }

        if (ePhaseID == EPhase.PERFORMANCE_STAGE_RESTART)
        {
            Trace.TraceInformation("Restarting");
            return (int)eReturnValueAfterFadeOut;
        }
        
        return 0;
    }

    private static readonly int probeLegacyDraw = AllocationProbe.Register("Legacy draw (all)");
    private static readonly int probeVideo = AllocationProbe.Register("video", 1);
    private static readonly int probeLanes = AllocationProbe.Register("lanes", 1);
    private static readonly int probeChips = AllocationProbe.Register("chips", 1);
    private static readonly int probeScore = AllocationProbe.Register("score", 1);
    private static readonly int probeCombo = AllocationProbe.Register("combo", 1);
    private static readonly int probeGraph = AllocationProbe.Register("graph", 1);
    private static readonly int probeJudgement = AllocationProbe.Register("judgement string", 1);
    private static readonly int probeOverlay = AllocationProbe.Register("debug overlay", 1);
    private static readonly int probeEffects = AllocationProbe.Register("chip fire", 1);
    private static readonly int probeFade = AllocationProbe.Register("fade", 1);
    private static readonly int probeKeyInput = AllocationProbe.Register("key input", 1);

    public override void OnUpdate()
    {
        if (!bActivated) return;

        if (CDTXMania.ConfigIni.bSTAGEFAILEDEnabled && !bIsTrainingMode && actGauge.IsFailed(EInstrumentPart.DRUMS)
            && ePhaseID == EPhase.Common_DefaultState)
        {
            actStageFailed.Start();
            CDTXMania.DTX.tStopPlayingAllChips();
            ePhaseID = EPhase.PERFORMANCE_STAGE_FAILED;
        }

        AllocationProbe.Begin(probeVideo);
        UpdateVideo();
        AllocationProbe.End(probeVideo);

        AllocationProbe.Begin(probeChips);
        bIsFinishedPlaying = UpdateChips(EInstrumentPart.DRUMS);
        AllocationProbe.End(probeChips);

        //loop instead of going to the result screen
        if (bIsFinishedPlaying && ePhaseID == EPhase.Common_DefaultState && previewMode)
        {
            tRestartForPreview();
        }
        else if (bIsFinishedPlaying && (ePhaseID == EPhase.Common_DefaultState))
        {
            if ((actGauge.IsFailed(EInstrumentPart.DRUMS)) && (ePhaseID == EPhase.Common_DefaultState))
            {
                actStageFailed.Start();
                CDTXMania.DTX.tStopPlayingAllChips();
                ePhaseID = EPhase.PERFORMANCE_STAGE_FAILED;
            }
            else
            {
                eReturnValueAfterFadeOut = EPerfScreenReturnValue.StageClear;
                ePhaseID = EPhase.PERFORMANCE_STAGE_CLEAR;
                tStartResultDelay();
            }
        }

        UpdateStageFailed();

        if (LoopEndMs != -1 && AudioMixer.Timer.nCurrentTime > LoopEndMs)
        {
            Trace.TraceInformation("Reached end of loop");
            tJumpInSong(LoopBeginMs == -1 ? 0 : LoopBeginMs);

            //Reset hit counts and scores, so that the displayed score reflects the looped part only
            nHitCount_ExclAuto.Drums.Perfect = 0;
            nHitCount_ExclAuto.Drums.Great = 0;
            nHitCount_ExclAuto.Drums.Good = 0;
            nHitCount_ExclAuto.Drums.Poor = 0;
            nHitCount_ExclAuto.Drums.Miss = 0;
            actCombo.nCurrentCombo.Drums = 0;
            actCombo.nCurrentCombo.HighestValue.Drums = 0;
            actScore.nCurrentTrueScore.Drums = 0;

            nTimingHitCount.Drums.nLate = 0;
            nTimingHitCount.Drums.nEarly = 0;
        }

        AllocationProbe.Begin(probeKeyInput);
        tHandleKeyInput();
        AllocationProbe.End(probeKeyInput);
    }

    private void DrawDrumsLanes()
    {
        AllocationProbe.Begin(probeLegacyDraw);
        sw.Start();

        bIsFinishedFadeout = false;
        bExc = false;
        bFullCom = false;

        DrawVideoFillIn();

        AllocationProbe.Begin(probeLanes);
        tUpdateAndDraw_LaneFlushD();
        tUpdateAndDraw_ScrollSpeed();
        AllocationProbe.End(probeLanes);

        AllocationProbe.Begin(probeChips);
        tUpdateAndDraw_ChipAnimation();
        DrawChips(EInstrumentPart.DRUMS);
        AllocationProbe.End(probeChips);

        AllocationProbe.Begin(probeLanes);
        actProgressBar.OnUpdateAndDraw();

        DrawLaneCover();
        AllocationProbe.End(probeLanes);

        sw.Stop();
        AllocationProbe.End(probeLegacyDraw);
    }

    //drawn over the judge lines and shutters
    private void DrawDrumsOverlay()
    {
        AllocationProbe.Begin(probeLegacyDraw);
        sw.Start();

        AllocationProbe.Begin(probeLanes);
        tUpdateAndDraw_DrumPad();
        AllocationProbe.End(probeLanes);
        
        //handle end of performance
        AllocationProbe.Begin(probeFade);
        bIsFinishedFadeout = tUpdateAndDraw_FadeIn_Out();
        AllocationProbe.End(probeFade);


        AllocationProbe.Begin(probeScore);
        if (CDTXMania.ConfigIni.bShowScore)
            tUpdateAndDraw_Score();
        AllocationProbe.End(probeScore);

        AllocationProbe.Begin(probeCombo);
        tUpdateAndDraw_Combo();
        AllocationProbe.End(probeCombo);

        AllocationProbe.Begin(probeGraph);
        tUpdateAndDraw_Graph();
        AllocationProbe.End(probeGraph);

        AllocationProbe.Begin(probeOverlay);
        tUpdateAndDraw_PerformanceInformation();
        AllocationProbe.End(probeOverlay);

        AllocationProbe.Begin(probeJudgement);
        tUpdateAndDraw_JudgementString1_ForNormalPosition();
        AllocationProbe.End(probeJudgement);
        AllocationProbe.Begin(probeEffects);
        tUpdateAndDraw_JudgementString2_ForPositionOnJudgementLine();
        AllocationProbe.End(probeEffects);

        sw.Stop();
        AllocationProbe.End(probeLegacyDraw);
    }

    private void DrawLaneCover()
    {
        //シャッターを使うのはLC、LP、FT、RDレーンのみ。その他のレーンでは一切使用しない。
        //If Skill Mode is CLASSIC, always display lvl as Classic Style
        if (CDTXMania.ConfigIni.nSkillMode == 0 || (CDTXMania.ConfigIni.bClassicScoreDisplay &&
                                                    (!CDTXMania.DTX.bHasChips.LeftCymbal &&
                                                     !CDTXMania.DTX.bHasChips.FT &&
                                                     !CDTXMania.DTX.bHasChips.Ride &&
                                                     !CDTXMania.DTX.bHasChips.LP &&
                                                     !CDTXMania.DTX.bHasChips.LBD &&
                                                     !CDTXMania.DTX.bForceXGChart)))
        {
            if (txLaneCover != null)
            {
                {
                    txLaneCover.tDraw2D(295, 0, new RectangleF(0, 0, 70, 720));
                }
                {
                    //レーンタイプでの入れ替わりあり
                    if (CDTXMania.ConfigIni.eLaneType.Drums == EType.A ||
                        CDTXMania.ConfigIni.eLaneType.Drums == EType.C)
                    {
                        txLaneCover.tDraw2D(416, 0, new RectangleF(124, 0, 54, 720));
                    }
                    else if (CDTXMania.ConfigIni.eLaneType.Drums == EType.B)
                    {
                        txLaneCover.tDraw2D(470, 0, new RectangleF(124, 0, 54, 720));
                    }
                    else if (CDTXMania.ConfigIni.eLaneType.Drums == EType.D)
                    {
                        txLaneCover.tDraw2D(522, 0, new RectangleF(124, 0, 54, 720));
                    }
                }
                {
                    txLaneCover.tDraw2D(690, 0, new RectangleF(71, 0, 52, 720));
                }
                {
                    //RDPositionで入れ替わり
                    if (CDTXMania.ConfigIni.eRDPosition == ERDPosition.RCRD)
                    {
                        txLaneCover.tDraw2D(815, 0, new RectangleF(178, 0, 38, 720));
                    }
                    else if (CDTXMania.ConfigIni.eRDPosition == ERDPosition.RDRC)
                    {
                        txLaneCover.tDraw2D(743, 0, new RectangleF(178, 0, 38, 720));
                    }
                }
            }
        }
    }


    // Other

    #region [ private ]
    //-----------------
    public bool bExc;
    public bool bFullCom;
    public int nNumberOfMistakes;
    public int nNumberPerfects;
    private IPerfFire actChipFireD;
    public CActPerfDrumsPad actPad;
    public bool bInFillIn;
    public bool bEndFillIn;
    public bool bChorusSection;
    public bool bBonus;
    private readonly EPad[] eChannelToPad = new EPad[12]
    {
        EPad.HH, EPad.SD, EPad.BD, EPad.HT,
        EPad.LT, EPad.CY, EPad.FT, EPad.HHO,
        EPad.RD, EPad.UNKNOWN, EPad.UNKNOWN, EPad.LC
    };
    private int[] nチャンネルtoX座標 = new int[] { 370, 470, 582, 527, 645, 748, 694, 373, 815, 298, 419, 419 };
    private int[] nチャンネルtoX座標B = new int[] { 370, 419, 533, 596, 645, 748, 694, 373, 815, 298, 476, 476 };
    private int[] nチャンネルtoX座標C = new int[] { 370, 470, 533, 596, 645, 748, 694, 373, 815, 298, 419, 419 };
    private int[] nチャンネルtoX座標D = new int[] { 370, 419, 582, 476, 645, 748, 694, 373, 815, 298, 525, 525 };
    private int[] nチャンネルtoX座標改 = new int[] { 370, 470, 582, 527, 645, 786, 694, 373, 746, 298, 419, 419 };
    private int[] nチャンネルtoX座標B改 = new int[] { 370, 419, 533, 596, 645, 786, 694, 373, 746, 298, 476, 476 };
    private int[] nチャンネルtoX座標C改 = new int[] { 370, 470, 533, 596, 644, 786, 694, 373, 746, 298, 419, 419 };
    private int[] nチャンネルtoX座標D改 = new int[] { 370, 419, 582, 476, 645, 786, 694, 373, 746, 298, 527, 527 };

    //the 改 tables are for RD RC
    private int ChipX(EChannel channel)
    {
        bool rdrc = CDTXMania.ConfigIni.eRDPosition == ERDPosition.RDRC;
        int[] table = CDTXMania.ConfigIni.eLaneType.Drums switch
        {
            EType.A => rdrc ? nチャンネルtoX座標改 : nチャンネルtoX座標,
            EType.B => rdrc ? nチャンネルtoX座標B改 : nチャンネルtoX座標B,
            EType.C => rdrc ? nチャンネルtoX座標C改 : nチャンネルtoX座標C,
            EType.D => rdrc ? nチャンネルtoX座標D改 : nチャンネルtoX座標D,
            _ => nチャンネルtoX座標
        };

        return table[channel - EChannel.HiHatClose];
    }

    private int[] nボーナスチャンネルtoX座標 = new int[] { 0, 298, 370, 419, 470, 527, 582, 645, 694, 748, 815, 0 };
    private int[] nボーナスチャンネルtoX座標B = new int[] { 0, 298, 370, 476, 419, 596, 533, 645, 694, 748, 815, 476 };
    private int[] nボーナスチャンネルtoX座標C = new int[] { 0, 298, 370, 419, 470, 596, 533, 645, 694, 748, 815, 419 };
    private int[] nボーナスチャンネルtoX座標D = new int[] { 0, 298, 370, 527, 420, 477, 582, 645, 694, 748, 815, 527 };
    private int[] nボーナスチャンネルtoX座標改 = new int[] { 0, 298, 370, 419, 470, 527, 582, 645, 694, 786, 748, 419 };
    private int[] nボーナスチャンネルtoX座標B改 = new int[] { 0, 298, 370, 476, 419, 596, 533, 645, 694, 786, 748, 476 };
    private int[] nボーナスチャンネルtoX座標C改 = new int[] { 0, 298, 370, 419, 470, 596, 533, 645, 694, 786, 748, 419 };
    private int[] nボーナスチャンネルtoX座標D改 = new int[] { 0, 298, 370, 527, 420, 477, 582, 645, 694, 786, 748, 527 };
    //HH SD BD HT LT CY FT HHO RD LC LP LBD
    //レーンタイプB
    //LC 298  HH 371 HHO 374  SD 420  LP 477  BD 534  HT 597 LT 646  FT 695  CY 749  RD 815
    //レーンタイプC

    public double UnitTime;
    private BaseTexture txLaneCover;
    //-----------------
    protected override EJudgement tProcessChipHit( long nHitTime, CChip pChip, bool bCorrectLane )
    {
        EJudgement eJudgeResult = tProcessChipHit( nHitTime, pChip, EInstrumentPart.DRUMS, bCorrectLane );
        // #24074 2011.01.23 add ikanick
        if (CDTXMania.ConfigIni.nSkillMode == 0)
        {
            actGraph.dbグラフ値現在_渡 = CScoreIni.tCalculatePlayingSkillOld(CDTXMania.DTX.nVisibleChipsCount.Drums, nHitCount_ExclAuto.Drums.Perfect, nHitCount_ExclAuto.Drums.Great, nHitCount_ExclAuto.Drums.Good, nHitCount_ExclAuto.Drums.Poor, nHitCount_ExclAuto.Drums.Miss, actCombo.nCurrentCombo.HighestValue.Drums, EInstrumentPart.DRUMS, bIsAutoPlay);
        }
        else if (CDTXMania.ConfigIni.nSkillMode == 1)
        {
            actGraph.dbグラフ値現在_渡 = CScoreIni.tCalculatePlayingSkill(CDTXMania.DTX.nVisibleChipsCount.Drums, nHitCount_ExclAuto.Drums.Perfect, nHitCount_ExclAuto.Drums.Great, nHitCount_ExclAuto.Drums.Good, nHitCount_ExclAuto.Drums.Poor, nHitCount_ExclAuto.Drums.Miss, actCombo.nCurrentCombo.HighestValue.Drums, EInstrumentPart.DRUMS, bIsAutoPlay);
        }
        // #35411 2015.09.07 add chnmr0
        if ( CDTXMania.listTargetGhsotLag.Drums != null &&
            CDTXMania.ConfigIni.eTargetGhost.Drums == ETargetGhostData.ONLINE &&
            CDTXMania.DTX.nVisibleChipsCount.Drums > 0 )
        {
            // Online Stats の計算式
            actGraph.dbグラフ値現在_渡 = 100 *
                ( nHitCount_ExclAuto.Drums.Perfect * 17 +
                  nHitCount_ExclAuto.Drums.Great * 7 +
                  actCombo.nCurrentCombo.HighestValue.Drums * 3 ) / ( 20.0 * CDTXMania.DTX.nVisibleChipsCount.Drums );
        }

        dbAchievementRate.Drums = bIsTrainingMode ? 0 : actGraph.dbグラフ値現在_渡;
        return eJudgeResult;
    }

    protected override void tチップのヒット処理_BadならびにTight時のMiss( EInstrumentPart part )
    {
        tチップのヒット処理_BadならびにTight時のMiss( part, 0, EInstrumentPart.DRUMS );
    }
    protected override void tチップのヒット処理_BadならびにTight時のMiss( EInstrumentPart part, int nLane )
    {
        tチップのヒット処理_BadならびにTight時のMiss( part, nLane, EInstrumentPart.DRUMS );
    }

    protected override void tJudgeLineMovingUpandDown()
    {
        actChipFireD.iPosY = (CDTXMania.ConfigIni.bReverse.Drums ? nJudgeLinePosY.Drums - 183 : nJudgeLinePosY.Drums - 186);
        CDTXMania.stagePerfDrumsScreen.actPlayInfo.jl = (CDTXMania.ConfigIni.bReverse.Drums ? 0 : nJudgeLineMaxPosY - nJudgeLinePosY.Drums);
    }

    protected override void ScrollSpeedUp()
    {
        CDTXMania.ConfigIni.nScrollSpeed.Drums = Math.Min( CDTXMania.ConfigIni.nScrollSpeed.Drums + 1, 1999 );
    }
    protected override void ScrollSpeedDown()
    {
        CDTXMania.ConfigIni.nScrollSpeed.Drums = Math.Max( CDTXMania.ConfigIni.nScrollSpeed.Drums - 1, 0 );
    }
    
    protected override void tUpdateAndDraw_DANGER()
    {
        actDANGER.tUpdateAndDraw( actGauge.IsDanger(EInstrumentPart.DRUMS), false, false );
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

    private void tUpdateAndDraw_DrumPad()  // t進行描画_ドラムパッド
    {
        actPad.OnUpdateAndDraw();
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

    protected override void DrawChip_BarLine( CConfigIni configIni, ref CDTX dTX, ref CChip pChip )
    {
        int n小節番号plus1 = pChip.nPlaybackPosition / 384;
        if ( configIni.bShowPerformanceInformation && ( configIni.nLaneDisp.Drums == 0 || configIni.nLaneDisp.Drums == 1 ) )
        {
            int n小節番号 = n小節番号plus1 - 1;
            CDTXMania.actDisplayString.tPrint( configIni.bGraph有効.Drums && configIni.bSmallGraph ? 828 : 858, (int) (configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + pChip.nDistanceFromBar.Drums) - 0x11) : ((nJudgeLinePosY.Drums - pChip.nDistanceFromBar.Drums) - 0x11)), CCharacterConsole.EFontType.White, n小節番号.ToString());
        }
        if (((configIni.nLaneDisp.Drums == 0 || configIni.nLaneDisp.Drums == 1) && pChip.bVisible) && (txChip != null))
        {
            int l_drumPanelWidth = 0x22f;
            int l_xOffset = 0;
            if (configIni.eNumOfLanes.Drums == EType.B)
            {
                l_drumPanelWidth = 0x207;
            }
            else if (CDTXMania.ConfigIni.eNumOfLanes.Drums == EType.C)
            {
                l_drumPanelWidth = 447;
                l_xOffset = 72;
            }

            txChip.tDraw2D(295 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + pChip.nDistanceFromBar.Drums) - 1) : ((nJudgeLinePosY.Drums - pChip.nDistanceFromBar.Drums) - 1), new RectangleF(0, 769, l_drumPanelWidth, 2));
        }
              
    }
    //移植完了。

    protected override void tDraw_LoopLine(CConfigIni configIni, bool bIsEnd)
    {
        const double speed = 286;	// BPM150の時の1小節の長さ[dot]
        double ScrollSpeedDrums = (actScrollSpeed.db現在の譜面スクロール速度.Drums + 1.0) * 0.5 * 37.5 * speed / 60000.0;

        int nDistanceFromBar = (int)(((bIsEnd ? LoopEndMs : LoopBeginMs) - AudioMixer.Timer.nCurrentTime) * ScrollSpeedDrums);

        //Display Loop Begin/Loop End text
        CDTXMania.actDisplayString.tPrint(830, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + nDistanceFromBar) - 0x11) : ((nJudgeLinePosY.Drums - nDistanceFromBar) - 0x11), CCharacterConsole.EFontType.White, (bIsEnd ? "End loop" : "Begin loop"));
        if ((configIni.nLaneDisp.Drums == 0 || configIni.nLaneDisp.Drums == 1))
        {
            int l_drumPanelWidth = 0x22f;
            int l_xOffset = 0;
            if (configIni.eNumOfLanes.Drums == EType.B)
            {
                l_drumPanelWidth = 0x207;
            }
            else if (CDTXMania.ConfigIni.eNumOfLanes.Drums == EType.C)
            {
                l_drumPanelWidth = 447;
                l_xOffset = 72;
            }

            if (bIsEnd)
            {
                txChip.tDraw2D(295 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + nDistanceFromBar) - 1) : ((nJudgeLinePosY.Drums - nDistanceFromBar) - 1), new RectangleF(0, 769, l_drumPanelWidth, 2));
                txChip.tDraw2D(295 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + nDistanceFromBar) + 1) : ((nJudgeLinePosY.Drums - nDistanceFromBar) - 3), new RectangleF(0, 769, l_drumPanelWidth, 2));
                txChip.tDraw2D(295 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + nDistanceFromBar) + 3) : ((nJudgeLinePosY.Drums - nDistanceFromBar) - 5), new RectangleF(0, 769, l_drumPanelWidth, 2));
                txChip.tDraw2D(295 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + nDistanceFromBar) + 9) : ((nJudgeLinePosY.Drums - nDistanceFromBar) - 11), new RectangleF(0, 769, l_drumPanelWidth, 2));
                txChip.tDraw2D(295 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + nDistanceFromBar) + 11) : ((nJudgeLinePosY.Drums - nDistanceFromBar) - 13), new RectangleF(0, 769, l_drumPanelWidth, 2));
                txChip.tDraw2D(295 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + nDistanceFromBar) + 17) : ((nJudgeLinePosY.Drums - nDistanceFromBar) - 19), new RectangleF(0, 769, l_drumPanelWidth, 2));
                txChip.tDraw2D(295 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + nDistanceFromBar) + 23) : ((nJudgeLinePosY.Drums - nDistanceFromBar) - 25), new RectangleF(0, 769, l_drumPanelWidth, 2));
            }
            else
            {
                txChip.tDraw2D(295 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + nDistanceFromBar) - 1) : ((nJudgeLinePosY.Drums - nDistanceFromBar) - 1), new RectangleF(0, 769, l_drumPanelWidth, 2));
                txChip.tDraw2D(295 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + nDistanceFromBar) - 3) : ((nJudgeLinePosY.Drums - nDistanceFromBar) + 1), new RectangleF(0, 769, l_drumPanelWidth, 2));
                txChip.tDraw2D(295 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + nDistanceFromBar) - 5) : ((nJudgeLinePosY.Drums - nDistanceFromBar) + 3), new RectangleF(0, 769, l_drumPanelWidth, 2));
                txChip.tDraw2D(295 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + nDistanceFromBar) - 11) : ((nJudgeLinePosY.Drums - nDistanceFromBar) + 9), new RectangleF(0, 769, l_drumPanelWidth, 2));
                txChip.tDraw2D(295 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + nDistanceFromBar) - 13) : ((nJudgeLinePosY.Drums - nDistanceFromBar) + 11), new RectangleF(0, 769, l_drumPanelWidth, 2));
                txChip.tDraw2D(295 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + nDistanceFromBar) - 19) : ((nJudgeLinePosY.Drums - nDistanceFromBar) + 17), new RectangleF(0, 769, l_drumPanelWidth, 2));
                txChip.tDraw2D(295 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + nDistanceFromBar) - 25) : ((nJudgeLinePosY.Drums - nDistanceFromBar) + 23), new RectangleF(0, 769, l_drumPanelWidth, 2));
            }
        }
    }

    #endregion
}