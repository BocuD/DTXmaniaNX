using System.Numerics;
using Hexa.NET.ImGui;
using Vector2 = System.Numerics.Vector2;

namespace DTXMania.UI.Inspector;

public class GameWindow
{
    public static Viewport viewport { get; } = new();

    public static Vector2 DesiredRenderSize => viewport.desiredRenderSize;

    public static void Draw(ImTextureID? gameTextureId, Vector2 gameTextureSize)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.Begin("Game Window", ImGuiWindowFlags.NoFocusOnAppearing);

        viewport.Draw("GameViewport", gameTextureId, gameTextureSize, ImGui.GetContentRegionAvail());

        ImGui.End();
        ImGui.PopStyleVar();
    }
}
