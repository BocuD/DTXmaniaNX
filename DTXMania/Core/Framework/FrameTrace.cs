using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace DTXMania.Core.Framework;

public static class FrameTrace
{
    //a couple of minutes at the rates this runs at. The buffer wraps rather than filling up, because what
    //is worth catching is rare: play until it happens, then stop, and the frames before it are still here
    private const int Capacity = 60_000;

    private static readonly int Sections = FrameProfiler.Sections.Length;

    /// <summary>What the whole frame did, beside the per-section detail held separately.</summary>
    private struct FrameRecord
    {
        public float TotalMs;
        public int Gen0;
        public int Gen1;
        public int Gen2;
        public long Allocated;
        public int Underruns;

        //how late the worst audio callback of this frame was, which a count alone does not say
        public float AudioGapMs;
    }

    //int rather than the profiler's long: plenty per section per frame, and it halves what the buffer
    //costs at this capacity
    private readonly record struct SectionSample(float Ms, int Bytes);

    /// <summary>The counters a frame's numbers are differences against.</summary>
    private struct Counters
    {
        public long Timestamp;
        public long Allocated;
        public int Gen0;
        public int Gen1;
        public int Gen2;
        public int Underruns;
    }

    private const int MaxProbes = 24;

    //held only while there is something in them: at this capacity they are tens of megabytes, which is
    //not worth carrying around for a recording nobody asked for
    private static FrameRecord[]? frames;
    private static SectionSample[]? sectionSamples;
    private static int[]? probeBytes;

    /// <summary>What the buffers cost while they exist, which is nothing until a recording starts.</summary>
    public static long HeldBytes => frames == null
        ? 0
        : (long)Capacity * (Unsafe.SizeOf<FrameRecord>()
                            + FrameProfiler.SectionCount * Unsafe.SizeOf<SectionSample>()
                            + MaxProbes * sizeof(int));

    //where the next frame goes, and how many have ever been written; the two differ once it has wrapped
    private static int next;
    private static long written;

    private static Counters previous;

    public static bool Recording { get; private set; }

    /// <summary>How many frames are held, which stops climbing once the buffer has wrapped.</summary>
    public static int Frames => (int)Math.Min(written, Capacity);

    public static bool Full => written >= Capacity;

    public static void Start()
    {
        //a trace of frames nobody measured would be all zeroes
        FrameProfiler.Enabled = true;

        frames ??= new FrameRecord[Capacity];
        sectionSamples ??= new SectionSample[Capacity * FrameProfiler.SectionCount];
        probeBytes ??= new int[Capacity * MaxProbes];

        next = 0;
        written = 0;

        previous = new Counters
        {
            Timestamp = 0,
            Allocated = GC.GetAllocatedBytesForCurrentThread(),
            Gen0 = GC.CollectionCount(0),
            Gen1 = GC.CollectionCount(1),
            Gen2 = GC.CollectionCount(2),
            Underruns = Audio.AudioUnderruns.Count
        };

        Recording = true;
    }

    public static void Stop() => Recording = false;

    /// <summary>Gives the buffers back, losing whatever has not been exported.</summary>
    public static void Release()
    {
        Recording = false;
        frames = null;
        sectionSamples = null;
        probeBytes = null;
        next = 0;
        written = 0;
    }

