using System.Diagnostics;
using DTXMania.Core.Audio;

namespace DTXMania.Core;

/// <summary>
/// Owns every channel the game plays a system sound on.
///
/// One voice is one channel with one playback position, so playing it again restarts it and cuts off what
/// it was doing. Sounding twice at once therefore needs two of them. Rather than each sound declaring up
/// front how many times it might overlap — a number nobody can know — this keeps whatever the game has
/// actually asked for: a clip gets another voice the first time it needs one, and every voice is reused
/// from then on. A menu settles at however many its fastest scroll needs and never allocates again.
/// </summary>
public static partial class AudioMixer
{
    //a channel and when it last started, which only matters if a clip ever hits the runaway guard
    private sealed class Voice
    {
        public IAudioVoice sound = null!;
        public long startedAt;
    }

    private sealed class Clip
    {
        //the loaded audio every voice of this clip sounds from
        public IAudioClip? audio;

        public readonly List<Voice> voices = [];
        public IAudioVoice? lastPlayed;

        //freed once it falls silent, rather than cut off; see Release
        public bool releasing;

        //how often this has sounded, for the mixer window to show what the game actually leans on
        public int plays;
    }

    //not a tuning knob: a clip needing this many at once means something is retriggering in a loop, and
    //silently allocating channels forever would be worse than reusing one
    private const int RunawayGuard = 64;

    /// <summary>What actually makes sound. One implementation today; see AUDIO.md for what replaces it.</summary>
    public static IAudioDevice Device { get; set; } = new FdkAudioDevice();

    private static readonly Dictionary<CSystemSound, Clip> clips = new();
    private static long sequence;
    private static bool warnedRunaway;

    /// <summary>Sounds <paramref name="clip"/> on a channel that is free, making one if none is.</summary>
    public static void Play(CSystemSound clip, int volume, int pan)
    {
        //the only pump this needs: anything released is reclaimed the next time audio happens at all
        Sweep();

        Clip state = StateFor(clip);
        state.releasing = false;

        if ((FreeVoice(state) ?? Grow(clip, state)) is not { } voice)
        {
            return;
        }

        voice.sound.Volume = volume;
        voice.sound.Pan = pan;
        voice.sound.Play(clip.loop);

        voice.startedAt = ++sequence;
        state.lastPlayed = voice.sound;
        state.plays++;
    }

    /// <summary>The channel that sounded most recently, so a fade can ride it while it plays.</summary>
    public static IAudioVoice? Current(CSystemSound clip)
        => clips.TryGetValue(clip, out Clip? state) ? state.lastPlayed : null;

    public static bool IsPlaying(CSystemSound clip) => Current(clip)?.IsPlaying ?? false;

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
    /// Gives up a clip, but lets a one-shot that is still audible finish first — for an owner going away
    /// rather than a sound being replaced. A loop is stopped outright, since it would never finish.
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

        //the clip owns the loaded audio and everything sounding from it
        state.audio?.Dispose();
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
                state.audio = Device.Load(path);
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
