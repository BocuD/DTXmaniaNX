using Hexa.NET.ImGui;
using SharpDX;

namespace DTXMania.UI.Drawable;

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
            texture = t;
            size = new Vector2(t.Width, t.Height);
        }
        else
        {
            texture = BaseTexture.None;
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
        base.Dispose();

        if (texture != null)
            texture.Dispose();
    }

    public override void OnDeserialize()
    {
        base.OnDeserialize();
        
        texture = BaseTexture.None;
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

                float windowWidth = ImGui.GetWindowWidth();
                float textureWidth = windowWidth - 64;

                if (textureWidth > texture.Width * 3)
                    textureWidth = texture.Width * 3;

                float textureHeight = texture.Height * (textureWidth / texture.Width);

                ImGui.Dummy(new System.Numerics.Vector2(textureWidth, textureHeight));

                var pMin = ImGui.GetItemRectMin();
                var pMax = ImGui.GetItemRectMax();
                
                var textureId = texture.GetImTextureID();

                if (textureId != null)
                {
                    ImGui.GetWindowDrawList().AddImage(textureId.Value, pMin, pMax);
                    
                    //draw bounds around the texture
                    ImGui.GetWindowDrawList().AddRect(pMin, pMax, 0xFF00FF00, 0, 0, 2);
                }
            }
            else
            {
                ImGui.Text("No texture");
            }
        }
    }
}