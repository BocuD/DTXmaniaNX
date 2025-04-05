using DTXMania.Core;
using DTXUIRenderer;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using SharpDX;
using SharpDX.Direct3D9;
using SlimDX.DirectInput;
using Vector2 = System.Numerics.Vector2;

namespace DTXMania.UI;

public static class InspectorManager
{
    public static Inspector inspector { get; private set; }
    public static HierarchyWindow hierarchyWindow { get; private set; }

    private static bool inspectorEnabled = false;
    
    public static ImDrawListPtr gizmoDrawList;
    public static Rectangle gizmoRect;
    
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
            //game window will set imguizmo rect and drawlist
            var windowInfo = GameWindow.Draw(gameSurface);
            
            gizmoRect = windowInfo.rect;
            gizmoDrawList = windowInfo.drawList;
        }
        else
        {
            //manually set the rect to the size of imguizmo 
            ImGuizmo.SetDrawlist(gizmoDrawList);
            Vector2 size = ImGui.GetIO().DisplaySize;
            gizmoRect = new Rectangle(0, 0, (int) size.X, (int) size.Y);
            gizmoDrawList = ImGui.GetBackgroundDrawList();
        }
        
        ImGuizmo.SetDrawlist(gizmoDrawList);
        ImGuizmo.SetRect(gizmoRect.X, gizmoRect.Y, gizmoRect.Width, gizmoRect.Height);
        
        if (inspectorEnabled)
        {
            inspector.Draw();
            hierarchyWindow.Draw();
        }
    }
}