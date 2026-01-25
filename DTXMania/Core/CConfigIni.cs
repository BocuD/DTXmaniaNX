using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using FDK;
using SlimDXKey = SlimDX.DirectInput.Key;

namespace DTXMania.Core;

internal class CConfigIni
{
	// Class

	public class CKeyAssign
	{
		public class CKeyAssignPad
		{
			public STKEYASSIGN[] HH
			{
				get => padHH_R;
				set => padHH_R = value;
			}
			public STKEYASSIGN[] R
			{
				get => padHH_R;
				set => padHH_R = value;
			}
			public STKEYASSIGN[] SD
			{
				get => padSD_G;
				set => padSD_G = value;
			}
			public STKEYASSIGN[] G
			{
				get => padSD_G;
				set => padSD_G = value;
			}
			public STKEYASSIGN[] BD
			{
				get => padBD_B;
				set => padBD_B = value;
			}
			public STKEYASSIGN[] B
			{
				get => padBD_B;
				set => padBD_B = value;
			}
			public STKEYASSIGN[] HT
			{
				get => padHT_Pick;
				set => padHT_Pick = value;
			}
			public STKEYASSIGN[] Pick
			{
				get => padHT_Pick;
				set => padHT_Pick = value;
			}
			public STKEYASSIGN[] LT
			{
				get => padLT_Wail;
				set => padLT_Wail = value;
			}
			public STKEYASSIGN[] Wail
			{
				get => padLT_Wail;
				set => padLT_Wail = value;
			}
			public STKEYASSIGN[] FT
			{
				get => padFT_Help;
				set => padFT_Help = value;
			}
			public STKEYASSIGN[] Help
			{
				get => padFT_Help;
				set => padFT_Help = value;
			}
			public STKEYASSIGN[] CY
			{
				get => padCY_Decide;
				set => padCY_Decide = value;
			}
			public STKEYASSIGN[] Decide
			{
				get => padCY_Decide;
				set => padCY_Decide = value;
			}
			public STKEYASSIGN[] HHO
			{
				get => padHHO_Y;
				set => padHHO_Y = value;
			}
			public STKEYASSIGN[] Y
			{
				get => padHHO_Y;
				set => padHHO_Y = value;
			}
			public STKEYASSIGN[] RD
			{
				get => padRD;
				set => padRD = value;
			}
			public STKEYASSIGN[] P
			{
				get => padLC_P;
				set => padLC_P = value;
			}
			public STKEYASSIGN[] LC
			{
				get => padLC_P;
				set => padLC_P = value;
			}
			public STKEYASSIGN[] LP
			{
				get => padLP;
				set => padLP = value;
			}

			public STKEYASSIGN[] LBD
			{
				get => padLBD;
				set => padLBD = value;
			}

			public STKEYASSIGN[] Cancel
			{
				get => padCancel;
				set => padCancel = value;
			}

			public STKEYASSIGN[] Capture
			{
				get => padCapture;
				set => padCapture = value;
			}

			public STKEYASSIGN[] Search
			{
				get => padSearch;
				set => padSearch = value;
			}
			public STKEYASSIGN[] LoopCreate
			{
				get => padLoopCreate;
				set => padLoopCreate = value;
			}
			public STKEYASSIGN[] LoopDelete
			{
				get => padLoopDelete;
				set => padLoopDelete = value;
			}
			public STKEYASSIGN[] SkipForward
			{
				get => padSkipForward;
				set => padSkipForward = value;
			}
			public STKEYASSIGN[] SkipBackward
			{
				get => padSkipBackward;
				set => padSkipBackward = value;
			}
			public STKEYASSIGN[] IncreasePlaySpeed
			{
				get => padIncreasePlaySpeed;
				set => padIncreasePlaySpeed = value;
			}
			public STKEYASSIGN[] DecreasePlaySpeed
			{
				get => padDecreasePlaySpeed;
				set => padDecreasePlaySpeed = value;
			}
			public STKEYASSIGN[] Restart
			{
				get => padRestart;
				set => padRestart = value;
			}
			public STKEYASSIGN[] this[ int index ]
			{
				get
				{
					switch ( index )
					{
						case (int) EKeyConfigPad.HH:
							return padHH_R;

						case (int) EKeyConfigPad.SD:
							return padSD_G;

						case (int) EKeyConfigPad.BD:
							return padBD_B;

						case (int) EKeyConfigPad.HT:
							return padHT_Pick;

						case (int) EKeyConfigPad.LT:
							return padLT_Wail;

						case (int) EKeyConfigPad.FT:
							return padFT_Help;

						case (int) EKeyConfigPad.CY:
							return padCY_Decide;

						case (int) EKeyConfigPad.HHO:
							return padHHO_Y;

						case (int) EKeyConfigPad.RD:
							return padRD;

						case (int) EKeyConfigPad.LC:
							return padLC_P;

						case (int) EKeyConfigPad.LP:	// #27029 2012.1.4 from
							return padLP;			//(HPからLPに。)

						case (int) EKeyConfigPad.LBD:
							return padLBD;

						case (int) EKeyConfigPad.Cancel:
							return padCancel;

						case (int) EKeyConfigPad.Capture:
							return padCapture;

						case (int)EKeyConfigPad.Search:
							return padSearch;

						case (int)EKeyConfigPad.LoopCreate:
							return padLoopCreate;

						case (int)EKeyConfigPad.LoopDelete:
							return padLoopDelete;

						case (int)EKeyConfigPad.SkipForward:
							return padSkipForward;

						case (int)EKeyConfigPad.SkipBackward:
							return padSkipBackward;

						case (int)EKeyConfigPad.IncreasePlaySpeed:
							return padIncreasePlaySpeed;

						case (int)EKeyConfigPad.DecreasePlaySpeed:
							return padDecreasePlaySpeed;

						case (int)EKeyConfigPad.Restart:
							return padRestart;
					}
					throw new IndexOutOfRangeException();
				}
				set
				{
					switch ( index )
					{
						case (int) EKeyConfigPad.HH:
							padHH_R = value;
							return;

						case (int) EKeyConfigPad.SD:
							padSD_G = value;
							return;

						case (int) EKeyConfigPad.BD:
							padBD_B = value;
							return;

						case (int) EKeyConfigPad.Pick:
							padHT_Pick = value;
							return;

						case (int) EKeyConfigPad.LT:
							padLT_Wail = value;
							return;

						case (int) EKeyConfigPad.FT:
							padFT_Help = value;
							return;

						case (int) EKeyConfigPad.CY:
							padCY_Decide = value;
							return;

						case (int) EKeyConfigPad.HHO:
							padHHO_Y = value;
							return;
                            
						case (int) EKeyConfigPad.RD:
							padRD = value;
							return;
                            
						case (int) EKeyConfigPad.LC:
							padLC_P = value;
							return;
                            
						case (int) EKeyConfigPad.LP:
							padLP = value;
							return;
                            
						case (int) EKeyConfigPad.LBD:
							padLBD = value;
							return;

						case (int) EKeyConfigPad.Cancel:
							padCancel = value;
							return;

						case (int) EKeyConfigPad.Capture:
							padCapture = value;
							return;

						case (int)EKeyConfigPad.Search:
							padSearch = value;
							return;

						case (int)EKeyConfigPad.LoopCreate:
							padLoopCreate = value;
							return;

						case (int)EKeyConfigPad.LoopDelete:
							padLoopDelete = value;
							return;

						case (int)EKeyConfigPad.SkipForward:
							padSkipForward = value;
							return;

						case (int)EKeyConfigPad.SkipBackward:
							padSkipBackward = value;
							return;

						case (int)EKeyConfigPad.IncreasePlaySpeed:
							padIncreasePlaySpeed = value;
							return;

						case (int)EKeyConfigPad.DecreasePlaySpeed:
							padDecreasePlaySpeed = value;
							return;

						case (int)EKeyConfigPad.Restart:
							padRestart = value;
							return;
					}
					throw new IndexOutOfRangeException();
				}
			}

			#region [ private ]
			//-----------------
			private STKEYASSIGN[] padBD_B;
			private STKEYASSIGN[] padCY_Decide;
			private STKEYASSIGN[] padFT_Help;
			private STKEYASSIGN[] padHH_R;
			private STKEYASSIGN[] padHHO_Y;
			private STKEYASSIGN[] padHT_Pick;
			private STKEYASSIGN[] padLC_P;
			private STKEYASSIGN[] padLT_Wail;
			private STKEYASSIGN[] padRD;
			private STKEYASSIGN[] padSD_G;
			private STKEYASSIGN[] padLP;
			private STKEYASSIGN[] padLBD;
			private STKEYASSIGN[] padCancel; 
			private STKEYASSIGN[] padCapture;
			private STKEYASSIGN[] padSearch;
			private STKEYASSIGN[] padLoopCreate;
			private STKEYASSIGN[] padLoopDelete;
			private STKEYASSIGN[] padSkipForward;
			private STKEYASSIGN[] padSkipBackward;
			private STKEYASSIGN[] padIncreasePlaySpeed;
			private STKEYASSIGN[] padDecreasePlaySpeed;
			private STKEYASSIGN[] padRestart;
			//-----------------
			#endregion
		}

		[StructLayout( LayoutKind.Sequential )]
		public struct STKEYASSIGN
		{
			public EInputDevice InputDevice;
			public int ID;
			public int Code;
			public STKEYASSIGN( EInputDevice DeviceType, int nID, int nCode )
			{
				InputDevice = DeviceType;
				ID = nID;
				Code = nCode;
			}
		}

		public CKeyAssignPad Bass = new CKeyAssignPad();
		public CKeyAssignPad Drums = new CKeyAssignPad();
		public CKeyAssignPad Guitar = new CKeyAssignPad();
		public CKeyAssignPad System = new CKeyAssignPad();
		public CKeyAssignPad this[ int index ]
		{
			get
			{
				switch( index )
				{
					case (int) EKeyConfigPart.DRUMS:
						return Drums;

					case (int) EKeyConfigPart.GUITAR:
						return Guitar;

					case (int) EKeyConfigPart.BASS:
						return Bass;

					case (int) EKeyConfigPart.SYSTEM:
						return System;
				}
				throw new IndexOutOfRangeException();
			}
			set
			{
				switch( index )
				{
					case (int) EKeyConfigPart.DRUMS:
						Drums = value;
						return;

					case (int) EKeyConfigPart.GUITAR:
						Guitar = value;
						return;

					case (int) EKeyConfigPart.BASS:
						Bass = value;
						return;

					case (int) EKeyConfigPart.SYSTEM:
						System = value;
						return;
				}
				throw new IndexOutOfRangeException();
			}
		}
	}
	public enum ESoundDeviceTypeForConfig
	{
		ACM = 0,
		ASIO,
		WASAPI,
		WASAPI_Share,
		Unknown = 99
	}

	// プロパティ

#if false		// #23625 2011.1.11 Config.iniからダメージ/回復値の定数変更を行う場合はここを有効にする 087リリースに合わせ機能無効化
		//----------------------------------------
		public float[,] fGaugeFactor = new float[5,2];
		public float[] fDamageLevelFactor = new float[3];
		//----------------------------------------
#endif
	public int nBGAlpha;
	public int nMovieAlpha;
	public bool bAVIEnabled;
	public bool bBGAEnabled;
	public bool bBGM音を発声する;
	public STDGBVALUE<bool> bHidden;
	public STDGBVALUE<bool> bLeft;
	public STDGBVALUE<bool> bLight;
	public STDGBVALUE<bool> bSpecialist; // 2024.02.22 Add Specialist mode for Guitar/Bass
	public bool bLogDTX詳細ログ出力;
	public bool bLogSongSearch;
	public bool bLog作成解放ログ出力;
	public STDGBVALUE<bool> bReverse;
	public bool bScoreIniを出力する;
	public bool bSTAGEFAILEDEnabled;
	public STDGBVALUE<bool> bSudden;
	public bool bTight;
	public STDGBVALUE<bool> bGraph有効;     // #24074 2011.01.23 add ikanick
	public bool bSmallGraph;
	public bool bWave再生位置自動調整機能有効;
	public bool bシンバルフリー;
	public bool bストイックモード;
	public bool bドラム打音を発声する;
	public bool bFillInEnabled;
	public bool bランダムセレクトで子BOXを検索対象とする;
	public bool bOutputLogs;
	public STDGBVALUE<bool> b演奏音を強調する;
	public bool bShowPerformanceInformation;
	public bool bAutoAddGage; //2012.9.18
	public bool b歓声を発声する;
	public bool bVerticalSyncWait;
	public bool b選曲リストフォントを斜体にする;
	public bool b選曲リストフォントを太字にする;
	//public bool bDirectShowMode;
	public bool bFullScreenMode;
	public bool bFullScreenExclusive;
	public int nInitialWindowXPosition; // #30675 2013.02.04 ikanick add
	public int nInitialWindowYPosition;
	public int nWindowWidth;				// #23510 2010.10.31 yyagi add
	public int nWindowHeight;				// #23510 2010.10.31 yyagi add
	public bool DisplayBonusEffects;
	public bool bHAZARD;
	public int nSoundDeviceType; // #24820 2012.12.23 yyagi 出力サウンドデバイス(0=ACM(にしたいが設計がきつそうならDirectShow), 1=ASIO, 2=WASAPI)
	public int nWASAPIBufferSizeMs; // #24820 2013.1.15 yyagi WASAPIのバッファサイズ
	//public int nASIOBufferSizeMs; // #24820 2012.12.28 yyagi ASIOのバッファサイズ
	public int nASIODevice; // #24820 2013.1.17 yyagi ASIOデバイス
	public bool bEventDrivenWASAPI;
	public bool bMetronome; // 2023.9.22 henryzx
	public bool bUseOSTimer;
	public bool bDynamicBassMixerManagement; // #24820
	public int nMasterVolume;
	public int nChipPlayTimeComputeMode; // 2024.2.17 fisyher (0=Original, 1=Accurate)

	public STDGBVALUE<EType> eAttackEffect;
	public STDGBVALUE<EType> eNumOfLanes;
	public STDGBVALUE<EType> eDkdkType;
	public STDGBVALUE<EType> eLaneType;
	public STDGBVALUE<EType> eLBDGraphics;
	public STDGBVALUE<EType> eHHOGraphics;
	public ERDPosition eRDPosition;
	public int nInfoType;
	public int nSkillMode;
	public Dictionary<int, string> joystickDict;
	public ECYGroup eCYGroup;
	public EDarkMode eDark;
	public EFTGroup eFTGroup;
	public EHHGroup eHHGroup;
	public EBDGroup eBDGroup;					// #27029 2012.1.4 from add
	public EPlaybackPriority eHitSoundPriorityCY;
	public EPlaybackPriority eHitSoundPriorityFT;
	public EPlaybackPriority eHitSoundPriorityHH;
	public EPlaybackPriority eHitSoundPriorityLP;
	public STDGBVALUE<ERandomMode> eRandom;
	public STDGBVALUE<ERandomMode> eRandomPedal;
	public STDGBVALUE<bool> bAssignToLBD;
	public EDamageLevel eDamageLevel;
	public CKeyAssign KeyAssign;

	public STDGBVALUE<int> nLaneDisp;
	public STDGBVALUE<bool> bDisplayJudge;
	public STDGBVALUE<bool> bJudgeLineDisp;
	public STDGBVALUE<bool> bLaneFlush;

	public int nPedalLagTime;   //#xxxxx 2013.07.11 kairera0467

	public int n非フォーカス時スリープms;       // #23568 2010.11.04 ikanick add
	public int nフレーム毎スリープms;			// #xxxxx 2011.11.27 yyagi add
	public int nPlaySpeed;
	public bool bSaveScoreIfModifiedPlaySpeed;
	public int nSongSelectSoundPreviewWaitTimeMs;
	public int nSongSelectImagePreviewWaitTimeMs;
	public int n自動再生音量;  // nAutoVolume
	public int n手動再生音量;  // nChipVolume
	public int n選曲リストフォントのサイズdot;
	public int[] nNameColor;
	public STDGBVALUE<int> n表示可能な最小コンボ数;
	public STDGBVALUE<int> nScrollSpeed;
	public string strDTXManiaのバージョン;
	public string strSongDataSearchPath;
	public string songListFont;
	public string[] strCardName; //2015.12.3 kaiera0467 DrumとGuitarとBassで名前を別々にするため、string[3]に変更。
	public string[] strGroupName;
	public EDrumComboTextDisplayPosition ドラムコンボ文字の表示位置;
	public bool bドラムコンボ文字の表示;
	public STDGBVALUE<EType> JudgementStringPosition;  // 判定文字表示位置
	public int nMovieMode;
	public STDGBVALUE<int> nJudgeLine;
	public STDGBVALUE<int> nShutterInSide;
	public STDGBVALUE<int> nShutterOutSide;
	public bool bClassicScoreDisplay;
	public bool bMutingLP;
	public bool bSkillModeを自動切換えする;

	public bool b曲名表示をdefのものにする;

	#region [ XGオプション ]
	public EType eNamePlate;
	public EType eドラムセットを動かす;
	public EType eBPMbar;
	public bool bLivePoint;
	public bool bSpeaker;
	#endregion

	#region [ GDオプション ]
	public bool bDisplayDifficultyXGStyle;
	public bool bShowMusicInfo;
	public bool bShowScore;
	public string str曲名表示フォント;
	#endregion

	#region[ 画像関連 ]
	public int nJudgeAnimeType;
	public int nJudgeFrames;
	public int nJudgeInterval;
	public int nJudgeWidgh;
	public int nJudgeHeight;
	public int nExplosionFrames;
	public int nExplosionInterval;
	public int nExplosionWidgh;
	public int nExplosionHeight;
	public int nWailingFireFrames;
	public int nWailingFireInterval;
	public int nWailingFireWidgh;
	public int nWailingFireHeight;
	public STDGBVALUE<int> nWailingFireX;
	public int nWailingFireY;
	#endregion

	public STDGBVALUE<int> nInputAdjustTimeMs;	// #23580 2011.1.3 yyagi タイミングアジャスト機能
	public int nCommonBGMAdjustMs;              // #36372 2016.06.19 kairera0467 全曲共通のBGMオフセット
	public STDGBVALUE<int> nJudgeLinePosOffset; // #31602 2013.6.23 yyagi 判定ライン表示位置のオフセット
	public int nShowLagType;					// #25370 2011.6.5 yyagi ズレ時間表示機能
	public int nShowLagTypeColor;
	public bool bShowLagHitCount;				// fisyher New Config to enable hit count display or not
	public int nShowPlaySpeed;
	public STDGBVALUE<int> nHidSud;
	public bool bIsAutoResultCapture;			// #25399 2011.6.9 yyagi リザルト画像自動保存機能のON/OFF制御
	public int nPoliphonicSounds;				// #28228 2012.5.1 yyagi レーン毎の最大同時発音数
	public bool bBufferedInput;
	public bool bIsEnabledSystemMenu;			// #28200 2012.5.1 yyagi System Menuの使用可否切替
	public string strSystemSkinSubfolderFullName;	// #28195 2012.5.2 yyagi Skin切替用 System/以下のサブフォルダ名
	public bool bUseBoxDefSkin;                     // #28195 2012.5.6 yyagi Skin切替用 box.defによるスキン変更機能を使用するか否か
	public int nSkipTimeMs;

	//つまみ食い
	public STDGBVALUE<EAutoGhostData> eAutoGhost;               // #35411 2015.8.18 chnmr0 プレー時使用ゴーストデータ種別
	public STDGBVALUE<ETargetGhostData> eTargetGhost;               // #35411 2015.8.18 chnmr0 ゴーストデータ再生方法

	public bool bConfigIniがないかDTXManiaのバージョンが異なる => ( !bConfigIniExists || !CDTXMania.VERSION.Equals( strDTXManiaのバージョン ) );

	public bool bDrumsEnabled
	{
		get => _bDrums有効;
		set
		{
			_bDrums有効 = value;
			if( !_bGuitar有効 && !_bDrums有効 )
			{
				_bGuitar有効 = true;
			}
		}
	}

	public bool bInstrumentAvailable(EInstrumentPart inst)
	{
		switch (inst)
		{
			case EInstrumentPart.DRUMS:
				return _bDrums有効;
			case EInstrumentPart.GUITAR:
			case EInstrumentPart.BASS:
				return _bGuitar有効;
			default:
				return false;
		}
	}

	public bool bGuitarEnabled
	{
		get => _bGuitar有効;
		set
		{
			_bGuitar有効 = value;
			if( !_bGuitar有効 && !_bDrums有効 )
			{
				_bDrums有効 = true;
			}
		}
	}
	public bool bWindowMode
	{
		get => !bFullScreenMode;
		set => bFullScreenMode = !value;
	}
	public bool bGuitarRevolutionMode => ( !bDrumsEnabled && bGuitarEnabled );

	public bool bAllDrumsAreAutoPlay
	{
		get
		{
			for (int i = (int) ELane.LC; i < (int) ELane.LBD; i++)
			{
				if( !bAutoPlay[ i ] )
				{
					return false;
				}
			}
			return true;
		}
	}
	public bool bAllGuitarsAreAutoPlay
	{
		get
		{
			for ( int i = (int) ELane.GtR; i <= (int) ELane.GtPick; i++ )
			{
				if ( !bAutoPlay[ i ] )
				{
					return false;
				}
			}
			return true;
		}
	}
	public bool bAllBassAreAutoPlay
	{
		get
		{
			for ( int i = (int) ELane.BsR; i <= (int) ELane.BsPick; i++ )
			{
				if ( !bAutoPlay[ i ] )
				{
					return false;
				}
			}
			return true;
		}
	}

