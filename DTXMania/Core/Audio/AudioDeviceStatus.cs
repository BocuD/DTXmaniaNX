namespace DTXMania.Core.Audio;

/// <summary>
/// What an output can say about itself. One record rather than a member each, so a new backend does not
/// grow the interface with whatever it happens to know.
/// </summary>
public readonly record struct AudioDeviceStatus
{
    public AudioDeviceStatus()
    {
    }

    /// <summary>eg "WASAPI(Exclusive)".</summary>
    public string Backend { get; init; } = string.Empty;

    /// <summary>Empty when the backend cannot say.</summary>
    public string Output { get; init; } = string.Empty;

    public string Mode { get; init; } = string.Empty;

    /// <summary>-1 when the backend has no figure to give.</summary>
    public long BufferMs { get; init; } = -1;

    /// <summary>0 when the backend does not say.</summary>
    public int SampleRate { get; init; }

    /// <summary>The output buffer in sample frames, which is what exclusive WASAPI and ASIO are
    /// configured in. 0 when the backend counts in time.</summary>
    public int BufferFrames { get; init; }

    /// <summary>How much the output moves at a time, in frames. 0 when the backend does not say.</summary>
    public int PeriodFrames { get; init; }

    /// <summary>What the backend's own documentation calls one frame, for display.</summary>
    public string FrameUnit { get; init; } = "frames";

    /// <summary>0 to 100.</summary>
    public float CpuUsage { get; init; }

    /// <summary>Sounds decoded and resident.</summary>
    public int Streams { get; init; }

    /// <summary>Channels attached to the mix, sounding or not.</summary>
    public int MixedChannels { get; init; }

    /// <summary>How the default output is connected, eg "USB". Empty when the backend does not look.</summary>
    public string DefaultOutputBusType { get; init; } = string.Empty;

    /// <summary>
    /// The buffer in milliseconds, taken from the frame count where there is one. This is the exact
    /// figure <see cref="BufferMs"/> rounds for display, and the one to calculate with.
    /// </summary>
    public double BufferLatencyMs => SampleRate > 0 && BufferFrames > 0
        ? BufferFrames * 1000.0 / SampleRate
        : BufferMs;

    /// <summary>How often the output is refilled, in milliseconds. 0 when the backend does not say.</summary>
    public double PeriodMs => SampleRate > 0 && PeriodFrames > 0
        ? PeriodFrames * 1000.0 / SampleRate
        : 0.0;
}
