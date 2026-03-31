using System.Drawing;
using System.Numerics;
using DTXMania.UI;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Drawable;

public abstract class UITexture : UIDrawable
{
    public Color4 color = Color4.White;
    protected BaseTexture texture = BaseTexture.None;

    protected UITexture(BaseTexture texture)
    {
        SetTexture(texture);
    }

    public BaseTexture Texture => texture;

    public void SetTexture(BaseTexture t, bool updateSize = true)
    {
        if (t.isValid())
        {
            texture = t;

            if (updateSize)
            {
                size = new Vector2(t.Width, t.Height);
            }
        }
        else
        {
            texture = BaseTexture.None;
            size = Vector2.Zero;
        }
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        if (!isVisible)
        {
            return;
        }

        UpdateLocalTransformMatrix();
        Matrix4x4 combinedMatrix = localTransformMatrix * parentMatrix;
        texture.tDraw2DMatrix(combinedMatrix, size, new RectangleF(0, 0, texture.Width, texture.Height), color);
    }

    public override void Dispose()
    {
        base.Dispose();
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

        if (!ImGui.CollapsingHeader("Texture"))
        {
            return;
        }

        if (!texture.isValid())
        {
            ImGui.Text("No texture");
            return;
        }

        ImGui.Text($"Name: {texture.name}");
        ImGui.Text($"Width: {texture.Width}");
        ImGui.Text($"Height: {texture.Height}");

        float windowWidth = ImGui.GetWindowWidth();
        float textureWidth = MathF.Min(windowWidth - 64f, texture.Width * 3f);
        float textureHeight = texture.Height * (textureWidth / MathF.Max(texture.Width, 1f));

        ImGui.Dummy(new Vector2(textureWidth, textureHeight));

        Vector2 pMin = ImGui.GetItemRectMin();
        Vector2 pMax = ImGui.GetItemRectMax();
        ImTextureID? textureId = texture.GetImTextureID();

        if (textureId is { } id)
        {
            unsafe
            {
                ImTextureRef textureRef = new((ImTextureData*)null, id);
                ImGui.GetWindowDrawList().AddImage(textureRef, pMin, pMax);
            }

            ImGui.GetWindowDrawList().AddRect(pMin, pMax, 0xFF00FF00, 0, 0, 2);
        }
    }
}
