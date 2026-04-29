using System.Numerics;
using DTXMania.Core.Framework;
using DTXMania.Core.OpenGL;
using Hexa.NET.ImGui;

namespace DTXMania.UI;

internal static class RendererInfo
{
    public static GlfwOpenGlHost host;
    
    public static void Draw()
    {
        ImGui.SetNextWindowSize(new Vector2(320, 240), ImGuiCond.FirstUseEver);
        ImGui.Begin("Display Controls", ImGuiWindowFlags.NoFocusOnAppearing);
        
        ImGui.Text($"Renderer: {host.Renderer.name}");
        ImGui.Text($"FPS: {host.Fps:F1}");
        ImGui.Text($"Draw calls: {host.Renderer.lastFrameDrawCalls}");
        ImGui.Text($"Frame time: {host.FrameTimeMs:F2} ms");
        ImGui.Text($"Window: {host.WindowWidth} x {host.WindowHeight}");
        ImGui.Text($"Framebuffer: {host.FramebufferWidth} x {host.FramebufferHeight}");

        ImGui.Separator();

        bool vsyncEnabled = host.VsyncEnabled;
        if (ImGui.Checkbox("VSync", ref vsyncEnabled))
        {
            host.RequestVsync(vsyncEnabled);
        }

        bool renderInGameWindow = host.RenderInGameWindow;
        if (ImGui.Checkbox("Render In Game Window", ref renderInGameWindow))
        {
            host.RenderInGameWindow = renderInGameWindow;
        }

        int fullscreenMode = (int)host.FullscreenMode;
        if (ImGui.RadioButton("Windowed", ref fullscreenMode, (int)FullscreenMode.Windowed))
        {
            host.RequestFullscreenMode((FullscreenMode)fullscreenMode);
        }

        if (ImGui.RadioButton("Borderless fullscreen", ref fullscreenMode, (int)FullscreenMode.BorderlessFullscreen))
        {
            host.RequestFullscreenMode((FullscreenMode)fullscreenMode);
        }

        if (ImGui.RadioButton("Exclusive fullscreen", ref fullscreenMode, (int)FullscreenMode.ExclusiveFullscreen))
        {
            host.RequestFullscreenMode((FullscreenMode)fullscreenMode);
        }

        ImGui.End();
    }
}
