using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Drawable;
using DTXMania.UI.Inspector;
using Silk.NET.OpenGL;

namespace OpenGLTest;

internal sealed unsafe class DTXManiaGL : OpenGlGame
{
    private CDTXMania mania;
    
    public override void Init()
    {
        mania = new CDTXMania();
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
}
