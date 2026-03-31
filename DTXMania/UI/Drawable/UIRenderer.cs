using System.Drawing;
using System.Numerics;
using DTXMania.UI.Text;
using Hexa.NET.ImGui;
using SharpDX.Direct3D9;

namespace DTXMania.UI.Drawable;

public abstract class BaseTexture : IDisposable
{
    public static BaseTexture None { get; } = new NoneTexture();
    public static BaseTextureFactory Factory { get; set; }
    public static IUiTextRenderer SkiaTextRenderer { get; set; }

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

    public void tDraw2D(Device device, int x, int y)
    {
        Matrix4x4 transformMatrix = Matrix4x4.CreateTranslation(x, y, 0);
        tDraw2DMatrix(transformMatrix);
    }
    
    public void tDraw2DMatrix(Matrix4x4 transformMatrix)
    {
        tDraw2DMatrix(transformMatrix, new Vector2(Width, Height), new RectangleF(0, 0, Width, Height), Color4.White);
    }

    public void tDraw2DMatrix(Matrix4x4 transformMatrix, Vector2 size)
    {
        tDraw2DMatrix(transformMatrix, size, new RectangleF(0, 0, Width, Height), Color4.White);
    }
    
    public void tDraw2DMatrix(Matrix4x4 transformMatrix, Vector2 size, RectangleF clipRect)
    {
        tDraw2DMatrix(transformMatrix, size, clipRect, Color4.White);
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
