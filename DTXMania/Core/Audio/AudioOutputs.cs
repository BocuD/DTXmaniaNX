using System.Diagnostics;
using SharpDX.DirectSound;
using Un4seen.Bass;
using Un4seen.BassAsio;
using Un4seen.BassWasapi;

namespace DTXMania.Core.Audio;

public readonly record struct AudioOutput(string Name, bool IsSystemDefault);

/// <summary>
/// What each backend can play through. Answers in names rather than indices, because an index moves when
/// a device is plugged in. Safe before the device is initialised; too slow for every frame.
/// </summary>
public static class AudioOutputs
{
    public static IReadOnlyList<AudioOutput> For(AudioBackend backend)
    {
        try
        {
            return backend switch
            {
                AudioBackend.Asio => Asio(),
                AudioBackend.Bass => BassDevices(),
                AudioBackend.WasapiExclusive or AudioBackend.WasapiShared => Wasapi(),
                _ => DirectSound()
            };
        }
        catch (Exception e)
        {
            //a backend whose driver is not installed throws rather than returning nothing
            Trace.TraceWarning($"Could not list {backend} outputs: {e.Message}");
            return [];
        }
    }

    /// <summary>The output Windows would pick right now, or empty if the backend has no default.</summary>
    public static string SystemDefault(AudioBackend backend)
    {
        foreach (AudioOutput output in For(backend))
        {
            if (output.IsSystemDefault)
            {
                return output.Name;
            }
        }

        return string.Empty;
    }

    private static List<AudioOutput> Wasapi()
    {
        List<AudioOutput> outputs = [];

        for (int n = 0; BassWasapi.BASS_WASAPI_GetDeviceInfo(n) is { } info; n++)
        {
            //disabled means unplugged or switched off
            if ((info.flags & BASSWASAPIDeviceInfo.BASS_DEVICE_ENABLED) == 0
                || (info.flags & BASSWASAPIDeviceInfo.BASS_DEVICE_INPUT) != 0)
            {
                continue;
            }

            outputs.Add(new AudioOutput(info.name, info.IsDefault));
        }

        return outputs;
    }

    private static List<AudioOutput> BassDevices()
    {
        List<AudioOutput> outputs = [];

        //from 1: device 0 is "no sound", which is what the other backends decode on
        for (int n = 1; Bass.BASS_GetDeviceInfo(n) is { } info; n++)
        {
            if (info.IsEnabled)
            {
                outputs.Add(new AudioOutput(info.name, info.IsDefault));
            }
        }

        return outputs;
    }

    private static List<AudioOutput> Asio()
    {
        List<AudioOutput> outputs = [];

        //ASIO has no system default; a driver is chosen, not inherited
        foreach (BASS_ASIO_DEVICEINFO info in BassAsio.BASS_ASIO_GetDeviceInfos())
        {
            outputs.Add(new AudioOutput(info.name, false));
        }

        return outputs;
    }

    private static List<AudioOutput> DirectSound()
    {
        List<AudioOutput> outputs = [];

        foreach (DeviceInformation info in SharpDX.DirectSound.DirectSound.GetDevices())
        {
            //the primary driver has an empty GUID and follows whatever Windows defaults to
            outputs.Add(new AudioOutput(info.Description, info.DriverGuid == Guid.Empty));
        }

        return outputs;
    }

    /// <summary>The index of the ASIO driver with this name, or -1 if there is none.</summary>
    internal static int AsioDriver(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return -1;
        }

        BASS_ASIO_DEVICEINFO[] drivers = BassAsio.BASS_ASIO_GetDeviceInfos();

        for (int n = 0; n < drivers.Length; n++)
        {
            if (drivers[n].name == name)
            {
                return n;
            }
        }

        return -1;
    }
}