	public bool bIsAutoPlay(EInstrumentPart inst)
	{
		bool result = false;
		switch (inst)
		{
			case EInstrumentPart.DRUMS:
				result = bAllDrumsAreAutoPlay;
				break;
			case EInstrumentPart.GUITAR:
				result = bAllGuitarsAreAutoPlay;
				break;
			case EInstrumentPart.BASS:
				result = bAllBassAreAutoPlay;
				break;
		}
		return result;
	}

	public bool bHidePerformanceInformation
	{
		get => !bShowPerformanceInformation;
		set => bShowPerformanceInformation = !value;
	}
	public int nBackgroundTransparency
	{
		get => nBGAlpha;
		set
		{
			if( value < 0 )
			{
				nBGAlpha = 0;
			}
			else if( value > 0xff )
			{
				nBGAlpha = 0xff;
			}
			else
			{
				nBGAlpha = value;
			}
		}
	}
	public int nRisky;						// #23559 2011.6.20 yyagi Riskyでの残ミス数。0で閉店
	public bool bIsAllowedDoubleClickFullscreen;	// #26752 2011.11.27 yyagi ダブルクリックしてもフルスクリーンに移行しない
	public bool bIsSwappedGuitarBass			// #24063 2011.1.16 yyagi ギターとベースの切り替え中か否か
	{
		get;
		set;
	}
	public bool bIsSwappedGuitarBass_AutoFlagsAreSwapped	// #24415 2011.2.21 yyagi FLIP中にalt-f4終了で、AUTOフラグがswapした状態でconfig.iniが出力されてしまうことを避けるためのフラグ
	{
		get;
		set;
	}
	public bool bTimeStretch;					// #23664 2013.2.24 yyagi ピッチ変更無しで再生速度を変更するかどうか
	public STAUTOPLAY bAutoPlay;

	/// <summary>
	/// The <see cref="STHitRanges"/> for all drum chips, except pedals.
	/// </summary>
	public STHitRanges stDrumHitRanges;

	/// <summary>
	/// The <see cref="STHitRanges"/> for drum pedal chips.
	/// </summary>
	public STHitRanges stDrumPedalHitRanges;

	/// <summary>
	/// The <see cref="STHitRanges"/> for guitar chips.
	/// </summary>
	public STHitRanges stGuitarHitRanges;

	/// <summary>
	/// The <see cref="STHitRanges"/> for bass guitar chips.
	/// </summary>
	public STHitRanges stBassHitRanges;

	/// <summary>
	/// Whether or not Discord Rich Presence integration is enabled.
	/// </summary>
	public bool bDiscordRichPresenceEnabled;

	/// <summary>
	/// The unique client identifier of the Discord Application to use for Discord Rich Presence integration.
	/// </summary>
	public string strDiscordRichPresenceApplicationID;

	/// <summary>
	/// The unique identifier of the large image to display alongside presences.
	/// </summary>
	public string strDiscordRichPresenceLargeImageKey;

	/// <summary>
	/// The unique identifier of the small image to display alongside presences when playing in drum mode.
	/// </summary>
	public string strDiscordRichPresenceSmallImageKeyDrums;

	/// <summary>
	/// The unique identifier of the small image to display alongside presences when playing in guitar mode.
	/// </summary>
	public string strDiscordRichPresenceSmallImageKeyGuitar;

	public STLANEVALUE nVelocityMin;
	[StructLayout( LayoutKind.Sequential )]
	public struct STLANEVALUE
	{
		public int LC;
		public int HH;
		public int SD;
		public int BD;
		public int HT;
		public int LT;
		public int FT;
		public int CY;
		public int RD;
		public int LP;
		public int LBD;
		public int Guitar;
		public int Bass;
		public int this[ int index ]
		{
			get
			{
				switch( index )
				{
					case 0:
						return LC;

					case 1:
						return HH;

					case 2:
						return SD;

					case 3:
						return BD;

					case 4:
						return HT;

					case 5:
						return LT;

					case 6:
						return FT;

					case 7:
						return CY;

					case 8:
						return RD;

					case 9:
						return LP;

					case 10:
						return LBD;

					case 11:
						return Guitar;

					case 12:
						return Bass;
				}
				throw new IndexOutOfRangeException();
			}
			set
			{
				switch( index )
				{
					case 0:
						LC = value;
						return;

					case 1:
						HH = value;
						return;

					case 2:
						SD = value;
						return;

					case 3:
						BD = value;
						return;

					case 4:
						HT = value;
						return;

					case 5:
						LT = value;
						return;

					case 6:
						FT = value;
						return;

					case 7:
						CY = value;
						return;

					case 8:
						RD = value;
						return;

					case 9:
						LP = value;
						return;

					case 10:
						LBD = value;
						return;

					case 11:
						Guitar = value;
						return;

					case 12:
						Bass = value;
						return;
				}
				throw new IndexOutOfRangeException();
			}
		}
	}

	/*
	// #27029 2012.1.5 from:
	// BDGroup が FP|BD→FP&BD に変化した際に自動変化するパラメータの値のバックアップ。FP&BD→FP|BD の時に元に戻す。
	// これらのバックアップ値は、BDGroup が FP&BD 状態の時にのみ Config.ini に出力され、BDGroup が FP|BD に戻ったら Config.ini から消える。
	public class CBackupOf1BD
	{
		public EHHGroup eHHGroup = EHHGroup.全部打ち分ける;
		public EPlaybackPriority eHitSoundPriorityHH = EPlaybackPriority.ChipOverPadPriority;
	}
	public CBackupOf1BD BackupOf1BD = null;
	*/
	public void SwapGuitarBassInfos_AutoFlags()
	{
		//bool ts = CDTXMania.ConfigIni.bAutoPlay.Bass;			// #24415 2011.2.21 yyagi: FLIP時のリザルトにAUTOの記録が混ざらないよう、AUTOのフラグもswapする
		//CDTXMania.ConfigIni.bAutoPlay.Bass = CDTXMania.ConfigIni.bAutoPlay.Guitar;
		//CDTXMania.ConfigIni.bAutoPlay.Guitar = ts;

		int looptime = (int)ELane.GtW - (int)ELane.GtR + 1;		// #29390 2013.1.25 yyagi ギターのAutoLane/AutoPick対応に伴い、FLIPもこれに対応
		for (int i = 0; i < looptime; i++)							// こんなに離れたところを独立して修正しなければならない設計ではいけませんね___
		{
			bool b = CDTXMania.ConfigIni.bAutoPlay[i + (int)ELane.BsR];
			CDTXMania.ConfigIni.bAutoPlay[i + (int)ELane.BsR] = CDTXMania.ConfigIni.bAutoPlay[i + (int)ELane.GtR];
			CDTXMania.ConfigIni.bAutoPlay[i + (int)ELane.GtR] = b;
		}

		CDTXMania.ConfigIni.bIsSwappedGuitarBass_AutoFlagsAreSwapped = !CDTXMania.ConfigIni.bIsSwappedGuitarBass_AutoFlagsAreSwapped;
	}

	public EInstrumentPart GetFlipInst(EInstrumentPart inst)
	{
		EInstrumentPart retPart = inst;
		if (bIsSwappedGuitarBass)
		{
			switch (inst)
			{
				case EInstrumentPart.GUITAR:
					retPart = EInstrumentPart.BASS;
					break;
				case EInstrumentPart.BASS:
					retPart = EInstrumentPart.GUITAR;
					break;
			}
		}
		return retPart;
	}

	// コンストラクタ

	public CConfigIni()
	{
#if false		// #23625 2011.1.11 Config.iniからダメージ/回復値の定数変更を行う場合はここを有効にする 087リリースに合わせ機能無効化
			//----------------------------------------
			this.fGaugeFactor[0,0] =  0.004f;
			this.fGaugeFactor[0,1] =  0.006f;
			this.fGaugeFactor[1,0] =  0.002f;
			this.fGaugeFactor[1,1] =  0.003f;
			this.fGaugeFactor[2,0] =  0.000f;
			this.fGaugeFactor[2,1] =  0.000f;
			this.fGaugeFactor[3,0] = -0.020f;
			this.fGaugeFactor[3,1] = -0.030f;
			this.fGaugeFactor[4,0] = -0.050f;
			this.fGaugeFactor[4,1] = -0.050f;

			this.fDamageLevelFactor[0] = 0.5f;
			this.fDamageLevelFactor[1] = 1.0f;
			this.fDamageLevelFactor[2] = 1.5f;
			//----------------------------------------
#endif
		strDTXManiaのバージョン = "Unknown";
		strSongDataSearchPath = @".\";
		bFullScreenMode = false;
		bFullScreenExclusive = true;
		bVerticalSyncWait = true;
		nInitialWindowXPosition = 0; // #30675 2013.02.04 ikanick add
		nInitialWindowYPosition = 0;
		//this.bDirectShowMode = true;
		nWindowWidth = GameFramebufferSize.Width;			// #23510 2010.10.31 yyagi add
		nWindowHeight = GameFramebufferSize.Height;			// 
		nMovieMode = 1;
		nMovieAlpha = 0;
		nJudgeLine.Drums = 0;
		nJudgeLine.Guitar = 0;
		nJudgeLine.Bass = 0;
		nShutterInSide = new STDGBVALUE<int>();
		nShutterInSide.Drums = 0;
		nShutterOutSide = new STDGBVALUE<int>();
		nShutterOutSide.Drums = 0;
		nフレーム毎スリープms = -1;			// #xxxxx 2011.11.27 yyagi add
		n非フォーカス時スリープms = 1;			// #23568 2010.11.04 ikanick add
		_bGuitar有効 = false;
		_bDrums有効 = true;
		nBGAlpha = 255;
		eDamageLevel = EDamageLevel.Normal;
		bSTAGEFAILEDEnabled = true;
		bAVIEnabled = true;
		bBGAEnabled = true;
		bFillInEnabled = true;
		DisplayBonusEffects = true;
		eRDPosition = ERDPosition.RCRD;
		nInfoType = 1;
		nSkillMode = 1;
		eAttackEffect.Drums = EType.A;
		eAttackEffect.Guitar = EType.A;
		eAttackEffect.Bass = EType.A;
		eLaneType = new STDGBVALUE<EType>();
		eLaneType.Drums = EType.A;
		eHHOGraphics = new STDGBVALUE<EType>();
		eHHOGraphics.Drums = EType.A;
		eLBDGraphics = new STDGBVALUE<EType>();
		eLBDGraphics.Drums = EType.A;
		eDkdkType = new STDGBVALUE<EType>();
		eDkdkType.Drums = EType.A;
		eNumOfLanes = new STDGBVALUE<EType>();
		eNumOfLanes.Drums = EType.A;
		eNumOfLanes.Guitar = EType.A;
		eNumOfLanes.Bass = EType.A;
		bAssignToLBD = default(STDGBVALUE<bool>);
		bAssignToLBD.Drums = false;
		eRandom = default(STDGBVALUE<ERandomMode>);
		eRandom.Drums = ERandomMode.OFF;
		eRandom.Guitar = ERandomMode.OFF;
		eRandom.Bass = ERandomMode.OFF;
		eRandomPedal = default(STDGBVALUE<ERandomMode>);
		eRandomPedal.Drums = ERandomMode.OFF;
		eRandomPedal.Guitar = ERandomMode.OFF;
		eRandomPedal.Bass = ERandomMode.OFF;
		nLaneDisp = new STDGBVALUE<int>();
		nLaneDisp.Drums = 0;
		nLaneDisp.Guitar = 0;
		nLaneDisp.Bass = 0;
		bDisplayJudge = new STDGBVALUE<bool>();
		bDisplayJudge.Drums = true;
		bDisplayJudge.Guitar = true;
		bDisplayJudge.Bass = true;
		bJudgeLineDisp = new STDGBVALUE<bool>();
		bJudgeLineDisp.Drums = true;
		bJudgeLineDisp.Guitar = true;
		bJudgeLineDisp.Bass = true;
		bLaneFlush = new STDGBVALUE<bool>();
		bLaneFlush.Drums = true;
		bLaneFlush.Guitar = true;
		bLaneFlush.Bass = true;

		strCardName = new string[ 3 ];
		strGroupName = new string[ 3 ];
		nNameColor = new int[ 3 ];

		#region[ 画像関連 ]
		nJudgeAnimeType = 1;
		nJudgeFrames = 24;
		nJudgeInterval = 14;
		nJudgeWidgh = 250;
		nJudgeHeight = 170;

		nExplosionFrames = 1;
		nExplosionInterval = 50;
		nExplosionWidgh = 0;
		nExplosionHeight = 0;

		nWailingFireFrames = 0;
		nWailingFireInterval = 0;
		nWailingFireWidgh = 0;
		nWailingFireHeight = 0;
		nWailingFireY = 0;
		#endregion

		nPedalLagTime = 0;

		bAutoAddGage = false;
		nSongSelectSoundPreviewWaitTimeMs = 50;
		nSongSelectImagePreviewWaitTimeMs = 0;
		bWave再生位置自動調整機能有効 = true;
		bBGM音を発声する = true;
		bドラム打音を発声する = true;
		b歓声を発声する = true;
		bScoreIniを出力する = true;
		bランダムセレクトで子BOXを検索対象とする = true;
		n表示可能な最小コンボ数 = new STDGBVALUE<int>();
		n表示可能な最小コンボ数.Drums = 10;
		n表示可能な最小コンボ数.Guitar = 2;
		n表示可能な最小コンボ数.Bass = 2;
		songListFont = "MS PGothic";
		n選曲リストフォントのサイズdot = 20;
		b選曲リストフォントを太字にする = true;
		n自動再生音量 = 80;
		n手動再生音量 = 100;
		bOutputLogs = true;
		b曲名表示をdefのものにする = true;
		b演奏音を強調する = new STDGBVALUE<bool>();
		bSudden = new STDGBVALUE<bool>();
		bHidden = new STDGBVALUE<bool>();
		bReverse = new STDGBVALUE<bool>();
		eRandom = new STDGBVALUE<ERandomMode>();
		bLight = new STDGBVALUE<bool>();
		bSpecialist = new STDGBVALUE<bool>();
		bLeft = new STDGBVALUE<bool>();
		JudgementStringPosition = new STDGBVALUE<EType>();
		nScrollSpeed = new STDGBVALUE<int>();
		nInputAdjustTimeMs = new STDGBVALUE<int>();	// #23580 2011.1.3 yyagi
		nCommonBGMAdjustMs = 0; // #36372 2016.06.19 kairera0467
		nJudgeLinePosOffset = new STDGBVALUE<int>(); // #31602 2013.6.23 yyagi
		for ( int i = 0; i < 3; i++ )
		{
			b演奏音を強調する[ i ] = true;
			bSudden[ i ] = false;
			bHidden[ i ] = false;
			bReverse[ i ] = false;
			eRandom[ i ] = ERandomMode.OFF;
			bLight[ i ] = true; //fisyher: Change to default true, following actual game
			bSpecialist[ i ] = false;
			bLeft[ i ] = false;
			JudgementStringPosition[ i ] = EType.A;
			nScrollSpeed[ i ] = 1;
			nInputAdjustTimeMs[ i ] = 0;
			nJudgeLinePosOffset[i] = 0;
		}
		nPlaySpeed = 20;
		bSaveScoreIfModifiedPlaySpeed = false;
		bSmallGraph = true;
		ドラムコンボ文字の表示位置 = EDrumComboTextDisplayPosition.RIGHT;
		bドラムコンボ文字の表示 = true;
		bClassicScoreDisplay = false;
		bSkillModeを自動切換えする = false;
		bMutingLP = true;
		#region [ AutoPlay ]
		bAutoPlay = new STAUTOPLAY();
		bAutoPlay.HH = false;
		bAutoPlay.SD = false;
		bAutoPlay.BD = false;
		bAutoPlay.HT = false;
		bAutoPlay.LT = false;
		bAutoPlay.FT = false;
		bAutoPlay.CY = false;
		bAutoPlay.RD = false;
		bAutoPlay.LC = false;
		bAutoPlay.LP = false;
		bAutoPlay.LBD = false;
		//this.bAutoPlay.Guitar = true;
		//this.bAutoPlay.Bass = true;
		bAutoPlay.GtR = false;
		bAutoPlay.GtG = false;
		bAutoPlay.GtB = false;
		bAutoPlay.GtY = false;
		bAutoPlay.GtP = false;
		bAutoPlay.GtPick = false;
		bAutoPlay.GtW = false;
		bAutoPlay.BsR = false;
		bAutoPlay.BsG = false;
		bAutoPlay.BsB = false;
		bAutoPlay.BsY = false;
		bAutoPlay.BsP = false;
		bAutoPlay.BsPick = false;
		bAutoPlay.BsW = false;
		#endregion

		#region [ HitRange ]

		stDrumHitRanges = STHitRanges.tCreateDefaultDTXHitRanges();
		stDrumPedalHitRanges = STHitRanges.tCreateDefaultDTXHitRanges();
		stGuitarHitRanges = STHitRanges.tCreateDefaultDTXHitRanges();
		stBassHitRanges = STHitRanges.tCreateDefaultDTXHitRanges();

		#endregion

		#region [ DiscordRichPresence ]
		bDiscordRichPresenceEnabled = false;
		strDiscordRichPresenceApplicationID = @"802329379979657257";
		strDiscordRichPresenceLargeImageKey = @"dtxmania";
		strDiscordRichPresenceSmallImageKeyDrums = @"drums";
		strDiscordRichPresenceSmallImageKeyGuitar = @"guitar";
		#endregion

		ConfigIniファイル名 = "";
		joystickDict = new Dictionary<int, string>( 10 );
		tSetDefaultKeyAssignments();
		#region [ velocityMin ]
		nVelocityMin.LC = 0;					// #23857 2011.1.31 yyagi VelocityMin
		nVelocityMin.HH = 20;
		nVelocityMin.SD = 0;
		nVelocityMin.BD = 0;
		nVelocityMin.HT = 0;
		nVelocityMin.LT = 0;
		nVelocityMin.FT = 0;
		nVelocityMin.CY = 0;
		nVelocityMin.RD = 0;
		nVelocityMin.LP = 0;
		nVelocityMin.LBD = 0;
		#endregion

		bHAZARD = false;
		nRisky = 0;							// #23539 2011.7.26 yyagi RISKYモード
		nShowLagType = (int) EShowLagType.OFF;	// #25370 2011.6.3 yyagi ズレ時間表示
		nShowLagTypeColor = 0;
		bShowLagHitCount = false;
		nShowPlaySpeed = (int)EShowPlaySpeed.IF_CHANGED_IN_GAME;
		bIsAutoResultCapture = true;			// #25399 2011.6.9 yyagi リザルト画像自動保存機能ON/OFF

		#region [ XGオプション ]
		bLivePoint = true;
		bSpeaker = true;
		eNamePlate = EType.A;
		#endregion

		#region [ GDオプション ]
		bDisplayDifficultyXGStyle = true;
		bShowScore = true;
		bShowMusicInfo = true;
		str曲名表示フォント = "MS PGothic";
		#endregion

		bBufferedInput = true;
		bIsSwappedGuitarBass = false;			// #24063 2011.1.16 yyagi ギターとベースの切り替え
		bIsAllowedDoubleClickFullscreen = true;	// #26752 2011.11.26 ダブルクリックでのフルスクリーンモード移行を許可
		eBDGroup = EBDGroup.打ち分ける;		// #27029 2012.1.4 from HHPedalとBassPedalのグルーピング
		nPoliphonicSounds = 4;                 // #28228 2012.5.1 yyagi レーン毎の最大同時発音数
		// #24820 2013.1.15 yyagi 初期値を4から2に変更。BASS.net使用時の負荷軽減のため。
		// #24820 2013.1.17 yyagi 初期値を4に戻した。動的なミキサー制御がうまく動作しているため。
		bIsEnabledSystemMenu = true;			// #28200 2012.5.1 yyagi System Menuの利用可否切替(使用可)
		strSystemSkinSubfolderFullName = "";	// #28195 2012.5.2 yyagi 使用中のSkinサブフォルダ名
		bUseBoxDefSkin = true;					// #28195 2012.5.6 yyagi box.defによるスキン切替機能を使用するか否か
		bTight = false;                        // #29500 2012.9.11 kairera0467
		nSoundDeviceType = (int)ESoundDeviceTypeForConfig.ACM; // #24820 2012.12.23 yyagi 初期値はACM
		nWASAPIBufferSizeMs = 0;               // #24820 2013.1.15 yyagi 初期値は0(自動設定)
		nASIODevice = 0;                       // #24820 2013.1.17 yyagi
//          this.nASIOBufferSizeMs = 0;                 // #24820 2012.12.25 yyagi 初期値は0(自動設定)
		bEventDrivenWASAPI = false;
		bUseOSTimer = false; ;                 // #33689 2014.6.6 yyagi 初期値はfalse (FDKのタイマー。ＦＲＯＭ氏考案の独自タイマー)
		bDynamicBassMixerManagement = true;    //
		nMasterVolume = 100;
		bTimeStretch = false;                  // #23664 2013.2.24 yyagi 初期値はfalse (再生速度変更を、ピッチ変更にて行う)
		nSkipTimeMs = 5000;
		nChipPlayTimeComputeMode = 1;			// 2024.2.17 fisyher Set to Accurate by default

	}
	public CConfigIni( string iniファイル名 )
		: this()
	{
		tReadFromFile( iniファイル名 );
	}


	// メソッド

