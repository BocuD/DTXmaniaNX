using System.Drawing;
using System.IO;
using System.Numerics;
using Hexa.NET.ImGui;
using DTXMania.UI.Drawable;
using StbImageSharp;

namespace DTXMania.UI.OpenGL;

internal sealed class OpenGlTexture : BaseTexture
{
    private readonly OpenGlRenderer _renderer;
    private readonly bool _ownsTexture;
    private uint _textureId;

    public override float Width { get; }
    public override float Height { get; }
    public override string name { get; }

    public OpenGlTexture(OpenGlRenderer renderer, uint textureId, int width, int height, string name, bool ownsTexture = true)
    {
        _renderer = renderer;
        _textureId = textureId;
        Width = width;
        Height = height;
        this.name = name;
        _ownsTexture = ownsTexture;
    }

    public static OpenGlTexture CreateFromRgba32(OpenGlRenderer renderer, ReadOnlySpan<byte> rgbaPixels, int width, int height, string name = "OpenGLTexture")
    {
        uint textureId = renderer.CreateTexture(width, height, rgbaPixels);
        return new OpenGlTexture(renderer, textureId, width, height, name);
    }

    public static OpenGlTexture CreateEmpty(OpenGlRenderer renderer, int width, int height, string name = "DynamicTexture")
    {
        uint textureId = renderer.CreateTextureEmpty(width, height);
        return new OpenGlTexture(renderer, textureId, width, height, name);
    }

    public new static OpenGlTexture LoadFromPath(string texturePath)
    {
        if (OpenGlUi.Renderer == null)
        {
            throw new InvalidOperationException("OpenGL UI renderer is not available.");
        }

        byte[] fileBytes = File.ReadAllBytes(texturePath);
        ImageResult image = ImageResult.FromMemory(fileBytes, ColorComponents.RedGreenBlueAlpha);
        return CreateFromRgba32(OpenGlUi.Renderer, image.Data, image.Width, image.Height, Path.GetFileName(texturePath));
    }

    public static OpenGlTexture CreateSolidColor(OpenGlRenderer renderer, Color4 color, string name = "Solid")
    {
        byte[] pixels =
        [
            (byte)(Math.Clamp(color.Red, 0f, 1f) * 255f),
            (byte)(Math.Clamp(color.Green, 0f, 1f) * 255f),
            (byte)(Math.Clamp(color.Blue, 0f, 1f) * 255f),
            (byte)(Math.Clamp(color.Alpha, 0f, 1f) * 255f)
        ];

        return CreateFromRgba32(renderer, pixels, 1, 1, name);
    }

    public override void tDraw2DMatrix(Matrix4x4 transformMatrix, Vector2 size, RectangleF clipRect, Color4 color)
    {
        _renderer.DrawTexture(_textureId, Width, Height, transformMatrix, size, clipRect, color);
    }

    public override void tDraw2DMatrixSliced(Matrix4x4 transformMatrix, Vector2 size, RectangleF clipRect, Color4 color, RectangleF sliceRect)
    {
        _renderer.DrawTextureSliced(_textureId, Width, Height, transformMatrix, size, clipRect, color, sliceRect);
    }

    public override void Dispose()
    {
        if (_ownsTexture && _textureId != 0)
        {
            _renderer.DeleteTexture(_textureId);
            _textureId = 0;
        }
    }

    public override void UpdateRgba32(ReadOnlySpan<byte> rgbaPixels, int width, int height, int dstX = 0, int dstY = 0)
    {
        if (_textureId == 0)
        {
            throw new ObjectDisposedException(name, "Cannot update a disposed texture.");
        }

        if (dstX < 0 || dstY < 0)
        {
            throw new ArgumentOutOfRangeException(dstX < 0 ? nameof(dstX) : nameof(dstY), "Destination offsets must be non-negative.");
        }

        int textureWidth = (int)Width;
        int textureHeight = (int)Height;
        if (width <= 0 || height <= 0 || dstX + width > textureWidth || dstY + height > textureHeight)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Update region must stay within the texture bounds.");
        }

        _renderer.UpdateTexture(_textureId, rgbaPixels, width, height, dstX, dstY);
    }

    public override bool isValid()
    {
        return _textureId != 0;
    }

    public override ImTextureID? GetImTextureID()
    {
        return _textureId == 0 ? null : new ImTextureID((nint)_textureId);
    }
}

internal sealed class OpenGlTextureFactory : BaseTextureFactory
{
    public override BaseTexture LoadFromPath(string texturePath)
    {
        return OpenGlTexture.LoadFromPath(texturePath);
    }

    public override BaseTexture LoadFromMemory(ReadOnlySpan<byte> rgbaPixels, int width, int height, string name = "Texture")
    {
        if (OpenGlUi.Renderer == null)
        {
            throw new InvalidOperationException("OpenGL UI renderer is not available.");
        }

        return OpenGlTexture.CreateFromRgba32(OpenGlUi.Renderer, rgbaPixels, width, height, name);
    }

    public override BaseTexture CreateEmpty(int width, int height, string name = "DynamicTexture")
    {
        if (OpenGlUi.Renderer == null)
        {
            throw new InvalidOperationException("OpenGL UI renderer is not available.");
        }

        return OpenGlTexture.CreateEmpty(OpenGlUi.Renderer, width, height, name);
    }
}
