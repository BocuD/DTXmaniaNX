using DTXMania.Core;
using DTXMania.UI.Drawable;

namespace DTXMania;

/// <summary>
/// The textures every song row shares. Loaded once for the list rather than per row, and handed to
/// <see cref="SongRowData"/> when it fills itself so the row model has no global state of its own.
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

    public BaseTexture Bar { get; private set; } = BaseTexture.None;
    public BaseTexture BoxClosed { get; private set; } = BaseTexture.None;
    public BaseTexture BoxOpen { get; private set; } = BaseTexture.None;
    public BaseTexture FallbackPreImage { get; private set; } = BaseTexture.None;

    public BaseTexture[] Lamps { get; private set; } = [];

    public SongSelectionAssets()
    {
        Bar = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\5_bar.png"));
        BoxClosed = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\5_box_closed.png"));
        BoxOpen = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\5_box_open.png"));
        FallbackPreImage = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\5_preimage default.png"));

        Lamps = new BaseTexture[6];
        for (int i = 0; i < Lamps.Length; i++)
        {
            Lamps[i] = BaseTexture.LoadFromPath(CSkin.Path($@"Graphics\Lamp\{i:00}.png"));
        }
    }

    public void Dispose()
    {
        Bar.Dispose();
        BoxClosed.Dispose();
        BoxOpen.Dispose();
        FallbackPreImage.Dispose();

        foreach (BaseTexture lamp in Lamps)
        {
            lamp.Dispose();
        }

        Lamps = [];
    }
}
