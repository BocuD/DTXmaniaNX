using System.Diagnostics;
using DTXMania.Core;
using SharpDX;
using FDK;

using Rectangle = System.Drawing.Rectangle;

namespace DTXMania;

internal class CStagePerfDrumsScreen : CStagePerfCommonScreen
{
    // Constructor

    public CStagePerfDrumsScreen()
    {
        eStageID = EStage.Performance_6;
        ePhaseID = EPhase.Common_DefaultState;
        bNotActivated = true;
        listChildActivities.Add( actPad = new CActPerfDrumsPad() );
        listChildActivities.Add( actCombo = new CActPerfDrumsComboDGB() );
        listChildActivities.Add( actDANGER = new CActPerfDrumsDanger() );
        listChildActivities.Add( actChipFireD = new CActPerfDrumsChipFireD() );
        listChildActivities.Add( actGauge = new CActPerfDrumsGauge() );
        listChildActivities.Add( actGraph = new CActPerfSkillMeter() ); // #24074 2011.01.23 add ikanick
        listChildActivities.Add( actJudgeString = new CActPerfDrumsJudgementString() );
        listChildActivities.Add( actLaneFlushD = new CActPerfDrumsLaneFlushD() );
        listChildActivities.Add( actScore = new CActPerfDrumsScore() );
        listChildActivities.Add( actStatusPanel = new CActPerfDrumsStatusPanel() );
        listChildActivities.Add( actScrollSpeed = new CActPerfScrollSpeed() );
        listChildActivities.Add( actAVI = new CActPerfAVI() );
        listChildActivities.Add( actBGA = new CActPerfBGA() );
//			base.listChildActivities.Add( this.actPanel = new CActPerfPanelString() );
        listChildActivities.Add( actStageFailed = new CActPerfStageFailure() );
        listChildActivities.Add( actPlayInfo = new CActPerformanceInformation() );
        listChildActivities.Add( actFI = new CActFIFOBlackStart() );
        listChildActivities.Add( actFO = new CActFIFOBlack() );
        listChildActivities.Add( actFOClear = new CActFIFOWhite() );
        listChildActivities.Add( actFOStageClear = new CActFIFOWhiteClear());
        listChildActivities.Add( actFillin = new CActPerfDrumsFillingEffect() );
        listChildActivities.Add( actLVFont = new CActLVLNFont() );
        listChildActivities.Add( actProgressBar = new CActPerfProgressBar());
        listChildActivities.Add(actBackgroundAVI = new CActSelectBackgroundAVI());
        //          base.listChildActivities.Add( this.actChipFireGB = new CActPerfDrumsChipFireGB());
        //			base.listChildActivities.Add( this.actLaneFlushGB = new CActPerfDrumsLaneFlushGB() );
        //			base.listChildActivities.Add( this.actRGB = new CActPerfDrumsRGB() );
        //			base.listChildActivities.Add( this.actWailingBonus = new CActPerfDrumsWailingBonus() );
        //          base.listChildActivities.Add( this.actStageCleared = new CAct演奏ステージクリア());
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
            if( r空打ちドラムチップ[ i ] == null )
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
        CScore cScore = CDTXMania.stageSongSelection.rChosenScore;
        ct登場用 = new CCounter(0, 12, 16, CDTXMania.Timer);

        actChipFireD.iPosY = (CDTXMania.ConfigIni.bReverse.Drums ? nJudgeLinePosY.Drums - 183 : nJudgeLinePosY.Drums - 186);
        actPlayInfo.jl = (CDTXMania.ConfigIni.bReverse.Drums ? nJudgeLinePosY.Drums - 159 : nJudgeLineMaxPosY - nJudgeLinePosY.Drums);

        if( CDTXMania.bCompactMode )
        {
            var score = new CScore();
            CDTXMania.SongManager.tReadScoreIniAndSetScoreInformation( CDTXMania.strCompactModeFile + ".score.ini", ref score );
            actGraph.dbGraphValue_Goal = score.SongInformation.HighSkill[ 0 ];
        }
        else
        {
            actGraph.dbGraphValue_Goal = CDTXMania.stageSongSelection.rChosenScore.SongInformation.HighSkill[ 0 ];	// #24074 2011.01.23 add ikanick
            actGraph.dbGraphValue_PersonalBest = CDTXMania.stageSongSelection.rChosenScore.SongInformation.HighSkill[ 0 ];

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
        dtLastQueueOperation = DateTime.MinValue;
    }
    public override void OnDeactivate()
    {
        base.OnDeactivate();
    }
    public override void OnManagedCreateResources()
    {
        if( !bNotActivated )
        {
            bChorusSection = false;
            bBonus = false;
            txChip = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\7_chips_drums.png"));
            txHitBar = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\ScreenPlayDrums hit-bar.png" ) );
            txシャッター = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\7_shutter.png" ) );
            txLaneCover = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\7_lanes_Cover_cls.png"));

            /*
            this.txヒットバーGB = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\ScreenPlayDrums hit-bar guitar.png" ) );
            this.txレーンフレームGB = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\ScreenPlayDrums lane parts guitar.png" ) );
            if( this.txレーンフレームGB != null )
            {
                this.txレーンフレームGB.nTransparency = 0xff - CDTXMania.ConfigIni.nBackgroundTransparency;
            }
             */

            base.OnManagedCreateResources();
        }
    }
    public override void OnManagedReleaseResources()
    {
        if( !bNotActivated )
        {
            CDTXMania.tReleaseTexture( ref txHitBar );
            CDTXMania.tReleaseTexture( ref txChip );
            CDTXMania.tReleaseTexture( ref txLaneCover );
            CDTXMania.tReleaseTexture( ref txシャッター );
//				CDTXMania.tReleaseTexture( ref this.txヒットバーGB );
//				CDTXMania.tReleaseTexture( ref this.txレーンフレームGB );
                
            base.OnManagedReleaseResources();
        }
    }

    public override void FirstUpdate()
    {
        CSoundManager.rcPerformanceTimer.tReset();
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

        if (tx判定画像anime != null && txBonusEffect != null)
        {
            tx判定画像anime.tDraw2D(CDTXMania.app.Device, 1280, 720);
            txBonusEffect.tDraw2D(CDTXMania.app.Device, 1280, 720);
        }

        actFI.tStartFadeIn();
        ct登場用.tUpdate();

        if (CDTXMania.DTXVmode.Enabled)
        {
            tSetSettingsForDTXV();
            tJumpInSongToBar(CDTXMania.DTXVmode.nStartBar + 1);
        }
        
        // display presence now that the initial timer reset has been performed
        tDisplayPresence();
    }

    public override int OnUpdateAndDraw()
    {
        sw.Start();

        if (bNotActivated) return 0;

        base.OnUpdateAndDraw();

        bIsFinishedPlaying = false;
        bIsFinishedFadeout = false;
        bExc = false;
        bFullCom = false;

        if ((CDTXMania.ConfigIni.bSTAGEFAILEDEnabled && !bIsTrainingMode && actGauge.IsFailed(EInstrumentPart.DRUMS)) &&
            (ePhaseID == EPhase.Common_DefaultState))
        {
            actStageFailed.Start();
            CDTXMania.DTX.tStopPlayingAllChips();
            ePhaseID = EPhase.PERFORMANCE_STAGE_FAILED;
        }

        tUpdateAndDraw_Background();
        tUpdateAndDraw_MIDIBGM();
        tUpdateAndDraw_AVI();
        tUpdateAndDraw_LaneFlushD();
        tUpdateAndDraw_ScrollSpeed();
        tUpdateAndDraw_ChipAnimation();
        tUpdateAndDraw_BarLines(EInstrumentPart.DRUMS);
        tDraw_LoopLines();
        tUpdateAndDraw_Chip_PatternOnly(EInstrumentPart.DRUMS);
        bIsFinishedPlaying = tUpdateAndDraw_Chips(EInstrumentPart.DRUMS);
        actProgressBar.OnUpdateAndDraw();

        #region[ シャッター ]

        //シャッターを使うのはLC、LP、FT、RDレーンのみ。その他のレーンでは一切使用しない。
        //If Skill Mode is CLASSIC, always display lvl as Classic Style
        if (CDTXMania.ConfigIni.nSkillMode == 0 || ((CDTXMania.ConfigIni.bCLASSIC譜面判別を有効にする == true) &&
                                                    ((CDTXMania.DTX.bHasChips.LeftCymbal == false) &&
                                                     (CDTXMania.DTX.bHasChips.FT == false) &&
                                                     (CDTXMania.DTX.bHasChips.Ride == false) &&
                                                     (CDTXMania.DTX.bHasChips.LP == false) &&
                                                     (CDTXMania.DTX.bHasChips.LBD == false) &&
                                                     (CDTXMania.DTX.bForceXGChart == false))))
        {
            if (txLaneCover != null)
            {
                //旧画像
                //this.txLaneCover.tDraw2D(CDTXMania.app.Device, 295, 0);
                //if (CDTXMania.DTX.bチップがある.LeftCymbal == false)
                {
                    txLaneCover.tDraw2D(CDTXMania.app.Device, 295, 0, new Rectangle(0, 0, 70, 720));
                }
                //if ((CDTXMania.DTX.bチップがある.LP == false) && (CDTXMania.DTX.bチップがある.LBD == false))
                {
                    //レーンタイプでの入れ替わりあり
                    if (CDTXMania.ConfigIni.eLaneType.Drums == EType.A ||
                        CDTXMania.ConfigIni.eLaneType.Drums == EType.C)
                    {
                        txLaneCover.tDraw2D(CDTXMania.app.Device, 416, 0, new Rectangle(124, 0, 54, 720));
                    }
                    else if (CDTXMania.ConfigIni.eLaneType.Drums == EType.B)
                    {
                        txLaneCover.tDraw2D(CDTXMania.app.Device, 470, 0, new Rectangle(124, 0, 54, 720));
                    }
                    else if (CDTXMania.ConfigIni.eLaneType.Drums == EType.D)
                    {
                        txLaneCover.tDraw2D(CDTXMania.app.Device, 522, 0, new Rectangle(124, 0, 54, 720));
                    }
                }
                //if (CDTXMania.DTX.bチップがある.FT == false)
                {
                    txLaneCover.tDraw2D(CDTXMania.app.Device, 690, 0, new Rectangle(71, 0, 52, 720));
                }
                //if (CDTXMania.DTX.bチップがある.Ride == false)
                {
                    //RDPositionで入れ替わり
                    if (CDTXMania.ConfigIni.eRDPosition == ERDPosition.RCRD)
                    {
                        txLaneCover.tDraw2D(CDTXMania.app.Device, 815, 0, new Rectangle(178, 0, 38, 720));
                    }
                    else if (CDTXMania.ConfigIni.eRDPosition == ERDPosition.RDRC)
                    {
                        txLaneCover.tDraw2D(CDTXMania.app.Device, 743, 0, new Rectangle(178, 0, 38, 720));
                    }
                }
            }
        }

        double db倍率 = 7.2;
        double dbシャッターIN = (nShutterInPosY.Drums * db倍率);
        double dbシャッターOUT = 720 - (nShutterOutPosY.Drums * db倍率);

        if (CDTXMania.ConfigIni.bReverse.Drums)
        {
            dbシャッターIN = (nShutterOutPosY.Drums * db倍率);
            txシャッター.tDraw2D(CDTXMania.app.Device, 295, (int)(-720 + dbシャッターIN));

            if (CDTXMania.ConfigIni.bShowPerformanceInformation)
                actLVFont.tDrawString(564, (int)dbシャッターIN - 20, CDTXMania.ConfigIni.nShutterOutSide.Drums.ToString());

            dbシャッターOUT = 720 - (nShutterInPosY.Drums * db倍率);
            txシャッター.tDraw2D(CDTXMania.app.Device, 295, (int)dbシャッターOUT);

            if (CDTXMania.ConfigIni.bShowPerformanceInformation)
                actLVFont.tDrawString(564, (int)dbシャッターOUT + 2, CDTXMania.ConfigIni.nShutterInSide.Drums.ToString());
        }
        else
        {
            txシャッター.tDraw2D(CDTXMania.app.Device, 295, (int)(-720 + dbシャッターIN));

            if (CDTXMania.ConfigIni.bShowPerformanceInformation)
                actLVFont.tDrawString(564, (int)dbシャッターIN - 20, CDTXMania.ConfigIni.nShutterInSide.Drums.ToString());

            txシャッター.tDraw2D(CDTXMania.app.Device, 295, (int)dbシャッターOUT);

            if (CDTXMania.ConfigIni.bShowPerformanceInformation)
                actLVFont.tDrawString(564, (int)dbシャッターOUT + 2, CDTXMania.ConfigIni.nShutterOutSide.Drums.ToString());
        }

        #endregion

        tUpdateAndDraw_JudgementLine();
        tUpdateAndDraw_DrumPad();
        bIsFinishedFadeout = tUpdateAndDraw_FadeIn_Out();
        if (bIsFinishedPlaying && (ePhaseID == EPhase.Common_DefaultState))
        {
            if (CDTXMania.DTXVmode.Enabled)
            {
                if (CDTXMania.Timer.b停止していない)
                {
                    CDTXMania.Timer.tPause();
                }

                Thread.Sleep(5);
                // Keep waiting for next message from DTX Creator
            }
            else if ((actGauge.IsFailed(EInstrumentPart.DRUMS)) && (ePhaseID == EPhase.Common_DefaultState))
            {
                actStageFailed.Start();
                CDTXMania.DTX.tStopPlayingAllChips();
                ePhaseID = EPhase.PERFORMANCE_STAGE_FAILED;
            }
            else
            {
                eReturnValueAfterFadeOut = EPerfScreenReturnValue.StageClear;
                ePhaseID = EPhase.PERFORMANCE_STAGE_CLEAR_FadeOut;
                if (nHitCount_ExclAuto.Drums.Miss + nHitCount_ExclAuto.Drums.Poor == 0)
                {
                    nNumberPerfects = CDTXMania.ConfigIni.bAllDrumsAreAutoPlay
                        ? nNumberPerfects = nHitCount_IncAuto.Drums.Perfect
                        : nHitCount_ExclAuto.Drums.Perfect;
                    if (nNumberPerfects == CDTXMania.DTX.nVisibleChipsCount.Drums)

                        #region[ エクセ ]

                    {
                    }

                    #endregion

                    else

                        #region[ フルコン ]

                    {
                    }

                    #endregion
                }
                else
                {

                }

                actFOStageClear.tStartFadeOut();
            }
        }

        if (CDTXMania.ConfigIni.bShowScore)
            tUpdateAndDraw_Score();
//              if( CDTXMania.ConfigIni.bShowMusicInfo )
//                  this.t進行描画_パネル文字列();
        if (CDTXMania.ConfigIni.nInfoType == 1)
            tUpdateAndDraw_StatusPanel();
        //this.actProgressBar.OnUpdateAndDraw();
        tUpdateAndDraw_Gauge();
        tUpdateAndDraw_Combo();
        tUpdateAndDraw_Graph();
        tUpdateAndDraw_PerformanceInformation();
        tUpdateAndDraw_JudgementString1_ForNormalPosition();
        tUpdateAndDraw_JudgementString2_ForPositionOnJudgementLine();
        tUpdateAndDraw_ChipFireD();
        tUpdateAndDraw_PlaySpeed();
        //

        tUpdateAndDraw_STAGEFAILED();
        bすべてのチップが判定された = true;
        if (bIsFinishedFadeout)
        {
            if (!CDTXMania.Skin.soundStageClear.b再生中 && !CDTXMania.Skin.soundSTAGEFAILED音.b再生中)
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

                        if (nNumberPerfects == CDTXMania.DTX.nVisibleChipsCount.Drums)

                            #region[ エクセ ]

                        {
                            bExc = true;
                            if (CDTXMania.ConfigIni.nSkillMode == 1)
                                actScore.nCurrentTrueScore.Drums += 30000;
                            break;
                        }

                        #endregion

                        else

                            #region[ フルコン ]

                        {
                            bFullCom = true;
                            if (CDTXMania.ConfigIni.nSkillMode == 1)
                                actScore.nCurrentTrueScore.Drums += 15000;
                            break;
                        }

                        #endregion
                    }
                    default:
                    {
                        break;
                    }
                }

                return (int)eReturnValueAfterFadeOut;
            }
        }

        if (ePhaseID == EPhase.PERFORMANCE_STAGE_RESTART)
        {
            Debug.WriteLine("Restarting");
            return (int)eReturnValueAfterFadeOut;
        }

        // もしサウンドの登録/削除が必要なら、実行する
        if (queueMixerSound.Count > 0)
        {
            //Debug.WriteLine( "☆queueLength=" + queueMixerSound.Count );
            DateTime dtnow = DateTime.Now;
            TimeSpan ts = dtnow - dtLastQueueOperation;
            if (ts.Milliseconds > 7)
            {
                for (int i = 0; i < 2 && queueMixerSound.Count > 0; i++)
                {
                    dtLastQueueOperation = dtnow;
                    stmixer stm = queueMixerSound.Dequeue();
                    if (stm.bIsAdd)
                    {
                        CDTXMania.SoundManager.AddMixer(stm.csound);
                    }
                    else
                    {
                        CDTXMania.SoundManager.RemoveMixer(stm.csound);
                    }
                }
            }
        }

        if (LoopEndMs != -1 && CSoundManager.rcPerformanceTimer.nCurrentTime > LoopEndMs)
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

            //
            nTimingHitCount.Drums.nLate = 0;
            nTimingHitCount.Drums.nEarly = 0;
        }

        // キー入力
        tHandleKeyInput();
        sw.Stop();

        return 0;
    }




    // Other

    #region [ private ]
    //-----------------
    public bool bIsFinishedFadeout;
    public bool bIsFinishedPlaying;
    public bool bExc;
    public bool bFullCom;
    public bool bすべてのチップが判定された;
    public int nNumberOfMistakes;
    public int nNumberPerfects;
    private CActPerfDrumsChipFireD actChipFireD;
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
//		private CTexture txヒットバーGB;
//		private CTexture txレーンフレームGB;
    public CTexture txシャッター;
    private CTexture txLaneCover;
    //-----------------

    private void tFadeOut()
    {
        eReturnValueAfterFadeOut = EPerfScreenReturnValue.StageClear;
        ePhaseID = EPhase.PERFORMANCE_STAGE_CLEAR_FadeOut;

        actFOStageClear.tStartFadeOut();
    }

    private bool bフィルイン区間の最後のChipである( CChip pChip )
    {
        if( pChip == null )
        {
            return false;
        }
        int num = pChip.nPlaybackPosition;
        for (int i = listChip.IndexOf(pChip) + 1; i < listChip.Count; i++)
        {
            pChip = listChip[i];
            if( ( pChip.nChannelNumber == EChannel.FillIn) && ( pChip.nIntegerValue == 2 ) )
            {
                return true;
            }
            if( ( ( pChip.nChannelNumber >= EChannel.HiHatClose) && ( pChip.nChannelNumber <= EChannel.LeftBassDrum) ) && ( ( pChip.nPlaybackPosition - num ) > 0x18 ) )
            {
                return false;
            }
        }
        return true;
    }

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
        if( CDTXMania.listTargetGhsotLag.Drums != null &&
            CDTXMania.ConfigIni.eTargetGhost.Drums == ETargetGhostData.ONLINE &&
            CDTXMania.DTX.nVisibleChipsCount.Drums > 0 )
        {
            // Online Stats の計算式
            actGraph.dbグラフ値現在_渡 = 100 *
                ( nHitCount_ExclAuto.Drums.Perfect * 17 +
                  nHitCount_ExclAuto.Drums.Great * 7 +
                  actCombo.nCurrentCombo.HighestValue.Drums * 3 ) / ( 20.0 * CDTXMania.DTX.nVisibleChipsCount.Drums );
        }

        actStatusPanel.db現在の達成率.Drums = actGraph.dbグラフ値現在_渡;
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
        actJudgeString.iP_A = (CDTXMania.ConfigIni.bReverse.Drums ? nJudgeLinePosY.Drums : nJudgeLinePosY.Drums - 189);
        actJudgeString.iP_B = (CDTXMania.ConfigIni.bReverse.Drums ? nJudgeLinePosY.Drums : nJudgeLinePosY.Drums + 23);
        actChipFireD.iPosY = (CDTXMania.ConfigIni.bReverse.Drums ? nJudgeLinePosY.Drums - 183 : nJudgeLinePosY.Drums - 186);
        CDTXMania.stagePerfDrumsScreen.actPlayInfo.jl = (CDTXMania.ConfigIni.bReverse.Drums ? 0 : nJudgeLineMaxPosY - nJudgeLinePosY.Drums);
    }

    private bool tProcessDrumHit( long nHitTime, EPad type, CChip pChip, int n強弱度合い0to127)  // tドラムヒット処理
    {
        if( pChip == null )
        {
            return false;
        }
        EChannel index = pChip.nChannelNumber;
        if ( ( index >= EChannel.HiHatClose) && ( index <= EChannel.LeftBassDrum) )
        {
            index -= 0x11;
        }
        else if ( ( index >= EChannel.HiHatClose_Hidden) && ( index <= EChannel.LeftBassDrum_Hidden) )
        {
            index -= 0x31;
        }
        int nLane = nチャンネル0Atoレーン07[ (int)index ];
        int nPad = nチャンネル0Atoパッド08[(int)index ];
        bool bPChipIsAutoPlay = bIsAutoPlay[ nLane ];
        int nInputAdjustTime = bPChipIsAutoPlay ? 0 : nInputAdjustTimeMs.Drums;
        EJudgement e判定 = e指定時刻からChipのJUDGEを返す( nHitTime, pChip, nInputAdjustTime );
        if( e判定 == EJudgement.Miss )
        {
            return false;
        }
        tProcessChipHit( nHitTime, pChip );
        actLaneFlushD.Start( (ELane) nLane, ( (float) n強弱度合い0to127 ) / 127f );
        actPad.Hit( nPad );
        if( ( e判定 != EJudgement.Poor ) && ( e判定 != EJudgement.Miss ) )
        {
            bool flag = bInFillIn;
            bool flag2 = bInFillIn && bフィルイン区間の最後のChipである( pChip );
            actChipFireD.Start( (ELane)nLane, flag, flag2, flag2, nJudgeLinePosY_delta.Drums );
            // #31602 2013.6.24 yyagi 判定ラインの表示位置をずらしたら、チップのヒットエフェクトの表示もずらすために、nJudgeLine..を追加
        }
        if( CDTXMania.ConfigIni.bドラム打音を発声する )
        {
            CChip rChip = null;
            bool bIsChipsoundPriorToPad = true;
            if( ( ( type == EPad.HH ) || ( type == EPad.HHO ) ) || ( type == EPad.LC ) )
            {
                bIsChipsoundPriorToPad = CDTXMania.ConfigIni.eHitSoundPriorityHH == EPlaybackPriority.ChipOverPadPriority;
            }
            else if( ( type == EPad.LT ) || ( type == EPad.FT ) )
            {
                bIsChipsoundPriorToPad = CDTXMania.ConfigIni.eHitSoundPriorityFT == EPlaybackPriority.ChipOverPadPriority;
            }
            else if( ( type == EPad.CY ) || ( type == EPad.RD ) )
            {
                bIsChipsoundPriorToPad = CDTXMania.ConfigIni.eHitSoundPriorityCY == EPlaybackPriority.ChipOverPadPriority;
            }
            else if (((type == EPad.LP) || (type == EPad.LBD)) || (type == EPad.BD))
            {
                bIsChipsoundPriorToPad = CDTXMania.ConfigIni.eHitSoundPriorityLP == EPlaybackPriority.ChipOverPadPriority;
            }

            if( bIsChipsoundPriorToPad )
            {
                rChip = pChip;
            }
            else
            {
                EPad hH = type;
                if( !CDTXMania.DTX.bHasChips.HHOpen && ( type == EPad.HHO ) )
                {
                    hH = EPad.HH;
                }
                if( !CDTXMania.DTX.bHasChips.Ride && ( type == EPad.RD ) )
                {
                    hH = EPad.CY;
                }
                if( !CDTXMania.DTX.bHasChips.LeftCymbal && ( type == EPad.LC ) )
                {
                    hH = EPad.HH;
                }
                rChip = r指定時刻に一番近いChip_ヒット未済問わず不可視考慮( nHitTime, nパッド0Atoチャンネル0A[ (int) hH ], nInputAdjustTime );
                if( rChip == null )
                {
                    rChip = pChip;
                }
            }
            tPlaySound( rChip, CSoundManager.rcPerformanceTimer.nシステム時刻, EInstrumentPart.DRUMS, CDTXMania.ConfigIni.n手動再生音量, CDTXMania.ConfigIni.b演奏音を強調する.Drums );
        }
        return true;
    }

    protected override void ScrollSpeedUp()
    {
        CDTXMania.ConfigIni.nScrollSpeed.Drums = Math.Min( CDTXMania.ConfigIni.nScrollSpeed.Drums + 1, 1999 );
    }
    protected override void ScrollSpeedDown()
    {
        CDTXMania.ConfigIni.nScrollSpeed.Drums = Math.Max( CDTXMania.ConfigIni.nScrollSpeed.Drums - 1, 0 );
    }
	
    /*
    protected override void tUpdateAndDraw_AVI()
    {
        base.tUpdateAndDraw_AVI( 0, 0 );
    }
    protected override void t進行描画_BGA()
    {
        base.t進行描画_BGA( 990, 0 );
    }
     */
    protected override void tUpdateAndDraw_DANGER()
    {
        actDANGER.tUpdateAndDraw( actGauge.IsDanger(EInstrumentPart.DRUMS), false, false );
    }

    protected override void tUpdateAndDraw_WailingFrame()
    {
        base.tUpdateAndDraw_WailingFrame( 587, 478,
            CDTXMania.ConfigIni.bReverse.Guitar ? ( 400 - txWailingFrame.szImageSize.Height ) : 69,
            CDTXMania.ConfigIni.bReverse.Bass ? ( 400 - txWailingFrame.szImageSize.Height ) : 69
        );
    }

    /*
    private void t進行描画_ギターベースフレーム()
    {
        if( ( ( CDTXMania.ConfigIni.eDark != EDarkMode.HALF ) && ( CDTXMania.ConfigIni.eDark != EDarkMode.FULL ) ) && CDTXMania.ConfigIni.bGuitarEnabled )
        {
            if( CDTXMania.DTX.bチップがある.Guitar )
            {
                for( int i = 0; i < 355; i += 0x80 )
                {
                    Rectangle rectangle = new Rectangle( 0, 0, 0x6d, 0x80 );
                    if( ( i + 0x80 ) > 355 )
                    {
                        rectangle.Height -= ( i + 0x80 ) - 355;
                    }
                    if( this.txレーンフレームGB != null )
                    {
                        this.txレーンフレームGB.tDraw2D( CDTXMania.app.Device, 0x1fb, 0x39 + i, rectangle );
                    }
                }
            }
            if( CDTXMania.DTX.bチップがある.Bass )
            {
                for( int j = 0; j < 355; j += 0x80 )
                {
                    Rectangle rectangle2 = new Rectangle( 0, 0, 0x6d, 0x80 );
                    if( ( j + 0x80 ) > 355 )
                    {
                        rectangle2.Height -= ( j + 0x80 ) - 355;
                    }
                    if( this.txレーンフレームGB != null )
                    {
                        this.txレーンフレームGB.tDraw2D( CDTXMania.app.Device, 0x18e, 0x39 + j, rectangle2 );
                    }
                }
            }
        }
    }
    private void t進行描画_ギターベース判定ライン()		// yyagi: ギタレボモードとは座標が違うだけですが、まとめづらかったのでそのまま放置してます。
    {
        if ( ( CDTXMania.ConfigIni.eDark != EDarkMode.FULL ) && CDTXMania.ConfigIni.bGuitarEnabled )
        {
            if ( CDTXMania.DTX.bチップがある.Guitar )
            {
                int y = ( CDTXMania.ConfigIni.bReverse.Guitar ? 374 + nJudgeLinePosY_delta.Guitar : 95 - nJudgeLinePosY_delta.Guitar ) - 3;
                    // #31602 2013.6.23 yyagi 描画遅延対策として、判定ラインの表示位置をオフセット調整できるようにする
                if ( this.txヒットバーGB != null )
                {
                    for ( int i = 0; i < 3; i++ )
                    {
                        this.txヒットバーGB.tDraw2D( CDTXMania.app.Device, 509 + ( 26 * i ), y );
                        this.txヒットバーGB.tDraw2D( CDTXMania.app.Device, ( 509 + ( 26 * i ) ) + 16, y, new Rectangle( 0, 0, 10, 16 ) );
                    }
                }
            }
            if ( CDTXMania.DTX.bチップがある.Bass )
            {
                int y = ( CDTXMania.ConfigIni.bReverse.Bass ? 374 + nJudgeLinePosY_delta.Bass : 95 - nJudgeLinePosY_delta.Bass ) - 3;
                // #31602 2013.6.23 yyagi 描画遅延対策として、判定ラインの表示位置をオフセット調整できるようにする
                if ( this.txヒットバーGB != null )
                {
                    for ( int j = 0; j < 3; j++ )
                    {
                        this.txヒットバーGB.tDraw2D( CDTXMania.app.Device, 400 + ( 26 * j ), y );
                        this.txヒットバーGB.tDraw2D( CDTXMania.app.Device, ( 400 + ( 26 * j ) ) + 16, y, new Rectangle( 0, 0, 10, 16 ) );
                    }
                }
            }
        }
    }
     */

    private void tUpdateAndDraw_Graph()  // t進行描画_グラフ
    {
        if( CDTXMania.ConfigIni.bGraph有効.Drums )
        {
            actGraph.OnUpdateAndDraw();
        }
    }

    private void tUpdateAndDraw_ChipFireD()  // t進行描画_チップファイアD
    {
        actChipFireD.OnUpdateAndDraw();
    }
    private void tUpdateAndDraw_DrumPad()  // t進行描画_ドラムパッド
    {
        actPad.OnUpdateAndDraw();
    }

    /*
    protected override void t進行描画_パネル文字列()
    {
        base.t進行描画_パネル文字列(912, 640);
    }
     */

    protected override void tUpdateAndDraw_PerformanceInformation()
    {
        base.tUpdateAndDraw_PerformanceInformation( 1000, 257 );
    }

    private void tUpdateAndDraw_PlaySpeed()
    {
        if (txPlaySpeed != null)
        {
            txPlaySpeed.tDraw2D(CDTXMania.app.Device, 25, 200);
        }
    }

    protected override void tHandleInput_Drums()
    {

        for (int nPad = 0; nPad < (int)EPad.MAX; nPad++)
        {
            List<STInputEvent> listInputEvent = CDTXMania.Pad.GetEvents(EInstrumentPart.DRUMS, (EPad)nPad);

            if ((listInputEvent == null) || (listInputEvent.Count == 0))
                continue;

            tSaveInputMethod(EInstrumentPart.DRUMS);

            #region [ 打ち分けグループ調整 ]
            //-----------------------------
            EHHGroup eHHGroup = CDTXMania.ConfigIni.eHHGroup;
            EFTGroup eFTGroup = CDTXMania.ConfigIni.eFTGroup;
            ECYGroup eCYGroup = CDTXMania.ConfigIni.eCYGroup;
            EBDGroup eBDGroup = CDTXMania.ConfigIni.eBDGroup;

            if (!CDTXMania.DTX.bHasChips.Ride && (eCYGroup == ECYGroup.打ち分ける))
            {
                eCYGroup = ECYGroup.共通;
            }
            if (!CDTXMania.DTX.bHasChips.HHOpen && (eHHGroup == EHHGroup.全部打ち分ける))
            {
                eHHGroup = EHHGroup.左シンバルのみ打ち分ける;
            }
            if (!CDTXMania.DTX.bHasChips.HHOpen && (eHHGroup == EHHGroup.ハイハットのみ打ち分ける))
            {
                eHHGroup = EHHGroup.全部共通;
            }
            if (!CDTXMania.DTX.bHasChips.LeftCymbal && (eHHGroup == EHHGroup.全部打ち分ける))
            {
                eHHGroup = EHHGroup.ハイハットのみ打ち分ける;
            }
            if (!CDTXMania.DTX.bHasChips.LeftCymbal && (eHHGroup == EHHGroup.左シンバルのみ打ち分ける))
            {
                eHHGroup = EHHGroup.全部共通;
            }
            //-----------------------------
            #endregion

            foreach (STInputEvent inputEvent in listInputEvent)
            {

                if (!inputEvent.b押された)
                    continue;

                long nTime = inputEvent.nTimeStamp - CSoundManager.rcPerformanceTimer.n前回リセットした時のシステム時刻;
                int nInputAdjustTime = bIsAutoPlay[nチャンネル0Atoレーン07[nPad]] ? 0 : nInputAdjustTimeMs.Drums;
                int nPedalLagTime = CDTXMania.ConfigIni.nPedalLagTime;

                bool bHitted = false;

                #region [ (A) ヒットしていればヒット処理して次の inputEvent へ ]
                //-----------------------------
                switch (((EPad)nPad))
                {
                    case EPad.HH:
                        #region [ HHとLC(groupingしている場合) のヒット処理 ]
                        //-----------------------------
                    {
                        if (inputEvent.nVelocity <= CDTXMania.ConfigIni.nVelocityMin.HH)
                            continue;	// 電子ドラムによる意図的なクロストークを無効にする

                        CChip chipHC = r指定時刻に一番近い未ヒットChip(nTime, 0x11, nInputAdjustTime);	// HiHat Close
                        CChip chipHO = r指定時刻に一番近い未ヒットChip(nTime, 0x18, nInputAdjustTime);	// HiHat Open
                        CChip chipLC = r指定時刻に一番近い未ヒットChip(nTime, 0x1a, nInputAdjustTime);	// LC
                        EJudgement e判定HC = (chipHC != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipHC, nInputAdjustTime) : EJudgement.Miss;
                        EJudgement e判定HO = (chipHO != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipHO, nInputAdjustTime) : EJudgement.Miss;
                        EJudgement e判定LC = (chipLC != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipLC, nInputAdjustTime) : EJudgement.Miss;
                        switch (eHHGroup)
                        {
                            case EHHGroup.ハイハットのみ打ち分ける:
                                #region [ HCとLCのヒット処理 ]
                                //-----------------------------
                                if ((e判定HC != EJudgement.Miss) && (e判定LC != EJudgement.Miss))
                                {
                                    if (chipHC.nPlaybackPosition < chipLC.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipHC, inputEvent.nVelocity);
                                    }
                                    else if (chipHC.nPlaybackPosition > chipLC.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipLC, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipHC, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.HH, chipLC, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if (e判定HC != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HH, chipHC, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定LC != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HH, chipLC, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (!bHitted)
                                    break;
                                continue;
                            //-----------------------------
                            #endregion

                            case EHHGroup.左シンバルのみ打ち分ける:
                                #region [ HCとHOのヒット処理 ]
                                //-----------------------------
                                if ((e判定HC != EJudgement.Miss) && (e判定HO != EJudgement.Miss))
                                {
                                    if (chipHC.nPlaybackPosition < chipHO.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipHC, inputEvent.nVelocity);
                                    }
                                    else if (chipHC.nPlaybackPosition > chipHO.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipHO, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipHC, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.HH, chipHO, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if (e判定HC != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HH, chipHC, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定HO != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HH, chipHO, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (!bHitted)
                                    break;
                                continue;
                            //-----------------------------
                            #endregion

                            case EHHGroup.全部共通:
                                #region [ HC,HO,LCのヒット処理 ]
                                //-----------------------------
                                if (((e判定HC != EJudgement.Miss) && (e判定HO != EJudgement.Miss)) && (e判定LC != EJudgement.Miss))
                                {
                                    CChip chip;
                                    CChip[] chipArray = new CChip[] { chipHC, chipHO, chipLC };
                                    // ここから、chipArrayをn発生位置の小さい順に並び替える
                                    if (chipArray[1].nPlaybackPosition > chipArray[2].nPlaybackPosition)
                                    {
                                        chip = chipArray[1];
                                        chipArray[1] = chipArray[2];
                                        chipArray[2] = chip;
                                    }
                                    if (chipArray[0].nPlaybackPosition > chipArray[1].nPlaybackPosition)
                                    {
                                        chip = chipArray[0];
                                        chipArray[0] = chipArray[1];
                                        chipArray[1] = chip;
                                    }
                                    if (chipArray[1].nPlaybackPosition > chipArray[2].nPlaybackPosition)
                                    {
                                        chip = chipArray[1];
                                        chipArray[1] = chipArray[2];
                                        chipArray[2] = chip;
                                    }
                                    tProcessDrumHit(nTime, EPad.HH, chipArray[0], inputEvent.nVelocity);
                                    if (chipArray[0].nPlaybackPosition == chipArray[1].nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipArray[1], inputEvent.nVelocity);
                                    }
                                    if (chipArray[0].nPlaybackPosition == chipArray[2].nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipArray[2], inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if ((e判定HC != EJudgement.Miss) && (e判定HO != EJudgement.Miss))
                                {
                                    if (chipHC.nPlaybackPosition < chipHO.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipHC, inputEvent.nVelocity);
                                    }
                                    else if (chipHC.nPlaybackPosition > chipHO.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipHO, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipHC, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.HH, chipHO, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if ((e判定HC != EJudgement.Miss) && (e判定LC != EJudgement.Miss))
                                {
                                    if (chipHC.nPlaybackPosition < chipLC.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipHC, inputEvent.nVelocity);
                                    }
                                    else if (chipHC.nPlaybackPosition > chipLC.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipLC, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipHC, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.HH, chipLC, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if ((e判定HO != EJudgement.Miss) && (e判定LC != EJudgement.Miss))
                                {
                                    if (chipHO.nPlaybackPosition < chipLC.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipHO, inputEvent.nVelocity);
                                    }
                                    else if (chipHO.nPlaybackPosition > chipLC.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipLC, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipHO, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.HH, chipLC, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if (e判定HC != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HH, chipHC, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定HO != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HH, chipHO, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定LC != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HH, chipLC, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (!bHitted)
                                    break;
                                continue;
                            //-----------------------------
                            #endregion

                            default:
                                #region [ 全部打ち分け時のヒット処理 ]
                                //-----------------------------
                                if (e判定HC != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HH, chipHC, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (!bHitted)
                                    break;
                                continue;
                            //-----------------------------
                            #endregion
                        }
                        if (!bHitted)
                            break;
                        continue;
                    }
                    //-----------------------------
                    #endregion

                    case EPad.SD:
                        #region [ SDのヒット処理 ]
                        //-----------------------------
                        if (inputEvent.nVelocity <= CDTXMania.ConfigIni.nVelocityMin.SD)	// #23857 2010.12.12 yyagi: to support VelocityMin
                            continue;	// 電子ドラムによる意図的なクロストークを無効にする
                        if (!tProcessDrumHit(nTime, EPad.SD, r指定時刻に一番近い未ヒットChip(nTime, 0x12, nInputAdjustTime), inputEvent.nVelocity))
                            break;
                        continue;
                    //-----------------------------
                    #endregion

                    case EPad.BD:
                        #region [ BDとLPとLBD(ペアリングしている場合)のヒット処理 ]
                        //-----------------------------
                    {
                        if (inputEvent.nVelocity <= CDTXMania.ConfigIni.nVelocityMin.BD)	// #23857 2010.12.12 yyagi: to support VelocityMin
                            continue;	// 電子ドラムによる意図的なクロストークを無効にする

                        CChip chipBD  = r指定時刻に一番近い未ヒットChip(nTime, 0x13, nInputAdjustTime + nPedalLagTime);	// BD
                        CChip chipLP  = r指定時刻に一番近い未ヒットChip(nTime, 0x1b, nInputAdjustTime + nPedalLagTime);	// LP
                        CChip chipLBD = r指定時刻に一番近い未ヒットChip(nTime, 0x1c, nInputAdjustTime + nPedalLagTime);	// LBD
                        EJudgement e判定BD  = (chipBD  != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipBD, nInputAdjustTime + nPedalLagTime) : EJudgement.Miss;
                        EJudgement e判定LP  = (chipLP  != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipLP, nInputAdjustTime + nPedalLagTime) : EJudgement.Miss;
                        EJudgement e判定LBD = (chipLBD != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipLBD, nInputAdjustTime + nPedalLagTime) : EJudgement.Miss;
                        switch (eBDGroup)
                        {
                            case EBDGroup.BDとLPで打ち分ける:
                                #region[ BD & LBD | LP ]
                                if( e判定BD != EJudgement.Miss && e判定LBD != EJudgement.Miss )
                                {
                                    if( chipBD.nPlaybackPosition < chipLBD.nPlaybackPosition )
                                    {
                                        tProcessDrumHit( nTime, EPad.BD, chipBD, inputEvent.nVelocity );
                                    }
                                    else if( chipBD.nPlaybackPosition > chipLBD.nPlaybackPosition )
                                    {
                                        tProcessDrumHit( nTime, EPad.BD, chipLBD, inputEvent.nVelocity );
                                    }
                                    else
                                    {
                                        tProcessDrumHit( nTime, EPad.BD, chipBD, inputEvent.nVelocity);
                                        tProcessDrumHit( nTime, EPad.BD, chipLBD, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if( e判定BD != EJudgement.Miss)
                                {
                                    tProcessDrumHit( nTime, EPad.BD, chipBD, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if( e判定LBD != EJudgement.Miss)
                                {
                                    tProcessDrumHit( nTime, EPad.BD, chipLBD, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if( bHitted )
                                    continue;
                                else
                                    break;
                            #endregion

                            case EBDGroup.左右ペダルのみ打ち分ける:
                                #region[ BDのヒット処理]
                                if (e判定BD != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.BD, chipBD, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (bHitted)
                                {
                                    continue;
                                }
                                break;
                            #endregion

                            case EBDGroup.どっちもBD:
                                #region[ LP&LBD&BD ]
                                if (((e判定LP != EJudgement.Miss) && (e判定LBD != EJudgement.Miss)) && (e判定BD != EJudgement.Miss))
                                {
                                    CChip chip8;
                                    CChip[] chipArray2 = new CChip[] { chipLP, chipLBD, chipBD };
                                    if (chipArray2[1].nPlaybackPosition > chipArray2[2].nPlaybackPosition)
                                    {
                                        chip8 = chipArray2[1];
                                        chipArray2[1] = chipArray2[2];
                                        chipArray2[2] = chip8;
                                    }
                                    if (chipArray2[0].nPlaybackPosition > chipArray2[1].nPlaybackPosition)
                                    {
                                        chip8 = chipArray2[0];
                                        chipArray2[0] = chipArray2[1];
                                        chipArray2[1] = chip8;
                                    }
                                    if (chipArray2[1].nPlaybackPosition > chipArray2[2].nPlaybackPosition)
                                    {
                                        chip8 = chipArray2[1];
                                        chipArray2[1] = chipArray2[2];
                                        chipArray2[2] = chip8;
                                    }
                                    tProcessDrumHit(nTime, EPad.BD, chipArray2[0], inputEvent.nVelocity);
                                    if (chipArray2[0].nPlaybackPosition == chipArray2[1].nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.BD, chipArray2[1], inputEvent.nVelocity);
                                    }
                                    if (chipArray2[0].nPlaybackPosition == chipArray2[2].nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.BD, chipArray2[2], inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                //chip7 BD  chip6LBD  chip5LP
                                //判定6 BD  判定5　　 判定4
                                else if ((e判定LP != EJudgement.Miss) && (e判定LBD != EJudgement.Miss))
                                {
                                    if (chipLP.nPlaybackPosition < chipLBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.BD, chipLP, inputEvent.nVelocity);
                                    }
                                    else if (chipLP.nPlaybackPosition > chipLBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.BD, chipLBD, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.BD, chipLP, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.BD, chipLBD, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if ((e判定LP != EJudgement.Miss) && (e判定BD != EJudgement.Miss))
                                {
                                    if (chipLP.nPlaybackPosition < chipBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.BD, chipLP, inputEvent.nVelocity);
                                    }
                                    else if (chipLP.nPlaybackPosition > chipBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.BD, chipBD, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.BD, chipLP, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.BD, chipBD, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                //chip7 BD  chip6LBD  chip5LP
                                //判定6 BD  判定5　　 判定4
                                else if ((e判定LBD != EJudgement.Miss) && (e判定BD != EJudgement.Miss))
                                {
                                    if (chipLBD.nPlaybackPosition < chipBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.BD, chipLBD, inputEvent.nVelocity);
                                    }
                                    else if (chipLBD.nPlaybackPosition > chipBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.BD, chipBD, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.BD, chipLBD, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.BD, chipBD, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if (e判定LP != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.BD, chipLP, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定LBD != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.BD, chipLBD, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定BD != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.BD, chipBD, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (bHitted)
                                {
                                    continue;
                                }
                                break;
                            #endregion

                            default:
                                #region [ 全部打ち分け時のヒット処理 ]
                                //-----------------------------
                                if (e判定BD != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.BD, chipBD, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (!bHitted)
                                    break;
                                continue;
                            //-----------------------------
                            #endregion
                        }
                        if (!bHitted)
                            break;
                        continue;
                    }
                    //-----------------------------
                    #endregion

                    case EPad.HT:
                        #region [ HTのヒット処理 ]
                        //-----------------------------
                        if (inputEvent.nVelocity <= CDTXMania.ConfigIni.nVelocityMin.HT)	// #23857 2010.12.12 yyagi: to support VelocityMin
                            continue;	// 電子ドラムによる意図的なクロストークを無効にする
                        if (tProcessDrumHit(nTime, EPad.HT, r指定時刻に一番近い未ヒットChip(nTime, 20, nInputAdjustTime), inputEvent.nVelocity))
                            continue;
                        break;
                    //-----------------------------
                    #endregion

                    case EPad.LT:
                        #region [ LTとFT(groupingしている場合)のヒット処理 ]
                        //-----------------------------
                    {
                        if (inputEvent.nVelocity <= CDTXMania.ConfigIni.nVelocityMin.LT)	// #23857 2010.12.12 yyagi: to support VelocityMin
                            continue;	// 電子ドラムによる意図的なクロストークを無効にする
                        CChip chipLT = r指定時刻に一番近い未ヒットChip(nTime, 0x15, nInputAdjustTime);	// LT
                        CChip chipFT = r指定時刻に一番近い未ヒットChip(nTime, 0x17, nInputAdjustTime);	// FT
                        EJudgement e判定LT = (chipLT != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipLT, nInputAdjustTime) : EJudgement.Miss;
                        EJudgement e判定FT = (chipFT != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipFT, nInputAdjustTime) : EJudgement.Miss;
                        switch (eFTGroup)
                        {
                            case EFTGroup.打ち分ける:
                                #region [ LTのヒット処理 ]
                                //-----------------------------
                                if (e判定LT != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.LT, chipLT, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                break;
                            //-----------------------------
                            #endregion

                            case EFTGroup.共通:
                                #region [ LTとFTのヒット処理 ]
                                //-----------------------------
                                if ((e判定LT != EJudgement.Miss) && (e判定FT != EJudgement.Miss))
                                {
                                    if (chipLT.nPlaybackPosition < chipFT.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LT, chipLT, inputEvent.nVelocity);
                                    }
                                    else if (chipLT.nPlaybackPosition > chipFT.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LT, chipFT, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.LT, chipLT, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.LT, chipFT, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if (e判定LT != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.LT, chipLT, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定FT != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.LT, chipFT, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                break;
                            //-----------------------------
                            #endregion
                        }
                        if (!bHitted)
                            break;
                        continue;
                    }
                    //-----------------------------
                    #endregion

                    case EPad.FT:
                        #region [ FTとLT(groupingしている場合)のヒット処理 ]
                        //-----------------------------
                    {
                        if (inputEvent.nVelocity <= CDTXMania.ConfigIni.nVelocityMin.FT)	// #23857 2010.12.12 yyagi: to support VelocityMin
                            continue;	// 電子ドラムによる意図的なクロストークを無効にする
                        CChip chipLT = r指定時刻に一番近い未ヒットChip(nTime, 0x15, nInputAdjustTime);	// LT
                        CChip chipFT = r指定時刻に一番近い未ヒットChip(nTime, 0x17, nInputAdjustTime);	// FT
                        EJudgement e判定LT = (chipLT != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipLT, nInputAdjustTime) : EJudgement.Miss;
                        EJudgement e判定FT = (chipFT != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipFT, nInputAdjustTime) : EJudgement.Miss;
                        switch (eFTGroup)
                        {
                            case EFTGroup.打ち分ける:
                                #region [ FTのヒット処理 ]
                                //-----------------------------
                                if (e判定FT != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.FT, chipFT, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                //-----------------------------
                                #endregion
                                break;

                            case EFTGroup.共通:
                                #region [ FTとLTのヒット処理 ]
                                //-----------------------------
                                if ((e判定LT != EJudgement.Miss) && (e判定FT != EJudgement.Miss))
                                {
                                    if (chipLT.nPlaybackPosition < chipFT.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.FT, chipLT, inputEvent.nVelocity);
                                    }
                                    else if (chipLT.nPlaybackPosition > chipFT.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.FT, chipFT, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.FT, chipLT, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.FT, chipFT, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if (e判定LT != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.FT, chipLT, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定FT != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.FT, chipFT, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                //-----------------------------
                                #endregion
                                break;
                        }
                        if (!bHitted)
                            break;
                        continue;
                    }
                    //-----------------------------
                    #endregion

                    case EPad.CY:
                        #region [ CY(とLCとRD:groupingしている場合)のヒット処理 ]
                        //-----------------------------
                    {
                        if (inputEvent.nVelocity <= CDTXMania.ConfigIni.nVelocityMin.CY)	// #23857 2010.12.12 yyagi: to support VelocityMin
                            continue;	// 電子ドラムによる意図的なクロストークを無効にする
                        CChip chipCY = r指定時刻に一番近い未ヒットChip(nTime, 0x16, nInputAdjustTime);	// CY
                        CChip chipRD = r指定時刻に一番近い未ヒットChip(nTime, 0x19, nInputAdjustTime);	// RD
                        CChip chipLC = CDTXMania.ConfigIni.bシンバルフリー ? r指定時刻に一番近い未ヒットChip(nTime, 0x1a, nInputAdjustTime) : null;
                        EJudgement e判定CY = (chipCY != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipCY, nInputAdjustTime) : EJudgement.Miss;
                        EJudgement e判定RD = (chipRD != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipRD, nInputAdjustTime) : EJudgement.Miss;
                        EJudgement e判定LC = (chipLC != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipLC, nInputAdjustTime) : EJudgement.Miss;
                        CChip[] chipArray = new CChip[] { chipCY, chipRD, chipLC };
                        EJudgement[] e判定Array = new EJudgement[] { e判定CY, e判定RD, e判定LC };
                        const int NumOfChips = 3;	// chipArray.GetLength(0)

                        //num8 = 0;
                        //while( num8 < 2 )

                        // CY/RD/LC群を, n発生位置の小さい順に並べる + nullを大きい方に退かす
                        SortChipsByNTime(chipArray, e判定Array, NumOfChips);
                        //for ( int i = 0; i < NumOfChips - 1; i++ )
                        //{
                        //    //num9 = 2;
                        //    //while( num9 > num8 )
                        //    for ( int j = NumOfChips - 1; j > i; j-- )
                        //    {
                        //        if ( ( chipArray[ j - 1 ] == null ) || ( ( chipArray[ j ] != null ) && ( chipArray[ j - 1 ].nPlaybackPosition > chipArray[ j ].nPlaybackPosition ) ) )
                        //        {
                        //            // swap
                        //            CChip chipTemp = chipArray[ j - 1 ];
                        //            chipArray[ j - 1 ] = chipArray[ j ];
                        //            chipArray[ j ] = chipTemp;
                        //            EJudgement e判定Temp = e判定Array[ j - 1 ];
                        //            e判定Array[ j - 1 ] = e判定Array[ j ];
                        //            e判定Array[ j ] = e判定Temp;
                        //        }
                        //        //num9--;
                        //    }
                        //    //num8++;
                        //}
                        switch (eCYGroup)
                        {
                            case ECYGroup.打ち分ける:
                                #region [打ち分ける]
                                if (!CDTXMania.ConfigIni.bシンバルフリー)
                                {
                                            
                                    if (e判定CY != EJudgement.Miss)
                                    {
                                        tProcessDrumHit(nTime, EPad.CY, chipCY, inputEvent.nVelocity);
                                        bHitted = true;
                                    }
                                    if (!bHitted)
                                        break;
                                    continue;
                                }
                                //num10 = 0;
                                //while ( num10 < NumOfChips )
                                for (int i = 0; i < NumOfChips; i++)
                                {
                                    if ((e判定Array[i] != EJudgement.Miss) && ((chipArray[i] == chipCY) || (chipArray[i] == chipLC)))
                                    {
                                        tProcessDrumHit(nTime, EPad.CY, chipArray[i], inputEvent.nVelocity);
                                        bHitted = true;
                                        break;
                                    }
                                    //num10++;
                                }
                                if (e判定CY != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.CY, chipCY, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (!bHitted)
                                    break;
                                continue;
                            #endregion

                            case ECYGroup.共通:
                                        
                                if (!CDTXMania.ConfigIni.bシンバルフリー)
                                {
                                    //num12 = 0;
                                    //while ( num12 < NumOfChips )
                                    for (int i = 0; i < NumOfChips; i++)
                                    {
                                        if ((e判定Array[i] != EJudgement.Miss) && ((chipArray[i] == chipCY) || (chipArray[i] == chipRD)))
                                        {
                                            tProcessDrumHit(nTime, EPad.CY, chipArray[i], inputEvent.nVelocity);
                                            bHitted = true;
                                            break;
                                        }
                                        //num12++;
                                    }
                                    if (!bHitted)
                                        break;
                                    continue;
                                }
                                //num11 = 0;
                                //while ( num11 < NumOfChips )
                                for (int i = 0; i < NumOfChips; i++)
                                {
                                    if (e判定Array[i] != EJudgement.Miss)
                                    {
                                        tProcessDrumHit(nTime, EPad.CY, chipArray[i], inputEvent.nVelocity);
                                        bHitted = true;
                                        break;
                                    }
                                    //num11++;
                                }
                                if (!bHitted)
                                    break;
                                continue;
                        }
                        if (!bHitted)
                            break;
                        continue;
                    }
                    //-----------------------------
                    #endregion

                    case EPad.HHO:
                        #region [ HO(とHCとLC:groupingしている場合)のヒット処理 ]
                        //-----------------------------
                    {
                        if (inputEvent.nVelocity <= CDTXMania.ConfigIni.nVelocityMin.HH)
                            continue;	// 電子ドラムによる意図的なクロストークを無効にする

                        CChip chipHC = r指定時刻に一番近い未ヒットChip(nTime, 0x11, nInputAdjustTime);	// HC
                        CChip chipHO = r指定時刻に一番近い未ヒットChip(nTime, 0x18, nInputAdjustTime);	// HO
                        CChip chipLC = r指定時刻に一番近い未ヒットChip(nTime, 0x1a, nInputAdjustTime);	// LC
                        EJudgement e判定HC = (chipHC != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipHC, nInputAdjustTime) : EJudgement.Miss;
                        EJudgement e判定HO = (chipHO != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipHO, nInputAdjustTime) : EJudgement.Miss;
                        EJudgement e判定LC = (chipLC != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipLC, nInputAdjustTime) : EJudgement.Miss;
                        switch (eHHGroup)
                        {
                            case EHHGroup.全部打ち分ける:
                                if (e判定HO != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HHO, chipHO, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (!bHitted)
                                    break;
                                continue;

                            case EHHGroup.ハイハットのみ打ち分ける:
                                if ((e判定HO != EJudgement.Miss) && (e判定LC != EJudgement.Miss))
                                {
                                    if (chipHO.nPlaybackPosition < chipLC.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipHO, inputEvent.nVelocity);
                                    }
                                    else if (chipHO.nPlaybackPosition > chipLC.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipLC, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipHO, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.HHO, chipLC, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if (e判定HO != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HHO, chipHO, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定LC != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HHO, chipLC, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (!bHitted)
                                    break;
                                continue;

                            case EHHGroup.左シンバルのみ打ち分ける:
                                if ((e判定HC != EJudgement.Miss) && (e判定HO != EJudgement.Miss))
                                {
                                    if (chipHC.nPlaybackPosition < chipHO.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipHC, inputEvent.nVelocity);
                                    }
                                    else if (chipHC.nPlaybackPosition > chipHO.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipHO, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipHC, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.HHO, chipHO, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if (e判定HC != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HHO, chipHC, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定HO != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HHO, chipHO, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (!bHitted)
                                    break;
                                continue;

                            case EHHGroup.全部共通:
                                if (((e判定HC != EJudgement.Miss) && (e判定HO != EJudgement.Miss)) && (e判定LC != EJudgement.Miss))
                                {
                                    CChip chip;
                                    CChip[] chipArray = new CChip[] { chipHC, chipHO, chipLC };
                                    // ここから、chipArrayをn発生位置の小さい順に並び替える
                                    if (chipArray[1].nPlaybackPosition > chipArray[2].nPlaybackPosition)
                                    {
                                        chip = chipArray[1];
                                        chipArray[1] = chipArray[2];
                                        chipArray[2] = chip;
                                    }
                                    if (chipArray[0].nPlaybackPosition > chipArray[1].nPlaybackPosition)
                                    {
                                        chip = chipArray[0];
                                        chipArray[0] = chipArray[1];
                                        chipArray[1] = chip;
                                    }
                                    if (chipArray[1].nPlaybackPosition > chipArray[2].nPlaybackPosition)
                                    {
                                        chip = chipArray[1];
                                        chipArray[1] = chipArray[2];
                                        chipArray[2] = chip;
                                    }
                                    tProcessDrumHit(nTime, EPad.HHO, chipArray[0], inputEvent.nVelocity);
                                    if (chipArray[0].nPlaybackPosition == chipArray[1].nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipArray[1], inputEvent.nVelocity);
                                    }
                                    if (chipArray[0].nPlaybackPosition == chipArray[2].nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipArray[2], inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if ((e判定HC != EJudgement.Miss) && (e判定HO != EJudgement.Miss))
                                {
                                    if (chipHC.nPlaybackPosition < chipHO.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipHC, inputEvent.nVelocity);
                                    }
                                    else if (chipHC.nPlaybackPosition > chipHO.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipHO, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipHC, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.HHO, chipHO, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if ((e判定HC != EJudgement.Miss) && (e判定LC != EJudgement.Miss))
                                {
                                    if (chipHC.nPlaybackPosition < chipLC.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipHC, inputEvent.nVelocity);
                                    }
                                    else if (chipHC.nPlaybackPosition > chipLC.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipLC, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipHC, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.HHO, chipLC, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if ((e判定HO != EJudgement.Miss) && (e判定LC != EJudgement.Miss))
                                {
                                    if (chipHO.nPlaybackPosition < chipLC.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipHO, inputEvent.nVelocity);
                                    }
                                    else if (chipHO.nPlaybackPosition > chipLC.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipLC, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipHO, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.HHO, chipLC, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if (e判定HC != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HHO, chipHC, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定HO != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HHO, chipHO, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定LC != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HHO, chipLC, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (!bHitted)
                                    break;
                                continue;
                        }
                        if (!bHitted)
                            break;
                        continue;
                    }
                    //-----------------------------
                    #endregion

                    case EPad.RD:
                        #region [ RD(とCYとLC:groupingしている場合)のヒット処理 ]
                        //-----------------------------
                    {
                        if (inputEvent.nVelocity <= CDTXMania.ConfigIni.nVelocityMin.RD)	// #23857 2010.12.12 yyagi: to support VelocityMin
                            continue;	// 電子ドラムによる意図的なクロストークを無効にする
                        CChip chipCY = r指定時刻に一番近い未ヒットChip(nTime, 0x16, nInputAdjustTime);	// CY
                        CChip chipRD = r指定時刻に一番近い未ヒットChip(nTime, 0x19, nInputAdjustTime);	// RD
                        CChip chipLC = CDTXMania.ConfigIni.bシンバルフリー ? r指定時刻に一番近い未ヒットChip(nTime, 0x1a, nInputAdjustTime) : null;
                        EJudgement e判定CY = (chipCY != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipCY, nInputAdjustTime) : EJudgement.Miss;
                        EJudgement e判定RD = (chipRD != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipRD, nInputAdjustTime) : EJudgement.Miss;
                        EJudgement e判定LC = (chipLC != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipLC, nInputAdjustTime) : EJudgement.Miss;
                        CChip[] chipArray = new CChip[] { chipCY, chipRD, chipLC };
                        EJudgement[] e判定Array = new EJudgement[] { e判定CY, e判定RD, e判定LC };
                        const int NumOfChips = 3;	// chipArray.GetLength(0)
                        SortChipsByNTime(chipArray, e判定Array, NumOfChips);
                        switch (eCYGroup)
                        {
                            case ECYGroup.打ち分ける:
                                if (e判定RD != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.RD, chipRD, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                break;

                            case ECYGroup.共通:
                                if (!CDTXMania.ConfigIni.bシンバルフリー)
                                {
                                    //num16 = 0;
                                    //while( num16 < 3 )
                                    for (int i = 0; i < NumOfChips; i++)
                                    {
                                        if ((e判定Array[i] != EJudgement.Miss) && ((chipArray[i] == chipCY) || (chipArray[i] == chipRD)))
                                        {
                                            tProcessDrumHit(nTime, EPad.CY, chipArray[i], inputEvent.nVelocity);
                                            bHitted = true;
                                            break;
                                        }
                                        //num16++;
                                    }
                                    break;
                                }
                                //num15 = 0;
                                //while( num15 < 3 )
                                for (int i = 0; i < NumOfChips; i++)
                                {
                                    if (e判定Array[i] != EJudgement.Miss)
                                    {
                                        tProcessDrumHit(nTime, EPad.CY, chipArray[i], inputEvent.nVelocity);
                                        bHitted = true;
                                        break;
                                    }
                                    //num15++;
                                }
                                break;
                        }
                        if (bHitted)
                        {
                            continue;
                        }
                        break;
                    }
                    //-----------------------------
                    #endregion

                    case EPad.LC:
                        #region [ LC(とHC/HOとCYと:groupingしている場合)のヒット処理 ]
                        //-----------------------------
                    {
                        if (inputEvent.nVelocity <= CDTXMania.ConfigIni.nVelocityMin.LC)	// #23857 2010.12.12 yyagi: to support VelocityMin
                            continue;	// 電子ドラムによる意図的なクロストークを無効にする
                        CChip chipHC = r指定時刻に一番近い未ヒットChip(nTime, 0x11, nInputAdjustTime);	// HC
                        CChip chipHO = r指定時刻に一番近い未ヒットChip(nTime, 0x18, nInputAdjustTime);	// HO
                        CChip chipLC = r指定時刻に一番近い未ヒットChip(nTime, 0x1a, nInputAdjustTime);	// LC
                        CChip chipCY = CDTXMania.ConfigIni.bシンバルフリー ? r指定時刻に一番近い未ヒットChip(nTime, 0x16, nInputAdjustTime) : null;
                        CChip chipRD = CDTXMania.ConfigIni.bシンバルフリー ? r指定時刻に一番近い未ヒットChip(nTime, 0x19, nInputAdjustTime) : null;
                        EJudgement e判定HC = (chipHC != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipHC, nInputAdjustTime) : EJudgement.Miss;
                        EJudgement e判定HO = (chipHO != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipHO, nInputAdjustTime) : EJudgement.Miss;
                        EJudgement e判定LC = (chipLC != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipLC, nInputAdjustTime) : EJudgement.Miss;
                        EJudgement e判定CY = (chipCY != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipCY, nInputAdjustTime) : EJudgement.Miss;
                        EJudgement e判定RD = (chipRD != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipRD, nInputAdjustTime) : EJudgement.Miss;
                        CChip[] chipArray = new CChip[] { chipHC, chipHO, chipLC, chipCY, chipRD };
                        EJudgement[] e判定Array = new EJudgement[] { e判定HC, e判定HO, e判定LC, e判定CY, e判定RD };
                        const int NumOfChips = 5;	// chipArray.GetLength(0)
                        SortChipsByNTime(chipArray, e判定Array, NumOfChips);

                        switch (eHHGroup)
                        {
                            case EHHGroup.全部打ち分ける:
                            case EHHGroup.左シンバルのみ打ち分ける:
                                #region[左シンバルのみ打ち分ける]
                                if (!CDTXMania.ConfigIni.bシンバルフリー)
                                {
                                    if (e判定LC != EJudgement.Miss)
                                    {
                                        tProcessDrumHit(nTime, EPad.LC, chipLC, inputEvent.nVelocity);
                                        bHitted = true;
                                    }
                                    if (!bHitted)
                                        break;
                                    continue;
                                }
                                //num5 = 0;
                                //while( num5 < 5 )
                                for (int i = 0; i < NumOfChips; i++)
                                {
                                    if ((e判定Array[i] != EJudgement.Miss) && (((chipArray[i] == chipLC) || (chipArray[i] == chipCY)) || ((chipArray[i] == chipRD) && (CDTXMania.ConfigIni.eCYGroup == ECYGroup.共通))))
                                    {
                                        tProcessDrumHit(nTime, EPad.LC, chipArray[i], inputEvent.nVelocity);
                                        bHitted = true;
                                        break;
                                    }
                                    //num5++;
                                }
                                if (!bHitted)
                                    break;
                                continue;
                            #endregion
                            case EHHGroup.ハイハットのみ打ち分ける:
                            case EHHGroup.全部共通:
                                if (!CDTXMania.ConfigIni.bシンバルフリー)
                                    #region[全部共通]
                                {
                                    //num7 = 0;
                                    //while( num7 < 5 )
                                    for (int i = 0; i < NumOfChips; i++)
                                    {
                                        if ((e判定Array[i] != EJudgement.Miss) && (((chipArray[i] == chipLC) || (chipArray[i] == chipHC)) || (chipArray[i] == chipHO)))
                                        {
                                            tProcessDrumHit(nTime, EPad.LC, chipArray[i], inputEvent.nVelocity);
                                            bHitted = true;
                                            break;
                                        }
                                        //num7++;
                                    }
                                    if (!bHitted)
                                        break;
                                    continue;
                                }
                                //num6 = 0;
                                //while( num6 < 5 )
                                for (int i = 0; i < NumOfChips; i++)
                                {
                                    if ((e判定Array[i] != EJudgement.Miss) && ((chipArray[i] != chipRD) || (CDTXMania.ConfigIni.eCYGroup == ECYGroup.共通)))
                                    {
                                        tProcessDrumHit(nTime, EPad.LC, chipArray[i], inputEvent.nVelocity);
                                        bHitted = true;
                                        break;
                                    }
                                    //num6++;
                                }
                                if (!bHitted)
                                    break;
                                continue;
                            #endregion
                        }
                        if (!bHitted)
                            break;

                        break;
                    }
                    //-----------------------------
                    #endregion

                    #region [rev030追加処理]
                    case EPad.LP:
                        #region [ LPのヒット処理 ]
                        //-----------------
                    {
                        if (inputEvent.nVelocity <= CDTXMania.ConfigIni.nVelocityMin.LP)
                            continue;
                        CChip chipBD  = r指定時刻に一番近い未ヒットChip(nTime, 0x13, nInputAdjustTime + nPedalLagTime);	// BD
                        CChip chipLP  = r指定時刻に一番近い未ヒットChip(nTime, 0x1b, nInputAdjustTime + nPedalLagTime);	// LP
                        CChip chipLBD = r指定時刻に一番近い未ヒットChip(nTime, 0x1c, nInputAdjustTime + nPedalLagTime);	// LBD
                        EJudgement e判定BD  = (chipBD  != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipBD,  nInputAdjustTime + nPedalLagTime) : EJudgement.Miss;
                        EJudgement e判定LP  = (chipLP  != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipLP,  nInputAdjustTime + nPedalLagTime) : EJudgement.Miss;
                        EJudgement e判定LBD = (chipLBD != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipLBD, nInputAdjustTime + nPedalLagTime) : EJudgement.Miss;
                        switch (eBDGroup)
                        {
                            case EBDGroup.左右ペダルのみ打ち分ける:
                                #region[ LPのヒット処理]
                                if (e判定LP != EJudgement.Miss && e判定LBD != EJudgement.Miss)
                                {
                                    if (chipLP.nPlaybackPosition < chipLBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LP, chipLP, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        if (chipLP.nPlaybackPosition > chipLBD.nPlaybackPosition)
                                        {
                                            tProcessDrumHit(nTime, EPad.LP, chipLBD, inputEvent.nVelocity);
                                        }
                                        else
                                        {
                                            tProcessDrumHit(nTime, EPad.LP, chipLP, inputEvent.nVelocity);
                                            tProcessDrumHit(nTime, EPad.LP, chipLBD, inputEvent.nVelocity);
                                        }
                                    }
                                    bHitted = true;
                                }
                                else
                                {
                                    if (e判定LP != EJudgement.Miss)
                                    {
                                        tProcessDrumHit(nTime, EPad.LP, chipLP, inputEvent.nVelocity);
                                        bHitted = true;
                                    }
                                    else
                                    {
                                        if (e判定LBD != EJudgement.Miss)
                                        {
                                            tProcessDrumHit(nTime, EPad.LP, chipLBD, inputEvent.nVelocity);
                                            bHitted = true;
                                        }
                                    }
                                }
                                if (bHitted)
                                {
                                    continue;
                                }
                                break;
                            #endregion

                            case EBDGroup.どっちもBD:
                                #region[ LP&LBD&BD ]
                                if (((e判定LP != EJudgement.Miss) && (e判定LBD != EJudgement.Miss)) && (e判定BD != EJudgement.Miss))
                                {
                                    CChip chip;
                                    CChip[] chipArray = new CChip[] { chipLP, chipLBD, chipBD };
                                    if (chipArray[1].nPlaybackPosition > chipArray[2].nPlaybackPosition)
                                    {
                                        chip = chipArray[1];
                                        chipArray[1] = chipArray[2];
                                        chipArray[2] = chip;
                                    }
                                    if (chipArray[0].nPlaybackPosition > chipArray[1].nPlaybackPosition)
                                    {
                                        chip = chipArray[0];
                                        chipArray[0] = chipArray[1];
                                        chipArray[1] = chip;
                                    }
                                    if (chipArray[1].nPlaybackPosition > chipArray[2].nPlaybackPosition)
                                    {
                                        chip = chipArray[1];
                                        chipArray[1] = chipArray[2];
                                        chipArray[2] = chip;
                                    }
                                    tProcessDrumHit(nTime, EPad.LP, chipArray[0], inputEvent.nVelocity);
                                    if (chipArray[0].nPlaybackPosition == chipArray[1].nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LP, chipArray[1], inputEvent.nVelocity);
                                    }
                                    if (chipArray[0].nPlaybackPosition == chipArray[2].nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LP, chipArray[2], inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if ((e判定LP != EJudgement.Miss) && (e判定LBD != EJudgement.Miss))
                                {
                                    if (chipLP.nPlaybackPosition < chipLBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LP, chipLP, inputEvent.nVelocity);
                                    }
                                    else if (chipLP.nPlaybackPosition > chipLBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LP, chipLBD, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.LP, chipLP, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.LP, chipLBD, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if ((e判定LP != EJudgement.Miss) && (e判定BD != EJudgement.Miss))
                                {
                                    if (chipLP.nPlaybackPosition < chipBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LP, chipLP, inputEvent.nVelocity);
                                    }
                                    else if (chipLP.nPlaybackPosition > chipBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LP, chipBD, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.LP, chipLP, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.LP, chipBD, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if ((e判定LBD != EJudgement.Miss) && (e判定BD != EJudgement.Miss))
                                {
                                    if (chipLBD.nPlaybackPosition < chipBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LP, chipLBD, inputEvent.nVelocity);
                                    }
                                    else if (chipLBD.nPlaybackPosition > chipBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LP, chipBD, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.LP, chipLBD, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.LP, chipBD, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if (e判定LP != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.LP, chipLP, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定LBD != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.LP, chipLBD, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定BD != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.LP, chipBD, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (bHitted)
                                {
                                    continue;
                                }
                                #endregion
                                break;

                            case EBDGroup.BDとLPで打ち分ける:
                            default:
                                #region [ 全部打ち分け時のヒット処理 ]
                                //-----------------------------
                                if (e判定LP != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.LP, chipLP, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (!bHitted)
                                    break;
                                continue;
                            //-----------------------------
                            #endregion
                        }
                        if (!bHitted)
                            break;
                        continue;
                    }
                    //-----------------
                    #endregion

                    case EPad.LBD:
                        #region [ LBDのヒット処理 ]
                        //-----------------
                    {
                        if (inputEvent.nVelocity <= CDTXMania.ConfigIni.nVelocityMin.LBD)
                            continue;
                        CChip chipBD  = r指定時刻に一番近い未ヒットChip(nTime, 0x13, nInputAdjustTime + nPedalLagTime);	// BD
                        CChip chipLP  = r指定時刻に一番近い未ヒットChip(nTime, 0x1b, nInputAdjustTime + nPedalLagTime);	// LP
                        CChip chipLBD = r指定時刻に一番近い未ヒットChip(nTime, 0x1c, nInputAdjustTime + nPedalLagTime);	// LBD
                        EJudgement e判定BD  = (chipBD  != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipBD,  nInputAdjustTime + nPedalLagTime) : EJudgement.Miss;
                        EJudgement e判定LP  = (chipLP  != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipLP,  nInputAdjustTime + nPedalLagTime) : EJudgement.Miss;
                        EJudgement e判定LBD = (chipLBD != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipLBD, nInputAdjustTime + nPedalLagTime) : EJudgement.Miss;
                        switch (eBDGroup)
                        {
                            case EBDGroup.BDとLPで打ち分ける:
                                #region[ BD & LBD | LP ]
                                if( e判定BD != EJudgement.Miss && e判定LBD != EJudgement.Miss )
                                {
                                    if( chipBD.nPlaybackPosition < chipLBD.nPlaybackPosition )
                                    {
                                        tProcessDrumHit( nTime, EPad.LBD, chipBD, inputEvent.nVelocity );
                                    }
                                    else if( chipBD.nPlaybackPosition > chipLBD.nPlaybackPosition )
                                    {
                                        tProcessDrumHit( nTime, EPad.LBD, chipLBD, inputEvent.nVelocity );
                                    }
                                    else
                                    {
                                        tProcessDrumHit( nTime, EPad.LBD, chipBD, inputEvent.nVelocity );
                                        tProcessDrumHit( nTime, EPad.LBD, chipLBD, inputEvent.nVelocity );
                                    }
                                    bHitted = true;
                                }
                                else if( e判定BD != EJudgement.Miss )
                                {
                                    tProcessDrumHit( nTime, EPad.LBD, chipBD, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if( e判定LBD != EJudgement.Miss )
                                {
                                    tProcessDrumHit( nTime, EPad.LBD, chipLBD, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if( bHitted )
                                    continue;
                                else
                                    break;
                            #endregion

                            case EBDGroup.左右ペダルのみ打ち分ける:
                                #region[ LPのヒット処理]
                                if (e判定LP != EJudgement.Miss && e判定LBD != EJudgement.Miss)
                                {
                                    if (chipLP.nPlaybackPosition < chipLBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LBD, chipLP, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        if (chipLP.nPlaybackPosition > chipLBD.nPlaybackPosition)
                                        {
                                            tProcessDrumHit(nTime, EPad.LBD, chipLBD, inputEvent.nVelocity);
                                        }
                                        else
                                        {
                                            tProcessDrumHit(nTime, EPad.LBD, chipLP, inputEvent.nVelocity);
                                            tProcessDrumHit(nTime, EPad.LBD, chipLBD, inputEvent.nVelocity);
                                        }
                                    }
                                    bHitted = true;
                                }
                                else
                                {
                                    if (e判定LP != EJudgement.Miss)
                                    {
                                        tProcessDrumHit(nTime, EPad.LBD, chipLP, inputEvent.nVelocity);
                                        bHitted = true;
                                    }
                                    else
                                    {
                                        if (e判定LBD != EJudgement.Miss)
                                        {
                                            tProcessDrumHit(nTime, EPad.LBD, chipLBD, inputEvent.nVelocity);
                                            bHitted = true;
                                        }
                                    }
                                }
                                if (bHitted)
                                {
                                    continue;
                                }
                                break;
                            #endregion

                            case EBDGroup.どっちもBD:
                                #region[ LP&LBD&BD ]
                                if (((e判定LP != EJudgement.Miss) && (e判定LBD != EJudgement.Miss)) && (e判定BD != EJudgement.Miss))
                                {
                                    CChip chip;
                                    CChip[] chipArray = new CChip[] { chipLP, chipLBD, chipBD };
                                    if (chipArray[1].nPlaybackPosition > chipArray[2].nPlaybackPosition)
                                    {
                                        chip = chipArray[1];
                                        chipArray[1] = chipArray[2];
                                        chipArray[2] = chip;
                                    }
                                    if (chipArray[0].nPlaybackPosition > chipArray[1].nPlaybackPosition)
                                    {
                                        chip = chipArray[0];
                                        chipArray[0] = chipArray[1];
                                        chipArray[1] = chip;
                                    }
                                    if (chipArray[1].nPlaybackPosition > chipArray[2].nPlaybackPosition)
                                    {
                                        chip = chipArray[1];
                                        chipArray[1] = chipArray[2];
                                        chipArray[2] = chip;
                                    }
                                    tProcessDrumHit(nTime, EPad.LBD, chipArray[0], inputEvent.nVelocity);
                                    if (chipArray[0].nPlaybackPosition == chipArray[1].nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LBD, chipArray[1], inputEvent.nVelocity);
                                    }
                                    if (chipArray[0].nPlaybackPosition == chipArray[2].nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LBD, chipArray[2], inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if ((e判定LP != EJudgement.Miss) && (e判定LBD != EJudgement.Miss))
                                {
                                    if (chipLP.nPlaybackPosition < chipLBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LBD, chipLP, inputEvent.nVelocity);
                                    }
                                    else if (chipLP.nPlaybackPosition > chipLBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LBD, chipLBD, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.LBD, chipLP, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.LBD, chipLBD, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if ((e判定LP != EJudgement.Miss) && (e判定BD != EJudgement.Miss))
                                {
                                    if (chipLP.nPlaybackPosition < chipBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LBD, chipLP, inputEvent.nVelocity);
                                    }
                                    else if (chipLP.nPlaybackPosition > chipBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LBD, chipBD, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.LBD, chipLP, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.LBD, chipBD, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if ((e判定LBD != EJudgement.Miss) && (e判定BD != EJudgement.Miss))
                                {
                                    if (chipLBD.nPlaybackPosition < chipBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LBD, chipLBD, inputEvent.nVelocity);
                                    }
                                    else if (chipLBD.nPlaybackPosition > chipBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LBD, chipBD, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.LBD, chipLBD, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.LBD, chipBD, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if (e判定LP != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.LBD, chipLP, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定LBD != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.LBD, chipLBD, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定BD != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.LBD, chipBD, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (bHitted)
                                {
                                    continue;
                                }
                                #endregion
                                break;

                            default:
                                #region [ 全部打ち分け時のヒット処理 ]
                                //-----------------------------
                                if (e判定LBD != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.LBD, chipLBD, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (!bHitted)
                                    break;
                                continue;
                            //-----------------------------
                            #endregion
                        }
                        if (!bHitted)
                            break;
                        continue;
                    }
                    //-----------------
                    #endregion
                    #endregion

                }
                //-----------------------------
                #endregion
                #region [ (B) ヒットしてなかった場合は、レーンフラッシュ、パッドアニメ、空打ち音再生を実行 ]
                //-----------------------------
                actLaneFlushD.Start((ELane)nパッド0Atoレーン07[nPad], ((float)inputEvent.nVelocity) / 127f);
                actPad.Hit(nパッド0Atoパッド08[nPad]);

                if (CDTXMania.ConfigIni.bドラム打音を発声する)
                {
                    CChip rChip = r空うちChip(EInstrumentPart.DRUMS, (EPad)nPad);
                    if (rChip != null)
                    {
                        #region [ (B1) 空打ち音が譜面で指定されているのでそれを再生する。]
                        //-----------------
                        tPlaySound(rChip, CSoundManager.rcPerformanceTimer.nシステム時刻, EInstrumentPart.DRUMS, CDTXMania.ConfigIni.n手動再生音量, CDTXMania.ConfigIni.b演奏音を強調する.Drums);
                        //-----------------
                        #endregion
                    }
                    else
                    {
                        #region [ (B2) 空打ち音が指定されていないので一番近いチップを探して再生する。]
                        //-----------------
                        switch (((EPad)nPad))
                        {
                            case EPad.HH:
                                #region [ *** ]
                                //-----------------------------
                            {
                                CChip chipHC = r指定時刻に一番近いChip_ヒット未済問わず不可視考慮(nTime, nパッド0Atoチャンネル0A[0], nInputAdjustTime);
                                CChip chipHO = r指定時刻に一番近いChip_ヒット未済問わず不可視考慮(nTime, nパッド0Atoチャンネル0A[7], nInputAdjustTime);
                                CChip chipLC = r指定時刻に一番近いChip_ヒット未済問わず不可視考慮(nTime, nパッド0Atoチャンネル0A[9], nInputAdjustTime);
                                switch (CDTXMania.ConfigIni.eHHGroup)
                                {
                                    case EHHGroup.ハイハットのみ打ち分ける:
                                        rChip = (chipHC != null) ? chipHC : chipLC;
                                        break;

                                    case EHHGroup.左シンバルのみ打ち分ける:
                                        rChip = (chipHC != null) ? chipHC : chipHO;
                                        break;

                                    case EHHGroup.全部共通:
                                        if (chipHC != null)
                                        {
                                            rChip = chipHC;
                                        }
                                        else if (chipHO == null)
                                        {
                                            rChip = chipLC;
                                        }
                                        else if (chipLC == null)
                                        {
                                            rChip = chipHO;
                                        }
                                        else if (chipHO.nPlaybackPosition < chipLC.nPlaybackPosition)
                                        {
                                            rChip = chipHO;
                                        }
                                        else
                                        {
                                            rChip = chipLC;
                                        }
                                        break;

                                    default:
                                        rChip = chipHC;
                                        break;
                                }
                            }
                                //-----------------------------
                                #endregion
                                break;

                            case EPad.LT:
                                #region [ *** ]
                                //-----------------------------
                            {
                                CChip chipLT = r指定時刻に一番近いChip_ヒット未済問わず不可視考慮(nTime, nパッド0Atoチャンネル0A[4], nInputAdjustTime);
                                CChip chipFT = r指定時刻に一番近いChip_ヒット未済問わず不可視考慮(nTime, nパッド0Atoチャンネル0A[5], nInputAdjustTime);
                                if (CDTXMania.ConfigIni.eFTGroup != EFTGroup.打ち分ける)
                                    rChip = (chipLT != null) ? chipLT : chipFT;
                                else
                                    rChip = chipLT;
                            }
                                //-----------------------------
                                #endregion
                                break;

                            case EPad.FT:
                                #region [ *** ]
                                //-----------------------------
                            {
                                CChip chipLT = r指定時刻に一番近いChip_ヒット未済問わず不可視考慮(nTime, nパッド0Atoチャンネル0A[4], nInputAdjustTime);
                                CChip chipFT = r指定時刻に一番近いChip_ヒット未済問わず不可視考慮(nTime, nパッド0Atoチャンネル0A[5], nInputAdjustTime);
                                if (CDTXMania.ConfigIni.eFTGroup != EFTGroup.打ち分ける)
                                    rChip = (chipFT != null) ? chipFT : chipLT;
                                else
                                    rChip = chipFT;
                            }
                                //-----------------------------
                                #endregion
                                break;

                            case EPad.CY:
                                #region [ *** ]
                                //-----------------------------
                            {

                                CChip chipCY = r指定時刻に一番近いChip_ヒット未済問わず不可視考慮(nTime, nパッド0Atoチャンネル0A[6], nInputAdjustTime);
                                CChip chipRD = r指定時刻に一番近いChip_ヒット未済問わず不可視考慮(nTime, nパッド0Atoチャンネル0A[8], nInputAdjustTime);
                                if (CDTXMania.ConfigIni.eCYGroup != ECYGroup.打ち分ける)
                                    rChip = (chipCY != null) ? chipCY : chipRD;
                                else
                                    rChip = chipCY;
                            }
                                //-----------------------------
                                #endregion
                                break;

                            case EPad.HHO:
                                #region [ *** ]
                                //-----------------------------
                            {
                                CChip chipHC = r指定時刻に一番近いChip_ヒット未済問わず不可視考慮(nTime, nパッド0Atoチャンネル0A[0], nInputAdjustTime);
                                CChip chipHO = r指定時刻に一番近いChip_ヒット未済問わず不可視考慮(nTime, nパッド0Atoチャンネル0A[7], nInputAdjustTime);
                                CChip chipLC = r指定時刻に一番近いChip_ヒット未済問わず不可視考慮(nTime, nパッド0Atoチャンネル0A[9], nInputAdjustTime);
                                switch (CDTXMania.ConfigIni.eHHGroup)
                                {
                                    case EHHGroup.全部打ち分ける:
                                        rChip = chipHO;
                                        break;

                                    case EHHGroup.ハイハットのみ打ち分ける:
                                        rChip = (chipHO != null) ? chipHO : chipLC;
                                        break;

                                    case EHHGroup.左シンバルのみ打ち分ける:
                                        rChip = (chipHO != null) ? chipHO : chipHC;
                                        break;

                                    case EHHGroup.全部共通:
                                        if (chipHO != null)
                                        {
                                            rChip = chipHO;
                                        }
                                        else if (chipHC == null)
                                        {
                                            rChip = chipLC;
                                        }
                                        else if (chipLC == null)
                                        {
                                            rChip = chipHC;
                                        }
                                        else if (chipHC.nPlaybackPosition < chipLC.nPlaybackPosition)
                                        {
                                            rChip = chipHC;
                                        }
                                        else
                                        {
                                            rChip = chipLC;
                                        }
                                        break;
                                }
                            }
                                //-----------------------------
                                #endregion
                                break;

                            case EPad.RD:
                                #region [ *** ]
                                //-----------------------------
                            {
                                CChip chipCY = r指定時刻に一番近いChip_ヒット未済問わず不可視考慮(nTime, nパッド0Atoチャンネル0A[6], nInputAdjustTime);
                                CChip chipRD = r指定時刻に一番近いChip_ヒット未済問わず不可視考慮(nTime, nパッド0Atoチャンネル0A[8], nInputAdjustTime);
                                if (CDTXMania.ConfigIni.eCYGroup != ECYGroup.打ち分ける)
                                    rChip = (chipRD != null) ? chipRD : chipCY;
                                else
                                    rChip = chipRD;
                            }
                                //-----------------------------
                                #endregion
                                break;

                            case EPad.LC:
                                #region [ *** ]
                                //-----------------------------
                            {
                                CChip chipHC = r指定時刻に一番近いChip_ヒット未済問わず不可視考慮(nTime, nパッド0Atoチャンネル0A[0], nInputAdjustTime);
                                CChip chipHO = r指定時刻に一番近いChip_ヒット未済問わず不可視考慮(nTime, nパッド0Atoチャンネル0A[7], nInputAdjustTime);
                                CChip chipLC = r指定時刻に一番近いChip_ヒット未済問わず不可視考慮(nTime, nパッド0Atoチャンネル0A[9], nInputAdjustTime);
                                switch (CDTXMania.ConfigIni.eHHGroup)
                                {
                                    case EHHGroup.全部打ち分ける:
                                    case EHHGroup.左シンバルのみ打ち分ける:
                                        rChip = chipLC;
                                        break;

                                    case EHHGroup.ハイハットのみ打ち分ける:
                                    case EHHGroup.全部共通:
                                        if (chipLC != null)
                                        {
                                            rChip = chipLC;
                                        }
                                        else if (chipHC == null)
                                        {
                                            rChip = chipHO;
                                        }
                                        else if (chipHO == null)
                                        {
                                            rChip = chipHC;
                                        }
                                        else if (chipHC.nPlaybackPosition < chipHO.nPlaybackPosition)
                                        {
                                            rChip = chipHC;
                                        }
                                        else
                                        {
                                            rChip = chipHO;
                                        }
                                        break;
                                }
                            }
                                //-----------------------------
                                #endregion
                                break;

                            case EPad.BD:
                                #region [ *** ]
                                //-----------------------------
                            {
                                CChip chipBD = r指定時刻に一番近いChip_ヒット未済問わず不可視考慮(nTime, nパッド0Atoチャンネル0A[2], nInputAdjustTime + nPedalLagTime);
                                CChip chipLP = r指定時刻に一番近いChip_ヒット未済問わず不可視考慮(nTime, nパッド0Atoチャンネル0A[10], nInputAdjustTime + nPedalLagTime);
                                CChip chipLBD = r指定時刻に一番近いChip_ヒット未済問わず不可視考慮(nTime, nパッド0Atoチャンネル0A[11], nInputAdjustTime + nPedalLagTime);
                                switch (CDTXMania.ConfigIni.eBDGroup)
                                {
                                    case EBDGroup.打ち分ける:
                                        rChip = chipBD;
                                        break;

                                    case EBDGroup.左右ペダルのみ打ち分ける:
                                        rChip = (chipBD != null) ? chipBD : chipLP;
                                        break;

                                    case EBDGroup.BDとLPで打ち分ける:
                                        if( chipBD != null && chipLBD != null )
                                        {
                                            if( chipBD.nPlaybackTimeMs >= chipLBD.nPlaybackTimeMs )
                                                rChip = chipBD;
                                            else if( chipBD.nPlaybackTimeMs < chipLBD.nPlaybackTimeMs )
                                                rChip = chipLBD;
                                        }
                                        else if( chipLBD != null )
                                        {
                                            rChip = chipLBD;
                                        }
                                        else
                                        {
                                            rChip = chipBD;
                                        }
                                        break;

                                    case EBDGroup.どっちもBD:
                                        #region [ *** ]
                                        if (chipBD != null)
                                        {
                                            rChip = chipBD;
                                        }
                                        else if (chipLP == null)
                                        {
                                            rChip = chipLBD;
                                        }
                                        else if (chipLBD == null)
                                        {
                                            rChip = chipLP;
                                        }
                                        else if (chipLP.nPlaybackPosition < chipLBD.nPlaybackPosition)
                                        {
                                            rChip = chipLP;
                                        }
                                        else
                                        {
                                            rChip = chipLBD;
                                        }
                                        #endregion
                                        break;
                                }
                            }
                                //-----------------------------
                                #endregion
                                break;
                                    
                            case EPad.LP:
                                #region [ *** ]
                                //-----------------------------
                            {
                                CChip chipBD = r指定時刻に一番近いChip_ヒット未済問わず不可視考慮(nTime, nパッド0Atoチャンネル0A[2], nInputAdjustTime + nPedalLagTime );
                                CChip chipLP = r指定時刻に一番近いChip_ヒット未済問わず不可視考慮(nTime, nパッド0Atoチャンネル0A[10], nInputAdjustTime + nPedalLagTime );
                                CChip chipLBD = r指定時刻に一番近いChip_ヒット未済問わず不可視考慮(nTime, nパッド0Atoチャンネル0A[11], nInputAdjustTime + nPedalLagTime );
                                switch (CDTXMania.ConfigIni.eBDGroup)
                                {
                                    case EBDGroup.打ち分ける:
                                        rChip = chipLP;
                                        break;
                                    case EBDGroup.左右ペダルのみ打ち分ける:
                                        #region[左右ペダル]
                                        rChip = (chipLP != null) ? chipLP : chipLBD;
                                        #endregion
                                        break;
                                    case EBDGroup.BDとLPで打ち分ける:
                                        #region[ BDとLP ]
                                        if( chipLP != null ){ rChip = chipLP; }
                                        #endregion
                                        break;
                                    case EBDGroup.どっちもBD:
                                        #region[共通]
                                        if (chipLP != null)
                                        {
                                            rChip = chipLP;
                                        }
                                        else if (chipLBD == null)
                                        {
                                            rChip = chipBD;
                                        }
                                        else if (chipBD == null)
                                        {
                                            rChip = chipLBD;
                                        }
                                        else if (chipLBD.nPlaybackPosition < chipBD.nPlaybackPosition)
                                        {
                                            rChip = chipLBD;
                                        }
                                        else
                                        {
                                            rChip = chipBD;
                                        }
                                        #endregion
                                        break;

                                }
                            }
                                //-----------------------------
                                #endregion
                                break;

                            case EPad.LBD:
                                #region [ *** ]
                            {
                                CChip chipBD = r指定時刻に一番近いChip_ヒット未済問わず不可視考慮(nTime, nパッド0Atoチャンネル0A[2], nInputAdjustTime + nPedalLagTime);
                                CChip chipLP = r指定時刻に一番近いChip_ヒット未済問わず不可視考慮(nTime, nパッド0Atoチャンネル0A[10], nInputAdjustTime + nPedalLagTime);
                                CChip chipLBD = r指定時刻に一番近いChip_ヒット未済問わず不可視考慮(nTime, nパッド0Atoチャンネル0A[11], nInputAdjustTime + nPedalLagTime);
                                switch (CDTXMania.ConfigIni.eBDGroup)
                                {
                                    case EBDGroup.打ち分ける:
                                        rChip = chipLBD;
                                        break;
                                    case EBDGroup.左右ペダルのみ打ち分ける:
                                        #region [ *** ]
                                        rChip = (chipLBD != null) ? chipLBD : chipBD;
                                        #endregion
                                        break;
                                    case EBDGroup.BDとLPで打ち分ける:
                                        #region[ BDとLBD ]
                                        if( chipBD != null && chipLBD != null )
                                        {
                                            if( chipBD.nPlaybackTimeMs <= chipLBD.nPlaybackTimeMs )
                                                rChip = chipLBD;
                                            else if( chipBD.nPlaybackTimeMs > chipLBD.nPlaybackTimeMs )
                                                rChip = chipBD;
                                        }
                                        else if( chipLBD != null )
                                        {
                                            rChip = chipLBD;
                                        }
                                        else
                                        {
                                            rChip = chipBD;
                                        }
                                        #endregion
                                        break;
                                    case EBDGroup.どっちもBD:
                                        #region[ *** ]
                                        if (chipLBD != null)
                                        {
                                            rChip = chipLBD;
                                        }
                                        else if (chipLP == null)
                                        {
                                            rChip = chipBD;
                                        }
                                        else if (chipBD == null)
                                        {
                                            rChip = chipLP;
                                        }
                                        else if (chipLP.nPlaybackPosition < chipBD.nPlaybackPosition)
                                        {
                                            rChip = chipLP;
                                        }
                                        else
                                        {
                                            rChip = chipBD;
                                        }
                                        #endregion
                                        break;
                                }
                            }
                                #endregion
                                break;



                            default:
                                #region [ *** ]
                                //-----------------------------
                                rChip = r指定時刻に一番近いChip_ヒット未済問わず不可視考慮(nTime, nパッド0Atoチャンネル0A[nPad], nInputAdjustTime);
                                //-----------------------------
                                #endregion
                                break;
                        }
                        if (rChip != null)
                        {
                            // 空打ち音が見つかったので再生する。
                            tPlaySound(rChip, CSoundManager.rcPerformanceTimer.nシステム時刻, EInstrumentPart.DRUMS, CDTXMania.ConfigIni.n手動再生音量, CDTXMania.ConfigIni.b演奏音を強調する.Drums);
                        }
                        //-----------------
                        #endregion
                    }
                }

                // BAD or TIGHT 時の処理。
                if (CDTXMania.ConfigIni.bTight)
                    tチップのヒット処理_BadならびにTight時のMiss(EInstrumentPart.DRUMS, nパッド0Atoレーン07[nPad]);
                //-----------------------------
                #endregion
            }
        }
    }

    // tHandleInput_Drums()からメソッドを抽出したもの。
    /// <summary>
    /// chipArrayの中を, n発生位置の小さい順に並べる + nullを大きい方に退かす。セットでe判定Arrayも並べ直す。
    /// </summary>
    /// <param name="chipArray">ソート対象chip群</param>
    /// <param name="e判定Array">ソート対象e判定群</param>
    /// <param name="NumOfChips">チップ数</param>
    private static void SortChipsByNTime( CChip[] chipArray, EJudgement[] e判定Array, int NumOfChips )
    {
        for ( int i = 0; i < NumOfChips - 1; i++ )
        {
            //num9 = 2;
            //while( num9 > num8 )
            for ( int j = NumOfChips - 1; j > i; j-- )
            {
                if ( ( chipArray[ j - 1 ] == null ) || ( ( chipArray[ j ] != null ) && ( chipArray[ j - 1 ].nPlaybackPosition > chipArray[ j ].nPlaybackPosition ) ) )
                {
                    // swap
                    CChip chipTemp = chipArray[ j - 1 ];
                    chipArray[ j - 1 ] = chipArray[ j ];
                    chipArray[ j ] = chipTemp;
                    EJudgement e判定Temp = e判定Array[ j - 1 ];
                    e判定Array[ j - 1 ] = e判定Array[ j ];
                    e判定Array[ j ] = e判定Temp;
                }
                //num9--;
            }
            //num8++;
        }
    }

    protected override void tGenerateBackgroundTexture()
    {
        Rectangle bgrect = new Rectangle(980, 0, 0, 0);
        if (CDTXMania.ConfigIni.bBGAEnabled)
        {
            bgrect = new Rectangle(980, 0, 278, 355);
        }
        string DefaultBgFilename = @"Graphics\7_background.jpg";
        string BgFilename = "";
        if ( ( ( CDTXMania.DTX.BACKGROUND != null ) && ( CDTXMania.DTX.BACKGROUND.Length > 0 ) ) && !CDTXMania.ConfigIni.bストイックモード )
        {
            BgFilename = CDTXMania.DTX.strFolderName + CDTXMania.DTX.BACKGROUND;
        }
        base.tGenerateBackgroundTexture( DefaultBgFilename, bgrect, BgFilename );
    }

    protected override void tUpdateAndDraw_Chip_PatternOnly_Drums(CConfigIni configIni, ref CDTX dTX, ref CChip pChip)  // t進行描画_チップ_模様のみ_ドラムス
    {
        if (configIni.bDrumsEnabled)
        {
            #region [ Sudden処理 ]
            if ((CDTXMania.ConfigIni.nHidSud.Drums == 2) || (CDTXMania.ConfigIni.nHidSud.Drums == 3))
            {
                if (pChip.nDistanceFromBar.Drums < 200)
                {
                    pChip.bVisible = true;
                    pChip.nTransparency = 0xff;
                }
                else if (pChip.nDistanceFromBar.Drums < 250)
                {
                    pChip.bVisible = true;
                    pChip.nTransparency = 0xff - ((int)((((double)(pChip.nDistanceFromBar.Drums - 200)) * 255.0) / 50.0));
                }
                else
                {
                    pChip.bVisible = false;
                    pChip.nTransparency = 0;
                }
            }
            #endregion
            #region [ Hidden処理 ]
            if ((CDTXMania.ConfigIni.nHidSud.Drums == 1) || (CDTXMania.ConfigIni.nHidSud.Drums == 3))
            {
                if (pChip.nDistanceFromBar.Drums < 100)
                {
                    pChip.bVisible = false;
                }
                else if (pChip.nDistanceFromBar.Drums < 150)
                {
                    pChip.bVisible = true;
                    pChip.nTransparency = (int)((((double)(pChip.nDistanceFromBar.Drums - 100)) * 255.0) / 50.0);
                }
            }
            #endregion
            #region [ ステルス処理 ]
            if (CDTXMania.ConfigIni.nHidSud.Drums == 4)
            {
                pChip.bVisible = false;
            }
            #endregion
            if (!pChip.bHit && pChip.bVisible)
            {
                if (txChip != null)
                {
                    txChip.nTransparency = pChip.nTransparency;
                }
                int x = nチャンネルtoX座標[pChip.nChannelNumber - EChannel.HiHatClose];

                if (configIni.eLaneType.Drums == EType.A)
                {
                    if (configIni.eRDPosition == ERDPosition.RCRD)
                    {
                        x = nチャンネルtoX座標[pChip.nChannelNumber - EChannel.HiHatClose];
                    }
                    else if (configIni.eRDPosition == ERDPosition.RDRC)
                    {
                        x = nチャンネルtoX座標改[pChip.nChannelNumber - EChannel.HiHatClose];
                    }
                }
                else if (configIni.eLaneType.Drums == EType.B)
                {
                    if (configIni.eRDPosition == ERDPosition.RCRD)
                    {
                        x = nチャンネルtoX座標B[pChip.nChannelNumber - EChannel.HiHatClose];
                    }
                    else if (configIni.eRDPosition == ERDPosition.RDRC)
                    {
                        x = nチャンネルtoX座標B改[pChip.nChannelNumber - EChannel.HiHatClose];
                    }
                }
                else if (configIni.eLaneType.Drums == EType.C)
                {
                    if (configIni.eRDPosition == ERDPosition.RCRD)
                    {
                        x = nチャンネルtoX座標C[pChip.nChannelNumber - EChannel.HiHatClose];
                    }
                    else if (configIni.eRDPosition == ERDPosition.RDRC)
                    {
                        x = nチャンネルtoX座標C改[pChip.nChannelNumber - EChannel.HiHatClose];
                    }
                }
                else if (configIni.eLaneType.Drums == EType.D)
                {
                    if (configIni.eRDPosition == ERDPosition.RCRD)
                    {
                        x = nチャンネルtoX座標D[pChip.nChannelNumber - EChannel.HiHatClose];
                    }
                    else if (configIni.eRDPosition == ERDPosition.RDRC)
                    {
                        x = nチャンネルtoX座標D改[pChip.nChannelNumber - EChannel.HiHatClose];
                    }
                }

                if (configIni.eRDPosition == ERDPosition.RDRC)
                {
                    if (configIni.eLaneType.Drums == EType.A)
                    {
                        x = nチャンネルtoX座標改[pChip.nChannelNumber - EChannel.HiHatClose];
                    }
                    else if (configIni.eLaneType.Drums == EType.B)
                    {
                        x = nチャンネルtoX座標B改[pChip.nChannelNumber - EChannel.HiHatClose];
                    }
                }

                int y = configIni.bReverse.Drums ? (nJudgeLinePosY.Drums + pChip.nDistanceFromBar.Drums) : (nJudgeLinePosY.Drums - pChip.nDistanceFromBar.Drums);
                if (txChip != null)
                {
                    txChip.vcScaleRatio = new Vector3((float)pChip.dbChipSizeRatio, (float)pChip.dbChipSizeRatio, 1f);
                }
                int num9 = ctChipPatternAnimation.Drums.nCurrentValue;

                switch (pChip.nChannelNumber)
                {
                    case EChannel.HiHatClose:
                        x = (x + 0x10) - ((int)((32.0 * pChip.dbChipSizeRatio) / 2.0));
                        if (txChip != null)
                        {
                            txChip.tDraw2D(CDTXMania.app.Device, x - 5, y - 0x20, new Rectangle(60 + 10, 128 + (num9 * 64), 0x2e + 10, 64));
                        }
                        break;

                    case EChannel.Snare:
                        x = (x + 0x10) - ((int)((32.0 * pChip.dbChipSizeRatio) / 2.0));
                        if (txChip != null)
                        {
                            txChip.tDraw2D(CDTXMania.app.Device, x - 5, y - 32, new Rectangle(0x6a + 20, 128 + (num9 * 64), 0x36 + 10, 64));
                        }
                        break;

                    case EChannel.BassDrum:
                        x = (x + 0x16) - ((int)((44.0 * pChip.dbChipSizeRatio) / 2.0));
                        if (txChip != null)
                        {
                            txChip.tDraw2D(CDTXMania.app.Device, x - 5, y - 0x20, new Rectangle(0, 128 + (num9 * 0x40), 60 + 10, 0x40));
                        }
                        break;

                    case EChannel.HighTom:
                        x = (x + 0x10) - ((int)((32.0 * pChip.dbChipSizeRatio) / 2.0));
                        if (txChip != null)
                        {
                            txChip.tDraw2D(CDTXMania.app.Device, x - 5, y - 32, new Rectangle(160 + 30, 128 + (num9 * 0x40), 0x2e + 10, 64));
                        }
                        break;

                    case EChannel.LowTom:
                        x = (x + 0x10) - ((int)((32.0 * pChip.dbChipSizeRatio) / 2.0));
                        if (txChip != null)
                        {
                            txChip.tDraw2D(CDTXMania.app.Device, x - 5, y - 32, new Rectangle(0xce + 40, 128 + (num9 * 0x40), 0x2e + 10, 64));
                        }
                        break;

                    case EChannel.Cymbal:
                        x = (x + 19) - ((int)((38.0 * pChip.dbChipSizeRatio) / 2.0));
                        if (txChip != null)
                        {
                            txChip.tDraw2D(CDTXMania.app.Device, x - 5, y - 0x20, new Rectangle(298 + 60, 128 + (num9 * 64), 64 + 10, 64));
                        }
                        break;

                    case EChannel.FloorTom:
                        x = (x + 0x10) - ((int)((32.0 * pChip.dbChipSizeRatio) / 2.0));
                        if (txChip != null)
                        {
                            txChip.tDraw2D(CDTXMania.app.Device, x - 5, y - 0x20, new Rectangle(0xfc + 50, 128 + (num9 * 64), 0x2e + 10, 0x40));
                        }
                        break;

                    case EChannel.HiHatOpen:
                        x = (x + 13) - ((int)((26.0 * pChip.dbChipSizeRatio) / 2.0));
                        if (txChip != null)
                        {
                            switch (configIni.eHHOGraphics.Drums)
                            {
                                case EType.A:
                                    x = (x + 14) - ((int)((26.0 * pChip.dbChipSizeRatio) / 2.0));
                                    txChip.tDraw2D(CDTXMania.app.Device, x - 5, y - 0x20, new Rectangle(0x200 + 100, 128 + (num9 * 64), 0x26 + 10, 64));
                                    break;

                                /*
                            case EType.B:
                                x = (x + 14) - ((int)((26.0 * pChip.dbChipSizeRatio) / 2.0));
                                this.txチップ.tDraw2D(CDTXMania.app.Device, x, y - 32, new Rectangle(0x200, 128 + (num9 * 64), 0x26, 64));
                                break;
                                 */

                                case EType.C:
                                    x = (x + 13) - ((int)((32.0 * pChip.dbChipSizeRatio) / 2.0));
                                    txChip.tDraw2D(CDTXMania.app.Device, x - 5, y - 0x20, new Rectangle(60 + 10, 128 + (num9 * 64), 0x2e + 10, 64));
                                    break;
                            }
                        }
                        break;

                    case EChannel.RideCymbal:
                        x = (x + 13) - ((int)((26.0 * pChip.dbChipSizeRatio) / 2.0));
                        if (txChip != null)
                        {
                            txChip.tDraw2D(CDTXMania.app.Device, x - 5, y - 0x20, new Rectangle(0x16a + 70, 128 + (num9 * 64), 0x26 + 10, 0x40));
                        }
                        break;

                    case EChannel.LeftCymbal:
                        x = (x + 0x13) - ((int)((38.0 * pChip.dbChipSizeRatio) / 2.0));
                        if (txChip != null)
                        {
                            txChip.tDraw2D(CDTXMania.app.Device, x - 5, y - 0x20, new Rectangle(448 + 90, 128 + (num9 * 64), 64 + 10, 64));
                        }
                        break;

                    case EChannel.LeftPedal:
                        x = (x + 0x13) - ((int)((38.0 * pChip.dbChipSizeRatio) / 2.0));
                        if (txChip != null)
                        {
                            txChip.tDraw2D(CDTXMania.app.Device, x - 5, y - 0x20, new Rectangle(550 + 110, 128 + (num9 * 64), 0x30 + 10, 64));
                        }
                        break;

                    case EChannel.LeftBassDrum:
                        x = (x + 0x13) - ((int)((38.0 * pChip.dbChipSizeRatio) / 2.0));
                        if (txChip != null)
                        {

                            if (configIni.eLBDGraphics.Drums == EType.A)
                            {
                                txChip.tDraw2D(CDTXMania.app.Device, x - 5, y - 0x20, new Rectangle(550 + 110, 128 + (num9 * 64), 0x30, 0x40));
                            }
                            else if (configIni.eLBDGraphics.Drums == EType.B)
                            {
                                txChip.tDraw2D(CDTXMania.app.Device, x - 5, y - 0x20, new Rectangle(400 + 80, 128 + (num9 * 64), 0x30 + 10, 0x40));
                            }
                        }
                        break;
                }
                if (txChip != null)
                {
                    txChip.vcScaleRatio = new Vector3(1f, 1f, 1f);
                    txChip.nTransparency = 0xff;
                }
            }

            /*
            int indexSevenLanes = this.nチャンネル0Atoレーン07[ pChip.nChannelNumber - 0x11 ];
            if ( ( configIni.bAutoPlay[ indexSevenLanes ] && !pChip.bHit ) && ( pChip.nDistanceFromBar.Drums < 0 ) )
            {
                pChip.bHit = true;
                this.actLaneFlushD.Start( (ELane) indexSevenLanes, ( (float) CInputManager.n通常音量 ) / 127f );
                bool flag = this.bInFillIn;
                bool flag2 = this.bInFillIn && this.bフィルイン区間の最後のChipである( pChip );
                //bool flag3 = flag2;
                // #31602 2013.6.24 yyagi 判定ラインの表示位置をずらしたら、チップのヒットエフェクトの表示もずらすために、nJudgeLine..を追加
                this.actChipFireD.Start( (ELane)indexSevenLanes, flag, flag2, flag2, nJudgeLinePosY_delta.Drums );
                this.actPad.Hit( this.nチャンネル0Atoパッド08[ pChip.nChannelNumber - 0x11 ] );
                this.tPlaySound( pChip, CSoundManager.rcPerformanceTimer.n前回リセットした時のシステム時刻 + pChip.nPlaybackTimeMs, EInstrumentPart.DRUMS, dTX.nモニタを考慮した音量( EInstrumentPart.DRUMS ) );
                this.tProcessChipHit( pChip.nPlaybackTimeMs, pChip );
            }
            */
            return;
        }	// end of "if configIni.bDrumsEnabled"
        if (!pChip.bHit && (pChip.nDistanceFromBar.Drums < 0))
        {
            //this.tPlaySound(pChip, CSoundManager.rcPerformanceTimer.n前回リセットした時のシステム時刻 + pChip.nPlaybackTimeMs, EInstrumentPart.DRUMS, dTX.nモニタを考慮した音量(EInstrumentPart.DRUMS));
            pChip.bHit = true;
        }
    }
    protected override void tUpdateAndDraw_Chip_Drums( CConfigIni configIni, ref CDTX dTX, ref CChip pChip )
    {
        if( configIni.bDrumsEnabled )
        {
            #region [ Sudden処理 ]
            if( ( CDTXMania.ConfigIni.nHidSud.Drums == 2 ) || ( CDTXMania.ConfigIni.nHidSud.Drums == 3 ) )
            {
                if( pChip.nDistanceFromBar.Drums < 200 )
                {
                    pChip.bVisible = true;
                    pChip.nTransparency = 0xff;
                }
                else if( pChip.nDistanceFromBar.Drums < 250 )
                {
                    pChip.bVisible = true;
                    pChip.nTransparency = 0xff - ( (int) ( ( ( (double) ( pChip.nDistanceFromBar.Drums - 200 ) ) * 255.0 ) / 50.0 ) );
                }
                else
                {
                    pChip.bVisible = false;
                    pChip.nTransparency = 0;
                }
            }
            #endregion
            #region [ Hidden処理 ]
            if( ( CDTXMania.ConfigIni.nHidSud.Drums == 1 ) || ( CDTXMania.ConfigIni.nHidSud.Drums == 3 ) )
            {
                if( pChip.nDistanceFromBar.Drums < 100 )
                {
                    pChip.bVisible = false;
                }
                else if( pChip.nDistanceFromBar.Drums < 150 )
                {
                    pChip.bVisible = true;
                    pChip.nTransparency = (int) ( ( ( (double) ( pChip.nDistanceFromBar.Drums - 100 ) ) * 255.0 ) / 50.0 );
                }
            }
            #endregion
            #region [ ステルス処理 ]
            if( CDTXMania.ConfigIni.nHidSud.Drums == 4 )
            {
                pChip.bVisible = false;
            }
            #endregion
            if( !pChip.bHit && pChip.bVisible )
            {
                if( txChip != null )
                {
                    txChip.nTransparency = pChip.nTransparency;
                }
                int x = nチャンネルtoX座標[ pChip.nChannelNumber - EChannel.HiHatClose];

                if( configIni.eLaneType.Drums == EType.A )
                {
                    if (configIni.eRDPosition == ERDPosition.RCRD)
                    {
                        x = nチャンネルtoX座標[ pChip.nChannelNumber - EChannel.HiHatClose];
                    }
                    else if( configIni.eRDPosition == ERDPosition.RDRC )
                    {
                        x = nチャンネルtoX座標改[ pChip.nChannelNumber - EChannel.HiHatClose];
                    }
                }
                else if( configIni.eLaneType.Drums == EType.B )
                {
                    if( configIni.eRDPosition == ERDPosition.RCRD )
                    {
                        x = nチャンネルtoX座標B[ pChip.nChannelNumber - EChannel.HiHatClose];
                    }
                    else if( configIni.eRDPosition == ERDPosition.RDRC )
                    {
                        x = nチャンネルtoX座標B改[ pChip.nChannelNumber - EChannel.HiHatClose];
                    }
                }
                else if( configIni.eLaneType.Drums == EType.C )
                {
                    if( configIni.eRDPosition == ERDPosition.RCRD )
                    {
                        x = nチャンネルtoX座標C[ pChip.nChannelNumber - EChannel.HiHatClose];
                    }
                    else if( configIni.eRDPosition == ERDPosition.RDRC )
                    {
                        x = nチャンネルtoX座標C改[ pChip.nChannelNumber - EChannel.HiHatClose];
                    }
                }
                else if( configIni.eLaneType.Drums == EType.D )
                {
                    if( configIni.eRDPosition == ERDPosition.RCRD )
                    {
                        x = nチャンネルtoX座標D[ pChip.nChannelNumber - EChannel.HiHatClose];
                    }
                    else if( configIni.eRDPosition == ERDPosition.RDRC )
                    {
                        x = nチャンネルtoX座標D改[ pChip.nChannelNumber - EChannel.HiHatClose];
                    }
                }

                if( configIni.eRDPosition == ERDPosition.RDRC )
                {
                    if( configIni.eLaneType.Drums == EType.A )
                    {
                        x = nチャンネルtoX座標改[ pChip.nChannelNumber - EChannel.HiHatClose];
                    }
                    else if( configIni.eLaneType.Drums == EType.B )
                    {
                        x = nチャンネルtoX座標B改[ pChip.nChannelNumber - EChannel.HiHatClose];
                    }
                }

                int y = configIni.bReverse.Drums ? ( nJudgeLinePosY.Drums + pChip.nDistanceFromBar.Drums ) : ( nJudgeLinePosY.Drums - pChip.nDistanceFromBar.Drums );
                if( txChip != null )
                {
                    txChip.vcScaleRatio = new Vector3( ( float )pChip.dbChipSizeRatio, ( float )pChip.dbChipSizeRatio, 1f );
                }
                int num9 = ctChipPatternAnimation.Drums.nCurrentValue;

                switch( pChip.nChannelNumber )
                {
                    case EChannel.HiHatClose:
                        x = ( x + 0x10 ) - ( ( int )( ( 32.0 * pChip.dbChipSizeRatio ) / 2.0 ) );
                        if( txChip != null )
                        {
                            txChip.tDraw2D( CDTXMania.app.Device, x - 5, y - 32, new Rectangle( 60 + 10, 0, 0x2e + 10, 64 ) );

                            if( pChip.bBonusChip )
                                txChip.tDraw2D( CDTXMania.app.Device, x - 5, y - 32, new Rectangle( 60 + 10, 64, 0x2e + 10, 64 ) );
                        }
                        break;

                    case EChannel.Snare:
                        x = ( x + 0x10 ) - ( ( int )( ( 32.0 * pChip.dbChipSizeRatio ) / 2.0 ) );
                        if (txChip != null)
                        {
                            txChip.tDraw2D(CDTXMania.app.Device, x - 5, y - 32, new Rectangle( 0x6a + 20, 0, 0x36 +10, 64 ) );

                            if( pChip.bBonusChip )
                                txChip.tDraw2D( CDTXMania.app.Device, x - 5, y - 32, new Rectangle( 0x6a + 20, 64, 0x36 + 10, 64 ) );
                        }
                        break;

                    case EChannel.BassDrum:
                        x = ( x + 0x16 ) - ( ( int )( ( 44.0 * pChip.dbChipSizeRatio ) / 2.0 ) );
                        if( txChip != null )
                        {
                            txChip.tDraw2D( CDTXMania.app.Device, x - 5, y - 32, new Rectangle( 0, 0, 60 + 10, 64 ) );

                            if( pChip.bBonusChip )
                                txChip.tDraw2D( CDTXMania.app.Device, x - 5, y - 32, new Rectangle( 0, 64, 60 + 10, 64 ) );
                        }
                        break;

                    case EChannel.HighTom:
                        x = ( x + 0x10 ) - ( ( int )( ( 32.0 * pChip.dbChipSizeRatio ) / 2.0 ) );
                        if( txChip != null )
                        {
                            txChip.tDraw2D( CDTXMania.app.Device, x - 5, y - 32, new Rectangle( 160 + 30, 0, 0x2e + 10, 64 ) );

                            if( pChip.bBonusChip )
                                txChip.tDraw2D( CDTXMania.app.Device, x - 5, y - 32, new Rectangle( 160 + 30, 64, 0x2e + 10, 64 ) );
                        }
                        break;

                    case EChannel.LowTom:
                        x = ( x + 0x10 ) - ( ( int )( ( 32.0 * pChip.dbChipSizeRatio ) / 2.0 ) );
                        if (txChip != null)
                        {
                            txChip.tDraw2D( CDTXMania.app.Device, x - 5, y - 32, new Rectangle( 0xce + 40, 0, 0x2e + 10, 64 ) );

                            if( pChip.bBonusChip )
                                txChip.tDraw2D( CDTXMania.app.Device, x - 5, y - 32, new Rectangle( 0xce + 40, 64, 0x2e + 10, 64 ) );
                        }
                        break;

                    case EChannel.Cymbal:
                        x = ( x + 19 ) - ( ( int )( ( 38.0 * pChip.dbChipSizeRatio ) / 2.0 ) );
                        if( txChip != null )
                        {
                            txChip.tDraw2D( CDTXMania.app.Device, x - 5, y - 32, new Rectangle( 298 + 60, 0, 0x40 + 10, 64 ) );

                            if( pChip.bBonusChip )
                                txChip.tDraw2D( CDTXMania.app.Device, x - 5, y - 32, new Rectangle( 298 + 60, 64, 0x40 + 10, 64 ) );
                        }
                        break;

                    case EChannel.FloorTom:
                        x = ( x + 0x10 ) - ( ( int )( ( 32.0 * pChip.dbChipSizeRatio ) / 2.0 ) );
                        if( txChip != null )
                        {
                            txChip.tDraw2D( CDTXMania.app.Device, x - 5, y - 32, new Rectangle( 0xfc + 50, 0, 0x2e + 10, 64 ) );

                            if( pChip.bBonusChip )
                                txChip.tDraw2D( CDTXMania.app.Device, x - 5, y - 32, new Rectangle( 0xfc + 50, 64, 0x2e + 10, 64 ) );
                        }
                        break;

                    case EChannel.HiHatOpen:
                        x = ( x + 13 ) - ( ( int )( ( 26.0 * pChip.dbChipSizeRatio ) / 2.0 ) );
                        if( txChip != null )
                        {
                            switch( configIni.eHHOGraphics.Drums )
                            {
                                case EType.A:
                                    x = ( x + 14 ) - ( ( int )( ( 26.0 * pChip.dbChipSizeRatio ) / 2.0 ) );
                                    txChip.tDraw2D( CDTXMania.app.Device, x - 5, y - 32, new Rectangle( 0x200 + 100, 0, 0x26 + 10, 64 ) );
                                    if( pChip.bBonusChip )
                                        txChip.tDraw2D( CDTXMania.app.Device, x - 5, y - 32, new Rectangle( 0x200 + 100, 64, 0x26 + 10, 64 ) );
                                    break;

                                case EType.B:
                                    x = ( x + 14 ) - ( ( int )( ( 26.0 * pChip.dbChipSizeRatio ) / 2.0 ) );
                                    txChip.tDraw2D( CDTXMania.app.Device, x - 5, y - 32, new Rectangle( 0x200 + 100, 0, 0x26 + 10, 64 ) );
                                    if( pChip.bBonusChip )
                                        txChip.tDraw2D( CDTXMania.app.Device, x - 5, y - 32, new Rectangle( 0x200 + 100, 64, 0x26 + 10, 64 ) );
                                    break;

                                case EType.C:
                                    x = ( x + 13 ) - ( ( int )( ( 32.0 * pChip.dbChipSizeRatio ) / 2.0 ) );
                                    txChip.tDraw2D( CDTXMania.app.Device, x - 5, y - 32, new Rectangle( 60 + 10, 0, 0x2e + 10, 64 ) );
                                    if( pChip.bBonusChip )
                                        txChip.tDraw2D( CDTXMania.app.Device, x - 5, y - 32, new Rectangle( 60 + 100, 64, 0x2e + 10, 64 ) );
                                    break;
                            }
                        }
                        break;

                    case EChannel.RideCymbal:
                        x = ( x + 13 ) - ( ( int )( ( 26.0 * pChip.dbChipSizeRatio ) / 2.0 ) );
                        if( txChip != null )
                        {
                            txChip.tDraw2D( CDTXMania.app.Device, x - 5, y - 32, new Rectangle( 0x16a + 70, 0, 0x26 + 10, 64 ) );

                            if( pChip.bBonusChip )
                                txChip.tDraw2D( CDTXMania.app.Device, x - 5, y - 0x20, new Rectangle( 0x16a + 70, 64, 0x26 + 10, 0x40 ) );
                        }
                        break;

                    case EChannel.LeftCymbal:
                        x = ( x + 0x13 ) - ( ( int )( ( 38.0 * pChip.dbChipSizeRatio ) / 2.0 ) );
                        if (txChip != null)
                        {
                            txChip.tDraw2D( CDTXMania.app.Device, x - 5, y - 32, new Rectangle( 448 + 90, 0, 64 + 10, 64 ) );

                            if( pChip.bBonusChip )
                                txChip.tDraw2D( CDTXMania.app.Device, x - 5, y - 0x20, new Rectangle( 448 + 90, 64, 64 + 10, 64 ) );
                        }
                        break;

                    case EChannel.LeftPedal:
                        x = ( x + 0x13 ) - ( ( int )( ( 38.0 * pChip.dbChipSizeRatio ) / 2.0 ) );
                        if (txChip != null)
                        {
                            txChip.tDraw2D( CDTXMania.app.Device, x - 5, y - 32, new Rectangle( 550 + 110, 0, 0x30 + 10, 64 ) );
                                
                            if( pChip.bBonusChip )
                                txChip.tDraw2D( CDTXMania.app.Device, x - 5, y - 32, new Rectangle( 550 + 110, 64, 0x30 + 10, 64 ) );
                        }
                        break;

                    case EChannel.LeftBassDrum:
                        x = ( x + 0x13 ) - ( ( int )( ( 38.0 * pChip.dbChipSizeRatio ) / 2.0 ) );
                        if( txChip != null )
                        {
                            if( configIni.eLBDGraphics.Drums == EType.A )
                            {
                                txChip.tDraw2D( CDTXMania.app.Device, x - 5, y - 32, new Rectangle( 550 + 110, 0, 0x30 + 10, 64 ) );
                                if( pChip.bBonusChip )
                                    txChip.tDraw2D( CDTXMania.app.Device, x - 5, y - 32, new Rectangle( 550 + 110, 64, 0x30 + 10, 64 ) );
                            }
                            else if( configIni.eLBDGraphics.Drums == EType.B )
                            {
                                txChip.tDraw2D( CDTXMania.app.Device, x - 5, y - 32, new Rectangle( 400 + 80, 0, 0x30 + 10, 64 ) );
                                if( pChip.bBonusChip )
                                    txChip.tDraw2D( CDTXMania.app.Device, x - 5, y - 32, new Rectangle( 400 + 80, 64, 0x30 + 10, 64 ) );
                            }
                        }
                        break;
                }
                if( txChip != null )
                {
                    txChip.vcScaleRatio = new Vector3( 1f, 1f, 1f );
                    txChip.nTransparency = 0xff;
                }
            }

            int indexSevenLanes = nチャンネル0Atoレーン07[ pChip.nChannelNumber - EChannel.HiHatClose];
            // #35411 chnmr0 modified
            bool autoPlayCondition = ( configIni.bAutoPlay[ indexSevenLanes ] && !pChip.bHit );
            bool UsePerfectGhost = true;
            long ghostLag = 0;

            if( CDTXMania.ConfigIni.eAutoGhost.Drums != EAutoGhostData.PERFECT &&
                CDTXMania.listAutoGhostLag.Drums != null &&
                0 <= pChip.n楽器パートでの出現順 && pChip.n楽器パートでの出現順 < CDTXMania.listAutoGhostLag.Drums.Count)

            {
                // ゴーストデータが有効 : ラグに合わせて判定
                ghostLag = CDTXMania.listAutoGhostLag.Drums[pChip.n楽器パートでの出現順];
                ghostLag = (ghostLag & 255) - 128;
                ghostLag -= nInputAdjustTimeMs.Drums;
                autoPlayCondition &= !pChip.bHit && (ghostLag + pChip.nPlaybackTimeMs <= CSoundManager.rcPerformanceTimer.n現在時刻ms);
                UsePerfectGhost = false;
            }
            if( UsePerfectGhost )
            {
                // 従来の AUTO : バー下で判定
                autoPlayCondition &= ( pChip.nDistanceFromBar.Drums < 0 );
            }

            if ( autoPlayCondition )
            {
                pChip.bHit = true;
                actLaneFlushD.Start( (ELane) indexSevenLanes, ( (float) CInputManager.n通常音量 ) / 127f );
                bool flag = bInFillIn;
                bool flag2 = bInFillIn && bフィルイン区間の最後のChipである( pChip );
                //bool flag3 = flag2;
                // #31602 2013.6.24 yyagi 判定ラインの表示位置をずらしたら、チップのヒットエフェクトの表示もずらすために、nJudgeLine..を追加
                actChipFireD.Start( (ELane)indexSevenLanes, flag, flag2, flag2, nJudgeLinePosY_delta.Drums );
                actPad.Hit( nチャンネル0Atoパッド08[ pChip.nChannelNumber - EChannel.HiHatClose] );
                tPlaySound( pChip, CSoundManager.rcPerformanceTimer.n前回リセットした時のシステム時刻 + pChip.nPlaybackTimeMs + ghostLag, EInstrumentPart.DRUMS, dTX.nモニタを考慮した音量( EInstrumentPart.DRUMS ) );
                tProcessChipHit(pChip.nPlaybackTimeMs + ghostLag, pChip);
                //cInvisibleChip.StartSemiInvisible( EInstrumentPart.DRUMS );
            }
            // #35411 modify end
                
            // #35411 2015.08.21 chnmr0 add
            // 目標値グラフにゴーストの達成率を渡す
            if (CDTXMania.ConfigIni.eTargetGhost.Drums != ETargetGhostData.NONE &&
                CDTXMania.listTargetGhsotLag.Drums != null)
            {
                double val = 0;
                if (CDTXMania.ConfigIni.eTargetGhost.Drums == ETargetGhostData.ONLINE)
                {
                    if (CDTXMania.DTX.nVisibleChipsCount.Drums > 0)
                    {
                        // Online Stats の計算式
                        val = 100 *
                            (nヒット数_TargetGhost.Drums.Perfect * 17 +
                             nヒット数_TargetGhost.Drums.Great * 7 +
                             n最大コンボ数_TargetGhost.Drums * 3) / (20.0 * CDTXMania.DTX.nVisibleChipsCount.Drums);
                    }
                }
                else
                {
                    if( CDTXMania.ConfigIni.nSkillMode == 0 )
                    {
                        val = CScoreIni.tCalculatePlayingSkillOld(
                            CDTXMania.DTX.nVisibleChipsCount.Drums,
                            nヒット数_TargetGhost.Drums.Perfect,
                            nヒット数_TargetGhost.Drums.Great,
                            nヒット数_TargetGhost.Drums.Good,
                            nヒット数_TargetGhost.Drums.Poor,
                            nヒット数_TargetGhost.Drums.Miss,
                            n最大コンボ数_TargetGhost.Drums,
                            EInstrumentPart.DRUMS, new STAUTOPLAY());
                    }
                    else
                    {
                        val = CScoreIni.tCalculatePlayingSkill(
                            CDTXMania.DTX.nVisibleChipsCount.Drums,
                            nヒット数_TargetGhost.Drums.Perfect,
                            nヒット数_TargetGhost.Drums.Great,
                            nヒット数_TargetGhost.Drums.Good,
                            nヒット数_TargetGhost.Drums.Poor,
                            nヒット数_TargetGhost.Drums.Miss,
                            n最大コンボ数_TargetGhost.Drums,
                            EInstrumentPart.DRUMS, new STAUTOPLAY());
                    }

                }
                if (val < 0) val = 0;
                if (val > 100) val = 100;
                actGraph.dbGraphValue_Goal = val;
            }
            return;
        }	// end of "if configIni.bDrumsEnabled"
        if( !pChip.bHit && ( pChip.nDistanceFromBar.Drums < 0 ) )
        {
            tPlaySound( pChip, CSoundManager.rcPerformanceTimer.n前回リセットした時のシステム時刻 + pChip.nPlaybackTimeMs, EInstrumentPart.DRUMS, dTX.nモニタを考慮した音量( EInstrumentPart.DRUMS ) );
            pChip.bHit = true;
        }
    }
    protected override void tUpdateAndDraw_Chip_GuitarBass(CConfigIni configIni, ref CDTX dTX, ref CChip pChip, EInstrumentPart inst)
    {
        base.tUpdateAndDraw_Chip_GuitarBass( configIni, ref dTX, ref pChip, inst,
            95, 374, 57, 412, 509, 400,
            268, 144, 76, 6,
            24, 509, 561, 400, 452, 26, 24 );
    }

    /*
    protected override void tUpdateAndDraw_Chip_Guitar_Wailing( CConfigIni configIni, ref CDTX dTX, ref CChip pChip )
    {
        if ( configIni.bGuitarEnabled )
        {
            //if ( configIni.bSudden.Guitar )
            //{
            //    pChip.bVisible = pChip.nDistanceFromBar.Guitar < 200;
            //}
            //if ( configIni.bHidden.Guitar && ( pChip.nDistanceFromBar.Guitar < 100 ) )
            //{
            //    pChip.bVisible = false;
            //}

            // 後日、以下の部分を何とかCStage演奏画面共通.csに移したい。
            if ( !pChip.bHit && pChip.bVisible )
            {
                int[] y_base = { 0x5f, 0x176 };		// 判定バーのY座標: ドラム画面かギター画面かで変わる値
                int offset = 0x39;					// ドラム画面かギター画面かで変わる値

                const int WailingWidth = 20;		// ウェイリングチップ画像の幅: 4種全て同じ値
                const int WailingHeight = 50;		// ウェイリングチップ画像の高さ: 4種全て同じ値
                const int baseTextureOffsetX = 268;	// テクスチャ画像中のウェイリングチップ画像の位置X: ドラム画面かギター画面かで変わる値
                const int baseTextureOffsetY = 174;	// テクスチャ画像中のウェイリングチップ画像の位置Y: ドラム画面かギター画面かで変わる値
                const int drawX = 588;				// ウェイリングチップ描画位置X座標: 4種全て異なる値

                const int numA = 25;				// 4種全て同じ値
                int y = configIni.bReverse.Guitar ? ( y_base[1] - pChip.nDistanceFromBar.Guitar ) : ( y_base[0] + pChip.nDistanceFromBar.Guitar );
                int numB = y - offset;				// 4種全て同じ定義
                int numC = 0;						// 4種全て同じ初期値
                const int numD = 355;				// ドラム画面かギター画面かで変わる値
                if ( ( numB < ( numD + numA ) ) && ( numB > -numA ) )	// 以下のロジックは4種全て同じ
                {
                    int c = this.ctWailingChipPatternAnimation.nCurrentValue;
                    Rectangle rect = new Rectangle( baseTextureOffsetX + ( c * WailingWidth ), baseTextureOffsetY, WailingWidth, WailingHeight);
                    if ( numB < numA )
                    {
                        rect.Y += numA - numB;
                        rect.Height -= numA - numB;
                        numC = numA - numB;
                    }
                    if ( numB > ( numD - numA ) )
                    {
                        rect.Height -= numB - ( numD - numA );
                    }
                    if ( ( rect.Bottom > rect.Top ) && ( this.txチップ != null ) )
                    {
                        this.txチップ.tDraw2D( CDTXMania.app.Device, drawX, ( y - numA ) + numC, rect );
                    }
                }
            }
            //    if ( !pChip.bHit && ( pChip.nDistanceFromBar.Guitar < 0 ) )
            //    {
            //        if ( pChip.nDistanceFromBar.Guitar < -234 )	// #25253 2011.5.29 yyagi: Don't set pChip.bHit=true for wailing at once. It need to 1sec-delay (234pix per 1sec).
            //        {
            //            pChip.bHit = true;
            //        }
            //        if ( configIni.bAutoPlay.Guitar )
            //        {
            //            pChip.bHit = true;						// #25253 2011.5.29 yyagi: Set pChip.bHit=true if autoplay.
            //            this.actWailingBonus.Start( EInstrumentPart.GUITAR, this.r現在の歓声Chip.Guitar );
            //        }
            //    }
            //    return;
            //}
            //pChip.bHit = true;
        }
        base.tUpdateAndDraw_Chip_Guitar_Wailing( configIni, ref dTX, ref pChip );
    }
     */
    protected override void tUpdateAndDraw_Chip_FillIn( CConfigIni configIni, ref CDTX dTX, ref CChip pChip )
    {
        if ( !pChip.bHit && ( pChip.nDistanceFromBar.Drums < 0 ) )
        {
            pChip.bHit = true;
            switch ( pChip.nIntegerValue )
            {
                case 0x01:	// フィルイン開始
                    bEndFillIn = true;
                    if ( configIni.bFillInEnabled )
                    {
                        bInFillIn = true;
                    }
                    break;

                case 0x02:	// フィルイン終了
                    bEndFillIn = true;
                    if ( configIni.bFillInEnabled )
                    {
                        bInFillIn = false;
                    }
                    if (((actCombo.nCurrentCombo.Drums > 0) || configIni.bAllDrumsAreAutoPlay) && configIni.b歓声を発声する)
                    {
                        actAVI.Start(bInFillIn);
                        if (r現在の歓声Chip.Drums != null)
                        {
                            dTX.tPlayChip(r現在の歓声Chip.Drums, CSoundManager.rcPerformanceTimer.nシステム時刻, (int)ELane.BGM, dTX.nモニタを考慮した音量(EInstrumentPart.UNKNOWN));
                        }
                        else
                        {
                            CDTXMania.Skin.soundAudience.tPlay();
                            CDTXMania.Skin.soundAudience.n位置_次に鳴るサウンド = 0;
                        }
                        //if (CDTXMania.ConfigIni.nSkillMode == 1)
                        //    this.actScore.nCurrentTrueScore.Drums += 500;
                    }
                    break;
                case 0x03:
                    bChorusSection = true;
                    break;
                case 0x04:
                    bChorusSection = false;
                    break;
                case 0x05:
                    if (configIni.bFillInEnabled)
                    {
                        bChorusSection = true;
                    }
                    if (((actCombo.nCurrentCombo.Drums > 0) || configIni.bAllDrumsAreAutoPlay) && configIni.b歓声を発声する && configIni.DisplayBonusEffects)
                    {
                        actAVI.Start(true);
                        if (r現在の歓声Chip.Drums != null)
                        {
                            dTX.tPlayChip(r現在の歓声Chip.Drums, CSoundManager.rcPerformanceTimer.nシステム時刻, (int)ELane.BGM, dTX.nモニタを考慮した音量(EInstrumentPart.UNKNOWN));
                        }
                        else
                        {
                            CDTXMania.Skin.soundAudience.tPlay();
                            CDTXMania.Skin.soundAudience.n位置_次に鳴るサウンド = 0;
                        }
                    }
                    break;
                case 0x06:
                    if (configIni.bFillInEnabled)
                    {
                        bChorusSection = false;
                    }
                    if (((actCombo.nCurrentCombo.Drums > 0) || configIni.bAllDrumsAreAutoPlay) && configIni.b歓声を発声する && configIni.DisplayBonusEffects)
                    {
                        actAVI.Start(true);
                        if (r現在の歓声Chip.Drums != null)
                        {
                            dTX.tPlayChip(r現在の歓声Chip.Drums, CSoundManager.rcPerformanceTimer.nシステム時刻, (int)ELane.BGM, dTX.nモニタを考慮した音量(EInstrumentPart.UNKNOWN));
                        }
                        else
                        {
                            CDTXMania.Skin.soundAudience.tPlay();
                            CDTXMania.Skin.soundAudience.n位置_次に鳴るサウンド = 0;
                        }
                    }
                    break;
            }
        }
    }

        
    protected override void tUpdateAndDraw_Chip_Bonus(CConfigIni configIni, ref CDTX dTX, ref CChip pChip)  // t進行描画_チップ_ボーナス
    {

    }
        
    public void tProcessChipHit_BonusChip( CConfigIni configIni, CDTX dTX, CChip pChip)  // tボーナスチップのヒット処理
    {
        pChip.bHit = true;

        //if ((this.actCombo.nCurrentCombo.Drums > 0) && configIni.b歓声を発声する )
        if( pChip.bBonusChip )
        {
            bBonus = true;
            switch( pChip.nChannelNumber )
            {
                //case 0x01: //LC
                //    this.actPad.Start(0, true, pChip.nChannelNumber);
                //    break;

                //case 0x02: //HH
                //    this.actPad.Start(1, true, pChip.nChannelNumber);
                //    break;

                //case 0x03: //LP
                //    this.actPad.Start(2, true, pChip.nChannelNumber);
                //    break;

                //case 0x04: //SD
                //    this.actPad.Start(3, true, pChip.nChannelNumber);
                //    break;

                //case 0x05: //HT
                //    this.actPad.Start(4, true, pChip.nChannelNumber);
                //    break;

                //case 0x06: //BD
                //    this.actPad.Start(5, true, pChip.nChannelNumber);
                //    break;

                //case 0x07: //LT
                //    this.actPad.Start(6, true, pChip.nChannelNumber);
                //    break;

                //case 0x08: //FT
                //    this.actPad.Start(7, true, pChip.nChannelNumber);
                //    break;

                //case 0x09: //CY
                //    this.actPad.Start(8, true, pChip.nChannelNumber);
                //    break;

                //case 0x0A: //RD
                //    this.actPad.Start(9, true, pChip.nChannelNumber);
                //    break;

                case EChannel.LeftCymbal: //LC
                    actPad.Start( 0, true, pChip.nChannelNumber );
                    break;

                case EChannel.HiHatClose: //HH
                case EChannel.HiHatOpen:
                    actPad.Start( 1, true, pChip.nChannelNumber );
                    break;

                case EChannel.LeftPedal: //LP
                case EChannel.LeftBassDrum:
                    actPad.Start( 2, true, pChip.nChannelNumber );
                    break;

                case EChannel.Snare: //SD
                    actPad.Start( 3, true, pChip.nChannelNumber );
                    break;

                case EChannel.HighTom: //HT
                    actPad.Start( 4, true, pChip.nChannelNumber );
                    break;

                case EChannel.BassDrum: //BD
                    actPad.Start( 5, true, pChip.nChannelNumber );
                    break;

                case EChannel.LowTom: //LT
                    actPad.Start( 6, true, pChip.nChannelNumber );
                    break;

                case EChannel.FloorTom: //FT
                    actPad.Start( 7, true, pChip.nChannelNumber );
                    break;

                case EChannel.Cymbal: //CY
                    actPad.Start( 8, true, pChip.nChannelNumber );
                    break;

                case EChannel.RideCymbal: //RD
                    actPad.Start(9, true, pChip.nChannelNumber);
                    break;
                default:
                    break;
            }
            if( configIni.DisplayBonusEffects )
            {
                actAVI.Start( true );
                CDTXMania.Skin.soundAudience.tPlay();
                CDTXMania.Skin.soundAudience.n位置_次に鳴るサウンド = 0;
            }
            if( CDTXMania.ConfigIni.nSkillMode == 1 && ( !CDTXMania.ConfigIni.bAllDrumsAreAutoPlay || CDTXMania.ConfigIni.bAutoAddGage ) )
                actScore.Add( EInstrumentPart.DRUMS, bIsAutoPlay, 500L );
        }


    }

    /*
    protected override void t進行描画_チップ_ベース_ウェイリング( CConfigIni configIni, ref CDTX dTX, ref CChip pChip )
    {
        if ( configIni.bGuitarEnabled )
        {
            //if ( configIni.bSudden.Bass )
            //{
            //    pChip.bVisible = pChip.nDistanceFromBar.Bass < 200;
            //}
            //if ( configIni.bHidden.Bass && ( pChip.nDistanceFromBar.Bass < 100 ) )
            //{
            //    pChip.bVisible = false;
            //}

            //
            // 後日、以下の部分を何とかCStage演奏画面共通.csに移したい。
            //
            if ( !pChip.bHit && pChip.bVisible )
            {
                int[] y_base = { 0x5f, 0x176 };		// 判定バーのY座標: ドラム画面かギター画面かで変わる値
                int offset = 0x39;					// ドラム画面かギター画面かで変わる値

                const int WailingWidth = 20;		// ウェイリングチップ画像の幅: 4種全て同じ値
                const int WailingHeight = 50;		// ウェイリングチップ画像の高さ: 4種全て同じ値
                const int baseTextureOffsetX = 268;	// テクスチャ画像中のウェイリングチップ画像の位置X: ドラム画面かギター画面かで変わる値
                const int baseTextureOffsetY = 174;	// テクスチャ画像中のウェイリングチップ画像の位置Y: ドラム画面かギター画面かで変わる値
                const int drawX = 479;				// ウェイリングチップ描画位置X座標: 4種全て異なる値

                const int numA = 25;				// 4種全て同じ値
                int y = configIni.bReverse.Bass ? ( y_base[ 1 ] - pChip.nDistanceFromBar.Bass ) : ( y_base[ 0 ] + pChip.nDistanceFromBar.Bass );
                int numB = y - offset;				// 4種全て同じ定義
                int numC = 0;						// 4種全て同じ初期値
                const int numD = 355;				// ドラム画面かギター画面かで変わる値
                if ( ( numB < ( numD + numA ) ) && ( numB > -numA ) )	// 以下のロジックは4種全て同じ
                {
                    int c = this.ctWailingChipPatternAnimation.nCurrentValue;
                    Rectangle rect = new Rectangle( baseTextureOffsetX + ( c * WailingWidth ), baseTextureOffsetY, WailingWidth, WailingHeight );
                    if ( numB < numA )
                    {
                        rect.Y += numA - numB;
                        rect.Height -= numA - numB;
                        numC = numA - numB;
                    }
                    if ( numB > ( numD - numA ) )
                    {
                        rect.Height -= numB - ( numD - numA );
                    }
                    if ( ( rect.Bottom > rect.Top ) && ( this.txチップ != null ) )
                    {
                        this.txチップ.tDraw2D( CDTXMania.app.Device, drawX, ( y - numA ) + numC, rect );
                    }
                }
            }
            //    if ( !pChip.bHit && ( pChip.nDistanceFromBar.Bass < 0 ) )
            //    {
            //        if ( pChip.nDistanceFromBar.Bass < -234 )	// #25253 2011.5.29 yyagi: Don't set pChip.bHit=true for wailing at once. It need to 1sec-delay (234pix per 1sec).
            //        {
            //            pChip.bHit = true;
            //        }
            //        if ( configIni.bAutoPlay.Bass )
            //        {
            //            this.actWailingBonus.Start( EInstrumentPart.BASS, this.r現在の歓声Chip.Bass );
            //            pChip.bHit = true;						// #25253 2011.5.29 yyagi: Set pChip.bHit=true if autoplay.
            //        }
            //    }
            //    return;
            //}
            //pChip.bHit = true;
        }
            base.t進行描画_チップ_ベース_ウェイリング( configIni, ref dTX, ref pChip);
    }
     */

    protected override void tUpdateAndDraw_Chip_NoSound_Drums(CConfigIni configIni, ref CDTX dTX, ref CChip pChip)  // t進行描画_チップ_空打ち音設定_ドラム
    {
        if (!pChip.bHit && (pChip.nDistanceFromBar.Drums < 0))
        {
            try
            {
                pChip.bHit = true;
                r現在の空うちドラムChip[(int)eChannelToPad[(int)pChip.nChannelNumber - (int)EChannel.HiHatClose_NoChip]] = pChip;
                pChip.nChannelNumber = ((pChip.nChannelNumber < EChannel.LeftCymbal_NoChip) || (pChip.nChannelNumber > EChannel.LeftBassDrum_NoChip)) ? ((pChip.nChannelNumber - 0xb1) + 0x11) : ((pChip.nChannelNumber - 0xb3) + 0x11);
            }
            catch
            {
                return;
            }
        }

    }
    protected override void tUpdateAndDraw_Chip_BarLine( CConfigIni configIni, ref CDTX dTX, ref CChip pChip )
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
            if ( configIni.bWave再生位置自動調整機能有効 && bIsDirectSound )
            {
                dTX.tAutoCorrectWavPlaybackPosition();
            }
        }
        if ( configIni.bShowPerformanceInformation && ( configIni.nLaneDisp.Drums == 0 || configIni.nLaneDisp.Drums == 1 ) )
        {
            int n小節番号 = n小節番号plus1 - 1;
            CDTXMania.actDisplayString.tPrint( configIni.bGraph有効.Drums && configIni.bSmallGraph ? 828 : 858, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + pChip.nDistanceFromBar.Drums) - 0x11) : ((nJudgeLinePosY.Drums - pChip.nDistanceFromBar.Drums) - 0x11), CCharacterConsole.EFontType.White, n小節番号.ToString());
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

            txChip.tDraw2D(CDTXMania.app.Device, 295 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + pChip.nDistanceFromBar.Drums) - 1) : ((nJudgeLinePosY.Drums - pChip.nDistanceFromBar.Drums) - 1), new Rectangle(0, 769, l_drumPanelWidth, 2));
        }
              
        /*
        if ( ( pChip.bVisible && configIni.bGuitarEnabled ) && ( configIni.eDark != EDarkMode.FULL ) )
        {
            int y = configIni.bReverse.Guitar ? ( ( 0x176 - pChip.nDistanceFromBar.Guitar ) - 1 ) : ( ( 0x5f + pChip.nDistanceFromBar.Guitar ) - 1 );
            if ( ( dTX.bチップがある.Guitar && ( y > 0x39 ) ) && ( ( y < 0x19c ) && ( this.txチップ != null ) ) )
            {
                this.txチップ.tDraw2D( CDTXMania.app.Device, 374, y, new Rectangle( 0, 450, 0x4e, 1 ) );
            }
            y = configIni.bReverse.Bass ? ( ( 0x176 - pChip.nDistanceFromBar.Bass ) - 1 ) : ( ( 0x5f + pChip.nDistanceFromBar.Bass ) - 1 );
            if ( ( dTX.bチップがある.Bass && ( y > 0x39 ) ) && ( ( y < 0x19c ) && ( this.txチップ != null ) ) )
            {
                this.txチップ.tDraw2D( CDTXMania.app.Device, 398, y, new Rectangle( 0, 450, 0x4e, 1 ) );
            }
        }
         */
    }
    //移植完了。

    protected override void tDraw_LoopLine(CConfigIni configIni, bool bIsEnd)
    {
        const double speed = 286;	// BPM150の時の1小節の長さ[dot]
        double ScrollSpeedDrums = (actScrollSpeed.db現在の譜面スクロール速度.Drums + 1.0) * 0.5 * 37.5 * speed / 60000.0;

        int nDistanceFromBar = (int)(((bIsEnd ? LoopEndMs : LoopBeginMs) - CSoundManager.rcPerformanceTimer.nCurrentTime) * ScrollSpeedDrums);

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
                txChip.tDraw2D(CDTXMania.app.Device, 295 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + nDistanceFromBar) - 1) : ((nJudgeLinePosY.Drums - nDistanceFromBar) - 1), new Rectangle(0, 769, l_drumPanelWidth, 2));
                txChip.tDraw2D(CDTXMania.app.Device, 295 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + nDistanceFromBar) + 1) : ((nJudgeLinePosY.Drums - nDistanceFromBar) - 3), new Rectangle(0, 769, l_drumPanelWidth, 2));
                txChip.tDraw2D(CDTXMania.app.Device, 295 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + nDistanceFromBar) + 3) : ((nJudgeLinePosY.Drums - nDistanceFromBar) - 5), new Rectangle(0, 769, l_drumPanelWidth, 2));
                txChip.tDraw2D(CDTXMania.app.Device, 295 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + nDistanceFromBar) + 9) : ((nJudgeLinePosY.Drums - nDistanceFromBar) - 11), new Rectangle(0, 769, l_drumPanelWidth, 2));
                txChip.tDraw2D(CDTXMania.app.Device, 295 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + nDistanceFromBar) + 11) : ((nJudgeLinePosY.Drums - nDistanceFromBar) - 13), new Rectangle(0, 769, l_drumPanelWidth, 2));
                txChip.tDraw2D(CDTXMania.app.Device, 295 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + nDistanceFromBar) + 17) : ((nJudgeLinePosY.Drums - nDistanceFromBar) - 19), new Rectangle(0, 769, l_drumPanelWidth, 2));
                txChip.tDraw2D(CDTXMania.app.Device, 295 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + nDistanceFromBar) + 23) : ((nJudgeLinePosY.Drums - nDistanceFromBar) - 25), new Rectangle(0, 769, l_drumPanelWidth, 2));
            }
            else
            {
                txChip.tDraw2D(CDTXMania.app.Device, 295 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + nDistanceFromBar) - 1) : ((nJudgeLinePosY.Drums - nDistanceFromBar) - 1), new Rectangle(0, 769, l_drumPanelWidth, 2));
                txChip.tDraw2D(CDTXMania.app.Device, 295 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + nDistanceFromBar) - 3) : ((nJudgeLinePosY.Drums - nDistanceFromBar) + 1), new Rectangle(0, 769, l_drumPanelWidth, 2));
                txChip.tDraw2D(CDTXMania.app.Device, 295 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + nDistanceFromBar) - 5) : ((nJudgeLinePosY.Drums - nDistanceFromBar) + 3), new Rectangle(0, 769, l_drumPanelWidth, 2));
                txChip.tDraw2D(CDTXMania.app.Device, 295 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + nDistanceFromBar) - 11) : ((nJudgeLinePosY.Drums - nDistanceFromBar) + 9), new Rectangle(0, 769, l_drumPanelWidth, 2));
                txChip.tDraw2D(CDTXMania.app.Device, 295 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + nDistanceFromBar) - 13) : ((nJudgeLinePosY.Drums - nDistanceFromBar) + 11), new Rectangle(0, 769, l_drumPanelWidth, 2));
                txChip.tDraw2D(CDTXMania.app.Device, 295 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + nDistanceFromBar) - 19) : ((nJudgeLinePosY.Drums - nDistanceFromBar) + 17), new Rectangle(0, 769, l_drumPanelWidth, 2));
                txChip.tDraw2D(CDTXMania.app.Device, 295 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + nDistanceFromBar) - 25) : ((nJudgeLinePosY.Drums - nDistanceFromBar) + 23), new Rectangle(0, 769, l_drumPanelWidth, 2));
            }
        }
    }

    #endregion
}