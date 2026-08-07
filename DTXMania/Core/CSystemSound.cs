using FDK;

namespace DTXMania.Core;

public class CSystemSound : IDisposable
{
    //the exclusive sound currently playing, so the next one can stop it
    public static CSystemSound? rLastPlayedExclusiveSystemSound;

    public string strFilename = "";
    public bool loop;
    public bool bExclusive;

    //whether loading has not been attempted yet; the first play does it
    public bool bReadNotTried;

    public bool loadSucceeded;

    private CSound?[] sounds = new CSound?[2];
    private int nextIndex;
    private bool disposed;

    //the copy that is sounding now is the one that was played last, so it is the other one
    private CSound? CurrentSound => sounds[1 - nextIndex];

    public bool bIsPlaying => CurrentSound?.bIsPlaying ?? false;

    //the copy that will sound next, which is what a caller seeks or sets a level on before playing it
    private CSound? NextSound => sounds[nextIndex];

    public int nextSoundPosition
    {
        get => NextSound?.nPosition ?? 0;
        set
        {
            if (NextSound is { } sound)
            {
                sound.nPosition = value;
            }
        }
    }

    public int nextSoundVolume
    {
        get => NextSound?.nVolume ?? 0;
        set
        {
            if (NextSound is { } sound)
            {
                sound.nVolume = value;
            }
        }
    }

    public int nCurrentSoundVolume
    {
        get => CurrentSound?.nVolume ?? 0;
        set
        {
            if (CurrentSound is { } sound)
            {
                sound.nVolume = value;
            }
        }
    }

    private string? absolutePath;

    public CSystemSound(string fileName, bool loop, bool exclusive)
    {
        strFilename = fileName;
        this.loop = loop;
        bExclusive = exclusive;
        bReadNotTried = true;
    }

    public CSystemSound()
    {
        bReadNotTried = true;
    }

    /// <summary>A sound whose file has already been located, as the skin system does for a stage's own.</summary>
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

        string path = absolutePath ?? CSkin.Path(strFilename);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(strFilename);
        }

        for (int i = 0; i < sounds.Length; i++)
        {
            try
            {
                sounds[i] = CDTXMania.SoundManager.tGenerateSound(path);
            }
            catch
            {
                sounds[i] = null;
                throw;
            }
        }

        loadSucceeded = true;
    }

    public void tPlay() => tPlay(100);

    public void tPlay(int nVolume)
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

        if (sounds[nextIndex] is { } sound)
        {
            sound.nVolume = nVolume;
            sound.tStartPlaying(loop);
        }

        nextIndex = 1 - nextIndex;
    }

    public void tStop()
    {
        foreach (CSound? sound in sounds)
        {
            sound?.tStopPlayback();
        }

        if (rLastPlayedExclusiveSystemSound == this)
        {
            rLastPlayedExclusiveSystemSound = null;
        }
    }

    public void tRemoveMixer()
    {
        //DirectSound has no mixer to remove them from
        if (CDTXMania.SoundManager.GetCurrentSoundDeviceType() == "DirectSound")
        {
            return;
        }

        foreach (CSound? sound in sounds)
        {
            if (sound != null)
            {
                CDTXMania.SoundManager.RemoveMixer(sound);
            }
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        for (int i = 0; i < sounds.Length; i++)
        {
            if (sounds[i] != null)
            {
                CDTXMania.SoundManager.tDiscard(sounds[i]);
                sounds[i] = null;
            }
        }

        loadSucceeded = false;
        disposed = true;
    }
}
