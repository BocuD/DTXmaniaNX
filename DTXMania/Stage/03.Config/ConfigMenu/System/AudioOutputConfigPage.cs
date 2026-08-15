using DTXMania.Core;
using DTXMania.Core.Audio;
using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

/// <summary>Applied on leaving CONFIG, or straight away by the rebuild at the bottom.</summary>
internal sealed class AudioOutputConfigPage : ConfigPage
{
    private readonly AudioDriverConfigPage driverPage;
    private readonly Action onRebuilt;

    //held so the legacy toggle can reshape the driver list without the page being reopened
    private CItemList audioDriver;

    private CItemList outputDevice;
    private IReadOnlyList<AudioOutput> outputs = [];

    private AudioBackend? listedBackend;

    private const string AutoDevice = "Auto (system default)";

    public AudioOutputConfigPage(ConfigList list, AudioDriverConfigPage driverPage, Action onRebuilt)
        : base(list)
    {
        this.driverPage = driverPage;
        this.onRebuilt = onRebuilt;
    }

    /// <summary>FDK has no BASS output and would fall through to DirectSound, so it is not offered
    /// one.</summary>
    private static string[] Drivers(bool legacy) => legacy
        ? ["DirectSound", "ASIO", "WASAPI Exclusive", "WASAPI Shared"]
        : ["DirectSound", "ASIO", "WASAPI Exclusive", "WASAPI Shared", "BASS"];

    protected override void CreateElements() => AddElement(new ConfigAudioPanel());

    public override List<CItemBase> Build()
    {
        List<CItemBase> items = [];

        audioDriver = new CItemList("Audio Driver", CItemBase.EPanelType.Normal, 0,
            "サウンドデバイスの種類を選択します。\nWASAPIまたはASIOが推奨です。BASSはポータブル用のフォールバックで、WASAPIよりもレイテンシが大きくなります。\n可能であればDirectSoundは避けてください。",
            "Selected output driver.\nWASAPI or ASIO is recommended. BASS: portable fallback, higher latency than WASAPI\nAvoid DirectSound if possible",
            Drivers(CDTXMania.ConfigIni.bUseFDKAudio));
        audioDriver.BindConfig(
            ShowDrivers,
            () =>
            {
                //committing the page writes every row, not just the edited one, so a driver that has not
                //changed must do nothing: relisting here would reset the device row's selection before
                //that row has had the chance to write it
                if (audioDriver.nCurrentlySelectedIndex == CDTXMania.ConfigIni.nSoundDriverType)
                {
                    return;
                }

                CDTXMania.ConfigIni.nSoundDriverType = audioDriver.nCurrentlySelectedIndex;

                //WASAPI is only worth its latency event driven: polling needs a buffer four update
                //periods long where the device driving it needs two
                if (CDTXMania.ConfigIni.nSoundDriverType is 2 or 3)
                {
                    CDTXMania.ConfigIni.bEventDrivenWASAPI = true;
                }

                ShowOutputs();
            });
        ShowDrivers();
        items.Add(audioDriver);

        items.Add(BuildOutputDevice());

        CItemToggle fdkAudio = new("Legacy Audio", CDTXMania.ConfigIni.bUseFDKAudio,
            "旧FDKサウンドデバイスを使用します。\n新しいオーディオ層に問題がある場合のみONにしてください。\n近い将来削除されます。",
            "Play through the old FDK sound device instead of the current audio layer.\nThis option will be removed in a future release.");
        fdkAudio.BindConfig(
            () => fdkAudio.bON = CDTXMania.ConfigIni.bUseFDKAudio,
            () =>
            {
                if (fdkAudio.bON == CDTXMania.ConfigIni.bUseFDKAudio)
                {
                    return;
                }

                CDTXMania.ConfigIni.bUseFDKAudio = fdkAudio.bON;
                ShowDrivers();
                ShowOutputs();
            });
        items.Add(fdkAudio);

        items.Add(FolderItem("Audio Driver Options",
            "選択中のドライバー固有の設定を行います。",
            "Settings belonging to the selected driver.", driverPage));

        items.Add(Rebuild());

        items.Add(BackItem());
        return items;
    }

    private CItemBase Rebuild()
    {
        return new CItemBase("Reinitialize Audio", CItemBase.EPanelType.Normal,
            "現在の設定でサウンドデバイスを開き直します。\nCONFIGを抜けるのを待たずに反映されます。",
            "Close the audio output and open it again on the current settings.\nApplies now rather than on leaving CONFIG.")
        {
            action = () =>
            {
                //a deliberate rebuild is worth attempting even if the last one gave up for good
                AudioMixer.RetryOutput();

                AudioMixer.Reinitialize(AudioDeviceOptions.FromConfig(CDTXMania.ConfigIni));
                CDTXMania.app.UpdateWindowTitle();

                onRebuilt();
            },
            formatValue = () => CDTXMania.isJapanese ? "実行" : "Apply now"
        };
    }

    private CItemBase BuildOutputDevice()
    {
        outputDevice = new CItemList("Output Device", CItemBase.EPanelType.Normal, 0,
            "サウンドの出力先デバイスを選択します。\nAutoにすると、Windowsの既定のデバイスに\n追従します（ヘッドホンを抜いたときなど）。\n*印は現在の既定のデバイスです。",
            "Output device to play through\nAuto follows the Windows default\n* marks the current system default",
            [AutoDevice]);

        outputDevice.BindConfig(ShowOutputs, WriteOutputDevice);

        ShowOutputs();
        return outputDevice;
    }

    private void WriteOutputDevice()
    {
        //index 0 is Auto, so a device sits one place further down the list than in outputs
        int index = outputDevice.nCurrentlySelectedIndex - 1;

        CDTXMania.ConfigIni.strOutputDevice = index >= 0 && index < outputs.Count
            ? outputs[index].Name
            : "";
    }

    private void ShowOutputs()
    {
        AudioBackend backend = AudioDeviceOptions.FromConfig(CDTXMania.ConfigIni).Backend;

        if (backend != listedBackend)
        {
            bool relisting = listedBackend.HasValue;
            listedBackend = backend;
            outputs = AudioOutputs.For(backend);

            if (relisting && IndexOf(CDTXMania.ConfigIni.strOutputDevice) == 0)
            {
                CDTXMania.ConfigIni.strOutputDevice = "";
            }
        }

        outputDevice.listItemValues.Clear();
        outputDevice.listItemValues.Add(AutoDevice);

        foreach (AudioOutput output in outputs)
        {
            outputDevice.listItemValues.Add(output.IsSystemDefault ? $"{output.Name} *" : output.Name);
        }

        outputDevice.nCurrentlySelectedIndex = IndexOf(CDTXMania.ConfigIni.strOutputDevice);
    }

    private int IndexOf(string name)
    {
        for (int n = 0; n < outputs.Count; n++)
        {
            if (outputs[n].Name == name)
            {
                return n + 1;
            }
        }

        return 0;
    }

    /// <summary>Moves the selection off a driver the current layer cannot open.</summary>
    private void ShowDrivers()
    {
        string[] drivers = Drivers(CDTXMania.ConfigIni.bUseFDKAudio);

        audioDriver.listItemValues.Clear();
        audioDriver.listItemValues.AddRange(drivers);

        if (CDTXMania.ConfigIni.nSoundDriverType >= drivers.Length)
        {
            CDTXMania.ConfigIni.nSoundDriverType = 3;
        }

        audioDriver.nCurrentlySelectedIndex = Math.Clamp(CDTXMania.ConfigIni.nSoundDriverType, 0, drivers.Length - 1);
    }
}
