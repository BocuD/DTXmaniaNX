using Hexa.NET.ImGui;
using SharpDX;

namespace DTXUIRenderer;

public abstract class UITexture : UIDrawable
{
    protected UITexture(BaseTexture texture)
    {
        if (texture.isValid())
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
    protected BaseTexture texture = BaseTexture.None;
    
    public void SetTexture(BaseTexture t)
    {
        if (t.isValid())
        {
            this.texture = t;
            size = new Vector2(t.Width, t.Height);
        }
        else
        {
            this.texture = BaseTexture.None;
            size = new Vector2(0, 0);
        }
    }
        
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
            if (texture.isValid())
            {
                ImGui.Text($"Name: {texture.name}");
                ImGui.Text($"Width: {texture.Width}");
                ImGui.Text($"Height: {texture.Height}");

                ImTextureID? tex = texture.GetImTextureID();
                if (tex != null)
                {
                    float windowWidth = ImGui.GetWindowWidth();
                    float textureWidth = windowWidth - 64;
                    
                    if (textureWidth > texture.Width * 3)
                    {
                        textureWidth = texture.Width * 3;
                    }
                    
                    float textureHeight = texture.Height * (textureWidth / texture.Width);
                    ImGui.Image(tex.Value, new System.Numerics.Vector2(textureWidth, textureHeight));
                }
            }
            else
            {
                ImGui.Text("No texture");
            }
        }
    }
}