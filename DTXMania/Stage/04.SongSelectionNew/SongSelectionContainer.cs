using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.SongDb;
using DTXMania.UI.Drawable;
using DTXMania.UI.OpenGL;
using FDK;
using Hexa.NET.ImGui;
using SlimDX.DirectInput;

namespace DTXMania;

public class SongSelectionContainer : UIGroup
{
    private SongDb.SongDb songDb;
    private UIImage albumArt;
    private UIGroup elementsContainer;

    public SongNode CurrentRoot => currentRoot;
    private SongNode currentRoot;
    
    public static BaseTexture fallbackPreImage;
    
    private SongSelectionElement[] songSelectionElements = new SongSelectionElement[20];
    private int bufferStartIndex = 0;

    private int WrapIndex(int index)
    {
        return (index + songSelectionElements.Length) % songSelectionElements.Length;
    }
    
    public SongSelectionContainer(SongDb.SongDb songDb, UIImage albumArt)
    {
        name = "SongSelectionContainer";

        this.songDb = songDb;
        this.albumArt = albumArt;
        
        SongSelectionElement.LoadSongSelectElementAssets();
        
        currentRoot = songDb.songNodeRoot;
        dontSerialize = true;

        elementsContainer = AddChild(new UIGroup("Elements"));
        elementsContainer.sortByRenderOrder = false;

        for (int i = 0; i < songSelectionElements.Length; i++)
        {
            songSelectionElements[i] = elementsContainer.AddChild(new SongSelectionElement());
        }
    }

    private float elementSpacing = 85.0f; //spacing between elements
    private int selectionIndex = 10; //the center element is the 10th element in the array (0-based index)

    public SongNode? currentSelection => songSelectionElements[WrapIndex(bufferStartIndex + selectionIndex)].node;
    
    private bool updateRootRequested;
    private bool requestIsFiltered;
    private SongNode? newSongRoot;
    
    public void RequestUpdateRoot(SongNode newRoot, bool isFiltered = false)
    {
        updateRootRequested = true;
        newSongRoot = newRoot;
        requestIsFiltered = isFiltered;
    }
    
    public SongNode UnfilteredRoot;
    
    public void UpdateRoot(SongNode? newRoot = null, bool preLoadImages = true, bool isFiltered = false)
    {
        Trace.TraceInformation("Updating song selection root to {0}", newRoot?.title ?? "default root");
        DateTime start = DateTime.Now;
        
        //make sure the fallback is loaded (once; it's shared and long-lived)
        if (fallbackPreImage == null || !fallbackPreImage.IsValid())
        {
            fallbackPreImage = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\5_preimage default.png"));
        }

        currentRoot = newRoot ?? songDb.songNodeRoot;
        SongNode node = currentRoot.CurrentSelection;

        if (!isFiltered)
        {
            UnfilteredRoot = currentRoot;
        }

        bufferStartIndex = 0;

        //note: the preimage cache is keyed by path and shared across sort views, so it is NOT
        //cleared here - re-pointing at another sort reuses already-decoded thumbnails.

        //assign first node to the first element, then use rNextSong to fill the rest
        AssignNode(songSelectionElements[selectionIndex], node);
        songSelectionElements[selectionIndex].position.Y = 0;
        songSelectionElements[selectionIndex].SetHighlighted(true);

        //fill the rest of the elements with next songs
        for (int i = selectionIndex + 1; i < songSelectionElements.Length; i++)
        {
            SongNode nextNode = SongNode.rNextSong(songSelectionElements[i - 1].node);

            AssignNode(songSelectionElements[i], nextNode);
            songSelectionElements[i].position.Y = (i - selectionIndex) * elementSpacing;
            songSelectionElements[i].SetHighlighted(false);
        }

        //fill the first elements with previous songs
        for (int i = selectionIndex - 1; i >= 0; i--)
        {
            SongNode prevNode = SongNode.rPreviousSong(songSelectionElements[i + 1].node);

            AssignNode(songSelectionElements[i], prevNode);
            songSelectionElements[i].position.Y = (i - selectionIndex) * elementSpacing;
            songSelectionElements[i].SetHighlighted(false);
        }

        //kick off async image requests now unless we're warming up (the caller will bulk-load).
        if (preLoadImages)
        {
            UpdateImageCache();
        }

        lastDrawTime = CDTXMania.Timer.nCurrentTime;

        //update album art, preview sound, etc
        HandleSelectionChanged();
        
        Trace.TraceInformation("Song selection root updated in {0}ms", (DateTime.Now - start).TotalMilliseconds);
    }
    
