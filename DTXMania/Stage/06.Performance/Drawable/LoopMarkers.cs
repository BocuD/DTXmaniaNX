using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;

namespace DTXMania;

//the begin and end of the practice loop. The marker art is stretched to the element's width
public class LoopMarkers : UIDrawable
{
    //set by the stage every frame
    public bool showsBegin;
    public bool showsEnd;
    public float beginDistance;
    public float endDistance;
    public bool showsLines;
    //where a chip at distance 0 is centred
    public float judgeLineY;
    public bool scrollsDown;

    public int textX;

    private BaseTexture? marker = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_LoopMarker.png"));

    public LoopMarkers()
    {
        name = "LoopMarkers";
        dontSerialize = true;
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        UpdateLocalTransformMatrix();
        Matrix4x4 origin = localTransformMatrix * parentMatrix;

        if (showsBegin)
        {
            DrawMarker(origin, beginDistance, "Begin loop");
        }

        if (showsEnd)
        {
            DrawMarker(origin, endDistance, "End loop");
        }
    }

    private void DrawMarker(Matrix4x4 origin, float distance, string text)
    {
        float y = scrollsDown ? judgeLineY - distance : judgeLineY + distance;
        float halfHeight = marker != null ? marker.Height / 2f : 0;

        //chips are not drawn this far out either. The text sits above the marker
        if (distance > ChipRenderer.DrawRange || y + halfHeight < 0 || y - 17 > 720)
        {
            return;
        }

        CDTXMania.actDisplayString.tPrint(origin, textX, (int)(y - 17), CCharacterConsole.EFontType.White, text);

        if (showsLines && marker != null)
        {
            marker.tDraw2D(origin, 0, y - halfHeight, new RectangleF(0, 0, marker.Width, marker.Height), Color4.White, new Vector2(size.X, marker.Height));
        }
    }

    public override void Dispose()
    {
        base.Dispose();

        marker?.Dispose();
        marker = null;
    }
}
