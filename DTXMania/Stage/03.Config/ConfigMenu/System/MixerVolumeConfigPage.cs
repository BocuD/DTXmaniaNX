using DTXMania.Core;
using DTXMania.Core.Audio;
using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

/// <summary>One level per group, under the master. These apply live rather than on exit.</summary>
internal sealed class MixerVolumeConfigPage : ConfigPage
{
    private static readonly (AudioGroup group, string label, string japanese, string english)[] Groups =
    [
        (AudioGroup.Bgm, "BGM", "BGMの音量を指定します。",
            "Volume of background music."),
        (AudioGroup.Se, "SE", "メニュー音などの効果音の音量を指定します。",
            "Volume of interface sounds, such as menu movement and decision sounds."),
        (AudioGroup.Drums, "Drums", "ドラムの音量を指定します。",
            "Volume of drum sounds."),
        (AudioGroup.Bass, "Bass", "ベースの音量を指定します。",
            "Volume of bass sounds."),
        (AudioGroup.Guitar, "Guitar", "ギターの音量を指定します。",
            "Volume of guitar sounds.")
    ];

    public MixerVolumeConfigPage(ConfigList list) : base(list)
    {
    }

    public override List<CItemBase> Build()
    {
        List<CItemBase> items = [];

        string note = AudioMixer.Device.MixesGroups
            ? string.Empty
            : $"\n\nNote: {AudioMixer.Device.TypeName} does not mix groups separately, so this only " +
              "applies to sounds the game mixes itself.";

        foreach ((AudioGroup group, string label, string japanese, string english) in Groups)
        {
            items.Add(Volume(group, label, japanese, english + note));
        }

        items.Add(BackItem());
        return items;
    }

    private static CItemInteger Volume(AudioGroup group, string label, string japanese, string english)
    {
        CItemInteger item = new(label, 0, 100, CDTXMania.ConfigIni.nGroupVolume[(int)group],
            japanese + "\n0 ～ 100 % の値が指定可能です。",
            english + "\nYou can set 0 - 100.");

        item.BindConfig(
            () => item.nCurrentValue = CDTXMania.ConfigIni.nGroupVolume[(int)group],
            () =>
            {
                CDTXMania.ConfigIni.nGroupVolume[(int)group] = item.nCurrentValue;
                AudioMixer.SetGroupVolume(group, item.nCurrentValue);
            });

        return item;
    }
}
