namespace DTXMania.Core.Audio;

/// <summary>
/// How long a sound waits between being played and reaching the card, in milliseconds. Both -1 when the
/// device cannot say.
/// </summary>
public readonly record struct AudioLatency(double Typical, double Worst)
{
    public static readonly AudioLatency Unknown = new(-1.0, -1.0);

    public bool IsKnown => Worst >= 0.0;

    /// <summary>
    /// The wait a buffer topped up every <paramref name="periodMs"/> imposes. A pulled output is already
    /// holding the buffer when a sound arrives, so it waits out only what is queued ahead of it. A pushed
    /// one waits for the next top-up first, so the period lands on top of the buffer instead of inside
    /// it. Measured in AUDIO.md §9.11.
    /// </summary>
    public static AudioLatency FromBuffer(double bufferMs, double periodMs, bool pulls) => bufferMs < 0.0
        ? Unknown
        : pulls
            ? new AudioLatency(Math.Max(bufferMs - periodMs / 2.0, 0.0), bufferMs)
            : new AudioLatency(bufferMs + periodMs / 2.0, bufferMs + periodMs);
}

/// <summary>
/// An audio output, and where clips come from. Disposing it kills every clip and voice made from it,
/// so <see cref="AudioMixer.Reinitialize"/> is what swaps one: it gives them up first.
/// </summary>
public interface IAudioDevice : IDisposable
{
    AudioDeviceStatus Status { get; }

    /// <summary>
    /// What a hit waits before it is heard, of the part this device is responsible for. The DAC and
    /// anything after it are on top and cannot be known from here.
    ///
    /// The default treats <see cref="Status"/> as a pulled buffer, which is what WASAPI and ASIO are. A
    /// backend shaped differently overrides this; one that cannot say leaves
    /// <see cref="AudioDeviceStatus.BufferMs"/> negative and gets <see cref="AudioLatency.Unknown"/>.
    /// </summary>
    AudioLatency Latency
    {
        get
        {
            AudioDeviceStatus status = Status;

            return status.BufferMs < 0
                ? AudioLatency.Unknown
                : AudioLatency.FromBuffer(status.BufferLatencyMs, status.PeriodMs, true);
        }
    }

    /// <summary>Throws if the file cannot be read.</summary>
    IAudioClip Load(string path, AudioGroup group);

    /// <summary>0 to 100. Applied in the output graph; group levels are folded into each voice by
    /// <see cref="AudioMixer"/> instead, so no backend has to mix groups.</summary>
    int MasterVolume { get; set; }

    /// <summary>Change speed without changing pitch. Costs a tempo stream per sound, which is why it is
    /// a setting.</summary>
    bool TimeStretch { get; set; }

    /// <summary>Whether taking a voice out of the mix buys anything. A chart only schedules attach and
    /// detach per chip when it does.</summary>
    bool MixesChannels { get; }

    /// <summary>Whether a voice's position drifts from <see cref="ElapsedMs"/>, so a long chip has to be
    /// seeked back onto it.</summary>
    bool NeedsDriftCorrection { get; }

    /// <summary>The clock a chart is played against, in ms.</summary>
    long ElapsedMs { get; }

    /// <summary>An input device timestamp translated onto that clock.</summary>
    long ElapsedMsFor(long deviceTimestamp);
}

/// <summary>
/// A file decoded or streamed once. A voice is what sounds it. Disposing it frees the data and
/// everything sounding from it.
/// </summary>
public interface IAudioClip : IDisposable
{
    /// <summary>
    /// A channel that sounds independently of every other from this clip, with its own position, volume
    /// and pan. Null if one could not be made.
    /// </summary>
    IAudioVoice? CreateVoice();

    /// <summary>How extra voices of this clip are made. Diagnostic, shown in the mixer window.</summary>
    string VoiceKind { get; }

    /// <summary>How long the whole clip is, in ms. 0 if it cannot be told.</summary>
    long LengthMs { get; }
}

/// <summary>One playing channel of a clip.</summary>
public interface IAudioVoice : IDisposable
{
    bool IsPlaying { get; }

    /// <summary>0 to 100.</summary>
    int Volume { get; set; }

    /// <summary>-100 hard left, 0 centre, 100 hard right.</summary>
    int Pan { get; set; }

    /// <summary>
    /// Playback rate, 1.0 being unchanged. With time stretch on this needs a tempo stream, which only a
    /// stream-backed voice has.
    /// </summary>
    double Speed { get; set; }

    /// <summary>Frequency multiplier, 1.0 being unchanged. The wrong-note detune on guitar and bass.</summary>
    double Pitch { get; set; }

    long PositionMs { get; }

    /// <summary>Moves playback to <paramref name="positionMs"/>. Only meaningful while sounding.</summary>
    void Seek(long positionMs);

    void Play(bool loop);

    void Stop();

    /// <summary>Stops without giving up where it was, so <see cref="Resume"/> can pick it up.</summary>
    void Pause();

    /// <summary>Starts again from <paramref name="positionMs"/>.</summary>
    void Resume(long positionMs);

    /// <summary>Takes this voice out of the output mix without freeing it. The performance stage does
    /// this to sounds that must not carry into a song.</summary>
    void DetachFromMixer();

    /// <summary>Puts it back. A chart attaches a chip shortly before it sounds and detaches it after.</summary>
    void AttachToMixer();
}