    public void UpdateSelection(SongNode node)
    {
        if (node == null || currentRoot == null)
            return;

        // Find the target index inside the current root
        int targetIndex = currentRoot.childNodes.FindIndex(x => x == node);
        if (targetIndex < 0)
            return; // Node not in current root
        
        //unhighlight old
        var previouslySelected = songSelectionElements[WrapIndex(bufferStartIndex + selectionIndex)];
        previouslySelected.SetHighlighted(false);
        
        bufferStartIndex = 0;

        //update selectionIndex element with the requested node
        AssignNode(songSelectionElements[selectionIndex], node);
        songSelectionElements[selectionIndex].position.Y = 0;
        songSelectionElements[selectionIndex].SetHighlighted(true);

        //fill next elements
        for (int i = selectionIndex + 1; i < songSelectionElements.Length; i++)
        {
            SongNode nextNode = SongNode.rNextSong(songSelectionElements[i - 1].node);
            AssignNode(songSelectionElements[i], nextNode);
            songSelectionElements[i].position.Y = (i - selectionIndex) * elementSpacing;
            songSelectionElements[i].SetHighlighted(false);
        }

        //fill previous elements
        for (int i = selectionIndex - 1; i >= 0; i--)
        {
            SongNode prevNode = SongNode.rPreviousSong(songSelectionElements[i + 1].node);
            AssignNode(songSelectionElements[i], prevNode);
            songSelectionElements[i].position.Y = (i - selectionIndex) * elementSpacing;
            songSelectionElements[i].SetHighlighted(false);
        }

        //kick off async image requests for the newly assigned nodes
        UpdateImageCache();

        //update album art, preview sound, etc.
        HandleSelectionChanged();
        UpdateRingbufferPositions();
    }

    private void HandleSelectionChanged()
    {
        UpdateSelectedSongAlbumArt();
        
        currentRoot.CurrentSelection = currentSelection;
        
        int closestLevelToTarget = CDTXMania.StageManager.stageSongSelectionNew.GetClosestLevelToTargetForSong(currentSelection);
        CDTXMania.StageManager.stageSongSelectionNew.ChangeSelection(currentSelection, currentSelection?.charts[closestLevelToTarget]);
    }

    private void UpdateSelectedSongAlbumArt()
    {
        BaseTexture? tex = null;
        if (currentSelection != null)
        {
            string path = GetPreImagePath(currentSelection);
            if (!string.IsNullOrEmpty(path) && preImageCache.TryGetValue(path, out tex))
            {
                Touch(path);
            }
        }

        tex ??= fallbackPreImage;

        albumArt.SetTexture(tex, false, false);
        albumArt.clipRect = new RectangleF(0, 0, tex.Width, tex.Height);
    }

    private long lastDrawTime;
    private float targetY; //used for smooth scrolling
    
    public override void Draw(Matrix4x4 parentMatrix)
    {
        if (updateRootRequested)
        {
            UpdateRoot(newSongRoot, false, requestIsFiltered);
            updateRootRequested = false;
        }
        
        //update image cache at the start of the frame
        //this makes sure that requesting a new image to be cached means
        //it won't get handled until the next frame,
        //further spreading the load
        UpdateImageCache();
        
        float delta = (CDTXMania.Timer.nCurrentTime - lastDrawTime) / 1000.0f;
        lastDrawTime = CDTXMania.Timer.nCurrentTime;

        if (Math.Abs(targetY - elementsContainer.position.Y) > 0.01f)
        {
            float movementAmount = (targetY - elementsContainer.position.Y) * delta * 10.0f; //10.0f is the speed factor
            
            //clamp
            movementAmount = Math.Clamp(movementAmount, -10.0f, 10.0f);
            elementsContainer.position.Y += movementAmount;
        }
        else
        {
            elementsContainer.position.Y = targetY; //snap to target if close enough
        }
        
        if (elementsContainer.position.Y >= elementSpacing / 2)
        {
            elementsContainer.position.Y -= elementSpacing;
            targetY -= elementSpacing;
            MoveUp();
        }
        else if (elementsContainer.position.Y <= -elementSpacing / 2)
        {
            elementsContainer.position.Y += elementSpacing;
            targetY += elementSpacing;
            MoveDown();
        }
        
        for (int i = 0; i < songSelectionElements.Length; i++)
        {
            int realIndex = WrapIndex(bufferStartIndex + i);
            var slot = songSelectionElements[realIndex];
            
            //determine how close we are to Y 0: y 0 should give an offset to x,
            //abs of y greater than offsetRange should give no offset, with the part in between having a curve
            float distanceTo0 = MathF.Abs(slot.position.Y + elementsContainer.position.Y); //positive only
            float t = Math.Clamp((distanceTo0 - offsetRange) * -1, 0, offsetRange);
            //first subtract offsetRange so the range is now -offsetRange - maxDistance.
            //then invert, so range becomes -maxDistance - offsetRange, then clamp from 0-offsetRange
            t /= offsetRange; //normalize range

            //x offset is 30 to the left here
            slot.position.X = t * -offsetDistance;
        }

        base.Draw(parentMatrix);
    }

