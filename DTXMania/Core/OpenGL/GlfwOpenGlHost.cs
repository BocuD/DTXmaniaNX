using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DTXMania.Core.Framework;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using DTXMania.UI.Inspector;
using DTXMania.UI.OpenGL;
using Hexa.NET.GLFW;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.GLFW;
using Hexa.NET.ImGui.Backends.OpenGL3;

using GLFWwindowPtr = Hexa.NET.GLFW.GLFWwindowPtr;

namespace DTXMania.Core.OpenGL;

internal sealed unsafe class GlfwOpenGlHost : IGameHost, IDisposable
{
    public IRenderer Renderer => renderer;

    private const int GlfwTrue = 1;
    private const int GlfwFalse = 0;
    private const int GlfwDecorated = 0x00020005;
    private const int GlfwContextVersionMajor = 0x00022002;
    private const int GlfwContextVersionMinor = 0x00022003;
    private const int GlfwOpenGlProfile = 0x00022008;
    private const int GlfwOpenGlCoreProfile = 0x00032001;
    private const int GlfwOpenGlForwardCompat = 0x00022006;

    private readonly OpenGlGame _game;
    private readonly GameRenderTarget _gameRenderTarget = new();
    private readonly OpenGlRenderer renderer = new();
    private readonly OpenGlSkiaTextRenderer _skiaTextRenderer = new();
    private readonly OpenGlTextureFactory _textureFactory = new();
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    private GLFWwindowPtr _window;
    private GlfwNativeContext? _nativeContext;
    private Silk.NET.OpenGL.GL? _gl;
    private ImGuiContextPtr _imguiContext;

    private bool _vsyncEnabled = true;
    public FullscreenMode fullscreenMode { get; private set; } = FullscreenMode.Windowed;
    private bool _renderInGameWindow;
    private int _windowedX = 80;
    private int _windowedY = 80;
    private int _windowedWidth = 1280;
    private int _windowedHeight = 720;
    private string _windowTitle = "";
    private bool _clearImGuiFocusOnNextFrame = true;

    private double _lastFrameTime;
    private double _fpsAccumulatedTime;
    private int _fpsFrameCount;
    private float _displayedFps;
    private float _displayedFrameTimeMs;

    private float _deltaTime;
    private int _windowWidth;
    private int _windowHeight;
    private int _framebufferWidth;
    private int _framebufferHeight;

    private bool? _pendingVsyncEnabled;
    private FullscreenMode? _pendingFullscreenMode;

    [DllImport("glfw3", EntryPoint = "glfwGetWin32Window")]
    private static extern IntPtr glfwGetWin32Window(IntPtr window);

    public GlfwOpenGlHost(OpenGlGame game)
    {
        _game = game;
        _game.host = this;
        RendererInfo.host = this;
    }

    public bool VsyncEnabled => _vsyncEnabled;
    public FullscreenMode FullscreenMode => fullscreenMode;
    public bool RenderInGameWindow
    {
        get => _renderInGameWindow;
        set => _renderInGameWindow = value;
    }

    public float Fps => _displayedFps;
    public float FrameTimeMs => _displayedFrameTimeMs;
    public int WindowWidth => _windowWidth;
    public int WindowHeight => _windowHeight;
    public int FramebufferWidth => _framebufferWidth;
    public int FramebufferHeight => _framebufferHeight;

    public void RequestVsync(bool enabled)
    {
        _pendingVsyncEnabled = enabled;
    }

    public void RequestFullscreenMode(FullscreenMode fullscreenMode)
    {
        _pendingFullscreenMode = fullscreenMode;
    }
    
    public IntPtr GetWindowHandle()
    {
        // Return the native Windows HWND for the GLFW window when running on Windows.
        // If the window isn't created yet or the platform isn't Windows, return IntPtr.Zero.
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return IntPtr.Zero;
        }

        if (_window.Handle == null)
        {
            return IntPtr.Zero;
        }

