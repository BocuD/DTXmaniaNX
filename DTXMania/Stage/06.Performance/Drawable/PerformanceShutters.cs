using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Inspector;
using DTXMania.UI.Skin;

namespace DTXMania;

public class PerformanceShutters : PerformanceElement
{
    [AddChildMenu("Game/Performance/Shutters")]
    public new static UIDrawable Create() => new PerformanceShutters();

    public PerformanceShutters() : this(EInstrumentPart.DRUMS)
    {
    }

    public PerformanceShutters(EInstrumentPart instrument) : base("Shutters", instrument)
    {
    }

    protected override string ComponentName(EInstrumentPart part) => part switch
    {
        EInstrumentPart.GUITAR => "GuitarShutters",
        EInstrumentPart.BASS => "BassShutters",
        _ => "DrumsShutters"
    };

    protected override UIGroup BuildDefault(EInstrumentPart part)
    {
        bool drums = part == EInstrumentPart.DRUMS;
        string image = drums ? @"Graphics\7_shutter.png" : @"Graphics\7_shutter_GB.png";
        float numberRight = drums ? 286 : 132;

        UIGroup root = new(ComponentName(part));

        UIGroup top = root.AddChild(new UIGroup("Top"));
        top.bindings.Add(new UIBinding("position.Y", "Stats.TopShutterEdge"));
        top.bindings.Add(new UIBinding("isVisible", "Stats.TopShutter") { condition = BindingCondition.NotEqual });
        top.AddChild(new UIImage
        {
            name = "Shutter",
            imageSource = ImageSource.File,
            image = SkinResource.System(image),
            position = new Vector3(0, -720, 0)
        });
        top.AddChild(LaneNumber("Value", "Stats.TopShutter", new Vector2(numberRight, -28)));

        UIGroup bottom = root.AddChild(new UIGroup("Bottom"));
        bottom.bindings.Add(new UIBinding("position.Y", "Stats.BottomShutterEdge"));
        bottom.bindings.Add(new UIBinding("isVisible", "Stats.BottomShutter") { condition = BindingCondition.NotEqual });
        bottom.AddChild(new UIImage
        {
            name = "Shutter",
            imageSource = ImageSource.File,
            image = SkinResource.System(image)
        });
        bottom.AddChild(LaneNumber("Value", "Stats.BottomShutter", new Vector2(numberRight, -3)));

        return root;
    }
}
