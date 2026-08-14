using System.Diagnostics;
using DTXMania.Core.Audio;

namespace DTXMania.Core;

/// <summary>
/// Owns every channel the game plays a sound on.
///
/// One voice is one channel with one playback position, so playing it again restarts it. Sounding twice
/// at once needs two of them. A clip gets another voice the first time it needs one and reuses it from
/// then on, so a pool settles at whatever the game actually asks for and stops growing.
///
/// Nothing reached from <see cref="Play"/> allocates; see AUDIO.md §8.3.
/// </summary>
public static partial class AudioMixer
{
    //a clip needing this many at once is retriggering in a loop; reuse a channel rather than keep growing
    private const int RunawayGuard = 64;

    //enumerating outputs costs a round trip to the driver, so the system is only asked this often
    private const int OutputCheckIntervalMs = 1000;

    public static IAudioDevice Device { get; set; } = new NullAudioDevice();

    //held here rather than on the device: every backend would store them identically, and a rebuild
    //makes a new device
    private static readonly int[] groupVolumes = [100, 100, 100, 100, 100];
    private static int masterVolume = 100;
    private static bool timeStretch;

    /// <summary>The clock gameplay is timed against.</summary>
    public static readonly PerformanceTimer Timer = new();

    private static readonly List<MixerClip> clips = [];

    //so Sweep looks at these rather than scanning every clip; a chart puts hundreds in the list
    private static readonly List<MixerClip> releasing = [];

    //voices set up but not started, so a song jump can start them together; see HoldLastAt
    private static readonly List<Voice> held = [];

    //what to restore on the far side of a device rebuild; -1 means loaded but not sounding
    private static readonly List<(MixerClip clip, int volume, int pan, long positionMs)> resume = [];

    private static long sequence;
    private static long nextOutputCheck;
    private static bool warnedRunaway;

    //a backend that will not open does not start working on its own, and following the system default
    //would otherwise try it again every second for as long as the game runs
    private const int MaxOutputAttempts = 3;

    private static int failedOutputAttempts;

    public static bool OutputGaveUp => failedOutputAttempts >= MaxOutputAttempts;

    /// <summary>Empty while an output is running.</summary>
    public static string OutputError { get; private set; } = string.Empty;

    //interlocked because a loader preloads on its own thread, and preloading makes a voice
    private static int voiceCount;
    private static int peakVoiceCount;

    private static int clipsCreated;
    private static int clipsFreed;

    /// <summary>Voices held right now. FDK's own counters do not see these.</summary>
    public static int VoiceCount => Volatile.Read(ref voiceCount);

    /// <summary>The most ever held. Still climbing long after startup means something is retriggering
    /// faster than it finishes.</summary>
    public static int PeakVoiceCount => Volatile.Read(ref peakVoiceCount);

    /// <summary>
    /// Makes a clip that belongs to the caller. The file is not read until <see cref="Preload"/> or the
    /// first play, and the mixer does not know about it until <see cref="Publish"/>.
    ///
    /// Safe from any thread: an unpublished clip is reachable only by whoever made it.
    /// </summary>
    public static MixerClip CreateClip(string path, AudioGroup group, bool loop)
    {
        Interlocked.Increment(ref clipsCreated);
        return new MixerClip(path, group, loop);
    }

    /// <summary>
    /// Clips made but not yet freed, published or not. Every clip owns decoded audio, so this has to come
    /// back down once a chart is torn down.
    /// </summary>
    public static int LiveClips => Volatile.Read(ref clipsCreated) - Volatile.Read(ref clipsFreed);

    /// <summary>
    /// Live clips the mixer cannot see. A loader still building them counts here; anything left once
    /// loading is done was leaked by whoever made it.
    /// </summary>
    public static int UnaccountedClips => LiveClips - clips.Count;

    /// <summary>
    /// Hands a clip over to the mixer, after which it is swept, rebuilt and shown with the rest.
    ///
    /// Game thread only: this is the one place the clip list is added to, which is what makes it safe to
    /// walk without locking.
    /// </summary>
    public static void Publish(MixerClip clip)
    {
        if (clip.published || clip.freed)
        {
            return;
        }

        clip.published = true;
        clips.Add(clip);
    }

    /// <summary>Sounds <paramref name="clip"/> on a channel that is free, making one if none is.</summary>
    /// <param name="speed">Playback rate. Only the song BGM passes anything but 1.0.</param>
    /// <param name="pitch">Frequency multiplier, for the wrong-note detune on guitar and bass.</param>
    /// <param name="atMs">When this started, on the caller's clock, for drift correction to work from.</param>
    public static void Play(MixerClip clip, int volume, int pan,
        double speed = 1.0, double pitch = 1.0, long atMs = 0)
    {
        Sweep();

        SetReleasing(clip, false);

        if ((SoleVoice(clip) ?? FreeVoice(clip) ?? Grow(clip)) is not { } voice)
        {
            return;
        }

        //rate before level: the speed decides which stream the sound is attached to
        voice.sound.Speed = speed;
        voice.sound.Pitch = pitch;

        voice.requested = volume;
        voice.sound.Volume = Scaled(clip.group, volume);
        voice.sound.Pan = pan;
        voice.sound.Play(clip.loop);

        voice.startedAt = ++sequence;
        voice.startedAtMs = atMs;
        clip.lastPlayed = voice;
        clip.plays++;
    }

