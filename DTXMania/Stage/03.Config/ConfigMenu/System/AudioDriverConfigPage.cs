using DTXMania.Core;
using DTXMania.UI.Config;
using DTXMania.UI.Item;
using FDK;

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

        switch (CDTXMania.ConfigIni.nSoundDriverType)
        {
            case 0: // DirectSound
                items.Add(BuildAdjustWaves());
                break;

            case 1: // ASIO
                items.Add(BuildAsioDevice());
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

    private static CItemList BuildAsioDevice()
    {
        string[] asioDevices = CEnumerateAllAsioDevices.GetAllASIODevices();
        CItemList item = new("ASIO device", CItemBase.EPanelType.Normal, CDTXMania.ConfigIni.nASIODevice,
            "ASIOデバイス:\nASIO使用時の\nサウンドデバイスを選択\nします。\n",
            "ASIO Sound Device:\nSelect the sound device to use under ASIO mode.\n\nNote: Exit CONFIG to make the setting take effect.",
            asioDevices);
        item.BindConfig(
            () => item.nCurrentlySelectedIndex = CDTXMania.ConfigIni.nASIODevice,
            () => CDTXMania.ConfigIni.nASIODevice = item.nCurrentlySelectedIndex);
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
