using DTXMania.Core;
using DTXMania.SongDb;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using SharpDX;
using SlimDX.DirectInput;

namespace DTXMania;

public class SongSelectionContainer : UIGroup
{
    private SongDb.SongDb songDb;
    private UIImage albumArt;
    
    private SongNode currentRoot;
    
    public static DTXTexture fallbackPreImage = DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_preimage default.png"));
    
    private SongSelectionElement[] songSelectionElements = new SongSelectionElement[20];
    private int bufferStartIndex = 0;

    private int WrapIndex(int index)
    {
        return (index + songSelectionElements.Length) % songSelectionElements.Length;
    }

    private SongSelectionElement GetElement(int logicalIndex)
    {
        return songSelectionElements[WrapIndex(bufferStartIndex + logicalIndex)];
    }

    private UIGroup elementsContainer;
    
    public SongSelectionContainer(SongDb.SongDb songDb, UIImage albumArt)
    {
        name = "SongSelectionContainer";

        this.songDb = songDb;
        this.albumArt = albumArt;
        
        currentRoot = songDb.songNodeRoot;
        dontSerialize = true;

        elementsContainer = AddChild(new UIGroup("Elements"));
        
        for (int i = 0; i < songSelectionElements.Length; i++)
        {
            songSelectionElements[i] = elementsContainer.AddChild(SongSelectionElement.Create());
        }
    }

    private const float rowSpacing = 85.0f; //vertical spacing between elements
    private const int selectionIndex = 10; //the center element is the 10th element in the array (0-based index)

    private SongNode currentSelection => songSelectionElements[WrapIndex(bufferStartIndex + selectionIndex)].node;
    
    public void UpdateRoot(SongNode? newRoot = null)
    {
        currentRoot = newRoot ?? songDb.songNodeRoot;
        SongNode node = currentRoot.CurrentSelection;

        bufferStartIndex = 0;
        
        //clear image cache
        foreach (KeyValuePair<SongNode, DTXTexture> element in preImageCache)
        {
            RemoveFromCache(element.Key);
        }
        
        //assign first node to the first element, then use rNextSong to fill the rest
        songSelectionElements[selectionIndex].UpdateSongNode(node, CachePreImage(node));
        songSelectionElements[selectionIndex].position.Y = 0;
        
        //fill the rest of the elements with next songs
        for (int i = selectionIndex + 1; i < songSelectionElements.Length; i++)
        {
            SongNode nextNode = SongNode.rNextSong(songSelectionElements[i - 1].node);
            var tex = CachePreImage(nextNode);
            songSelectionElements[i].UpdateSongNode(nextNode, tex);
            songSelectionElements[i].position.Y = (i - selectionIndex) * rowSpacing;
        }
        
        //fill the first elements with previous songs
        for (int i = selectionIndex - 1; i >= 0; i--)
        {
            SongNode prevNode = SongNode.rPreviousSong(songSelectionElements[i + 1].node);
            var tex = CachePreImage(prevNode);
            songSelectionElements[i].UpdateSongNode(prevNode, tex);
            songSelectionElements[i].position.Y = (i - selectionIndex) * rowSpacing;
        }
        
        //update album art
        UpdateSelectedSongAlbumArt();
        
        lastDrawTime = CDTXMania.Timer.nCurrentTime;
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
        float delta = (CDTXMania.Timer.nCurrentTime - lastDrawTime) / 1000.0f;
        lastDrawTime = CDTXMania.Timer.nCurrentTime;

        if (Math.Abs(targetY - elementsContainer.position.Y) > 0.01f)
        {
            //smoothly move towards targetY
            elementsContainer.position.Y += (targetY - elementsContainer.position.Y) * delta * 10.0f; //10.0f is the speed factor
        }
        else
        {
            elementsContainer.position.Y = targetY; //snap to target if close enough
        }
        
        if (elementsContainer.position.Y >= rowSpacing / 2)
        {
            elementsContainer.position.Y -= rowSpacing;
            targetY -= rowSpacing;
            MoveUp();
        }
        else if (elementsContainer.position.Y <= -rowSpacing / 2)
        {
            elementsContainer.position.Y += rowSpacing;
            targetY += rowSpacing;
            MoveDown();
        }
        
        for (int i = 0; i < songSelectionElements.Length; i++)
        {
            int realIndex = WrapIndex(bufferStartIndex + i);
            var slot = songSelectionElements[realIndex];
            slot.position = new Vector3(i == selectionIndex ? -20 : 0, slot.position.Y, 0);
        }

        base.Draw(parentMatrix);
    }

