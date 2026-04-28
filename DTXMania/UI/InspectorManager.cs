using System.Drawing;
using System.Numerics;
using DTXMania.UI.Drawable;
using DTXMania.UI.Inspector;
using DTXMania.UI.OpenGL;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Vector2 = System.Numerics.Vector2;

namespace DTXMania.UI;

public static class InspectorManager
{
    public static Inspector.Inspector inspector { get; private set; }
    public static HierarchyWindow hierarchyWindow { get; private set; }
    public static TextureInspector textureInspector { get; private set; }
    public static LogWindow logWindow { get; } = new();

    public static bool inspectorEnabled = false;
    public static bool logWindowEnabled = false;

    public static ImDrawListPtr gizmoDrawList;
    public static Rectangle gizmoRect;

    private static Matrix4x4 view = Matrix4x4.Identity;

    public static string toRemove = string.Empty;
    public static UIDrawable? toRemoveDrawable => DrawableTracker.GetDrawable(toRemove);

    private class Window(string name, Action draw)
    {
        public string name = name;
        public bool enabled = true;
        public Action draw = draw;
    }
    
    private static readonly List<Window> windows = [];
    
    static InspectorManager()
    {
        inspector = new Inspector.Inspector();
        hierarchyWindow = new HierarchyWindow();
        
        windows.Add(new Window("Inspector", () => inspector.Draw()));
        windows.Add(new Window("Hierarchy", () => hierarchyWindow.Draw()));
        windows.Add(new Window("Drawable Tracker", () => DrawableTracker.DrawWindow()));
        windows.Add(new Window("Textures", () => textureInspector.DrawWindow()));
        windows.Add(new Window("Game Status", () => GameStatus.Draw()));
        windows.Add(new Window("Display Controls", () => DisplayControlsWindow.Draw()));
    }

    public static void Draw(bool drawGameWindow, ImTextureID? gameTextureId, Vector2 gameTextureSize)
    {
        if (!string.IsNullOrWhiteSpace(toRemove))
        {
            if (Inspector.Inspector.inspectorTarget == toRemove)
            {
                Inspector.Inspector.inspectorTarget = string.Empty;
            }

            if (toRemoveDrawable?.parent != null)
            {
                toRemoveDrawable.parent.RemoveChild(toRemoveDrawable);
            }

            toRemoveDrawable?.Dispose();
            toRemove = string.Empty;
        }

        if (textureInspector == null)
        {
            textureInspector = new TextureInspector(OpenGlUi.Renderer, OpenGlUi.Renderer.GetTrackedTextures());
        }
        
        if (ImGui.GetIO().KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.I))
        {
            inspectorEnabled = !inspectorEnabled;
        }
        
        if (ImGui.GetIO().KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.L))
        {
            logWindowEnabled = !logWindowEnabled;
        }

        if (inspectorEnabled)
        {
            ImGuiDockNodeFlags flags = ImGuiDockNodeFlags.PassthruCentralNode;
            ImGui.DockSpaceOverViewport(ImGui.GetMainViewport(), flags);
        }

        UIDrawable? selectedDrawable = null;
        if (!string.IsNullOrEmpty(Inspector.Inspector.inspectorTarget))
        {
            selectedDrawable = DrawableTracker.GetDrawable(Inspector.Inspector.inspectorTarget);
        }

        if (drawGameWindow)
        {
            var windowInfo = GameWindow.Draw(gameTextureId, gameTextureSize);
            gizmoRect = windowInfo.rect;
            gizmoDrawList = windowInfo.drawList;
            view = GameWindow.GetViewMatrix();
        }
        else
        {
            ImGuiViewportPtr mainViewport = ImGui.GetMainViewport();
            gizmoRect = new Rectangle(
                (int)mainViewport.Pos.X,
                (int)mainViewport.Pos.Y,
                (int)MathF.Max(mainViewport.Size.X, 1f),
                (int)MathF.Max(mainViewport.Size.Y, 1f));
            gizmoDrawList = ImGui.GetBackgroundDrawList();
            view = Matrix4x4.Identity;
        }

        ImGuizmo.SetImGuiContext(ImGui.GetCurrentContext());
        ImGuizmo.BeginFrame();
        ImGuizmo.Enable(true);
        ImGuizmo.SetOrthographic(true);
        ImGuizmo.SetDrawlist(gizmoDrawList);
        ImGuizmo.SetRect(gizmoRect.X, gizmoRect.Y, gizmoRect.Width, gizmoRect.Height);

        if (inspectorEnabled)
        {
            DrawMenuBar();

            foreach (var window in windows)
            {
                if (window.enabled)
                {
                    window.draw();
                }
            }
        }

        if (logWindowEnabled)
        {
            logWindow.DrawWindow();
        }

        selectedDrawable?.DrawTransformGizmo();
    }

    private static void DrawMenuBar()
    {
        //draw menu bar
        ImGui.BeginMainMenuBar();

        if (ImGui.BeginMenu("Window"))
        {
            for (int index = 0; index < windows.Count; index++)
            {
                Window window = windows[index];
                if (ImGui.MenuItem(window.name, window.enabled))
                {
                    window.enabled = !window.enabled;
                }
            }

            ImGui.EndMenu();
        }
        
        ImGui.EndMainMenuBar();
    }

    public static void DrawGizmoPoint(Vector2 point, float radius, uint color, float thickness = 1.0f)
    {
        Vector2 transformed = Vector2.Transform(point, view);
        transformed += new Vector2(gizmoRect.X, gizmoRect.Y);
        gizmoDrawList.AddCircle(new Vector2(transformed.X, transformed.Y), radius, color, 12, thickness);
    }

    public static void DrawGizmoLine(Vector2 start, Vector2 end, uint color)
    {
        Vector2 startTransformed = Vector2.Transform(start, view);
        Vector2 endTransformed = Vector2.Transform(end, view);
        startTransformed += new Vector2(gizmoRect.X, gizmoRect.Y);
        endTransformed += new Vector2(gizmoRect.X, gizmoRect.Y);
        gizmoDrawList.AddLine(new Vector2(startTransformed.X, startTransformed.Y), new Vector2(endTransformed.X, endTransformed.Y), color);
    }

    public static void DrawGizmoQuad(Vector2 topLeft, Vector2 topRight, Vector2 bottomLeft, Vector2 bottomRight, uint color)
    {
        DrawGizmoLine(topLeft, topRight, color);
        DrawGizmoLine(topRight, bottomRight, color);
        DrawGizmoLine(bottomRight, bottomLeft, color);
        DrawGizmoLine(bottomLeft, topLeft, color);
    }

    public static Matrix4x4 GetViewMatrix()
    {
        return view;
    }
}
