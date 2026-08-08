namespace DTXMania.Core.Audio;

/// <summary>
/// One sound the mixer can play, and the channels it has made for it. Whoever owns the sound holds this,
/// so playing it is a field dereference.
/// </summary>
public sealed class MixerClip
{
    internal readonly string path;
    internal readonly string name;
    internal readonly AudioGroup group;
    internal readonly bool loop;

    internal IAudioClip? audio;

    internal readonly List<Voice> voices = [];
    internal Voice? lastPlayed;

    //only AudioMixer.SetReleasing writes this, so its list of released clips cannot drift
    internal bool releasing;

    //how often this has sounded, shown in the mixer window
    internal int plays;

    //until published the clip belongs to whoever made it, which is what lets a loader build one on
    //another thread; see AudioMixer.Publish
    internal bool published;

    //terminal, and the guard Free uses: an unpublished clip still owns audio to give back
    internal bool freed;

    internal MixerClip(string path, AudioGroup group, bool loop)
    {
        this.path = path;
        this.group = group;
        this.loop = loop;
        name = path.Length > 0 ? Path.GetFileName(path) : "(unnamed)";
    }
}

internal sealed class Voice
{
    public IAudioVoice sound = null!;
    public long startedAt;

    //on the caller's clock: the mixer never reads one, because drift correction has to agree with the
    //chart rather than with the mixer
    public long startedAtMs;
    public long pausedAtMs;

    //see AudioMixer.HoldLastAt
    public long heldPositionMs;

    //before the group level was folded in; the voice only knows the result
    public int requested = 100;
}
