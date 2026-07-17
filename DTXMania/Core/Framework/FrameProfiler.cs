using System.Diagnostics;

namespace DTXMania.Core.Framework;

public enum FrameSection
{
    PollEvents,
    ImGuiNewFrame,
    Update,
    GameRender,
    //subsections of GameRender (the game combines update+draw in CDTXMania.Draw)
    PumpUploads,
    Sound,
    DeviceScan,
    InputPolling,
    StageDraw,
    //end of subsections
    Inspector,
    Blit,
    ImGuiRender,
    SwapBuffers,
}

///Lightweight per-frame CPU timing markers. Begin/End (or a using-scope) accumulate time per
///<see cref="FrameSection"/>; NewFrame rolls the totals into a rolling history used for the
///last/average/max readouts in the inspector's Game Status window. Zero allocations per frame.
public static class FrameProfiler
{
    public static readonly FrameSection[] Sections = Enum.GetValues<FrameSection>();
    public static readonly string[] SectionNames = Enum.GetNames<FrameSection>();

    private const int HistoryFrames = 120;

    /// <summary>
    /// GPU time of a recent frame in ms, from GL timer queries (set by the host each frame,
    /// lags a few frames behind). Compare against the CPU sections: if this is close to the
    /// total frame time the GPU is the bottleneck, not draw submission.
    /// </summary>
    public static float GpuFrameMs;

    private static readonly double TicksToMs = 1000.0 / Stopwatch.Frequency;

    private static readonly long[] startTimestamps = new long[Sections.Length];
    private static readonly long[] currentFrameTicks = new long[Sections.Length];
    private static readonly float[][] historyMs = CreateHistory();
    private static int historyIndex;
    private static int recordedFrames;

    private static float[][] CreateHistory()
    {
        var history = new float[Sections.Length][];
        for (int i = 0; i < history.Length; i++)
        {
            history[i] = new float[HistoryFrames];
        }

        return history;
    }

    public static void NewFrame()
    {
        for (int i = 0; i < Sections.Length; i++)
        {
            historyMs[i][historyIndex] = (float)(currentFrameTicks[i] * TicksToMs);
            currentFrameTicks[i] = 0;
        }

        historyIndex = (historyIndex + 1) % HistoryFrames;
        if (recordedFrames < HistoryFrames)
        {
            recordedFrames++;
        }
    }

    public static void Begin(FrameSection section)
    {
        startTimestamps[(int)section] = Stopwatch.GetTimestamp();
    }

    /// <summary>Multiple Begin/End pairs of the same section within a frame accumulate.</summary>
    public static void End(FrameSection section)
    {
        currentFrameTicks[(int)section] += Stopwatch.GetTimestamp() - startTimestamps[(int)section];
    }

    public static SectionScope Scope(FrameSection section)
    {
        Begin(section);
        return new SectionScope(section);
    }

    public readonly struct SectionScope(FrameSection section) : IDisposable
    {
        public void Dispose() => End(section);
    }

    /// <summary>Milliseconds spent in the section during the most recently completed frame.</summary>
    public static float GetLastMs(FrameSection section)
    {
        int lastIndex = (historyIndex - 1 + HistoryFrames) % HistoryFrames;
        return historyMs[(int)section][lastIndex];
    }

    public static float GetAverageMs(FrameSection section)
    {
        if (recordedFrames == 0)
        {
            return 0f;
        }

        float[] history = historyMs[(int)section];
        float sum = 0f;
        for (int i = 0; i < recordedFrames; i++)
        {
            sum += history[i];
        }

        return sum / recordedFrames;
    }

    public static float GetMaxMs(FrameSection section)
    {
        float[] history = historyMs[(int)section];
        float max = 0f;
        for (int i = 0; i < recordedFrames; i++)
        {
            if (history[i] > max)
            {
                max = history[i];
            }
        }

        return max;
    }
}
