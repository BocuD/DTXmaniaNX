using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Inspector;
using DTXMania.UI.OpenGL;
using DTXMania.UI.Skin;
using DTXMania.UI.Text;
using Hexa.NET.ImGui;
using NativeFileDialog.Extended;

namespace DTXMania.UI.Drawable;

public enum TextSource
{
    String,
    Dynamic
}

public enum UiTextAlignment
{
    Left,
    Center,
    Right
}

public class UIText : UITexture
{
    private const float DefaultFontSize = 32f;
    private bool _dirty = true;

    [Themable] public string text = "New UIText";
    [Themable] public FontSource fontSource = FontSource.System;
    [Themable] public string font = UIFonts.FallbackFont;
    [Themable] public string fontFamily = string.Empty;
    [Themable] public float fontSize = DefaultFontSize;
    [Themable] public float outlineWidth = 3f;
    [Themable] public float texturePadding = 0f;
    [Themable] public float lineSpacing = 1f;
    [Themable] public bool antialias = true;
    [Themable] public bool subpixelText = true;
    [Themable] public UiTextStyle style = UiTextStyle.Regular;
    [Themable] public UiTextAlignment alignment = UiTextAlignment.Left;
    [Themable] public UiTextRenderBackend renderBackend = UiTextRenderBackend.Skia;
    [Themable] public Color4 fillColor = Color4.White;
    [Themable] public Color4 outlineColor = new(0f, 0f, 0f, 1f);
    [Themable] public UiTextGradientMode fillGradientMode = UiTextGradientMode.None;
    [Themable] public Color4 fillGradientTopColor = Color4.White;
    [Themable] public Color4 fillGradientBottomColor = Color4.White;
    [Themable] public UiTextGradientMode outlineGradientMode = UiTextGradientMode.None;
    [Themable] public Color4 outlineGradientTopColor = new(0f, 0f, 0f, 1f);
    [Themable] public Color4 outlineGradientBottomColor = new(0f, 0f, 0f, 1f);

    [Themable] public TextSource textSource = TextSource.String;
    [Themable] public string dynamicSource = "Not Set";
    
    [AddChildMenu]
    public static UIDrawable Create()
    {
        return new UIText();
    }

    public UIText()
        : base(BaseTexture.None)
    {
    }

    public UIText(string textValue, float size = DefaultFontSize)
        : base(BaseTexture.None)
    {
        text = textValue;
        fontSize = size;
    }

    public void SetText(string newText)
    {
        if (text == newText)
        {
            return;
        }

        text = newText;
        _dirty = true;
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        if (!isVisible)
        {
            return;
        }
        
        if (textSource == TextSource.Dynamic)
        {
            UpdateDynamicText();
        }

        if (_dirty)
        {
            RenderTexture();
        }

        if (!texture.isValid())
        {
            return;
        }

        UpdateLocalTransformMatrix();
        Matrix4x4 combinedMatrix = localTransformMatrix * parentMatrix;
        texture.tDraw2DMatrix(combinedMatrix, size, new RectangleF(0, 0, texture.Width, texture.Height), Color4.White);
    }
    
    private void UpdateDynamicText()
    {
        CDTXMania.StageManager.rCurrentStage.dynamicStringSources.TryGetValue(dynamicSource, out var source);
        if (source != null)
        {
            SetText(source.GetString());
        }
        else
        {
            SetText($"Dynamic source: {dynamicSource} not found");
        }
    }

    public void RenderTexture()
    {
        if (texture.isValid())
        {
            texture.Dispose();
            SetTexture(BaseTexture.None);
        }

        if (OpenGlUi.Renderer == null)
        {
            _dirty = true;
            return;
        }

        if (string.IsNullOrEmpty(text))
        {
            _dirty = false;
            return;
        }

        BaseTexture renderedTexture = renderBackend switch
        {
            UiTextRenderBackend.Skia when BaseTexture.SkiaTextRenderer != null => BaseTexture.SkiaTextRenderer.Render(CreateRenderRequest()),
            UiTextRenderBackend.Skia => throw new InvalidOperationException("Skia text renderer is not available."),
            _ => throw new ArgumentOutOfRangeException()
        };

        SetTexture(renderedTexture);
        _dirty = false;
    }

    public override void OnDeserialize()
    {
        base.OnDeserialize();
        _dirty = true;
    }

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
                            string resourcePath = currentSkin.AddResource(SkinDescriptor.ResourceType.Font, path);
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

        // if (ImGui.Button("Browse Font"))
        // {
        //     Dictionary<string, string> filterList = new()
        //     {
        //         { "Fonts", "ttf,otf,ttc,woff,woff2" }
        //     };
        //
        //     string path = NFD.OpenDialog("", filterList);
        //     if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        //     {
        //         font = path;
        //         _dirty = true;
        //     }
        // }

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

    private UiTextRenderRequest CreateRenderRequest()
    {
        //determine renderscale
        float renderSize = fontSize * CDTXMania.renderScale;
        scale = new Vector3(1 / CDTXMania.renderScale);
        
        return new UiTextRenderRequest
        {
            Name = name,
            Text = text,
            FontPath = UIFonts.ResolveFontPath(fontSource, font),
            FontFamily = fontFamily,
            FontSize = renderSize,
            OutlineWidth = outlineWidth,
            TexturePadding = texturePadding,
            LineSpacing = lineSpacing,
            Antialias = antialias,
            SubpixelText = subpixelText,
            Style = style,
            Alignment = alignment,
            FillColor = fillColor,
            OutlineColor = outlineColor,
            FillGradientMode = fillGradientMode,
            FillGradientTopColor = fillGradientTopColor,
            FillGradientBottomColor = fillGradientBottomColor,
            OutlineGradientMode = outlineGradientMode,
            OutlineGradientTopColor = outlineGradientTopColor,
            OutlineGradientBottomColor = outlineGradientBottomColor,
            Backend = renderBackend
        };
    }
}
