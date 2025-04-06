using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Drawable;
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
    
    private static Matrix4x4 view = Matrix4x4.Identity;

    public static UIDrawable? toRemove;
    
    static InspectorManager()
    {
        inspector = new Inspector();
        hierarchyWindow = new HierarchyWindow();
    }
    
    public static void Draw(bool drawGameWindow, Texture gameSurface)
    {
        if (toRemove != null)
        {
            if (Inspector.inspectorTarget == toRemove)
            {
                Inspector.inspectorTarget = null;
            }
            
            toRemove.Dispose();
            toRemove = null;
        }
        
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

            view = GameWindow.GetViewMatrix();
        }
        else
        {
            //manually set the rect to the size of imguizmo 
            ImGuizmo.SetDrawlist(gizmoDrawList);
            Vector2 size = ImGui.GetIO().DisplaySize;
            gizmoRect = new Rectangle(0, 0, (int) size.X, (int) size.Y);
            gizmoDrawList = ImGui.GetBackgroundDrawList();
            
            view = Matrix4x4.Identity;
        }
        
        ImGuizmo.SetDrawlist(gizmoDrawList);
        ImGuizmo.SetRect(gizmoRect.X, gizmoRect.Y, gizmoRect.Width, gizmoRect.Height);
        
        if (inspectorEnabled)
        {
            inspector.Draw();
            hierarchyWindow.Draw();
        }
    }
    
    public static void DrawGizmoPoint(Vector2 point, float radius, uint color, float thickness = 1.0f)
    {
        //transform from world space to screen space
        Vector2 transformed = Vector2.Transform(point, view);
        transformed += new Vector2(gizmoRect.X, gizmoRect.Y);
        
        gizmoDrawList.AddCircle(new Vector2(transformed.X, transformed.Y), radius, color, 12, thickness);
    }
    
    public static void DrawGizmoPoint(SharpDX.Vector2 point, float radius, uint color, float thickness = 1.0f)
    {
        DrawGizmoPoint(new Vector2(point.X, point.Y), radius, color, thickness);
    }
    
    public static void DrawGizmoLine(Vector2 start, Vector2 end, uint color)
    {
        Vector2 startTransformed = Vector2.Transform(start, view);
        Vector2 endTransformed = Vector2.Transform(end, view);
        
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

    public static Matrix4x4 GetViewMatrix()
    {
        return view;
    }
}