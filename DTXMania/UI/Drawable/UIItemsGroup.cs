using System.Globalization;
using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Inspector;
using Hexa.NET.ImGui;
using Newtonsoft.Json;

namespace DTXMania.UI.Drawable;

/// <summary>
/// Where a list's selection is, for a cursor to bind to. Read live, so it can travel towards the selected
/// item every frame without formatting a string per frame.
/// </summary>
public sealed class UISelectionInfo
{
    [DataField] public int Index { get; internal set; }

    [DataField] public double X { get; private set; }

    [DataField] public double Y { get; private set; }

    private float targetX;
    private float targetY;
    private bool placed;

    internal void SetTarget(float x, float y)
    {
        targetX = x;
        targetY = y;
    }

    //zero puts the cursor on the item outright
    internal void Advance(float elapsedSeconds, float speed)
    {
        if (!placed || speed <= 0.0f || elapsedSeconds <= 0.0f)
        {
            placed = true;
            X = targetX;
            Y = targetY;
            return;
        }

        float step = 1.0f - MathF.Exp(-speed * elapsedSeconds);
        X += (targetX - X) * step;
        Y += (targetY - Y) * step;
    }
}

/// <summary>Supplies the items a <see cref="UIItemsGroup"/> shows. Items are read once per rebind, not
/// per frame, so building one per call is fine as long as the owner only rebinds when its data changes.</summary>
public interface IUIItemSource
{
    int ItemCount { get; }
    object? GetItem(int index);
}

/// <summary>
/// One item's copy of the list's component. Its children bind to <c>"Item.*"</c>, so one component
/// serves every row.
/// </summary>
public sealed class UIItemSlot : ComponentInstance
{
    private readonly UIDataContext data = new();

    public int index { get; private set; } = -1;

    private bool selected;

    public UIItemSlot() : base("Item")
    {
        dontSerialize = true;
    }

    private Func<UIGroup>? buildDefault;

    public void Bind(string componentPath, int itemIndex, Func<object?> item, Func<UIGroup>? itemDefault)
    {
        component = componentPath;
        buildDefault = itemDefault;

        data.RegisterObject("Item", item);
        data.SetString("IsSelected", "false");
        dataContext = data;

        SetItemIndex(itemIndex);
    }

    /// <summary>Points this slot at a different item; its children pick the new values up as they draw.</summary>
    public void SetItemIndex(int itemIndex)
    {
        if (index == itemIndex)
        {
            return;
        }

        index = itemIndex;
        data.SetString("Index", itemIndex.ToString());
    }

    /// <summary>Marks this slot as the selected one, which its component reads as <c>"IsSelected"</c>.</summary>
    public void SetSelected(bool value)
    {
        if (selected == value)
        {
            return;
        }

        selected = value;
        data.SetString("IsSelected", value ? "true" : "false");
    }

    //the list owns the item's code default, since the list is what knows what it is showing
    protected override UIGroup BuildDefault() => buildDefault?.Invoke() ?? new UIGroup("Item");
}

/// <summary>
/// Shows a component once per item, each copy carrying its own <c>"Item"</c> data context: the list owns
/// the repetition, the component owns the look, and the item's fields drive the content.
///
/// The item count comes from the <see cref="IUIItemSource"/> a behaviour class attaches, or from
/// <see cref="itemCount"/> for a list placed by hand in a layout.
/// </summary>
public class UIItemsGroup : UIGroup, IUIInputHandler
{
    //skin-relative path of the component stamped per item, e.g. "Components/ChartRow.json"
    [Themable] public string itemComponent = string.Empty;

    //the step from one item to the next; a vector so a list can run diagonally
    [Themable] public Vector3 itemOffset = new(0, 32, 0);

    /// <summary>How far apart items sit, measured along the list's own direction.</summary>
    public float ItemDistance => Math.Max(itemOffset.Length(), 0.001f);

    /// <summary>Unit vector along the list, which scrolling and the curve both measure against.</summary>
    public Vector3 ItemDirection => itemOffset.LengthSquared() > 0.000001f
        ? Vector3.Normalize(itemOffset)
        : Vector3.UnitY;

