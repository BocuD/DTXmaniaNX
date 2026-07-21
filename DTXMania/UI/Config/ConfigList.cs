using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Drawable;
using DTXMania.UI.Item;

namespace DTXMania.UI.Config;

/// <summary>
/// Scrolling settings list built on the same idea as the song-select container: a small
/// ring of reusable <see cref="ConfigItemElement"/> rows over a finite <see cref="CItemBase"/> list.
/// Input is driven by the host (MoveUp/MoveDown/Confirm/Cancel) so there is a single input owner.
/// </summary>
internal class ConfigList : UIGroup
{
    private const float ElementSpacing = 67f;
    private const int ScrollUnitsPerRow = 100;

    private readonly ConfigItemElement[] elements;
    private int selectionIndex = 4;
    private int bufferStartIndex;

    private readonly UIGroup elementsContainer;
    private readonly UIImage cursor;
    private readonly UIImage arrowTop;
    private readonly UIImage arrowBottom;

    private List<CItemBase> currentItems = [];
    public readonly Stack<(List<CItemBase> items, int selection)> pageStack = new();

    private bool editing;

    //smooth accelerating scroll, ported from the old CActConfigList (units of 100 per row)
    private int currentScrollCounter;
    private int targetScrollCounter;
    private long scrollTimerValue = -1;

    //runs when Cancel is pressed at the root page (nothing left to go back to)
    public Action? onExitRoot;

    //runs when a key-assign pad row is confirmed; the host opens the KeyAssignPanel for (part, pad, name)
    public Action<EKeyConfigPart, EKeyConfigPad, string>? onOpenKeyAssign;

    public Action<(EKeyConfigPart part, EKeyConfigPad pad, string label)[]>? onOpenInputTest;

    public Action? onOpenMidiTest;

    public ConfigList(int slotCount, int selectionIndex) : base("ConfigList")
    {
        this.selectionIndex = selectionIndex;
        elements = new ConfigItemElement[slotCount];
        
        ConfigItemElement.LoadAssets();
        dontSerialize = true;
        
        cursor = AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\4_itembox cursor.png"))));
        cursor.name = "cursor";
        cursor.renderOrder = 1;
        
        cursor.position = new Vector3(-7, 4, 0);
        cursor.isVisible = false;

        BaseTexture arrowTexture = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\4_Arrow.png"));

        arrowTop = AddChild(new UIImage(arrowTexture));
        arrowTop.name = "arrowTop";
        arrowTop.renderOrder = 1;
        arrowTop.size = new Vector2(40, 40);
        arrowTop.position = new Vector3(-26, -15, 0);
        arrowTop.clipRect = new RectangleF(0, 0, 40, 40);
        arrowTop.isVisible = false;

        arrowBottom = AddChild(new UIImage(arrowTexture));
        arrowBottom.name = "arrowBottom";
        arrowBottom.renderOrder = 1;
        arrowBottom.size = new Vector2(40, 40);
        arrowBottom.position = new Vector3(-26, 51, 0);
        arrowBottom.clipRect = new RectangleF(0, 40, 40, 40);
        arrowBottom.isVisible = false;

        elementsContainer = AddChild(new UIGroup("Elements"));
        elementsContainer.sortByRenderOrder = false;
        elementsContainer.renderOrder = 0;

        //create child elements
        for (int i = 0; i < elements.Length; i++)
        {
            elements[i] = elementsContainer.AddChild(new ConfigItemElement());
        }
    }

    public CItemBase? CurrentItem => currentItems.Count == 0 ? null : elements[WrapIndex(bufferStartIndex + selectionIndex)].item;

    /// <summary>True when the list is fully aligned (not scrolling); used to gate the description panel.</summary>
    public bool IsSettled => currentScrollCounter == 0 && targetScrollCounter == 0;

    /// <summary>Show the cursor + scroll arrows only when this list (not the left menu) has focus.</summary>
    public void SetFocused(bool focused)
    {
        cursor.isVisible = focused;
        arrowTop.isVisible = focused;
        arrowBottom.isVisible = focused;
    }

    private ConfigItemElement SelectedElement => elements[WrapIndex(bufferStartIndex + selectionIndex)];

    /// <summary>Forces every row's text to re-render (e.g. after a resolution/renderScale change).</summary>
    public void RefreshAllText()
    {
        foreach (ConfigItemElement element in elements)
        {
            element.ForceReRenderText();
        }
    }

