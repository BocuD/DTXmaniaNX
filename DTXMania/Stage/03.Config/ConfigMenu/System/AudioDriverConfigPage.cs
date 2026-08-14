using DTXMania.Core;
using DTXMania.Core.Audio;
using DTXMania.UI.Config;
using DTXMania.UI.Drawable;
using DTXMania.UI.Item;

namespace DTXMania;

/// <summary>
/// The audio-driver sub-page. Which items are shown depends on the currently-selected driver
/// (written to config by the parent page's "Audio Driver" item). Changes here write straight to
/// config; the actual device rebuild is applied on exit by <see cref="AudioConfigPage"/>.
/// </summary>
internal sealed class AudioDriverConfigPage : ConfigPage
{
    public AudioDriverConfigPage(ConfigList list) : base(list)
    {
    }

    public override List<CItemBase> Build()
    {
        List<CItemBase> items = [];

        items.Add(BuildOutputDevice());

        switch (CDTXMania.ConfigIni.nSoundDriverType)
        {
            case 0: // DirectSound
                items.Add(BuildAdjustWaves());
                break;

            case 1: // ASIO
                items.Add(BuildAsioBufferSize());
                items.Add(BuildUseOsTimer());
                break;

            case 2: // ExclusiveWASAPI
                items.Add(BuildWasapiBufferSize());

                //shared mode does not get one: Windows drives its engine either way
                items.Add(BuildWasapiEventDriven());
                items.Add(BuildUseOsTimer());
                break;

            case 3: // SharedWASAPI
                items.Add(BuildWasapiBufferSize());
                items.Add(BuildUseOsTimer());
                break;

            case 4: // BASS
                items.Add(BuildBassBufferSize());
                items.Add(BuildUseOsTimer());
                break;
        }

        items.Add(BackItem());
        return items;
    }

    /// <summary>
    /// Lists what the driver selected in config can play through. That is not necessarily the driver
    /// running, since a driver change only takes effect on exit.
    /// </summary>
    private static CItemList BuildOutputDevice()
    {
        AudioBackend backend = AudioDeviceOptions.FromConfig(CDTXMania.ConfigIni).Backend;
        IReadOnlyList<AudioOutput> outputs = AudioOutputs.For(backend);

        //index 0 is Auto, so a device sits one place further down the list than in outputs
        List<string> names = ["Auto (system default)"];
        names.AddRange(outputs.Select(output => output.IsSystemDefault ? $"{output.Name} *" : output.Name));

        int selected = 0;
        for (int n = 0; n < outputs.Count; n++)
        {
            if (outputs[n].Name == CDTXMania.ConfigIni.strOutputDevice)
            {
                selected = n + 1;
                break;
            }
        }

        CItemList item = new("Output Device", CItemBase.EPanelType.Normal, selected,
            "サウンドの出力先デバイスを選択します。\nAutoにすると、Windowsの既定のデバイスに\n追従します（ヘッドホンを抜いたときなど）。\n*印は現在の既定のデバイスです。",
            "Output device to play through\nAuto follows the Windows default\n* marks the current system default\n\nNote: Exit CONFIG to apply",
            names.ToArray());

        item.BindConfig(
            () => { },
            () => CDTXMania.ConfigIni.strOutputDevice = item.nCurrentlySelectedIndex <= 0
                ? ""
                : outputs[item.nCurrentlySelectedIndex - 1].Name);

        return item;
    }

    private static CItemToggle BuildAdjustWaves()
    {
        CItemToggle item = new("AdjustWaves", CDTXMania.ConfigIni.bWave再生位置自動調整機能有効,
            "サウンド再生位置自動補正：\nハードウェアやOSに起因する\nサウンドのずれを補正します。\n通常はONを推奨します。\n※DirectSound使用時のみ有効です。",
            "Automatically corrects sound-playback position drift caused by hardware/OS.\nUsually best left ON.\nNote: effective only when DirectSound is used.");
        item.BindConfig(
            () => item.bON = CDTXMania.ConfigIni.bWave再生位置自動調整機能有効,
            () => CDTXMania.ConfigIni.bWave再生位置自動調整機能有効 = item.bON);
        return item;
    }

    private static CItemInteger BuildWasapiBufferSize()
    {
        CItemInteger item = new("WASAPIBufSize", 0, 500, CDTXMania.ConfigIni.nWASAPIBufferSizeMs,
            "WASAPI時のバッファサイズ:\n0を指定するとデバイスが扱える最小値に\nなります。音切れが出る場合は増やして\nください。\n実際の値はデバイスの下限まで\n切り上げられます。\nこのバッファがそのまま出力遅延に\nなります。",
            "Output buffer for WASAPI, in ms. 0 asks for the lowest the device will take.\nIt is rounded to whole sample frames and never goes below the device's own\nfloor, so the window may show more than you asked for.\nThis buffer is the output latency: a hit waits out at most one of it, and\nhalf a fill period less than that on average.\nRaise it if you hear crackling or dropouts.\nNote: Exit CONFIG to make the setting take effect.");
        item.BindConfig(
            () => item.nCurrentValue = CDTXMania.ConfigIni.nWASAPIBufferSizeMs,
            () => CDTXMania.ConfigIni.nWASAPIBufferSizeMs = item.nCurrentValue);
        return item;
    }

