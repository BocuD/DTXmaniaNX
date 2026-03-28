using System.Drawing;
using System.Numerics;
using Hexa.NET.ImGui;
using Vector2 = System.Numerics.Vector2;

namespace DTXMania.UI.Inspector;

public class GameWindow
{
    public static Vector2 Translation => translation;
    public static float Scale => scale;
    public static Vector2 DesiredRenderSize => desiredRenderSize;

    private static float scale = 1.0f;
    private static Vector2 translation = Vector2.Zero;
    private static Vector2 mouseDragStart = Vector2.Zero;
    private static bool isDragging;
    private static Vector2 desiredRenderSize = new(1280, 720);

    public static unsafe (ImDrawListPtr drawList, Rectangle rect) Draw(ImTextureID? gameTextureId, Vector2 gameTextureSize)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.Begin("Game Window");
        Vector2 availableSize = ImGui.GetContentRegionAvail();
        availableSize = new Vector2(MathF.Max(availableSize.X, 1f), MathF.Max(availableSize.Y, 1f));

        ImGuiWindowFlags viewportFlags = ImGuiWindowFlags.NoScrollbar |
                                         ImGuiWindowFlags.NoScrollWithMouse |
                                         ImGuiWindowFlags.NoMove |
                                         ImGuiWindowFlags.NoNav;

        ImGui.BeginChild("GameViewport", availableSize, ImGuiChildFlags.None, viewportFlags);
        ImDrawListPtr windowDrawList = ImGui.GetWindowDrawList();

        Vector2 renderOffset = ImGui.GetCursorScreenPos();
        Vector2 size = ImGui.GetContentRegionAvail();
        desiredRenderSize = new Vector2(MathF.Max(size.X, 1f), MathF.Max(size.Y, 1f));

        if (ImGui.IsWindowHovered())
        {
            float scroll = ImGui.GetIO().MouseWheel;
            if (scroll != 0)
            {
                Vector2 mouseScreenPos = ImGui.GetMousePos() - renderOffset;
                Vector2 mouseWorldBeforeZoom = (mouseScreenPos - translation) / scale;

                scale *= 1 + scroll * 0.1f;
                scale = Math.Clamp(scale, 0.1f, 10f);

                Vector2 mouseWorldAfterZoom = (mouseScreenPos - translation) / scale;
                translation += (mouseWorldAfterZoom - mouseWorldBeforeZoom) * scale;
            }

            if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                mouseDragStart = ImGui.GetMousePos() - renderOffset;
                isDragging = true;
            }
        }

        if (isDragging)
        {
            Vector2 currentMouse = ImGui.GetMousePos() - renderOffset;
            Vector2 dragDelta = currentMouse - mouseDragStart;

            translation += dragDelta;
            mouseDragStart = currentMouse;

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Right))
            {
                isDragging = false;
            }
        }

        if (gameTextureId is { } textureId)
        {
            Vector2 topLeft = translation;
            Vector2 bottomRight = translation + gameTextureSize * scale;
            ImTextureRef textureRef = new((ImTextureData*)null, textureId);
            windowDrawList.AddImage(textureRef, renderOffset + topLeft, renderOffset + bottomRight, new Vector2(0, 1), new Vector2(1, 0));
        }
        else
        {
            ImGui.TextUnformatted("No game render target available.");
        }

        Rectangle viewportRect = new(
            (int)renderOffset.X,
            (int)renderOffset.Y,
            (int)size.X,
            (int)size.Y);

        ImGui.EndChild();

        DrawViewOverlay(viewportRect);

        ImGui.End();
        ImGui.PopStyleVar();

        return (windowDrawList, viewportRect);
    }

    public static Matrix4x4 GetViewMatrix()
    {
        return Matrix4x4.CreateScale(scale, scale, 1.0f) *
               Matrix4x4.CreateTranslation(new Vector3(translation, 0));
    }

    public static Vector2 WorldToScreen(Vector2 worldPos)
    {
        Matrix4x4 view = GetViewMatrix();
        Vector3 transformed = Vector3.Transform(new Vector3(worldPos.X, worldPos.Y, 0), view);
        return new Vector2(transformed.X, transformed.Y);
    }

    public static Vector2 ScreenToWorld(Vector2 screenPos)
    {
        Matrix4x4 view = GetViewMatrix();
        Matrix4x4.Invert(view, out Matrix4x4 inv);
        Vector3 world = Vector3.Transform(new Vector3(screenPos.X, screenPos.Y, 0), inv);
        return new Vector2(world.X, world.Y);
    }

    private static void DrawViewOverlay(Rectangle viewportRect)
    {
        float zoomOffset = scale - 1.0f;
        if (translation == Vector2.Zero && MathF.Abs(zoomOffset) < 0.0001f)
        {
            return;
        }

        Vector2 overlayPosition = new(viewportRect.X + 12, viewportRect.Y + 12);
        ImGui.SetCursorScreenPos(overlayPosition);
        ImGui.SetNextWindowBgAlpha(0.75f);

        ImGuiWindowFlags flags = ImGuiWindowFlags.NoDecoration |
                                 ImGuiWindowFlags.NoSavedSettings |
                                 ImGuiWindowFlags.NoFocusOnAppearing |
                                 ImGuiWindowFlags.NoNav |
                                 ImGuiWindowFlags.NoMove;

        ImGuiChildFlags childFlags = ImGuiChildFlags.Borders |
                                     ImGuiChildFlags.AlwaysAutoResize |
                                     ImGuiChildFlags.AutoResizeX |
                                     ImGuiChildFlags.AutoResizeY;

        if (ImGui.BeginChild("GameWindowOverlay", new Vector2(220, 0), childFlags, flags))
        {
            ImGui.Text($"Offset: {translation.X:F1}, {translation.Y:F1}");
            ImGui.Text($"Zoom: {zoomOffset:F2}");

            if (ImGui.Button("Reset View"))
            {
                translation = Vector2.Zero;
                scale = 1.0f;
                isDragging = false;
            }
        }

        ImGui.EndChild();
    }
}