    private float offsetRange = 90;
    private float offsetDistance = 25;
    public override void DrawInspector()
    {
        base.DrawInspector();

        if (ImGui.CollapsingHeader("Element Positioning"))
        {
            ImGui.InputFloat("Element Spacing", ref elementSpacing);
            ImGui.InputInt("Selection Index", ref selectionIndex);
        }

        if (ImGui.CollapsingHeader("Element Animation"))
        {
            ImGui.InputFloat("Offset Range", ref offsetRange);
            ImGui.InputFloat("Offset Distance", ref offsetDistance);
        }
    }
    
    public int HandleNavigation()
    {
        void ApplyScrollDelta(float amount)
        {
            if (amount < 0 && targetY > amount * 2) targetY += amount;
            else if (amount > 0 && targetY < amount * 2) targetY += amount;
        }

        if (CDTXMania.InputManager.Keyboard.bKeyPressed(Key.R))
        {
            //find a random song
            var random = new Random();
            int randomIndex = random.Next(0, currentRoot.childNodes.Count);
            var randomNode = currentRoot.childNodes[randomIndex];
            UpdateSelection(randomNode);
            return (int) CStageSongSelectionNew.EReturnValue.Continue;
        }

        CDTXMania.Input.Navigate(() => ApplyScrollDelta(elementSpacing), () => ApplyScrollDelta(-elementSpacing));

        if (CDTXMania.Input.ActionDecide())
        {
            if (ActionDecide()) //returns true on song select
                return (int)CStageSongSelectionNew.EReturnValue.Selected;
        }
        
        if (CDTXMania.Input.ActionCancel())
        {
            if (currentRoot.nodeType != SongNode.ENodeType.ROOT)
            {
                CDTXMania.Skin.soundCancel.tPlay();
                RequestUpdateRoot(currentRoot.parent);
            }
            else
            {
                CDTXMania.Skin.soundCancel.tPlay();
                return (int)CStageSongSelectionNew.EReturnValue.ReturnToTitle;
            }
        }

        return (int) CStageSongSelectionNew.EReturnValue.Continue;
    }
    
    private void MoveUp()
    {
        CDTXMania.Skin.soundCursorMovement.tPlay();
        
        //determine the currently highlighted element and update it
        var previouslySelected = songSelectionElements[WrapIndex(bufferStartIndex + selectionIndex)];
        previouslySelected.SetHighlighted(false);

        //slot to overwrite is the last one logically (before decrementing)
        int overwriteIndex = WrapIndex(bufferStartIndex + songSelectionElements.Length - 1);
        var overwriteElement = songSelectionElements[overwriteIndex];

        //determine new node to load: one before the currently top-most visible element
        int topIndex = bufferStartIndex;
        var firstVisibleNode = songSelectionElements[topIndex].node;
        var newNode = SongNode.rPreviousSong(firstVisibleNode);
        
        AssignNode(overwriteElement, newNode);

        //move ring buffer backward
        bufferStartIndex = WrapIndex(bufferStartIndex - 1);

        //overwrite the new head
        int newTopIndex = bufferStartIndex;
        float newYOffset = songSelectionElements[WrapIndex(newTopIndex + 1)].position.Y - elementSpacing;
        overwriteElement.position.Y = newYOffset;
        songSelectionElements[newTopIndex] = overwriteElement;
        
        //determine the new highlighted element
        var newlySelected = songSelectionElements[WrapIndex(bufferStartIndex + selectionIndex)];
        newlySelected.SetHighlighted(true);

        HandleSelectionChanged();
        UpdateRingbufferPositions();
    }
    
