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

    private static bool inspectorEnabled = true;
    
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
    
    public static void DrawGizmoPoint(Vector2 point, uint color)
    {
        //transform from world space to screen space
        var m = GameWindow.GetViewMatrix();
        Vector2 transformed = Vector2.Transform(point, m);
        transformed += new Vector2(gizmoRect.X, gizmoRect.Y);
        
        gizmoDrawList.AddCircle(new Vector2(transformed.X, transformed.Y), 5, color);
    }
    
    public static void DrawGizmoPoint(SharpDX.Vector2 point, uint color)
    {
        DrawGizmoPoint(new Vector2(point.X, point.Y), color);
    }
    
    public static void DrawGizmoLine(Vector2 start, Vector2 end, uint color)
    {
        var m = GameWindow.GetViewMatrix();
        Vector2 startTransformed = Vector2.Transform(start, m);
        Vector2 endTransformed = Vector2.Transform(end, m);
        
        startTransformed += new Vector2(gizmoRect.X, gizmoRect.Y);
        endTransformed += new Vector2(gizmoRect.X, gizmoRect.Y);
        gizmoDrawList.AddLine(new Vector2(startTransformed.X, startTransformed.Y), new Vector2(endTransformed.X, endTransformed.Y), color);
    }
    
    public static void DrawGizmoLine(SharpDX.Vector2 start, SharpDX.Vector2 end, uint color)
    {
        DrawGizmoLine(new Vector2(start.X, start.Y), new Vector2(end.X, end.Y), color);
    }
    
    public static void DrawGizmoQuad(Vector2 topLeft, Vector2 topRight, Vector2 bottomLeft, Vector2 bottomRight, uint color)
    {
        DrawGizmoLine(topLeft, topRight, color);
        DrawGizmoLine(topRight, bottomRight, color);
        DrawGizmoLine(bottomRight, bottomLeft, color);
        DrawGizmoLine(bottomLeft, topLeft, color);
    }

    public static void DrawGizmoQuad(SharpDX.Vector2 topLeft, SharpDX.Vector2 topRight, SharpDX.Vector2 bottomLeft,
        SharpDX.Vector2 bottomRight, uint color)
    {
        DrawGizmoQuad(new Vector2(topLeft.X, topLeft.Y),
            new Vector2(topRight.X, topRight.Y),
            new Vector2(bottomLeft.X, bottomLeft.Y),
            new Vector2(bottomRight.X, bottomRight.Y),
            color);
    }
}