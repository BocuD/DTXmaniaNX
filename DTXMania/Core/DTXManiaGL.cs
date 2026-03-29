using DTXMania.Core;
using DTXMania.UI.Inspector;
using Hexa.NET.GLFW;
using Silk.NET.OpenGL;

namespace OpenGLTest;

internal sealed unsafe class DTXManiaGL : OpenGlGame
{
    private CDTXMania mania;
    
    public override void Init()
    {
        mania = new CDTXMania(this);
    }

    protected override void CreateSharedResources()
    {
        
    }

    protected override void CreateContextResources()
    {
        
    }

    public override void Update(float deltaTime, double totalTime)
    {
        
    }

    public override void Render(int width, int height, double totalTime)
    {
        Gl.Enable(GLEnum.DepthTest);
        Gl.Viewport(0, 0, (uint)Math.Max(width, 1), (uint)Math.Max(height, 1));
        Gl.ClearColor(0.00f, 0.00f, 0.10f, 1f);
        Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        float aspectRatio = width / (float)Math.Max(height, 1);

        mania.Draw();
        GameStatus.Draw();
    }

    protected override void DestroyContextResources()
    {
        
    }

    protected override void DestroySharedResources()
    {
        
    }

    public override void KeyDown(GlfwKey key, GlfwMod mods)
    {
        //check for alt + enter
        if (key == GlfwKey.Enter && mods == GlfwMod.Alt)
        {
            //toggle fullscreen
            host.RequestFullscreenMode(host.fullscreenMode == FullscreenMode.Windowed
                ? FullscreenMode.ExclusiveFullscreen
                : FullscreenMode.Windowed);
        }
        
        mania.KeyPress(key, mods);
    }

    public override void KeyUp(GlfwKey key, GlfwMod mods)
    {
        
    }
}
