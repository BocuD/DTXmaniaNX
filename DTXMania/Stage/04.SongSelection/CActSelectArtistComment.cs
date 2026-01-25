using System.Drawing;
using System.Diagnostics;
using DTXMania.Core;
using SharpDX;
using FDK;

using Rectangle = System.Drawing.Rectangle;
using RectangleF = SharpDX.RectangleF;

namespace DTXMania;

internal class CActSelectArtistComment : CActivity
{
	// メソッド

	public CActSelectArtistComment()
	{
		bActivated = false;
	}
	public void t選択曲が変更された()
	{
		CScore cスコア = CDTXMania.stageSongSelection.rSelectedScore;
		if( cスコア != null )
		{
			Bitmap image = new Bitmap( 1, 1 );
			CDTXMania.tReleaseTexture( ref txArtist );
			strArtist = cスコア.SongInformation.ArtistName;
			if( ( strArtist != null ) && ( strArtist.Length > 0 ) )
			{
				Graphics graphics = Graphics.FromImage( image );
				graphics.PageUnit = GraphicsUnit.Pixel;
				SizeF ef = graphics.MeasureString( strArtist, ft描画用フォント );
				graphics.Dispose();
				//if (ef.Width > GameWindowSize.Width)
				//{
				//	ef.Width = GameWindowSize.Width;
				//}
				try
				{
					//Fix length issue of Artist using the same method used for Song Title
					int nLargestLengthPx = 510;//510px is the available space for artist in the bar
					int widthAfterScaling = (int)((ef.Width + 2) * 0.5f);//+2 buffer
					if (widthAfterScaling > (CDTXMania.app.Device.Capabilities.MaxTextureWidth / 2))
						widthAfterScaling = CDTXMania.app.Device.Capabilities.MaxTextureWidth / 2;  // 右端断ち切れ仕方ないよね
					//Compute horizontal scaling factor
					float f拡大率X = (widthAfterScaling <= nLargestLengthPx) ? 0.5f : (((float)nLargestLengthPx / (float)widthAfterScaling) * 0.5f);   // 長い文字列は横方向に圧縮。
					//ef.Width
					Bitmap bitmap2 = new Bitmap( (int) Math.Ceiling( (double)widthAfterScaling * 2), (int) Math.Ceiling( (double) ft描画用フォント.Size ) );
					graphics = Graphics.FromImage( bitmap2 );
					graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
					graphics.DrawString( strArtist, ft描画用フォント, Brushes.White, ( float ) 0f, ( float ) 0f );
					graphics.Dispose();
					txArtist = new CTexture( CDTXMania.app.Device, bitmap2, CDTXMania.TextureFormat );
					txArtist.vcScaleRatio = new Vector3(f拡大率X, 0.5f, 1f );
					bitmap2.Dispose();
				}
				catch( CTextureCreateFailedException )
				{
					Trace.TraceError( "ARTISTテクスチャの生成に失敗しました。" );
					txArtist = null;
				}
			}
			CDTXMania.tReleaseTexture( ref txComment );
			strComment = cスコア.SongInformation.Comment;
			if( ( strComment != null ) && ( strComment.Length > 0 ) )
			{
				Graphics graphics2 = Graphics.FromImage( image );
				graphics2.PageUnit = GraphicsUnit.Pixel;
				SizeF ef2 = graphics2.MeasureString( strComment, ft描画用フォント );
				Size size = new Size( (int) Math.Ceiling( (double) ef2.Width ), (int) Math.Ceiling( (double) ef2.Height ) );
				graphics2.Dispose();
				nテクスチャの最大幅 = CDTXMania.app.Device.Capabilities.MaxTextureWidth;
				int maxTextureHeight = CDTXMania.app.Device.Capabilities.MaxTextureHeight;
				Bitmap bitmap3 = new Bitmap( size.Width, (int) Math.Ceiling( (double) ft描画用フォント.Size ) );
				graphics2 = Graphics.FromImage( bitmap3 );
				graphics2.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
				graphics2.DrawString( strComment, ft描画用フォント, Brushes.White, ( float ) 0f, ( float ) 0f );
				graphics2.Dispose();
				nComment行数 = 1;
				nComment最終行の幅 = size.Width;
				while( nComment最終行の幅 > nテクスチャの最大幅 )
				{
					nComment行数++;
					nComment最終行の幅 -= nテクスチャの最大幅;
				}
				while( ( nComment行数 * ( (int) Math.Ceiling( (double) ft描画用フォント.Size ) ) ) > maxTextureHeight )
				{
					nComment行数--;
					nComment最終行の幅 = nテクスチャの最大幅;
				}
				Bitmap bitmap4 = new Bitmap( ( nComment行数 > 1 ) ? nテクスチャの最大幅 : nComment最終行の幅, nComment行数 * ( (int) Math.Ceiling( (double) ft描画用フォント.Size ) ) );
				graphics2 = Graphics.FromImage( bitmap4 );
				Rectangle srcRect = new Rectangle();
				Rectangle destRect = new Rectangle();
				for( int i = 0; i < nComment行数; i++ )
				{
					srcRect.X = i * nテクスチャの最大幅;
					srcRect.Y = 0;
					srcRect.Width = ( ( i + 1 ) == nComment行数 ) ? nComment最終行の幅 : nテクスチャの最大幅;
					srcRect.Height = bitmap3.Height;
					destRect.X = 0;
					destRect.Y = i * bitmap3.Height;
					destRect.Width = srcRect.Width;
					destRect.Height = srcRect.Height;
					graphics2.DrawImage( bitmap3, destRect, srcRect, GraphicsUnit.Pixel );
				}
				graphics2.Dispose();
				try
				{
					txComment = new CTexture( CDTXMania.app.Device, bitmap4, CDTXMania.TextureFormat );
					txComment.vcScaleRatio = new Vector3( 0.5f, 0.5f, 1f );
				}
				catch( CTextureCreateFailedException )
				{
					Trace.TraceError( "COMMENTテクスチャの生成に失敗しました。" );
					txComment = null;
				}
				bitmap4.Dispose();
				bitmap3.Dispose();
			}
			image.Dispose();
			if( txComment != null )
			{
				ctComment = new CCounter( -740, (int) ( ( ( ( nComment行数 - 1 ) * nテクスチャの最大幅 ) + nComment最終行の幅 ) * txComment.vcScaleRatio.X ), 10, CDTXMania.Timer );
			}
		}
	}


