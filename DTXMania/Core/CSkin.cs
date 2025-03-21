﻿using System.Diagnostics;
using System.Text;
using FDK;

namespace DTXMania.Core;
// グローバル定数

public enum ESystemSound
{
	BGMオプション画面 = 0,
	BGMコンフィグ画面,
	BGM起動画面,
	BGM選曲画面,
	BGM結果画面,
	SOUNDステージ失敗音,
	SOUNDカーソル移動音,
	SOUNDゲーム開始音,
	SOUNDゲーム終了音,
	SOUNDステージクリア音,
	SOUNDタイトル音,
	SOUNDフルコンボ音,
	SOUND歓声音,
	SOUND曲読込開始音,
	SOUND決定音,
	SOUND取消音,
	SOUND変更音,
	SOUND曲決定,
	SOUNDエクセレント音,
	SOUND新記録音,
	SOUNDSELECTMUSIC,
	SOUNDNOVICE,
	SOUNDREGULAR,
	SOUNDEXPERT,
	SOUNDMASTER,
	SOUNDBASIC,
	SOUNDADVANCED,
	SOUNDEXTREME,
	Count				// システムサウンド総数の計算用
}

internal class CSkin : IDisposable
{
	// クラス

	public class CSystemSound : IDisposable
	{
		// static フィールド

		public static CSystemSound rLastPlayedExclusiveSystemSound;

		// フィールド、プロパティ

		public bool bCompact対象;
		public bool bループ;
		public bool bReadNotTried;
		public bool b読み込み成功;
		public bool bExclusive;
		public string strFilename = "";
		public bool b再生中
		{
			get
			{
				if( rSound[ 1 - n次に鳴るサウンド番号 ] == null )
					return false;

				return rSound[ 1 - n次に鳴るサウンド番号 ].b再生中;
			}
		}
		public int n位置_現在のサウンド
		{
			get
			{
				CSound sound = rSound[ 1 - n次に鳴るサウンド番号 ];
				if( sound == null )
					return 0;

				return sound.nPosition;
			}
			set
			{
				CSound sound = rSound[ 1 - n次に鳴るサウンド番号 ];
				if( sound != null )
					sound.nPosition = value;
			}
		}
		public int n位置_次に鳴るサウンド
		{
			get
			{
				CSound sound = rSound[ n次に鳴るサウンド番号 ];
				if( sound == null )
					return 0;

				return sound.nPosition;
			}
			set
			{
				CSound sound = rSound[ n次に鳴るサウンド番号 ];
				if( sound != null )
					sound.nPosition = value;
			}
		}
		public int n音量_現在のサウンド
		{
			get
			{
				CSound sound = rSound[ 1 - n次に鳴るサウンド番号 ];
				if( sound == null )
					return 0;

				return sound.nVolume;
			}
			set
			{
				CSound sound = rSound[ 1 - n次に鳴るサウンド番号 ];
				if( sound != null )
					sound.nVolume = value;
			}
		}
		public int n音量_次に鳴るサウンド
		{
			get
			{
				CSound sound = rSound[ n次に鳴るサウンド番号 ];
				if( sound == null )
				{
					return 0;
				}
				return sound.nVolume;
			}
			set
			{
				CSound sound = rSound[ n次に鳴るサウンド番号 ];
				if( sound != null )
				{
					sound.nVolume = value;
				}
			}
		}
		public int nLength_CurrentSound
		{
			get
			{
				CSound sound = rSound[ 1 - n次に鳴るサウンド番号 ];
				if( sound == null )
				{
					return 0;
				}
				return sound.nTotalPlayTimeMs;
			}
		}
		public int nLength_NextSound
		{
			get
			{
				CSound sound = rSound[ n次に鳴るサウンド番号 ];
				if( sound == null )
				{
					return 0;
				}
				return sound.nTotalPlayTimeMs;
			}
		}


		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="strファイル名"></param>
		/// <param name="bループ"></param>
		/// <param name="b排他"></param>
		/// <param name="bCompact対象"></param>
		public CSystemSound(string strファイル名, bool bループ, bool b排他, bool bCompact対象)
		{
			strFilename = strファイル名;
			this.bループ = bループ;
			bExclusive = b排他;
			this.bCompact対象 = bCompact対象;
			bReadNotTried = true;
		}
		public CSystemSound()
		{
			bReadNotTried = true;
		}
			

		// メソッド

