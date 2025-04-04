using DTXMania.Core;
using DTXUIRenderer;
using Hexa.NET.ImGui;
using SharpDX.Direct3D9;
using SlimDX.DirectInput;

namespace DTXMania.UI;

public static class InspectorManager
{
    public static Inspector inspector { get; private set; }
    public static HierarchyWindow hierarchyWindow { get; private set; }

    private static bool inspectorEnabled = false;
    
    static InspectorManager()
    {
        inspector = new Inspector();
        hierarchyWindow = new HierarchyWindow();
    }
    
    public static void Draw(bool drawGameWindow, Texture gameSurface)
    {
        if (CDTXMania.InputManager.Keyboard.bKeyPressing(Key.LeftControl) 
            && CDTXMania.InputManager.Keyboard.bKeyPressed(Key.I))
        {
            inspectorEnabled = !inspectorEnabled;
        }
        
        ImGuiDockNodeFlags flags = ImGuiDockNodeFlags.PassthruCentralNode;
        ImGui.DockSpaceOverViewport(ImGui.GetMainViewport(), flags);

        if (drawGameWindow)
        {
            GameWindow.Draw(gameSurface);
        }
        
        if (inspectorEnabled)
        {
            inspector.Draw();
            hierarchyWindow.Draw();
        }
    }
}