using DTXMania.UI.Drawable;
using Newtonsoft.Json;

namespace DTXMania.UI.Animation;

/// <summary>
/// A named collection of tracks with a duration. Serializable to a flat JSON file.
/// </summary>
public sealed class AnimationClip
{
    [JsonProperty("name")] public string name = string.Empty;
    [JsonProperty("duration")] public float duration = 1f;
    [JsonProperty("frameRate")] public float frameRate = 60f;
    [JsonProperty("loop")] public bool loop;
    [JsonProperty("tracks")] public List<AnimationTrack> tracks = new();

    public void InvalidateBindings()
    {
        foreach (AnimationTrack track in tracks)
        {
            track.Invalidate();
        }
    }

    public void Evaluate(UIGroup root, float time)
    {
        foreach (AnimationTrack track in tracks)
        {
            track.Evaluate(root, time);
        }
    }
}

/// <summary>
/// Lives on a UIGroup and drives one clip at a time. Tick from UIGroup.Draw before children
/// render so animation writes are visible the same frame.
/// </summary>
public sealed partial class Animator
{
    [JsonProperty("clips")] public List<AnimationClip> clips = new();

    [JsonProperty("autoPlay", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string? autoPlayClip;

    [JsonIgnore] public AnimationClip? currentClip;
    [JsonIgnore] public float time;
    [JsonIgnore] public bool isPlaying;
    [JsonIgnore] public float speed = 1f;

    [JsonIgnore] private long lastTickTicks;
    [JsonIgnore] private bool tickTimerStarted;

    public AnimationClip? FindClip(string name) => clips.FirstOrDefault(c => c.name == name);

    public void Play(string clipName, bool restart = true)
    {
        AnimationClip? clip = FindClip(clipName);
        if (clip == null)
        {
            return;
        }
        if (currentClip != clip || restart)
        {
            currentClip = clip;
            time = 0f;
            currentClip.InvalidateBindings();
        }
        isPlaying = true;
    }

    public void Pause() => isPlaying = false;
    public void Resume() => isPlaying = true;
    public void Stop()
    {
        isPlaying = false;
        time = 0f;
    }

    /// <summary>
    /// Call once per frame from UIGroup.Draw with the elapsed time since the last call.
    /// Evaluates the current clip and writes values onto the tree rooted at <paramref name="root"/>.
    /// </summary>
    public void Tick(UIGroup root, float deltaSeconds)
    {
        if (currentClip == null && autoPlayClip != null)
        {
            Play(autoPlayClip);
        }

        if (!isPlaying || currentClip == null)
        {
            return;
        }

        time += deltaSeconds * speed;

        if (currentClip.duration > 0f)
        {
            if (time >= currentClip.duration)
            {
                if (currentClip.loop)
                {
                    time %= currentClip.duration;
                }
                else
                {
                    time = currentClip.duration;
                    isPlaying = false;
                }
            }
        }

        currentClip.Evaluate(root, time);
    }

    /// <summary>
    /// Convenience tick that measures delta itself from System.Diagnostics.Stopwatch. Use this
    /// from UIGroup.Draw so we don't have to thread a delta through the existing Draw signature.
    /// </summary>
    public void TickAuto(UIGroup root)
    {
        long now = System.Diagnostics.Stopwatch.GetTimestamp();
        float delta;
        if (!tickTimerStarted)
        {
            tickTimerStarted = true;
            delta = 0f;
        }
        else
        {
            delta = (float)((now - lastTickTicks) / (double)System.Diagnostics.Stopwatch.Frequency);
        }
        lastTickTicks = now;
        // Clamp delta to avoid huge jumps after pauses / breakpoints.
        if (delta > 0.25f) delta = 0.25f;
        Tick(root, delta);
    }

    /// <summary>
    /// Call when the children tree under the root has been restructured (added/removed/renamed
    /// drawables). Forces all tracks to re-resolve their targets on next evaluation.
    /// </summary>
    public void InvalidateBindings()
    {
        foreach (AnimationClip clip in clips)
        {
            clip.InvalidateBindings();
        }
    }
}