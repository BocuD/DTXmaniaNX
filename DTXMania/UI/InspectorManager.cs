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
    public static Inspector.Inspector inspector { get; } = new();
    public static HierarchyWindow hierarchyWindow { get; } = new();
    public static SkinEditorWindow skinEditor { get; } = new();
    public static SkinPreviewPanel skinPreview { get; } = new();
    public static TextureInspector textureInspector { get; private set; }
    public static LogWindow logWindow { get; } = new();
    public static GameWindow gameWindow { get; } = new();

    public static bool inspectorEnabled = false;
    public static bool logWindowEnabled = false;

    public static bool rendersGameToWindow => inspectorEnabled && gameWindow.enabled;

    /// <summary>Whether the pointer is over the game window, where ImGui is showing the game rather than
    /// something of its own: a click there belongs to the game.</summary>
    public static bool PointerIsOverGame => rendersGameToWindow && gameWindow.Contains(PointerInput.windowPosition);

    //where the game's pixels are in the window, both ways round: the pointer comes in through one and the
    //IME caret goes out through the other
    public static Vector2 WindowToGame(Vector2 windowPosition)
        => rendersGameToWindow ? gameWindow.ToRenderTarget(windowPosition) : windowPosition;

    public static Vector2 GameToWindow(Vector2 gamePosition)
        => rendersGameToWindow ? gameWindow.ToWindow(gamePosition) : gamePosition;

    public static void ToggleInspector()
    {
        inspectorEnabled = !inspectorEnabled;

        if (!inspectorEnabled)
        {
            Inspector.Inspector.inspectorTarget = DrawableRef.None;
        }
    }

    public static bool WantsImGui => inspectorEnabled || logWindowEnabled;

    public static ImDrawListPtr gizmoDrawList;
    public static Rectangle gizmoRect;

    //the state a window that renders into its own target mid-frame has to put back
    public static Vector2 framebufferSize { get; private set; }
    public static Vector2 gameRenderSize { get; private set; }

    private static Matrix4x4 view = Matrix4x4.Identity;

    public static DrawableRef toRemove = DrawableRef.None;

    private class Window(string name, Action draw, bool defaultShow = false)
    {
        public string name = name;
        public bool enabled = defaultShow;
        public Action draw = draw;
    }
    
    private static readonly List<Window> windows = [];
    
    static InspectorManager()
    {
        windows.Add(new Window("Inspector", () => inspector.Draw()));
        windows.Add(new Window("Hierarchy", () => hierarchyWindow.Draw()));
        windows.Add(new Window("Game Status", () => GameStatus.Draw()));
        windows.Add(new Window("Profiler", () => Profiler.Draw()));
        windows.Add(new Window("Skin Editor", () => skinEditor.Draw()));

        windows.Add(new Window("Focus", () => FocusWindow.Draw()));
        windows.Add(new Window("Textures", () => textureInspector.DrawWindow()));
        windows.Add(new Window("Drawable Tracker", () => DrawableTracker.DrawWindow()));
        windows.Add(new Window("Audio Mixer", () => Core.AudioMixer.DrawWindow()));
        windows.Add(new Window("Display Controls", () => RendererInfo.Draw()));
    }

    //name as shown in the Window menu; unknown names do nothing
    public static void ShowWindow(string name, bool show = true)
    {
        foreach (Window window in windows)
        {
            if (window.name == name)
            {
                window.enabled = show;
                return;
            }
        }
    }

    public static void Draw(bool drawGameWindow, ImTextureID? gameTextureId, Vector2 gameTextureSize, Vector2 defaultFramebufferSize)
    {
        framebufferSize = defaultFramebufferSize;
        gameRenderSize = gameTextureSize;

        if (toRemove.Target is { } removing)
        {
            if (Inspector.Inspector.inspectorTarget.Is(removing))
            {
                Inspector.Inspector.inspectorTarget = DrawableRef.None;
            }

            removing.parent?.RemoveChild(removing);
            removing.Dispose();
        }

        toRemove = DrawableRef.None;

        if (textureInspector == null)
        {
            textureInspector = new TextureInspector(OpenGlRenderer.Instance, OpenGlRenderer.Instance.GetTrackedTextures());
        }
        
        if (inspectorEnabled)
        {
            ImGuiDockNodeFlags flags = ImGuiDockNodeFlags.PassthruCentralNode;
            ImGui.DockSpaceOverViewport(ImGui.GetMainViewport(), flags);
        }

        UIDrawable? selectedDrawable = Inspector.Inspector.inspectorTarget.Target;

        Rectangle gameRect;
        ImDrawListPtr gameDrawList;
        Matrix4x4 gameView;

        if (drawGameWindow)
        {
            gameWindow.Draw(gameTextureId, gameTextureSize);
        }

        if (drawGameWindow && gameWindow.viewport.hasDrawList)
        {
            gameRect = gameWindow.viewport.rect;
            gameDrawList = gameWindow.viewport.drawList;
            gameView = gameWindow.viewport.GetViewMatrix();
        }
        else
        {
            ImGuiViewportPtr mainViewport = ImGui.GetMainViewport();
            gameRect = new Rectangle(
                (int)mainViewport.Pos.X,
                (int)mainViewport.Pos.Y,
                (int)MathF.Max(mainViewport.Size.X, 1f),
                (int)MathF.Max(mainViewport.Size.Y, 1f));
            gameDrawList = ImGui.GetBackgroundDrawList();
            gameView = Matrix4x4.Identity;
        }

        ImGuizmo.SetImGuiContext(ImGui.GetCurrentContext());
        ImGuizmo.BeginFrame();
        ImGuizmo.Enable(true);
        ImGuizmo.SetOrthographic(true);
        SetGizmoTarget(gameRect, gameDrawList, gameView);

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

            //each editor draws the gizmo for a selection inside its own tree, in its own viewport
            ComponentEditor.DrawAll(selectedDrawable);

            ResourceImporter.DrawPending();
        }

        if (logWindowEnabled)
        {
            logWindow.DrawWindow();
        }

        if (ComponentEditor.OwnerOf(selectedDrawable) == null)
        {
            SetGizmoTarget(gameRect, gameDrawList, gameView);
            selectedDrawable?.DrawTransformGizmo();
        }
    }

    /// <summary>Points the gizmos at one viewport: where it is on screen, what to draw into, and how it
    /// maps world positions to that region.</summary>
    public static void SetGizmoTarget(Rectangle rect, ImDrawListPtr drawList, Matrix4x4 viewMatrix)
    {
        gizmoRect = rect;
        gizmoDrawList = drawList;
        view = viewMatrix;

        ImGuizmo.SetDrawlist(drawList);
        ImGuizmo.SetRect(rect.X, rect.Y, rect.Width, rect.Height);
    }

    private static void DrawMenuBar()
    {
        //draw menu bar
        ImGui.BeginMainMenuBar();

        if (ImGui.BeginMenu("Window"))
        {
            //not one of the windows below: opening it is what makes the game render into a target
            if (ImGui.MenuItem("Game Window", gameWindow.enabled))
            {
                gameWindow.enabled = !gameWindow.enabled;
            }

            ImGui.Separator();

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
