using System.Drawing;
using System.Numerics;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using Silk.NET.OpenGL;

namespace DTXMania.UI.OpenGL;

public sealed unsafe class OpenGlRenderer : IRenderer, IDisposable
{
    public override string name => "OpenGL";
    
    private readonly uint[] _indices = [0, 1, 2, 2, 3, 0];

    private GL? _gl;
    private bool _sharedResourcesCreated;
    private bool _contextResourcesCreated;
    private uint _program;
    private uint _vbo;
    private uint _ebo;
    private uint _vao;
    private int _projectionLocation;
    private int _transformLocation;
    private int _colorLocation;
    private Matrix4x4 _projection = Matrix4x4.Identity;

    // Events to notify external observers (e.g. an inspector) about texture lifecycle
    internal event Action<uint, int, int>? TextureCreated;
    internal event Action<uint>? TextureDeleted;
    internal event Action? RendererDisposed;

    // Internal tracking of textures so external inspectors can query existing textures
    private readonly Dictionary<uint, (int Width, int Height)> _trackedTextures = new();

    public readonly record struct TextureInfo(uint Id, int Width, int Height);

    public void AttachGraphics(GL gl)
    {
        _gl = gl;

        if (!_sharedResourcesCreated)
        {
            CreateSharedResources();
            _sharedResourcesCreated = true;
        }

        ReleaseContextResources();
        CreateContextResources();
        _contextResourcesCreated = true;
    }

    public void BeginFrame(int viewportWidth, int viewportHeight)
    {
        _projection = Matrix4x4.CreateOrthographicOffCenter(
            0f,
            MathF.Max(viewportWidth, 1),
            MathF.Max(viewportHeight, 1),
            0f,
            -1f,
            1f);
    }

    public void ReleaseContextResources()
    {
        if (!_contextResourcesCreated || _gl == null)
        {
            return;
        }

        if (_vao != 0)
        {
            _gl.DeleteVertexArray(_vao);
            _vao = 0;
        }

        _contextResourcesCreated = false;
    }

    public void DrawTexture(uint textureId, float textureWidth, float textureHeight, Matrix4x4 transformMatrix, Vector2 size, RectangleF clipRect, Color4 color, BlendMode blendMode)
    {
        DrawQuad(textureId, textureWidth, textureHeight, transformMatrix, size, clipRect, color, blendMode);
    }

