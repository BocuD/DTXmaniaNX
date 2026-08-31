using DTXMania.UI.Skin;
using System.Diagnostics;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.SongDb;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.OpenGL;
using DTXMania.UI.Text;
using SlimDX.DirectInput;
using Color = System.Drawing.Color;

namespace DTXMania;

/// <summary>
/// The song list. A <see cref="UIScrollItemsGroup"/> supplies the recycling window and the smooth scroll;
/// this class supplies the data for it — a window of <see cref="SongRowData"/> walked out of the song tree
/// — plus the thumbnail cache and the list's input handling.
///
/// Songs are a linked structure rather than an indexed one, so the window is kept aligned to the scroll
/// ring's item indices and extended by traversal as it moves.
/// </summary>
public class SongSelectionContainer : UIScrollItemsGroup, IUIItemSource
{
    private const float RowSpacing = 85.0f;
    private const int DefaultWindowSize = 20;
    private const int DefaultSelectionRow = 10;

    private readonly SongDb.SongDb songDb;

    private static SongSelectionAssets assets => SongSelectionAssets.Shared;

    //aligned to the scroll ring, one row per slot, so a skin widening the list widens its data too
    private readonly UISlidingWindow<SongRowData> window = new();

    //read through the stage's "AlbumArt" texture source, so the big art stays a plain skinnable UIImage
    public BaseTexture? CurrentBigAlbumArt { get; private set; }

    public SongNode CurrentRoot => currentRoot;
    private SongNode currentRoot;

    public SongNode UnfilteredRoot;

    private bool updateRootRequested;
    private bool requestIsFiltered;
    private SongNode? newSongRoot;

    //cached so polling navigation every frame doesn't allocate a closure per frame
    private readonly Action scrollToPrevious;
    private readonly Action scrollToNext;

    //the stage drives this list, since deciding on a song is stage flow
    private readonly NavigationRepeat listNavigation = NavigationRepeat.Vertical(useNeck: true);

    public SongSelectionContainer() : base("SongSelectionContainer")
    {
        songDb = CDTXMania.SongDb;
        currentRoot = songDb.songNodeRoot;
        UnfilteredRoot = currentRoot;

        itemComponent = "Components/SongRow.json";
        itemOffset = new Vector3(0, RowSpacing, 0);
        visibleSlots = DefaultWindowSize;
        selectionOffset = DefaultSelectionRow;
        curve = new UIItemCurve(UIAxis.X, -25.0f, 90.0f);
        itemDefault = BuildSongRowDefault;

        SetSource(this);

        scrollToPrevious = () => ScrollBy(-1);
        scrollToNext = () => ScrollBy(1);
    }

    public int ItemCount => Math.Max(1, visibleSlots);

    public object? GetItem(int index)
    {
        EnsureWindow();
        return window.Covers(index) ? window.At(index) : null;
    }

    public SongNode? currentSelection
    {
        get
        {
            EnsureWindow();
            return window.At(SelectedItem).Node;
        }
    }

    private void EnsureWindow() => window.Resize(Math.Max(1, visibleSlots), static () => new SongRowData());

    public void RequestUpdateRoot(SongNode newRoot, bool isFiltered = false)
    {
        updateRootRequested = true;
        newSongRoot = newRoot;
        requestIsFiltered = isFiltered;
    }

    public void UpdateRoot(SongNode? newRoot = null, bool preLoadImages = true, bool isFiltered = false)
    {
        Trace.TraceInformation("Updating song selection root to {0}", newRoot?.title ?? "default root");
        DateTime start = DateTime.Now;

        currentRoot = newRoot ?? songDb.songNodeRoot;

        if (!isFiltered)
        {
            UnfilteredRoot = currentRoot;
        }

        //not cleared: the cache is keyed by path and shared, so another sort reuses decoded thumbnails
        FillWindowAround(currentRoot.CurrentSelection);

        //kick off async image requests now unless we're warming up, where the caller bulk-loads
        if (preLoadImages)
        {
            UpdateImageCache();
        }

        HandleSelectionChanged();

        Trace.TraceInformation("Song selection root updated in {0}ms", (DateTime.Now - start).TotalMilliseconds);
    }

