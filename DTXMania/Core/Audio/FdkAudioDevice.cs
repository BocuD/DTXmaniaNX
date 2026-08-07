using System.Diagnostics;
using FDK;
using Un4seen.Bass;

namespace DTXMania.Core.Audio;

/// <summary>
/// Thin audio interface implemented on top of existing FDK audio implementation so we keep output during
/// migration to new audio mixer interface.
/// </summary>
public sealed class FdkAudioDevice : IAudioDevice
{
    public string TypeName => CDTXMania.SoundManager.GetCurrentSoundDeviceType();

    public IAudioClip Load(string path) => new FdkAudioClip(path);
}

public sealed class FdkAudioClip : IAudioClip
{
    //a sample is the whole file decoded in memory, which suits a short effect and not a long BGM — and a
    //BGM only ever needs one voice anyway, so it never reaches the point of asking for a second
    private const long SampleSizeLimit = 4 * 1024 * 1024;

    private readonly string path;
    private readonly List<IAudioVoice> voices = [];

    public string VoiceKind { get; private set; } = "stream";

    //the first voice is a CSound, which is also how the mixer this clip feeds is discovered
    private CSound? first;
    private int mixer;

    //one decode shared by every voice after the first; 0 until it is needed or if it cannot be had
    private int sample;
    private bool sampleTried;

    public FdkAudioClip(string path)
    {
        this.path = path;

        if (CreateVoice() == null)
        {
            throw new FileNotFoundException(path);
        }
    }

    public IAudioVoice? CreateVoice()
    {
        //the first voice comes from FDK, so a clip behaves exactly as it did before whatever happens next
        if (first == null)
        {
            CSound sound = CDTXMania.SoundManager.tGenerateSound(path);
            first = sound;
            mixer = sound.nMixerHandle;

            return Track(new FdkAudioVoice(sound), "stream");
        }

        if (SampleHandle() is { } handle && BassSampleVoice.Create(handle, mixer) is { } shared)
        {
            return Track(shared, "sample");
        }

        if (!first.bUsesBASS && Duplicate() is { } duplicated)
        {
            return Track(duplicated, "duplicate");
        }

        return Track(new FdkAudioVoice(CDTXMania.SoundManager.tGenerateSound(path)), "stream");
    }

    public void Dispose()
    {
        foreach (IAudioVoice voice in voices)
        {
            voice.Dispose();
        }

        voices.Clear();
        first = null;

        if (sample != 0)
        {
            Bass.BASS_SampleFree(sample);
            sample = 0;
        }
    }

    private IAudioVoice Track(IAudioVoice voice, string kind)
    {
        voices.Add(voice);
        VoiceKind = kind;
        return voice;
    }

    //loaded once, on the first voice that would otherwise cost a decode
    private int? SampleHandle()
    {
        if (sampleTried)
        {
            return sample == 0 ? null : sample;
        }

        sampleTried = true;

        if (first is not { bUsesBASS: true } || mixer == 0 || !Qualifies())
        {
            return null;
        }

        //BASS_SAMPLE_OVER_POS only decides what happens past max, which the mixer's own pool already
        //prevents; max is high enough that it is the runaway guard that bites first
        sample = Bass.BASS_SampleLoad(path, 0, 0, 64, BASSFlag.BASS_SAMPLE_OVER_POS);

        if (sample == 0)
        {
            Trace.TraceInformation($"'{Path.GetFileName(path)}' will not share a decode " +
                                   $"({Bass.BASS_ErrorGetCode()}); every voice loads it again.");
            return null;
        }

        return sample;
    }

    private bool Qualifies()
    {
        try
        {
            return new FileInfo(path).Length <= SampleSizeLimit;
        }
        catch
        {
            return false;
        }
    }

    private IAudioVoice? Duplicate()
    {
        try
        {
            return first?.Clone() is CSound clone ? new FdkAudioVoice(clone) : null;
        }
        catch (Exception e)
        {
            Trace.TraceInformation($"'{Path.GetFileName(path)}' cannot be duplicated: {e.Message}");
            return null;
        }
    }
}

public sealed class FdkAudioVoice : IAudioVoice
{
    private CSound? sound;

    public FdkAudioVoice(CSound sound)
    {
        this.sound = sound;
    }

    public bool IsPlaying => sound?.bIsPlaying ?? false;

    public int Volume
    {
        get => sound?.nVolume ?? 0;
        set
        {
            if (sound is { } current)
            {
                current.nVolume = value;
            }
        }
    }

    public int Pan
    {
        get => sound?.nPosition ?? 0;
        set
        {
            if (sound is { } current)
            {
                current.nPosition = value;
            }
        }
    }

    public void Play(bool loop) => sound?.tStartPlaying(loop);

    public void Stop() => sound?.tStopPlayback();

    public void DetachFromMixer()
    {
        //DirectSound has no mixer to remove them from
        if (sound is not { } current || CDTXMania.SoundManager.GetCurrentSoundDeviceType() == "DirectSound")
        {
            return;
        }

        CDTXMania.SoundManager.RemoveMixer(current);
    }

    public void Dispose()
    {
        if (sound is not { } current)
        {
            return;
        }

        current.tStopPlayback();
        CDTXMania.SoundManager.tDiscard(current);
        sound = null;
    }
}
