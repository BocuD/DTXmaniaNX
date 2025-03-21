﻿using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Drawing.Text;
using SharpDX;
using FDK;

using Color = System.Drawing.Color;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using System.Drawing.Drawing2D;
using DTXMania.Core;

namespace DTXMania;

internal class CActSelectSongList : CActivity
{
	// プロパティ

	public bool bIsEnumeratingSongs
	{
		get;
		set;
	}
	public bool bScrolling
	{
		get
		{
			if( nTargetScrollCounter == 0 )
			{
				return ( nCurrentScrollCounter != 0 );
			}
			return true;
		}
	}
	public int n現在のアンカ難易度レベル 
	{
		get;
		private set;
	}
	public int n現在選択中の曲の現在の難易度レベル => n現在のアンカ難易度レベルに最も近い難易度レベルを返す( rSelectedSong );

	public CScore rSelectedScore  // r現在選択中のスコア
	{
		get
		{
			if( rSelectedSong != null )
			{
				return rSelectedSong.arScore[ n現在選択中の曲の現在の難易度レベル ];
			}
			return null;
		}
	}
	public CSongListNode rSelectedSong  // r現在選択中の曲
	{
		get;
		private set;
	}

	public int nスクロールバー相対y座標
	{
		get;
		private set;
	}

	// tSelectedSongChanged()内で使う、直前の選曲の保持
	// (前と同じ曲なら選択曲変更に掛かる再計算を省略して高速化するため)
	private CSongListNode song_last = null;

		
	// コンストラクタ

	public CActSelectSongList()
	{
		rSelectedSong = null;
		n現在のアンカ難易度レベル = 0;
		bNotActivated = true;
		bIsEnumeratingSongs = false;

		listChildActivities.Add( actステータスパネル = new CActSelectStatusPanel() );

		stパネルマップ = null;
		stパネルマップ = new STATUSPANEL[12];		// yyagi: 以下、手抜きの初期化でスマン
		string[] labels = new string[12] 
		{
			"DTXMANIA",     //0
			"DEBUT",        //1
			"NOVICE",       //2
			"REGULAR",      //3
			"EXPERT",       //4
			"MASTER",       //5
			"BASIC",        //6
			"ADVANCED",     //7
			"EXTREME",      //8
			"RAW",          //9
			"RWS",          //10
			"REAL"          //11
		};
		int[] status = new int[12] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };

