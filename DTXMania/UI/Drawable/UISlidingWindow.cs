namespace DTXMania.UI.Drawable;

/// <summary>
/// A fixed set of item models covering a moving range of item indices, the data counterpart to
/// <see cref="UIScrollRing"/>. Entries live at <c>index mod length</c>, so moving the window costs
/// nothing: the entries that fall off one end are already in place at the other, and only their contents
/// need refilling.
///
/// Kept free of any drawable dependency so the wrap-around arithmetic can be tested directly.
/// </summary>
public sealed class UISlidingWindow<T> where T : class
{
    private T[] items = [];

    /// <summary>Lowest item index the window currently covers.</summary>
    public int Start { get; private set; }

    public int Length => items.Length;

    public bool Covers(int itemIndex) => items.Length > 0 && itemIndex >= Start && itemIndex < Start + items.Length;

    /// <summary>The entry for an item index. Only meaningful when <see cref="Covers"/> is true.</summary>
    public T At(int itemIndex) => items[Slot(itemIndex)];

    /// <summary>Grows or shrinks the window, keeping the entries that still fit.</summary>
    public void Resize(int length, Func<T> create)
    {
        length = Math.Max(0, length);
        if (items.Length == length)
        {
            return;
        }

        T[] resized = new T[length];
        for (int i = 0; i < length; i++)
        {
            resized[i] = i < items.Length ? items[i] : create();
        }

        items = resized;
    }

    /// <summary>Moves the window to start at <paramref name="start"/>, treating every entry as stale.</summary>
    public void Reset(int start) => Start = start;

    /// <summary>
    /// Moves the window by <paramref name="steps"/> items. Reports the run of item indices that the move
    /// brought into view and therefore need refilling: the new tail moving forward, the new head moving
    /// back. Returns false when the move is longer than the window, where nothing can be reused and the
    /// caller should refill the lot.
    /// </summary>
    public bool Shift(int steps, out int firstStaleItem, out int count)
    {
        firstStaleItem = Start;
        count = 0;

        if (items.Length == 0 || Math.Abs(steps) >= items.Length)
        {
            Start += steps;
            return false;
        }

        if (steps > 0)
        {
            firstStaleItem = Start + items.Length;
            count = steps;
        }
        else if (steps < 0)
        {
            firstStaleItem = Start + steps;
            count = -steps;
        }

        Start += steps;
        return count > 0;
    }

    private int Slot(int itemIndex) => ((itemIndex % items.Length) + items.Length) % items.Length;
}
