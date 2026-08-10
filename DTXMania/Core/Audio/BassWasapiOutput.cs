using System.Diagnostics;
using FDK;
using Un4seen.Bass;
using Un4seen.BassWasapi;

namespace DTXMania.Core.Audio;

/// <summary>
/// WASAPI, shared or exclusive. BASS decodes into the mixer and this pulls the result into the WASAPI
/// buffer, timing playback from how much it has handed over.
/// </summary>
internal sealed class BassWasapiOutput : IBassOutput
{
    private readonly bool exclusive;
    private readonly BassOutputClock clock = new();

    //held as a field or the GC moves the address out from under the unmanaged side
    private readonly WASAPIPROC pull;

    private int mixer;
    private long mixerBytesPerSecond;
    private long transferredBytes;
    private bool opened;

    public BassWasapiOutput(bool exclusive)
    {
        this.exclusive = exclusive;
        pull = Pull;
    }

    public string Backend => exclusive ? "WASAPI(Exclusive)" : "WASAPI(Shared)";

    public string Name { get; private set; } = string.Empty;

    public long BufferMs { get; private set; }

    public float CpuUsage => BassWasapi.BASS_WASAPI_GetCPU();

    public long ElapsedMs => clock.ElapsedMs;

    /// <summary>The bus the default output is on, eg "USB".</summary>
    public string DefaultBusType { get; private set; } = string.Empty;

    public BassMixerFormat Open(AudioDeviceOptions options)
    {
        Trace.TraceInformation($"Starting BASS ({Backend}) initialization...");

        BassRuntime.Register();
        BassRuntime.RequireVersions(wasapi: true);

        //BASS does not update its own streams; WASAPI pulls them
        Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATEPERIOD, 0);

        string wanted = options.OutputDevice;
        string systemDefault = FindSystemDefault();

        (int index, BASS_WASAPI_DEVICEINFO device) = Find(wanted.Length > 0 ? wanted : systemDefault, wanted.Length > 0)
                                                     ?? Find(systemDefault, false)
                                                     ?? throw new Exception("No WASAPI output device was found.");

        //BASS is opened on "no sound" at the device's own rate, so nothing is resampled on the way in
        int frequency = device.mixfreq > 0 ? device.mixfreq : 44100;

        if (!Bass.BASS_Init(0, frequency, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero)
            && Bass.BASS_ErrorGetCode() != BASSError.BASS_ERROR_ALREADY)
        {
            throw new Exception($"BASS ({Backend}) initialization failed. "
                                + $"(BASS_Init)[{Bass.BASS_ErrorGetCode()}]");
        }

        InitWasapi(index, device, options);
        opened = true;

        BASS_WASAPI_INFO info = BassWasapi.BASS_WASAPI_GetInfo();

        //what was opened, not what was searched for: the retries can land elsewhere
        Name = BassWasapi.BASS_WASAPI_GetDeviceInfo(BassWasapi.BASS_WASAPI_GetDevice())?.name ?? string.Empty;

