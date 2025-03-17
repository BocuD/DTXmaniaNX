﻿using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Management;
using System.Windows.Forms;

namespace DTXMania
{
	/// <summary>
	/// プライベートフォントでの描画を扱うクラス。
	/// </summary>
	/// <exception cref="FileNotFoundException">フォントファイルが見つからない時に例外発生</exception>
	/// <exception cref="ArgumentException">スタイル指定不正時に例外発生</exception>
	/// <remarks>
	/// 簡単な使い方
	/// CPrivateFont prvFont = new CPrivateFont( CSkin.Path( @"Graphics\fonts\mplus-1p-bold.ttf" ), 36 );	// プライベートフォント
	/// とか
	/// CPrivateFont prvFont = new CPrivateFont( new FontFamily("MS UI Gothic"), 36, FontStyle.Bold );		// システムフォント
	/// とかした上で、
	/// Bitmap bmp = prvFont.DrawPrivateFont( "ABCDE", Color.White, Color.Black );							// フォント色＝白、縁の色＝黒の例。縁の色は省略可能
	/// とか
	/// Bitmap bmp = prvFont.DrawPrivateFont( "ABCDE", Color.White, Color.Black, Color.Yellow, Color.OrangeRed ); // 上下グラデーション(Yellow→OrangeRed)
	/// とかして、
	/// CTexture ctBmp = CDTXMania.tGenerateTexture( bmp, false );
	/// ctBMP.tDraw2D( ～～～ );
	/// で表示してください。
	/// 
	/// 注意点
	/// 任意のフォントでのレンダリングは結構負荷が大きいので、なるべｋなら描画フレーム毎にフォントを再レンダリングするようなことはせず、
	/// 一旦レンダリングしたものを描画に使い回すようにしてください。
	/// また、長い文字列を与えると、返されるBitmapも横長になります。この横長画像をそのままテクスチャとして使うと、
	/// 古いPCで問題を発生させやすいです。これを回避するには、一旦Bitmapとして取得したのち、256pixや512pixで分割して
	/// テクスチャに定義するようにしてください。
	/// </remarks>
	public class CPrivateFont : IDisposable
	{
		protected void Initialize(string fontPath, FontFamily fontFamily, int pt, FontStyle style)
		{
			_pfc = null;
			_fontFamily = null;
			_font = null;
			_pt = pt;
			_rectStrings = new Rectangle(0, 0, 0, 0);
			_ptOrigin = new Point(0, 0);
			bDispose完了済み = false;

			if (fontFamily != null)
			{
				_fontFamily = fontFamily;
			}
			else
			{
				try
				{
					_pfc = new System.Drawing.Text.PrivateFontCollection();    //PrivateFontCollectionオブジェクトを作成する
					_pfc.AddFontFile(fontPath);                                //PrivateFontCollectionにフォントを追加する
				}
				catch (FileNotFoundException)
				{
					Trace.TraceError("プライベートフォントの追加に失敗しました。({0})", fontPath);
					throw new FileNotFoundException("プライベートフォントの追加に失敗しました。({0})", Path.GetFileName(fontPath));
					//return;
				}

				//foreach ( FontFamily ff in pfc.Families )
				//{
				//    Debug.WriteLine( "fontname=" + ff.Name );
				//    if ( ff.Name == Path.GetFileNameWithoutExtension( fontpath ) )
				//    {
				//        _fontfamily = ff;
				//        break;
				//    }
				//}
				//if ( _fontfamily == null )
				//{
				//    Trace.TraceError( "プライベートフォントの追加後、検索に失敗しました。({0})", fontpath );
				//    return;
				//}
				_fontFamily = _pfc.Families[0];
			}

			// 指定されたフォントスタイルが適用できない場合は、フォント内で定義されているスタイルから候補を選んで使用する
			// 何もスタイルが使えないようなフォントなら、例外を出す。
			if (_fontFamily != null)
			{
				if (!_fontFamily.IsStyleAvailable(style))
				{
					FontStyle[] fs = { FontStyle.Regular, FontStyle.Bold, FontStyle.Italic, FontStyle.Underline, FontStyle.Strikeout };
					style = FontStyle.Regular | FontStyle.Bold | FontStyle.Italic | FontStyle.Underline | FontStyle.Strikeout;  // null非許容型なので、代わりに全盛をNGワードに設定
					foreach (FontStyle ff in fs)
					{
						if (_fontFamily.IsStyleAvailable(ff))
						{
							style = ff;
							Trace.TraceWarning("フォント{0}へのスタイル指定を、{1}に変更しました。", Path.GetFileName(fontPath), style.ToString());
							break;
						}
					}
					if (style == (FontStyle.Regular | FontStyle.Bold | FontStyle.Italic | FontStyle.Underline | FontStyle.Strikeout))
					{
						Trace.TraceWarning("フォント{0}は適切なスタイル{1}を選択できませんでした。", Path.GetFileName(fontPath), style.ToString());
					}
				}
				//this._font = new Font(this._fontfamily, pt, style);			//PrivateFontCollectionの先頭のフォントのFontオブジェクトを作成する
				float emSize = pt * 96.0f / 72.0f;
				_font = new Font(_fontFamily, emSize, style, GraphicsUnit.Pixel); //PrivateFontCollectionの先頭のフォントのFontオブジェクトを作成する
																							//HighDPI対応のため、pxサイズで指定
			}
			else
			// フォントファイルが見つからなかった場合 (MS PGothicを代わりに指定する)
			{
				float emSize = pt * 96.0f / 72.0f;
				_font = new Font("MS PGothic", emSize, style, GraphicsUnit.Pixel); //MS PGothicのFontオブジェクトを作成する
				FontFamily[] ffs = new System.Drawing.Text.InstalledFontCollection().Families;
				int lcid = System.Globalization.CultureInfo.GetCultureInfo("en-us").LCID;
				foreach (FontFamily ff in ffs)
				{
					// Trace.WriteLine( lcid ) );
					if (ff.GetName(lcid) == "MS PGothic")
					{
						_fontFamily = ff;
						Trace.TraceInformation("MS PGothicを代わりに指定しました。");
						return;
					}
				}
				throw new FileNotFoundException("プライベートフォントの追加に失敗し、MS PGothicでの代替処理にも失敗しました。({0})", Path.GetFileName(fontPath));
			}
		}

