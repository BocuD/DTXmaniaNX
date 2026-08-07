using System.Drawing;
using System.Numerics;
using Hexa.NET.ImGui;
using Vector2 = System.Numerics.Vector2;

namespace DTXMania.UI.Inspector;

/// <summary>
/// A pannable, zoomable region showing one render target's texture. Every window that shows a rendered
/// tree owns one: the pan/zoom is what maps a drawable's position onto the screen, so a gizmo can only be
/// placed correctly by the viewport the drawable was rendered in.
/// </summary>
public sealed class Viewport
{
    public float scale { get; private set; } = 1.0f;
    public Vector2 translation { get; private set; }

    //the size the content region wants to be rendered at, read by whoever sizes the render target
    public Vector2 desiredRenderSize { get; private set; } = new(1280, 720);

    //where the content ended up on screen, and the list to draw overlays into
    public Rectangle rect { get; private set; }
    public ImDrawListPtr drawList { get; private set; }

    //for content authored around a point rather than filling the target: axes through that point, drawn
    //behind the image so they show through wherever it is transparent
    public bool showOrigin;
    public Vector2 origin;

    private Vector2 mouseDragStart;
    private bool isDragging;

    /// <summary>Draws the texture inside a child region of the current window. Call between Begin/End.</summary>
    public unsafe void Draw(string id, ImTextureID? textureId, Vector2 textureSize, Vector2 availableSize)
    {
        availableSize = new Vector2(MathF.Max(availableSize.X, 1f), MathF.Max(availableSize.Y, 1f));

        ImGuiWindowFlags viewportFlags = ImGuiWindowFlags.NoScrollbar |
                                         ImGuiWindowFlags.NoScrollWithMouse |
                                         ImGuiWindowFlags.NoMove |
                                         ImGuiWindowFlags.NoNav;

        ImGui.BeginChild(id, availableSize, ImGuiChildFlags.None, viewportFlags);
        drawList = ImGui.GetWindowDrawList();

        Vector2 renderOffset = ImGui.GetCursorScreenPos();
        Vector2 size = ImGui.GetContentRegionAvail();
        desiredRenderSize = new Vector2(MathF.Max(size.X, 1f), MathF.Max(size.Y, 1f));

        HandleMouse(renderOffset);
        rect = new Rectangle((int)renderOffset.X, (int)renderOffset.Y, (int)size.X, (int)size.Y);

        if (textureId is { } texture)
        {
            Vector2 topLeft = renderOffset + translation;
            Vector2 bottomRight = topLeft + textureSize * scale;

            if (showOrigin)
            {
                DrawOrigin(topLeft, bottomRight);
            }

            ImTextureRef textureRef = new((ImTextureData*)null, texture);
            drawList.AddImage(textureRef, topLeft, bottomRight, new Vector2(0, 1), new Vector2(1, 0));
        }
        else
        {
            ImGui.TextUnformatted("No render target available.");
        }

        ImGui.EndChild();

        DrawOverlay(id);
    }

    public Matrix4x4 GetViewMatrix()
    {
        return Matrix4x4.CreateScale(scale, scale, 1.0f) *
               Matrix4x4.CreateTranslation(new Vector3(translation, 0));
    }

    public Vector2 WorldToScreen(Vector2 worldPos)
    {
        Vector3 transformed = Vector3.Transform(new Vector3(worldPos.X, worldPos.Y, 0), GetViewMatrix());
        return new Vector2(transformed.X, transformed.Y);
    }

    public Vector2 ScreenToWorld(Vector2 screenPos)
    {
        Matrix4x4.Invert(GetViewMatrix(), out Matrix4x4 inverse);
        Vector3 world = Vector3.Transform(new Vector3(screenPos.X, screenPos.Y, 0), inverse);
        return new Vector2(world.X, world.Y);
    }

    public void ResetView()
    {
        translation = Vector2.Zero;
        scale = 1.0f;
        isDragging = false;
    }

    /// <summary>Centres content of the given size in a region, at 1:1 zoom.</summary>
    public void CenterOn(Vector2 contentSize, Vector2 regionSize)
    {
        scale = 1.0f;
        translation = (regionSize - contentSize) * 0.5f;
        isDragging = false;
    }

    private void HandleMouse(Vector2 renderOffset)
    {
        if (ImGui.IsWindowHovered())
        {
            float scroll = ImGui.GetIO().MouseWheel;
            if (scroll != 0)
            {
                Vector2 mouseScreenPos = ImGui.GetMousePos() - renderOffset;
                Vector2 mouseWorldBeforeZoom = (mouseScreenPos - translation) / scale;

                scale = Math.Clamp(scale * (1 + scroll * 0.1f), 0.1f, 10f);

                Vector2 mouseWorldAfterZoom = (mouseScreenPos - translation) / scale;
                translation += (mouseWorldAfterZoom - mouseWorldBeforeZoom) * scale;
            }

            if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                mouseDragStart = ImGui.GetMousePos() - renderOffset;
                isDragging = true;
            }
        }

        if (!isDragging)
        {
            return;
        }

        Vector2 currentMouse = ImGui.GetMousePos() - renderOffset;
        translation += currentMouse - mouseDragStart;
        mouseDragStart = currentMouse;

        if (ImGui.IsMouseReleased(ImGuiMouseButton.Right))
        {
            isDragging = false;
        }
    }

    private void DrawOrigin(Vector2 canvasTopLeft, Vector2 canvasBottomRight)
    {
        const uint canvasColor = 0xFF221E1A;
        const uint axisColor = 0x66FFFFFF;

        drawList.AddRectFilled(canvasTopLeft, canvasBottomRight, canvasColor);
        drawList.AddRect(canvasTopLeft, canvasBottomRight, axisColor);

        Vector2 point = canvasTopLeft + origin * scale;
        drawList.AddLine(new Vector2(rect.X, point.Y), new Vector2(rect.X + rect.Width, point.Y), axisColor);
        drawList.AddLine(new Vector2(point.X, rect.Y), new Vector2(point.X, rect.Y + rect.Height), axisColor);
    }

    private void DrawOverlay(string id)
    {
        float zoomOffset = scale - 1.0f;
        if (translation == Vector2.Zero && MathF.Abs(zoomOffset) < 0.0001f)
        {
            return;
        }

        ImGui.SetCursorScreenPos(new Vector2(rect.X + 12, rect.Y + 12));
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

        if (ImGui.BeginChild($"{id}Overlay", new Vector2(220, 0), childFlags, flags))
        {
            ImGui.Text($"Offset: {translation.X:F1}, {translation.Y:F1}");
            ImGui.Text($"Zoom: {zoomOffset:F2}");

            if (ImGui.Button("Reset View"))
            {
                ResetView();
            }
        }

        ImGui.EndChild();
    }
}