    private int WrapIndex(int index) => (index + elements.Length) % elements.Length;

    private static int Mod(int value, int modulus) => ((value % modulus) + modulus) % modulus;

    #region Page navigation

    /// <summary>Sets the current page's items, centering on <paramref name="selection"/>.</summary>
    public void SetItems(List<CItemBase> items, int selection = 0)
    {
        currentItems = items;
        bufferStartIndex = 0;
        currentScrollCounter = 0;
        targetScrollCounter = 0;
        scrollTimerValue = -1;
        elementsContainer.position.Y = 0;
        SetEditing(false);

        int count = items.Count;
        for (int i = 0; i < elements.Length; i++)
        {
            int itemIndex = count > 0 ? Mod(selection + (i - selectionIndex), count) : -1;
            elements[i].Bind(itemIndex >= 0 ? items[itemIndex] : null);
            elements[i].position.Y = (i - selectionIndex) * ElementSpacing;
            elements[i].position.X = 0;
        }
    }

    /// <summary>Enters a folder: remembers the current page + selection, then shows the new items.</summary>
    public void OpenFolder(List<CItemBase> items)
    {
        pageStack.Push((currentItems, currentItems.Count == 0 ? 0 : Mod(currentItems.IndexOf(CurrentItem!), currentItems.Count)));
        SetItems(items);
    }

    public CItemBase? SelectNextNormal()
    {
        if (currentItems.Count == 0) return null;

        int start = Mod(currentItems.IndexOf(CurrentItem!), currentItems.Count);
        for (int step = 1; step <= currentItems.Count; step++)
        {
            int idx = Mod(start + step, currentItems.Count);
            if (currentItems[idx].ePanelType == CItemBase.EPanelType.Normal)
            {
                SetItems(currentItems, idx);
                return currentItems[idx];
            }
        }
        return CurrentItem; // no other Normal item to move to
    }

    /// <summary>Returns to the parent page, or invokes <see cref="onExitRoot"/> at the root.</summary>
    public void Back()
    {
        if (pageStack.Count == 0)
        {
            onExitRoot?.Invoke();
            return;
        }

        (List<CItemBase> items, int selection) = pageStack.Pop();
        SetItems(items, selection);
    }

    #endregion

    #region Input (called by the host stage)

    // Up selects the previous item, down selects the next (the ring rotation happens in Draw when
    // the scroll counter crosses a full row, exactly like the old CActConfigList).
    public void MoveUp() => Move(up: true, invertEdit: false);
    public void MoveDown() => Move(up: false, invertEdit: false);

    // Drum-pad variants: when editing an integer the drums intentionally reverse the value
    // direction (HT decreases, LT increases) because pressing right for an increase feels
    // much more natural
    public void MoveUpDrums() => Move(up: true, invertEdit: true);
    public void MoveDownDrums() => Move(up: false, invertEdit: true);

    private void Move(bool up, bool invertEdit)
    {
        if (editing)
        {
            bool increase = up ^ invertEdit;
            if (increase) CurrentItem!.tMoveItemValueToNext();
            else CurrentItem!.tMoveItemValueToPrevious();

            CommitPage();
            CDTXMania.Skin.soundCursorMovement.tPlay();
            return;
        }

        QueueScroll(up ? -ScrollUnitsPerRow : +ScrollUnitsPerRow);
    }

    private void QueueScroll(int units)
    {
        if (currentItems.Count == 0) return;

        targetScrollCounter += units;
        CDTXMania.Skin.soundCursorMovement.tPlay();
    }

    public void Confirm()
    {
        if (currentItems.Count == 0) return;

        CDTXMania.Skin.soundDecide.tPlay();

        if (editing)
        {
            SetEditing(false);
            return;
        }

        CItemBase item = CurrentItem!;

        if (item.eType == CItemBase.EType.Integer)
        {
            SetEditing(true);
            return;
        }

        item.RunAction(); // cycles toggles/lists, or runs a folder/back action

        if (item.ePanelType is CItemBase.EPanelType.Return or CItemBase.EPanelType.Normal)
        {
            CommitPage();
        }
    }

    public void Cancel()
    {
        if (editing)
        {
            SetEditing(false);
            return;
        }

        Back();
    }

