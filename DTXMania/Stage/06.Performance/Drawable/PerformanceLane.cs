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

    public PerformanceLane() : this(EPlayer.Guitar1)
    {
    }

    public PerformanceLane(EPlayer player) : base("Lane", player)
    {
    }

    protected override string ComponentName(EPlayer player) => player switch
    {
        EPlayer.Guitar2 => "BassLane",
        _ => "GuitarLane"
    };

    protected override UIGroup BuildDefault(EPlayer player)
    {
        UIGroup root = new(ComponentName(player));

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
