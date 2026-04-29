using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using DTXMania.UI.Text;
using SkiaSharp;
using StbImageSharp;

namespace DTXMania.UI.OpenGL;

internal sealed class OpenGlSkiaTextRenderer : IUiTextRenderer
{
    public BaseTexture Render(UiTextRenderRequest request)
    {
        if (OpenGlRenderer.Instance == null)
        {
            throw new InvalidOperationException("OpenGL UI renderer is not available.");
        }

        if (string.IsNullOrEmpty(request.Text))
        {
            return BaseTexture.None;
        }

        using SKTypeface typeface = ResolveTypeface(request);
        using SKFont font = CreateFont(typeface, request);

        string[] lines = NormalizeLines(request.Text);
        SKFontMetrics metrics = font.Metrics;
        float ascent = -metrics.Ascent;
        float descent = metrics.Descent;
        float baseLineHeight = MathF.Max(font.Spacing, ascent + descent);
        float actualLineHeight = MathF.Max(baseLineHeight * request.LineSpacing, 1f);
        float effectivePadding = MathF.Max(request.TexturePadding + request.OutlineWidth + 2f, 2f);

        float maxLineWidth = 0f;
        foreach (string line in lines)
        {
            maxLineWidth = MathF.Max(maxLineWidth, MeasureLineWidth(font, line));
        }

        int bitmapWidth = Math.Max((int)MathF.Ceiling(maxLineWidth + effectivePadding * 2f), 1);
        int bitmapHeight = Math.Max((int)MathF.Ceiling(lines.Length * actualLineHeight + effectivePadding * 2f), 1);

        using SKSurface surface = SKSurface.Create(new SKImageInfo(bitmapWidth, bitmapHeight, SKColorType.Rgba8888, SKAlphaType.Premul));
        if (surface == null)
        {
            throw new InvalidOperationException("Failed to create Skia text surface.");
        }

        SKCanvas canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        using SKPaint fillPaint = CreateFillPaint(request, bitmapHeight);
        using SKPaint strokePaint = CreateStrokePaint(request, bitmapHeight);

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            float lineWidth = MeasureLineWidth(font, line);
            float drawX = effectivePadding + GetAlignedOffset(request.Alignment, bitmapWidth - effectivePadding * 2f, lineWidth);
            float drawY = effectivePadding + ascent + i * actualLineHeight;

            if (request.OutlineWidth > 0.01f)
            {
                canvas.DrawText(line, drawX, drawY, SKTextAlign.Left, font, strokePaint);
            }

            canvas.DrawText(line, drawX, drawY, SKTextAlign.Left, font, fillPaint);

            if (request.Style.HasFlag(UiTextStyle.Underline) && lineWidth > 0f)
            {
                DrawUnderline(canvas, drawX, drawY, lineWidth, metrics, fillPaint, request, bitmapHeight);
            }
        }

        using SKImage image = surface.Snapshot();
        using SKData encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        if (encoded == null)
        {
            throw new InvalidOperationException("Failed to encode Skia text image.");
        }

