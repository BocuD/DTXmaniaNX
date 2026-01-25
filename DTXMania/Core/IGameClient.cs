using System;
using System.Windows.Forms;

namespace DTXMania.Core;

internal interface IGameClient : IDisposable
{
    void Initialize();
    void LoadContent();
    void UnloadContent();
    void Draw();
    void OnExiting();
    void OnDeviceLost();
    void OnDeviceReset();
    void OnActivated();
    void OnDeactivated();
    void OnResizeEnd();
    void ToggleFullscreen();
    void OnKeyDown(Keys keyCode, bool isAlt);
    void OnKeyUp(Keys keyCode);
    void OnKeyChar(char keyChar);
    void OnMouseMove(int x, int y);
    void OnMouseDown(MouseButtons button, int x, int y);
    void OnMouseUp(MouseButtons button, int x, int y);
    void OnMouseWheel(int delta);
    void OnMouseDoubleClick(MouseButtons button, int x, int y);
}
