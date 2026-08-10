namespace DTXMania.Core.Audio;

public enum AudioBackend
{
    DirectSound,
    Asio,
    WasapiExclusive,
    WasapiShared,

    /// <summary>BASS's own output, which is what runs on macOS and Linux.</summary>
    Bass
}

/// <summary>
/// Everything that decides what the output is. Changing any of it means building a new device, so it is
/// passed as a whole rather than set one property at a time.
/// </summary>
public sealed record AudioDeviceOptions
{
    public required AudioBackend Backend { get; init; }

    /// <summary>Output buffer in ms, 0 leaving it to the device. The WASAPI buffer, or the BASS backend's
    /// device buffer; both are raised to whatever the card will accept.</summary>
    public int BufferSizeMs { get; init; }

    /// <summary>
    /// Refill the WASAPI buffer from the device's own event rather than by polling, which lets the buffer
    /// be two update periods instead of four. Only exclusive mode changes: in shared mode the engine's
    /// period decides and this reaches nothing.
    /// </summary>
    public bool EventDriven { get; init; }

    public int AsioDevice { get; init; }

    /// <summary>
    /// The output to play through, by name. Empty follows the system default and moves with it. A name
    /// that matches nothing falls back to the default.
    /// </summary>
    public string OutputDevice { get; init; } = string.Empty;

    /// <summary>Time the performance from the OS rather than from the sound device.</summary>
    public bool UseOsTimer { get; init; }

    /// <summary>
    /// Play through FDK's sound device rather than this layer's own. Kept only so the two can be compared
    /// against each other, and goes when FDK's audio does.
    /// </summary>
    public bool UseFdk { get; init; }

    internal static AudioDeviceOptions FromConfig(CConfigIni config) => new()
    {
        Backend = config.nSoundDriverType switch
        {
            1 => AudioBackend.Asio,
            2 => AudioBackend.WasapiExclusive,
            3 => AudioBackend.WasapiShared,
            4 => AudioBackend.Bass,
            _ => AudioBackend.DirectSound
        },
        BufferSizeMs = config.nWASAPIBufferSizeMs,
        EventDriven = config.bEventDrivenWASAPI,
        AsioDevice = config.nASIODevice,
        UseOsTimer = config.bUseOSTimer,
        UseFdk = config.bUseFDKAudio,
        OutputDevice = config.strOutputDevice ?? string.Empty
    };
}
