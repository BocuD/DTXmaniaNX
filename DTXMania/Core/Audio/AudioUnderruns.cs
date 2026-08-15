using System.Diagnostics;

namespace DTXMania.Core.Audio;

/// <summary>
/// How often the output's callback arrived too late to keep the card fed.
///
/// Measured as the gap between calls, not as a short read: the mixer is non-stop, so it pads a request it
/// has nothing for with silence and always returns the full length. Counted rather than logged, because
/// this runs on the audio thread where taking a lock or allocating would cause the next one.
/// </summary>
public static class AudioUnderruns
{
    //only the one callback thread writes these, so plain reads and writes are enough between them
    private static long previousCallback;
    private static int late;
    private static long worstGapTicks;

    /// <summary>Callbacks that arrived later than the buffer could cover.</summary>
    public static int Count => Volatile.Read(ref late);

    /// <summary>The longest gap between two callbacks since the device was built.</summary>
    public static double WorstGapMs => Volatile.Read(ref worstGapTicks) * 1000.0 / Stopwatch.Frequency;

    /// <summary>
    /// Called at the top of an output's callback. <paramref name="bufferMs"/> is how much audio is
    /// queued ahead, so a gap longer than that is the card having run dry before this call landed.
    /// </summary>
    public static void Observe(long bufferMs)
    {
        long now = Stopwatch.GetTimestamp();
        long previous = previousCallback;
        previousCallback = now;

        if (previous == 0 || bufferMs <= 0)
        {
            return;
        }

        long gap = now - previous;

        if (gap > Volatile.Read(ref worstGapTicks))
        {
            Volatile.Write(ref worstGapTicks, gap);
        }

        if (gap * 1000.0 / Stopwatch.Frequency > bufferMs)
        {
            Interlocked.Increment(ref late);
        }
    }

    /// <summary>Called when an output is built, so a count always belongs to one device.</summary>
    public static void Reset()
    {
        Interlocked.Exchange(ref late, 0);
        Volatile.Write(ref worstGapTicks, 0);
        previousCallback = 0;
    }
}
