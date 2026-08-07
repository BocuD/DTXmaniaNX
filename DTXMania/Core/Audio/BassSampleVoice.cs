using System.Diagnostics;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Mix;

namespace DTXMania.Core.Audio;

public sealed class BassSampleVoice : IAudioVoice
{
    private readonly long lengthBytes;
    private int channel;

    private BassSampleVoice(int channel)
    {
        this.channel = channel;
        lengthBytes = Bass.BASS_ChannelGetLength(channel);
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

        return new BassSampleVoice(channel);
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
        BassMix.BASS_Mixer_ChannelPlay(channel);
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
