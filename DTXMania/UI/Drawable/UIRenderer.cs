using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Text;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Drawable;

public enum BlendMode
{
    Alpha,
    Additive
}

public abstract class BaseTexture : IDisposable
{
    public static BaseTexture None { get; } = new NoneTexture();
    public static BaseTextureFactory Factory { get; set; }
    public static IUiTextRenderer SkiaTextRenderer { get; set; }
    
    public bool notFound = false;

    public abstract int Width { get; }
    public abstract int Height { get; }
    public abstract string name { get; }
    
    public BlendMode blendMode = BlendMode.Alpha;
    
    public bool blackIsTransparency = false;

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

    //stubs for non recreated Device based render code
    private Matrix4x4 scaleMatrix
    {
        get
        {
            float scale = CDTXMania.renderScale;
            return Matrix4x4.CreateScale(scale);
        }
    }
    
    public void tDraw2D(float x, float y)
    {
        Matrix4x4 transformMatrix = Matrix4x4.CreateTranslation(x, y, 0);
        tDraw2DMatrix(transformMatrix * scaleMatrix);
    }
    
    public void tDraw2D(float x, float y, Color4 color)
    {
        Matrix4x4 transformMatrix = Matrix4x4.CreateTranslation(x, y, 0);
        tDraw2DMatrix(transformMatrix * scaleMatrix, new Vector2(Width, Height), new RectangleF(0, 0, Width, Height), color);
    }
    
    public void tDraw2D(float x, float y, RectangleF clipRect)
    {
        Matrix4x4 transformMatrix = Matrix4x4.CreateTranslation(x, y, 0);
        tDraw2DMatrix(transformMatrix * scaleMatrix, new Vector2(clipRect.Width, clipRect.Height), clipRect);
    }
    
    public void tDraw2D(float x, float y, RectangleF clipRect, Color4 color)
    {
        Matrix4x4 transformMatrix = Matrix4x4.CreateTranslation(x, y, 0);
        tDraw2DMatrix(transformMatrix * scaleMatrix, new Vector2(clipRect.Width, clipRect.Height), clipRect, color);
    }
    
    public void tDraw2DMatrix(Matrix4x4 transformMatrix)
    {
        tDraw2DMatrix(transformMatrix, new Vector2(Width, Height), new RectangleF(0, 0, Width, Height), Color4.White);
    }
    
    public void tDraw2DMatrix(Matrix4x4 transformMatrix, Color4 col)
    {
        tDraw2DMatrix(transformMatrix, new Vector2(Width, Height), new RectangleF(0, 0, Width, Height), col);
    }

    public void tDraw2DMatrix(Matrix4x4 transformMatrix, Vector2 size)
    {
        tDraw2DMatrix(transformMatrix, size, new RectangleF(0, 0, Width, Height), Color4.White);
    }
    
    public void tDraw2DMatrix(Matrix4x4 transformMatrix, RectangleF clipRect, Color4 col)
    {
        tDraw2DMatrix(transformMatrix, new Vector2(Width, Height), clipRect, col);
    }
    
    public void tDraw2DMatrix(Matrix4x4 transformMatrix, Vector2 size, RectangleF clipRect)
    {
        tDraw2DMatrix(transformMatrix, size, clipRect, Color4.White);
    }
    
    public abstract void tDraw2DMatrix(Matrix4x4 transformMatrix, Vector2 size, RectangleF clipRect, Color4 color);
    public abstract void tDraw2DMatrixSliced(Matrix4x4 transformMatrix, Vector2 size, RectangleF clipRect, Color4 color, RectangleF sliceRect);

    public void tDraw3D(Matrix4x4 mat)
    {
        tDraw3D(mat, new RectangleF(0, 0, Width, Height), Color4.White);
    }
    
    public void tDraw3D(Matrix4x4 mat, Color4 col)
    {
        tDraw3D(mat, new RectangleF(0, 0, Width, Height), col);
    }
    
    public void tDraw3D(Matrix4x4 transformMatrix, RectangleF clipRect, Color4 color)
    {
        if (clipRect.Width <= 0f || clipRect.Height <= 0f)
        {
            return;
        }

        // Legacy DX9 tDraw3D used centered local vertices with Y-up world space.
        // Convert that space into the current top-left, Y-down 2D draw space.
        Matrix4x4 localToLegacyCentered =
            Matrix4x4.CreateScale(1f, -1f, 1f) *
            Matrix4x4.CreateTranslation(-clipRect.Width / 2f, clipRect.Height / 2f, 0f);

        float renderScale = CDTXMania.renderScale <= 0f ? 1f : CDTXMania.renderScale;
        float renderWidth = SampleFramework.GameWindowSize.Width * renderScale;
        float renderHeight = SampleFramework.GameWindowSize.Height * renderScale;

        Matrix4x4 legacyWorldToScreen =
            Matrix4x4.CreateScale(renderScale, -renderScale, 1f) *
            Matrix4x4.CreateTranslation(renderWidth / 2f, renderHeight / 2f, 0f);

        Matrix4x4 convertedMatrix = localToLegacyCentered * transformMatrix * legacyWorldToScreen;
        tDraw2DMatrix(convertedMatrix, new Vector2(clipRect.Width, clipRect.Height), clipRect, color);
    }

    
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
    public override int Width => 0;
    public override int Height => 0;
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
