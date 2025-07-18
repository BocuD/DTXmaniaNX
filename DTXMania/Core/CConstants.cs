using System.Runtime.InteropServices;

namespace DTXMania.Core;

public enum ECYGroup
{
	打ち分ける,
	共通
}
public enum EFTGroup
{
	打ち分ける,
	共通
}
public enum EHHGroup
{
	全部打ち分ける,
	ハイハットのみ打ち分ける,
	左シンバルのみ打ち分ける,
	全部共通
}
public enum EBDGroup		// #27029 2012.1.4 from add
{
	打ち分ける,
	BDとLPで打ち分ける,
	左右ペダルのみ打ち分ける,
	どっちもBD
}
public enum EType
{
	A,
	B,
	C,
	D,
	E
}
public enum ERDPosition
{
	RDRC,
	RCRD
}
public enum EDarkMode
{
	OFF,
	HALF,
	FULL
}
public enum EDamageLevel
{
	Small	= 0,
	Normal	= 1,
	High	= 2
}
public enum EPad			// 演奏用のenum。ここを修正するときは、次に出てくる EKeyConfigPad と EPadFlag もセットで修正すること。
{
	HH		= 0,
	R		= 0,
	SD		= 1,
	G		= 1,
	BD		= 2,
	B		= 2,
	HT		= 3,
	Pick	= 3,
	LT		= 4,
	Wail	= 4,
	FT		= 5,
	Help	= 5,
	CY		= 6,
	Decide	= 6,
	HHO		= 7,
	Y       = 7,
	RD		= 8,
	LC		= 9,
	P       = 9,
	//HP		= 10,	// #27029 2012.1.4 from
	LP      = 10,
	LBD     = 11,
	Cancel  = 12,   // fisyher: New Cancel/Go Back Key
	MAX,			// 門番用として定義
	UNKNOWN = 99
}
public enum EKeyConfigPad		// #24609 キーコンフィグで使うenum。capture要素あり。
{
	HH		= EPad.HH,
	R		= EPad.R,
	SD		= EPad.SD,
	G		= EPad.G,
	BD		= EPad.BD,
	B		= EPad.B,
	HT		= EPad.HT,
	Pick	= EPad.Pick,
	LT		= EPad.LT,
	Wail	= EPad.Wail,
	FT		= EPad.FT,
	Help	= EPad.Help,
	CY		= EPad.CY,
	Decide	= EPad.Decide,
	HHO		= EPad.HHO,
	Y       = EPad.Y,
	RD		= EPad.RD,
	P       = EPad.P,
	LC		= EPad.LC,
	//HP		= EPad.HP,		// #27029 2012.1.4 from
	LP      = EPad.LP,
	LBD     = EPad.LBD,
	Cancel  = EPad.Cancel,     // fisyher: New Cancel/Go Back Key
	//Non-Pad System buttons
	Capture,
	Search,
	LoopCreate,
	LoopDelete,
	SkipForward,
	SkipBackward, // = Rewind
	IncreasePlaySpeed,
	DecreasePlaySpeed,
	Restart,
	MAX,          // Gatekeeper
	UNKNOWN = EPad.UNKNOWN
}
[Flags]
public enum EPadFlag		// #24063 2011.1.16 yyagi コマンド入力用 パッド入力のフラグ化
{
	None	= 0,
	HH		= 1,
	R		= 1,
	SD		= 2,
	G		= 2,
	B		= 4,
	BD		= 4,
	HT		= 8,
	Pick	= 8,
	LT		= 16,
	Wail	= 16,
	FT		= 32,
	Help	= 32,
	CY		= 64,
	Decide	= 128,
	HHO		= 128,
	RD		= 256,
	Y       = 256,
	LC		= 512,
	P       = 512,
	LP      = 1024,
	LBD     = 2048,
	Cancel  = 4096,
	UNKNOWN = 8192
}
public enum ERandomMode
{
	OFF,
	MIRROR,
	RANDOM,
	SUPERRANDOM,
	HYPERRANDOM,
	MASTERRANDOM,
	ANOTHERRANDOM
}
public enum EInstrumentPart		// ここを修正するときは、セットで次の EKeyConfigPart も修正すること。
{
	DRUMS	= 0,
	GUITAR	= 1,
	BASS	= 2,
	//SYSTEM  = 3,
	UNKNOWN	= 99
}
public enum EKeyConfigPart	// : EInstrumentPart
{
	DRUMS	= EInstrumentPart.DRUMS,
	GUITAR	= EInstrumentPart.GUITAR,
	BASS	= EInstrumentPart.BASS,
	SYSTEM,
	UNKNOWN	= EInstrumentPart.UNKNOWN
}

