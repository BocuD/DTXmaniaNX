using System.Diagnostics;
using FDK;
using SharpDX.DirectSound;
using SharpDX.Multimedia;

namespace DTXMania.Core.Audio;

/// <summary>
/// DirectSound, which mixes in the driver rather than in a graph of our own. It shares nothing with the
/// BASS backends but the decoders, so it is its own device rather than another output.
/// </summary>
public sealed class DirectSoundAudioDevice : IAudioDevice
{
    internal const BufferFlags BufferFlags = SharpDX.DirectSound.BufferFlags.Defer
                                             | SharpDX.DirectSound.BufferFlags.GetCurrentPosition2
                                             | SharpDX.DirectSound.BufferFlags.GlobalFocus
                                             | SharpDX.DirectSound.BufferFlags.ControlVolume
                                             | SharpDX.DirectSound.BufferFlags.ControlPan
                                             | SharpDX.DirectSound.BufferFlags.ControlFrequency;

    private DirectSound? directSound;
    private CTimer? clock;

    internal DirectSound Output => directSound ?? throw new ObjectDisposedException(nameof(DirectSoundAudioDevice));

    internal int Clips;

    public DirectSoundAudioDevice(AudioDeviceOptions options)
    {
        Trace.TraceInformation("Starting DirectSound initialization...");

        //SharpDX plays it, but the decoders are BASS, so it still needs the "no sound" device
        BassRuntime.Register();
        BassRuntime.OpenDecoder();

        Guid driver = Resolve(options.OutputDevice, out string name);
        Name = name;

        directSound = driver == Guid.Empty ? new DirectSound() : new DirectSound(driver);

        bool priority = true;

        try
        {
            directSound.SetCooperativeLevel(AudioDevice.WindowHandle, CooperativeLevel.Priority);
        }
        catch
        {
            directSound.SetCooperativeLevel(AudioDevice.WindowHandle, CooperativeLevel.Normal);
            priority = false;
        }

        clock = new CTimer(CTimer.EType.MultiMedia);

        Trace.TraceInformation($"DirectSound initialized on \"{Name}\" ({(priority ? "Priority" : "Normal")})");
    }

    internal string Name { get; }

    public AudioDeviceStatus Status => new()
    {
        Backend = "DirectSound",
        Output = Name,
        Streams = Clips
    };

    public IAudioClip Load(string path, AudioGroup group) => new DirectSoundClip(this, path);

    /// <summary>Set per buffer, so there is nothing to apply here.</summary>
    public int MasterVolume { get; set; } = 100;

    /// <summary>DirectSound changes speed by frequency, which changes pitch with it.</summary>
    public bool TimeStretch { get; set; }

    public bool MixesChannels => false;

    /// <summary>A buffer's position runs off the driver's clock, not <see cref="ElapsedMs"/>.</summary>
    public bool NeedsDriftCorrection => true;

    public long ElapsedMs => clock?.nSystemTimeMs ?? 0;

    //input is stamped on this same clock, so there is no offset to correct
    public long ElapsedMsFor(long deviceTimestamp) => deviceTimestamp;

    public void Dispose()
    {
        directSound?.Dispose();
        directSound = null;

        clock?.Dispose();
        clock = null;

        BassRuntime.CloseDecoder();
    }

    /// <summary>
    /// The driver GUID for a pinned name. Empty means the primary driver, which follows the system
    /// default; it is reported by name so a caller watching for the default to change sees them agree.
    /// </summary>
    private static Guid Resolve(string wanted, out string name)
    {
        name = string.Empty;

        foreach (DeviceInformation device in DirectSound.GetDevices())
        {
            if (wanted.Length > 0 && device.Description == wanted && device.DriverGuid != Guid.Empty)
            {
                name = device.Description;
                return device.DriverGuid;
            }

            if (device.DriverGuid == Guid.Empty)
            {
                name = device.Description;
            }
        }

        if (wanted.Length > 0)
        {
            Trace.TraceWarning($"Requested output device \"{wanted}\" was not found; using the default.");
        }

        return Guid.Empty;
    }
}
