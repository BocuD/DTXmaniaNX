using System.Numerics;
using Hexa.NET.ImGui;
using SharpDX.Direct3D9;

namespace DTXMania.UI;

public class GameWindow
{
    public static void Draw(Texture gameRenderTargetTexture)
    {
        // Draw the game window
        ImGui.Begin("Game Window");
        
        // Display the game render target texture
        ImTextureID textureID = new(gameRenderTargetTexture.NativePointer);
        ImGui.Image(textureID, new Vector2(1280, 720));
        
        ImGui.End();
    }
}