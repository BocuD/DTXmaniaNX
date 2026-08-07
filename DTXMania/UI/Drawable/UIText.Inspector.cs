using System.Numerics;
using DTXMania.UI.Skin;
using DTXMania.UI.Text;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Drawable;

public partial class UIText
{
    public override void DrawInspector()
    {
        base.DrawInspector();

        if (!ImGui.CollapsingHeader("UIText"))
        {
            return;
        }

        //a binding on "text" overwrites whatever is typed here on the next frame, so say so rather than
        //letting the field look broken
        string boundTo = TextBindingSource();

        if (boundTo.Length > 0)
        {
            ImGui.LabelText("String", $"bound to {boundTo}");
        }
        else if (ImGui.InputTextMultiline("String", ref _text, 256))
        {
            _dirty = true;
        }

        Inspector.ResourceEditor.Draw("Font", ResourceType.Font, font, chosen =>
        {
            font = chosen;
            _dirty = true;
        });

        if (ImGui.InputText("Fallback Family", ref fontFamily, 256))
        {
            _dirty = true;
        }

        if (ImGui.InputFloat("Font Size", ref fontSize, 1f, 4f, "%.1f"))
        {
            fontSize = MathF.Max(fontSize, 1f);
            _dirty = true;
        }

        if (ImGui.InputFloat("Outline Width", ref outlineWidth, 0.5f, 2f, "%.1f"))
        {
            outlineWidth = MathF.Max(outlineWidth, 0f);
            _dirty = true;
        }

        if (Inspector.Inspector.Inspect("Texture Padding", ref texturePadding))
        {
            texturePadding = Vector2.Max(texturePadding, new Vector2(0f));
            _dirty = true;
        }

        if (ImGui.InputFloat("Line Spacing", ref lineSpacing, 0.05f, 0.25f, "%.2f"))
        {
            lineSpacing = MathF.Max(lineSpacing, 0.25f);
            _dirty = true;
        }

        bool isBold = style.HasFlag(UiTextStyle.Bold);
        if (ImGui.Checkbox("Bold", ref isBold))
        {
            style = isBold ? style | UiTextStyle.Bold : style & ~UiTextStyle.Bold;
            _dirty = true;
        }

        bool isItalic = style.HasFlag(UiTextStyle.Italic);
        if (ImGui.Checkbox("Italic", ref isItalic))
        {
            style = isItalic ? style | UiTextStyle.Italic : style & ~UiTextStyle.Italic;
            _dirty = true;
        }

        bool isUnderline = style.HasFlag(UiTextStyle.Underline);
        if (ImGui.Checkbox("Underline", ref isUnderline))
        {
            style = isUnderline ? style | UiTextStyle.Underline : style & ~UiTextStyle.Underline;
            _dirty = true;
        }

        if (Inspector.Inspector.Inspect("Alignment", ref alignment))
        {
            _dirty = true;
        }

        if (Inspector.Inspector.Inspect("Render Backend", ref renderBackend))
        {
            _dirty = true;
        }

        if (Inspector.Inspector.Inspect("Fill Color", ref fillColor))
        {
            _dirty = true;
        }

        if (Inspector.Inspector.Inspect("Fill Gradient", ref fillGradientMode))
        {
            _dirty = true;
        }

        if (fillGradientMode != UiTextGradientMode.None)
        {
            if (Inspector.Inspector.Inspect("Fill Gradient Top", ref fillGradientTopColor))
            {
                _dirty = true;
            }

            if (Inspector.Inspector.Inspect("Fill Gradient Bottom", ref fillGradientBottomColor))
            {
                _dirty = true;
            }
        }

        if (Inspector.Inspector.Inspect("Outline Color", ref outlineColor))
        {
            _dirty = true;
        }

        if (Inspector.Inspector.Inspect("Outline Gradient", ref outlineGradientMode))
        {
            _dirty = true;
        }

        if (outlineGradientMode != UiTextGradientMode.None)
        {
            if (Inspector.Inspector.Inspect("Outline Gradient Top", ref outlineGradientTopColor))
            {
                _dirty = true;
            }

            if (Inspector.Inspector.Inspect("Outline Gradient Bottom", ref outlineGradientBottomColor))
            {
                _dirty = true;
            }
        }

        if (ImGui.Checkbox("Antialias", ref antialias))
        {
            _dirty = true;
        }

        if (ImGui.Checkbox("Subpixel", ref subpixelText))
        {
            _dirty = true;
        }

        if (ImGui.Button("Rebuild Text Texture"))
        {
            _dirty = true;
            RenderTexture();
        }
    }
}