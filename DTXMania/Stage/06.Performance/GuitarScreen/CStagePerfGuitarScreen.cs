using System.Drawing;
using System.Diagnostics;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Drawable;
using DTXMania.UI.Drawable;
using FDK;

namespace DTXMania;

internal partial class CStagePerfGuitarScreen : CStagePerfCommonScreen
{
	// コンストラクタ

	public CStagePerfGuitarScreen()
	{
		eStageID = EStage.Performance_6;
		ePhaseID = EPhase.Common_DefaultState;
		bActivated = false;
		listChildActivities.Add( actStageFailed = new CActPerfStageFailure() );
		listChildActivities.Add( video = new CActPerfVideo() );
		listChildActivities.Add( actBGA = new CActPerfBGA() );
		listChildActivities.Add( actGraph = new CActPerfSkillMeter() );
		listChildActivities.Add( actScrollSpeed = new CActPerfScrollSpeed() );
		listChildActivities.Add( actWailingBonus = new CActPerfGuitarWailingBonus() );
		listChildActivities.Add( actScore = new CActPerfGuitarScore(playerData) );
		listChildActivities.Add( actLaneFlushGB = new CActPerfGuitarLaneFlushGB() );
		listChildActivities.Add( actJudgeString = new CActPerfGuitarJudgementString() );
		listChildActivities.Add( actCombo = new CActPerfGuitarCombo(playerData) );
		
		listChildActivities.Add( actPlayInfo = new CActPerformanceInformation() );
		listChildActivities.Add( actProgressBar = new CActPerfProgressBar());
	}

	public override void OnLayoutReady()
	{
		base.OnLayoutReady();
		
		guitarPlayfields = new GuitarPlayfield[2];
		SetUpGuitarSection(EPlayer.Guitar1);
		SetUpGuitarSection(EPlayer.Guitar2);
		playfields = guitarPlayfields;
		RefreshPlayfields();

		UIGroup hud = LayoutGroup(HudGroup);
		AddLegacyPart(hud, "Graph", LegacyOverlayLayer, _ => tUpdateAndDraw_Graph());
		AddLegacyPart(hud, "PerformanceInformation", LegacyOverlayLayer, _ => tUpdateAndDraw_PerformanceInformation());
	}

	private void SetUpGuitarSection(EPlayer player)
	{
		GuitarPlayfield playfield = FindGuitarPlayfield(player);
		bool right = player == EPlayer.Guitar2;
		int side = right ? 1 : 0;

		playfield.SetParts(actScore, actCombo, actProgressBar, actJudgeString);
		playfield.Setup(actLaneFlushGB, actWailingBonus);
		playfield.SetChart(timeline);

		guitarPlayfields[side] = playfield;
	}


	// メソッド

	// CStage 実装

	public override void OnActivate()
	{
		EPlayer graphPlayer = CDTXMania.ConfigIni.bGraph有効.Guitar ? EPlayer.Guitar1 : EPlayer.Guitar2;
		int nGraphUsePart = (int)PartOf(graphPlayer);
		ct登場用 = new CCounter(0, 12, 16, CDTXMania.Timer);
		if ( CDTXMania.bCompactMode )
		{
			var score = new CChartData();
			actGraph.dbGraphValue_Goal = score.SongInformation.HighCompletionRate[ nGraphUsePart ];
		}
		else
		{
			actGraph.dbGraphValue_Goal = CDTXMania.chosenChartData.SongInformation.HighCompletionRate[ nGraphUsePart ];	// #24074 2011.01.23 add ikanick
			actGraph.dbGraphValue_PersonalBest = CDTXMania.chosenChartData.SongInformation.HighCompletionRate[ nGraphUsePart ];

			// #35411 2015.08.21 chnmr0 add
			// ゴースト利用可のなとき、0で初期化
			if (CDTXMania.ConfigIni.eTargetGhost[ (int)graphPlayer ] != ETargetGhostData.NONE)
			{
				if (CDTXMania.listTargetGhsotLag[ nGraphUsePart ] != null)
				{
					actGraph.dbGraphValue_Goal = 0;
				}
			}
		}
		base.OnActivate();
	}
	public override void OnManagedCreateResources()
	{
		if ( bActivated )
		{
			base.OnManagedCreateResources();
		}
	}

	public override void FirstUpdate()
	{
		AudioMixer.Timer.tReset();
		CDTXMania.Timer.tReset();
		
		ctComboTimer = new CCounter(1, 16,
			(int)((60.0 / (CDTXMania.stagePerfGuitarScreen.actPlayInfo.dbBPM) / 16.0 * 1000.0)), CDTXMania.Timer);
		ctWailingChipPatternAnimation = new CCounter(0, 4, 50, CDTXMania.Timer);

		ePhaseID = EPhase.Common_DefaultState;
		
		// display presence now that the initial timer reset has been performed
		tDisplayPresence();
	}

	public override int OnUpdateAndDraw()
	{
		if (!bActivated) return 0;

		base.OnUpdateAndDraw();

		if (CanLeave)
		{
			if (!CDTXMania.Skin.soundStageClear.bIsPlaying)
			{

				AddClearBonus(EInstrumentPart.GUITAR);
				AddClearBonus(EInstrumentPart.BASS);

				return (int)eReturnValue;
			}
		}

		if (ePhaseID == EPhase.PERFORMANCE_STAGE_RESTART)
		{
			Debug.WriteLine("Restarting");
			return (int)eReturnValue;
		}

		return 0;
	}

