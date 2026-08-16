using System.Globalization;
using System.Numerics;
using System.Text;
using DTXMania.Core;
using DTXMania.Core.Audio;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;

namespace DTXMania.UI.Config;

internal sealed class ConfigAudioPanel : UIGroup
{
    private const float Inset = 7f;
    private const float Width = 280f;
    private const float Height = 132f;

    private static readonly Vector3 Place = new(864, 140, 0);

    private const float HeaderHeight = 21f;

    private const int MaxDeviceName = 30;

    private readonly UIText text;

    //rebuilt only when what it says changes, so sitting on the page costs nothing to draw
    private string shown = string.Empty;

    public ConfigAudioPanel() : base("ConfigAudioPanel")
    {
        dontSerialize = true;
        position = Place;
        size = new Vector2(Width, Height);

        UIImage background = AddChild(new UIImage(BaseTexture.CreateSolidColor(new Color4(1f, 1f, 1f, 0.8f))));
        background.size = size;
        background.renderOrder = 0;

        UIText header = AddChild(new UIText(CDTXMania.isJapanese ? "オーディオデバイス" : "Audio device", 18));
        header.name = "AudioDeviceHeader";
        header.renderOrder = 1;
        header.position = new Vector3(Inset, Inset, 0);
        header.outlineWidth = 0;
        header.fillColor = new Color4(0.25f, 0.25f, 0.25f);

        text = AddChild(new UIText("", 15));
        text.name = "AudioDeviceText";
        text.renderOrder = 1;
        text.position = new Vector3(Inset, Inset + HeaderHeight, 0);
        text.wrap = true;
        text.size.X = Width - Inset * 2f;
        text.outlineWidth = 0;
        text.fillColor = Color4.Black;
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        string next = Describe();
        if (next != shown)
        {
            shown = next;
            text.SetText(next);
        }

        base.Draw(parentMatrix);
    }

    private static string Describe()
    {
        AudioDeviceStatus audio = AudioMixer.Device.Status;
        AudioLatency latency = AudioMixer.Device.Latency;
        bool japanese = CDTXMania.isJapanese;

        StringBuilder lines = new();

        lines.Append(audio.Backend);
        if (audio.SampleRate > 0)
        {
            lines.Append("   ").Append(audio.SampleRate.ToString(CultureInfo.InvariantCulture)).Append(" Hz");
        }

        lines.Append('\n').Append(audio.Output.Length > 0
            ? Shorten(audio.Output)
            : japanese ? "デバイス名不明" : "unnamed device");

        lines.Append('\n').Append(japanese ? "バッファ " : "Buffer ");
        if (audio.BufferFrames > 0)
        {
            lines.Append(audio.BufferFrames.ToString(CultureInfo.InvariantCulture))
                .Append(' ').Append(audio.FrameUnit)
                .Append(" (").Append(Ms(audio.BufferLatencyMs)).Append(')');
        }
        else
        {
            lines.Append(audio.BufferMs >= 0 ? Ms(audio.BufferLatencyMs) : japanese ? "不明" : "unknown");
        }

        if (audio.PeriodMs > 0.0)
        {
            lines.Append('\n').Append(japanese ? "周期 " : "Period ");

            if (audio.PeriodFrames > 0)
            {
                lines.Append(audio.PeriodFrames.ToString(CultureInfo.InvariantCulture))
                    .Append(' ').Append(audio.FrameUnit)
                    .Append(" (").Append(Ms(audio.PeriodMs)).Append(')');
            }
            else
            {
                lines.Append(Ms(audio.PeriodMs));
            }
        }

        lines.Append('\n').Append(japanese ? "レイテンシ " : "Latency ");
        lines.Append(latency.IsKnown
            ? Ms(latency.Ms)
            : japanese ? "不明" : "not reported");

        return lines.ToString();
    }

    private static string Ms(double value)
        => value.ToString("0.0", CultureInfo.InvariantCulture) + "ms";

    private static string Shorten(string name)
        => name.Length <= MaxDeviceName ? name : name[..(MaxDeviceName - 1)] + "…";
}
