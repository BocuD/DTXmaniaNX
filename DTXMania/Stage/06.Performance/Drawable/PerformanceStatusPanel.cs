using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Inspector;
using DTXMania.UI.Skin;
using DTXMania.UI.Text;

namespace DTXMania;

public class PerformanceStatusPanel : PerformanceElement
{
    [AddChildMenu("Game/Performance/Status Panel")]
    public new static UIDrawable Create() => new PerformanceStatusPanel();

    public PerformanceStatusPanel() : this(EInstrumentPart.DRUMS)
    {
    }

    public PerformanceStatusPanel(EInstrumentPart instrument) : base("StatusPanel", instrument)
    {
        size = new Vector2(268, 446);
    }

    protected override bool IsShown => stats.ShowsPanel;

    protected override string ComponentName(EInstrumentPart part) => "PerformanceStatus";

    private static readonly Digits Count = new(26f, 0.93f, 0.2f, 1.6f);
    private static readonly Digits Percent = new(22f, 0.93f, 0.2f, 0.5f);
    private static readonly Digits PercentSign = new(16f, 0.9f, -0.15f, 0f);
    private static readonly Digits Level = new(28f, 0.9f, -0.1f, 0f);
    private static readonly Digits Rate = new(54f, 0.85f, -0.2f, 0f);
    private static readonly Digits RateDecimals = new(45f, 0.85f, -0.2f, 0f);
    private static readonly Digits RatePercent = new(33f, 0.86f, -0.15f, 0f);
    private static readonly Digits Skill = new(58f, 1f, -0.6f, 0f);
    private static readonly Digits SkillPoint = new(46f, 0.76f, -0.6f, 0f);
    private static readonly Digits SkillDecimals = new(38f, 1f, -0.6f, 0f);

    private readonly record struct Digits(float Size, float HorizontalScale, float Weight, float Spacing);

    private static readonly SkinResource Face = SkinResource.System("texgyreadventor-regular.otf");
    private static readonly SkinResource ItalicFace = SkinResource.System("texgyreadventor-bolditalic.otf");
    private static readonly SkinResource LabelFace = SkinResource.System("Futura PT Book.otf");

    private static readonly Color4 LabelColor = new(0.8f, 0.8f, 0.8f);
    private static readonly Color4 LagBlue = new(0.204f, 0.733f, 0.8f);
    private static readonly Color4 LagRed = new(0.808f, 0.024f, 0.188f);

    private static readonly Color4 Dim = new(0.31f, 0.31f, 0.31f);

    protected override UIGroup BuildDefault(EInstrumentPart part)
    {
        UIGroup root = new("PerformanceStatus");

        root.AddChild(Image("Background", @"Graphics\Performance\stats_box.png", 0, 0)).renderOrder = -1;

        UIPlayerNameplate nameplate = root.AddChild(new UIPlayerNameplate());
        nameplate.name = "Nameplate";
        nameplate.position = new Vector3(6, 14, 0);
        nameplate.bindings.Add(new UIBinding("instrument", "Stats.Instrument"));

        Row(root, "Perfect", "Perfect", 100);
        Row(root, "Great", "Great", 130);
        Row(root, "Good", "Good", 160);
        Row(root, "Poor", "Ok", 190);
        Row(root, "Miss", "Miss", 220);
        Row(root, "MaxCombo", "Max Combo", 250).GetChild<UIText>("Label")!.horizontalScale = 0.64f;

        UIGroup difficulty = Group(root, "Difficulty", 16, 292);

        UIText instrumentName = difficulty.AddChild(Label("Instrument", "", 13, 30, -2));
        instrumentName.pivot = new Vector2(0.5f, 0);
        instrumentName.bindings.Add(new UIBinding("text", "Stats.InstrumentName"));

        UIImage badge = difficulty.AddChild(Image("Badge", @"Graphics\7_Difficulty.png", 0, 4));
        badge.size = new Vector2(60, 60);
        badge.clipRect = new RectangleF(0, 0, 60, 60);
        badge.bindings.Add(new UIBinding("clipRect.Y", "Stats.DifficultyBadgeY"));

        UIText level = Text(difficulty, "Level", "", Level, 29, 19);
        level.pivot = new Vector2(0.5f, 0);
        level.bindings.Add(new UIBinding("text", "Stats.LevelText"));

        root.AddChild(Image("RateLabel", @"Graphics\Result\icon_rate_en.png", 91, 270)).scale = new Vector3(0.52f, 0.52f, 1);

        UIGroup rate = Group(root, "Rate", 0, 0, "Stats.IsMaxRate", invert: true);
        Number(rate, "Whole", "Stats.RateWhole", Rate, 2, 166, 291, Dim);
        Text(rate, "Point", ".", RateDecimals, 163, 301);
        Number(rate, "Hundredths", "Stats.RateHundredths", RateDecimals, 2, 218, 301, Color4.White);
        Text(rate, "PercentSign", "%", RatePercent, 220, 315);

        UIImage rateMax = root.AddChild(Image("RateMax", @"Graphics\7_skill max.png", 127, 310));
        rateMax.bindings.Add(new UIBinding("isVisible", "Stats.IsMaxRate"));

        root.AddChild(Image("SkillIcon", @"Graphics\Result\icon_skill.png", 16, 376)).scale = new Vector3(0.37f, 0.37f, 1);
        root.AddChild(Image("SkillLabel", @"Graphics\Result\label_skill_en.png", 16, 415)).scale = new Vector3(0.38f, 0.38f, 1);

        UIGroup skill = Group(root, "Skill", 0, 0);
        //the slant reaches past the glyphs' advance
        Number(skill, "Whole", "Stats.SkillWhole", Skill, 3, 187, 359, Dim, face: ItalicFace, style: UiTextStyle.Italic)
            .texturePadding = new Vector2(12, 0);
        Text(skill, "Point", ".", SkillPoint, 192, 372, ItalicFace, UiTextStyle.Italic);
        Number(skill, "Hundredths", "Stats.SkillHundredths", SkillDecimals, 2, 244, 383, Color4.White,
            face: ItalicFace, style: UiTextStyle.Italic)
            .texturePadding = new Vector2(12, 0);

        UIGroup lag = Group(root, "LagCounts", 0, 0, "Config.ShowLagHitCount");

        UIGroup standard = Group(lag, "Standard", 0, 0, "Config.LagColorsSwapped", invert: true);
        LagCount(standard, "Early", "Stats.Early", 200, LagBlue);
        LagCount(standard, "Late", "Stats.Late", 252, LagRed);

        UIGroup swapped = Group(lag, "Swapped", 0, 0, "Config.LagColorsSwapped");
        LagCount(swapped, "Early", "Stats.Early", 200, LagRed);
        LagCount(swapped, "Late", "Stats.Late", 252, LagBlue);

        return root;
    }

