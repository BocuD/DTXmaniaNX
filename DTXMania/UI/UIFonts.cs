using DTXMania.Core;
using DTXMania.UI.Skin;

namespace DTXMania.UI;

public enum FontSource
{
    System,
    Resource
}

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

    private static string[]? systemFontCache = null;
    public static string[] GetAvailableSystemFonts(bool ignoreCache = false)
    {
        if (systemFontCache != null && !ignoreCache)
        {
            return systemFontCache;
        }

        systemFontCache = [];

        List<string> temp = [];
        
        string[] folders =
        [
            Path.Combine(AppContext.BaseDirectory, "Fonts"),
            Path.Combine(Directory.GetCurrentDirectory(), "Fonts"),
            "Fonts",
        ];
        
        foreach (string folder in folders)
        {
            //get all ttf and otf files
            string[] files = Directory.GetFiles(folder);
            foreach (string file in files)
            {
                if (file.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".otf", StringComparison.OrdinalIgnoreCase))
                {
                    //add it if its not already there
                    string filename = Path.GetFileName(file);

                    if (!temp.Contains(filename))
                    {
                        temp.Add(filename);
                    }
                }
            }
        }
        
        systemFontCache = temp.ToArray();
        return systemFontCache;
    }
    
    private static string[]? resourceFontCache = null;
    public static string[] GetAvailableResourceFonts(bool ignoreCache = false)
    {
        if (resourceFontCache != null && !ignoreCache)
        {
            return resourceFontCache;
        }

        resourceFontCache = [];

        List<string> temp = [];
        
        if (CDTXMania.SkinManager.currentSkin != null)
        {
            string resourceFolder = Path.Combine(CDTXMania.SkinManager.currentSkin.basePath, SkinDescriptor.GetResourceFolder(ResourceType.Font));
            if (Directory.Exists(resourceFolder))
            {
                string[] files = Directory.GetFiles(resourceFolder);
                foreach (string file in files)
                {
                    if (file.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".otf", StringComparison.OrdinalIgnoreCase))
                    {
                        //add it if its not already there
                        string filename = Path.GetFileName(file);

                        if (!temp.Contains(filename))
                        {
                            temp.Add(filename);
                        }
                    }
                }
            }
        }
        
        resourceFontCache = temp.ToArray();
        return resourceFontCache;
    }
    
    public static string ResolveFontPath(FontSource source, string fontName)
    {
        switch (source)
        {
            case FontSource.Resource:
                string? resolvedPath = CDTXMania.SkinManager.currentSkin?.GetResource(ResourceType.Font, fontName);
                if (string.IsNullOrWhiteSpace(resolvedPath))
                {
                    return FallbackFontPath;
                }
                return resolvedPath;
                
            case FontSource.System:
                return GetSystemFont(fontName);
        }

        return FallbackFontPath;
    }
}
