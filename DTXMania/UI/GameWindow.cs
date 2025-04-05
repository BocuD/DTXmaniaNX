using Hexa.NET.ImGui;
using SharpDX;
using SharpDX.Direct3D9;
using Vector2 = System.Numerics.Vector2;

namespace DTXMania.UI;

public class GameWindow
{
    private static Vector2 size = new(1280, 720);
    private static Vector2 position = new(0, 0);
    
    public static (ImDrawListPtr drawList, Rectangle rect) Draw(Texture gameRenderTargetTexture)
    {
        // Draw the game window
        ImGui.Begin("Game Window");
        
        // Display the game render target texture
        ImTextureID textureID = new(gameRenderTargetTexture.NativePointer);
        ImGui.Image(textureID, new Vector2(1280, 720));
        
        //get rect of the image
        ImGui.GetItemRectSize(ref size);
        ImGui.GetItemRectMin(ref position);
        
        ImDrawListPtr windowDrawList = ImGui.GetWindowDrawList();
        ImGui.End();

        return (windowDrawList, new Rectangle((int) position.X, (int) position.Y, (int) size.X, (int) size.Y));
    }
}