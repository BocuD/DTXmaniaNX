using System.Drawing;
using System.Numerics;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using Hexa.NET.ImGui;

namespace DTXMania.UI.OpenGL;

//every text is its own small texture, and every texture change is a draw call of its own, which old drivers
//make expensive. Small text shares a few large pages instead, so a panel of labels and numbers batches together
internal sealed class TextAtlas
{
    private const int PageSize = 1024;
    private const int MaxPages = 8;
    private const int MaxEntryWidth = 512;
    private const int MaxEntryHeight = 256;

    //transparent texels kept around every entry: linear filtering reads up to one texel past what is drawn
    private const int Gutter = 2;

    private sealed class Shelf
    {
        public int y;
        public int height;

        //where the never used part of the shelf starts
        public int end;

        public readonly List<int> freeX = new();
        public readonly List<int> freeWidth = new();
    }

    private sealed class Page
    {
        public uint textureId;
        public readonly List<Shelf> shelves = new();
        public int nextShelfY;
        public int entries;
    }

    private readonly OpenGlRenderer renderer;
    private readonly List<Page> pages = new();

    public TextAtlas(OpenGlRenderer renderer)
    {
        this.renderer = renderer;
    }

    public BaseTexture Add(DecodedPixels pixels)
    {
        int width = pixels.Width;
        int height = pixels.Height;

        if (width > MaxEntryWidth || height > MaxEntryHeight)
        {
            return OpenGlTexture.CreateFromRgba32(renderer, pixels.Rgba, width, height, pixels.Name);
        }

        int slotHeight = height + Gutter * 2;
        int neededWidth = width + Gutter * 2;

        if (!TryAllocate(neededWidth, slotHeight, out Page page, out Shelf shelf, out int slotX, out int slotWidth))
        {
            return OpenGlTexture.CreateFromRgba32(renderer, pixels.Rgba, width, height, pixels.Name);
        }

        //the gutter is uploaded with the entry, which also clears whatever an earlier entry left there
        byte[] padded = new byte[neededWidth * slotHeight * 4];
        for (int row = 0; row < height; row++)
        {
            Buffer.BlockCopy(pixels.Rgba, row * width * 4, padded, ((row + Gutter) * neededWidth + Gutter) * 4, width * 4);
        }

        renderer.UpdateTexture(page.textureId, padded, neededWidth, slotHeight, slotX, shelf.y);
        page.entries++;

        return new AtlasTexture(this, page, shelf, slotX, slotWidth, width, height, pixels.Name);
    }

    private bool TryAllocate(int width, int height, out Page page, out Shelf shelf, out int slotX, out int slotWidth)
    {
        //rounded so entries of nearly the same height share shelves
        int shelfHeight = (height + 7) & ~7;

        for (int i = 0; i <= pages.Count; i++)
        {
            if (i == pages.Count)
            {
                if (pages.Count == MaxPages)
                {
                    break;
                }

                pages.Add(new Page { textureId = renderer.CreateTextureEmpty(PageSize, PageSize) });
            }

            page = pages[i];

            for (int s = 0; s < page.shelves.Count; s++)
            {
                shelf = page.shelves[s];
                if (shelf.height < height || shelf.height > shelfHeight + 8)
                {
                    continue;
                }

                for (int f = 0; f < shelf.freeX.Count; f++)
                {
                    if (shelf.freeWidth[f] >= width)
                    {
                        //taken whole, so it goes back whole
                        slotX = shelf.freeX[f];
                        slotWidth = shelf.freeWidth[f];
                        shelf.freeX.RemoveAt(f);
                        shelf.freeWidth.RemoveAt(f);
                        return true;
                    }
                }

                if (shelf.end + width <= PageSize)
                {
                    slotX = shelf.end;
                    slotWidth = width;
                    shelf.end += width;
                    return true;
                }
            }

            if (page.nextShelfY + shelfHeight <= PageSize)
            {
                shelf = new Shelf { y = page.nextShelfY, height = shelfHeight, end = width };
                page.shelves.Add(shelf);
                page.nextShelfY += shelfHeight;
                slotX = 0;
                slotWidth = width;
                return true;
            }
        }

        page = null!;
        shelf = null!;
        slotX = 0;
        slotWidth = 0;
        return false;
    }

