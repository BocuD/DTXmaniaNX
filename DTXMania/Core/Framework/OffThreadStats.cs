using System.Diagnostics;

namespace DTXMania.Core.Framework;

public sealed class OffThreadStats(string name)
{
    private static readonly List<OffThreadStats> all = [];

    /// <summary>Every counter that has recorded something, in the order they first did.</summary>
    public static IReadOnlyList<OffThreadStats> All => all;

    private static readonly double TicksToMs = 1000.0 / Stopwatch.Frequency;

    private long items;
    private long totalTicks;
    private long lastTicks;
    private long bytes;
    private int queued;
    private bool listed;

    public string Name => name;

    /// <summary>How many pieces of work the thread has finished.</summary>
    public long Items => Interlocked.Read(ref items);

    public double LastMs => Interlocked.Read(ref lastTicks) * TicksToMs;

    public double AverageMs
    {
        get
        {
            long done = Interlocked.Read(ref items);
            return done == 0 ? 0.0 : Interlocked.Read(ref totalTicks) * TicksToMs / done;
        }
    }

    /// <summary>Bytes the worker thread has allocated, which no per-frame counter can see.</summary>
    public long Bytes => Interlocked.Read(ref bytes);

    /// <summary>Work finished but not yet taken by the main thread.</summary>
    public int Queued => Volatile.Read(ref queued);

    public void Record(long ticks, long allocated, int depth)
    {
        Interlocked.Increment(ref items);
        Interlocked.Add(ref totalTicks, ticks);
        Interlocked.Exchange(ref lastTicks, ticks);
        Interlocked.Add(ref bytes, allocated);
        Volatile.Write(ref queued, depth);

        if (!listed)
        {
            listed = true;
            lock (all)
            {
                all.Add(this);
            }
        }
    }

    /// <summary>Records a depth without any work having finished, so an idle queue still reads true.</summary>
    public void SetQueued(int depth) => Volatile.Write(ref queued, depth);

    public void Reset()
    {
        Interlocked.Exchange(ref items, 0);
        Interlocked.Exchange(ref totalTicks, 0);
        Interlocked.Exchange(ref lastTicks, 0);
        Interlocked.Exchange(ref bytes, 0);
    }

    public static void ResetAll()
    {
        lock (all)
        {
            foreach (OffThreadStats stats in all)
            {
                stats.Reset();
            }
        }
    }
}
