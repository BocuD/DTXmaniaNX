using System.Drawing;
using DTXMania.Core;
using DTXMania.UI.Drawable;
using FDK;
using Hexa.NET.ImGui;
using SharpDX;
using Color = SharpDX.Color;
using RectangleF = SharpDX.RectangleF;

namespace DTXMania.UI;

public class DTXTexture : BaseTexture
{
    private CTexture texture;

    public override float Width => texture.szTextureSize.Width;
    public override float Height => texture.szTextureSize.Height;
    public override string name => texture.filename;

    private bool isFallback = false;

    public static DTXTexture LoadFromPath(string texturePath)
    {
        var tex = CDTXMania.tGenerateTexture(texturePath);

        if (tex != null)
        {
            return new DTXTexture(tex);
        }

        return fallback;
    }

    public static DTXTexture fallback;

    public static void UpdateFallback()
    {
        if (fallback != null)
        {
            CDTXMania.tReleaseTexture(ref fallback.texture);
        }

        fallback = new DTXTexture(CDTXMania.FallbackTexture);
        fallback.isFallback = true;
    }

    public DTXTexture(CTexture texture)
    {
        this.texture = texture;
    }

    public override void tDraw2DMatrix(Matrix transformMatrix, Vector2 size, RectangleF clipRect, Color4 color)
    {
        texture.tDraw2DMatrix(CDTXMania.app.Device, transformMatrix, size, clipRect, color);
    }

    public override void tDraw2DMatrixSliced(Matrix transformMatrix, Vector2 size, RectangleF clipRect, Color4 color4, RectangleF sliceRect)
    {
        texture.tDraw2DMatrixSliced(CDTXMania.app.Device, transformMatrix, size, clipRect, color4, sliceRect);
    }

    public override void Dispose()
    {
        if (isFallback) return;
        
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