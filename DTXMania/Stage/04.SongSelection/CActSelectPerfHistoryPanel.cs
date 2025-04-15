using System.Diagnostics;
using System.Drawing;
using DTXMania.Core;
using SharpDX;
using FDK;

namespace DTXMania;

internal class CActSelectPerfHistoryPanel : CActivity
{
	// メソッド

	public CActSelectPerfHistoryPanel()
	{
		listChildActivities.Add( actステータスパネル = new CActSelectStatusPanel() );
		bActivated = false;
	}
	public void t選択曲が変更された()
	{
		CScore cスコア = CDTXMania.stageSongSelection.rSelectedScore;
		if( ( cスコア != null ) && !CDTXMania.stageSongSelection.bScrolling )
		{
			try
			{
				Bitmap image = new Bitmap( 800, 0xc3 );
				Graphics graphics = Graphics.FromImage( image );
				graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
				for ( int i = 0; i < 5; i++ )
				{
					if( ( cスコア.SongInformation.PerformanceHistory[ i ] != null ) && ( cスコア.SongInformation.PerformanceHistory[ i ].Length > 0 ) )
					{
						graphics.DrawString( cスコア.SongInformation.PerformanceHistory[ i ], ft表示用フォント, Brushes.Yellow, (float) 0f, (float) ( i * 36f ) );
					}
				}
				graphics.Dispose();
				if( tx文字列パネル != null )
				{
					tx文字列パネル.Dispose();
				}
				tx文字列パネル = new CTexture( CDTXMania.app.Device, image, CDTXMania.TextureFormat );
				tx文字列パネル.vcScaleRatio = new Vector3( 0.5f, 0.5f, 1f );
				image.Dispose();
			}
			catch( CTextureCreateFailedException )
			{
				Trace.TraceError( "演奏履歴文字列テクスチャの作成に失敗しました。" );
				tx文字列パネル = null;
			}
		}
	}


	// CActivity 実装

	public override void OnActivate()
	{
		ft表示用フォント = new Font( "メイリオ", 26f, FontStyle.Bold, GraphicsUnit.Pixel );
		base.OnActivate();
	}
	public override void OnDeactivate()
	{
		if( ft表示用フォント != null )
		{
			ft表示用フォント.Dispose();
			ft表示用フォント = null;
		}
		ct登場アニメ用 = null;
		base.OnDeactivate();
	}
	public override void OnManagedCreateResources()
	{
		if( bActivated )
		{
			txパネル本体 = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\5_play history panel.png" ), true );
			t選択曲が変更された();
			base.OnManagedCreateResources();
		}
	}
	public override void OnManagedReleaseResources()
	{
		if( bActivated )
		{
			CDTXMania.tReleaseTexture( ref txパネル本体 );
			CDTXMania.tReleaseTexture( ref tx文字列パネル );
			base.OnManagedReleaseResources();
		}
	}
	public override int OnUpdateAndDraw()
	{
		if( bActivated )
		{
			if( bJustStartedUpdate )
			{
				ct登場アニメ用 = new CCounter( 0, 100, 5, CDTXMania.Timer );
				bJustStartedUpdate = false;
			}
			ct登場アニメ用.tUpdate();

			if ( actステータスパネル.txパネル本体 != null )
				n本体X = 700;
			else
				n本体X = 210;

			if( ct登場アニメ用.bReachedEndValue )
			{
				n本体Y = 0x23a;
			}
			else
			{
				double num = ( (double) ct登場アニメ用.nCurrentValue ) / 100.0;
				double num2 = Math.Cos( ( 1.5 + ( 0.5 * num ) ) * Math.PI );
				n本体Y = 0x23a + ( (int) ( txパネル本体.szImageSize.Height * ( 1.0 - ( num2 * num2 ) ) ) );
			}

			if( txパネル本体 != null )
			{
				txパネル本体.tDraw2D( CDTXMania.app.Device, n本体X, n本体Y );

				if ( tx文字列パネル != null )
					tx文字列パネル.tDraw2D( CDTXMania.app.Device, n本体X + 18, n本体Y + 0x20 );
			}
		}
		return 0;
	}
		

	// Other

	#region [ private ]
	//-----------------
	private CCounter ct登場アニメ用;
	private Font ft表示用フォント;
	private int n本体X;
	private int n本体Y;
	private CTexture txパネル本体;
	private CTexture tx文字列パネル;
	private CActSelectStatusPanel actステータスパネル;
	//-----------------
	#endregion
}