using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;

namespace DTXMania.UI.Text;

public sealed class UiTextRenderRequest
{
    public required string Name { get; init; }
    public required string Text { get; init; }
    public required string FontPath { get; init; }
    public string FontFamily { get; init; } = string.Empty;
    public float FontSize { get; init; } = 12;
    public float OutlineWidth { get; init; } = 0;
    public float TexturePadding { get; init; } = 0;
    public float LineSpacing { get; init; } = 1;
    public bool Antialias { get; init; } = true;
    public bool SubpixelText { get; init; } = true;
    public UiTextStyle Style { get; init; } = UiTextStyle.Regular;
    public UiTextAlignment Alignment { get; init; } = UiTextAlignment.Left;
    public Color4 FillColor { get; init; } = Color4.Black;
    public Color4 OutlineColor { get; init; } = Color4.White;
    public UiTextGradientMode FillGradientMode { get; init; } = UiTextGradientMode.None;
    public Color4 FillGradientTopColor { get; init; } = Color4.White;
    public Color4 FillGradientBottomColor { get; init; } = Color4.White;
    public UiTextGradientMode OutlineGradientMode { get; init; } = UiTextGradientMode.None;
    public Color4 OutlineGradientTopColor { get; init; } = Color4.White;
    public Color4 OutlineGradientBottomColor { get; init; } = Color4.White;
    public required UiTextRenderBackend Backend { get; init; }
}
