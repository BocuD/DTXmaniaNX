using System.Diagnostics;
using DTXMania.Core.Audio;

namespace DTXMania.Core;

/// <summary>
/// Owns every channel the game plays a system sound on.
///
/// One voice is one channel with one playback position, so playing it again restarts it. Sounding twice
/// at once needs two of them. A clip gets another voice the first time it needs one and reuses it from
/// then on, so a pool settles at whatever the game actually asks for and stops growing.
/// </summary>
public static partial class AudioMixer
{
    //a channel and when it last started, which only matters if a clip ever hits the runaway guard
    private sealed class Voice
    {
        public IAudioVoice sound = null!;
        public long startedAt;

        //the level asked for, before the group level was folded in. The voice only knows the result, so
        //moving a group slider needs this to recompute what is already sounding
        public int requested = 100;
    }

    private sealed class Clip
    {
        //the loaded audio every voice of this clip sounds from
        public IAudioClip? audio;

        public readonly List<Voice> voices = [];
        public Voice? lastPlayed;

        //freed once it falls silent, rather than cut off; see Release
        public bool releasing;

        //how often this has sounded, shown in the mixer window
        public int plays;
    }

    //a clip needing this many at once is retriggering in a loop; reuse a channel rather than keep growing
    private const int RunawayGuard = 64;

    public static IAudioDevice Device { get; set; } = new FdkAudioDevice();

    //enumerating outputs costs a round trip to the driver, so the system is only asked this often
    private const int OutputCheckIntervalMs = 1000;

    private static readonly Dictionary<CSystemSound, Clip> clips = new();
    private static long sequence;
    private static long nextOutputCheck;
    private static bool warnedRunaway;

    /// <summary>Voices held right now. Each one is a channel in the output mix. FDK's own counters do not
    /// see these.</summary>
    public static int VoiceCount { get; private set; }

    /// <summary>The most ever held. Still climbing long after startup means something is retriggering
    /// faster than it finishes.</summary>
    public static int PeakVoiceCount { get; private set; }

    /// <summary>Sounds <paramref name="clip"/> on a channel that is free, making one if none is.</summary>
    public static void Play(CSystemSound clip, int volume, int pan)
    {
        //the only pump there is; released clips are reclaimed the next time anything plays
        Sweep();

        Clip state = StateFor(clip);
        state.releasing = false;

        if ((FreeVoice(state) ?? Grow(clip, state)) is not { } voice)
        {
            return;
        }

        voice.requested = volume;
        voice.sound.Volume = Scaled(clip.group, volume);
        voice.sound.Pan = pan;
        voice.sound.Play(clip.loop);

        voice.startedAt = ++sequence;
        state.lastPlayed = voice;
        state.plays++;
    }

    /// <summary>
    /// The level asked for on the channel that sounded most recently, not what the channel ended up at.
    /// A fade reads this back, so it must not see the group level folded in.
    /// </summary>
    public static int CurrentVolume(CSystemSound clip) => Latest(clip)?.requested ?? 0;

    public static void SetCurrentVolume(CSystemSound clip, int volume)
    {
        if (Latest(clip) is not { } voice)
        {
            return;
        }

        voice.requested = volume;
        voice.sound.Volume = Scaled(clip.group, volume);
    }

    public static bool IsPlaying(CSystemSound clip) => Latest(clip)?.sound.IsPlaying ?? false;

    private static Voice? Latest(CSystemSound clip)
        => clips.TryGetValue(clip, out Clip? state) ? state.lastPlayed : null;

    public static int GetGroupVolume(AudioGroup group) => Device.GetGroupVolume(group);

    /// <summary>
    /// Sets how loud a whole group is, 0 to 100. An output that mixes groups applies it to everything it
    /// plays. One that does not only covers voices this mixer holds, and those have to be recomputed.
    /// </summary>
    public static void SetGroupVolume(AudioGroup group, int volume)
    {
        Device.SetGroupVolume(group, volume);

        if (Device.MixesGroups)
        {
            return;
        }

        foreach ((CSystemSound clip, Clip state) in clips)
        {
            if (clip.group != group)
            {
                continue;
            }

            foreach (Voice voice in state.voices)
            {
                voice.sound.Volume = Scaled(group, voice.requested);
            }
        }
    }

    private static int Scaled(AudioGroup group, int volume)
        => Device.MixesGroups ? volume : volume * Device.GetGroupVolume(group) / 100;

    /// <summary>Creates a clip's first channel, so the first play does not pay for decoding it.</summary>
    public static void Preload(CSystemSound clip) => Grow(clip, StateFor(clip));

