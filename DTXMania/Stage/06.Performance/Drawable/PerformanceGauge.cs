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

    private struct GaugeArt
    {
        public string Frame;
        public Vector2 FrameSize;
        public Vector3 Bar;
        public Vector2 BarSize;
        public Vector3 Speed;
    }

    private static GaugeArt Art(EInstrumentPart part) => part switch
    {
        EInstrumentPart.GUITAR => new GaugeArt
        {
            Frame = @"Graphics\7_Gauge_Guitar.png", FrameSize = new Vector2(290, 68), Bar = new Vector3(6, 31, 0),
            BarSize = new Vector2(242, 30), Speed = new Vector3(254, 30, 0)
        },
        EInstrumentPart.BASS => new GaugeArt
        {
            Frame = @"Graphics\7_Gauge_Bass.png", FrameSize = new Vector2(290, 68), Bar = new Vector3(42, 31, 0),
            BarSize = new Vector2(242, 30), Speed = new Vector3(4, 30, 0)
        },
        _ => new GaugeArt
        {
            Frame = @"Graphics\7_Gauge.png", FrameSize = new Vector2(543, 47), Bar = new Vector3(20, 9, 0),
            BarSize = new Vector2(480, 31), Speed = new Vector3(506, 8, 0)
        }
    };

    protected override UIGroup BuildDefault(EInstrumentPart part)
    {
        GaugeArt art = Art(part);

        UIGroup root = new(ComponentName(part));

        root.AddChild(new UIImage
        {
            name = "FrameBack",
            imageSource = ImageSource.File,
            image = SkinResource.System(art.Frame),
            size = art.FrameSize,
            clipRect = new RectangleF(0, 0, art.FrameSize.X, art.FrameSize.Y)
        });

        root.AddChild(new UIImage
        {
            name = "ScrollSpeed",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\7_Panel_icons.jpg"),
            position = art.Speed,
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
            position = art.Bar,
            size = art.BarSize,
            clipRect = new RectangleF(0, 0, art.BarSize.X, art.BarSize.Y),
            renderOrder = 2,
            bindings =
            {
                new UIBinding("scale.X", "Stats.Gauge"),
                new UIBinding("isVisible", "Stats.Gauge") { condition = BindingCondition.Less, value = 1 }
            }
        });

        root.AddChild(new UIImage
        {
            name = "FullBar",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\7_gauge_bar.jpg"),
            position = art.Bar,
            size = art.BarSize,
            clipRect = new RectangleF(0, 0, art.BarSize.X, art.BarSize.Y),
            renderOrder = 2,
            bindings = { new UIBinding("isVisible", "Stats.Gauge") { condition = BindingCondition.GreaterOrEqual, value = 1 } }
        });

        root.AddChild(new UIImage
        {
            name = "FrameFront",
            imageSource = ImageSource.File,
            image = SkinResource.System(art.Frame),
            size = art.FrameSize,
            clipRect = new RectangleF(0, art.FrameSize.Y, art.FrameSize.X, art.FrameSize.Y),
            renderOrder = 3
        });

        if (part == EInstrumentPart.GUITAR)
        {
            MirrorOnRightSide(root);
        }

        return root;
    }

    private static void MirrorOnRightSide(UIGroup root)
    {
        GaugeArt left = Art(EInstrumentPart.GUITAR);
        GaugeArt right = Art(EInstrumentPart.BASS);

        Mirror(root.GetChild<UIImage>("FrameBack")!);
        Mirror(root.GetChild<UIImage>("FrameFront")!);

        root.GetChild<UIImage>("Bar")!.bindings.Add(PlayerSection.OnRightSide("position.X", right.Bar.X, left.Bar.X));
        root.GetChild<UIImage>("FullBar")!.bindings.Add(PlayerSection.OnRightSide("position.X", right.Bar.X, left.Bar.X));
        root.GetChild<UIImage>("ScrollSpeed")!.bindings.Add(PlayerSection.OnRightSide("position.X", right.Speed.X, left.Speed.X));
    }

    private static void Mirror(UIImage frame)
    {
        frame.pivot = new Vector2(0.5f, 0);
        frame.position = new Vector3(frame.size.X / 2, 0, 0);
        frame.bindings.Add(PlayerSection.OnRightSide("scale.X", -1, 1));
    }
}
