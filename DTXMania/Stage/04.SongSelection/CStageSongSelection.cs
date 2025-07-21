using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Diagnostics;
using System.Globalization;
using FDK;
using DiscordRPC;
using DTXMania.Core;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using SlimDXKey = SlimDX.DirectInput.Key;

namespace DTXMania;

internal class CStageSongSelection : CStage
{
	protected override RichPresence Presence => new CDTXRichPresence
	{
		State = "In Menu",
		Details = "Selecting a song",
	};

	public int nScrollbarRelativeYCoordinate => actSongList != null ? actSongList.nスクロールバー相対y座標 : 0;

	public bool bIsEnumeratingSongs
	{
		get => actSongList.bIsEnumeratingSongs;
		set => actSongList.bIsEnumeratingSongs = value;
	}
	public bool bIsPlayingPremovie => actPreimagePanel.bIsPlayingPremovie;

	public bool bScrolling => actSongList.bScrolling;

	public int nConfirmedSongDifficulty
	{
		get;
		private set;
	}
	public CScore rChosenScore
	{
		get;
		private set;
	}
	public CSongListNode rConfirmedSong 
	{
		get;
		private set;
	}
	/// <summary>
	/// <para>現在演奏中の曲のスコアに対応する背景動画。</para>
	/// <para>r現在演奏中の曲のスコア の読み込み時に、自動検索_抽出_生成される。</para>
	/// </summary>
	//public CDirectShow r現在演奏中のスコアの背景動画 = null;
	public int nSelectedSongDifficultyLevel => actSongList.n現在選択中の曲の現在の難易度レベル;

	public CScore rSelectedScore => actSongList.rSelectedScore; // r現在選択中のスコア

	public CSongListNode r現在選択中の曲 => actSongList.rSelectedSong;

	// コンストラクタ
	public CStageSongSelection()
	{
		eStageID = EStage.SongSelection_4;
		ePhaseID = EPhase.Common_DefaultState;
		bActivated = false;
		listChildActivities.Add(actFIFO = new CActFIFOBlack());
		listChildActivities.Add(actFIFromResultsScreen = new CActFIFOBlack());
		listChildActivities.Add(actSongList = new CActSelectSongList(this));
		listChildActivities.Add(actStatusPanel = new CActSelectStatusPanel());
		listChildActivities.Add(actPerHistoryPanel = new CActSelectPerfHistoryPanel());
		listChildActivities.Add(actPreimagePanel = new CActSelectPreimagePanel(this));
		listChildActivities.Add(actPresound = new CActSelectPresound());
		listChildActivities.Add(actArtistComment = new CActSelectArtistComment());
		listChildActivities.Add(actInformation = new CActSelectInformation());
		listChildActivities.Add(actSortSongs = new CActSortSongs());
		listChildActivities.Add(actShowCurrentPosition = new CActScrollBar(this));
		listChildActivities.Add(actBackgroundVideoAVI = new CActSelectBackgroundAVI());
		listChildActivities.Add(actQuickConfig = new CActSelectQuickConfig());
		
		listChildActivities.Add(actSearchBox = new CActSearchBox());

		CommandHistory = new CCommandHistory(); // #24063 2011.1.16 yyagi
		bCheckDrumsEnabled = CDTXMania.ConfigIni.bDrumsEnabled;
		bCheckRandSubBox = CDTXMania.ConfigIni.bランダムセレクトで子BOXを検索対象とする;
	}


	// メソッド

	public void tSelectedSongChanged()
	{
		actPreimagePanel.t選択曲が変更された();
		actPresound.t選択曲が変更された();
		actPerHistoryPanel.t選択曲が変更された();
		actStatusPanel.tSelectedSongChanged();
		actArtistComment.t選択曲が変更された();
	}

	// CStage 実装

	/// <summary>
	/// 曲リストをリセットする
	/// </summary>
	/// <param name="cs"></param>
	public void Refresh( CSongManager cs, bool bRemakeSongTitleBar)
	{
		actSongList.Refresh( cs, bRemakeSongTitleBar );
	}

	public override void OnActivate()
	{
		Trace.TraceInformation( "選曲ステージを活性化します。" );
		Trace.Indent();
		try
		{
			eReturnValueWhenFadeOutCompleted = EReturnValue.Continue;
			bBGMPlayed = false;
			ftFont = new Font( "MS PGothic", 26f, GraphicsUnit.Pixel );
			ftSearchInputNotificationFont = new Font("MS PGothic", 14f, GraphicsUnit.Pixel);
			for( int i = 0; i < 4; i++ )
				ctKeyRepeat[ i ] = new CCounter( 0, 0, 0, CDTXMania.Timer );

			base.OnActivate();

			actSearchBox.t検索説明文を表示する設定にする();
			actStatusPanel.tSelectedSongChanged(); // 最大ランクを更新

			//Reset random list upon reactivation only when a change in config for drumsEnabled or RandSubBox is detected
			bool bToReset = false;
			if(bCheckDrumsEnabled != CDTXMania.ConfigIni.bDrumsEnabled)
			{
				bToReset = true;
				bCheckDrumsEnabled = CDTXMania.ConfigIni.bDrumsEnabled;
			}

			if(bCheckRandSubBox != CDTXMania.ConfigIni.bランダムセレクトで子BOXを検索対象とする)
			{
				bToReset = true;
				bCheckRandSubBox = CDTXMania.ConfigIni.bランダムセレクトで子BOXを検索対象とする;
			}

			if (bToReset)
			{
				tResetRandomListForNode(null);
			}
			
			dynamicStringSources["SongName"] = new DynamicStringSource(() => actSongList.rSelectedScore.SongInformation.Title ?? "");
			dynamicStringSources["SongArtist"] = new DynamicStringSource(() => actSongList.rSelectedScore.SongInformation.ArtistName ?? "");
			dynamicStringSources["SongGenre"] = new DynamicStringSource(() => actSongList.rSelectedScore.SongInformation.Genre ?? "");
			dynamicStringSources["SongBPM"] = new DynamicStringSource(() => actSongList.rSelectedScore.SongInformation.Bpm.ToString(CultureInfo.InvariantCulture));
		}
		finally
		{
			Trace.TraceInformation( "選曲ステージの活性化を完了しました。" );
			Trace.Unindent();
		}
	}
	public override void OnDeactivate()
	{
		Trace.TraceInformation( "選曲ステージを非活性化します。" );
		Trace.Indent();
		try
		{
			if (rBackgroundVideoAVI != null)
			{
				rBackgroundVideoAVI.Dispose();
				rBackgroundVideoAVI = null;
			}

			if ( ftFont != null )
			{
				ftFont.Dispose();
				ftFont = null;
			}

			if(ftSearchInputNotificationFont != null)
			{
				ftSearchInputNotificationFont.Dispose();
				ftSearchInputNotificationFont = null;
			}

			for( int i = 0; i < 4; i++ )
			{
				ctKeyRepeat[ i ] = null;
			}
			base.OnDeactivate();
		}
		finally
		{
			Trace.TraceInformation( "選曲ステージの非活性化を完了しました。" );
			Trace.Unindent();
		}
	}

