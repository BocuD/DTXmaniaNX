using Hexa.NET.GLFW;

namespace OpenGLTest;

internal sealed unsafe class GlfwNativeContext : Silk.NET.Core.Contexts.INativeContext, HexaGen.Runtime.INativeContext
{
    public nint GetProcAddress(string proc)
    {
        return (nint)GLFW.GetProcAddress(proc);
    }

    public nint GetProcAddress(string proc, int? slot = null)
    {
        return GetProcAddress(proc);
    }

    public bool TryGetProcAddress(string proc, out nint addr)
    {
        addr = GetProcAddress(proc);
        return addr != 0;
    }

    public bool TryGetProcAddress(string proc, out nint addr, int? slot = null)
    {
        addr = GetProcAddress(proc);
        return addr != 0;
    }

    public bool IsExtensionSupported(string extension)
    {
        return false;
    }

    public void Dispose()
    {
    }
}
