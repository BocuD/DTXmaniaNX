using System.Diagnostics;
using Kawazu;

namespace DTXMania.SongDb;

public class TextConversionCache
{
    private static KawazuConverter jpConverter = new();
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
        return (jpConverter.Convert(input).Result,
        jpConverter.Convert(input, To.Romaji).Result);
    }
}