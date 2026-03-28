using System.IO;

namespace DTXMania.UI;

public static class UiFontDefaults
{
    public const string DefaultUiFontFileName = "NotoSansCJKjp-Regular.otf";

    public static string? TryGetDefaultUiFontPath()
    {
        string[] candidates =
        [
            Path.Combine(AppContext.BaseDirectory, "Fonts", DefaultUiFontFileName),
            Path.Combine(Directory.GetCurrentDirectory(), "Fonts", DefaultUiFontFileName),
            Path.Combine("Fonts", DefaultUiFontFileName)
        ];

        foreach (string candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
