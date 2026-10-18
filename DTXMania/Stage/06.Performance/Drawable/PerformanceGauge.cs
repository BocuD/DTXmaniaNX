using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Inspector;
using DTXMania.UI.Skin;

namespace DTXMania;

public class PerformanceGauge : PerformanceElement
{
    [AddChildMenu("Game/Performance/Gauge")]
    public new static UIDrawable Create() => new PerformanceGauge();

    public PerformanceGauge() : this(EInstrumentPart.DRUMS)
    {
    }

    public PerformanceGauge(EInstrumentPart instrument) : base("Gauge", instrument)
    {
        size = instrument == EInstrumentPart.DRUMS ? new Vector2(543, 47) : new Vector2(290, 68);
    }

    protected override string ComponentName(EInstrumentPart part) => part switch
    {
        EInstrumentPart.GUITAR => "GuitarGauge",
        EInstrumentPart.BASS => "BassGauge",
        _ => "DrumsGauge"
    };

    protected override UIGroup BuildDefault(EInstrumentPart part)
    {
        (string frame, Vector2 frameSize, Vector3 bar, Vector2 barSize, Vector3 speed) = part switch
        {
            EInstrumentPart.GUITAR => (@"Graphics\7_Gauge_Guitar.png", new Vector2(290, 68), new Vector3(6, 31, 0),
                new Vector2(242, 30), new Vector3(254, 30, 0)),
            EInstrumentPart.BASS => (@"Graphics\7_Gauge_Bass.png", new Vector2(290, 68), new Vector3(42, 31, 0),
                new Vector2(242, 30), new Vector3(4, 30, 0)),
            _ => (@"Graphics\7_Gauge.png", new Vector2(543, 47), new Vector3(20, 9, 0),
                new Vector2(480, 31), new Vector3(506, 8, 0))
        };

        UIGroup root = new(ComponentName(part));

        root.AddChild(new UIImage
        {
            name = "FrameBack",
            imageSource = ImageSource.File,
            image = SkinResource.System(frame),
            size = frameSize,
            clipRect = new RectangleF(0, 0, frameSize.X, frameSize.Y)
        });

        root.AddChild(new UIImage
        {
            name = "ScrollSpeed",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\7_Panel_icons.jpg"),
            position = speed,
            size = new Vector2(32, 32),
            clipRect = new RectangleF(0, 0, 42, 48),
            renderOrder = 1,
            bindings = { new UIBinding("clipRect.Y", "Stats.ScrollSpeedIconY") }
        });

        root.AddChild(new UIImage
        {
            name = "Bar",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\7_gauge_bar.png"),
            position = bar,
            size = barSize,
            clipRect = new RectangleF(0, 0, barSize.X, barSize.Y),
            renderOrder = 2,
            bindings =
            {
                new UIBinding("scale.X", "Stats.Gauge"),
                new UIBinding("isVisible", "Stats.IsGaugeFull") { invert = true }
            }
        });

        root.AddChild(new UIImage
        {
            name = "FullBar",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\7_gauge_bar.jpg"),
            position = bar,
            size = barSize,
            clipRect = new RectangleF(0, 0, barSize.X, barSize.Y),
            renderOrder = 2,
            bindings = { new UIBinding("isVisible", "Stats.IsGaugeFull") }
        });

        root.AddChild(new UIImage
        {
            name = "FrameFront",
            imageSource = ImageSource.File,
            image = SkinResource.System(frame),
            size = frameSize,
            clipRect = new RectangleF(0, frameSize.Y, frameSize.X, frameSize.Y),
            renderOrder = 3
        });

        return root;
    }
}
