using DTXMania.UI.Drawable;

namespace DTXMania.UI.OpenGL;

//used to reduce texture bind count and reduce draw calls
internal sealed class TextureAtlas
{
    public static bool Enabled = true;

    private const int PageSize = 2048;

    //64 MB with a 2048 page size (2048*2048*4 = 16MB)
    private const int MaxPages = 4;

    //if a texture is excessively large we would be wasting the atlas on it
    private const int MaxEntryWidth = 1024;
    private const int MaxEntryHeight = 1024;

    //linear filtering reads up to one texel past what is drawn so we need some padding for those cases
    private const int Gutter = 2;

    //an entry's page texture and where it sits inside it
    internal sealed class Slot
    {
        public uint textureId;
        public ShelfPacker.Placement placement = null!;

        public int pageSize => PageSize;
        public int regionX => placement.x + Gutter;
        public int regionY => placement.y + Gutter;
    }

    internal readonly OpenGlRenderer renderer;
    private readonly ShelfPacker packer = new(PageSize, MaxPages);
    private readonly List<uint> pageTextures = new();

    public TextureAtlas(OpenGlRenderer renderer)
    {
        this.renderer = renderer;
    }

    public BaseTexture Add(DecodedPixels pixels)
    {
        Slot? slot = Take(pixels.Width, pixels.Height);

        if (slot == null)
        {
            return OpenGlTexture.CreateFromRgba32(renderer, pixels.Rgba, pixels.Width, pixels.Height, pixels.Name);
        }

        renderer.UpdateTexture(slot.textureId, Padded(pixels), pixels.Width + Gutter * 2,
            pixels.Height + Gutter * 2, slot.placement.x, slot.placement.y);

        return new AtlasTexture(this, slot, pixels.Width, pixels.Height, pixels.Name);
    }

    internal Slot? AddFromTexture(uint sourceTextureId, int width, int height)
    {
        Slot? slot = Take(width, height);

        if (slot != null)
        {
            renderer.CopyIntoPage(sourceTextureId, width, height, slot.textureId, slot.regionX, slot.regionY);
        }

        return slot;
    }

    internal void Free(Slot slot) => packer.Free(slot.placement);

    private Slot? Take(int width, int height)
    {
        if (!Enabled || width > MaxEntryWidth || height > MaxEntryHeight)
        {
            return null;
        }

        if (!packer.TryTake(width + Gutter * 2, height + Gutter * 2, out ShelfPacker.Placement placement))
        {
            return null;
        }

        while (pageTextures.Count <= placement.pageIndex)
        {
            pageTextures.Add(renderer.CreateTextureEmpty(PageSize, PageSize));
        }

        return new Slot { textureId = pageTextures[placement.pageIndex], placement = placement };
    }

    //add padding to the entry
    private static byte[] Padded(DecodedPixels pixels)
    {
        int width = pixels.Width;
        int height = pixels.Height;
        int rowWidth = width + Gutter * 2;
        byte[] padded = new byte[rowWidth * (height + Gutter * 2) * 4];

        for (int row = 0; row < height; row++)
        {
            int rowStart = (row + Gutter) * rowWidth * 4;
            Buffer.BlockCopy(pixels.Rgba, row * width * 4, padded, rowStart + Gutter * 4, width * 4);

            for (int g = 0; g < Gutter; g++)
            {
                Buffer.BlockCopy(padded, rowStart + Gutter * 4, padded, rowStart + g * 4, 4);
                Buffer.BlockCopy(padded, rowStart + (Gutter + width - 1) * 4, padded,
                    rowStart + (Gutter + width + g) * 4, 4);
            }
        }

        for (int g = 0; g < Gutter; g++)
        {
            Buffer.BlockCopy(padded, Gutter * rowWidth * 4, padded, g * rowWidth * 4, rowWidth * 4);
            Buffer.BlockCopy(padded, (Gutter + height - 1) * rowWidth * 4, padded,
                (Gutter + height + g) * rowWidth * 4, rowWidth * 4);
        }

        return padded;
    }

    public int PageCount => packer.PageCount;

    //Used by the inspector to give stats
    public string Describe() => packer.Describe();
}
