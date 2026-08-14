using DTXMania.Core;
using DTXMania.Core.Audio;
using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

internal sealed class AudioConfigPage : ConfigPage
{
    private readonly AudioOutputConfigPage outputPage;
    private readonly MixerVolumeConfigPage volumePage;

    // snapshot taken on entry; the device is only rebuilt on exit if one of these changed
    private int soundTypeInitial;
    private int wasapiBufferInitial;
    private int asioDeviceInitial;
    private int asioBufferInitial;
    private string outputDeviceInitial;
    private bool osTimerInitial;
    private bool eventDrivenInitial;
    private bool fdkAudioInitial;
    private bool opened;

    private CItemToggle timeStretch;

    public AudioConfigPage(ConfigList list) : base(list)
    {
        //a rebuild done from the output page is not one this page has to repeat on the way out
        outputPage = new AudioOutputConfigPage(list, new AudioDriverConfigPage(list), CacheInitialState);
        volumePage = new MixerVolumeConfigPage(list);
    }

    public override void CacheInitialState()
    {
        soundTypeInitial = CDTXMania.ConfigIni.nSoundDriverType;
        wasapiBufferInitial = CDTXMania.ConfigIni.nWASAPIBufferSizeMs;
        asioDeviceInitial = CDTXMania.ConfigIni.nASIODevice;
        asioBufferInitial = CDTXMania.ConfigIni.nASIOBufferSizeSamples;
        osTimerInitial = CDTXMania.ConfigIni.bUseOSTimer;
        eventDrivenInitial = CDTXMania.ConfigIni.bEventDrivenWASAPI;
        fdkAudioInitial = CDTXMania.ConfigIni.bUseFDKAudio;
        outputDeviceInitial = CDTXMania.ConfigIni.strOutputDevice;
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
                AudioMixer.MasterVolume = masterVolume.nCurrentValue;
            });
        items.Add(masterVolume);

        items.Add(FolderItem("Mixer Volumes",
            "BGM・効果音・各楽器ごとの音量を設定します。",
            "Set the volume of BGM, sound effects and each instrument separately.", volumePage));

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

        CItemToggle speedAffectsChips = new("SpeedOnChips", CDTXMania.ConfigIni.bPlaySpeedAffectsChips,
            "演奏速度をチップ音にも適用します。\nOFFにすると曲だけが速度に追従し、\nチップ音は録音されたまま鳴ります。",
            "Apply PlaySpeed to chip sounds as well as the song.\nOFF: only the song follows it, and chips sound as recorded.\nON: chips follow it too, which detunes them unless TimeStretch is on.");
        speedAffectsChips.BindConfig(
            () => speedAffectsChips.bON = CDTXMania.ConfigIni.bPlaySpeedAffectsChips,
            () => CDTXMania.ConfigIni.bPlaySpeedAffectsChips = speedAffectsChips.bON);
        items.Add(speedAffectsChips);

        items.Add(FolderItem("Audio Output",
            "サウンドの出力方式とドライバー設定を行います。",
            "Which layer and backend the game plays through, and that backend's own settings.",
            outputPage));

        items.Add(BackItem());
        return items;
    }

    /// <summary>Rebuilds only when this menu was opened and something the device is built from
    /// changed.</summary>
    public override void ApplyPendingChanges()
    {
        if (!opened) return;
        opened = false;

        if (soundTypeInitial != CDTXMania.ConfigIni.nSoundDriverType ||
            wasapiBufferInitial != CDTXMania.ConfigIni.nWASAPIBufferSizeMs ||
            asioDeviceInitial != CDTXMania.ConfigIni.nASIODevice ||
            asioBufferInitial != CDTXMania.ConfigIni.nASIOBufferSizeSamples ||
            osTimerInitial != CDTXMania.ConfigIni.bUseOSTimer ||
            eventDrivenInitial != CDTXMania.ConfigIni.bEventDrivenWASAPI ||
            fdkAudioInitial != CDTXMania.ConfigIni.bUseFDKAudio ||
            outputDeviceInitial != CDTXMania.ConfigIni.strOutputDevice)
        {
            //a changed setting is worth trying even if the last one gave up for good
            AudioMixer.RetryOutput();

            //through the mixer, which has to give up its own channels before the rebuild frees them
            AudioMixer.Reinitialize(AudioDeviceOptions.FromConfig(CDTXMania.ConfigIni));
            CDTXMania.app.UpdateWindowTitle();
        }

        if (timeStretch != null)
        {
            AudioMixer.TimeStretch = timeStretch.bON;
        }
    }
}
