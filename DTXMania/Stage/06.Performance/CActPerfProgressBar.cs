using System.Diagnostics;
using System.Drawing;
using DTXMania.Core;
using DTXMania.UI.Drawable;
using FDK;
using SkiaSharp;
using StbImageSharp;

namespace DTXMania;

internal class CActPerfProgressBar : CActivity
{
	public CActPerfProgressBar(bool bIsCalledFromOutsidePerformance = false)
	{
		this.bIsCalledFromOutsidePerformance = bIsCalledFromOutsidePerformance;
		bActivated = false;
	}

	public override void OnActivate()
	{
		if (bActivated)
			return;

		ct登場用 = null;
		epartプレイ楽器 = EInstrumentPart.DRUMS;
		nWidth = 20;
		nHeight = 540;

		pBarPosition[(int)EInstrumentPart.DRUMS] = new Point(855, 15);
		pBarPosition[(int)EInstrumentPart.GUITAR] = new Point(334, 85);
		pBarPosition[(int)EInstrumentPart.BASS] = new Point(1204, 85);

		nブロック最大数 = 10;
		n楽器毎のチップ数基準値.Drums = 1600;
		n楽器毎のチップ数基準値.Guitar = 800;
		n楽器毎のチップ数基準値.Bass = 800;

		try
		{
			for (EInstrumentPart ePart = EInstrumentPart.DRUMS; ePart <= EInstrumentPart.BASS; ePart++)
			{
				listProgressSection[(int)ePart] = new List<CProgressSection>();
				for (int i = 0; i < nSectionIntervalCount; i++)
				{
					listProgressSection[(int)ePart].Add(new CProgressSection());
				}

				if (!bIsCalledFromOutsidePerformance && CDTXMania.ConfigIni.bInstrumentAvailable(ePart) && CDTXMania.DTX.bHasChips[(int)ePart])
				{
					int x = pBarPosition[(int)ePart].X;
					p表示位置[(int)ePart] = new Point(x, 0);
				}
				else
				{
					p表示位置[(int)ePart] = new Point(0, 0);
				}
			}

			if (!bIsCalledFromOutsidePerformance)
			{
				nLastChipTime = CDTXMania.DTX.listChip[CDTXMania.DTX.listChip.Count - 1].nPlaybackTimeMs;
				foreach (CChip item in CDTXMania.DTX.listChip)
				{
					if (item.eInstrumentPart >= EInstrumentPart.DRUMS && item.eInstrumentPart <= EInstrumentPart.BASS)
					{
						int index = item.nPlaybackTimeMs * nSectionIntervalCount / nLastChipTime;
						listProgressSection[(int)item.eInstrumentPart][index].nChipCount++;
					}
				}
			}

			for (EInstrumentPart ePart2 = EInstrumentPart.DRUMS; ePart2 <= EInstrumentPart.BASS; ePart2++)
			{
				double num = (double)n楽器毎のチップ数基準値[(int)ePart2] / nブロック最大数 / nSectionIntervalCount;
				float y2 = nHeight;
				for (int j = 0; j < nSectionIntervalCount; j++)
				{
					CProgressSection c区間 = listProgressSection[(int)ePart2][j];
					int num2 = (int)(c区間.nChipCount / num) + 1;
					if (num2 > nブロック最大数)
					{
						num2 = nブロック最大数;
					}

					c区間.rectDrawingFrame.Y = (int)Math.Round(nHeight - ((double)j + 1.0) * nHeight / nSectionIntervalCount);
					c区間.rectDrawingFrame.Width = num2 * (nWidth / nブロック最大数);
					c区間.rectDrawingFrame.Height = y2 - c区間.rectDrawingFrame.Y;
					y2 = c区間.rectDrawingFrame.Y;
				}
			}
		}
		catch (Exception ex)
		{
			Trace.TraceError("プログレスバー活性化で例外が発生しました。");
			Trace.TraceError("例外 : " + ex.Message);
		}

		base.OnActivate();
	}

	public override void OnDeactivate()
	{
		if (bActivated)
		{
			ct登場用 = null;
		}

		base.OnDeactivate();
	}

