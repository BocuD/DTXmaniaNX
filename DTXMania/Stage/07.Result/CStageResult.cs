using System.Diagnostics;
using DTXMania.UI.Skin;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI;
using DTXMania.UI.Animation;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Text;
using FDK;
using SlimDXKey = SlimDX.DirectInput.Key;

namespace DTXMania;

internal class CStageResult : CStage
{
	private readonly ResultData resultData = new();

	public STDGBVALUE<bool> bNewRecordSkill;
	public STDGBVALUE<bool> bNewRecordScore;
	public STDGBVALUE<bool> bNewRecordRank;
	public STDGBVALUE<float> fPerfectPercentage;
	public STDGBVALUE<float> fGreatPercentage;
	public STDGBVALUE<float> fGoodPercentage;
	public STDGBVALUE<float> fPoorPercentage;
	public STDGBVALUE<float> fMissPercentage;
	public STDGBVALUE<bool> bAuto;        // #23596 10.11.16 add ikanick
	//        10.11.17 change (int to bool) ikanick
	public STDGBVALUE<int> nRankValue;
	public int nResultRank;
	public CChip[] rEmptyDrumChip;

	//refilled per pad rather than allocated per pad
	private readonly List<STInputEvent> listPadEvents = [];
	public STDGBVALUE<CScoreIni.CPerformanceEntry> stPerformanceEntry;
	public bool bIsTrainingMode;

	//Progress Bar temp variables
	public STDGBVALUE<string> strBestProgressBarRecord;
	public STDGBVALUE<string> strCurrProgressBarRecord;

	// コンストラクタ

	public CStageResult()
	{
		stPerformanceEntry.Drums = new CScoreIni.CPerformanceEntry();
		stPerformanceEntry.Guitar = new CScoreIni.CPerformanceEntry();
		stPerformanceEntry.Bass = new CScoreIni.CPerformanceEntry();
		rEmptyDrumChip = new CChip[ 10 ];
		nResultRank = -1;
		nチャンネル0Atoレーン07 = [1, 2, 3, 4, 5, 7, 6, 1, 8, 0, 9];
		eStageID = EStage.Result_7;
		ePhaseID = EPhase.Common_DefaultState;
		bActivated = false;
		//listChildActivities.Add( actResultImage = new CActResultImage(this) );
	}

		
	// CStage 実装

	public override void RegisterBindings()
	{
		var context = new UIDataContext();
		context.RegisterObject("Result", () => resultData);
		ui.dataContext = context;
	}

	public override void BuildDefaultLayout()
	{
		var stageNumber = ui.AddChild(new UIText("", 46));
		stageNumber.name = "StageNumber";
		stageNumber.bindings.Add(new UIBinding("text", "Result.StageNumber"));
		stageNumber.position = new Vector3(640, 50, 0);
		stageNumber.pivot = new Vector2(0.5f, 0);
		stageNumber.font = SkinResource.System("Futura PT Book.otf");
		stageNumber.style = UiTextStyle.Bold;
		stageNumber.outlineWidth = 0;

		var titleArtistBg = ui.AddChild(new UIImage
		{
			imageSource = ImageSource.File,
			image = SkinResource.System(@"Graphics\Result\songname_bg.png")
		});
		titleArtistBg.pivot = new Vector2(0.5f, 0);
		titleArtistBg.position = new Vector3(640, 529, 0);
		titleArtistBg.renderOrder = 1;
		titleArtistBg.name = "TitleArtistBg";

		HorizontallyScrollingText songNameText = ui.AddChild(new HorizontallyScrollingText("", 29));
		songNameText.bindings.Add(new UIBinding("text", "Result.SongTitle"));
		songNameText.fillColor = Color4.Black;
		songNameText.outlineColor = Color4.White;
		songNameText.name = "SongName";
		songNameText.font = SkinResource.System(UIFonts.FallbackFont);
		songNameText.position = new Vector3(464, 547, 0);
		songNameText.outlineWidth = 2;
		songNameText.renderOrder = 2;
		songNameText.scrollingEnabled = true;
		songNameText.size.X = 355;
		songNameText.scrollSpeed = 20.0f;

		HorizontallyScrollingText artistNameText = ui.AddChild(new HorizontallyScrollingText("", 20));
		artistNameText.bindings.Add(new UIBinding("text", "Result.Artist"));
		artistNameText.fillColor = Color4.Black;
		artistNameText.outlineColor = Color4.White;
		artistNameText.name = "ArtistName";
		artistNameText.font = SkinResource.System(UIFonts.FallbackFont);
		artistNameText.position = new Vector3(466, 589, 0);
		artistNameText.outlineWidth = 2;
		artistNameText.renderOrder = 2;
		artistNameText.scrollingEnabled = true;
		artistNameText.size.X = 355;
		artistNameText.scrollSpeed = 20.0f;
		
	}

