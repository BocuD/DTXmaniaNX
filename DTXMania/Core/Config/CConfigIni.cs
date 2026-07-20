using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using DTXMania.Core.Framework;

namespace DTXMania.Core;

internal partial class CConfigIni
{
	// Class

	public enum ESoundDeviceTypeForConfig
	{
		ACM = 0,
		ASIO,
		WASAPI,
		WASAPI_Share,
		Unknown = 99
	}

	// プロパティ

#if false // #23625 2011.1.11 Config.iniからダメージ/回復値の定数変更を行う場合はここを有効にする 087リリースに合わせ機能無効化
		//----------------------------------------
		public float[,] fGaugeFactor = new float[5,2];
		public float[] fDamageLevelFactor = new float[3];
		//----------------------------------------
#endif

	public enum LanguageMode
	{
		[EnumLabel("Auto", "自動")] Auto,
		[EnumLabel("Japanese", "日本語")] Japanese,
		[EnumLabel("English", "英語")] English
	}

	public LanguageMode languageMode;
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
	public STDGBVALUE<bool> bGraph有効; // #24074 2011.01.23 add ikanick
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
	public int nWindowWidth; // #23510 2010.10.31 yyagi add
	public int nWindowHeight; // #23510 2010.10.31 yyagi add
	public bool DisplayBonusEffects;
	public bool bHAZARD;

	public int
		nSoundDriverType; // #24820 2012.12.23 yyagi 出力サウンドデバイス(0=ACM(にしたいが設計がきつそうならDirectShow), 1=ASIO, 2=WASAPI)

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
	public STDGBVALUE<EDarkMode> eDark
	{
		get
		{
			STDGBVALUE<EDarkMode> ret = new();
			for (int i = 0; i < 3; i++)
			{
				if (CDTXMania.ConfigIni.nLaneDisp[i] == 3
				    && !CDTXMania.ConfigIni.bJudgeLineDisp[i]
				    && !CDTXMania.ConfigIni.bLaneFlush[i])
				{
					ret[i] = EDarkMode.FULL;
				}
				else if (CDTXMania.ConfigIni.nLaneDisp[i] == 1
				         && CDTXMania.ConfigIni.bJudgeLineDisp[i]
				         && CDTXMania.ConfigIni.bLaneFlush[i])
				{
					ret[i] = EDarkMode.HALF;
				}
				else ret[i] = EDarkMode.OFF;
			}
			return ret; 
		}
	}

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
	public int nSleepNMsEveryFrame;			// #xxxxx 2011.11.27 yyagi add
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

	public enum UnpackSongs
	{
		Off,
		Ask,
		Always
	}
	
