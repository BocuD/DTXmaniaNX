using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Inspector;
using DTXMania.UI.Skin;

namespace DTXMania;

//fret labels at the top of a guitar or bass lane, plate at the bottom. A shutter hides its end
public class PerformanceLaneCaps : PerformanceElement
{
    [AddChildMenu("Game/Performance/Lane Caps")]
    public new static UIDrawable Create() => new PerformanceLaneCaps();

    public PerformanceLaneCaps() : this(EInstrumentPart.GUITAR)
    {
    }

    public PerformanceLaneCaps(EInstrumentPart instrument) : base("LaneCaps", instrument)
    {
    }

    protected override string ComponentName(EInstrumentPart part) => part switch
    {
        EInstrumentPart.BASS => "BassLaneCaps",
        _ => "GuitarLaneCaps"
    };

    protected override UIGroup BuildDefault(EInstrumentPart part)
    {
        UIGroup root = new(ComponentName(part));

        root.AddChild(new UIImage
        {
            name = "Top",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\7_RGB buttons.png"),
            size = new Vector2(277, 64),
            clipRect = new RectangleF(0, 0, 277, 64),
            bindings =
            {
                new UIBinding("isVisible", "Stats.TopShutter") { condition = BindingCondition.Equal },
                new UIBinding("clipRect.Y", "Stats.IsLeft") { condition = BindingCondition.IsTrue, whenTrue = 64, whenFalse = 0 }
            }
        });

        root.AddChild(new UIImage
        {
            name = "Bottom",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\7_RGB buttons.png"),
            position = new Vector3(0, 628, 0),
            size = new Vector2(277, 50),
            clipRect = new RectangleF(0, 128, 277, 50),
            bindings = { new UIBinding("isVisible", "Stats.BottomShutter") { condition = BindingCondition.Equal } }
        });

        return root;
    }
}
