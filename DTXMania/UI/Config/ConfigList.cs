using DTXMania.UI.Skin;
using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Item;

namespace DTXMania.UI.Config;

/// <summary>
/// The settings list: a page of <see cref="CItemBase"/> shown through the same scrolling machinery as the
/// song list, one <see cref="ConfigRowData"/> per item. What is left here is what is actually about
/// settings — the page stack, editing a value, and the panels a row can open.
/// </summary>
internal class ConfigList : UIScrollItemsGroup, IUIItemSource
{
    private const float RowSpacing = 67f;

    //where a text input sits within a row, matching where the component draws its value
    private static readonly Vector3 ValueOffset = new(265, 30, 0);

    private readonly List<ConfigRowData> rows = [];
    private readonly ConfigItemEditor editor;

    private readonly UIImage cursor;
    private readonly UIImage arrowTop;
    private readonly UIImage arrowBottom;

    private List<CItemBase> currentItems = [];
    public readonly Stack<(List<CItemBase> items, int selection)> pageStack = new();

    private bool editing;

    //runs when Cancel is pressed at the root page (nothing left to go back to)
    public Action? onExitRoot;

    //runs when a key-assign pad row is confirmed; the host opens the KeyAssignPanel for (part, pad, name)
    public Action<EKeyConfigPart, EKeyConfigPad, string>? onOpenKeyAssign;

    public Action<(EKeyConfigPart part, EKeyConfigPad pad, string label)[]>? onOpenInputTest;

    public Action<(EKeyConfigPart part, EKeyConfigPad pad, string label)[]>? onOpenMidiTest;

    public ConfigList(int slotCount, int selectionIndex) : base("ConfigList")
    {
        dontSerialize = true;
        editor = new ConfigItemEditor(this);

        visibleSlots = slotCount;
        selectionOffset = selectionIndex;
        itemOffset = new Vector3(0, RowSpacing, 0);
        itemComponent = "Components/ConfigRow.json";
        itemDefault = BuildRowDefault;

        //the original settings-list feel: a constant speed that rises with the backlog
        motion = new UIScrollMotion(rate: 4.0f, minSpeed: 10.0f, maxSpeed: 40.0f);

        SetSource(this);

        cursor = AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\4_itembox cursor.png"))));
        cursor.name = "cursor";
        cursor.renderOrder = 1;
        cursor.position = new Vector3(-7, 4, 0);

        BaseTexture arrowTexture = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\4_Arrow.png"));

        arrowTop = AddChild(new UIImage(arrowTexture));
        arrowTop.name = "arrowTop";
        arrowTop.renderOrder = 1;
        arrowTop.size = new Vector2(40, 40);
        arrowTop.position = new Vector3(-26, -15, 0);
        arrowTop.clipRect = new RectangleF(0, 0, 40, 40);

        arrowBottom = AddChild(new UIImage(arrowTexture));
        arrowBottom.name = "arrowBottom";
        arrowBottom.renderOrder = 1;
        arrowBottom.size = new Vector2(40, 40);
        arrowBottom.position = new Vector3(-26, 51, 0);
        arrowBottom.clipRect = new RectangleF(0, 40, 40, 40);
    }

    public int ItemCount => Math.Max(1, rows.Count);

    //a page is cyclic, and the ring counts on unbounded indices, so they wrap onto the page when read
    public object? GetItem(int index) => rows.Count == 0 ? null : rows[Mod(index, rows.Count)];

    public CItemBase? CurrentItem => SelectedRow?.Item;

    /// <summary>True once the selection has stopped changing; used to gate the description panel.</summary>
    public bool IsSettled => !IsScrolling;

    /// <summary>Whether this list is the one being driven, editing a value included.</summary>
    public bool IsActive => UIFocus.Holds(this) || editing;

    private ConfigRowData? SelectedRow => GetItem(SelectedItem) as ConfigRowData;

    private static int Mod(int value, int modulus) => ((value % modulus) + modulus) % modulus;

    /// <summary>Re-reads every row's value, which an item's action may have changed.</summary>
    public void RefreshValues()
    {
        foreach (ConfigRowData row in rows)
        {
            row.RefreshValue();
        }
    }

    #region Page navigation

    /// <summary>Sets the current page's items, centering on <paramref name="selection"/>.</summary>
    public void SetItems(List<CItemBase> items, int selection = 0)
    {
        currentItems = items;

        while (rows.Count < items.Count)
        {
            rows.Add(new ConfigRowData());
        }

        rows.RemoveRange(items.Count, rows.Count - items.Count);

        for (int i = 0; i < items.Count; i++)
        {
            rows[i].SetItem(items[i]);
        }

        StopEditing();
        ScrollTo(selection);
    }

    /// <summary>Enters a folder: remembers the current page + selection, then shows the new items.</summary>
    public void OpenFolder(List<CItemBase> items)
    {
        pageStack.Push((currentItems, SelectedIndexOnPage));
        SetItems(items);
    }