	// CActivity 実装

	public override void OnActivate()
	{
		ft描画用フォント = new Font( "MS PGothic", 40f, GraphicsUnit.Pixel );
		txArtist = null;
		txComment = null;
		strArtist = "";
		strComment = "";
		nComment最終行の幅 = 0;
		nComment行数 = 0;
		nテクスチャの最大幅 = 0;
		ctComment = new CCounter();
		base.OnActivate();
	}
	public override void OnDeactivate()
	{
		CDTXMania.tReleaseTexture( ref txArtist );
		CDTXMania.tReleaseTexture( ref txComment );
		if( ft描画用フォント != null )
		{
			ft描画用フォント.Dispose();
			ft描画用フォント = null;
		}
		ctComment = null;
		base.OnDeactivate();
	}
	public override void OnManagedCreateResources()
	{
		if( bActivated )
		{
			txコメントバー = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\5_comment bar.png"), true);
			t選択曲が変更された();
			base.OnManagedCreateResources();
		}
	}
	public override void OnManagedReleaseResources()
	{
		if( bActivated )
		{
			CDTXMania.tReleaseTexture( ref txArtist );
			CDTXMania.tReleaseTexture( ref txComment );
			CDTXMania.tReleaseTexture( ref txコメントバー );
			base.OnManagedReleaseResources();
		}
	}
	public override int OnUpdateAndDraw()
	{
		if( bActivated )
		{
			if (txコメントバー != null)
				txコメントバー.tDraw2D(CDTXMania.app.Device, 560, 257);

			if (ctComment.bInProgress)
			{
				ctComment.tUpdateLoop();
			}
			if( txArtist != null )
			{
				int x = 1260 - 25 - ( (int) ( txArtist.szTextureSize.Width * txArtist.vcScaleRatio.X ) );		// #27648 2012.3.14 yyagi: -12 for scrollbar
				int y = 320;
				txArtist.tDraw2D( CDTXMania.app.Device, x, y );
				//this.txArtist.tDraw2D(CDTXMania.app.Device, 64, 570);
			}

			int num3 = 683;
			int num4 = 339;

			if ((txComment != null) && ((txComment.szTextureSize.Width * txComment.vcScaleRatio.X) < (1250 - num3)))
			{
				txComment.tDraw2D(CDTXMania.app.Device, num3, num4);
			}
			else if (txComment != null)
			{
				Rectangle rectangle = new Rectangle(ctComment.nCurrentValue, 0, 750, (int)ft描画用フォント.Size);
				if (rectangle.X < 0)
				{
					num3 += -rectangle.X;
					rectangle.Width -= -rectangle.X;
					rectangle.X = 0;
				}
				int num5 = ((int)(rectangle.X / txComment.vcScaleRatio.X)) / nテクスチャの最大幅;
				RectangleF rectangle2 = new();
				while (rectangle.Width > 0)
				{
					rectangle2.X = ((int)(rectangle.X / txComment.vcScaleRatio.X)) % nテクスチャの最大幅;
					rectangle2.Y = num5 * ((int)ft描画用フォント.Size);
					float num6 = ((num5 + 1) == nComment行数) ? nComment最終行の幅 : nテクスチャの最大幅;
					float num7 = num6 - rectangle2.X;
					rectangle2.Width = num7;
					rectangle2.Height = (int)ft描画用フォント.Size;
					txComment.tDraw2D(CDTXMania.app.Device, num3, num4, rectangle2);
					if (++num5 == nComment行数)
					{
						break;
					}
					int num8 = (int)(rectangle2.Width * txComment.vcScaleRatio.X);
					rectangle.X += num8;
					rectangle.Width -= num8;
					num3 += num8;
				}
			}
		}
		return 0;
	}


	// Other

	#region [ private ]
	//-----------------
	private CTexture txコメントバー;
	private CCounter ctComment;
	private Font ft描画用フォント;
	private int nComment行数;
	private int nComment最終行の幅;
	private const int nComment表示幅 = 510;
	private int nテクスチャの最大幅;
	private string strArtist;
	private string strComment;
	private CTexture txArtist;
	private CTexture txComment;
	//-----------------
	#endregion
}
