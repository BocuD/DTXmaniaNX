using System.Diagnostics;
using FDK;
using Un4seen.Bass;

namespace DTXMania.Core.Audio;

/// <summary>
/// <see cref="IAudioDevice"/> on top of FDK, so there is output while the new audio layer is built.
/// </summary>
public sealed class FdkAudioDevice : IAudioDevice
{
    public string TypeName => CDTXMania.SoundManager.GetCurrentSoundDeviceType();

    public IAudioClip Load(string path, AudioGroup group) => new FdkAudioClip(path, group);

    public int MasterVolume
    {
        get => CDTXMania.SoundManager.nMasterVolume;
        set => CDTXMania.SoundManager.nMasterVolume = value;
    }

    public int GetGroupVolume(AudioGroup group) => CDTXMania.SoundManager.nGetGroupVolume(ToFdk(group));

    public void SetGroupVolume(AudioGroup group, int volume)
        => CDTXMania.SoundManager.tSetGroupVolume(ToFdk(group), volume);

    //only WASAPI has a mixer per instrument group; ASIO has one for everything and DirectSound has none
    public bool MixesGroups => CSoundManager.SoundDeviceType
        is ESoundDeviceType.ExclusiveWASAPI or ESoundDeviceType.SharedWASAPI;

    public void Reinitialize(AudioDeviceOptions options)
    {
        Request(options);

        //the ASIO buffer stays 0, meaning the device's own setting; only the WASAPI one is configurable
        CDTXMania.SoundManager.tInitialize(ToFdk(options.Backend),
            options.BufferSizeMs,
            options.EventDriven,
            0,
            AsioDevice(options),
            options.UseOsTimer);
    }

    /// <summary>
    /// Hands the requested output to FDK, whose device constructors read it. Also needed before the first
    /// device, which is built by FDK's constructor rather than here.
    /// </summary>
    internal static void Request(AudioDeviceOptions options)
        => CSoundManager.strRequestedOutputDevice = options.OutputDevice;

    /// <summary>
    /// The ASIO driver index to open. ASIO picks by index, so a name is resolved to one; the stored index
    /// is the fallback when no name is set or it matches nothing.
    /// </summary>
    internal static int AsioDevice(AudioDeviceOptions options)
        => options.Backend == AudioBackend.Asio && AudioOutputs.AsioDriver(options.OutputDevice) is var n and >= 0
            ? n
            : options.AsioDevice;

    public IReadOnlyList<AudioOutput> Outputs
        => AudioOutputs.For(CurrentBackend);

    public string CurrentOutput => CSoundManager.strActiveOutputDevice;

    private static AudioBackend CurrentBackend => CSoundManager.SoundDeviceType switch
    {
        ESoundDeviceType.ASIO => AudioBackend.Asio,
        ESoundDeviceType.ExclusiveWASAPI => AudioBackend.WasapiExclusive,
        ESoundDeviceType.SharedWASAPI => AudioBackend.WasapiShared,
        _ => AudioBackend.DirectSound
    };

    internal static CSound.EInstType ToFdk(AudioGroup group) => group switch
    {
        AudioGroup.Bgm => CSound.EInstType.BGM,
        AudioGroup.Drums => CSound.EInstType.Drums,
        AudioGroup.Bass => CSound.EInstType.Bass,
        AudioGroup.Guitar => CSound.EInstType.Guitar,
        _ => CSound.EInstType.SE
    };

    internal static ESoundDeviceType ToFdk(AudioBackend backend) => backend switch
    {
        AudioBackend.Asio => ESoundDeviceType.ASIO,
        AudioBackend.WasapiExclusive => ESoundDeviceType.ExclusiveWASAPI,
        AudioBackend.WasapiShared => ESoundDeviceType.SharedWASAPI,
        _ => ESoundDeviceType.DirectSound
    };
}

public sealed class FdkAudioClip : IAudioClip
{
    //a sample is the whole file decoded in memory, which suits a short effect and not a long BGM. A BGM
    //only ever needs one voice, so it never asks for a second anyway
    private const long SampleSizeLimit = 4 * 1024 * 1024;

    private readonly string path;
    private readonly CSound.EInstType instrument;
    private readonly List<IAudioVoice> voices = [];

    public string VoiceKind { get; private set; } = "stream";

    private CSound? first;
    private int mixer;

    //one decode shared by every voice after the first; 0 until it is needed or if it cannot be had
    private int sample;
    private bool sampleTried;

    public FdkAudioClip(string path, AudioGroup group)
    {
        instrument = FdkAudioDevice.ToFdk(group);

        //no voice yet; making one here would leave a channel nobody holds. The file is checked, not read
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(path);
        }

        this.path = path;
    }

    public IAudioVoice? CreateVoice()
    {
        //the first voice comes from FDK, and is where the mixer handle for the rest comes from
        if (first == null)
        {
            CSound sound = CDTXMania.SoundManager.tGenerateSound(path, instrument);
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

        return Track(new FdkAudioVoice(CDTXMania.SoundManager.tGenerateSound(path, instrument)), "stream");
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

        //64 is above the mixer's own runaway guard, so BASS_SAMPLE_OVER_POS never has to decide anything
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