    public CItemBase? SelectNextNormal()
    {
        if (currentItems.Count == 0) return null;

        int start = SelectedIndexOnPage;
        for (int step = 1; step <= currentItems.Count; step++)
        {
            int index = Mod(start + step, currentItems.Count);
            if (currentItems[index].ePanelType == CItemBase.EPanelType.Normal)
            {
                SetItems(currentItems, index);
                return currentItems[index];
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

    private int SelectedIndexOnPage => rows.Count == 0 ? 0 : Mod(SelectedItem, rows.Count);

    #endregion

    #region Input

    protected override void Decide()
    {
        if (CurrentItem is not { } item) return;

        CDTXMania.Skin.soundDecide.tPlay();

        if (item.eType == CItemBase.EType.Integer)
        {
            StartEditing();
            return;
        }

        item.RunAction(); // cycles toggles/lists, or runs a folder/back action

        if (item.ePanelType is CItemBase.EPanelType.Return or CItemBase.EPanelType.Normal)
        {
            CommitPage();
        }
    }

    protected override void Cancel()
    {
        CDTXMania.Skin.soundCancel.tPlay();
        Back(); //pops a folder, or at the root hands focus back to whoever opened this list
    }

    //scrolling by whole rows is what moves the selection here
    protected override void OnScrolled(int steps) => CDTXMania.Skin.soundCursorMovement.tPlay();

    public void CommitPage()
    {
        // Write every item on the page, not just the changed one: a "master" item's action can
        // modify sibling items (e.g. Drums "Dark" / "AutoPlay All"). Then refresh all visible rows
        // so their displayed values stay in sync. Writes are idempotent for untouched items.
        foreach (CItemBase item in currentItems)
        {
            item.WriteToConfig();
        }

        RefreshValues();
    }

    //editing takes focus: up and down mean something else, and escape must stop editing before going back
    private void StartEditing()
    {
        SetEditing(true);
        UIFocus.Push(editor);
    }

    private void StopEditing()
    {
        UIFocus.Pop(editor);
        SetEditing(false);
    }

    private void SetEditing(bool value)
    {
        editing = value;

        if (SelectedRow is { } row)
        {
            row.IsEditing = value;
        }
    }

    private void ChangeValue(bool increase)
    {
        if (CurrentItem == null)
        {
            return;
        }

        if (increase)
        {
            CurrentItem.tMoveItemValueToNext();
        }
        else
        {
            CurrentItem.tMoveItemValueToPrevious();
        }

        CommitPage();
        CDTXMania.Skin.soundCursorMovement.tPlay();
    }

    /// <summary>
    /// Holds input while a row's value is being changed. The drums deliberately run the other way round —
    /// HT decreases and LT increases — because hitting the right-hand tom to raise a value is what reads
    /// as natural on a kit.
    /// </summary>
    private sealed class ConfigItemEditor(ConfigList list) : IUIInputHandler
    {
        private readonly NavigationRepeat navigation = new();
        private readonly Action increase = () => list.ChangeValue(true);
        private readonly Action decrease = () => list.ChangeValue(false);

        public string FocusName => $"editing {list.CurrentItem?.strItemName}";

        public NavigationRepeat? Navigation => navigation;

        public void HandleInput()
        {
            if (CDTXMania.Input.ActionCancel() || CDTXMania.Input.ActionDecide())
            {
                CDTXMania.Skin.soundDecide.tPlay();
                list.StopEditing();
                return;
            }

            navigation.Poll(increase, decrease, decrease, increase);
        }
    }

    #endregion

    #region Rendering

    public override void Draw(Matrix4x4 parentMatrix)
    {
        //the cursor and arrows say "this is what you are driving", which is what holding focus means
        bool active = IsActive;
        cursor.isVisible = active;
        arrowTop.isVisible = active;
        arrowBottom.isVisible = active;

        base.Draw(parentMatrix);

        DrawTextInput(parentMatrix);
    }

    //a text input belongs to its item, and only the selected row can be typing in one
    private void DrawTextInput(Matrix4x4 parentMatrix)
    {
        if (CurrentItem is not CItemTextInput textInput)
        {
            return;
        }

        textInput.drawableTextInput.position = SelectedPosition + ValueOffset;
        textInput.drawableTextInput.Draw(localTransformMatrix * parentMatrix);
    }

    //the panel behind a row, its name, and its value in one style or the other
    private UIGroup BuildRowDefault()
    {
        UIGroup root = new("ConfigRow");

        root.AddChild(new TextureArray
        {
            name = "Panel",
            resources =
            {
                SkinResource.System(@"Graphics\4_itembox.png"),
                SkinResource.System(@"Graphics\4_itembox folder.png"),
                SkinResource.System(@"Graphics\4_itembox other.png")
            },
            renderOrder = 0,
            bindings =
            {
                new UIBinding("textureIndex", "Item.PanelIndex"),
                new UIBinding("isVisible", "Item.HasPanel")
            }
        });

        UIText name = root.AddChild(new UIText(string.Empty, 16));
        name.name = "Name";
        name.position = new Vector3(30, 30, 0);
        name.fillColor = Color4.White;
        name.outlineWidth = 0;
        name.renderOrder = 1;
        name.bindings.Add(new UIBinding("text", "Item.Name"));

        AddValueText(root, "Value", Color4.FromColor(Color.Black), "Item.ShowValue");

        UIText edited = AddValueText(root, "ValueEdited", Color4.White, "Item.ShowEditedValue");
        edited.outlineColor = Color4.FromColor(Color.OrangeRed);
        edited.outlineWidth = 4;

        return root;
    }

    private static UIText AddValueText(UIGroup root, string name, Color4 fill, string visibleWhen)
    {
        UIText value = root.AddChild(new UIText(string.Empty, 16));
        value.name = name;
        value.position = ValueOffset;
        value.fillColor = fill;
        value.outlineWidth = 0;
        value.renderOrder = 1;
        value.bindings.Add(new UIBinding("text", "Item.Value"));
        value.bindings.Add(new UIBinding("isVisible", visibleWhen));

        return value;
    }

    #endregion
}
