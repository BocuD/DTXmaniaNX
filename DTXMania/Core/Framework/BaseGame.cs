using System.Numerics;
using DTXMania.Core.Framework;
using Hexa.NET.GLFW;

namespace OpenGLTest;

public abstract class BaseGame
{
    public IGameHost host { get; internal set; }
    public Vector2 windowSize { get; internal set; }
    public Vector2 windowPosition { get; internal set; }
    
    public void SetWindowTitle(string newTitle) => host.SetWindowTitle(newTitle);
    public void SetWindowPosition(Vector2 newPosition) => host.SetWindowPosition(newPosition);
    public void SetWindowSize(Vector2 newSize) => host.SetWindowSize(newSize);

    public bool isExiting { get; private set; }
    
    public bool isFocused { get; internal set; }
    
    public string name { get; set; }

    public abstract void KeyDown(GlfwKey key, GlfwMod mods);
    public abstract void KeyUp(GlfwKey key, GlfwMod mods);

    public virtual void WindowHandleUpdated(IntPtr newHandle) {}

    public void RequestExit()
    {
        isExiting = true;
    }
}