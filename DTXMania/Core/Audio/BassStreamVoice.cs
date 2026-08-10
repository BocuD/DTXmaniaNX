using System.Diagnostics;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;
using Un4seen.Bass.AddOn.Mix;

namespace DTXMania.Core.Audio;

/// <summary>
/// A voice with its own decoder. What a clip falls back to when it cannot share one, and the only kind
/// that can change tempo, since that needs a stream of its own to do it in.
/// </summary>
internal sealed class BassStreamVoice : IAudioVoice
{
    private readonly BassAudioDevice device;
    private readonly long lengthBytes;
    private readonly int originalFrequency;

    private int source;

    //0 unless time stretch is on. Built per sound, which is why it is a setting
    private int tempo;

    //whichever of the two is in the mix: the tempo stream costs mixing time, so it is bypassed at 1x
    private int channel;

    private double speed = 1.0;
    private double pitch = 1.0;

    public long LengthMs { get; }

    private BassStreamVoice(BassAudioDevice device, int source, int tempo)
    {
        this.device = device;
        this.source = source;
        this.tempo = tempo;

        channel = tempo != 0 && speed != 1.0 ? tempo : source;

        lengthBytes = Bass.BASS_ChannelGetLength(source);
        LengthMs = (long)(Bass.BASS_ChannelBytes2Seconds(source, lengthBytes) * 1000.0);

        float frequency = 0.0f;
        Bass.BASS_ChannelGetAttribute(source, BASSAttribute.BASS_ATTRIB_FREQ, ref frequency);
        originalFrequency = (int)frequency;
    }

    public static BassStreamVoice? Create(BassAudioDevice device, string path)
        => Create(device, Bass.BASS_StreamCreateFile(path, 0, 0, device.StreamFlags), path);

    /// <summary>Over a decoded image the caller keeps pinned for as long as the voice lives.</summary>
    public static BassStreamVoice? Create(BassAudioDevice device, IntPtr image, int length, string path)
        => Create(device, Bass.BASS_StreamCreateFile(image, 0, length, device.StreamFlags), path);

    private static BassStreamVoice? Create(BassAudioDevice device, int source, string path)
    {
        if (source == 0)
        {
            Trace.TraceWarning($"'{Path.GetFileName(path)}' could not be opened as a stream: "
                               + $"{Bass.BASS_ErrorGetCode()}");
            return null;
        }

        int tempo = 0;

        if (device.TimeStretch)
        {
            tempo = BassFx.BASS_FX_TempoCreate(source, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_FX_FREESOURCE);

            if (tempo == 0)
            {
                Bass.BASS_StreamFree(source);
                Trace.TraceWarning($"'{Path.GetFileName(path)}' could not be given a tempo stream: "
                                   + $"{Bass.BASS_ErrorGetCode()}");
                return null;
            }

            //quicker, at some cost in quality
            Bass.BASS_ChannelSetAttribute(tempo, BASSAttribute.BASS_ATTRIB_TEMPO_OPTION_USE_QUICKALGO, 1.0f);
        }

        Interlocked.Increment(ref device.Streams);
        return new BassStreamVoice(device, source, tempo);
    }

    public bool IsPlaying =>
        channel != 0
        //a channel played to its end stays BASS_ACTIVE_PLAYING, so the position has to be checked too
        && BassMix.BASS_Mixer_ChannelIsActive(channel) == BASSActive.BASS_ACTIVE_PLAYING
        && BassMix.BASS_Mixer_ChannelGetPosition(channel) < lengthBytes;

    public int Volume
    {
        get
        {
            float level = 0.0f;
            return Bass.BASS_ChannelGetAttribute(channel, BASSAttribute.BASS_ATTRIB_VOL, ref level)
                ? (int)(level * 100)
                : 100;
        }
        set => Bass.BASS_ChannelSetAttribute(channel, BASSAttribute.BASS_ATTRIB_VOL,
            Math.Clamp(value, 0, 100) / 100.0f);
    }