	public UnpackSongs eUnpackSongs;
	
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
		get => _bDrumsEnabled;
		set
		{
			_bDrumsEnabled = value;
			if ( !_bGuitarEnabled && !_bDrumsEnabled )
			{
				_bGuitarEnabled = true;
			}
		}
	}

	public bool bInstrumentAvailable(EInstrumentPart inst)
	{
		switch (inst)
		{
			case EInstrumentPart.DRUMS:
				return _bDrumsEnabled;
			case EInstrumentPart.GUITAR:
			case EInstrumentPart.BASS:
				return _bGuitarEnabled;
			default:
				return false;
		}
	}

	public bool bSingleGuitar = true;
	
	public bool bGuitarEnabled
	{
		get => _bGuitarEnabled;
		set
		{
			_bGuitarEnabled = value;
			if ( !_bGuitarEnabled && !_bDrumsEnabled )
			{
				_bDrumsEnabled = true;
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
				if ( !bAutoPlay[ i ] )
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
			if ( value < 0 )
			{
				nBGAlpha = 0;
			}
			else if ( value > 0xff )
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
	/// The user's saved "Custom" auto-play flags. Kept separate from <see cref="bAutoPlay"/> so that
	/// cycling through the auto-play presets (which overwrite bAutoPlay) doesn't clobber the custom set.
	/// </summary>
	public STAUTOPLAY bAutoPlayCustom;

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
		eUnpackSongs = UnpackSongs.Ask;
		languageMode = LanguageMode.Auto;
		bFullScreenMode = false;
		bFullScreenExclusive = true;
		bVerticalSyncWait = true;
		nInitialWindowXPosition = 50; // #30675 2013.02.04 ikanick add
		nInitialWindowYPosition = 50;
		//this.bDirectShowMode = true;
		nWindowWidth = GameWindowSize.Width;			// #23510 2010.10.31 yyagi add
		nWindowHeight = GameWindowSize.Height;			// 
		nMovieMode = 1;
		nMovieAlpha = 0;
		nJudgeLine.Drums = 0;
		nJudgeLine.Guitar = 0;
		nJudgeLine.Bass = 0;
		nShutterInSide = new STDGBVALUE<int>();
		nShutterInSide.Drums = 0;
		nShutterOutSide = new STDGBVALUE<int>();
		nShutterOutSide.Drums = 0;
		nSleepNMsEveryFrame = -1;			// #xxxxx 2011.11.27 yyagi add
		n非フォーカス時スリープms = 1;			// #23568 2010.11.04 ikanick add
		_bGuitarEnabled = false;
		_bDrumsEnabled = true;
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

		bAutoPlayCustom = new STAUTOPLAY();
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
		nSoundDriverType = (int)ESoundDeviceTypeForConfig.ACM; // #24820 2012.12.23 yyagi 初期値はACM
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
					if ( ( ( KeyAssign[ i ][ j ][ k ].InputDevice == DeviceType ) && ( KeyAssign[ i ][ j ][ k ].ID == nID ) ) && ( KeyAssign[ i ][ j ][ k ].Code == nCode ) )
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

		tWriteSchemaSection( sw, "System" );
		sw.WriteLine( ";-------------------" );

		tWriteSchemaSection( sw, "Log" );
		sw.WriteLine( ";-------------------" );

		tWriteSchemaSection( sw, "PlayOption" );
		sw.WriteLine( ";-------------------" );

		tWriteSchemaSection( sw, "AutoPlay" );
		tWriteSchemaSection( sw, "AutoPlayCustom" );
		sw.WriteLine( ";-------------------" );

		tWriteSchemaSection( sw, "HitRange" );
		sw.WriteLine( ";-------------------" );

		tWriteSchemaSection( sw, "DiscordRichPresence" );
		sw.WriteLine( ";-------------------" );

		tWriteSchemaSection( sw, "GUID" );

		// The key-assign legend is emitted as [DrumsKeyAssign]'s header lines (see KeyAssignLegend).
		tWriteSchemaSection( sw, "DrumsKeyAssign" );
		tWriteSchemaSection( sw, "GuitarKeyAssign" );
		tWriteSchemaSection( sw, "BassKeyAssign" );
		tWriteSchemaSection( sw, "SystemKeyAssign" );

		sw.Close();
	}

	public void tReadFromFile( string iniファイル名 )
	{
		ConfigIniファイル名 = iniファイル名;
		bConfigIniExists = File.Exists( ConfigIniファイル名 );
		if ( bConfigIniExists )
		{
			string str;
			StreamReader reader = new StreamReader( ConfigIniファイル名, Encoding.GetEncoding( "Shift_JIS" ) );
			str = reader.ReadToEnd();
			tReadFromString( str );
		}
	}

	private void tReadFromString( string strAllSettings )	// 2011.4.13 yyagi; refactored to make initial KeyConfig easier.
	{
		string currentSectionName = "";
		string[] delimiter = { "\n" };
		string[] strSingleLine = strAllSettings.Split( delimiter, StringSplitOptions.RemoveEmptyEntries );
		foreach ( string s in strSingleLine )
		{
			string str = s.Replace( '\t', ' ' ).TrimStart( new char[] { '\t', ' ' } );
			if ( ( str.Length == 0 ) || ( str[ 0 ] == ';' ) )
			{
				continue;
			}

			try
			{
				if ( str[ 0 ] == '[' )
				{
					// Section change: capture the name between the brackets.
					StringBuilder builder = new StringBuilder( 32 );
					int num = 1;
					while ( ( num < str.Length ) && ( str[ num ] != ']' ) )
					{
						builder.Append( str[ num++ ] );
					}
					currentSectionName = builder.ToString();
					continue;
				}

				string[] strArray = str.Split( new char[] { '=' } );
				if ( strArray.Length != 2 )
				{
					continue;
				}
				string str3 = strArray[ 0 ].Trim();
				string str4 = strArray[ 1 ].Trim();

				// Every section is described declaratively by the schema (CConfigIni.Schema.cs) and
				// dispatched through a single lookup — no per-section special cases.
				tReadSchemaField( currentSectionName, str3, str4 );
			}
			catch ( Exception exception )
			{
				Trace.TraceError( exception.Message );
			}
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

	private bool _bDrumsEnabled;
	private bool _bGuitarEnabled;
	private bool bConfigIniExists;
	private string ConfigIniファイル名;

	private void tAcquireJoystickID( string strキー記述 )
	{
		string[] strArray = strキー記述.Split( new char[] { ',' } );
		if ( strArray.Length >= 2 )
		{
			int result = 0;
			if ( ( int.TryParse( strArray[ 0 ], out result ) && ( result >= 0 ) ) && ( result <= 9 ) )
			{
				if ( joystickDict.ContainsKey( result ) )
				{
					joystickDict.Remove( result );
				}
				joystickDict.Add( result, strArray[ 1 ] );
			}
		}
	}
	private void tClearAllKeyAssignments()
	{
		// A fresh CKeyAssign already allocates every pad and fills it with unassigned (Unknown) slots.
		KeyAssign = new CKeyAssign();
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

	public void SyncGraphicsSettings(IGameHost host)
	{
		Trace.TraceInformation("Synchronizing graphics settings...");

		//sync graphics options with game host
		FullscreenMode targetMode = FullscreenMode.Windowed;
		if (bFullScreenMode)
		{
			targetMode = bFullScreenExclusive 
				? FullscreenMode.ExclusiveFullscreen 
				: FullscreenMode.BorderlessFullscreen;
		}

		Trace.TraceInformation($"Requesting fullscreen mode: {targetMode}, vsync {(bVerticalSyncWait ? "enabled" : "disabled")}");
		host.RequestFullscreenMode(targetMode);
		host.RequestVsync(bVerticalSyncWait);
		
		host.SetWindowPosition(new Vector2(nInitialWindowXPosition, nInitialWindowYPosition));
		host.SetWindowSize(new Vector2(nWindowWidth, nWindowHeight));
	}
}