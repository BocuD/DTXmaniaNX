using System.Diagnostics;
using Kawazu;

namespace DTXMania.SongDb;

public class TextConversionCache
{
    private static KawazuConverter jpConverter = new();
    private static readonly object converterSync = new();
    private static Dictionary<string, (string kana, string romaji)> cache = new();
    
    public static (string kana, string romaji) GetOrCacheTextConversion(string originalText)
    {
        try
        {
            if (cache.TryGetValue(originalText, out var cached))
            {
                return cached;
            }

            var (kana, romaji) = Convert(originalText);

            try
            {
                cache[originalText] = (kana, romaji);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"Failed to cache text conversion: {ex.Message}");
            }

            return (kana, romaji);
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"Error accessing text conversion cache: {ex.Message}");
            return Convert(originalText);
        }
    }

    private static (string kana, string romaji) Convert(string input)
    {
        lock (converterSync)
        {
            return (jpConverter.Convert(input).Result,
            jpConverter.Convert(input, To.Romaji).Result);
        }
    }

    /// <summary>Splits <paramref name="input"/> into morphemes, in order. Empty if it cannot be read.</summary>
    //slow enough per call that callers memoize. Shares the tagger above rather than loading IpaDic twice.
    public static List<string> Segment(string input)
    {
        try
        {
            lock (converterSync)
            {
                return jpConverter.GetDivisions(input).Result.Select(division => division.Surface).ToList();
            }
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"Failed to segment Japanese text: {ex.Message}");
            return [];
        }
    }
}