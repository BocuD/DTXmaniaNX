using FDK;

namespace DTXMania.Core.Audio;

/// <summary>What the mixer graph has to be built as for an output to accept it.</summary>
internal readonly record struct BassMixerFormat(int Frequency, int Channels, bool Float);

/// <summary>
/// The half of a BASS device that owns the sound card. BASS itself is opened on the "no sound" device
/// and only decodes into the mixer; an output drags the result out and times it.
/// </summary>
internal interface IBassOutput : IDisposable
{
    /// <summary>eg "WASAPI(Exclusive)".</summary>
    string Backend { get; }

    /// <summary>The output that was opened, which is not the one asked for if that was empty or gone.</summary>
    string Name { get; }

    long BufferMs { get; }

    int SampleRate => 0;

    /// <summary>The buffer in sample frames, for backends configured that way rather than in time.</summary>
    int BufferFrames => 0;

    int PeriodFrames => 0;

    /// <summary>What the backend's own documentation calls one frame, for display.</summary>
    string FrameUnit => "frames";

    float CpuUsage { get; }

    long ElapsedMs { get; }

    /// <summary>
    /// Whether the output drags data out of the mixer, which is how WASAPI and ASIO work. False means BASS
    /// owns the card and plays the mixer itself, so the final stage has to sound rather than decode.
    /// </summary>
    bool Pulls => true;

    /// <summary>The buffer in milliseconds, from frames where the backend counts that way.</summary>
    double BufferLatencyMs => BufferFrames > 0 && SampleRate > 0
        ? BufferFrames * 1000.0 / SampleRate
        : BufferMs;

    /// <summary>How often the buffer is topped up, in milliseconds. 0 when the output does not say.</summary>
    double PeriodMs => PeriodFrames > 0 && SampleRate > 0
        ? PeriodFrames * 1000.0 / SampleRate
        : 0.0;

    /// <summary>What a sound waits here. An output overrides this only if it is neither pulled nor pushed
    /// in the ordinary way.</summary>
    AudioLatency Latency => AudioLatency.FromBuffer(BufferLatencyMs, PeriodMs, Pulls);

    /// <summary>Opens the card and answers the format the mixer must match. Throws if it cannot be
    /// opened, so the caller can fall back to another backend.</summary>
    BassMixerFormat Open(AudioDeviceOptions options);

    /// <summary>Starts on <paramref name="mixer"/>. Separate from <see cref="Open"/> because the mixer
    /// cannot be built until the format is known.</summary>
    void Start(int mixer);
}

/// <summary>
/// An output's own clock, interpolated. A pull callback moves it once per buffer, so a read between two
/// of them adds the system time since the last. Without that a chart is judged against a clock that
/// steps.
/// </summary>
internal sealed class BassOutputClock : IDisposable
{
    private CTimer? system = new(CTimer.EType.MultiMedia);
    private long elapsedMs;

    //negative until the first callback, since the system clock counts from boot and interpolating
    //against it before there is anything to interpolate from would answer the uptime
    private long systemMsAtUpdate = -1;

    /// <summary>Called from the output's pull callback.</summary>
    public void Update(long elapsed)
    {
        elapsedMs = elapsed;
        systemMsAtUpdate = SystemMs;
    }

    public long ElapsedMs
    {
        get
        {
            //one read each: the callback can land between them
            long elapsed = elapsedMs;
            long at = systemMsAtUpdate;

            //nothing has been handed to the card yet, so no time has passed on this clock
            return at < 0 ? 0 : elapsed + (SystemMs - at);
        }
    }

    public long SystemMs => system?.nSystemTimeMs ?? 0;

    public void Dispose()
    {
        system?.Dispose();
        system = null;
    }
}
