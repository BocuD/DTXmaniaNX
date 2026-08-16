using System.Numerics;
using DTXMania.Core;
using Hexa.NET.ImGui;
using Vector2 = System.Numerics.Vector2;

namespace DTXMania.UI.Inspector;

public sealed class GameWindow
{
    public Viewport viewport { get; } = new();

    public bool enabled;

    //null renders at the size of the viewport, which is what the game does when it fills the window
    public float? renderScale;

    public bool fit = true;

    public Vector2 renderSize => renderScale is { } scale
        ? new Vector2(GameWindowSize.Width, GameWindowSize.Height) * scale
        : viewport.desiredRenderSize;

    public void Draw(ImTextureID? gameTextureId, Vector2 gameTextureSize)
    {
        ImGui.SetNextWindowSize(new Vector2(960, 640), ImGuiCond.FirstUseEver);

        //a collapsed window has no content region to measure, so the target keeps the size it had
        if (!ImGui.Begin("Game Window", ref enabled, ImGuiWindowFlags.NoFocusOnAppearing))
        {
            ImGui.End();
            return;
        }

        DrawToolbar(gameTextureSize);

        viewport.renderScale = UICanvas.ScaleFor(gameTextureSize);

        if (fit)
        {
            viewport.FitTo(gameTextureSize, viewport.desiredRenderSize);
        }

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        viewport.Draw("GameViewport", gameTextureId, gameTextureSize, ImGui.GetContentRegionAvail());
        ImGui.PopStyleVar();

        if (viewport.wasAdjustedByUser)
        {
            fit = false;
        }

        ImGui.End();
    }

    private void DrawToolbar(Vector2 gameTextureSize)
    {
        RenderScalePicker.Draw("Scale", ref renderScale);

        ImGui.SameLine();
        ImGui.Checkbox("Fit", ref fit);

        ImGui.SameLine();
        if (ImGui.Button("Center"))
        {
            fit = false;
            viewport.CenterOn(viewport.desiredRenderSize);
        }

        ImGui.SameLine();
        ImGui.TextDisabled($"{(int)gameTextureSize.X} x {(int)gameTextureSize.Y}");
    }
}