	public override void OnManagedCreateResources()
	{
		if (!bActivated)
		{
			return;
		}

		tCreateBestProgressBarRecordTexture(CDTXMania.confirmedChart);
		tサイズが絡むテクスチャの生成();
		tx灰 = CreateTextureFromSurface(64, 64, canvas =>
		{
			canvas.Clear(SKColors.Transparent);
			using SKPaint fill = new() { Color = new SKColor(255, 255, 255, 64), Style = SKPaintStyle.Fill };
			canvas.DrawRect(SKRect.Create(64, 64), fill);
		}, "PerfProgressBarGray");
		tx黄 = CreateTextureFromSurface(64, 64, canvas =>
		{
			canvas.Clear(SKColors.Transparent);
			using SKPaint fill = new() { Color = new SKColor(255, 255, 0, 192), Style = SKPaintStyle.Fill };
			canvas.DrawRect(SKRect.Create(64, 64), fill);
		}, "PerfProgressBarYellow");
		tx青 = CreateTextureFromSurface(64, 64, canvas =>
		{
			canvas.Clear(SKColors.Transparent);
			using SKPaint fill = new() { Color = SKColors.DeepSkyBlue, Style = SKPaintStyle.Fill };
			canvas.DrawRect(SKRect.Create(64, 64), fill);
		}, "PerfProgressBarBlue");

		txProgressBarBackgroundDrums = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_Drum_Progress_bg.png"));
		txProgressBarBackgroundGuitar = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_Guitar_Progress_bg.png"));

		base.OnManagedCreateResources();
	}

	public override int OnUpdateAndDraw()
	{
		if (!bActivated)
		{
			return 0;
		}

		if (bIsCalledFromOutsidePerformance)
		{
			if (bJustStartedUpdate)
			{
				ct登場用 = new CCounter(0, 100, 3, CDTXMania.Timer);
				bJustStartedUpdate = false;
			}

			ct登場用?.tUpdate();
		}

		for (EInstrumentPart ePart = EInstrumentPart.DRUMS; ePart <= EInstrumentPart.BASS; ePart++)
		{
			if ((!bIsCalledFromOutsidePerformance &&
			     (!CDTXMania.ConfigIni.bInstrumentAvailable(ePart) ||
			      !CDTXMania.DTX.bHasChips[(int)ePart] ||
			      (EDarkMode)CDTXMania.ConfigIni.eDark == EDarkMode.FULL)) ||
			    (bIsCalledFromOutsidePerformance && epartプレイ楽器 != ePart && (epartプレイ楽器 != EInstrumentPart.UNKNOWN || ePart != 0)))
			{
				continue;
			}

			int num = p表示位置[(int)ePart].X + (bIsCalledFromOutsidePerformance ? 20 : 0);
			int num2 = p表示位置[(int)ePart].Y + (bIsCalledFromOutsidePerformance ? 20 : 0) + pBarPosition[(int)ePart].Y;
			if (bIsCalledFromOutsidePerformance)
			{
				num += (int)((-60 - p表示位置[(int)ePart].X) * Math.Cos(Math.PI / 200.0 * ct登場用.nCurrentValue));
				txパネル用.tDraw2D(num - 20, num2 - 20);
			}

			if (ePart == EInstrumentPart.DRUMS)
			{
				txProgressBarBackgroundDrums.tDraw2D(num - 2, num2 - 15);
			}
			else
			{
				txProgressBarBackgroundGuitar.tDraw2D(num - 2, num2 - 70);
			}

			txBackground.tDraw2D(num, num2);

			if (!ReferenceEquals(txBestProgressBarRecord[(int)ePart], BaseTexture.None))
			{
				txBestProgressBarRecord[(int)ePart].tDraw2D(num + 22, num2);
			}

			if (epartプレイ楽器 == EInstrumentPart.UNKNOWN)
			{
				continue;
			}

			if (!bIsCalledFromOutsidePerformance)
			{
				tx縦線.tDraw2D(num + nWidth, num2);
				int num3 = (int)(((CTimerBase)CDTXMania.Timer).n現在時刻ms / (double)nLastChipTime * nHeightFactor);
				if (num3 > nHeight)
				{
					num3 = nHeight;
				}

				RectangleF rectangle = new(0, 0, tx進捗.Width, num3);
				num2 = nHeight - num3 + pBarPosition[(int)ePart].Y;
				tx進捗.tDraw2D(num, num2, rectangle);
			}

			for (int i = 0; i < nSectionIntervalCount; i++)
			{
				CProgressSection c区間 = listProgressSection[(int)ePart][i];
				num2 = p表示位置[(int)ePart].Y + (bIsCalledFromOutsidePerformance ? 20 : 0) + (int)c区間.rectDrawingFrame.Y + pBarPosition[(int)ePart].Y;
				if (!CDTXMania.ConfigIni.bIsAutoPlay(ePart) || bIsCalledFromOutsidePerformance)
				{
					if ((i + 1) * nLastChipTime / nSectionIntervalCount - 1 > ((CTimerBase)CDTXMania.Timer).n現在時刻ms && !bIsCalledFromOutsidePerformance)
					{
						tx灰.tDraw2D(num, num2, c区間.rectDrawingFrame);
					}
					else
					{
						if (!c区間.bIsAttempted)
						{
							c区間.bIsAttempted = true;
						}

						if (c区間.nChipCount > 0)
						{
							if (c区間.nHitCount == c区間.nChipCount)
							{
								tx黄.tDraw2D(num, num2, c区間.rectDrawingFrame);
							}
							else
							{
								tx青.tDraw2D(num, num2, c区間.rectDrawingFrame);
							}
						}
					}
				}
				else if (c区間.nChipCount > 0)
				{
					tx灰.tDraw2D(num, num2, c区間.rectDrawingFrame);
				}
			}
		}

		return 0;
	}

