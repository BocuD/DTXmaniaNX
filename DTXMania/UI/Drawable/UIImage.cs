using System;
using System.Linq;
using Hexa.NET.ImGui;
using SharpDX;

namespace DTXUIRenderer;

public class UIImage : UITexture
{
    public RectangleF clipRect;
    public RectangleF sliceRect;
    
    public ERenderMode renderMode = ERenderMode.Stretched;

    [AddChildMenu]
    public UIImage() : base(BaseTexture.None)
    {
    }
    
    public UIImage(BaseTexture texture) : base(texture)
    {
        if (texture.isValid())
        {
            clipRect = new RectangleF(0, 0, texture.Width, texture.Height);
        }
    }

    public override void Draw(Matrix parentMatrix)
    {
        if (!isVisible) return;
        if (!texture.isValid()) return;
        
        UpdateLocalTransformMatrix();
            
        Matrix combinedMatrix = localTransformMatrix * parentMatrix;

        switch (renderMode)
        {
            case ERenderMode.Stretched:
                texture.tDraw2DMatrix(combinedMatrix, size, clipRect);
                break;
                
            case ERenderMode.Sliced:
                texture.tDraw2DMatrixSliced(combinedMatrix, size, clipRect, sliceRect);
                break;
        }
    }

    public void SetTexture(BaseTexture newTexture, bool updateRects = true)
    {
        texture = newTexture;
            
        if (!texture.isValid()) return;

        if (updateRects)
        {
            size = new Vector2(texture.Width, texture.Height);
            clipRect = new RectangleF(0, 0, texture.Width, texture.Height);
        }
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (ImGui.CollapsingHeader("Image"))
        {
            int rm = (int)renderMode;
            string options = Enum.GetNames(typeof(ERenderMode)).Aggregate((a, b) => $"{a}\0{b}");
            ImGui.Combo("Render Mode", ref rm, options);
            renderMode = (ERenderMode)rm;

            Inspector.Inspect("Clip Rect", ref clipRect);
            Inspector.Inspect("Slice Rect", ref sliceRect);
        }
    }
}
    
public enum ERenderMode
{
    Stretched,
    Sliced
}