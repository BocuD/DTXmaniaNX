namespace DTXMania.Core.Audio;

/// <summary>
/// An audio output, and where clips come from. Replaces the FDK device layer; see AUDIO.md.
/// </summary>
public interface IAudioDevice
{
    /// <summary>Output / backend name, eg "WASAPI" or "DirectSound"</summary>
    string TypeName { get; }

    /// <summary>Throws if the file cannot be read.</summary>
    IAudioClip Load(string path, AudioGroup group);

    /// <summary>0 to 100.</summary>
    int MasterVolume { get; set; }

    /// <summary>Output level of one group, 0 to 100. Kept even on an output that cannot apply it, so
    /// switching to one that can does not lose the setting.</summary>
    int GetGroupVolume(AudioGroup group);

    void SetGroupVolume(AudioGroup group, int volume);

    /// <summary>
    /// Whether this output applies group volume in its own mixing. When false, the caller has to fold the
    /// group level into each voice itself.
    /// </summary>
    bool MixesGroups { get; }

    /// <summary>
    /// Builds a new output to the given settings. Every clip and voice made before this is dead
    /// afterwards, so use <see cref="AudioMixer.Reinitialize"/>, which gives them up first.
    /// </summary>
    void Reinitialize(AudioDeviceOptions options);

    IReadOnlyList<AudioOutput> Outputs { get; }

    /// <summary>The output in use, which is not the one asked for if that was empty or missing. Empty
    /// when the backend cannot say.</summary>
    string CurrentOutput { get; }
}

/// <summary>
/// Loaded audio: the file, decoded or streamed, once. Not playable on its own — it is the data a voice
/// sounds. Disposing it frees the data and everything sounding from it.
/// </summary>
public interface IAudioClip : IDisposable
{
    /// <summary>
    /// A channel that sounds independently of every other from this clip, with its own position, volume
    /// and pan. How cheap this is depends on the backend and is not the caller's concern. Null if a voice
    /// could not be made.
    /// </summary>
    IAudioVoice? CreateVoice();

    /// <summary>How extra voices of this clip are made. Diagnostic, shown in the mixer window.</summary>
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

    /// <summary>Takes this voice out of the output mix without freeing it. The performance stage does
    /// this to sounds that must not carry into a song.</summary>
    void DetachFromMixer();
}
