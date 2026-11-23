namespace DTXMania.UI.OpenGL;

//packs rectangles into square pages. Each new height starts a "shelf", a horizontal strip filled left
//to right: naive, but it places in one pass and never has to repack
internal sealed class ShelfPacker
{
    internal sealed class Shelf
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
        public readonly List<Shelf> shelves = new();
        public int nextShelfY;
        public int entries;
    }

    //where one rectangle ended up
    internal sealed class Placement
    {
        public int pageIndex;
        public Shelf shelf = null!;
        public int x;
        public int width;

        public int y => shelf.y;
    }

    private readonly int pageSize;
    private readonly int maxPages;
    private readonly List<Page> pages = new();

    public ShelfPacker(int pageSize, int maxPages)
    {
        this.pageSize = pageSize;
        this.maxPages = maxPages;
    }

    public int PageCount => pages.Count;

    public bool TryTake(int width, int height, out Placement placement)
    {
        //rounded so entries of nearly the same height share shelves
        int shelfHeight = (height + 7) & ~7;

        for (int i = 0; i <= pages.Count; i++)
        {
            if (i == pages.Count)
            {
                if (pages.Count == maxPages)
                {
                    break;
                }

                pages.Add(new Page());
            }

            Page page = pages[i];

            //a shelf of about the right height first. Failing that any shelf tall enough will do:
            //its spare width is already paid for, while a new shelf costs the page more of its height
            for (int pass = 0; pass < 2; pass++)
            {
                for (int s = 0; s < page.shelves.Count; s++)
                {
                    Shelf shelf = page.shelves[s];
                    if (shelf.height < height || (pass == 0 && shelf.height > shelfHeight + 8))
                    {
                        continue;
                    }

                    for (int f = 0; f < shelf.freeX.Count; f++)
                    {
                        if (shelf.freeWidth[f] >= width)
                        {
                            //taken whole, so it goes back whole
                            placement = Placed(page, i, shelf, shelf.freeX[f], shelf.freeWidth[f]);
                            shelf.freeX.RemoveAt(f);
                            shelf.freeWidth.RemoveAt(f);
                            return true;
                        }
                    }

                    if (shelf.end + width <= pageSize)
                    {
                        placement = Placed(page, i, shelf, shelf.end, width);
                        shelf.end += width;
                        return true;
                    }
                }
            }

            if (page.nextShelfY + shelfHeight <= pageSize)
            {
                Shelf fresh = new() { y = page.nextShelfY, height = shelfHeight, end = width };
                page.shelves.Add(fresh);
                page.nextShelfY += shelfHeight;
                placement = Placed(page, i, fresh, 0, width);
                return true;
            }
        }

        placement = null!;
        return false;
    }

    private static Placement Placed(Page page, int pageIndex, Shelf shelf, int x, int width)
    {
        page.entries++;
        return new Placement { pageIndex = pageIndex, shelf = shelf, x = x, width = width };
    }

    public void Free(Placement placement)
    {
        Page page = pages[placement.pageIndex];
        Shelf shelf = placement.shelf;

        page.entries--;

        if (page.entries == 0)
        {
            page.shelves.Clear();
            page.nextShelfY = 0;
            return;
        }

        if (placement.x + placement.width != shelf.end)
        {
            shelf.freeX.Add(placement.x);
            shelf.freeWidth.Add(placement.width);
            return;
        }

        //space freed at the end of a shelf joins the unused part, along with any free slots it now touches
        shelf.end = placement.x;

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
                + $"{100 * filled / (pageSize * pageSize)}% used, {page.nextShelfY} of {pageSize} tall");
        }

        return text.ToString();
    }
}