    public void HandleNavigation()
    {
        //ctKeyRepeat.Up.tRepeatKey(CDTXMania.InputManager.Keyboard.bKeyPressing(Key.UpArrow), new CCounter.DGキー処理(MoveUp));
        //ctKeyRepeat.R.tRepeatKey(CDTXMania.Pad.bPressingGB(EPad.R), new CCounter.DGキー処理(MoveUp));
        if (CDTXMania.InputManager.Keyboard.bKeyPressed(Key.UpArrow)
            || CDTXMania.Pad.bPressedGB(EPad.R)
            || CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.HT))
        {
            targetY += rowSpacing;
        }
        //ctKeyRepeat.Down.tRepeatKey(CDTXMania.InputManager.Keyboard.bKeyPressing(Key.DownArrow), new CCounter.DGキー処理(MoveDown));
        //ctKeyRepeat.B.tRepeatKey(CDTXMania.Pad.bPressingGB(EPad.G), new CCounter.DGキー処理(MoveDown));
        if (CDTXMania.InputManager.Keyboard.bKeyPressed(Key.DownArrow)
            || CDTXMania.Pad.bPressedGB(EPad.G)
            || CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.LT))
        {
            targetY -= rowSpacing;
        }
        if (CDTXMania.InputManager.Keyboard.bKeyPressed(Key.LeftArrow)
            || CDTXMania.Pad.bPressedGB(EPad.Pick) //??
            || CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.SD))
        {
            MoveLeft();
        }
        if (CDTXMania.InputManager.Keyboard.bKeyPressed(Key.RightArrow)
            || CDTXMania.Pad.bPressedGB(EPad.Pick) //??
            || CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.FT))
        {
            MoveRight();
        }

        if (CDTXMania.Input.ActionDecide())
        {
            ActionDecide();
        }
    }
    
    private void MoveUp()
    {
        CDTXMania.Skin.soundCursorMovement.tPlay();

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
        
        Task.Run(() =>
        {
            var newTex = CachePreImage(newNode);
            overwriteElement.UpdateSongNode(newNode, newTex);
        });

        //move ring buffer backward
        bufferStartIndex = WrapIndex(bufferStartIndex - 1);

        //overwrite the new head
        //overwriteElement.UpdateSongNode(newNode, newTex);
        int newTopIndex = bufferStartIndex;
        float newYOffset = songSelectionElements[WrapIndex(newTopIndex + 1)].position.Y - rowSpacing;
        overwriteElement.position.Y = newYOffset;
        songSelectionElements[newTopIndex] = overwriteElement;

        currentRoot.CurrentSelection = currentSelection;
        UpdateSelectedSongAlbumArt();
        
        UpdateRingbufferPositions();
    }
    
    private void MoveDown()
    {
        CDTXMania.Skin.soundCursorMovement.tPlay();

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
        
        Task.Run(() =>
        {
            var newTex = CachePreImage(newNode);
            overwriteElement.UpdateSongNode(newNode, newTex);
        });

        //update the overwritten slot
        //overwriteElement.UpdateSongNode(newNode, newTex);
        overwriteElement.position.Y = songSelectionElements[bottomIndex].position.Y + rowSpacing;
        songSelectionElements[overwriteIndex] = overwriteElement;

        //advance ring buffer start
        bufferStartIndex = WrapIndex(bufferStartIndex + 1);

        currentRoot.CurrentSelection = currentSelection;
        UpdateSelectedSongAlbumArt();
        
        UpdateRingbufferPositions();
    }

    private void UpdateRingbufferPositions()
    {
        //recalculate all positions based on logical order
        for (int i = 0; i < songSelectionElements.Length; i++)
        {
            int realIndex = WrapIndex(bufferStartIndex + i);
            songSelectionElements[realIndex].position.Y = (i - selectionIndex) * rowSpacing;
        }
    }
    
    private void MoveLeft()
    {
        CDTXMania.Skin.soundCursorMovement.tPlay();
    }
    
    private void MoveRight()
    {
        CDTXMania.Skin.soundCursorMovement.tPlay();
    }
    
    private void ActionDecide()
    {
        switch (currentSelection.nodeType)
        {
            case SongNode.ENodeType.SONG:
                break;

            //switch to box node
            case SongNode.ENodeType.BOX:
                CDTXMania.Skin.soundDecide.tPlay();
                UpdateRoot(currentSelection);
                break;
            
            case SongNode.ENodeType.BACKBOX:
                CDTXMania.Skin.soundCancel.tPlay();
                //two levels: the parent of current selection is the box we are in right now
                UpdateRoot(currentSelection.parent.parent);
                break;
        }
    }

    #region PreImage cache
    private Dictionary<SongNode, DTXTexture> preImageCache = new();

    private DTXTexture? CachePreImage(SongNode node)
    {
        if (!preImageCache.TryGetValue(node, out DTXTexture? preImage))
        {
            string imagePath = node.GetImagePath();
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
    #endregion
}