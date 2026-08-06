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

public struct SkinResourceRef
{
    [Themable] public ResourceSource source;

    //relative to the root the source names, e.g. "Graphics\5_bar.png" or "Sounds/decide.ogg"
    [Themable] public string path;

    public SkinResourceRef(ResourceSource source, string path)
    {
        this.source = source;
        this.path = path;
    }

    public static SkinResourceRef System(string path) => new(ResourceSource.System, path);

    public bool IsEmpty => string.IsNullOrWhiteSpace(path);

    public string Resolve(ResourceType type)
    {
        if (IsEmpty)
        {
            return string.Empty;
        }

        if (source == ResourceSource.System)
        {
            return SkinManager.SystemPath(path);
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