	//elements that build runtime textures from the result data (rank icon, jacket, progress bar) can't be
	//part of the serializable layout, so they are added here and marked dontSerialize. The open animation
	//is set up here too, once the panels it targets exist
	public override void OnLayoutReady()
	{
		background = ui.AddChild(new UIImage(BaseTexture.LoadFromPath(ResultBackgroundPath())));
		background.renderOrder = -100;
		background.name = "Background";
		background.dontSerialize = true;

		var rankIcon = ui.AddChild(new ResultRankIcon(CDTXMania.GetCurrentInstrument()));
		rankIcon.position = new Vector3(225, 360, 0);
		rankIcon.renderOrder = 3;
		rankIcon.dontSerialize = true;

		string path = CDTXMania.DTX.strFolderName + CDTXMania.DTX.PREIMAGE;
		var txJacket = BaseTexture.LoadFromPath(!File.Exists(path) ? CSkin.Path(@"Graphics\5_preimage default.png") : path);
		var jacket = ui.AddChild(new UIImage(txJacket));
		jacket.size = new Vector2(380, 380);
		jacket.position = new Vector3(640, 130, 0);
		jacket.name = "AlbumArt";
		jacket.pivot.X = 0.5f;
		jacket.dontSerialize = true;

		//todo: position these
		if (CDTXMania.GetCurrentInstrument() == 0)
		{
			var drums = ui.AddChild(new UIPlayerNameplate(0, true));
			drums.position = new Vector3(989, 53, 0);
			drums.dontSerialize = true;
		}
		else
		{
			var guitar1 = ui.AddChild(new UIPlayerNameplate(1, true));
			guitar1.position = new Vector3(989, 53, 0);
			guitar1.dontSerialize = true;
		}

		var infoPanel = ui.AddChild(new ResultInfoPanel());
		infoPanel.position = new Vector3(830, 120, 0);
		infoPanel.dontSerialize = true;

		var paramPanel = ui.AddChild(new ResultParameterPanel(CDTXMania.GetCurrentInstrument()));
		paramPanel.position = new Vector3(879, 479, 0);
		paramPanel.dontSerialize = true;

		var progressBar = ui.AddChild(new ResultProgressBar(CDTXMania.GetCurrentInstrument()));
		progressBar.position = new Vector3(435, 130, 0);
		progressBar.renderOrder = 4;
		progressBar.dontSerialize = true;

		//the clip lives in its own file, so a saved layout references it rather than copying it in
		ui.animator = new Animator();
		ui.animator.AddResource(SkinResource.System(@"Graphics\Result\open.json"));
		ui.animator.Play("open", false);
	}

	//an optional per-rank background overrides the default one; rank 99 (unknown) shares E's
	private string ResultBackgroundPath()
	{
		string[] rankNames = ["SS", "S", "A", "B", "C", "D", "E"];
		int rank = nResultRank == 99 ? rankNames.Length - 1 : nResultRank;

		if (rank >= 0 && rank < rankNames.Length)
		{
			string rankPath = CSkin.Path($@"Graphics\8_background rank{rankNames[rank]}.png");
			if (File.Exists(rankPath))
			{
				return rankPath;
			}
		}

		return CSkin.Path(@"Graphics\8_background.jpg");
	}