		public void tRead()
		{
			bReadNotTried = false;
			b読み込み成功 = false;
			if( string.IsNullOrEmpty( strFilename ) )
				throw new InvalidOperationException( "ファイル名が無効です。" );

			if( !File.Exists( Path( strFilename ) ) )
			{
				throw new FileNotFoundException( strFilename );
			}
////				for( int i = 0; i < 2; i++ )		// #27790 2012.3.10 yyagi 2回読み出しを、1回読みだし＋1回メモリコピーに変更
////				{
//                    try
//                    {
//                        this.rSound[ 0 ] = CDTXMania.SoundManager.tGenerateSound( CSkin.Path( this.strFilename ) );
//                    }
//                    catch
//                    {
//                        this.rSound[ 0 ] = null;
//                        throw;
//                    }
//                    if ( this.rSound[ 0 ] == null )	// #28243 2012.5.3 yyagi "this.rSound[ 0 ].bストリーム再生する"時もCloneするようにし、rSound[1]がnullにならないよう修正→rSound[1]の再生正常化
//                    {
//                        this.rSound[ 1 ] = null;
//                    }
//                    else
//                    {
//                        this.rSound[ 1 ] = ( CSound ) this.rSound[ 0 ].Clone();	// #27790 2012.3.10 yyagi add: to accelerate loading chip sounds
//                        CDTXMania.SoundManager.tサウンドを登録する( this.rSound[ 1 ] );	// #28243 2012.5.3 yyagi add (登録漏れによりストリーム再生処理が発生していなかった)
//                    }

////				}

			for ( int i = 0; i < 2; i++ )		// 一旦Cloneを止めてASIO対応に専念
			{
				try
				{
					rSound[ i ] = CDTXMania.SoundManager.tGenerateSound( Path( strFilename ) );
				}
				catch
				{
					rSound[ i ] = null;
					throw;
				}
			}
			b読み込み成功 = true;
		}
		public void tPlay()
		{
			tPlay(100);
		}
		public void tPlay( int nVolume )
		{
			if ( bReadNotTried )
			{
				try
				{
					tRead();
				}
				catch
				{
					bReadNotTried = false;
				}
			}
			if( bExclusive )
			{
				if( rLastPlayedExclusiveSystemSound != null )
					rLastPlayedExclusiveSystemSound.t停止する();

				rLastPlayedExclusiveSystemSound = this;
			}
			CSound sound = rSound[ n次に鳴るサウンド番号 ];
			if( sound != null )
			{
				sound.nVolume = nVolume;
				sound.tStartPlaying(bループ);
			}
			n次に鳴るサウンド番号 = 1 - n次に鳴るサウンド番号;
		}
		public void t停止する()
		{
			if( rSound[ 0 ] != null )
				rSound[ 0 ].tStopPlayback();

			if( rSound[ 1 ] != null )
				rSound[ 1 ].tStopPlayback();

			if( rLastPlayedExclusiveSystemSound == this )
				rLastPlayedExclusiveSystemSound = null;
		}

		public void tRemoveMixer()
		{
			if ( CDTXMania.SoundManager.GetCurrentSoundDeviceType() != "DirectSound" )
			{
				for ( int i = 0; i < 2; i++ )
				{
					if ( rSound[ i ] != null )
					{
						CDTXMania.SoundManager.RemoveMixer( rSound[ i ] );
					}
				}
			}
		}

		#region [ IDisposable 実装 ]
		//-----------------
		public void Dispose()
		{
			if( !bDisposed済み )
			{
				for( int i = 0; i < 2; i++ )
				{
					if( rSound[ i ] != null )
					{
						CDTXMania.SoundManager.tDiscard( rSound[ i ] );
						rSound[ i ] = null;
					}
				}
				b読み込み成功 = false;
				bDisposed済み = true;
			}
		}
		//-----------------
		#endregion

		#region [ private ]
		//-----------------
		private bool bDisposed済み;
		private int n次に鳴るサウンド番号;
		private CSound[] rSound = new CSound[ 2 ];
		//-----------------
		#endregion
	}

	
	// プロパティ

	public CSystemSound bgmオプション画面 = null;
	public CSystemSound bgmコンフィグ画面 = null;
	public CSystemSound bgm起動画面 = null;
	public CSystemSound bgm選曲画面 = null;
	public CSystemSound bgm結果画面 = null;
	public CSystemSound soundSTAGEFAILED音 = null;
	public CSystemSound soundCursorMovement = null;
	public CSystemSound soundGameStart = null;  // soundゲーム開始音
	public CSystemSound soundGameEnd = null;
	public CSystemSound soundStageClear = null;
	public CSystemSound soundTitle = null;  // soundタイトル音
	public CSystemSound soundFullCombo = null;
	public CSystemSound soundAudience = null;  // sound歓声音
	public CSystemSound soundNowLoading = null;
	public CSystemSound soundDecide = null;  // sound決定音
	public CSystemSound soundCancel = null;  // sound取消音
	public CSystemSound soundChange = null;  // sound変更音
	public CSystemSound soundDecideSong = null;  // sound曲決定
	public CSystemSound soundExcellent = null;
	public CSystemSound soundNewRecord = null;  // sound新記録音
	public CSystemSound soundSelectMusic = null;
	public CSystemSound soundNovice = null;
	public CSystemSound soundRegular = null;
	public CSystemSound soundExpert = null;
	public CSystemSound soundMaster = null;
	public CSystemSound soundBasic = null;
	public CSystemSound soundAdvanced = null;
	public CSystemSound soundExtreme = null;
	public CSystemSound soundMetronome = null;
	public readonly int nシステムサウンド数 = (int)ESystemSound.Count;
	public CSystemSound this[ ESystemSound sound ]
	{
		get
		{
			switch( sound )
			{
				case ESystemSound.SOUNDカーソル移動音:
					return soundCursorMovement;

				case ESystemSound.SOUND決定音:
					return soundDecide;

				case ESystemSound.SOUND変更音:
					return soundChange;

				case ESystemSound.SOUND取消音:
					return soundCancel;

				case ESystemSound.SOUND歓声音:
					return soundAudience;

				case ESystemSound.SOUNDステージ失敗音:
					return soundSTAGEFAILED音;

				case ESystemSound.SOUNDゲーム開始音:
					return soundGameStart;

				case ESystemSound.SOUNDゲーム終了音:
					return soundGameEnd;

				case ESystemSound.SOUNDステージクリア音:
					return soundStageClear;

				case ESystemSound.SOUNDフルコンボ音:
					return soundFullCombo;

				case ESystemSound.SOUNDエクセレント音:
					return soundExcellent;

				case ESystemSound.SOUND新記録音:
					return soundNewRecord;

				case ESystemSound.SOUND曲読込開始音:
					return soundNowLoading;

				case ESystemSound.SOUNDタイトル音:
					return soundTitle;

				case ESystemSound.SOUND曲決定:
					return soundDecideSong;

				case ESystemSound.SOUNDNOVICE:
					return soundNovice;

				case ESystemSound.SOUNDREGULAR:
					return soundRegular;

				case ESystemSound.SOUNDEXPERT:
					return soundExpert;

				case ESystemSound.SOUNDMASTER:
					return soundMaster;

				case ESystemSound.SOUNDBASIC:
					return soundBasic;

				case ESystemSound.SOUNDADVANCED:
					return soundAdvanced;

				case ESystemSound.SOUNDEXTREME:
					return soundExtreme;

				case ESystemSound.SOUNDSELECTMUSIC:
					return soundSelectMusic;

				case ESystemSound.BGM起動画面:
					return bgm起動画面;

				case ESystemSound.BGMオプション画面:
					return bgmオプション画面;

				case ESystemSound.BGMコンフィグ画面:
					return bgmコンフィグ画面;

				case ESystemSound.BGM選曲画面:
					return bgm選曲画面;

				case ESystemSound.BGM結果画面:
					return bgm結果画面;
			}
			throw new IndexOutOfRangeException();
		}
	}
	public CSystemSound this[ int index ]
	{
		get
		{
			switch( index )
			{
				case 0:
					return soundCursorMovement;

				case 1:
					return soundDecide;

				case 2:
					return soundChange;

				case 3:
					return soundCancel;

				case 4:
					return soundAudience;

				case 5:
					return soundSTAGEFAILED音;

				case 6:
					return soundGameStart;

				case 7:
					return soundGameEnd;

				case 8:
					return soundFullCombo;

				case 9:
					return soundExcellent;

				case 10:
					return soundNewRecord;

				case 11:
					return soundNowLoading;

				case 12:
					return soundTitle;

				case 13:
					return soundDecideSong;

				case 14:
					return bgm起動画面;

				case 15:
					return bgmオプション画面;

				case 16:
					return bgmコンフィグ画面;

				case 17:
					return bgm選曲画面;

				case 18:
					return bgm結果画面;

				case 19:
					return soundStageClear;

				case 20:
					return soundNovice;

				case 21:
					return soundRegular;

				case 22:
					return soundExpert;

				case 23:
					return soundMaster;

				case 24:
					return soundSelectMusic;
                    
				case 25:
					return soundBasic;

				case 26:
					return soundAdvanced;

				case 27:
					return soundExtreme;
			}
			throw new IndexOutOfRangeException();
		}
	}


