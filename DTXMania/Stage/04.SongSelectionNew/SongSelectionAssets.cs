using DTXMania.Core;
using DTXMania.UI.Drawable;

namespace DTXMania;

/// <summary>
/// The art a song row falls back to when a song has none of its own. The row's own art belongs to the
/// SongRow component, which names it; only this is picked at runtime.
/// </summary>
public sealed class SongSelectionAssets : IDisposable
{
    private static SongSelectionAssets? shared;

    //created on first use rather than with the list: the serializer builds a default instance of every
    //drawable type to compare against, and that must not load ten textures as a side effect
    public static SongSelectionAssets Shared => shared ??= new SongSelectionAssets();

    public static void DisposeShared()
    {
        shared?.Dispose();
        shared = null;
    }

    public BaseTexture FallbackPreImage { get; private set; } = BaseTexture.None;

    public SongSelectionAssets()
    {
        FallbackPreImage = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\5_preimage default.png"));
    }

    public void Dispose()
    {
        FallbackPreImage.Dispose();
    }
}