public enum EPlaybackPriority
{
	ChipOverPadPriority,
	PadOverChipPriority
}
internal enum EInputDevice
{
	Keyboard	= 0,
	MIDI入力		= 1,
	Joypad		= 2,
	Mouse		= 3,
	Unknown		= -1
}
public enum EJudgement
{
	Perfect	= 0,
	Great	= 1,
	Good	= 2,
	Poor	= 3,
	Miss	= 4,
	Bad		= 5,
	Auto
}



internal enum E判定文字表示位置
{
	OnTheLane,
	判定ライン上または横,
	表示OFF
}
internal enum EAVIType
{
	Unknown,
	AVI,
	AVIPAN
}
internal enum EBGAType
{
	Unknown,
	BMP,
	BMPTEX,
	BGA,
	BGAPAN
}
internal enum EFIFOMode
{
	FadeIn,
	FadeOut
}
internal enum EDrumComboTextDisplayPosition
{
	LEFT,
	CENTER,
	RIGHT,
	OFF
}
public enum ELane
{
	LC = 0,
	HH,
	SD,
	BD,
	HT,
	LT,
	FT,
	CY,
	LP,
	RD,		// 将来の独立レーン化/独立AUTO設定を見越して追加
	LBD,
	Guitar,	// AUTOレーン判定を容易にするため、便宜上定義しておく(未使用)
	Bass,	// (未使用)
	GtR,
	GtG,
	GtB,
	GtY,
	GtP,
	GtPick,
	GtW,
	BsR,
	BsG,
	BsB,
	BsY,
	BsP,
	BsPick,
	BsW,
	MAX,	// 要素数取得のための定義 ("BGM"は使わない前提で)
	BGM
}
internal enum EOutputLog
{
	OFF,
	ONNormal,
	ONVerbose
}
internal enum EPerfScreenReturnValue
{
	Continue,
	Interruption,
	Restart,
	StageFailure,
	StageClear
}
internal enum ESongLoadingScreenReturnValue
{
	Continue = 0,
	LoadingComplete,
	LoadingStopped
}
/// <summary>
/// 入力ラグ表示タイプ
/// </summary>
internal enum EShowLagType
{
	OFF,			// 全く表示しない
	ON,				// 判定に依らず全て表示する
	GREAT_POOR		// GREAT-MISSの時のみ表示する(PERFECT時は表示しない)
}

internal enum EShowPlaySpeed
{
	OFF,
	ON,
	IF_CHANGED_IN_GAME
}

/// <summary>
/// 使用するAUTOゴーストデータの種類 (#35411 chnmr0)
/// </summary>
public enum EAutoGhostData
{
	PERFECT = 0, // 従来のAUTO
	LAST_PLAY = 1, // (.score.ini) の LastPlay ゴースト
	HI_SKILL = 2, // (.score.ini) の HiSkill ゴースト
	HI_SCORE = 3, // (.score.ini) の HiScore ゴースト
	ONLINE = 4 // オンラインゴースト (DTXMOS からプラグインが取得、本体のみでは指定しても無効)
}

/// <summary>
/// 使用するターゲットゴーストデータの種類 (#35411 chnmr0)
/// ここでNONE以外を指定してかつ、ゴーストが利用可能な場合グラフの目標値に描画される
/// NONE の場合従来の動作
/// </summary>
public enum ETargetGhostData
{
	NONE = 0,
	PERFECT = 1,
	LAST_PLAY = 2, // (.score.ini) の LastPlay ゴースト
	HI_SKILL = 3, // (.score.ini) の HiSkill ゴースト
	HI_SCORE = 4, // (.score.ini) の HiScore ゴースト
	ONLINE = 5 // オンラインゴースト (DTXMOS からプラグインが取得、本体のみでは指定しても無効)
}
/// <summary>
/// Drum/Guitar/Bass の値を扱う汎用の構造体。
/// </summary>
/// <typeparam name="T">値の型。</typeparam>
[Serializable]
[StructLayout( LayoutKind.Sequential )]
public struct STDGBVALUE<T>			// indexはE楽器パートと一致させること
{
	public T Drums;
	public T Guitar;
	public T Bass;
	public T Unknown;
	public T this[ int index ]
	{
		get
		{
			switch( index )
			{
				case (int) EInstrumentPart.DRUMS:
					return Drums;

				case (int) EInstrumentPart.GUITAR:
					return Guitar;

				case (int) EInstrumentPart.BASS:
					return Bass;

				case (int) EInstrumentPart.UNKNOWN:
					return Unknown;
			}
			throw new IndexOutOfRangeException();
		}
		set
		{
			switch( index )
			{
				case (int) EInstrumentPart.DRUMS:
					Drums = value;
					return;

				case (int) EInstrumentPart.GUITAR:
					Guitar = value;
					return;

				case (int) EInstrumentPart.BASS:
					Bass = value;
					return;

				case (int) EInstrumentPart.UNKNOWN:
					Unknown = value;
					return;
			}
			throw new IndexOutOfRangeException();
		}
	}
}

