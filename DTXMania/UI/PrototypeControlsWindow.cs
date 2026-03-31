using System.Numerics;
using Hexa.NET.ImGui;

namespace OpenGLTest;

internal static class PrototypeControlsWindow
{
    public static void Draw(GlfwOpenGlHost host)
    {
        ImGui.SetNextWindowSize(new Vector2(320, 240), ImGuiCond.FirstUseEver);
        ImGui.Begin("Display Controls");
        ImGui.Text($"Renderer: {host.Renderer.name}");
        ImGui.Text($"FPS: {host.Fps:F1}");
        ImGui.Text($"Frame time: {host.FrameTimeMs:F2} ms");
        ImGui.Text($"Window: {host.WindowWidth} x {host.WindowHeight}");
        ImGui.Text($"Framebuffer: {host.FramebufferWidth} x {host.FramebufferHeight}");
        ImGui.Text($"Working set: {host.WorkingSetMb:F1} MB");
        ImGui.Text($"Private bytes: {host.PrivateMb:F1} MB");
        ImGui.Text($"Managed heap: {host.ManagedMb:F1} MB");
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
        if (ImGui.RadioButton("Windowed", ref fullscreenMode, (int)OpenGLTest.FullscreenMode.Windowed))
        {
            host.RequestFullscreenMode((FullscreenMode)fullscreenMode);
        }

        if (ImGui.RadioButton("Borderless fullscreen", ref fullscreenMode, (int)OpenGLTest.FullscreenMode.BorderlessFullscreen))
        {
            host.RequestFullscreenMode((FullscreenMode)fullscreenMode);
        }

        if (ImGui.RadioButton("Exclusive fullscreen", ref fullscreenMode, (int)OpenGLTest.FullscreenMode.ExclusiveFullscreen))
        {
            host.RequestFullscreenMode((FullscreenMode)fullscreenMode);
        }

        ImGui.End();
    }
}