	public override void OnActivate()
	{
		Trace.TraceInformation( "結果ステージを活性化します。" );
		Trace.Indent();
		try
		{
			#region [ Initialize ]
			//---------------------
			eReturnValueWhenFadeOutCompleted = EReturnValue.Continue;
			bAnimationComplete = false;
			bIsCheckedWhetherResultScreenShouldSaveOrNot = false;				// #24609 2011.3.14 yyagi
			n最後に再生したHHのWAV番号 = -1;
			n最後に再生したHHのチャンネル番号 = 0;
			for( int i = 0; i < 3; i++ )
			{
				bNewRecordSkill[i] = false;
				bNewRecordScore[i] = false;
				bNewRecordRank[i] = false;
				
				//Initialize to empty string so that the Progress Bar texture can be drawn correctly
				strBestProgressBarRecord[i] = "";
				strCurrProgressBarRecord[i] = "";
			}
			//---------------------
			#endregion

			if (CDTXMania.ConfigIni.bScoreIniを出力する && !bIsTrainingMode && (CDTXMania.ConfigIni.bSaveScoreIfModifiedPlaySpeed || CDTXMania.ConfigIni.nPlaySpeed == 20))
			{
				#region [ Calculate results ]
				//---------------------
				for (int i = 0; i < 3; i++)
				{
					nRankValue[i] = -1;
					fPerfectPercentage[i] = fGreatPercentage[i] = fGoodPercentage[i] = fPoorPercentage[i] = fMissPercentage[i] = 0.0f;  // #28500 2011.5.24 yyagi
					if ((i != 0 || (CDTXMania.DTX.bHasChips.Drums && !CDTXMania.ConfigIni.bGuitarRevolutionMode)) &&
					    (i != 1 || (CDTXMania.DTX.bHasChips.Guitar && CDTXMania.ConfigIni.bGuitarRevolutionMode)) &&
					    (i != 2 || (CDTXMania.DTX.bHasChips.Bass && CDTXMania.ConfigIni.bGuitarRevolutionMode)))
					{
						CScoreIni.CPerformanceEntry part = stPerformanceEntry[i];
						bool bIsAutoPlay = true;
						switch (i)
						{
							case 0:
								bIsAutoPlay = CDTXMania.ConfigIni.bAllDrumsAreAutoPlay;
								break;

							case 1:
								bIsAutoPlay = CDTXMania.ConfigIni.bAllGuitarsAreAutoPlay;
								break;

							case 2:
								bIsAutoPlay = CDTXMania.ConfigIni.bAllBassAreAutoPlay;
								break;
						}

						bAuto[i] = bIsAutoPlay;
						fPerfectPercentage[i] = bIsAutoPlay ? 0f : 100f * part.nPerfectCount / part.nTotalChipsCount;
						fGreatPercentage[i] = bIsAutoPlay ? 0f : 100f * part.nGreatCount / part.nTotalChipsCount;
						fGoodPercentage[i] = bIsAutoPlay ? 0f : 100f * part.nGoodCount / part.nTotalChipsCount;
						fPoorPercentage[i] = bIsAutoPlay ? 0f : 100f * part.nPoorCount / part.nTotalChipsCount;
						fMissPercentage[i] = bIsAutoPlay ? 0f : 100f * part.nMissCount / part.nTotalChipsCount;
						
						//Skill mode 1 for XG style, 0 for old style
						if (CDTXMania.ConfigIni.nSkillMode == 1)
						{
							nRankValue[i] = CScoreIni.tCalculateRank(part);
						}
						else if (CDTXMania.ConfigIni.nSkillMode == 0)
						{
							nRankValue[i] = CScoreIni.tCalculateRankOld(part);
						}

						//Save progress bar records
						CChartData cChartData = CDTXMania.chosenChartData;
						strBestProgressBarRecord[i] = cChartData.SongInformation.progress[i];
						
						//May not need to save this...
						strCurrProgressBarRecord[i] = stPerformanceEntry[i].strProgress;
					}
				}
				nResultRank = CScoreIni.tCalculateOverallRankValue(stPerformanceEntry.Drums, stPerformanceEntry.Guitar, stPerformanceEntry.Bass);
				//---------------------
				#endregion

				#region [ Write .score.ini ]
				//---------------------
				string iniPath = CDTXMania.DTX.strFileNameFullPath + ".score.ini";
				CScoreIni ini = new(iniPath);

				bool[] bExistingFullCombo = [false, false, false]; //did we have a full combo before?
				if (!CDTXMania.ConfigIni.bAllDrumsAreAutoPlay || !CDTXMania.ConfigIni.bAllGuitarsAreAutoPlay || !CDTXMania.ConfigIni.bAllBassAreAutoPlay)
				{
					for (int instrument = 0; instrument < 3; instrument++)
					{
						// フルコンボチェックならびに新記録ランクチェックは、ini.Record[] が、スコアチェックや演奏型スキルチェックの IF 内で書き直されてしまうよりも前に行う。(2010.9.10)
						bExistingFullCombo[instrument] = ini.stSection[instrument * 2].bIsFullCombo | ini.stSection[instrument * 2 + 1].bIsFullCombo;

						// #24459 上記の条件だと[HiSkill.***]でのランクしかチェックしていないので、BestRankと比較するよう変更。
						if (nRankValue[instrument] >= 0 && ini.stFile.BestRank[instrument] > nRankValue[instrument])     // #24459 2011.3.1 yyagi update BestRank
						{
							bNewRecordRank[instrument] = true;
							ini.stFile.BestRank[instrument] = nRankValue[instrument];
						}

						//new record score check
						if (stPerformanceEntry[instrument].nScore > ini.stSection[instrument * 2].nScore)
						{
							bNewRecordScore[instrument] = true;
							ini.stSection[instrument * 2] = stPerformanceEntry[instrument];
							SaveGhost(instrument * 2); // #35411 chnmr0 add
						}

						//new record skill check
						if (stPerformanceEntry[instrument].dbPerformanceSkill > ini.stSection[instrument * 2 + 1].dbPerformanceSkill && !bAuto[instrument])
						{
							bNewRecordSkill[instrument] = true;
							ini.stSection[instrument * 2 + 1] = stPerformanceEntry[instrument];
							SaveGhost(instrument * 2 + 1); // #35411 chnmr0 add
						}

						//last play (if using auto play, don't save)
						if (!bAuto[instrument])
						{
							ini.stSection[instrument + 6] = stPerformanceEntry[instrument];
							SaveGhost(instrument + 6); // #35411 chnmr0 add
						}

						// #23596 10.11.16 add ikanick オートじゃないならクリア回数を1増やす
						//        11.02.05 bAuto to tGetIsUpdateNeeded use      ikanick
						STDGBVALUE<bool> isUpdateNeeded = CScoreIni.tGetIsUpdateNeeded();

						//only update clear count if the score is not auto-played
						if (isUpdateNeeded[instrument])
						{
							switch (instrument)
							{
								case 0:
									ini.stFile.ClearCountDrums++;
									break;
								case 1:
									ini.stFile.ClearCountGuitar++;
									break;
								case 2:
									ini.stFile.ClearCountBass++;
									break;
								default:
									throw new Exception("クリア回数増加のk(0-2)が範囲外です。");
							}
						}

						//---------------------------------------------------------------------/
					}

					ini.tExport(iniPath);
				}
				//---------------------
				#endregion
				
				#region [ Update score information on Song Selection screen ]
				//---------------------
				if (!CDTXMania.bCompactMode)
				{
					CChartData cChartData = CDTXMania.chosenChartData;
					STDGBVALUE<bool> isUpdateNeeded = CScoreIni.tGetIsUpdateNeeded();
					for (int instrument = 0; instrument < 3; instrument++)
					{
						if (isUpdateNeeded[instrument])
						{
							// FullCombo した記録を FullCombo なしで超えた場合、FullCombo マークが消えてしまう。
							// → FullCombo は、最新記録と関係なく、一度達成したらずっとつくようにする。(2010.9.11)
							cChartData.SongInformation.FullCombo[instrument] = stPerformanceEntry[instrument].bIsFullCombo | bExistingFullCombo[instrument];

							if (bNewRecordSkill[instrument])
							{
								cChartData.SongInformation.HighCompletionRate[instrument] = stPerformanceEntry[instrument].dbPerformanceSkill;
								// New Song Progress for new skill record
								cChartData.SongInformation.progress[instrument] = stPerformanceEntry[instrument].strProgress;
							}

							if (bNewRecordRank[instrument])
							{
								cChartData.SongInformation.BestRank[instrument] = nRankValue[instrument];
							}

							//Check if Progress record existed or not; if not, update anyway
							if (CScoreIni.tProgressBarLength(cChartData.SongInformation.progress[instrument]) == 0)
							{
								cChartData.SongInformation.progress[instrument] = stPerformanceEntry[instrument].strProgress;
							}
						}
					}
				}
				//---------------------
				#endregion
			}

			base.OnActivate();
		}
		finally
		{
			Trace.TraceInformation( "結果ステージの活性化を完了しました。" );
			Trace.Unindent();
		}
	}
	//fork
	// #35411 chnmr0 add
	private void SaveGhost(int sectionIndex)
	{
		//return; //2015.12.31 kairera0467 以下封印

		STDGBVALUE<bool> saveCond = new()
		{
			Drums = true,
			Guitar = true,
			Bass = true
		};

		foreach( CChip chip in CDTXMania.DTX.listChip )
		{
			if ( chip.bIsAutoPlayed )
			{
				if (chip.nChannelNumber != EChannel.Guitar_Wailing && chip.nChannelNumber != EChannel.Bass_Wailing) // Guitar/Bass Wailing は OK
				{
					saveCond[(int)chip.eInstrumentPart] = false;
				}
			}
		}
		for(int instIndex = 0; instIndex < 3; ++instIndex)
		{
			saveCond[instIndex] &= CDTXMania.listAutoGhostLag.Drums == null;
		}

		string directory = CDTXMania.DTX.strFolderName;
		string filename = CDTXMania.DTX.strFileName + ".";
		EInstrumentPart inst = EInstrumentPart.UNKNOWN;

		switch (sectionIndex)
		{
			case 0:
				filename += "hiscore.dr.ghost";
				inst = EInstrumentPart.DRUMS;
				break;
			case 1:
				filename += "hiskill.dr.ghost";
				inst = EInstrumentPart.DRUMS;
				break;
			case 2:
				filename += "hiscore.gt.ghost";
				inst = EInstrumentPart.GUITAR;
				break;
			case 3:
				filename += "hiskill.gt.ghost";
				inst = EInstrumentPart.GUITAR;
				break;
			case 4:
				filename += "hiscore.bs.ghost";
				inst = EInstrumentPart.BASS;
				break;
			case 5:
				filename += "hiskill.bs.ghost";
				inst = EInstrumentPart.BASS;
				break;
			case 6:
				filename += "lastplay.dr.ghost";
				inst = EInstrumentPart.DRUMS;
				break;
			case 7:
				filename += "lastplay.gt.ghost";
				inst = EInstrumentPart.GUITAR;
				break;
			case 8:
				filename += "lastplay.bs.ghost";
				inst = EInstrumentPart.BASS;
				break;
		}

		if (inst == EInstrumentPart.UNKNOWN)
		{
			return;
		}

		int cnt = 0;
		foreach (CChip chip in CDTXMania.DTX.listChip)
		{
			if (chip.eInstrumentPart == inst)
			{
				++cnt;
			}
		}

		if ( saveCond[(int)inst] )
			//if (false)
		{
			using FileStream fs = new(directory + "\\" + filename, FileMode.Create, FileAccess.Write);
			using BinaryWriter bw = new(fs);
			
			bw.Write(cnt);
			
			foreach (CChip chip in CDTXMania.DTX.listChip)
			{
				if (chip.eInstrumentPart == inst)
				{
					// -128 ms から 127 ms までのラグしか保存しない
					// その範囲を超えているラグはクランプ
					// ラグデータの 上位８ビットでそのチップの前でギター空打ちBADがあったことを示す
					int lag = chip.nLag;
					if (lag < -128)
					{
						lag = -128;
					}
					if (lag > 127)
					{
						lag = 127;
					}
					byte lower = (byte)(lag + 128);
					int upper = chip.nCurrentComboForGhost == 0 ? 1 : 0;
					bw.Write((short)((upper << 8) | lower));
				}
			}
		}
	}
	public override void OnDeactivate()
	{
		base.OnDeactivate();
	}
	public override void OnManagedCreateResources()
	{
		if ( bActivated )
		{
			base.OnManagedCreateResources();
		}
	}
	public override void OnManagedReleaseResources()
	{
		if ( bActivated )
		{
			if (ctPlayNewRecord != null)
			{
				ctPlayNewRecord = null;
			}
			
			//CDTXMania.t安全にDisposeする( ref this.ds背景動画 );t
			base.OnManagedReleaseResources();
		}
	}

