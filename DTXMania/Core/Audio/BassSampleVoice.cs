using System.Diagnostics;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Mix;

namespace DTXMania.Core.Audio;

public sealed class BassSampleVoice : IAudioVoice
{
    private readonly long lengthBytes;
    private readonly int originalFrequency;
    private readonly int mixer;
    private int channel;
    private double speed = 1.0;
    private double pitch = 1.0;

    private BassSampleVoice(int channel, int mixer)
    {
        this.channel = channel;
        this.mixer = mixer;
        lengthBytes = Bass.BASS_ChannelGetLength(channel);

        BASS_CHANNELINFO info = Bass.BASS_ChannelGetInfo(channel);
        originalFrequency = info?.freq ?? 0;
    }

    /// <summary>Takes a channel off <paramref name="sample"/> and joins it to the mix, or null if either
    /// step fails — the caller then falls back to a stream.</summary>
    public static BassSampleVoice? Create(int sample, int mixer)
    {
        int channel = Bass.BASS_SampleGetChannel(sample, false);

        if (channel == 0)
        {
            return null;
        }

        //the same flags and the same order CSound uses: add paused, rewind after adding because doing it
        //before has no effect, then pre-buffer
        const BASSFlag flags = BASSFlag.BASS_SPEAKER_FRONT | BASSFlag.BASS_MIXER_NORAMPIN | BASSFlag.BASS_MIXER_PAUSE;

        if (!BassMix.BASS_Mixer_StreamAddChannel(mixer, channel, flags))
        {
            Bass.BASS_StreamFree(channel);
            return null;
        }

        BassMix.BASS_Mixer_ChannelSetPosition(channel, 0);
        Bass.BASS_ChannelUpdate(channel, 0);

        return new BassSampleVoice(channel, mixer);
    }

    public bool IsPlaying
    {
        get
        {
            if (channel == 0)
            {
                return false;
            }

            //a channel played to its end stays BASS_ACTIVE_PLAYING, so the position has to be checked too
            return BassMix.BASS_Mixer_ChannelIsActive(channel) == BASSActive.BASS_ACTIVE_PLAYING
                   && BassMix.BASS_Mixer_ChannelGetPosition(channel) < lengthBytes;
        }
    }

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

    //a sample channel has no tempo stream, so speed is a frequency change like pitch. Only the song BGM
    //asks for a speed other than 1.0 and it is far too long to be sample-backed, so this never runs
    public double Speed
    {
        get => speed;
        set { speed = value; ApplyRate(); }
    }

    public double Pitch
    {
        get => pitch;
        set { pitch = value; ApplyRate(); }
    }

    private void ApplyRate()
    {
        if (channel != 0 && originalFrequency > 0)
        {
            Bass.BASS_ChannelSetAttribute(channel, BASSAttribute.BASS_ATTRIB_FREQ,
                (float)(originalFrequency * speed * pitch));
        }
    }

    public long PositionMs => channel == 0
        ? 0
        : (long)(Bass.BASS_ChannelBytes2Seconds(channel, BassMix.BASS_Mixer_ChannelGetPosition(channel)) * 1000.0);

    public void Seek(long positionMs)
    {
        if (channel != 0)
        {
            BassMix.BASS_Mixer_ChannelSetPosition(channel, Bass.BASS_ChannelSeconds2Bytes(channel, positionMs / 1000.0));
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
            BassMix.BASS_Mixer_ChannelPlay(channel);
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

        //from the top, the way tStartPlaying does: a voice is only handed out when it is free, but a
        //stolen one is not
        BassMix.BASS_Mixer_ChannelSetPosition(channel, 0);

        if (!BassMix.BASS_Mixer_ChannelPlay(channel))
        {
            //playing fails when the chart has already detached this chip. Put it back and play it, the
            //way CSound.tPlaySound does — this is what lets pads still sound on the result screen
            AttachToMixer();
            BassMix.BASS_Mixer_ChannelPlay(channel);
        }
    }

    public void Stop()
    {
        if (channel != 0)
        {
            BassMix.BASS_Mixer_ChannelPause(channel);
            BassMix.BASS_Mixer_ChannelSetPosition(channel, 0);
        }
    }

    public void DetachFromMixer()
    {
        if (channel != 0)
        {
            BassMix.BASS_Mixer_ChannelRemove(channel);
        }
    }

    public void AttachToMixer()
    {
        //same order as Create: paused, then rewound, because seeking before the add has no effect
        if (channel == 0 || BassMix.BASS_Mixer_ChannelGetMixer(channel) == mixer)
        {
            return;
        }

        const BASSFlag flags = BASSFlag.BASS_SPEAKER_FRONT | BASSFlag.BASS_MIXER_NORAMPIN | BASSFlag.BASS_MIXER_PAUSE;

        if (BassMix.BASS_Mixer_StreamAddChannel(mixer, channel, flags))
        {
            BassMix.BASS_Mixer_ChannelSetPosition(channel, 0);
            Bass.BASS_ChannelUpdate(channel, 0);
        }
    }

    public void Dispose()
    {
        if (channel == 0)
        {
            return;
        }

        BassMix.BASS_Mixer_ChannelRemove(channel);

        if (!Bass.BASS_StreamFree(channel))
        {
            Trace.TraceWarning($"Could not free sample channel {channel}: {Bass.BASS_ErrorGetCode()}");
        }

        channel = 0;
    }
}
