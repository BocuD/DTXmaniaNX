using DTXMania.UI.Drawable;

namespace DTXMania.UI.Text;

public sealed class UiTextRenderRequest
{
    public required string Name { get; init; }
    public required string Text { get; init; }
    public string FontPath { get; init; } = string.Empty;
    public string FontFamily { get; init; } = string.Empty;
    public float FontSize { get; init; }
    public float OutlineWidth { get; init; }
    public float TexturePadding { get; init; }
    public float LineSpacing { get; init; }
    public bool Antialias { get; init; }
    public bool SubpixelText { get; init; }
    public UiTextStyle Style { get; init; }
    public UiTextAlignment Alignment { get; init; }
    public Color4 FillColor { get; init; }
    public Color4 OutlineColor { get; init; }
    public UiTextGradientMode FillGradientMode { get; init; }
    public Color4 FillGradientTopColor { get; init; }
    public Color4 FillGradientBottomColor { get; init; }
    public UiTextGradientMode OutlineGradientMode { get; init; }
    public Color4 OutlineGradientTopColor { get; init; }
    public Color4 OutlineGradientBottomColor { get; init; }
    public UiTextRenderBackend Backend { get; init; }
}
