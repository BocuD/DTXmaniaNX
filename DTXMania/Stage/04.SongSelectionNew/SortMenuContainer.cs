using System.Diagnostics;
using DTXMania.Core;
using DTXMania.SongDb;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using Hexa.NET.ImGui;
using SharpDX;
using SlimDX.DirectInput;

namespace DTXMania;

public class SortMenuContainer : UIGroup
{
    private UIGroup elementsContainer;

    //ring buffer
    private SortMenuElement[] sortMenuElements;
    private SortMenuElement currentSelection => sortMenuElements[selectionIndex];
    
    private int selectionIndex = 2;
    
    public SortMenuContainer(SongDb.SongDb songDb, SongDbSort[] sorters)
    {
        name = "SortMenuContainer";
        
        size = new Vector2(662, 92);
        anchor = new Vector2(1.0f, 0.0f);
        
        var backgroundImage = AddChild(new UIImage(DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_sortmenu_bg.png"))));
        
        sortMenuElements = new SortMenuElement[sorters.Length];
        
        elementsContainer = AddChild(new UIGroup("Elements"));
        elementsContainer.position = new Vector3(0, 40, 0);
        
        for (int i = 0; i < sortMenuElements.Length; i++)
        {
            sortMenuElements[i] = elementsContainer.AddChild(new SortMenuElement(songDb, sorters[i]));
            sortMenuElements[i].position = new Vector3(i * elementSpacing, 0, 0);
        }
    }

    private float elementSpacing = 90.0f; //spacing between elements
    private long lastDrawTime;
    private float targetX = 0f;
    
    //animation
    private float offsetRange = 90;
    private float offsetDistance = 18;
    public override void Draw(Matrix parentMatrix)
    {
        float delta = (CDTXMania.Timer.nCurrentTime - lastDrawTime) / 1000.0f;
        lastDrawTime = CDTXMania.Timer.nCurrentTime;

        if (Math.Abs(targetX - elementsContainer.position.X) > 0.01f)
        {
            float movementAmount = (targetX - elementsContainer.position.X) * delta * 10.0f;
            
            //clamp
            movementAmount = Math.Clamp(movementAmount, -10.0f, 10.0f);
            elementsContainer.position.X += movementAmount;
        }
        else
        {
            elementsContainer.position.X = targetX; //snap to target if close enough
        }
        
        if (elementsContainer.position.X >= elementSpacing / 2)
        {
            elementsContainer.position.X -= elementSpacing;
            targetX -= elementSpacing;
            MoveLeft();
        }
        else if (elementsContainer.position.X <= -elementSpacing / 2)
        {
            elementsContainer.position.X += elementSpacing;
            targetX += elementSpacing;
            MoveRight();
        }
        
        //animate the selected one slightly downwards
        foreach (var element in sortMenuElements)
        {
            var targetX = elementSpacing * selectionIndex;
            
            //same logic as in SongSelectionContainer
            float distanceTo0 = MathF.Abs(targetX - (element.position.X + elementsContainer.position.X)); //positive only
            float t = Math.Clamp((distanceTo0 - offsetRange) * -1, 0, offsetRange);
            //first subtract offsetRange so the range is now -offsetRange - maxDistance.
            //then invert, so range becomes -maxDistance - offsetRange, then clamp from 0-offsetRange
            t /= offsetRange; //normalize range
            
            //x offset is 30 to the left here
            element.position.Y = t * offsetDistance;
        }
        
        base.Draw(parentMatrix);
    }

    public void HandleNavigation()
    {
        if (CDTXMania.InputManager.Keyboard.bKeyPressed(Key.LeftArrow)
            || CDTXMania.Pad.bPressedGB(EPad.Pick) //??
            || CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.SD))
        {
            targetX += elementSpacing;
        }
        if (CDTXMania.InputManager.Keyboard.bKeyPressed(Key.RightArrow)
            || CDTXMania.Pad.bPressedGB(EPad.Pick) //??
            || CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.FT))
        {
            targetX -= elementSpacing;
        }
    }
    
    private void MoveLeft()
    {
        CDTXMania.Skin.soundCursorMovement.tPlay();

        //move the last element to the front
        var last = sortMenuElements[^1];
        for (int i = sortMenuElements.Length - 1; i > 0; i--)
        {
            sortMenuElements[i] = sortMenuElements[i - 1];
        }
        sortMenuElements[0] = last;

        RecalculateElementPositions();
        ApplySort();
    }

    private void MoveRight()
    {
        CDTXMania.Skin.soundCursorMovement.tPlay();

        //move the first element to the end
        var first = sortMenuElements[0];
        for (int i = 0; i < sortMenuElements.Length - 1; i++)
        {
            sortMenuElements[i] = sortMenuElements[i + 1];
        }
        sortMenuElements[^1] = first;
        
        RecalculateElementPositions();
        ApplySort();
    }

    private bool sortLocked = false;
    private void ApplySort()
    {
        if (sortLocked)
        {
            Trace.TraceWarning("Sort operation skipped as another sort is in progress");
            return;
        }
        
        sortMenuElements[selectionIndex].PlaySound();

        //apply sort
        Task.Run(async () =>
        {
            try
            {
                sortLocked = true;
                SongNode newRoot = await sortMenuElements[selectionIndex].Sort();
                CDTXMania.StageManager.stageSongSelectionNew.RequestUpdateRoot(newRoot);
            }
            catch (Exception e)
            {
                Trace.TraceError("Sorting failed: " + e.Message);
            }
            finally
            {
                sortLocked = false;
            }
        });
    }

    private void RecalculateElementPositions()
    {
        for (int i = 0; i < sortMenuElements.Length; i++)
        {
            sortMenuElements[i].position = new Vector3(i * elementSpacing, 0, 0);
        }
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (ImGui.CollapsingHeader("Element Positioning"))
        {
            ImGui.InputFloat("Element Spacing", ref elementSpacing);
            ImGui.Text("Current Selection Index: " + selectionIndex);
        }
        
        if (ImGui.CollapsingHeader("Animation"))
        {
            ImGui.InputFloat("Offset Range", ref offsetRange);
            ImGui.InputFloat("Offset Distance", ref offsetDistance);
        }
    }
}