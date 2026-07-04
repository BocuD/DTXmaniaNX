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
        
        //make sure the fallback is loaded
        fallbackPreImage = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\5_preimage default.png"));

        currentRoot = newRoot ?? songDb.songNodeRoot;
        SongNode node = currentRoot.CurrentSelection;

        if (!isFiltered)
        {
            UnfilteredRoot = currentRoot;
        }

        bufferStartIndex = 0;
        
        //clear image cache (snapshot the keys: RemoveFromCache mutates the dictionary)
        foreach (SongNode key in preImageCache.Keys.ToList())
        {
            RemoveFromCache(key);
        }
        
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
            preImageCache.TryGetValue(currentSelection, out tex);
        }

        if (tex == null)
        {
            tex = fallbackPreImage;
        }

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
    
    private struct STKeyRepeatCounter()
    {
        public CCounter Up = new( 0, 0, 0, CDTXMania.Timer );
        public CCounter Down = new( 0, 0, 0, CDTXMania.Timer );
        public CCounter R = new( 0, 0, 0, CDTXMania.Timer );
        public CCounter B = new( 0, 0, 0, CDTXMania.Timer );
    }
    private STKeyRepeatCounter ctKeyRepeat = new();
    
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
        
        ctKeyRepeat.Up.tRepeatKey(CDTXMania.InputManager.Keyboard.bKeyPressing(Key.UpArrow),
            () => ApplyScrollDelta(elementSpacing), 400, 25);
        ctKeyRepeat.R.tRepeatKey(CDTXMania.Pad.bPressingGB(EPad.R),
            () => ApplyScrollDelta(elementSpacing), 400, 25);
        
        if (CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.HT))
        {
            ApplyScrollDelta(elementSpacing);
        }
        
        ctKeyRepeat.Down.tRepeatKey(CDTXMania.InputManager.Keyboard.bKeyPressing(Key.DownArrow), 
            () => ApplyScrollDelta(-elementSpacing), 400, 25);
        ctKeyRepeat.B.tRepeatKey(CDTXMania.Pad.bPressingGB(EPad.G), 
            () => ApplyScrollDelta(-elementSpacing), 400, 25);
        
        if (CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.LT))
        {
            ApplyScrollDelta(-elementSpacing);
        }

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

        if (currentRoot.childNodes.Count > songSelectionElements.Length)
        {
            RemoveFromCache(overwriteElement.node);
        }

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

        if (currentRoot.childNodes.Count > songSelectionElements.Length)
        {
            RemoveFromCache(overwriteElement.node);
        }

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

    private readonly List<SongNode> toBeCached = [];
    private readonly Dictionary<SongNode, BaseTexture> preImageCache = new();
    private readonly HashSet<SongNode> requestedNodes = new(); //nodes with a decode in flight
    private bool disposed;

    public bool isScrolling => MathF.Abs(targetY) > 2.0f;

    private static string GetPreImagePath(SongNode node)
    {
        return node.nodeType != SongNode.ENodeType.BACKBOX
            ? node.GetImagePath()
            : CSkin.Path(@"Graphics\5_preimage backbox.png");
    }

    private void AssignThumbnailToElements(SongNode node, BaseTexture tex)
    {
        foreach (SongSelectionElement element in songSelectionElements)
        {
            if (element.node == node)
            {
                element.UpdateSongThumbnail(tex);
            }
        }
    }

    private bool IsNodeDisplayed(SongNode node)
    {
        foreach (SongSelectionElement element in songSelectionElements)
        {
            if (element.node == node) return true;
        }
        return false;
    }

    //assign a node to an element: show the cached image if present (fallback otherwise) so a
    //recycled element never draws a stale/disposed texture, and queue the image for caching.
    private void AssignNode(SongSelectionElement element, SongNode? node)
    {
        element.UpdateSongNode(node);
        element.UpdateSongThumbnail(node != null ? preImageCache.GetValueOrDefault(node) : null);

        if (node != null && !preImageCache.ContainsKey(node))
        {
            toBeCached.Add(node);
        }
    }

    //queue a preimage to be decoded on the background thread and uploaded (throttled) on the main
    //thread. Elements keep showing the fallback until it arrives.
    private void RequestPreImage(SongNode? node)
    {
        if (node == null) return;

        if (preImageCache.TryGetValue(node, out BaseTexture? cached))
        {
            AssignThumbnailToElements(node, cached);
            return;
        }

        if (!requestedNodes.Add(node)) return; //already in flight

        string imagePath = GetPreImagePath(node);
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
        {
            requestedNodes.Remove(node);
            return; //no image; elements keep the fallback
        }

        AsyncTextureUploader.Instance.RequestImage(imagePath, PreImageMaxDimension,
            tex => OnPreImageUploaded(node, tex));
    }

    //runs on the main thread (from the upload pump) once the decode + GPU upload completes.
    private void OnPreImageUploaded(SongNode node, BaseTexture? tex)
    {
        requestedNodes.Remove(node);

        if (tex == null) return; //missing/undecodable; keep fallback

        //drop the result if the container is gone or the node scrolled out of the buffer.
        if (disposed || (!IsNodeDisplayed(node) && currentSelection != node))
        {
            tex.Dispose();
            return;
        }

        //guard against a duplicate upload racing in
        if (preImageCache.TryGetValue(node, out BaseTexture? existing) && existing is { } valid && valid.IsValid())
        {
            tex.Dispose();
            return;
        }

        preImageCache[node] = tex;
        AssignThumbnailToElements(node, tex);

        if (currentSelection == node)
        {
            UpdateSelectedSongAlbumArt();
        }
    }

    //synchronous decode + upload used to warm images up before the screen becomes visible.
    private void LoadPreImageSync(SongNode? node)
    {
        if (node == null) return;

        if (preImageCache.TryGetValue(node, out BaseTexture? cached))
        {
            AssignThumbnailToElements(node, cached);
            return;
        }

        string imagePath = GetPreImagePath(node);
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath)) return;

        DecodedPixels pixels = AsyncTextureUploader.DecodeImage(imagePath, PreImageMaxDimension);
        if (!pixels.IsValid) return;

        BaseTexture tex = BaseTexture.LoadFromMemory(pixels.Rgba, pixels.Width, pixels.Height, pixels.Name);
        preImageCache[node] = tex;
        AssignThumbnailToElements(node, tex);

        if (currentSelection == node)
        {
            UpdateSelectedSongAlbumArt();
        }
    }

    private void RemoveFromCache(SongNode node)
    {
        if (preImageCache.TryGetValue(node, out BaseTexture? tex))
        {
            tex.Dispose();
            preImageCache.Remove(node);
        }
    }

    public void UpdateImageCache(bool updateAll = false)
    {
        if (toBeCached.Count == 0) return;

        if (updateAll)
        {
            //warmup: load synchronously so images are ready before the transition opens.
            foreach (SongNode node in toBeCached)
            {
                LoadPreImageSync(node);
            }
        }
        else
        {
            //live: hand everything to the background decoder; uploads are throttled by the pump.
            foreach (SongNode node in toBeCached)
            {
                RequestPreImage(node);
            }
        }

        toBeCached.Clear();
    }

    #endregion

    public override void Dispose()
    {
        //stop applying uploads that complete after we're gone
        disposed = true;

        foreach (SongNode key in preImageCache.Keys.ToList())
        {
            RemoveFromCache(key);
        }
        requestedNodes.Clear();
        toBeCached.Clear();

        base.Dispose();

        SongSelectionElement.DisposeSongSelectElementAssets();
    }

  
}