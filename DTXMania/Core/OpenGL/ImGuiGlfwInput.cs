using System.Numerics;
using Hexa.NET.GLFW;
using Hexa.NET.ImGui;

using GLFWwindowPtr = Hexa.NET.GLFW.GLFWwindowPtr;

namespace DTXMania.Core.OpenGL;

//game window is not created by hexa.net ImGui copy; ImGui input does not work on platforms where the window pointer
//is not shared
//this class bridges around this issue by implementing it manually instead using glfw callbacks set in GlfwOpenGlHost
internal sealed class ImGuiGlfwInput
{
    public void NewFrame(Vector2 windowSize, Vector2 framebufferSize, float deltaTime)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.DisplaySize = Vector2.Max(windowSize, Vector2.One);
        io.DisplayFramebufferScale = windowSize.X > 0 && windowSize.Y > 0 ? framebufferSize / windowSize : Vector2.One;
        io.DeltaTime = Math.Max(deltaTime, 1e-6f);
    }

    public void OnKey(GLFWwindowPtr window, int key, int action)
    {
        if (action == GLFW.GLFW_REPEAT)
        {
            return;
        }

        ImGuiIOPtr io = ImGui.GetIO();

        //mods
        io.AddKeyEvent(ImGuiKey.ModCtrl, IsDown(window, GlfwKey.LeftControl) || IsDown(window, GlfwKey.RightControl));
        io.AddKeyEvent(ImGuiKey.ModShift, IsDown(window, GlfwKey.LeftShift) || IsDown(window, GlfwKey.RightShift));
        io.AddKeyEvent(ImGuiKey.ModAlt, IsDown(window, GlfwKey.LeftAlt) || IsDown(window, GlfwKey.RightAlt));
        io.AddKeyEvent(ImGuiKey.ModSuper, IsDown(window, GlfwKey.LeftSuper) || IsDown(window, GlfwKey.RightSuper));

        ImGuiKey imguiKey = ToImGuiKey((GlfwKey)key);
        if (imguiKey != ImGuiKey.None)
        {
            io.AddKeyEvent(imguiKey, action == GLFW.GLFW_PRESS);
        }
    }

    public void OnChar(uint codepoint) => ImGui.GetIO().AddInputCharacter(codepoint);

    public void OnCursorPos(double x, double y) => ImGui.GetIO().AddMousePosEvent((float)x, (float)y);

    public void OnMouseButton(int button, int action)
    {
        if (button is >= 0 and < (int)ImGuiMouseButton.Count)
        {
            ImGui.GetIO().AddMouseButtonEvent(button, action == GLFW.GLFW_PRESS);
        }
    }

    public void OnScroll(double x, double y) => ImGui.GetIO().AddMouseWheelEvent((float)x, (float)y);

    public void OnFocus(bool focused) => ImGui.GetIO().AddFocusEvent(focused);

    private static bool IsDown(GLFWwindowPtr window, GlfwKey key) => GLFW.GetKey(window, (int)key) == GLFW.GLFW_PRESS;

    private static ImGuiKey ToImGuiKey(GlfwKey key) => key switch
    {
        GlfwKey.Tab => ImGuiKey.Tab,
        GlfwKey.Left => ImGuiKey.LeftArrow,
        GlfwKey.Right => ImGuiKey.RightArrow,
        GlfwKey.Up => ImGuiKey.UpArrow,
        GlfwKey.Down => ImGuiKey.DownArrow,
        GlfwKey.PageUp => ImGuiKey.PageUp,
        GlfwKey.PageDown => ImGuiKey.PageDown,
        GlfwKey.Home => ImGuiKey.Home,
        GlfwKey.End => ImGuiKey.End,
        GlfwKey.Insert => ImGuiKey.Insert,
        GlfwKey.Delete => ImGuiKey.Delete,
        GlfwKey.Backspace => ImGuiKey.Backspace,
        GlfwKey.Space => ImGuiKey.Space,
        GlfwKey.Enter => ImGuiKey.Enter,
        GlfwKey.Escape => ImGuiKey.Escape,
        GlfwKey.Apostrophe => ImGuiKey.Apostrophe,
        GlfwKey.Comma => ImGuiKey.Comma,
        GlfwKey.Minus => ImGuiKey.Minus,
        GlfwKey.Period => ImGuiKey.Period,
        GlfwKey.Slash => ImGuiKey.Slash,
        GlfwKey.Semicolon => ImGuiKey.Semicolon,
        GlfwKey.Equal => ImGuiKey.Equal,
        GlfwKey.LeftBracket => ImGuiKey.LeftBracket,
        GlfwKey.Backslash => ImGuiKey.Backslash,
        GlfwKey.RightBracket => ImGuiKey.RightBracket,
        GlfwKey.GraveAccent => ImGuiKey.GraveAccent,
        GlfwKey.CapsLock => ImGuiKey.CapsLock,
        GlfwKey.ScrollLock => ImGuiKey.ScrollLock,
        GlfwKey.NumLock => ImGuiKey.NumLock,
        GlfwKey.PrintScreen => ImGuiKey.PrintScreen,
        GlfwKey.Pause => ImGuiKey.Pause,
        GlfwKey.LeftShift => ImGuiKey.LeftShift,
        GlfwKey.LeftControl => ImGuiKey.LeftCtrl,
        GlfwKey.LeftAlt => ImGuiKey.LeftAlt,
        GlfwKey.LeftSuper => ImGuiKey.LeftSuper,
        GlfwKey.RightShift => ImGuiKey.RightShift,
        GlfwKey.RightControl => ImGuiKey.RightCtrl,
        GlfwKey.RightAlt => ImGuiKey.RightAlt,
        GlfwKey.RightSuper => ImGuiKey.RightSuper,
        GlfwKey.Menu => ImGuiKey.Menu,
        >= GlfwKey.Key0 and <= GlfwKey.Key9 => ImGuiKey.Key0 + (key - GlfwKey.Key0),
        >= GlfwKey.A and <= GlfwKey.Z => ImGuiKey.A + (key - GlfwKey.A),
        >= GlfwKey.F1 and <= GlfwKey.F12 => ImGuiKey.F1 + (key - GlfwKey.F1),
        >= GlfwKey.Kp0 and <= GlfwKey.Kp9 => ImGuiKey.Keypad0 + (key - GlfwKey.Kp0),
        GlfwKey.KpDecimal => ImGuiKey.KeypadDecimal,
        GlfwKey.KpDivide => ImGuiKey.KeypadDivide,
        GlfwKey.KpMultiply => ImGuiKey.KeypadMultiply,
        GlfwKey.KpSubtract => ImGuiKey.KeypadSubtract,
        GlfwKey.KpAdd => ImGuiKey.KeypadAdd,
        GlfwKey.KpEnter => ImGuiKey.KeypadEnter,
        GlfwKey.KpEqual => ImGuiKey.KeypadEqual,
        _ => ImGuiKey.None
    };
}
