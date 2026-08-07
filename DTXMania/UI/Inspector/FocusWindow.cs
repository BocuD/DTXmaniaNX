using Hexa.NET.ImGui;

namespace DTXMania.UI.Inspector;

/// <summary>
/// The focus stack as it stands, top first. Which handler is reading input is otherwise invisible, and
/// input that goes to the wrong place looks exactly like input that does nothing.
/// </summary>
public static class FocusWindow
{
    public static void Draw()
    {
        ImGui.Begin("Focus", ImGuiWindowFlags.NoFocusOnAppearing);

        IReadOnlyList<IUIInputHandler> stack = UIFocus.Stack;

        if (stack.Count == 0)
        {
            ImGui.TextDisabled("nothing has focus");
            ImGui.End();
            return;
        }

        for (int i = stack.Count - 1; i >= 0; i--)
        {
            IUIInputHandler handler = stack[i];

            if (i == stack.Count - 1)
            {
                ImGui.TextColored(new System.Numerics.Vector4(0.4f, 1.0f, 0.4f, 1.0f), $"{i}  {handler.FocusName}");
            }
            else
            {
                ImGui.TextDisabled($"{i}  {handler.FocusName}");
            }

            ImGui.SameLine();
            ImGui.TextDisabled($"({handler.GetType().Name})");
        }

        ImGui.End();
    }
}
