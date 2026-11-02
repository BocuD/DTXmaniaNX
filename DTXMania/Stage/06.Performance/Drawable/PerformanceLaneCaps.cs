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

    public PerformanceLaneCaps() : this(EPlayer.Guitar1)
    {
    }

    public PerformanceLaneCaps(EPlayer player) : base("LaneCaps", player)
    {
    }

    protected override string ComponentName(EPlayer player) => player switch
    {
        EPlayer.Guitar2 => "BassLaneCaps",
        _ => "GuitarLaneCaps"
    };

    protected override UIGroup BuildDefault(EPlayer player)
    {
        UIGroup root = new(ComponentName(player));

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