	private void tCreateBestProgressBarRecordTexture(CScore cScore)
	{
		if (cScore == null)
		{
			return;
		}

		for (EInstrumentPart ePart = EInstrumentPart.DRUMS; ePart <= EInstrumentPart.BASS; ePart++)
		{
			BaseTexture currTexture = txBestProgressBarRecord[(int)ePart];
			txGenerateProgressBarLine(ref currTexture, cScore.SongInformation.progress[(int)CDTXMania.ConfigIni.GetFlipInst(ePart)]);
			txBestProgressBarRecord[(int)ePart] = currTexture;
		}
	}

	private void txGenerateProgressBarLine(ref BaseTexture txProgressBarTexture, string strProgressBar)
	{
		DisposeTexture(ref txProgressBarTexture);

		int nBarWidth = 8;
		int nBarHeight = nHeight;
		char[] arrProgress = strProgressBar.ToCharArray();

		if (arrProgress.Length == nSectionIntervalCount)
		{
			txProgressBarTexture = CreateTextureFromSurface(nBarWidth, nBarHeight, canvas =>
			{
				canvas.Clear(SKColors.Transparent);
				using SKPaint sectionPaint = new() { Style = SKPaintStyle.Fill, IsAntialias = false };
				using SKPaint borderPaint = new() { Color = SKColors.Gray, Style = SKPaintStyle.Fill, IsAntialias = false };

				int nOffsetY = nBarHeight;
				for (int i = 0; i < nSectionIntervalCount; i++)
				{
					int nCurrentPosY = (int)Math.Round(nBarHeight - ((double)i + 1.0) * nBarHeight / nSectionIntervalCount);
					int nCurrentSectionHeight = nOffsetY - nCurrentPosY;
					nOffsetY = nCurrentPosY;

					int nColorIndex = arrProgress[i] - '0';
					if (nColorIndex < 0 || nColorIndex > 3)
					{
						nColorIndex = 0;
					}

					sectionPaint.Color = clProgressBarColors[nColorIndex];
					canvas.DrawRect(SKRect.Create(2, nCurrentPosY, nBarWidth - 4, nCurrentSectionHeight), sectionPaint);
				}

				canvas.DrawRect(SKRect.Create(0, 0, 2, nBarHeight), borderPaint);
				canvas.DrawRect(SKRect.Create(6, 0, 1, nBarHeight), borderPaint);
			}, "PerfProgressBarRecord");
		}
		else
		{
			txProgressBarTexture = CreateTextureFromSurface(nBarWidth, nBarHeight, canvas =>
			{
				canvas.Clear(SKColors.Transparent);
				using SKPaint sectionPaint = new() { Color = clProgressBarColors[0], Style = SKPaintStyle.Fill, IsAntialias = false };
				using SKPaint borderPaint = new() { Color = SKColors.Gray, Style = SKPaintStyle.Fill, IsAntialias = false };
				canvas.DrawRect(SKRect.Create(2, 0, nBarWidth - 4, nBarHeight), sectionPaint);
				canvas.DrawRect(SKRect.Create(0, 0, 2, nBarHeight), borderPaint);
				canvas.DrawRect(SKRect.Create(6, 0, 2, nBarHeight), borderPaint);
			}, "PerfProgressBarRecordFallback");
		}
	}

