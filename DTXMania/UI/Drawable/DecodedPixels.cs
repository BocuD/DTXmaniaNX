namespace DTXMania.UI.Drawable;

/// <summary>
/// CPU-side RGBA pixel data produced off the main thread
/// ready to be uploaded to a GPU texture on the main thread via <see cref="BaseTexture.LoadFromMemory"/>.
/// Alpha is straight (non-premultiplied)
/// Used by (for example) text rendering and async image decoding
/// </summary>
public readonly struct DecodedPixels
{
    public readonly byte[] Rgba;
    public readonly int Width;
    public readonly int Height;
    public readonly string Name;

    public DecodedPixels(byte[] rgba, int width, int height, string name)
    {
        Rgba = rgba;
        Width = width;
        Height = height;
        Name = name;
    }

    public bool IsValid => Rgba != null && Width > 0 && Height > 0;

    public long ByteCount => Rgba?.LongLength ?? 0;
}