    /// <summary>
    /// Samples the frame <see cref="FrameProfiler.NewFrame"/> has just rolled up. Allocates nothing, so
    /// recording cannot itself be what provokes a collection.
    /// </summary>
    internal static void Record()
    {
        //Recording is only ever set by Start, which is what makes these
        if (frames == null || sectionSamples == null || probeBytes == null)
        {
            return;
        }

        long now = System.Diagnostics.Stopwatch.GetTimestamp();

        //the first call has no previous frame to measure against, and only sets the origin
        if (previous.Timestamp == 0)
        {
            previous.Timestamp = now;
            return;
        }

        int at = next;

        for (int i = 0; i < Sections; i++)
        {
            FrameProfiler.SectionSample sample = FrameProfiler.GetLast(FrameProfiler.Sections[i]);
            sectionSamples[at * Sections + i] = new SectionSample(sample.Ms, (int)sample.Bytes);
        }

        int probes = Math.Min(AllocationProbe.Count, MaxProbes);
        for (int i = 0; i < probes; i++)
        {
            probeBytes[at * MaxProbes + i] = (int)AllocationProbe.BytesLastFrame(i);
        }

        Counters counters = new()
        {
            Timestamp = now,
            Allocated = GC.GetAllocatedBytesForCurrentThread(),
            Gen0 = GC.CollectionCount(0),
            Gen1 = GC.CollectionCount(1),
            Gen2 = GC.CollectionCount(2),
            Underruns = Audio.AudioUnderruns.Count
        };

        frames[at] = new FrameRecord
        {
            TotalMs = (float)((now - previous.Timestamp) * 1000.0
                              / System.Diagnostics.Stopwatch.Frequency),
            Gen0 = counters.Gen0 - previous.Gen0,
            Gen1 = counters.Gen1 - previous.Gen1,
            Gen2 = counters.Gen2 - previous.Gen2,
            Allocated = counters.Allocated - previous.Allocated,
            Underruns = counters.Underruns - previous.Underruns,
            AudioGapMs = (float)Audio.AudioUnderruns.TakeWorstGapMs()
        };

        previous = counters;

        next = at + 1 == Capacity ? 0 : at + 1;
        written++;
    }

    /// <summary>Writes what has been recorded next to the executable and answers the path.</summary>
    public static string Export()
    {
        Recording = false;

        if (frames == null || sectionSamples == null || probeBytes == null)
        {
            return string.Empty;
        }

        string path = Path.Combine(CDTXMania.executableDirectory,
            $"frametrace-{DateTime.Now:yyyyMMdd-HHmmss}.csv");

        int held = Frames;

        //oldest first once it has wrapped, so a row number still reads as time going forwards
        int oldest = written > Capacity ? next : 0;

        StringBuilder csv = new(held * 96);

        //written at export rather than held per frame, since it does not change during a recording
        Audio.AudioDeviceStatus audio = AudioMixer.Device.Status;
        string device = $",{audio.Backend},{audio.BufferMs},{audio.BufferFrames},{audio.PeriodFrames}";

        csv.Append("frame,totalMs,gen0,gen1,gen2,allocatedBytes,underruns,audioGapMs")
            .Append(",backend,audioBufferMs,audioBufferFrames,audioPeriodFrames");
        for (int i = 0; i < Sections; i++)
        {
            csv.Append(',').Append(FrameProfiler.SectionNames[i]);
        }

        for (int i = 0; i < Sections; i++)
        {
            csv.Append(",B_").Append(FrameProfiler.SectionNames[i]);
        }

        int probes = Math.Min(AllocationProbe.Count, MaxProbes);
        for (int i = 0; i < probes; i++)
        {
            csv.Append(",P_").Append(AllocationProbe.NameOf(i));
        }

        csv.Append('\n');

        for (int frame = 0; frame < held; frame++)
        {
            int at = (oldest + frame) % Capacity;

            ref FrameRecord record = ref frames[at];

            csv.Append(frame).Append(',')
                .Append(record.TotalMs.ToString("0.###", CultureInfo.InvariantCulture)).Append(',')
                .Append(record.Gen0).Append(',')
                .Append(record.Gen1).Append(',')
                .Append(record.Gen2).Append(',')
                .Append(record.Allocated).Append(',')
                .Append(record.Underruns).Append(',')
                .Append(record.AudioGapMs.ToString("0.###", CultureInfo.InvariantCulture))
                .Append(device);

            for (int i = 0; i < Sections; i++)
            {
                csv.Append(',').Append(sectionSamples[at * Sections + i].Ms
                    .ToString("0.###", CultureInfo.InvariantCulture));
            }

            for (int i = 0; i < Sections; i++)
            {
                csv.Append(',').Append(sectionSamples[at * Sections + i].Bytes);
            }

            for (int i = 0; i < probes; i++)
            {
                csv.Append(',').Append(probeBytes[at * MaxProbes + i]);
            }

            csv.Append('\n');
        }

        File.WriteAllText(path, csv.ToString());
        return path;
    }
}
