using DTXMania.Core.Framework;
using DTXMania.Core.OpenGL;
using DTXMania.UI.Inspector;
using DTXMania.UI.OpenGL;
using Hexa.NET.GLFW;
using Hexa.NET.ImGui;
using Silk.NET.OpenGL;

namespace DTXMania.Core;

public sealed class DTXManiaGL : OpenGlGame
{
    public static DTXManiaGL instance;

    //max bytes of texture data uploaded to the GPU per frame from the async loader. ~1.5 MB lets
    //one 512x512 preimage (1 MB) plus several text textures through per frame while keeping the
    //per-frame upload cost bounded.
    private const long AsyncUploadBytesPerFrame = 1_500_000;

    private CDTXMania mania;
    private bool clearImGuiFocusOnNextRender = true;
    private bool isReady = false;
    
    public override void Init()
    {
        mania = new CDTXMania(this);
        instance = this;
    }

    protected override void CreateSharedResources()
    {
        
    }

    protected override void CreateContextResources()
    {
        
    }

    public override void Update(float deltaTime, double totalTime)
    {
        mania.Update();
        GameStatus.UpdatePerformanceGraph(deltaTime);
    }

    public override void Render(int width, int height, double totalTime)
    {
        Gl.Enable(GLEnum.DepthTest);
        Gl.Viewport(0, 0, (uint)Math.Max(width, 1), (uint)Math.Max(height, 1));
        Gl.ClearColor(0.00f, 0.00f, 0.00f, 1f);
        Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        //upload any images/text decoded on the background thread since last frame, throttled so a
        //burst of newly-loaded textures can never cause a frame-time spike.
        AsyncTextureUploader.Instance.PumpUploads(AsyncUploadBytesPerFrame);

        CDTXMania.renderScale = windowSize.X / GameWindowSize.Width;

        if (host.fullscreenMode == FullscreenMode.Windowed)
        {
            CDTXMania.ConfigIni.nWindowWidth = (int)windowSize.X;
            CDTXMania.ConfigIni.nWindowHeight = (int)windowSize.Y;
            CDTXMania.ConfigIni.nInitialWindowXPosition = (int)windowPosition.X;
            CDTXMania.ConfigIni.nInitialWindowYPosition = (int)windowPosition.Y;
        }
        
        mania.Draw();

        if (clearImGuiFocusOnNextRender)
        {
            ImGui.SetWindowFocus((string?)null);
            clearImGuiFocusOnNextRender = false;
        }
    }

    protected override void DestroyContextResources()
    {
        
    }

    protected override void DestroySharedResources()
    {
        
    }

    public override string name => "DTXManiaNX";

    public override void KeyDown(GlfwKey key, GlfwMod mods)
    {
        //check for alt + enter
        if (key == GlfwKey.Enter && mods == GlfwMod.Alt)
        {
            CDTXMania.ConfigIni.bFullScreenMode = !CDTXMania.ConfigIni.bFullScreenMode;
            var targetFullscreenMode = CDTXMania.ConfigIni.bFullScreenExclusive
                ? FullscreenMode.ExclusiveFullscreen
                : FullscreenMode.BorderlessFullscreen;
            
            //toggle fullscreen
            host.RequestFullscreenMode(CDTXMania.ConfigIni.bFullScreenMode
                ? targetFullscreenMode
                : FullscreenMode.Windowed);
        }
        
        mania.KeyPress(key, mods);
    }

    public override void KeyUp(GlfwKey key, GlfwMod mods)
    {
        
    }

    public override void WindowHandleUpdated(IntPtr newHandle)
    {
        CDTXMania.InputManager?.UpdateWindowHandle(newHandle);
        clearImGuiFocusOnNextRender = true;
    }
}
