using System.Diagnostics;
using Un4seen.Bass;

namespace DTXMania.Core.Audio;

/// <summary>
/// BASS's own output. Unlike the others it opens the sound card itself and plays the mixer rather than
/// being handed data from it, so there is no callback and no format to negotiate. It is the only backend
/// that exists off Windows, where BASS is CoreAudio or ALSA underneath.
/// </summary>
internal sealed class BassOutput : IBassOutput
{
    //what BASS queues to the card, and so what the output latency is. BASS defaults it to 30ms and
    //clamps whatever it is given to the device's own minimum
    private const int DefaultDeviceBufferMs = 10;

    //the channel's own playback buffer, which BASS_ATTRIB_NOBUFFER keeps out of the path. It only bounds
    //what a hitch would survive without that, so there is nothing to gain by lowering it
    private const int PlaybackBufferMs = 30;

    //how often BASS tops the buffer up. Its own floor is 5ms
    private const int UpdatePeriodMs = 5;

    private int played;
    private bool opened;

    public string Backend => "BASS";

    public string Name { get; private set; } = string.Empty;

    public long BufferMs { get; private set; }

    public float CpuUsage => Bass.BASS_GetCPU();

    /// <summary>BASS plays the mixer, so the final stage sounds rather than decodes.</summary>
    public bool Pulls => false;

    /// <summary>Where the output has got to. BASS reports the position being heard, so the playback buffer
    /// is already taken off.</summary>
    public long ElapsedMs => played == 0
        ? 0
        : (long)(Bass.BASS_ChannelBytes2Seconds(played,
            Bass.BASS_ChannelGetPosition(played, BASSMode.BASS_POS_BYTE)) * 1000.0);

    public BassMixerFormat Open(AudioDeviceOptions options)
    {
        Trace.TraceInformation("Starting BASS initialization...");

        BassRuntime.Register();
        BassRuntime.RequireVersions();

        //read when a stream is created, so before the mixer is. A WASAPI or ASIO device leaves the update
        //period at 0, which would leave nothing topping the buffer up here
        Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_BUFFER, PlaybackBufferMs);
        Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATEPERIOD, UpdatePeriodMs);

        //read when the device is opened, so before BASS_Init. The config value drives this one because it
        //is the one that decides the latency
        int wanted = options.BufferSizeMs > 0 ? options.BufferSizeMs : DefaultDeviceBufferMs;
        Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_DEV_BUFFER, wanted);

        int device = Find(options.OutputDevice);

        Trace.TraceInformation($"Attempting BASS_Init (Device: {device}, Device buffer: {wanted}ms)");

        if (!Bass.BASS_Init(device, 48000, BASSInit.BASS_DEVICE_DEFAULT | BASSInit.BASS_DEVICE_LATENCY,
                AudioDevice.WindowHandle))
        {
            BASSError error = Bass.BASS_ErrorGetCode();

            //an already-open device is the one we asked for, since only one device layer exists at a time
            if (error != BASSError.BASS_ERROR_ALREADY)
            {
                throw new Exception($"BASS initialization failed. (BASS_Init)[{error}]");
            }
        }

        opened = true;

        BASS_INFO info = Bass.BASS_GetInfo();
        Name = Bass.BASS_GetDeviceInfo(Bass.BASS_GetDevice())?.name ?? string.Empty;

        //what BASS says it got, not what was asked for: it clamps the device buffer to the card's own
        //minimum, and this is the figure the rest of the game shows as the output buffer
        BufferMs = info.latency;

        Trace.TraceInformation($"BASS Initialized (Device: \"{Name}\", {info.freq}Hz, {info.speakers} speakers, "
                               + $"Device buffer: {wanted}ms requested, {info.minbuf}ms is this card's "
                               + $"minimum, {info.latency}ms output latency)");

        //two channels whatever the card offers: the game mixes stereo and BASS spreads it
        return new BassMixerFormat(info.freq > 0 ? info.freq : 48000, 2, true);
    }

    public void Start(int mixer)
    {
        played = mixer;

        //BASS pulls the mixer as the device needs it instead of running ahead into a buffer of its own,
        //which is the shape WASAPI and ASIO already have and is worth about 20ms here. Pulling the mixer
        //is cheap, so the buffer was covering nothing
        Bass.BASS_ChannelSetAttribute(mixer, BASSAttribute.BASS_ATTRIB_NOBUFFER, 1.0f);

        if (!Bass.BASS_ChannelPlay(mixer, false))
        {
            throw new Exception($"The BASS mixer would not play. [{Bass.BASS_ErrorGetCode()}]");
        }
    }

    public void Dispose()
    {
        if (!opened)
        {
            return;
        }

        Bass.BASS_Free();
        opened = false;
        played = 0;
    }

    /// <summary>
    /// The device to open by name, or -1 for whatever BASS calls default. Device 0 is "no sound" and is
    /// what the other backends decode on, so it is never a real output.
    /// </summary>
    private static int Find(string name)
    {
        if (name.Length == 0)
        {
            return -1;
        }

        for (int n = 1; Bass.BASS_GetDeviceInfo(n) is { } info; n++)
        {
            if (info.IsEnabled && info.name == name)
            {
                return n;
            }
        }

        Trace.TraceWarning($"Requested output device \"{name}\" was not found; using the default.");
        return -1;
    }
}
