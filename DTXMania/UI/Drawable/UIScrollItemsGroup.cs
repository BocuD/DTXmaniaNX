using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Inspector;
using Hexa.NET.ImGui;
using Newtonsoft.Json;

namespace DTXMania.UI.Drawable;

/// <summary>
/// A <see cref="UIItemsGroup"/> for lists longer than the screen: it stamps <see cref="visibleSlots"/>
/// copies of the component and recycles them as the window scrolls, so the cost is the window size rather
/// than the item count. <see cref="UIScrollRing"/> owns the wrap-around arithmetic.
///
/// The selected item sits at the group's origin, so a skin positions the list by where its selection
/// should be. Slots are laid out around that, displaced by <see cref="curve"/>.
/// </summary>
public class UIScrollItemsGroup : UIItemsGroup
{
    //how many copies exist; enough to cover the visible window plus overscan at each end
    [Themable] public int visibleSlots = 8;

    //which position in the window counts as selected, e.g. the middle row of a song list
    [Themable] public int selectionOffset;

    //how the list travels towards where it has been asked to go; see UIScrollMotion
    [Themable] [SkinSerialize] public UIScrollMotion motion = new();

    //which input direction drives this list. Separate from itemOffset on purpose: a list can be laid out
    //diagonally, or run bottom-to-top, and still be navigated with the obvious keys
    [Themable] public UINavigationAxis navigationAxis = UINavigationAxis.Vertical;

    //flips which way that input moves the list
    [Themable] public bool invertNavigation;

    //displaces items by their distance from the selection; see UIItemCurve
    [Themable] [SkinSerialize] public UIItemCurve curve = new();

    [JsonIgnore] private UIScrollRing? ring;

    public UIScrollItemsGroup() : base("ScrollItems")
    {
    }

    public UIScrollItemsGroup(string name) : base(name)
    {
    }

    /// <summary>The item index currently under the selection position. Setting it scrolls there.</summary>
    public override int SelectedItem
    {
        get => (ring?.FirstItem ?? 0) + selectionOffset;
        set => ScrollTo(value);
    }

    //a scrolling list moves by scrolling, which is where the queue limit and the easing live
    protected override void MoveSelection(int items) => ScrollBy(items);

    /// <summary>
    /// True while the selection is still going to change, so callers can hold off on work that should
    /// only happen once the user has settled on an item — loading a preview, say. This goes false while
    /// the list is still visibly easing, which is deliberate: see <see cref="UIScrollRing.IsSettled"/>.
    /// </summary>
    public bool IsScrolling => ring is { IsSettled: false };

    /// <summary>Scrolls by whole items, travelling there over the following frames. Positive moves towards
    /// later items; how far a held key may run ahead is <see cref="motion"/>'s to decide.
    /// Honours <see cref="invertNavigation"/>, so callers pass the direction the user asked for.</summary>
    public void ScrollBy(int items)
    {
        if (invertNavigation)
        {
            items = -items;
        }

        EnsureRing().Queue(-items * SpacingAlongAxis, motion);
    }

    /// <summary>Whether this list responds to the given input direction, so an owner can wire only the
    /// keys the list actually uses.</summary>
    public bool RespondsTo(UINavigationAxis axis) => navigationAxis == axis;

    /// <summary>Puts <paramref name="itemIndex"/> under the selection position immediately.</summary>
    public void ScrollTo(int itemIndex)
    {
        EnsureSlots();
        EnsureRing().JumpTo(itemIndex - selectionOffset);
        RebindAllSlots();
        LayOutSlots();
    }

    //the ring works in whole items along the list's own direction, whichever way that points
    protected float SpacingAlongAxis => ItemDistance;

    /// <summary>Called after the window moved by whole items, so a list can react to the selection
    /// changing. Not called for the sub-item movement in between.</summary>
    protected virtual void OnScrolled(int steps)
    {
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        EnsureSlots();

        int steps = EnsureRing().Advance(CDTXMania.Timer.nCurrentTime, motion);
        if (steps != 0)
        {
            //the list updates its data first, then slots are pointed at the new indices
            OnScrolled(steps);
            RebindAllSlots();
        }

        LayOutSlots();
        base.Draw(parentMatrix);
    }

    //the selection sits at the group's origin, so slots run outwards from there and the curve measures
    //distance from that same point
    protected override void LayOutSlots()
    {
        if (ring == null)
        {
            return;
        }

        //the whole list slides by the scroll offset, along the direction its items are spaced in
        Vector3 direction = ItemDirection;
        Vector3 scrolled = direction * ring.Offset;

        for (int position = 0; position < Slots.Count; position++)
        {
            Vector3 slotPosition = itemOffset * (position - selectionOffset) + scrolled;

            if (curve.IsActive)
            {
                //how far down the list this item sits, which for a diagonal list is its projection
                slotPosition += curve.Evaluate(Vector3.Dot(slotPosition, direction));
            }

            Slots[ring.SlotAt(position)].position = slotPosition;
        }
    }

    //a recycled slot only changes which item index it reads, so its children keep their bindings
    private void RebindAllSlots()
    {
        if (ring == null)
        {
            return;
        }

        for (int position = 0; position < Slots.Count; position++)
        {
            Slots[ring.SlotAt(position)].SetItemIndex(ring.ItemAt(position));
        }
    }

    private UIScrollRing EnsureRing()
    {
        if (ring == null || ring.SlotCount != Slots.Count || Math.Abs(ring.Spacing - SpacingAlongAxis) > 0.001f)
        {
            ring = new UIScrollRing(Math.Max(1, Slots.Count), SpacingAlongAxis);
            RebindAllSlots();
        }

        return ring;
    }

    //the window size is what decides how many slots exist, not the length of the list
    protected override int SlotCountFor(int itemCount) => Math.Max(1, visibleSlots);

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (!ImGui.CollapsingHeader("Scrolling"))
        {
            return;
        }

        if (ImGui.InputInt("Visible Slots", ref visibleSlots))
        {
            visibleSlots = Math.Max(1, visibleSlots);
        }

        ImGui.InputInt("Selection Offset", ref selectionOffset);
        Inspector.Inspector.Inspect("Navigation Axis", ref navigationAxis);
        ImGui.Checkbox("Invert Navigation", ref invertNavigation);

        ImGui.SeparatorText("Motion");
        motion.DrawInspector();

        //live scroll state, so a list that looks misplaced can be read rather than guessed at
        ImGui.LabelText("Selected Item", SelectedItem.ToString());
        ImGui.LabelText("Offset / Target", ring == null
            ? "no ring"
            : $"{ring.Offset:0.##} / {ring.Target:0.##}   (spacing {ring.Spacing:0.##}, settled {ring.IsSettled})");

        ImGui.SeparatorText("Curve");
        Inspector.Inspector.Inspect("Axis", ref curve.axis);
        Inspector.Inspector.Inspect("Shape", ref curve.shape);
        ImGui.InputFloat("Distance", ref curve.distance);
        ImGui.InputFloat("Range", ref curve.range);
        ImGui.InputFloat("Focus", ref curve.focus);

        //what the selected item and its neighbours actually get, to see where the peak really lands
        if (ring != null)
        {
            float spacing = SpacingAlongAxis;
            ImGui.LabelText("Curve at prev / sel / next",
                $"{curve.EvaluateAmount(-spacing + ring.Offset):0.#} / " +
                $"{curve.EvaluateAmount(ring.Offset):0.#} / " +
                $"{curve.EvaluateAmount(spacing + ring.Offset):0.#}");
        }
    }
}
