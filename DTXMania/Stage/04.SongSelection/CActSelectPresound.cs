using System.Diagnostics;
using DTXMania.Core;
using FDK;

namespace DTXMania;

internal class CActSelectPresound : CActivity
{
	// メソッド

	public CActSelectPresound()
	{
		bActivated = false;
	}
	public void tサウンド停止()
	{
		if( sound != null )
		{
			sound.tStopPlayback();
			CDTXMania.SoundManager.tDiscard( sound );
			sound = null;
		}
	}
	public void t選択曲が変更された()
	{
		CScore cスコア = CDTXMania.stageSongSelection.rSelectedScore;
		if( ( cスコア != null ) && ( ( !( cスコア.FileInformation.AbsoluteFolderPath + cスコア.SongInformation.Presound ).Equals( str現在のファイル名 ) || ( sound == null ) ) || !sound.b再生中 ) )
		{
			tサウンド停止();
			tBGMフェードイン開始();
			if( ( cスコア.SongInformation.Presound != null ) && ( cスコア.SongInformation.Presound.Length > 0 ) )
			{
				ct再生待ちウェイト = new CCounter( 0, CDTXMania.ConfigIni.n曲が選択されてからプレビュー音が鳴るまでのウェイトms, 1, CDTXMania.Timer );
			}
		}
	}


	// CActivity 実装

	public override void OnActivate()
	{
		sound = null;
		str現在のファイル名 = "";
		ct再生待ちウェイト = null;
		ctBGMフェードアウト用 = null;
		ctBGMフェードイン用 = null;
		base.OnActivate();
	}
	public override void OnDeactivate()
	{
		tサウンド停止();
		ct再生待ちウェイト = null;
		ctBGMフェードイン用 = null;
		ctBGMフェードアウト用 = null;
		base.OnDeactivate();
	}
	public override int OnUpdateAndDraw()
	{
		if( bActivated )
		{
			if( ( ctBGMフェードイン用 != null ) && ctBGMフェードイン用.bInProgress )
			{
				ctBGMフェードイン用.tUpdate();
				CDTXMania.Skin.bgm選曲画面.n音量_現在のサウンド = ctBGMフェードイン用.nCurrentValue;
				if( ctBGMフェードイン用.bReachedEndValue )
				{
					ctBGMフェードイン用.tStop();
				}
			}
			if( ( ctBGMフェードアウト用 != null ) && ctBGMフェードアウト用.bInProgress )
			{
				ctBGMフェードアウト用.tUpdate();
				CDTXMania.Skin.bgm選曲画面.n音量_現在のサウンド = 100 - ctBGMフェードアウト用.nCurrentValue;
				if( ctBGMフェードアウト用.bReachedEndValue )
				{
					ctBGMフェードアウト用.tStop();
				}
			}
			t進行処理_プレビューサウンド();
		}
		return 0;
	}


	// Other

	#region [ private ]
	//-----------------
	private CCounter ctBGMフェードアウト用;
	private CCounter ctBGMフェードイン用;
	private CCounter ct再生待ちウェイト;
	private CSound sound;
	private string str現在のファイル名;
		
	private void tBGMフェードアウト開始()
	{
		if( ctBGMフェードイン用 != null )
		{
			ctBGMフェードイン用.tStop();
		}
		ctBGMフェードアウト用 = new CCounter( 0, 100, 10, CDTXMania.Timer );
		ctBGMフェードアウト用.nCurrentValue = 100 - CDTXMania.Skin.bgm選曲画面.n音量_現在のサウンド;
	}
	private void tBGMフェードイン開始()
	{
		if( ctBGMフェードアウト用 != null )
		{
			ctBGMフェードアウト用.tStop();
		}
		ctBGMフェードイン用 = new CCounter( 0, 100, 20, CDTXMania.Timer );
		ctBGMフェードイン用.nCurrentValue = CDTXMania.Skin.bgm選曲画面.n音量_現在のサウンド;
	}
	private void tプレビューサウンドの作成()
	{
		CScore cスコア = CDTXMania.stageSongSelection.rSelectedScore;
		if( ( cスコア != null ) && !string.IsNullOrEmpty( cスコア.SongInformation.Presound ) )
		{
			string strPreviewFilename = cスコア.FileInformation.AbsoluteFolderPath + cスコア.SongInformation.Presound;
			try
			{
				sound = CDTXMania.SoundManager.tGenerateSound( strPreviewFilename );
				sound.nVolume = 80;	// CDTXMania.ConfigIni.n自動再生音量;			// #25217 changed preview volume from AutoVolume
				sound.tStartPlaying( true );
				str現在のファイル名 = strPreviewFilename;
				tBGMフェードアウト開始();
				Trace.TraceInformation( "プレビューサウンドを生成しました。({0})", strPreviewFilename );
			}
			catch
			{
				Trace.TraceError( "プレビューサウンドの生成に失敗しました。({0})", strPreviewFilename );
				if( sound != null )
				{
					sound.Dispose();
				}
				sound = null;
			}
		}
	}
	private void t進行処理_プレビューサウンド()
	{
		if( ( ct再生待ちウェイト != null ) && !ct再生待ちウェイト.bStopped )
		{
			ct再生待ちウェイト.tUpdate();
			if( !ct再生待ちウェイト.b終了値に達してない )
			{
				ct再生待ちウェイト.tStop();
				if( !CDTXMania.stageSongSelection.bScrolling )
				{
					tプレビューサウンドの作成();
				}
			}
		}
	}
	//-----------------
	#endregion
}