        byte[] encodedBytes = encoded.ToArray();
        ImageResult decoded = ImageResult.FromMemory(encodedBytes, ColorComponents.RedGreenBlueAlpha);
        return OpenGlTexture.CreateFromRgba32(OpenGlRenderer.Instance, decoded.Data, decoded.Width, decoded.Height, $"Text:{request.Name}");
    }

    private static string[] NormalizeLines(string value)
    {
        string normalized = value.Replace("\r\n", "\n");
        return normalized.Split('\n');
    }

    private static SKTypeface ResolveTypeface(UiTextRenderRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.FontPath) && File.Exists(request.FontPath))
        {
            SKTypeface? typefaceFromFile = SKTypeface.FromFile(request.FontPath);
            if (typefaceFromFile != null)
            {
                return typefaceFromFile;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.FontFamily))
        {
            SKTypeface? typefaceFromFamily = SKTypeface.FromFamilyName(request.FontFamily);
            if (typefaceFromFamily != null)
            {
                return typefaceFromFamily;
            }
        }

        return SKTypeface.Default;
    }

    private static SKFont CreateFont(SKTypeface typeface, UiTextRenderRequest request)
    {
        return new SKFont(typeface, request.FontSize)
        {
            Edging = request.SubpixelText ? SKFontEdging.SubpixelAntialias : SKFontEdging.Antialias,
            Subpixel = request.SubpixelText,
            Embolden = request.Style.HasFlag(UiTextStyle.Bold),
            SkewX = request.Style.HasFlag(UiTextStyle.Italic) ? -0.25f : 0f
        };
    }

    private static SKPaint CreateFillPaint(UiTextRenderRequest request, int bitmapHeight)
    {
        SKPaint paint = new()
        {
            IsAntialias = request.Antialias,
            Color = ToSkColor(request.FillColor),
            Style = SKPaintStyle.Fill,
            StrokeJoin = SKStrokeJoin.Round
        };

        paint.Shader = CreateGradientShader(
            request.FillGradientMode,
            request.FillGradientTopColor,
            request.FillGradientBottomColor,
            bitmapHeight);

        return paint;
    }

    private static SKPaint CreateStrokePaint(UiTextRenderRequest request, int bitmapHeight)
    {
        SKPaint paint = new()
        {
            IsAntialias = request.Antialias,
            Color = ToSkColor(request.OutlineColor),
            Style = SKPaintStyle.Stroke,
            StrokeJoin = SKStrokeJoin.Round,
            StrokeWidth = MathF.Max(request.OutlineWidth * 2f, 0.01f)
        };

        paint.Shader = CreateGradientShader(
            request.OutlineGradientMode,
            request.OutlineGradientTopColor,
            request.OutlineGradientBottomColor,
            bitmapHeight);

        return paint;
    }

    private static float MeasureLineWidth(SKFont font, string line)
    {
        return string.IsNullOrEmpty(line) ? 0f : font.MeasureText(line);
    }

    private static float GetAlignedOffset(UiTextAlignment alignment, float availableWidth, float lineWidth)
    {
        return alignment switch
        {
            UiTextAlignment.Center => MathF.Max((availableWidth - lineWidth) * 0.5f, 0f),
            UiTextAlignment.Right => MathF.Max(availableWidth - lineWidth, 0f),
            _ => 0f
        };
    }

    private static SKColor ToSkColor(Color4 color)
    {
        byte r = (byte)(Math.Clamp(color.Red, 0f, 1f) * 255f);
        byte g = (byte)(Math.Clamp(color.Green, 0f, 1f) * 255f);
        byte b = (byte)(Math.Clamp(color.Blue, 0f, 1f) * 255f);
        byte a = (byte)(Math.Clamp(color.Alpha, 0f, 1f) * 255f);
        return new SKColor(r, g, b, a);
    }

    private static SKShader? CreateGradientShader(UiTextGradientMode gradientMode, Color4 topColor, Color4 bottomColor, int bitmapHeight)
    {
        return gradientMode switch
        {
            UiTextGradientMode.Vertical => SKShader.CreateLinearGradient(
                new SKPoint(0f, 0f),
                new SKPoint(0f, MathF.Max(bitmapHeight, 1)),
                [ToSkColor(topColor), ToSkColor(bottomColor)],
                SKShaderTileMode.Clamp),
            _ => null
        };
    }

    private static void DrawUnderline(
        SKCanvas canvas,
        float startX,
        float baselineY,
        float lineWidth,
        SKFontMetrics metrics,
        SKPaint fillPaint,
        UiTextRenderRequest request,
        int bitmapHeight)
    {
        float underlineOffset = metrics.UnderlinePosition ?? request.FontSize * 0.08f;
        float underlineThickness = MathF.Max(metrics.UnderlineThickness ?? MathF.Max(request.FontSize * 0.05f, 1f), 1f);
        float underlineY = baselineY + underlineOffset + underlineThickness * 0.5f;

        if (request.OutlineWidth > 0.01f)
        {
            using SKPaint outlineUnderlinePaint = new()
            {
                IsAntialias = request.Antialias,
                Color = ToSkColor(request.OutlineColor),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = underlineThickness + request.OutlineWidth * 2f,
                StrokeCap = SKStrokeCap.Round,
                Shader = CreateGradientShader(
                    request.OutlineGradientMode,
                    request.OutlineGradientTopColor,
                    request.OutlineGradientBottomColor,
                    bitmapHeight)
            };

            canvas.DrawLine(startX, underlineY, startX + lineWidth, underlineY, outlineUnderlinePaint);
        }

        using SKPaint underlinePaint = new()
        {
            IsAntialias = request.Antialias,
            Color = ToSkColor(request.FillColor),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = underlineThickness,
            StrokeCap = SKStrokeCap.Round,
            Shader = CreateGradientShader(
                request.FillGradientMode,
                request.FillGradientTopColor,
                request.FillGradientBottomColor,
                bitmapHeight)
        };

        canvas.DrawLine(startX, underlineY, startX + lineWidth, underlineY, underlinePaint);
    }
}