    public void UpdateSelection(SongNode node)
    {
        if (node == null || currentRoot == null || !currentRoot.childNodes.Contains(node))
        {
            return;
        }

        FillWindowAround(node);
        UpdateImageCache();
        HandleSelectionChanged();
    }

    //walks outwards from the selected node to fill the window, with that node at the selection position
    private void FillWindowAround(SongNode? selected)
    {
        EnsureWindow();

        //the remembered selection can be a song the current instrument has no chart for
        if (selected != null && !selected.ShowInSongList())
        {
            selected = SongNode.rNextSong(selected);
        }

        //the selected row sits at the selection position, and the window starts that far before it
        window.Reset(0);
        ScrollTo(selectionOffset);

        int selectedIndex = SelectedItem;
        FillRow(selectedIndex, selected);

        for (int i = selectedIndex + 1; i < window.Start + window.Length; i++)
        {
            FillRow(i, SongNode.rNextSong(window.At(i - 1).Node));
        }

        for (int i = selectedIndex - 1; i >= window.Start; i--)
        {
            FillRow(i, SongNode.rPreviousSong(window.At(i + 1).Node));
        }
    }

    //the window moved by whole items, so walk new nodes into the rows that came into view
    protected override void OnScrolled(int steps)
    {
        EnsureWindow();
        CDTXMania.Skin.soundCursorMovement.tPlay();

        if (!window.Shift(steps, out int firstStale, out int staleCount))
        {
            //jumped further than the window is wide, so nothing is worth reusing
            FillWindowAround(currentSelection);
            HandleSelectionChanged();
            return;
        }

        if (steps > 0)
        {
            for (int i = firstStale; i < firstStale + staleCount; i++)
            {
                FillRow(i, SongNode.rNextSong(window.At(i - 1).Node));
            }
        }
        else
        {
            for (int i = firstStale + staleCount - 1; i >= firstStale; i--)
            {
                FillRow(i, SongNode.rPreviousSong(window.At(i + 1).Node));
            }
        }

        HandleSelectionChanged();
    }

    //fills a row from a node and gives it whatever thumbnail is already decoded, queueing the rest
    private void FillRow(int index, SongNode? node)
    {
        SongRowData row = window.At(index);
        row.SetNode(node);

        BaseTexture? cached = null;
        string path = node != null ? GetPreImagePath(node) : string.Empty;
        if (!string.IsNullOrEmpty(path) && preImageCache.TryGetValue(path, out cached))
        {
            Touch(path);
        }

        row.AlbumArt = cached ?? assets.FallbackPreImage;

        if (node != null && cached == null)
        {
            toBeCached.Add(node);
        }
    }

    private void HandleSelectionChanged()
    {
        UpdateSelectedSongAlbumArt();

        currentRoot.CurrentSelection = currentSelection;

        int closestLevelToTarget = CDTXMania.StageManager.stageSongSelectionNew.GetClosestLevelToTargetForSong(currentSelection);
        CDTXMania.StageManager.stageSongSelectionNew.ChangeSelection(currentSelection, currentSelection?.charts[closestLevelToTarget]);
    }

    //the selected chart's image where it names one, the song's otherwise; charts can differ
    private string SelectedArtPath
    {
        get
        {
            if (currentSelection == null)
            {
                return string.Empty;
            }

            return currentSelection.nodeType == SongNode.ENodeType.BACKBOX
                ? GetPreImagePath(currentSelection)
                : currentSelection.GetImagePath(CDTXMania.StageManager.stageSongSelectionNew.selectedChart);
        }
    }

