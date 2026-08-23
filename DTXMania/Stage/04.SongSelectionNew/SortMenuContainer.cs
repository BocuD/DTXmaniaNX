using Hexa.NET.ImGui;
using DTXMania.UI.Skin;
using System.Numerics;
using DTXMania.Core;
using DTXMania.SongDb.Sorting;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using FDK;

namespace DTXMania;

/// <summary>
/// The carousel of sort modes. A <see cref="UIScrollItemsGroup"/> over one <see cref="SortRowData"/> per
/// sort: the wrap-around, the easing and the dip towards the selected entry all come from there, so this
/// only says which sort is showing and reacts when that changes.
/// </summary>
public class SortMenuContainer : ComponentInstance, IUIItemSource
{
    private const float EntrySpacing = 90.0f;

    private readonly SortRowData[] rows = BuildRows();

    private UIScrollItemsGroup? entries;

    private readonly NavigationRepeat navigation = NavigationRepeat.Horizontal();

    //cached, so the repeat does not allocate a closure on every polled frame
    private readonly Action scrollPrevious;
    private readonly Action scrollNext;

    //one per sort mode, in the order SongDbSort declares them
    private SoundReference[]? sounds;

    //the sort the stage is actually showing, or -1 until it has said. The menu applies whatever scrolls
    //under the selection, so the first frame must not mistake the starting position for a scroll
    private int appliedIndex = -1;

    public SortMenuContainer() : base("SortMenuContainer")
    {
        scrollPrevious = () => entries?.ScrollBy(-1);
        scrollNext = () => entries?.ScrollBy(1);

        size = new Vector2(662, 92);
        anchor = new Vector2(1.0f, 0.0f);
    }

    public int ItemCount => rows.Length;
    public object GetItem(int index) => rows[Mod(index, rows.Length)];

    protected override void OnContentLoaded()
    {
        LoadSounds();

        entries = GetChild<UIScrollItemsGroup>("Entries");

        if (entries != null)
        {
            entries.itemDefault = BuildEntryDefault;
            entries.SetSource(this);
        }
    }

    /// <summary>Shows a sort without applying it, for restoring what was selected last time.</summary>
    public void ShowSort(SongDbSort sort)
    {
        EnsureContent();

        appliedIndex = Math.Max(Array.IndexOf(SongDbSort.All, sort), 0);
        entries?.ScrollTo(appliedIndex);
    }

    public void HandleNavigation()
    {
        navigation.Poll(scrollPrevious, scrollNext);
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        EnsureContent();

        //the list decides what is selected, so the sort follows it rather than the other way round
        if (entries != null && Mod(entries.SelectedItem, rows.Length) is var showing && showing != appliedIndex)
        {
            bool scrolled = appliedIndex >= 0;
            appliedIndex = showing;

            if (scrolled)
            {
                PlaySound(showing);
                CDTXMania.StageManager.stageSongSelectionNew.ApplySort(SongDbSort.All[showing]);
            }
        }

        base.Draw(parentMatrix);
    }

    public override void Dispose()
    {
        base.Dispose();

        if (sounds == null)
        {
            return;
        }

        foreach (SoundReference sound in sounds)
        {
            sound.Unload();
        }

        sounds = null;
    }

    private void PlaySound(int index)
    {
        CDTXMania.Skin.soundCursorMovement.tPlay();
        sounds?[index].Play(80);
    }

    //only reached for a real instance; the default the serializer compares against never gets here
    private void LoadSounds()
    {
        sounds = new SoundReference[SongDbSort.All.Length];

        for (int i = 0; i < sounds.Length; i++)
        {
            sounds[i] = new SoundReference(
                SkinResource.System($@"Graphics\Sorting\{SongDbSort.All[i].IconName}.wav"));
            sounds[i].Load();
        }
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (!ImGui.CollapsingHeader("Sort Sounds"))
        {
            return;
        }

        if (sounds == null)
        {
            ImGui.TextDisabled("Loaded with the menu's content.");
            return;
        }

        for (int i = 0; i < sounds.Length; i++)
        {
            string name = SongDbSort.All[i].IconName;
            bool open = ImGui.TreeNode(name);

            ImGui.SameLine();
            ImGui.TextDisabled(sounds[i].Summary);

            if (open)
            {
                sounds[i].DrawInspector(name);
                ImGui.TreePop();
            }
        }
    }

    private static SortRowData[] BuildRows()
    {
        SortRowData[] built = new SortRowData[SongDbSort.All.Length];

        for (int i = 0; i < built.Length; i++)
        {
            built[i] = new SortRowData { Name = SongDbSort.All[i].Name, IconIndex = i };
        }

        return built;
    }

    private static int Mod(int value, int length) => length <= 0 ? 0 : (value % length + length) % length;

    //the code default, also the seed for Components/SortMenu.json
    protected override UIGroup BuildDefault()
    {
        UIGroup root = new("SortMenu");

        root.AddChild(new UIImage
        {
            name = "Background",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\5_sortmenu_bg.png"),
            renderOrder = 0
        });

        root.AddChild(new UIScrollItemsGroup("Entries")
        {
            itemComponent = "Components/SortItem.json",
            itemOffset = new Vector3(EntrySpacing, 0.0f, 0.0f),
            navigationAxis = UINavigationAxis.Horizontal,

            //one slot per sort: nothing is ever recycled, the ring is here for the wrap-around
            visibleSlots = SongDbSort.All.Length,
            selectionOffset = 2,
            position = new Vector3(2 * EntrySpacing, 40.0f, 0.0f),
            renderOrder = 1,

            //the original feel: eases the whole way with no floor, capped at what the old per-frame
            //clamp of 10px allowed at 60fps. Speeds are in entries per second, not pixels
            //queueLimit keeps a held key two entries ahead of what is on screen at most, so letting go
            //stops it rather than draining a backlog built while the repeat outran the easing
            motion = new UIScrollMotion(rate: 10.0f, maxSpeed: 600.0f / EntrySpacing, queueLimit: 2.0f),

            //the selected entry sits lower than its neighbours
            curve = new UIItemCurve(UIAxis.Y, distance: 18.0f, range: EntrySpacing)
        });

        return root;
    }

    //the code default for one entry, seeded into Components/SortItem.json
    private static UIGroup BuildEntryDefault()
    {
        UIGroup root = new("SortItem");

        TextureArray icon = root.AddChild(new TextureArray
        {
            name = "Icon",
            anchor = new Vector2(0.5f, 0.5f),
            bindings = { new UIBinding("textureIndex", "Item.IconIndex") }
        });

        //named in sort order, so an entry only carries an index
        foreach (SongDbSort sort in SongDbSort.All)
        {
            icon.resources.Add(SkinResource.System($@"Graphics\Sorting\{sort.IconName}.png"));
        }

        root.AddChild(new UIText(string.Empty, 18)
        {
            name = "Name",
            anchor = new Vector2(0.5f, 0.5f),
            isVisible = false,
            bindings = { new UIBinding("text", "Item.Name") }
        });

        return root;
    }
}
