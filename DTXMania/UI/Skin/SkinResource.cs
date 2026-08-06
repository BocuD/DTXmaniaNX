using DTXMania.Core;
using DTXMania.UI.Drawable;

namespace DTXMania.UI.Skin;

public enum ResourceSource
{
    //the built-in base skin, under <exe>/System
    System,

    //the active skin's own resource folder
    Skin
}

public struct SkinResource
{
    [Themable] public ResourceSource source;

    //relative to the root the source names, e.g. "Graphics\5_bar.png" or "Sounds/decide.ogg"
    [Themable] public string path;

    public SkinResource(ResourceSource source, string path)
    {
        this.source = source;
        this.path = path;
    }

    public static SkinResource System(string path) => new(ResourceSource.System, path);

    public bool IsEmpty => string.IsNullOrWhiteSpace(path);

    public string Resolve(ResourceType type)
    {
        if (IsEmpty)
        {
            return string.Empty;
        }

        if (source == ResourceSource.System)
        {
            //fonts are the one thing the built-in skin does not hold: a system font is one of the game's
            //own Fonts folders or one Windows already has, so it is looked up rather than composed
            return type == ResourceType.Font ? UIFonts.GetSystemFont(path) : SkinManager.SystemPath(path);
        }

        if (CDTXMania.SkinManager.currentSkin is not { } skin)
        {
            return string.Empty;
        }

        return Path.Combine(skin.basePath, SkinDescriptor.GetResourceFolder(type), path);
    }

    public bool Exists(ResourceType type) => Resolve(type) is { Length: > 0 } full && File.Exists(full);

    public override string ToString() => IsEmpty ? "(none)" : $"{source}: {path}";
}
