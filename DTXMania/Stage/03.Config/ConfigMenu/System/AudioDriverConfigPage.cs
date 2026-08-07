using DTXMania.Core;
using DTXMania.Core.Audio;
using DTXMania.UI.Config;
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
                items.Add(BuildUseOsTimer());
                break;

            case 2: // ExclusiveWASAPI
            case 3: // SharedWASAPI
                items.Add(BuildWasapiBufferSize());
                items.Add(BuildWasapiEventDriven());
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
        CItemInteger item = new("WASAPIBufSize", 0, 99999, CDTXMania.ConfigIni.nWASAPIBufferSizeMs,
            "WASAPI時のバッファサイズ:\n0～99999msを指定できます。\n0を指定するとOSが自動設定します。\n値を小さくするほどラグが減少しますが、\n音割れや異常を引き起こす場合があります。",
            "Sound buffer size for WASAPI, from 0 to 99999ms.\nSet 0 to use the default system buffer size.\nSmaller values reduce lag but may cause audio glitches.\nNote: Exit CONFIG to make the setting take effect.");
        item.BindConfig(
            () => item.nCurrentValue = CDTXMania.ConfigIni.nWASAPIBufferSizeMs,
            () => CDTXMania.ConfigIni.nWASAPIBufferSizeMs = item.nCurrentValue);
        return item;
    }

    private static CItemToggle BuildWasapiEventDriven()
    {
        CItemToggle item = new("WASAPIEventDriven", CDTXMania.ConfigIni.bEventDrivenWASAPI,
            "WASAPIをEvent Drivenモードで使用します。\nサウンド出力の遅延を小さくできますが、\nシステム負荷は上昇します。",
            "Use WASAPI Event Driven mode.\nIt reduces sound output lag, but decreases system performance.");
        item.BindConfig(
            () => item.bON = CDTXMania.ConfigIni.bEventDrivenWASAPI,
            () => CDTXMania.ConfigIni.bEventDrivenWASAPI = item.bON);
        return item;
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