    /// <summary>Re-reads the art for the selection, which changing difficulty can change.</summary>
    public void UpdateSelectedSongAlbumArt()
    {
        BaseTexture? tex = null;
        string path = SelectedArtPath;

        if (!string.IsNullOrEmpty(path))
        {
            if (preImageCache.TryGetValue(path, out tex))
            {
                Touch(path);
            }
            else
            {
                //a chart's own image is only asked for once it is selected, so it may not be cached yet
                RequestPreImage(path);
            }
        }

        CurrentBigAlbumArt = tex ?? assets.FallbackPreImage;
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        if (updateRootRequested)
        {
            UpdateRoot(newSongRoot, false, requestIsFiltered);
            updateRootRequested = false;
        }

        //at the start of the frame, so a new request waits for the next one and the load stays spread out
        UpdateImageCache();

        base.Draw(parentMatrix);
    }

    public int HandleNavigation()
    {
        if (CDTXMania.InputManager.Keyboard.bKeyPressed(Key.R))
        {
            List<SongNode> candidates = currentRoot.childNodes.FindAll(node => node.ShowInSongList());
            if (candidates.Count > 0)
            {
                UpdateSelection(candidates[Random.Shared.Next(0, candidates.Count)]);
            }
            return (int)CStageSongSelectionNew.EReturnValue.Continue;
        }

        //only the vertical keys exist for now, so a list configured for horizontal input simply has none
        if (RespondsTo(UINavigationAxis.Vertical))
        {
            listNavigation.Poll(scrollToPrevious, scrollToNext);
        }

        if (CDTXMania.Input.ActionDecide() && ActionDecide())
        {
            return (int)CStageSongSelectionNew.EReturnValue.Selected;
        }

        if (CDTXMania.Input.ActionCancel())
        {
            CDTXMania.Skin.soundCancel.tPlay();

            if (currentRoot.nodeType == SongNode.ENodeType.ROOT)
            {
                return (int)CStageSongSelectionNew.EReturnValue.ReturnToTitle;
            }

            RequestUpdateRoot(currentRoot.parent);
        }

        return (int)CStageSongSelectionNew.EReturnValue.Continue;
    }

    public bool ConfirmRandomSong()
    {
        List<SongNode> songs = currentRoot.childNodes
            .FindAll(node => node.nodeType == SongNode.ENodeType.SONG && node.ShowInSongList());

        if (songs.Count == 0)
        {
            return false;
        }

        UpdateSelection(songs[Random.Shared.Next(songs.Count)]);
        return ActionDecide();
    }

    private bool ActionDecide()
    {
        if (currentSelection == null)
        {
            CDTXMania.Skin.soundCancel.tPlay();
            return false;
        }

        switch (currentSelection.nodeType)
        {
            case SongNode.ENodeType.SONG:
                SongNode selectedSong = currentSelection;
                int confirmedSongDifficulty = CDTXMania.StageManager.stageSongSelectionNew.GetClosestLevelToTargetForSong(currentSelection);
                CChartData? selectedChart = selectedSong.charts[confirmedSongDifficulty];

                if (selectedChart is { } chart && chart.HasChartForCurrentMode())
                {
                    CDTXMania.UpdateSelection(selectedSong, chart, confirmedSongDifficulty);
                    return true;
                }

                //todo: Notification lol
                CDTXMania.Skin.soundCancel.tPlay();
                Trace.TraceInformation("Score unavailable for {0} mode",
                    CDTXMania.ConfigIni.bDrumsEnabled ? "Drum" : "Guitar/Bass");
                break;

            case SongNode.ENodeType.BOX:
                CDTXMania.Skin.soundDecide.tPlay();
                RequestUpdateRoot(currentSelection);
                break;

            case SongNode.ENodeType.BACKBOX:
                CDTXMania.Skin.soundCancel.tPlay();
                //two levels: the parent of the current selection is the box we are in right now
                RequestUpdateRoot(currentSelection.parent.parent);
                break;
        }

        return false;
    }

