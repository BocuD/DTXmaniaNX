namespace DTXMania.Core.Audio;

/// <summary>
/// How often an output has asked the mixer to fill its buffer and been handed less than it asked for.
/// The mixer is created non-stop, so it answers with silence rather than nothing when it has no source:
/// a short answer means it could not produce in time, which is the dropout being heard.
///
/// Counted rather than logged, because it happens on the audio callback's thread where anything that
/// takes a lock or allocates would itself cause the next one.
/// </summary>
public static class AudioUnderruns
{
    private static int count;

    public static int Count => Volatile.Read(ref count);

    public static void Report() => Interlocked.Increment(ref count);

    /// <summary>Called when an output is built, so a count always belongs to one device.</summary>
    public static void Reset() => Interlocked.Exchange(ref count, 0);
}