	public void tDeleteAlreadyAssignedInputs( EInputDevice DeviceType, int nID, int nCode )
	{
		for( int i = 0; i <= (int)EKeyConfigPart.SYSTEM; i++ )
		{
			for( int j = 0; j < (int)EKeyConfigPad.MAX; j++ )
			{
				for( int k = 0; k < 0x10; k++ )
				{
					if( ( ( KeyAssign[ i ][ j ][ k ].InputDevice == DeviceType ) && ( KeyAssign[ i ][ j ][ k ].ID == nID ) ) && ( KeyAssign[ i ][ j ][ k ].Code == nCode ) )
					{
						for( int m = k; m < 15; m++ )
						{
							KeyAssign[ i ][ j ][ m ] = KeyAssign[ i ][ j ][ m + 1 ];
						}
						KeyAssign[ i ][ j ][ 15 ].InputDevice = EInputDevice.Unknown;
						KeyAssign[ i ][ j ][ 15 ].ID = 0;
						KeyAssign[ i ][ j ][ 15 ].Code = 0;
						k--;
					}
				}
			}
		}
	}
	public void tWrite( string iniFilename )
	{
		StreamWriter sw = new StreamWriter( iniFilename, false, Encoding.GetEncoding( "unicode" ) );
		sw.WriteLine( ";-------------------" );
			
		#region [ System ]
		sw.WriteLine( "[System]" );
		sw.WriteLine();

#if false		// #23625 2011.1.11 Config.iniからダメージ/回復値の定数変更を行う場合はここを有効にする 087リリースに合わせ機能無効化
	//------------------------------
			sw.WriteLine("; ライフゲージのパラメータ調整(調整完了後削除予定)");
			sw.WriteLine("; GaugeFactorD: ドラムのPerfect, Great,... の回復量(ライフMAXを1.0としたときの値を指定)");
			sw.WriteLine("; GaugeFactorG:  Gt/BsのPerfect, Great,... の回復量(ライフMAXを1.0としたときの値を指定)");
			sw.WriteLine("; DamageFactorD: DamageLevelがSmall, Normal, Largeの時に対するダメージ係数");
			sw.WriteLine("GaugeFactorD={0}, {1}, {2}, {3}, {4}", this.fGaugeFactor[0, 0], this.fGaugeFactor[1, 0], this.fGaugeFactor[2, 0], this.fGaugeFactor[3, 0], this.fGaugeFactor[4, 0]);
			sw.WriteLine("GaugeFactorG={0}, {1}, {2}, {3}, {4}", this.fGaugeFactor[0, 1], this.fGaugeFactor[1, 1], this.fGaugeFactor[2, 1], this.fGaugeFactor[3, 1], this.fGaugeFactor[4, 1]);
			sw.WriteLine("DamageFactor={0}, {1}, {2}", this.fDamageLevelFactor[0], this.fDamageLevelFactor[1], fDamageLevelFactor[2]);
			sw.WriteLine();
	//------------------------------
#endif
		#region [ Version ]
		sw.WriteLine( "; リリースバージョン" );
		sw.WriteLine( "; Release Version." );
		sw.WriteLine( "Version={0}", CDTXMania.VERSION );
		sw.WriteLine();
		#endregion
		#region [ DTXPath ]
		sw.WriteLine( "; 演奏データの格納されているフォルダへのパス。" );
		sw.WriteLine( @"; セミコロン(;)で区切ることにより複数のパスを指定できます。（例: d:\DTXFiles1\;e:\DTXFiles2\）" );
		sw.WriteLine( "; Pathes for DTX data." );
		sw.WriteLine( @"; You can specify many pathes separated with semicolon(;). (e.g. d:\DTXFiles1\;e:\DTXFiles2\)" );
		sw.WriteLine( "DTXPath={0}", strSongDataSearchPath );
		sw.WriteLine();
		#endregion
		sw.WriteLine("; プレイヤーネーム。");
		sw.WriteLine(@"; 演奏中のネームプレートに表示される名前を設定できます。");
		sw.WriteLine("; 英字、数字の他、ひらがな、カタカナ、半角カナ、漢字なども入力できます。");
		sw.WriteLine("; 入力されていない場合は「GUEST」と表示されます。");
		sw.WriteLine("CardNameDrums={0}", strCardName[ 0 ] );
		sw.WriteLine("CardNameGuitar={0}", strCardName[ 1 ] );
		sw.WriteLine("CardNameBass={0}", strCardName[ 2 ] );
		sw.WriteLine();
		sw.WriteLine("; グループ名っぽいあれ。");
		sw.WriteLine(@"; 演奏中のネームプレートに表示されるXG2でいうグループ名を設定できます。");
		sw.WriteLine("; 英字、数字の他、ひらがな、カタカナ、半角カナ、漢字なども入力できます。");
		sw.WriteLine("; 入力されていない場合は何も表示されません。");
		sw.WriteLine("GroupNameDrums={0}", strGroupName[ 0 ]);
		sw.WriteLine("GroupNameGuitar={0}", strGroupName[ 1 ]);
		sw.WriteLine("GroupNameBass={0}", strGroupName[ 2 ]);
		sw.WriteLine();
		sw.WriteLine("; ネームカラー");
		sw.WriteLine("; 0=白, 1=薄黄色, 2=黄色, 3=緑, 4=青, 5=紫 以下略。");
		sw.WriteLine("NameColorDrums={0}", nNameColor[ 0 ]);
		sw.WriteLine("NameColorGuitar={0}", nNameColor[ 1 ]);
		sw.WriteLine("NameColorBass={0}", nNameColor[ 2 ]);
		sw.WriteLine();
		sw.WriteLine("; クリップの表示位置");
		sw.WriteLine("; 0=表示しない, 1=全画面, 2=ウインドウ, 3=全画面&ウインドウ");
		sw.WriteLine("MovieMode={0}", nMovieMode);
		sw.WriteLine();
		sw.WriteLine("; レーンの透明度(名前に突っ込まないでください。)");
		sw.WriteLine("; 数値が高いほどレーンが薄くなります。");
		sw.WriteLine("; 0=0% 10=100%");
		sw.WriteLine("MovieAlpha={0}", nMovieAlpha);
		sw.WriteLine();
		sw.WriteLine("; プレイ中にHelpボタンを押したときに出てくる演奏情報の種類。");
		sw.WriteLine("; 0=デバッグ情報 1=判定情報");
		sw.WriteLine("InfoType={0}", nInfoType);
		sw.WriteLine();
		#region [ スキン関連 ]
		#region [ Skinパスの絶対パス→相対パス変換 ]
		Uri uriRoot = new Uri( Path.Combine( CDTXMania.executableDirectory, "System" + Path.DirectorySeparatorChar ) );
		Uri uriPath = new Uri( Path.Combine( strSystemSkinSubfolderFullName, "." + Path.DirectorySeparatorChar ) );
		string relPath = uriRoot.MakeRelativeUri( uriPath ).ToString();				// 相対パスを取得
		relPath = System.Web.HttpUtility.UrlDecode( relPath );						// デコードする
		relPath = relPath.Replace( '/', Path.DirectorySeparatorChar );	// 区切り文字が\ではなく/なので置換する
		#endregion
		sw.WriteLine( "; 使用するSkinのフォルダ名。" );
		sw.WriteLine( "; 例えば System\\Default\\Graphics\\... などの場合は、SkinPath=.\\Default\\ を指定します。" );
		sw.WriteLine( "; Skin folder path." );
		sw.WriteLine( "; e.g. System\\Default\\Graphics\\... -> Set SkinPath=.\\Default\\" );
		sw.WriteLine( "SkinPath={0}", relPath );
		sw.WriteLine();
		sw.WriteLine( "; box.defが指定するSkinに自動で切り替えるかどうか (0=切り替えない、1=切り替える)" );
		sw.WriteLine( "; Automatically change skin specified in box.def. (0=No 1=Yes)" );
		sw.WriteLine( "SkinChangeByBoxDef={0}", bUseBoxDefSkin? 1 : 0 );
		sw.WriteLine();
		#endregion
		#region [ Window関連 ]
		sw.WriteLine( "; 画面モード(0:ウィンドウ, 1:全画面)" );
		sw.WriteLine( "; Screen mode. (0:Window, 1:Fullscreen)" );
		sw.WriteLine( "FullScreen={0}", bFullScreenMode ? 1 : 0 );
		sw.WriteLine();
		sw.WriteLine("; Fullscreen mode uses DirectX exclusive mode instead of maximized window. (0:Maximized window, 1:Exclusive)");
		sw.WriteLine("FullScreenExclusive={0}", bFullScreenExclusive ? 1 : 0);
		sw.WriteLine();
		sw.WriteLine("; ウインドウモード時の画面幅");				// #23510 2010.10.31 yyagi add
		sw.WriteLine("; A width size in the window mode.");			//
		sw.WriteLine("WindowWidth={0}", nWindowWidth);		//
		sw.WriteLine();												//
		sw.WriteLine("; ウインドウモード時の画面高さ");				//
		sw.WriteLine("; A height size in the window mode.");		//
		sw.WriteLine("WindowHeight={0}", nWindowHeight);	//
		sw.WriteLine();												//
		sw.WriteLine("; ウィンドウモード時の位置X");				            // #30675 2013.02.04 ikanick add
		sw.WriteLine("; X position in the window mode.");			            //
		sw.WriteLine("WindowX={0}", nInitialWindowXPosition);			//
		sw.WriteLine();											            	//
		sw.WriteLine("; ウィンドウモード時の位置Y");			            	//
		sw.WriteLine("; Y position in the window mode.");	            	    //
		sw.WriteLine("WindowY={0}", nInitialWindowYPosition);   		//
		sw.WriteLine();												            //

		sw.WriteLine( "; ウインドウをダブルクリックした時にフルスクリーンに移行するか(0:移行しない,1:移行する)" );	// #26752 2011.11.27 yyagi
		sw.WriteLine( "; Whether double click to go full screen mode or not." );					//
		sw.WriteLine( "DoubleClickFullScreen={0}", bIsAllowedDoubleClickFullscreen? 1 : 0);	//
		sw.WriteLine();																				//
		sw.WriteLine( "; ALT+SPACEのメニュー表示を抑制するかどうか(0:抑制する 1:抑制しない)" );		// #28200 2012.5.1 yyagi
		sw.WriteLine( "; Whether ALT+SPACE menu would be masked or not.(0=masked 1=not masked)" );	//
		sw.WriteLine( "EnableSystemMenu={0}", bIsEnabledSystemMenu? 1 : 0 );					//
		sw.WriteLine();																				//
		sw.WriteLine("; 非フォーカス時のsleep値[ms]");	    			    // #23568 2011.11.04 ikanick add
		sw.WriteLine("; A sleep time[ms] while the window is inactive.");	//
		sw.WriteLine("BackSleep={0}", n非フォーカス時スリープms);		// そのまま引用（苦笑）
		sw.WriteLine();											        			//
		#endregion
		#region [ フレーム処理関連(VSync, フレーム毎のsleep) ]
		sw.WriteLine("; 垂直帰線同期(0:OFF,1:ON)");
		sw.WriteLine( "VSyncWait={0}", bVerticalSyncWait ? 1 : 0 );
		sw.WriteLine();
		sw.WriteLine("; フレーム毎のsleep値[ms] (-1でスリープ無し, 0以上で毎フレームスリープ。動画キャプチャ等で活用下さい)");	// #xxxxx 2011.11.27 yyagi add
		sw.WriteLine("; A sleep time[ms] per frame.");							//
		sw.WriteLine("SleepTimePerFrame={0}", nフレーム毎スリープms); //
		sw.WriteLine();											        			//
		#endregion
		#region [ WASAPI/ASIO関連 ]
		sw.WriteLine("; サウンド出力方式(0=ACM(って今はまだDirectShowですが), 1=ASIO, 2=WASAPI排他, 3=WASAPI共有");
		sw.WriteLine("; WASAPIはVista以降のOSで使用可能。推奨方式はWASAPI。");
		sw.WriteLine("; なお、WASAPIが使用不可ならASIOを、ASIOが使用不可ならACMを使用します。");
		sw.WriteLine("; Sound device type(0=ACM, 1=ASIO, 2=WASAPI Exclusive, 3=WASAPI Shared)");
		sw.WriteLine("; WASAPI can use on Vista or later OSs.");
		sw.WriteLine("; If WASAPI is not available, DTXMania try to use ASIO. If ASIO can't be used, ACM is used.");
		sw.WriteLine("SoundDeviceType={0}", (int)nSoundDeviceType);
		sw.WriteLine();

		sw.WriteLine("; WASAPI使用時のサウンドバッファサイズ");
		sw.WriteLine("; (0=デバイスに設定されている値を使用, 1～9999=バッファサイズ(単位:ms)の手動指定");
		sw.WriteLine("; WASAPI Sound Buffer Size.");
		sw.WriteLine("; (0=Use system default buffer size, 1-9999=specify the buffer size(ms) by yourself)");
		sw.WriteLine("WASAPIBufferSizeMs={0}", (int)nWASAPIBufferSizeMs);
		sw.WriteLine();

		sw.WriteLine("; ASIO使用時のサウンドデバイス");
		sw.WriteLine("; 存在しないデバイスを指定すると、DTXManiaが起動しないことがあります。");
		sw.WriteLine("; Sound device used by ASIO.");
		sw.WriteLine("; Don't specify unconnected device, as the DTXMania may not bootup.");
		string[] asiodev = CEnumerateAllAsioDevices.GetAllASIODevices();
		for (int i = 0; i < asiodev.Length; i++)
		{
			sw.WriteLine("; {0}: {1}", i, asiodev[i]);
		}
		sw.WriteLine("ASIODevice={0}", (int)nASIODevice);
		sw.WriteLine();

		//sw.WriteLine( "; ASIO使用時のサウンドバッファサイズ" );
		//sw.WriteLine( "; (0=デバイスに設定されている値を使用, 1～9999=バッファサイズ(単位:ms)の手動指定" );
		//sw.WriteLine( "; ASIO Sound Buffer Size." );
		//sw.WriteLine( "; (0=Use the value specified to the device, 1-9999=specify the buffer size(ms) by yourself)" );
		//sw.WriteLine( "ASIOBufferSizeMs={0}", (int) this.nASIOBufferSizeMs );
		//sw.WriteLine();

		//sw.WriteLine("; Bass.Mixの制御を動的に行うか否か。");
		//sw.WriteLine("; ONにすると、ギター曲などチップ音の多い曲も再生できますが、画面が少しがたつきます。");
		//sw.WriteLine("; (0=行わない, 1=行う)");
		//sw.WriteLine("DynamicBassMixerManagement={0}", this.bDynamicBassMixerManagement ? 1 : 0);
		//sw.WriteLine();

		sw.WriteLine("; WASAPI/ASIO時に使用する演奏タイマーの種類");
		sw.WriteLine("; Playback timer used for WASAPI/ASIO");
		sw.WriteLine("; (0=FDK Timer, 1=System Timer)");
		sw.WriteLine("SoundTimerType={0}", bUseOSTimer ? 1 : 0);
		sw.WriteLine();

		sw.WriteLine("; WASAPI使用時にEventDrivenモードを使う");
		sw.WriteLine("EventDrivenWASAPI={0}", bEventDrivenWASAPI ? 1 : 0);
		sw.WriteLine();

		sw.WriteLine("; Enable Embedded Metronome");
		sw.WriteLine("; Please make sure Metronome.ogg exists in Your current skin sounds folder");
		sw.WriteLine("; e.g. ./System/{Skin}/Sounds/Metronome.ogg");
		sw.WriteLine("Metronome={0}", bMetronome ? 1 : 0);
		sw.WriteLine();

		sw.WriteLine("; Chip PlayTime Compute Mode");
		sw.WriteLine("; Select which method of Chip PlayTime Computation to use: (0=Original, 1=Accurate)");
		sw.WriteLine("; Original method is compatible with other DTXMania players but loses time due to integer truncation");
		sw.WriteLine("; Accurate method improves overall accuracy by using proper number rounding");
		sw.WriteLine("; NOTE: Only songs with many BPM changes will have observable difference in either mode. Single BPM songs are not affected");
		sw.WriteLine("ChipPlayTimeComputeMode={0}", nChipPlayTimeComputeMode);
		sw.WriteLine();

		sw.WriteLine("; 全体ボリュームの設定");
		sw.WriteLine("; (0=無音 ～ 100=最大。WASAPI/ASIO時のみ有効)");
		sw.WriteLine("; Master volume settings");
		sw.WriteLine("; (0=Silent - 100=Max)");
		sw.WriteLine("MasterVolume={0}", nMasterVolume);
		sw.WriteLine();

		#endregion
		#region [ ギター/ベース/ドラム 有効/無効 ]
		sw.WriteLine( "; ギター/ベース有効(0:OFF,1:ON)" );
		sw.WriteLine( "; Enable Guitar/Bass or not.(0:OFF,1:ON)" );
		sw.WriteLine( "Guitar={0}", bGuitarEnabled ? 1 : 0 );
		sw.WriteLine();
		sw.WriteLine( "; ドラム有効(0:OFF,1:ON)" );
		sw.WriteLine( "; Enable Drums or not.(0:OFF,1:ON)" );
		sw.WriteLine( "Drums={0}", bDrumsEnabled ? 1 : 0 );
		sw.WriteLine();
		#endregion
		sw.WriteLine( "; 背景画像の半透明割合(0:透明～255:不透明)" );
		sw.WriteLine( "; Transparency for background image in playing screen.(0:tranaparent - 255:no transparent)" );
		sw.WriteLine( "BGAlpha={0}", nBGAlpha );
		sw.WriteLine();
		sw.WriteLine( "; Missヒット時のゲージ減少割合(0:少, 1:Normal, 2:大)" );
		sw.WriteLine( "DamageLevel={0}", (int) eDamageLevel );
		sw.WriteLine();
		sw.WriteLine("; ゲージゼロでSTAGE FAILED (0:OFF, 1:ON)");
		sw.WriteLine("StageFailed={0}", bSTAGEFAILEDEnabled ? 1 : 0);
		sw.WriteLine();
		#region [ 打ち分け関連 ]
		sw.WriteLine("; LC/HHC/HHO 打ち分けモード (0:LC|HHC|HHO, 1:LC&(HHC|HHO), 2:LC|(HHC&HHO), 3:LC&HHC&HHO)");
		sw.WriteLine("; LC/HHC/HHO Grouping       (0:LC|HHC|HHO, 1:LC&(HHC|HHO), 2:LC|(HHC&HHO), 3:LC&HHC&HHO)");
		sw.WriteLine("HHGroup={0}", (int)eHHGroup);
		sw.WriteLine();
		sw.WriteLine("; LT/FT 打ち分けモード (0:LT|FT, 1:LT&FT)");
		sw.WriteLine("; LT/FT Grouping       (0:LT|FT, 1:LT&FT)");
		sw.WriteLine("FTGroup={0}", (int)eFTGroup);
		sw.WriteLine();
		sw.WriteLine("; CY/RD 打ち分けモード (0:CY|RD, 1:CY&RD)");
		sw.WriteLine("; CY/RD Grouping       (0:CY|RD, 1:CY&RD)");
		sw.WriteLine("CYGroup={0}", (int)eCYGroup);
		sw.WriteLine();
		sw.WriteLine( "; LP/LBD/BD 打ち分けモード(0:LP|LBD|BD, 1:LP|(LBD&BD), 2:LP&(LBD|BD), 3:LP&LBD&BD)" );		// #27029 2012.1.4 from
		sw.WriteLine( "; LP/LBD/BD Grouping     (0:LP|LBD|BD, 1:LP(LBD&BD), 2:LP&(LBD|BD), 3:LP&LBD&BD)");
		sw.WriteLine( "BDGroup={0}", (int) eBDGroup );				// 
		sw.WriteLine();													//
		sw.WriteLine( "; 打ち分け時の再生音の優先順位(HHGroup)(0:Chip>Pad, 1:Pad>Chip)" );
		sw.WriteLine( "HitSoundPriorityHH={0}", (int) eHitSoundPriorityHH );
		sw.WriteLine();
		sw.WriteLine( "; 打ち分け時の再生音の優先順位(FTGroup)(0:Chip>Pad, 1:Pad>Chip)" );
		sw.WriteLine( "HitSoundPriorityFT={0}", (int) eHitSoundPriorityFT );
		sw.WriteLine();
		sw.WriteLine( "; 打ち分け時の再生音の優先順位(CYGroup)(0:Chip>Pad, 1:Pad>Chip)" );
		sw.WriteLine( "HitSoundPriorityCY={0}", (int) eHitSoundPriorityCY );
		sw.WriteLine();
		sw.WriteLine( "; 打ち分け時の再生音の優先順位(LPGroup)(0:Chip>Pad, 1:Pad>Chip)");
		sw.WriteLine( "HitSoundPriorityLP={0}", (int) eHitSoundPriorityLP);
		sw.WriteLine();
		sw.WriteLine("; シンバルフリーモード(0:OFF, 1:ON)");
		sw.WriteLine("CymbalFree={0}", bシンバルフリー ? 1 : 0);
		sw.WriteLine();
		#endregion
		#region [ AVI/BGA ]
		sw.WriteLine( "; AVIの表示(0:OFF, 1:ON)" );
		sw.WriteLine( "AVI={0}", bAVIEnabled ? 1 : 0 );
		sw.WriteLine();
		sw.WriteLine( "; BGAの表示(0:OFF, 1:ON)" );
		sw.WriteLine( "BGA={0}", bBGAEnabled ? 1 : 0 );
		sw.WriteLine();
		#endregion
		#region [ フィルイン ]
		sw.WriteLine( "; フィルイン効果(0:OFF, 1:ON)" );
		sw.WriteLine( "FillInEffect={0}", bFillInEnabled ? 1 : 0 );
		sw.WriteLine();
		sw.WriteLine("; フィルイン達成時の歓声の再生(0:OFF, 1:ON)");
		sw.WriteLine("AudienceSound={0}", b歓声を発声する ? 1 : 0);
		sw.WriteLine();
		#endregion     
		#region [ プレビュー音 ]
		sw.WriteLine( "; 曲選択からプレビュー音の再生までのウェイト[ms]" );
		sw.WriteLine( "PreviewSoundWait={0}", nSongSelectSoundPreviewWaitTimeMs );
		sw.WriteLine();
		sw.WriteLine( "; 曲選択からプレビュー画像表示までのウェイト[ms]" );
		sw.WriteLine( "PreviewImageWait={0}", nSongSelectImagePreviewWaitTimeMs );
		sw.WriteLine();
		#endregion
		sw.WriteLine( "; Waveの再生位置自動補正(0:OFF, 1:ON)" );
		sw.WriteLine( "AdjustWaves={0}", bWave再生位置自動調整機能有効 ? 1 : 0 );
		sw.WriteLine();
		#region [ BGM/ドラムヒット音の再生 ]
		sw.WriteLine( "; BGM の再生(0:OFF, 1:ON)" );
		sw.WriteLine( "BGMSound={0}", bBGM音を発声する ? 1 : 0 );
		sw.WriteLine();
		sw.WriteLine( "; ドラム打音の再生(0:OFF, 1:ON)" );
		sw.WriteLine( "HitSound={0}", bドラム打音を発声する ? 1 : 0 );
		sw.WriteLine();
		#endregion
		sw.WriteLine( "; 演奏記録（～.score.ini）の出力 (0:OFF, 1:ON)" );
		sw.WriteLine( "SaveScoreIni={0}", bScoreIniを出力する ? 1 : 0 );
		sw.WriteLine();
		sw.WriteLine( "; RANDOM SELECT で子BOXを検索対象に含める (0:OFF, 1:ON)" );
		sw.WriteLine( "RandomFromSubBox={0}", bランダムセレクトで子BOXを検索対象とする ? 1 : 0 );
		sw.WriteLine();
		#region [ モニターサウンド(ヒット音の再生音量アップ) ]
		sw.WriteLine( "; ドラム演奏時にドラム音を強調する (0:OFF, 1:ON)" );
		sw.WriteLine( "SoundMonitorDrums={0}", b演奏音を強調する.Drums ? 1 : 0 );
		sw.WriteLine();
		sw.WriteLine( "; ギター演奏時にギター音を強調する (0:OFF, 1:ON)" );
		sw.WriteLine( "SoundMonitorGuitar={0}", b演奏音を強調する.Guitar ? 1 : 0 );
		sw.WriteLine();
		sw.WriteLine( "; ベース演奏時にベース音を強調する (0:OFF, 1:ON)" );
		sw.WriteLine( "SoundMonitorBass={0}", b演奏音を強調する.Bass ? 1 : 0 );
		sw.WriteLine();
		#endregion
		sw.WriteLine( "; 表示可能な最小コンボ数(1～99999)" );
		sw.WriteLine( "; ギターとベースでは0にするとコンボを表示しません。" );
		sw.WriteLine( "MinComboDrums={0}", n表示可能な最小コンボ数.Drums );
		sw.WriteLine( "MinComboGuitar={0}", n表示可能な最小コンボ数.Guitar );
		sw.WriteLine( "MinComboBass={0}", n表示可能な最小コンボ数.Bass );
		sw.WriteLine();
		sw.WriteLine( "; 曲名表示をdefファイルの曲名にする (0:OFF, 1:ON)" );
		sw.WriteLine( "MusicNameDispDef={0}", b曲名表示をdefのものにする ? 1 : 0 );
		sw.WriteLine();
		sw.WriteLine( "; 演奏情報を表示する (0:OFF, 1:ON)" );
		sw.WriteLine( "; Showing playing info on the playing screen. (0:OFF, 1:ON)" );
		sw.WriteLine( "ShowDebugStatus={0}", bShowPerformanceInformation ? 1 : 0 );
		sw.WriteLine();
		#region [ GDオプション ]
		sw.WriteLine( "; 選曲画面の難易度表示をXG表示にする (0:OFF, 1:ON)");
		sw.WriteLine( "Difficulty={0}", bDisplayDifficultyXGStyle ? 1 : 0);
		sw.WriteLine();
		sw.WriteLine("; スコアの表示(0:OFF, 1:ON)");
		sw.WriteLine("ShowScore={0}", bShowScore ? 1 : 0);
		sw.WriteLine();
		sw.WriteLine("; 演奏中の曲情報の表示(0:OFF, 1:ON)");
		sw.WriteLine("ShowMusicInfo={0}", bShowMusicInfo ? 1 : 0);
		sw.WriteLine();
		sw.WriteLine("; Show custom play speed (0:OFF, 1:ON, 2:If changed in game)");    //
		sw.WriteLine("ShowPlaySpeed={0}", nShowPlaySpeed);                         //
		sw.WriteLine();
		sw.WriteLine("; 読み込み画面、演奏画面、ネームプレート、リザルト画面の曲名で使用するフォント名");
		sw.WriteLine("DisplayFontName={0}", str曲名表示フォント);
		sw.WriteLine();
		#endregion
		#region [ 選曲リストのフォント ]
		sw.WriteLine( "; 選曲リストのフォント名" );
		sw.WriteLine( "; Font name for select song item." );
		sw.WriteLine( "SelectListFontName={0}", songListFont );
		sw.WriteLine();
		sw.WriteLine( "; 選曲リストのフォントのサイズ[dot]" );
		sw.WriteLine( "; Font size[dot] for select song item." );
		sw.WriteLine( "SelectListFontSize={0}", n選曲リストフォントのサイズdot );
		sw.WriteLine();
		sw.WriteLine( "; 選曲リストのフォントを斜体にする (0:OFF, 1:ON)" );
		sw.WriteLine( "; Using italic font style select song list. (0:OFF, 1:ON)" );
		sw.WriteLine( "SelectListFontItalic={0}", b選曲リストフォントを斜体にする ? 1 : 0 );
		sw.WriteLine();
		sw.WriteLine( "; 選曲リストのフォントを太字にする (0:OFF, 1:ON)" );
		sw.WriteLine( "; Using bold font style select song list. (0:OFF, 1:ON)" );
		sw.WriteLine( "SelectListFontBold={0}", b選曲リストフォントを太字にする ? 1 : 0 );
		sw.WriteLine();
		#endregion
		sw.WriteLine( "; 打音の音量(0～100%)" );
		sw.WriteLine( "; Sound volume (you're playing) (0-100%)" );
		sw.WriteLine( "ChipVolume={0}", n手動再生音量 );
		sw.WriteLine();
		sw.WriteLine( "; 自動再生音の音量(0～100%)" );
		sw.WriteLine( "; Sound volume (auto playing) (0-100%)" );
		sw.WriteLine( "AutoChipVolume={0}", n自動再生音量 );
		sw.WriteLine();
		sw.WriteLine( "; ストイックモード(0:OFF, 1:ON)" );
		sw.WriteLine( "; Stoic mode. (0:OFF, 1:ON)" );
		sw.WriteLine( "StoicMode={0}", bストイックモード ? 1 : 0 );
		sw.WriteLine();
		sw.WriteLine( "; バッファ入力モード(0:OFF, 1:ON)" );
		sw.WriteLine( "; Using Buffered input (0:OFF, 1:ON)" );
		sw.WriteLine( "BufferedInput={0}", bBufferedInput ? 1 : 0 );
		sw.WriteLine();
		sw.WriteLine("; オープンハイハットの表示画像(0:DTXMania仕様, 1:○なし, 2:クローズハットと同じ)");
		sw.WriteLine("HHOGraphics={0}", (int)eHHOGraphics.Drums);
		sw.WriteLine();
		sw.WriteLine("; 左バスペダルの表示画像(0:バス寄り, 1:LPと同じ)");
		sw.WriteLine("LBDGraphics={0}", (int)eLBDGraphics.Drums);
		sw.WriteLine();
		sw.WriteLine("; ライドシンバルレーンの表示位置(0:...RD RC, 1:...RC RD)");
		sw.WriteLine("RDPosition={0}", (int)eRDPosition);
		sw.WriteLine();
		sw.WriteLine( "; レーン毎の最大同時発音数(1～8)" );
		sw.WriteLine( "; Number of polyphonic sounds per lane. (1-8)" );
		sw.WriteLine( "PolyphonicSounds={0}", nPoliphonicSounds );
		sw.WriteLine();
		sw.WriteLine( "; 判定ズレ時間表示(0:OFF, 1:ON, 2=GREAT-POOR)" );				// #25370 2011.6.3 yyagi
		sw.WriteLine( "; Whether displaying the lag times from the just timing or not." );	//
		sw.WriteLine( "ShowLagTime={0}", nShowLagType );							//
		sw.WriteLine();
		sw.WriteLine("; 判定ズレ時間表示の色(0:Slow赤、Fast青, 1:Slow青、Fast赤)");
		sw.WriteLine( "ShowLagTimeColor={0}", nShowLagTypeColor );                         //
		sw.WriteLine();
		sw.WriteLine("; 判定ズレヒット数表示(0:OFF, 1:ON)");
		sw.WriteLine("ShowLagHitCount={0}", bShowLagHitCount ? 1 : 0);                         //
		sw.WriteLine();
		sw.WriteLine( "; リザルト画像自動保存機能(0:OFF, 1:ON)" );						// #25399 2011.6.9 yyagi
		sw.WriteLine( "; Set ON if you'd like to save result screen image automatically");	//
		sw.WriteLine( "; when you get hiscore/hiskill.");								//
		sw.WriteLine( "AutoResultCapture={0}", bIsAutoResultCapture? 1 : 0 );		//
		sw.WriteLine();
		sw.WriteLine("; 再生速度変更を、ピッチ変更で行うかどうか(0:ピッチ変更, 1:タイムストレッチ");	// #23664 2013.2.24 yyagi
		sw.WriteLine("; (WASAPI/ASIO使用時のみ有効) ");
		sw.WriteLine("; Set \"0\" if you'd like to use pitch shift with PlaySpeed.");	//
		sw.WriteLine("; Set \"1\" for time stretch.");								//
		sw.WriteLine("; (Only available when you're using using WASAPI or ASIO)");	//
		sw.WriteLine("TimeStretch={0}", bTimeStretch ? 1 : 0);					//
		sw.WriteLine();
		#region [ Adjust ]
		sw.WriteLine("; 判定タイミング調整(ドラム, ギター, ベース)(-99～99)[ms]");		// #23580 2011.1.3 yyagi
		sw.WriteLine("; Revision value to adjust judgement timing for the drums, guitar and bass.");	//
		sw.WriteLine("InputAdjustTimeDrums={0}", nInputAdjustTimeMs.Drums);		//
		sw.WriteLine("InputAdjustTimeGuitar={0}", nInputAdjustTimeMs.Guitar);		//
		sw.WriteLine("InputAdjustTimeBass={0}", nInputAdjustTimeMs.Bass);			//
		sw.WriteLine();

		sw.WriteLine( "; BGMタイミング調整(-99～99)[ms]" );                              // #36372 2016.06.19 kairera0467
		sw.WriteLine( "; Revision value to adjust judgement timing for BGM." );	        //
		sw.WriteLine( "BGMAdjustTime={0}", nCommonBGMAdjustMs );		            //
		sw.WriteLine();

		sw.WriteLine("; 判定ラインの表示位置調整(ドラム, ギター, ベース)(-99～99)[px]"); // #31602 2013.6.23 yyagi 判定ラインの表示位置オフセット
		sw.WriteLine("; Offset value to adjust displaying judgement line for the drums, guitar and bass."); //
		sw.WriteLine("JudgeLinePosOffsetDrums={0}", nJudgeLinePosOffset.Drums); //
		sw.WriteLine("JudgeLinePosOffsetGuitar={0}", nJudgeLinePosOffset.Guitar); //
		sw.WriteLine("JudgeLinePosOffsetBass={0}", nJudgeLinePosOffset.Bass); //
		sw.WriteLine();
		#endregion
		sw.WriteLine( "; LC, HH, SD,...の入力切り捨て下限Velocity値(0～127)" );			// #23857 2011.1.31 yyagi
		sw.WriteLine( "; Minimum velocity value for LC, HH, SD, ... to accept." );		//
		sw.WriteLine( "LCVelocityMin={0}", nVelocityMin.LC );						//
		sw.WriteLine("HHVelocityMin={0}", nVelocityMin.HH );						//
//			sw.WriteLine("; ハイハット以外の入力切り捨て下限Velocity値(0～127)");			// #23857 2010.12.12 yyagi
//			sw.WriteLine("; Minimum velocity value to accept. (except HiHat)");				//
//			sw.WriteLine("VelocityMin={0}", this.n切り捨て下限Velocity);					//
//			sw.WriteLine();																	//
		sw.WriteLine( "SDVelocityMin={0}", nVelocityMin.SD );						//
		sw.WriteLine( "BDVelocityMin={0}", nVelocityMin.BD );						//
		sw.WriteLine( "HTVelocityMin={0}", nVelocityMin.HT );						//
		sw.WriteLine( "LTVelocityMin={0}", nVelocityMin.LT );						//
		sw.WriteLine( "FTVelocityMin={0}", nVelocityMin.FT );						//
		sw.WriteLine( "CYVelocityMin={0}", nVelocityMin.CY );						//
		sw.WriteLine( "RDVelocityMin={0}", nVelocityMin.RD );						//
		sw.WriteLine( "LPVelocityMin={0}", nVelocityMin.LP );
		sw.WriteLine( "LBDVelocityMin={0}",nVelocityMin.LBD ); 
		sw.WriteLine();																	//
		sw.WriteLine( "; オート時のゲージ加算(0:OFF, 1:ON )");
		sw.WriteLine( "AutoAddGage={0}", bAutoAddGage ? 1 : 0);
		sw.WriteLine();
		sw.WriteLine("; Number of milliseconds to skip forward/backward (100-10000)");
		sw.WriteLine("SkipTimeMs={0}", nSkipTimeMs);
		sw.WriteLine();

		sw.WriteLine( ";-------------------" );
		#endregion

		#region [ Log ]
		sw.WriteLine( "[Log]" );
		sw.WriteLine();
		sw.WriteLine( "; Log出力(0:OFF, 1:ON)" );
		sw.WriteLine( "OutputLog={0}", bOutputLogs ? 1 : 0 );
		sw.WriteLine();
		sw.WriteLine( "; 曲データ検索に関するLog出力(0:OFF, 1:ON)" );
		sw.WriteLine( "TraceSongSearch={0}", bLogSongSearch ? 1 : 0 );
		sw.WriteLine();
		sw.WriteLine( "; 画像やサウンドの作成_解放に関するLog出力(0:OFF, 1:ON)" );
		sw.WriteLine( "TraceCreatedDisposed={0}", bLog作成解放ログ出力 ? 1 : 0 );
		sw.WriteLine();
		sw.WriteLine( "; DTX読み込み詳細に関するLog出力(0:OFF, 1:ON)" );
		sw.WriteLine( "TraceDTXDetails={0}", bLogDTX詳細ログ出力 ? 1 : 0 );
		sw.WriteLine();
		sw.WriteLine( ";-------------------" );
		#endregion

		#region [ PlayOption ]
		sw.WriteLine( "[PlayOption]" );
		sw.WriteLine();
		sw.WriteLine( "; REVERSEモード(0:OFF, 1:ON)" );
		sw.WriteLine( "DrumsReverse={0}", bReverse.Drums ? 1 : 0 );
		sw.WriteLine( "GuitarReverse={0}", bReverse.Guitar ? 1 : 0 );
		sw.WriteLine( "BassReverse={0}", bReverse.Bass ? 1 : 0 );
		sw.WriteLine();
		sw.WriteLine( "; ギター/ベースRANDOMモード(0:OFF, 1:Mirror, 2:Random, 3:SuperRandom, 4:HyperRandom)" );
		sw.WriteLine( "GuitarRandom={0}", (int) eRandom.Guitar );
		sw.WriteLine( "BassRandom={0}", (int) eRandom.Bass );
		sw.WriteLine();
		sw.WriteLine( "; ギター/ベースLIGHTモード(0:OFF, 1:ON)" );
		sw.WriteLine( "GuitarLight={0}", bLight.Guitar ? 1 : 0 );
		sw.WriteLine( "BassLight={0}", bLight.Bass ? 1 : 0 );
		sw.WriteLine();
		sw.WriteLine("; ギター/ベース演奏モード(0:Normal, 1:Specialist)");
		sw.WriteLine("GuitarSpecialist={0}", bSpecialist.Guitar ? 1 : 0);
		sw.WriteLine("BassSpecialist={0}", bSpecialist.Bass ? 1 : 0);
		sw.WriteLine();
		sw.WriteLine( "; ギター/ベースLEFTモード(0:OFF, 1:ON)" );
		sw.WriteLine( "GuitarLeft={0}", bLeft.Guitar ? 1 : 0 );
		sw.WriteLine( "BassLeft={0}", bLeft.Bass ? 1 : 0 );
		sw.WriteLine();
		sw.WriteLine( "; RISKYモード(0:OFF, 1-10)" );									// #23559 2011.6.23 yyagi
		sw.WriteLine( "; RISKY mode. 0=OFF, 1-10 is the times of misses to be Failed." );	//
		sw.WriteLine( "Risky={0}", nRisky );			//
		sw.WriteLine();
		sw.WriteLine("; HAZARDモード(0:OFF, 1:ON)");									// #23559 2011.6.23 yyagi
		sw.WriteLine("; HAZARD mode. 0=OFF, 1=ON is the times of misses to be Failed.");	//
		sw.WriteLine("HAZARD={0}", bHAZARD ? 1 : 0);			//
		sw.WriteLine();
		sw.WriteLine("; TIGHTモード(0:OFF, 1:ON)");									// #29500 2012.9.11 kairera0467
		sw.WriteLine(": TIGHT mode. 0=OFF, 1=ON ");
		sw.WriteLine("DrumsTight={0}", bTight ? 1 : 0 );									//
		sw.WriteLine();
		sw.WriteLine("; Hidden/Suddenモード(0:OFF, 1:HIDDEN, 2:SUDDEN, 3:HID/SUD, 4:STEALTH)");
		sw.WriteLine("; Hidden/Sudden mode. 0=OFF, 1=HIDDEN, 2=SUDDEN, 3=HID/SUD, 4=STEALTH ");
		sw.WriteLine("DrumsHiddenSudden={0}", (int)nHidSud.Drums);
		sw.WriteLine("GuitarHiddenSudden={0}", (int)nHidSud.Guitar);
		sw.WriteLine("BassHiddenSudden={0}", (int)nHidSud.Bass);
		sw.WriteLine();
		sw.WriteLine( "; ドラム判定文字表示位置(0:OnTheLane,1:判定ライン上,2:表示OFF)" );
		sw.WriteLine( "DrumsPosition={0}", (int) JudgementStringPosition.Drums );
		sw.WriteLine();
		sw.WriteLine( "; ギター/ベース判定文字表示位置(0:OnTheLane, 1:レーン横, 2:判定ライン上, 3:表示OFF)" );
		sw.WriteLine( "GuitarPosition={0}", (int) JudgementStringPosition.Guitar );
		sw.WriteLine( "BassPosition={0}", (int) JudgementStringPosition.Bass );
		sw.WriteLine();
		sw.WriteLine( "; 譜面スクロール速度(0:x0.5, 1:x1.0, 2:x1.5,…,1999:x1000.0)" );
		sw.WriteLine( "DrumsScrollSpeed={0}", nScrollSpeed.Drums );
		sw.WriteLine( "GuitarScrollSpeed={0}", nScrollSpeed.Guitar );
		sw.WriteLine( "BassScrollSpeed={0}", nScrollSpeed.Bass );
		sw.WriteLine();
		sw.WriteLine( "; 演奏速度(5～40)(→x5/20～x40/20)" );
		sw.WriteLine( "PlaySpeed={0}", nPlaySpeed );
		sw.WriteLine();
		sw.WriteLine("; Save score when PlaySpeed is not 100% (0:OFF, 1:ON)");
		sw.WriteLine("SaveScoreIfModifiedPlaySpeed={0}", bSaveScoreIfModifiedPlaySpeed ? 1 : 0);
		sw.WriteLine();

		// #24074 2011.01.23 add ikanick
		sw.WriteLine( "; グラフ表示(0:OFF, 1:ON)" );
		sw.WriteLine( "DrumGraph={0}", bGraph有効.Drums ? 1 : 0 );
		sw.WriteLine( "GuitarGraph={0}", bGraph有効.Guitar ? 1 : 0 );
		sw.WriteLine( "BassGraph={0}", bGraph有効.Bass ? 1 : 0 );
		sw.WriteLine();

		sw.WriteLine("; Small Graph (0:OFF, 1:ON)");
		sw.WriteLine("SmallGraph={0}", bSmallGraph ? 1 : 0);
		sw.WriteLine();

		sw.WriteLine( "; ドラムコンボの表示(0:OFF, 1:ON)" );									// #29500 2012.9.11 kairera0467
		sw.WriteLine( ": DrumPart Display Combo. 0=OFF, 1=ON " );
		sw.WriteLine( "DrumComboDisp={0}", bドラムコンボ文字の表示 ? 1 : 0 );				//
		sw.WriteLine();

		//fork
		// #35411 2015.8.18 chnmr0 add
		sw.WriteLine("; AUTOゴースト種別 (0:PERFECT, 1:LAST_PLAY, 2:HI_SKILL, 3:HI_SCORE)" );
		sw.WriteLine("DrumAutoGhost={0}", (int)eAutoGhost.Drums);
		sw.WriteLine("GuitarAutoGhost={0}", (int)eAutoGhost.Guitar);
		sw.WriteLine("BassAutoGhost={0}", (int)eAutoGhost.Bass);
		sw.WriteLine();
		sw.WriteLine("; ターゲットゴースト種別 (0:NONE, 1:PERFECT, 2:LAST_PLAY, 3:HI_SKILL, 4:HI_SCORE)");
		sw.WriteLine("DrumTargetGhost={0}", (int)eTargetGhost.Drums);
		sw.WriteLine("GuitarTargetGhost={0}", (int)eTargetGhost.Guitar);
		sw.WriteLine("BassTargetGhost={0}", (int)eTargetGhost.Bass);
		sw.WriteLine();

		#region[DTXManiaXG追加オプション]
		sw.WriteLine("; 譜面仕様変更(0:デフォルト10レーン, 1:XG9レーン, 2:CLASSIC6レーン)");
		sw.WriteLine("NumOfLanes={0}", (int)eNumOfLanes.Drums);
		sw.WriteLine();
		sw.WriteLine("; dkdk仕様変更(0:デフォルト, 1:始動足変更, 2:dkdk1レーン化)");
		sw.WriteLine("DkdkType={0}", (int)eDkdkType.Drums);
		sw.WriteLine();
		sw.WriteLine("; バスをLBDに振り分け(0:OFF, 1:ON)");
		sw.WriteLine("AssignToLBD={0}", bAssignToLBD.Drums ? 1 : 0);
		sw.WriteLine();
		sw.WriteLine("; ドラムパッドRANDOMモード(0:OFF, 1:Mirror, 2:Random, 3:SuperRandom, 4:HyperRandom, 5:MasterRandom, 6:AnotherRandom)");
		sw.WriteLine("DrumsRandomPad={0}", (int)eRandom.Drums);
		sw.WriteLine();
		sw.WriteLine("; ドラム足RANDOMモード(0:OFF, 1:Mirror, 2:Random, 3:SuperRandom, 4:HyperRandom, 5:MasterRandom, 6:AnotherRandom)");
		sw.WriteLine("DrumsRandomPedal={0}", (int)eRandomPedal.Drums);
		sw.WriteLine();
		sw.WriteLine("; LP消音機能(0:OFF, 1:ON)");
		sw.WriteLine("MutingLP={0}", bMutingLP ? 1 : 0);
		sw.WriteLine();
		#endregion
		#region[ DTXHD追加オプション ]
		sw.WriteLine("; 判定ライン(0～100)" );
		sw.WriteLine("DrumsJudgeLine={0}", (int)nJudgeLine.Drums);
		sw.WriteLine("GuitarJudgeLine={0}", (int)nJudgeLine.Guitar);
		sw.WriteLine("BassJudgeLine={0}", (int)nJudgeLine.Bass);
		sw.WriteLine();
		#endregion
		#region[ ver.K追加オプション ]
		#region [ XGオプション ]
		sw.WriteLine("; ネームプレートタイプ");
		sw.WriteLine("; 0:タイプA XG2風の表示がされます。 ");
		sw.WriteLine("; 1:タイプB XG風の表示がされます。このタイプでは7_NamePlate_XG.png、7_Difficulty_XG.pngが読み込まれます。");
		sw.WriteLine("NamePlateType={0}", (int)eNamePlate);
		sw.WriteLine();
		sw.WriteLine("; 動くドラムセット(0:ON, 1:OFF, 2:NONE)");
		sw.WriteLine("DrumSetMoves={0}", (int)eドラムセットを動かす);
		sw.WriteLine();
		sw.WriteLine("; BPMバーの表示(0:表示する, 1:左のみ表示, 2:動くバーを表示しない, 3:表示しない)");
		sw.WriteLine("BPMBar={0}", (int)eBPMbar); ;
		sw.WriteLine();
		sw.WriteLine("; LivePointの表示(0:OFF, 1:ON)");
		sw.WriteLine("LivePoint={0}", bLivePoint ? 1 : 0);
		sw.WriteLine();
		sw.WriteLine("; スピーカーの表示(0:OFF, 1:ON)");
		sw.WriteLine("Speaker={0}", bSpeaker ? 1 : 0);
		sw.WriteLine();
		#endregion
		sw.WriteLine("; シャッターINSIDE(0～100)");
		sw.WriteLine("DrumsShutterIn={0}", (int)nShutterInSide.Drums);
		sw.WriteLine("GuitarShutterIn={0}", (int)nShutterInSide.Guitar);
		sw.WriteLine("BassShutterIn={0}", (int)nShutterInSide.Bass);
		sw.WriteLine();
		sw.WriteLine("; シャッターOUTSIDE(0～100)");
		sw.WriteLine("DrumsShutterOut={0}", (int)nShutterOutSide.Drums);
		sw.WriteLine("GuitarShutterOut={0}", (int)nShutterOutSide.Guitar);
		sw.WriteLine("BassShutterOut={0}", (int)nShutterOutSide.Bass);
		sw.WriteLine();
		sw.WriteLine( "; ボーナス演出の表示(0:表示しない, 1:表示する)");
		sw.WriteLine("DrumsStageEffect={0}", DisplayBonusEffects ? 1 : 0);
		sw.WriteLine();
		sw.WriteLine("; ドラムレーンタイプ(0:A, 1:B, 2:C 3:D )");
		sw.WriteLine("DrumsLaneType={0}", (int)eLaneType.Drums);
		sw.WriteLine();
		sw.WriteLine("; CLASSIC譜面判別");
		sw.WriteLine("CLASSIC={0}", bClassicScoreDisplay ? 1 : 0);
		sw.WriteLine();
		sw.WriteLine("; スキルモード(0:旧仕様, 1:XG仕様)");
		sw.WriteLine("SkillMode={0}", (int)nSkillMode);
		sw.WriteLine();
		sw.WriteLine("; スキルモードの自動切換え(0:OFF, 1:ON)");
		sw.WriteLine("SwitchSkillMode={0}", bSkillModeを自動切換えする ? 1 : 0);
		sw.WriteLine();
		sw.WriteLine("; ドラム アタックエフェクトタイプ");
		sw.WriteLine("; 0:ALL 粉と爆発エフェクトを表示します。");
		sw.WriteLine("; 1:ChipOFF チップのエフェクトを消します。");
		sw.WriteLine("; 2:EffectOnly 粉を消します。");
		sw.WriteLine("; 3:ALLOFF すべて消します。");
		sw.WriteLine("DrumsAttackEffect={0}", (int)eAttackEffect.Drums);
		sw.WriteLine();
		sw.WriteLine("; ギター / ベース アタックエフェクトタイプ (0:OFF, 1:ON)");
		sw.WriteLine("GuitarAttackEffect={0}", (int)eAttackEffect.Guitar);
		sw.WriteLine("BassAttackEffect={0}", (int)eAttackEffect.Bass);
		sw.WriteLine();
		sw.WriteLine("; レーン表示");
		sw.WriteLine("; 0:ALL ON レーン背景、小節線を表示します。");
		sw.WriteLine("; 1:LANE FF レーン背景を消します。");
		sw.WriteLine("; 2:LINE OFF 小節線を消します。");
		sw.WriteLine("; 3:ALL OFF すべて消します。");
		sw.WriteLine("DrumsLaneDisp={0}", (int)nLaneDisp.Drums);
		sw.WriteLine("GuitarLaneDisp={0}", (int)nLaneDisp.Guitar);
		sw.WriteLine("BassLaneDisp={0}", (int)nLaneDisp.Bass);
		sw.WriteLine();
		sw.WriteLine("; Display Judgement");
		sw.WriteLine("DrumsDisplayJudge={0}", bDisplayJudge.Drums ? 1 : 0);
		sw.WriteLine("GuitarDisplayJudge={0}", bDisplayJudge.Guitar ? 1 : 0);
		sw.WriteLine("BassDisplayJudge={0}", bDisplayJudge.Bass ? 1 : 0);
		sw.WriteLine();
		sw.WriteLine("; 判定ライン表示");
		sw.WriteLine("DrumsJudgeLineDisp={0}", bJudgeLineDisp.Drums ? 1 : 0);
		sw.WriteLine("GuitarJudgeLineDisp={0}", bJudgeLineDisp.Guitar ? 1 : 0);
		sw.WriteLine("BassJudgeLineDisp={0}", bJudgeLineDisp.Bass ? 1 : 0);
		sw.WriteLine();
		sw.WriteLine("; レーンフラッシュ表示");
		sw.WriteLine("DrumsLaneFlush={0}", bLaneFlush.Drums ? 1 : 0);
		sw.WriteLine("GuitarLaneFlush={0}", bLaneFlush.Guitar ? 1 : 0);
		sw.WriteLine("BassLaneFlush={0}", bLaneFlush.Bass ? 1 : 0);
		sw.WriteLine();
		sw.WriteLine("; ペダル部分のラグ時間調整");
		sw.WriteLine("; 入力が遅い場合、マイナス方向に調節してください。");
		sw.WriteLine("PedalLagTime={0}", nPedalLagTime );
		sw.WriteLine();
		#endregion

		//sw.WriteLine( ";-------------------" );
		#endregion
		#region[ 画像周り ]
		sw.WriteLine( ";判定画像のアニメーション方式" );
		sw.WriteLine( ";(0:旧DTXMania方式 1:コマ方式 2:擬似XG方式)");
		sw.WriteLine( "JudgeAnimeType={0}", nJudgeAnimeType );
		sw.WriteLine();
		sw.WriteLine( ";判定画像のコマ数" );
		sw.WriteLine( "JudgeFrames={0}", nJudgeFrames );
		sw.WriteLine();
		sw.WriteLine( ";判定画像の1コマのフレーム数" );
		sw.WriteLine( "JudgeInterval={0}", nJudgeInterval );
		sw.WriteLine();
		sw.WriteLine( ";判定画像の1コマの幅" );
		sw.WriteLine( "JudgeWidgh={0}", nJudgeWidgh );
		sw.WriteLine();
		sw.WriteLine( ";判定画像の1コマの高さ" );
		sw.WriteLine( "JudgeHeight={0}", nJudgeHeight );
		sw.WriteLine();
		sw.WriteLine( ";アタックエフェクトのコマ数" );
		sw.WriteLine( "ExplosionFrames={0}", (int)nExplosionFrames );
		sw.WriteLine();
		sw.WriteLine( ";アタックエフェクトの1コマのフレーム数" );
		sw.WriteLine( "ExplosionInterval={0}", (int)nExplosionInterval );
		sw.WriteLine();
		sw.WriteLine( ";アタックエフェクトの1コマの幅" );
		sw.WriteLine( "ExplosionWidgh={0}", nExplosionWidgh );
		sw.WriteLine();
		sw.WriteLine( ";アタックエフェクトの1コマの高さ" );
		sw.WriteLine( "ExplosionHeight={0}", nExplosionHeight );
		sw.WriteLine();
		sw.WriteLine( "ワイリングエフェクトのコマ数;" );
		sw.WriteLine( "WailingFireFrames={0}", (int)nWailingFireFrames );
		sw.WriteLine();
		sw.WriteLine( ";ワイリングエフェクトの1コマのフレーム数" );
		sw.WriteLine( "WailingFireInterval={0}", (int)nWailingFireInterval );
		sw.WriteLine();
		sw.WriteLine( ";ワイリングエフェクトの1コマの幅" );
		sw.WriteLine( "WailingFireWidgh={0}", nWailingFireWidgh );
		sw.WriteLine();
		sw.WriteLine( ";ワイリングエフェクトの1コマの高さ" );
		sw.WriteLine( "WailingFireHeight={0}", nWailingFireHeight );
		sw.WriteLine();
		sw.WriteLine( ";ワイリングエフェクトのX座標" );
		sw.WriteLine( "WailingFirePosXGuitar={0}", nWailingFireX.Guitar );
		sw.WriteLine( "WailingFirePosXBass={0}", nWailingFireX.Bass );
		sw.WriteLine();
		sw.WriteLine( ";ワイリングエフェクトのY座標(Guitar、Bass共通)" );
		sw.WriteLine( "WailingFirePosY={0}", nWailingFireY );
		sw.WriteLine();
		sw.WriteLine(";-------------------");
		#endregion
		#region [ AutoPlay ]
		sw.WriteLine( "[AutoPlay]" );
		sw.WriteLine();
		sw.WriteLine( "; 自動演奏(0:OFF, 1:ON)" );
		sw.WriteLine();
		sw.WriteLine( "; Drums" );
		sw.WriteLine("LC={0}", bAutoPlay.LC ? 1 : 0);
		sw.WriteLine("HH={0}", bAutoPlay.HH ? 1 : 0);
		sw.WriteLine("SD={0}", bAutoPlay.SD ? 1 : 0);
		sw.WriteLine("BD={0}", bAutoPlay.BD ? 1 : 0);
		sw.WriteLine("HT={0}", bAutoPlay.HT ? 1 : 0);
		sw.WriteLine("LT={0}", bAutoPlay.LT ? 1 : 0);
		sw.WriteLine("FT={0}", bAutoPlay.FT ? 1 : 0);
		sw.WriteLine("CY={0}", bAutoPlay.CY ? 1 : 0);
		sw.WriteLine("RD={0}", bAutoPlay.RD ? 1 : 0);
		sw.WriteLine("LP={0}", bAutoPlay.LP ? 1 : 0);
		sw.WriteLine("LBD={0}", bAutoPlay.LBD ? 1 : 0);
		sw.WriteLine();
		sw.WriteLine( "; Guitar" );
		//sw.WriteLine( "Guitar={0}", this.bAutoPlay.Guitar ? 1 : 0 );
		sw.WriteLine( "GuitarR={0}", bAutoPlay.GtR ? 1 : 0 );
		sw.WriteLine( "GuitarG={0}", bAutoPlay.GtG ? 1 : 0 );
		sw.WriteLine( "GuitarB={0}", bAutoPlay.GtB ? 1 : 0 );
		sw.WriteLine( "GuitarY={0}", bAutoPlay.GtY ? 1 : 0 );
		sw.WriteLine( "GuitarP={0}", bAutoPlay.GtP ? 1 : 0 );
		sw.WriteLine( "GuitarPick={0}", bAutoPlay.GtPick ? 1 : 0 );
		sw.WriteLine( "GuitarWailing={0}", bAutoPlay.GtW ? 1 : 0 );
		sw.WriteLine();
		sw.WriteLine( "; Bass" );
		// sw.WriteLine( "Bass={0}", this.bAutoPlay.Bass ? 1 : 0 );
		sw.WriteLine( "BassR={0}", bAutoPlay.BsR ? 1 : 0 );
		sw.WriteLine( "BassG={0}", bAutoPlay.BsG ? 1 : 0 );
		sw.WriteLine( "BassB={0}", bAutoPlay.BsB ? 1 : 0 );
		sw.WriteLine( "BassY={0}", bAutoPlay.BsY ? 1 : 0);
		sw.WriteLine( "BassP={0}", bAutoPlay.BsP ? 1 : 0);
		sw.WriteLine( "BassPick={0}", bAutoPlay.BsPick ? 1 : 0 );
		sw.WriteLine( "BassWailing={0}", bAutoPlay.BsW ? 1 : 0 );
		sw.WriteLine();
		sw.WriteLine( ";-------------------" );
		#endregion
		#region [ HitRange ]
		sw.WriteLine(@"[HitRange]");
		sw.WriteLine();
		sw.WriteLine(@"; Perfect～Poor とみなされる範囲[ms]");
		sw.WriteLine(@"; Hit ranges for each judgement type (in ± milliseconds)");
		sw.WriteLine();
		sw.WriteLine(@"; Drum chips, except pedals");
		tWriteHitRanges(stDrumHitRanges, @"Drum", sw);
		sw.WriteLine();
		sw.WriteLine(@"; Drum pedal chips");
		tWriteHitRanges(stDrumPedalHitRanges, @"DrumPedal", sw);
		sw.WriteLine();
		sw.WriteLine(@"; Guitar chips");
		tWriteHitRanges(stGuitarHitRanges, @"Guitar", sw);
		sw.WriteLine();
		sw.WriteLine(@"; Bass chips");
		tWriteHitRanges(stBassHitRanges, @"Bass", sw);
		sw.WriteLine();
		sw.WriteLine( ";-------------------" );
		#endregion

		#region [ Discord Rich Presence ]
		sw.WriteLine(@"[DiscordRichPresence]");
		sw.WriteLine();
		sw.WriteLine("; Enable Rich Presence integration (0:OFF, 1:ON)");
		sw.WriteLine($"Enable={(bDiscordRichPresenceEnabled ? 1 : 0)}");
		sw.WriteLine();
		sw.WriteLine("; Unique client identifier of the Discord Application to use");
		sw.WriteLine($"ApplicationID={strDiscordRichPresenceApplicationID}");
		sw.WriteLine();
		sw.WriteLine("; Unique identifier of the large image to display alongside presences");
		sw.WriteLine($"LargeImage={strDiscordRichPresenceLargeImageKey}");
		sw.WriteLine();
		sw.WriteLine("; Unique identifier of the small image to display alongside presences in drum mode");
		sw.WriteLine($"SmallImageDrums={strDiscordRichPresenceSmallImageKeyDrums}");
		sw.WriteLine();
		sw.WriteLine("; Unique identifier of the small image to display alongside presences in guitar mode");
		sw.WriteLine($"SmallImageGuitar={strDiscordRichPresenceSmallImageKeyGuitar}");
		sw.WriteLine();
		sw.WriteLine(@";-------------------");
		#endregion

		#region [ GUID ]
		sw.WriteLine( "[GUID]" );
		sw.WriteLine();
		foreach( KeyValuePair<int, string> pair in joystickDict )
		{
			sw.WriteLine( "JoystickID={0},{1}", pair.Key, pair.Value );
		}
		#endregion
		#region [ DrumsKeyAssign ]
		sw.WriteLine();
		sw.WriteLine( ";-------------------" );
		sw.WriteLine( "; キーアサイン" );
		sw.WriteLine( ";   項　目：Keyboard → 'K'＋'0'＋キーコード(10進数)" );
		sw.WriteLine( ";           Mouse    → 'N'＋'0'＋ボタン番号(0～7)" );
		sw.WriteLine( ";           MIDI In  → 'M'＋デバイス番号1桁(0～9,A～Z)＋ノート番号(10進数)" );
		sw.WriteLine( ";           Joystick → 'J'＋デバイス番号1桁(0～9,A～Z)＋ 0 ...... Ｘ減少(左)ボタン" );
		sw.WriteLine( ";                                                         1 ...... Ｘ増加(右)ボタン" );
		sw.WriteLine( ";                                                         2 ...... Ｙ減少(上)ボタン" );
		sw.WriteLine( ";                                                         3 ...... Ｙ増加(下)ボタン" );
		sw.WriteLine( ";                                                         4 ...... Ｚ減少(前)ボタン" );
		sw.WriteLine( ";                                                         5 ...... Ｚ増加(後)ボタン" );
		sw.WriteLine( ";                                                         6～133.. ボタン1～128" );
		sw.WriteLine( ";           これらの項目を 16 個まで指定可能(',' で区切って記述）。" );
		sw.WriteLine( ";" );
		sw.WriteLine( ";   表記例：HH=K044,M042,J16" );
		sw.WriteLine( ";           → HiHat を Keyboard の 44 ('Z'), MidiIn#0 の 42, JoyPad#1 の 6(ボタン1) に割当て" );
		sw.WriteLine( ";" );
		sw.WriteLine( ";   ※Joystick のデバイス番号とデバイスとの関係は [GUID] セクションに記してあるものが有効。" );
		sw.WriteLine( ";" );
		sw.WriteLine();
		sw.WriteLine( "[DrumsKeyAssign]" );
		sw.WriteLine();
		sw.Write( "HH=" );
		tWriteKey( sw, KeyAssign.Drums.HH );
		sw.WriteLine();
		sw.Write( "SD=" );
		tWriteKey( sw, KeyAssign.Drums.SD );
		sw.WriteLine();
		sw.Write( "BD=" );
		tWriteKey( sw, KeyAssign.Drums.BD );
		sw.WriteLine();
		sw.Write( "HT=" );
		tWriteKey( sw, KeyAssign.Drums.HT );
		sw.WriteLine();
		sw.Write( "LT=" );
		tWriteKey( sw, KeyAssign.Drums.LT );
		sw.WriteLine();
		sw.Write( "FT=" );
		tWriteKey( sw, KeyAssign.Drums.FT );
		sw.WriteLine();
		sw.Write( "CY=" );
		tWriteKey( sw, KeyAssign.Drums.CY );
		sw.WriteLine();
		sw.Write( "HO=" );
		tWriteKey( sw, KeyAssign.Drums.HHO );
		sw.WriteLine();
		sw.Write( "RD=" );
		tWriteKey( sw, KeyAssign.Drums.RD );
		sw.WriteLine();
		sw.Write( "LC=" );
		tWriteKey( sw, KeyAssign.Drums.LC );
		sw.WriteLine();
		sw.Write( "LP=" );										// #27029 2012.1.4 from
		tWriteKey( sw, KeyAssign.Drums.LP );	//
		sw.WriteLine();											//
		sw.Write( "LBD=" );
		tWriteKey( sw, KeyAssign.Drums.LBD );
		sw.WriteLine();
		sw.WriteLine();
		#endregion
		#region [ GuitarKeyAssign ]
		sw.WriteLine( "[GuitarKeyAssign]" );
		sw.WriteLine();
		sw.Write( "R=" );
		tWriteKey( sw, KeyAssign.Guitar.R );
		sw.WriteLine();
		sw.Write( "G=" );
		tWriteKey( sw, KeyAssign.Guitar.G );
		sw.WriteLine();
		sw.Write( "B=" );
		tWriteKey( sw, KeyAssign.Guitar.B );
		sw.WriteLine();
		sw.Write( "Y=" );
		tWriteKey( sw, KeyAssign.Guitar.Y );
		sw.WriteLine();
		sw.Write( "P=" );
		tWriteKey( sw, KeyAssign.Guitar.P );
		sw.WriteLine();
		sw.Write( "Pick=" );
		tWriteKey( sw, KeyAssign.Guitar.Pick );
		sw.WriteLine();
		sw.Write( "Wail=" );
		tWriteKey( sw, KeyAssign.Guitar.Wail );
		sw.WriteLine();
		sw.Write( "Decide=" );
		tWriteKey( sw, KeyAssign.Guitar.Decide );
		sw.WriteLine();
		sw.Write("Cancel=");
		tWriteKey(sw, KeyAssign.Guitar.Cancel);
		sw.WriteLine();
		sw.WriteLine();
		#endregion
		#region [ BassKeyAssign ]
		sw.WriteLine( "[BassKeyAssign]" );
		sw.WriteLine();
		sw.Write( "R=" );
		tWriteKey( sw, KeyAssign.Bass.R );
		sw.WriteLine();
		sw.Write( "G=" );
		tWriteKey( sw, KeyAssign.Bass.G );
		sw.WriteLine();
		sw.Write( "B=" );
		tWriteKey( sw, KeyAssign.Bass.B );
		sw.WriteLine();
		sw.Write( "Y=" );
		tWriteKey( sw, KeyAssign.Bass.Y );
		sw.WriteLine();
		sw.Write( "P=" );
		tWriteKey( sw, KeyAssign.Bass.P );
		sw.WriteLine();
		sw.Write( "Pick=" );
		tWriteKey( sw, KeyAssign.Bass.Pick );
		sw.WriteLine();
		sw.Write( "Wail=" );
		tWriteKey( sw, KeyAssign.Bass.Wail );
		sw.WriteLine();
		sw.Write( "Decide=" );
		tWriteKey( sw, KeyAssign.Bass.Decide );
		sw.WriteLine();
		sw.Write("Cancel=");
		tWriteKey(sw, KeyAssign.Bass.Cancel);
		sw.WriteLine();
		sw.WriteLine();
		#endregion
		#region [ SystemkeyAssign ]
		sw.WriteLine( "[SystemKeyAssign]" );
		sw.WriteLine();
		sw.Write( "Capture=" );
		tWriteKey( sw, KeyAssign.System.Capture );
		sw.WriteLine();
		sw.Write("Search=");
		tWriteKey(sw, KeyAssign.System.Search);
		sw.WriteLine();
		sw.Write( "Help=" );
		tWriteKey( sw, KeyAssign.Guitar.Help );
		sw.WriteLine();
		sw.Write( "Pause=" );
		tWriteKey( sw, KeyAssign.Bass.Help );
		sw.WriteLine();
		sw.Write("LoopCreate=");
		tWriteKey(sw, KeyAssign.System.LoopCreate);
		sw.WriteLine();
		sw.Write("LoopDelete=");
		tWriteKey(sw, KeyAssign.System.LoopDelete);
		sw.WriteLine();
		sw.Write("SkipForward=");
		tWriteKey(sw, KeyAssign.System.SkipForward);
		sw.WriteLine();
		sw.Write("SkipBackward=");
		tWriteKey(sw, KeyAssign.System.SkipBackward);
		sw.WriteLine();
		sw.Write("IncreasePlaySpeed=");
		tWriteKey(sw, KeyAssign.System.IncreasePlaySpeed);
		sw.WriteLine();
		sw.Write("DecreasePlaySpeed=");
		tWriteKey(sw, KeyAssign.System.DecreasePlaySpeed);
		sw.WriteLine();
		sw.Write("Restart=");
		tWriteKey(sw, KeyAssign.System.Restart);
		sw.WriteLine();
		sw.WriteLine();
		#endregion
			
		sw.Close();
	}

