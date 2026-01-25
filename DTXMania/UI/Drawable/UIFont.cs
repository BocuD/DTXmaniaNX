using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;

namespace DTXMania.UI.Drawable;

public class UIFont
{
	public PrivateFontCollection? collection; //Font collection is what we load from a ttf file
	public FontFamily fontFamily; //Family is a specific subset of this collection

	public float fontSize
	{
		get => _fontSize;
		set
		{
			_fontSize = value;
			fontDirty = true;
		}
	}
	private float _fontSize;

	public FontStyle fontStyle
	{
		get => _fontStyle;
		set
		{
			_fontStyle = value;
			fontDirty = true;
		}
	}
	private FontStyle _fontStyle;
	
	public Font font; //Font is the actual object with size, style etc that is used for rendering
	private bool fontDirty;

	//todo: actually load the family that is specified
	public UIFont(string fontPath, string fontFamily, FontStyle style, float fontSize)
	{
		try
		{
			collection = new PrivateFontCollection(); //PrivateFontCollectionオブジェクトを作成する //Create privatefontcollection object
			collection.AddFontFile(fontPath); //PrivateFontCollectionにフォントを追加する //Add the font to the collection
			this.fontFamily = collection.Families[0]; //Get the first family from the collection (for now)
		}
		catch (FileNotFoundException e)
		{
			Trace.TraceError($"Adding font to private font collection failed: ({fontPath}) {e}");
		}
		
		if (this.fontFamily == null) //Fall back to MS PGothic if the font file is not found
		{
			LoadFallbackFont();
		}
	}
	
	public UIFont(FontFamily fontFamily, FontStyle style, float fontSize)
	{
		this.fontFamily = fontFamily;
		this.fontSize = fontSize;

		if (this.fontFamily == null)
		{
			LoadFallbackFont();
		}
		
		UpdateFont();
	}

	public void UpdateFont()
	{
		if (!fontFamily.IsStyleAvailable(fontStyle))
		{
			FontStyle[] fs = [FontStyle.Regular, FontStyle.Bold, FontStyle.Italic, FontStyle.Underline, FontStyle.Strikeout];
			fontStyle = FontStyle.Regular | FontStyle.Bold | FontStyle.Italic | FontStyle.Underline | FontStyle.Strikeout;
				
			foreach (FontStyle ff in fs)
			{
				if (!fontFamily.IsStyleAvailable(ff)) continue;
				
				fontStyle = ff;
				Trace.TraceWarning("Failed to apply style {0} to font {1}, using {2} instead.", ff, fontFamily.Name, fontStyle.ToString());
				break;
			}
		}

		float emSize = fontSize * 96.0f / 72.0f; //??
		font = new Font(fontFamily, emSize, fontStyle, GraphicsUnit.Pixel);
	}

	private void LoadFallbackFont()
	{
		float emSize = fontSize * 96.0f / 72.0f;
		font = new Font("MS PGothic", emSize, fontStyle, GraphicsUnit.Pixel);
		FontFamily[] ffs = new InstalledFontCollection().Families;
		int lcid = System.Globalization.CultureInfo.GetCultureInfo("en-us").LCID;
		
		foreach (FontFamily ff in ffs)
		{
			// Trace.WriteLine( lcid ) );
			if (ff.GetName(lcid) == "MS PGothic")
			{
				fontFamily = ff;
				Trace.TraceInformation("MS PGothicを代わりに指定しました。");
			}
		}
	}
	
	public Bitmap DrawPrivateFont(string drawStr, CPrivateFont.DrawMode drawMode, Color fontColor, Color edgeColor, Color gradationTopColor, Color gradationBottomColor, bool bEdgeGradation = false)
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

			return new Bitmap(1, 1);
		}
			
		bool bEdge = (drawMode == CPrivateFont.DrawMode.Edge);
		bool bGradation = (drawMode & CPrivateFont.DrawMode.Gradation) == CPrivateFont.DrawMode.Gradation;
			
		// 縁取りの縁のサイズは、とりあえずフォントの大きさの1/4とする
		// Changed to 1/6 as 1/4 is too thick for new Black-White Style
		float nEdgePt = bEdgeGradation ? fontSize / 6 : bEdge ? fontSize / 4 : 0;
		const TextFormatFlags flags = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
		
		// 描画サイズを測定する
		Size stringSize = TextRenderer.MeasureText(drawStr, font, new Size(int.MaxValue, int.MaxValue), flags);
			
		//取得した描画サイズを基に、描画先のbitmapを作成する
		int lWidth = (int)(stringSize.Width * 1.1f + 10.0f); //Add 10% and 10 pixels so we avoid text truncation
		Bitmap bmp = new((int)(lWidth + nEdgePt * 2), (int)(stringSize.Height + nEdgePt * 2));
		bmp.MakeTransparent();
		Graphics g = Graphics.FromImage(bmp);
		g.SmoothingMode = SmoothingMode.HighQuality;
		StringFormat sf = new();
		sf.LineAlignment = StringAlignment.Far; // 画面下部（垂直方向位置）
		sf.Alignment = StringAlignment.Near;    // 画面中央（水平方向位置）//Changed to Left (Near) of Texture rect
		sf.FormatFlags = StringFormatFlags.NoClip | StringFormatFlags.NoWrap;
		
		//center vertically
		
		// レイアウト枠
		Rectangle r = new(0, 0, (int)(lWidth + nEdgePt * 2), (int)(stringSize.Height + nEdgePt * 2));
			
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
				Brush brushOutline = new LinearGradientBrush(r, gradationTopColor, gradationBottomColor, LinearGradientMode.Vertical);

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

		sf.Dispose();
		g.Dispose();

		return bmp;
	}
}
