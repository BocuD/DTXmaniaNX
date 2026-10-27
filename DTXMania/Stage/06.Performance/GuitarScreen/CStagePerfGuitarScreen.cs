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
		listChildActivities.Add(actGuitarBonus = new CActPerfGuitarBonus());
		listChildActivities.Add( actScrollSpeed = new CActPerfScrollSpeed() );
		listChildActivities.Add( actWailingBonus = new CActPerfGuitarWailingBonus() );
		listChildActivities.Add( actScore = new CActPerfGuitarScore(partData) );
		listChildActivities.Add( actLaneFlushGB = new CActPerfGuitarLaneFlushGB() );
		listChildActivities.Add( actJudgeString = new CActPerfGuitarJudgementString() );
		listChildActivities.Add( actCombo = new CActPerfGuitarCombo(partData) );
		
		listChildActivities.Add( actPlayInfo = new CActPerformanceInformation() );
		listChildActivities.Add( actProgressBar = new CActPerfProgressBar());
	}

	public override void OnLayoutReady()
	{
		base.OnLayoutReady();
		
		actChipFireGB = new ActPerfNewFire[2];
		wailingEffect = new WailingEffect[2];
		holdNotes = new HoldNote[2, 5];
		SetUpGuitarSection(EInstrument.Guitar1, EInstrumentPart.GUITAR);
		SetUpGuitarSection(EInstrument.Guitar2, EInstrumentPart.BASS);

		UIGroup hud = LayoutGroup(HudGroup);
		AddLegacyPart(hud, "Graph", LegacyOverlayLayer, _ => tUpdateAndDraw_Graph());
		AddLegacyPart(hud, "PerformanceInformation", LegacyOverlayLayer, _ => tUpdateAndDraw_PerformanceInformation());
	}

	//both sides draw at Guitar1's coordinates. Guitar2's section is 870 to the right
	private void SetUpGuitarSection(EInstrument player, EInstrumentPart part)
	{
		UIGroup section = SectionFor(player);
		bool right = player == EInstrument.Guitar2;
		int side = right ? 1 : 0;

		AddLegacyPart(section, "LaneFlush", LegacyLanesLayer, origin => tUpdateAndDraw_LaneFlushGB(origin, part));
		AddLegacyPart(section, "WailingBonus", LegacyLanesLayer, origin => tUpdateAndDraw_WailingBonus(origin, part));
		AddLegacyPart(section, "Chips", LegacyLanesLayer, origin => DrawChips(origin, part));

		//drawn over the judge lines and shutters
		int lane = right ? 14 : 13;
		AddLegacyPart(section, "JudgementStrings", LegacyOverlayLayer, _ => actJudgeString.DrawLanes(lane, lane));
		AddLegacyPart(section, "ProgressBar", LegacyOverlayLayer, origin => actProgressBar.Draw(origin, part, part));

		//the right side's score, combo and bonus sit beside its panel, not 870 to the right
		AddLegacyPart(section, "GuitarBonus", LegacyOverlayLayer, origin => tUpdateAndDraw_GuitarBonus(origin, part)).position.X = right ? -318 : 0;
		AddLegacyPart(section, "Score", ScoreLayer, origin => DrawScore(origin, part)).position.X = right ? -578 : 0;
		AddLegacyPart(section, "Combo", ScoreLayer, origin => DrawCombo(origin, (int)part)).position.X = right ? -585 : 0;

		actJudgeString.InitUI(section, lane, lane, section.position.X);

		actChipFireGB[side] = section.AddChild(new ActPerfNewFire(side + 1));
		actChipFireGB[side].name = "ChipFire";
		actChipFireGB[side].renderOrder = EffectsLayer;

		UIGroup holdParent = section.AddChild(new UIGroup("HoldNotes"));
		holdParent.renderOrder = 10;
		holdParent.dontSerialize = true;
		holdParent.isVisible = false;

		int[] holdX = [107, 146, 185, 224, 264];
		float holdY = CDTXMania.ConfigIni.bReverse[(int)part]
			? 611 - CDTXMania.ConfigIni.nJudgeLine[(int)part]
			: 155 + CDTXMania.ConfigIni.nJudgeLine[(int)part];
		for (int i = 0; i < 5; i++)
		{
			holdNotes[side, i] = holdParent.AddChild(new HoldNote());
			holdNotes[side, i].position = new Vector3(holdX[i], holdY, 0);
		}

		wailingEffect[side] = section.AddChild(new WailingEffect());
		wailingEffect[side].renderOrder = LaneArtLayer;
		wailingEffect[side].position = new Vector3(242, 58, 0);
	}

	private HoldNote[,] holdNotes;

	// メソッド

	// CStage 実装

	public override void OnActivate()
	{
		int nGraphUsePart = CDTXMania.ConfigIni.bGraph有効.Guitar ? 1 : 2;
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
			txChip = BaseTexture.LoadFromPath( CSkin.Path( @"Graphics\7_Chips_Guitar.png" ) );

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
		AudioMixer.Timer.tReset();
		CDTXMania.Timer.tReset();
		
		ctChipPatternAnimation.Guitar = new CCounter(0, 0x17, 20, CDTXMania.Timer);
		ctChipPatternAnimation.Bass = new CCounter(0, 0x17, 20, CDTXMania.Timer);
		ctChipPatternAnimation[0] = null;
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

				//Update Guitar score like in PerfDrumsScreen
				int nNumberOfMistakes = GuitarData.HitCountExclAuto.Miss + GuitarData.HitCountExclAuto.Poor;
				if (nNumberOfMistakes == 0)
				{
					{
						int nNumberPerfects = GuitarData.HitCountExclAuto.Perfect;
						if (CDTXMania.ConfigIni.bAllGuitarsAreAutoPlay)
						{
							nNumberPerfects = GuitarData.HitCountIncAuto.Perfect;
						}

						if (nNumberPerfects == CDTXMania.DTX.nVisibleChipsCount.Guitar)

							#region[ エクセ ]

						{
							if (CDTXMania.ConfigIni.nSkillMode == 1)
								GuitarData.AddClearBonus(true);
						}

						#endregion

						else

							#region[ フルコン ]

						{
							if (CDTXMania.ConfigIni.nSkillMode == 1)
								GuitarData.AddClearBonus(false);
						}

						#endregion
					}
				}

				//Repeat for Bass
				nNumberOfMistakes = BassData.HitCountExclAuto.Miss + BassData.HitCountExclAuto.Poor;
				if (nNumberOfMistakes == 0)
				{
					{
						int nNumberPerfects = BassData.HitCountExclAuto.Perfect;
						if (CDTXMania.ConfigIni.bAllBassAreAutoPlay)
						{
							nNumberPerfects = BassData.HitCountIncAuto.Perfect;
						}

						if (nNumberPerfects == CDTXMania.DTX.nVisibleChipsCount.Bass)

							#region[ エクセ ]

						{
							if (CDTXMania.ConfigIni.nSkillMode == 1)
								BassData.AddClearBonus(true);
						}

						#endregion

						else

							#region[ フルコン ]

						{
							if (CDTXMania.ConfigIni.nSkillMode == 1)
								BassData.AddClearBonus(false);
						}

						#endregion
					}
				}

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

		//either part failing fails the stage, as does a chart with nothing to play
		if (CDTXMania.ConfigIni.bSTAGEFAILEDEnabled && !bIsTrainingMode && !StageManager.dropStageChanges && ePhaseID == EPhase.Common_DefaultState
		    && (GuitarData.IsFailed() || BassData.IsFailed()
		        || (!CDTXMania.DTX.bHasChips.Guitar && !CDTXMania.DTX.bHasChips.Bass)))
		{
			actStageFailed.Start();
			CDTXMania.DTX.tStopPlayingAllChips();
			ePhaseID = EPhase.PERFORMANCE_STAGE_FAILED;
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
			for (int inst = 1; inst < 3; ++inst)
			{
				DataFor((EInstrumentPart)inst).ResetForLoop();
			}
		}

		tHandleKeyInput();

		actScore.Update();
	}

	// Other

	#region [ private ]

	protected override void UpdateGraph(EInstrumentPart part)
	{
		PerformanceData data = DataFor(part);
		int index = (int)part;
		actGraph.dbグラフ値現在_渡 = data.CalculateSkill();
		data.SetAchievementRate(bIsTrainingMode ? 0 : actGraph.dbグラフ値現在_渡);

		if (!CDTXMania.ConfigIni.bGraph有効[index])
		{
			return;
		}

		int chips = data.ChartChips;
		if (CDTXMania.listTargetGhsotLag[index] != null && CDTXMania.ConfigIni.eTargetGhost[index] == ETargetGhostData.ONLINE && chips > 0)
		{
			actGraph.dbグラフ値現在_渡 = 100 * (data.HitCountExclAuto.Perfect * 17 + data.HitCountExclAuto.Great * 7 + data.MaxCombo * 3) / (20.0 * chips);
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
	
	protected override void DrawChip_Guitar_Wailing( CConfigIni configIni, ref CDTX dTX, ref CChip pChip, Matrix4x4 origin )
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
						txChip.tDraw2D(origin, drawX, ( y - numA ) + numC, rect );
					}
				}
			}
		}
	}
	protected override void UpdateChip_FillIn( CConfigIni configIni, ref CDTX dTX, ref CChip pChip )
	{
		if ( !pChip.bHit && ( pChip.nDistanceFromBar.Drums < 0 ) )
		{
			pChip.bHit = true;
		}
	}

	protected override void DrawChip_Bass_Wailing( CConfigIni configIni, ref CDTX dTX, ref CChip pChip, Matrix4x4 origin )
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
						txChip.tDraw2D(origin, drawX, (y - numA) + numC, rect);
					}
				}
			}
			
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

	protected override void DrawChip_BarLine( CConfigIni configIni, ref CDTX dTX, ref CChip pChip, EInstrumentPart part, Matrix4x4 origin )
	{
		if ( !pChip.bVisible || !configIni.bGuitarEnabled )
		{
			return;
		}

		int i = (int)part;
		float y = configIni.bReverse[ i ] ? ( ( nJudgeLinePosY[ i ] - pChip.nDistanceFromBar[ i ] ) + 0 ) : ( ( nJudgeLinePosY[ i ] + pChip.nDistanceFromBar[ i ] ) + 9 );
		if ( ( dTX.bHasChips[ i ] && ( y > 104 ) ) && ( ( y < 670 ) && ( txChip != null ) ) )
		{
			if ( configIni.nLaneDisp[ i ] == 0 || configIni.nLaneDisp[ i ] == 1 )
			{
				txChip.tDraw2D(origin, 88, y, new RectangleF(0, 20, 193, 2));
			}
			if ( configIni.bShowPerformanceInformation )
			{
				int n小節番号 = pChip.nPlaybackPosition / 0x180 - 1;
				CDTXMania.actDisplayString.tPrint(origin, 60, (int)y - 16, CCharacterConsole.EFontType.White, n小節番号.ToString());
			}
		}
	}

	protected override void tDraw_LoopLine(CConfigIni configIni, bool bIsEnd, EInstrumentPart part, Matrix4x4 origin)
	{
		if (!configIni.bGuitarEnabled)
		{
			return;
		}

		const double speed = 286;   // BPM150の時の1小節の長さ[dot]
		int i = (int)part;
		double scrollSpeed = (actScrollSpeed.db現在の譜面スクロール速度[i] + 1.0) * 0.5 * 0.5 * 37.5 * speed * 1.52f / 60000.0; //todo: verify if this is the correct approach to fix guitar scroll speed
		int nDistanceFromBar = (int)(((bIsEnd ? LoopEndMs : LoopBeginMs) - AudioMixer.Timer.nCurrentTime) * scrollSpeed);

		int y = configIni.bReverse[i] ? ((nJudgeLinePosY[i] - nDistanceFromBar) + 0) : ((nJudgeLinePosY[i] + nDistanceFromBar) + 9);
		if ((CDTXMania.DTX.bHasChips[i] && (y > 104)) && ((y < 670) && (txChip != null)))
		{
			//Display Loop Begin/Loop End text
			CDTXMania.actDisplayString.tPrint(origin, 60, y - 16, CCharacterConsole.EFontType.White, (bIsEnd ? "End loop" : "Begin loop"));

			if (configIni.nLaneDisp[i] == 0 || configIni.nLaneDisp[i] == 1)
			{
				txChip.tDraw2D(origin, 88, y - 1, new RectangleF(0, 20, 193, 2));
				txChip.tDraw2D(origin, 88, y + 1, new RectangleF(0, 20, 193, 2));
			}
		}
	}
	#endregion
}