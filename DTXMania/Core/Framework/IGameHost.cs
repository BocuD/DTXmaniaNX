using System.Numerics;

namespace DTXMania.Core.Framework;

public interface IGameHost
{
    public FullscreenMode fullscreenMode { get; }
    public void RequestVsync(bool enabled);
    public void RequestFullscreenMode(FullscreenMode fullscreenMode);
    public IRenderer Renderer { get; }
    public void InitializeGraphics();
    
    IntPtr GetWindowHandle();
    void SetWindowTitle(string newTitle);
    void FocusWindow();
    void SetCursorVisible(bool visible);

    bool IsWindowFocused { get; }
    void SetWindowSize(Vector2 value);
    void SetWindowPosition(Vector2 value);
    string GetClipboardText();
    void SetClipboardText(string value);
    public RuntimeLogListener RuntimeLogListener { get; }
}