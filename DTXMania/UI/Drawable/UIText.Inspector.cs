using DTXMania.Core;
using DTXMania.UI.Skin;
using DTXMania.UI.Text;
using Hexa.NET.ImGui;
using NativeFileDialog.Extended;

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

        if (Inspector.Inspector.Inspect("Text Source", ref textSource))
        {
            _dirty = true;
        }

        switch (textSource)
        {
            case TextSource.String:
            {
                if (ImGui.InputTextMultiline("String", ref text, 256))
                {
                    _dirty = true;
                }

                break;
            }
            case TextSource.Dynamic:
            {
                string[] sources = CDTXMania.StageManager.rCurrentStage.dynamicStringSources.Keys.ToArray();
                int selectedIndex = Array.IndexOf(sources, dynamicSource);
                if (ImGui.Combo("Dynamic Source", ref selectedIndex, sources, sources.Length))
                {
                    dynamicSource = sources[selectedIndex];
                    _dirty = true;
                }
                break;
            }
        }

        //dropdown for font source
        Inspector.Inspector.Inspect("Font Location", ref fontSource);
        switch (fontSource)
        {
            case FontSource.Resource:
            {
                ImGui.LabelText("Resource", font);

                string[] resourceFonts = UIFonts.GetAvailableResourceFonts();
                int selectedIndex = Array.IndexOf(resourceFonts, font);
                if (ImGui.Combo("Resource", ref selectedIndex, resourceFonts, resourceFonts.Length))
                {
                    font = resourceFonts[selectedIndex];
                    _dirty = true;
                }

                ImGui.SameLine();

                if (ImGui.Button("Refresh List"))
                {
                    UIFonts.GetAvailableResourceFonts(true);
                }

                ImGui.BeginDisabled(CDTXMania.SkinManager.currentSkin == null);
                if (ImGui.Button("Add new Font Resource"))
                {
                    Dictionary<string, string> filterList = new()
                    {
                        { "Fonts", "ttf,otf" }
                    };

                    string path = NFD.OpenDialog("", filterList);

                    if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    {
                        var currentSkin = CDTXMania.SkinManager.currentSkin;

                        if (currentSkin != null)
                        {
                            string resourcePath = currentSkin.AddResource(ResourceType.Font, path);
                            font = resourcePath;
                            _dirty = true;
                        }
                    }
                }

                ImGui.EndDisabled();
                break;
            }

            case FontSource.System:
            {
                string[] systemFonts = UIFonts.GetAvailableSystemFonts();
                int selectedIndex = Array.IndexOf(systemFonts, font);
                if (ImGui.Combo("System Font", ref selectedIndex, systemFonts, systemFonts.Length))
                {
                    font = systemFonts[selectedIndex];
                    _dirty = true;
                }
                
                ImGui.SameLine();
                
                if (ImGui.Button("Refresh List"))
                {
                    UIFonts.GetAvailableSystemFonts(true);
                }
                break;
            }
        }

        if (ImGui.InputText("Font Path", ref font, 1024))
        {
            _dirty = true;
        }

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

        if (ImGui.InputFloat("Texture Padding", ref texturePadding, 1f, 4f, "%.1f"))
        {
            texturePadding = MathF.Max(texturePadding, 0f);
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