    public void DrawTextureSliced(uint textureId, float textureWidth, float textureHeight, Matrix4x4 transformMatrix, Vector2 size, RectangleF clipRect, Color4 color, RectangleF sliceRect, BlendMode blendMode)
    {
        float sourceRightBorder = MathF.Max(clipRect.Width - sliceRect.Right, 0f);
        float sourceBottomBorder = MathF.Max(clipRect.Height - sliceRect.Bottom, 0f);

        float leftBorder = MathF.Min(sliceRect.X, size.X);
        float topBorder = MathF.Min(sliceRect.Y, size.Y);
        float rightBorder = MathF.Min(sourceRightBorder, MathF.Max(size.X - leftBorder, 0f));
        float bottomBorder = MathF.Min(sourceBottomBorder, MathF.Max(size.Y - topBorder, 0f));

        float destCenterWidth = MathF.Max(size.X - leftBorder - rightBorder, 0f);
        float destCenterHeight = MathF.Max(size.Y - topBorder - bottomBorder, 0f);
        float sourceCenterWidth = MathF.Max(sliceRect.Width, 0f);
        float sourceCenterHeight = MathF.Max(sliceRect.Height, 0f);

        float[] destX = [0f, leftBorder, leftBorder + destCenterWidth, size.X];
        float[] destY = [0f, topBorder, topBorder + destCenterHeight, size.Y];
        float[] srcX =
        [
            clipRect.Left,
            clipRect.Left + sliceRect.X,
            clipRect.Left + sliceRect.X + sourceCenterWidth,
            clipRect.Right
        ];
        float[] srcY =
        [
            clipRect.Top,
            clipRect.Top + sliceRect.Y,
            clipRect.Top + sliceRect.Y + sourceCenterHeight,
            clipRect.Bottom
        ];

        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                RectangleF region = RectangleF.FromLTRB(srcX[x], srcY[y], srcX[x + 1], srcY[y + 1]);
                Vector2 regionSize = new(destX[x + 1] - destX[x], destY[y + 1] - destY[y]);
                if (regionSize.X <= 0f || regionSize.Y <= 0f || region.Width <= 0f || region.Height <= 0f)
                {
                    continue;
                }

                Matrix4x4 segmentTransform = Matrix4x4.CreateTranslation(destX[x], destY[y], 0f) * transformMatrix;
                DrawQuad(textureId, textureWidth, textureHeight, segmentTransform, regionSize, region, color, blendMode);
            }
        }
    }

    public void Dispose()
    {
        ReleaseContextResources();

        // Notify observers that the renderer is being disposed so they can drop references
        RendererDisposed?.Invoke();

        if (!_sharedResourcesCreated || _gl == null)
        {
            return;
        }

        if (_ebo != 0)
        {
            _gl.DeleteBuffer(_ebo);
            _ebo = 0;
        }

        if (_vbo != 0)
        {
            _gl.DeleteBuffer(_vbo);
            _vbo = 0;
        }

        if (_program != 0)
        {
            _gl.DeleteProgram(_program);
            _program = 0;
        }

        _sharedResourcesCreated = false;

        // Clear tracked textures - they are no longer valid after dispose
        _trackedTextures.Clear();
    }

    internal void DeleteTexture(uint textureId)
    {
        if (_gl != null && textureId != 0)
        {
            _gl.DeleteTexture(textureId);
            // Update internal tracking before notifying observers
            _trackedTextures.Remove(textureId);
            TextureDeleted?.Invoke(textureId);
        }
    }

    internal uint CreateTexture(int width, int height, ReadOnlySpan<byte> rgbaPixels)
    {
        ValidateTextureSize(width, height);
        if (rgbaPixels.Length < width * height * 4)
        {
            throw new ArgumentException("RGBA buffer is smaller than width*height*4 bytes.", nameof(rgbaPixels));
        }

        if (_gl == null)
        {
            throw new InvalidOperationException("OpenGL UI renderer has no active GL context.");
        }

        uint textureId = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, textureId);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);

        fixed (byte* pixels = rgbaPixels)
        {
            _gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba8, (uint)width, (uint)height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
        }

        _gl.BindTexture(TextureTarget.Texture2D, 0);
        // Update internal tracking and notify observers
        _trackedTextures[textureId] = (width, height);
        TextureCreated?.Invoke(textureId, width, height);
        return textureId;
    }

    internal uint CreateTextureEmpty(int width, int height)
    {
        ValidateTextureSize(width, height);

        if (_gl == null)
        {
            throw new InvalidOperationException("OpenGL UI renderer has no active GL context.");
        }

        uint textureId = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, textureId);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);

        _gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba8, (uint)width, (uint)height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);

        _gl.BindTexture(TextureTarget.Texture2D, 0);
        // Update internal tracking and notify observers
        _trackedTextures[textureId] = (width, height);
        TextureCreated?.Invoke(textureId, width, height);
        return textureId;
    }

    // Returns a snapshot of currently tracked textures. Safe to call from any thread; returns a copy.
    internal IReadOnlyCollection<TextureInfo> GetTrackedTextures()
    {
        lock (_trackedTextures)
        {
            return _trackedTextures.Select(kvp => new TextureInfo(kvp.Key, kvp.Value.Width, kvp.Value.Height)).ToArray();
        }
    }

    internal void UpdateTexture(uint textureId, ReadOnlySpan<byte> rgbaPixels, int width, int height, int dstX = 0, int dstY = 0)
    {
        ValidateTextureSize(width, height);
        if (rgbaPixels.Length < width * height * 4)
        {
            throw new ArgumentException("RGBA buffer is smaller than width*height*4 bytes.", nameof(rgbaPixels));
        }

        if (_gl == null)
        {
            throw new InvalidOperationException("OpenGL UI renderer has no active GL context.");
        }

        _gl.BindTexture(TextureTarget.Texture2D, textureId);
        _gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

        fixed (byte* pixels = rgbaPixels)
        {
            _gl.TexSubImage2D(TextureTarget.Texture2D, 0, dstX, dstY, (uint)width, (uint)height, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
        }

        _gl.BindTexture(TextureTarget.Texture2D, 0);
    }
    
    private void DrawQuad(uint textureId, float textureWidth, float textureHeight, Matrix4x4 transformMatrix, Vector2 size, RectangleF clipRect, Color4 color, BlendMode blendMode)
    {
        if (_gl == null || !_sharedResourcesCreated || !_contextResourcesCreated)
        {
            throw new InvalidOperationException("OpenGL UI renderer is not initialized.");
        }

        float u0 = clipRect.Left / textureWidth;
        float v0 = clipRect.Top / textureHeight;
        float u1 = clipRect.Right / textureWidth;
        float v1 = clipRect.Bottom / textureHeight;

        float[] vertices =
        [
            0f, 0f, 0f, u0, v0,
            size.X, 0f, 0f, u1, v0,
            size.X, size.Y, 0f, u1, v1,
            0f, size.Y, 0f, u0, v1
        ];

        switch (blendMode)
        {
            case BlendMode.Additive:
                _gl.Enable(GLEnum.Blend);
                _gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.One);
                break;
            case BlendMode.Alpha:
                _gl.Enable(GLEnum.Blend);
                _gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
                break;
        }
        // _gl.Enable(GLEnum.Blend);
        // _gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
        _gl.Disable(GLEnum.DepthTest);
        _gl.DepthMask(false);
        _gl.Disable(GLEnum.CullFace);
        _gl.UseProgram(_program);
        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

        fixed (float* vertexPtr = vertices)
        fixed (uint* indexPtr = _indices)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), vertexPtr, BufferUsageARB.DynamicDraw);
            _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(_indices.Length * sizeof(uint)), indexPtr, BufferUsageARB.StaticDraw);
        }

        Vector4 colorVector = color.ToVector4();
        float[] projectionValues =
        [
            _projection.M11, _projection.M12, _projection.M13, _projection.M14,
            _projection.M21, _projection.M22, _projection.M23, _projection.M24,
            _projection.M31, _projection.M32, _projection.M33, _projection.M34,
            _projection.M41, _projection.M42, _projection.M43, _projection.M44
        ];
        float[] transformValues =
        [
            transformMatrix.M11, transformMatrix.M12, transformMatrix.M13, transformMatrix.M14,
            transformMatrix.M21, transformMatrix.M22, transformMatrix.M23, transformMatrix.M24,
            transformMatrix.M31, transformMatrix.M32, transformMatrix.M33, transformMatrix.M34,
            transformMatrix.M41, transformMatrix.M42, transformMatrix.M43, transformMatrix.M44
        ];
        float[] colorValues = [colorVector.X, colorVector.Y, colorVector.Z, colorVector.W];

        fixed (float* projectionPtr = projectionValues)
        fixed (float* transformPtr = transformValues)
        fixed (float* colorPtr = colorValues)
        {
            _gl.UniformMatrix4(_projectionLocation, 1, false, projectionPtr);
            _gl.UniformMatrix4(_transformLocation, 1, false, transformPtr);
            _gl.Uniform4(_colorLocation, 1, colorPtr);
        }

        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, textureId);
        _gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null);
        _gl.BindTexture(TextureTarget.Texture2D, 0);
        _gl.DepthMask(true);
        _gl.BindVertexArray(0);
    }

    private void CreateSharedResources()
    {
        if (_gl == null)
        {
            throw new InvalidOperationException("OpenGL UI renderer has no active GL context.");
        }

        const string vertexShaderSource = """
            #version 330 core
            layout (location = 0) in vec3 aPosition;
            layout (location = 1) in vec2 aTexCoord;

            uniform mat4 uProjection;
            uniform mat4 uTransform;

            out vec2 vTexCoord;

            void main()
            {
                vTexCoord = aTexCoord;
                gl_Position = uProjection * uTransform * vec4(aPosition, 1.0);
            }
            """;

        const string fragmentShaderSource = """
            #version 330 core
            in vec2 vTexCoord;

            uniform sampler2D uTexture0;
            uniform vec4 uColor;

            out vec4 fragColor;

            void main()
            {
                fragColor = texture(uTexture0, vTexCoord) * uColor;
            }
            """;

        _program = CreateProgram(vertexShaderSource, fragmentShaderSource);
        _vbo = _gl.GenBuffer();
        _ebo = _gl.GenBuffer();
        _projectionLocation = _gl.GetUniformLocation(_program, "uProjection");
        _transformLocation = _gl.GetUniformLocation(_program, "uTransform");
        _colorLocation = _gl.GetUniformLocation(_program, "uColor");

        _gl.UseProgram(_program);
        int textureLocation = _gl.GetUniformLocation(_program, "uTexture0");
        _gl.Uniform1(textureLocation, 0);
    }

    private void CreateContextResources()
    {
        if (_gl == null)
        {
            throw new InvalidOperationException("OpenGL UI renderer has no active GL context.");
        }

        _vao = _gl.GenVertexArray();
        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)0);
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)(3 * sizeof(float)));
        _gl.EnableVertexAttribArray(1);
        _gl.BindVertexArray(0);
    }

    private uint CreateProgram(string vertexShaderSource, string fragmentShaderSource)
    {
        if (_gl == null)
        {
            throw new InvalidOperationException("OpenGL UI renderer has no active GL context.");
        }

        uint vertexShader = CompileShader(ShaderType.VertexShader, vertexShaderSource);
        uint fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentShaderSource);
        uint program = _gl.CreateProgram();
        _gl.AttachShader(program, vertexShader);
        _gl.AttachShader(program, fragmentShader);
        _gl.LinkProgram(program);

        _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int linkStatus);
        if (linkStatus == 0)
        {
            throw new InvalidOperationException($"UI shader link failed: {_gl.GetProgramInfoLog(program)}");
        }

        _gl.DetachShader(program, vertexShader);
        _gl.DetachShader(program, fragmentShader);
        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);
        return program;
    }

    private uint CompileShader(ShaderType shaderType, string source)
    {
        if (_gl == null)
        {
            throw new InvalidOperationException("OpenGL UI renderer has no active GL context.");
        }

        uint shader = _gl.CreateShader(shaderType);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);

        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out int compileStatus);
        if (compileStatus == 0)
        {
            throw new InvalidOperationException($"UI {shaderType} compilation failed: {_gl.GetShaderInfoLog(shader)}");
        }

        return shader;
    }

    private static void ValidateTextureSize(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Texture width must be greater than zero.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Texture height must be greater than zero.");
        }
    }
}