	/// <summary>
	/// Write the given <see cref="STHitRanges"/> as INI fields to the given <see cref="StreamWriter"/>.
	/// </summary>
	/// <param name="stHitRanges">The <see cref="STHitRanges"/> to write.</param>
	/// <param name="strName">The unique identifier of <paramref name="stHitRanges"/>.</param>
	/// <param name="writer">The <see cref="StreamWriter"/> to write to.</param>
	private void tWriteHitRanges(STHitRanges stHitRanges, string strName, StreamWriter writer)
	{
		writer.WriteLine($@"{strName}Perfect={stHitRanges.nPerfectSizeMs}");
		writer.WriteLine($@"{strName}Great={stHitRanges.nGreatSizeMs}");
		writer.WriteLine($@"{strName}Good={stHitRanges.nGoodSizeMs}");
		writer.WriteLine($@"{strName}Poor={stHitRanges.nPoorSizeMs}");
	}

	public void tReadFromFile( string iniファイル名 )
	{
		ConfigIniファイル名 = iniファイル名;
		bConfigIniExists = File.Exists( ConfigIniファイル名 );
		if( bConfigIniExists )
		{
			string str;
			StreamReader reader = new StreamReader( ConfigIniファイル名, Encoding.GetEncoding( "Shift_JIS" ) );
			str = reader.ReadToEnd();
			tReadFromString( str );
		}
	}

