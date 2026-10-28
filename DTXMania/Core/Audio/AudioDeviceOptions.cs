namespace DTXMania.Core.Audio;

//values are CConfigIni.nSoundDriverType
public enum AudioBackend
{
    DirectSound = 0,
    Asio = 1,
    WasapiExclusive = 2,
    WasapiShared = 3,

    /// <summary>BASS's own output, which is what runs on macOS and Linux.</summary>
    Bass = 4
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
    /// be two update periods instead of four. Only exclusive mode changes; Windows drives the shared
    /// engine either way.
    /// </summary>
    public bool EventDriven { get; init; }

    public int AsioDevice { get; init; }

    /// <summary>ASIO's buffer, in sample frames, 0 leaving the driver on its own setting. Separate from
    /// <see cref="BufferSizeMs"/> because ASIO is configured in samples, not time.</summary>
    public int AsioBufferSamples { get; init; }

    /// <summary>
    /// The output to play through, by name. Empty follows the system default and moves with it. A name
    /// that matches nothing falls back to the default.
    /// </summary>
    public string OutputDevice { get; init; } = string.Empty;

    /// <summary>A driver the platform cannot open is replaced by its fallback.</summary>
    internal static AudioDeviceOptions FromConfig(CConfigIni config)
    {
        AudioBackend backend = Enum.IsDefined(typeof(AudioBackend), config.nSoundDriverType)
            ? (AudioBackend)config.nSoundDriverType
            : AudioBackends.Fallback;

        return new AudioDeviceOptions
        {
            Backend = AudioBackends.Resolve(backend),
            BufferSizeMs = config.nWASAPIBufferSizeMs,
            EventDriven = config.bEventDrivenWASAPI,
            AsioDevice = config.nASIODevice,
            AsioBufferSamples = config.nASIOBufferSizeSamples,
            OutputDevice = config.strOutputDevice ?? string.Empty
        };
    }
}
