using DTXMania.Core.Audio;

namespace DTXMania.Core;

public class CSystemSound : IDisposable
{
    //the exclusive sound currently playing, so the next one can stop it
    public static CSystemSound? rLastPlayedExclusiveSystemSound;

    public string strFilename = "";
    public bool loop;
    public bool bExclusive;

    public AudioGroup group = AudioGroup.Se;

    //whether loading has not been attempted yet; the first play does it
    public bool bReadNotTried;

    public bool loadSucceeded;

    private bool disposed;
    private string? absolutePath;

    //made on first use: a skin reload re-reads these, and the name resolves to a different file
    private MixerClip? clip;

    /// <summary>The file this clip plays. Empty until it can be resolved.</summary>
    internal string ResolvedPath => absolutePath ?? (strFilename.Length > 0 ? CSkin.Path(strFilename) : string.Empty);

    //replaced once the mixer has reclaimed it, rather than reused
    private MixerClip Clip
    {
        get
        {
            if (clip is null or { freed: true })
            {
                clip = AudioMixer.CreateClip(ResolvedPath, group, loop);
                AudioMixer.Publish(clip);
            }

            return clip;
        }
    }

    public bool bIsPlaying => clip != null && AudioMixer.IsPlaying(clip);

    /// <summary>The level of the channel sounding now, which a fade changes while it plays.</summary>
    public int nCurrentSoundVolume
    {
        get => clip == null ? 0 : AudioMixer.CurrentVolume(clip);
        set
        {
            if (clip != null)
            {
                AudioMixer.SetCurrentVolume(clip, value);
            }
        }
    }

    public CSystemSound(string fileName, bool loop, bool exclusive, AudioGroup group = AudioGroup.Se)
    {
        strFilename = fileName;
        this.loop = loop;
        bExclusive = exclusive;
        this.group = group;
        bReadNotTried = true;
    }

    public CSystemSound() : this(string.Empty, false, false)
    {
    }

    /// <summary>A sound whose file has already been located, as the skin system does.</summary>
    public static CSystemSound FromPath(string path, bool loop, bool exclusive)
    {
        return new CSystemSound(Path.GetFileName(path), loop, exclusive) { absolutePath = path };
    }

    public void tRead()
    {
        bReadNotTried = false;
        loadSucceeded = false;

        //a skin reload disposes these and reads them again, so reading makes one live a second time
        disposed = false;

        if (string.IsNullOrEmpty(strFilename))
        {
            throw new InvalidOperationException("A system sound needs a file name.");
        }

        if (!File.Exists(ResolvedPath))
        {
            throw new FileNotFoundException(strFilename);
        }

        //one channel up front, so the first play does not pay to decode it
        AudioMixer.Preload(Clip);
        loadSucceeded = true;
    }

    public void tPlay() => tPlay(100);

    /// <param name="pan">Where it sits in the stereo field, -100 left to 100 right.</param>
    public void tPlay(int nVolume, int pan = 0)
    {
        //loaded on first play rather than up front, and a failure is not retried every time it is played
        if (bReadNotTried)
        {
            try
            {
                tRead();
            }
            catch
            {
                bReadNotTried = false;
            }
        }

        if (bExclusive)
        {
            //a replaced BGM is done with, and it is the large one; effects are small and stay loaded
            if (rLastPlayedExclusiveSystemSound is { group: AudioGroup.Bgm } outgoing)
            {
                outgoing.Unload();
            }
            else
            {
                rLastPlayedExclusiveSystemSound?.tStop();
            }

            rLastPlayedExclusiveSystemSound = this;
        }

        AudioMixer.Play(Clip, nVolume, pan);
    }

    public void tStop()
    {
        if (clip != null)
        {
            AudioMixer.Stop(clip);
        }

        if (rLastPlayedExclusiveSystemSound == this)
        {
            rLastPlayedExclusiveSystemSound = null;
        }
    }

    /// <summary>Gives up this clip's channels, letting a one-shot still sounding finish first.</summary>
    public void ReleaseWhenFinished()
    {
        if (clip != null)
        {
            //the handle stays: a sound that is still audible still has to be stoppable
            AudioMixer.Release(clip);
        }
    }

    /// <summary>Stops this and gives its audio back. The next play reads the file again.</summary>
    public void Unload()
    {
        tStop();

        if (clip != null)
        {
            AudioMixer.Free(clip);
            clip = null;
        }

        bReadNotTried = true;
        loadSucceeded = false;
    }

    public void tRemoveMixer()
    {
        if (clip != null)
        {
            AudioMixer.DetachFromMixer(clip);
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        if (clip != null)
        {
            AudioMixer.Free(clip);
            clip = null;
        }

        //tStop clears this, but a sound can be disposed without being stopped
        if (rLastPlayedExclusiveSystemSound == this)
        {
            rLastPlayedExclusiveSystemSound = null;
        }

        loadSucceeded = false;
        disposed = true;
    }
}
