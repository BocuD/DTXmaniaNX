using System;
using System.Windows.Forms;

namespace DTXMania.Core;

internal sealed class GameSwitcher : IGameClient
{
    private readonly GameContext context;
    private bool useTriangleTest;
    private IGameClient? current;

    public GameSwitcher(GameContext context, bool startWithTriangleTest)
    {
        this.context = context;
        useTriangleTest = startWithTriangleTest;
    }

    public void Initialize()
    {
        current = CreateGame();
        current.Initialize();
    }

    public void LoadContent()
    {
        current?.LoadContent();
    }

    public void UnloadContent()
    {
        current?.UnloadContent();
    }
    
    public void Draw()
    {
        current?.Draw();
    }

    public void OnExiting()
    {
        current?.OnExiting();
    }

    public void OnDeviceLost()
    {
        current?.OnDeviceLost();
    }

    public void OnDeviceReset()
    {
        current?.OnDeviceReset();
    }

    public void OnActivated()
    {
        current?.OnActivated();
    }

    public void OnDeactivated()
    {
        current?.OnDeactivated();
    }

    public void OnResizeEnd()
    {
        current?.OnResizeEnd();
    }

    public void ToggleFullscreen()
    {
        current?.ToggleFullscreen();
    }

    public void OnKeyDown(Keys keyCode, bool isAlt)
    {
        if (keyCode == Keys.F1)
        {
            SwapGame();
            return;
        }

        current?.OnKeyDown(keyCode, isAlt);
    }

    public void OnKeyUp(Keys keyCode)
    {
        current?.OnKeyUp(keyCode);
    }

    public void OnKeyChar(char keyChar)
    {
        current?.OnKeyChar(keyChar);
    }

    public void OnMouseMove(int x, int y)
    {
        current?.OnMouseMove(x, y);
    }

    public void OnMouseDown(MouseButtons button, int x, int y)
    {
        current?.OnMouseDown(button, x, y);
    }

    public void OnMouseUp(MouseButtons button, int x, int y)
    {
        current?.OnMouseUp(button, x, y);
    }

    public void OnMouseWheel(int delta)
    {
        current?.OnMouseWheel(delta);
    }

    public void OnMouseDoubleClick(MouseButtons button, int x, int y)
    {
        current?.OnMouseDoubleClick(button, x, y);
    }

    public void Dispose()
    {
        current?.Dispose();
    }

    private IGameClient CreateGame()
    {
        return useTriangleTest ? new TriangleTest(context) : new CDTXMania(context);
    }

    private void SwapGame()
    {
        if (current != null)
        {
            current.UnloadContent();
            current.Dispose();
        }

        useTriangleTest = !useTriangleTest;
        current = CreateGame();
        current.Initialize();
        current.LoadContent();
    }
}
