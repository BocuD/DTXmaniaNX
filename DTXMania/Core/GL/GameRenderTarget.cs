using Hexa.NET.ImGui;
using Silk.NET.OpenGL;

namespace OpenGLTest;

internal sealed unsafe class GameRenderTarget : IDisposable
{
    private GL? _gl;
    private bool _sharedResourcesCreated;
    private bool _contextResourcesCreated;
    private uint _framebuffer;
    private uint _colorTexture;
    private uint _depthStencilTexture;

    public int Width { get; private set; }
    public int Height { get; private set; }
    public ImTextureID? TextureId => _colorTexture == 0 ? null : new ImTextureID((nint)_colorTexture);

    public void AttachGraphics(GL gl)
    {
        _gl = gl;

        if (!_sharedResourcesCreated)
        {
            _colorTexture = _gl.GenTexture();
            _depthStencilTexture = _gl.GenTexture();
            _sharedResourcesCreated = true;
        }

        ReleaseContextResources();
        _framebuffer = _gl.GenFramebuffer();

        if (Width > 0 && Height > 0)
        {
            Resize(Width, Height);
        }

        _contextResourcesCreated = true;
    }

    public void Resize(int width, int height)
    {
        if (_gl == null)
        {
            throw new InvalidOperationException("Render target has no active GL context.");
        }

        width = Math.Max(width, 1);
        height = Math.Max(height, 1);

        if (width == Width && height == Height && _framebuffer != 0)
        {
            AttachFramebuffer();
            return;
        }

        Width = width;
        Height = height;

        _gl.BindTexture(TextureTarget.Texture2D, _colorTexture);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
        _gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba8, (uint)Width, (uint)Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);

        _gl.BindTexture(TextureTarget.Texture2D, _depthStencilTexture);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
        _gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Depth24Stencil8, (uint)Width, (uint)Height, 0, PixelFormat.DepthStencil, PixelType.UnsignedInt248, null);
        _gl.BindTexture(TextureTarget.Texture2D, 0);

        AttachFramebuffer();
    }

    public void BindForRendering()
    {
        if (_gl == null || !_contextResourcesCreated)
        {
            throw new InvalidOperationException("Render target is not initialized.");
        }

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
        _gl.Viewport(0, 0, (uint)Width, (uint)Height);
    }

    public void BindDefaultFramebuffer(int width, int height)
    {
        if (_gl == null)
        {
            throw new InvalidOperationException("Render target has no active GL context.");
        }

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        _gl.Viewport(0, 0, (uint)Math.Max(width, 1), (uint)Math.Max(height, 1));
    }

    public void BlitToDefaultFramebuffer(int width, int height)
    {
        if (_gl == null)
        {
            throw new InvalidOperationException("Render target has no active GL context.");
        }

        _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _framebuffer);
        _gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        _gl.BlitFramebuffer(0, 0, Width, Height, 0, 0, Math.Max(width, 1), Math.Max(height, 1), (uint)ClearBufferMask.ColorBufferBit, GLEnum.Linear);
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void ClearDefaultFramebuffer(float r, float g, float b, float a, int width, int height)
    {
        if (_gl == null)
        {
            throw new InvalidOperationException("Render target has no active GL context.");
        }

        BindDefaultFramebuffer(width, height);
        _gl.Disable(GLEnum.DepthTest);
        _gl.ClearColor(r, g, b, a);
        _gl.Clear(ClearBufferMask.ColorBufferBit);
    }

    public void ReleaseContextResources()
    {
        if (_gl != null && _framebuffer != 0)
        {
            _gl.DeleteFramebuffer(_framebuffer);
            _framebuffer = 0;
        }

        _contextResourcesCreated = false;
    }

    public void Dispose()
    {
        ReleaseContextResources();

        if (_gl != null)
        {
            if (_depthStencilTexture != 0)
            {
                _gl.DeleteTexture(_depthStencilTexture);
                _depthStencilTexture = 0;
            }

            if (_colorTexture != 0)
            {
                _gl.DeleteTexture(_colorTexture);
                _colorTexture = 0;
            }
        }

        _sharedResourcesCreated = false;
    }

    private void AttachFramebuffer()
    {
        if (_gl == null || _framebuffer == 0)
        {
            return;
        }

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, GLEnum.ColorAttachment0, TextureTarget.Texture2D, _colorTexture, 0);
        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, GLEnum.DepthStencilAttachment, TextureTarget.Texture2D, _depthStencilTexture, 0);

        var status = _gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != GLEnum.FramebufferComplete)
        {
            throw new InvalidOperationException($"Game framebuffer is incomplete: {status}");
        }

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }
}
