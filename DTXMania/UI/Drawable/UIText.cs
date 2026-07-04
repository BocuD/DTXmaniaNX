using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Inspector;
using DTXMania.UI.OpenGL;
using DTXMania.UI.Text;

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

public partial class UIText : UITexture
{
    private const float DefaultFontSize = 32f;
    private bool _dirty = true;

    // Incremented every time a render starts (async or sync). An async result is only applied if
    // its token still matches, so text that changed again mid-flight discards the stale result.
    private int _renderToken;
    
    [Themable] public string text = "New UIText";
    [Themable] public FontSource fontSource = FontSource.System;
    [Themable] public string font = UIFonts.FallbackFont;
    [Themable] public string fontFamily = string.Empty;
    [Themable] public float fontSize = DefaultFontSize;
    [Themable] public float outlineWidth = 3f;
    [Themable] public Vector2 texturePadding = Vector2.Zero;
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
            RequestRender();
        }

        if (!texture.IsValid())
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

    /// <summary>
    /// Requests an asynchronous re-render. The Skia rasterization runs on a background thread and
    /// the resulting texture is uploaded + applied on a later frame (see <see cref="AsyncTextureUploader"/>);
    /// the current texture keeps being drawn until then. This is the normal per-frame path and keeps
    /// text changes (e.g. scrolling the song list) off the critical path.
    /// </summary>
    private void RequestRender()
    {
        if (OpenGlRenderer.Instance == null)
        {
            _dirty = true;
            return;
        }

        _dirty = false;

        // Invalidate any in-flight render and clear immediately when there is nothing to draw.
        int token = ++_renderToken;

        if (string.IsNullOrEmpty(text))
        {
            if (texture.IsValid())
            {
                texture.Dispose();
                SetTexture(BaseTexture.None);
            }
            return;
        }

        UiTextParameters request = renderBackend switch
        {
            UiTextRenderBackend.Skia when BaseTexture.SkiaTextRenderer != null => CreateRenderRequest(),
            UiTextRenderBackend.Skia => throw new InvalidOperationException("Skia text renderer is not available."),
            _ => throw new ArgumentOutOfRangeException()
        };

        AsyncTextureUploader.Instance.RequestText(request, tex => ApplyRenderedText(token, tex));
    }

    private void ApplyRenderedText(int token, BaseTexture? renderedTexture)
    {
        // A newer render (or a synchronous RenderTexture) superseded this one.
        if (token != _renderToken)
        {
            renderedTexture?.Dispose();
            return;
        }

        if (texture.IsValid())
        {
            texture.Dispose();
        }

        SetTexture(renderedTexture ?? BaseTexture.None);
    }

    /// <summary>
    /// Synchronously rasterizes and uploads the text on the calling (main) thread. Used to warm
    /// text up before a stage becomes visible (see the song-select load phase). Prefer the async
    /// path (<see cref="RequestRender"/>) during normal frames.
    /// </summary>
    public void RenderTexture()
    {
        // Bump the token so any in-flight async render for this element is discarded on arrival.
        ++_renderToken;

        if (texture.IsValid())
        {
            texture.Dispose();
            SetTexture(BaseTexture.None);
        }

        if (OpenGlRenderer.Instance == null)
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

    private UiTextParameters CreateRenderRequest()
    {
        //determine renderscale
        float renderSize = fontSize * CDTXMania.renderScale;
        scale = new Vector3(1 / CDTXMania.renderScale);
        
        return new UiTextParameters
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
