namespace DTXMania.Core.Audio;

public enum AudioBackend
{
    DirectSound,
    Asio,
    WasapiExclusive,
    WasapiShared
}

/// <summary>
/// Everything that decides what the output is. Changing any of it means building a new device, so it is
/// passed as a whole rather than set one property at a time.
/// </summary>
public sealed record AudioDeviceOptions
{
    public required AudioBackend Backend { get; init; }

    /// <summary>WASAPI output buffer, in ms. 0 leaves it to the device.</summary>
    public int BufferSizeMs { get; init; }

    /// <summary>WASAPI only: refill the buffer from the device's own event rather than by polling.</summary>
    public bool EventDriven { get; init; }

    public int AsioDevice { get; init; }

    /// <summary>
    /// The output to play through, by name. Empty follows the system default and moves with it. A name
    /// that matches nothing falls back to the default.
    /// </summary>
    public string OutputDevice { get; init; } = string.Empty;

    /// <summary>Time the performance from the OS rather than from the sound device.</summary>
    public bool UseOsTimer { get; init; }

    internal static AudioDeviceOptions FromConfig(CConfigIni config) => new()
    {
        Backend = config.nSoundDriverType switch
        {
            1 => AudioBackend.Asio,
            2 => AudioBackend.WasapiExclusive,
            3 => AudioBackend.WasapiShared,
            _ => AudioBackend.DirectSound
        },
        BufferSizeMs = config.nWASAPIBufferSizeMs,
        EventDriven = config.bEventDrivenWASAPI,
        AsioDevice = config.nASIODevice,
        UseOsTimer = config.bUseOSTimer,
        OutputDevice = config.strOutputDevice ?? string.Empty
    };
}
