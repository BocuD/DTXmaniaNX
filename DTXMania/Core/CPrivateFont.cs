using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace DTXMania.Core;

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
	protected void Initialize(string fontPath, FontFamily? fontFamily, int fontSize, FontStyle style)
	{
		pfc = null;
		this.fontFamily = null;
		font = null;
		pt = fontSize;
		RectStrings = new Rectangle(0, 0, 0, 0);
		PtOrigin = new Point(0, 0);
		bDisposed = false;

		if (fontFamily != null)
		{
			this.fontFamily = fontFamily;
		}
		else
		{
			try
			{
				pfc = new PrivateFontCollection();    //PrivateFontCollectionオブジェクトを作成する
				pfc.AddFontFile(fontPath);                                //PrivateFontCollectionにフォントを追加する
			}
			catch (FileNotFoundException)
			{
				Trace.TraceError("プライベートフォントの追加に失敗しました。({0})", fontPath);
				throw new FileNotFoundException("プライベートフォントの追加に失敗しました。({0})", Path.GetFileName(fontPath));
				//return;
			}
				
			this.fontFamily = pfc.Families[0];
		}

		// 指定されたフォントスタイルが適用できない場合は、フォント内で定義されているスタイルから候補を選んで使用する
		// 何もスタイルが使えないようなフォントなら、例外を出す。
		if (this.fontFamily != null)
		{
			if (!this.fontFamily.IsStyleAvailable(style))
			{
				FontStyle[] fs = { FontStyle.Regular, FontStyle.Bold, FontStyle.Italic, FontStyle.Underline, FontStyle.Strikeout };
				style = FontStyle.Regular | FontStyle.Bold | FontStyle.Italic | FontStyle.Underline | FontStyle.Strikeout;  // null非許容型なので、代わりに全盛をNGワードに設定
				foreach (FontStyle ff in fs)
				{
					if (this.fontFamily.IsStyleAvailable(ff))
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
			float emSize = fontSize * 96.0f / 72.0f;
			font = new Font(this.fontFamily, emSize, style, GraphicsUnit.Pixel); //PrivateFontCollectionの先頭のフォントのFontオブジェクトを作成する
			//HighDPI対応のため、pxサイズで指定
		}
		else
			//Fall back to MS PGothic if the font file is not found
		{
			float emSize = fontSize * 96.0f / 72.0f;
			font = new Font("MS PGothic", emSize, style, GraphicsUnit.Pixel);
			FontFamily[] ffs = new InstalledFontCollection().Families;
			int lcid = System.Globalization.CultureInfo.GetCultureInfo("en-us").LCID;
			foreach (FontFamily ff in ffs)
			{
				// Trace.WriteLine( lcid ) );
				if (ff.GetName(lcid) == "MS PGothic")
				{
					this.fontFamily = ff;
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
	/// Returns a texture with the drawn string (call from main thread)
	/// </summary>
	/// <param name="drawStr">String to draw</param>
	/// <param name="drawMode">Draw mode</param>
	/// <param name="fontColor">Font color</param>
	/// <param name="edgeColor">Outline color</param>
	/// <param name="gradationTopColor">Gradient top color</param>
	/// <param name="gradationBottomColor">Gradient bottom color</param>
	/// <param name="bEdgeGradation">Color gradient on outline</param>
	/// <returns>Returns the drawn texture</returns>
	public Bitmap DrawPrivateFont(string drawStr, DrawMode drawMode, Color fontColor, Color edgeColor, Color gradationTopColor, Color gradationBottomColor, bool bEdgeGradation = false)
	{
		if (fontFamily == null || string.IsNullOrEmpty(drawStr))
		{
			// nullを返すと、その後bmp→texture処理や、textureのサイズを見て__の処理で全部例外が発生することになる。
			// それは非常に面倒なので、最小限のbitmapを返してしまう。
			// まずはこの仕様で進めますが、問題有れば(上位側からエラー検出が必要であれば)例外を出したりエラー状態であるプロパティを定義するなり検討します。
			if (fontFamily == null)
			{
				Trace.TraceError("Invalid input for DrawPrivateFont(): fontFamily is null. Empty texture will be returned.");
			}
			if (string.IsNullOrEmpty(drawStr))
			{
				Trace.TraceError("Invalid input for DrawPrivateFont(): drawStr is null or empty. Empty texture will be returned.");
			}

			RectStrings = new Rectangle(0, 0, 0, 0);
			PtOrigin = new Point(0, 0);
			return new Bitmap(1, 1);
		}
			
		bool bEdge = (drawMode & DrawMode.Edge) == DrawMode.Edge;
		bool bGradation = (drawMode & DrawMode.Gradation) == DrawMode.Gradation;
			
		// 縁取りの縁のサイズは、とりあえずフォントの大きさの1/4とする
		// Changed to 1/6 as 1/4 is too thick for new Black-White Style
		int nEdgePt = bEdgeGradation ? pt / 6 : bEdge ? pt / 4 : 0;
		const TextFormatFlags flags = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
			
		// 描画サイズを測定する
		Size stringSize = TextRenderer.MeasureText(drawStr, font, 
			new Size(int.MaxValue, int.MaxValue), flags);
			
		//取得した描画サイズを基に、描画先のbitmapを作成する
		int lWidth = (int)(stringSize.Width * 1.1f + 10.0f); //Add 10% and 10 pixels so we avoid text truncation
		Bitmap bmp = new(lWidth + nEdgePt * 2, stringSize.Height + nEdgePt * 2);
		bmp.MakeTransparent();
		Graphics g = Graphics.FromImage(bmp);
		g.SmoothingMode = SmoothingMode.HighQuality;
		StringFormat sf = new();
		sf.LineAlignment = StringAlignment.Far; // 画面下部（垂直方向位置）
		sf.Alignment = StringAlignment.Near;    // 画面中央（水平方向位置）//Changed to Left (Near) of Texture rect
		sf.FormatFlags = StringFormatFlags.NoClip | StringFormatFlags.NoWrap;
			
		// レイアウト枠
		Rectangle r = new(0, 0, lWidth + nEdgePt * 2, stringSize.Height + nEdgePt * 2);
			
		if (bEdgeGradation || bEdge)
		{
			// DrawPathで、ポイントサイズを使って描画するために、DPIを使って単位変換する
			// (これをしないと、単位が違うために、小さめに描画されてしまう)
			float sizeInPixels = font.SizeInPoints * g.DpiY / 72; // 1 inch = 72 points
				
			GraphicsPath gp = new();
			gp.AddString(drawStr, fontFamily, (int)font.Style, sizeInPixels, r, sf);
				
			Brush br;

			if (bEdgeGradation)
			{
				// 縁取りを描画する
				Brush brushOutline = new LinearGradientBrush(r, gradationTopColor, gradationBottomColor,
					LinearGradientMode.Vertical);

				Pen p = new(brushOutline, nEdgePt);
				p.LineJoin = LineJoin.Round;
				g.DrawPath(p, gp);
				p.Dispose();

				// 塗りつぶす
				br = new SolidBrush(fontColor);
			}
			else
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

			g.FillPath(br, gp);
			br.Dispose();
			gp.Dispose();
		}
		else
		{
			TextRenderer.DrawText(g, drawStr, font, r, fontColor, flags);
		}

#if debug表示
			g.DrawRectangle( new Pen( Color.White, 1 ), new Rectangle( 1, 1, stringSize.Width-1, stringSize.Height-1 ) );
			g.DrawRectangle( new Pen( Color.Green, 1 ), new Rectangle( 0, 0, bmp.Width - 1, bmp.Height - 1 ) );
#endif
		RectStrings = new Rectangle(0, 0, stringSize.Width, stringSize.Height);
		PtOrigin = new Point(nEdgePt * 2, nEdgePt * 2);


		sf.Dispose();
		g.Dispose();

		return bmp;
	}

	/// <summary>
	/// 最後にDrawPrivateFont()した文字列の描画領域を取得します。
	/// </summary>
	public Rectangle RectStrings { get; protected set; }

	protected Point PtOrigin { get; set; }

	#region [ IDisposable 実装 ]
	//-----------------
	public void Dispose()
	{
		if (!bDisposed)
		{
			if (font != null)
			{
				font.Dispose();
				font = null;
			}
			if (pfc != null)
			{
				pfc.Dispose();
				pfc = null;
			}

			bDisposed = true;
		}
	}
	//-----------------
	#endregion

	#region [ private ]
	//-----------------
	private bool bDisposed;
	private Font font;

	private PrivateFontCollection pfc;
	private FontFamily fontFamily;
	private int pt;

	//-----------------
	#endregion
}