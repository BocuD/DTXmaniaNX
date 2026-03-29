using Silk.NET.OpenGL;

namespace OpenGLTest;

internal abstract class OpenGlGame : BaseWindow, IDisposable
{
    protected GL Gl { get; private set; } = null!;
    private bool _sharedResourcesCreated;
    private bool _contextResourcesCreated;

    public void AttachGraphics(GL gl)
    {
        Gl = gl;

        if (!_sharedResourcesCreated)
        {
            CreateSharedResources();
            _sharedResourcesCreated = true;
        }

        if (_contextResourcesCreated)
        {
            DestroyContextResources();
        }

        CreateContextResources();
        _contextResourcesCreated = true;
    }
    
    public abstract void Init();

    public abstract void Update(float deltaTime, double totalTime);

    public abstract void Render(int framebufferWidth, int framebufferHeight, double totalTime);
    
    public void ReleaseContextResources()
    {
        if (!_contextResourcesCreated)
        {
            return;
        }

        DestroyContextResources();
        _contextResourcesCreated = false;
    }

    protected abstract void CreateSharedResources();

    protected abstract void CreateContextResources();

    protected abstract void DestroyContextResources();

    protected abstract void DestroySharedResources();

    public void Dispose()
    {
        ReleaseContextResources();

        if (_sharedResourcesCreated)
        {
            DestroySharedResources();
            _sharedResourcesCreated = false;
        }
    }
}