    private static CItemInteger BuildAsioBufferSize()
    {
        CItemInteger item = new("ASIOBufSize", 0, 8192, CDTXMania.ConfigIni.nASIOBufferSizeSamples,
            "ASIO時のバッファサイズ(単位:サンプル):\n0を指定するとドライバ側の設定値を\n使用します。\n音切れが出る場合は増やしてください。",
            "ASIO buffer, in samples. 0 uses whatever the driver's own control panel is set to.\nA value the driver will not take is corrected rather than refused, so the\nwindow may show something other than what you asked for.\nRaise it if you hear crackling or dropouts.\nNote: Exit CONFIG to make the setting take effect.");
        item.BindConfig(
            () => item.nCurrentValue = CDTXMania.ConfigIni.nASIOBufferSizeSamples,
            () => CDTXMania.ConfigIni.nASIOBufferSizeSamples = item.nCurrentValue);
        return item;
    }

    private static CItemInteger BuildBassBufferSize()
    {
        CItemInteger item = new("BASSBufSize", 0, 200, CDTXMania.ConfigIni.nWASAPIBufferSizeMs,
            "BASS出力時のデバイスバッファ:\n0を指定すると10msになります。\nサウンドカードの最小値まで自動的に\n切り上げられます。",
            "Device buffer for the BASS output, in ms — this is the output latency.\n0 uses 10ms.\nBASS raises it to the sound card's own minimum, so the window may show\nmore than you asked for.\nNote: Exit CONFIG to make the setting take effect.");
        item.BindConfig(
            () => item.nCurrentValue = CDTXMania.ConfigIni.nWASAPIBufferSizeMs,
            () => CDTXMania.ConfigIni.nWASAPIBufferSizeMs = item.nCurrentValue);
        return item;
    }

    private CItemToggle BuildWasapiEventDriven()
    {
        CItemToggle item = new("WASAPIEventDriven", CDTXMania.ConfigIni.bEventDrivenWASAPI,
            "WASAPIをEvent Drivenモードで使用します。\n出力バッファを大幅に小さくできます。\nOFFにすると遅延が増加します。",
            "Let the device drive the WASAPI buffer instead of polling it.\nOn exclusive mode a polled buffer is several times the size of a driven one.\nLeave it ON unless you hear dropouts.");
        item.BindConfig(
            () => item.bON = CDTXMania.ConfigIni.bEventDrivenWASAPI,
            () =>
            {
                bool wasOn = CDTXMania.ConfigIni.bEventDrivenWASAPI;
                CDTXMania.ConfigIni.bEventDrivenWASAPI = item.bON;

                if (wasOn && !item.bON)
                {
                    _ = ConfirmPolling(item);
                }
            });
        return item;
    }

    /// <summary>
    /// Polling needs the buffer to be four update periods where the device driving it needs two, so
    /// turning this off multiplies the exclusive buffer.
    /// </summary>
    private async Task ConfirmPolling(CItemToggle item)
    {
        string title = CDTXMania.isJapanese ? "遅延が増加します" : "This increases latency";

        string description = CDTXMania.isJapanese
            ? "Event Drivenを切ると、WASAPI排他モードの出力バッファが\n数倍に大きくなります。\n音切れが発生する場合以外はONのままを推奨します。"
            : "Turning this off makes the WASAPI exclusive output buffer several times\nlarger, because a polled buffer has to be four update periods long where\na driven one is two.\n\nOnly do this if you are hearing dropouts.";

        string[] options = CDTXMania.isJapanese
            ? ["ONのままにする", "OFFにする"]
            : ["Keep it on", "Turn it off"];

        int choice = await Modal.ShowAsync(CDTXMania.persistentUIGroup, title, description, options);

        //anything but a deliberate "turn it off" puts it back, including dismissing the dialog
        if (choice != 1)
        {
            CDTXMania.ConfigIni.bEventDrivenWASAPI = true;
            item.bON = true;
            CDTXMania.RunOnMainThread(list.RefreshValues);
        }
    }

    private static CItemToggle BuildUseOsTimer()
    {
        CItemToggle item = new("UseOSTimer", CDTXMania.ConfigIni.bUseOSTimer,
            "OSタイマーを使用するかどうか:\nOS標準タイマーを使うとスクロールが滑らかに\nなりますが、演奏で音ズレが発生することが\nあります。\nこの指定はWASAPI/ASIO使用時のみ有効です。\n",
            "Use OS Timer or not.\nON = smooth scroll but may cause sound lag; OFF = original timer.\nAvailable only when using WASAPI/ASIO.");
        item.BindConfig(
            () => item.bON = CDTXMania.ConfigIni.bUseOSTimer,
            () => CDTXMania.ConfigIni.bUseOSTimer = item.bON);
        return item;
    }
}
