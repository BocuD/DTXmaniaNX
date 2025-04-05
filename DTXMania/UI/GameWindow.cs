using System.Numerics;
using DTXMania.Core;
using Hexa.NET.ImGui;
using SharpDX;
using SharpDX.Direct3D9;
using Vector2 = System.Numerics.Vector2;

namespace DTXMania.UI;

public class GameWindow
{
    public static Vector2 Translation => translation;
    public static float Scale => scale;
    
    private static float scale = 1.0f;
    private static Vector2 translation = Vector2.Zero;
    private static Vector2 mouseDragStart = Vector2.Zero;
    private static bool isDragging = false;

    public static (ImDrawListPtr drawList, Rectangle rect) Draw(Texture gameRenderTargetTexture)
    {
        ImGui.Begin("Game Window");
        ImDrawListPtr windowDrawList = ImGui.GetWindowDrawList();

        Vector2 renderOffset = ImGui.GetCursorScreenPos();
        Vector2 size = ImGui.GetContentRegionAvail();

        // Handle zoom input
        if (ImGui.IsWindowHovered())
        {
            float scroll = ImGui.GetIO().MouseWheel;
            if (scroll != 0)
            {
                Vector2 mouseScreenPos = ImGui.GetMousePos();
                Vector2 mouseWorldBeforeZoom = (mouseScreenPos - translation) / scale;

                scale *= 1 + scroll * 0.1f;
                scale = Math.Clamp(scale, 0.1f, 10f);

                Vector2 mouseWorldAfterZoom = (mouseScreenPos - translation) / scale;
                translation += (mouseWorldAfterZoom - mouseWorldBeforeZoom) * scale;
            }

            if (ImGui.IsMouseClicked(ImGuiMouseButton.Middle))
            {
                mouseDragStart = ImGui.GetMousePos();
                isDragging = true;
            }
        }

        if (isDragging)
        {
            Vector2 currentMouse = ImGui.GetMousePos();
            Vector2 dragDelta = currentMouse - mouseDragStart;

            translation += dragDelta;
            mouseDragStart = currentMouse;

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Middle))
            {
                isDragging = false;
            }
        }

        // Texture size
        Vector2 texSize = new(1280, 720);
        ImTextureID textureID = new(gameRenderTargetTexture.NativePointer);

        // Apply transform to corners
        Vector2 topLeft = translation;
        Vector2 bottomRight = translation + texSize * scale;

        unsafe
        {
            ImDrawCallback callback = new((drawList, cmd) =>
            {
                var device = CDTXMania.app.Device;
                device.SetSamplerState(0, SamplerState.MinFilter, TextureFilter.Point);
                device.SetSamplerState(0, SamplerState.MagFilter, TextureFilter.Point);
                device.SetSamplerState(0, SamplerState.MipFilter, TextureFilter.Point);
                device.SetSamplerState(0, SamplerState.AddressU, TextureAddress.Clamp);
                device.SetSamplerState(0, SamplerState.AddressV, TextureAddress.Clamp);
            });
            ImGui.AddCallback(windowDrawList, callback, null);
        }

        windowDrawList.AddImage(textureID, renderOffset + topLeft, renderOffset + bottomRight);

        unsafe
        {
            ImDrawCallback resetCallback = new((drawList, cmd) =>
            {
                var device = CDTXMania.app.Device;
                device.SetSamplerState(0, SamplerState.MinFilter, TextureFilter.Linear);
                device.SetSamplerState(0, SamplerState.MagFilter, TextureFilter.Linear);
                device.SetSamplerState(0, SamplerState.MipFilter, TextureFilter.Linear);
                device.SetSamplerState(0, SamplerState.AddressU, TextureAddress.Wrap);
                device.SetSamplerState(0, SamplerState.AddressV, TextureAddress.Wrap);
            });
            ImGui.AddCallback(windowDrawList, resetCallback, null);
        }

        ImGui.End();

        // Return whatever additional data you need
        return (windowDrawList, new Rectangle(
            (int)renderOffset.X,
            (int)renderOffset.Y,
            (int)size.X,
            (int)size.Y));
    }
    
    public static Matrix4x4 GetViewMatrix()
    {
        // Build a 4x4 matrix that applies scale then translation
        return
            Matrix4x4.CreateScale(scale, scale, 1.0f) *
            Matrix4x4.CreateTranslation(new System.Numerics.Vector3(translation, 0));
    }
    
    public static Vector2 WorldToScreen(Vector2 worldPos)
    {
        Matrix4x4 view = GetViewMatrix();
        System.Numerics.Vector3 transformed = System.Numerics.Vector3.Transform(new System.Numerics.Vector3(worldPos.X, worldPos.Y, 0), view);
        return new Vector2(transformed.X, transformed.Y);
    }
    
    public static Vector2 ScreenToWorld(Vector2 screenPos)
    {
        Matrix4x4 view = GetViewMatrix();
        Matrix4x4.Invert(view, out Matrix4x4 inv);
        System.Numerics.Vector3 world = System.Numerics.Vector3.Transform(new System.Numerics.Vector3(screenPos.X, screenPos.Y, 0), inv);
        return new Vector2(world.X, world.Y);
    }
}