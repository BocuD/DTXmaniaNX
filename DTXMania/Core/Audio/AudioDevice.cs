using System.Diagnostics;

namespace DTXMania.Core.Audio;

public static class AudioDevice
{
    /// <summary>The window DirectSound sets its cooperative level on. Set once at startup.</summary>
    public static IntPtr WindowHandle { get; set; }

    /// <summary>
    /// Builds the output the settings ask for. A backend that cannot start falls back to a
    /// <see cref="NullAudioDevice"/> rather than throwing, so a machine with no working sound card
    /// still reaches the menu.
    /// </summary>
    public static IAudioDevice Create(AudioDeviceOptions options)
    {
        try
        {
            return Build(options);
        }
        catch (Exception e)
        {
            Trace.TraceError($"No audio output could be built: {e.Message}");
            return new NullAudioDevice();
        }
    }

    private static IAudioDevice Build(AudioDeviceOptions options)
    {
        if (options.UseFdk)
        {
            return new FdkAudioDevice(options);
        }

        return options.Backend == AudioBackend.DirectSound
            ? new DirectSoundAudioDevice(options)
            : new BassAudioDevice(options);
    }
}