	private void tReadFromString( string strAllSettings )	// 2011.4.13 yyagi; refactored to make initial KeyConfig easier.
	{
		ESectionType unknown = ESectionType.Unknown;
		string[] delimiter = { "\n" };
		string[] strSingleLine = strAllSettings.Split( delimiter, StringSplitOptions.RemoveEmptyEntries );
		foreach ( string s in strSingleLine )
		{
			string str = s.Replace( '\t', ' ' ).TrimStart( new char[] { '\t', ' ' } );
			if ( ( str.Length != 0 ) && ( str[ 0 ] != ';' ) )
			{
				try
				{
					string str3;
					string str4;
					if ( str[ 0 ] == '[' )
					{
						#region [ セクションの変更 ]
						//-----------------------------
						StringBuilder builder = new StringBuilder( 32 );
						int num = 1;
						while ( ( num < str.Length ) && ( str[ num ] != ']' ) )
						{
							builder.Append( str[ num++ ] );
						}
						string str2 = builder.ToString();
						if ( str2.Equals( "System" ) )
						{
							unknown = ESectionType.System;
						}
						else if ( str2.Equals( "Log" ) )
						{
							unknown = ESectionType.Log;
						}
						else if ( str2.Equals( "PlayOption" ) )
						{
							unknown = ESectionType.PlayOption;
						}
						else if ( str2.Equals( "AutoPlay" ) )
						{
							unknown = ESectionType.AutoPlay;
						}
						else if ( str2.Equals( "HitRange" ) )
						{
							unknown = ESectionType.HitRange;
						}
						else if (str2.Equals(@"DiscordRichPresence"))
						{
							unknown = ESectionType.DiscordRichPresence;
						}
						else if ( str2.Equals( "GUID" ) )
						{
							unknown = ESectionType.GUID;
						}
						else if ( str2.Equals( "DrumsKeyAssign" ) )
						{
							unknown = ESectionType.DrumsKeyAssign;
						}
						else if ( str2.Equals( "GuitarKeyAssign" ) )
						{
							unknown = ESectionType.GuitarKeyAssign;
						}
						else if ( str2.Equals( "BassKeyAssign" ) )
						{
							unknown = ESectionType.BassKeyAssign;
						}
						else if ( str2.Equals( "SystemKeyAssign" ) )
						{
							unknown = ESectionType.SystemKeyAssign;
						}
						else
						{
							unknown = ESectionType.Unknown;
						}
						//-----------------------------
						#endregion
					}
					else
					{
						string[] strArray = str.Split( new char[] { '=' } );
						if( strArray.Length == 2 )
						{
							str3 = strArray[ 0 ].Trim();
							str4 = strArray[ 1 ].Trim();
							switch( unknown )
							{
								#region [ [System] ]
								//-----------------------------
								case ESectionType.System:
								{
#if false		// #23625 2011.1.11 Config.iniからダメージ/回復値の定数変更を行う場合はここを有効にする 087リリースに合わせ機能無効化
										//----------------------------------------
												if (str3.Equals("GaugeFactorD"))
												{
													int p = 0;
													string[] splittedFactor = str4.Split(',');
													foreach (string s in splittedFactor) {
														this.fGaugeFactor[p++, 0] = Convert.ToSingle(s);
													}
												} else
												if (str3.Equals("GaugeFactorG"))
												{
													int p = 0;
													string[] splittedFactor = str4.Split(',');
													foreach (string s in splittedFactor)
													{
														this.fGaugeFactor[p++, 1] = Convert.ToSingle(s);
													}
												}
												else
												if (str3.Equals("DamageFactor"))
												{
													int p = 0;
													string[] splittedFactor = str4.Split(',');
													foreach (string s in splittedFactor)
													{
														this.fDamageLevelFactor[p++] = Convert.ToSingle(s);
													}
												}
												else
										//----------------------------------------
#endif
									if( str3.Equals( "Version" ) )
									{
										strDTXManiaのバージョン = str4;
									}
									else if( str3.Equals( "DTXPath" ) )
									{
										strSongDataSearchPath = str4;
									}
									else if ( str3.Equals( "SkinPath" ) )
									{
										string absSkinPath = str4;
										if ( !Path.IsPathRooted( str4 ) )
										{
											absSkinPath = Path.Combine( CDTXMania.executableDirectory, "System" );
											absSkinPath = Path.Combine( absSkinPath, str4 );
											Uri u = new Uri( absSkinPath );
											absSkinPath = u.AbsolutePath.ToString();	// str4内に相対パスがある場合に備える
											absSkinPath = System.Web.HttpUtility.UrlDecode( absSkinPath );						// デコードする
											absSkinPath = absSkinPath.Replace( '/', Path.DirectorySeparatorChar );	// 区切り文字が\ではなく/なので置換する
										}
										if ( absSkinPath[ absSkinPath.Length - 1 ] != Path.DirectorySeparatorChar )	// フォルダ名末尾に\を必ずつけて、CSkin側と表記を統一する
										{
											absSkinPath += Path.DirectorySeparatorChar;
										}
										strSystemSkinSubfolderFullName = absSkinPath;
									}
									else if( str3.Equals( "CardNameDrums" ) )
									{
										strCardName[0] = str4;
									}
									else if( str3.Equals( "CardNameGuitar" ) )
									{
										strCardName[1] = str4;
									}
									else if( str3.Equals( "CardNameBass" ) )
									{
										strCardName[2] = str4;
									}
									else if( str3.Equals( "GroupNameDrums" ) )
									{
										strGroupName[0] = str4;
									}
									else if( str3.Equals( "GroupNameGuitar" ) )
									{
										strGroupName[1] = str4;
									}
									else if( str3.Equals( "GroupNameBass" ) )
									{
										strGroupName[2] = str4;
									}
									else if( str3.Equals( "NameColorDrums" ) )
									{
										nNameColor[ 0 ] = CConversion.nGetNumberIfInRange(str4, 0, 19, 0);
									}
									else if( str3.Equals( "NameColorGuitar" ) )
									{
										nNameColor[ 1 ] = CConversion.nGetNumberIfInRange(str4, 0, 19, 0);
									}
									else if( str3.Equals( "NameColorBass" ) )
									{
										nNameColor[ 2 ] = CConversion.nGetNumberIfInRange(str4, 0, 19, 0);
									}
									else if (str3.Equals("SkinChangeByBoxDef"))
									{
										bUseBoxDefSkin = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("FullScreen"))
									{
										bFullScreenMode = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("FullScreenExclusive"))
									{
										bFullScreenExclusive = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("WindowWidth"))		// #23510 2010.10.31 yyagi add
									{
										nWindowWidth = CConversion.nGetNumberIfInRange(str4, 1, 65535, nWindowWidth);
										if (nWindowWidth <= 0)
										{
											nWindowWidth = GameFramebufferSize.Width;
										}
									}
									else if (str3.Equals("WindowHeight"))		// #23510 2010.10.31 yyagi add
									{
										nWindowHeight = CConversion.nGetNumberIfInRange(str4, 1, 65535, nWindowHeight);
										if (nWindowHeight <= 0)
										{
											nWindowHeight = GameFramebufferSize.Height;
										}
									}
									else if (str3.Equals("WindowX"))		// #30675 2013.02.04 ikanick add
									{
										nInitialWindowXPosition = CConversion.nGetNumberIfInRange(
											str4, 0, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width - 1, nInitialWindowXPosition);
									}
									else if (str3.Equals("WindowY"))		// #30675 2013.02.04 ikanick add
									{
										nInitialWindowYPosition = CConversion.nGetNumberIfInRange(
											str4, 0, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height - 1, nInitialWindowYPosition);
									}
									else if (str3.Equals("MovieMode"))
									{
										nMovieMode = CConversion.nGetNumberIfInRange(str4, 0, 0xffff, nMovieMode);
										if (nMovieMode > 3)
										{
											nMovieMode = 0;
										}
									}
									else if (str3.Equals("MovieAlpha"))
									{
										nMovieAlpha = CConversion.nGetNumberIfInRange(str4, 0, 10, nMovieAlpha);
										if (nMovieAlpha > 10)
										{
											nMovieAlpha = 10;
										}
									}
									else if (str3.Equals("InfoType"))
									{
										nInfoType = CConversion.nGetNumberIfInRange(str4, 0, 1, (int)nInfoType);
									}
									else if (str3.Equals("DoubleClickFullScreen"))	// #26752 2011.11.27 yyagi
									{
										bIsAllowedDoubleClickFullscreen = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("EnableSystemMenu"))		// #28200 2012.5.1 yyagi
									{
										bIsEnabledSystemMenu = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("SoundDeviceType"))
									{
										nSoundDeviceType = CConversion.nGetNumberIfInRange(str4, 0, 3, nSoundDeviceType);
									}
									else if (str3.Equals("WASAPIBufferSizeMs"))
									{
										nWASAPIBufferSizeMs = CConversion.nGetNumberIfInRange(str4, 0, 9999, nWASAPIBufferSizeMs);
									}
									else if (str3.Equals("ASIODevice"))
									{
										string[] asiodev = CEnumerateAllAsioDevices.GetAllASIODevices();
										nASIODevice = CConversion.nGetNumberIfInRange(str4, 0, asiodev.Length - 1, nASIODevice);
									}
									//else if (str3.Equals("ASIOBufferSizeMs"))
									//{
									//    this.nASIOBufferSizeMs = CConversion.nGetNumberIfInRange(str4, 0, 9999, this.nASIOBufferSizeMs);
									//}
									//else if (str3.Equals("DynamicBassMixerManagement"))
									//{
									//    this.bDynamicBassMixerManagement = CConversion.bONorOFF(str4[0]);
									//}
									else if (str3.Equals("SoundTimerType"))         // #33689 2014.6.6 yyagi
									{
										bUseOSTimer = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("EventDrivenWASAPI"))
									{
										bEventDrivenWASAPI = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("Metronome"))
									{
										bMetronome = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("ChipPlayTimeComputeMode"))
									{
										nChipPlayTimeComputeMode = CConversion.nGetNumberIfInRange(str4, 0, 1, nChipPlayTimeComputeMode);
									}
									else if (str3.Equals("MasterVolume"))
									{
										nMasterVolume = CConversion.nGetNumberIfInRange(str4, 0, 100, nMasterVolume);
									}
									else if (str3.Equals("VSyncWait"))
									{
										bVerticalSyncWait = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("BackSleep"))				// #23568 2010.11.04 ikanick add
									{
										n非フォーカス時スリープms = CConversion.nRoundToRange(str4, 0, 50, n非フォーカス時スリープms);
									}
									else if (str3.Equals("SleepTimePerFrame"))		// #23568 2011.11.27 yyagi
									{
										nフレーム毎スリープms = CConversion.nRoundToRange(str4, -1, 50, nフレーム毎スリープms);
									}
									else if (str3.Equals("Guitar"))
									{
										bGuitarEnabled = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("Drums"))
									{
										bDrumsEnabled = CConversion.bONorOFF(str4[0]);
									}                                            
									else if (str3.Equals("BGAlpha"))
									{
										nBackgroundTransparency = CConversion.nGetNumberIfInRange(str4, 0, 0xff, nBackgroundTransparency);
									}
									else if (str3.Equals("DamageLevel"))
									{
										eDamageLevel = (EDamageLevel)CConversion.nGetNumberIfInRange(str4, 0, 2, (int)eDamageLevel);
									}
									else if (str3.Equals("HHGroup"))
									{
										eHHGroup = (EHHGroup)CConversion.nGetNumberIfInRange(str4, 0, 3, (int)eHHGroup);
									}
									else if (str3.Equals("FTGroup"))
									{
										eFTGroup = (EFTGroup)CConversion.nGetNumberIfInRange(str4, 0, 2, (int)eFTGroup);
									}
									else if (str3.Equals("CYGroup"))
									{
										eCYGroup = (ECYGroup)CConversion.nGetNumberIfInRange(str4, 0, 2, (int)eCYGroup);
									}
									else if (str3.Equals("BDGroup"))		// #27029 2012.1.4 from
									{
										eBDGroup = (EBDGroup)CConversion.nGetNumberIfInRange(str4, 0, 3, (int)eBDGroup);
									}
									else if (str3.Equals("HitSoundPriorityHH"))
									{
										eHitSoundPriorityHH = (EPlaybackPriority)CConversion.nGetNumberIfInRange(str4, 0, 1, (int)eHitSoundPriorityHH);
									}
									else if (str3.Equals("HitSoundPriorityFT"))
									{
										eHitSoundPriorityFT = (EPlaybackPriority)CConversion.nGetNumberIfInRange(str4, 0, 1, (int)eHitSoundPriorityFT);
									}
									else if (str3.Equals("HitSoundPriorityCY"))
									{
										eHitSoundPriorityCY = (EPlaybackPriority)CConversion.nGetNumberIfInRange(str4, 0, 1, (int)eHitSoundPriorityCY);
									}
									else if (str3.Equals("HitSoundPriorityLP"))
									{
										eHitSoundPriorityLP = (EPlaybackPriority)CConversion.nGetNumberIfInRange(str4, 0, 1, (int)eHitSoundPriorityLP);
									}
									else if (str3.Equals("StageFailed"))
									{
										bSTAGEFAILEDEnabled = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("AVI"))
									{
										bAVIEnabled = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("BGA"))
									{
										bBGAEnabled = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("FillInEffect"))
									{
										bFillInEnabled = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("PreviewSoundWait"))
									{
										nSongSelectSoundPreviewWaitTimeMs = CConversion.nGetNumberIfInRange(str4, 0, 0x5f5e0ff, nSongSelectSoundPreviewWaitTimeMs);
									}
									else if (str3.Equals("PreviewImageWait"))
									{
										nSongSelectImagePreviewWaitTimeMs = CConversion.nGetNumberIfInRange(str4, 0, 0x5f5e0ff, nSongSelectImagePreviewWaitTimeMs);
									}
									else if (str3.Equals("AdjustWaves"))
									{
										bWave再生位置自動調整機能有効 = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("BGMSound"))
									{
										bBGM音を発声する = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("HitSound"))
									{
										bドラム打音を発声する = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("AudienceSound"))
									{
										b歓声を発声する = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("SaveScoreIni"))
									{
										bScoreIniを出力する = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("RandomFromSubBox"))
									{
										bランダムセレクトで子BOXを検索対象とする = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("SoundMonitorDrums"))
									{
										b演奏音を強調する.Drums = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("SoundMonitorGuitar"))
									{
										b演奏音を強調する.Guitar = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("SoundMonitorBass"))
									{
										b演奏音を強調する.Bass = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("MinComboDrums"))
									{
										n表示可能な最小コンボ数.Drums = CConversion.nGetNumberIfInRange(str4, 1, 0x1869f, n表示可能な最小コンボ数.Drums);
									}
									else if (str3.Equals("MinComboGuitar"))
									{
										n表示可能な最小コンボ数.Guitar = CConversion.nGetNumberIfInRange(str4, 0, 0x1869f, n表示可能な最小コンボ数.Guitar);
									}
									else if (str3.Equals("MinComboBass"))
									{
										n表示可能な最小コンボ数.Bass = CConversion.nGetNumberIfInRange(str4, 0, 0x1869f, n表示可能な最小コンボ数.Bass);
									}
									else if( str3.Equals( "MusicNameDispDef" ) )
									{
										b曲名表示をdefのものにする = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("ShowDebugStatus"))
									{
										bShowPerformanceInformation = CConversion.bONorOFF(str4[0]);
									}
									#region [ GDオプション ]
									else if (str3.Equals("Difficulty"))
									{
										bDisplayDifficultyXGStyle = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("ShowScore"))
									{
										bShowScore = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("ShowMusicInfo"))
									{
										bShowMusicInfo = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("ShowPlaySpeed"))
									{
										nShowPlaySpeed = CConversion.nGetNumberIfInRange(str4, 0, 2, nShowPlaySpeed);
									}
									else if (str3.Equals("DisplayFontName"))
									{
										str曲名表示フォント = str4;
									}
									#endregion
									else if (str3.Equals("SelectListFontName"))
									{
										songListFont = str4;
									}
									else if (str3.Equals("SelectListFontSize"))
									{
										n選曲リストフォントのサイズdot = CConversion.nGetNumberIfInRange(str4, 1, 0x3e7, n選曲リストフォントのサイズdot);
									}
									else if (str3.Equals("SelectListFontItalic"))
									{
										b選曲リストフォントを斜体にする = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("SelectListFontBold"))
									{
										b選曲リストフォントを太字にする = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("ChipVolume"))
									{
										n手動再生音量 = CConversion.nGetNumberIfInRange(str4, 0, 100, n手動再生音量);
									}
									else if (str3.Equals("AutoChipVolume"))
									{
										n自動再生音量 = CConversion.nGetNumberIfInRange(str4, 0, 100, n自動再生音量);
									}
									else if (str3.Equals("StoicMode"))
									{
										bストイックモード = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("CymbalFree"))
									{
										bシンバルフリー = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("HHOGraphics"))
									{
										eHHOGraphics.Drums = (EType)CConversion.nGetNumberIfInRange(str4, 0, 2, (int)eHHOGraphics.Drums);
									}
									else if (str3.Equals("LBDGraphics"))
									{
										eLBDGraphics.Drums = (EType)CConversion.nGetNumberIfInRange(str4, 0, 1, (int)eLBDGraphics.Drums);
									}
									else if (str3.Equals("RDPosition"))
									{
										eRDPosition = (ERDPosition)CConversion.nGetNumberIfInRange(str4, 0, 1, (int)eRDPosition);
									}
									else if (str3.Equals("ShowLagTime"))				// #25370 2011.6.3 yyagi
									{
										nShowLagType = CConversion.nGetNumberIfInRange(str4, 0, 2, nShowLagType);
									}
									else if (str3.Equals("ShowLagTimeColor"))				// #25370 2011.6.3 yyagi
									{
										nShowLagTypeColor = CConversion.nGetNumberIfInRange( str4, 0, 1, nShowLagTypeColor );
									}
									else if (str3.Equals("ShowLagHitCount"))          //fisyher: New field
									{
										bShowLagHitCount = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("TimeStretch"))				// #23664 2013.2.24 yyagi
									{
										bTimeStretch = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("AutoResultCapture"))			// #25399 2011.6.9 yyagi
									{
										bIsAutoResultCapture = CConversion.bONorOFF(str4[0]);
									}
									#region [ AdjustTime ]
									else if ( str3.Equals( "InputAdjustTimeDrums" ) )		// #23580 2011.1.3 yyagi
									{
										nInputAdjustTimeMs.Drums = CConversion.nGetNumberIfInRange(str4, -99, 99, nInputAdjustTimeMs.Drums);
									}
									else if ( str3.Equals( "InputAdjustTimeGuitar" ) )	// #23580 2011.1.3 yyagi
									{
										nInputAdjustTimeMs.Guitar = CConversion.nGetNumberIfInRange(str4, -99, 99, nInputAdjustTimeMs.Guitar);
									}
									else if ( str3.Equals( "InputAdjustTimeBass" ) )		// #23580 2011.1.3 yyagi
									{
										nInputAdjustTimeMs.Bass = CConversion.nGetNumberIfInRange(str4, -99, 99, nInputAdjustTimeMs.Bass);
									}
									else if ( str3.Equals( "BGMAdjustTime" ) )              // #36372 2016.06.19 kairera0467
									{
										nCommonBGMAdjustMs = CConversion.nGetNumberIfInRange( str4, -99, 99, nCommonBGMAdjustMs );
									}
									else if ( str3.Equals( "JudgeLinePosOffsetDrums" ) ) // #31602 2013.6.23 yyagi
									{
										nJudgeLinePosOffset.Drums = CConversion.nGetNumberIfInRange( str4, -99, 99, nJudgeLinePosOffset.Drums );
									}
									else if ( str3.Equals( "JudgeLinePosOffsetGuitar" ) ) // #31602 2013.6.23 yyagi
									{
										nJudgeLinePosOffset.Guitar = CConversion.nGetNumberIfInRange( str4, -99, 99, nJudgeLinePosOffset.Guitar );
									}
									else if ( str3.Equals( "JudgeLinePosOffsetBass" ) ) // #31602 2013.6.23 yyagi
									{
										nJudgeLinePosOffset.Bass = CConversion.nGetNumberIfInRange( str4, -99, 99, nJudgeLinePosOffset.Bass );
									}
									#endregion
									else if (str3.Equals("BufferedInput"))
									{
										bBufferedInput = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("PolyphonicSounds"))		// #28228 2012.5.1 yyagi
									{
										nPoliphonicSounds = CConversion.nGetNumberIfInRange(str4, 1, 8, nPoliphonicSounds);
									}
									else if (str3.Equals("LCVelocityMin"))			// #23857 2010.12.12 yyagi
									{
										nVelocityMin.LC = CConversion.nGetNumberIfInRange(str4, 0, 127, nVelocityMin.LC);
									}
									else if (str3.Equals("HHVelocityMin"))
									{
										nVelocityMin.HH = CConversion.nGetNumberIfInRange(str4, 0, 127, nVelocityMin.HH);
									}
									else if (str3.Equals("SDVelocityMin"))			// #23857 2011.1.31 yyagi
									{
										nVelocityMin.SD = CConversion.nGetNumberIfInRange(str4, 0, 127, nVelocityMin.SD);
									}
									else if (str3.Equals("BDVelocityMin"))			// #23857 2011.1.31 yyagi
									{
										nVelocityMin.BD = CConversion.nGetNumberIfInRange(str4, 0, 127, nVelocityMin.BD);
									}
									else if (str3.Equals("HTVelocityMin"))			// #23857 2011.1.31 yyagi
									{
										nVelocityMin.HT = CConversion.nGetNumberIfInRange(str4, 0, 127, nVelocityMin.HT);
									}
									else if (str3.Equals("LTVelocityMin"))			// #23857 2011.1.31 yyagi
									{
										nVelocityMin.LT = CConversion.nGetNumberIfInRange(str4, 0, 127, nVelocityMin.LT);
									}
									else if (str3.Equals("FTVelocityMin"))			// #23857 2011.1.31 yyagi
									{
										nVelocityMin.FT = CConversion.nGetNumberIfInRange(str4, 0, 127, nVelocityMin.FT);
									}
									else if (str3.Equals("CYVelocityMin"))			// #23857 2011.1.31 yyagi
									{
										nVelocityMin.CY = CConversion.nGetNumberIfInRange(str4, 0, 127, nVelocityMin.CY);
									}
									else if (str3.Equals("RDVelocityMin"))			// #23857 2011.1.31 yyagi
									{
										nVelocityMin.RD = CConversion.nGetNumberIfInRange(str4, 0, 127, nVelocityMin.RD);
									}
									else if (str3.Equals("LPVelocityMin"))			// #23857 2011.1.31 yyagi
									{
										nVelocityMin.LP = CConversion.nGetNumberIfInRange(str4, 0, 127, nVelocityMin.LP);
									}
									else if (str3.Equals("LBDVelocityMin"))			// #23857 2011.1.31 yyagi
									{
										nVelocityMin.LBD = CConversion.nGetNumberIfInRange(str4, 0, 127, nVelocityMin.LBD);
									}
									else if (str3.Equals("AutoAddGage"))
									{
										bAutoAddGage = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("SkipTimeMs"))
									{
										nSkipTimeMs = CConversion.nGetNumberIfInRange(str4, 100, 20000, nSkipTimeMs);
									}
									continue;
								}
								//-----------------------------
								#endregion

								#region [ [Log] ]
								//-----------------------------
								case ESectionType.Log:
								{
									if( str3.Equals( "OutputLog" ) )
									{
										bOutputLogs = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if( str3.Equals( "TraceCreatedDisposed" ) )
									{
										bLog作成解放ログ出力 = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if( str3.Equals( "TraceDTXDetails" ) )
									{
										bLogDTX詳細ログ出力 = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if( str3.Equals( "TraceSongSearch" ) )
									{
										bLogSongSearch = CConversion.bONorOFF( str4[ 0 ] );
									}
									continue;
								}
								//-----------------------------
								#endregion

								#region [ [PlayOption] ]
								//-----------------------------
								case ESectionType.PlayOption:
								{
									if( str3.Equals( "DrumGraph" ) )  // #24074 2011.01.23 addikanick
									{
										bGraph有効.Drums = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if( str3.Equals( "GuitarGraph" ) )  // #24074 2011.01.23 addikanick
									{
										bGraph有効.Guitar = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if( str3.Equals( "BassGraph" ) )  // #24074 2011.01.23 addikanick
									{
										bGraph有効.Bass = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if (str3.Equals("SmallGraph"))
									{
										bSmallGraph = CConversion.bONorOFF(str4[0]);
									}
									else if ( str3.Equals( "DrumsReverse" ) )
									{
										bReverse.Drums = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if( str3.Equals( "GuitarReverse" ) )
									{
										bReverse.Guitar = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if( str3.Equals( "BassReverse" ) )
									{
										bReverse.Bass = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if( str3.Equals( "GuitarRandom" ) )
									{
										eRandom.Guitar = (ERandomMode) CConversion.nGetNumberIfInRange( str4, 0, 4, (int) eRandom.Guitar );
									}
									else if( str3.Equals( "BassRandom" ) )
									{
										eRandom.Bass = (ERandomMode) CConversion.nGetNumberIfInRange( str4, 0, 4, (int) eRandom.Bass );
									}
									else if( str3.Equals( "DrumsTight" ) )
									{
										bTight = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if( str3.Equals( "GuitarLight" ) )
									{
										bLight.Guitar = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if( str3.Equals( "BassLight" ) )
									{
										bLight.Bass = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if (str3.Equals("GuitarSpecialist"))
									{
										bSpecialist.Guitar = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("BassSpecialist"))
									{
										bSpecialist.Bass = CConversion.bONorOFF(str4[0]);
									}
									else if( str3.Equals( "GuitarLeft" ) )
									{
										bLeft.Guitar = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if( str3.Equals( "BassLeft" ) )
									{
										bLeft.Bass = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if (str3.Equals( "DrumsHiddenSudden") )
									{
										nHidSud.Drums = CConversion.nGetNumberIfInRange(str4, 0, 5, nHidSud.Drums);
									}
									else if (str3.Equals( "GuitarHiddenSudden") )
									{
										nHidSud.Guitar = CConversion.nGetNumberIfInRange(str4, 0, 5, nHidSud.Guitar);
									}
									else if (str3.Equals( "BassHiddenSudden") )
									{
										nHidSud.Bass = CConversion.nGetNumberIfInRange(str4, 0, 5, nHidSud.Bass);
									}
									else if( str3.Equals( "DrumsPosition" ) )
									{
										JudgementStringPosition.Drums = (EType)CConversion.nGetNumberIfInRange(str4, 0, 2, (int)JudgementStringPosition.Drums);
									}
									else if( str3.Equals( "GuitarPosition" ) )
									{
										JudgementStringPosition.Guitar = (EType)CConversion.nGetNumberIfInRange(str4, 0, 3, (int)JudgementStringPosition.Guitar);
									}
									else if( str3.Equals( "BassPosition" ) )
									{
										JudgementStringPosition.Bass = (EType)CConversion.nGetNumberIfInRange(str4, 0, 3, (int)JudgementStringPosition.Bass);
									}
									else if( str3.Equals( "DrumsScrollSpeed" ) )
									{
										nScrollSpeed.Drums = CConversion.nGetNumberIfInRange( str4, 0, 0x7cf, nScrollSpeed.Drums );
									}
									else if( str3.Equals( "GuitarScrollSpeed" ) )
									{
										nScrollSpeed.Guitar = CConversion.nGetNumberIfInRange( str4, 0, 0x7cf, nScrollSpeed.Guitar );
									}
									else if( str3.Equals( "BassScrollSpeed" ) )
									{
										nScrollSpeed.Bass = CConversion.nGetNumberIfInRange( str4, 0, 0x7cf, nScrollSpeed.Bass );
									}
									else if( str3.Equals( "PlaySpeed" ) )
									{
										nPlaySpeed = CConversion.nGetNumberIfInRange( str4, CConstants.PLAYSPEED_MIN, CConstants.PLAYSPEED_MAX, nPlaySpeed );
									}
									else if (str3.Equals("SaveScoreIfModifiedPlaySpeed"))
									{
										bSaveScoreIfModifiedPlaySpeed = CConversion.bONorOFF(str4[0]);
									}
									else if ( str3.Equals( "ComboPosition" ) )
									{
										ドラムコンボ文字の表示位置 = (EDrumComboTextDisplayPosition) CConversion.nGetNumberIfInRange( str4, 0, 3, (int) ドラムコンボ文字の表示位置 );
									}
									else if( str3.Equals( "Risky" ) )					// #2359 2011.6.23  yyagi
									{
										nRisky = CConversion.nGetNumberIfInRange( str4, 0, 10, nRisky );
									}
									else if( str3.Equals( "HAZARD" ) )				// #29500 2012.9.11 kairera0467
									{
										bHAZARD = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if( str3.Equals( "AssignToLBD" ) )
									{
										bAssignToLBD.Drums = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if (str3.Equals("DrumsJudgeLine"))
									{
										nJudgeLine.Drums = CConversion.nRoundToRange(str4, 0, 100, nJudgeLine.Drums);
									}
									else if ( str3.Equals( "DrumsShutterIn" ) )
									{
										nShutterInSide.Drums = CConversion.nGetNumberIfInRange( str4, 0, 100, nShutterInSide.Drums );
									}
									else if ( str3.Equals( "DrumsShutterOut" ) )
									{
										nShutterOutSide.Drums = CConversion.nGetNumberIfInRange( str4, -100, 100, nShutterOutSide.Drums );
									}
									else if ( str3.Equals( "GuitarJudgeLine" ) )
									{
										nJudgeLine.Guitar = CConversion.nRoundToRange(str4, 0, 100, nJudgeLine.Guitar);
									}
									else if ( str3.Equals( "GuitarShutterIn" ) )
									{
										nShutterInSide.Guitar = CConversion.nGetNumberIfInRange( str4, 0, 100, nShutterInSide.Guitar );
									}
									else if ( str3.Equals( "GuitarShutterOut" ) )
									{
										nShutterOutSide.Guitar = CConversion.nGetNumberIfInRange( str4, -100, 100, nShutterOutSide.Guitar );
									}
									else if ( str3.Equals( "BassJudgeLine" ) )
									{
										nJudgeLine.Bass = CConversion.nRoundToRange(str4, 0, 100, nJudgeLine.Bass);
									}
									else if ( str3.Equals( "BassShutterIn" ) )
									{
										nShutterInSide.Bass = CConversion.nGetNumberIfInRange( str4, 0, 100, nShutterInSide.Bass );
									}
									else if ( str3.Equals( "BassShutterOut" ) )
									{
										nShutterOutSide.Bass = CConversion.nGetNumberIfInRange( str4, -100, 100, nShutterOutSide.Guitar );
									}
									else if (str3.Equals("DrumsLaneType"))
									{
										eLaneType.Drums = (EType) CConversion.nGetNumberIfInRange(str4, 0, 3, (int) eLaneType.Drums);
									}
									else if (str3.Equals("RDPosition"))
									{
										eRDPosition = (ERDPosition)CConversion.nGetNumberIfInRange(str4, 0, 2, (int)eRDPosition);
									}
									else if (str3.Equals("DrumsTight"))				// #29500 2012.9.11 kairera0467
									{
										bTight = CConversion.bONorOFF(str4[0]);
									}
									#region [ XGオプション ]
									else if (str3.Equals("NamePlateType"))
									{
										eNamePlate = (EType)CConversion.nGetNumberIfInRange(str4, 0, 3, (int)eNamePlate);
									}
									else if (str3.Equals("DrumSetMoves"))
									{
										eドラムセットを動かす = (EType)CConversion.nGetNumberIfInRange(str4, 0, 2, (int)eドラムセットを動かす);
									}
									else if (str3.Equals("BPMBar"))
									{
										eBPMbar = ( EType )CConversion.nGetNumberIfInRange(str4, 0, 3, (int)eBPMbar);
									}
									else if (str3.Equals("LivePoint"))
									{
										bLivePoint = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("Speaker"))
									{
										bSpeaker = CConversion.bONorOFF(str4[0]);
									}
									#endregion
									else if (str3.Equals("DrumsStageEffect"))
									{
										DisplayBonusEffects = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("CLASSIC"))
									{
										bClassicScoreDisplay = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("MutingLP"))
									{
										bMutingLP = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("SkillMode"))
									{
										nSkillMode = CConversion.nGetNumberIfInRange(str4, 0, 1, (int)nSkillMode);
									}
									else if (str3.Equals("SwitchSkillMode"))
									{
										bSkillModeを自動切換えする = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("NumOfLanes"))
									{
										eNumOfLanes.Drums = (EType)CConversion.nGetNumberIfInRange(str4, 0, 2, (int)eNumOfLanes.Drums);
									}
									else if (str3.Equals("DkdkType"))
									{
										eDkdkType.Drums = (EType)CConversion.nGetNumberIfInRange(str4, 0, 2, (int)eDkdkType.Drums);
									}
									else if (str3.Equals("DrumsRandomPad"))
									{
										eRandom.Drums = (ERandomMode)CConversion.nGetNumberIfInRange(str4, 0, 6, (int)eRandom.Drums);
									}
									else if (str3.Equals("DrumsRandomPedal"))
									{
										eRandomPedal.Drums = (ERandomMode)CConversion.nGetNumberIfInRange(str4, 0, 6, (int)eRandomPedal.Drums);
									}
									else if (str3.Equals("DrumsAttackEffect"))
									{
										eAttackEffect.Drums = (EType)CConversion.nGetNumberIfInRange(str4, 0, 3, (int)eAttackEffect.Drums);
									}
									else if (str3.Equals("GuitarAttackEffect"))
									{
										eAttackEffect.Guitar = (EType)CConversion.nGetNumberIfInRange(str4, 0, 1, (int)eAttackEffect.Guitar);
									}
									else if (str3.Equals("BassAttackEffect"))
									{
										eAttackEffect.Bass = (EType)CConversion.nGetNumberIfInRange(str4, 0, 1, (int)eAttackEffect.Bass);
									}
									else if (str3.Equals("DrumsLaneDisp"))
									{
										nLaneDisp.Drums = CConversion.nGetNumberIfInRange(str4, 0, 4, (int)nLaneDisp.Drums);
									}
									else if (str3.Equals("GuitarLaneDisp"))
									{
										nLaneDisp.Guitar = CConversion.nGetNumberIfInRange(str4, 0, 4, (int)nLaneDisp.Guitar);
									}
									else if (str3.Equals("BassLaneDisp"))
									{
										nLaneDisp.Bass = CConversion.nGetNumberIfInRange(str4, 0, 4, (int)nLaneDisp.Bass);
									}
									else if (str3.Equals("DrumsDisplayJudge"))
									{
										bDisplayJudge.Drums = CConversion.bONorOFF(str4[0]);
									}
									else if( str3.Equals("GuitarDisplayJudge") )
									{
										bDisplayJudge.Guitar = CConversion.bONorOFF(str4[0]);
									}
									else if( str3.Equals("BassDisplayJudge") )
									{
										bDisplayJudge.Bass = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("DrumsJudgeLineDisp"))
									{
										bJudgeLineDisp.Drums = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("GuitarJudgeLineDisp"))
									{
										bJudgeLineDisp.Guitar = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("BassJudgeLineDisp"))
									{
										bJudgeLineDisp.Bass = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("DrumsLaneFlush"))
									{
										bLaneFlush.Drums = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("GuitarLaneFlush"))
									{
										bLaneFlush.Guitar = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("BassLaneFlush"))
									{
										bLaneFlush.Bass = CConversion.bONorOFF(str4[0]);
									}
									else if( str3.Equals( "JudgeAnimeType" ) )
									{
										nJudgeAnimeType = CConversion.nGetNumberIfInRange( str4, 0, 2, nJudgeAnimeType );
									}
									else if (str3.Equals( "JudgeFrames"))
									{
										nJudgeFrames = CConversion.nStringToInt( str4, nJudgeFrames );
									}
									else if ( str3.Equals( "JudgeInterval" ))
									{
										nJudgeInterval = CConversion.nStringToInt( str4, nJudgeInterval );
									}
									else if ( str3.Equals( "JudgeWidgh" ))
									{
										nJudgeWidgh = CConversion.nStringToInt( str4, nJudgeWidgh );
									}
									else if ( str3.Equals( "JudgeHeight" ))
									{
										nJudgeHeight = CConversion.nStringToInt( str4, nJudgeHeight );
									}
									else if ( str3.Equals( "ExplosionFrames" ))
									{
										nExplosionFrames = CConversion.nGetNumberIfInRange( str4, 0, int.MaxValue, (int)nExplosionFrames );
									}
									else if ( str3.Equals( "ExplosionInterval" ))
									{
										nExplosionInterval = CConversion.nGetNumberIfInRange( str4, 0, int.MaxValue, (int)nExplosionInterval );
									}
									else if ( str3.Equals( "ExplosionWidgh" ))
									{
										nExplosionWidgh = CConversion.nGetNumberIfInRange(str4, 0, int.MaxValue, (int)nExplosionWidgh);
									}
									else if ( str3.Equals( "ExplosionHeight" ))
									{
										nExplosionHeight = CConversion.nGetNumberIfInRange(str4, 0, int.MaxValue, (int)nExplosionHeight);
									}
									else if ( str3.Equals( "PedalLagTime" ) )
									{
										nPedalLagTime = CConversion.nGetNumberIfInRange( str4, -100, 100, nPedalLagTime );
									}
									else if ( str3.Equals( "WailingFireFrames" ))
									{
										nWailingFireFrames = CConversion.nGetNumberIfInRange( str4, 0, int.MaxValue, (int)nWailingFireFrames );
									}
									else if (str3.Equals("WailingFireInterval"))
									{
										nWailingFireInterval = CConversion.nGetNumberIfInRange( str4, 0, int.MaxValue, (int)nWailingFireInterval );
									}
									else if (str3.Equals("WailingFireWidgh"))
									{
										nWailingFireWidgh = CConversion.nGetNumberIfInRange(str4, 0, int.MaxValue, (int)nWailingFireWidgh);
									}
									else if (str3.Equals("WailingFireHeight"))
									{
										nWailingFireHeight = CConversion.nGetNumberIfInRange(str4, 0, int.MaxValue, (int)nWailingFireHeight);
									}
									else if (str3.Equals("WailingFirePosXGuitar"))
									{
										nWailingFireX.Guitar = CConversion.nStringToInt( str4, nWailingFireX.Guitar );
									}
									else if (str3.Equals("WailingFirePosXBass"))
									{
										nWailingFireX.Bass = CConversion.nStringToInt( str4, nWailingFireX.Bass );
									}
									else if (str3.Equals("WailingFirePosY"))
									{
										nWailingFireY = CConversion.nStringToInt( str4, nWailingFireY );
									}
									else if ( str3.Equals( "DrumComboDisp" ) )				// #29500 2012.9.11 kairera0467
									{
										bドラムコンボ文字の表示 = CConversion.bONorOFF(str4[0]);
									}

									//fork
									else if (str3.Equals("DrumAutoGhost")) // #35411 2015.08.18 chnmr0 add
									{
										eAutoGhost.Drums = (EAutoGhostData)CConversion.nGetNumberIfInRange(str4, 0, 3, 0);
									}
									else if (str3.Equals("GuitarAutoGhost")) // #35411 2015.08.18 chnmr0 add
									{
										eAutoGhost.Guitar = (EAutoGhostData)CConversion.nGetNumberIfInRange(str4, 0, 3, 0);
									}
									else if (str3.Equals("BassAutoGhost")) // #35411 2015.08.18 chnmr0 add
									{
										eAutoGhost.Bass = (EAutoGhostData)CConversion.nGetNumberIfInRange(str4, 0, 3, 0);
									}
									else if (str3.Equals("DrumTargetGhost")) // #35411 2015.08.18 chnmr0 add
									{
										eTargetGhost.Drums = (ETargetGhostData)CConversion.nGetNumberIfInRange(str4, 0, 4, 0);
									}
									else if (str3.Equals("GuitarTargetGhost")) // #35411 2015.08.18 chnmr0 add
									{
										eTargetGhost.Guitar = (ETargetGhostData)CConversion.nGetNumberIfInRange(str4, 0, 4, 0);
									}
									else if (str3.Equals("BassTargetGhost")) // #35411 2015.08.18 chnmr0 add
									{
										eTargetGhost.Bass = (ETargetGhostData)CConversion.nGetNumberIfInRange(str4, 0, 4, 0);
									}
									continue;
								}
								//-----------------------------
								#endregion

								#region [ [AutoPlay] ]
								//-----------------------------
								case ESectionType.AutoPlay:
									if( str3.Equals( "LC" ) )
									{
										bAutoPlay.LC = CConversion.bONorOFF( str4[ 0 ] );
									}
									if( str3.Equals( "HH" ) )
									{
										bAutoPlay.HH = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if( str3.Equals( "SD" ) )
									{
										bAutoPlay.SD = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if( str3.Equals( "BD" ) )
									{
										bAutoPlay.BD = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if( str3.Equals( "HT" ) )
									{
										bAutoPlay.HT = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if( str3.Equals( "LT" ) )
									{
										bAutoPlay.LT = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if( str3.Equals( "FT" ) )
									{
										bAutoPlay.FT = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if( str3.Equals( "CY" ) )
									{
										bAutoPlay.CY = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if (str3.Equals("RD"))
									{
										bAutoPlay.RD= CConversion.bONorOFF(str4[0]);
									}
									else if( str3.Equals( "LP" ) )
									{
										bAutoPlay.LP = CConversion.bONorOFF(str4[0]);
									}
									else if (str3.Equals("LBD"))
									{
										bAutoPlay.LBD = CConversion.bONorOFF(str4[0]);
									}
									//else if( str3.Equals( "Guitar" ) )
									//{
									//    this.bAutoPlay.Guitar = CConversion.bONorOFF( str4[ 0 ] );
									//}
									else if ( str3.Equals( "GuitarR" ) )
									{
										bAutoPlay.GtR = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if ( str3.Equals( "GuitarG" ) )
									{
										bAutoPlay.GtG = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if ( str3.Equals( "GuitarB" ) )
									{
										bAutoPlay.GtB = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if ( str3.Equals( "GuitarY" ) )
									{
										bAutoPlay.GtY = CConversion.bONorOFF(str4[0]);
									}
									else if ( str3.Equals( "GuitarP" ) )
									{
										bAutoPlay.GtP = CConversion.bONorOFF(str4[0]);
									}
									else if ( str3.Equals( "GuitarPick" ) )
									{
										bAutoPlay.GtPick = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if ( str3.Equals( "GuitarWailing" ) )
									{
										bAutoPlay.GtW = CConversion.bONorOFF( str4[ 0 ] );
									}
									//else if ( str3.Equals( "Bass" ) )
									//{
									//    this.bAutoPlay.Bass = CConversion.bONorOFF( str4[ 0 ] );
									//}
									else if ( str3.Equals( "BassR" ) )
									{
										bAutoPlay.BsR = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if ( str3.Equals( "BassG" ) )
									{
										bAutoPlay.BsG = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if ( str3.Equals( "BassB" ) )
									{
										bAutoPlay.BsB = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if ( str3.Equals( "BassY" ) )
									{
										bAutoPlay.BsY = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if ( str3.Equals( "BassP" ) )
									{
										bAutoPlay.BsP = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if ( str3.Equals( "BassPick" ) )
									{
										bAutoPlay.BsPick = CConversion.bONorOFF( str4[ 0 ] );
									}
									else if ( str3.Equals( "BassWailing" ) )
									{
										bAutoPlay.BsW = CConversion.bONorOFF( str4[ 0 ] );
									}
									continue;
								//-----------------------------
								#endregion

								#region [ [HitRange] ]
								//-----------------------------
								case ESectionType.HitRange:
									// map the legacy hit ranges to apply to each category
									// they will only appear when the program is running from an unmigrated state,
									// so simply copy values over whenever there is a change
									STHitRanges stLegacyHitRanges = new STHitRanges(nDefaultSizeMs: -1);
									if (tTryReadHitRangesField(str3, str4, string.Empty, ref stLegacyHitRanges))
									{
										stDrumHitRanges = STHitRanges.tCompose(stLegacyHitRanges, stDrumHitRanges);
										stDrumPedalHitRanges = STHitRanges.tCompose(stLegacyHitRanges, stDrumPedalHitRanges);
										stGuitarHitRanges = STHitRanges.tCompose(stLegacyHitRanges, stGuitarHitRanges);
										stBassHitRanges = STHitRanges.tCompose(stLegacyHitRanges, stBassHitRanges);
										continue;
									}

									if (tTryReadHitRangesField(str3, str4, @"Drum", ref stDrumHitRanges))
										continue;

									if (tTryReadHitRangesField(str3, str4, @"DrumPedal", ref stDrumPedalHitRanges))
										continue;

									if (tTryReadHitRangesField(str3, str4, @"Guitar", ref stGuitarHitRanges))
										continue;

									if (tTryReadHitRangesField(str3, str4, @"Bass", ref stBassHitRanges))
										continue;

									continue;
								//-----------------------------
								#endregion

								#region [ [DiscordRichPresence] ]
								case ESectionType.DiscordRichPresence:
									switch (str3)
									{
										case @"Enable":
											bDiscordRichPresenceEnabled = CConversion.bONorOFF(str4[0]);
											break;
										case @"ApplicationID":
											strDiscordRichPresenceApplicationID = str4;
											break;
										case @"LargeImage":
											strDiscordRichPresenceLargeImageKey = str4;
											break;
										case @"SmallImageDrums":
											strDiscordRichPresenceSmallImageKeyDrums = str4;
											break;
										case @"SmallImageGuitar":
											strDiscordRichPresenceSmallImageKeyGuitar = str4;
											break;
									}
									continue;
								#endregion

								#region [ [GUID] ]
								//-----------------------------
								case ESectionType.GUID:
									if( str3.Equals( "JoystickID" ) )
									{
										tAcquireJoystickID( str4 );
									}
									continue;
								//-----------------------------
								#endregion

								#region [ [DrumsKeyAssign] ]
								//-----------------------------
								case ESectionType.DrumsKeyAssign:
								{
									if( str3.Equals( "HH" ) )
									{
										tReadAndSetSkey( str4, KeyAssign.Drums.HH );
									}
									else if( str3.Equals( "SD" ) )
									{
										tReadAndSetSkey( str4, KeyAssign.Drums.SD );
									}
									else if( str3.Equals( "BD" ) )
									{
										tReadAndSetSkey( str4, KeyAssign.Drums.BD );
									}
									else if( str3.Equals( "HT" ) )
									{
										tReadAndSetSkey( str4, KeyAssign.Drums.HT );
									}
									else if( str3.Equals( "LT" ) )
									{
										tReadAndSetSkey( str4, KeyAssign.Drums.LT );
									}
									else if( str3.Equals( "FT" ) )
									{
										tReadAndSetSkey( str4, KeyAssign.Drums.FT );
									}
									else if( str3.Equals( "CY" ) )
									{
										tReadAndSetSkey( str4, KeyAssign.Drums.CY );
									}
									else if( str3.Equals( "HO" ) )
									{
										tReadAndSetSkey( str4, KeyAssign.Drums.HHO );
									}
									else if( str3.Equals( "RD" ) )
									{
										tReadAndSetSkey( str4, KeyAssign.Drums.RD );
									}
									else if( str3.Equals( "LC" ) )
									{
										tReadAndSetSkey( str4, KeyAssign.Drums.LC );
									}
									else if( str3.Equals( "LP" ) )										// #27029 2012.1.4 from
									{																	//
										tReadAndSetSkey( str4, KeyAssign.Drums.LP );	//
									}																	//
									else if (str3.Equals( "LBD" ))										
									{																	
										tReadAndSetSkey( str4, KeyAssign.Drums.LBD );	
									}	
									continue;
								}
								//-----------------------------
								#endregion

								#region [ [GuitarKeyAssign] ]
								//-----------------------------
								case ESectionType.GuitarKeyAssign:
								{
									if( str3.Equals( "R" ) )
									{
										tReadAndSetSkey( str4, KeyAssign.Guitar.R );
									}
									else if( str3.Equals( "G" ) )
									{
										tReadAndSetSkey( str4, KeyAssign.Guitar.G );
									}
									else if( str3.Equals( "B" ) )
									{
										tReadAndSetSkey( str4, KeyAssign.Guitar.B );
									}
									else if( str3.Equals( "Y" ) )
									{
										tReadAndSetSkey( str4, KeyAssign.Guitar.Y );
									}
									else if( str3.Equals( "P" ) )
									{
										tReadAndSetSkey( str4, KeyAssign.Guitar.P );
									}
									else if( str3.Equals( "Pick" ) )
									{
										tReadAndSetSkey( str4, KeyAssign.Guitar.Pick );
									}
									else if( str3.Equals( "Wail" ) )
									{
										tReadAndSetSkey( str4, KeyAssign.Guitar.Wail );
									}
									else if( str3.Equals( "Decide" ) )
									{
										tReadAndSetSkey( str4, KeyAssign.Guitar.Decide );
									}
									else if (str3.Equals("Cancel"))
									{
										tReadAndSetSkey(str4, KeyAssign.Guitar.Cancel);
									}
									continue;
								}
								//-----------------------------
								#endregion

								#region [ [BassKeyAssign] ]
								//-----------------------------
								case ESectionType.BassKeyAssign:
									if( str3.Equals( "R" ) )
									{
										tReadAndSetSkey( str4, KeyAssign.Bass.R );
									}
									else if( str3.Equals( "G" ) )
									{
										tReadAndSetSkey( str4, KeyAssign.Bass.G );
									}
									else if( str3.Equals( "B" ) )
									{
										tReadAndSetSkey( str4, KeyAssign.Bass.B );
									}
									else if( str3.Equals( "Y" ) )
									{
										tReadAndSetSkey( str4, KeyAssign.Bass.Y );
									}
									else if( str3.Equals( "P" ) ) 
									{
										tReadAndSetSkey( str4, KeyAssign.Bass.P );
									}
									else if( str3.Equals( "Pick" ) )
									{
										tReadAndSetSkey( str4, KeyAssign.Bass.Pick );
									}
									else if( str3.Equals( "Wail" ) )
									{
										tReadAndSetSkey( str4, KeyAssign.Bass.Wail );
									}
									else if( str3.Equals( "Decide" ) )
									{
										tReadAndSetSkey( str4, KeyAssign.Bass.Decide );
									}
									else if (str3.Equals("Cancel"))
									{
										tReadAndSetSkey(str4, KeyAssign.Bass.Cancel);
									}
									continue;
								//-----------------------------
								#endregion

								#region [ [SystemKeyAssign] ]
								//-----------------------------
								case ESectionType.SystemKeyAssign:
									if( str3.Equals( "Capture" ) )
									{
										tReadAndSetSkey( str4, KeyAssign.System.Capture );
									}
									else if (str3.Equals("Search"))
									{
										tReadAndSetSkey(str4, KeyAssign.System.Search);
									}
									else if (str3.Equals("Help"))
									{
										tReadAndSetSkey(str4, KeyAssign.Guitar.Help);
									}
									else if (str3.Equals("Pause"))
									{
										tReadAndSetSkey(str4, KeyAssign.Bass.Help);
									}
									else if (str3.Equals("LoopCreate"))
									{
										tReadAndSetSkey(str4, KeyAssign.System.LoopCreate);
									}
									else if (str3.Equals("LoopDelete"))
									{
										tReadAndSetSkey(str4, KeyAssign.System.LoopDelete);
									}
									else if (str3.Equals("SkipForward"))
									{
										tReadAndSetSkey(str4, KeyAssign.System.SkipForward);
									}
									else if (str3.Equals("SkipBackward"))
									{
										tReadAndSetSkey(str4, KeyAssign.System.SkipBackward);
									}
									else if (str3.Equals("IncreasePlaySpeed"))
									{
										tReadAndSetSkey(str4, KeyAssign.System.IncreasePlaySpeed);
									}
									else if (str3.Equals("DecreasePlaySpeed"))
									{
										tReadAndSetSkey(str4, KeyAssign.System.DecreasePlaySpeed);
									}
									else if (str3.Equals("Restart"))
									{
										tReadAndSetSkey(str4, KeyAssign.System.Restart);
									}
									continue;
								//-----------------------------
								#endregion

							}
						}
					}
					continue;
				}
				catch ( Exception exception )
				{
					Trace.TraceError( exception.Message );
					continue;
				}
			}
		}
	}

	/// <summary>
	/// Read the INI <see cref="STHitRanges"/> field, if any, described by the given parameters into the given <see cref="STHitRanges"/>.
	/// </summary>
	/// <param name="strFieldName">The name of the INI field being read from.</param>
	/// <param name="strFieldValue">The value of the INI field being read from.</param>
	/// <param name="strName">The unique identifier of <paramref name="stHitRanges"/>.</param>
	/// <param name="stHitRanges">The <see cref="STHitRanges"/> to read into.</param>
	/// <returns>Whether or not a field was read.</returns>
	private bool tTryReadHitRangesField(string strFieldName, string strFieldValue, string strName, ref STHitRanges stHitRanges)
	{
		const int nRangeMin = 0, nRangeMax = 0x3e7;
		switch (strFieldName)
		{
			// perfect range size (±ms)
			case var n when n == $@"{strName}Perfect":
				stHitRanges.nPerfectSizeMs = CConversion.nGetNumberIfInRange(strFieldValue, nRangeMin, nRangeMax, stHitRanges.nPerfectSizeMs);
				return true;

			// great range size (±ms)
			case var n when n == $@"{strName}Great":
				stHitRanges.nGreatSizeMs = CConversion.nGetNumberIfInRange(strFieldValue, nRangeMin, nRangeMax, stHitRanges.nGreatSizeMs);
				return true;

			// good range size (±ms)
			case var n when n == $@"{strName}Good":
				stHitRanges.nGoodSizeMs = CConversion.nGetNumberIfInRange(strFieldValue, nRangeMin, nRangeMax, stHitRanges.nGoodSizeMs);
				return true;

			// poor range size (±ms)
			case var n when n == $@"{strName}Poor":
				stHitRanges.nPoorSizeMs = CConversion.nGetNumberIfInRange(strFieldValue, nRangeMin, nRangeMax, stHitRanges.nPoorSizeMs);
				return true;

			// unknown field
			default:
				return false;
		}
	}

	/// <summary>
	/// ギターとベースのキーアサイン入れ替え
	/// </summary>
	/*
	public void SwapGuitarBassKeyAssign()		// #24063 2011.1.16 yyagi
	{
		for ( int j = 0; j <= (int)EKeyConfigPad.Capture; j++ )
		{
			CKeyAssign.STKEYASSIGN t; //= new CConfigIni.CKeyAssign.STKEYASSIGN();
			for ( int k = 0; k < 16; k++ )
			{
				t = this.KeyAssign[ (int)EKeyConfigPart.GUITAR ][ j ][ k ];
				this.KeyAssign[ (int)EKeyConfigPart.GUITAR ][ j ][ k ] = this.KeyAssign[ (int)EKeyConfigPart.BASS ][ j ][ k ];
				this.KeyAssign[ (int)EKeyConfigPart.BASS ][ j ][ k ] = t;
			}
		}
		this.bIsSwappedGuitarBass = !bIsSwappedGuitarBass;
	}
	*/


	// Other

	#region [ private ]
	//-----------------
	private enum ESectionType
	{
		Unknown,
		System,
		Log,
		PlayOption,
		AutoPlay,
		HitRange,
		DiscordRichPresence,
		GUID,
		DrumsKeyAssign,
		GuitarKeyAssign,
		BassKeyAssign,
		SystemKeyAssign,
		Temp,
	}

	private bool _bDrums有効;
	private bool _bGuitar有効;
	private bool bConfigIniExists;
	private string ConfigIniファイル名;

	private void tAcquireJoystickID( string strキー記述 )
	{
		string[] strArray = strキー記述.Split( new char[] { ',' } );
		if( strArray.Length >= 2 )
		{
			int result = 0;
			if( ( int.TryParse( strArray[ 0 ], out result ) && ( result >= 0 ) ) && ( result <= 9 ) )
			{
				if( joystickDict.ContainsKey( result ) )
				{
					joystickDict.Remove( result );
				}
				joystickDict.Add( result, strArray[ 1 ] );
			}
		}
	}
	private void tClearAllKeyAssignments()
	{
		KeyAssign = new CKeyAssign();
		for( int i = 0; i <= (int)EKeyConfigPart.SYSTEM; i++ )
		{
			for( int j = 0; j < (int)EKeyConfigPad.MAX; j++ )
			{
				KeyAssign[ i ][ j ] = new CKeyAssign.STKEYASSIGN[ 16 ];
				for( int k = 0; k < 16; k++ )
				{
					KeyAssign[ i ][ j ][ k ] = new CKeyAssign.STKEYASSIGN( EInputDevice.Unknown, 0, 0 );
				}
			}
		}
	}
	private void tWriteKey( StreamWriter sw, CKeyAssign.STKEYASSIGN[] assign )
	{
		bool flag = true;
		for( int i = 0; i < 0x10; i++ )
		{
			if( assign[ i ].InputDevice == EInputDevice.Unknown )
			{
				continue;
			}
			if( !flag )
			{
				sw.Write( ',' );
			}
			flag = false;
			switch( assign[ i ].InputDevice )
			{
				case EInputDevice.Keyboard:
					sw.Write( 'K' );
					break;

				case EInputDevice.MIDI入力:
					sw.Write( 'M' );
					break;

				case EInputDevice.Joypad:
					sw.Write( 'J' );
					break;

				case EInputDevice.Mouse:
					sw.Write( 'N' );
					break;
			}
			sw.Write( "{0}{1}", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".Substring( assign[ i ].ID, 1 ), assign[ i ].Code );	// #24166 2011.1.15 yyagi: to support ID > 10, change 2nd character from Decimal to 36-numeral system. (e.g. J1023 -> JA23)
		}
	}
	private void tReadAndSetSkey(string strキー記述, CKeyAssign.STKEYASSIGN[] assign)
	{
		string[] strArray = strキー記述.Split(new char[] { ',' });
		for (int i = 0; (i < strArray.Length) && (i < 16); i++)
		{
			EInputDevice eInputDevice;
			int id;
			int code;
			string str = strArray[i].Trim().ToUpper();
			if (str.Length >= 3)
			{
				eInputDevice = EInputDevice.Unknown;
				switch (str[0])
				{
					case 'J':
						eInputDevice = EInputDevice.Joypad;
						break;

					case 'K':
						eInputDevice = EInputDevice.Keyboard;
						break;

					case 'L':
						continue;

					case 'M':
						eInputDevice = EInputDevice.MIDI入力;
						break;

					case 'N':
						eInputDevice = EInputDevice.Mouse;
						break;
				}
			}
			else
			{
				continue;
			}
			id = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(str[1]);	// #24166 2011.1.15 yyagi: to support ID > 10, change 2nd character from Decimal to 36-numeral system. (e.g. J1023 -> JA23)
			if (((id >= 0) && int.TryParse(str.Substring(2), out code)) && ((code >= 0) && (code <= 0xff)))
			{
				tDeleteAlreadyAssignedInputs(eInputDevice, id, code);
				assign[i].InputDevice = eInputDevice;
				assign[i].ID = id;
				assign[i].Code = code;
			}
		}
	}
	private void tSetDefaultKeyAssignments()
	{
		tClearAllKeyAssignments();

		string strDefaultKeyAssign = @"
[DrumsKeyAssign]

HH=K033
SD=K012,K013
BD=K0126,K048
HT=K031,K015
LT=K011,K016
FT=K023,K017
CY=K022,K019
HO=K028
RD=K047,K020
LC=K035,K010
LP=K087
LBD=K077

[GuitarKeyAssign]

R=K054
G=K055,J012
B=K056
Y=K057
P=K058
Pick=K0115,K046,J06
Wail=K0116
Decide=K060
Cancel=K0115

[BassKeyAssign]

R=K090
G=K091,J013
B=K092
Y=K093
P=K094
Pick=K0103,K0100,J08
Wail=K089
Decide=K096
Cancel=K0103

[SystemKeyAssign]
Capture=K065
Search=K042
Help=K064
Pause=K0110
LoopCreate=
LoopDelete=
SkipForward=
SkipBackward=
IncreasePlaySpeed=
DecreasePlaySpeed=
Restart=K052
";
		tReadFromString( strDefaultKeyAssign );
	}
	//-----------------
	#endregion
}
