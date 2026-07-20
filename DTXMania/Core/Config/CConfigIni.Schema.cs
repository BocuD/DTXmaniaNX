#nullable disable

using FDK;

namespace DTXMania.Core;

//The Config.ini schema itself: the declarative table describing every section, the legend written
//above the key-assign sections, and the section/key lookup used when reading.
//
//Each value is described exactly once by a ConfigItem (see CConfigIni.SchemaModel.cs), built via the
//helpers in CConfigIni.SchemaEngine.cs, so there is a single source of truth per setting: adding a
//value means adding one line here. The format stays backwards compatible — only section/key names
//matter when reading, and the reader tolerates missing, reordered, or unknown entries.
internal partial class CConfigIni
{
	#region [ schema ]

	//Every section of Config.ini is described here. The only exception handled by dedicated code in
	//tReadFromString/tWrite is [GUID], which uses repeated JoystickID keys and so cannot be modelled
	//as one key -> one value.

	//documentation for keyassign
	private static readonly string[] KeyAssignLegend =
	[
		";-------------------",
		"; キーアサイン",
		";   項　目：Keyboard → 'K'＋'0'＋キーコード(10進数)",
		";           Mouse    → 'N'＋'0'＋ボタン番号(0～7)",
		";           MIDI In  → 'M'＋デバイス番号1桁(0～9,A～Z)＋ノート番号(10進数)",
		";           Joystick → 'J'＋デバイス番号1桁(0～9,A～Z)＋ 0 ...... Ｘ減少(左)ボタン",
		";                                                         1 ...... Ｘ増加(右)ボタン",
		";                                                         2 ...... Ｙ減少(上)ボタン",
		";                                                         3 ...... Ｙ増加(下)ボタン",
		";                                                         4 ...... Ｚ減少(前)ボタン",
		";                                                         5 ...... Ｚ増加(後)ボタン",
		";                                                         6～133.. ボタン1～128",
		";           これらの項目を 16 個まで指定可能(',' で区切って記述）。",
		";",
		";   表記例：HH=K044,M042,J16",
		";           → HiHat を Keyboard の 44 ('Z'), MidiIn#0 の 42, JoyPad#1 の 6(ボタン1) に割当て",
		";",
		";   ※Joystick のデバイス番号とデバイスとの関係は [GUID] セクションに記してあるものが有効。",
		";",
		""
	];