	// スキンの切り替えについて___
	//
	// _スキンの種類は大きく分けて2種類。Systemスキンとboxdefスキン。
	// 　前者はSystem/フォルダにユーザーが自らインストールしておくスキン。
	// 　後者はbox.defで指定する、曲データ制作者が提示するスキン。
	//
	// _Config画面で、2種のスキンを区別無く常時使用するよう設定することができる。
	// _box.defの#SKINPATH 設定により、boxdefスキンを一時的に使用するよう設定する。
	// 　(box.defの効果の及ばない他のmuxic boxでは、当該boxdefスキンの有効性が無くなる)
	//
	// これを実現するために___
	// _Systemスキンの設定情報と、boxdefスキンの設定情報は、分離して持つ。
	// 　(strSystem～～ と、strBoxDef～～～)
	// _Config画面からは前者のみ書き換えできるようにし、
	// 　選曲画面からは後者のみ書き換えできるようにする。(SetCurrent...())
	// _読み出しは両者から行えるようにすると共に
	// 　選曲画面用に二種の情報を区別しない読み出し方法も提供する(GetCurrent...)

	private object lockBoxDefSkin;
	public static bool bUseBoxDefSkin = true;						// box.defからのスキン変更を許容するか否か

	public string strSystemSkinRoot = null;
	public string[] strSystemSkinSubfolders = null;		// List<string>だとignoreCaseな検索が面倒なので、配列に逃げる :-)
	private string[] _strBoxDefSkinSubfolders = null;
	public string[] strBoxDefSkinSubfolders
	{
		get
		{
			lock ( lockBoxDefSkin )
			{
				return _strBoxDefSkinSubfolders;
			}
		}
		set
		{
			lock ( lockBoxDefSkin )
			{
				_strBoxDefSkinSubfolders = value;
			}
		}
	}			// 別スレッドからも書き込みアクセスされるため、スレッドセーフなアクセス法を提供

	private static string strSystemSkinSubfolderFullName;			// Config画面で設定されたスキン
	private static string strBoxDefSkinSubfolderFullName = "";		// box.defで指定されているスキン

	/// <summary>
	/// スキンパス名をフルパスで取得する
	/// </summary>
	/// <param name="bFromUserConfig">ユーザー設定用ならtrue, box.defからの設定ならfalse</param>
	/// <returns></returns>
	public string GetCurrentSkinSubfolderFullName( bool bFromUserConfig )
	{
		if ( !bUseBoxDefSkin || bFromUserConfig == true || strBoxDefSkinSubfolderFullName == "" )
		{
			return strSystemSkinSubfolderFullName;
		}
		else
		{
			return strBoxDefSkinSubfolderFullName;
		}
	}
	/// <summary>
	/// スキンパス名をフルパスで設定する
	/// </summary>
	/// <param name="value">スキンパス名</param>
	/// <param name="bFromUserConfig">ユーザー設定用ならtrue, box.defからの設定ならfalse</param>
	public void SetCurrentSkinSubfolderFullName( string value, bool bFromUserConfig )
	{
		if ( bFromUserConfig )
		{
			strSystemSkinSubfolderFullName = value;
		}
		else
		{
			strBoxDefSkinSubfolderFullName = value;
		}
	}


