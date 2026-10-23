using System.Diagnostics;
using System.Drawing;
using DTXMania.Core;
using DTXMania.Core.Video;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using FDK;

namespace DTXMania;

internal class CActPerfVideo : CActivity
{
    public VideoPlayerController Player { get; private set; } = new();

    public bool loadFailed;

    [DataField] public bool IsPlaying => Player.IsPlaying;
    [DataField] public bool IsWide => Player.IsWide;

    public void CycleMovieMode() => CDTXMania.ConfigIni.nMovieMode = (CDTXMania.ConfigIni.nMovieMode + 1) % 4;

    //chart time the current video started at (or -1)
    private long startTimeMs = -1;

    private readonly CCounter[] fillInCounters = new CCounter[2];

    public void Start(CAVI avi, int startTimeMs)
    {
        Trace.TraceInformation("CActPerfVideo: Start(): " + avi.strFileName);

        this.startTimeMs = startTimeMs != -1 ? startTimeMs : AudioMixer.Timer.nCurrentTime;

        string path = VideoPath(avi.strFileName);
        if (!CDTXMania.ConfigIni.bAVIEnabled || !File.Exists(path) || !Player.TryLoadVideo(path))
        {
            loadFailed = true;
            return;
        }

        Player.LoopOnEof = false;
        Player.PlaybackSpeed = CDTXMania.ConfigIni.nPlaySpeed / 20.0f;
    }

    public void SkipStart(int timeMs)
    {
        if (CDTXMania.DTX == null)
            return;

        foreach (CChip chip in CDTXMania.DTX.listChip)
        {
            if (chip.nPlaybackTimeMs > timeMs)
                break;

            if (chip.eAVI種別 is EAVIType.AVI or EAVIType.AVIPAN && chip.rAVI != null)
            {
                Start(chip.rAVI, chip.nPlaybackTimeMs);
                Player.SeekToSeconds((timeMs - chip.nPlaybackTimeMs) / 1000.0);
            }
        }
    }

    public void Stop()
    {
        Trace.TraceInformation("CActPerfVideo: Stop()");
        startTimeMs = -1;
        Player.SeekToSeconds(0);
    }

    public void StartFillInEffect()
    {
        fillInCounters[0] = new CCounter(0, 30, 30, CDTXMania.Timer);
        fillInCounters[1] = null;
    }

    public override void OnActivate()
    {
        startTimeMs = -1;
        loadFailed = false;
        Player = new VideoPlayerController();

        base.OnActivate();
    }

    public override void OnDeactivate()
    {
        Player.Dispose();
        base.OnDeactivate();
    }

    public void Update()
    {
        if (!bActivated)
            return;

        long now = AudioMixer.Timer.nCurrentTime;
        Player.IsPlaying = !loadFailed && startTimeMs != -1 && now >= startTimeMs && Player.CurrentFrame.IsValid;

        if (Player.IsPlaying)
        {
            long chartTime = (long)(now * (CDTXMania.ConfigIni.nPlaySpeed / 20.0f));
            Player.SyncToSeconds((chartTime - startTimeMs) / 1000.0);
            Player.Update();
        }
    }

    public void DrawFillInEffect()
    {
        if (!bActivated || !CDTXMania.ConfigIni.DisplayBonusEffects || CDTXMania.GetCurrentInstrument() != 0)
            return;

        for (int i = 0; i < fillInCounters.Length; i++)
        {
            CCounter counter = fillInCounters[i];
            if (counter == null)
                continue;

            int frame = counter.nCurrentValue;
            counter.tUpdate();

            if (counter.bReachedEndValue)
            {
                counter.tStop();
                fillInCounters[i] = null;
                continue;
            }

            BaseTexture effect = CDTXMania.stagePerfDrumsScreen.txBonusEffect;
            if (effect != null)
            {
                effect.blendMode = BlendMode.Additive;
                effect.tDraw2D(0, -2, new RectangleF(0, 360 * frame, 640, 360));
            }
        }
    }

    private static string VideoPath(string filename)
    {
        if (CDTXMania.DTX == null || Path.IsPathRooted(filename))
            return filename;

        if (!string.IsNullOrEmpty(CDTXMania.DTX.PATH_WAV))
            return CDTXMania.DTX.PATH_WAV + filename;
        return CDTXMania.DTX.strFolderName + CDTXMania.DTX.PATH + filename;
    }
}
