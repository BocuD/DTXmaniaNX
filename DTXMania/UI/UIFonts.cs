namespace DTXMania.UI;

public static class UIFonts
{
    //use MS PGothic for now as it used to be the default for DTXMania
    public const string DefaultUiFontFileName = "MS PGothic.otf";
    //public const string DefaultUiFontFileName = "NotoSansCJKjp-Regular.otf";

    public static string FallbackFont
    {
        get
        {
            _fallbackFont ??= GetFontByName(DefaultUiFontFileName);
            return _fallbackFont;
        }
    }
    private static string? _fallbackFont;
    
    public static string GetFontByName(string fontName)
    {
        string[] candidates =
        [
            Path.Combine(AppContext.BaseDirectory, "Fonts", fontName),
            Path.Combine(Directory.GetCurrentDirectory(), "Fonts", fontName),
            Path.Combine("Fonts", fontName),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts", fontName)
        ];
        
        foreach (string candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return FallbackFont;
    }
}
