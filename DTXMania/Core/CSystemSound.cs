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

    /// <summary>The file this clip plays. Empty until it can be resolved.</summary>
    internal string ResolvedPath => absolutePath ?? (strFilename.Length > 0 ? CSkin.Path(strFilename) : string.Empty);

    public bool bIsPlaying => AudioMixer.IsPlaying(this);

    /// <summary>The level of the channel sounding now, which a fade changes while it plays.</summary>
    public int nCurrentSoundVolume
    {
        get => AudioMixer.CurrentVolume(this);
        set => AudioMixer.SetCurrentVolume(this, value);
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

        if (string.IsNullOrEmpty(strFilename))
        {
            throw new InvalidOperationException("A system sound needs a file name.");
        }

        string path = ResolvedPath;

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(strFilename);
        }

        //one channel up front, so the first play does not pay to decode it
        AudioMixer.Preload(this);
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
            rLastPlayedExclusiveSystemSound?.tStop();
            rLastPlayedExclusiveSystemSound = this;
        }

        AudioMixer.Play(this, nVolume, pan);
    }

    public void tStop()
    {
        AudioMixer.Stop(this);

        if (rLastPlayedExclusiveSystemSound == this)
        {
            rLastPlayedExclusiveSystemSound = null;
        }
    }

    /// <summary>Gives up this clip's channels, letting a one-shot still sounding finish first.</summary>
    public void ReleaseWhenFinished() => AudioMixer.Release(this);

    public void tRemoveMixer() => AudioMixer.RemoveMixer(this);

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        AudioMixer.Free(this);

        //tStop clears this, but a sound can be disposed without being stopped, and leaving the static
        //pointing at a dead clip means the next exclusive play stops something that no longer exists
        if (rLastPlayedExclusiveSystemSound == this)
        {
            rLastPlayedExclusiveSystemSound = null;
        }

        loadSucceeded = false;
        disposed = true;
    }
}