		[Flags]
		public enum DrawMode
		{
			Normal,
			Edge,
			Gradation
		}

		/// <summary>
		/// 文字列を描画したテクスチャを返す(メイン処理)
		/// </summary>
		/// <param name="drawStr">描画文字列</param>
		/// <param name="drawMode">描画モード</param>
		/// <param name="fontColor">描画色</param>
		/// <param name="edgeColor">縁取色</param>
		/// <param name="gradationTopColor">グラデーション 上側の色</param>
		/// <param name="gradationBottomColor">グラデーション 下側の色</param>
		/// <returns>描画済テクスチャ</returns>
		public Bitmap DrawPrivateFont(string drawStr, DrawMode drawMode, Color fontColor, Color edgeColor, Color gradationTopColor, Color gradationBottomColor, bool bEdgeGradation = false)
		{
			if (_fontFamily == null || drawStr == null || drawStr == "")
			{
				// nullを返すと、その後bmp→texture処理や、textureのサイズを見て__の処理で全部例外が発生することになる。
				// それは非常に面倒なので、最小限のbitmapを返してしまう。
				// まずはこの仕様で進めますが、問題有れば(上位側からエラー検出が必要であれば)例外を出したりエラー状態であるプロパティを定義するなり検討します。
				Trace.TraceError("DrawPrivateFont()の入力不正。最小値のbitmapを返します。");
				_rectStrings = new Rectangle(0, 0, 0, 0);
				_ptOrigin = new Point(0, 0);
				return new Bitmap(1, 1);
			}
			
			
			bool bEdge = (drawMode & DrawMode.Edge) == DrawMode.Edge;
			bool bGradation = (drawMode & DrawMode.Gradation) == DrawMode.Gradation;
			
			// 縁取りの縁のサイズは、とりあえずフォントの大きさの1/4とする
			// Changed to 1/6 as 1/4 is too thick for new Black-White Style
			int nEdgePt = bEdgeGradation ? _pt / 6 : bEdge ? _pt / 4 : 0;
			var flags = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
			
			// 描画サイズを測定する
			Size stringSize = TextRenderer.MeasureText(drawStr, _font, 
				new Size(int.MaxValue, int.MaxValue), flags);
			
			//取得した描画サイズを基に、描画先のbitmapを作成する
			int l_width = (int)(stringSize.Width * 1.1f); //A constant proportion of 10% buffer should avoid the issue of text truncation
			Bitmap bmp = new(l_width + nEdgePt * 2, stringSize.Height + nEdgePt * 2);
			bmp.MakeTransparent();
			Graphics g = Graphics.FromImage(bmp);
			g.SmoothingMode = SmoothingMode.HighQuality;
			StringFormat sf = new();
			sf.LineAlignment = StringAlignment.Far; // 画面下部（垂直方向位置）
			sf.Alignment = StringAlignment.Near;    // 画面中央（水平方向位置）//Changed to Left (Near) of Texture rect
			sf.FormatFlags = StringFormatFlags.NoClip | StringFormatFlags.NoWrap;
			
			// レイアウト枠
			Rectangle r = new(0, 0, l_width + nEdgePt * 2, stringSize.Height + nEdgePt * 2);
			
			if (bEdgeGradation || bEdge)
			{
				// DrawPathで、ポイントサイズを使って描画するために、DPIを使って単位変換する
				// (これをしないと、単位が違うために、小さめに描画されてしまう)
				float sizeInPixels = _font.SizeInPoints * g.DpiY / 72; // 1 inch = 72 points
				
				GraphicsPath gp = new();
				gp.AddString(drawStr, _fontFamily, (int)_font.Style, sizeInPixels, r, sf);
				
				Brush br;

				if (!bEdgeGradation)
				{
					// 縁取りを描画する
					Pen p = new(edgeColor, nEdgePt);
					p.LineJoin = LineJoin.Round;
					g.DrawPath(p, gp);
					p.Dispose();

					// 塗りつぶす
					if (bGradation)
					{
						br = new LinearGradientBrush(r, gradationTopColor, gradationBottomColor,
							LinearGradientMode.Vertical);
					}
					else
					{
						br = new SolidBrush(fontColor);
					}
				}
				else
				{
					// 縁取りを描画する
					Brush br縁 = new LinearGradientBrush(r, gradationTopColor, gradationBottomColor,
						LinearGradientMode.Vertical);
					
					Pen p = new(br縁, nEdgePt);
					p.LineJoin = LineJoin.Round;
					g.DrawPath(p, gp);
					p.Dispose();

					// 塗りつぶす
					br = new SolidBrush(fontColor);
				}
				
				g.FillPath(br, gp);
				br.Dispose();
				gp.Dispose();
			}
			else
			{
				TextRenderer.DrawText(g, drawStr, _font, r, fontColor, flags);
			}

#if debug表示
			g.DrawRectangle( new Pen( Color.White, 1 ), new Rectangle( 1, 1, stringSize.Width-1, stringSize.Height-1 ) );
			g.DrawRectangle( new Pen( Color.Green, 1 ), new Rectangle( 0, 0, bmp.Width - 1, bmp.Height - 1 ) );
#endif
			_rectStrings = new Rectangle(0, 0, stringSize.Width, stringSize.Height);
			_ptOrigin = new Point(nEdgePt * 2, nEdgePt * 2);


			sf.Dispose();
			g.Dispose();

			return bmp;
		}

		/// <summary>
		/// 最後にDrawPrivateFont()した文字列の描画領域を取得します。
		/// </summary>
		public Rectangle RectStrings
		{
			get => _rectStrings;
			protected set => _rectStrings = value;
		}
		public Point PtOrigin
		{
			get => _ptOrigin;
			protected set => _ptOrigin = value;
		}

		#region [ IDisposable 実装 ]
		//-----------------
		public void Dispose()
		{
			if (!bDispose完了済み)
			{
				if (_font != null)
				{
					_font.Dispose();
					_font = null;
				}
				if (_pfc != null)
				{
					_pfc.Dispose();
					_pfc = null;
				}

				bDispose完了済み = true;
			}
		}
		//-----------------
		#endregion

		#region [ private ]
		//-----------------
		protected bool bDispose完了済み;
		protected Font _font;

		private System.Drawing.Text.PrivateFontCollection _pfc;
		private FontFamily _fontFamily;
		private int _pt;
		private Rectangle _rectStrings;
		private Point _ptOrigin;
		//-----------------
		#endregion
	}
}
