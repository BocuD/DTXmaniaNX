using System.Drawing;
using System.Numerics;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using Silk.NET.OpenGL;

namespace DTXMania.UI.OpenGL;

public sealed unsafe class OpenGlRenderer : IRenderer, IDisposable
{
    public static OpenGlRenderer? Instance { get; set; }
    
    public override string name => "OpenGL";

    private GL? _gl;
    private bool _sharedResourcesCreated;
    private bool _contextResourcesCreated;
    private uint _program;
    private uint _ebo;
    private uint _vao;

    private const int VboRingCount = 3;
    private uint[] _vboRing = Array.Empty<uint>();
    private int _vboRingIndex;
    private int _projectionLocation;
    private Matrix4x4 _projection = Matrix4x4.Identity;

    /// <summary>Actual glDrawElements calls issued last frame (batch flushes).</summary>
    public override int lastFrameDrawCalls => _lastFrameDrawCalls;

    /// <summary>Quads submitted last frame (before batching).</summary>
    public int lastFrameQuads => _lastFrameQuads;

    private int drawCalls;
    private int _lastFrameDrawCalls;
    private int quadCount;
    private int _lastFrameQuads;

    //per-frame GL state cache
    private bool _frameStateSet;
    private BlendMode? _activeBlendMode;
    private uint _boundTexture;

    //sprite batch: quads sharing texture + blend mode accumulate here (vertices already
    //transformed to screen space, color baked per vertex) and are drawn in one glDrawElements
    //when the batch breaks (texture/blend change, capacity, texture update/delete, readback,
    //or the explicit end-of-render Flush).
    private const int MaxBatchQuads = 2048;
    private const int FloatsPerVertex = 9; //pos(3) + uv(2) + color(4)
    private const int FloatsPerQuad = FloatsPerVertex * 4;
    private readonly float[] _batchVertices = new float[MaxBatchQuads * FloatsPerQuad];
    private int _batchQuadCount;
    private uint _batchTexture;
    private BlendMode _batchBlendMode;

    // PBO ring for stall-free streaming texture uploads (used by video). Uploading a
    // texture from a client pointer forces the driver to serialize with the GPU (a CPU
    // stall until the command queue drains). Streaming through an orphaned Pixel Buffer
    // Object instead turns the upload into a GPU-scheduled async DMA, so the CPU no
    // longer waits. Falls back to the direct path if PBOs misbehave on the driver.
    private const int StreamPboCount = 3;
    private uint[] _streamPbos = Array.Empty<uint>();
    private int _streamPboIndex;
    private int _streamPboSize;
    private bool _streamPboUnavailable;

    //events to notify external observers (e.g. an inspector) about texture lifecycle
    internal event Action<uint, int, int>? TextureCreated;
    internal event Action<uint>? TextureDeleted;
    internal event Action? RendererDisposed;

    //internal tracking of textures so external inspectors can query existing textures
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
        InvalidateStateCache();
        _batchQuadCount = 0;
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

        _lastFrameDrawCalls = drawCalls;
        drawCalls = 0;
        _lastFrameQuads = quadCount;
        quadCount = 0;

        //a batch pending across frames would target the wrong frame's render target; drop it
        _batchQuadCount = 0;

        InvalidateStateCache();

