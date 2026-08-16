using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Inspector;
using DTXMania.UI.Skin;
using Hexa.NET.ImGui;
using Newtonsoft.Json;

namespace DTXMania.UI.Drawable;

/// <summary>
/// Renders a short numeric string ("12.34", "98.76%", "-.--") from a fixed-cell glyph atlas: the classic
/// DTX bitmap-number look. Binds its text the same way <see cref="UIText"/> does.
/// </summary>
public class UINumberText : UIDrawable
{
    //System-relative path to the glyph atlas; cells are laid out per Glyphs below
    [Themable] public string atlas = @"Graphics\5_level number.png";

    [Themable] public string text = string.Empty;

    //emphasizeIntegerPart draws the part before the decimal larger, the difficulty-level style
    [Themable] public float textScale = 1.0f;
    [Themable] public bool emphasizeIntegerPart = true;
    [Themable] public Color4 color = Color4.White;

    [JsonIgnore] private BaseTexture atlasTexture = BaseTexture.None;
    [JsonIgnore] private string? _lastAtlasLoaded;

    [AddChildMenu("Text/Number Text")]
    public static UIDrawable Create() => new UINumberText();

    public UINumberText()
    {
    }

    public UINumberText(string source, float textScale = 1.0f)
    {
        bindings.Add(new UIBinding(nameof(text), source));
        this.textScale = textScale;
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        EnsureAtlas();

        if (!isVisible || !atlasTexture.IsValid())
        {
            return;
        }

        UpdateLocalTransformMatrix();
        Matrix4x4 baseMatrix = localTransformMatrix * parentMatrix;
        DrawNumber(baseMatrix, atlasTexture, textScale, ResolveText(), emphasizeIntegerPart, color);
    }

    private void EnsureAtlas()
    {
        if (string.IsNullOrWhiteSpace(atlas) || atlas == _lastAtlasLoaded)
        {
            return;
        }

        _lastAtlasLoaded = atlas;
        string full = SkinManager.SystemPath(atlas);
        if (File.Exists(full))
        {
            atlasTexture = BaseTexture.LoadFromPath(full);
        }
    }

    //whatever was last written, by code or by a binding
    private string ResolveText() => text;

    //advances are in the same renderScale-carrying basis as the passed matrix
    private static void DrawNumber(Matrix4x4 baseMatrix, BaseTexture atlasTexture, float scale, string str, bool emphasizeInteger, Color4 color)
    {
        float multiplier = atlasTexture.Height / 28.0f;

        bool foundDecimal = false;
        Matrix4x4 characterTranslation = Matrix4x4.Identity;

        for (int index = 0; index < str.Length; index++)
        {
            char c = str[index];
            if (!Glyphs.TryGetValue(c, out RectangleF rectangle)) continue;

            if (c == '.') foundDecimal = true;

            float characterScale = scale;
            if (emphasizeInteger && !foundDecimal) characterScale *= 1.35f;

            RectangleF scaledRect = new(
                rectangle.X * multiplier,
                rectangle.Y * multiplier,
                rectangle.Width * multiplier,
                rectangle.Height * multiplier
            );

            Matrix4x4 matrix = baseMatrix * characterTranslation;

            //compensate vertically for the larger integer-part glyphs so their baselines line up
            if (emphasizeInteger && !foundDecimal)
            {
                float offsetY = (rectangle.Height * characterScale - rectangle.Height * scale);
                offsetY *= 111.0f / 128.0f; //character is about 111/128 as tall as the texture height
                matrix *= Matrix4x4.CreateTranslation(0, -offsetY * CDTXMania.renderScale, 0);
            }

            atlasTexture.tDraw2DMatrix(matrix, new Vector2(rectangle.Width, rectangle.Height) * characterScale, scaledRect, color);

            characterTranslation *= Matrix4x4.CreateTranslation(rectangle.Width * CDTXMania.renderScale * characterScale, 0, 0);
        }
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (!ImGui.CollapsingHeader("Number Text"))
        {
            return;
        }

        ImGui.InputText("Text", ref text, 64);

        if (ImGui.InputText("Atlas", ref atlas, 512))
        {
            _lastAtlasLoaded = null;
        }
        ImGui.InputFloat("Text Scale", ref textScale, 0.05f, 0.25f, "%.2f");
        ImGui.Checkbox("Emphasize Integer Part", ref emphasizeIntegerPart);
        Inspector.Inspector.Inspect("Color", ref color);
    }

    //the standard DTX number-atlas cell layout: 20px cells, with a narrower period
    private static readonly Dictionary<char, RectangleF> Glyphs = new()
    {
        { '0', new RectangleF(0 * 20, 0, 20, 28) },
        { '1', new RectangleF(1 * 20, 0, 20, 28) },
        { '2', new RectangleF(2 * 20, 0, 20, 28) },
        { '3', new RectangleF(3 * 20, 0, 20, 28) },
        { '4', new RectangleF(4 * 20, 0, 20, 28) },
        { '5', new RectangleF(5 * 20, 0, 20, 28) },
        { '6', new RectangleF(6 * 20, 0, 20, 28) },
        { '7', new RectangleF(7 * 20, 0, 20, 28) },
        { '8', new RectangleF(8 * 20, 0, 20, 28) },
        { '9', new RectangleF(9 * 20, 0, 20, 28) },
        { '.', new RectangleF(10 * 20, 0, 10, 28) },
        { '-', new RectangleF(11 * 20 - 10, 0, 20, 28) },
        { '?', new RectangleF(12 * 20 - 10, 0, 20, 28) },
        { '%', new RectangleF(13 * 20 - 10, 0, 20, 28) }
    };
}
