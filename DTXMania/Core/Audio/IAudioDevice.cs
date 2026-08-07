namespace DTXMania.Core.Audio;

/// <summary>
/// New Audio device implementation. Provides a thinner and more structured API than the old FDK device layer
/// with the goal to eventually fully replace it.
/// </summary>
public interface IAudioDevice
{
    /// <summary>Output / backend name, eg "WASAPI" or "DirectSound"</summary>
    string TypeName { get; }

    /// <summary>Reads a file so it can be played. Throws if it cannot be read</summary>
    IAudioClip Load(string path);
}

/// <summary>
/// Loaded audio: the file, decoded or streamed, once. A clip is not playable on its own, it is the data
/// a voice sounds. Disposing it frees the data and everything sounding from it.
/// </summary>
public interface IAudioClip : IDisposable
{
    /// <summary>
    /// A channel that can sound independently of every other from this clip, with its own position,
    /// volume and pan. How cheap this is depends on the backend and is deliberately not the caller's
    /// concern: a sample hands one out for nothing, and anything else falls back to reloading the file.
    /// Returns null if a voice could not be made.
    /// </summary>
    IAudioVoice? CreateVoice();

    /// <summary>How extra voices of this clip are made, for the mixer window. Diagnostic only — it is the
    /// difference between believing a decode is shared and being able to see that it is.</summary>
    string VoiceKind { get; }
}

/// <summary>One playing channel of a clip.</summary>
public interface IAudioVoice : IDisposable
{
    bool IsPlaying { get; }

    /// <summary>0 to 100.</summary>
    int Volume { get; set; }

    /// <summary>-100 hard left, 0 centre, 100 hard right.</summary>
    int Pan { get; set; }

    void Play(bool loop);

    void Stop();

    /// <summary>Takes this voice out of the output mix without freeing it, which the performance stage
    /// does to sounds that must not carry into a song.</summary>
    void DetachFromMixer();
}
