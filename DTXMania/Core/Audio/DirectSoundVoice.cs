using SharpDX.DirectSound;
using SharpDX.Multimedia;

namespace DTXMania.Core.Audio;

/// <summary>
/// One secondary buffer. DirectSound takes levels in hundredths of a decibel, so the linear 0-100 the
/// rest of the mixer works in is converted here.
/// </summary>
internal sealed class DirectSoundVoice : IAudioVoice
{
    private readonly WaveFormat format;
    private readonly int originalFrequency;

    private SoundBuffer? buffer;
    private int volume = 100;
    private int pan;
    private double speed = 1.0;
    private double pitch = 1.0;

    public DirectSoundVoice(SoundBuffer buffer, WaveFormat format)
    {
        this.buffer = buffer;
        this.format = format;
        originalFrequency = format.SampleRate;
    }

    public bool IsPlaying => buffer != null && (buffer.Status & (int)BufferStatus.Playing) != 0;

    public int Volume
    {
        get => volume;
        set
        {
            volume = Math.Clamp(value, 0, 100);

            //-10000 is DirectSound's silence; the rest is 20log10 of the linear level, in hundredths
            Set(b => b.Volume = volume == 0 ? -10000 : (int)(2000.0 * Math.Log10(volume / 100.0)));
        }
    }

    public int Pan
    {
        get => pan;
        set
        {
            pan = Math.Clamp(value, -100, 100);

            Set(b => b.Pan = pan switch
            {
                0 => 0,
                -100 => -10000,
                100 => 10000,
                < 0 => (int)(2000.0 * Math.Log10((pan + 100) / 100.0)),
                _ => (int)(-2000.0 * Math.Log10((100 - pan) / 100.0))
            });
        }
    }

    public double Speed
    {
        get => speed;
        set
        {
            speed = value;
            ApplyRate();
        }
    }

    public double Pitch
    {
        get => pitch;
        set
        {
            pitch = value;
            ApplyRate();
        }
    }

    private void ApplyRate() => Set(b => b.Frequency = (int)(pitch * speed * originalFrequency));

    public long PositionMs
    {
        get
        {
            if (buffer == null)
            {
                return 0;
            }

            buffer.GetCurrentPosition(out int position, out _);
            return (long)(position / (format.AverageBytesPerSecond * 0.001 * pitch * speed));
        }
    }

    public void Seek(long positionMs)
    {
        //to a block boundary, since a buffer position is in bytes and a partial frame is noise
        int frame = (int)(format.SampleRate * positionMs * 0.001 * pitch * speed);
        Set(b => b.CurrentPosition = frame * format.BlockAlign);
    }

    public void Play(bool loop)
    {
        Set(b =>
        {
            b.CurrentPosition = 0;
            b.Play(0, loop ? PlayFlags.Looping : PlayFlags.None);
        });
    }

    public void Stop()
    {
        Set(b =>
        {
            b.Stop();
            b.CurrentPosition = 0;
        });
    }

    public void Pause() => Set(b => b.Stop());

    public void Resume(long positionMs)
    {
        Seek(positionMs);
        Set(b => b.Play(0, PlayFlags.None));
    }

    /// <summary>DirectSound mixes in the driver, so there is nothing to attach to.</summary>
    public void DetachFromMixer()
    {
    }

    public void AttachToMixer()
    {
    }

    public void Dispose()
    {
        Set(b => b.Stop());
        buffer?.Dispose();
        buffer = null;
    }

    /// <summary>
    /// A buffer is lost when another process takes the device exclusively, and every call on it throws
    /// from then on. There is nothing to do about it and nothing to say each time.
    /// </summary>
    private void Set(Action<SoundBuffer> action)
    {
        if (buffer is not { IsDisposed: false })
        {
            return;
        }

        try
        {
            action(buffer);
        }
        catch
        {
            // ignored
        }
    }
}
