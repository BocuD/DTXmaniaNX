using Hexa.NET.ImGui;
using SharpDX;

namespace DTXUIRenderer;

public abstract class UITexture : UIDrawable
{
    protected UITexture(BaseTexture texture)
    {
        if (texture != null)
        {
            this.texture = texture;
            if (!texture.isValid()) return;
            size = new Vector2(texture.Width, texture.Height);
        }
        else
        {
            size = new Vector2(0, 0);
        }
    }
        
    public BaseTexture Texture => texture;
    protected BaseTexture texture;
        
    public override void Draw(Matrix parentMatrix)
    {
        if (!isVisible) return;
            
        UpdateLocalTransformMatrix();
            
        Matrix combinedMatrix = localTransformMatrix * parentMatrix;
        texture.tDraw2DMatrix(combinedMatrix, size, new RectangleF(0, 0, texture.Width, texture.Height));
    }

    public override void Dispose()
    {
        texture.Dispose();
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (ImGui.CollapsingHeader("Texture"))
        {
            if (texture != null && texture.isValid())
            {
                ImGui.Text($"Name: {texture.name}");
                ImGui.Text($"Width: {texture.Width}");
                ImGui.Text($"Height: {texture.Height}");
            }
            else
            {
                ImGui.Text("No texture");
            }
        }
    }
}