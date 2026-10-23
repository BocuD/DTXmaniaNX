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
        
        AddLegacyPart(PlayfieldGroup, "FillInEffect", LegacyLanesLayer, DrawFillInEffect);
        AddLegacyPart(PlayfieldGroup, "LaneFlush", LegacyLanesLayer, () => Probed(probeLanes, tUpdateAndDraw_LaneFlushD));
        AddLegacyPart(PlayfieldGroup, "Chips", LegacyLanesLayer, () => Probed(probeChips, () => DrawChips(EInstrumentPart.DRUMS)));
        AddLegacyPart(PlayfieldGroup, "ProgressBar", LegacyLanesLayer, () => Probed(probeLanes, () => actProgressBar.OnUpdateAndDraw()));
        AddLegacyPart(PlayfieldGroup, "LaneCover", LegacyLanesLayer, () => Probed(probeLanes, DrawLaneCover));

        //drawn over the judge lines and shutters
        AddLegacyPart(PlayfieldGroup, "Pad", LegacyOverlayLayer, () => Probed(probeLanes, tUpdateAndDraw_DrumPad));
        AddLegacyPart(HudGroup, "Score", LegacyOverlayLayer, () => Probed(probeScore, DrawScore));
        AddLegacyPart(HudGroup, "Combo", LegacyOverlayLayer, () => Probed(probeCombo, tUpdateAndDraw_Combo));
        AddLegacyPart(HudGroup, "Graph", LegacyOverlayLayer, () => Probed(probeGraph, tUpdateAndDraw_Graph));
        AddLegacyPart(HudGroup, "PerformanceInformation", LegacyOverlayLayer, () => Probed(probeOverlay, tUpdateAndDraw_PerformanceInformation));
        AddLegacyPart(PlayfieldGroup, "JudgementStrings", LegacyOverlayLayer, () => Probed(probeJudgement, DrawJudgementStrings));

        UIDrawable chipFire = LayoutGroup(PlayfieldGroup).AddChild<UIDrawable>(useNewDrumsFire ? new ActPerfNewFire(0) : new ClassicDrumsChipFire());
        chipFire.name = "drumChipsFire";
        chipFire.renderOrder = EffectsLayer;

        actChipFireD = (IPerfFire)chipFire;
        actChipFireD.iPosY = CDTXMania.ConfigIni.bReverse.Drums ? nJudgeLinePosY.Drums - 183 : nJudgeLinePosY.Drums - 186;

        actJudgeString.InitUI(LayoutGroup(PlayfieldGroup));
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
                r空打ちドラムチップ[ i ] = timeline.NearestDrum(CDTXMania.Timer.nCurrentTime + nInputAdjustTimeMs.Drums, DrumsPerformance.PadChannels[i], HitState.DontCare);
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
    private static readonly int probeLanes = AllocationProbe.Register("lanes");
    private static readonly int probeChips = AllocationProbe.Register("chips");
    private static readonly int probeScore = AllocationProbe.Register("score");
    private static readonly int probeCombo = AllocationProbe.Register("combo");
    private static readonly int probeGraph = AllocationProbe.Register("graph");
    private static readonly int probeJudgement = AllocationProbe.Register("judgement string");
    private static readonly int probeOverlay = AllocationProbe.Register("debug overlay");
    private static readonly int probeKeyInput = AllocationProbe.Register("key input");

    private static void Probed(int probe, Action draw)
    {
        AllocationProbe.Begin(probe);
        draw();
        AllocationProbe.End(probe);
    }

    private void DrawJudgementStrings()
    {
        tUpdateAndDraw_JudgementString1_ForNormalPosition();
        tUpdateAndDraw_JudgementString2_ForPositionOnJudgementLine();
    }

    public override void OnUpdate()
    {
        if (!bActivated) return;

        bExc = false;
        bFullCom = false;

        if (CDTXMania.ConfigIni.bSTAGEFAILEDEnabled && !bIsTrainingMode && DrumsData.IsFailed()
            && ePhaseID == EPhase.Common_DefaultState)
        {
            actStageFailed.Start();
            CDTXMania.DTX.tStopPlayingAllChips();
            ePhaseID = EPhase.PERFORMANCE_STAGE_FAILED;
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
        if (bIsFinishedPlaying && ePhaseID == EPhase.Common_DefaultState && previewMode)
        {
            tRestartForPreview();
        }
        else if (bIsFinishedPlaying && (ePhaseID == EPhase.Common_DefaultState))
        {
            if ((DrumsData.IsFailed()) && (ePhaseID == EPhase.Common_DefaultState))
            {
                actStageFailed.Start();
                CDTXMania.DTX.tStopPlayingAllChips();
                ePhaseID = EPhase.PERFORMANCE_STAGE_FAILED;
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
    public double UnitTime;
    private BaseTexture txLaneCover;
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
                    actLaneFlushD.Start((ELane)e.Lane, e.Velocity);
                    StartChipFire(e.Lane, e.Chip);
                    actPad.Hit(e.Pad);
                    sequencer.PlaySound(e.Chip, AudioMixer.Timer.nResetAtMs + e.Time, EInstrumentPart.DRUMS, CDTXMania.DTX.nモニタを考慮した音量(EInstrumentPart.DRUMS));
                    break;

                case EDrumsEvent.Hit:
                    actLaneFlushD.Start((ELane)e.Lane, e.Velocity);
                    actPad.Hit(e.Pad);
                    if (e.Fire)
                    {
                        StartChipFire(e.Lane, e.Chip);
                    }
                    PlayHitSound(e.Sound);
                    break;

                case EDrumsEvent.EmptyHit:
                    actLaneFlushD.Start((ELane)e.Lane, e.Velocity);
                    actPad.Hit(e.Pad);
                    PlayHitSound(e.Sound);
                    if (e.Miss)
                    {
                        actJudgeString.Start(e.Lane, e.Auto ? EJudgement.Auto : EJudgement.Miss, 999);
                    }
                    break;

                case EDrumsEvent.Bonus:
                    actPad.ShowBonus();
                    if (CDTXMania.ConfigIni.DisplayBonusEffects)
                    {
                        CDTXMania.Skin.soundAudience.tPlay();
                    }
                    break;

                case EDrumsEvent.Judged:
                    actJudgeString.Start(e.Lane, e.Auto ? EJudgement.Auto : e.Judgement, e.Chip.nLag);
                    bool hit = e.Judgement is EJudgement.Perfect or EJudgement.Great or EJudgement.Good;
                    if (!e.Auto && hit)
                    {
                        actProgressBar.Hit(EInstrumentPart.DRUMS, e.Chip.nPlaybackTimeMs, e.Judgement);
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
        actChipFireD.Start((ELane)lane, bInFillIn, lastInFillIn, lastInFillIn, nJudgeLinePosY_delta.Drums);
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