using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct3D9;
using Rectangle = System.Drawing.Rectangle;

namespace DTXMania.Core;

internal sealed class GameContext : IDisposable
{
    private readonly WndProc wndProc;
    private readonly string className;
    private readonly bool ownsWindowClass;
    private readonly Queue<string> copyDataMessages = new();
    private bool disposed;
    private bool shouldExit;
    private IGameClient? game;
    private bool isFullscreen;
    private RECT windowedRect;
    public IntPtr WindowHandle { get; }
    public Direct3D Direct3D { get; }
    public Device Device { get; }
    private PresentParameters presentParameters;
    public PresentParameters PresentParameters => presentParameters;

    public GameContext(string title, int width, int height)
    {
        wndProc = WindowProc;
        className = "DTXManiaNX.GameWindow";

        var hInstance = GetModuleHandle(null);
        var windowClass = new WndClassEx
        {
            cbSize = (uint)Marshal.SizeOf<WndClassEx>(),
            style = CS_HREDRAW | CS_VREDRAW | CS_DBLCLKS,
            lpfnWndProc = wndProc,
            hInstance = hInstance,
            hCursor = LoadCursor(IntPtr.Zero, (int)IDC_ARROW),
            lpszClassName = className
        };

        ushort atom = RegisterClassEx(ref windowClass);
        int lastError = Marshal.GetLastWin32Error();
        if (atom == 0 && lastError != ERROR_CLASS_ALREADY_EXISTS)
        {
            throw new Win32Exception(lastError);
        }

        ownsWindowClass = atom != 0;

        var rect = new RECT { Left = 0, Top = 0, Right = width, Bottom = height };
        if (!AdjustWindowRect(ref rect, WS_OVERLAPPEDWINDOW, false))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        WindowHandle = CreateWindowEx(
            0,
            className,
            title,
            WS_OVERLAPPEDWINDOW,
            CW_USEDEFAULT,
            CW_USEDEFAULT,
            rect.Right - rect.Left,
            rect.Bottom - rect.Top,
            IntPtr.Zero,
            IntPtr.Zero,
            hInstance,
            IntPtr.Zero);

        if (WindowHandle == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        ShowWindow(WindowHandle, SW_SHOW);
        UpdateWindow(WindowHandle);

        Direct3D = new Direct3D();
        presentParameters = new PresentParameters
        {
            Windowed = true,
            SwapEffect = SwapEffect.Discard,
            BackBufferFormat = Format.X8R8G8B8,
            BackBufferWidth = width,
            BackBufferHeight = height,
            PresentationInterval = PresentInterval.Default,
            EnableAutoDepthStencil = true,
            AutoDepthStencilFormat = Format.D24S8
        };

        Device = new Device(
            Direct3D,
            0,
            DeviceType.Hardware,
            WindowHandle,
            CreateFlags.HardwareVertexProcessing | CreateFlags.Multithreaded,
            presentParameters);
    }

    public void Run(IGameClient game)
    {
        this.game = game;
        game.Initialize();
        game.LoadContent();

        var stopwatch = Stopwatch.StartNew();
        bool deviceLost = false;
        while (!shouldExit && ProcessEvents())
        {
            if (!EnsureDeviceReady(ref deviceLost))
            {
                Thread.Sleep(50);
                continue;
            }

            game.Draw();
            Device.Present();

            if (stopwatch.ElapsedMilliseconds > 0)
            {
                Thread.Sleep(1);
            }
        }

        game.OnExiting();
        game.UnloadContent();
    }

    private bool EnsureDeviceReady(ref bool deviceLost)
    {
        Result result;
        try
        {
            result = Device.TestCooperativeLevel();
        }
        catch (SharpDXException ex)
        {
            result = ex.ResultCode;
        }

        if (result == ResultCode.DeviceLost)
        {
            if (!deviceLost)
            {
                deviceLost = true;
                game?.OnDeviceLost();
            }
            return false;
        }

        if (result == ResultCode.DeviceNotReset)
        {
            if (!deviceLost)
            {
                deviceLost = true;
                game?.OnDeviceLost();
            }
            try
            {
                Device.Reset(presentParameters);
            }
            catch (SharpDXException)
            {
                return false;
            }

            deviceLost = false;
            game?.OnDeviceReset();
            return false;
        }

        if (deviceLost)
        {
            deviceLost = false;
            game?.OnDeviceReset();
        }

        return true;
    }

    public void RequestExit()
    {
        shouldExit = true;
        PostQuitMessage(0);
    }

    public void ResetDevice(bool windowed, int width, int height, bool enableVsync)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        Result result;
        try
        {
            result = Device.TestCooperativeLevel();
        }
        catch (SharpDXException ex)
        {
            result = ex.ResultCode;
        }

        if (result == ResultCode.DeviceLost)
        {
            return;
        }

        presentParameters.Windowed = windowed;
        presentParameters.BackBufferWidth = width;
        presentParameters.BackBufferHeight = height;
        presentParameters.PresentationInterval = enableVsync ? PresentInterval.Default : PresentInterval.Immediate;
        game?.OnDeviceLost();
        try
        {
            Device.Reset(presentParameters);
        }
        catch (SharpDXException ex)
        {
            Trace.TraceWarning($"Device.Reset failed: {ex.ResultCode}");
            Trace.TraceWarning(ex.StackTrace);
            return;
        }
        game?.OnDeviceReset();
    }