    //the code default for one row, seeded into Components/SongRow.json
    private UIGroup BuildSongRowDefault()
    {
        UIGroup root = new("SongRow");

        TextureArray background = root.AddChild(new TextureArray
        {
            name = "background",
            resources =
            {
                SkinResource.System(@"Graphics\5_bar.png"),
                SkinResource.System(@"Graphics\5_box_closed.png"),
                SkinResource.System(@"Graphics\5_box_open.png")
            },
            pivot = new Vector2(0.0f, 0.5f),
            position = new Vector3(-40.0f, 42.0f, 0.0f),
            renderOrder = -1
        });

        background.bindings.Add(new UIBinding("textureIndex", "Item.BackgroundIndex"));

        //the box textures need a slightly different clip and offset than the bar
        background.bindings.Add(new UIBinding("clipRect.X", "Item.BackgroundClipX"));
        background.bindings.Add(new UIBinding("position.X", "Item.BackgroundOffsetX"));

        root.AddChild(new UIImage
        {
            name = "albumArt",
            imageSource = ImageSource.Dynamic,
            dynamicSource = "Item.AlbumArt",
            size = new Vector2(65, 65),
            position = new Vector3(40, 40, 0),
            pivot = new Vector2(0.5f, 0.5f),
            renderOrder = 1
        });

        HorizontallyScrollingText title = root.AddChild(new HorizontallyScrollingText("", 18));
        title.name = "title";
        title.bindings.Add(new UIBinding("text", "Item.Title"));
        title.bindings.Add(new UIBinding("isVisible", "Item.HasTitle"));
        title.fillColor = Color4.FromColor(Color.Black);
        title.outlineColor = Color4.FromColor(Color.White);
        title.position = new Vector3(78, 38, 0);
        title.pivot = new Vector2(0, 0.5f);
        title.renderOrder = 1;
        title.size.X = 460.0f;
        title.bindings.Add(new UIBinding("scrollingEnabled", "IsSelected"));

        HorizontallyScrollingText artist = root.AddChild(new HorizontallyScrollingText("", 12));
        artist.name = "artist";
        artist.bindings.Add(new UIBinding("text", "Item.Artist"));
        artist.bindings.Add(new UIBinding("isVisible", "Item.HasArtist"));
        artist.fillColor = Color4.FromColor(Color.Black);
        artist.outlineColor = Color4.FromColor(Color.White);
        artist.position = new Vector3(80, 60, 0);
        artist.pivot = new Vector2(0, 0.5f);
        artist.renderOrder = 1;
        artist.size.X = 460.0f;
        artist.bindings.Add(new UIBinding("scrollingEnabled", "IsSelected"));

        root.AddChild(new UIImage
        {
            name = "skillbar",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\5_skillbar.png"),
            pivot = new Vector2(0.0f, 0.5f),
            position = new Vector3(82.0f, 15.0f, 0.0f),
            renderOrder = 2,
            isVisible = false,
            bindings = { new UIBinding("isVisible", "Item.ShowSkill") }
        });

        UIImage skillbarFill = root.AddChild(new UIImage
        {
            name = "skillbarFill",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\5_skillbar_fill.png"),
            pivot = new Vector2(0.0f, 0.5f),
            position = new Vector3(161.0f, 16.0f, 0.0f),
            size = new Vector2(286, 8),
            renderOrder = 1,
            isVisible = false,
            bindings = { new UIBinding("isVisible", "Item.ShowSkill") }
        });

        skillbarFill.bindings.Add(new UIBinding("size.X", "Item.SkillBarWidth"));

        UIText skill = root.AddChild(new UIText("", 12));
        skill.name = "skilltext";
        skill.bindings.Add(new UIBinding("text", "Item.Skill"));
        skill.bindings.Add(new UIBinding("isVisible", "Item.ShowSkill"));
        skill.position = new Vector3(105.0f, 16.0f, 0.0f);
        skill.fillColor = Color4.FromColor(Color.White);
        skill.style = UiTextStyle.Italic;
        skill.pivot = new Vector2(0, 0.5f);
        skill.renderOrder = 2;

        root.AddChild(new TextureArray
        {
            name = "lamp",
            resources =
            {
                SkinResource.System(@"Graphics\Lamp\00.png"),
                SkinResource.System(@"Graphics\Lamp\01.png"),
                SkinResource.System(@"Graphics\Lamp\02.png"),
                SkinResource.System(@"Graphics\Lamp\03.png"),
                SkinResource.System(@"Graphics\Lamp\04.png"),
                SkinResource.System(@"Graphics\Lamp\05.png")
            },
            position = new Vector3(-40, 40, 0),
            pivot = new Vector2(0.5f, 0.5f),
            renderOrder = 1,
            isVisible = false,
            bindings =
            {
                new UIBinding("textureIndex", "Item.LampIndex"),
                new UIBinding("isVisible", "Item.HasLamp")
            }
        });

        return root;
    }

