using DTXMania.UI.Skin;

namespace DTXMania.UI;

public static class UIFonts
{
    //use MS PGothic for now as it used to be the default for DTXMania
    public const string DefaultUiFontFileName = "MS PGothic.otf";
    //public const string DefaultUiFontFileName = "NotoSansCJKjp-Regular.otf";

    public static string FallbackFontPath
    {
        get
        {
            _fallbackFontPath ??= GetSystemFont(DefaultUiFontFileName);
            return _fallbackFontPath;
        }
    }
    private static string? _fallbackFontPath;

    public static string FallbackFont => DefaultUiFontFileName;

    public static string GetSystemFont(string fontName)
    {
        foreach (string folder in SystemFontFolders)
        {
            string candidate = Path.Combine(folder, fontName);

            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return FallbackFont;
    }

    //a font a skin no longer has must still draw something, so an unresolvable reference falls back
    public static string ResolveFontPath(SkinResource font)
    {
        string path = font.Resolve(ResourceType.Font);
        return string.IsNullOrWhiteSpace(path) ? FallbackFontPath : path;
    }

    //where the browse menu lists system fonts from
    public static string SystemFontFolder
    {
        get
        {
            foreach (string folder in SystemFontFolders)
            {
                if (Directory.Exists(folder))
                {
                    return folder;
                }
            }

            return "Fonts";
        }
    }

    //the game's own font folders first, then whatever Windows already has
    private static string[] SystemFontFolders =>
    [
        Path.Combine(AppContext.BaseDirectory, "Fonts"),
        Path.Combine(Directory.GetCurrentDirectory(), "Fonts"),
        "Fonts",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts")
    ];
}