	// コンストラクタ
	public CSkin( string _strSkinSubfolderFullName, bool _bUseBoxDefSkin )
	{
		lockBoxDefSkin = new object();
		strSystemSkinSubfolderFullName = _strSkinSubfolderFullName;
		bUseBoxDefSkin = _bUseBoxDefSkin;
		InitializeSkinPathRoot();
		ReloadSkinPaths();
		PrepareReloadSkin();
	}
	public CSkin()
	{
		lockBoxDefSkin = new object();
		InitializeSkinPathRoot();
		bUseBoxDefSkin = true;
		ReloadSkinPaths();
		PrepareReloadSkin();
	}
	private string InitializeSkinPathRoot()
	{
		strSystemSkinRoot = System.IO.Path.Combine( CDTXMania.executableDirectory, "System" + System.IO.Path.DirectorySeparatorChar );
		return strSystemSkinRoot;
	}

	/// <summary>
	/// Skin(Sounds)を再読込する準備をする(再生停止,Dispose,ファイル名再設定)。
	/// あらかじめstrSkinSubfolderを適切に設定しておくこと。
	/// その後、ReloadSkinPaths()を実行し、strSkinSubfolderの正当性を確認した上で、本メソッドを呼び出すこと。
	/// 本メソッド呼び出し後に、ReloadSkin()を実行することで、システムサウンドを読み込み直す。
	/// ReloadSkin()の内容は本メソッド内に含めないこと。起動時はReloadSkin()相当の処理をCEnumSongsで行っているため。
	/// </summary>
	public void PrepareReloadSkin()
	{
		Trace.TraceInformation( "SkinPath設定: {0}",
			( strBoxDefSkinSubfolderFullName == "" ) ?
				strSystemSkinSubfolderFullName :
				strBoxDefSkinSubfolderFullName
		);

		for ( int i = 0; i < nシステムサウンド数; i++ )
		{
			if ( this[ i ] != null && this[i].b読み込み成功 )
			{
				this[ i ].t停止する();
				this[ i ].Dispose();
			}
		}
		soundCursorMovement	= new CSystemSound( @"Sounds\Move.ogg",			false, false, false );
		soundDecide			= new CSystemSound( @"Sounds\Decide.ogg",			false, false, false );
		soundChange			= new CSystemSound( @"Sounds\Change.ogg",			false, false, false );
		soundCancel			= new CSystemSound( @"Sounds\Cancel.ogg",			false, false, true  );
		soundAudience			= new CSystemSound( @"Sounds\Audience.ogg",		false, false,  true  );
		soundSTAGEFAILED音		= new CSystemSound( @"Sounds\Stage failed.ogg",	false, true,  true  );
		soundGameStart		= new CSystemSound( @"Sounds\Game start.ogg",		false, false, false );
		soundGameEnd		= new CSystemSound( @"Sounds\Game end.ogg",		false, true,  false );
		soundStageClear	= new CSystemSound( @"Sounds\Stage clear.ogg",		false, true,  false );
		soundFullCombo		= new CSystemSound( @"Sounds\Full combo.ogg",		false, false, true  );
		soundNewRecord          = new CSystemSound( @"Sounds\New Record.ogg",      false, false, true  );
		soundExcellent    = new CSystemSound( @"Sounds\Excellent.ogg",       false, false, true  );
		soundNowLoading		= new CSystemSound( @"Sounds\Now loading.ogg",		false, true,  true  );
		soundTitle		= new CSystemSound( @"Sounds\Title.ogg",			false, true,  false );
		soundDecideSong            = new CSystemSound( @"Sounds\MusicDecide.ogg",     false, false, false );
		soundNovice            = new CSystemSound( @"Sounds\Novice.ogg",          false, false, false );
		soundRegular           = new CSystemSound( @"Sounds\Regular.ogg",         false, false, false );
		soundExpert		    = new CSystemSound( @"Sounds\Expert.ogg",		    false, false, false );
		soundBasic             = new CSystemSound( @"Sounds\Basic.ogg",           false, false, false );
		soundAdvanced          = new CSystemSound( @"Sounds\Advanced.ogg",        false, false, false );
		soundExtreme	        = new CSystemSound( @"Sounds\Extreme.ogg",		    false, false, false );
		soundMaster		    = new CSystemSound( @"Sounds\Master.ogg",			false, false, false );
		soundSelectMusic       = new CSystemSound( @"Sounds\SelectMusic.ogg",     false, false, false );
		bgm起動画面			= new CSystemSound( @"Sounds\Setup BGM.ogg",		true,  true,  false );
		bgmオプション画面		= new CSystemSound( @"Sounds\Option BGM.ogg",		true,  true,  false );
		bgmコンフィグ画面		= new CSystemSound( @"Sounds\Config BGM.ogg",		true,  true,  false );
		bgm選曲画面			= new CSystemSound( @"Sounds\Select BGM.ogg",		true,  true,  false );
		bgm結果画面            = new CSystemSound( @"Sounds\Result BGM.ogg",      true,  true,  false);
		soundMetronome     = new CSystemSound(@"Sounds\Metronome.ogg",         false, false, false);
	}

