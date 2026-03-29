using OpenGLTest;

namespace DTXMania.Core.Framework;

public interface IGameHost
{
    public FullscreenMode fullscreenMode { get; }
    public void RequestVsync(bool enabled);
    public void RequestFullscreenMode(FullscreenMode fullscreenMode);
    
    IntPtr GetWindowHandle();
}