	public override void FirstUpdate()
	{
		//Check result to select the correct sound to play
		int l_outputSoundEnum = 0; //0: Stage Clear 1: FC 2: EXC
		bool l_newRecord = false;
		for (int i = 0; i < 3; i++)
		{
			if ((i != 0 || (CDTXMania.DTX.bHasChips.Drums && !CDTXMania.ConfigIni.bGuitarRevolutionMode)) &&
			    (i != 1 || (CDTXMania.DTX.bHasChips.Guitar && CDTXMania.ConfigIni.bGuitarRevolutionMode)) &&
			    (i != 2 || (CDTXMania.DTX.bHasChips.Bass && CDTXMania.ConfigIni.bGuitarRevolutionMode)))
			{ 
				if (bAuto[i] == false)
				{
					if (fPerfectPercentage[i] == 100.0)
					{
						l_outputSoundEnum = 2; //Excellent
					}
					else if (fPoorPercentage[i] == 0.0 && fMissPercentage[i] == 0.0)
					{
						l_outputSoundEnum = 1; //Full Combo
					}
				}

				if (bNewRecordSkill[i] == true)
				{
					l_newRecord = true;
				}
			}
		}

		//Play the corresponding sound
		if (l_outputSoundEnum == 1)
		{
			CDTXMania.Skin.soundFullCombo.tPlay();
		}
		else if (l_outputSoundEnum == 2)
		{
			CDTXMania.Skin.soundExcellent.tPlay();
		}
		else
		{
			CDTXMania.Skin.soundStageClear.tPlay();
		}

		//Create the delay timer of 150 x 10 = 1500 ms to play New Record
		if (l_newRecord)
		{
			ctPlayNewRecord = new CCounter(0, 150, 10, CDTXMania.Timer);
		}
		
		CDTXMania.SongDb.RecalculateSkill();
		
		ePhaseID = EPhase.Common_DefaultState;
	}

