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

    /// <summary>-1 when the backend has no figure to give.</summary>
    public long BufferMs { get; init; } = -1;

    /// <summary>0 to 100.</summary>
    public float CpuUsage { get; init; }

    /// <summary>Sounds decoded and resident.</summary>
    public int Streams { get; init; }

    /// <summary>Channels attached to the mix, sounding or not.</summary>
    public int MixedChannels { get; init; }

    /// <summary>How the default output is connected, eg "USB". Empty when the backend does not look.</summary>
    public string DefaultOutputBusType { get; init; } = string.Empty;
}
