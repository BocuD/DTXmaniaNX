using System.Drawing;
using System.Diagnostics;
using DTXMania.Core;
using SharpDX;
using FDK;

using Rectangle = System.Drawing.Rectangle;

namespace DTXMania;

internal class CActResultSongBar : CActivity
{
	// コンストラクタ

	public CActResultSongBar()
	{
		bActivated = false;
	}


	// メソッド

	public void tアニメを完了させる()
	{
		ct登場用.nCurrentValue = ct登場用.nEndValue;
	}


	// CActivity 実装

	public override void OnActivate()
	{
		n本体X = 0;
		n本体Y = 0x18b;
		ft曲名用フォント = new Font( "MS PGothic", 44f, FontStyle.Regular, GraphicsUnit.Pixel );
		base.OnActivate();
	}
	public override void OnDeactivate()
	{
		if( ft曲名用フォント != null )
		{
			ft曲名用フォント.Dispose();
			ft曲名用フォント = null;
		}
		if( ct登場用 != null )
		{
			ct登場用 = null;
		}
		base.OnDeactivate();
	}
	public override void OnManagedCreateResources()
	{
		if( bActivated )
		{
			//this.txバー = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\ScreenResult song bar.png" ) );
			try
			{
				Bitmap image = new Bitmap( 0x3a8, 0x36 );
				Graphics graphics = Graphics.FromImage( image );
				graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
				graphics.DrawString( CDTXMania.DTX.TITLE, ft曲名用フォント, Brushes.White, ( float ) 8f, ( float ) 0f );
				tx曲名 = new CTexture( CDTXMania.app.Device, image, CDTXMania.TextureFormat );
				tx曲名.vcScaleRatio = new Vector3( 0.5f, 0.5f, 1f );
				graphics.Dispose();
				image.Dispose();
			}
			catch( CTextureCreateFailedException )
			{
				Trace.TraceError( "曲名テクスチャの生成に失敗しました。" );
				tx曲名 = null;
			}
			base.OnManagedCreateResources();
		}
	}
	public override void OnManagedReleaseResources()
	{
		if( bActivated )
		{
			CDTXMania.tReleaseTexture( ref txバー );
			CDTXMania.tReleaseTexture( ref tx曲名 );
			base.OnManagedReleaseResources();
		}
	}
	public override int OnUpdateAndDraw()
	{
		if (!bActivated)
		{
			return 0;
		}
		if( bJustStartedUpdate )
		{
			ct登場用 = new CCounter( 0, 270, 4, CDTXMania.Timer );
			bJustStartedUpdate = false;
		}
		ct登場用.tUpdate();
		int num = 0x1d4;
		int num2 = num - 0x40;
		if( ct登場用.bInProgress )
		{
			if( ct登場用.nCurrentValue <= 100 )
			{
				double num3 = 1.0 - ( ( (double) ct登場用.nCurrentValue ) / 100.0 );
				n本体X = -( (int) ( num * Math.Sin( Math.PI / 2 * num3 ) ) );
				n本体Y = 0x18b;
			}
			else if( ct登場用.nCurrentValue <= 200 )
			{
				double num4 = ( (double) ( ct登場用.nCurrentValue - 100 ) ) / 100.0;
				n本体X = -( (int) ( ( ( (double) num ) / 6.0 ) * Math.Sin( Math.PI * num4 ) ) );
				n本体Y = 0x18b;
			}
			else if( ct登場用.nCurrentValue <= 270 )
			{
				double num5 = ( (double) ( ct登場用.nCurrentValue - 200 ) ) / 70.0;
				n本体X = -( (int) ( ( ( (double) num ) / 18.0 ) * Math.Sin( Math.PI * num5 ) ) );
				n本体Y = 0x18b;
			}
		}
		else
		{
			n本体X = 0;
			n本体Y = 0x18b;
		}
		int num6 = n本体X;
		int y = n本体Y;
		int num8 = 0;
		while( num8 < num2 )
		{
			Rectangle rectangle = new Rectangle( 0, 0, 0x40, 0x40 );
			if( ( num8 + rectangle.Width ) >= num2 )
			{
				rectangle.Width -= ( num8 + rectangle.Width ) - num2;
			}
			num8 += rectangle.Width;
		}
		if( tx曲名 != null )
		{
			tx曲名.tDraw2D( CDTXMania.app.Device, n本体X, n本体Y + 20 );
		}
		if( !ct登場用.bReachedEndValue )
		{
			return 0;
		}
		return 1;
	}


	// Other

	#region [ private ]
	//-----------------
	private CCounter ct登場用;
	private Font ft曲名用フォント;
	private int n本体X;
	private int n本体Y;
	private CTexture txバー;
	private CTexture tx曲名;
	//-----------------
	#endregion
}