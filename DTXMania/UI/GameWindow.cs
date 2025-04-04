using System.Numerics;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using SharpDX.Direct3D9;

namespace DTXMania.UI;

public class GameWindow
{
    public static Vector2 size = new(1280, 720);
    public static Vector2 position = new(0, 0);
    
    public static void Draw(Texture gameRenderTargetTexture)
    {
        // Draw the game window
        ImGui.Begin("Game Window");
        
        // Display the game render target texture
        ImTextureID textureID = new(gameRenderTargetTexture.NativePointer);
        ImGui.Image(textureID, new Vector2(1280, 720));
        
        //update size and position with actual values
        ImGui.GetWindowSize(ref size);
        ImGui.GetWindowPos(ref position);
        
        ImGuizmo.SetDrawlist(ImGui.GetWindowDrawList());
        ImGuizmo.SetRect(position.X, position.Y, size.X, size.Y);
        
        ImGui.End();
    }
}