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

    protected override void CreateElements() => AddElement(new ConfigAudioPanel());

    public override List<CItemBase> Build()
    {
        opened = true;
        List<CItemBase> items = [];

        items.Add(FolderItem("Mixer Volumes",
            "マスター・BGM・効果音・各楽器・チップの音量を設定します。",
            "Every level: master, BGM, sound effects, each instrument, and chips.", volumePage));

        var output = FolderItem("Audio Output",
            "サウンドの出力方式とドライバー設定を行います。",
            "Which layer and backend the game plays through, and that backend's own settings.",
            outputPage);
        output.formatDescription = () =>
        {
            string latency = "";

            var audio = AudioMixer.Device.Status;
            if (audio.BufferMs > 0)
            {
                string buffer = audio.BufferFrames > 0
                    ? $"{audio.BufferFrames} {audio.FrameUnit} ({audio.BufferLatencyMs:0.0}ms)"
                    : $"{audio.BufferLatencyMs:0.0}ms";

                string wait = AudioMixer.Device.Latency.IsKnown
                    ? $"{AudioMixer.Device.Latency.Ms:0.0}ms"
                    : "not reported";

                latency = $"Buffer: {buffer}\nLatency {wait}";
            }

            return !CDTXMania.isJapanese ? $"Current output device: {AudioMixer.Device.Status.Output}\nCurrent output driver: {AudioMixer.Device.Status.Backend}\nLatency estimates: {latency}"
                : $"現在の出力デバイス: {AudioMixer.Device.Status.Output}\n現在の出力ドライバー: {AudioMixer.Device.Status.Backend}\nレイテンシー推定値: {latency}";
        };
        items.Add(output);

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
