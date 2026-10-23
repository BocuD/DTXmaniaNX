using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Inspector;
using DTXMania.UI.Skin;

namespace DTXMania;

public class PerformanceLane : PerformanceElement
{
    [AddChildMenu("Game/Performance/Lane")]
    public new static UIDrawable Create() => new PerformanceLane();

    public PerformanceLane() : this(EInstrumentPart.GUITAR)
    {
    }

    public PerformanceLane(EInstrumentPart instrument) : base("Lane", instrument)
    {
    }

    protected override string ComponentName(EInstrumentPart part) => part switch
    {
        EInstrumentPart.BASS => "BassLane",
        _ => "GuitarLane"
    };

    protected override UIGroup BuildDefault(EInstrumentPart part)
    {
        UIGroup root = new(ComponentName(part));

        root.AddChild(new UIImage
        {
            name = "Lane",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\7_Paret_Guitar.png"),
            bindings = { new UIBinding("isVisible", "Stats.IsLaneDark") { invert = true } }
        });

        root.AddChild(new UIImage
        {
            name = "DarkLane",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\7_Paret_Guitar_Dark.png"),
            bindings = { new UIBinding("isVisible", "Stats.IsLaneDark") }
        });

        return root;
    }
}
