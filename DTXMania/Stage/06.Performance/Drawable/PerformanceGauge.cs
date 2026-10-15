using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Inspector;
using DTXMania.UI.Skin;
using Hexa.NET.ImGui;

namespace DTXMania;

public class PerformanceGauge : UIGroup
{
    [AddChildMenu("Game/Performance/Gauge")]
    public new static UIDrawable Create() => new PerformanceGauge();

    [Themable] public EInstrumentPart instrument;

    [Themable] public bool moveWhenReversed;
    [Themable] public Vector3 reversedPosition;

    private readonly PerformanceStats stats = new();
    private readonly UIDataContext data = new();

    public PerformanceGauge() : this(EInstrumentPart.DRUMS)
    {
    }

    public PerformanceGauge(EInstrumentPart instrument) : base("Gauge")
    {
        this.instrument = instrument;
        stats.Instrument = (int)instrument;

        data.RegisterObject("Stats", () => stats);
        dataContext = data;

        size = instrument == EInstrumentPart.DRUMS ? new Vector2(543, 47) : new Vector2(290, 68);
        UseDefaultFor(instrument);
    }

    public override void OnDeserialize()
    {
        base.OnDeserialize();
        stats.Instrument = (int)instrument;
        UseDefaultFor(instrument);
    }

    private void UseDefaultFor(EInstrumentPart part)
    {
        switch (part)
        {
            case EInstrumentPart.GUITAR:
                MakeComponent("GuitarGauge", () => GaugeDefault("GuitarGauge", @"Graphics\7_Gauge_Guitar.png",
                    new Vector2(290, 68), new Vector3(6, 31, 0), new Vector2(242, 30), new Vector3(254, 30, 0)));
                break;

            case EInstrumentPart.BASS:
                MakeComponent("BassGauge", () => GaugeDefault("BassGauge", @"Graphics\7_Gauge_Bass.png",
                    new Vector2(290, 68), new Vector3(42, 31, 0), new Vector2(242, 30), new Vector3(4, 30, 0)));
                break;

            default:
                MakeComponent("DrumsGauge", () => GaugeDefault("DrumsGauge", @"Graphics\7_Gauge.png",
                    new Vector2(543, 47), new Vector3(20, 9, 0), new Vector2(480, 31), new Vector3(506, 8, 0)));
                break;
        }
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        stats.Instrument = (int)instrument;

        if (!stats.ShowsGauge)
        {
            return;
        }

        if (moveWhenReversed && stats.IsReversed)
        {
            parentMatrix = Matrix4x4.CreateTranslation(reversedPosition - position) * parentMatrix;
        }

        base.Draw(parentMatrix);
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (ImGui.CollapsingHeader("Gauge"))
        {
            Inspector.Inspect("Instrument", ref instrument);
            ImGui.Checkbox("Move When Reversed", ref moveWhenReversed);

            ImGui.BeginDisabled(!moveWhenReversed);
            Inspector.Inspect("Reversed Position", ref reversedPosition);
            ImGui.EndDisabled();
        }
    }

    private static UIGroup GaugeDefault(string name, string frame, Vector2 frameSize, Vector3 barPosition,
        Vector2 barSize, Vector3 speedPosition)
    {
        UIGroup root = new(name);
        RectangleF barRect = new(0, 0, barSize.X, barSize.Y);

        root.AddChild(Frame("FrameBack", frame, frameSize, 0, 0));

        root.AddChild(new UIImage
        {
            name = "ScrollSpeed",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\7_Panel_icons.jpg"),
            position = speedPosition,
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
            position = barPosition,
            size = barSize,
            clipRect = barRect,
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
            position = barPosition,
            size = barSize,
            clipRect = barRect,
            renderOrder = 2,
            bindings = { new UIBinding("isVisible", "Stats.IsGaugeFull") }
        });

        root.AddChild(Frame("FrameFront", frame, frameSize, frameSize.Y, 3));

        return root;
    }

    private static UIImage Frame(string name, string frame, Vector2 frameSize, float top, int renderOrder) => new()
    {
        name = name,
        imageSource = ImageSource.File,
        image = SkinResource.System(frame),
        size = frameSize,
        clipRect = new RectangleF(0, top, frameSize.X, frameSize.Y),
        renderOrder = renderOrder
    };
}
