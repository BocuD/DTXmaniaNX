using System.Diagnostics;
using Un4seen.Bass;
using Un4seen.BassAsio;

namespace DTXMania.Core.Audio;

/// <summary>
/// ASIO. Like WASAPI it pulls the mix into the driver's buffer. The driver decides the rate, and there
/// is no system default to follow: a card is chosen by name and opened by index.
/// </summary>
internal sealed class BassAsioOutput : IBassOutput
{
    private readonly BassOutputClock clock = new();

    //held as a field or the GC moves the address out from under the unmanaged side
    private readonly ASIOPROC pull;

    private int mixer;
    private long mixerBytesPerSecond;
    private long transferredBytes;
    private int outputChannels;
    private int bufferSamples;
    private int latencySamples;
    private long latencyMs;
    private double frequency;
    private bool opened;

    //16 bit, whatever the card's own format is
    private const BASSASIOFormat ChannelFormat = BASSASIOFormat.BASS_ASIO_FORMAT_16BIT;

    public BassAsioOutput()
    {
        pull = Pull;
    }

    public string Backend => "ASIO";

    public string Name { get; private set; } = string.Empty;

    public long BufferMs { get; private set; }

    public int SampleRate => (int)frequency;

    public int BufferFrames { get; private set; }

    /// <summary>What the driver hands over per callback, which is what its own control panel sets.</summary>
    public int PeriodFrames { get; private set; }

    public string FrameUnit => "samples";

    public AudioLatency Latency => latencySamples > 0 && frequency > 0
        ? AudioLatency.FromBuffer(latencySamples * 1000.0 / frequency,
            BufferFrames * 1000.0 / frequency, true)
        : AudioLatency.Unknown;

    public float CpuUsage => BassAsio.BASS_ASIO_GetCPU();

    public long ElapsedMs => clock.ElapsedMs;

    public BassMixerFormat Open(AudioDeviceOptions options)
    {
        Trace.TraceInformation("Starting BASS (ASIO) initialization...");

        BassRuntime.Register();
        BassRuntime.RequireVersions(asio: true);

        //BASS does not update its own streams; ASIO pulls them
        Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATEPERIOD, 0);

        //device 0 is "no sound": BASS decodes, the ASIO driver is what reaches the card
        if (!Bass.BASS_Init(0, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero)
            && Bass.BASS_ErrorGetCode() != BASSError.BASS_ERROR_ALREADY)
        {
            throw new Exception($"BASS initialization failed. (BASS_Init)[{Bass.BASS_ErrorGetCode()}]");
        }

        bufferSamples = options.AsioBufferSamples;

        int driver = AudioOutputs.AsioDriver(options.OutputDevice) is var named and >= 0
            ? named
            : options.AsioDevice;

        Trace.TraceInformation($"Attempting BASS_ASIO_Init (Device Index: {driver})");

        if (!BassAsio.BASS_ASIO_Init(driver, BASSASIOInit.BASS_ASIO_THREAD))
        {
            BASSError error = Bass.BASS_ErrorGetCode();
            Bass.BASS_Free();

            //a disconnected card reports success with no error set, which reads as BASS_OK
            throw new Exception("BASS (ASIO) initialization failed. (BASS_ASIO_Init)"
                                + $"[{(error == BASSError.BASS_OK ? "the device may be disconnected" : error)}]");
        }

        opened = true;

        BASS_ASIO_INFO info = BassAsio.BASS_ASIO_GetInfo();
        Name = info.name;
        outputChannels = info.outputs;
        frequency = BassAsio.BASS_ASIO_GetRate();

        //a driver left behind by its hardware still loads and reports channels, but answers 0 here, and
        //nothing downstream works at 0Hz
        if (frequency <= 0)
        {
            foreach (double rate in (double[])[48000.0, 44100.0])
            {
                if (BassAsio.BASS_ASIO_SetRate(rate))
                {
                    frequency = rate;
                    break;
                }
            }
        }

        if (frequency <= 0)
        {
            BASSError error = BassAsio.BASS_ASIO_ErrorGetCode();
            BassAsio.BASS_ASIO_Free();
            Bass.BASS_Free();
            opened = false;

            throw new Exception($"BASS (ASIO) initialization failed. \"{info.name}\" reports no sample "
                                + "rate and would not take one, which usually means the card it belongs "
                                + $"to is absent. (BASS_ASIO_GetRate)[{error}]");
        }

        BASSASIOFormat deviceFormat = BassAsio.BASS_ASIO_ChannelGetFormat(false, 0);

        Trace.TraceInformation($"BASS ASIO Initialized (Device: \"{info.name}\", Outputs: {info.outputs}, "
                               + $"Inputs: {info.inputs}, Rate: {frequency:0.###}Hz, "
                               + $"Buffer: {info.bufmin} to {info.bufmax} samples (preferred {info.bufpref}, "
                               + $"granularity {info.bufgran}), Format: {deviceFormat})");

        EnableChannels();

