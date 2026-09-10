using System.Text;
using SkiaSharp;

namespace DTXMania.UI.OpenGL;

/// <summary>
/// Skia draws a character its font does not have as a blank box, so to prevent it we can split up a line into runs and use a supported font on a per run basis
/// </summary>
internal sealed class FontSet : IDisposable
{
    internal readonly record struct Run(string Text, SKFont Font);

    private readonly record struct FallbackKey(int Codepoint, int Weight, int Width, SKFontStyleSlant Slant);

    //MatchCharacter hits the system font manager
    private static readonly Dictionary<FallbackKey, SKTypeface?> FallbackCache = new();
    private static readonly object FallbackSync = new();

    private readonly Func<SKTypeface, SKFont> makeFont;
    private readonly Dictionary<SKTypeface, SKFont> fonts = new();

    //metrics come from here even when a run uses a fallback, so two texts of the same size line up
    public SKFont Primary { get; }

    private readonly SKTypeface primaryTypeface;

    public FontSet(SKTypeface typeface, Func<SKTypeface, SKFont> makeFont)
    {
        this.makeFont = makeFont;

        primaryTypeface = typeface;
        Primary = makeFont(typeface);
        fonts[typeface] = Primary;
    }

    public float Measure(string line) => Width(Split(line));

    public static float Width(List<Run> runs)
    {
        float width = 0.0f;

        foreach (Run run in runs)
        {
            width += run.Font.MeasureText(run.Text);
        }

        return width;
    }

    public List<Run> Split(string line)
    {
        List<Run> runs = [];

        if (string.IsNullOrEmpty(line))
        {
            return runs;
        }

        SKTypeface current = primaryTypeface;
        int start = 0;

        for (int i = 0; i < line.Length;)
        {
            if (!Rune.TryGetRuneAt(line, i, out Rune rune))
            {
                rune = Rune.ReplacementChar;
            }

            SKTypeface typeface = TypefaceFor(rune);

            if (typeface != current && i > start)
            {
                runs.Add(new Run(line[start..i], FontFor(current)));
                start = i;
            }

            current = typeface;
            i += rune.Utf16SequenceLength;
        }

        runs.Add(new Run(line[start..], FontFor(current)));

        return runs;
    }

    private SKTypeface TypefaceFor(Rune rune)
        => primaryTypeface.ContainsGlyph(rune.Value) ? primaryTypeface : Fallback(rune.Value) ?? primaryTypeface;

    //match the fallback with the primary fonts' styling
    private SKTypeface? Fallback(int codepoint)
    {
        SKFontStyle style = primaryTypeface.FontStyle;
        FallbackKey key = new(codepoint, style.Weight, style.Width, style.Slant);

        lock (FallbackSync)
        {
            if (FallbackCache.TryGetValue(key, out SKTypeface? cached))
            {
                return cached;
            }

            SKTypeface? match = SKFontManager.Default.MatchCharacter(null, style, null, codepoint);
            FallbackCache[key] = match;

            return match;
        }
    }

    private SKFont FontFor(SKTypeface typeface)
    {
        if (fonts.TryGetValue(typeface, out SKFont? font))
        {
            return font;
        }

        font = makeFont(typeface);
        fonts[typeface] = font;

        return font;
    }

    public void Dispose()
    {
        foreach (SKFont font in fonts.Values)
        {
            font.Dispose();
        }

        fonts.Clear();
    }
}