    public int Pan
    {
        get
        {
            float pan = 0.0f;
            return Bass.BASS_ChannelGetAttribute(channel, BASSAttribute.BASS_ATTRIB_PAN, ref pan)
                ? (int)(pan * 100)
                : 0;
        }
        set => Bass.BASS_ChannelSetAttribute(channel, BASSAttribute.BASS_ATTRIB_PAN,
            Math.Clamp(value, -100, 100) / 100.0f);
    }

    public double Speed
    {
        get => speed;
        set
        {
            if (speed == value)
            {
                return;
            }

            speed = value;
            channel = tempo != 0 && speed != 1.0 ? tempo : source;

            if (tempo != 0)
            {
                //a percentage either side of the original, so 1.5x is +50
                Bass.BASS_ChannelSetAttribute(channel, BASSAttribute.BASS_ATTRIB_TEMPO,
                    (float)(speed * 100.0 - 100.0));
            }
            else
            {
                ApplyRate();
            }
        }
    }

    public double Pitch
    {
        get => pitch;
        set
        {
            if (pitch != value)
            {
                pitch = value;
                ApplyRate();
            }
        }
    }

    //rate rather than tempo, so it rises in pitch as it speeds up
    private void ApplyRate() => Bass.BASS_ChannelSetAttribute(channel, BASSAttribute.BASS_ATTRIB_FREQ,
        (float)(pitch * speed * originalFrequency));

    public long PositionMs => channel == 0
        ? 0
        : (long)(Bass.BASS_ChannelBytes2Seconds(channel, BassMix.BASS_Mixer_ChannelGetPosition(channel)) * 1000.0);

    public void Seek(long positionMs)
    {
        if (channel == 0)
        {
            return;
        }

        //the position is in the source's own time, which runs faster when the rate does
        long bytes = Bass.BASS_ChannelSeconds2Bytes(channel, positionMs * pitch * speed / 1000.0);

        if (!BassMix.BASS_Mixer_ChannelSetPosition(channel, bytes, BASSMode.BASS_POS_BYTE))
        {
            Trace.TraceInformation($"Seek to {positionMs}ms failed: {Bass.BASS_ErrorGetCode()}");
        }
    }

    public void Play(bool loop)
    {
        if (channel == 0)
        {
            return;
        }

        Bass.BASS_ChannelFlags(channel, loop ? BASSFlag.BASS_SAMPLE_LOOP : BASSFlag.BASS_DEFAULT,
            BASSFlag.BASS_SAMPLE_LOOP);

        BassMix.BASS_Mixer_ChannelSetPosition(channel, 0);
        Start();
    }

    public void Stop()
    {
        if (channel != 0)
        {
            BassMix.BASS_Mixer_ChannelPause(channel);
            BassMix.BASS_Mixer_ChannelSetPosition(channel, 0);
        }
    }

    public void Pause()
    {
        if (channel != 0)
        {
            BassMix.BASS_Mixer_ChannelPause(channel);
        }
    }

    public void Resume(long positionMs)
    {
        if (channel != 0)
        {
            Seek(positionMs);
            Start();
        }
    }

    //playing fails when the chart has already detached this one; put it back and try again, which is what
    //lets a chip still sound long after its detach window passed
    private void Start()
    {
        if (!BassMix.BASS_Mixer_ChannelPlay(channel))
        {
            device.Attach(channel);
            BassMix.BASS_Mixer_ChannelPlay(channel);
        }
    }

    public void DetachFromMixer() => device.Detach(channel);

    public void AttachToMixer() => device.Attach(channel);

    public void Dispose()
    {
        if (source == 0)
        {
            return;
        }

        device.Detach(channel);
        Interlocked.Decrement(ref device.Streams);

        if (tempo != 0)
        {
            //frees the source with it, which is what BASS_FX_FREESOURCE asked for
            Bass.BASS_StreamFree(tempo);
            tempo = 0;
        }
        else
        {
            Bass.BASS_StreamFree(source);
        }

        source = 0;
        channel = 0;
    }
}
