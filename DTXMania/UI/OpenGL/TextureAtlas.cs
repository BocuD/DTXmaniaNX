using System.Drawing;
using System.Numerics;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using Hexa.NET.ImGui;

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

    //single atlas
    internal sealed class Page
    {
        public uint textureId;
        public readonly List<Shelf> shelves = new();
        public int nextShelfY;
        public int entries;
    }

    //pages contain shelves; each new vertical size starts a "shelf", dividing up the atlas into horizontal strips
    //basically naive but fast packing
    internal sealed class Shelf
    {
        public int y;
        public int height;

        //where the never used part of the shelf starts
        public int end;

        public readonly List<int> freeX = new();
        public readonly List<int> freeWidth = new();
    }

    //where one entry sits in a page
    internal sealed class Slot
    {
        public Page page = null!;
        public Shelf shelf = null!;
        public int slotX;
        public int slotWidth;

        public uint textureId => page.textureId;
        public int pageSize => PageSize;
        public int regionX => slotX + Gutter;
        public int regionY => shelf.y + Gutter;
    }

    internal readonly OpenGlRenderer renderer;
    private readonly List<Page> pages = new();

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
            pixels.Height + Gutter * 2, slot.slotX, slot.shelf.y);

        return new AtlasTexture(this, slot, pixels.Width, pixels.Height, pixels.Name);
    }

    private Slot? Take(int width, int height)
    {
        if (!Enabled || width > MaxEntryWidth || height > MaxEntryHeight)
        {
            return null;
        }

        if (!TryAllocate(width + Gutter * 2, height + Gutter * 2, out Page page, out Shelf shelf, out int slotX,
                out int slotWidth))
        {
            return null;
        }

        page.entries++;
        return new Slot { page = page, shelf = shelf, slotX = slotX, slotWidth = slotWidth };
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

    public int PageCount => pages.Count;

    //Used by the inspector to give stats
    public string Describe()
    {
        System.Text.StringBuilder text = new();

        for (int i = 0; i < pages.Count; i++)
        {
            Page page = pages[i];
            int filled = 0;
            foreach (Shelf shelf in page.shelves)
            {
                filled += shelf.end * shelf.height;
            }

            text.AppendLine($"page {i}: {page.entries} entries, {page.shelves.Count} shelves, "
                + $"{100 * filled / (PageSize * PageSize)}% used, {page.nextShelfY} of {PageSize} tall");
        }

        return text.ToString();
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

    //try to find a slot for the given space
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

            //a shelf of about the right height first. Failing that any shelf tall enough will do:
            //its spare width is already paid for, while a new shelf costs the page more of its height
            for (int pass = 0; pass < 2; pass++)
            {
                for (int s = 0; s < page.shelves.Count; s++)
                {
                    shelf = page.shelves[s];
                    if (shelf.height < height || (pass == 0 && shelf.height > shelfHeight + 8))
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

    internal void Free(Slot slot)
    {
        Page page = slot.page;
        Shelf shelf = slot.shelf;
        int slotX = slot.slotX;
        int slotWidth = slot.slotWidth;

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

        //remove shelf when its empty
        if (shelf.end == 0 && shelf.y + shelf.height == page.nextShelfY)
        {
            page.shelves.Remove(shelf);
            page.nextShelfY = shelf.y;
        }
    }
}
