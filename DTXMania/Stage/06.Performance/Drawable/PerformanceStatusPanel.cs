using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Inspector;
using DTXMania.UI.Skin;
using DTXMania.UI.Text;
using Hexa.NET.ImGui;

namespace DTXMania;

public class PerformanceStatusPanel : UIGroup
{
    [AddChildMenu("Game/Performance/Status Panel")]
    public new static UIDrawable Create() => new PerformanceStatusPanel();

    [Themable] public EInstrumentPart instrument;

    private readonly PerformanceStats stats = new();
    private readonly UIDataContext data = new();

    public PerformanceStatusPanel() : this(EInstrumentPart.DRUMS)
    {
    }

    public PerformanceStatusPanel(EInstrumentPart instrument) : base("StatusPanel")
    {
        this.instrument = instrument;
        stats.Instrument = (int)instrument;

        //one instance, so per-frame reads allocate nothing
        data.RegisterObject("Stats", () => stats);
        dataContext = data;

        size = new Vector2(257, 439);

        MakeComponent("PerformanceStatus", PanelDefault);
    }

    public override void OnDeserialize()
    {
        base.OnDeserialize();
        stats.Instrument = (int)instrument;
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        stats.Instrument = (int)instrument;

        if (!stats.ShowsPanel)
        {
            return;
        }

        base.Draw(parentMatrix);
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (ImGui.CollapsingHeader("Status Panel"))
        {
            Inspector.Inspect("Instrument", ref instrument);
        }
    }

    private static readonly Digits Small = new(27.9f, 0.93f, 0.4f, 2.7f);
    private static readonly Digits SmallPercent = new(21.3f, 0.9f, -0.15f, 0f);
    private static readonly Digits Large = new(44.5f, 0.98f, -0.2f, 4.8f);
    private static readonly Digits LargePoint = new(41.8f, 0.81f, 0.25f, 0f);
    private static readonly Digits LargePercent = new(37.5f, 0.98f, -0.15f, 0f);
    private static readonly Digits Level = new(26.7f, 0.97f, -0.25f, 1.7f);
    private static readonly Digits LevelPoint = new(26.4f, 0.85f, 0f, 0f);
    private static readonly Digits Lag = new(20.2f, 0.98f, 0f, 4.1f);

    private readonly record struct Digits(float Size, float HorizontalScale, float Weight, float Spacing);

    private static readonly SkinResource Face = SkinResource.System("texgyreadventor-regular.otf");

    private static readonly Color4 LagBlue = new(0.204f, 0.733f, 0.8f);
    private static readonly Color4 LagRed = new(0.808f, 0.024f, 0.188f);

    private static readonly Color4 Blank = new(0, 0, 0, 0);
    private static readonly Color4 Dim = new(0.31f, 0.31f, 0.31f);

