using DTXMania.UI.Skin;
using DTXMania.UI.Text;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Drawable;

public partial class UIPaddedNumber
{
    public override void DrawInspector()
    {
        base.DrawInspector();

        if (!ImGui.CollapsingHeader("Padded Number"))
        {
            return;
        }

        int shown = (int)Math.Clamp(value, int.MinValue, int.MaxValue);
        if (ImGui.InputInt("Value", ref shown))
        {
            value = shown;
        }

        if (ImGui.InputInt("Padding", ref padding))
        {
            padding = Math.Clamp(padding, 1, 18);
        }

        Inspector.ResourceEditor.Draw("Font", ResourceType.Font, font, chosen => font = chosen);

        if (ImGui.InputFloat("Font Size", ref fontSize, 1f, 4f, "%.2f"))
        {
            fontSize = MathF.Max(fontSize, 1f);
        }

        ImGui.InputFloat("Letter Spacing", ref letterSpacing, 0.1f, 1f, "%.2f");
        ImGui.InputFloat("Digit Gap", ref digitGap, 0.1f, 1f, "%.2f");
        ImGui.InputFloat("Weight", ref weight, 0.05f, 0.25f, "%.2f");

        if (ImGui.InputFloat("Horizontal Scale", ref horizontalScale, 0.01f, 0.1f, "%.3f"))
        {
            horizontalScale = MathF.Max(horizontalScale, 0.1f);
        }

        if (Inspector.Inspector.Inspect("Texture Padding", ref texturePadding))
        {
            texturePadding = System.Numerics.Vector2.Max(texturePadding, System.Numerics.Vector2.Zero);
        }

        bool isBold = style.HasFlag(UiTextStyle.Bold);
        if (ImGui.Checkbox("Bold", ref isBold))
        {
            style = isBold ? style | UiTextStyle.Bold : style & ~UiTextStyle.Bold;
        }

        bool isItalic = style.HasFlag(UiTextStyle.Italic);
        if (ImGui.Checkbox("Italic", ref isItalic))
        {
            style = isItalic ? style | UiTextStyle.Italic : style & ~UiTextStyle.Italic;
        }

        Inspector.Inspector.Inspect("Number Color", ref numColor);
        Inspector.Inspector.Inspect("Padding Color", ref padColor);
    }
}