    private void MoveDown()
    {
        CDTXMania.Skin.soundCursorMovement.tPlay();
        
        //determine the currently highlighted element and unset highlight
        var previouslySelected = songSelectionElements[WrapIndex(bufferStartIndex + selectionIndex)];
        previouslySelected.SetHighlighted(false);

        //determine logical slot to overwrite: the "topmost" one
        int overwriteIndex = WrapIndex(bufferStartIndex);
        var overwriteElement = songSelectionElements[overwriteIndex];

        //determine new node to load: one after the currently bottom-most visible element
        int bottomIndex = WrapIndex(bufferStartIndex + songSelectionElements.Length - 1);
        var lastVisibleNode = songSelectionElements[bottomIndex].node;
        var newNode = SongNode.rNextSong(lastVisibleNode);
        
        AssignNode(overwriteElement, newNode);
        
        //update the overwritten slot
        overwriteElement.position.Y = songSelectionElements[bottomIndex].position.Y + elementSpacing;
        songSelectionElements[overwriteIndex] = overwriteElement;

        //advance ring buffer start
        bufferStartIndex = WrapIndex(bufferStartIndex + 1);
        
        //determine the new highlighted element and set highlight
        var newlySelected = songSelectionElements[WrapIndex(bufferStartIndex + selectionIndex)];
        newlySelected.SetHighlighted(true);
    
        HandleSelectionChanged();
        UpdateRingbufferPositions();
    }

    private void UpdateRingbufferPositions()
    {
        //recalculate all positions based on logical order
        for (int i = 0; i < songSelectionElements.Length; i++)
        {
            int realIndex = WrapIndex(bufferStartIndex + i);
            songSelectionElements[realIndex].position.Y = (i - selectionIndex) * elementSpacing;
        }
    }

    private bool ActionDecide()
    {
        if (currentSelection == null)
        {
            //play cancel sound if no selection
            CDTXMania.Skin.soundCancel.tPlay();
            return false;
        }
        switch (currentSelection.nodeType)
        {
            case SongNode.ENodeType.SONG:
                var selectedSong = currentSelection;
                var confirmedSongDifficulty = CDTXMania.StageManager.stageSongSelectionNew.GetClosestLevelToTargetForSong(currentSelection);
                var selectedChart = selectedSong.charts[confirmedSongDifficulty];

                if (selectedSong != null && selectedChart != null)
                {
                    if (selectedChart.HasChartForCurrentMode())
                    {
                        CDTXMania.UpdateSelection(selectedSong, selectedChart, confirmedSongDifficulty);
                        return true;
                    }
                }

                //todo: Notification lol
                CDTXMania.Skin.soundCancel.tPlay();
                Trace.TraceInformation("Score unavailable for {0} mode",
                    CDTXMania.ConfigIni.bDrumsEnabled ? "Drum" : "Guitar/Bass");
                break;

            //switch to box node
            case SongNode.ENodeType.BOX:
                CDTXMania.Skin.soundDecide.tPlay();
                RequestUpdateRoot(currentSelection);
                break;

            case SongNode.ENodeType.BACKBOX:
                CDTXMania.Skin.soundCancel.tPlay();
                //two levels: the parent of current selection is the box we are in right now
                RequestUpdateRoot(currentSelection.parent.parent);
                break;
        }

        return false;
    }

    public void PreRenderText()
    {
        foreach (SongSelectionElement element in songSelectionElements)
        {
            element.PreRenderText();
        }
    }

    #region PreImage cache

    //cap the decoded preimage size: the big album art draws at 300x300 and thumbnails at 65x65,
    //so 512 preserves quality while bounding per-upload cost and VRAM usage.
    private const int PreImageMaxDimension = 512;

    //max distinct preimages kept resident. Keyed by image path and shared across all sort views
    //(the same songs appear in every sort), so switching sorts reuses already-uploaded textures.
    //An LRU keeps a large library from growing the cache without bound.
    private const int PreImageCacheCapacity = 256;

