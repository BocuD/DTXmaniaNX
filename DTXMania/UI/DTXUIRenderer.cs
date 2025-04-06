using DTXMania.Core;
using DTXMania.UI.Drawable;
using DTXUIRenderer;
using FDK;
using Hexa.NET.ImGui;
using SharpDX;

namespace DTXMania.UI;

public class DTXTexture : BaseTexture
{
    private CTexture texture;

    public override float transparency
    {
        get => texture.nTransparency / 255.0f;
        set => texture.nTransparency = (int)(value * 255.0f);
    }

    public override float Width => texture.szTextureSize.Width;
    public override float Height => texture.szTextureSize.Height;
    public override string name => texture.filename;

    public DTXTexture(string texturePath)
    {
        texture = CDTXMania.tGenerateTexture(texturePath);
    }
        
    public DTXTexture(CTexture texture)
    {
        this.texture = texture;
    }

    public override void tDraw2DMatrix(Matrix transformMatrix, Vector2 size, RectangleF clipRect)
    {
        texture.tDraw2DMatrix(CDTXMania.app.Device, transformMatrix, size, clipRect);
    }

    public override void tDraw2DMatrixSliced(Matrix transformMatrix, Vector2 size, RectangleF clipRect, RectangleF sliceRect)
    {
        texture.tDraw2DMatrixSliced(CDTXMania.app.Device, transformMatrix, size, clipRect, sliceRect);
    }

    public override void Dispose()
    {
        CDTXMania.tReleaseTexture(ref texture);
        texture = null;
    }

    public override bool isValid()
    {
        return texture != null;
    }

    public override ImTextureID? GetImTextureID()
    {
        if (texture == null || texture.texture == null) return null;
        return new ImTextureID(texture.texture.NativePointer);
    }
}