	public static void txGenerateProgressBarHelper(ref BaseTexture txRefProgressBarTexture, string strProgressBar, int nWidth, int nHeight, int nIntervals)
	{
		if (strProgressBar == null)
		{
			return;
		}

		DisposeTexture(ref txRefProgressBarTexture);

		SKColor[] clBarColors =
		[
			SKColors.Black,
			SKColors.DeepSkyBlue,
			SKColors.Yellow,
			SKColors.Yellow
		];

		char[] arrProgress = strProgressBar.ToCharArray();
		if (arrProgress.Length == nIntervals)
		{
			txRefProgressBarTexture = CreateTextureFromSurface(nWidth, nHeight, canvas =>
			{
				canvas.Clear(SKColors.Transparent);
				using SKPaint sectionPaint = new() { Style = SKPaintStyle.Fill, IsAntialias = false };

				int nOffsetY = nHeight;
				for (int i = 0; i < nIntervals; i++)
				{
					int nCurrentPosY = (int)Math.Round(nHeight - ((double)i + 1.0) * nHeight / nIntervals);
					int nCurrentSectionHeight = nOffsetY - nCurrentPosY;
					nOffsetY = nCurrentPosY;

					int nColorIndex = arrProgress[i] - '0';
					if (nColorIndex < 0 || nColorIndex > 3)
					{
						nColorIndex = 0;
					}

					sectionPaint.Color = clBarColors[nColorIndex];
					canvas.DrawRect(SKRect.Create(0, nCurrentPosY, nWidth, nCurrentSectionHeight), sectionPaint);
				}
			}, "ProgressBarHelper");
		}
		else
		{
			txRefProgressBarTexture = CreateTextureFromSurface(nWidth, nHeight, canvas =>
			{
				canvas.Clear(SKColors.Transparent);
				using SKPaint sectionPaint = new() { Color = clBarColors[0], Style = SKPaintStyle.Fill, IsAntialias = false };
				canvas.DrawRect(SKRect.Create(0, 0, nWidth, nHeight), sectionPaint);
			}, "ProgressBarHelperFallback");
		}
	}

	private void tサイズが絡むテクスチャの生成()
	{
		DisposeTexture(ref txパネル用);
		if (bIsCalledFromOutsidePerformance)
		{
			txパネル用 = CreateTextureFromSurface(nWidth + 40, nHeight + 40, canvas =>
			{
				canvas.Clear(SKColors.Transparent);
				using SKPaint fill = new() { Color = new SKColor(255, 255, 255, 48), Style = SKPaintStyle.Fill };
				canvas.DrawRect(SKRect.Create(nWidth + 40, nHeight + 40), fill);
			}, "PerfProgressBarPanel");
		}

		DisposeTexture(ref txBackground);
		int num = 255;
		int backgroundWidth = nWidth + (bIsCalledFromOutsidePerformance ? 0 : 2);
		txBackground = CreateTextureFromSurface(backgroundWidth, nHeight, canvas =>
		{
			canvas.Clear(SKColors.Transparent);
			using SKPaint lightPaint = new() { Color = new SKColor(10, 10, 10, (byte)num), Style = SKPaintStyle.Fill, IsAntialias = false };
			using SKPaint darkPaint = new() { Color = new SKColor(14, 14, 14, (byte)num), Style = SKPaintStyle.Fill, IsAntialias = false };
			for (int y = 0; y < nHeight; y += 5)
			{
				SKPaint rowPaint = ((y / 5) % 2 == 0) ? lightPaint : darkPaint;
				canvas.DrawRect(SKRect.Create(0, y, backgroundWidth, Math.Min(5, nHeight - y)), rowPaint);
			}
		}, "PerfProgressBarBackground");

		DisposeTexture(ref tx縦線);
		tx縦線 = CreateTextureFromSurface(2, nHeight, canvas =>
		{
			canvas.Clear(SKColors.Transparent);
			using SKPaint line1 = new() { Color = new SKColor(255, 255, 255, (byte)((double)num / 255.0 * 64.0)), Style = SKPaintStyle.Fill, IsAntialias = false };
			using SKPaint line2 = new() { Color = new SKColor(255, 255, 255, (byte)((double)num / 255.0 * 32.0)), Style = SKPaintStyle.Fill, IsAntialias = false };
			canvas.DrawRect(SKRect.Create(0, 0, 1, nHeight), line1);
			canvas.DrawRect(SKRect.Create(1, 0, 1, nHeight), line2);
		}, "PerfProgressBarVerticalLine");

		DisposeTexture(ref tx進捗);
		tx進捗 = CreateTextureFromSurface(nWidth, nHeight, canvas =>
		{
			canvas.Clear(SKColors.Transparent);
			using SKPaint bodyPaint = new() { Color = new SKColor(255, 255, 255, 48), Style = SKPaintStyle.Fill, IsAntialias = false };
			using SKPaint headPaint = new() { Color = new SKColor(255, 255, 255, 128), Style = SKPaintStyle.Fill, IsAntialias = false };
			canvas.DrawRect(SKRect.Create(nWidth, nHeight), bodyPaint);
			canvas.DrawRect(SKRect.Create(0, 0, nWidth, 8), headPaint);
		}, "PerfProgressBarProgress");
	}