    /// <summary>
    /// Seeks everything sounding to where it should be by now: a long chip drifts because the sound
    /// device and the performance clock are not the same clock.
    /// </summary>
    public static void Correct(MixerClip clip, long nowMs)
    {
        foreach (Voice voice in clip.voices)
        {
            if (voice.sound.IsPlaying && nowMs > voice.startedAtMs)
            {
                voice.sound.Seek(nowMs - voice.startedAtMs);
            }
        }
    }

    public static void Pause(MixerClip clip, long nowMs)
    {
        foreach (Voice voice in clip.voices)
        {
            if (voice.sound.IsPlaying)
            {
                voice.sound.Pause();
                voice.pausedAtMs = nowMs;
            }
        }
    }

    public static void Resume(MixerClip clip, long nowMs)
    {
        foreach (Voice voice in clip.voices)
        {
            if (voice.pausedAtMs == 0)
            {
                continue;
            }

            voice.sound.Resume(voice.pausedAtMs - voice.startedAtMs);

            //the pause did not happen as far as the chart is concerned, so the start moves with it
            voice.startedAtMs += nowMs - voice.pausedAtMs;
            voice.pausedAtMs = 0;
        }
    }

    /// <summary>Moves when everything sounding is considered to have started, for a timing adjustment
    /// made while a song is playing.</summary>
    public static void ShiftStart(MixerClip clip, long deltaMs)
    {
        foreach (Voice voice in clip.voices)
        {
            if (voice.sound.IsPlaying)
            {
                voice.startedAtMs += deltaMs;
            }
        }
    }

    public static long LengthMs(MixerClip clip) => clip.audio?.LengthMs ?? 0;

    /// <summary>How this clip makes extra voices. Diagnostic; "-" until it has loaded.</summary>
    public static string VoiceKind(MixerClip clip) => clip.audio?.VoiceKind ?? "-";

    /// <summary>
    /// Stops the channel that just started and holds it at <paramref name="positionMs"/> until
    /// <see cref="StartHeld"/>. A song jump sets up every chip that should already be sounding, then
    /// starts them together so they are in sync.
    /// </summary>
    public static void HoldLastAt(MixerClip clip, long positionMs)
    {
        if (clip.lastPlayed is not { } voice)
        {
            return;
        }

        voice.sound.Pause();
        voice.heldPositionMs = positionMs;
        held.Add(voice);
    }

    public static void StartHeld()
    {
        foreach (Voice voice in held)
        {
            voice.sound.Resume(voice.heldPositionMs);
        }

        held.Clear();
    }

    /// <summary>
    /// The level asked for on the channel that sounded most recently, not what the channel ended up at.
    /// A fade reads this back, so it must not see the group level folded in.
    /// </summary>
    public static int CurrentVolume(MixerClip clip) => clip.lastPlayed?.requested ?? 0;

    public static void SetCurrentVolume(MixerClip clip, int volume)
    {
        if (clip.lastPlayed is not { } voice)
        {
            return;
        }

        voice.requested = volume;
        voice.sound.Volume = Scaled(clip.group, volume);
    }

    public static bool IsPlaying(MixerClip clip) => clip.lastPlayed?.sound.IsPlaying ?? false;

    /// <summary>0 to 100.</summary>
    public static int MasterVolume
    {
        get => masterVolume;
        set
        {
            masterVolume = value;
            Device.MasterVolume = value;
        }
    }

    /// <summary>Whether a speed change keeps the original pitch. Decided per sound as it loads, so this
    /// only reaches what is read after it changes.</summary>
    public static bool TimeStretch
    {
        get => timeStretch;
        set
        {
            timeStretch = value;
            Device.TimeStretch = value;
        }
    }

    public static int GetGroupVolume(AudioGroup group) => groupVolumes[(int)group];

    /// <summary>
    /// Sets how loud a whole group is, 0 to 100. The level is folded into each voice, so everything
    /// already sounding is recomputed here.
    /// </summary>
    public static void SetGroupVolume(AudioGroup group, int volume)
    {
        groupVolumes[(int)group] = volume;

        foreach (MixerClip clip in clips)
        {
            if (clip.group != group)
            {
                continue;
            }

            foreach (Voice voice in clip.voices)
            {
                voice.sound.Volume = Scaled(group, voice.requested);
            }
        }
    }