    private readonly List<SongNode> toBeCached = [];
    //keyed by resolved image path so entries survive UpdateRoot and are shared across sort views.
    private readonly Dictionary<string, BaseTexture> preImageCache = new();
    private readonly HashSet<string> requestedPaths = new(); //paths with a decode in flight
    private readonly Dictionary<string, long> lruUsage = new(); //path -> last-used tick
    private long lruTick;
    private bool disposed;

    public bool isScrolling => MathF.Abs(targetY) > 2.0f;

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

    //assign a decoded texture to every visible element whose node resolves to this image path.
    private void AssignThumbnailToElements(string path, BaseTexture tex)
    {
        foreach (SongSelectionElement element in songSelectionElements)
        {
            if (element.node != null && GetPreImagePath(element.node) == path)
            {
                element.UpdateSongThumbnail(tex);
            }
        }
    }

    //image paths currently referenced by a visible element (the current selection is always one of
    //them, so the big album art's texture is covered too); these must never be evicted mid-draw.
    private HashSet<string> GetPinnedPaths()
    {
        HashSet<string> pinned = new();
        foreach (SongSelectionElement element in songSelectionElements)
        {
            if (element.node == null) continue;
            string path = GetPreImagePath(element.node);
            if (!string.IsNullOrEmpty(path)) pinned.Add(path);
        }
        return pinned;
    }

    //assign a node to an element: show the cached image if present (fallback otherwise) so a
    //recycled element never draws a stale/disposed texture, and queue the image for caching.
    private void AssignNode(SongSelectionElement element, SongNode? node)
    {
        element.UpdateSongNode(node);

        BaseTexture? cached = null;
        string path = node != null ? GetPreImagePath(node) : string.Empty;
        if (!string.IsNullOrEmpty(path) && preImageCache.TryGetValue(path, out cached))
        {
            Touch(path);
        }

        element.UpdateSongThumbnail(cached);

        if (node != null && cached == null)
        {
            toBeCached.Add(node);
        }
    }

    //queue a preimage to be decoded on the background thread and uploaded (throttled) on the main
    //thread. Elements keep showing the fallback until it arrives.
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
            return; //no image; elements keep the fallback
        }

        AsyncTextureUploader.Instance.RequestImage(path, PreImageMaxDimension,
            tex => OnPreImageUploaded(path, tex));
    }

    //runs on the main thread (from the upload pump) once the decode + GPU upload completes.
    private void OnPreImageUploaded(string path, BaseTexture? tex)
    {
        requestedPaths.Remove(path);

        if (tex == null) return; //missing/undecodable; keep fallback

        if (disposed)
        {
            tex.Dispose();
            return;
        }

        //a duplicate upload raced in; keep the existing entry.
        if (preImageCache.TryGetValue(path, out BaseTexture? existing) && existing is { } valid && valid.IsValid())
        {
            tex.Dispose();
            Touch(path);
            return;
        }

        //always cache (even if no element currently shows it) so background prewarming works.
        preImageCache[path] = tex;
        Touch(path);
        EvictIfNeeded();

        AssignThumbnailToElements(path, tex);

        if (currentSelection != null && GetPreImagePath(currentSelection) == path)
        {
            UpdateSelectedSongAlbumArt();
        }
    }

    //synchronous decode + upload used to warm images up before the screen becomes visible.
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

        AssignThumbnailToElements(path, tex);

        if (currentSelection != null && GetPreImagePath(currentSelection) == path)
        {
            UpdateSelectedSongAlbumArt();
        }
    }

    private void EvictIfNeeded()
    {
        if (preImageCache.Count <= PreImageCacheCapacity) return;

        HashSet<string> pinned = GetPinnedPaths();

        //drop least-recently-used entries first, never a path currently on screen.
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
                //warmup: load synchronously so images are ready before the transition opens.
                LoadPreImageSync(path);
            }
            else
            {
                //live: hand everything to the background decoder; uploads are throttled by the pump.
                RequestPreImage(path);
            }
        }

        toBeCached.Clear();
    }

    //request the initial window of images around a root's remembered selection into the shared
    //cache, without changing what this container currently displays. Used to prewarm other sort
    //views in the background so switching to them shows thumbnails immediately.
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

        SongSelectionElement.DisposeSongSelectElementAssets();
    }

  
}