    //used only when no source is attached, i.e. a list placed by hand
    [Themable] public int itemCount;

    //code default for the item component, seeded into the skin the first time it is needed
    [JsonIgnore] public Func<UIGroup>? itemDefault;

    //where the list starts; a scrolling list takes the selection from its ring instead
    [Themable] public int selectedItem;

    //whether moving past either end comes back round; a short menu of distinct choices usually should not
    [Themable] public bool wrapSelection = true;

    [JsonIgnore] private IUIItemSource? source;
    [JsonIgnore] private readonly List<UIItemSlot> slots = [];
    [JsonIgnore] private int builtCount = -1;
    [JsonIgnore] private string? builtComponent;

    //how quickly a bound cursor travels to the selection, as a fraction of the distance per second
    [Themable] public float selectionSpeed;

    //the list's own context, so a cursor placed inside it can bind to where the selection is
    [JsonIgnore] private readonly UIDataContext data = new();
    [JsonIgnore] private readonly UISelectionInfo selection = new();
    [JsonIgnore] private long lastSelectionTime;

    [JsonIgnore] private readonly NavigationRepeat navigation = new();
    [JsonIgnore] private readonly Action selectPrevious;
    [JsonIgnore] private readonly Action selectNext;

    [AddChildMenu("Items Group")]
    public static new UIDrawable Create() => new UIItemsGroup();

    public UIItemsGroup() : this("Items")
    {
    }

    public UIItemsGroup(string name) : base(name)
    {
        //slots share a render order, so sorting would shuffle which item draws over which
        sortByRenderOrder = false;

        data.RegisterObject("Selection", () => selection);
        dataContext = data;

        selectPrevious = SelectPrevious;
        selectNext = SelectNext;
    }

    protected IReadOnlyList<UIItemSlot> Slots => slots;

    protected int SourceCount => source?.ItemCount ?? itemCount;

    /// <summary>Which item the user is on. A scrolling list answers from its scroll position.</summary>
    public virtual int SelectedItem
    {
        get => selectedItem;
        set => selectedItem = SourceCount == 0 ? 0 : Math.Clamp(value, 0, SourceCount - 1);
    }

    /// <summary>Where the selected item sits, which is also what <c>"Selection.X"</c> / <c>"Selection.Y"</c>
    /// give a bound cursor.</summary>
    public Vector3 SelectedPosition => new((float)selection.X, (float)selection.Y, 0.0f);

    public void SelectNext() => MoveSelection(1);

    public void SelectPrevious() => MoveSelection(-1);

    /// <summary>Moves the selection by whole items; a plain list wraps around at the ends.</summary>
    protected virtual void MoveSelection(int items)
    {
        int count = SourceCount;
        if (count == 0)
        {
            return;
        }

        int moved = SelectedItem + items;
        if (!wrapSelection && (moved < 0 || moved >= count))
        {
            return;
        }

        SelectedItem = (moved % count + count) % count;
        CDTXMania.Skin.soundCursorMovement.tPlay();
        OnSelectionMoved();
    }

    /// <summary>Called when the user moved onto another item, as opposed to the selection being set.</summary>
    protected virtual void OnSelectionMoved()
    {
    }

    public virtual string FocusName => name;

    public NavigationRepeat? Navigation => navigation;

    public virtual void HandleInput()
    {
        navigation.Poll(selectPrevious, selectNext);

        if (CDTXMania.Input.ActionDecide())
        {
            Decide();
        }
        else if (CDTXMania.Input.ActionCancel())
        {
            Cancel();
        }
    }

    /// <summary>Runs what the selected item does. A list that is not a menu leaves this alone.</summary>
    protected virtual void Decide()
    {
    }

    protected virtual void Cancel()
    {
    }

    /// <summary>Attaches the data behind the list. Safe to call repeatedly; the slots rebuild only when
    /// the item count or the component actually changes.</summary>
    public void SetSource(IUIItemSource? itemSource)
    {
        source = itemSource;
        EnsureSlots();
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        EnsureSlots();
        PublishSelection();
        base.Draw(parentMatrix);
    }