    private static int Scaled(AudioGroup group, int volume) => volume * groupVolumes[(int)group] / 100;

    /// <summary>Creates a clip's first channel, so the first play does not pay for decoding it.</summary>
    public static void Preload(MixerClip clip) => Grow(clip);

    public static void Stop(MixerClip clip)
    {
        foreach (Voice voice in clip.voices)
        {
            voice.sound.Stop();
        }
    }

    /// <summary>
    /// Gives up a clip, letting a one-shot that is still audible finish first. For an owner going away,
    /// not a sound being replaced. A loop is stopped outright, since it would never finish.
    /// </summary>
    public static void Release(MixerClip clip)
    {
        if (clip.loop)
        {
            Free(clip);
            return;
        }

        SetReleasing(clip, true);
        Sweep();
    }

    /// <summary>Stops a clip and frees its channels now.</summary>
    public static void Free(MixerClip clip)
    {
        //not list membership: a load that is cancelled or fails frees clips that were never published,
        //and those still own decoded audio
        if (clip.freed)
        {
            return;
        }

        clip.freed = true;
        Interlocked.Increment(ref clipsFreed);

        if (clip.published)
        {
            clip.published = false;
            clips.Remove(clip);
        }

        SetReleasing(clip, false);

        Unload(clip);
    }

    /// <summary>
    /// Builds a new output. Rebuilding tears the old one down underneath the clips and a freed handle can
    /// be reissued to a new channel, so they are unloaded while their handles are still valid.
    /// </summary>
    public static void Reinitialize(AudioDeviceOptions options)
    {
        //what was loaded stays loaded, or everything read up front reverts to loading on first play.
        //Only BGM resumes: an effect cut off mid-flight is over by the time the new device exists
        resume.Clear();

        foreach (MixerClip clip in clips)
        {
            if (clip.audio == null)
            {
                continue;
            }

            Voice? playing = clip.group == AudioGroup.Bgm && clip.lastPlayed is { sound.IsPlaying: true }
                ? clip.lastPlayed
                : null;

            resume.Add((clip, playing?.requested ?? 100, playing?.sound.Pan ?? 0, playing?.sound.PositionMs ?? -1));
        }

        //unloaded, not freed: a device change is not the owners giving them up
        foreach (MixerClip clip in clips)
        {
            Unload(clip);
        }

        Build(options);

        foreach ((MixerClip clip, int volume, int pan, long positionMs) in resume)
        {
            if (positionMs < 0)
            {
                Preload(clip);
                continue;
            }

            Play(clip, volume, pan);

            //back to where it was cut off, so a switch is a gap rather than a restart
            clip.lastPlayed?.sound.Seek(positionMs);
        }

        resume.Clear();
    }

    /// <summary>Replaces the output, and tells the new one the levels, which live here rather than on
    /// the device.</summary>
    public static void Build(AudioDeviceOptions options)
    {
        Device.Dispose();

        //nothing may read a disposed device while the new one is being built, which can take a moment
        Device = new NullAudioDevice();
        Device = AudioDevice.Create(options);

        if (Device is NullAudioDevice)
        {
            //a pinned output is never revisited by FollowSystemOutput, so one failure on it is final
            failedOutputAttempts = options.OutputDevice.Length > 0
                ? MaxOutputAttempts
                : failedOutputAttempts + 1;

            OutputError = AudioDevice.LastError;
        }
        else
        {
            failedOutputAttempts = 0;
            OutputError = string.Empty;
        }

        Device.MasterVolume = masterVolume;
        Device.TimeStretch = timeStretch;
    }

    /// <summary>Gives the output a fresh set of attempts after <see cref="OutputGaveUp"/>.</summary>
    public static void RetryOutput()
    {
        failedOutputAttempts = 0;
        OutputError = string.Empty;
    }

    /// <summary>Gives up every clip and then the output, since the clips' handles belong to it.</summary>
    public static void Shutdown()
    {
        //backwards because Free takes the clip out of the list
        for (int i = clips.Count - 1; i >= 0; i--)
        {
            Free(clips[i]);
        }

        Device.Dispose();
        Device = new NullAudioDevice();
    }

    /// <summary>Gives up a clip's channels and its loaded audio, keeping the clip itself. It loads again
    /// on its next play.</summary>
    private static void Unload(MixerClip clip)
    {
        //a jump that was set up but never started would otherwise leave this holding voices that are
        //about to be disposed, and StartHeld would resume them
        for (int i = held.Count - 1; i >= 0; i--)
        {
            if (clip.voices.Contains(held[i]))
            {
                held.RemoveAt(i);
            }
        }

        foreach (Voice voice in clip.voices)
        {
            voice.sound.Stop();
        }

        Interlocked.Add(ref voiceCount, -clip.voices.Count);
        clip.voices.Clear();
        clip.lastPlayed = null;

        clip.audio?.Dispose();
        clip.audio = null;
    }

