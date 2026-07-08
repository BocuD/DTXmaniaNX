using DTXMania.Core;
using DTXMania.UI.Config;
using DTXMania.UI.Item;
using FDK;

namespace DTXMania;

internal sealed class AudioConfigPage : ConfigPage
{
    private readonly AudioDriverConfigPage driverPage;

    // snapshot taken on entry; the device is only rebuilt on exit if one of these changed
    private int soundTypeInitial;
    private int wasapiBufferInitial;
    private int asioDeviceInitial;
    private bool osTimerInitial;
    private bool opened;

    private CItemToggle timeStretch;

    public AudioConfigPage(ConfigList list) : base(list)
    {
        driverPage = new AudioDriverConfigPage(list);
    }

    public override void CacheInitialState()
    {
        soundTypeInitial = CDTXMania.ConfigIni.nSoundDriverType;
        wasapiBufferInitial = CDTXMania.ConfigIni.nWASAPIBufferSizeMs;
        asioDeviceInitial = CDTXMania.ConfigIni.nASIODevice;
        osTimerInitial = CDTXMania.ConfigIni.bUseOSTimer;
    }

    public override List<CItemBase> Build()
    {
        opened = true;
        List<CItemBase> items = [];

        CItemInteger masterVolume = new("MasterVolume", 0, 100, CDTXMania.ConfigIni.nMasterVolume,
            "マスターボリュームの設定:\n全体の音量を設定します。\n0が無音で、100が最大値です。\n(WASAPI/ASIO時のみ有効です)",
            "Master Volume:\nYou can set 0 - 100.\n\nNote:\nOnly for WASAPI/ASIO mode.");
        masterVolume.BindConfig(
            () => masterVolume.nCurrentValue = CDTXMania.ConfigIni.nMasterVolume,
            () =>
            {
                // master volume applies live while adjusting (matches the original config screen)
                CDTXMania.ConfigIni.nMasterVolume = masterVolume.nCurrentValue;
                CDTXMania.SoundManager.nMasterVolume = masterVolume.nCurrentValue;
            });
        items.Add(masterVolume);

        CItemInteger chipVolume = new("ChipVolume", 0, 100, CDTXMania.ConfigIni.n手動再生音量,
            "打音の音量：\n入力に反応して再生される\nチップの音量を指定します。\n0 ～ 100 % の値が指定可能\nです。\n",
            "Volume for chips you hit.\nYou can specify from 0 to 100%.");
        chipVolume.BindConfig(
            () => chipVolume.nCurrentValue = CDTXMania.ConfigIni.n手動再生音量,
            () => CDTXMania.ConfigIni.n手動再生音量 = chipVolume.nCurrentValue);
        items.Add(chipVolume);

        CItemInteger autoVolume = new("AutoVolume", 0, 100, CDTXMania.ConfigIni.n自動再生音量,
            "自動再生音の音量：\n自動的に再生される\nチップの音量を指定します。\n0 ～ 100 % の値が指定可能\nです。\n",
            "Volume for AUTO chips.\nYou can specify from 0 to 100%.");
        autoVolume.BindConfig(
            () => autoVolume.nCurrentValue = CDTXMania.ConfigIni.n自動再生音量,
            () => CDTXMania.ConfigIni.n自動再生音量 = autoVolume.nCurrentValue);
        items.Add(autoVolume);

        CItemInteger bgmAdjust = new("BGM Offset", -99, 99, CDTXMania.ConfigIni.nCommonBGMAdjustMs,
            "BGMの再生タイミングを微調整します。\n-99 ～ 99ms まで指定可能です。",
            "Fine-tune the BGM playback timing.\nYou can set from -99 to 99 ms.");
        bgmAdjust.BindConfig(
            () => bgmAdjust.nCurrentValue = CDTXMania.ConfigIni.nCommonBGMAdjustMs,
            () => CDTXMania.ConfigIni.nCommonBGMAdjustMs = bgmAdjust.nCurrentValue);
        items.Add(bgmAdjust);

        CItemToggle bgmSound = new("BGM Sound", CDTXMania.ConfigIni.bBGM音を発声する,
            "OFFにするとBGMを再生しません。",
            "Turn OFF if you don't want to play the song music (BGM).");
        bgmSound.BindConfig(
            () => bgmSound.bON = CDTXMania.ConfigIni.bBGM音を発声する,
            () => CDTXMania.ConfigIni.bBGM音を発声する = bgmSound.bON);
        items.Add(bgmSound);

        CItemToggle audienceSound = new("Audience", CDTXMania.ConfigIni.b歓声を発声する,
            "OFFにすると歓声を再生しません。\n（フィルインゾーン成功時などに再生されます）",
            "Turn OFF to disable crowd cheering.\n(Played e.g. after successfully clearing a fill-in zone.)");
        audienceSound.BindConfig(
            () => audienceSound.bON = CDTXMania.ConfigIni.b歓声を発声する,
            () => CDTXMania.ConfigIni.b歓声を発声する = audienceSound.bON);
        items.Add(audienceSound);

        timeStretch = new CItemToggle("TimeStretch", CDTXMania.ConfigIni.bTimeStretch,
            "演奏速度の変更方式:\nONにすると、\n演奏速度の変更を、\n周波数変更ではなく\nタイムストレッチで行います。",
            "PlaySpeed mode:\nTurn ON to use time stretch instead of frequency change.");
        timeStretch.BindConfig(
            () => timeStretch.bON = CDTXMania.ConfigIni.bTimeStretch,
            () => CDTXMania.ConfigIni.bTimeStretch = timeStretch.bON);
        items.Add(timeStretch);

        CItemList audioDriver = new("Audio Driver", CItemBase.EPanelType.Normal, CDTXMania.ConfigIni.nSoundDriverType,
            "サウンド出力方式を選択\nします。\nWASAPIはVista以降、\nASIOは対応機器でのみ使用可能です。\nWASAPIかASIOを使うと、\n遅延を少なくできます。\n",
            "DSound: Direct Sound\nWASAPI: from Windows Vista\nASIO: with ASIO compatible devices only\nUse WASAPI or ASIO to decrease the sound lag.\nNote: Exit CONFIG to make the setting take effect.",
            ["DSound", "ASIO", "WASAPIExclusive", "WASAPIShared"]);
        audioDriver.BindConfig(
            () => audioDriver.nCurrentlySelectedIndex = CDTXMania.ConfigIni.nSoundDriverType,
            () => CDTXMania.ConfigIni.nSoundDriverType = audioDriver.nCurrentlySelectedIndex);
        items.Add(audioDriver);

        items.Add(FolderItem("Audio Driver Options",
            "システムのオーディオドライバー設定に関する項目を設定します。",
            "Open the audio driver settings sub menu.", driverPage));

        items.Add(BackItem());
        return items;
    }