	public override void OnUpdate()
	{
		if (!bActivated) return;

		//either player failing fails the stage, as does a chart with nothing to play
		if (CDTXMania.ConfigIni.bSTAGEFAILEDEnabled && !bIsTrainingMode && !StageManager.dropStageChanges && ePhaseID == EPhase.Common_DefaultState
		    && (DataFor(EPlayer.Guitar1).IsFailed() || DataFor(EPlayer.Guitar2).IsFailed()
		        || (!CDTXMania.DTX.bHasChips.Guitar && !CDTXMania.DTX.bHasChips.Bass)))
		{
			FailStage();
		}

		UpdateBackground();
		UpdateVideo();
		UpdateScrollSpeed();
		UpdateChipAnimation();
		bIsFinishedPlaying = UpdateChips();
		UpdateStageFailed();

		//loop instead of going to the result screen
		if (bIsFinishedPlaying && ePhaseID == EPhase.Common_DefaultState && StageManager.dropStageChanges)
		{
			Restart();
		}
		else if (bIsFinishedPlaying && (ePhaseID == EPhase.Common_DefaultState))
		{
			eReturnValue = EPerfScreenReturnValue.StageClear;
			ePhaseID = EPhase.PERFORMANCE_STAGE_CLEAR;
			tStartResultDelay();
		}

		UpdateResultDelay();

		if (LoopEndMs != -1 && AudioMixer.Timer.nCurrentTime > LoopEndMs)
		{
			Trace.TraceInformation("Reached end of loop");
			tJumpInSong(LoopBeginMs == -1 ? 0 : LoopBeginMs);

			//Reset hit counts and scores, so that the displayed score reflects the looped part only
			for (EPlayer player = EPlayer.Guitar1; player <= EPlayer.Guitar2; player++)
			{
				DataFor(player).ResetForLoop();
			}
		}

		tHandleKeyInput();

		actScore.Update();

		UpdateLoopMarkers();
	}

	// Other

	#region [ private ]

	private void AddClearBonus(EInstrumentPart part)
	{
		if (CDTXMania.ConfigIni.nSkillMode != 1)
		{
			return;
		}

		PerformanceData data = DataFor(PlayerOn(part));
		if (data.HitCountExclAuto.Miss + data.HitCountExclAuto.Poor > 0)
		{
			return;
		}

		bool allAuto = CDTXMania.ConfigIni.bIsAutoPlay(data.Player);
		int perfects = allAuto ? data.HitCountIncAuto.Perfect : data.HitCountExclAuto.Perfect;
		data.AddClearBonus(perfects == data.ChartChips);
	}

	protected override void UpdateGraph(EPlayer player)
	{
		PerformanceData data = DataFor(player);
		int slot = (int)player;
		actGraph.dbグラフ値現在_渡 = data.CalculateSkill();

		if (!CDTXMania.ConfigIni.bGraph有効[slot])
		{
			return;
		}

		for (int i = 0; i < 5; i++)
		{
			actGraph.n現在のAutoを含まない判定数_渡[i] = data.HitCountExclAuto[i];
		}
	}


	private void tUpdateAndDraw_Graph()  // t進行描画_グラフ
	{
		if ( !CDTXMania.ConfigIni.bストイックモード && ( CDTXMania.ConfigIni.bGraph有効.Guitar || CDTXMania.ConfigIni.bGraph有効.Bass ) )
		{
			actGraph.OnUpdateAndDraw();
		}
	}
	protected override void tUpdateAndDraw_WailingFrame()
	{
		base.tUpdateAndDraw_WailingFrame( 292, 0x251,
			CDTXMania.ConfigIni.bReverse.Guitar ? 340 : 130,
			CDTXMania.ConfigIni.bReverse.Bass ?   340 : 130
		);
	}


	protected override void tUpdateAndDraw_PerformanceInformation()
	{
		base.tUpdateAndDraw_PerformanceInformation(500, 257);
	}

	protected override void tJudgeLineMovingUpandDown()
	{

	}

	protected override void tHandleInput_Drums()
	{
		// ギタレボモードでは何もしない
	}
	
	protected override void UpdateChip_FillIn( CConfigIni configIni, ref CDTX dTX, ref CChip pChip )
	{
		if ( !pChip.bHit && ( pChip.nDistanceFromBar.Drums < 0 ) )
		{
			pChip.bHit = true;
		}
	}

	protected override void UpdateChip_BarLine( CConfigIni configIni, ref CDTX dTX, ref CChip pChip )
	{
		int n小節番号plus1 = pChip.nPlaybackPosition / 0x180;
		if ( !pChip.bHit && ( pChip.nDistanceFromBar.Drums < 0 ) )
		{
			pChip.bHit = true;
			actPlayInfo.n小節番号 = n小節番号plus1 - 1;
			if ( configIni.bWave再生位置自動調整機能有効 && bNeedsDriftCorrection )
			{
				dTX.tAutoCorrectWavPlaybackPosition();
			}
		}
	}
	#endregion
}