	public void ReloadSkin()
	{
		for ( int i = 0; i < nシステムサウンド数; i++ )
		{
			if ( !this[ i ].bExclusive )	// BGM系以外のみ読み込む。(BGM系は必要になったときに読み込む)
			{
				CSystemSound cシステムサウンド = this[ i ];
				if ( !CDTXMania.bCompactMode || cシステムサウンド.bCompact対象 )
				{
					try
					{
						cシステムサウンド.tRead();
						Trace.TraceInformation( "システムサウンドを読み込みました。({0})", cシステムサウンド.strFilename );
					}
					catch ( FileNotFoundException )
					{
						Trace.TraceWarning( "システムサウンドが存在しません。({0})", cシステムサウンド.strFilename );
					}
					catch ( Exception e )
					{
						Trace.TraceError( e.Message );
						Trace.TraceWarning( "システムサウンドの読み込みに失敗しました。({0})", cシステムサウンド.strFilename );
					}
				}
			}
		}
	}


	/// <summary>
	/// Skinの一覧を再取得する。
	/// System/*****/Graphics (やSounds/) というフォルダ構成を想定している。
	/// もし再取得の結果、現在使用中のSkinのパス(strSystemSkinSubfloderFullName)が消えていた場合は、
	/// 以下の優先順位で存在確認の上strSystemSkinSubfolderFullNameを再設定する。
	/// 1. System/Default/
	/// 2. System/*****/ で最初にenumerateされたもの
	/// 3. System/ (従来互換)
	/// </summary>
	public void ReloadSkinPaths()
	{
		#region [ まず System/*** をenumerateする ]
		string[] tempSkinSubfolders = Directory.GetDirectories( strSystemSkinRoot, "*" );
		strSystemSkinSubfolders = new string[ tempSkinSubfolders.Length ];
		int size = 0;
		for ( int i = 0; i < tempSkinSubfolders.Length; i++ )
		{
			#region [ 検出したフォルダがスキンフォルダかどうか確認する]
			if ( !bIsValid( tempSkinSubfolders[ i ] ) )
				continue;
			#endregion
			#region [ スキンフォルダと確認できたものを、strSkinSubfoldersに入れる ]
			// フォルダ名末尾に必ず\をつけておくこと。さもないとConfig読み出し側(必ず\をつける)とマッチできない
			if ( tempSkinSubfolders[ i ][ tempSkinSubfolders[ i ].Length - 1 ] != System.IO.Path.DirectorySeparatorChar )
			{
				tempSkinSubfolders[ i ] += System.IO.Path.DirectorySeparatorChar;
			}
			strSystemSkinSubfolders[ size ] = tempSkinSubfolders[ i ];
			Trace.TraceInformation( "SkinPath検出: {0}", strSystemSkinSubfolders[ size ] );
			size++;
			#endregion
		}
		Trace.TraceInformation( "SkinPath入力: {0}", strSystemSkinSubfolderFullName );
		Array.Resize( ref strSystemSkinSubfolders, size );
		Array.Sort( strSystemSkinSubfolders );	// BinarySearch実行前にSortが必要
		#endregion

		#region [ 現在のSkinパスがbox.defスキンをCONFIG指定していた場合のために、最初にこれが有効かチェックする。有効ならこれを使う。 ]
		if ( bIsValid( strSystemSkinSubfolderFullName ) &&
		     Array.BinarySearch( strSystemSkinSubfolders, strSystemSkinSubfolderFullName,
			     StringComparer.InvariantCultureIgnoreCase ) < 0 )
		{
			strBoxDefSkinSubfolders = new string[ 1 ]{ strSystemSkinSubfolderFullName };
			return;
		}
		#endregion

		#region [ 次に、現在のSkinパスが存在するか調べる。あれば終了。]
		if ( Array.BinarySearch( strSystemSkinSubfolders, strSystemSkinSubfolderFullName,
			    StringComparer.InvariantCultureIgnoreCase ) >= 0 )
			return;
		#endregion
		#region [ カレントのSkinパスが消滅しているので、以下で再設定する。]
		/// 以下の優先順位で現在使用中のSkinパスを再設定する。
		/// 1. System/Default/
		/// 2. System/*****/ で最初にenumerateされたもの
		/// 3. System/ (従来互換)
		#region [ System/Default/ があるなら、そこにカレントSkinパスを設定する]
		string tempSkinPath_default = System.IO.Path.Combine( strSystemSkinRoot, "Default" + System.IO.Path.DirectorySeparatorChar );
		if ( Array.BinarySearch( strSystemSkinSubfolders, tempSkinPath_default, 
			    StringComparer.InvariantCultureIgnoreCase ) >= 0 )
		{
			strSystemSkinSubfolderFullName = tempSkinPath_default;
			return;
		}
		#endregion
		#region [ System/SkinFiles.*****/ で最初にenumerateされたものを、カレントSkinパスに再設定する ]
		if ( strSystemSkinSubfolders.Length > 0 )
		{
			strSystemSkinSubfolderFullName = strSystemSkinSubfolders[ 0 ];
			return;
		}
		#endregion
		#region [ System/ に、カレントSkinパスを再設定する。]
		strSystemSkinSubfolderFullName = strSystemSkinRoot;
		strSystemSkinSubfolders = new string[ 1 ]{ strSystemSkinSubfolderFullName };
		#endregion
		#endregion
	}

	// メソッド

	public static string Path( string strファイルの相対パス )
	{
		if (string.IsNullOrEmpty(strBoxDefSkinSubfolderFullName) || !bUseBoxDefSkin )
		{
			return System.IO.Path.Combine( strSystemSkinSubfolderFullName, strファイルの相対パス );
		}
		else
		{
			return System.IO.Path.Combine( strBoxDefSkinSubfolderFullName, strファイルの相対パス );
		}
	}

