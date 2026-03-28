using System.Drawing;
using System.Numerics;
using DTXMania.UI;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Drawable;

public abstract class BaseTexture : IDisposable
{
    public static BaseTexture None { get; } = new NoneTexture();
    public static BaseTextureFactory? Factory { get; set; }

    public abstract float Width { get; }
    public abstract float Height { get; }
    public abstract string name { get; }

    public static BaseTexture LoadFromPath(string texturePath)
    {
        return EnsureFactoryConfigured().LoadFromPath(texturePath);
    }

    public static BaseTexture LoadFromMemory(ReadOnlySpan<byte> rgbaPixels, int width, int height, string name = "Texture")
    {
        return EnsureFactoryConfigured().LoadFromMemory(rgbaPixels, width, height, name);
    }

    public static BaseTexture CreateEmpty(int width, int height, string name = "DynamicTexture")
    {
        return EnsureFactoryConfigured().CreateEmpty(width, height, name);
    }

    public abstract void tDraw2DMatrix(Matrix4x4 transformMatrix, Vector2 size, RectangleF clipRect, Color4 color);
    public abstract void tDraw2DMatrixSliced(Matrix4x4 transformMatrix, Vector2 size, RectangleF clipRect, Color4 color, RectangleF sliceRect);

    public virtual void Dispose()
    {
    }

    public virtual void UpdateRgba32(ReadOnlySpan<byte> rgbaPixels, int width, int height, int dstX = 0, int dstY = 0)
    {
        throw new NotSupportedException($"Texture '{name}' does not support CPU-side updates.");
    }

    public abstract bool isValid();

    public abstract ImTextureID? GetImTextureID();

    private static BaseTextureFactory EnsureFactoryConfigured()
    {
        if (Factory == null)
        {
            throw new InvalidOperationException("BaseTexture factory is not configured.");
        }

        return Factory;
    }
}

public sealed class NoneTexture : BaseTexture
{
    public override float Width => 0;
    public override float Height => 0;
    public override string name => "None";

    public override void tDraw2DMatrix(Matrix4x4 transformMatrix, Vector2 size, RectangleF clipRect, Color4 color)
    {
    }

    public override void tDraw2DMatrixSliced(Matrix4x4 transformMatrix, Vector2 size, RectangleF clipRect, Color4 color, RectangleF sliceRect)
    {
    }

    public override bool isValid()
    {
        return false;
    }

    public override ImTextureID? GetImTextureID()
    {
        return null;
    }
}

public abstract class BaseTextureFactory
{
    public abstract BaseTexture LoadFromPath(string texturePath);
    public abstract BaseTexture LoadFromMemory(ReadOnlySpan<byte> rgbaPixels, int width, int height, string name = "Texture");
    public abstract BaseTexture CreateEmpty(int width, int height, string name = "DynamicTexture");
}