    /// <summary>
    /// Marks the selected slot and puts where the selection sits into this group's context, so a cursor
    /// can bind to <c>"Selection.Y"</c>.
    /// </summary>
    protected void PublishSelection()
    {
        int selected = SelectedItem;
        selection.Index = selected;

        foreach (UIItemSlot slot in slots)
        {
            slot.SetSelected(slot.index == selected);

            if (slot.index == selected)
            {
                selection.SetTarget(slot.position.X, slot.position.Y);
            }
        }

        long now = CDTXMania.Timer.nCurrentTime;
        float elapsed = lastSelectionTime == 0 ? 0.0f : Math.Min((now - lastSelectionTime) / 1000.0f, 0.25f);
        lastSelectionTime = now;

        selection.Advance(elapsed, selectionSpeed);
    }

    //one slot per item by default; a scrolling list overrides this to a fixed window
    protected virtual int SlotCountFor(int itemCount) => itemCount;

    //only the shape of the list matters here; the values inside a slot are pulled by its own bindings
    protected void EnsureSlots()
    {
        int count = SlotCountFor(SourceCount);
        if (count == builtCount && itemComponent == builtComponent)
        {
            return;
        }

        builtCount = count;
        builtComponent = itemComponent;

        foreach (UIItemSlot existing in slots)
        {
            RemoveChild(existing);
            existing.Dispose();
        }

        slots.Clear();

        for (int i = 0; i < count; i++)
        {
            UIItemSlot slot = AddChild(new UIItemSlot());

            //named by place, so an animation can address one ("Item3") whatever the list is showing
            slot.name = "Item" + i;

            //the provider reads the slot's current index, so recycling never rebuilds the closure
            slot.Bind(itemComponent, i, () => source?.GetItem(slot.index), itemDefault);
            slots.Add(slot);
        }

        LayOutSlots();
    }

    protected virtual void LayOutSlots()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].position = itemOffset * i;
        }
    }

    /// <summary>
    /// Resolves and rasterizes every bound text in the list now, so a stage can warm it up before it
    /// becomes visible. Walks the tree, so it keeps working whatever a skin puts in the item component.
    /// </summary>
    public void PreRenderText()
    {
        EnsureSlots();

        foreach (UIItemSlot slot in slots)
        {
            slot.EnsureContent();
            PreRender(slot);
        }
    }

    /// <summary>Re-renders every text in the list on its next draw, for when the render scale changed.</summary>
    public void InvalidateText()
    {
        foreach (UIItemSlot slot in slots)
        {
            Invalidate(slot);
        }
    }

    private static void Invalidate(UIDrawable node)
    {
        if (node is UIText text)
        {
            text.MarkDirty();
        }

        if (node is UIGroup group)
        {
            foreach (UIDrawable child in group.children)
            {
                Invalidate(child);
            }
        }
    }

    private static void PreRender(UIDrawable node)
    {
        //bindings first: they supply the text to rasterize, and decide whether it is worth rasterizing
        node.ApplyBindings();

        if (node is UIText { isVisible: true } text)
        {
            text.RenderTexture();
        }

        if (node is UIGroup group)
        {
            foreach (UIDrawable child in group.children)
            {
                PreRender(child);
            }
        }
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (!ImGui.CollapsingHeader("Items"))
        {
            return;
        }

        if (ImGui.InputText("Item Component", ref itemComponent, 512))
        {
            builtComponent = null;
        }

        Inspector.Inspector.Inspect("Item Offset", ref itemOffset);
        LayOutSlots();

        ImGui.BeginDisabled(source != null);
        if (ImGui.InputInt("Item Count", ref itemCount))
        {
            itemCount = Math.Max(0, itemCount);
        }

        ImGui.EndDisabled();

        ImGui.LabelText("Items", source != null ? $"{SourceCount} (from code)" : SourceCount.ToString());
    }
}