	public override int OnUpdateAndDraw()
	{
		if (!bActivated) return 0;
		
		base.OnUpdateAndDraw();

		bAnimationComplete = true;

		//Play new record if available
		if (ctPlayNewRecord != null && ctPlayNewRecord.bInProgress)
		{
			ctPlayNewRecord.tUpdate();
			if (ctPlayNewRecord.bReachedEndValue)
			{
				CDTXMania.Skin.soundNewRecord.tPlay();
				ctPlayNewRecord.tStop();
			}
		}
		
		// if ( actResultImage.OnUpdateAndDraw() == 0 )
		// {
		// 	bAnimationComplete = false;
		// }
		#region [ #24609 2011.3.14 yyagi ランク更新or演奏型スキル更新時、リザルト画像をpngで保存する ]
		if ( bAnimationComplete && bIsCheckedWhetherResultScreenShouldSaveOrNot == false	// #24609 2011.3.14 yyagi; to save result screen in case BestRank or HiSkill.
		                        && CDTXMania.ConfigIni.bScoreIniを出力する
		                        && CDTXMania.ConfigIni.bIsAutoResultCapture)												// #25399 2011.6.9 yyagi
		{
			CheckAndSaveResultScreen(true);
			bIsCheckedWhetherResultScreenShouldSaveOrNot = true;
		}
		#endregion

		// キー入力
		if ( CDTXMania.ConfigIni.bドラム打音を発声する && CDTXMania.ConfigIni.bDrumsEnabled )
		{
			for( int i = 0; i < 11; i++ )
			{
				List<STInputEvent> events = listPadEvents;
				CDTXMania.Pad.GetEvents( EInstrumentPart.DRUMS, (EPad) i, events );
				if ( events.Count > 0 )
				{
					foreach( STInputEvent event2 in events )
					{
						if ( !event2.b押された )
						{
							continue;
						}
						CChip rChip = rEmptyDrumChip[ i ];
						if ( rChip == null )
						{
							switch( (EPad) i )
							{
								case EPad.HH:
									rChip = rEmptyDrumChip[ 7 ];
									if ( rChip == null )
									{
										rChip = rEmptyDrumChip[ 9 ];
									}
									break;

								case EPad.FT:
									rChip = rEmptyDrumChip[ 4 ];
									break;

								case EPad.CY:
									rChip = rEmptyDrumChip[ 8 ];
									break;

								case EPad.HHO:
									rChip = rEmptyDrumChip[ 0 ];
									if ( rChip == null )
									{
										rChip = rEmptyDrumChip[ 9 ];
									}
									break;

								case EPad.RD:
									rChip = rEmptyDrumChip[ 6 ];
									break;

								case EPad.LC:
									rChip = rEmptyDrumChip[ 0 ];
									if ( rChip == null )
									{
										rChip = rEmptyDrumChip[ 7 ];
									}
									break;
							}
						}
						if ( rChip != null && rChip.nChannelNumber >= EChannel.HiHatClose && rChip.nChannelNumber <= EChannel.LeftPedal )
						{
							int nLane = nチャンネル0Atoレーン07[ rChip.nChannelNumber - EChannel.HiHatClose ];
							if ( nLane == 1 && ( rChip.nChannelNumber == EChannel.HiHatClose || ( rChip.nChannelNumber == EChannel.HiHatOpen && n最後に再生したHHのチャンネル番号 != EChannel.HiHatOpen ) ) )
							{
								CDTXMania.DTX.tStopPlayingWav( n最後に再生したHHのWAV番号 );
								n最後に再生したHHのWAV番号 = rChip.nIntegerValue_InternalNumber;
								n最後に再生したHHのチャンネル番号 = rChip.nChannelNumber;
							}
							CDTXMania.DTX.tPlayChip( rChip, CDTXMania.Timer.nSystemTimeMs, nLane, CDTXMania.ConfigIni.n手動再生音量, CDTXMania.ConfigIni.b演奏音を強調する.Drums );
						}
					}
				}
			}
		}
		if (CDTXMania.Input.ActionDecide())
		{
			//actParameterPanel.tアニメを完了させる();
			//actRank.tアニメを完了させる();
		}
		#region [ #24609 2011.4.7 yyagi リザルト画面で[F12]を押下すると、リザルト画像をpngで保存する機能は、CDTXManiaに移管。 ]
		if ( CDTXMania.InputManager.Keyboard.bKeyPressed( (int) SlimDXKey.F12 ) &&
			CDTXMania.ConfigIni.bScoreIniを出力する )
		{
			CheckAndSaveResultScreen(false);
			bIsCheckedWhetherResultScreenShouldSaveOrNot = true;
		}
		#endregion
		//leaving deactivates this stage before the change runs, and in preview the change is dropped. The
		//skin editor is how you leave instead
		if ( ePhaseID == EPhase.Common_DefaultState && UIFocus.Holds( this ) && !previewMode )
		{
			if ( CDTXMania.InputManager.Keyboard.bKeyPressed( (int)SlimDXKey.Escape ) )
			{
				CDTXMania.Skin.soundCancel.tPlay();
				ePhaseID = EPhase.Common_FadeOut;
				eReturnValueWhenFadeOutCompleted = EReturnValue.Complete;

				GitaDoraTransition.Close();
			}
			if (CDTXMania.Input.ActionDecide() && bAnimationComplete)
			{
				CDTXMania.Skin.soundCancel.tPlay();
				ePhaseID = EPhase.Common_FadeOut;
				eReturnValueWhenFadeOutCompleted = EReturnValue.Complete;

				GitaDoraTransition.Close();
			}
		}

		if (ePhaseID == EPhase.Common_FadeOut)
		{
			if (!GitaDoraTransition.isAnimating)
			{
				return (int) eReturnValueWhenFadeOutCompleted;
			}
		}
		return 0;
	}