/// <summary>
/// レーンの値を扱う汎用の構造体。列挙型"Eドラムレーン"に準拠。
/// </summary>
/// <typeparam name="T">値の型。</typeparam>
[StructLayout( LayoutKind.Sequential )]
public struct STLANEVALUE<T>
{
	public T LC;
	public T HH;
	public T SD;
	public T BD;
	public T HT;
	public T LT;
	public T FT;
	public T CY;
	public T LP;
	public T RD;
	public T LBD;
	public T Guitar;
	public T Bass;
	public T GtR;
	public T GtG;
	public T GtB;
	public T GtY;
	public T GtP;
	public T GtPick;
	public T GtW;
	public T BsR;
	public T BsG;
	public T BsB;
	public T BsY;
	public T BsP;
	public T BsPick;
	public T BsW;
	public T BGM;

	public T this[ int index ]
	{
		get
		{
			switch ( index )
			{
				case (int) ELane.LC:
					return LC;
				case (int) ELane.HH:
					return HH;
				case (int) ELane.SD:
					return SD;
				case (int) ELane.BD:
					return BD;
				case (int) ELane.HT:
					return HT;
				case (int) ELane.LT:
					return LT;
				case (int) ELane.FT:
					return FT;
				case (int) ELane.CY:
					return CY;
				case (int) ELane.LP:
					return LP;
				case (int) ELane.RD:
					return RD;
				case (int) ELane.LBD:
					return LBD;
				case (int) ELane.Guitar:
					return Guitar;
				case (int) ELane.Bass:
					return Bass;
				case (int) ELane.GtR:
					return GtR;
				case (int) ELane.GtG:
					return GtG;
				case (int) ELane.GtB:
					return GtB;
				case (int) ELane.GtY:
					return GtY;
				case (int) ELane.GtP:
					return GtP;
				case (int) ELane.GtPick:
					return GtPick;
				case (int) ELane.GtW:
					return GtW;
				case (int) ELane.BsR:
					return BsR;
				case (int) ELane.BsG:
					return BsG;
				case (int) ELane.BsB:
					return BsB;
				case (int) ELane.BsY:
					return BsY;
				case (int) ELane.BsP:
					return BsP;
				case (int) ELane.BsPick:
					return BsPick;
				case (int) ELane.BsW:
					return BsW;
			}
			throw new IndexOutOfRangeException();
		}
		set
		{
			switch ( index )
			{
				case (int) ELane.LC:
					LC = value;
					return;
				case (int) ELane.HH:
					HH = value;
					return;
				case (int) ELane.SD:
					SD = value;
					return;
				case (int) ELane.BD:
					BD = value;
					return;
				case (int) ELane.HT:
					HT = value;
					return;
				case (int) ELane.LT:
					LT = value;
					return;
				case (int) ELane.FT:
					FT = value;
					return;
				case (int) ELane.CY:
					CY = value;
					return;
				case (int) ELane.LP:
					LP = value;
					return;
				case (int) ELane.RD:
					RD = value;
					return;
				case (int) ELane.LBD:
					LBD = value;
					return;
				case (int) ELane.Guitar:
					Guitar = value;
					return;
				case (int) ELane.Bass:
					Bass = value;
					return;
				case (int) ELane.GtR:
					GtR = value;
					return;
				case (int) ELane.GtG:
					GtG = value;
					return;
				case (int) ELane.GtB:
					GtB = value;
					return;
				case (int) ELane.GtY:
					GtY = value;
					return;
				case (int) ELane.GtP:
					GtP = value;
					return;
				case (int) ELane.GtPick:
					GtPick = value;
					return;
				case (int) ELane.GtW:
					GtW = value;
					return;
				case (int) ELane.BsR:
					BsR = value;
					return;
				case (int) ELane.BsG:
					BsG = value;
					return;
				case (int) ELane.BsB:
					BsB = value;
					return;
				case (int) ELane.BsY:
					BsY = value;
					return;
				case (int) ELane.BsP:
					BsP = value;
					return;
				case (int) ELane.BsPick:
					BsPick = value;
					return;
				case (int) ELane.BsW:
					BsW = value;
					return;
			}
			throw new IndexOutOfRangeException();
		}
	}
}