    public void ToggleFullscreen()
    {
        bool enableVsync = presentParameters.PresentationInterval != PresentInterval.Immediate;
        if (!isFullscreen)
        {
            if (!GetWindowRect(WindowHandle, out windowedRect))
            {
                windowedRect = new RECT { Left = 0, Top = 0, Right = presentParameters.BackBufferWidth, Bottom = presentParameters.BackBufferHeight };
            }

            Rectangle bounds = Screen.FromHandle(WindowHandle).Bounds;
            SetBorderless(true);
            SetWindowPos(WindowHandle, IntPtr.Zero, bounds.Left, bounds.Top, bounds.Width, bounds.Height,
                SWP_NOZORDER | SWP_NOACTIVATE);
            isFullscreen = true;
            ResetDevice(true, bounds.Width, bounds.Height, enableVsync);
        }
        else
        {
            SetBorderless(false);
            SetWindowPos(WindowHandle, IntPtr.Zero, windowedRect.Left, windowedRect.Top,
                windowedRect.Right - windowedRect.Left, windowedRect.Bottom - windowedRect.Top,
                SWP_NOZORDER | SWP_NOACTIVATE);
            isFullscreen = false;
            Size clientSize = GetClientSize();
            ResetDevice(true, clientSize.Width, clientSize.Height, enableVsync);
        }
    }

    private void HandleResizeEnd()
    {
        if (isFullscreen)
        {
            return;
        }

        Size clientSize = GetClientSize();
        if (clientSize.Width <= 0 || clientSize.Height <= 0)
        {
            return;
        }

        bool enableVsync = presentParameters.PresentationInterval != PresentInterval.Immediate;
        ResetDevice(true, clientSize.Width, clientSize.Height, enableVsync);
    }

    public bool TryDequeueCopyData(out string message)
    {
        if (copyDataMessages.Count > 0)
        {
            message = copyDataMessages.Dequeue();
            return true;
        }

        message = string.Empty;
        return false;
    }

    public void SetWindowTitle(string title)
    {
        SetWindowText(WindowHandle, title);
    }

    public void SetWindowPosition(int x, int y)
    {
        SetWindowPos(WindowHandle, IntPtr.Zero, x, y, 0, 0, SWP_NOZORDER | SWP_NOSIZE | SWP_NOACTIVATE);
    }

    public System.Drawing.Point GetWindowPosition()
    {
        if (!GetWindowRect(WindowHandle, out var rect))
        {
            return System.Drawing.Point.Empty;
        }

        return new System.Drawing.Point(rect.Left, rect.Top);
    }