	public enum EReturnValue : int
	{
		Continue,
		Complete
	}


	// Other

	#region [ private ]
	//-----------------
	//New Counter
	private CCounter ctPlayNewRecord;
	private EReturnValue eReturnValueWhenFadeOutCompleted;  // eフェードアウト完了時の戻り値

	private bool bAnimationComplete;  // bアニメが完了
	private bool bIsCheckedWhetherResultScreenShouldSaveOrNot;				// #24509 2011.3.14 yyagi
	private readonly int[] nチャンネル0Atoレーン07;
	private int n最後に再生したHHのWAV番号;
	private EChannel n最後に再生したHHのチャンネル番号;
	private UIImage background;  // tx背景
	//Copy from CStagePerfCommonScreen
	public STDGBVALUE<CStagePerfCommonScreen.CLAGTIMINGHITCOUNT> nTimingHitCount;

	//private CDirectShow ds背景動画;
	private long lDshowPosition;
	private long lStopPosition;

	#region [ #24609 リザルト画像をpngで保存する ]		// #24609 2011.3.14 yyagi; to save result screen in case BestRank or HiSkill.
	/// <summary>
	/// リザルト画像のキャプチャと保存。
	/// 自動保存モード時は、ランク更新or演奏型スキル更新時に自動保存。
	/// 手動保存モード時は、ランクに依らず保存。
	/// </summary>
	/// <param name="bIsAutoSave">true=自動保存モード, false=手動保存モード</param>
	private void CheckAndSaveResultScreen(bool bIsAutoSave)
	{
		string datetime = DateTime.Now.ToString( "yyyyMMddHHmmss" );
		if (bIsAutoSave)
		{
			// リザルト画像を自動保存するときは、dtxファイル名.yyMMddHHmmss_DRUMS_SS.png という形式で保存。
			for (int i = 0; i < 3; i++)
			{
				if (bNewRecordRank[i] || bNewRecordSkill[i])
				{
					string strPart = ((EInstrumentPart)i).ToString();
					string strRank = ((CScoreIni.ERANK)nRankValue[i]).ToString();
					string strFullPath = $"{CDTXMania.DTX.strFileNameFullPath}.{datetime}_{strPart}_{strRank}.png";
					CDTXMania.app.SaveResultScreen(strFullPath);
				}
			}
		}
	}
	#endregion
	//-----------------
	#endregion
}