    private void Free(Page page, Shelf shelf, int slotX, int slotWidth)
    {
        page.entries--;

        if (page.entries == 0)
        {
            page.shelves.Clear();
            page.nextShelfY = 0;
            return;
        }

        if (slotX + slotWidth != shelf.end)
        {
            shelf.freeX.Add(slotX);
            shelf.freeWidth.Add(slotWidth);
            return;
        }

        //space freed at the end of a shelf joins the unused part, along with any free slots it now touches
        shelf.end = slotX;

        bool merged = true;
        while (merged)
        {
            merged = false;

            for (int f = 0; f < shelf.freeX.Count; f++)
            {
                if (shelf.freeX[f] + shelf.freeWidth[f] == shelf.end)
                {
                    shelf.end = shelf.freeX[f];
                    shelf.freeX.RemoveAt(f);
                    shelf.freeWidth.RemoveAt(f);
                    merged = true;
                    break;
                }
            }
        }

        //an empty top shelf gives its height back, so the page can take entries of another height there
        if (shelf.end == 0 && shelf.y + shelf.height == page.nextShelfY)
        {
            page.shelves.Remove(shelf);
            page.nextShelfY = shelf.y;
        }
    }

    private sealed class AtlasTexture : BaseTexture
    {
        private readonly TextAtlas atlas;
        private readonly Page page;
        private readonly Shelf shelf;
        private readonly int slotX;
        private readonly int slotWidth;
        private readonly int regionX;
        private readonly int regionY;
        private bool released;

        public override int Width { get; }
        public override int Height { get; }
        public override string name { get; }

        public AtlasTexture(TextAtlas atlas, Page page, Shelf shelf, int slotX, int slotWidth, int width, int height, string name)
        {
            this.atlas = atlas;
            this.page = page;
            this.shelf = shelf;
            this.slotX = slotX;
            this.slotWidth = slotWidth;
            regionX = slotX + Gutter;
            regionY = shelf.y + Gutter;
            Width = width;
            Height = height;
            this.name = name;
        }

        private RectangleF InPage(RectangleF clipRect) => new(clipRect.X + regionX, clipRect.Y + regionY, clipRect.Width, clipRect.Height);

        public override void tDraw2DMatrix(Matrix4x4 transformMatrix, Vector2 size, RectangleF clipRect, Color4 color)
        {
            atlas.renderer.DrawTexture(page.textureId, PageSize, PageSize, transformMatrix, size, InPage(clipRect), color, blendMode);
        }

        public override void tDraw2DMatrixSliced(Matrix4x4 transformMatrix, Vector2 size, RectangleF clipRect, Color4 color, RectangleF sliceRect)
        {
            atlas.renderer.DrawTextureSliced(page.textureId, PageSize, PageSize, transformMatrix, size, InPage(clipRect), color, sliceRect, blendMode);
        }

        public override void UpdateRgba32(ReadOnlySpan<byte> rgbaPixels, int width, int height, int dstX = 0, int dstY = 0)
        {
            if (released)
            {
                throw new ObjectDisposedException(name, "Cannot update a disposed texture.");
            }

            if (dstX < 0 || dstY < 0 || width <= 0 || height <= 0 || dstX + width > Width || dstY + height > Height)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Update region must stay within the texture bounds.");
            }

            atlas.renderer.UpdateTexture(page.textureId, rgbaPixels, width, height, regionX + dstX, regionY + dstY);
        }

        public override bool IsValid() => !released;

        //the whole page: ImGui has no way to be handed a part of a texture as a texture of its own
        public override ImTextureID? GetImTextureID() => released ? null : new ImTextureID((nint)page.textureId);

        public override void Dispose()
        {
            if (released)
            {
                return;
            }

            released = true;
            atlas.Free(page, shelf, slotX, slotWidth);
        }
    }
}