    public Size GetClientSize()
    {
        if (!GetClientRect(WindowHandle, out var rect))
        {
            return Size.Empty;
        }

        return new Size(rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    public void SetClientSize(int width, int height)
    {
        var rect = new RECT { Left = 0, Top = 0, Right = width, Bottom = height };
        if (!AdjustWindowRect(ref rect, WS_OVERLAPPEDWINDOW, false))
        {
            return;
        }

        SetWindowPos(WindowHandle, IntPtr.Zero, 0, 0, rect.Right - rect.Left, rect.Bottom - rect.Top,
            SWP_NOZORDER | SWP_NOMOVE | SWP_NOACTIVATE);
    }

    public bool IsVisible => IsWindowVisible(WindowHandle);

    public bool IsMaximized => GetWindowState() == WindowState.Maximized;

    public WindowState GetWindowState()
    {
        var placement = new WINDOWPLACEMENT { length = (uint)Marshal.SizeOf<WINDOWPLACEMENT>() };
        if (!GetWindowPlacement(WindowHandle, ref placement))
        {
            return WindowState.Normal;
        }

        return placement.showCmd switch
        {
            SW_SHOWMAXIMIZED => WindowState.Maximized,
            SW_SHOWMINIMIZED => WindowState.Minimized,
            _ => WindowState.Normal
        };
    }

    public void SetWindowState(WindowState state)
    {
        int cmd = state switch
        {
            WindowState.Maximized => SW_MAXIMIZE,
            WindowState.Minimized => SW_MINIMIZE,
            _ => SW_RESTORE
        };
        ShowWindow(WindowHandle, cmd);
    }

    public void SetBorderless(bool borderless)
    {
        uint style = GetWindowLong(WindowHandle, GWL_STYLE);
        if (borderless)
        {
            style &= ~WS_OVERLAPPEDWINDOW;
            style |= WS_POPUP;
        }
        else
        {
            style &= ~WS_POPUP;
            style |= WS_OVERLAPPEDWINDOW;
        }

        SetWindowLong(WindowHandle, GWL_STYLE, style);
        SetWindowPos(WindowHandle, IntPtr.Zero, 0, 0, 0, 0, SWP_FRAMECHANGED | SWP_NOACTIVATE | SWP_NOZORDER | SWP_NOSIZE | SWP_NOMOVE);
    }

    public void Show()
    {
        ShowWindow(WindowHandle, SW_SHOW);
    }

    public void Activate()
    {
        SetForegroundWindow(WindowHandle);
        BringWindowToTop(WindowHandle);
    }

    public void Focus()
    {
        SetFocus(WindowHandle);
    }

    public void SetTopMost(bool topMost)
    {
        SetWindowPos(WindowHandle, topMost ? HWND_TOPMOST : HWND_NOTOPMOST, 0, 0, 0, 0,
            SWP_NOACTIVATE | SWP_NOMOVE | SWP_NOSIZE);
    }

    public bool ProcessEvents()
    {
        while (PeekMessage(out var msg, IntPtr.Zero, 0, 0, PM_REMOVE))
        {
            if (msg.message == WM_QUIT)
            {
                return false;
            }

            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }

        return true;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Device.Dispose();
        Direct3D.Dispose();

        if (WindowHandle != IntPtr.Zero)
        {
            DestroyWindow(WindowHandle);
        }

        if (ownsWindowClass)
        {
            UnregisterClass(className, GetModuleHandle(null));
        }
    }

    private IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case WM_CLOSE:
                RequestExit();
                return IntPtr.Zero;
            case WM_DESTROY:
                PostQuitMessage(0);
                return IntPtr.Zero;
            case WM_ACTIVATEAPP:
                if (wParam != IntPtr.Zero)
                {
                    game?.OnActivated();
                }
                else
                {
                    game?.OnDeactivated();
                }
                return IntPtr.Zero;
            case WM_EXITSIZEMOVE:
                HandleResizeEnd();
                game?.OnResizeEnd();
                return IntPtr.Zero;
            case WM_COPYDATA:
            {
                var data = Marshal.PtrToStructure<COPYDATASTRUCT>(lParam);
                if (data.cbData > 0 && data.lpData != IntPtr.Zero)
                {
                    var message = Marshal.PtrToStringUni(data.lpData, data.cbData / 2) ?? string.Empty;
                    copyDataMessages.Enqueue(message.TrimEnd('\0'));
                }
                return IntPtr.Zero;
            }
            case WM_MOUSEMOVE:
                game?.OnMouseMove(GetX(lParam), GetY(lParam));
                return IntPtr.Zero;
            case WM_LBUTTONDOWN:
                game?.OnMouseDown(MouseButtons.Left, GetX(lParam), GetY(lParam));
                return IntPtr.Zero;
            case WM_LBUTTONUP:
                game?.OnMouseUp(MouseButtons.Left, GetX(lParam), GetY(lParam));
                return IntPtr.Zero;
            case WM_MBUTTONDOWN:
                game?.OnMouseDown(MouseButtons.Middle, GetX(lParam), GetY(lParam));
                return IntPtr.Zero;
            case WM_MBUTTONUP:
                game?.OnMouseUp(MouseButtons.Middle, GetX(lParam), GetY(lParam));
                return IntPtr.Zero;
            case WM_RBUTTONDOWN:
                game?.OnMouseDown(MouseButtons.Right, GetX(lParam), GetY(lParam));
                return IntPtr.Zero;
            case WM_RBUTTONUP:
                game?.OnMouseUp(MouseButtons.Right, GetX(lParam), GetY(lParam));
                return IntPtr.Zero;
            case WM_MOUSEWHEEL:
                game?.OnMouseWheel(GetWheelDelta(wParam));
                return IntPtr.Zero;
            case WM_LBUTTONDBLCLK:
                game?.OnMouseDoubleClick(MouseButtons.Left, GetX(lParam), GetY(lParam));
                return IntPtr.Zero;
            case WM_KEYDOWN:
            case WM_SYSKEYDOWN:
                if ((Keys)wParam == Keys.Return && msg == WM_SYSKEYDOWN)
                {
                    game?.ToggleFullscreen();
                    return IntPtr.Zero;
                }
                game?.OnKeyDown((Keys)wParam, (msg == WM_SYSKEYDOWN));
                return IntPtr.Zero;
            case WM_KEYUP:
            case WM_SYSKEYUP:
                game?.OnKeyUp((Keys)wParam);
                return IntPtr.Zero;
            case WM_CHAR:
                game?.OnKeyChar((char)wParam);
                return IntPtr.Zero;
        }

        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private static int GetX(IntPtr lParam) => (short)((long)lParam & 0xFFFF);

    private static int GetY(IntPtr lParam) => (short)(((long)lParam >> 16) & 0xFFFF);

    private static int GetWheelDelta(IntPtr wParam) => (short)(((long)wParam >> 16) & 0xFFFF);

    public enum WindowState
    {
        Normal,
        Minimized,
        Maximized
    }

    private const int ERROR_CLASS_ALREADY_EXISTS = 1410;
    private const int CW_USEDEFAULT = unchecked((int)0x80000000);
    private const int SW_SHOW = 5;
    private const int SW_RESTORE = 9;
    private const int SW_MAXIMIZE = 3;
    private const int SW_MINIMIZE = 6;
    private const int SW_SHOWMAXIMIZED = 3;
    private const int SW_SHOWMINIMIZED = 2;
    private const int IDC_ARROW = 32512;
    private const uint PM_REMOVE = 0x0001;
    private const uint WM_DESTROY = 0x0002;
    private const uint WM_CLOSE = 0x0010;
    private const uint WM_QUIT = 0x0012;
    private const uint WM_ACTIVATEAPP = 0x001C;
    private const uint WM_COPYDATA = 0x004A;
    private const uint WM_MOUSEMOVE = 0x0200;
    private const uint WM_LBUTTONDOWN = 0x0201;
    private const uint WM_LBUTTONUP = 0x0202;
    private const uint WM_LBUTTONDBLCLK = 0x0203;
    private const uint WM_RBUTTONDOWN = 0x0204;
    private const uint WM_RBUTTONUP = 0x0205;
    private const uint WM_MBUTTONDOWN = 0x0207;
    private const uint WM_MBUTTONUP = 0x0208;
    private const uint WM_MOUSEWHEEL = 0x020A;
    private const uint WM_KEYDOWN = 0x0100;
    private const uint WM_KEYUP = 0x0101;
    private const uint WM_CHAR = 0x0102;
    private const uint WM_SYSKEYDOWN = 0x0104;
    private const uint WM_SYSKEYUP = 0x0105;
    private const uint WM_EXITSIZEMOVE = 0x0232;
    private const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
    private const uint WS_POPUP = 0x80000000;
    private const uint CS_HREDRAW = 0x0002;
    private const uint CS_VREDRAW = 0x0001;
    private const uint CS_DBLCLKS = 0x0008;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const int GWL_STYLE = -16;
    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new(-2);

    private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WndClassEx
    {
        public uint cbSize;
        public uint style;
        public WndProc lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string lpszMenuName;
        public string lpszClassName;
        public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WINDOWPLACEMENT
    {
        public uint length;
        public uint flags;
        public int showCmd;
        public POINT ptMinPosition;
        public POINT ptMaxPosition;
        public RECT rcNormalPosition;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct COPYDATASTRUCT
    {
        public IntPtr dwData;
        public int cbData;
        public IntPtr lpData;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassEx([In] ref WndClassEx lpwcx);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(
        int dwExStyle,
        string lpClassName,
        string lpWindowName,
        uint dwStyle,
        int x,
        int y,
        int nWidth,
        int nHeight,
        IntPtr hWndParent,
        IntPtr hMenu,
        IntPtr hInstance,
        IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UpdateWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern void PostQuitMessage(int nExitCode);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool TranslateMessage([In] ref MSG lpMsg);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr DispatchMessage([In] ref MSG lpMsg);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AdjustWindowRect(ref RECT lpRect, uint dwStyle, bool bMenu);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetWindowText(IntPtr hWnd, string lpString);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetFocus(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);
}