	/// <summary>
	/// フルパス名を与えると、スキン名として、ディレクトリ名末尾の要素を返す
	/// 例: C:\foo\bar\ なら、barを返す
	/// </summary>
	/// <param name="skinpath">スキンが格納されたパス名(フルパス)</param>
	/// <returns>スキン名</returns>
	public static string GetSkinName( string skinPathFullName )
	{
		if ( skinPathFullName != null )
		{
			if ( skinPathFullName == "" )		// 「box.defで未定義」用
				skinPathFullName = strSystemSkinSubfolderFullName;
			string[] tmp = skinPathFullName.Split( System.IO.Path.DirectorySeparatorChar );
			return tmp[ tmp.Length - 2 ];		// ディレクトリ名の最後から2番目の要素がスキン名(最後の要素はnull。元stringの末尾が\なので。)
		}
		return null;
	}
	public static string[] GetSkinName( string[] skinPathFullNames )
	{
		string[] ret = new string[ skinPathFullNames.Length ];
		for ( int i = 0; i < skinPathFullNames.Length; i++ )
		{
			ret[ i ] = GetSkinName( skinPathFullNames[ i ] );
		}
		return ret;
	}


	public string GetSkinSubfolderFullNameFromSkinName( string skinName )
	{
		foreach ( string s in strSystemSkinSubfolders )
		{
			if ( GetSkinName( s ) == skinName )
				return s;
		}
		foreach ( string b in strBoxDefSkinSubfolders )
		{
			if ( GetSkinName( b ) == skinName )
				return b;
		}
		return null;
	}

	/// <summary>
	/// スキンパス名が妥当かどうか
	/// (タイトル画像にアクセスできるかどうかで判定する)
	/// </summary>
	/// <param name="skinPathFullName">妥当性を確認するスキンパス(フルパス)</param>
	/// <returns>妥当ならtrue</returns>
	public bool bIsValid( string skinPathFullName )
	{
		string filePathTitle;
		filePathTitle = System.IO.Path.Combine( skinPathFullName, @"Graphics\1_background.jpg" );
		return ( File.Exists( filePathTitle ) );
	}


	public void tRemoveMixerAll()
	{
		for (int i = 0; i < nシステムサウンド数; i++)
		{
			if (this[i] != null && this[i].b読み込み成功)
			{
				this[i].t停止する();
				this[i].tRemoveMixer();
			}
		}

	}

	public void tReadSkinConfig()
	{
		if( File.Exists( Path( @"SkinConfig.ini" ) ) )
		{
			string str;
			//this.tClearAllKeyAssignments();
			using ( StreamReader reader = new StreamReader( Path( @"SkinConfig.ini" ), Encoding.GetEncoding( "unicode" ) ) )
			{
				str = reader.ReadToEnd();
			}
			tReadFromString( str );
		}
	}