    #region PreImage cache

    //the big art draws at 300x300, so 512 keeps quality while bounding upload cost and VRAM
    private const int PreImageMaxDimension = 512;

    //max preimages kept resident, keyed by path and shared across sort views; evicted least-recent first
    private const int PreImageCacheCapacity = 256;

    private readonly List<SongNode> toBeCached = [];
    private readonly Dictionary<string, BaseTexture> preImageCache = new();
    private readonly HashSet<string> requestedPaths = new(); //paths with a decode in flight
    private readonly Dictionary<string, long> lruUsage = new(); //path -> last-used tick
    private long lruTick;
    private bool disposed;

    public bool isScrolling => IsScrolling;

    private static string GetPreImagePath(SongNode node)
    {
        return node.nodeType != SongNode.ENodeType.BACKBOX
            ? node.GetImagePath()
            : CSkin.Path(@"Graphics\5_preimage backbox.png");
    }

    private void Touch(string path)
    {
        lruUsage[path] = ++lruTick;
    }

    //hand a decoded texture to every row whose node resolves to this image path
    private void AssignThumbnailToRows(string path, BaseTexture tex)
    {
        for (int i = window.Start; i < window.Start + window.Length; i++)
        {
            SongRowData row = window.At(i);
            if (row.Node != null && GetPreImagePath(row.Node) == path)
            {
                row.AlbumArt = tex;
            }
        }
    }

    //image paths a row currently references; these must never be evicted mid-draw
    private HashSet<string> GetPinnedPaths()
    {
        HashSet<string> pinned = new();
        for (int i = window.Start; i < window.Start + window.Length; i++)
        {
            SongNode? node = window.At(i).Node;
            if (node == null) continue;

            string path = GetPreImagePath(node);
            if (!string.IsNullOrEmpty(path)) pinned.Add(path);
        }

        return pinned;
    }

    //decoded on a background thread and uploaded on the main one; rows show the fallback until it lands
    private void RequestPreImage(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;

        if (preImageCache.ContainsKey(path))
        {
            Touch(path);
            return;
        }

        if (!requestedPaths.Add(path)) return; //already in flight

        if (!File.Exists(path))
        {
            requestedPaths.Remove(path);
            return; //no image; rows keep the fallback
        }

        AsyncTextureUploader.Instance.RequestImage(path, PreImageMaxDimension,
            tex => OnPreImageUploaded(path, tex));
    }

