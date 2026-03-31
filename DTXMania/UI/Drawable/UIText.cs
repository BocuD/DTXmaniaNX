using System.Drawing;
using System.Numerics;
using DTXMania.UI.Inspector;
using DTXMania.UI.OpenGL;
using DTXMania.UI.Text;
using Hexa.NET.ImGui;
using NativeFileDialog.Extended;

namespace DTXMania.UI.Drawable;

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

    public string text = "New UIText";
    public string fontPath = UiFontDefaults.TryGetDefaultUiFontPath() ?? string.Empty;
    public string fontFamily = string.Empty;
    public float fontSize = DefaultFontSize;
    public float outlineWidth = 3f;
    public float texturePadding = 0f;
    public float lineSpacing = 1f;
    public bool antialias = true;
    public bool subpixelText = true;
    public UiTextStyle style = UiTextStyle.Regular;
    public UiTextAlignment alignment = UiTextAlignment.Left;
    public UiTextRenderBackend renderBackend = UiTextRenderBackend.Skia;
    public Color4 fillColor = Color4.White;
    public Color4 outlineColor = new(0f, 0f, 0f, 1f);
    public UiTextGradientMode fillGradientMode = UiTextGradientMode.None;
    public Color4 fillGradientTopColor = Color4.White;
    public Color4 fillGradientBottomColor = Color4.White;
    public UiTextGradientMode outlineGradientMode = UiTextGradientMode.None;
    public Color4 outlineGradientTopColor = new(0f, 0f, 0f, 1f);
    public Color4 outlineGradientBottomColor = new(0f, 0f, 0f, 1f);

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

        if (ImGui.InputTextMultiline("Text", ref text, 4096))
        {
            _dirty = true;
        }

        if (ImGui.InputText("Font Path", ref fontPath, 1024))
        {
            _dirty = true;
        }

        if (ImGui.Button("Browse Font"))
        {
            Dictionary<string, string> filterList = new()
            {
                { "Fonts", "ttf,otf,ttc,woff,woff2" }
            };

            string path = NFD.OpenDialog("", filterList);
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                fontPath = path;
                _dirty = true;
            }
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

    private UiTextRenderRequest CreateRenderRequest()
    {
        return new UiTextRenderRequest
        {
            Name = name,
            Text = text,
            FontPath = fontPath,
            FontFamily = fontFamily,
            FontSize = fontSize,
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
