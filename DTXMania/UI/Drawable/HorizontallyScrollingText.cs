using System.Drawing;
using Hexa.NET.ImGui;
using SharpDX;
using RectangleF = SharpDX.RectangleF;

namespace DTXMania.UI.Drawable;

public class HorizontallyScrollingText : UIText
{
    public bool scrollingEnabled;
    public float maximumWidth;

    public HorizontallyScrollingText(FontFamily family, int size) : base(family, size)
    {
        
    }

    public override void Draw(Matrix parentMatrix)
    {
        //scroll text
        if (scrollingEnabled)
        {
            overrideClipRect.X += 0.3f;
            if (overrideClipRect.X > texture.Width * 2)
                overrideClipRect.X = 0;
        }
        else
        {
            overrideClipRect.X = 0;
        }
        
        base.Draw(parentMatrix);
    }

    public override void RenderTexture()
    {
        base.RenderTexture();

        if (size.X > maximumWidth)
        {
            size.X = maximumWidth;
            overrideClipRect = new RectangleF(0, 0, maximumWidth, size.Y);
            customClipRect = true;
        }
        else
        {
            customClipRect = false;
        }
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (ImGui.CollapsingHeader("Horizontally Scrolling Text"))
        {
            ImGui.Checkbox("Scrolling enabled", ref scrollingEnabled);
            ImGui.InputFloat("Maximum Width", ref maximumWidth);
        }
    }
}