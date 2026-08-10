using System.Diagnostics;
using FDK;

namespace DTXMania.Core.Audio;

/// <summary>
/// <see cref="IAudioDevice"/> on top of FDK, kept only so the two device layers can be compared against
/// each other. Selected by <c>UseFDKAudio</c> in the config, and goes when FDK's audio does.
/// </summary>
public sealed class FdkAudioDevice : IAudioDevice
{
    private readonly CSoundManager manager;

    public FdkAudioDevice(AudioDeviceOptions options)
    {
        //FDK's device constructors read the requested output rather than being handed it
        CSoundManager.strRequestedOutputDevice = options.OutputDevice;

        //the ASIO buffer stays 0, meaning the device's own setting; only the WASAPI one is configurable
        manager = new CSoundManager(AudioDevice.WindowHandle,
            ToFdk(options.Backend),
            options.BufferSizeMs,
            options.EventDriven,
            0,
            AsioDevice(options),
            options.UseOsTimer);
    }

    public AudioDeviceStatus Status => new()
    {
        Backend = manager.GetCurrentSoundDeviceType(),
        Output = CSoundManager.strActiveOutputDevice,

        //DirectSound's figure is the requested delay rather than anything the device reported
        BufferMs = MixesChannels ? manager.GetSoundDelay() : -1,
        CpuUsage = manager.GetCPUusage(),
        Streams = manager.GetStreams(),
        MixedChannels = manager.GetMixingStreams(),
        DefaultOutputBusType = CSoundManager.strDefaultDeviceBusType
    };

    public IAudioClip Load(string path, AudioGroup group) => new FdkAudioClip(this, path, group);

    public int MasterVolume
    {
        get => manager.nMasterVolume;
        set => manager.nMasterVolume = value;
    }

    public bool TimeStretch
    {
        get => CSoundManager.bIsTimeStretch;
        set => CSoundManager.bIsTimeStretch = value;
    }

    public bool MixesChannels => CSoundManager.SoundDeviceType != ESoundDeviceType.DirectSound;

    public bool NeedsDriftCorrection => !MixesChannels;

    /// <summary>
    /// The ASIO driver index to open. ASIO picks by index, so a name is resolved to one; the stored index
    /// is the fallback when no name is set or it matches nothing.
    /// </summary>
    internal static int AsioDevice(AudioDeviceOptions options)
        => options.Backend == AudioBackend.Asio && AudioOutputs.AsioDriver(options.OutputDevice) is var n and >= 0
            ? n
            : options.AsioDevice;

    //the system clock while a device is being rebuilt, since there is no output clock to read
    public long ElapsedMs => CSoundManager.rcPerformanceTimer?.nSystemTimeMs ?? CSoundManager.nSystemClockMs;

    public long ElapsedMsFor(long deviceTimestamp)
        => CSoundManager.rcPerformanceTimer?.nSystemTimeMsFor(deviceTimestamp) ?? deviceTimestamp;

    public void Dispose() => manager.Dispose();

    internal CSound Generate(string path, CSound.EInstType instrument)
        => manager.tGenerateSound(path, instrument);

    internal void Discard(CSound sound) => manager.tDiscard(sound);

    internal void Attach(CSound sound) => manager.AddMixer(sound);

    internal void Detach(CSound sound) => manager.RemoveMixer(sound);

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
    private readonly FdkAudioDevice device;
    private readonly string path;
    private readonly CSound.EInstType instrument;
    private readonly List<IAudioVoice> voices = [];

    public string VoiceKind { get; private set; } = "stream";

    public long LengthMs => first?.nTotalPlayTimeMs ?? 0;

    private CSound? first;

    public FdkAudioClip(FdkAudioDevice device, string path, AudioGroup group)
    {
        this.device = device;
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
            CSound sound = device.Generate(path, instrument);
            first = sound;

            return Track(new FdkAudioVoice(device, sound), "stream");
        }

        if (!first.bUsesBASS && Duplicate() is { } duplicated)
        {
            return Track(duplicated, "duplicate");
        }

        return Track(new FdkAudioVoice(device, device.Generate(path, instrument)), "stream");
    }

    public void Dispose()
    {
        foreach (IAudioVoice voice in voices)
        {
            voice.Dispose();
        }

        voices.Clear();
        first = null;
    }

    private IAudioVoice Track(IAudioVoice voice, string kind)
    {
        voices.Add(voice);
        VoiceKind = kind;
        return voice;
    }

    private IAudioVoice? Duplicate()
    {
        try
        {
            return first?.Clone() is CSound clone ? new FdkAudioVoice(device, clone) : null;
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
    private readonly FdkAudioDevice device;
    private CSound? sound;

    public FdkAudioVoice(FdkAudioDevice device, CSound sound)
    {
        this.device = device;
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

    public double Speed
    {
        get => sound?.dbPlaySpeed ?? 1.0;
        set
        {
            if (sound is { } current)
            {
                current.dbPlaySpeed = value;
            }
        }
    }

    public double Pitch
    {
        get => sound?.db周波数倍率 ?? 1.0;
        set
        {
            if (sound is { } current)
            {
                current.db周波数倍率 = value;
            }
        }
    }

    public long PositionMs
    {
        get
        {
            if (sound is not { } current)
            {
                return 0;
            }

            current.t再生位置を取得する(out _, out double seconds);
            return (long)(seconds * 1000.0);
        }
    }

    public void Seek(long positionMs) => sound?.tChangePlaybackPosition(positionMs);

    public void Play(bool loop) => sound?.tStartPlaying(loop);

    public void Stop() => sound?.tStopPlayback();

    public void Pause() => sound?.tPausePlayback();

    public void Resume(long positionMs) => sound?.tResumePlayback(positionMs);

    public void DetachFromMixer()
    {
        if (sound is { } current && device.MixesChannels)
        {
            device.Detach(current);
        }
    }

    public void AttachToMixer()
    {
        if (sound is { } current && device.MixesChannels)
        {
            device.Attach(current);
        }
    }

    public void Dispose()
    {
        if (sound is not { } current)
        {
            return;
        }

        current.tStopPlayback();
        device.Discard(current);
        sound = null;
    }
}