    /// <summary>
    /// Deferred sound-device apply, exactly like CActConfigList.HandleSoundDeviceChanges: only when
    /// the audio menu was opened and the driver/buffer/device/timer actually changed.
    /// </summary>
    public override void ApplyPendingChanges()
    {
        if (!opened) return;
        opened = false;

        if (soundTypeInitial != CDTXMania.ConfigIni.nSoundDriverType ||
            wasapiBufferInitial != CDTXMania.ConfigIni.nWASAPIBufferSizeMs ||
            asioDeviceInitial != CDTXMania.ConfigIni.nASIODevice ||
            osTimerInitial != CDTXMania.ConfigIni.bUseOSTimer)
        {
            ESoundDeviceType soundDeviceType = CDTXMania.ConfigIni.nSoundDriverType switch
            {
                0 => ESoundDeviceType.DirectSound,
                1 => ESoundDeviceType.ASIO,
                2 => ESoundDeviceType.ExclusiveWASAPI,
                3 => ESoundDeviceType.SharedWASAPI,
                _ => ESoundDeviceType.Unknown
            };

            CDTXMania.SoundManager.tInitialize(soundDeviceType,
                CDTXMania.ConfigIni.nWASAPIBufferSizeMs,
                false,
                0,
                CDTXMania.ConfigIni.nASIODevice,
                CDTXMania.ConfigIni.bUseOSTimer);

            CDTXMania.app.UpdateWindowTitle();
        }

        if (timeStretch != null)
        {
            CSoundManager.bIsTimeStretch = timeStretch.bON;
        }
    }
}
