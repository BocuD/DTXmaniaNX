using System.Diagnostics;
using System.Text;
using DTXMania.Core.Audio;
using FDK;

namespace DTXMania.Core;

internal class CSkin : IDisposable
{
	// クラス


	
	// プロパティ

	public CSystemSound bgmコンフィグ画面 = null;
	public CSystemSound bgmTitleScreen = null;
	public CSystemSound bgmSongSelectScreen = null;
	public CSystemSound soundSTAGEFAILED音 = null;
	public CSystemSound soundCursorMovement = null;
	public CSystemSound soundGameEnd = null;
	public CSystemSound soundStageClear = null;
	public CSystemSound soundFullCombo = null;
	public CSystemSound soundAudience = null;  // sound歓声音
	public CSystemSound soundNowLoading = null;
	public CSystemSound soundDecide = null;  // sound決定音
	public CSystemSound soundCancel = null;  // sound取消音
	public CSystemSound soundChange = null;  // sound変更音
	public CSystemSound soundExcellent = null;
	public CSystemSound soundNewRecord = null;  // sound新記録音
	public CSystemSound soundMaster = null;
	public CSystemSound soundBasic = null;
	public CSystemSound soundAdvanced = null;
	public CSystemSound soundExtreme = null;
	public CSystemSound soundMetronome = null;
	//every system sound still living here, for the two things that treat them as a set: freeing
	//them when the skin changes, and reading them up front. Sounds are moving out to whoever
	//actually owns them — a stage's to its StageRoot, the shared ones to the skin — so this list
	//only ever shrinks, and the region goes with its last entry
	private CSystemSound[] AllSounds =>
	[
		bgmコンフィグ画面, bgmTitleScreen, bgmSongSelectScreen,
		soundCursorMovement, soundDecide, soundCancel, soundChange,
		soundNowLoading, soundStageClear, soundGameEnd,
		soundAudience, soundMetronome, soundSTAGEFAILED音,
		soundFullCombo, soundExcellent, soundNewRecord,
		soundMaster, soundBasic, soundAdvanced, soundExtreme
	];


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

		foreach ( CSystemSound sound in AllSounds )
		{
			if ( sound is { loadSucceeded: true } )
			{
				sound.tStop();
				sound.Dispose();
			}
		}
		soundCursorMovement	= new CSystemSound( @"Sounds\Move.ogg",			false, false);
		soundDecide			= new CSystemSound( @"Sounds\Decide.ogg",			false, false);
		soundChange			= new CSystemSound( @"Sounds\Change.ogg",			false, false);
		soundCancel			= new CSystemSound( @"Sounds\Cancel.ogg",			false, false);
		soundAudience			= new CSystemSound( @"Sounds\Audience.ogg",		false, false);
		soundSTAGEFAILED音		= new CSystemSound( @"Sounds\Stage failed.ogg",	false, true);
		soundGameEnd		= new CSystemSound( @"Sounds\Game end.ogg",		false, true);
		soundStageClear	= new CSystemSound( @"Sounds\Stage clear.ogg",		false, true);
		soundFullCombo		= new CSystemSound( @"Sounds\Full combo.ogg",		false, false);
		soundNewRecord          = new CSystemSound( @"Sounds\New Record.ogg",      false, false);
		soundExcellent    = new CSystemSound( @"Sounds\Excellent.ogg",       false, false);
		soundNowLoading		= new CSystemSound( @"Sounds\Now loading.ogg",		false, true);
		soundBasic             = new CSystemSound( @"Sounds\Basic.ogg",           false, false);
		soundAdvanced          = new CSystemSound( @"Sounds\Advanced.ogg",        false, false);
		soundExtreme	        = new CSystemSound( @"Sounds\Extreme.ogg",		    false, false);
		soundMaster		    = new CSystemSound( @"Sounds\Master.ogg",			false, false);
		bgmTitleScreen			= new CSystemSound( @"Sounds\Setup BGM.ogg",		true, true, AudioGroup.Bgm);
		bgmコンフィグ画面		= new CSystemSound( @"Sounds\Config BGM.ogg",		true, true, AudioGroup.Bgm);
		bgmSongSelectScreen			= new CSystemSound( @"Sounds\Select BGM.ogg",		true, true, AudioGroup.Bgm);
		soundMetronome     = new CSystemSound(@"Sounds\Metronome.ogg",         false, false);
	}

	public void ReloadSkin()
	{
		foreach ( CSystemSound cSystemSound in AllSounds )
		{
			if (cSystemSound.bExclusive) continue; // BGM系以外のみ読み込む。(BGM系は必要になったときに読み込む)

			try
			{
				cSystemSound.tRead();
				Trace.TraceInformation( "Loaded system sound: ({0})", cSystemSound.strFilename );
			}
			catch ( FileNotFoundException )
			{
				Trace.TraceWarning( "System sound doesn't exist: ({0})", cSystemSound.strFilename );
			}
			catch ( Exception e )
			{
				Trace.TraceError( e.Message );
				Trace.TraceWarning( "Failed to read system sound: ({0})", cSystemSound.strFilename );
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


	public void tRemoveMixerAll(CSystemSound? keepPlaying = null)
	{
		foreach (CSystemSound sound in AllSounds)
		{
			if (sound != keepPlaying && sound is { loadSucceeded: true })
			{
				sound.tStop();
				sound.tRemoveMixer();
			}
		}

	}

	public void tReadSkinConfig()
	{
		if ( File.Exists( Path( @"SkinConfig.ini" ) ) )
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
		if ( File.Exists( Path( @"SkinConfig.ini" ) ) )
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
			sw.WriteLine( "SelectListFontName={0}", CDTXMania.ConfigIni.songListFont );
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
							CDTXMania.ConfigIni.songListFont = str4;
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
		if ( !bDisposed済み )
		{
			foreach( CSystemSound sound in AllSounds )
				sound.Dispose();

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
