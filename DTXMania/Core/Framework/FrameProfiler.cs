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
    StageOwnDraw,
    VideoUpload,
    PersistentUiDraw,
    //end of subsections
    Inspector,
    Blit,
    ImGuiRender,
    SwapBuffers,
}

///Lightweight per-frame CPU timing markers. Begin/End (or a using-scope) accumulate time per
///<see cref="FrameSection"/>; NewFrame rolls the totals into a rolling history used for the
///last/average/max readouts in the profiler window. Zero allocations per frame.
public static class FrameProfiler
{
    public static readonly FrameSection[] Sections = Enum.GetValues<FrameSection>();
    public static readonly string[] SectionNames = Enum.GetNames<FrameSection>();

    //a constant so a trace buffer can be sized at field initialisation, before Sections is assigned
    public const int SectionCount = (int)FrameSection.SwapBuffers + 1;

    /// <summary>Zero is a top-level part of the frame.</summary>
    //the enum is declared in the order the frame runs, so depth and order together are the tree
    public static int DepthOf(FrameSection section) => section switch
    {
        FrameSection.PumpUploads or FrameSection.Sound or FrameSection.DeviceScan
            or FrameSection.InputPolling or FrameSection.StageDraw => 1,
        FrameSection.StageOwnDraw or FrameSection.PersistentUiDraw => 2,
        FrameSection.VideoUpload => 3,
        _ => 0
    };

    /// <summary>A caller summing sections has to skip these or it double counts.</summary>
    public static bool IsNested(FrameSection section) => DepthOf(section) > 0;

    private const int HistoryFrames = 120;

    /// <summary>
    /// GPU time of a recent frame in ms, from GL timer queries (set by the host each frame,
    /// lags a few frames behind). Compare against the CPU sections: if this is close to the
    /// total frame time the GPU is the bottleneck, not draw submission.
    /// </summary>
    public static float GpuFrameMs;

    private static readonly double TicksToMs = 1000.0 / Stopwatch.Frequency;

    /// <summary>What one section cost in one frame.</summary>
    //bytes as well as time: a collection is driven by how much is allocated, so finding what allocates is
    //how a stutter caused by one gets fixed rather than tuned around
    public readonly record struct SectionSample(float Ms, long Bytes);

    private struct SectionState
    {
        //where the open Begin started counting, meaningless between an End and the next Begin
        public long StartedAtTicks;
        public long StartedAtBytes;

        public long FrameTicks;
        public long FrameBytes;

        public SectionSample[] History;
    }

    private static readonly SectionState[] states = CreateSections();
    private static int historyIndex;
    private static int recordedFrames;

    private static bool measuring;
    private static bool requested;

    public static bool Enabled
    {
        get => requested;
        set => requested = value;
    }

    /// <summary>Whether the frame being drawn is actually being measured.</summary>
    public static bool Measuring => measuring;

    private static SectionState[] CreateSections()
    {
        var states = new SectionState[Sections.Length];
        for (int i = 0; i < states.Length; i++)
        {
            states[i].History = new SectionSample[HistoryFrames];
        }

        return states;
    }

    public static void NewFrame()
    {
        if (measuring)
        {
            for (int i = 0; i < states.Length; i++)
            {
                ref SectionState state = ref states[i];

                state.History[historyIndex] =
                    new SectionSample((float)(state.FrameTicks * TicksToMs), state.FrameBytes);

                state.FrameTicks = 0;
                state.FrameBytes = 0;
            }

            historyIndex = (historyIndex + 1) % HistoryFrames;
            if (recordedFrames < HistoryFrames)
            {
                recordedFrames++;
            }

            AllocationProbe.NewFrame();

            //after the roll, so the trace reads the frame that has just finished rather than the one before
            if (FrameTrace.Recording)
            {
                FrameTrace.Record();
            }
        }

        //applied here so a frame is measured by one setting throughout. The frame this turns on for was
        //not measured, so its history entry rolls in as zero
        measuring = requested;
        AllocationProbe.Enabled = measuring;
    }

    public static void Begin(FrameSection section)
    {
        if (!measuring)
        {
            return;
        }

        ref SectionState state = ref states[(int)section];

        state.StartedAtBytes = GC.GetAllocatedBytesForCurrentThread();
        state.StartedAtTicks = Stopwatch.GetTimestamp();
    }

    /// <summary>Multiple Begin/End pairs of the same section within a frame accumulate.</summary>
    public static void End(FrameSection section)
    {
        if (!measuring)
        {
            return;
        }

        ref SectionState state = ref states[(int)section];

        state.FrameTicks += Stopwatch.GetTimestamp() - state.StartedAtTicks;
        state.FrameBytes += GC.GetAllocatedBytesForCurrentThread() - state.StartedAtBytes;
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

    /// <summary>What the section cost during the most recently completed frame.</summary>
    public static SectionSample GetLast(FrameSection section)
    {
        int lastIndex = (historyIndex - 1 + HistoryFrames) % HistoryFrames;
        return states[(int)section].History[lastIndex];
    }

    public static float GetLastMs(FrameSection section) => GetLast(section).Ms;

    public static long GetLastBytes(FrameSection section) => GetLast(section).Bytes;

    public static float GetAverageMs(FrameSection section)
    {
        if (recordedFrames == 0)
        {
            return 0f;
        }

        SectionSample[] history = states[(int)section].History;
        float sum = 0f;
        for (int i = 0; i < recordedFrames; i++)
        {
            sum += history[i].Ms;
        }

        return sum / recordedFrames;
    }

    /// <summary>Bytes this thread allocated in the section, averaged over the recorded frames.</summary>
    public static long GetAverageBytes(FrameSection section)
    {
        if (recordedFrames == 0)
        {
            return 0;
        }

        SectionSample[] history = states[(int)section].History;
        long sum = 0;
        for (int i = 0; i < recordedFrames; i++)
        {
            sum += history[i].Bytes;
        }

        return sum / recordedFrames;
    }

    public static float GetMaxMs(FrameSection section)
    {
        SectionSample[] history = states[(int)section].History;
        float max = 0f;
        for (int i = 0; i < recordedFrames; i++)
        {
            if (history[i].Ms > max)
            {
                max = history[i].Ms;
            }
        }

        return max;
    }
}
