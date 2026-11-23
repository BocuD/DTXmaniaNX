using System.Drawing;
using System.Numerics;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using Hexa.NET.ImGui;

namespace DTXMania.UI.OpenGL;

internal sealed class AtlasTexture : BaseTexture
{
    private readonly TextureAtlas atlas;
    private readonly TextureAtlas.Slot slot;
    private bool released;

    public override int Width { get; }
    public override int Height { get; }
    public override string name { get; }

    public AtlasTexture(TextureAtlas atlas, TextureAtlas.Slot slot, int width, int height, string name)
    {
        this.atlas = atlas;
        this.slot = slot;
        Width = width;
        Height = height;
        this.name = name;
    }

    private RectangleF Region => new(slot.regionX, slot.regionY, Width, Height);

    private RectangleF InPage(RectangleF clipRect)
        => new(clipRect.X + slot.regionX, clipRect.Y + slot.regionY, clipRect.Width, clipRect.Height);

    public override void tDraw2DMatrix(Matrix4x4 transformMatrix, Vector2 size, RectangleF clipRect, Color4 color)
    {
        atlas.renderer.DrawTexture(slot.textureId, slot.pageSize, slot.pageSize, transformMatrix, size, InPage(clipRect),
            color, blendMode, Region);
    }

    public override void tDraw2DMatrixSliced(Matrix4x4 transformMatrix, Vector2 size, RectangleF clipRect, Color4 color,
        RectangleF sliceRect)
    {
        atlas.renderer.DrawTextureSliced(slot.textureId, slot.pageSize, slot.pageSize, transformMatrix, size,
            InPage(clipRect), color, sliceRect, blendMode, Region);
    }

    public override void UpdateRgba32(ReadOnlySpan<byte> rgbaPixels, int width, int height, int dstX = 0, int dstY = 0)
    {
        if (released)
        {
            throw new ObjectDisposedException(name, "Cannot update a disposed texture.");
        }

        if (dstX < 0 || dstY < 0 || width <= 0 || height <= 0 || dstX + width > Width || dstY + height > Height)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Update region must stay within the texture bounds.");
        }

        atlas.renderer.UpdateTexture(slot.textureId, rgbaPixels, width, height, slot.regionX + dstX, slot.regionY + dstY);
    }

    public override bool IsValid() => !released;

    public override ImTextureID? GetImTextureID() => released ? null : new ImTextureID((nint)slot.textureId);

    public override void Dispose()
    {
        if (released)
        {
            return;
        }

        released = true;
        atlas.Free(slot);
    }
}