	public void Hit(EInstrumentPart inst, int nTime, EJudgement judge)
	{
		if (judge == EJudgement.Perfect || judge == EJudgement.Great || judge == EJudgement.Good)
		{
			listProgressSection[(int)inst][nTime * nSectionIntervalCount / nLastChipTime].nHitCount++;
		}
	}

	public string GetScoreIniString(EInstrumentPart inst)
	{
		string text = "";
		for (int i = 0; i < nSectionIntervalCount; i++)
		{
			text += GetSectionChar(listProgressSection[(int)inst][i]);
		}

		return text;
	}

	private string GetSectionChar(CProgressSection cProgressSection)
	{
		string ret = "0";
		if (cProgressSection.bIsAttempted)
		{
			if (cProgressSection.nChipCount > 0)
			{
				ret = cProgressSection.nHitCount == cProgressSection.nChipCount ? "2" : "1";
			}
			else
			{
				ret = "3";
			}
		}

		return ret;
	}

	private static BaseTexture CreateTextureFromSurface(int width, int height, Action<SKCanvas> draw, string name)
	{
		using SKSurface surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
		if (surface == null)
		{
			throw new InvalidOperationException($"Failed to create Skia surface for {name}.");
		}

		draw(surface.Canvas);

		using SKImage image = surface.Snapshot();
		using SKData encoded = image.Encode(SKEncodedImageFormat.Png, 100);
		if (encoded == null)
		{
			throw new InvalidOperationException($"Failed to encode Skia texture for {name}.");
		}

		byte[] encodedBytes = encoded.ToArray();
		ImageResult decoded = ImageResult.FromMemory(encodedBytes, ColorComponents.RedGreenBlueAlpha);
		return BaseTexture.LoadFromMemory(decoded.Data, decoded.Width, decoded.Height, name);
	}

	private static void DisposeTexture(ref BaseTexture texture)
	{
		if (texture != null && !ReferenceEquals(texture, BaseTexture.None))
		{
			texture.Dispose();
		}

		texture = BaseTexture.None;
	}

	public class CProgressSection
	{
		public int nChipCount;
		public int nHitCount;
		public bool bHasMistakes;
		public bool bIsAttempted;
		public RectangleF rectDrawingFrame;

		public CProgressSection()
		{
			nChipCount = 0;
			nHitCount = 0;
			bHasMistakes = true;
			bIsAttempted = false;
			rectDrawingFrame = new RectangleF(0, 0, 1, 1);
		}
	}

	private STDGBVALUE<List<CProgressSection>> listProgressSection;
	public static int nSectionIntervalCount = 64;
	private int nブロック最大数;
	private int nLastChipTime;
	private STDGBVALUE<int> n楽器毎のチップ数基準値;
	private BaseTexture txパネル用 = BaseTexture.None;
	private BaseTexture txBackground = BaseTexture.None;
	private BaseTexture tx縦線 = BaseTexture.None;
	private BaseTexture tx進捗 = BaseTexture.None;
	private BaseTexture tx灰 = BaseTexture.None;
	private BaseTexture tx黄 = BaseTexture.None;
	private BaseTexture tx青 = BaseTexture.None;
	private STDGBVALUE<BaseTexture> txBestProgressBarRecord;
	private BaseTexture txProgressBarBackgroundDrums = BaseTexture.None;
	private BaseTexture txProgressBarBackgroundGuitar = BaseTexture.None;
	private STDGBVALUE<Point> p表示位置;
	private int nWidth;
	private int nHeight;
	private STDGBVALUE<Point> pBarPosition;
	private readonly double nHeightFactor = 540.0;
	private readonly bool bIsCalledFromOutsidePerformance;
	private EInstrumentPart epartプレイ楽器;
	private CCounter ct登場用;
	private readonly SKColor[] clProgressBarColors =
	[
		SKColors.Black,
		SKColors.DeepSkyBlue,
		SKColors.Yellow,
		SKColors.Yellow
	];
}