    private static UIGroup PanelDefault()
    {
        UIGroup root = new("PerformanceStatus");

        root.AddChild(new UIImage
        {
            name = "Background",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\7_SkillPanel.png"),
            renderOrder = -1
        });

        UIPlayerNameplate nameplate = root.AddChild(new UIPlayerNameplate());
        nameplate.name = "Nameplate";
        nameplate.bindings.Add(new UIBinding("instrument", "Stats.Instrument"));

        Row(root, "Perfect", 60);
        Row(root, "Great", 90);
        Row(root, "Good", 120);
        Row(root, "Poor", 150);
        Row(root, "Miss", 180);
        Row(root, "MaxCombo", 210);

        root.AddChild(new UIImage
        {
            name = "DifficultyBadge",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\7_Difficulty.png"),
            position = new Vector3(14, 266, 0),
            size = new Vector2(60, 60),
            clipRect = new System.Drawing.RectangleF(0, 0, 60, 60),
            bindings = { new UIBinding("clipRect.Y", "Stats.DifficultyBadgeY") }
        });

        UIGroup level = Group(root, "Level", "Stats.IsClassicLevel", invert: true);
        Number(level, "Whole", "Stats.LevelWhole", Level, 1, new Vector2(33, 283));
        Sign(level, "Point", ".", LevelPoint, new Vector2(31, 282));
        Number(level, "Hundredths", "Stats.LevelHundredths", Level, 2, new Vector2(70, 283), Color4.White);

        UIGroup classicLevel = Group(root, "ClassicLevel", "Stats.IsClassicLevel");
        Number(classicLevel, "Level", "Stats.LevelWhole", Level, 2, new Vector2(57, 283), Color4.White);

        UIGroup rate = Group(root, "Rate", "Stats.IsMaxRate", invert: true);
        Number(rate, "Whole", "Stats.RateWhole", Large, 3, new Vector2(142, 262));
        Sign(rate, "Point", ".", LargePoint, new Vector2(143, 265));
        Number(rate, "Hundredths", "Stats.RateHundredths", Large, 2, new Vector2(212, 262), Color4.White);
        Sign(rate, "PercentSign", "%", LargePercent, new Vector2(215, 272));

        root.AddChild(new UIImage
        {
            name = "RateMax",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\7_skill max.png"),
            position = new Vector3(127, 277, 0),
            bindings = { new UIBinding("isVisible", "Stats.IsMaxRate") }
        });

        UIGroup skill = Group(root, "Skill", null);
        Number(skill, "Whole", "Stats.SkillWhole", Large, 3, new Vector2(172, 348));
        Sign(skill, "Point", ".", LargePoint, new Vector2(173, 351));
        Number(skill, "Hundredths", "Stats.SkillHundredths", Large, 2, new Vector2(242, 348), Color4.White);

        UIGroup lag = Group(root, "LagCounts", "Stats.ShowLagCounts");

        UIGroup standard = Group(lag, "Standard", "Stats.LagColorsSwapped", invert: true);
        Number(standard, "Early", "Stats.Early", Lag, 4, new Vector2(168, 327), color: LagBlue);
        Number(standard, "Late", "Stats.Late", Lag, 4, new Vector2(243, 327), color: LagRed);

        UIGroup swapped = Group(lag, "Swapped", "Stats.LagColorsSwapped");
        Number(swapped, "Early", "Stats.Early", Lag, 4, new Vector2(168, 327), color: LagRed);
        Number(swapped, "Late", "Stats.Late", Lag, 4, new Vector2(243, 327), color: LagBlue);

        return root;
    }

    private static void Row(UIGroup root, string judgement, float y)
    {
        Number(root, judgement, $"Stats.{judgement}", Small, 4, new Vector2(157, y), Dim);
        Number(root, $"{judgement}Percent", $"Stats.{judgement}Percent", Small, 3, new Vector2(224, y), Dim);
        Sign(root, $"{judgement}PercentSign", "%", SmallPercent, new Vector2(227, y + 9));
    }

    private static UIGroup Group(UIGroup parent, string name, string? shownBy, bool invert = false)
    {
        UIGroup group = parent.AddChild(new UIGroup(name));

        if (shownBy != null)
        {
            group.bindings.Add(new UIBinding("isVisible", shownBy) { invert = invert });
        }

        return group;
    }

    //positioned by its right edge
    private static void Number(UIGroup parent, string name, string source, Digits digits, int padding,
        Vector2 position, Color4? padColor = null, Color4? color = null)
    {
        UIPaddedNumber number = parent.AddChild(new UIPaddedNumber(source));
        number.name = name;
        number.padding = padding;
        number.font = Face;
        number.fontSize = digits.Size;
        number.style = UiTextStyle.Regular;
        number.weight = digits.Weight;
        number.letterSpacing = digits.Spacing;
        number.horizontalScale = digits.HorizontalScale;
        number.numColor = color ?? Color4.White;
        number.padColor = padColor ?? Blank;
        number.position = new Vector3(position, 0);
        number.pivot = new Vector2(1, 0);
    }

    private static void Sign(UIGroup parent, string name, string sign, Digits digits, Vector2 position)
    {
        UIText text = parent.AddChild(new UIText(sign, digits.Size));
        text.name = name;
        text.position = new Vector3(position, 0);
        text.horizontalScale = digits.HorizontalScale;
        text.fillColor = Color4.White;
        text.outlineWidth = 0;
        text.font = Face;
        text.style = UiTextStyle.Regular;
        text.weight = digits.Weight;
    }
}
