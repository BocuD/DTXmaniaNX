namespace DTXMania.Core.Audio;

/// <summary>
/// Stands in while there is no output: during a rebuild, and when none could be built at all. Answers
/// the system clock and plays nothing, so the game runs silent rather than not at all.
/// </summary>
public sealed class NullAudioDevice : IAudioDevice
{
    public AudioDeviceStatus Status => new() { Backend = "None" };

    public IAudioClip Load(string path, AudioGroup group) => new SilentClip();

    public int MasterVolume { get; set; } = 100;

    public bool TimeStretch { get; set; }

    public bool MixesChannels => false;

    public bool NeedsDriftCorrection => false;

    public long ElapsedMs => Environment.TickCount64;

    public long ElapsedMsFor(long deviceTimestamp) => deviceTimestamp;

    public void Dispose()
    {
    }
}

/// <summary>
/// Voices that go through the motions and make no sound. A null voice would leave the mixer's counts
/// wrong instead.
/// </summary>
internal sealed class SilentClip : IAudioClip
{
    public string VoiceKind => "silent";

    public long LengthMs => 0;

    public IAudioVoice CreateVoice() => new SilentVoice();

    public void Dispose()
    {
    }
}

internal sealed class SilentVoice : IAudioVoice
{
    public bool IsPlaying => false;
    public int Volume { get; set; }
    public int Pan { get; set; }
    public double Speed { get; set; } = 1.0;
    public double Pitch { get; set; } = 1.0;
    public long PositionMs => 0;

    public void Seek(long positionMs)
    {
    }

    public void Play(bool loop)
    {
    }

    public void Stop()
    {
    }

    public void Pause()
    {
    }

    public void Resume(long positionMs)
    {
    }

    public void DetachFromMixer()
    {
    }

    public void AttachToMixer()
    {
    }

    public void Dispose()
    {
    }
}
