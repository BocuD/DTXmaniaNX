using DTXMania.Core;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Inspector;

public class GameStatus
{
    private static bool demoWindowShown = false;
    public static bool preventGameKeyboardInput = false;

    public static void Draw()
    {
        ImGuiIOPtr io = ImGui.GetIO();

        ImGui.Begin("Game State", ImGuiWindowFlags.NoFocusOnAppearing);

        ImGui.Text("Capturing input: " + (io.WantCaptureMouse ? "Mouse " : "") + (io.WantCaptureKeyboard ? "Keyboard" : ""));

        if (ImGui.CollapsingHeader("Game State"))
        {
            ImGui.Text("Current Stage: " + CDTXMania.StageManager.rCurrentStage.GetType());

            ImGui.Checkbox("Prevent game keyboard input", ref preventGameKeyboardInput);

            ImGui.Checkbox("Prevent stage transitions", ref StageManager.preventStageChanges);
        }

        if (ImGui.CollapsingHeader("Other"))
        {
            if (ImGui.Button("Toggle Demo Window"))
            {
                demoWindowShown = !demoWindowShown;
            }
        }

        ImGui.End();

        if (demoWindowShown)
        {
            ImGui.ShowDemoWindow(ref demoWindowShown);
        }
    }
}
