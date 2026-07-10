using System.Numerics;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using DTXMania.UI.Text;
using SkiaSharp;

namespace DTXMania.UI.OpenGL;

internal sealed class OpenGlSkiaTextRenderer : IUiTextRenderer
{
    public BaseTexture Render(UiTextParameters request)
    {
        if (OpenGlRenderer.Instance == null)
        {
            throw new InvalidOperationException("OpenGL UI renderer is not available.");
        }

        DecodedPixels pixels = RenderToPixels(request);
        if (!pixels.IsValid)
        {
            return BaseTexture.None;
        }

        return OpenGlTexture.CreateFromRgba32(OpenGlRenderer.Instance, pixels.Rgba, pixels.Width, pixels.Height, pixels.Name);
    }

    public DecodedPixels RenderToPixels(UiTextParameters request)
    {
        if (string.IsNullOrEmpty(request.Text))
        {
            return default;
        }

        SKTypeface typeface = ResolveTypeface(request);
        using SKFont font = CreateFont(typeface, request);

        string[] lines = NormalizeLines(request.Text);
        SKFontMetrics metrics = font.Metrics;
        float ascent = -metrics.Ascent;
        float descent = metrics.Descent;
        float baseLineHeight = MathF.Max(font.Spacing, ascent + descent);
        float actualLineHeight = MathF.Max(baseLineHeight * request.LineSpacing, 1f);
        Vector2 effectivePadding = Vector2.Max(request.TexturePadding + new Vector2(request.OutlineWidth + 2f), new Vector2(2f));

        float maxLineWidth = 0f;
        foreach (string line in lines)
        {
            maxLineWidth = MathF.Max(maxLineWidth, MeasureLineWidth(font, line));
        }

        int bitmapWidth = Math.Max((int)MathF.Ceiling(maxLineWidth + effectivePadding.X * 2f), 1);
        int bitmapHeight = Math.Max((int)MathF.Ceiling(lines.Length * actualLineHeight + effectivePadding.Y * 2f), 1);

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
            float drawX = effectivePadding.X + GetAlignedOffset(request.Alignment, bitmapWidth - effectivePadding.X * 2f, lineWidth);
            float drawY = effectivePadding.Y + ascent + i * actualLineHeight;

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

        // Read the rasterized pixels straight out of the surface as straight-alpha (Unpremul)
        // RGBA. The surface renders premultiplied for correct outline-over-fill blending, so we
        // convert on read-back. This avoids the previous PNG encode + re-decode round-trip.
        SKImageInfo readInfo = new(bitmapWidth, bitmapHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        byte[] rgba = new byte[readInfo.BytesSize];
        bool read;
        unsafe
        {
            fixed (byte* dst = rgba)
            {
                read = surface.ReadPixels(readInfo, (nint)dst, readInfo.RowBytes, 0, 0);
            }
        }

        if (!read)
        {
            throw new InvalidOperationException("Failed to read back Skia text pixels.");
        }

        return new DecodedPixels(rgba, bitmapWidth, bitmapHeight, $"Text:{request.Name}");
    }

    private static string[] NormalizeLines(string value)
    {
        string normalized = value.Replace("\r\n", "\n");
        return normalized.Split('\n');
    }

    // Typefaces are immutable and expensive to load (SKTypeface.FromFile hits disk). Cache and
    // reuse them across renders instead of loading one per call. Never disposed by callers.
    private static readonly Dictionary<string, SKTypeface> TypefaceCache = new();
    private static readonly object TypefaceCacheSync = new();

    private static SKTypeface ResolveTypeface(UiTextParameters request)
    {
        string key = !string.IsNullOrWhiteSpace(request.FontPath)
            ? "file:" + request.FontPath
            : !string.IsNullOrWhiteSpace(request.FontFamily)
                ? "family:" + request.FontFamily
                : "default";

        lock (TypefaceCacheSync)
        {
            if (TypefaceCache.TryGetValue(key, out SKTypeface? cached))
            {
                return cached;
            }

            SKTypeface typeface = LoadTypeface(request) ?? SKTypeface.Default;
            TypefaceCache[key] = typeface;
            return typeface;
        }
    }

    private static SKTypeface? LoadTypeface(UiTextParameters request)
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
            return SKTypeface.FromFamilyName(request.FontFamily);
        }

        return null;
    }

    private static SKFont CreateFont(SKTypeface typeface, UiTextParameters request)
    {
        return new SKFont(typeface, request.FontSize)
        {
            Edging = request.SubpixelText ? SKFontEdging.SubpixelAntialias : SKFontEdging.Antialias,
            Subpixel = request.SubpixelText,
            Embolden = request.Style.HasFlag(UiTextStyle.Bold),
            SkewX = request.Style.HasFlag(UiTextStyle.Italic) ? -0.25f : 0f
        };
    }

    private static SKPaint CreateFillPaint(UiTextParameters request, int bitmapHeight)
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

    private static SKPaint CreateStrokePaint(UiTextParameters request, int bitmapHeight)
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
        UiTextParameters request,
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