    /// <summary>
    /// Rebuilds on the system default when it changes, unless a device is pinned. Only looks at the
    /// system on a throttle. The caller decides when this is safe to call, since it rebuilds the output.
    /// </summary>
    /// <param name="settings">Only invoked when the throttle lets a check through, so reading config
    /// stays off the per-frame path.</param>
    public static void FollowSystemOutput(Func<AudioDeviceOptions> settings)
    {
        //not the game clock, which is itself rebuilt with the device
        long now = Environment.TickCount64;

        if (now < nextOutputCheck)
        {
            return;
        }

        nextOutputCheck = now + OutputCheckIntervalMs;

        if (OutputGaveUp)
        {
            return;
        }

        AudioDeviceOptions options = settings();

        if (options.OutputDevice.Length > 0)
        {
            return;
        }

        string system = AudioOutputs.SystemDefault(options.Backend);
        string playing = Device.Status.Output;

        //empty means the backend has no default to follow
        if (system.Length == 0 || system == playing)
        {
            return;
        }

        Trace.TraceInformation($"System output is now '{system}' (playing on '{playing}'); " +
                               "rebuilding on it.");

        Reinitialize(options);
    }

    public static void DetachFromMixer(MixerClip clip)
    {
        foreach (Voice voice in clip.voices)
        {
            voice.sound.DetachFromMixer();
        }
    }

    public static void AttachToMixer(MixerClip clip)
    {
        foreach (Voice voice in clip.voices)
        {
            voice.sound.AttachToMixer();
        }
    }

    private static void SetReleasing(MixerClip clip, bool value)
    {
        if (clip.releasing == value)
        {
            return;
        }

        clip.releasing = value;

        if (value)
        {
            releasing.Add(clip);
        }
        else
        {
            releasing.Remove(clip);
        }
    }

    /// <summary>
    /// Reclaims released clips that have fallen silent. Called every frame as well as on every play: a
    /// clip released while nothing is playing would otherwise hold its audio until something did.
    /// </summary>
    public static void Update() => Sweep();

    //backwards because Free takes the clip out of this list, which leaves the lower indices alone
    private static void Sweep()
    {
        for (int i = releasing.Count - 1; i >= 0; i--)
        {
            MixerClip clip = releasing[i];

            if (!AnySounding(clip))
            {
                Free(clip);
            }
        }
    }

    private static bool AnySounding(MixerClip clip)
    {
        foreach (Voice voice in clip.voices)
        {
            if (voice.sound.IsPlaying)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The one channel a clip that must not overlap itself is allowed. Playing music or a loop again
    /// restarts it rather than layering a second copy.
    /// </summary>
    private static Voice? SoleVoice(MixerClip clip)
        => (clip.loop || clip.group == AudioGroup.Bgm) && clip.voices.Count > 0 ? clip.voices[0] : null;

    private static Voice? FreeVoice(MixerClip clip)
    {
        foreach (Voice voice in clip.voices)
        {
            if (!voice.sound.IsPlaying)
            {
                return voice;
            }
        }

        return null;
    }

    private static Voice? Grow(MixerClip clip)
    {
        if (clip.voices.Count >= RunawayGuard)
        {
            if (!warnedRunaway)
            {
                warnedRunaway = true;
                Trace.TraceWarning($"'{clip.name}' wanted more than {RunawayGuard} channels at once; " +
                                   "reusing the one playing longest.");
            }

            return Oldest(clip);
        }

        if (clip.audio == null)
        {
            if (clip.path.Length == 0 || !File.Exists(clip.path))
            {
                return null;
            }

            try
            {
                clip.audio = Device.Load(clip.path, clip.group);
            }
            catch (Exception e)
            {
                Trace.TraceError($"Could not load '{clip.path}': {e.Message}");
                return null;
            }
        }

        if (clip.audio.CreateVoice() is not { } created)
        {
            Trace.TraceError($"Could not create a voice for '{clip.path}'.");
            return null;
        }

        Voice voice = new() { sound = created };
        CountVoiceAdded();

        clip.voices.Add(voice);
        return voice;
    }

    private static void CountVoiceAdded()
    {
        int now = Interlocked.Increment(ref voiceCount);

        //retry while another thread is raising it too
        int peak;
        while (now > (peak = Volatile.Read(ref peakVoiceCount)))
        {
            if (Interlocked.CompareExchange(ref peakVoiceCount, now, peak) == peak)
            {
                break;
            }
        }
    }

    private static Voice? Oldest(MixerClip clip)
    {
        Voice? oldest = null;

        foreach (Voice voice in clip.voices)
        {
            if (oldest == null || voice.startedAt < oldest.startedAt)
            {
                oldest = voice;
            }
        }

        return oldest;
    }
}
