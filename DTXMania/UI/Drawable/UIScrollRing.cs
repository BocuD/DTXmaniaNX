namespace DTXMania.UI.Drawable;

/// <summary>
/// The scroll position of a recycling list: a fixed set of slots covering a moving window over a longer
/// list of items. Holds both where the list currently sits and where it is heading, because the two must
/// shift together when an item passes — splitting them across classes is what made three separate bugs
/// possible, each from recombining them slightly wrong.
///
/// Kept free of any drawable dependency so it can be reasoned about, and tested, on its own.
/// </summary>
public sealed class UIScrollRing
{
    //below this the move is finished outright: easing the last fraction takes unbounded frames
    private const float SettleDistance = 0.01f;

    public int SlotCount { get; }
    public float Spacing { get; }

    //rotation of the ring: which slot currently holds the topmost visible item
    public int FirstSlot { get; private set; }

    //item index held by FirstSlot
    public int FirstItem { get; private set; }

    private long lastTime;

    /// <summary>
    /// Where the list sits within the current item, always in [-Spacing/2, Spacing/2]. Layout displaces
    /// every slot by this.
    /// </summary>
    public float Offset { get; private set; }

    /// <summary>
    /// Where <see cref="Offset"/> is heading. A passing item shifts this and <see cref="Offset"/> by the
    /// same amount, so it returns to zero the moment the last queued item arrives — before the list has
    /// finished easing.
    /// </summary>
    public float Target { get; private set; }

    /// <summary>
    /// Whether the selection has stopped changing. Note this goes true while the list is still visibly
    /// easing back onto the settled item: callers wanting "the user has picked something" want this,
    /// callers wanting "nothing is moving" do not.
    /// </summary>
    public bool IsSettled => Math.Abs(Target) <= SettleDistance;

    public UIScrollRing(int slotCount, float spacing)
    {
        if (slotCount <= 0) throw new ArgumentOutOfRangeException(nameof(slotCount));
        if (spacing <= 0) throw new ArgumentOutOfRangeException(nameof(spacing));

        SlotCount = slotCount;
        Spacing = spacing;
    }

    /// <summary>The slot holding the item <paramref name="position"/> places down the visible window.</summary>
    public int SlotAt(int position) => ((FirstSlot + position) % SlotCount + SlotCount) % SlotCount;

    /// <summary>The item index shown at <paramref name="position"/> down the visible window.</summary>
    public int ItemAt(int position) => FirstItem + position;

    /// <summary>
    /// Queues movement, clamped by <paramref name="motion"/> so a held key cannot run further ahead than
    /// what is on screen. Positive moves the list towards earlier items.
    /// </summary>
    public void Queue(float distance, UIScrollMotion motion)
        => Target = motion.ClampQueue((Target + distance) / Spacing) * Spacing;

    /// <summary>
    /// Moves towards the queued position and reports how many whole items passed under the selection:
    /// negative towards earlier items, positive towards later ones. Times itself from the clock the
    /// caller passes, since the first frame after the list is built has nothing to measure against.
    /// </summary>
    public int Advance(long nowMilliseconds, UIScrollMotion motion)
    {
        float elapsed = lastTime == 0 ? 0.0f : (nowMilliseconds - lastTime) / 1000.0f;
        lastTime = nowMilliseconds;

        return Ease(elapsed, motion);
    }

    private int Ease(float elapsedSeconds, UIScrollMotion motion)
    {
        //what is queued is never discarded, only spent: a frame too short to measure simply does not
        //move, which at high frame rates is most of them
        float remaining = Target - Offset;
        Offset += Math.Abs(remaining) <= SettleDistance
            ? remaining
            : motion.Step(remaining / Spacing, elapsedSeconds) * Spacing;

        int steps = 0;
        float half = Spacing / 2f;

        //an item passing shifts both, so the distance still to travel is preserved across it
        while (Offset > half)
        {
            Offset -= Spacing;
            Target -= Spacing;
            steps--;
        }

        while (Offset < -half)
        {
            Offset += Spacing;
            Target += Spacing;
            steps++;
        }

        if (steps != 0)
        {
            FirstItem += steps;
            FirstSlot = ((FirstSlot + steps) % SlotCount + SlotCount) % SlotCount;
        }

        return steps;
    }

    /// <summary>Puts <paramref name="itemIndex"/> at the top of the window, abandoning any queued scroll.</summary>
    public void JumpTo(int itemIndex)
    {
        FirstItem = itemIndex;
        FirstSlot = 0;
        Offset = 0f;
        Target = 0f;
    }
}
