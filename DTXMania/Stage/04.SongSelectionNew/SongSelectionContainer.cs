using DTXMania.Core;
using DTXMania.SongDb;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using SharpDX;
using SlimDX.DirectInput;

namespace DTXMania;

public class SongSelectionContainer : UIDrawable
{
    private SongDb.SongDb songDb;
    private UIImage albumArt;
    private SongSelectionElement[] songSelectionElements = new SongSelectionElement[20];
    private SongNode currentRoot;

    private int currentSongIndex;
    
    private DTXTexture fallbackPreImage = new(CSkin.Path(@"Graphics\5_preimage default.png"));
    
    public SongSelectionContainer(SongDb.SongDb songDb, UIImage albumArt)
    {
        this.songDb = songDb;
        this.albumArt = albumArt;
        
        currentRoot = songDb.songNodeRoot;
        dontSerialize = true;
        
        for (int i = 0; i < songSelectionElements.Length; i++)
        {
            songSelectionElements[i] = SongSelectionElement.Create();
        }
    }

    private const int selectionIndex = 4; //the center element is the 6th element in the array (0-based index)
    private SongNode currentSelection => songSelectionElements[selectionIndex].node;
    
    public void UpdateRoot(SongNode? newRoot = null)
    {
        currentRoot = newRoot ?? songDb.songNodeRoot;
        
        SongNode firstNode = currentRoot.childNodes.First();

        //remove cache
        foreach (KeyValuePair<SongNode, DTXTexture> element in preImageCache)
        {
            RemoveFromCache(element.Key);
        }
        
        //assign first node to the first element, then use rNextSong to fill the rest
        songSelectionElements[selectionIndex].UpdateSongNode(firstNode, CachePreImage(firstNode));
        
        //fill the rest of the elements with next songs
        for (int i = selectionIndex + 1; i < songSelectionElements.Length; i++)
        {
            SongNode nextNode = SongNode.rNextSong(songSelectionElements[i - 1].node);
            var tex = CachePreImage(nextNode);
            songSelectionElements[i].UpdateSongNode(nextNode, tex);
        }
        
        //fill the first elements with previous songs
        for (int i = selectionIndex - 1; i >= 0; i--)
        {
            SongNode prevNode = SongNode.rPreviousSong(songSelectionElements[i + 1].node);
            var tex = CachePreImage(prevNode);
            songSelectionElements[i].UpdateSongNode(prevNode, tex);
        }
        
        //update album art
        UpdateAlbumArt();
    }

    private void UpdateAlbumArt()
    {
        var tex = preImageCache[currentSelection];
        albumArt.SetTexture(tex, false);

        if (tex != null)
        {
            albumArt.clipRect = new RectangleF(0, 0, tex.Width, tex.Height);
        }
    }

    public override void Draw(Matrix parentMatrix)
    {
        //calculate object matrix for container
        UpdateLocalTransformMatrix();
        Matrix combinedMatrix = localTransformMatrix * parentMatrix;
        
        for (int i = 0; i < songSelectionElements.Length; i++)
        {
            SongSelectionElement element = songSelectionElements[i];
            element.position = new Vector3(i == selectionIndex ? -20 : 0, i * 85, 0);
            element.Draw(combinedMatrix);
        }
    }

    public void HandleNavigation()
    {
        //ctKeyRepeat.Up.tRepeatKey(CDTXMania.InputManager.Keyboard.bKeyPressing(Key.UpArrow), new CCounter.DGキー処理(MoveUp));
        //ctKeyRepeat.R.tRepeatKey(CDTXMania.Pad.bPressingGB(EPad.R), new CCounter.DGキー処理(MoveUp));
        if (CDTXMania.InputManager.Keyboard.bKeyPressed(Key.UpArrow)
            || CDTXMania.Pad.bPressedGB(EPad.R)
            || CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.HT))
        {
            MoveUp();
        }
        //ctKeyRepeat.Down.tRepeatKey(CDTXMania.InputManager.Keyboard.bKeyPressing(Key.DownArrow), new CCounter.DGキー処理(MoveDown));
        //ctKeyRepeat.B.tRepeatKey(CDTXMania.Pad.bPressingGB(EPad.G), new CCounter.DGキー処理(MoveDown));
        if (CDTXMania.InputManager.Keyboard.bKeyPressed(Key.DownArrow)
            || CDTXMania.Pad.bPressedGB(EPad.G)
            || CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.LT))
        {
            MoveDown();
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
    
    Dictionary<SongNode, DTXTexture> preImageCache = new();

    private DTXTexture CachePreImage(SongNode node)
    {
        if (!preImageCache.TryGetValue(node, out DTXTexture? preImage))
        {
            string imagePath = node.GetImagePath();
            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                preImage = new DTXTexture(imagePath);
            }
            else
            {
                preImage = fallbackPreImage;
            }
        }
        
        preImageCache[node] = preImage;
        return preImage;
    }

    private void RemoveFromCache(SongNode node)
    {
        if (preImageCache.TryGetValue(node, out DTXTexture? tex))
        {
            tex.Dispose();
            preImageCache.Remove(node);
        }
    }
    
    private void MoveUp()
    {
        CDTXMania.Skin.soundCursorMovement.tPlay();
        
        //add new image to cache
        CachePreImage(SongNode.rPreviousSong(songSelectionElements.First().node));

        if (currentRoot.childNodes.Count > songSelectionElements.Length)
        {
            //remove cache for last element
            RemoveFromCache(songSelectionElements.Last().node);
        }

        foreach (SongSelectionElement element in songSelectionElements)
        {
            var node = SongNode.rPreviousSong(element.node);
            element.UpdateSongNode(node, preImageCache[node]);
        }
        
        UpdateAlbumArt();
    }

    private void MoveDown()
    {
        CDTXMania.Skin.soundCursorMovement.tPlay();

        if (currentRoot.childNodes.Count > songSelectionElements.Length)
        {
            //remove for first element
            RemoveFromCache(songSelectionElements.First().node);
        }

        //add new image to cache
        CachePreImage(SongNode.rNextSong(songSelectionElements.Last().node));
        
        foreach (SongSelectionElement element in songSelectionElements)
        {
            var node = SongNode.rNextSong(element.node);
            element.UpdateSongNode(node, preImageCache[node]);
        }
        
        UpdateAlbumArt();
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

}