    //runs on the main thread (from the upload pump) once the decode + GPU upload completes
    private void OnPreImageUploaded(string path, BaseTexture? tex)
    {
        requestedPaths.Remove(path);

        if (tex == null) return; //missing/undecodable; keep fallback

        if (disposed)
        {
            tex.Dispose();
            return;
        }

        //a duplicate upload raced in; keep the existing entry
        if (preImageCache.TryGetValue(path, out BaseTexture? existing) && existing is { } valid && valid.IsValid())
        {
            tex.Dispose();
            Touch(path);
            return;
        }

        //always cache (even if no row currently shows it) so background prewarming works
        preImageCache[path] = tex;
        Touch(path);
        EvictIfNeeded();

        AssignThumbnailToRows(path, tex);

        //unconditional: the art may have been requested under a selection that has since moved
        UpdateSelectedSongAlbumArt();
    }

    //synchronous decode + upload used to warm images up before the screen becomes visible
    private void LoadPreImageSync(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;

        if (preImageCache.ContainsKey(path))
        {
            Touch(path);
            return;
        }

        if (!File.Exists(path)) return;

        DecodedPixels pixels = AsyncTextureUploader.DecodeImage(path, PreImageMaxDimension);
        if (!pixels.IsValid) return;

        BaseTexture tex = BaseTexture.LoadFromMemory(pixels.Rgba, pixels.Width, pixels.Height, pixels.Name);
        preImageCache[path] = tex;
        Touch(path);
        EvictIfNeeded();

        AssignThumbnailToRows(path, tex);

        //re-resolve unconditionally: the art wanted may have been requested under a different selection,
        //and comparing paths here only refreshes when nothing about the selection moved in between
        UpdateSelectedSongAlbumArt();
    }

    private void EvictIfNeeded()
    {
        if (preImageCache.Count <= PreImageCacheCapacity) return;

        HashSet<string> pinned = GetPinnedPaths();

        //drop least-recently-used entries first, never a path currently on screen
        foreach (string path in lruUsage.OrderBy(kvp => kvp.Value).Select(kvp => kvp.Key).ToList())
        {
            if (preImageCache.Count <= PreImageCacheCapacity) break;
            if (pinned.Contains(path)) continue;
            RemoveFromCache(path);
        }
    }

    private void RemoveFromCache(string path)
    {
        if (preImageCache.TryGetValue(path, out BaseTexture? tex))
        {
            tex.Dispose();
            preImageCache.Remove(path);
        }

        lruUsage.Remove(path);
    }

    public void UpdateImageCache(bool updateAll = false)
    {
        if (toBeCached.Count == 0) return;

        foreach (SongNode node in toBeCached)
        {
            string path = GetPreImagePath(node);
            if (string.IsNullOrEmpty(path)) continue;

            if (updateAll)
            {
                //warmup: load synchronously so images are ready before the transition opens
                LoadPreImageSync(path);
            }
            else
            {
                //live: hand everything to the background decoder; uploads are throttled by the pump
                RequestPreImage(path);
            }
        }

        toBeCached.Clear();
    }

    //request the initial window of images around a root's remembered selection into the shared cache,
    //without changing what this container currently displays. Used to prewarm other sort views in the
    //background so switching to them shows thumbnails immediately.
    public void PrewarmWindow(SongNode? root, int radius = 10)
    {
        if (disposed || root == null) return;

        SongNode? node = root.CurrentSelection;
        if (node != null) RequestPreImage(GetPreImagePath(node));

        SongNode? next = node;
        SongNode? prev = node;
        for (int i = 0; i < radius; i++)
        {
            next = SongNode.rNextSong(next);
            if (next != null) RequestPreImage(GetPreImagePath(next));

            prev = SongNode.rPreviousSong(prev);
            if (prev != null) RequestPreImage(GetPreImagePath(prev));
        }
    }

    #endregion

    public override void Dispose()
    {
        //stop applying uploads that complete after we're gone
        disposed = true;

        foreach (string key in preImageCache.Keys.ToList())
        {
            RemoveFromCache(key);
        }

        requestedPaths.Clear();
        lruUsage.Clear();
        toBeCached.Clear();

        base.Dispose();

        SongSelectionAssets.DisposeShared();
    }
}