    public void CommitPage()
    {
        // Write every item on the page, not just the changed one: a "master" item's action can
        // modify sibling items (e.g. Drums "Dark" / "AutoPlay All"). Then refresh all visible rows
        // so their displayed values stay in sync. Writes are idempotent for untouched items.
        foreach (CItemBase item in currentItems)
        {
            item.WriteToConfig();
        }

        foreach (ConfigItemElement element in elements)
        {
            element.RefreshValue();
        }
    }

    private void SetEditing(bool value)
    {
        editing = value;
        SelectedElement.SetEditing(value);
    }

    #endregion

    #region Scrolling / rendering

    public override void Draw(Matrix4x4 parentMatrix)
    {
        AdvanceScroll();

        // convert the scroll counter to a pixel offset for the whole strip. A positive counter
        // (scrolling toward the next item) slides the content up.
        elementsContainer.position.Y = -(currentScrollCounter / (float)ScrollUnitsPerRow) * ElementSpacing;

        base.Draw(parentMatrix);
    }

    // fixed-timestep accelerating scroll, ported verbatim from CActConfigList.tUpdateAndDraw
    private void AdvanceScroll()
    {
        long currentTime = CDTXMania.Timer.nCurrentTime;
        if (scrollTimerValue < 0 || currentTime < scrollTimerValue) scrollTimerValue = currentTime;

        const int interval = 2; // ms
        while (currentTime - scrollTimerValue >= interval)
        {
            int distance = Math.Abs(targetScrollCounter - currentScrollCounter);
            int acceleration = distance <= 100 ? 2 : distance <= 300 ? 3 : distance <= 500 ? 4 : 8;

            if (currentScrollCounter < targetScrollCounter)
            {
                currentScrollCounter = Math.Min(currentScrollCounter + acceleration, targetScrollCounter);
            }
            else if (currentScrollCounter > targetScrollCounter)
            {
                currentScrollCounter = Math.Max(currentScrollCounter - acceleration, targetScrollCounter);
            }

            if (currentScrollCounter >= ScrollUnitsPerRow)
            {
                ScrollToNext();
                currentScrollCounter -= ScrollUnitsPerRow;
                targetScrollCounter -= ScrollUnitsPerRow;
            }
            else if (currentScrollCounter <= -ScrollUnitsPerRow)
            {
                ScrollToPrevious();
                currentScrollCounter += ScrollUnitsPerRow;
                targetScrollCounter += ScrollUnitsPerRow;
            }

            scrollTimerValue += interval;
        }
    }

    // recycle the bottom slot to the top and select the previous item
    private void ScrollToPrevious()
    {
        if (currentItems.Count == 0) return;

        bufferStartIndex = WrapIndex(bufferStartIndex - 1);

        ConfigItemElement head = elements[WrapIndex(bufferStartIndex)];
        CItemBase? below = elements[WrapIndex(bufferStartIndex + 1)].item;
        head.Bind(PreviousItem(below));

        UpdateSlotPositions();
    }

    // recycle the top slot to the bottom and select the next item
    private void ScrollToNext()
    {
        if (currentItems.Count == 0) return;

        ConfigItemElement head = elements[WrapIndex(bufferStartIndex)];
        CItemBase? above = elements[WrapIndex(bufferStartIndex + elements.Length - 1)].item;
        head.Bind(NextItem(above));

        bufferStartIndex = WrapIndex(bufferStartIndex + 1);

        UpdateSlotPositions();
    }

    private void UpdateSlotPositions()
    {
        for (int i = 0; i < elements.Length; i++)
        {
            elements[WrapIndex(bufferStartIndex + i)].position.Y = (i - selectionIndex) * ElementSpacing;
        }
    }

    private CItemBase? NextItem(CItemBase? item)
    {
        if (item == null || currentItems.Count == 0) return null;
        int index = currentItems.IndexOf(item);
        return currentItems[Mod(index + 1, currentItems.Count)];
    }

    private CItemBase? PreviousItem(CItemBase? item)
    {
        if (item == null || currentItems.Count == 0) return null;
        int index = currentItems.IndexOf(item);
        return currentItems[Mod(index - 1, currentItems.Count)];
    }

    #endregion

    public override void Dispose()
    {
        base.Dispose();
        ConfigItemElement.DisposeAssets();
    }
}
