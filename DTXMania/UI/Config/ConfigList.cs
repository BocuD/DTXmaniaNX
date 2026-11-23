using DTXMania.UI.Skin;
using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Animation;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Item;
using DTXMania.UI.Inspector;

namespace DTXMania.UI.Config;

/// <summary>
/// The settings list: a page of <see cref="CItemBase"/> shown through the same scrolling machinery as the
/// song list, one <see cref="ConfigRowData"/> per item. What is left here is what is actually about
/// settings — the page stack, changing a value, and the panels a row can open.
/// </summary>
internal class ConfigList : UIScrollItemsGroup, IUIItemSource
{

    [AddChildMenu("Game/Config/Config List")]
    public new static UIDrawable Create() => new ConfigList();
    private const float RowSpacing = 67f;

    //where a text input sits within a row, matching where the component draws its value
    private static readonly Vector3 ValueOffset = new(265, 30, 0);

    private readonly List<ConfigRowData> rows = [];

    //found once the component has loaded, rather than built here: a class that adds children of its own
    //cannot be loaded from a layout without ending up with both sets
    private UIImage? cursor;
    private UIGroup? arrowUp;
    private UIGroup? arrowDown;
    private UIGroup? arrowLeft;
    private UIGroup? arrowRight;

    //its own counters, so holding left or right does not take the repeats meant for up and down
    private readonly NavigationRepeat valueNavigation = new();

    //made once: a lambda per polled frame would allocate
    private readonly Action increaseValue;
    private readonly Action decreaseValue;

    private List<CItemBase> currentItems = [];
    public readonly Stack<(List<CItemBase> items, int selection, ConfigPage? page)> pageStack = new();

    /// <summary>The page whose rows are on screen.</summary>
    public ConfigPage? CurrentPage { get; private set; }

    /// <summary>Where a page's own elements are parented, in screen space rather than beside the rows.</summary>
    public UIGroup? pageElements;

    /// <summary>Opens a page as the root of the list, dropping whatever folder history there was.</summary>
    public void OpenRoot(ConfigPage page)
    {
        pageStack.Clear();
        SetPage(page);
        SetItems(page.Build());
    }

    public void ClosePage() => SetPage(null);

    //a page owns elements outside the list, so every move between pages goes through here
    private void SetPage(ConfigPage? page)
    {
        if (ReferenceEquals(page, CurrentPage))
        {
            return;
        }

        CurrentPage?.CloseElements();
        CurrentPage = page;
        CurrentPage?.OpenElements();
    }


    //runs when Cancel is pressed at the root page (nothing left to go back to)
    public Action? onExitRoot;

    //runs when a key-assign pad row is confirmed; the host opens the KeyAssignPanel for (part, pad, name)
    public Action<EKeyConfigPart, EKeyConfigPad, string>? onOpenKeyAssign;

    public Action<(EKeyConfigPart part, EKeyConfigPad pad, string label)[]>? onOpenInputTest;

    public Action<(EKeyConfigPart part, EKeyConfigPad pad, string label)[]>? onOpenMidiTest;

    public ConfigList() : base("ConfigList")
    {
        increaseValue = () => ChangeValue(true);
        decreaseValue = () => ChangeValue(false);

        itemOffset = new Vector3(0, RowSpacing, 0);
        itemComponentSource = ConfigRow;

        //the original settings-list feel: a constant speed that rises with the backlog
        motion = new UIScrollMotion(rate: 4.0f, minSpeed: 10.0f, maxSpeed: 40.0f);

        //the cursor and arrows load before any slot exists, so insertion order would put them under the
        //rows. The rows never overlap each other, so ordering them by hand costs nothing here
        sortByRenderOrder = true;

        MakeComponent("ConfigListCursor", CursorElement);
    }

    public ConfigList(int slotCount, int selectionIndex) : this()
    {
        visibleSlots = slotCount;
        selectionOffset = selectionIndex;
    }

    protected override void OnContentLoaded()
    {
        cursor = GetChild<UIImage>("Cursor");
        arrowUp = GetChild<UIGroup>("ArrowUp");
        arrowDown = GetChild<UIGroup>("ArrowDown");
        arrowLeft = GetChild<UIGroup>("ArrowLeft");
        arrowRight = GetChild<UIGroup>("ArrowRight");
    }

    //every settings list shares this, so the quick menu and the config screen are styled together
    private static UIGroup CursorElement()
    {
        UIGroup root = new("ConfigListCursor");

        root.AddChild(new UIImage
        {
            name = "Cursor",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\4_itembox cursor.png"),
            size = new Vector2(444, 67),
            position = new Vector3(-7, 4, 0),
            renderOrder = 1
        });

        root.AddChild(AnimatedArrow("ArrowUp", -6, 5, 0));
        root.AddChild(AnimatedArrow("ArrowDown", -6, 71, MathF.PI));
        
        root.AddChild(AnimatedArrow("ArrowLeft", 238, 39, -MathF.PI / 2));
        root.AddChild(AnimatedArrow("ArrowRight", 426, 39, MathF.PI / 2));

        return root;
    }

    private static UIGroup AnimatedArrow(string name, float x, float y, float turn)
    {
        var group = new UIGroup(name)
        {
            position = new Vector3(x, y, 0),
            renderOrder = 1,
        };
        
        var image = new UIImage
        {
            name = "Arrow",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\4_Arrow.png"),
            size = new Vector2(40, 40),
            pivot = new Vector2(0.5f, 0.5f),
            rotation = new Vector3(0, 0, turn),
            clipRect = new RectangleF(0, 0, 40, 40),
        };
        
        var image2 = new UIImage
        {
            name = "ArrowEffect",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\4_Arrow.png"),
            size = new Vector2(40, 40),
            pivot = new Vector2(0.5f, 0.5f),
            rotation = new Vector3(0, 0, turn),
            clipRect = new RectangleF(0, 0, 40, 40),
            color = new Color4(1, 1, 1, 0),
            renderOrder = 1
        };
        
        group.AddChild(image);
        group.AddChild(image2);

        group.animator = new Animator();
        group.animator.AddResource(SkinResource.System(@"Graphics\Config\arrow.json"));
        
        return group;
    }