	private static readonly ConfigSection[] Schema =
	[
		new("System", [
			G(["リリースバージョン", "Release Version."],
				// Written as the running version, but read into a field so we can detect config upgrades.
				Custom("Version", (c, v) => c.strDTXManiaのバージョン = v, c => CDTXMania.VERSION)),
			G([
					"演奏データの格納されているフォルダへのパス。",
					@"セミコロン(;)で区切ることにより複数のパスを指定できます。（例: d:\DTXFiles1\;e:\DTXFiles2\）",
					"Pathes for DTX data.",
					@"You can specify many pathes separated with semicolon(;). (e.g. d:\DTXFiles1\;e:\DTXFiles2\)"
				],
				Str("DTXPath", c => c.strSongDataSearchPath)),
			G(["ZIPファイルの展開", "0=展開しない, 1=確認する, 2=常に展開する"], Enum("UnpackSongs", 0, 2, c => c.eUnpackSongs)),
			G(["言語設定", "0=自動, 1=日本語, 2=英語", "Language mode", "0=auto, 1=japanese, 2=english"], Enum("Language", 0, 2, c => c.languageMode)),
			G([
					"プレイヤーネーム。",
					"演奏中のネームプレートに表示される名前を設定できます。",
					"英字、数字の他、ひらがな、カタカナ、半角カナ、漢字なども入力できます。",
					"入力されていない場合は「GUEST」と表示されます。"
				],
				Str("CardNameDrums", c => c.strCardName[0]),
				Str("CardNameGuitar", c => c.strCardName[1]),
				Str("CardNameBass", c => c.strCardName[2])),
			G([
					"グループ名っぽいあれ。",
					"演奏中のネームプレートに表示されるXG2でいうグループ名を設定できます。",
					"英字、数字の他、ひらがな、カタカナ、半角カナ、漢字なども入力できます。",
					"入力されていない場合は何も表示されません。"
				],
				Str("GroupNameDrums", c => c.strGroupName[0]),
				Str("GroupNameGuitar", c => c.strGroupName[1]),
				Str("GroupNameBass", c => c.strGroupName[2])),
			G(["ネームカラー", "0=白, 1=薄黄色, 2=黄色, 3=緑, 4=青, 5=紫 以下略。"],
				Int("NameColorDrums", 0, 19, c => c.nNameColor[0]),
				Int("NameColorGuitar", 0, 19, c => c.nNameColor[1]),
				Int("NameColorBass", 0, 19, c => c.nNameColor[2])),
			G(["クリップの表示位置", "0=表示しない, 1=全画面, 2=ウインドウ, 3=全画面&ウインドウ"],
				Custom("MovieMode",
					(c, v) => { c.nMovieMode = CConversion.nGetNumberIfInRange(v, 0, 0xffff, c.nMovieMode); if (c.nMovieMode > 3) c.nMovieMode = 0; },
					c => c.nMovieMode.ToString())),
			G(["レーンの透明度(名前に突っ込まないでください。)", "数値が高いほどレーンが薄くなります。", "0=0% 10=100%"],
				Int("MovieAlpha", 0, 10, c => c.nMovieAlpha)),
			G(["プレイ中にHelpボタンを押したときに出てくる演奏情報の種類。", "0=デバッグ情報 1=判定情報"],
				Int("InfoType", 0, 1, c => c.nInfoType)),
			G([
					"使用するSkinのフォルダ名。",
					@"例えば System\Default\Graphics\... などの場合は、SkinPath=.\Default\ を指定します。",
					"Skin folder path.",
					@"e.g. System\Default\Graphics\... -> Set SkinPath=.\Default\"
				],
				Custom("SkinPath", tReadSkinPath, tWriteSkinPath)),
			G([
					"box.defが指定するSkinに自動で切り替えるかどうか (0=切り替えない、1=切り替える)",
					"Automatically change skin specified in box.def. (0=No 1=Yes)"
				],
				Bool("SkinChangeByBoxDef", c => c.bUseBoxDefSkin)),
			G(["画面モード(0:ウィンドウ, 1:全画面)", "Screen mode. (0:Window, 1:Fullscreen)"],
				Bool("FullScreen", c => c.bFullScreenMode)),
			G("Fullscreen mode uses exclusive mode instead of maximized window. (0:Maximized window, 1:Exclusive)",
				Bool("FullScreenExclusive", c => c.bFullScreenExclusive)),
			G(["ウインドウモード時の画面幅", "A width size in the window mode."],
				Custom("WindowWidth",
					(c, v) => { c.nWindowWidth = CConversion.nGetNumberIfInRange(v, 1, 65535, c.nWindowWidth); if (c.nWindowWidth <= 0) c.nWindowWidth = GameWindowSize.Width; },
					c => c.nWindowWidth.ToString())),
			G(["ウインドウモード時の画面高さ", "A height size in the window mode."],
				Custom("WindowHeight",
					(c, v) => { c.nWindowHeight = CConversion.nGetNumberIfInRange(v, 1, 65535, c.nWindowHeight); if (c.nWindowHeight <= 0) c.nWindowHeight = GameWindowSize.Height; },
					c => c.nWindowHeight.ToString())),
			G(["ウィンドウモード時の位置X", "X position in the window mode."],
				Custom("WindowX",
					(c, v) => c.nInitialWindowXPosition = CConversion.nGetNumberIfInRange(v, 0, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width - 1, c.nInitialWindowXPosition),
					c => c.nInitialWindowXPosition.ToString())),
			G(["ウィンドウモード時の位置Y", "Y position in the window mode."],
				Custom("WindowY",
					(c, v) => c.nInitialWindowYPosition = CConversion.nGetNumberIfInRange(v, 0, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height - 1, c.nInitialWindowYPosition),
					c => c.nInitialWindowYPosition.ToString())),
			G([
					"ウインドウをダブルクリックした時にフルスクリーンに移行するか(0:移行しない,1:移行する)",
					"Whether double click to go full screen mode or not."
				],
				Bool("DoubleClickFullScreen", c => c.bIsAllowedDoubleClickFullscreen)),
			G([
					"ALT+SPACEのメニュー表示を抑制するかどうか(0:抑制する 1:抑制しない)",
					"Whether ALT+SPACE menu would be masked or not.(0=masked 1=not masked)"
				],
				Bool("EnableSystemMenu", c => c.bIsEnabledSystemMenu)),
			G(["非フォーカス時のsleep値[ms]", "A sleep time[ms] while the window is inactive."],
				IntRound("BackSleep", 0, 50, c => c.n非フォーカス時スリープms)),
			G("垂直帰線同期(0:OFF,1:ON)",
				Bool("VSyncWait", c => c.bVerticalSyncWait)),
			G([
					"フレーム毎のsleep値[ms] (-1でスリープ無し, 0以上で毎フレームスリープ。動画キャプチャ等で活用下さい)",
					"A sleep time[ms] per frame."
				],
				IntRound("SleepTimePerFrame", -1, 50, c => c.nSleepNMsEveryFrame)),
			G([
					"サウンド出力方式(0=ACM(って今はまだDirectShowですが), 1=ASIO, 2=WASAPI排他, 3=WASAPI共有",
					"WASAPIはVista以降のOSで使用可能。推奨方式はWASAPI。",
					"なお、WASAPIが使用不可ならASIOを、ASIOが使用不可ならACMを使用します。",
					"Sound device type(0=ACM, 1=ASIO, 2=WASAPI Exclusive, 3=WASAPI Shared)",
					"WASAPI can use on Vista or later OSs.",
					"If WASAPI is not available, DTXMania try to use ASIO. If ASIO can't be used, ACM is used."
				],
				Int("SoundDeviceType", 0, 3, c => c.nSoundDriverType)),
			G([
					"WASAPI使用時のサウンドバッファサイズ",
					"(0=デバイスに設定されている値を使用, 1～9999=バッファサイズ(単位:ms)の手動指定",
					"WASAPI Sound Buffer Size.",
					"(0=Use system default buffer size, 1-9999=specify the buffer size(ms) by yourself)"
				],
				Int("WASAPIBufferSizeMs", 0, 9999, c => c.nWASAPIBufferSizeMs)),
			new([
					"ASIO使用時のサウンドデバイス",
					"存在しないデバイスを指定すると、DTXManiaが起動しないことがあります。",
					"Sound device used by ASIO.",
					"Don't specify unconnected device, as the DTXMania may not bootup."
				],
				[
					Custom("ASIODevice",
						(c, v) => c.nASIODevice = CConversion.nGetNumberIfInRange(v, 0, CEnumerateAllAsioDevices.GetAllASIODevices().Length - 1, c.nASIODevice),
						c => c.nASIODevice.ToString())
				],
				() => CEnumerateAllAsioDevices.GetAllASIODevices().Select((name, i) => $"{i}: {name}")),
			G(["WASAPI/ASIO時に使用する演奏タイマーの種類", "Playback timer used for WASAPI/ASIO", "(0=FDK Timer, 1=System Timer)"],
				Bool("SoundTimerType", c => c.bUseOSTimer)),
			G("WASAPI使用時にEventDrivenモードを使う",
				Bool("EventDrivenWASAPI", c => c.bEventDrivenWASAPI)),
			G([
					"Enable Embedded Metronome",
					"Please make sure Metronome.ogg exists in Your current skin sounds folder",
					"e.g. ./System/{Skin}/Sounds/Metronome.ogg"
				],
				Bool("Metronome", c => c.bMetronome)),
			G([
					"Chip PlayTime Compute Mode",
					"Select which method of Chip PlayTime Computation to use: (0=Original, 1=Accurate)",
					"Original method is compatible with other DTXMania players but loses time due to integer truncation",
					"Accurate method improves overall accuracy by using proper number rounding",
					"NOTE: Only songs with many BPM changes will have observable difference in either mode. Single BPM songs are not affected"
				],
				Int("ChipPlayTimeComputeMode", 0, 1, c => c.nChipPlayTimeComputeMode)),
			G(["全体ボリュームの設定", "(0=無音 ～ 100=最大。WASAPI/ASIO時のみ有効)", "Master volume settings", "(0=Silent - 100=Max)"],
				Int("MasterVolume", 0, 100, c => c.nMasterVolume)),
			G(["ギター/ベース有効(0:OFF,1:ON)", "Enable Guitar/Bass or not.(0:OFF,1:ON)"],
				Bool("Guitar", c => c.bGuitarEnabled)),
			G(["シングルプレイのギター有効(0:OFF,1:ON)", "Enable Singleplayer Guitar or not.(0:OFF,1:ON)"],
				Bool("SingleGuitar", c => c.bSingleGuitar)),
			G(["ドラム有効(0:OFF,1:ON)", "Enable Drums or not.(0:OFF,1:ON)"],
				Bool("Drums", c => c.bDrumsEnabled)),
			G([
					"背景画像の半透明割合(0:透明～255:不透明)",
					"Transparency for background image in playing screen.(0:tranaparent - 255:no transparent)"
				],
				Int("BGAlpha", 0, 0xff, c => c.nBackgroundTransparency)),
			G("Missヒット時のゲージ減少割合(0:少, 1:Normal, 2:大)",
				Enum<EDamageLevel>("DamageLevel", 0, 2, c => c.eDamageLevel)),
			G("ゲージゼロでSTAGE FAILED (0:OFF, 1:ON)",
				Bool("StageFailed", c => c.bSTAGEFAILEDEnabled)),
			G([
					"LC/HHC/HHO 打ち分けモード (0:LC|HHC|HHO, 1:LC&(HHC|HHO), 2:LC|(HHC&HHO), 3:LC&HHC&HHO)",
					"LC/HHC/HHO Grouping       (0:LC|HHC|HHO, 1:LC&(HHC|HHO), 2:LC|(HHC&HHO), 3:LC&HHC&HHO)"
				],
				Enum<EHHGroup>("HHGroup", 0, 3, c => c.eHHGroup)),
			G(["LT/FT 打ち分けモード (0:LT|FT, 1:LT&FT)", "LT/FT Grouping       (0:LT|FT, 1:LT&FT)"],
				Enum<EFTGroup>("FTGroup", 0, 2, c => c.eFTGroup)),
			G(["CY/RD 打ち分けモード (0:CY|RD, 1:CY&RD)", "CY/RD Grouping       (0:CY|RD, 1:CY&RD)"],
				Enum<ECYGroup>("CYGroup", 0, 2, c => c.eCYGroup)),
			G([
					"LP/LBD/BD 打ち分けモード(0:LP|LBD|BD, 1:LP|(LBD&BD), 2:LP&(LBD|BD), 3:LP&LBD&BD)",
					"LP/LBD/BD Grouping     (0:LP|LBD|BD, 1:LP(LBD&BD), 2:LP&(LBD|BD), 3:LP&LBD&BD)"
				],
				Enum<EBDGroup>("BDGroup", 0, 3, c => c.eBDGroup)),
			G("打ち分け時の再生音の優先順位(HHGroup)(0:Chip>Pad, 1:Pad>Chip)",
				Enum<EPlaybackPriority>("HitSoundPriorityHH", 0, 1, c => c.eHitSoundPriorityHH)),
			G("打ち分け時の再生音の優先順位(FTGroup)(0:Chip>Pad, 1:Pad>Chip)",
				Enum<EPlaybackPriority>("HitSoundPriorityFT", 0, 1, c => c.eHitSoundPriorityFT)),
			G("打ち分け時の再生音の優先順位(CYGroup)(0:Chip>Pad, 1:Pad>Chip)",
				Enum<EPlaybackPriority>("HitSoundPriorityCY", 0, 1, c => c.eHitSoundPriorityCY)),
			G("打ち分け時の再生音の優先順位(LPGroup)(0:Chip>Pad, 1:Pad>Chip)",
				Enum<EPlaybackPriority>("HitSoundPriorityLP", 0, 1, c => c.eHitSoundPriorityLP)),
			G("シンバルフリーモード(0:OFF, 1:ON)",
				Bool("CymbalFree", c => c.bシンバルフリー)),
			G("AVIの表示(0:OFF, 1:ON)",
				Bool("AVI", c => c.bAVIEnabled)),
			G("BGAの表示(0:OFF, 1:ON)",
				Bool("BGA", c => c.bBGAEnabled)),
			G("フィルイン効果(0:OFF, 1:ON)",
				Bool("FillInEffect", c => c.bFillInEnabled)),
			G("フィルイン達成時の歓声の再生(0:OFF, 1:ON)",
				Bool("AudienceSound", c => c.b歓声を発声する)),
			G("曲選択からプレビュー音の再生までのウェイト[ms]",
				Int("PreviewSoundWait", 0, 0x5f5e0ff, c => c.nSongSelectSoundPreviewWaitTimeMs)),
			G("曲選択からプレビュー画像表示までのウェイト[ms]",
				Int("PreviewImageWait", 0, 0x5f5e0ff, c => c.nSongSelectImagePreviewWaitTimeMs)),
			G("Waveの再生位置自動補正(0:OFF, 1:ON)",
				Bool("AdjustWaves", c => c.bWave再生位置自動調整機能有効)),
			G("BGM の再生(0:OFF, 1:ON)",
				Bool("BGMSound", c => c.bBGM音を発声する)),
			G("ドラム打音の再生(0:OFF, 1:ON)",
				Bool("HitSound", c => c.bドラム打音を発声する)),
			G("演奏記録（～.score.ini）の出力 (0:OFF, 1:ON)",
				Bool("SaveScoreIni", c => c.bScoreIniを出力する)),
			G("RANDOM SELECT で子BOXを検索対象に含める (0:OFF, 1:ON)",
				Bool("RandomFromSubBox", c => c.bランダムセレクトで子BOXを検索対象とする)),
			G("ドラム演奏時にドラム音を強調する (0:OFF, 1:ON)",
				Bool("SoundMonitorDrums", c => c.b演奏音を強調する.Drums)),
			G("ギター演奏時にギター音を強調する (0:OFF, 1:ON)",
				Bool("SoundMonitorGuitar", c => c.b演奏音を強調する.Guitar)),
			G("ベース演奏時にベース音を強調する (0:OFF, 1:ON)",
				Bool("SoundMonitorBass", c => c.b演奏音を強調する.Bass)),
			G(["表示可能な最小コンボ数(1～99999)", "ギターとベースでは0にするとコンボを表示しません。"],
				Int("MinComboDrums", 1, 0x1869f, c => c.n表示可能な最小コンボ数.Drums),
				Int("MinComboGuitar", 0, 0x1869f, c => c.n表示可能な最小コンボ数.Guitar),
				Int("MinComboBass", 0, 0x1869f, c => c.n表示可能な最小コンボ数.Bass)),
			G("曲名表示をdefファイルの曲名にする (0:OFF, 1:ON)",
				Bool("MusicNameDispDef", c => c.b曲名表示をdefのものにする)),
			G(["演奏情報を表示する (0:OFF, 1:ON)", "Showing playing info on the playing screen. (0:OFF, 1:ON)"],
				Bool("ShowDebugStatus", c => c.bShowPerformanceInformation)),
			G("選曲画面の難易度表示をXG表示にする (0:OFF, 1:ON)",
				Bool("Difficulty", c => c.bDisplayDifficultyXGStyle)),
			G("スコアの表示(0:OFF, 1:ON)",
				Bool("ShowScore", c => c.bShowScore)),
			G("演奏中の曲情報の表示(0:OFF, 1:ON)",
				Bool("ShowMusicInfo", c => c.bShowMusicInfo)),
			G("Show custom play speed (0:OFF, 1:ON, 2:If changed in game)",
				Int("ShowPlaySpeed", 0, 2, c => c.nShowPlaySpeed)),
			G("読み込み画面、演奏画面、ネームプレート、リザルト画面の曲名で使用するフォント名",
				Str("DisplayFontName", c => c.str曲名表示フォント)),
			G(["選曲リストのフォント名", "Font name for select song item."],
				Str("SelectListFontName", c => c.songListFont)),
			G(["選曲リストのフォントのサイズ[dot]", "Font size[dot] for select song item."],
				Int("SelectListFontSize", 1, 0x3e7, c => c.n選曲リストフォントのサイズdot)),
			G(["選曲リストのフォントを斜体にする (0:OFF, 1:ON)", "Using italic font style select song list. (0:OFF, 1:ON)"],
				Bool("SelectListFontItalic", c => c.b選曲リストフォントを斜体にする)),
			G(["選曲リストのフォントを太字にする (0:OFF, 1:ON)", "Using bold font style select song list. (0:OFF, 1:ON)"],
				Bool("SelectListFontBold", c => c.b選曲リストフォントを太字にする)),
			G(["打音の音量(0～100%)", "Sound volume (you're playing) (0-100%)"],
				Int("ChipVolume", 0, 100, c => c.n手動再生音量)),
			G(["自動再生音の音量(0～100%)", "Sound volume (auto playing) (0-100%)"],
				Int("AutoChipVolume", 0, 100, c => c.n自動再生音量)),
			G(["ストイックモード(0:OFF, 1:ON)", "Stoic mode. (0:OFF, 1:ON)"],
				Bool("StoicMode", c => c.bストイックモード)),
			G(["バッファ入力モード(0:OFF, 1:ON)", "Using Buffered input (0:OFF, 1:ON)"],
				Bool("BufferedInput", c => c.bBufferedInput)),
			G("オープンハイハットの表示画像(0:DTXMania仕様, 1:○なし, 2:クローズハットと同じ)",
				Enum<EType>("HHOGraphics", 0, 2, c => c.eHHOGraphics.Drums)),
			G("左バスペダルの表示画像(0:バス寄り, 1:LPと同じ)",
				Enum<EType>("LBDGraphics", 0, 1, c => c.eLBDGraphics.Drums)),
			G("ライドシンバルレーンの表示位置(0:...RD RC, 1:...RC RD)",
				Enum<ERDPosition>("RDPosition", 0, 1, c => c.eRDPosition)),
			G(["レーン毎の最大同時発音数(1～8)", "Number of polyphonic sounds per lane. (1-8)"],
				Int("PolyphonicSounds", 1, 8, c => c.nPoliphonicSounds)),
			G(["判定ズレ時間表示(0:OFF, 1:ON, 2=GREAT-POOR)", "Whether displaying the lag times from the just timing or not."],
				Int("ShowLagTime", 0, 2, c => c.nShowLagType)),
			G("判定ズレ時間表示の色(0:Slow赤、Fast青, 1:Slow青、Fast赤)",
				Int("ShowLagTimeColor", 0, 1, c => c.nShowLagTypeColor)),
			G("判定ズレヒット数表示(0:OFF, 1:ON)",
				Bool("ShowLagHitCount", c => c.bShowLagHitCount)),
			G([
					"リザルト画像自動保存機能(0:OFF, 1:ON)",
					"Set ON if you'd like to save result screen image automatically",
					"when you get hiscore/hiskill."
				],
				Bool("AutoResultCapture", c => c.bIsAutoResultCapture)),
			G([
					"再生速度変更を、ピッチ変更で行うかどうか(0:ピッチ変更, 1:タイムストレッチ",
					"(WASAPI/ASIO使用時のみ有効) ",
					"Set \"0\" if you'd like to use pitch shift with PlaySpeed.",
					"Set \"1\" for time stretch.",
					"(Only available when you're using using WASAPI or ASIO)"
				],
				Bool("TimeStretch", c => c.bTimeStretch)),
			G([
					"判定タイミング調整(ドラム, ギター, ベース)(-99～99)[ms]",
					"Revision value to adjust judgement timing for the drums, guitar and bass."
				],
				Int("InputAdjustTimeDrums", -99, 99, c => c.nInputAdjustTimeMs.Drums),
				Int("InputAdjustTimeGuitar", -99, 99, c => c.nInputAdjustTimeMs.Guitar),
				Int("InputAdjustTimeBass", -99, 99, c => c.nInputAdjustTimeMs.Bass)),
			G(["BGMタイミング調整(-99～99)[ms]", "Revision value to adjust judgement timing for BGM."],
				Int("BGMAdjustTime", -99, 99, c => c.nCommonBGMAdjustMs)),
			G([
					"判定ラインの表示位置調整(ドラム, ギター, ベース)(-99～99)[px]",
					"Offset value to adjust displaying judgement line for the drums, guitar and bass."
				],
				Int("JudgeLinePosOffsetDrums", -99, 99, c => c.nJudgeLinePosOffset.Drums),
				Int("JudgeLinePosOffsetGuitar", -99, 99, c => c.nJudgeLinePosOffset.Guitar),
				Int("JudgeLinePosOffsetBass", -99, 99, c => c.nJudgeLinePosOffset.Bass)),
			G(["LC, HH, SD,...の入力切り捨て下限Velocity値(0～127)", "Minimum velocity value for LC, HH, SD, ... to accept."],
				Int("LCVelocityMin", 0, 127, c => c.nVelocityMin.LC),
				Int("HHVelocityMin", 0, 127, c => c.nVelocityMin.HH),
				Int("SDVelocityMin", 0, 127, c => c.nVelocityMin.SD),
				Int("BDVelocityMin", 0, 127, c => c.nVelocityMin.BD),
				Int("HTVelocityMin", 0, 127, c => c.nVelocityMin.HT),
				Int("LTVelocityMin", 0, 127, c => c.nVelocityMin.LT),
				Int("FTVelocityMin", 0, 127, c => c.nVelocityMin.FT),
				Int("CYVelocityMin", 0, 127, c => c.nVelocityMin.CY),
				Int("RDVelocityMin", 0, 127, c => c.nVelocityMin.RD),
				Int("LPVelocityMin", 0, 127, c => c.nVelocityMin.LP),
				Int("LBDVelocityMin", 0, 127, c => c.nVelocityMin.LBD)),
			G("オート時のゲージ加算(0:OFF, 1:ON )",
				Bool("AutoAddGage", c => c.bAutoAddGage)),
			G("Number of milliseconds to skip forward/backward (100-10000)",
				Int("SkipTimeMs", 100, 20000, c => c.nSkipTimeMs))
		]),

		new("PlayOption", [
			G("REVERSEモード(0:OFF, 1:ON)",
				Bool("DrumsReverse", c => c.bReverse.Drums),
				Bool("GuitarReverse", c => c.bReverse.Guitar),
				Bool("BassReverse", c => c.bReverse.Bass)),
			G("ギター/ベースRANDOMモード(0:OFF, 1:Mirror, 2:Random, 3:SuperRandom, 4:HyperRandom)",
				Enum<ERandomMode>("GuitarRandom", 0, 4, c => c.eRandom.Guitar),
				Enum<ERandomMode>("BassRandom", 0, 4, c => c.eRandom.Bass)),
			G("ギター/ベースLIGHTモード(0:OFF, 1:ON)",
				Bool("GuitarLight", c => c.bLight.Guitar),
				Bool("BassLight", c => c.bLight.Bass)),
			G("ギター/ベース演奏モード(0:Normal, 1:Specialist)",
				Bool("GuitarSpecialist", c => c.bSpecialist.Guitar),
				Bool("BassSpecialist", c => c.bSpecialist.Bass)),
			G("ギター/ベースLEFTモード(0:OFF, 1:ON)",
				Bool("GuitarLeft", c => c.bLeft.Guitar),
				Bool("BassLeft", c => c.bLeft.Bass)),
			G(["RISKYモード(0:OFF, 1-10)", "RISKY mode. 0=OFF, 1-10 is the times of misses to be Failed."],
				Int("Risky", 0, 10, c => c.nRisky)),
			G(["HAZARDモード(0:OFF, 1:ON)", "HAZARD mode. 0=OFF, 1=ON is the times of misses to be Failed."],
				Bool("HAZARD", c => c.bHAZARD)),
			G(["TIGHTモード(0:OFF, 1:ON)", "TIGHT mode. 0=OFF, 1=ON"],
				Bool("DrumsTight", c => c.bTight)),
			G([
					"Hidden/Suddenモード(0:OFF, 1:HIDDEN, 2:SUDDEN, 3:HID/SUD, 4:STEALTH)",
					"Hidden/Sudden mode. 0=OFF, 1=HIDDEN, 2=SUDDEN, 3=HID/SUD, 4=STEALTH "
				],
				Int("DrumsHiddenSudden", 0, 5, c => c.nHidSud.Drums),
				Int("GuitarHiddenSudden", 0, 5, c => c.nHidSud.Guitar),
				Int("BassHiddenSudden", 0, 5, c => c.nHidSud.Bass)),
			G("ドラム判定文字表示位置(0:OnTheLane,1:判定ライン上,2:表示OFF)",
				Enum<EType>("DrumsPosition", 0, 2, c => c.JudgementStringPosition.Drums)),
			G("ギター/ベース判定文字表示位置(0:OnTheLane, 1:レーン横, 2:判定ライン上, 3:表示OFF)",
				Enum<EType>("GuitarPosition", 0, 3, c => c.JudgementStringPosition.Guitar),
				Enum<EType>("BassPosition", 0, 3, c => c.JudgementStringPosition.Bass)),
			G("譜面スクロール速度(0:x0.5, 1:x1.0, 2:x1.5,…,1999:x1000.0)",
				Int("DrumsScrollSpeed", 0, 0x7cf, c => c.nScrollSpeed.Drums),
				Int("GuitarScrollSpeed", 0, 0x7cf, c => c.nScrollSpeed.Guitar),
				Int("BassScrollSpeed", 0, 0x7cf, c => c.nScrollSpeed.Bass)),
			G("演奏速度(5～40)(→x5/20～x40/20)",
				Int("PlaySpeed", CConstants.PLAYSPEED_MIN, CConstants.PLAYSPEED_MAX, c => c.nPlaySpeed)),
			G("Save score when PlaySpeed is not 100% (0:OFF, 1:ON)",
				Bool("SaveScoreIfModifiedPlaySpeed", c => c.bSaveScoreIfModifiedPlaySpeed)),
			G("グラフ表示(0:OFF, 1:ON)",
				Bool("DrumGraph", c => c.bGraph有効.Drums),
				Bool("GuitarGraph", c => c.bGraph有効.Guitar),
				Bool("BassGraph", c => c.bGraph有効.Bass)),
			G("Small Graph (0:OFF, 1:ON)",
				Bool("SmallGraph", c => c.bSmallGraph)),
			G(["ドラムコンボの表示(0:OFF, 1:ON)", "DrumPart Display Combo. 0=OFF, 1=ON"],
				Bool("DrumComboDisp", c => c.bドラムコンボ文字の表示),
				// Legacy key: parsed for backwards compatibility, but no longer written.
				ReadOnly("ComboPosition",
					(c, v) => c.ドラムコンボ文字の表示位置 = (EDrumComboTextDisplayPosition)CConversion.nGetNumberIfInRange(v, 0, 3, (int)c.ドラムコンボ文字の表示位置))),
			G("AUTOゴースト種別 (0:PERFECT, 1:LAST_PLAY, 2:HI_SKILL, 3:HI_SCORE)",
				Enum<EAutoGhostData>("DrumAutoGhost", 0, 3, c => c.eAutoGhost.Drums),
				Enum<EAutoGhostData>("GuitarAutoGhost", 0, 3, c => c.eAutoGhost.Guitar),
				Enum<EAutoGhostData>("BassAutoGhost", 0, 3, c => c.eAutoGhost.Bass)),
			G("ターゲットゴースト種別 (0:NONE, 1:PERFECT, 2:LAST_PLAY, 3:HI_SKILL, 4:HI_SCORE)",
				Enum<ETargetGhostData>("DrumTargetGhost", 0, 4, c => c.eTargetGhost.Drums),
				Enum<ETargetGhostData>("GuitarTargetGhost", 0, 4, c => c.eTargetGhost.Guitar),
				Enum<ETargetGhostData>("BassTargetGhost", 0, 4, c => c.eTargetGhost.Bass)),
			G("譜面仕様変更(0:デフォルト10レーン, 1:XG9レーン, 2:CLASSIC6レーン)",
				Enum<EType>("NumOfLanes", 0, 2, c => c.eNumOfLanes.Drums)),
			G("dkdk仕様変更(0:デフォルト, 1:始動足変更, 2:dkdk1レーン化)",
				Enum<EType>("DkdkType", 0, 2, c => c.eDkdkType.Drums)),
			G("バスをLBDに振り分け(0:OFF, 1:ON)",
				Bool("AssignToLBD", c => c.bAssignToLBD.Drums)),
			G("ドラムパッドRANDOMモード(0:OFF, 1:Mirror, 2:Random, 3:SuperRandom, 4:HyperRandom, 5:MasterRandom, 6:AnotherRandom)",
				Enum<ERandomMode>("DrumsRandomPad", 0, 6, c => c.eRandom.Drums)),
			G("ドラム足RANDOMモード(0:OFF, 1:Mirror, 2:Random, 3:SuperRandom, 4:HyperRandom, 5:MasterRandom, 6:AnotherRandom)",
				Enum<ERandomMode>("DrumsRandomPedal", 0, 6, c => c.eRandomPedal.Drums)),
			G("LP消音機能(0:OFF, 1:ON)",
				Bool("MutingLP", c => c.bMutingLP)),
			G("判定ライン(0～100)",
				IntRound("DrumsJudgeLine", 0, 100, c => c.nJudgeLine.Drums),
				IntRound("GuitarJudgeLine", 0, 100, c => c.nJudgeLine.Guitar),
				IntRound("BassJudgeLine", 0, 100, c => c.nJudgeLine.Bass)),
			G([
					"ネームプレートタイプ",
					"0:タイプA XG2風の表示がされます。 ",
					"1:タイプB XG風の表示がされます。このタイプでは7_NamePlate_XG.png、7_Difficulty_XG.pngが読み込まれます。"
				],
				Enum<EType>("NamePlateType", 0, 3, c => c.eNamePlate)),
			G("動くドラムセット(0:ON, 1:OFF, 2:NONE)",
				Enum<EType>("DrumSetMoves", 0, 2, c => c.eドラムセットを動かす)),
			G("BPMバーの表示(0:表示する, 1:左のみ表示, 2:動くバーを表示しない, 3:表示しない)",
				Enum<EType>("BPMBar", 0, 3, c => c.eBPMbar)),
			G("LivePointの表示(0:OFF, 1:ON)",
				Bool("LivePoint", c => c.bLivePoint)),
			G("スピーカーの表示(0:OFF, 1:ON)",
				Bool("Speaker", c => c.bSpeaker)),
			G("シャッターINSIDE(0～100)",
				Int("DrumsShutterIn", 0, 100, c => c.nShutterInSide.Drums),
				Int("GuitarShutterIn", 0, 100, c => c.nShutterInSide.Guitar),
				Int("BassShutterIn", 0, 100, c => c.nShutterInSide.Bass)),
			G("シャッターOUTSIDE(0～100)",
				Int("DrumsShutterOut", -100, 100, c => c.nShutterOutSide.Drums),
				Int("GuitarShutterOut", -100, 100, c => c.nShutterOutSide.Guitar),
				Int("BassShutterOut", -100, 100, c => c.nShutterOutSide.Bass)),
			G("ボーナス演出の表示(0:表示しない, 1:表示する)",
				Bool("DrumsStageEffect", c => c.DisplayBonusEffects)),
			G("ドラムレーンタイプ(0:A, 1:B, 2:C 3:D )",
				Enum<EType>("DrumsLaneType", 0, 3, c => c.eLaneType.Drums)),
			G("CLASSIC譜面判別",
				Bool("CLASSIC", c => c.bClassicScoreDisplay)),
			G("スキルモード(0:旧仕様, 1:XG仕様)",
				Int("SkillMode", 0, 1, c => c.nSkillMode)),
			G("スキルモードの自動切換え(0:OFF, 1:ON)",
				Bool("SwitchSkillMode", c => c.bSkillModeを自動切換えする)),
			G([
					"ドラム アタックエフェクトタイプ",
					"0:ALL 粉と爆発エフェクトを表示します。",
					"1:ChipOFF チップのエフェクトを消します。",
					"2:EffectOnly 粉を消します。",
					"3:ALLOFF すべて消します。"
				],
				Enum<EType>("DrumsAttackEffect", 0, 3, c => c.eAttackEffect.Drums)),
			G("ギター / ベース アタックエフェクトタイプ (0:OFF, 1:ON)",
				Enum<EType>("GuitarAttackEffect", 0, 1, c => c.eAttackEffect.Guitar),
				Enum<EType>("BassAttackEffect", 0, 1, c => c.eAttackEffect.Bass)),
			G([
					"レーン表示",
					"0:ALL ON レーン背景、小節線を表示します。",
					"1:LANE FF レーン背景を消します。",
					"2:LINE OFF 小節線を消します。",
					"3:ALL OFF すべて消します。"
				],
				Int("DrumsLaneDisp", 0, 4, c => c.nLaneDisp.Drums),
				Int("GuitarLaneDisp", 0, 4, c => c.nLaneDisp.Guitar),
				Int("BassLaneDisp", 0, 4, c => c.nLaneDisp.Bass)),
			G("Display Judgement",
				Bool("DrumsDisplayJudge", c => c.bDisplayJudge.Drums),
				Bool("GuitarDisplayJudge", c => c.bDisplayJudge.Guitar),
				Bool("BassDisplayJudge", c => c.bDisplayJudge.Bass)),
			G("判定ライン表示",
				Bool("DrumsJudgeLineDisp", c => c.bJudgeLineDisp.Drums),
				Bool("GuitarJudgeLineDisp", c => c.bJudgeLineDisp.Guitar),
				Bool("BassJudgeLineDisp", c => c.bJudgeLineDisp.Bass)),
			G("レーンフラッシュ表示",
				Bool("DrumsLaneFlush", c => c.bLaneFlush.Drums),
				Bool("GuitarLaneFlush", c => c.bLaneFlush.Guitar),
				Bool("BassLaneFlush", c => c.bLaneFlush.Bass)),
			G(["ペダル部分のラグ時間調整", "入力が遅い場合、マイナス方向に調節してください。"],
				Int("PedalLagTime", -100, 100, c => c.nPedalLagTime)),
			G(["判定画像のアニメーション方式", "判定画像のコマ数"],
				IntRaw("JudgeFrames", c => c.nJudgeFrames)),
			G("判定画像の1コマのフレーム数",
				IntRaw("JudgeInterval", c => c.nJudgeInterval)),
			G("判定画像の1コマの幅",
				IntRaw("JudgeWidgh", c => c.nJudgeWidgh)),
			G("判定画像の1コマの高さ",
				IntRaw("JudgeHeight", c => c.nJudgeHeight)),
			G("アタックエフェクトのコマ数",
				Int("ExplosionFrames", 0, int.MaxValue, c => c.nExplosionFrames)),
			G("アタックエフェクトの1コマのフレーム数",
				Int("ExplosionInterval", 0, int.MaxValue, c => c.nExplosionInterval)),
			G("アタックエフェクトの1コマの幅",
				Int("ExplosionWidgh", 0, int.MaxValue, c => c.nExplosionWidgh)),
			G("アタックエフェクトの1コマの高さ",
				Int("ExplosionHeight", 0, int.MaxValue, c => c.nExplosionHeight)),
			G("ワイリングエフェクトのコマ数",
				Int("WailingFireFrames", 0, int.MaxValue, c => c.nWailingFireFrames)),
			G("ワイリングエフェクトの1コマのフレーム数",
				Int("WailingFireInterval", 0, int.MaxValue, c => c.nWailingFireInterval)),
			G("ワイリングエフェクトの1コマの幅",
				Int("WailingFireWidgh", 0, int.MaxValue, c => c.nWailingFireWidgh)),
			G("ワイリングエフェクトの1コマの高さ",
				Int("WailingFireHeight", 0, int.MaxValue, c => c.nWailingFireHeight)),
			G("ワイリングエフェクトのX座標",
				IntRaw("WailingFirePosXGuitar", c => c.nWailingFireX.Guitar),
				IntRaw("WailingFirePosXBass", c => c.nWailingFireX.Bass)),
			G("ワイリングエフェクトのY座標(Guitar、Bass共通)",
				IntRaw("WailingFirePosY", c => c.nWailingFireY))
		]),

		new("Log", [
			G("Log出力(0:OFF, 1:ON)",
				Bool("OutputLog", c => c.bOutputLogs)),
			G("曲データ検索に関するLog出力(0:OFF, 1:ON)",
				Bool("TraceSongSearch", c => c.bLogSongSearch)),
			G("画像やサウンドの作成_解放に関するLog出力(0:OFF, 1:ON)",
				Bool("TraceCreatedDisposed", c => c.bLog作成解放ログ出力)),
			G("DTX読み込み詳細に関するLog出力(0:OFF, 1:ON)",
				Bool("TraceDTXDetails", c => c.bLogDTX詳細ログ出力))
		]),

		new("DiscordRichPresence", [
			G("Enable Rich Presence integration (0:OFF, 1:ON)",
				Bool("Enable", c => c.bDiscordRichPresenceEnabled)),
			G("Unique client identifier of the Discord Application to use",
				Str("ApplicationID", c => c.strDiscordRichPresenceApplicationID)),
			G("Unique identifier of the large image to display alongside presences",
				Str("LargeImage", c => c.strDiscordRichPresenceLargeImageKey)),
			G("Unique identifier of the small image to display alongside presences in drum mode",
				Str("SmallImageDrums", c => c.strDiscordRichPresenceSmallImageKeyDrums)),
			G("Unique identifier of the small image to display alongside presences in guitar mode",
				Str("SmallImageGuitar", c => c.strDiscordRichPresenceSmallImageKeyGuitar))
		]),

		new("AutoPlay", tAutoPlayGroups(c => c.bAutoPlay)),
		new("AutoPlayCustom", tAutoPlayGroups(c => c.bAutoPlayCustom)),

		new("HitRange", [
			G(["Perfect～Poor とみなされる範囲[ms]", "Hit ranges for each judgement type (in ± milliseconds)"],
				// Legacy un-prefixed keys: apply to every category. Parsed for backwards compatibility, never written.
				ReadOnly("Perfect", (c, v) => tApplyLegacyHitRange(v, ref c.stDrumHitRanges.nPerfectSizeMs, ref c.stDrumPedalHitRanges.nPerfectSizeMs, ref c.stGuitarHitRanges.nPerfectSizeMs, ref c.stBassHitRanges.nPerfectSizeMs)),
				ReadOnly("Great", (c, v) => tApplyLegacyHitRange(v, ref c.stDrumHitRanges.nGreatSizeMs, ref c.stDrumPedalHitRanges.nGreatSizeMs, ref c.stGuitarHitRanges.nGreatSizeMs, ref c.stBassHitRanges.nGreatSizeMs)),
				ReadOnly("Good", (c, v) => tApplyLegacyHitRange(v, ref c.stDrumHitRanges.nGoodSizeMs, ref c.stDrumPedalHitRanges.nGoodSizeMs, ref c.stGuitarHitRanges.nGoodSizeMs, ref c.stBassHitRanges.nGoodSizeMs)),
				ReadOnly("Poor", (c, v) => tApplyLegacyHitRange(v, ref c.stDrumHitRanges.nPoorSizeMs, ref c.stDrumPedalHitRanges.nPoorSizeMs, ref c.stGuitarHitRanges.nPoorSizeMs, ref c.stBassHitRanges.nPoorSizeMs))),
			G("Drum chips, except pedals",
				Int("DrumPerfect", 0, 0x3e7, c => c.stDrumHitRanges.nPerfectSizeMs),
				Int("DrumGreat", 0, 0x3e7, c => c.stDrumHitRanges.nGreatSizeMs),
				Int("DrumGood", 0, 0x3e7, c => c.stDrumHitRanges.nGoodSizeMs),
				Int("DrumPoor", 0, 0x3e7, c => c.stDrumHitRanges.nPoorSizeMs)),
			G("Drum pedal chips",
				Int("DrumPedalPerfect", 0, 0x3e7, c => c.stDrumPedalHitRanges.nPerfectSizeMs),
				Int("DrumPedalGreat", 0, 0x3e7, c => c.stDrumPedalHitRanges.nGreatSizeMs),
				Int("DrumPedalGood", 0, 0x3e7, c => c.stDrumPedalHitRanges.nGoodSizeMs),
				Int("DrumPedalPoor", 0, 0x3e7, c => c.stDrumPedalHitRanges.nPoorSizeMs)),
			G("Guitar chips",
				Int("GuitarPerfect", 0, 0x3e7, c => c.stGuitarHitRanges.nPerfectSizeMs),
				Int("GuitarGreat", 0, 0x3e7, c => c.stGuitarHitRanges.nGreatSizeMs),
				Int("GuitarGood", 0, 0x3e7, c => c.stGuitarHitRanges.nGoodSizeMs),
				Int("GuitarPoor", 0, 0x3e7, c => c.stGuitarHitRanges.nPoorSizeMs)),
			G("Bass chips",
				Int("BassPerfect", 0, 0x3e7, c => c.stBassHitRanges.nPerfectSizeMs),
				Int("BassGreat", 0, 0x3e7, c => c.stBassHitRanges.nGreatSizeMs),
				Int("BassGood", 0, 0x3e7, c => c.stBassHitRanges.nGoodSizeMs),
				Int("BassPoor", 0, 0x3e7, c => c.stBassHitRanges.nPoorSizeMs))
		]),

		new("GUID", [
			// Joystick device-number -> GUID mappings. The key repeats, once per registered device.
			G(Repeated("JoystickID",
				(c, v) => c.tAcquireJoystickID(v),
				c => c.joystickDict.Select(p => $"{p.Key},{p.Value}")))
		]),

		new("DrumsKeyAssign", [
			G(KeyItem("HH", c => c.KeyAssign.Drums.HH),
				KeyItem("SD", c => c.KeyAssign.Drums.SD),
				KeyItem("BD", c => c.KeyAssign.Drums.BD),
				KeyItem("HT", c => c.KeyAssign.Drums.HT),
				KeyItem("LT", c => c.KeyAssign.Drums.LT),
				KeyItem("FT", c => c.KeyAssign.Drums.FT),
				KeyItem("CY", c => c.KeyAssign.Drums.CY),
				KeyItem("HO", c => c.KeyAssign.Drums.HHO),
				KeyItem("RD", c => c.KeyAssign.Drums.RD),
				KeyItem("LC", c => c.KeyAssign.Drums.LC),
				KeyItem("LP", c => c.KeyAssign.Drums.LP),
				KeyItem("LBD", c => c.KeyAssign.Drums.LBD))
		], headerLines: KeyAssignLegend),

		new("GuitarKeyAssign", [
			G(KeyItem("R", c => c.KeyAssign.Guitar.R),
				KeyItem("G", c => c.KeyAssign.Guitar.G),
				KeyItem("B", c => c.KeyAssign.Guitar.B),
				KeyItem("Y", c => c.KeyAssign.Guitar.Y),
				KeyItem("P", c => c.KeyAssign.Guitar.P),
				KeyItem("Pick", c => c.KeyAssign.Guitar.Pick),
				KeyItem("Wail", c => c.KeyAssign.Guitar.Wail),
				KeyItem("Decide", c => c.KeyAssign.Guitar.Decide),
				KeyItem("Cancel", c => c.KeyAssign.Guitar.Cancel))
		]),

		new("BassKeyAssign", [
			G(KeyItem("R", c => c.KeyAssign.Bass.R),
				KeyItem("G", c => c.KeyAssign.Bass.G),
				KeyItem("B", c => c.KeyAssign.Bass.B),
				KeyItem("Y", c => c.KeyAssign.Bass.Y),
				KeyItem("P", c => c.KeyAssign.Bass.P),
				KeyItem("Pick", c => c.KeyAssign.Bass.Pick),
				KeyItem("Wail", c => c.KeyAssign.Bass.Wail),
				KeyItem("Decide", c => c.KeyAssign.Bass.Decide),
				KeyItem("Cancel", c => c.KeyAssign.Bass.Cancel))
		]),

		new("SystemKeyAssign", [
			G(KeyItem("Capture", c => c.KeyAssign.System.Capture),
				KeyItem("Search", c => c.KeyAssign.System.Search),
				KeyItem("Help", c => c.KeyAssign.Guitar.Help),
				KeyItem("Pause", c => c.KeyAssign.Bass.Help),
				KeyItem("LoopCreate", c => c.KeyAssign.System.LoopCreate),
				KeyItem("LoopDelete", c => c.KeyAssign.System.LoopDelete),
				KeyItem("SkipForward", c => c.KeyAssign.System.SkipForward),
				KeyItem("SkipBackward", c => c.KeyAssign.System.SkipBackward),
				KeyItem("IncreasePlaySpeed", c => c.KeyAssign.System.IncreasePlaySpeed),
				KeyItem("DecreasePlaySpeed", c => c.KeyAssign.System.DecreasePlaySpeed),
				KeyItem("Restart", c => c.KeyAssign.System.Restart))
		])
	];

	// Fast lookup for reading: section name -> (key -> item).
	private static readonly Dictionary<string, Dictionary<string, ConfigItem>> SchemaByKey = BuildSchemaLookup();

	private static Dictionary<string, Dictionary<string, ConfigItem>> BuildSchemaLookup()
	{
		var lookup = new Dictionary<string, Dictionary<string, ConfigItem>>();
		foreach (ConfigSection section in Schema)
		{
			var byKey = new Dictionary<string, ConfigItem>();
			foreach (ConfigGroup group in section.Groups)
			{
				foreach (ConfigItem item in group.Items)
				{
					byKey[item.Key] = item;
				}
			}
			lookup[section.Name] = byKey;
		}
		return lookup;
	}

	#endregion
}