	private UIImage? topPanel;
	private UIImage? songListTopPanel;
	private UIImage? songListBottomPanel;
	
	public override void InitializeBaseUI()
	{
		LegacyDrawable backgroundVideo = ui.AddChild(new LegacyDrawable(() => actBackgroundVideoAVI.tUpdateAndDraw()));
		backgroundVideo.renderOrder = -99;
		backgroundVideo.name = "BackgroundVideo";
		
		LegacyDrawable preImagePanel = ui.AddChild(new LegacyDrawable(() => actPreimagePanel.OnUpdateAndDraw()));
		preImagePanel.name = "PreImagePanel";
		
		LegacyDrawable artistComment = ui.AddChild(new LegacyDrawable(() => actArtistComment.OnUpdateAndDraw()));
		artistComment.name = "ArtistComment";

		UIGroup songList = ui.AddChild(new UIGroup("SongList"));
		songList.renderOrder = 1;

		LegacyDrawable listView = songList.AddChild(new LegacyDrawable(() => actSongList.OnUpdateAndDraw()));
		listView.name = "SongList";
		
		LegacyDrawable statusPanel = ui.AddChild(new LegacyDrawable(() => actStatusPanel.OnUpdateAndDraw()));
		statusPanel.name = "StatusPanel";
		statusPanel.renderOrder = 5;
		
		LegacyDrawable perfHistoryPanel = ui.AddChild(new LegacyDrawable(() => actPerHistoryPanel.OnUpdateAndDraw()));
		perfHistoryPanel.name = "PerfHistoryPanel";
		perfHistoryPanel.renderOrder = 5;
		
		LegacyDrawable information = ui.AddChild(new LegacyDrawable(() => actInformation.OnUpdateAndDraw()));
		information.name = "Information";
		information.renderOrder = 5;
		
		LegacyDrawable showCurrentPosition = ui.AddChild(new LegacyDrawable(() => actShowCurrentPosition.OnUpdateAndDraw()));
		showCurrentPosition.name = "ScrollBar";
		showCurrentPosition.renderOrder = 5;
	}
	
	public override void InitializeDefaultUI()
	{
		DTXTexture bgTex = DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_background.jpg"));
		UIImage bg = ui.AddChild(new UIImage(bgTex));
		bg.renderOrder = -100;
		bg.position = SharpDX.Vector3.Zero;
		bg.name = "Background";
		
		DTXTexture topPanelTex = DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_header panel.png"));
		topPanel = ui.AddChild(new UIImage(topPanelTex));
		topPanel.position = new SharpDX.Vector3(0, 0, 0);
		topPanel.name = "TopPanel";
		topPanel.renderOrder = 4;
		
		DTXTexture bottomPanelTex = DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_footer panel.png"));
		UIImage bottomPanel = ui.AddChild(new UIImage(bottomPanelTex));
		bottomPanel.position = new SharpDX.Vector3(0, 720 - bottomPanelTex.Height, 0);
		bottomPanel.name = "BottomPanel";
		bottomPanel.renderOrder = 4;
		
		DTXTexture bpmLabelTex = DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_BPM.png"));
		UIImage bpmLabel = ui.AddChild(new UIImage(bpmLabelTex));
		bpmLabel.position = new SharpDX.Vector3(32, 258, 0);
		bpmLabel.name = "BPMLabel";

		UIGroup? songList = ui.GetChild<UIGroup>("SongList");

		if (songList != null)
		{
			DTXTexture songListTopPanelTex = DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_header song list.png"));
			songListTopPanel = songList.AddChild(new UIImage(songListTopPanelTex));
			songListTopPanel.name = "SongListTopPanel";

			DTXTexture songListBottomPanelTex = DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_footer song list.png"));
			songListBottomPanel = songList.AddChild(new UIImage(songListBottomPanelTex));
			songListBottomPanel.position = new SharpDX.Vector3(0, 720 - songListBottomPanelTex.Height, 0);
			songListBottomPanel.name = "SongListBottomPanel";
		}
	}
	
	public override void OnManagedCreateResources()
	{
		if( bActivated )
		{
			prvFontSearchInputNotification = new CPrivateFastFont(new FontFamily(CDTXMania.ConfigIni.songListFont), 14, FontStyle.Regular);
			//this.dsBackgroundVideo = CDTXMania.t失敗してもスキップ可能なDirectShowを生成する(CSkin.Path(@"Graphics\5_background.mp4"), CDTXMania.app.WindowHandle, true);
			txBPMLabel = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\5_BPM.png"), false);

			//
			rBackgroundVideoAVI = new CAVI(1290, CSkin.Path(@"Graphics\5_background.mp4"), "", 20.0);
			rBackgroundVideoAVI.OnDeviceCreated();
			if (rBackgroundVideoAVI.avi != null)
			{					
				actBackgroundVideoAVI.bLoop = true;
				actBackgroundVideoAVI.Start(EChannel.MovieFull, rBackgroundVideoAVI, 0, -1);
				Trace.TraceInformation("選曲ムービーを有効化しました。");
			}

			base.OnManagedCreateResources();
		}
	}
	public override void OnManagedReleaseResources()
	{
		if( bActivated )
		{
			actBackgroundVideoAVI.Stop();
			
			CDTXMania.tReleaseTexture(ref txBPMLabel);
			
			CDTXMania.tDisposeSafely(ref txSearchInputNotification);
			CDTXMania.tDisposeSafely(ref prvFontSearchInputNotification);

			base.OnManagedReleaseResources();
		}
	}