	/// <summary>
	/// 2016.07.30 kairera0467 #36413
	/// </summary>
	public void tSaveSkinConfig()
	{
		if( File.Exists( Path( @"SkinConfig.ini" ) ) )
		{
			StreamWriter sw = new StreamWriter( Path( @"SkinConfig.ini" ), false, Encoding.GetEncoding( "unicode" ) );
			sw.WriteLine( "; スキンごとでの設定ファイル。現在テスト段階です。" );
			sw.WriteLine( "; ここで設定した数値が優先的に使用されます。" );
			sw.WriteLine( ";" );
			sw.WriteLine( "; 読み込み画面、演奏画面、ネームプレート、リザルト画面の曲名で使用するフォント名" );
			sw.WriteLine( "DisplayFontName={0}", CDTXMania.ConfigIni.str曲名表示フォント );
			sw.WriteLine();
			sw.WriteLine( "; 選曲リストのフォント名" );
			sw.WriteLine( "; Font name for select song item." );
			sw.WriteLine( "SelectListFontName={0}", CDTXMania.ConfigIni.str選曲リストフォント );
			sw.WriteLine();
			sw.WriteLine( "; 選曲リストのフォントのサイズ[dot]" );
			sw.WriteLine( "; Font size[dot] for select song item." );
			sw.WriteLine( "SelectListFontSize={0}", CDTXMania.ConfigIni.n選曲リストフォントのサイズdot );
			sw.WriteLine();
			sw.WriteLine( "; ネームプレートタイプ" );
			sw.WriteLine( "; 0:タイプA XG2風の表示がされます。" );
			sw.WriteLine( "; 1:タイプB XG風の表示がされます。このタイプでは7_NamePlate_XG.png、7_Difficulty_XG.pngが読み込まれます。" );
			sw.WriteLine( "NamePlateType={0}", (int)CDTXMania.ConfigIni.eNamePlate );
			sw.WriteLine();
			sw.WriteLine( "; 動くドラムセット(0:ON, 1:OFF, 2:NONE)" );
			sw.WriteLine( "DrumSetMoves={0}", (int)CDTXMania.ConfigIni.eドラムセットを動かす );
			sw.WriteLine();
			sw.WriteLine( "; BPMバーの表示(0:表示する, 1:左のみ表示, 2:動くバーを表示しない, 3:表示しない)" );
			sw.WriteLine( "BPMBar={0}", (int)CDTXMania.ConfigIni.eBPMbar );
			sw.WriteLine();
			sw.WriteLine( "; LivePointの表示(0:OFF, 1:ON)" );
			sw.WriteLine( "LivePoint={0}", CDTXMania.ConfigIni.bLivePoint ? 1 : 0 );
			sw.WriteLine();
			sw.WriteLine( "; スピーカーの表示(0:OFF, 1:ON)" );
			sw.WriteLine( "Speaker={0}", CDTXMania.ConfigIni.bSpeaker ? 1 : 0 );
			sw.WriteLine();
			sw.WriteLine( ";判定画像のアニメーション方式" );
			sw.WriteLine( ";(0:旧DTXMania方式 1:コマ方式 2:擬似XG方式)" );
			sw.WriteLine( "JudgeAnimeType={0}", CDTXMania.ConfigIni.nJudgeAnimeType );
			sw.WriteLine();
			sw.WriteLine( ";判定画像のコマ数" );
			sw.WriteLine( "JudgeFrames={0}", CDTXMania.ConfigIni.nJudgeFrames );
			sw.WriteLine();
			sw.WriteLine( ";判定画像の1コマのフレーム数" );
			sw.WriteLine( "JudgeInterval={0}", CDTXMania.ConfigIni.nJudgeInterval );
			sw.WriteLine();
			sw.WriteLine( ";判定画像の1コマの幅" );
			sw.WriteLine( "JudgeWidgh={0}", CDTXMania.ConfigIni.nJudgeWidgh );
			sw.WriteLine();
			sw.WriteLine( ";判定画像の1コマの高さ" );
			sw.WriteLine( "JudgeHeight={0}", CDTXMania.ConfigIni.nJudgeHeight );
			sw.WriteLine();
			sw.WriteLine( ";アタックエフェクトのコマ数" );
			sw.WriteLine( "ExplosionFrames={0}", CDTXMania.ConfigIni.nExplosionFrames );
			sw.WriteLine();
			sw.WriteLine( ";アタックエフェクトの1コマのフレーム数" );
			sw.WriteLine( "ExplosionInterval={0}", CDTXMania.ConfigIni.nExplosionInterval );
			sw.WriteLine();
			sw.WriteLine( ";アタックエフェクトの1コマの幅" );
			sw.WriteLine( "ExplosionWidgh={0}", CDTXMania.ConfigIni.nExplosionWidgh );
			sw.WriteLine();
			sw.WriteLine( ";アタックエフェクトの1コマの高さ" );
			sw.WriteLine( "ExplosionHeight={0}", CDTXMania.ConfigIni.nExplosionHeight );
			sw.WriteLine();
			sw.WriteLine( "ワイリングエフェクトのコマ数;" );
			sw.WriteLine( "WailingFireFrames={0}", CDTXMania.ConfigIni.nWailingFireFrames );
			sw.WriteLine();
			sw.WriteLine( ";ワイリングエフェクトの1コマのフレーム数" );
			sw.WriteLine( "WailingFireInterval={0}", CDTXMania.ConfigIni.nWailingFireInterval );
			sw.WriteLine();
			sw.WriteLine( ";ワイリングエフェクトの1コマの幅" );
			sw.WriteLine( "WailingFireWidgh={0}", CDTXMania.ConfigIni.nWailingFireWidgh );
			sw.WriteLine();
			sw.WriteLine( ";ワイリングエフェクトの1コマの高さ" );
			sw.WriteLine( "WailingFireHeight={0}", CDTXMania.ConfigIni.nWailingFireHeight );
			sw.WriteLine();
			sw.WriteLine( ";ワイリングエフェクトのX座標" );
			sw.WriteLine( "WailingFirePosXGuitar={0}", CDTXMania.ConfigIni.nWailingFireX.Guitar );
			sw.WriteLine( "WailingFirePosXBass={0}", CDTXMania.ConfigIni.nWailingFireX.Bass );
			sw.WriteLine();
			sw.WriteLine( ";ワイリングエフェクトのY座標(Guitar、Bass共通)" );
			sw.WriteLine( "WailingFirePosY={0}", CDTXMania.ConfigIni.nWailingFireY );
			sw.WriteLine();

			sw.Close();
		}
	}

