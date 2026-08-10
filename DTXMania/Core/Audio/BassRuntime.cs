using Un4seen.Bass;
using Un4seen.Bass.AddOn.Mix;
using Un4seen.BassAsio;
using Un4seen.BassWasapi;

namespace DTXMania.Core.Audio;

/// <summary>The BASS libraries themselves, before any device exists.</summary>
internal static class BassRuntime
{
    private static bool registered;

    /// <summary>Hides the BASS splash. Only takes effect once.</summary>
    public static void Register()
    {
        if (registered)
        {
            return;
        }

        registered = true;
        BassNet.Registration("dtxmaniaxgk@gmail.com", "2X9182021152222");
    }

    /// <summary>
    /// Checks the DLLs against the headers Bass.Net was built for. A mismatch is a wrong file on disk,
    /// and left unchecked it fails in ways that look like a broken sound card.
    /// </summary>
    public static void RequireVersions(bool wasapi = false, bool asio = false)
    {
        Require("bass.dll", Utils.HighWord(Bass.BASS_GetVersion()), Bass.BASSVERSION);
        Require("bassmix.dll", Utils.HighWord(BassMix.BASS_Mixer_GetVersion()), BassMix.BASSMIXVERSION);

        if (wasapi)
        {
            Require("basswasapi.dll", Utils.HighWord(BassWasapi.BASS_WASAPI_GetVersion()), BassWasapi.BASSWASAPIVERSION);
        }

        if (asio)
        {
            Require("bassasio.dll", Utils.HighWord(BassAsio.BASS_ASIO_GetVersion()), BassAsio.BASSASIOVERSION);
        }
    }

    /// <summary>
    /// Opens the "no sound" device so <see cref="PcmDecoder"/> works. A backend that pulls from BASS
    /// already does this as part of opening its own output; DirectSound plays through SharpDX and would
    /// otherwise have no BASS at all.
    /// </summary>
    public static void OpenDecoder()
    {
        if (!Bass.BASS_Init(0, 48000, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero)
            && Bass.BASS_ErrorGetCode() is var error and not BASSError.BASS_ERROR_ALREADY)
        {
            System.Diagnostics.Trace.TraceWarning($"BASS_Init (decode device) failed: {error}");
        }
    }

    public static void CloseDecoder() => Bass.BASS_Free();

    private static void Require(string library, int found, int wanted)
    {
        if (found != wanted)
        {
            throw new DllNotFoundException($"{library} is version {found}; this program needs {wanted}.");
        }
    }
}