	public override void FirstUpdate()
	{
		ctInitialAppearAnimation = new CCounter( 0, 100, 3, CDTXMania.Timer );
		if (CDTXMania.StageManager.rPreviousStage.eStageID == EStage.Result_7)
		{
			actFIFromResultsScreen.tフェードイン開始();
			ePhaseID = EPhase.選曲_結果画面からのフェードイン;
		}
		else
		{
			actFIFO.tフェードイン開始();
			ePhaseID = EPhase.Common_FadeIn;
		}
		ctSearchInputDisplayCounter = new CCounter(0, 1, 10000, CDTXMania.Timer);
		tSelectedSongChanged();
	}

	public override int OnUpdateAndDraw()
	{
		if (!bActivated) return 0;
		
		base.OnUpdateAndDraw();

		ctInitialAppearAnimation.tUpdate();
		ctSearchInputDisplayCounter.tUpdate();
		if (ctSearchInputDisplayCounter.bReachedEndValue)
		{
			tUpdateSearchNotification("");
		}

		if (topPanel != null)
		{
			float y = 0;
			if (ctInitialAppearAnimation.bInProgress)
			{
				double db登場割合 = ctInitialAppearAnimation.nCurrentValue / 100.0;	// 100が最終値
				double dbY表示割合 = Math.Sin( Math.PI / 2 * db登場割合 );
				y = (float)(topPanel.Texture.Height * dbY表示割合) - topPanel.Texture.Height;
			}
			
			topPanel.position.Y = y;
		}

		if (songListTopPanel != null)
		{
			songListTopPanel.position.Y = songListTopPanel.size.Y * (ctInitialAppearAnimation.nCurrentValue / 100f) 
			                              - songListTopPanel.size.Y;
		}
		
		if (songListBottomPanel != null)
		{
			songListBottomPanel.position.Y = 720 - 
			                                 songListBottomPanel.size.Y * (ctInitialAppearAnimation.nCurrentValue / 100f);
		}


		actPresound.OnUpdateAndDraw();

		switch ( ePhaseID )
		{
			case EPhase.Common_FadeIn:
				if( actFIFO.OnUpdateAndDraw() != 0 )
				{
					ePhaseID = EPhase.Common_DefaultState;
				}
				break;

			case EPhase.Common_FadeOut:
				if( actFIFO.OnUpdateAndDraw() == 0 )
				{
					break;
				}
				return (int) eReturnValueWhenFadeOutCompleted;

			case EPhase.選曲_結果画面からのフェードイン:
				if( actFIFromResultsScreen.OnUpdateAndDraw() != 0 )
				{
					ePhaseID = EPhase.Common_DefaultState;
				}
				break;

			case EPhase.選曲_NowLoading画面へのフェードアウト:
				return (int) eReturnValueWhenFadeOutCompleted;
		}
		if( !bBGMPlayed && ( ePhaseID == EPhase.Common_DefaultState ) )
		{
			CDTXMania.Skin.bgm選曲画面.n音量_次に鳴るサウンド = 100;
			CDTXMania.Skin.bgm選曲画面.tPlay();
			bBGMPlayed = true;
		}
		

		// キー入力
		if( ePhaseID == EPhase.Common_DefaultState)
		{
			#region [ 簡易CONFIGでMore、またはShift+F1: 詳細CONFIG呼び出し ]
			if (  actQuickConfig.bGotoDetailConfig )
			{	// 詳細CONFIG呼び出し
				actQuickConfig.tDeativatePopupMenu();
				actPresound.tサウンド停止();
				eReturnValueWhenFadeOutCompleted = EReturnValue.CallConfig;	// #24525 2011.3.16 yyagi: [SHIFT]-[F1]でCONFIG呼び出し
				actFIFO.tStartFadeOut();
				ePhaseID = EPhase.Common_FadeOut;
				CDTXMania.Skin.soundCancel.tPlay();
				return 0;
			}
			#endregion
			if ( !actSortSongs.bIsActivePopupMenu && !actQuickConfig.bIsActivePopupMenu && !CDTXMania.app.bテキスト入力中)
			{
				#region [ ESC ]
				if (CDTXMania.Input.ActionCancel())
				{	// [ESC]
					CDTXMania.Skin.soundCancel.tPlay();
					eReturnValueWhenFadeOutCompleted = EReturnValue.ReturnToTitle;
					actFIFO.tStartFadeOut();
					ePhaseID = EPhase.Common_FadeOut;
					return 0;
				}
				#endregion
				#region [ CONFIG画面 ]
				if (CDTXMania.Pad.bPressed(EInstrumentPart.GUITAR, EPad.Help))
				{	// [SHIFT] + [F1] CONFIG
					actPresound.tサウンド停止();
					eReturnValueWhenFadeOutCompleted = EReturnValue.CallConfig;	// #24525 2011.3.16 yyagi: [SHIFT]-[F1]でCONFIG呼び出し
					actFIFO.tStartFadeOut();
					ePhaseID = EPhase.Common_FadeOut;
					CDTXMania.Skin.soundCancel.tPlay();
					return 0;
				}
				#endregion

				if (actSongList.rSelectedSong != null)
				{
					#region [ Decide ]
					if (CDTXMania.Input.ActionDecide())
					{
						if (actSongList.rSelectedSong != null)
						{
							switch (actSongList.rSelectedSong.eNodeType)
							{
								case CSongListNode.ENodeType.SCORE:
									CDTXMania.Skin.soundDecide.tPlay();
									tSelectSong();
									break;

								case CSongListNode.ENodeType.SCORE_MIDI:
									CDTXMania.Skin.soundDecide.tPlay();
									tSelectSong();
									break;

								case CSongListNode.ENodeType.BOX:
								{
									CDTXMania.Skin.soundDecide.tPlay();
									bool bNeedChangeSkin = actSongList.tGoIntoBOX();
									if (bNeedChangeSkin)
									{
										eReturnValueWhenFadeOutCompleted = EReturnValue.ChangeSking;
										ePhaseID = EPhase.選曲_NowLoading画面へのフェードアウト;
									}
								}
									break;

								case CSongListNode.ENodeType.BACKBOX:
								{
									CDTXMania.Skin.soundCancel.tPlay();
									bool bNeedChangeSkin = actSongList.tExitBOX();
									if (bNeedChangeSkin)
									{
										eReturnValueWhenFadeOutCompleted = EReturnValue.ChangeSking;
										ePhaseID = EPhase.選曲_NowLoading画面へのフェードアウト;
									}
								}
									break;

								case CSongListNode.ENodeType.RANDOM:
									CDTXMania.Skin.soundDecide.tPlay();
									tSelectSongRandomly();
									break;
							}
						}
					}
					#endregion
					#region [ Up ]
					ctKeyRepeat.Up.tRepeatKey(CDTXMania.InputManager.Keyboard.bKeyPressing(SlimDXKey.UpArrow), new CCounter.DGキー処理(tMoveCursorUp));
					ctKeyRepeat.R.tRepeatKey(CDTXMania.Pad.bPressingGB(EPad.R), new CCounter.DGキー処理(tMoveCursorUp));
					//SD changed to HT to follow Gitadora style
					if (CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.HT))
					{
						tMoveCursorUp();
					}
					#endregion
					#region [ Down ]
					ctKeyRepeat.Down.tRepeatKey(CDTXMania.InputManager.Keyboard.bKeyPressing(SlimDXKey.DownArrow), new CCounter.DGキー処理(tMoveCursorDown));
					ctKeyRepeat.B.tRepeatKey(CDTXMania.Pad.bPressingGB(EPad.G), new CCounter.DGキー処理(tMoveCursorDown));
					//FT changed to LT to follow Gitadora style
					if (CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.LT))
					{
						tMoveCursorDown();
					}
					#endregion
					#region [ Upstairs ]
					if (((actSongList.rSelectedSong != null) && (actSongList.rSelectedSong.parentNode != null)) && (CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.LC) || CDTXMania.Pad.bPressedGB(EPad.Cancel)))
					{
						actPresound.tサウンド停止();
						CDTXMania.Skin.soundCancel.tPlay();
						actSongList.tExitBOX();
						tSelectedSongChanged();
					}
					#endregion
					#region [ BDx2: 簡易CONFIG ]
					if (CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.BD))
					{	// [BD]x2 スクロール速度変更
						CommandHistory.Add(EInstrumentPart.DRUMS, EPadFlag.BD);
						EPadFlag[] comChangeScrollSpeed = new EPadFlag[] { EPadFlag.BD, EPadFlag.BD };
						if (CommandHistory.CheckCommand(comChangeScrollSpeed, EInstrumentPart.DRUMS))
						{
							// Debug.WriteLine( "ドラムススクロール速度変更" );
							// CDTXMania.ConfigIni.nScrollSpeed.Drums = ( CDTXMania.ConfigIni.nScrollSpeed.Drums + 1 ) % 0x10;
							CDTXMania.Skin.soundChange.tPlay();
							actQuickConfig.tActivatePopupMenu(EInstrumentPart.DRUMS);
						}
					}
					#endregion
					#region [ HHx2: 難易度変更 ]
					if (CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.HH) || CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.HHO))
					{	// [HH]x2 難易度変更
						CommandHistory.Add(EInstrumentPart.DRUMS, EPadFlag.HH);
						EPadFlag[] comChangeDifficulty = [EPadFlag.HH, EPadFlag.HH];
						if (CommandHistory.CheckCommand(comChangeDifficulty, EInstrumentPart.DRUMS))
						{
							Debug.WriteLine("ドラムス難易度変更");
							actSongList.t難易度レベルをひとつ進める();
							//CDTXMania.Skin.sound変更音.tPlay();
						}
					}
					#endregion
					#region [ Bx2 Guitar: 難易度変更 ]
					if (CDTXMania.Pad.bPressed(EInstrumentPart.GUITAR, EPad.B))	// #24177 2011.1.17 yyagi || -> &&
					{	// [B]x2 ギター難易度変更
						CommandHistory.Add(EInstrumentPart.GUITAR, EPadFlag.B);
						EPadFlag[] comChangeDifficultyG = new EPadFlag[] { EPadFlag.B, EPadFlag.B };
						if (CommandHistory.CheckCommand(comChangeDifficultyG, EInstrumentPart.GUITAR))
						{
							Debug.WriteLine("ギター難易度変更");
							actSongList.t難易度レベルをひとつ進める();
							//CDTXMania.Skin.sound変更音.tPlay();
						}
					}
					#endregion
					#region [ Bx2 Bass: 難易度変更 ]
					if (CDTXMania.Pad.bPressed(EInstrumentPart.BASS, EPad.B))		// #24177 2011.1.17 yyagi || -> &&
					{	// [B]x2 ベース難易度変更
						CommandHistory.Add(EInstrumentPart.BASS, EPadFlag.B);
						EPadFlag[] comChangeDifficultyB = new EPadFlag[] { EPadFlag.B, EPadFlag.B };
						if (CommandHistory.CheckCommand(comChangeDifficultyB, EInstrumentPart.BASS))
						{
							Debug.WriteLine("ベース難易度変更");
							actSongList.t難易度レベルをひとつ進める();
							//CDTXMania.Skin.sound変更音.tPlay();
						}
					}
					#endregion
					#region [ Yx2 Guitar: ギターとベースを入れ替え ]
					if (CDTXMania.Pad.bPressed(EInstrumentPart.GUITAR, EPad.Y))
					{	// Pick, Y, Y, Pick で、ギターとベースを入れ替え
						CommandHistory.Add(EInstrumentPart.GUITAR, EPadFlag.Y);
						EPadFlag[] comSwapGtBs1 = new EPadFlag[] { EPadFlag.Y, EPadFlag.Y };
						if (CommandHistory.CheckCommand(comSwapGtBs1, EInstrumentPart.GUITAR))
						{
							Debug.WriteLine("ギターとベースの入れ替え1");
							CDTXMania.Skin.soundChange.tPlay();
							// ギターとベースのキーを入れ替え
							//CDTXMania.ConfigIni.SwapGuitarBassKeyAssign();
							CDTXMania.ConfigIni.bIsSwappedGuitarBass = !CDTXMania.ConfigIni.bIsSwappedGuitarBass;
							actSongList.tSwapClearLamps();
						}
					}
					#endregion
					#region [ Yx2 Bass: ギターとベースを入れ替え ]
					if (CDTXMania.Pad.bPressed(EInstrumentPart.BASS, EPad.Y))
					{	// ベース[Pick]: コマンドとしてEnqueue
						CommandHistory.Add(EInstrumentPart.BASS, EPadFlag.Y);
						// Pick, Y, Y, Pick で、ギターとベースを入れ替え
						EPadFlag[] comSwapGtBs1 = new EPadFlag[] { EPadFlag.Y, EPadFlag.Y };
						if (CommandHistory.CheckCommand(comSwapGtBs1, EInstrumentPart.BASS))
						{
							Debug.WriteLine("ギターとベースの入れ替え2");
							CDTXMania.Skin.soundChange.tPlay();
							// ギターとベースのキーを入れ替え
							//CDTXMania.ConfigIni.SwapGuitarBassKeyAssign();
							CDTXMania.ConfigIni.bIsSwappedGuitarBass = !CDTXMania.ConfigIni.bIsSwappedGuitarBass;
							actSongList.tSwapClearLamps();
						}
					}
					#endregion
					#region [ Px2 Guitar: 簡易CONFIG ]
					if (CDTXMania.Pad.bPressed(EInstrumentPart.GUITAR, EPad.P))
					{	// [BD]x2 スクロール速度変更
						CommandHistory.Add(EInstrumentPart.GUITAR, EPadFlag.P);
						EPadFlag[] comChangeScrollSpeed = new EPadFlag[] { EPadFlag.P, EPadFlag.P };
						if (CommandHistory.CheckCommand(comChangeScrollSpeed, EInstrumentPart.GUITAR))
						{
							// Debug.WriteLine( "ドラムススクロール速度変更" );
							// CDTXMania.ConfigIni.nScrollSpeed.Drums = ( CDTXMania.ConfigIni.nScrollSpeed.Drums + 1 ) % 0x10;
							CDTXMania.Skin.soundChange.tPlay();
							actQuickConfig.tActivatePopupMenu(EInstrumentPart.GUITAR);
						}
					}
					#endregion
					#region [ Px2 Bass: 簡易CONFIG ]
					if (CDTXMania.Pad.bPressed(EInstrumentPart.BASS, EPad.P))
					{	// [BD]x2 スクロール速度変更
						CommandHistory.Add(EInstrumentPart.BASS, EPadFlag.P);
						EPadFlag[] comChangeScrollSpeed = new EPadFlag[] { EPadFlag.P, EPadFlag.P };
						if (CommandHistory.CheckCommand(comChangeScrollSpeed, EInstrumentPart.BASS))
						{
							// Debug.WriteLine( "ドラムススクロール速度変更" );
							// CDTXMania.ConfigIni.nScrollSpeed.Drums = ( CDTXMania.ConfigIni.nScrollSpeed.Drums + 1 ) % 0x10;
							CDTXMania.Skin.soundChange.tPlay();
							actQuickConfig.tActivatePopupMenu(EInstrumentPart.BASS);
						}
					}
					#endregion
					#region [ Y P Guitar: ソート画面 ]
					if (CDTXMania.Pad.bPressing(EInstrumentPart.GUITAR, EPad.Y) && CDTXMania.Pad.bPressed(EInstrumentPart.GUITAR, EPad.P))
					{	// ギター[Pick]: コマンドとしてEnqueue
						CDTXMania.Skin.soundChange.tPlay();
						actSortSongs.tActivatePopupMenu(EInstrumentPart.GUITAR, ref actSongList);
					}
					#endregion
					#region [ Y P Bass: ソート画面 ]
					if (CDTXMania.Pad.bPressing(EInstrumentPart.BASS, EPad.Y) && CDTXMania.Pad.bPressed(EInstrumentPart.BASS, EPad.P))
					{	// ベース[Pick]: コマンドとしてEnqueue
						CDTXMania.Skin.soundChange.tPlay();
						actSortSongs.tActivatePopupMenu(EInstrumentPart.BASS, ref actSongList);
					}
					#endregion
					#region [ FTx2 Drums: ソート画面 ]
					if (CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.FT))
					{	// [HT]x2 ソート画面        2013.12.31.kairera0467
						//Change to FT x 2 to follow Gitadora style
						//
						CommandHistory.Add(EInstrumentPart.DRUMS, EPadFlag.FT);
						EPadFlag[] comSort = new EPadFlag[] { EPadFlag.FT, EPadFlag.FT };
						if (CommandHistory.CheckCommand(comSort, EInstrumentPart.DRUMS))
						{
							CDTXMania.Skin.soundChange.tPlay();
							actSortSongs.tActivatePopupMenu(EInstrumentPart.DRUMS, ref actSongList);
						}
					}
					#endregion
				}
			}

			#region [Test text field]
			if (!CDTXMania.app.bテキスト入力中 && CDTXMania.Pad.bPressed(EKeyConfigPart.SYSTEM, EKeyConfigPad.Search))
			{
				CDTXMania.Skin.soundDecide.tPlay();
				actSearchBox.t表示();
				actSearchBox.t入力を開始();
			}
			#endregion

			actSortSongs.tUpdateAndDraw();
			actQuickConfig.tUpdateAndDraw();
			actSearchBox.OnUpdateAndDraw();
			
			if (actSearchBox.b入力が終了した)
			{
				strSearchString = actSearchBox.str確定文字列を返す();
				if(strSearchString != "" && strSearchString != CSongSearch.ExitSwitch)
				{
					string searchOutcome = "Search Input: " + strSearchString;
					Trace.TraceInformation("Search Input: " + strSearchString);
					if(CDTXMania.SongManager.listSongBeforeSearch == null)
					{
						CDTXMania.SongManager.listSongBeforeSearch = CDTXMania.SongManager.listSongRoot;
					}

					List<CSongListNode> searchOutputList = CSongSearch.tSearchForSongs(CDTXMania.SongManager.listSongBeforeSearch, strSearchString);
					if(searchOutputList.Count == 0)
					{
						Trace.TraceInformation("No songs found!");
						//To print a outcome message
						searchOutcome += "\r\nNo songs found";
					}
					else
					{
						CDTXMania.SongManager.listSongRoot = searchOutputList;

						//
						actSongList.SearchUpdate();
						//this.actSongList.Refresh(CDTXMania.SongManager, true);
					}

					tUpdateSearchNotification(searchOutcome);
					ctSearchInputDisplayCounter.tStart(0, 1, 10000, CDTXMania.Timer);
					CDTXMania.Skin.soundDecide.tPlay();
				}
				else if(strSearchString == CSongSearch.ExitSwitch)
				{
					if(CDTXMania.SongManager.listSongBeforeSearch != null)
					{
						CDTXMania.SongManager.listSongRoot = CDTXMania.SongManager.listSongBeforeSearch;
						CDTXMania.SongManager.listSongBeforeSearch = null;
						actSongList.SearchUpdate();
						tUpdateSearchNotification("Exit Search Mode");
						ctSearchInputDisplayCounter.tStart(0, 1, 10000, CDTXMania.Timer);
						CDTXMania.Skin.soundDecide.tPlay();
					}
					else
					{
						//Play cancel sound if input has no effect
						CDTXMania.Skin.soundCancel.tPlay(); 
					}
				}
				else
				{
					//Play cancel sound if input has no effect
					CDTXMania.Skin.soundCancel.tPlay();
				}						
						
				actSearchBox.tHide();
			}

			if(txSearchInputNotification != null)
			{
				txSearchInputNotification.tDraw2D(CDTXMania.app.Device, 10, 130);
			}

		}
		return 0;
	}

	public enum EReturnValue : int  // E戻り値
	{
		Continue,      // 継続
		ReturnToTitle, // タイトルに戻る
		Selected,      // 選曲した
		CallConfig,    // コンフィグ呼び出し
		ChangeSking    // スキン変更
	}
		

	// Other

	#region [ private ]
	//-----------------
	[StructLayout( LayoutKind.Sequential )]
	private struct STKeyRepeatCounter  // STキー反復用カウンタ
	{
		public CCounter Up;
		public CCounter Down;
		public CCounter R;
		public CCounter B;
		public CCounter this[ int index ]
		{
			get
			{
				switch( index )
				{
					case 0:
						return Up;

					case 1:
						return Down;

					case 2:
						return R;

					case 3:
						return B;
				}
				throw new IndexOutOfRangeException();
			}
			set
			{
				switch( index )
				{
					case 0:
						Up = value;
						return;

					case 1:
						Down = value;
						return;

					case 2:
						R = value;
						return;

					case 3:
						B = value;
						return;
				}
				throw new IndexOutOfRangeException();
			}
		}
	}
	private CActSelectArtistComment actArtistComment;
	private CActFIFOBlack actFIFO;
	private CActFIFOBlack actFIFromResultsScreen;
	private CActSelectInformation actInformation;
	private CActSelectPreimagePanel actPreimagePanel;  // actPreimageパネル
	private CActSelectPresound actPresound;
	public CActSelectStatusPanel actStatusPanel;  // actステータスパネル
	private CActSelectPerfHistoryPanel actPerHistoryPanel;  // act演奏履歴パネル
	private CActSelectSongList actSongList;
	private CActScrollBar actShowCurrentPosition;
	private readonly CActSelectBackgroundAVI actBackgroundVideoAVI;

	private CActSortSongs actSortSongs;
	private CActSelectQuickConfig actQuickConfig;

	//
	private CActSearchBox actSearchBox;
	private string strSearchString;
	private bool bBGMPlayed;  // bBGM再生済み
	private STKeyRepeatCounter ctKeyRepeat;  // ctキー反復用
	public CCounter ctInitialAppearAnimation;
	private CCounter ctSearchInputDisplayCounter;
	private EReturnValue eReturnValueWhenFadeOutCompleted;  // eフェードアウト完了時の戻り値
	private Font ftFont;  // ftフォント
	
	private CTexture txBPMLabel;
	private CAVI rBackgroundVideoAVI;// background Video using CAVI class
	private long lDshowPosition;
	private long lStopPosition;

	//
	private Font ftSearchInputNotificationFont;
	private CPrivateFastFont prvFontSearchInputNotification;
	private CTexture txSearchInputNotification = null;

	//
	private bool bCheckDrumsEnabled = false;
	private bool bCheckRandSubBox = false;

	private struct STCommandTime		// #24063 2011.1.16 yyagi コマンド入力時刻の記録用
	{
		public EInstrumentPart eInst;		// 使用楽器
		public EPadFlag ePad;		// 押されたコマンド(同時押しはOR演算で列挙する)
		public long time;				// コマンド入力時刻
	}
	public class CCommandHistory		// #24063 2011.1.16 yyagi コマンド入力履歴を保持_確認するクラス
	{
		readonly int buffersize = 16;
		private List<STCommandTime> stct;

		public CCommandHistory()		// コンストラクタ
		{
			stct = new List<STCommandTime>( buffersize );
		}

		/// <summary>
		/// コマンド入力履歴へのコマンド追加
		/// </summary>
		/// <param name="_eInst">楽器の種類</param>
		/// <param name="_ePad">入力コマンド(同時押しはOR演算で列挙すること)</param>
		public void Add( EInstrumentPart _eInst, EPadFlag _ePad )
		{
			STCommandTime _stct = new STCommandTime {
				eInst = _eInst,
				ePad = _ePad,
				time = CDTXMania.Timer.nCurrentTime
			};

			if ( stct.Count >= buffersize )
			{
				stct.RemoveAt( 0 );
			}
			stct.Add(_stct);
//Debug.WriteLine( "CMDHIS: 楽器=" + _stct.eInst + ", CMD=" + _stct.ePad + ", time=" + _stct.time );
		}
		public void RemoveAt( int index )
		{
			stct.RemoveAt( index );
		}

		/// <summary>
		/// コマンド入力に成功しているか調べる
		/// </summary>
		/// <param name="_ePad">入力が成功したか調べたいコマンド</param>
		/// <param name="_eInst">対象楽器</param>
		/// <returns>コマンド入力成功時true</returns>
		public bool CheckCommand( EPadFlag[] _ePad, EInstrumentPart _eInst)
		{
			int targetCount = _ePad.Length;
			int stciCount = stct.Count;
			if ( stciCount < targetCount )
			{
//Debug.WriteLine("NOT start checking...stciCount=" + stciCount + ", targetCount=" + targetCount);
				return false;
			}

			long curTime = CDTXMania.Timer.nCurrentTime;
//Debug.WriteLine("Start checking...targetCount=" + targetCount);
			for ( int i = targetCount - 1, j = stciCount - 1; i >= 0; i--, j-- )
			{
				if ( _ePad[ i ] != stct[ j ].ePad )
				{
//Debug.WriteLine( "CMD解析: false targetCount=" + targetCount + ", i=" + i + ", j=" + j + ": ePad[]=" + _ePad[i] + ", stci[j] = " + stct[j].ePad );
					return false;
				}
				if ( stct[ j ].eInst != _eInst )
				{
//Debug.WriteLine( "CMD解析: false " + i );
					return false;
				}
				if ( curTime - stct[ j ].time > 500 )
				{
//Debug.WriteLine( "CMD解析: false " + i + "; over 500ms" );
					return false;
				}
				curTime = stct[ j ].time;
			}

//Debug.Write( "CMD解析: 成功!(" + _ePad.Length + ") " );
//for ( int i = 0; i < _ePad.Length; i++ ) Debug.Write( _ePad[ i ] + ", " );
//Debug.WriteLine( "" );
			//stct.RemoveRange( 0, targetCount );			// #24396 2011.2.13 yyagi 
			stct.Clear();									// #24396 2011.2.13 yyagi Clear all command input history in case you succeeded inputting some command

			return true;
		}
	}
	public CCommandHistory CommandHistory;

	private void tMoveCursorDown()  // tカーソルを下へ移動する
	{
		CDTXMania.Skin.soundCursorMovement.tPlay();
		actSongList.tMoveToNext();
	}
	private void tMoveCursorUp()  // tカーソルを上へ移動する
	{
		CDTXMania.Skin.soundCursorMovement.tPlay();
		actSongList.tMoveToPrevious();
	}
	private void tSelectSongRandomly()
	{
		CSongListNode song = actSongList.rSelectedSong;
		if( ( song.stackRandomPerformanceNumber.Count == 0 ) || ( song.listランダム用ノードリスト == null ) )
		{
			if( song.listランダム用ノードリスト == null )
			{
				song.listランダム用ノードリスト = t指定された曲が存在する場所の曲を列挙する_子リスト含む( song );
			}
			int count = song.listランダム用ノードリスト.Count;
			if( count == 0 )
			{
				tUpdateSearchNotification(string.Format("Random Song List is empty for {0} mode",
					CDTXMania.ConfigIni.bDrumsEnabled ? "Drum" : "Guitar/Bass"
				));
				ctSearchInputDisplayCounter.tStart(0, 1, 10000, CDTXMania.Timer);
				return;
			}
			int[] numArray = new int[ count ];
			for( int i = 0; i < count; i++ )
			{
				numArray[ i ] = i;
			}
			for( int j = 0; j < ( count * 1.5 ); j++ )
			{
				int index = CDTXMania.Random.Next( count );
				int num5 = CDTXMania.Random.Next( count );
				int num6 = numArray[ num5 ];
				numArray[ num5 ] = numArray[ index ];
				numArray[ index ] = num6;
			}
			for( int k = 0; k < count; k++ )
			{
				song.stackRandomPerformanceNumber.Push( numArray[ k ] );
			}
			if( CDTXMania.ConfigIni.bLogDTX詳細ログ出力 )
			{
				StringBuilder builder = new StringBuilder( 0x400 );
				builder.Append( string.Format( "ランダムインデックスリストを作成しました: {0}曲: ", song.stackRandomPerformanceNumber.Count ) );
				for( int m = 0; m < count; m++ )
				{
					builder.Append( string.Format( "{0} ", numArray[ m ] ) );
				}
				Trace.TraceInformation( builder.ToString() );
			}
		}
		rConfirmedSong = song.listランダム用ノードリスト[ song.stackRandomPerformanceNumber.Pop() ];
		nConfirmedSongDifficulty = actSongList.n現在のアンカ難易度レベルに最も近い難易度レベルを返す( rConfirmedSong );
		rChosenScore = rConfirmedSong.arScore[ nConfirmedSongDifficulty ];
		eReturnValueWhenFadeOutCompleted = EReturnValue.Selected;
		//	this.actFOtoNowLoading.tStartFadeOut();					// #27787 2012.3.10 yyagi 曲決定時の画面フェードアウトの省略
		ePhaseID = EPhase.選曲_NowLoading画面へのフェードアウト;
		if( CDTXMania.ConfigIni.bLogDTX詳細ログ出力 )
		{
			int[] numArray2 = song.stackRandomPerformanceNumber.ToArray();
			StringBuilder builder2 = new StringBuilder( 0x400 );
			builder2.Append( "ランダムインデックスリスト残り: " );
			if( numArray2.Length > 0 )
			{
				for( int n = 0; n < numArray2.Length; n++ )
				{
					builder2.Append( string.Format( "{0} ", numArray2[ n ] ) );
				}
			}
			else
			{
				builder2.Append( "(なし)" );
			}
			Trace.TraceInformation( builder2.ToString() );
		}
		CDTXMania.Skin.bgm選曲画面.t停止する();
	}
	private void tSelectSong()  // t曲を選択する
	{
		rConfirmedSong = actSongList.rSelectedSong;
		rChosenScore = actSongList.rSelectedScore;
		nConfirmedSongDifficulty = actSongList.n現在選択中の曲の現在の難易度レベル;

		bool bScoreExistForMode = tCheckScoreExistForMode(rChosenScore);
		if ( ( rConfirmedSong != null ) && ( rChosenScore != null ) && bScoreExistForMode)
		{
			eReturnValueWhenFadeOutCompleted = EReturnValue.Selected;
			ePhaseID = EPhase.選曲_NowLoading画面へのフェードアウト;
		}
		else
		{
			tUpdateSearchNotification($"Score unavailable for {(CDTXMania.ConfigIni.bDrumsEnabled ? "Drum" : "Guitar/Bass")} mode");
			ctSearchInputDisplayCounter.tStart(0, 1, 10000, CDTXMania.Timer);
		}
		CDTXMania.Skin.bgm選曲画面.t停止する();
	}

	private bool tCheckScoreExistForMode(CScore score)
	{
		bool bScoreExistForMode = CDTXMania.ConfigIni.bDrumsEnabled && score.SongInformation.bScoreExists.Drums;
		if (!bScoreExistForMode)
		{
			bScoreExistForMode = CDTXMania.ConfigIni.bGuitarEnabled &&
			                     (score.SongInformation.bScoreExists.Guitar || score.SongInformation.bScoreExists.Bass);
		}
		return bScoreExistForMode;
	}

	private void tResetRandomListForNode(CSongListNode song)
	{
		//If songNode is null, start from root and recursively call each child
		if (song == null && CDTXMania.SongManager.listSongRoot.Count > 0)
		{
			foreach (CSongListNode cSong in CDTXMania.SongManager.listSongRoot)
			{
				tResetRandomListForNode(cSong);
			}
		}
		else
		{
			song.listランダム用ノードリスト = null;
			song.stackRandomPerformanceNumber = new Stack<int>();
			if(song.listChildNodes != null)
			{
				foreach (CSongListNode cSong in song.listChildNodes)
				{
					tResetRandomListForNode(cSong);
				}
			}
		}
	}

	private List<CSongListNode> t指定された曲が存在する場所の曲を列挙する_子リスト含む( CSongListNode song )
	{
		List<CSongListNode> list = [];
		song = song.parentNode;
		if( ( song == null ) && ( CDTXMania.SongManager.listSongRoot.Count > 0 ) )
		{
			foreach( CSongListNode c曲リストノード in CDTXMania.SongManager.listSongRoot )
			{
				if( ( c曲リストノード.eNodeType == CSongListNode.ENodeType.SCORE ) || ( c曲リストノード.eNodeType == CSongListNode.ENodeType.SCORE_MIDI ) )
				{
					//Check that at least one score is available for current game mode
					bool bAtLeastOneScoreExist = false;
					for (int i = 0; i < 5; i++)
					{
						if(c曲リストノード.arScore[i] != null)
						{
							bAtLeastOneScoreExist = tCheckScoreExistForMode(c曲リストノード.arScore[i]);
							if(bAtLeastOneScoreExist)
							{
								break;
							}
						}
					}

					//Add to list only if score exist for current mode
					if(bAtLeastOneScoreExist)
					{
						list.Add(c曲リストノード);
					}						
				}
				if( ( c曲リストノード.listChildNodes != null ) && CDTXMania.ConfigIni.bランダムセレクトで子BOXを検索対象とする )
				{
					t指定された曲の子リストの曲を列挙する_孫リスト含む( c曲リストノード, ref list );
				}
			}
			return list;
		}
		t指定された曲の子リストの曲を列挙する_孫リスト含む( song, ref list );
		return list;
	}
	private void t指定された曲の子リストの曲を列挙する_孫リスト含む( CSongListNode r親, ref List<CSongListNode> list )
	{
		if( ( r親 != null ) && ( r親.listChildNodes != null ) )
		{
			foreach( CSongListNode c曲リストノード in r親.listChildNodes )
			{
				if( ( c曲リストノード.eNodeType == CSongListNode.ENodeType.SCORE ) || ( c曲リストノード.eNodeType == CSongListNode.ENodeType.SCORE_MIDI ) )
				{
					//Check that at least one score is available for current game mode
					bool bAtLeastOneScoreExist = false;
					for (int i = 0; i < 5; i++)
					{
						if (c曲リストノード.arScore[i] != null)
						{
							bAtLeastOneScoreExist = tCheckScoreExistForMode(c曲リストノード.arScore[i]);
							if (bAtLeastOneScoreExist)
							{
								break;
							}
						}
					}
					//Add to list only if score exist for current mode
					if (bAtLeastOneScoreExist)
					{
						list.Add(c曲リストノード);
					}
				}
				if( ( c曲リストノード.listChildNodes != null ) && CDTXMania.ConfigIni.bランダムセレクトで子BOXを検索対象とする )
				{
					t指定された曲の子リストの曲を列挙する_孫リスト含む( c曲リストノード, ref list );
				}
			}
		}
	}

	public void tUpdateSearchNotification(string strNotification)
	{
		CDTXMania.tDisposeSafely(ref txSearchInputNotification);

		//
		if(strNotification != "")
		{
			//using (Bitmap bmp = prvFontSearchInputNotification.DrawPrivateFont(strNotification,
			//CPrivateFont.DrawMode.Edge, Color.White, Color.White, Color.White, Color.White, true))
			using (Bitmap bmp = prvFontSearchInputNotification.DrawPrivateFont(strNotification, Color.White, Color.Black))
			{
				txSearchInputNotification = CDTXMania.tGenerateTexture(bmp);
			}
		}
		else
		{
			txSearchInputNotification = null;
		}			

	}

	//-----------------
	#endregion
}