	private void tReadFromString(string strAllSettings)	// 2011.4.13 yyagi; refactored to make initial KeyConfig easier.
	{
		string[] delimiter = { "\n" };
		string[] strSingleLine = strAllSettings.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
		foreach (string s in strSingleLine)
		{
			string str = s.Replace('\t', ' ').TrimStart(new char[] { '\t', ' ' });
			if ((str.Length != 0) && (str[0] != ';'))
			{
				try
				{
					string str3;
					string str4;
					string[] strArray = str.Split(new char[] { '=' });
					if (strArray.Length == 2)
					{
						str3 = strArray[0].Trim();
						str4 = strArray[1].Trim();
						//-----------------------------
						if (str3.Equals("SelectListFontName"))
						{
							CDTXMania.ConfigIni.str選曲リストフォント = str4;
						}
						else if (str3.Equals("DisplayFontName"))
						{
							CDTXMania.ConfigIni.str曲名表示フォント = str4;
						}
						else if (str3.Equals("SelectListFontSize"))
						{
							CDTXMania.ConfigIni.n選曲リストフォントのサイズdot = CConversion.nGetNumberIfInRange(str4, 1, 0x3e7, CDTXMania.ConfigIni.n選曲リストフォントのサイズdot);
						}
						else if (str3.Equals("SelectListFontBold"))
						{
							CDTXMania.ConfigIni.b選曲リストフォントを太字にする = CConversion.bONorOFF(str4[0]);
						}
						else if (str3.Equals("NamePlateType"))
						{
							CDTXMania.ConfigIni.eNamePlate = (EType)CConversion.nGetNumberIfInRange(str4, 0, 3, (int)CDTXMania.ConfigIni.eNamePlate);
						}
						else if (str3.Equals("DrumSetMoves"))
						{
							CDTXMania.ConfigIni.eドラムセットを動かす = (EType)CConversion.nGetNumberIfInRange(str4, 0, 2, (int)CDTXMania.ConfigIni.eドラムセットを動かす);
						}
						else if (str3.Equals("BPMBar"))
						{
							CDTXMania.ConfigIni.eBPMbar = (EType)CConversion.nGetNumberIfInRange(str4, 0, 3, (int)CDTXMania.ConfigIni.eBPMbar);
						}
						else if (str3.Equals("LivePoint"))
						{
							CDTXMania.ConfigIni.bLivePoint = CConversion.bONorOFF(str4[0]);
						}
						else if (str3.Equals("Speaker"))
						{
							CDTXMania.ConfigIni.bSpeaker = CConversion.bONorOFF(str4[0]);
						}
						else if (str3.Equals("JudgeAnimeType"))
						{
							CDTXMania.ConfigIni.nJudgeAnimeType = CConversion.nGetNumberIfInRange(str4, 0, 2, CDTXMania.ConfigIni.nJudgeAnimeType);
						}
						else if (str3.Equals("JudgeFrames"))
						{
							CDTXMania.ConfigIni.nJudgeFrames = CConversion.nStringToInt(str4, CDTXMania.ConfigIni.nJudgeFrames);
						}
						else if (str3.Equals("JudgeInterval"))
						{
							CDTXMania.ConfigIni.nJudgeInterval = CConversion.nStringToInt(str4, CDTXMania.ConfigIni.nJudgeInterval);
						}
						else if (str3.Equals("JudgeWidgh"))
						{
							CDTXMania.ConfigIni.nJudgeWidgh = CConversion.nStringToInt(str4, CDTXMania.ConfigIni.nJudgeWidgh);
						}
						else if (str3.Equals("JudgeHeight"))
						{
							CDTXMania.ConfigIni.nJudgeHeight = CConversion.nStringToInt(str4, CDTXMania.ConfigIni.nJudgeHeight);
						}
						else if (str3.Equals("ExplosionFrames"))
						{
							CDTXMania.ConfigIni.nExplosionFrames = CConversion.nGetNumberIfInRange(str4, 0, int.MaxValue, (int)CDTXMania.ConfigIni.nExplosionFrames);
						}
						else if (str3.Equals("ExplosionInterval"))
						{
							CDTXMania.ConfigIni.nExplosionInterval = CConversion.nGetNumberIfInRange(str4, 0, int.MaxValue, (int)CDTXMania.ConfigIni.nExplosionInterval);
						}
						else if (str3.Equals("ExplosionWidgh"))
						{
							CDTXMania.ConfigIni.nExplosionWidgh = CConversion.nGetNumberIfInRange(str4, 0, int.MaxValue, (int)CDTXMania.ConfigIni.nExplosionWidgh);
						}
						else if (str3.Equals("ExplosionHeight"))
						{
							CDTXMania.ConfigIni.nExplosionHeight = CConversion.nGetNumberIfInRange(str4, 0, int.MaxValue, (int)CDTXMania.ConfigIni.nExplosionHeight);
						}
						else if (str3.Equals("WailingFireFrames"))
						{
							CDTXMania.ConfigIni.nWailingFireFrames = CConversion.nGetNumberIfInRange(str4, 0, int.MaxValue, (int)CDTXMania.ConfigIni.nWailingFireFrames);
						}
						else if (str3.Equals("WailingFireInterval"))
						{
							CDTXMania.ConfigIni.nWailingFireInterval = CConversion.nGetNumberIfInRange(str4, 0, int.MaxValue, (int)CDTXMania.ConfigIni.nWailingFireInterval);
						}
						else if (str3.Equals("WailingFireWidgh"))
						{
							CDTXMania.ConfigIni.nWailingFireWidgh = CConversion.nGetNumberIfInRange(str4, 0, int.MaxValue, (int)CDTXMania.ConfigIni.nWailingFireWidgh);
						}
						else if (str3.Equals("WailingFireHeight"))
						{
							CDTXMania.ConfigIni.nWailingFireHeight = CConversion.nGetNumberIfInRange(str4, 0, int.MaxValue, (int)CDTXMania.ConfigIni.nWailingFireHeight);
						}
						else if (str3.Equals("WailingFirePositionXGuitar"))
						{
							CDTXMania.ConfigIni.nWailingFireX.Guitar = CConversion.nGetNumberIfInRange(str4, 0, int.MaxValue, (int)CDTXMania.ConfigIni.nWailingFireX.Guitar);
						}
						else if (str3.Equals("WailingFirePositionXBass"))
						{
							CDTXMania.ConfigIni.nWailingFireX.Bass = CConversion.nGetNumberIfInRange(str4, 0, int.MaxValue, (int)CDTXMania.ConfigIni.nWailingFireX.Bass);
						}
						else if (str3.Equals("WailingFirePosY"))
						{
							CDTXMania.ConfigIni.nWailingFireX.Bass = CConversion.nGetNumberIfInRange(str4, 0, int.MaxValue, (int)CDTXMania.ConfigIni.nWailingFireY);
						}
						//-----------------------------
					}
					continue;
				}
				catch (Exception exception)
				{
					Trace.TraceError(exception.Message);
					continue;
				}
			}
		}
	}

	#region [ IDisposable 実装 ]
	//-----------------
	public void Dispose()
	{
		if( !bDisposed済み )
		{
			for( int i = 0; i < nシステムサウンド数; i++ )
				this[ i ].Dispose();

			bDisposed済み = true;
		}
	}
	//-----------------
	#endregion


	// Other

	#region [ private ]
	//-----------------
	private bool bDisposed済み;
	//-----------------
	#endregion
}