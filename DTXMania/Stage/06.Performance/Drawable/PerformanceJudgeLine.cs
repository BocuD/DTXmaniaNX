using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Inspector;
using DTXMania.UI.Skin;

namespace DTXMania;

public class PerformanceJudgeLine : PerformanceElement
{
    [AddChildMenu("Game/Performance/Judge Line")]
    public new static UIDrawable Create() => new PerformanceJudgeLine();

    public PerformanceJudgeLine() : this(EInstrumentPart.DRUMS)
    {
    }

    public PerformanceJudgeLine(EInstrumentPart instrument) : base("JudgeLine", instrument)
    {
    }

    protected override string ComponentName(EInstrumentPart part) => part switch
    {
        EInstrumentPart.GUITAR => "GuitarJudgeLine",
        EInstrumentPart.BASS => "BassJudgeLine",
        _ => "DrumsJudgeLine"
    };

    protected override UIGroup BuildDefault(EInstrumentPart part)
    {
        UIGroup root = new(ComponentName(part));

        UIGroup line = root.AddChild(new UIGroup("Line"));
        line.bindings.Add(new UIBinding("position.Y", "Stats.JudgeLineY"));
        line.bindings.Add(new UIBinding("isVisible", "Stats.ShowsJudgeLine"));

        line.AddChild(new UIImage
        {
            name = "Bar",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\ScreenPlayDrums hit-bar.png"),
            size = new Vector2(559, 6),
            clipRect = new RectangleF(0, 0, 559, 6),
            bindings =
            {
                new UIBinding("position.X", "Stats.JudgeLineX"),
                new UIBinding("size.X", "Stats.JudgeLineWidth"),
                new UIBinding("clipRect.Width", "Stats.JudgeLineWidth")
            }
        });

        UIGroup label = root.AddChild(new UIGroup("Label"));
        label.bindings.Add(new UIBinding("position.Y", "Stats.JudgeLineNumberY"));
        label.AddChild(LaneNumber("Value", "Stats.JudgeLine", new Vector2(part == EInstrumentPart.DRUMS ? 17 : 247, -6)));

        return root;
    }
}