    private static UIGroup Row(UIGroup root, string judgement, string label, float y)
    {
        UIGroup row = Group(root, judgement, 0, y);
        row.AddChild(Label("Label", label, 20, 15, -16));
        Number(row, "Count", $"Stats.{judgement}", Count, 4, 169, -24, Dim);
        Number(row, "Percent", $"Stats.{judgement}Percent", Percent, 3, 236, -22, Dim);
        Text(row, "PercentSign", "%", PercentSign, 237, -16);
        return row;
    }

    private static void LagCount(UIGroup parent, string name, string source, float right, Color4 color)
    {
        UIText count = Text(parent, name, string.Empty, Percent, right, 284);
        count.pivot = new Vector2(1, 0);
        count.fillColor = color;
        count.bindings.Add(new UIBinding("text", source));
    }

    private static UIGroup Group(UIGroup parent, string name, float x, float y, string? shownBy = null, bool invert = false)
    {
        UIGroup group = parent.AddChild(new UIGroup(name));
        group.position = new Vector3(x, y, 0);

        if (shownBy != null)
        {
            group.bindings.Add(new UIBinding("isVisible", shownBy) { invert = invert });
        }

        return group;
    }

    private static UIImage Image(string name, string path, float x, float y) => new()
    {
        name = name,
        imageSource = ImageSource.File,
        image = SkinResource.System(path),
        position = new Vector3(x, y, 0)
    };

    private static UIText Label(string name, string text, float size, float x, float y) => new(text, size)
    {
        name = name,
        font = LabelFace,
        fillColor = LabelColor,
        outlineWidth = 0,
        position = new Vector3(x, y, 0)
    };

    //positioned by its right edge
    private static UIPaddedNumber Number(UIGroup parent, string name, string source, Digits digits, int padding,
        float right, float top, Color4 padColor, Color4? color = null, SkinResource? face = null,
        UiTextStyle style = UiTextStyle.Regular)
    {
        UIPaddedNumber number = parent.AddChild(new UIPaddedNumber(source));
        number.name = name;
        number.padding = padding;
        number.font = face ?? Face;
        number.fontSize = digits.Size;
        number.style = style;
        number.weight = digits.Weight;
        number.letterSpacing = digits.Spacing;
        number.horizontalScale = digits.HorizontalScale;
        number.numColor = color ?? Color4.White;
        number.padColor = padColor;
        number.position = new Vector3(right, top, 0);
        number.pivot = new Vector2(1, 0);
        return number;
    }

    private static UIText Text(UIGroup parent, string name, string value, Digits digits, float left, float top,
        SkinResource? face = null, UiTextStyle style = UiTextStyle.Regular)
    {
        UIText text = parent.AddChild(new UIText(value, digits.Size));
        text.name = name;
        text.position = new Vector3(left, top, 0);
        text.horizontalScale = digits.HorizontalScale;
        text.fillColor = Color4.White;
        text.outlineWidth = 0;
        text.font = face ?? Face;
        text.style = style;
        text.weight = digits.Weight;
        return text;
    }
}
