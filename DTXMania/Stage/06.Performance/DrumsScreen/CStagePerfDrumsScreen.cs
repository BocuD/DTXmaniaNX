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
        listChildActivities.Add(actCombo = new CActPerfDrumsComboDGB(playerData));

        listChildActivities.Add(actGraph = new CActPerfSkillMeter());
        listChildActivities.Add(actJudgeString = new CActPerfDrumsJudgementString());
        listChildActivities.Add(actLaneFlushD = new CActPerfDrumsLaneFlushD());
        listChildActivities.Add(actScore = new CActPerfDrumsScore(playerData));
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
        if (bActivated)
        {
            bChorusSection = false;
        }

        base.OnManagedCreateResources();
    }

    public override void FirstUpdate()
    {
        base.FirstUpdate();

        ctBPMBar = new CCounter(1, 14, (int)(60.0 / actPlayInfo.dbBPM / 14.0 * 1000.0), CDTXMania.Timer);
        ct登場用.tUpdate();
    }

    protected override void AwardClearBonus() => AddClearBonus(EInstrumentPart.DRUMS);

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
    public bool bInFillIn;
    public bool bEndFillIn;
    public bool bChorusSection;
    //-----------------
    protected override void UpdateGraph(EPlayer player)
    {
        actGraph.dbグラフ値現在_渡 = DrumsData.CalculateSkill();
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
                        actCombo.tComboAnime(EPlayer.Drums);
                    }
                    UpdateGraph(EPlayer.Drums);
                    break;
            }
        }
        drums.Events.Clear();
    }

    private void StartChipFire(int lane, CChip chip)
    {
        bool lastInFillIn = bInFillIn && bフィルイン区間の最後のChipである(chip);
        playfield.StartFire((ELane)lane, bInFillIn, lastInFillIn);
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

    #endregion
}