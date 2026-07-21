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
		listChildActivities.Add( actDANGER = new CActPerfGuitarDanger() );
		listChildActivities.Add( video = new CActPerfVideo() );
		listChildActivities.Add( actBGA = new CActPerfBGA() );
		listChildActivities.Add( actGraph = new CActPerfSkillMeter() );
		listChildActivities.Add(actGuitarBonus = new CActPerfGuitarBonus());
		listChildActivities.Add( actScrollSpeed = new CActPerfScrollSpeed() );
		listChildActivities.Add( actStatusPanel = new CActPerfGuitarStatusPanel() );
		listChildActivities.Add( actWailingBonus = new CActPerfGuitarWailingBonus() );
		listChildActivities.Add( actScore = new CActPerfGuitarScore() );
		listChildActivities.Add( actRGB = new CActPerfGuitarRGB() );
		listChildActivities.Add( actLaneFlushGB = new CActPerfGuitarLaneFlushGB() );
		listChildActivities.Add( actJudgeString = new CActPerfGuitarJudgementString() );
		listChildActivities.Add( actGauge = new CActPerfGuitarGauge() );
		listChildActivities.Add( actCombo = new CActPerfGuitarCombo() );
		
		listChildActivities.Add( actPlayInfo = new CActPerformanceInformation() );
		listChildActivities.Add( actProgressBar = new CActPerfProgressBar());
	}

	public override void InitializeBaseUI()
	{
		base.InitializeBaseUI();
		
		var txLane = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_Paret_Guitar.png")); 
		var txLaneDark = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_Paret_Guitar_Dark.png"));
		
		if (CDTXMania.DTX.bHasChips.Guitar)
		{
			var guitarLaneTex = ui.AddChild(new UIImage());
			guitarLaneTex.name = "GuitarLane";
			guitarLaneTex.position = new Vector3(67, 42, 0);

			if (CDTXMania.ConfigIni.nLaneDisp.Guitar == 0 || CDTXMania.ConfigIni.nLaneDisp.Guitar == 2)
				guitarLaneTex.SetTexture(txLane);
			else
				guitarLaneTex.SetTexture(txLaneDark);
		}
		if (CDTXMania.DTX.bHasChips.Bass)
		{
			var bassLaneTex = ui.AddChild(new UIImage());
			bassLaneTex.name = "BassLane";
			bassLaneTex.position = new Vector3(937, 42, 0);
	        
			if (CDTXMania.ConfigIni.nLaneDisp.Bass == 0 || CDTXMania.ConfigIni.nLaneDisp.Bass == 2)
				bassLaneTex.SetTexture(txLane);
			else
				bassLaneTex.SetTexture(txLaneDark);
		}
		
		var guitarScreen = ui.AddChild(new LegacyDrawable(DrawGuitarScreen));
		guitarScreen.name = "guitarScreen";
		guitarScreen.renderOrder = 1;

		actJudgeString.InitUI(ui);
		actStatusPanel.InitUI(ui);
		
		actChipFireGB = new ActPerfNewFire[2];
		actChipFireGB[0] = ui.AddChild(new ActPerfNewFire(1));
		actChipFireGB[0].name = "chipsFireGuitar";
		actChipFireGB[0].renderOrder = 2;
		
		actChipFireGB[1] = ui.AddChild(new ActPerfNewFire(2));
		actChipFireGB[1].name = "chipsFireBass";
		actChipFireGB[1].renderOrder = 2;
		
		//set up hold note rendering
		float guitarY = CDTXMania.ConfigIni.bReverse[1]
			? 611 - CDTXMania.ConfigIni.nJudgeLine[1]
			: 155 + CDTXMania.ConfigIni.nJudgeLine[1];
		
		float bassY = CDTXMania.ConfigIni.bReverse[2]
			? 611 - CDTXMania.ConfigIni.nJudgeLine[2]
			: 155 + CDTXMania.ConfigIni.nJudgeLine[2];
		
		Vector2[] guitarPositions =
		[
			new(107, guitarY), 
			new(146, guitarY), 
			new(185, guitarY), 
			new(224, guitarY),
			new(264, guitarY)
		];
                
		Vector2[] bassPositions =
		[
			new(978, bassY),
			new(1017, bassY),
			new(1056, bassY),
			new(1095, bassY),
			new(1134, bassY)
		];

		var holdParent = ui.AddChild(new UIGroup("HoldNotes"));
		holdParent.renderOrder = 10;
		holdParent.isVisible = false;
		holdNotes = new HoldNote[2, 5];
		
		for (int i = 0; i < 5; i++)
		{
			holdNotes[0, i] = holdParent.AddChild(new HoldNote());
			holdNotes[0, i].position = new Vector3(guitarPositions[i], 0);
			holdNotes[1, i] = holdParent.AddChild(new HoldNote());
			holdNotes[1, i].position = new Vector3(bassPositions[i], 0);
		}

		wailingEffect = new WailingEffect[2];
		wailingEffect[0] = ui.AddChild(new WailingEffect());
		wailingEffect[0].position = new Vector3(242, 58, 0);
		
		wailingEffect[1] = ui.AddChild(new WailingEffect());
		wailingEffect[1].position = new Vector3(1111, 58, 0);
	}

	private HoldNote[,] holdNotes;

	// メソッド

	public void tStorePerfResults( out CScoreIni.CPerformanceEntry Drums, out CScoreIni.CPerformanceEntry Guitar, out CScoreIni.CPerformanceEntry Bass, out bool bIsTrainingMode )
	{
		Drums = new CScoreIni.CPerformanceEntry();

		tStorePerfResults_Guitar( out Guitar );
		tStorePerfResultsBass( out Bass );

		bIsTrainingMode = this.bIsTrainingMode;
	}


	// CStage 実装

	public override void OnActivate()
	{
		int nGraphUsePart = CDTXMania.ConfigIni.bGraph有効.Guitar ? 1 : 2;
		ct登場用 = new CCounter(0, 12, 16, CDTXMania.Timer);
		dtLastQueueOperation = DateTime.MinValue;
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
			if (CDTXMania.ConfigIni.eTargetGhost[ nGraphUsePart ] != ETargetGhostData.NONE)
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
			//this.tGenerateBackgroundTexture();
			txChip = BaseTexture.LoadFromPath( CSkin.Path( @"Graphics\7_Chips_Guitar.png" ) );
			txHitBar = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\\ScreenPlayDrums hit-bar.png"));
			//txWailingFrame = BaseTexture.LoadFromPath( CSkin.Path( @"Graphics\ScreenPlay wailing cursor.png" ) );

			txNote = new BaseTexture[5];
			for (int i = 0; i < 5; i++)
			{
				txNote[i] = BaseTexture.LoadFromPath(CSkin.Path($@"Graphics\Note\Guitar\note0{i}.png"));
			}
			
			txHoldNoteBg = new BaseTexture[5];
			for (int i = 0; i < 5; i++)
			{
				txHoldNoteBg[i] = BaseTexture.LoadFromPath(CSkin.Path($@"Graphics\Note\Guitar\lnote{i}1.png"));
			}
			base.OnManagedCreateResources();
		}
	}

	public override void FirstUpdate()
	{
		CSoundManager.rcPerformanceTimer.tReset();
		CDTXMania.Timer.tReset();
		
		ctChipPatternAnimation.Guitar = new CCounter(0, 0x17, 20, CDTXMania.Timer);
		ctChipPatternAnimation.Bass = new CCounter(0, 0x17, 20, CDTXMania.Timer);
		ctChipPatternAnimation[0] = null;
		ctComboTimer = new CCounter(1, 16,
			(int)((60.0 / (CDTXMania.stagePerfGuitarScreen.actPlayInfo.dbBPM) / 16.0 * 1000.0)), CDTXMania.Timer);
		ctWailingChipPatternAnimation = new CCounter(0, 4, 50, CDTXMania.Timer);

		ePhaseID = EPhase.Common_FadeIn;
		
		// display presence now that the initial timer reset has been performed
		tDisplayPresence();
	}

	public override int OnUpdateAndDraw()
	{
		if (!bActivated) return 0;

		base.OnUpdateAndDraw();

		if (bIsFinishedFadeout)
		{
			if (!CDTXMania.Skin.soundStageClear.bIsPlaying)
			{
				Debug.WriteLine("Total OnUpdateAndDraw=" + sw.ElapsedMilliseconds + "ms");

				//Update Guitar score like in PerfDrumsScreen
				int nNumberOfMistakes = nHitCount_ExclAuto.Guitar.Miss + nHitCount_ExclAuto.Guitar.Poor;
				if (nNumberOfMistakes == 0)
				{
					{
						int nNumberPerfects = nHitCount_ExclAuto.Guitar.Perfect;
						if (CDTXMania.ConfigIni.bAllGuitarsAreAutoPlay)
						{
							nNumberPerfects = nHitCount_IncAuto.Guitar.Perfect;
						}

						if (nNumberPerfects == CDTXMania.DTX.nVisibleChipsCount.Guitar)

							#region[ エクセ ]

						{
							if (CDTXMania.ConfigIni.nSkillMode == 1)
								actScore.nCurrentTrueScore.Guitar += 30000;
						}

						#endregion

						else

							#region[ フルコン ]

						{
							if (CDTXMania.ConfigIni.nSkillMode == 1)
								actScore.nCurrentTrueScore.Guitar += 15000;
						}

						#endregion
					}
				}

				//Repeat for Bass
				nNumberOfMistakes = nHitCount_ExclAuto.Bass.Miss + nHitCount_ExclAuto.Bass.Poor;
				if (nNumberOfMistakes == 0)
				{
					{
						int nNumberPerfects = nHitCount_ExclAuto.Bass.Perfect;
						if (CDTXMania.ConfigIni.bAllBassAreAutoPlay)
						{
							nNumberPerfects = nHitCount_IncAuto.Bass.Perfect;
						}

						if (nNumberPerfects == CDTXMania.DTX.nVisibleChipsCount.Bass)

							#region[ エクセ ]

						{
							if (CDTXMania.ConfigIni.nSkillMode == 1)
								actScore.nCurrentTrueScore.Bass += 30000;
						}

						#endregion

						else

							#region[ フルコン ]

						{
							if (CDTXMania.ConfigIni.nSkillMode == 1)
								actScore.nCurrentTrueScore.Bass += 15000;
						}

						#endregion
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

		return 0;
	}

	private void DrawGuitarScreen()
	{
		sw.Start();

		//detect stage failed and switch to it
		if (CDTXMania.ConfigIni.bSTAGEFAILEDEnabled && !bIsTrainingMode && (ePhaseID == EPhase.Common_DefaultState))
		{
			bool bFailedGuitar =
				actGauge.IsFailed(EInstrumentPart
					.GUITAR); // #23630 2011.11.12 yyagi: deleted AutoPlay condition: not to be failed at once
			bool bFailedBass = actGauge.IsFailed(EInstrumentPart.BASS); // #23630
			bool bFailedNoChips =
				(!CDTXMania.DTX.bHasChips.Guitar &&
				 !CDTXMania.DTX.bHasChips.Bass); // #25216 2011.5.21 yyagi add condition
			if (bFailedGuitar || bFailedBass || bFailedNoChips) // #25216 2011.5.21 yyagi: changed codition: && -> ||
			{
				actStageFailed.Start();
				CDTXMania.DTX.tStopPlayingAllChips();
				ePhaseID = EPhase.PERFORMANCE_STAGE_FAILED;
			}
		}

		tUpdateAndDraw_Background();
		tUpdateAndDraw_AVI();
		tUpdateAndDraw_MIDIBGM();

		tUpdateAndDraw_LaneFlushGB();

		tUpdateAndDraw_DANGER();

		tUpdateAndDraw_WailingBonus();
		tUpdateAndDraw_ScrollSpeed();
		tUpdateAndDraw_ChipAnimation();
		tUpdateAndDraw_BarLines(EInstrumentPart.GUITAR);
		tDraw_LoopLines();
		bIsFinishedPlaying = tUpdateAndDraw_Chips(EInstrumentPart.GUITAR);
		tUpdateAndDraw_RGBButton();
		tUpdateAndDraw_GuitarBass_JudgementLine();
		tUpdateAndDraw_JudgementString();
		actProgressBar.OnUpdateAndDraw();
		tUpdateAndDraw_Gauge();
		tUpdateAndDraw_StatusPanel();
		if (CDTXMania.ConfigIni.bShowScore)
			tUpdateAndDraw_Score();

		tUpdateAndDraw_Graph();
		tUpdateAndDraw_Combo();
		tUpdateAndDraw_PerformanceInformation();
		//this.tUpdateAndDraw_WailingFrame();

		tUpdateAndDraw_GuitarBonus();
		tUpdateAndDraw_STAGEFAILED();
		
		//handle end of performance
		bIsFinishedFadeout = tUpdateAndDraw_FadeIn_Out();
		if (bIsFinishedPlaying && (ePhaseID == EPhase.Common_DefaultState))
		{
			eReturnValueAfterFadeOut = EPerfScreenReturnValue.StageClear;
			ePhaseID = EPhase.PERFORMANCE_STAGE_CLEAR;
			tStartResultDelay();
		}
		
		ManageMixerQueue();

		if (LoopEndMs != -1 && CSoundManager.rcPerformanceTimer.nCurrentTime > LoopEndMs)
		{
			Trace.TraceInformation("Reached end of loop");
			tJumpInSong(LoopBeginMs == -1 ? 0 : LoopBeginMs);

			//Reset hit counts and scores, so that the displayed score reflects the looped part only
			for (int inst = 1; inst < 3; ++inst)
			{
				nHitCount_ExclAuto[inst].Perfect = 0;
				nHitCount_ExclAuto[inst].Great = 0;
				nHitCount_ExclAuto[inst].Good = 0;
				nHitCount_ExclAuto[inst].Poor = 0;
				nHitCount_ExclAuto[inst].Miss = 0;
				actCombo.nCurrentCombo[inst] = 0;
				actCombo.nCurrentCombo.HighestValue[inst] = 0;
				actScore.nCurrentTrueScore[inst] = 0;

				//
				nTimingHitCount[inst].nLate = 0;
				nTimingHitCount[inst].nEarly = 0;
			}
		}
		
		// キー入力
		tHandleKeyInput();
		sw.Stop();
	}

	// Other

	#region [ private ]

	protected override EJudgement tProcessChipHit( long nHitTime, CChip pChip, bool bCorrectLane )
	{
		EJudgement eJudgeResult = tProcessChipHit( nHitTime, pChip, EInstrumentPart.GUITAR, bCorrectLane );
		if (pChip.eInstrumentPart == EInstrumentPart.GUITAR)
		{
			if (CDTXMania.ConfigIni.nSkillMode == 0)
				actGraph.dbグラフ値現在_渡 = CScoreIni.tCalculatePlayingSkillOld(CDTXMania.DTX.nVisibleChipsCount.Guitar, nHitCount_ExclAuto.Guitar.Perfect, nHitCount_ExclAuto.Guitar.Great, nHitCount_ExclAuto.Guitar.Good, nHitCount_ExclAuto.Guitar.Poor, nHitCount_ExclAuto.Guitar.Miss, actCombo.nCurrentCombo.HighestValue.Guitar, EInstrumentPart.GUITAR, bIsAutoPlay);
			else
				actGraph.dbグラフ値現在_渡 = CScoreIni.tCalculatePlayingSkill(CDTXMania.DTX.nVisibleChipsCount.Guitar, nHitCount_ExclAuto.Guitar.Perfect, nHitCount_ExclAuto.Guitar.Great, nHitCount_ExclAuto.Guitar.Good, nHitCount_ExclAuto.Guitar.Poor, nHitCount_ExclAuto.Guitar.Miss, actCombo.nCurrentCombo.HighestValue.Guitar, EInstrumentPart.GUITAR, bIsAutoPlay);
			actStatusPanel.db現在の達成率.Guitar = actGraph.dbグラフ値現在_渡;

			if (CDTXMania.ConfigIni.bGraph有効.Guitar)
			{

				if (CDTXMania.listTargetGhsotLag.Guitar != null &&
				    CDTXMania.ConfigIni.eTargetGhost.Guitar == ETargetGhostData.ONLINE &&
				    CDTXMania.DTX.nVisibleChipsCount.Guitar > 0)
				{

					actGraph.dbグラフ値現在_渡 = 100 *
						(nHitCount_ExclAuto.Guitar.Perfect * 17 +
						 nHitCount_ExclAuto.Guitar.Great * 7 +
						 actCombo.nCurrentCombo.HighestValue.Guitar * 3) / (20.0 * CDTXMania.DTX.nVisibleChipsCount.Guitar);
				}

				actGraph.n現在のAutoを含まない判定数_渡[0] = nHitCount_ExclAuto.Guitar.Perfect;
				actGraph.n現在のAutoを含まない判定数_渡[1] = nHitCount_ExclAuto.Guitar.Great;
				actGraph.n現在のAutoを含まない判定数_渡[2] = nHitCount_ExclAuto.Guitar.Good;
				actGraph.n現在のAutoを含まない判定数_渡[3] = nHitCount_ExclAuto.Guitar.Poor;
				actGraph.n現在のAutoを含まない判定数_渡[4] = nHitCount_ExclAuto.Guitar.Miss;
			}
		}
		else if (pChip.eInstrumentPart == EInstrumentPart.BASS)
		{
			if (CDTXMania.ConfigIni.nSkillMode == 0)
				actGraph.dbグラフ値現在_渡 = CScoreIni.tCalculatePlayingSkillOld(CDTXMania.DTX.nVisibleChipsCount.Bass, nHitCount_ExclAuto.Bass.Perfect, nHitCount_ExclAuto.Bass.Great, nHitCount_ExclAuto.Bass.Good, nHitCount_ExclAuto.Bass.Poor, nHitCount_ExclAuto.Bass.Miss, actCombo.nCurrentCombo.HighestValue.Bass, EInstrumentPart.BASS, bIsAutoPlay);
			else
				actGraph.dbグラフ値現在_渡 = CScoreIni.tCalculatePlayingSkill(CDTXMania.DTX.nVisibleChipsCount.Bass, nHitCount_ExclAuto.Bass.Perfect, nHitCount_ExclAuto.Bass.Great, nHitCount_ExclAuto.Bass.Good, nHitCount_ExclAuto.Bass.Poor, nHitCount_ExclAuto.Bass.Miss, actCombo.nCurrentCombo.HighestValue.Bass, EInstrumentPart.BASS, bIsAutoPlay);
			actStatusPanel.db現在の達成率.Bass = actGraph.dbグラフ値現在_渡;

			if (CDTXMania.ConfigIni.bGraph有効.Bass)
			{

				if (CDTXMania.listTargetGhsotLag.Bass != null &&
				    CDTXMania.ConfigIni.eTargetGhost.Bass == ETargetGhostData.ONLINE &&
				    CDTXMania.DTX.nVisibleChipsCount.Bass > 0)
				{

					actGraph.dbグラフ値現在_渡 = 100 *
						(nHitCount_ExclAuto.Bass.Perfect * 17 +
						 nHitCount_ExclAuto.Bass.Great * 7 +
						 actCombo.nCurrentCombo.HighestValue.Bass * 3) / (20.0 * CDTXMania.DTX.nVisibleChipsCount.Bass);
				}

				actGraph.n現在のAutoを含まない判定数_渡[0] = nHitCount_ExclAuto.Bass.Perfect;
				actGraph.n現在のAutoを含まない判定数_渡[1] = nHitCount_ExclAuto.Bass.Great;
				actGraph.n現在のAutoを含まない判定数_渡[2] = nHitCount_ExclAuto.Bass.Good;
				actGraph.n現在のAutoを含まない判定数_渡[3] = nHitCount_ExclAuto.Bass.Poor;
				actGraph.n現在のAutoを含まない判定数_渡[4] = nHitCount_ExclAuto.Bass.Miss;
			}
		}
		return eJudgeResult;
	}

	protected override void tチップのヒット処理_BadならびにTight時のMiss( EInstrumentPart part )
	{
		tチップのヒット処理_BadならびにTight時のMiss( part, 0, EInstrumentPart.GUITAR );
	}
	protected override void tチップのヒット処理_BadならびにTight時のMiss( EInstrumentPart part, int nLane )
	{
		tチップのヒット処理_BadならびにTight時のMiss( part, nLane, EInstrumentPart.GUITAR );
	}

	/*
	protected override void tUpdateAndDraw_AVI()
	{
		base.tUpdateAndDraw_AVI( 0, 0 );
	}
	protected override void t進行描画_BGA()
	{
		base.t進行描画_BGA( 500, 50 );
	}
	 */
	protected override void tUpdateAndDraw_DANGER()			// #23631 2011.4.19 yyagi
	{
		//this.actDANGER.tUpdateAndDraw( false, this.actGauge.db現在のゲージ値.Guitar < 0.3, this.actGauge.db現在のゲージ値.Bass < 0.3 );
		actDANGER.tUpdateAndDraw( false, actGauge.IsDanger(EInstrumentPart.GUITAR), actGauge.IsDanger(EInstrumentPart.BASS) );
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
	private void tUpdateAndDraw_GuitarBass_JudgementLine()  // t進行描画_ギターベース判定ライン	yyagi: ドラム画面とは座標が違うだけですが、まとめづらかったのでそのまま放置してます。
	{
		if ( CDTXMania.ConfigIni.bGuitarEnabled )
		{
			if ( CDTXMania.DTX.bHasChips.Guitar )
			{
				int y = CDTXMania.ConfigIni.bReverse.Guitar ? nJudgeLinePosY.Guitar : nJudgeLinePosY.Guitar - 1;

				if ( txHitBar != null && CDTXMania.ConfigIni.bJudgeLineDisp.Guitar )
					txHitBar.tDraw2D(80, y, new RectangleF( 0, 0, 252, 6 ) );

				if (CDTXMania.ConfigIni.bShowPerformanceInformation)
					actLVFont.tDrawString(310, (CDTXMania.ConfigIni.bReverse.Guitar ? y + 8 : y - 20), CDTXMania.ConfigIni.nJudgeLine.Guitar.ToString());
			}
			if ( CDTXMania.DTX.bHasChips.Bass )
			{
				int y = CDTXMania.ConfigIni.bReverse.Bass ? nJudgeLinePosY.Bass : nJudgeLinePosY.Bass - 1;

				if ( txHitBar != null && CDTXMania.ConfigIni.bJudgeLineDisp.Bass )
					txHitBar.tDraw2D(950, y, new RectangleF(0, 0, 252, 6));

				if (CDTXMania.ConfigIni.bShowPerformanceInformation)
					actLVFont.tDrawString(1180, (CDTXMania.ConfigIni.bReverse.Bass ? y + 8 : y - 20), CDTXMania.ConfigIni.nJudgeLine.Bass.ToString());
			}
		}
	}

	/*
	protected override void t進行描画_パネル文字列()
	{
		base.t進行描画_パネル文字列( 0xb5, 430 );
	}
	 */

	protected override void tUpdateAndDraw_PerformanceInformation()
	{
		base.tUpdateAndDraw_PerformanceInformation(500, 257);
	}

	protected override void tJudgeLineMovingUpandDown()
	{

	}

	protected override void ScrollSpeedUp()
	{
		CDTXMania.ConfigIni.nScrollSpeed.Guitar = Math.Min(CDTXMania.ConfigIni.nScrollSpeed.Guitar + 1, 1999);
	}
	protected override void ScrollSpeedDown()
	{
		CDTXMania.ConfigIni.nScrollSpeed.Guitar = Math.Max(CDTXMania.ConfigIni.nScrollSpeed.Guitar - 1, 0);
	}

	protected override void tHandleInput_Drums()
	{
		// ギタレボモードでは何もしない
	}
	
	protected override void tUpdateAndDraw_Chip_PatternOnly_Drums(CConfigIni configIni, ref CDTX dTX, ref CChip pChip)
	{
		// int indexSevenLanes = this.nチャンネル0Atoレーン07[ pChip.nChannelNumber - 0x11 ];
		if (!pChip.bHit && (pChip.nDistanceFromBar.Drums < 0))
		{
			//pChip.bHit = true;
			//this.tPlaySound(pChip, CSoundManager.rcPerformanceTimer.n前回リセットした時のシステム時刻 + pChip.nPlaybackTimeMs, EInstrumentPart.DRUMS, dTX.nモニタを考慮した音量(EInstrumentPart.DRUMS));
		}
	}
	protected override void tUpdateAndDraw_Chip_Drums(CConfigIni configIni, ref CDTX dTX, ref CChip pChip)
	{
		// int indexSevenLanes = this.nチャンネル0Atoレーン07[ pChip.nChannelNumber - 0x11 ];
		if ( !pChip.bHit && ( pChip.nDistanceFromBar.Drums < 0 ) )
		{
			pChip.bHit = true;
			tPlaySound(pChip, CSoundManager.rcPerformanceTimer.n前回リセットした時のシステム時刻 + pChip.nPlaybackTimeMs, EInstrumentPart.DRUMS, dTX.nモニタを考慮した音量(EInstrumentPart.DRUMS));
		}
	}
	
	protected override void tUpdateAndDraw_Chip_Guitar_Wailing( CConfigIni configIni, ref CDTX dTX, ref CChip pChip )
	{
		if ( configIni.bGuitarEnabled )
		{
			if ( !pChip.bHit && pChip.bVisible )
			{
				int[] y_base = { 154, 611 };			// ドラム画面かギター画面かで変わる値
				int offset = 0;						// ドラム画面かギター画面かで変わる値

				const int WailingWidth = 54;		// 4種全て同じ値
				const int WailingHeight = 68;		// 4種全て同じ値
				const int baseTextureOffsetX = 0;	// ドラム画面かギター画面かで変わる値
				const int baseTextureOffsetY = 22;	// ドラム画面かギター画面かで変わる値
				const int drawX = 287;				// 4種全て異なる値

				const int numA = 34;				// 4種全て同じ値;
				float y = configIni.bReverse.Guitar ? ( y_base[ 1 ] - pChip.nDistanceFromBar.Guitar ) : ( y_base[ 0 ] + pChip.nDistanceFromBar.Guitar );
				float numB = y - offset;				// 4種全て同じ定義
				float numC = 0;						// 4種全て同じ初期値
				const int numD = 709;				// ドラム画面かギター画面かで変わる値
				if ( ( numB < ( numD + numA ) ) && ( numB > -numA ) )	// 以下のロジックは4種全て同じ
				{
					int c = ctWailingChipPatternAnimation.nCurrentValue;
					RectangleF rect = new( baseTextureOffsetX, baseTextureOffsetY, WailingWidth, WailingHeight );
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
					if ( ( rect.Bottom > rect.Top ) && ( txChip != null ) )
					{
						txChip.tDraw2D(drawX, ( y - numA ) + numC, rect );
					}
				}
			}
		}
		base.tUpdateAndDraw_Chip_Guitar_Wailing( configIni, ref dTX, ref pChip );
	}
	protected override void tUpdateAndDraw_Chip_FillIn( CConfigIni configIni, ref CDTX dTX, ref CChip pChip )
	{
		if ( !pChip.bHit && ( pChip.nDistanceFromBar.Drums < 0 ) )
		{
			pChip.bHit = true;
		}
	}
	protected override void tUpdateAndDraw_Chip_Bonus(CConfigIni configIni, ref CDTX dTX, ref CChip pChip)
	{
		if (!pChip.bHit && (pChip.nDistanceFromBar.Drums < 0))
		{
			pChip.bHit = true;
		}
	}

	protected override void tUpdateAndDraw_Chip_Bass_Wailing( CConfigIni configIni, ref CDTX dTX, ref CChip pChip )
	{
		if ( configIni.bGuitarEnabled )
		{
			if ( !pChip.bHit && pChip.bVisible )
			{
				int[] y_base = { 154, 611 };			// ドラム画面かギター画面かで変わる値
				int offset = 0;						// ドラム画面かギター画面かで変わる値

				const int WailingWidth = 54;		// 4種全て同じ値
				const int WailingHeight = 68;		// 4種全て同じ値
				const int baseTextureOffsetX = 0;	// ドラム画面かギター画面かで変わる値
				const int baseTextureOffsetY = 22;	// ドラム画面かギター画面かで変わる値
				const int drawX = 1155;				// 4種全て異なる値

				const int numA = 34;				// 4種全て同じ値
				float y = CDTXMania.ConfigIni.bReverse.Bass ? ( y_base[ 1 ] - pChip.nDistanceFromBar.Bass ) : ( y_base[ 0 ] + pChip.nDistanceFromBar.Bass );
				float numB = y - offset;				// 4種全て同じ定義
				float numC = 0;						// 4種全て同じ初期値
				const int numD = 709;				// ドラム画面かギター画面かで変わる値
				if ( ( numB < ( numD + numA ) ) && ( numB > -numA ) )	// 以下のロジックは4種全て同じ
				{
					int c = ctWailingChipPatternAnimation.nCurrentValue;
					RectangleF rect = new(baseTextureOffsetX, baseTextureOffsetY, WailingWidth, WailingHeight);
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
					if ( ( rect.Bottom > rect.Top ) && ( txChip != null ) )
					{
						txChip.tDraw2D(drawX, (y - numA) + numC, rect);
					}
				}
			}
			
			base.tUpdateAndDraw_Chip_Bass_Wailing( configIni, ref dTX, ref pChip );
		}
	}
	protected override void tUpdateAndDraw_Chip_NoSound_Drums( CConfigIni configIni, ref CDTX dTX, ref CChip pChip )
	{
		if ( !pChip.bHit && ( pChip.nDistanceFromBar.Drums < 0 ) )
		{
			pChip.bHit = true;
		}
	}
	protected override void tUpdateAndDraw_Chip_BarLine( CConfigIni configIni, ref CDTX dTX, ref CChip pChip )
	{
		int n小節番号plus1 = pChip.nPlaybackPosition / 0x180;
		if ( !pChip.bHit && ( pChip.nDistanceFromBar.Drums < 0 ) )
		{
			pChip.bHit = true;
			actPlayInfo.n小節番号 = n小節番号plus1 - 1;
			if ( configIni.bWave再生位置自動調整機能有効 && bIsDirectSound )
			{
				dTX.tAutoCorrectWavPlaybackPosition();
			}
		}
		if ( ( pChip.bVisible && configIni.bGuitarEnabled ))
		{
			float y = CDTXMania.ConfigIni.bReverse.Guitar ? ((nJudgeLinePosY.Guitar - pChip.nDistanceFromBar.Guitar) + 0) : ((nJudgeLinePosY.Guitar + pChip.nDistanceFromBar.Guitar) + 9);
			if ( ( dTX.bHasChips.Guitar && ( y > 104 ) ) && ( ( y < 670 ) && ( txChip != null ) ) )
			{
				if (CDTXMania.ConfigIni.nLaneDisp.Guitar == 0 || CDTXMania.ConfigIni.nLaneDisp.Guitar == 1)
				{
					txChip.tDraw2D(88, y, new RectangleF(0, 20, 193, 2));
				}
				if ( configIni.bShowPerformanceInformation )
				{
					int n小節番号 = n小節番号plus1 - 1;
					CDTXMania.actDisplayString.tPrint(60, (int)y - 16, CCharacterConsole.EFontType.White, n小節番号.ToString());
				}
			}
			y = CDTXMania.ConfigIni.bReverse.Bass ? ((nJudgeLinePosY.Bass - pChip.nDistanceFromBar.Bass) + 0) : ((nJudgeLinePosY.Bass + pChip.nDistanceFromBar.Bass) + 9);
			if ( ( dTX.bHasChips.Bass && ( y > 104 ) ) && ( ( y < 670 ) && ( txChip != null ) ) )
			{
				if ( CDTXMania.ConfigIni.nLaneDisp.Bass == 0 || CDTXMania.ConfigIni.nLaneDisp.Bass == 1 )
				{
					//todo: what the fuck is vcScaleRatio
					//txChip.vcScaleRatio.Y = 1f;
					txChip.tDraw2D(959, y, new RectangleF(0, 20, 193, 2));
				}
					    

				if ( configIni.bShowPerformanceInformation )
				{
					int n小節番号 = n小節番号plus1 - 1;
					CDTXMania.actDisplayString.tPrint(930, (int)y - 16, CCharacterConsole.EFontType.White, n小節番号.ToString());
				}
			}
		}

	}

	protected override void tDraw_LoopLine(CConfigIni configIni, bool bIsEnd)
	{
		const double speed = 286;   // BPM150の時の1小節の長さ[dot]
		double ScrollSpeedGuitar = (actScrollSpeed.db現在の譜面スクロール速度.Guitar + 1.0) * 0.5 * 0.5 * 37.5 * speed * 1.52f / 60000.0; //todo: verify if this is the correct approach to fix guitar scroll speed
		double ScrollSpeedBass = (actScrollSpeed.db現在の譜面スクロール速度.Bass + 1.0) * 0.5 * 0.5 * 37.5 * speed * 1.52f / 60000.0; //todo: verify if this is the correct approach to fix guitar scroll speed

		int nDistanceFromBarGuitar = (int)(((bIsEnd ? LoopEndMs : LoopBeginMs) - CSoundManager.rcPerformanceTimer.nCurrentTime) * ScrollSpeedGuitar);
		int nDistanceFromBarBass = (int)(((bIsEnd ? LoopEndMs : LoopBeginMs) - CSoundManager.rcPerformanceTimer.nCurrentTime) * ScrollSpeedBass);

		if (configIni.bGuitarEnabled)
		{
			int y = CDTXMania.ConfigIni.bReverse.Guitar ? ((nJudgeLinePosY.Guitar - nDistanceFromBarGuitar) + 0) : ((nJudgeLinePosY.Guitar + nDistanceFromBarGuitar) + 9);
			if ((CDTXMania.DTX.bHasChips.Guitar && (y > 104)) && ((y < 670) && (txChip != null)))
			{
				//Display Loop Begin/Loop End text
				CDTXMania.actDisplayString.tPrint(60, y - 16, CCharacterConsole.EFontType.White, (bIsEnd ? "End loop" : "Begin loop"));

				if (CDTXMania.ConfigIni.nLaneDisp.Guitar == 0 || CDTXMania.ConfigIni.nLaneDisp.Guitar == 1)
				{
					//todo: what the fuck is vcScaleRatio
					//txChip.vcScaleRatio.Y = 1.0f;
					txChip.tDraw2D(88, y - 1, new RectangleF(0, 20, 193, 2));
					txChip.tDraw2D(88, y + 1, new RectangleF(0, 20, 193, 2));
				}
			}
			y = CDTXMania.ConfigIni.bReverse.Bass ? ((nJudgeLinePosY.Bass - nDistanceFromBarBass) + 0) : ((nJudgeLinePosY.Bass + nDistanceFromBarBass) + 9);
			if ((CDTXMania.DTX.bHasChips.Bass && (y > 104)) && ((y < 670) && (txChip != null)))
			{
				//Display Loop Begin/Loop End text
				CDTXMania.actDisplayString.tPrint(930, y - 16, CCharacterConsole.EFontType.White, (bIsEnd ? "End loop" : "Begin loop"));

				if (CDTXMania.ConfigIni.nLaneDisp.Bass == 0 || CDTXMania.ConfigIni.nLaneDisp.Bass == 1)
				{
					//todo: what the fuck is vcScaleRatio
					//txChip.vcScaleRatio.Y = 1.0f;
					txChip.tDraw2D(959, y - 1, new RectangleF(0, 20, 193, 2));
					txChip.tDraw2D(959, y + 1, new RectangleF(0, 20, 193, 2));
				}
			}
		}
	}
	#endregion
}