        //the 2D pipeline leaves the depth mask off for the whole frame; restore it here so the
        //depth clear at the start of the frame (DTXManiaGL.Render) still works.
        _gl?.DepthMask(true);
    }

    /// <summary>
    /// Forgets all cached GL state so the next DrawQuad re-applies the full 2D pipeline state.
    /// Call after running arbitrary GL code (custom shaders, FBO work) between game draws.
    /// </summary>
    public void InvalidateStateCache()
    {
        _frameStateSet = false;
        _activeBlendMode = null;
        _boundTexture = uint.MaxValue;
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

        DeleteStreamPbos();

        _contextResourcesCreated = false;
    }

    public void DrawTexture(uint textureId, float textureWidth, float textureHeight, Matrix4x4 transformMatrix, Vector2 size, RectangleF clipRect, Color4 color, BlendMode blendMode)
    {
        DrawQuad(textureId, textureWidth, textureHeight, transformMatrix, size, clipRect, color, blendMode);
    }
    
    //the up-to-9 quads share texture and blend mode, so the batcher folds them into one draw call
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

        Span<float> destX = [0f, leftBorder, leftBorder + destCenterWidth, size.X];
        Span<float> destY = [0f, topBorder, topBorder + destCenterHeight, size.Y];
        Span<float> srcX =
        [
            clipRect.Left,
            clipRect.Left + sliceRect.X,
            clipRect.Left + sliceRect.X + sourceCenterWidth,
            clipRect.Right
        ];
        Span<float> srcY =
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

        if (_vboRing.Length > 0)
        {
            foreach (uint vbo in _vboRing)
            {
                if (vbo != 0) _gl.DeleteBuffer(vbo);
            }
            _vboRing = Array.Empty<uint>();
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
            //pending quads referencing this texture must be drawn before it disappears
            if (_batchQuadCount > 0 && _batchTexture == textureId)
            {
                Flush();
            }

            //texture ids get recycled by the driver, so a stale cache entry could suppress a
            //required re-bind later in the frame
            if (_boundTexture == textureId)
            {
                _boundTexture = uint.MaxValue;
            }

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
        _boundTexture = 0;
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
        _boundTexture = 0;
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

        //pending quads referencing this texture were submitted against its current content;
        //draw them before overwriting it
        if (_batchQuadCount > 0 && _batchTexture == textureId)
        {
            Flush();
        }

        _gl.BindTexture(TextureTarget.Texture2D, textureId);
        _gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

        fixed (byte* pixels = rgbaPixels)
        {
            _gl.TexSubImage2D(TextureTarget.Texture2D, 0, dstX, dstY, (uint)width, (uint)height, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
        }

        _gl.BindTexture(TextureTarget.Texture2D, 0);
        _boundTexture = 0;
    }

    /// <summary>
    /// Uploads pixels into <paramref name="textureId"/> via an orphaned Pixel Buffer Object so the
    /// transfer is a GPU-scheduled DMA rather than a synchronous client-pointer copy. This avoids the
    /// implicit CPU/GPU synchronization that a plain <see cref="UpdateTexture"/> triggers on frames
    /// where the texture is still referenced by in-flight draw commands. Falls back to the direct path
    /// if the driver cannot map a PBO.
    /// </summary>
    internal void UpdateTextureStreaming(uint textureId, ReadOnlySpan<byte> rgbaPixels, int width, int height)
    {
        ValidateTextureSize(width, height);

        int size = width * height * 4;
        if (rgbaPixels.Length < size)
        {
            throw new ArgumentException("RGBA buffer is smaller than width*height*4 bytes.", nameof(rgbaPixels));
        }

        if (_gl == null)
        {
            throw new InvalidOperationException("OpenGL UI renderer has no active GL context.");
        }

        // Driver already told us PBO streaming doesn't work here: use the direct path.
        if (_streamPboUnavailable)
        {
            UpdateTexture(textureId, rgbaPixels, width, height);
            return;
        }

        //pending quads referencing this texture were submitted against its current content;
        //draw them before overwriting it
        if (_batchQuadCount > 0 && _batchTexture == textureId)
        {
            Flush();
        }

        try
        {
            EnsureStreamPbos(size);

            _streamPboIndex = (_streamPboIndex + 1) % StreamPboCount;
            uint pbo = _streamPbos[_streamPboIndex];

            _gl.BindBuffer(BufferTargetARB.PixelUnpackBuffer, pbo);

            // InvalidateBuffer orphans the store, so the driver can hand us fresh memory without
            // waiting for the previous DMA out of this PBO to finish.
            void* mapped = _gl.MapBufferRange(BufferTargetARB.PixelUnpackBuffer, 0, (nuint)size,
                MapBufferAccessMask.WriteBit | MapBufferAccessMask.InvalidateBufferBit);

            if (mapped == null)
            {
                _gl.BindBuffer(BufferTargetARB.PixelUnpackBuffer, 0);
                UpdateTexture(textureId, rgbaPixels, width, height);
                return;
            }

            rgbaPixels.Slice(0, size).CopyTo(new Span<byte>(mapped, size));
            _gl.UnmapBuffer(BufferTargetARB.PixelUnpackBuffer);

            _gl.BindTexture(TextureTarget.Texture2D, textureId);
            _gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

            // With a PBO bound, the final arg is a byte OFFSET into that buffer, not a client
            // pointer — the driver schedules the copy on the GPU timeline and returns immediately.
            _gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, (uint)width, (uint)height,
                PixelFormat.Rgba, PixelType.UnsignedByte, (void*)0);

            _gl.BindTexture(TextureTarget.Texture2D, 0);
            // Must unbind the PBO, or a later client-pointer upload would be misread as an offset.
            _gl.BindBuffer(BufferTargetARB.PixelUnpackBuffer, 0);
            _boundTexture = 0;
        }
        catch (Exception e)
        {
            System.Diagnostics.Trace.TraceWarning(
                $"PBO streaming upload failed ({e.Message}); disabling it and falling back to direct upload.");
            _streamPboUnavailable = true;
            DeleteStreamPbos();
            _gl.BindBuffer(BufferTargetARB.PixelUnpackBuffer, 0);
            UpdateTexture(textureId, rgbaPixels, width, height);
        }
    }

    private void EnsureStreamPbos(int size)
    {
        if (_streamPbos.Length == StreamPboCount && _streamPboSize == size)
        {
            return;
        }

        DeleteStreamPbos();

        _streamPbos = new uint[StreamPboCount];
        for (int i = 0; i < StreamPboCount; i++)
        {
            uint pbo = _gl!.GenBuffer();
            _gl.BindBuffer(BufferTargetARB.PixelUnpackBuffer, pbo);
            _gl.BufferData(BufferTargetARB.PixelUnpackBuffer, (nuint)size, (void*)null, BufferUsageARB.StreamDraw);
            _streamPbos[i] = pbo;
        }
        _gl.BindBuffer(BufferTargetARB.PixelUnpackBuffer, 0);

        _streamPboSize = size;
        _streamPboIndex = 0;
    }

    private void DeleteStreamPbos()
    {
        if (_gl != null)
        {
            foreach (uint pbo in _streamPbos)
            {
                if (pbo != 0) _gl.DeleteBuffer(pbo);
            }
        }

        _streamPbos = Array.Empty<uint>();
        _streamPboSize = 0;
        _streamPboIndex = 0;
    }

    private void DrawQuad(uint textureId, float textureWidth, float textureHeight, Matrix4x4 transformMatrix, Vector2 size, RectangleF clipRect, Color4 color, BlendMode blendMode)
    {
        if (_gl == null || !_sharedResourcesCreated || !_contextResourcesCreated)
        {
            throw new InvalidOperationException("OpenGL UI renderer is not initialized.");
        }

        quadCount++;

        if (_batchQuadCount > 0 && (textureId != _batchTexture || blendMode != _batchBlendMode || _batchQuadCount >= MaxBatchQuads))
        {
            Flush();
        }

        if (_batchQuadCount == 0)
        {
            _batchTexture = textureId;
            _batchBlendMode = blendMode;
        }

        float u0 = clipRect.Left / textureWidth;
        float v0 = clipRect.Top / textureHeight;
        float u1 = clipRect.Right / textureWidth;
        float v1 = clipRect.Bottom / textureHeight;

        //transform on the CPU (row-vector convention, v * M — matches what the shader computed
        //with the per-quad uTransform uniform before batching)
        Vector4 p0 = Vector4.Transform(new Vector4(0f, 0f, 0f, 1f), transformMatrix);
        Vector4 p1 = Vector4.Transform(new Vector4(size.X, 0f, 0f, 1f), transformMatrix);
        Vector4 p2 = Vector4.Transform(new Vector4(size.X, size.Y, 0f, 1f), transformMatrix);
        Vector4 p3 = Vector4.Transform(new Vector4(0f, size.Y, 0f, 1f), transformMatrix);
        Vector4 c = color.ToVector4();

        float[] v = _batchVertices;
        int o = _batchQuadCount * FloatsPerQuad;
        WriteVertex(v, o, p0, u0, v0, c);
        WriteVertex(v, o + FloatsPerVertex, p1, u1, v0, c);
        WriteVertex(v, o + FloatsPerVertex * 2, p2, u1, v1, c);
        WriteVertex(v, o + FloatsPerVertex * 3, p3, u0, v1, c);
        _batchQuadCount++;
    }

    private static void WriteVertex(float[] buffer, int offset, Vector4 position, float u, float texV, Vector4 color)
    {
        buffer[offset] = position.X;
        buffer[offset + 1] = position.Y;
        buffer[offset + 2] = position.Z;
        buffer[offset + 3] = u;
        buffer[offset + 4] = texV;
        buffer[offset + 5] = color.X;
        buffer[offset + 6] = color.Y;
        buffer[offset + 7] = color.Z;
        buffer[offset + 8] = color.W;
    }

    /// <summary>
    /// Draws all pending batched quads. Called automatically when the batch breaks and once at
    /// the end of the game render (GlfwOpenGlHost); must also run before any framebuffer
    /// readback or render-target switch that expects submitted draws to be visible.
    /// </summary>
    public void Flush()
    {
        if (_batchQuadCount == 0 || _gl == null)
        {
            return;
        }

        if (!_frameStateSet)
        {
            ApplyFrameState();
        }

        if (_activeBlendMode != _batchBlendMode)
        {
            _gl.BlendFunc(GLEnum.SrcAlpha, _batchBlendMode == BlendMode.Additive ? GLEnum.One : GLEnum.OneMinusSrcAlpha);
            _activeBlendMode = _batchBlendMode;
        }

        if (_boundTexture != _batchTexture)
        {
            _gl.BindTexture(TextureTarget.Texture2D, _batchTexture);
            _boundTexture = _batchTexture;
        }

        //orphaning BufferData (rather than BufferSubData) so the driver never has to sync with
        //draws still reading the previous batch's vertices. We also rotate through a small ring
        //of buffers, so even drivers that don't rename an orphaned buffer well never stall: the
        //buffer we upload into hasn't been touched by the GPU for VboRingCount draws.
        _vboRingIndex = (_vboRingIndex + 1) % VboRingCount;
        uint vbo = _vboRing[_vboRingIndex];
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);

        fixed (float* vertexPtr = _batchVertices)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(_batchQuadCount * FloatsPerQuad * sizeof(float)), vertexPtr, BufferUsageARB.DynamicDraw);
        }

        // Re-point the vertex attributes at the ring buffer we just filled (the VAO records which
        // buffer each attribute reads from). Cheap CPU-only calls; no GPU sync.
        int stride = FloatsPerVertex * sizeof(float);
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, (uint)stride, (void*)0);
        _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, (uint)stride, (void*)(3 * sizeof(float)));
        _gl.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, (uint)stride, (void*)(5 * sizeof(float)));

        _gl.DrawElements(PrimitiveType.Triangles, (uint)(_batchQuadCount * 6), DrawElementsType.UnsignedShort, null);
        drawCalls++;
        _batchQuadCount = 0;
    }

    //Binds the full 2D pipeline state once per frame (first flush); subsequent flushes only
    //touch the vertex buffer and (when changed) the texture / blend func
    private void ApplyFrameState()
    {
        _gl!.Enable(GLEnum.Blend);
        _gl.Disable(GLEnum.DepthTest);
        _gl.DepthMask(false);
        _gl.Disable(GLEnum.CullFace);
        _gl.UseProgram(_program);
        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vboRing[0]);
        _gl.ActiveTexture(TextureUnit.Texture0);

        Matrix4x4 projection = _projection;
        _gl.UniformMatrix4(_projectionLocation, 1, false, (float*)&projection);

        _frameStateSet = true;
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
            layout (location = 2) in vec4 aColor;

            uniform mat4 uProjection;

            out vec2 vTexCoord;
            out vec4 vColor;

            void main()
            {
                vTexCoord = aTexCoord;
                vColor = aColor;
                gl_Position = uProjection * vec4(aPosition, 1.0);
            }
            """;

        const string fragmentShaderSource = """
            #version 330 core
            in vec2 vTexCoord;
            in vec4 vColor;

            uniform sampler2D uTexture0;

            out vec4 fragColor;

            void main()
            {
                fragColor = texture(uTexture0, vTexCoord) * vColor;
            }
            """;

        _program = CreateProgram(vertexShaderSource, fragmentShaderSource);
        _vboRing = new uint[VboRingCount];
        for (int i = 0; i < VboRingCount; i++)
        {
            _vboRing[i] = _gl.GenBuffer();
        }
        _ebo = _gl.GenBuffer();
        _projectionLocation = _gl.GetUniformLocation(_program, "uProjection");

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
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vboRing[0]);
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);

        //index data never changes (two triangles per quad, repeated for every batch slot) so we
        //only need to upload it once. 2048 quads * 4 vertices = 8192 max index, fits ushort.
        ushort[] indices = new ushort[MaxBatchQuads * 6];
        for (int quad = 0; quad < MaxBatchQuads; quad++)
        {
            int vertexBase = quad * 4;
            int indexBase = quad * 6;
            indices[indexBase] = (ushort)vertexBase;
            indices[indexBase + 1] = (ushort)(vertexBase + 1);
            indices[indexBase + 2] = (ushort)(vertexBase + 2);
            indices[indexBase + 3] = (ushort)(vertexBase + 2);
            indices[indexBase + 4] = (ushort)(vertexBase + 3);
            indices[indexBase + 5] = (ushort)vertexBase;
        }

        fixed (ushort* indexPtr = indices)
        {
            _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(ushort)), indexPtr, BufferUsageARB.StaticDraw);
        }

        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, FloatsPerVertex * sizeof(float), (void*)0);
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, FloatsPerVertex * sizeof(float), (void*)(3 * sizeof(float)));
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, FloatsPerVertex * sizeof(float), (void*)(5 * sizeof(float)));
        _gl.EnableVertexAttribArray(2);
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
