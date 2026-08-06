using DTXMania.UI.Skin;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Inspector;
using DTXMania.UI.Text;
using Newtonsoft.Json;

namespace DTXMania.UI.Drawable;

/// <summary>
/// Renders a zero-padded number in two tones: the leading zeros dimmed in <see cref="padColor"/> and the
/// significant digits in <see cref="numColor"/>, positioned right after the padding by measuring its
/// rendered width. Set <see cref="value"/> directly, or bind it.
/// </summary>
public class UIPaddedNumber : UIGroup
{
    [Themable] public long value;
    [Themable] public int padding = 4;

    [Themable] public SkinResource font = SkinResource.System("texgyreadventor-regular.otf");
    [Themable] public int fontSize = 23;
    [Themable] public UiTextStyle style = UiTextStyle.Bold;
    [Themable] public Color4 padColor = new(0.31f, 0.31f, 0.31f);
    [Themable] public Color4 numColor = Color4.White;

    //code-managed text parts, recreated by the ctor on load and never part of the layout
    [JsonIgnore] private readonly UIText padText;
    [JsonIgnore] private readonly UIText numText;

    [JsonIgnore] private long lastValue;
    [JsonIgnore] private int lastStyleHash;
    [JsonIgnore] private bool applied;

    public UIPaddedNumber() : base("PaddedNumber")
    {
        padText = AddChild(new UIText("") { name = "Pad", dontSerialize = true });
        numText = AddChild(new UIText("") { name = "Num", dontSerialize = true });
    }

    public UIPaddedNumber(string source) : this()
    {
        bindings.Add(new UIBinding(nameof(value), source));
    }

    [AddChildMenu]
    public static UIDrawable Create() => new UIPaddedNumber();

    public override void Draw(Matrix4x4 parentMatrix)
    {
        long resolved = value;

        //the split, width measure and re-render are wasted work unless something actually changed
        int styleHash = HashCode.Combine(padding, fontSize, font.source, font.path, style, padColor, numColor);
        if (!applied || resolved != lastValue || styleHash != lastStyleHash)
        {
            applied = true;
            lastValue = resolved;
            lastStyleHash = styleHash;
            Apply(resolved);
        }

        base.Draw(parentMatrix);
    }

    private void Apply(long resolved)
    {
        string s = resolved.ToString("D" + Math.Max(1, padding));

        int zeros = 0;
        foreach (char c in s)
        {
            if (c == '0') zeros++;
            else break;
        }
        if (zeros == s.Length) zeros = s.Length - 1;

        StylePart(padText, s[..zeros], padColor);
        StylePart(numText, s[zeros..], numColor);

        padText.RenderTexture();
        numText.position.X = padText.position.X + padText.Texture.Width * (1f / CDTXMania.renderScale);
    }

    private void StylePart(UIText part, string text, Color4 color)
    {
        part.fontSize = fontSize;
        part.font = font;
        part.style = style;
        part.outlineWidth = 0;
        part.fillColor = color;
        part.SetText(text);
    }
}