        return new BassMixerFormat((int)frequency, outputChannels,
            deviceFormat == BASSASIOFormat.BASS_ASIO_FORMAT_FLOAT);
    }

    /// <summary>
    /// Output 0 carries the mix and every other output is joined to it. Joining only channel 1 breaks
    /// cards with more than two outputs.
    /// </summary>
    private void EnableChannels()
    {
        if (!BassAsio.BASS_ASIO_ChannelEnable(false, 0, pull, IntPtr.Zero))
        {
            throw Failed("BASS_ASIO_ChannelEnable");
        }

        for (int channel = 1; channel < outputChannels; channel++)
        {
            if (!BassAsio.BASS_ASIO_ChannelJoin(false, channel, 0))
            {
                throw Failed($"BASS_ASIO_ChannelJoin({channel})");
            }
        }

        if (!BassAsio.BASS_ASIO_ChannelSetFormat(false, 0, ChannelFormat))
        {
            throw Failed("BASS_ASIO_ChannelSetFormat");
        }
    }

    public void Start(int mixer)
    {
        this.mixer = mixer;

        BASS_CHANNELINFO info = Bass.BASS_ChannelGetInfo(mixer);

        int bytesPerSample = ChannelFormat switch
        {
            BASSASIOFormat.BASS_ASIO_FORMAT_16BIT => 2,
            BASSASIOFormat.BASS_ASIO_FORMAT_24BIT => 3,
            _ => 4
        };

        mixerBytesPerSecond = (long)info.chans * bytesPerSample * info.freq;

        //a driver refuses a buffer it cannot take rather than correcting it, so the request is brought
        //into range first
        int buffer = BufferFor(BassAsio.BASS_ASIO_GetInfo(), bufferSamples);

        if (!BassAsio.BASS_ASIO_Start(buffer))
        {
            throw Failed("BASS_ASIO_Start");
        }

        //bufpref lags a request that has just changed it, so a buffer that was asked for is its own answer
        BufferFrames = buffer > 0
            ? buffer
            : BassAsio.BASS_ASIO_GetInfo() is { } driver ? driver.bufpref : 0;

        //ASIO hands over exactly one buffer per callback, so the fill period is the buffer
        PeriodFrames = BufferFrames;
        BufferMs = (long)Math.Round(BufferFrames * 1000.0 / frequency);

        //only answers once started, and counts the driver's whole path rather than just the buffer
        latencySamples = BassAsio.BASS_ASIO_GetLatency(false);
        latencyMs = (long)Math.Round(latencySamples * 1000.0 / frequency);

        Trace.TraceInformation($"ASIO output started: buffer {BufferFrames} samples ({BufferMs}ms), "
                               + $"latency {latencySamples} samples "
                               + $"({latencySamples * 1000.0 / frequency:0.#}ms), "
                               + $"{bufferSamples} requested, {buffer} used");
    }

    /// <summary>
    /// The requested buffer brought onto something the driver will take: inside its range and on its
    /// granularity. 0 asks for the driver's own setting.
    /// </summary>
    private static int BufferFor(BASS_ASIO_INFO? driver, int wanted)
    {
        if (wanted <= 0 || driver == null)
        {
            return 0;
        }

        int buffer = Math.Clamp(wanted, driver.bufmin, driver.bufmax);

        //a granularity of -1 means the driver only takes powers of two
        if (driver.bufgran == -1)
        {
            int power = driver.bufmin;

            while (power * 2 <= buffer)
            {
                power *= 2;
            }

            return power;
        }

        return driver.bufgran > 0
            ? buffer - (buffer - driver.bufmin) % driver.bufgran
            : buffer;
    }

    public void Dispose()
    {
        if (opened)
        {
            //before the clock: the pull callback reads it
            FreeDriver();
            Bass.BASS_Free();
            opened = false;
        }

        clock.Dispose();
    }

    private Exception Failed(string call)
    {
        BASSError error = BassAsio.BASS_ASIO_ErrorGetCode();
        FreeDriver();
        Bass.BASS_Free();
        opened = false;

        return new Exception($"BASS (ASIO) initialization failed. ({call})[{error}]");
    }

    private const int FreeTimeoutMs = 2000;

    private static void FreeDriver()
    {
        Thread free = new(() =>
        {
            try
            {
                BassAsio.BASS_ASIO_Free();
            }
            catch (Exception e)
            {
                Trace.TraceWarning($"BASS_ASIO_Free threw: {e.Message}");
            }
        })
        {
            IsBackground = true,
            Name = "ASIO teardown"
        };

        free.Start();

        if (!free.Join(FreeTimeoutMs))
        {
            Trace.TraceWarning($"BASS_ASIO_Free did not return within {FreeTimeoutMs}ms; "
                               + "leaving the driver to the process exit.");
        }
    }

    /// <summary>
    /// Hands the mixed output straight to ASIO. The elapsed time comes from the running total less the
    /// driver's latency, so it describes what is being heard rather than what has been handed over.
    /// </summary>
    private int Pull(bool input, int channel, IntPtr buffer, int length, IntPtr user)
    {
        if (input)
        {
            return 0;
        }

        int transferred = Bass.BASS_ChannelGetData(mixer, buffer, length);

        if (transferred == -1)
        {
            transferred = 0;
        }

        //the driver's whole path, not just the buffer, or the clock leads what is being heard
        clock.Update(transferredBytes * 1000 / mixerBytesPerSecond - latencyMs);

        transferredBytes += transferred;
        return transferred;
    }
}