        // Convert the GLFW window handle to IntPtr and call the native glfw function.
        // _window.Handle may be an IntPtr or an unmanaged pointer type; casting to IntPtr is allowed in unsafe context.
        return glfwGetWin32Window((IntPtr)_window.Handle);
    }

    public void SetWindowTitle(string newTitle)
    {
        _windowTitle = newTitle;
        
        if (_window.Handle != null)
        {
            GLFW.SetWindowTitle(_window, newTitle);
        }
    }

    public void SetWindowSize(Vector2 value)
    {
        if (_window.Handle != null)
        {
            GLFW.SetWindowSize(_window, (int)value.X, (int)value.Y);
        }
    }

    public void SetWindowPosition(Vector2 value)
    {
        if (_window.Handle != null)
        {
            GLFW.SetWindowPos(_window, (int)value.X, (int)value.Y);
        }
    }

    public void Run()
    {
        Trace.Listeners.Add(InspectorManager.logWindow);

        if (GLFW.Init() == 0)
        {
            throw new InvalidOperationException("GLFW initialization failed.");
        }

        try
        {
            CreateInitialWindowAndGraphics();
            MainLoop();
        }
        finally
        {
            Dispose();
        }
    }

    public void Dispose()
    {
        if (_window.Handle != null)
        {
            GLFW.MakeContextCurrent(_window);
        }

        ShutdownImGui();
        renderer.Dispose();
        OpenGlRenderer.Instance = null;
        BaseTexture.SkiaTextRenderer = null!;
        BaseTexture.Factory = null!;
        _gameRenderTarget.Dispose();
        _game.Dispose();

        if (_window.Handle != null)
        {
            GLFW.DestroyWindow(_window);
            _window = default;
        }

        _nativeContext?.Dispose();
        _nativeContext = null;
        _gl = null;

        GLFW.Terminate();
    }

    private void CreateInitialWindowAndGraphics()
    {
        _window = CreateWindow(default);
        _nativeContext = new GlfwNativeContext();
        _gl = Silk.NET.OpenGL.GL.GetApi(_nativeContext);
        _game.AttachGraphics(_gl);
        _gameRenderTarget.AttachGraphics(_gl);
        renderer.AttachGraphics(_gl);
        OpenGlRenderer.Instance = renderer;
        
        BaseTexture.SkiaTextRenderer = _skiaTextRenderer;
        BaseTexture.Factory = _textureFactory;
        InitializeImGui();
    }

    private GLFWwindowPtr CreateWindow(GLFWwindowPtr shareWindow)
    {
        GLFW.DefaultWindowHints();
        GLFW.WindowHint(GlfwContextVersionMajor, 3);
        GLFW.WindowHint(GlfwContextVersionMinor, 3);
        GLFW.WindowHint(GlfwOpenGlProfile, GlfwOpenGlCoreProfile);

        if (OperatingSystem.IsMacOS())
        {
            GLFW.WindowHint(GlfwOpenGlForwardCompat, GlfwTrue);
        }

        Hexa.NET.GLFW.GLFWmonitorPtr primaryMonitor = GLFW.GetPrimaryMonitor();
        GLFWvidmodePtr videoMode = primaryMonitor.Handle != null ? GLFW.GetVideoMode(primaryMonitor) : default;
        GLFWwindowPtr window;

        switch (fullscreenMode)
        {
            case FullscreenMode.Windowed:
                GLFW.WindowHint(GlfwDecorated, GlfwTrue);
                window = GLFW.CreateWindow(_windowedWidth, _windowedHeight, _game.name, default, shareWindow);
                break;
            
            case FullscreenMode.BorderlessFullscreen:
                if (primaryMonitor.Handle == null || videoMode.Handle == null)
                {
                    throw new InvalidOperationException("Primary monitor unavailable.");
                }

                GLFW.WindowHint(GlfwDecorated, GlfwFalse);
                window = GLFW.CreateWindow(videoMode.Width, videoMode.Height, _game.name, default, shareWindow);
                break;
            
            case FullscreenMode.ExclusiveFullscreen:
                if (primaryMonitor.Handle == null || videoMode.Handle == null)
                {
                    throw new InvalidOperationException("Primary monitor unavailable.");
                }

                window = GLFW.CreateWindow(videoMode.Width, videoMode.Height, _game.name, primaryMonitor, shareWindow);
                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (window.Handle == null)
        {
            throw new InvalidOperationException("Window creation failed.");
        }

        GLFW.MakeContextCurrent(window);

        if (fullscreenMode == FullscreenMode.Windowed)
        {
            GLFW.SetWindowPos(window, _windowedX, _windowedY);
        }
        else if (fullscreenMode == FullscreenMode.BorderlessFullscreen)
        {
            int monitorX = 0;
            int monitorY = 0;
            GLFW.GetMonitorPos(primaryMonitor, ref monitorX, ref monitorY);
            GLFW.SetWindowPos(window, monitorX, monitorY);
        }
        
        SetCallbacks(window);

        // Ensure the newly created window receives focus so input starts immediately.
        GLFW.FocusWindow(window);

        GLFW.SwapInterval(_vsyncEnabled ? 1 : 0);
        return window;
    }
    
    private GLFWkeyfun? keyCallback;
    private GLFWwindowfocusfun? focusCallback;
    private GLFWwindowposfun? windowPosCallback;
    private GLFWwindowsizefun? windowSizeCallback;

    private void SetCallbacks(GLFWwindowPtr window)
    {
        keyCallback = (_, key, _, action, mods) =>
        {
            switch (action)
            {
                case GLFW.GLFW_PRESS:
                    _game.KeyDown((GlfwKey)key, (GlfwMod)mods);
                    break;

                case GLFW.GLFW_RELEASE:
                    _game.KeyUp((GlfwKey)key, (GlfwMod)mods);
                    break;
            }
        };
        
        focusCallback = (_, focused) => _game.isFocused = focused != 0;
        windowPosCallback = (_, xpos, ypos) => _game.windowPosition = new Vector2(xpos, ypos);
        windowSizeCallback = (_, width, height) => _game.windowSize = new Vector2(width, height);
        
        //set key callbacks
        GLFW.SetKeyCallback(window, keyCallback);
        GLFW.SetWindowFocusCallback(window, focusCallback);
        GLFW.SetWindowPosCallback(window, windowPosCallback);
        GLFW.SetWindowSizeCallback(window, windowSizeCallback);
        
        //initial update pos and size
        GLFW.GetWindowPos(window, ref _windowedX, ref _windowedY);
        GLFW.GetWindowSize(window, ref _windowedWidth, ref _windowedHeight);
        _game.windowPosition  = new Vector2(_windowedX, _windowedY);
        _game.windowSize    = new Vector2(_windowedWidth, _windowedHeight);

        // Focus callbacks may not fire when a window starts already focused.
        // Seed this state so input polling is enabled immediately on startup/recreate.
        _game.isFocused = true;
    }

    private void InitializeImGui()
    {
        _imguiContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(_imguiContext);
        ImGui.StyleColorsDark();

        ImGuiIOPtr io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        ConfigureImGuiFonts(io);

        ImGuiImplGLFW.SetCurrentContext(_imguiContext);
        ImGuiImplOpenGL3.SetCurrentContext(_imguiContext);

        var backendWindow = Unsafe.BitCast<GLFWwindowPtr, Hexa.NET.ImGui.Backends.GLFW.GLFWwindowPtr>(_window);
        if (!ImGuiImplGLFW.InitForOpenGL(backendWindow, true))
        {
            throw new InvalidOperationException("Failed to initialize Hexa.NET ImGui GLFW backend.");
        }

        if (!ImGuiImplOpenGL3.Init("#version 330 core"))
        {
            throw new InvalidOperationException("Failed to initialize Hexa.NET ImGui OpenGL3 backend.");
        }
    }

    private static void ConfigureImGuiFonts(ImGuiIOPtr io)
    {
        io.Fonts.Clear();

        string? defaultFontPath = UIFonts.FallbackFontPath;
        if (string.IsNullOrWhiteSpace(defaultFontPath) || !File.Exists(defaultFontPath))
        {
            io.Fonts.AddFontDefault();
            return;
        }

        ImFontPtr font = ImGui.AddFontFromFileTTF(io.Fonts, defaultFontPath, 18f);
        if (font.Handle == null)
        {
            io.Fonts.AddFontDefault();
            return;
        }

        io.FontDefault = font;
    }

    private void ShutdownImGui()
    {
        if (_imguiContext.Handle == null)
        {
            return;
        }

        if (_window.Handle != null)
        {
            GLFW.MakeContextCurrent(_window);
        }

        ImGui.SetCurrentContext(_imguiContext);
        ImGuiImplOpenGL3.SetCurrentContext(_imguiContext);
        ImGuiImplGLFW.SetCurrentContext(_imguiContext);
        ImGuiImplOpenGL3.Shutdown();
        ImGuiImplGLFW.Shutdown();
        ImGuiImplOpenGL3.SetCurrentContext(default);
        ImGuiImplGLFW.SetCurrentContext(default);
        ImGui.DestroyContext(_imguiContext);
        _imguiContext = default;
    }

    private void RecreateWindow(FullscreenMode previousMode)
    {
        if (_window.Handle != null && previousMode == FullscreenMode.Windowed)
        {
            GLFW.GetWindowPos(_window, ref _windowedX, ref _windowedY);
            GLFW.GetWindowSize(_window, ref _windowedWidth, ref _windowedHeight);
        }
        
        GLFWwindowPtr oldWindow = _window;
        _game.ReleaseContextResources();
        _gameRenderTarget.ReleaseContextResources();
        renderer.ReleaseContextResources();
        ShutdownImGui();
        
        GLFWwindowPtr newWindow = CreateWindow(oldWindow);
        _window = newWindow;
        _game.AttachGraphics(_gl!);
        _game.windowSize = new Vector2(_windowWidth, _windowHeight);
        _gameRenderTarget.AttachGraphics(_gl!);
        renderer.AttachGraphics(_gl!);
        GLFW.SetWindowTitle(newWindow, _windowTitle);
        InitializeImGui();
        _clearImGuiFocusOnNextFrame = true;

        _game.WindowHandleUpdated(GetWindowHandle());

        if (oldWindow.Handle != null)
        {
            GLFW.DestroyWindow(oldWindow);
        }
    }

    private void MainLoop()
    {
        _game.Init();
        
        while (GLFW.WindowShouldClose(_window) == 0 && !_game.isExiting)
        {
            GLFW.PollEvents();

            if (GLFW.WindowShouldClose(_window) != 0)
            {
                break;
            }

            GLFW.GetFramebufferSize(_window, ref _framebufferWidth, ref _framebufferHeight);
            GLFW.GetWindowSize(_window, ref _windowWidth, ref _windowHeight);
            UpdateDiagnostics();

            GLFW.MakeContextCurrent(_window);
            ImGui.SetCurrentContext(_imguiContext);
            ImGuiImplGLFW.SetCurrentContext(_imguiContext);
            ImGuiImplOpenGL3.SetCurrentContext(_imguiContext);
            ImGuiImplOpenGL3.NewFrame();
            ImGuiImplGLFW.NewFrame();
            ImGui.NewFrame();

            if (_clearImGuiFocusOnNextFrame)
            {
                ImGui.SetWindowFocus((string?)null);
                _clearImGuiFocusOnNextFrame = false;
            }

            int targetWidth;
            int targetHeight;
            if (_renderInGameWindow)
            {
                var desiredRenderSize = GameWindow.DesiredRenderSize;
                targetWidth = Math.Max((int)desiredRenderSize.X, 1);
                targetHeight = Math.Max((int)desiredRenderSize.Y, 1);
            }
            else
            {
                targetWidth = Math.Max(_framebufferWidth, 1);
                targetHeight = Math.Max(_framebufferHeight, 1);
            }

            _gameRenderTarget.Resize(targetWidth, targetHeight);
            renderer.BeginFrame(targetWidth, targetHeight);
            _gameRenderTarget.BindForRendering();
            _game.Update(_deltaTime, _stopwatch.Elapsed.TotalSeconds);
            _game.Render(targetWidth, targetHeight, _stopwatch.Elapsed.TotalSeconds);
            _gameRenderTarget.BindDefaultFramebuffer(Math.Max(_framebufferWidth, 1), Math.Max(_framebufferHeight, 1));

            InspectorManager.Draw(_renderInGameWindow, _gameRenderTarget.TextureId, new Vector2(_gameRenderTarget.Width, _gameRenderTarget.Height));
            
            ImGui.Render();
            GLFW.MakeContextCurrent(_window);
            if (_renderInGameWindow)
            {
                _gameRenderTarget.ClearDefaultFramebuffer(0.08f, 0.09f, 0.12f, 1f, Math.Max(_framebufferWidth, 1), Math.Max(_framebufferHeight, 1));
            }
            else
            {
                _gameRenderTarget.BlitToDefaultFramebuffer(Math.Max(_framebufferWidth, 1), Math.Max(_framebufferHeight, 1));
            }
            ImGuiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());
            GLFW.SwapBuffers(_window);

            if (GLFW.WindowShouldClose(_window) != 0)
            {
                break;
            }

            ApplyPendingDisplayChanges();
        }
    }

    private void UpdateDiagnostics()
    {
        double currentTime = _stopwatch.Elapsed.TotalSeconds;
        _deltaTime = Math.Max((float)(currentTime - _lastFrameTime), 1e-6f);
        _lastFrameTime = currentTime;

        UpdateFrameStats(_deltaTime);
    }

    private void UpdateFrameStats(float deltaTime)
    {
        _fpsAccumulatedTime += deltaTime;
        _fpsFrameCount++;

        if (_fpsAccumulatedTime < 0.25)
        {
            return;
        }

        _displayedFps = (float)(_fpsFrameCount / _fpsAccumulatedTime);
        _displayedFrameTimeMs = 1000f / Math.Max(_displayedFps, 0.0001f);
        _fpsAccumulatedTime = 0;
        _fpsFrameCount = 0;
    }

    private void ApplyPendingDisplayChanges()
    {
        if (_pendingFullscreenMode is { } fullscreenMode && fullscreenMode != this.fullscreenMode)
        {
            FullscreenMode previousMode = this.fullscreenMode;
            this.fullscreenMode = fullscreenMode;
            RecreateWindow(previousMode);
            _pendingFullscreenMode = null;
            _pendingVsyncEnabled = null;
            return;
        }

        if (_pendingVsyncEnabled is { } vsyncEnabled && vsyncEnabled != _vsyncEnabled)
        {
            _vsyncEnabled = vsyncEnabled;
            RecreateWindow(this.fullscreenMode);
        }

        _pendingVsyncEnabled = null;
        _pendingFullscreenMode = null;
    }
}
