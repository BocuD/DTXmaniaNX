using DTXMania.Core;
using Hexa.NET.ImGui;

namespace DTXMania.UI;

public class GameStatus
{
    public static void Draw()
    {
        ImGui.Begin("Game Status");
        ImGui.Text("Current Stage: " + CDTXMania.rCurrentStage.GetType());
        
        InspectorManager.hierarchyWindow.target = CDTXMania.rCurrentStage.ui;
        
        ImGui.End();
    }
}