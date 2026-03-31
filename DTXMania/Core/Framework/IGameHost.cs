using System.Numerics;
using OpenGLTest;

namespace DTXMania.Core.Framework;

public interface IGameHost
{
    public FullscreenMode fullscreenMode { get; }
    public void RequestVsync(bool enabled);
    public void RequestFullscreenMode(FullscreenMode fullscreenMode);
    public IRenderer Renderer { get; }

    IntPtr GetWindowHandle();
    void SetWindowTitle(string newTitle);
    void SetWindowSize(Vector2 value);
    void SetWindowPosition(Vector2 value);
}