    public static void Stop(CSystemSound clip)
    {
        if (!clips.TryGetValue(clip, out Clip? state))
        {
            return;
        }

        foreach (Voice voice in state.voices)
        {
            voice.sound.Stop();
        }
    }

    /// <summary>
    /// Gives up a clip, letting a one-shot that is still audible finish first. For an owner going away,
    /// not for a sound being replaced. A loop is stopped outright, since it would never finish.
    /// </summary>
    public static void Release(CSystemSound clip)
    {
        if (!clips.TryGetValue(clip, out Clip? state))
        {
            return;
        }

        if (clip.loop)
        {
            Free(clip);
            return;
        }

        state.releasing = true;
        Sweep();
    }

    /// <summary>Stops a clip and frees its channels now.</summary>
    public static void Free(CSystemSound clip)
    {
        if (!clips.Remove(clip, out Clip? state))
        {
            return;
        }

        foreach (Voice voice in state.voices)
        {
            voice.sound.Stop();
        }

        VoiceCount -= state.voices.Count;

        //the clip owns the loaded audio and everything sounding from it
        state.audio?.Dispose();
    }

    /// <summary>
    /// Builds a new output, giving up every clip first. Rebuilding tears BASS down underneath them, and a
    /// freed handle can be reissued to a channel of the new device, so they have to go while their handles
    /// are still valid. A clip still in use reloads on its next play.
    /// </summary>
    public static void Reinitialize(AudioDeviceOptions options)
    {
        foreach (CSystemSound clip in clips.Keys.ToArray())
        {
            Free(clip);
        }

        Device.Reinitialize(options);
    }

    /// <summary>
    /// Rebuilds on the system default when it changes, unless a device is pinned. Cheap to call every
    /// frame; it only looks at the system on a throttle. The caller decides when this is safe to call at
    /// all, since it rebuilds the output.
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

        AudioDeviceOptions options = settings();

        if (options.OutputDevice.Length > 0)
        {
            return;
        }

        string system = AudioOutputs.SystemDefault(options.Backend);

        //empty means the backend has no default to follow
        if (system.Length == 0 || system == Device.CurrentOutput)
        {
            return;
        }

        Trace.TraceInformation($"System output is now '{system}' (playing on " +
                               $"'{Device.CurrentOutput}'); rebuilding on it.");

        Reinitialize(options);
    }

    public static void RemoveMixer(CSystemSound clip)
    {
        if (!clips.TryGetValue(clip, out Clip? state))
        {
            return;
        }

        foreach (Voice voice in state.voices)
        {
            voice.sound.DetachFromMixer();
        }
    }

    private static void Sweep()
    {
        //released clips only; anything still owned keeps its channels for the next play
        List<CSystemSound>? silent = null;

        foreach ((CSystemSound clip, Clip state) in clips)
        {
            if (!state.releasing || state.voices.Any(voice => voice.sound.IsPlaying))
            {
                continue;
            }

            silent ??= [];
            silent.Add(clip);
        }

        foreach (CSystemSound clip in silent ?? [])
        {
            Free(clip);
        }
    }

    private static Clip StateFor(CSystemSound clip)
    {
        if (!clips.TryGetValue(clip, out Clip? state))
        {
            state = new Clip();
            clips[clip] = state;
        }

        return state;
    }

    private static Voice? FreeVoice(Clip state)
    {
        foreach (Voice voice in state.voices)
        {
            if (!voice.sound.IsPlaying)
            {
                return voice;
            }
        }

        return null;
    }

    private static Voice? Grow(CSystemSound clip, Clip state)
    {
        if (state.voices.Count >= RunawayGuard)
        {
            if (!warnedRunaway)
            {
                warnedRunaway = true;
                Trace.TraceWarning($"'{clip.strFilename}' wanted more than {RunawayGuard} channels at once; " +
                                   "reusing the one playing longest.");
            }

            return Oldest(state);
        }

        string path = clip.ResolvedPath;

        if (state.audio == null)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                state.audio = Device.Load(path, clip.group);
            }
            catch (Exception e)
            {
                Trace.TraceError($"Could not load '{path}': {e.Message}");
                return null;
            }
        }

        if (state.audio.CreateVoice() is not { } created)
        {
            Trace.TraceError($"Could not create a voice for '{path}'.");
            return null;
        }

        Voice voice = new() { sound = created };

        VoiceCount++;
        PeakVoiceCount = Math.Max(PeakVoiceCount, VoiceCount);

        state.voices.Add(voice);
        return voice;
    }

    private static Voice? Oldest(Clip state)
    {
        Voice? oldest = null;

        foreach (Voice voice in state.voices)
        {
            if (oldest == null || voice.startedAt < oldest.startedAt)
            {
                oldest = voice;
            }
        }

        return oldest;
    }
}