    public int ItemCount => Math.Max(1, rows.Count);

    //a page is cyclic, and the ring counts on unbounded indices, so they wrap onto the page when read
    public object? GetItem(int index) => rows.Count == 0 ? null : rows[Mod(index, rows.Count)];

    public CItemBase? CurrentItem => SelectedRow?.Item;

    /// <summary>True once the selection has stopped changing; used to gate the description panel.</summary>
    public bool IsSettled => !IsScrolling;

    /// <summary>Whether this list is the one being driven.</summary>
    public bool IsActive => UIFocus.Holds(this);

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

        ScrollTo(selection);
    }

    /// <summary>Enters a folder: remembers the current page + selection, then shows the new items.</summary>
    public void OpenFolder(List<CItemBase> items)
    {
        pageStack.Push((currentItems, SelectedIndexOnPage, CurrentPage));
        SetPage(null);
        SetItems(items);
    }

    public void OpenFolder(ConfigPage page)
    {
        pageStack.Push((currentItems, SelectedIndexOnPage, CurrentPage));
        SetPage(page);
        SetItems(page.Build());
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

        (List<CItemBase> items, int selection, ConfigPage? page) = pageStack.Pop();
        SetPage(page);
        SetItems(page is { RebuildsOnReturn: true } ? page.Build() : items, selection);
    }

    private int SelectedIndexOnPage => rows.Count == 0 ? 0 : Mod(SelectedItem, rows.Count);

    #endregion

    #region Input

    protected override void Decide()
    {
        if (CurrentItem is not { } item) return;

        CDTXMania.Skin.soundDecide.tPlay();

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

    protected override void OnSelectionMoved(int items)
    {
        if (Navigation.Repeating)
        {
            return;
        }

        UIGroup? arrow = items < 0 ? arrowUp : arrowDown;
        arrow?.animator?.Play("arrow");
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

        RefreshValues();
    }

    private void ChangeValue(bool increase)
    {
        CItemBase? item = CurrentItem;

        if (item == null)
        {
            return;
        }

        if (increase)
        {
            item.tMoveItemValueToNext();
        }
        else
        {
            item.tMoveItemValueToPrevious();
        }

        if (!valueNavigation.Repeating)
        {
            UIGroup? arrow = increase ? arrowRight : arrowLeft;
            arrow?.animator?.Play("arrow");
        }

        item.action?.Invoke();

        CommitPage();
        CDTXMania.Skin.soundChange.tPlay();
    }

    /// <summary>Whether left and right change this row, as opposed to it running an action.</summary>
    private static bool StepsByArrows(CItemBase item) => item.eType == CItemBase.EType.Integer
                                                         || item.eType == CItemBase.EType.Float
                                                         || item.eType == CItemBase.EType.List
                                                         || item.eType == CItemBase.EType.切替リスト
                                                         || item.eType == CItemBase.EType.ONorOFFToggle
                                                         || item.eType == CItemBase.EType.ONorOFForUndefined3State;

    public override void HandleInput()
    {
        if (CurrentItem != null && StepsByArrows(CurrentItem))
        {
            //whichever of the neck and the strum moves the selection, the value takes the other one
            UIGuitarNavigation guitar = CDTXMania.ConfigIni.bStrumScrollsMenus
                ? UIGuitarNavigation.Neck
                : UIGuitarNavigation.Strum;

            valueNavigation.Poll(UINavigationAxis.Horizontal, guitar, decreaseValue, increaseValue);
        }

        base.HandleInput();
    }

    #endregion

    #region Rendering

    public override void Draw(Matrix4x4 parentMatrix)
    {
        //the cursor and arrows say "this is what you are driving", which is what holding focus means
        bool active = IsActive;

        if (cursor != null)
        {
            cursor.isVisible = active;
        }

        if (arrowUp != null)
        {
            arrowUp.isVisible = active;
        }

        if (arrowDown != null)
        {
            arrowDown.isVisible = active;
        }

        bool steps = active && CurrentItem != null && StepsByArrows(CurrentItem);

        if (arrowLeft != null)
        {
            arrowLeft.isVisible = steps;
        }

        if (arrowRight != null)
        {
            arrowRight.isVisible = steps;
        }

        CItemTextInput? textInput = CurrentItem as CItemTextInput;
        bool typing = textInput != null && textInput.drawableTextInput.IsActive;
        ConfigRowData? selected = SelectedRow;

        //the field has just saved its value
        if (textInput != null && selected != null && selected.IsEditing && !typing)
        {
            selected.RefreshValue();
        }

        foreach (ConfigRowData row in rows)
        {
            row.IsEditing = ReferenceEquals(row, selected) && (steps || typing);
        }

        base.Draw(parentMatrix);

        if (textInput != null && typing)
        {
            textInput.drawableTextInput.position = SelectedPosition + ValueOffset;
            textInput.drawableTextInput.Draw(localTransformMatrix * parentMatrix);
        }
    }

    //the panel behind a row, its name, and its value in one style or the other
    private UIGroup ConfigRow()
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

        //the outline grows the glyphs downwards, so the text sits where the plain one does
        edited.position.Y -= 4;

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