        return new BassMixerFormat(info.freq, info.chans, true);
    }

    public void Start(int mixer)
    {
        this.mixer = mixer;

        BASS_CHANNELINFO info = Bass.BASS_ChannelGetInfo(mixer);

        //float samples, which is what the mixer is created as
        mixerBytesPerSecond = (long)info.chans * 4 * info.freq;

        BassWasapi.BASS_WASAPI_Start();
    }

    public void Dispose()
    {
        if (opened)
        {
            //before the clock: the pull callback reads it
            BassWasapi.BASS_WASAPI_Free();
            Bass.BASS_Free();
            opened = false;
        }

        clock.Dispose();
    }

    /// <summary>
    /// Hands the mixed output straight to WASAPI, and takes the elapsed time from the running total
    /// rather than from this call's size, so a short or missed transfer does not move the clock.
    /// </summary>
    private int Pull(IntPtr buffer, int length, IntPtr user)
    {
        int transferred = Bass.BASS_ChannelGetData(mixer, buffer, length);

        if (transferred == -1)
        {
            transferred = 0;
        }

        //asked for as late as possible, so it describes the same moment the time is stamped with
        int unplayed = BassWasapi.BASS_WASAPI_GetData(null, (int)BASSData.BASS_DATA_AVAILABLE);

        clock.Update((transferredBytes - unplayed) * 1000 / mixerBytesPerSecond);

        transferredBytes += transferred;
        return transferred;
    }

    /// <summary>The name of the enabled BASS device Windows flags as default, and its bus type.</summary>
    private string FindSystemDefault()
    {
        string name = string.Empty;

        for (int n = 0; Bass.BASS_GetDeviceInfo(n) is { } info; n++)
        {
            if (!info.IsEnabled)
            {
                continue;
            }

            Trace.TraceInformation($"Sound Device #{n}: {info.name} "
                                   + $"(Default={info.IsDefault}, Flags={info.flags}, ID={info.id})");

            if (!info.IsDefault)
            {
                continue;
            }

            name = info.name;

            //the PNPID prefix, eg "USB" or "HDAUDIO"
            if (info.id?.ToUpperInvariant().Split('#') is [{ Length: > 0 } bus, ..])
            {
                DefaultBusType = bus;
            }
        }

        return name;
    }

    /// <summary>
    /// The WASAPI output called <paramref name="name"/>, or null. Several entries can share a name, so
    /// when following the system default the one WASAPI also calls default wins.
    /// </summary>
    private static (int Index, BASS_WASAPI_DEVICEINFO Device)? Find(string name, bool requested)
    {
        if (name.Length == 0)
        {
            return null;
        }

        Trace.TraceInformation($"Searching for WASAPI device: \"{name}\" "
                               + $"({(requested ? "explicitly requested" : "system default")})");

        (int Index, BASS_WASAPI_DEVICEINFO Device)? found = null;

        for (int n = 0; BassWasapi.BASS_WASAPI_GetDeviceInfo(n) is { } info; n++)
        {
            //disabled means unplugged or switched off
            if ((info.flags & BASSWASAPIDeviceInfo.BASS_DEVICE_ENABLED) == 0
                || (info.flags & BASSWASAPIDeviceInfo.BASS_DEVICE_INPUT) != 0)
            {
                continue;
            }

            Trace.TraceInformation($"WASAPI Device #{n}: {info.name} (Default={info.IsDefault})");

            if (info.name == name && (found == null || (!requested && info.IsDefault)))
            {
                found = (n, info);
            }
        }

        if (found == null && requested)
        {
            Trace.TraceWarning($"Requested output device \"{name}\" was not found; "
                               + "falling back to the system default.");
        }

        return found;
    }

    /// <summary>
    /// Opens the device, widening the buffer and then the format until it takes. Exclusive mode is the
    /// fussy one: the period cannot go below what the device reports, and the buffer has to be a
    /// multiple of it.
    /// </summary>
    private void InitWasapi(int index, BASS_WASAPI_DEVICEINFO device, AudioDeviceOptions options)
    {
        BASSWASAPIInit flags = BASSWASAPIInit.BASS_WASAPI_AUTOFORMAT
                               | (exclusive ? BASSWASAPIInit.BASS_WASAPI_EXCLUSIVE : BASSWASAPIInit.BASS_WASAPI_SHARED);

        if (COS.bIsWin7OrLater() && options.EventDriven)
        {
            flags |= BASSWASAPIInit.BASS_WASAPI_EVENT;
        }

        //below the device's own period WASAPI fails outright, so that is the floor
        float period = exclusive ? device.minperiod : device.defperiod;
        float buffer = options.BufferSizeMs > 0 ? options.BufferSizeMs / 1000.0f : period + 0.001f;

        if (buffer < period)
        {
            buffer = period + 0.001f;
        }

        if (exclusive)
        {
            //event driven refills twice per buffer rather than four times
            float least = period * ((flags & BASSWASAPIInit.BASS_WASAPI_EVENT) != 0 ? 2 : 4);

            if (buffer < least)
            {
                buffer = least;
            }
        }

        Trace.TraceInformation($"Attempting BASS_WASAPI_Init (Device: {device.name}, "
                               + $"Frequency: {device.mixfreq}, Channels: {device.mixchans}, "
                               + $"Flags: {flags}, Buffer: {buffer * 1000.0f:0.###}ms, "
                               + $"Period: {period * 1000.0f:0.###}ms, "
                               + $"device minperiod {device.minperiod * 1000.0f:0.###}ms, "
                               + $"defperiod {device.defperiod * 1000.0f:0.###}ms)");

        if (BassWasapi.BASS_WASAPI_Init(index, device.mixfreq, device.mixchans, flags, buffer, period, pull, IntPtr.Zero))
        {
            Report(buffer, period);
            return;
        }

        BASSError error = Bass.BASS_ErrorGetCode();

        if (error is BASSError.BASS_ERROR_DRIVER or BASSError.BASS_ERROR_FORMAT)
        {
            foreach (int frequency in (int[])[device.mixfreq, 48000, 44100])
            {
                foreach (int channels in (int[])[device.mixchans, 2])
                {
                    if (frequency <= 0 || channels <= 0 || (frequency == device.mixfreq && channels == device.mixchans))
                    {
                        continue;
                    }

                    Trace.TraceWarning($"BASS_WASAPI_Init failed with {error}. Retrying with explicit "
                                       + $"frequency ({frequency}) and channels ({channels}).");

                    if (BassWasapi.BASS_WASAPI_Init(index, frequency, channels, flags, buffer, period, pull, IntPtr.Zero))
                    {
                        Report(buffer, period);
                        return;
                    }

                    error = Bass.BASS_ErrorGetCode();
                }
            }
        }

        Bass.BASS_Free();
        throw new Exception($"BASS ({Backend}) initialization failed. (BASS_WASAPI_Init)[{error}]");
    }

    private void Report(float requestedBuffer, float period)
    {
        BASS_WASAPI_INFO info = BassWasapi.BASS_WASAPI_GetInfo();

        int bytesPerSample = info.format switch
        {
            BASSWASAPIFormat.BASS_WASAPI_FORMAT_8BIT => 1,
            BASSWASAPIFormat.BASS_WASAPI_FORMAT_24BIT => 3,
            BASSWASAPIFormat.BASS_WASAPI_FORMAT_32BIT or BASSWASAPIFormat.BASS_WASAPI_FORMAT_FLOAT => 4,
            _ => 2
        } * info.chans;

        BufferMs = (long)(info.buflen * 1000.0f / (bytesPerSample * info.freq));

        Trace.TraceInformation($"BASS WASAPI Initialized ({Backend}, {info.freq}Hz, {info.chans}ch, "
                               + $"Format: {info.format}, Buffer: {info.buflen} bytes [{BufferMs}ms "
                               + $"(Requested: {requestedBuffer * 1000}ms)], Update Period: {period * 1000}ms)");
    }
}
