using DTXMania.UI.Skin;
using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Inspector;
using DTXMania.UI.OpenGL;
using DTXMania.UI.Text;
using Newtonsoft.Json;

namespace DTXMania.UI.Drawable;


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

    //Incremented every time a render starts (async or sync). An async result is only applied if
    //its token still matches, so text that changed again mid-flight discards the stale result.
    private int _renderToken;
    
    //a property so every writer goes through the same check and only re-rasterizes on a real change
    [Themable]
    public string text
    {
        get => _text;
        set
        {
            if (_text == value)
            {
                return;
            }

            _text = value;
            _dirty = true;
        }
    }

    private string _text = "New UIText";

    [Themable] public SkinResource font = SkinResource.System(UIFonts.FallbackFont);

    //typeface name to fall back on when the font file cannot be resolved
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

    [JsonIgnore] private string? _unresolvedText;

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

    public void SetText(string newText) => text = newText;

    //Forces a re-render on the next Draw (e.g. after changing color/outline/style)
    public void MarkDirty()
    {
        _dirty = true;
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        if (!isVisible)
        {
            return;
        }
        
        ShowUnresolvedBinding();

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
        texture.tDraw2DMatrix(combinedMatrix, GetTextureDrawSize(), GetTextureSourceRect(), Color4.White);
    }

    //Source rectangle (in texture pixels) sampled from the rendered text texture. Defaults to the
    //whole texture; subclasses can override to draw a sub-region (e.g. a scrolling clip window).
    protected virtual RectangleF GetTextureSourceRect() => new(0, 0, texture.Width, texture.Height);

    /// Destination size the sampled region is drawn at (before this element's scale). Defaults to
    /// <see cref="UIDrawable.size"/>; subclasses can override to clamp the drawn width.
    protected virtual Vector2 GetTextureDrawSize() => size;

    /// <summary>What drives this text, or empty when nothing does.</summary>
    public string TextBindingSource()
    {
        for (int i = 0; i < bindings.Count; i++)
        {
            if (bindings[i].target == nameof(text))
            {
                return bindings[i].source;
            }
        }

        return string.Empty;
    }

    //an unresolved binding leaves its target alone, which for text just looks empty; say so instead
    private void ShowUnresolvedBinding()
    {
        for (int i = 0; i < bindings.Count; i++)
        {
            UIBinding binding = bindings[i];

            if (binding.target != nameof(text) || binding.resolved)
            {
                continue;
            }

            //built once per key, since this runs every frame the binding stays broken
            text = _unresolvedText ??= $"Can't resolve binding: {binding.source}";
            return;
        }

        _unresolvedText = null;
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

        //invalidate any in-flight render and clear immediately when there is nothing to draw.
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
        //A newer render (or a synchronous RenderTexture) superseded this one
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
        //Bump the token so any in-flight async render for this element is discarded on arrival
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
            FontPath = UIFonts.ResolveFontPath(font),
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