		for (int i = 0; i < 12; i++)
		{
			stパネルマップ[i] = default(STATUSPANEL);
			stパネルマップ[i].status = status[i];
			stパネルマップ[i].label = labels[i];
		}
	}


	// メソッド

	public int n現在のアンカ難易度レベルに最も近い難易度レベルを返す( CSongListNode song )
	{
		// 事前チェック。

		if( song == null )
			return n現在のアンカ難易度レベル;	// 曲がまったくないよ

		if( song.arScore[ n現在のアンカ難易度レベル ] != null )
			return n現在のアンカ難易度レベル;	// 難易度ぴったりの曲があったよ

		if( ( song.eNodeType == CSongListNode.ENodeType.BOX ) || ( song.eNodeType == CSongListNode.ENodeType.BACKBOX ) )
			return 0;								// BOX と BACKBOX は関係無いよ


		// 現在のアンカレベルから、難易度上向きに検索開始。

		int closestLevel = n現在のアンカ難易度レベル;

		for( int i = 0; i < 5; i++ )
		{
			if( song.arScore[ closestLevel ] != null )
				break;	// 曲があった。

			closestLevel = ( closestLevel + 1 ) % 5;	// 曲がなかったので次の難易度レベルへGo。（5以上になったら0に戻る。）
		}


		// 見つかった曲がアンカより下のレベルだった場合……
		// アンカから下向きに検索すれば、もっとアンカに近い曲があるんじゃね？

		if( closestLevel < n現在のアンカ難易度レベル )
		{
			// 現在のアンカレベルから、難易度下向きに検索開始。

			closestLevel = n現在のアンカ難易度レベル;

			for( int i = 0; i < 5; i++ )
			{
				if( song.arScore[ closestLevel ] != null )
					break;	// 曲があった。

				closestLevel = ( ( closestLevel - 1 ) + 5 ) % 5;	// 曲がなかったので次の難易度レベルへGo。（0未満になったら4に戻る。）
			}
		}

		return closestLevel;
	}

	private List<CSongListNode> GetSongListWithinMe( CSongListNode song )
	{
		if ( song.r親ノード == null )					// root階層のノートだったら
		{
			return CDTXMania.SongManager.listSongRoot;	// rootのリストを返す
		}
		else
		{
			if ( ( song.r親ノード.list子リスト != null ) && ( song.r親ノード.list子リスト.Count > 0 ) )
			{
				return song.r親ノード.list子リスト;
			}
			else
			{
				return null;
			}
		}
	}


	public delegate void DGSortFunc( List<CSongListNode> songList, EInstrumentPart eInst, int order, params object[] p);

	public void tSortSongList( DGSortFunc sf, EInstrumentPart eInst, int order, params object[] p)  // t曲リストのソート
	{
		List<CSongListNode> songList = GetSongListWithinMe( rSelectedSong );
		if ( songList == null )
		{
		}
		else
		{
			sf( songList, eInst, order, p );
			t現在選択中の曲を元に曲バーを再構成する();
		}
	}

	//Regenerate the clear lamps texture after GB Swap event occur
	public void tSwapClearLamps() 
	{
		for (int i = 0; i < 13; i++)
		{				
			tGenerateClearLampTexture(i, stBarInformation[i].nClearLamps);
		}
	}

	public bool tGoIntoBOX()  //tBOXに入る
	{
		bool ret = false;
		if ( CSkin.GetSkinName( CDTXMania.Skin.GetCurrentSkinSubfolderFullName( false ) ) != CSkin.GetSkinName( rSelectedSong.strSkinPath )
		     && CSkin.bUseBoxDefSkin )
		{
			ret = true;
			// BOXに入るときは、スキン変更発生時のみboxdefスキン設定の更新を行う
			CDTXMania.Skin.SetCurrentSkinSubfolderFullName(
				CDTXMania.Skin.GetSkinSubfolderFullNameFromSkinName( CSkin.GetSkinName( rSelectedSong.strSkinPath ) ), false );
		}

		if( ( rSelectedSong.list子リスト != null ) && ( rSelectedSong.list子リスト.Count > 0 ) )
		{
			rSelectedSong = rSelectedSong.list子リスト[ 0 ];
			t現在選択中の曲を元に曲バーを再構成する();
			t選択曲が変更された(false);									// #27648 項目数変更を反映させる
		}
		return ret;
	}
	public bool tExitBOX()  // tBOXを出る
	{
		bool ret = false;
		if ( CSkin.GetSkinName( CDTXMania.Skin.GetCurrentSkinSubfolderFullName( false ) ) != CSkin.GetSkinName( rSelectedSong.strSkinPath )
		     && CSkin.bUseBoxDefSkin )
		{
			ret = true;
		}
		CDTXMania.Skin.SetCurrentSkinSubfolderFullName(
			( rSelectedSong.strSkinPath == "" ) ? "" : CDTXMania.Skin.GetSkinSubfolderFullNameFromSkinName( CSkin.GetSkinName( rSelectedSong.strSkinPath ) ), false );
		if ( rSelectedSong.r親ノード != null )
		{
			rSelectedSong = rSelectedSong.r親ノード;
			t現在選択中の曲を元に曲バーを再構成する();
			t選択曲が変更された(false);									// #27648 項目数変更を反映させる
		}
		return ret;
	}
	public void t現在選択中の曲を元に曲バーを再構成する()
	{
		tInitializeBar();
		for( int i = 0; i < 13; i++ )
		{
			tGenerateSongNameBar( i, stBarInformation[ i ].strTitleString, stBarInformation[ i ].colLetter );
			tGeneratePreviewImageTexture(i, stBarInformation[i].strPreviewImageFullPath, stBarInformation[i].eBarType);
			tGenerateClearLampTexture(i, stBarInformation[i].nClearLamps);
		}
	}
	public void tMoveToNext()  // t次に移動
	{
		if( rSelectedSong != null )
		{
			nTargetScrollCounter += 100;
		}
	}
	public void tMoveToPrevious()  // t前に移動
	{
		if( rSelectedSong != null )
		{
			nTargetScrollCounter -= 100;
		}
	}
	public void t難易度レベルをひとつ進める()
	{
		if( ( rSelectedSong == null ) || ( rSelectedSong.nスコア数 <= 1 ) )
			return;		// 曲にスコアが０～１個しかないなら進める意味なし。
			

		// 難易度レベルを＋１し、現在選曲中のスコアを変更する。

		n現在のアンカ難易度レベル = n現在のアンカ難易度レベルに最も近い難易度レベルを返す( rSelectedSong );

		for( int i = 0; i < 5; i++ )
		{
			n現在のアンカ難易度レベル = ( n現在のアンカ難易度レベル + 1 ) % 5;	// ５以上になったら０に戻る。
			if( rSelectedSong.arScore[ n現在のアンカ難易度レベル ] != null )	// 曲が存在してるならここで終了。存在してないなら次のレベルへGo。
				break;
		}


		// 曲毎に表示しているスキル値を、新しい難易度レベルに合わせて取得し直す。（表示されている13曲全部。）

		CSongListNode song = rSelectedSong;
		for( int i = 0; i < 5; i++ )
			song = rPreviousSong( song );

		for( int i = nSelectedRow - 5; i < ( ( nSelectedRow - 5 ) + 13 ); i++ )
		{
			int index = ( i + 13 ) % 13;
			for( int m = 0; m < 3; m++ )
			{
				stBarInformation[ index ].nSkillValue[ m ] = (int) song.arScore[ n現在のアンカ難易度レベルに最も近い難易度レベルを返す( song ) ].SongInformation.HighSkill[ m ];
			}
			song = rNextSong( song );
		}

		tラベル名からステータスパネルを決定する( rSelectedSong.arDifficultyLabel[ n現在選択中の曲の現在の難易度レベル ] );

		switch( nIndex  )
		{
			case 2:
				CDTXMania.Skin.soundNovice.tPlay();
				string strnov = CSkin.Path( @"Sounds\Novice.ogg" );
				if( !File.Exists( strnov ) )
					CDTXMania.Skin.soundChange.tPlay();
				break;

			case 3:
				CDTXMania.Skin.soundRegular.tPlay();
				string strreg = CSkin.Path( @"Sounds\Regular.ogg" );
				if( !File.Exists( strreg ) )
					CDTXMania.Skin.soundChange.tPlay();
				break;

			case 4:
				CDTXMania.Skin.soundExpert.tPlay();
				string strexp = CSkin.Path( @"Sounds\Expert.ogg" );
				if( !File.Exists( strexp ) )
					CDTXMania.Skin.soundChange.tPlay();
				break;

			case 5:
				CDTXMania.Skin.soundMaster.tPlay();
				string strmas = CSkin.Path( @"Sounds\Master.ogg" );
				if( !File.Exists( strmas ) )
					CDTXMania.Skin.soundChange.tPlay();
				break;
                
			case 6:
				CDTXMania.Skin.soundBasic.tPlay();
				string strbsc = CSkin.Path( @"Sounds\Basic.ogg" );
				if( !File.Exists( strbsc ) )
					CDTXMania.Skin.soundChange.tPlay();
				break;

			case 7:
				CDTXMania.Skin.soundAdvanced.tPlay();
				string stradv = CSkin.Path( @"Sounds\Advanced.ogg" );
				if( !File.Exists( stradv ) )
					CDTXMania.Skin.soundChange.tPlay();
				break;

			case 8:
				CDTXMania.Skin.soundExtreme.tPlay();
				string strext = CSkin.Path( @"Sounds\Extreme.ogg" );
				if( !File.Exists( strext ) )
					CDTXMania.Skin.soundChange.tPlay();
				break;

			default:
				CDTXMania.Skin.soundChange.tPlay();
				break;
		}

		// 選曲ステージに変更通知を発出し、関係Activityの対応を行ってもらう。

		CDTXMania.stageSongSelection.tSelectedSongChanged();
	}

	public void tラベル名からステータスパネルを決定する(string strラベル名)
	{
		if (string.IsNullOrEmpty(strラベル名))
		{
			nIndex = 0;
		}
		else
		{
			STATUSPANEL[] array = stパネルマップ;
			for (int i = 0; i < array.Length; i++)
			{
				STATUSPANEL sTATUSPANEL = array[i];
				if (strラベル名.Equals(sTATUSPANEL.label, StringComparison.CurrentCultureIgnoreCase))
				{
					nIndex = sTATUSPANEL.status;
					return;
				}
				nIndex++;
			}
		}
	}

	//
	public void SearchUpdate()
	{
		rSelectedSong = CDTXMania.SongManager.listSongRoot[0];
		t現在選択中の曲を元に曲バーを再構成する();
		t選択曲が変更された(true);
		CDTXMania.stageSongSelection.tSelectedSongChanged();
	}

	/// <summary>
	/// 曲リストをリセットする
	/// </summary>
	/// <param name="cs"></param>
	public void Refresh(CSongManager cs, bool bRemakeSongTitleBar )		// #26070 2012.2.28 yyagi
	{
//			this.OnDeactivate();

		if ( cs != null && cs.listSongRoot.Count > 0 )	// 新しい曲リストを検索して、1曲以上あった
		{
			CDTXMania.SongManager = cs;

			if ( rSelectedSong != null )			// r現在選択中の曲==null とは、「最初songlist.dbが無かった or 検索したが1曲もない」
			{
				rSelectedSong = searchCurrentBreadcrumbsPosition( CDTXMania.SongManager.listSongRoot, rSelectedSong.strBreadcrumbs );
				if ( bRemakeSongTitleBar )					// 選曲画面以外に居るときには再構成しない (非活性化しているときに実行すると例外となる)
				{
					t現在選択中の曲を元に曲バーを再構成する();
				}
#if false          // list子リストの中まではmatchしてくれないので、検索ロジックは手書きで実装 (searchCurrentBreadcrumbs())
					string bc = this.rSelectedSong.strBreadcrumbs;
					Predicate<C曲リストノード> match = delegate( C曲リストノード c )
					{
						return ( c.strBreadcrumbs.Equals( bc ) );
					};
					int nMatched = CDTXMania.Songs管理.list曲ルート.FindIndex( match );

					this.rSelectedSong = ( nMatched == -1 ) ? null : CDTXMania.Songs管理.list曲ルート[ nMatched ];
					this.t現在選択中の曲を元に曲バーを再構成する();
#endif
				return;
			}
		}
		OnDeactivate();
		rSelectedSong = null;
		if( CDTXMania.rCurrentStage.eStageID == CStage.EStage.SongSelection_4 )
			OnActivate();
	}


	/// <summary>
	/// 現在選曲している位置を検索する
	/// (曲一覧クラスを新しいものに入れ替える際に用いる)
	/// </summary>
	/// <param name="ln">検索対象のList</param>
	/// <param name="bc">検索するパンくずリスト(文字列)</param>
	/// <returns></returns>
	private CSongListNode searchCurrentBreadcrumbsPosition( List<CSongListNode> ln, string bc )
	{
		foreach (CSongListNode n in ln)
		{
			if ( n.strBreadcrumbs == bc )
			{
				return n;
			}
			else if ( n.list子リスト != null && n.list子リスト.Count > 0 )	// 子リストが存在するなら、再帰で探す
			{
				CSongListNode r = searchCurrentBreadcrumbsPosition( n.list子リスト, bc );
				if ( r != null ) return r;
			}
		}
		return null;
	}

	/// <summary>
	/// BOXのアイテム数と、今何番目を選択しているかをセットする
	/// </summary>
	public void t選択曲が変更された( bool bForce)    // t選択曲が変更された  #27648
	{
		CSongListNode song = CDTXMania.stageSongSelection.r現在選択中の曲;
		if ( song == null )
			return;
		if ( song == song_last && bForce == false )
			return;
				
		song_last = song;
		List<CSongListNode> list = ( song.r親ノード != null ) ? song.r親ノード.list子リスト : CDTXMania.SongManager.listSongRoot;
		int index = list.IndexOf( song ) + 1;
		if ( index <= 0 )
		{
			nCurrentPosition = nNumOfItems = 0;
		}
		else
		{
			nCurrentPosition = index;
			nNumOfItems = list.Count;
		}
	}

	// CActivity 実装

	public override void OnActivate()
	{
		if( bActivated )
			return;

		eInstrumentPart = EInstrumentPart.DRUMS;
		bAllAnimationsCompleted = false;
		nTargetScrollCounter = 0;
		nCurrentScrollCounter = 0;
		nScrollTimer = -1;

		// フォント作成。
		// 曲リスト文字は２倍（面積４倍）でテクスチャに描画してから縮小表示するので、フォントサイズは２倍とする。

		FontStyle regular = FontStyle.Regular;
		if( CDTXMania.ConfigIni.b選曲リストフォントを斜体にする ) regular |= FontStyle.Italic;
		if( CDTXMania.ConfigIni.b選曲リストフォントを太字にする ) regular |= FontStyle.Bold;
		ftSongListFont = new Font( CDTXMania.ConfigIni.str選曲リストフォント, (float) ( CDTXMania.ConfigIni.n選曲リストフォントのサイズdot * 2 ), regular, GraphicsUnit.Pixel );
			

		// 現在選択中の曲がない（＝はじめての活性化）なら、現在選択中の曲をルートの先頭ノードに設定する。

		if( ( rSelectedSong == null ) && ( CDTXMania.SongManager.listSongRoot.Count > 0 ) )
			rSelectedSong = CDTXMania.SongManager.listSongRoot[ 0 ];


		// バー情報を初期化する。

		tInitializeBar();

		base.OnActivate();

		t選択曲が変更された(true);		// #27648 2012.3.31 yyagi 選曲画面に入った直後の 現在位置/全アイテム数 の表示を正しく行うため
	}
	public override void OnDeactivate()
	{
		if( bNotActivated )
			return;

		CDTXMania.tDisposeSafely( ref ftSongListFont );

		for( int i = 0; i < 13; i++ )
			ct登場アニメ用[ i ] = null;

		base.OnDeactivate();
	}
	public override void OnManagedCreateResources()
	{
		if( bNotActivated )
			return;

		strDefaultPreImage = CSkin.Path(@"Graphics\5_preimage default.png");
		txSongNameBar.Score = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\5_bar score.png" ), false );
		txSongNameBar.Box = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\5_bar box.png" ), false );
		txSongNameBar.Other = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\5_bar other.png" ), false );
		txSongSelectionBar.Score = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\5_bar score selected.png" ), false );
		txSongSelectionBar.Box = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\5_bar box selected.png" ), false );
		txSongSelectionBar.Other = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\5_bar other selected.png" ), false );
		txSkillNumbers = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\ScreenSelect skill number on list.png"), false);
		txTopPanel = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\5_header song list.png"), false);
		txBottomPanel = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\5_footer song list.png"), false);

		prvFont = new CPrivateFastFont( new FontFamily( CDTXMania.ConfigIni.str選曲リストフォント ), 30, FontStyle.Regular );
		prvFontSmall = new CPrivateFastFont( new FontFamily( CDTXMania.ConfigIni.str選曲リストフォント ), 15, FontStyle.Regular );

		for( int i = 0; i < 13; i++ )
		{
			tGenerateSongNameBar(i, stBarInformation[i].strTitleString, stBarInformation[i].colLetter);
			tGeneratePreviewImageTexture(i, stBarInformation[i].strPreviewImageFullPath, stBarInformation[i].eBarType);
			tGenerateClearLampTexture(i, stBarInformation[i].nClearLamps);
		}

			

		int c = CDTXMania.isJapanese ? 0 : 1;
		#region [ Songs not found画像 ]
		try
		{
			using( Bitmap image = new Bitmap( 640, 128 ) )
			using( Graphics graphics = Graphics.FromImage( image ) )
			{
				string[] s1 = { "曲データが見つかりません。", "Songs not found." };
				string[] s2 = { "曲データをDTXManiaNX.exe以下の", "You need to install songs." };
				string[] s3 = { "フォルダにインストールして下さい。", "" };
				graphics.DrawString( s1[c], ftSongListFont, Brushes.DarkGray, (float) 2f, (float) 2f );
				graphics.DrawString( s1[c], ftSongListFont, Brushes.White, (float) 0f, (float) 0f );
				graphics.DrawString( s2[c], ftSongListFont, Brushes.DarkGray, (float) 2f, (float) 44f );
				graphics.DrawString( s2[c], ftSongListFont, Brushes.White, (float) 0f, (float) 42f );
				graphics.DrawString( s3[c], ftSongListFont, Brushes.DarkGray, (float) 2f, (float) 86f );
				graphics.DrawString( s3[c], ftSongListFont, Brushes.White, (float) 0f, (float) 84f );

				txSongNotFound = new CTexture( CDTXMania.app.Device, image, CDTXMania.TextureFormat );

				txSongNotFound.vcScaleRatio = new Vector3( 0.5f, 0.5f, 1f );	// 半分のサイズで表示する。
			}
		}
		catch( CTextureCreateFailedException )
		{
			Trace.TraceError( "SoungNotFoundテクスチャの作成に失敗しました。" );
			txSongNotFound = null;
		}
		#endregion
		#region [ "曲データを検索しています"画像 ]
		try
		{
			using ( Bitmap image = new Bitmap( 640, 96 ) )
			using ( Graphics graphics = Graphics.FromImage( image ) )
			{
				string[] s1 = { "曲データを検索しています。", "Now enumerating songs." };
				string[] s2 = { "そのまましばらくお待ち下さい。", "Please wait..." };
				graphics.DrawString( s1[c], ftSongListFont, Brushes.DarkGray, (float) 2f, (float) 2f );
				graphics.DrawString( s1[c], ftSongListFont, Brushes.White, (float) 0f, (float) 0f );
				graphics.DrawString( s2[c], ftSongListFont, Brushes.DarkGray, (float) 2f, (float) 44f );
				graphics.DrawString( s2[c], ftSongListFont, Brushes.White, (float) 0f, (float) 42f );

				txEnumeratingSongs = new CTexture( CDTXMania.app.Device, image, CDTXMania.TextureFormat );

				txEnumeratingSongs.vcScaleRatio = new Vector3( 0.5f, 0.5f, 1f );	// 半分のサイズで表示する。
			}
		}
		catch ( CTextureCreateFailedException )
		{
			Trace.TraceError( "txEnumeratingSongsテクスチャの作成に失敗しました。" );
			txEnumeratingSongs = null;
		}
		#endregion
		#region [ 曲数表示 ]
		txItemNumbers = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\5_skill number on gauge etc.png"), false);
		#endregion
		base.OnManagedCreateResources();
	}
	public override void OnManagedReleaseResources()
	{
		if( bNotActivated )
			return;

		CDTXMania.tDisposeSafely( ref txItemNumbers );

		for( int i = 0; i < 13; i++ )
		{
			CDTXMania.tDisposeSafely(ref stBarInformation[i].txTitleName);
			CDTXMania.tDisposeSafely(ref stBarInformation[i].txPreviewImage);
			CDTXMania.tDisposeSafely(ref stBarInformation[i].txClearLamp);
		}

		CDTXMania.tDisposeSafely( ref txSkillNumbers );
		CDTXMania.tDisposeSafely( ref txEnumeratingSongs );
		CDTXMania.tDisposeSafely( ref txSongNotFound );
		CDTXMania.tDisposeSafely( ref txSongNameBar.Score );
		CDTXMania.tDisposeSafely( ref txSongNameBar.Box );
		CDTXMania.tDisposeSafely( ref txSongNameBar.Other );
		CDTXMania.tDisposeSafely( ref txSongSelectionBar.Score );
		CDTXMania.tDisposeSafely( ref txSongSelectionBar.Box );
		CDTXMania.tDisposeSafely( ref txSongSelectionBar.Other );
		CDTXMania.tDisposeSafely( ref txTopPanel );
		CDTXMania.tDisposeSafely( ref txBottomPanel );

		CDTXMania.tDisposeSafely( ref prvFont );
		CDTXMania.tDisposeSafely( ref prvFontSmall );
            
		if( txSelectedSongName != null )
		{
			txSelectedSongName.Dispose();
			txSelectedSongName = null;
		}
		if( txSelectedArtistName != null )
		{
			txSelectedArtistName.Dispose();
			txSelectedArtistName = null;
		}

		base.OnManagedReleaseResources();
	}
	public override int OnUpdateAndDraw()
	{
		if( bNotActivated )
			return 0;

		#region [ 初めての進行描画 ]
		//-----------------
		if( bJustStartedUpdate )
		{
			for( int i = 0; i < 13; i++ )
				ct登場アニメ用[ i ] = new CCounter( -i * 10, 100, 3, CDTXMania.Timer );

			nScrollTimer = CSoundManager.rcPerformanceTimer.nCurrentTime;
			CDTXMania.stageSongSelection.tSelectedSongChanged();
				
			bJustStartedUpdate = false;
		}
		//-----------------
		#endregion

			
		// まだ選択中の曲が決まってなければ、曲ツリールートの最初の曲にセットする。

		if( ( rSelectedSong == null ) && ( CDTXMania.SongManager.listSongRoot.Count > 0 ) )
			rSelectedSong = CDTXMania.SongManager.listSongRoot[ 0 ];


		// 本ステージは、(1)登場アニメフェーズ → (2)通常フェーズ　と二段階にわけて進む。
		// ２つしかフェーズがないので CStage.ePhaseID を使ってないところがまた本末転倒。

			
		// 進行。

		if( !bAllAnimationsCompleted )
		{
			#region [ (1) 登場アニメフェーズの進行。]
			//-----------------
			for( int i = 0; i < 13; i++ )	// パネルは全13枚。
			{
				ct登場アニメ用[ i ].tUpdate();

				if( ct登場アニメ用[ i ].bReachedEndValue )
					ct登場アニメ用[ i ].tStop();
			}

			// 全部の進行が終わったら、this.b登場アニメ全部完了 を true にする。

			bAllAnimationsCompleted = true;
			for( int i = 0; i < 13; i++ )	// パネルは全13枚。
			{
				if( ct登場アニメ用[ i ].bInProgress )
				{
					bAllAnimationsCompleted = false;	// まだ進行中のアニメがあるなら false のまま。
					break;
				}
			}


			//-----------------
			#endregion
		}
		else
		{
			#region [ (2) 通常フェーズの進行。]
			//-----------------
			long n現在時刻 = CDTXMania.Timer.nCurrentTime;
				
			if( n現在時刻 < nScrollTimer )	// 念のため
				nScrollTimer = n現在時刻;

			const int nアニメ間隔 = 2;
			while( ( n現在時刻 - nScrollTimer ) >= nアニメ間隔 )
			{
				int n加速度 = 1;
				int n残距離 = Math.Abs( (int) ( nTargetScrollCounter - nCurrentScrollCounter ) );

				#region [ 残距離が遠いほどスクロールを速くする（＝n加速度を多くする）。]
				//-----------------
				if( n残距離 <= 100 )
				{
					n加速度 = 2;
				}
				else if( n残距離 <= 300 )
				{
					n加速度 = 3;
				}
				else if( n残距離 <= 500 )
				{
					n加速度 = 4;
				}
				else
				{
					n加速度 = 8;
				}
				//-----------------
				#endregion

				#region [ 加速度を加算し、現在のスクロールカウンタを目標のスクロールカウンタまで近づける。 ]
				//-----------------
				if( nCurrentScrollCounter < nTargetScrollCounter )		// (A) 正の方向に未達の場合：
				{
					nCurrentScrollCounter += n加速度;								// カウンタを正方向に移動する。

					if( nCurrentScrollCounter > nTargetScrollCounter )
						nCurrentScrollCounter = nTargetScrollCounter;	// 到着！スクロール停止！
				}

				else if( nCurrentScrollCounter > nTargetScrollCounter )	// (B) 負の方向に未達の場合：
				{
					nCurrentScrollCounter -= n加速度;								// カウンタを負方向に移動する。

					if( nCurrentScrollCounter < nTargetScrollCounter )	// 到着！スクロール停止！
						nCurrentScrollCounter = nTargetScrollCounter;
				}
				//-----------------
				#endregion

				if( nCurrentScrollCounter >= 100 )		// １行＝100カウント。
				{
					#region [ パネルを１行上にシフトする。]
					//-----------------

					// 選択曲と選択行を１つ下の行に移動。

					rSelectedSong = rNextSong( rSelectedSong );
					nSelectedRow = ( nSelectedRow + 1 ) % 13;


					// 選択曲から７つ下のパネル（＝新しく最下部に表示されるパネル。消えてしまう一番上のパネルを再利用する）に、新しい曲の情報を記載する。

					CSongListNode song = rSelectedSong;
					for( int i = 0; i < 7; i++ )
						song = rNextSong( song );

					int index = ( nSelectedRow + 7 ) % 13;	// 新しく最下部に表示されるパネルのインデックス（0～12）。
					stBarInformation[ index ].strTitleString = song.strタイトル;
					stBarInformation[ index ].colLetter = song.col文字色;
					tGenerateSongNameBar( index, stBarInformation[ index ].strTitleString, stBarInformation[ index ].colLetter );
					stBarInformation[index].eBarType = eGetSongBarType(song);

					int nNearestIndex = n現在のアンカ難易度レベルに最も近い難易度レベルを返す(song);
					//Update Preview Image Path					
					stBarInformation[index].strPreviewImageFullPath = sGetPreviewImagePath(song.arScore[nNearestIndex]);
					//Load the image (NOTE: May have performance issue)
					tGeneratePreviewImageTexture(index, stBarInformation[index].strPreviewImageFullPath, stBarInformation[index].eBarType);
					// stバー情報[] の内容を1行ずつずらす。
					//Update Clear Lamp values
					tUpdateBarClearLampValue(index, song);
					//Draw Clear lamps for new song in list
					tGenerateClearLampTexture(index, stBarInformation[index].nClearLamps);

					// 新しく最下部に表示されるパネル用のスキル値を取得。

					for ( int i = 0; i < 3; i++ )
						stBarInformation[ index ].nSkillValue[ i ] = (int) song.arScore[nNearestIndex].SongInformation.HighSkill[ i ];


					// 1行(100カウント)移動完了。

					nCurrentScrollCounter -= 100;
					nTargetScrollCounter -= 100;

					t選択曲が変更された( false );				// スクロールバー用に今何番目を選択しているかを更新
					if( txSelectedSongName != null )
					{
						txSelectedSongName.Dispose();
						txSelectedSongName = null;
					}
					if( txSelectedArtistName != null )
					{
						txSelectedArtistName.Dispose();
						txSelectedArtistName = null;
					}

					if( nTargetScrollCounter == 0 )
						CDTXMania.stageSongSelection.tSelectedSongChanged();		// スクロール完了＝選択曲変更！

					//-----------------
					#endregion
				}
				else if( nCurrentScrollCounter <= -100 )
				{
					#region [ パネルを１行下にシフトする。]
					//-----------------

					// 選択曲と選択行を１つ上の行に移動。

					rSelectedSong = rPreviousSong( rSelectedSong );
					nSelectedRow = ( ( nSelectedRow - 1 ) + 13 ) % 13;


					// 選択曲から５つ上のパネル（＝新しく最上部に表示されるパネル。消えてしまう一番下のパネルを再利用する）に、新しい曲の情報を記載する。

					CSongListNode song = rSelectedSong;
					for( int i = 0; i < 5; i++ )
						song = rPreviousSong( song );

					int index = ( ( nSelectedRow - 5 ) + 13 ) % 13;	// 新しく最上部に表示されるパネルのインデックス（0～12）。
					stBarInformation[ index ].strTitleString = song.strタイトル;
					stBarInformation[ index ].colLetter = song.col文字色;
					tGenerateSongNameBar( index, stBarInformation[ index ].strTitleString, stBarInformation[ index ].colLetter );
					stBarInformation[index].eBarType = eGetSongBarType(song);

					int nNearestIndex = n現在のアンカ難易度レベルに最も近い難易度レベルを返す(song);
					//Update Preview Image Path						
					stBarInformation[index].strPreviewImageFullPath = sGetPreviewImagePath(song.arScore[nNearestIndex]);
					//Load the image (NOTE: May have performance issue)
					tGeneratePreviewImageTexture(index, stBarInformation[index].strPreviewImageFullPath, stBarInformation[index].eBarType);
					// stバー情報[] の内容を1行ずつずらす。
					//Update Clear Lamp values
					tUpdateBarClearLampValue(index, song);
					//Draw Clear lamps for new song in list
					tGenerateClearLampTexture(index, stBarInformation[index].nClearLamps);

					// 新しく最上部に表示されるパネル用のスキル値を取得。

					for ( int i = 0; i < 3; i++ )
						stBarInformation[ index ].nSkillValue[ i ] = (int) song.arScore[nNearestIndex].SongInformation.HighSkill[ i ];


					// 1行(100カウント)移動完了。

					nCurrentScrollCounter += 100;
					nTargetScrollCounter += 100;

					t選択曲が変更された( false );				// スクロールバー用に今何番目を選択しているかを更新
					if( txSelectedSongName != null )
					{
						txSelectedSongName.Dispose();
						txSelectedSongName = null;
					}
					if( txSelectedArtistName != null )
					{
						txSelectedArtistName.Dispose();
						txSelectedArtistName = null;
					}
						
					if( nTargetScrollCounter == 0 )
						CDTXMania.stageSongSelection.tSelectedSongChanged();		// スクロール完了＝選択曲変更！
					//-----------------
					#endregion
				}

				nScrollTimer += nアニメ間隔;
			}
			//-----------------
			#endregion
		}


		// 描画。

		if( rSelectedSong == null )
		{
			#region [ 曲が１つもないなら「Songs not found.」を表示してここで帰れ。]
			//-----------------
			if ( bIsEnumeratingSongs )
			{
				if ( txEnumeratingSongs != null )
				{
					txEnumeratingSongs.tDraw2D( CDTXMania.app.Device, 800, 280 );
				}
			}
			else
			{
				if ( txSongNotFound != null )
					txSongNotFound.tDraw2D( CDTXMania.app.Device, 800, 280 );
			}
			//-----------------
			#endregion

			return 0;
		}

		int i選曲バーX座標 = 673; //選曲バーの座標用
		int i選択曲バーX座標 = 665; //選択曲バーの座標用

		if( !bAllAnimationsCompleted )
		{
			#region [ (1) 登場アニメフェーズの描画。]
			//-----------------
			for( int i = 0; i < 13; i++ )	// パネルは全13枚。
			{
				if( ct登場アニメ用[ i ].nCurrentValue >= 0 )
				{
					double db割合0to1 = ( (double) ct登場アニメ用[ i ].nCurrentValue ) / 100.0;
					double db回転率 = Math.Sin( Math.PI * 3 / 5 * db割合0to1 );
					int nパネル番号 = ( ( ( nSelectedRow - 5 ) + i ) + 13 ) % 13;
						
					if( i == 5 )
					{
						// (A) 選択曲パネルを描画。

						#region [ バーテクスチャを描画。]
						//-----------------
						int width = (int) ( 425.0 / Math.Sin( Math.PI * 3 / 5 ) );
						int x = 665 - ( (int) ( width * db回転率 ) );
						int y = 269;
						tDrawBar(i選択曲バーX座標, y - 30, stBarInformation[nパネル番号].eBarType, true);
						//-----------------
						#endregion
						#region [ タイトル名テクスチャを描画。]
						//-----------------
						Point titleOffsets = new Point(0, 0);
						if( stBarInformation[ nパネル番号 ].txTitleName != null )
							stBarInformation[ nパネル番号 ].txTitleName.tDraw2D(CDTXMania.app.Device, i選択曲バーX座標 + 55 + titleOffsets.X, y + titleOffsets.Y);
						//-----------------
						#endregion
						#region [ Draw Preview Image ]
						if (stBarInformation[nパネル番号].txPreviewImage != null)
							stBarInformation[nパネル番号].txPreviewImage.tDraw2D(CDTXMania.app.Device, i選択曲バーX座標 + 7, y - 3);
						#endregion
						#region [Draw Clear Lamps]
						if (stBarInformation[nパネル番号].txClearLamp != null)
							stBarInformation[nパネル番号].txClearLamp.tDraw2D(CDTXMania.app.Device, i選択曲バーX座標, y + 1);
						#endregion
					}
					else
					{
						// (B) その他のパネルの描画。

						#region [ バーテクスチャの描画。]
						//-----------------
						int width = (int) ( ( (double) ( ( 720 - ptバーの基本座標[ i ].X ) + 1 ) ) / Math.Sin( Math.PI * 3 / 5 ) );
//							int x = 720 - ( (int) ( width * db回転率 ) );
						int x = i選曲バーX座標 + 500 - (int)(db割合0to1 * 500);
						int y = ptバーの基本座標[ i ].Y;
						tDrawBar( x, y, stBarInformation[ nパネル番号 ].eBarType, false );
						//-----------------
						#endregion
						#region [ タイトル名テクスチャを描画。]
						//-----------------
						Point titleOffsets = new Point(0, 0);
						if ( stBarInformation[ nパネル番号 ].txTitleName != null )
							stBarInformation[ nパネル番号 ].txTitleName.tDraw2D( CDTXMania.app.Device, x + 78 + titleOffsets.X, y + 5 + titleOffsets.Y);
						//-----------------
						#endregion
						#region [ Draw Preview Image ]
						if (stBarInformation[nパネル番号].txPreviewImage != null)
							stBarInformation[nパネル番号].txPreviewImage.tDraw2D(CDTXMania.app.Device, x + 31, y + 2);
						#endregion
						#region [Draw Clear Lamps]
						if (stBarInformation[nパネル番号].txClearLamp != null)
							stBarInformation[nパネル番号].txClearLamp.tDraw2D(CDTXMania.app.Device, x + 24, y + 6);
						#endregion
					}
					if (txTopPanel != null)
						txTopPanel.tDraw2DFloat(CDTXMania.app.Device, 0f, ((float)(txTopPanel.szTextureSize.Height) * ((float)(ct登場アニメ用[0].nCurrentValue) / 100f)) - (float)(txTopPanel.szTextureSize.Height));
					if (txBottomPanel != null)
						txBottomPanel.tDraw2DFloat(CDTXMania.app.Device, 0f, 720 - ((float)(txBottomPanel.szTextureSize.Height) * ((float)(ct登場アニメ用[0].nCurrentValue) / 100f)));
				}
			}
			//-----------------
			#endregion
		}
		else
		{
			#region [ (2) 通常フェーズの描画。]
			//-----------------
			for( int i = 0; i < 13; i++ )	// パネルは全13枚。
			{
				if( ( i == 0 && nCurrentScrollCounter > 0 ) ||		// 最上行は、上に移動中なら表示しない。
				    ( i == 12 && nCurrentScrollCounter < 0 ) )		// 最下行は、下に移動中なら表示しない。
					continue;

				int nパネル番号 = ( ( ( nSelectedRow - 5 ) + i ) + 13 ) % 13;
				int n見た目の行番号 = i;
				int n次のパネル番号 = ( nCurrentScrollCounter <= 0 ) ? ( ( i + 1 ) % 13 ) : ( ( ( i - 1 ) + 13 ) % 13 );
//					int x = this.ptバーの基本座標[ n見た目の行番号 ].X + ( (int) ( ( this.ptバーの基本座標[ n次のパネル番号 ].X - this.ptバーの基本座標[ n見た目の行番号 ].X ) * ( ( (double) Math.Abs( this.nCurrentScrollCounter ) ) / 100.0 ) ) );
				int x = i選曲バーX座標;
				int y = ptバーの基本座標[ n見た目の行番号 ].Y + ( (int) ( ( ptバーの基本座標[ n次のパネル番号 ].Y - ptバーの基本座標[ n見た目の行番号 ].Y ) * ( ( (double) Math.Abs( nCurrentScrollCounter ) ) / 100.0 ) ) );

				if( ( i == 5 ) && ( nCurrentScrollCounter == 0 ) )
				{
					// (A) スクロールが停止しているときの選択曲バーの描画。

					int y選曲 = 269;

					#region [ バーテクスチャを描画。]
					//-----------------
					tDrawBar(i選択曲バーX座標, y選曲 - 30, stBarInformation[nパネル番号].eBarType, true);
					//-----------------
					#endregion
					#region [ Draw Preview Image ]
					if (stBarInformation[nパネル番号].txPreviewImage != null)
						stBarInformation[nパネル番号].txPreviewImage.tDraw2D(CDTXMania.app.Device, i選択曲バーX座標 + 7, y選曲 - 3);
					#endregion
					#region [Draw Clear Lamps]
					if (stBarInformation[nパネル番号].txClearLamp != null)
						stBarInformation[nパネル番号].txClearLamp.tDraw2D(CDTXMania.app.Device, i選択曲バーX座標, y選曲 + 1);
					#endregion
					#region [ タイトル名テクスチャを描画。]
					//-----------------
					Point titleOffsets = new Point(0, 0);
					if ( stBarInformation[ nパネル番号 ].txTitleName != null )
						stBarInformation[ nパネル番号 ].txTitleName.tDraw2D( CDTXMania.app.Device, i選択曲バーX座標 + 55 + titleOffsets.X, y選曲 + titleOffsets.Y);

					if (CDTXMania.stageSongSelection.r現在選択中の曲.eNodeType == CSongListNode.ENodeType.SCORE && actステータスパネル.txパネル本体 == null)
					{
						if (txSelectedSongName == null)
							txSelectedSongName = tGenerateTextTexture(CDTXMania.stageSongSelection.rSelectedScore.SongInformation.Title);
						if (txSelectedSongName != null)
						{
							if (txSelectedSongName.szImageSize.Width > 600)
								txSelectedSongName.vcScaleRatio.X = 600f / txSelectedSongName.szImageSize.Width;
                                
							txSelectedSongName.tDraw2D(CDTXMania.app.Device, 60, 490);
						}

						if ( txSelectedArtistName == null )
							txSelectedArtistName = tGenerateTextTexture_Small( CDTXMania.stageSongSelection.rSelectedScore.SongInformation.ArtistName );
						if ( txSelectedArtistName != null )
						{
							if ( txSelectedArtistName.szImageSize.Width > 600 )
								txSelectedArtistName.vcScaleRatio.X = 600f / txSelectedArtistName.szImageSize.Width;

							txSelectedArtistName.tDraw2D( CDTXMania.app.Device, 60, 545 );
						}
					}

					//-----------------
					#endregion
				}
				else
				{
					// (B) スクロール中の選択曲バー、またはその他のバーの描画。

					#region [ バーテクスチャを描画。]
					//-----------------
					tDrawBar( x, y, stBarInformation[ nパネル番号 ].eBarType, false );
					//-----------------
					#endregion
					#region [ タイトル名テクスチャを描画。]
					//-----------------
					Point titleOffsets = new Point(0, 0);
					if ( stBarInformation[ nパネル番号 ].txTitleName != null )
						stBarInformation[ nパネル番号 ].txTitleName.tDraw2D( CDTXMania.app.Device, x + 78 + titleOffsets.X, y + 5 + titleOffsets.Y);
					//-----------------
					#endregion
					#region [ Draw Preview Image ]
					if (stBarInformation[nパネル番号].txPreviewImage != null)
						stBarInformation[nパネル番号].txPreviewImage.tDraw2D(CDTXMania.app.Device, x + 31, y + 2);
					#endregion
					#region [Draw Clear Lamps]
					if (stBarInformation[nパネル番号].txClearLamp != null)
						stBarInformation[nパネル番号].txClearLamp.tDraw2D(CDTXMania.app.Device, x + 24, y + 6);
					#endregion
				}
			}
			//-----------------
			#endregion
			if( txTopPanel != null )
				txTopPanel.tDraw2D( CDTXMania.app.Device, 0, 0 );
			if( txBottomPanel != null )
				txBottomPanel.tDraw2D( CDTXMania.app.Device, 0, 720 - txBottomPanel.szTextureSize.Height );

		}
		#region [ スクロール地点の計算(描画はCActSelectShowCurrentPositionにて行う) #27648 ]
		int py;
		double d = 0;
		if ( nNumOfItems > 1 )
		{
			d = ( 492 - 12 ) / (double) ( nNumOfItems - 1 );
			py = (int) ( d * ( nCurrentPosition - 1 ) );
		}
		else
		{
			d = 0;
			py = 0;
		}
		int delta = (int) ( d * nCurrentScrollCounter / 100 );
		if ( py + delta <= 492 - 12 )
		{
			nスクロールバー相対y座標 = py + delta;
		}
		#endregion

		#region [ アイテム数の描画 #27648 ]
		tアイテム数の描画();
		#endregion
		return 0;
	}
		

	// Other

	#region [ private ]
	//-----------------
	private enum EBarType { Score, Box, Other }  // Eバー種別

	private struct STBar  // STバー
	{
		public CTexture Score;
		public CTexture Box;
		public CTexture Other;
		public CTexture this[ int index ]
		{
			get
			{
				switch( index )
				{
					case 0:
						return Score;

					case 1:
						return Box;

					case 2:
						return Other;
				}
				throw new IndexOutOfRangeException();
			}
			set
			{
				switch( index )
				{
					case 0:
						Score = value;
						return;

					case 1:
						Box = value;
						return;

					case 2:
						Other = value;
						return;
				}
				throw new IndexOutOfRangeException();
			}
		}
	}

	private struct STBarInformation  // STバー情報
	{
		public EBarType eBarType;  // eバー種別
		public string strTitleString;  // strタイトル文字列
		public CTexture txTitleName;  // タイトル名
		public STDGBVALUE<int> nSkillValue;  // nスキル値
		public Color colLetter;  // col文字色
		//
		public CTexture txPreviewImage;// txプレビュー画像
		public CTexture txClearLamp;// txクリアランプ
		public string strPreviewImageFullPath; // 
		public STDGBVALUE<int[]> nClearLamps;
	}

	private struct STSongSelectionBar  // ST選曲バー
	{
		public CTexture Score;
		public CTexture Box;
		public CTexture Other;
		public CTexture this[ int index ]
		{
			get
			{
				switch( index )
				{
					case 0:
						return Score;

					case 1:
						return Box;

					case 2:
						return Other;
				}
				throw new IndexOutOfRangeException();
			}
			set
			{
				switch( index )
				{
					case 0:
						Score = value;
						return;

					case 1:
						Box = value;
						return;

					case 2:
						Other = value;
						return;
				}
				throw new IndexOutOfRangeException();
			}
		}
	}
	[StructLayout(LayoutKind.Sequential)]
	public struct STATUSPANEL
	{
		public string label;
		public int status;
	}
	public int nIndex;
	public STATUSPANEL[] stパネルマップ;

	private bool bAllAnimationsCompleted;  // b登場アニメ全部完了
	private Color color文字影 = Color.FromArgb( 0x40, 10, 10, 10 );
	private CCounter[] ct登場アニメ用 = new CCounter[ 13 ];
	private EInstrumentPart eInstrumentPart;  // e楽器パート
	private Font ftSongListFont;           // ft曲リスト用フォント
	private long nScrollTimer;             // nスクロールタイマ
	private int nCurrentScrollCounter;
	private int nSelectedRow;              // n現在の選択行
	private int nTargetScrollCounter;
	private readonly Point[] ptバーの基本座標 = new Point[] { new Point(0x2c4, 5), new Point(0x272, 56), new Point(0x242, 107), new Point(0x222, 158), new Point(0x210, 209), new Point(0x1d0, 270), new Point(0x224, 362), new Point(0x242, 413), new Point(0x270, 464), new Point(0x2ae, 515), new Point(0x314, 566), new Point(0x3e4, 617), new Point(0x500, 668) };
	private STBarInformation[] stBarInformation = new STBarInformation[ 13 ];  // stバー情報
	private CTexture txSongNotFound, txEnumeratingSongs;
	private CTexture txSkillNumbers;       // txスキル数字
	private CTexture txItemNumbers;        // txアイテム数数字
	private CTexture txSelectedSongName;
	private CTexture txSelectedArtistName;
	private CTexture txTopPanel;           // tx上部パネル
	private CTexture txBottomPanel;        // tx下部パネル
	private CActSelectStatusPanel actステータスパネル;
	private STBar txSongNameBar;           // tx曲名バー
	private STSongSelectionBar txSongSelectionBar;  // tx選曲バー
	private CPrivateFastFont prvFont;
	private CPrivateFastFont prvFontSmall;
	private string strDefaultPreImage;

	//2014.04.05.kairera0467 GITADORAグラデーションの色。
	//本当は共通のクラスに設置してそれを参照する形にしたかったが、なかなかいいメソッドが無いため、とりあえず個別に設置。
	public Color clGITADORAgradationTopColor = Color.FromArgb(0, 220, 200);
	public Color clGITADORAgradationBottomColor = Color.FromArgb(255, 250, 40);

	private int nCurrentPosition = 0;
	private int nNumOfItems = 0;

	//private string strBoxDefSkinPath = "";
	private EBarType eGetSongBarType( CSongListNode song)  // e曲のバー種別を返す
	{
		if( song != null )
		{
			switch( song.eNodeType )
			{
				case CSongListNode.ENodeType.SCORE:
				case CSongListNode.ENodeType.SCORE_MIDI:
					return EBarType.Score;

				case CSongListNode.ENodeType.BOX:
				case CSongListNode.ENodeType.BACKBOX:
					return EBarType.Box;
			}
		}
		return EBarType.Other;
	}

	private string sGetPreviewImagePath(CScore score)
	{
		string previewImagePath = score.SongInformation.Preimage;
		if (!string.IsNullOrEmpty(previewImagePath))
		{
			previewImagePath = Path.Combine(
				score.FileInformation.AbsoluteFolderPath,
				previewImagePath
			);
		}
		else
		{
			previewImagePath = "";
		}
		return previewImagePath;
	}

	private CSongListNode rNextSong( CSongListNode song)  // r次の曲
	{
		if( song == null )
			return null;

		List<CSongListNode> list = (song.r親ノード != null ) ? song.r親ノード.list子リスト : CDTXMania.SongManager.listSongRoot;
	
		int index = list.IndexOf( song );

		if( index < 0 )
			return null;

		if( index == ( list.Count - 1 ) )
			return list[ 0 ];

		return list[ index + 1 ];
	}
	private CSongListNode rPreviousSong( CSongListNode song)  // r前の曲
	{
		if( song == null )
			return null;

		List<CSongListNode> list = (song.r親ノード != null ) ? song.r親ノード.list子リスト : CDTXMania.SongManager.listSongRoot;

		int index = list.IndexOf( song );
	
		if( index < 0 )
			return null;

		if( index == 0 )
			return list[ list.Count - 1 ];

		return list[ index - 1 ];
	}
	private void tInitializeBar()  // tバーの初期化
	{
		CSongListNode song = rSelectedSong;
						
		if( song == null )
			return;

		for( int i = 0; i < 5; i++ )
			song = rPreviousSong( song );			

		for( int i = 0; i < 13; i++ )
		{
			stBarInformation[ i ].strTitleString = song.strタイトル;
			stBarInformation[ i ].colLetter = song.col文字色;
			stBarInformation[ i ].eBarType = eGetSongBarType( song );
				
			int nNearestScoreIndex = n現在のアンカ難易度レベルに最も近い難易度レベルを返す(song);				

			for ( int j = 0; j < 3; j++ )
				stBarInformation[ i ].nSkillValue[ j ] = (int) song.arScore[nNearestScoreIndex].SongInformation.HighSkill[ j ];

			//
			stBarInformation[i].strPreviewImageFullPath = sGetPreviewImagePath(song.arScore[nNearestScoreIndex]);
			//
			tUpdateBarClearLampValue(i, song);			

			song = rNextSong( song );
		}			

		nSelectedRow = 5;
	}

	private void tUpdateBarClearLampValue(int index, CSongListNode cSong)
	{
		for (int j = 0; j < 3; j++)
		{
			stBarInformation[index].nClearLamps[j] = new int[5] { 0, 0, 0, 0, 0 };

			for (int k = 0; k < 5; k++)
			{
				if (cSong.arScore[k] != null)
				{
					if (cSong.arScore[k].SongInformation.HighSkill[j] > 0.0)
					{
						if(cSong.arDifficultyLabel[k] != null)
						{
							stBarInformation[index].nClearLamps[j][k] = 1;
						}
						else
						{
							//Use 2 as value for uncategorised clear
							stBarInformation[index].nClearLamps[j][k] = 2;
						}
							
					}
				}
			}
		}
	}

	private void tDrawBar( int x, int y, EBarType type, bool b選択曲)  // tバーの描画
	{
		if( x >= SampleFramework.GameFramebufferSize.Width || y >= SampleFramework.GameFramebufferSize.Height )
			return;

		if (b選択曲)
		{
			#region [ (A) 選択曲の場合 ]
			//-----------------
			if (txSongSelectionBar[(int)type] != null)
				txSongSelectionBar[(int)type].tDraw2D(CDTXMania.app.Device, x, y);	// ヘサキ
			//-----------------
			#endregion
		}
		else
		{
			#region [ (B) その他の場合 ]
			//-----------------
			if (txSongNameBar[(int)type] != null)
				txSongNameBar[(int)type].tDraw2D(CDTXMania.app.Device, x, y);		// ヘサキ
			//-----------------
			#endregion
		}
	}
	private CTexture tGenerateTextTexture( string str文字)  // t指定された文字テクスチャを生成する
	{
		//2013.09.05.kairera0467 中央にしか使用することはないので、色は黒固定。
		//現在は機能しない(面倒なので実装してない)が、そのうち使用する予定。
		//PrivateFontの試験運転も兼ねて。
		//CPrivateFastFont
		//if(prvFont != null)
		//    prvFont.Dispose();
            
		Bitmap bmp;
		bmp = prvFont.DrawPrivateFont( str文字, CPrivateFont.DrawMode.Edge, Color.Black, Color.Black, clGITADORAgradationTopColor, clGITADORAgradationBottomColor, true);
		CTexture tx文字テクスチャ = CDTXMania.tGenerateTexture( bmp, false );
		bmp.Dispose();

		return tx文字テクスチャ;
	}
	private CTexture tGenerateTextTexture_Small( string str文字)  // t指定された文字テクスチャを生成する_小
	{
		Bitmap bmp;
		bmp = prvFontSmall.DrawPrivateFont( str文字, CPrivateFont.DrawMode.Edge, Color.Black, Color.Black, clGITADORAgradationTopColor, clGITADORAgradationBottomColor, true);
		CTexture tx文字テクスチャ = CDTXMania.tGenerateTexture( bmp, false );
		bmp.Dispose();

		return tx文字テクスチャ;
	}
	private void tGenerateSongNameBar( int nバー番号, string strSongName, Color color)  // t曲名バーの生成
	{
		if( nバー番号 < 0 || nバー番号 > 12 )
			return;

		try
		{
			SizeF sz曲名;

			#region [ 曲名表示に必要となるサイズを取得する。]
			//-----------------
			using( var bmpDummy = new Bitmap( 1, 1 ) )
			{
				var g = Graphics.FromImage( bmpDummy );
				g.PageUnit = GraphicsUnit.Pixel;
				sz曲名 = g.MeasureString( strSongName, ftSongListFont );

				g.Dispose();
			}
			//-----------------
			#endregion

			int n最大幅px = 510;
			int height = 0x25;
			int width = (int) ( ( sz曲名.Width + 2 ) * 0.5f );
			if( width > ( CDTXMania.app.Device.Capabilities.MaxTextureWidth / 2 ) )
				width = CDTXMania.app.Device.Capabilities.MaxTextureWidth / 2;	// 右端断ち切れ仕方ないよね

			float f拡大率X = ( width <= n最大幅px ) ? 0.5f : ( ( (float) n最大幅px / (float) width ) * 0.5f );	// 長い文字列は横方向に圧縮。

			using( var bmp = new Bitmap( width * 2, height * 2, PixelFormat.Format32bppArgb ) )		// 2倍（面積4倍）のBitmapを確保。（0.5倍で表示する前提。）
			using( var g = Graphics.FromImage( bmp ) )
			{
				g.TextRenderingHint = TextRenderingHint.AntiAlias;
				float y = ( ( ( float ) bmp.Height ) / 2f ) - ( ( CDTXMania.ConfigIni.n選曲リストフォントのサイズdot * 2f ) / 2f );
				g.DrawString( strSongName, ftSongListFont, new SolidBrush( color文字影 ), (float) 2f, (float) ( y + 2f ) );
				g.DrawString( strSongName, ftSongListFont, new SolidBrush( color ), 0f, y );

				CDTXMania.tDisposeSafely( ref stBarInformation[ nバー番号 ].txTitleName );

				stBarInformation[ nバー番号 ].txTitleName = new CTexture( CDTXMania.app.Device, bmp, CDTXMania.TextureFormat );
				stBarInformation[ nバー番号 ].txTitleName.vcScaleRatio = new Vector3( f拡大率X, 0.5f, 1f );

				g.Dispose();
			}
		}
		catch( CTextureCreateFailedException )
		{
			Trace.TraceError( "曲名テクスチャの作成に失敗しました。[{0}]", strSongName );
			stBarInformation[ nバー番号 ].txTitleName = null;
		}
	}

	private void tGeneratePreviewImageTexture(int nBarIndex, string strPreviewImagePath, EBarType eBarType)
	{
		if (nBarIndex < 0 || nBarIndex > 12)
			return;

		try
		{
			CDTXMania.tDisposeSafely(ref stBarInformation[nBarIndex].txPreviewImage);
			string strSelectedPreviewImagePath = strPreviewImagePath;
			if(!File.Exists(strSelectedPreviewImagePath))
			{
				strSelectedPreviewImagePath = strDefaultPreImage;
			}

			if (File.Exists(strSelectedPreviewImagePath))
			{
				Bitmap bitmap = new Bitmap(44, 44);
				using (Graphics graphics = Graphics.FromImage(bitmap))
				{
					using (Bitmap preImage = new Bitmap(strSelectedPreviewImagePath))
					{
						//Set default value first
						float imageWidth = 44.0f;
						float imageHeight = 44.0f;
						//Calculate imageAspect
						float imageAspect = (float)preImage.Width / preImage.Height;
						if (imageAspect > 1.0)
						{
							// Fit based on width                                
							imageHeight = (int)(imageWidth / imageAspect);
						}
						else
						{
							// Fit based on height                                
							imageWidth = (int)(imageHeight * imageAspect);
						}

						// Calculate position to center the image in the frame
						int imageX = (int)((44.0f - imageWidth) / 2);
						int imageY = (int)((44.0f - imageHeight) / 2);

						graphics.SmoothingMode = SmoothingMode.HighQuality;
						graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
							
						graphics.DrawImage(preImage, imageX, imageY, imageWidth, imageHeight);
					}

					stBarInformation[nBarIndex].txPreviewImage = CDTXMania.tGenerateTexture(bitmap);
				}
				bitmap.Dispose();
			}
		}
		catch (CTextureCreateFailedException)
		{
			Trace.TraceError("Fail to load preview image for Bar[{0}]", nBarIndex);
			stBarInformation[nBarIndex].txPreviewImage = null;
		}
	}

	private void tGenerateClearLampTexture(int nBarIndex, STDGBVALUE<int[]> sClearLampValues)
	{
		if (nBarIndex < 0 || nBarIndex > 12)
			return;

		//Decide to show Drum, Guitar or Bass based on current configuration
		//0 for Drums, 1 for Guitar, 2 for Bass
		int nDGBmode = (CDTXMania.ConfigIni.bDrumsEnabled ? 0 : 1);
		if (CDTXMania.ConfigIni.bIsSwappedGuitarBass)
		{
			nDGBmode = 2;
		}

		if(sClearLampValues[nDGBmode] != null)
		{
			try
			{
				CDTXMania.tDisposeSafely(ref stBarInformation[nBarIndex].txClearLamp);

				Bitmap bitmap = new Bitmap(7, 41);
				SolidBrush[] lampBrushes = {
					new SolidBrush(Color.FromArgb(255, 148, 215, 255)),
					new SolidBrush(Color.FromArgb(255, 255, 239, 65)),
					new SolidBrush(Color.FromArgb(255, 255, 65, 116)),
					new SolidBrush(Color.FromArgb(255, 255, 66, 255)),
					new SolidBrush(Color.FromArgb(255, 255, 255, 255))
				};
				using (Graphics graphics = Graphics.FromImage(bitmap))
				{
					for (int i = 0; i < 5; i++)
					{
						if (sClearLampValues[nDGBmode][i] == 1)
						{
							graphics.FillRectangle(lampBrushes[i], 1, 33 - 8 * i, 5, 7);
						}
						else if (sClearLampValues[nDGBmode][i] == 2)
						{
							graphics.FillRectangle(lampBrushes[4], 1, 1, 5, 7);
						}
					}
					stBarInformation[nBarIndex].txClearLamp = CDTXMania.tGenerateTexture(bitmap);
				}
				bitmap.Dispose();
				for (int i = 0; i < 5; i++)
				{
					lampBrushes[i].Dispose();
				}
			}
			catch (CTextureCreateFailedException)
			{
				Trace.TraceError("Fail to generate Clear Lamp Texture Bar[{0}]", nBarIndex);
				stBarInformation[nBarIndex].txClearLamp = null;
			}
		}
	}


	private void tアイテム数の描画()
	{
		string s = nCurrentPosition + "/" + nNumOfItems;
		int x = 1260;
		int y = 620;

		for (int p = s.Length - 1; p >= 0; p--)
		{
			tアイテム数の描画_１桁描画(x, y, s[p]);
			x -= 16;
		}
	}
	private void tアイテム数の描画_１桁描画(int x, int y, char s数値)
	{
		int dx, dy;
		if (s数値 == '/')
		{
			dx = 96;
			dy = 0;
		}
		else
		{
			int n = s数値 - '0';
			dx = (n % 6) * 16;
			dy = (n / 6) * 16;
		}
		if (txItemNumbers != null)
		{
			txItemNumbers.tDraw2D(CDTXMania.app.Device, x, y, new Rectangle(dx, dy, 16, 16));
		}
	}
	//-----------------
	#endregion
}