[StructLayout( LayoutKind.Sequential )]
public struct STAUTOPLAY								// Eレーンとindexを一致させること
{
	public bool LC;			// 0
	public bool HH;			// 1
	public bool SD;			// 2
	public bool BD;			// 3
	public bool HT;			// 4
	public bool LT;			// 5
	public bool FT;			// 6
	public bool CY;			// 7
	public bool LP;
	public bool RD;			// 8
	public bool LBD;
	public bool Guitar;		// 9	(not used)
	public bool Bass;		// 10	(not used)
	public bool GtR;		// 11
	public bool GtG;		// 12
	public bool GtB;		// 13
	public bool GtY;
	public bool GtP;
	public bool GtPick;		// 14
	public bool GtW;		// 15
	public bool BsR;		// 16
	public bool BsG;		// 17
	public bool BsB;		// 18
	public bool BsY;
	public bool BsP;
	public bool BsPick;		// 19
	public bool BsW;		// 20
	public bool this[ int index ]
	{
		get
		{
			switch ( index )
			{
				case (int) ELane.LC:
					return LC;
				case (int) ELane.HH:
					return HH;
				case (int) ELane.SD:
					return SD;
				case (int) ELane.BD:
					return BD;
				case (int) ELane.HT:
					return HT;
				case (int) ELane.LT:
					return LT;
				case (int) ELane.FT:
					return FT;
				case (int) ELane.CY:
					return CY;
				case (int) ELane.LP:
					return LP;
				case (int) ELane.RD:
					return RD;
				case (int) ELane.LBD:
					return LBD;
				case (int)ELane.Guitar:
					if (!GtR) return false;
					if (!GtG) return false;
					if (!GtB) return false;
					if (!GtY) return false;
					if (!GtP) return false;
					if (!GtPick) return false;
					if (!GtW) return false;
					return true;
				case (int)ELane.Bass:
					if (!BsR) return false;
					if (!BsG) return false;
					if (!BsB) return false;
					if (!BsY) return false;
					if (!BsP) return false;
					if (!BsPick) return false;
					if (!BsW) return false;
					return true;
				case (int) ELane.GtR:
					return GtR;
				case (int) ELane.GtG:
					return GtG;
				case (int) ELane.GtB:
					return GtB;
				case (int) ELane.GtY:
					return GtY;
				case (int) ELane.GtP:
					return GtP;
				case (int) ELane.GtPick:
					return GtPick;
				case (int) ELane.GtW:
					return GtW;
				case (int) ELane.BsR:
					return BsR;
				case (int) ELane.BsG:
					return BsG;
				case (int) ELane.BsB:
					return BsB;
				case (int) ELane.BsY:
					return BsY;
				case (int) ELane.BsP:
					return BsP;
				case (int) ELane.BsPick:
					return BsPick;
				case (int) ELane.BsW:
					return BsW;
			}
			throw new IndexOutOfRangeException();
		}
		set
		{
			switch ( index )
			{
				case (int) ELane.LC:
					LC = value;
					return;
				case (int) ELane.HH:
					HH = value;
					return;
				case (int) ELane.SD:
					SD = value;
					return;
				case (int) ELane.BD:
					BD = value;
					return;
				case (int) ELane.HT:
					HT = value;
					return;
				case (int) ELane.LT:
					LT = value;
					return;
				case (int) ELane.FT:
					FT = value;
					return;
				case (int) ELane.CY:
					CY = value;
					return;
				case (int) ELane.LP:
					LP = value;
					return;
				case (int) ELane.RD:
					RD = value;
					return;
				case (int) ELane.LBD:
					LBD = value;
					return;
				case (int)ELane.Guitar:
					GtR = GtG = GtB = GtY = GtP = GtPick = GtW = value;
					//this.GtR = this.GtG = this.GtB = this.GtPick = this.GtW = value;
					return;
				case (int)ELane.Bass:
					BsR = BsG = BsB = BsY = BsP = BsPick = BsW = value;
					//this.BsR = this.BsG = this.BsB = this.BsPick = this.BsW = value;
					return;
				case (int) ELane.GtR:
					GtR = value;
					return;
				case (int) ELane.GtG:
					GtG = value;
					return;
				case (int) ELane.GtB:
					GtB = value;
					return;
				case (int) ELane.GtY:
					GtY = value;
					return;
				case (int) ELane.GtP:
					GtP = value;
					return;
				case (int) ELane.GtPick:
					GtPick = value;
					return;
				case (int) ELane.GtW:
					GtW = value;
					return;
				case (int) ELane.BsR:
					BsR = value;
					return;
				case (int) ELane.BsG:
					BsG = value;
					return;
				case (int) ELane.BsB:
					BsB = value;
					return;
				case (int) ELane.BsY:
					BsY = value;
					return;
				case (int) ELane.BsP:
					BsP = value;
					return;
				case (int) ELane.BsPick:
					BsPick = value;
					return;
				case (int) ELane.BsW:
					BsW = value;
					return;
			}
			throw new IndexOutOfRangeException();
		}
	}
}


internal class CConstants
{
	public const int PLAYSPEED_MIN = 5;
	public const int PLAYSPEED_MAX = 40;
}