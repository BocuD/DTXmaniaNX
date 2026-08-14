using DTXMania.Core;
using DTXMania.Core.Audio;
using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

/// <summary>
/// Every level, in the order they multiply: master, then the group a sound belongs to, then whether a
/// chip was hit or played automatically. The last is a separate axis from the group, so a chip's level is
/// its group's and its origin's together.
/// </summary>
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
        List<CItemBase> items = [Master()];

        foreach ((AudioGroup group, string label, string japanese, string english) in Groups)
        {
            items.Add(Volume(group, label, japanese, english));
        }

        items.Add(Chips());
        items.Add(AutoChips());

        items.Add(BackItem());
        return items;
    }

    private static CItemInteger Master()
    {
        CItemInteger item = new("Master", 0, 100, CDTXMania.ConfigIni.nMasterVolume,
            "マスターボリュームの設定:\n全体の音量を設定します。\n0が無音で、100が最大値です。\n(WASAPI/ASIO時のみ有効です)",
            "Everything, under all the levels below.\nYou can set 0 - 100.\n\nNote:\nOnly for WASAPI/ASIO mode.");

        item.BindConfig(
            () => item.nCurrentValue = CDTXMania.ConfigIni.nMasterVolume,
            () =>
            {
                CDTXMania.ConfigIni.nMasterVolume = item.nCurrentValue;
                AudioMixer.MasterVolume = item.nCurrentValue;
            });

        return item;
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

    private static CItemInteger Chips()
    {
        CItemInteger item = new("Hit Chips", 0, 100, CDTXMania.ConfigIni.n手動再生音量,
            "打音の音量：\n入力に反応して再生される\nチップの音量を指定します。\n楽器ごとの音量と掛け合わされます。\n0 ～ 100 % の値が指定可能です。",
            "Chips you hit, whichever instrument they belong to.\nMultiplied with that instrument's level above.\nYou can set 0 - 100.");

        item.BindConfig(
            () => item.nCurrentValue = CDTXMania.ConfigIni.n手動再生音量,
            () => CDTXMania.ConfigIni.n手動再生音量 = item.nCurrentValue);

        return item;
    }

    private static CItemInteger AutoChips()
    {
        CItemInteger item = new("Auto Chips", 0, 100, CDTXMania.ConfigIni.n自動再生音量,
            "自動再生音の音量：\n自動的に再生されるチップの音量を\n指定します。\n楽器ごとの音量と掛け合わされます。\n0 ～ 100 % の値が指定可能です。",
            "Chips the game plays for you, whichever instrument they belong to.\nMultiplied with that instrument's level above.\nTurn this down to hear your own playing over the backing.\nYou can set 0 - 100.");

        item.BindConfig(
            () => item.nCurrentValue = CDTXMania.ConfigIni.n自動再生音量,
            () => CDTXMania.ConfigIni.n自動再生音量 = item.nCurrentValue);

        return item;
    }
}
