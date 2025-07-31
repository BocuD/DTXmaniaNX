using System.Diagnostics;
using DTXMania.Core;
using DTXMania.SongDb;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using FDK;
using Hexa.NET.ImGui;
using SharpDX;
using SlimDX.DirectInput;

namespace DTXMania;

public class SongSelectionContainer : UIGroup
{
    private SongDb.SongDb songDb;
    private UIImage albumArt;
    private UIGroup elementsContainer;
    
    private SongNode currentRoot;
    
    public static DTXTexture fallbackPreImage;
    
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
        
        currentRoot = songDb.songNodeRoot;
        dontSerialize = true;
        sortByRenderOrder = false;

        elementsContainer = AddChild(new UIGroup("Elements"));
        
        for (int i = 0; i < songSelectionElements.Length; i++)
        {
            songSelectionElements[i] = elementsContainer.AddChild(new SongSelectionElement());
        }
    }

    private float elementSpacing = 85.0f; //spacing between elements
    private int selectionIndex = 10; //the center element is the 10th element in the array (0-based index)

    public SongNode currentSelection => songSelectionElements[WrapIndex(bufferStartIndex + selectionIndex)].node;
    
    private bool updateRootRequested;
    private SongNode? newSongRoot;
    
    public void RequestUpdateRoot(SongNode newRoot)
    {
        updateRootRequested = true;
        newSongRoot = newRoot;
    }
    
    public void UpdateRoot(SongNode? newRoot = null, bool preLoadImages = true)
    {
        Trace.TraceInformation("Updating song selection root to {0}", newRoot?.title ?? "default root");
        DateTime start = DateTime.Now;
        
        //make sure the fallback is loaded
        fallbackPreImage = DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_preimage default.png"));

        currentRoot = newRoot ?? songDb.songNodeRoot;
        SongNode node = currentRoot.CurrentSelection;

        bufferStartIndex = 0;
        
        //clear image cache
        foreach (KeyValuePair<SongNode, DTXTexture> element in preImageCache)
        {
            RemoveFromCache(element.Key);
        }
        
        //assign first node to the first element, then use rNextSong to fill the rest
        songSelectionElements[selectionIndex].UpdateSongNode(node);
        if (preLoadImages)
            songSelectionElements[selectionIndex].UpdateSongThumbnail(CachePreImage(node));
        else toBeCached.Add(node);

        songSelectionElements[selectionIndex].position.Y = 0;
        songSelectionElements[selectionIndex].SetHighlighted(true);

        //fill the rest of the elements with next songs
        for (int i = selectionIndex + 1; i < songSelectionElements.Length; i++)
        {
            SongNode nextNode = SongNode.rNextSong(songSelectionElements[i - 1].node);

            songSelectionElements[i].UpdateSongNode(nextNode);
            if (preLoadImages)
                songSelectionElements[i].UpdateSongThumbnail(CachePreImage(nextNode));
            else toBeCached.Add(nextNode);
            
            songSelectionElements[i].position.Y = (i - selectionIndex) * elementSpacing;
            songSelectionElements[i].SetHighlighted(false);
        }
        
        //fill the first elements with previous songs
        for (int i = selectionIndex - 1; i >= 0; i--)
        {
            SongNode prevNode = SongNode.rPreviousSong(songSelectionElements[i + 1].node);

            songSelectionElements[i].UpdateSongNode(prevNode);
            if (preLoadImages)
                songSelectionElements[i].UpdateSongThumbnail(CachePreImage(prevNode));
            else toBeCached.Add(prevNode);
            
            songSelectionElements[i].position.Y = (i - selectionIndex) * elementSpacing;
            songSelectionElements[i].SetHighlighted(false);
        }
        
        lastDrawTime = CDTXMania.Timer.nCurrentTime;
        
        //update album art, preview sound, etc
        HandleSelectionChanged();
        
        Trace.TraceInformation("Song selection root updated in {0}ms", (DateTime.Now - start).TotalMilliseconds);
    }

    private void HandleSelectionChanged()
    {
        UpdateSelectedSongAlbumArt();
        
        currentRoot.CurrentSelection = currentSelection;
        
        int closestLevelToTarget = CDTXMania.StageManager.stageSongSelectionNew.GetClosestLevelToTargetForSong(currentSelection);
        CDTXMania.StageManager.stageSongSelectionNew.ChangeSelection(currentSelection, currentSelection.charts[closestLevelToTarget]);
    }

    private void UpdateSelectedSongAlbumArt()
    {
        preImageCache.TryGetValue(currentSelection, out DTXTexture? tex);

        if (tex == null)
        {
            tex = fallbackPreImage;
        }

        albumArt.SetTexture(tex, false);
        albumArt.clipRect = new RectangleF(0, 0, tex.Width, tex.Height);
    }

    private long lastDrawTime;
    private float targetY; //used for smooth scrolling
    
    public override void Draw(Matrix parentMatrix)
    {
        if (updateRootRequested)
        {
            UpdateRoot(newSongRoot, false);
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
                return (int)CStageSongSelection.EReturnValue.Selected;
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
                return (int)CStageSongSelection.EReturnValue.ReturnToTitle;
            }
        }

        return 0;
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
        
        overwriteElement.UpdateSongNode(newNode);
        toBeCached.Add(newNode);

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
        
        overwriteElement.UpdateSongNode(newNode);
        toBeCached.Add(newNode);
        
        //update the overwritten slot
        //overwriteElement.UpdateSongNode(newNode, newTex);
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
                var confirmedSong = currentSelection;
                var confirmedSongDifficulty = CDTXMania.StageManager.stageSongSelectionNew.GetClosestLevelToTargetForSong(currentSelection);
                var confirmedChart = confirmedSong.charts[confirmedSongDifficulty];

                if (confirmedSong != null && confirmedChart != null)
                {
                    if (confirmedChart.HasChartForCurrentMode())
                    {
                        CDTXMania.UpdateSelection(confirmedSong, confirmedChart,
                            confirmedSongDifficulty);
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

    private readonly List<SongNode> toBeCached = [];
    private readonly Dictionary<SongNode, DTXTexture> preImageCache = new();
    
    public bool isScrolling => MathF.Abs(targetY) > 2.0f;

    private DTXTexture? CachePreImage(SongNode node)
    {
        if (!preImageCache.TryGetValue(node, out DTXTexture? preImage))
        {
            string imagePath = node.nodeType != SongNode.ENodeType.BACKBOX 
                ? node.GetImagePath()
                : CSkin.Path(@"Graphics\5_preimage backbox.png");
            
            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                preImage = DTXTexture.LoadFromPath(imagePath);
            }
        }

        if (preImage != null)
        {
            preImageCache[node] = preImage;
            return preImage;
        }
        return null;
    }

    private void RemoveFromCache(SongNode node)
    {
        if (preImageCache.TryGetValue(node, out DTXTexture? tex))
        {
            tex.Dispose();
            preImageCache.Remove(node);
        }
    }

    public void UpdateImageCache(bool updateAll = false)
    {
        int maxIterationCount = updateAll ? songSelectionElements.Length : 1;
        //cache one image per frame
        if (toBeCached.Count > 0)
        {
            for (int i = 0; i < maxIterationCount && toBeCached.Count > 0; i++)
            {
                SongNode toCache = toBeCached.First();
                toBeCached.RemoveAt(0);

                DTXTexture? preImage = CachePreImage(toCache);

                foreach (SongSelectionElement element in songSelectionElements)
                {
                    if (element.node == toCache)
                    {
                        element.UpdateSongThumbnail(preImage);
                    }
                }
            }
        }
    }

    #endregion

    public override void Dispose()
    {
        base.Dispose();
        
        SongSelectionElement.DisposeSongSelectElementAssets();
    }
}