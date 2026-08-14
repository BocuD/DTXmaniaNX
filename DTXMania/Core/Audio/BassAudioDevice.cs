using System.Diagnostics;
using FDK;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Mix;
using Timer = System.Threading.Timer;

namespace DTXMania.Core.Audio;

/// <summary>
/// Every backend BASS can reach. It owns the mixer graph, the clips and their voices; an
/// <see cref="IBassOutput"/> owns the sound card and pulls the mix out of it.
/// </summary>
public sealed class BassAudioDevice : IAudioDevice
{
    private readonly IBassOutput output;

    //everything sounding feeds this, and the master volume is on it. Setting a volume on the channel the
    //output pulls from does not reach BASS_ChannelGetData, so there is a second stage to pull from
    private int mixer;
    private int deviceOut;

    private int masterVolume = 100;

    //input arrives stamped with its own device's clock; these correlate the two, measured once a second
    private Timer? snap;
    private CTimer? inputClock;
    private long inputMsAtSnap;
    private long outputMsAtSnap;

    internal int Mixer => mixer;

    /// <summary>Decoders open. A shared clip still has one per voice; what it shares is the audio they
    /// read, not the decoding.</summary>
    internal int Streams;

    internal int MixedChannelCount;

    public BassAudioDevice(AudioDeviceOptions options)
    {
        output = options.Backend switch
        {
            AudioBackend.Asio => new BassAsioOutput(),
            AudioBackend.Bass => new BassOutput(),
            AudioBackend.WasapiShared => new BassWasapiOutput(false),
            _ => new BassWasapiOutput(true)
        };

        try
        {
            Build(options);
        }
        catch
        {
            output.Dispose();
            throw;
        }
    }

    private void Build(AudioDeviceOptions options)
    {
        BassMixerFormat format = output.Open(options);

        //BASS_MIXER_POSEX would make the mixer keep a position record for every source, and nothing here
        //reads one
        BASSFlag common = BASSFlag.BASS_MIXER_NONSTOP
                          | (format.Float ? BASSFlag.BASS_SAMPLE_FLOAT : BASSFlag.BASS_DEFAULT);

        //decode only: it sounds when the output takes it, not before
        mixer = BassMix.BASS_Mixer_StreamCreate(format.Frequency, format.Channels,
            common | BASSFlag.BASS_STREAM_DECODE);

        if (mixer == 0)
        {
            throw new Exception($"The BASS mixer could not be created. [{Bass.BASS_ErrorGetCode()}]");
        }

        //an output that plays the mixer needs the last stage to sound; one that pulls needs it to decode
        deviceOut = BassMix.BASS_Mixer_StreamCreate(format.Frequency, format.Channels,
            output.Pulls ? common | BASSFlag.BASS_STREAM_DECODE : common);

        if (deviceOut == 0 || !BassMix.BASS_Mixer_StreamAddChannel(deviceOut, mixer, BASSFlag.BASS_DEFAULT))
        {
            throw new Exception($"The BASS output stage could not be created. [{Bass.BASS_ErrorGetCode()}]");
        }

        StreamFlags = BASSFlag.BASS_STREAM_DECODE | (format.Float ? BASSFlag.BASS_SAMPLE_FLOAT : BASSFlag.BASS_DEFAULT);

        MasterVolume = masterVolume;
        output.Start(deviceOut);

        //BASS decodes on its own threads; one per core is what a chart's worth of streams needs
        Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATETHREADS, Environment.ProcessorCount);

        //only applies to sources that are playing channels, and ours all decode, so it is not in the path
        Trace.TraceInformation($"BASS mixer source buffer: "
                               + $"{Bass.BASS_GetConfig(BASSConfig.BASS_CONFIG_MIXER_BUFFER)}ms");

        inputClock = new CTimer(CTimer.EType.MultiMedia);
        snap = new Timer(Snap, null, 0, 1000);
    }

    /// <summary>How a clip's streams have to be created to join this mixer.</summary>
    internal BASSFlag StreamFlags { get; private set; }

    public AudioDeviceStatus Status => new()
    {
        Backend = output.Backend,
        Output = output.Name,
        BufferMs = output.BufferMs,
        SampleRate = output.SampleRate,
        BufferFrames = output.BufferFrames,
        PeriodFrames = output.PeriodFrames,
        FrameUnit = output.FrameUnit,
        CpuUsage = output.CpuUsage,
        Streams = Streams,
        MixedChannels = MixedChannelCount,
        DefaultOutputBusType = (output as BassWasapiOutput)?.DefaultBusType ?? string.Empty
    };

    /// <summary>The output owns the buffer, and whether it pulls or is pushed decides how the wait works
    /// out, so it answers this rather than the default deriving it from <see cref="Status"/>.</summary>
    public AudioLatency Latency => output.Latency;

    public IAudioClip Load(string path, AudioGroup group) => new BassAudioClip(this, path);

    public int MasterVolume
    {
        get => masterVolume;
        set
        {
            masterVolume = value;
            Bass.BASS_ChannelSetAttribute(mixer, BASSAttribute.BASS_ATTRIB_VOL, value / 100.0f);
        }
    }

    /// <summary>Read as each stream is created, so it reaches sounds loaded after it changes.</summary>
    public bool TimeStretch { get; set; }

    public bool MixesChannels => true;

    public bool NeedsDriftCorrection => false;

    public long ElapsedMs => output.ElapsedMs;

    public long ElapsedMsFor(long deviceTimestamp) => deviceTimestamp - inputMsAtSnap + outputMsAtSnap;

    //the two clocks run independently, so the offset between them is measured rather than assumed
    private void Snap(object? state)
    {
        inputMsAtSnap = inputClock?.nSystemTimeMs ?? 0;
        outputMsAtSnap = ElapsedMs;
    }

    public void Dispose()
    {
        snap?.Dispose();
        snap = null;

        //the output first: its pull callback reads the mixer
        output.Dispose();

        Free(ref deviceOut);
        Free(ref mixer);

        inputClock?.Dispose();
        inputClock = null;
    }

    private static void Free(ref int stream)
    {
        if (stream != 0)
        {
            BassMix.BASS_Mixer_ChannelPause(stream);
            Bass.BASS_StreamFree(stream);
            stream = 0;
        }
    }

    /// <summary>Adds a decoding channel to the mix, paused and rewound so it starts where it should.</summary>
    internal bool Attach(int channel)
    {
        if (channel == 0 || BassMix.BASS_Mixer_ChannelGetMixer(channel) == mixer)
        {
            return true;
        }

        const BASSFlag flags = BASSFlag.BASS_SPEAKER_FRONT | BASSFlag.BASS_MIXER_NORAMPIN | BASSFlag.BASS_MIXER_CHAN_PAUSE;

        if (!BassMix.BASS_Mixer_StreamAddChannel(mixer, channel, flags))
        {
            return false;
        }

        Interlocked.Increment(ref MixedChannelCount);

        //after adding, not before: seeking a channel the mixer does not hold yet does nothing
        BassMix.BASS_Mixer_ChannelSetPosition(channel, 0);
        Bass.BASS_ChannelUpdate(channel, 0);
        return true;
    }

    internal void Detach(int channel)
    {
        if (channel != 0 && BassMix.BASS_Mixer_ChannelRemove(channel))
        {
            Interlocked.Decrement(ref MixedChannelCount);
        }
    }
}
