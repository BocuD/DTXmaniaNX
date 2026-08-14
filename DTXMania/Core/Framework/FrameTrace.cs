using System.Globalization;
using System.Text;

namespace DTXMania.Core.Framework;

public static class FrameTrace
{
    //about ten minutes at 100fps. Recording stops at the end rather than wrapping, so what is exported
    //is one contiguous run and not a window whose start moved
    private const int Capacity = 60_000;

    private static readonly int Sections = FrameProfiler.Sections.Length;

    private static readonly float[] totalMs = new float[Capacity];
    private static readonly float[] sectionMs = new float[Capacity * FrameProfiler.SectionCount];
    private static readonly int[] gen0 = new int[Capacity];
    private static readonly int[] gen1 = new int[Capacity];
    private static readonly int[] gen2 = new int[Capacity];
    private static readonly long[] allocated = new long[Capacity];
    private static readonly int[] underruns = new int[Capacity];

    private static int count;
    private static long previousTimestamp;
    private static long previousAllocated;
    private static int previousGen0;
    private static int previousGen1;
    private static int previousGen2;
    private static int previousUnderruns;

    public static bool Recording { get; private set; }

    public static int Frames => count;

    public static bool Full => count >= Capacity;

    public static void Start()
    {
        count = 0;
        previousTimestamp = 0;
        previousAllocated = GC.GetAllocatedBytesForCurrentThread();
        previousGen0 = GC.CollectionCount(0);
        previousGen1 = GC.CollectionCount(1);
        previousGen2 = GC.CollectionCount(2);
        previousUnderruns = Audio.AudioUnderruns.Count;
        Recording = true;
    }

    public static void Stop() => Recording = false;

    /// <summary>
    /// Samples the frame <see cref="FrameProfiler.NewFrame"/> has just rolled up. Allocates nothing, so
    /// recording cannot itself be what provokes a collection.
    /// </summary>
    internal static void Record()
    {
        long now = System.Diagnostics.Stopwatch.GetTimestamp();

        //the first call has no previous frame to measure against, and only sets the origin
        if (previousTimestamp == 0)
        {
            previousTimestamp = now;
            return;
        }

        if (count >= Capacity)
        {
            Recording = false;
            return;
        }

        totalMs[count] = (float)((now - previousTimestamp) * 1000.0
                                 / System.Diagnostics.Stopwatch.Frequency);
        previousTimestamp = now;

        for (int i = 0; i < Sections; i++)
        {
            sectionMs[count * Sections + i] = FrameProfiler.GetLastMs(FrameProfiler.Sections[i]);
        }

        int collected0 = GC.CollectionCount(0);
        int collected1 = GC.CollectionCount(1);
        int collected2 = GC.CollectionCount(2);
        long allocatedNow = GC.GetAllocatedBytesForCurrentThread();
        int underrunsNow = Audio.AudioUnderruns.Count;

        gen0[count] = collected0 - previousGen0;
        gen1[count] = collected1 - previousGen1;
        gen2[count] = collected2 - previousGen2;
        allocated[count] = allocatedNow - previousAllocated;
        underruns[count] = underrunsNow - previousUnderruns;

        previousGen0 = collected0;
        previousGen1 = collected1;
        previousGen2 = collected2;
        previousAllocated = allocatedNow;
        previousUnderruns = underrunsNow;

        count++;
    }

    /// <summary>Writes what has been recorded next to the executable and answers the path.</summary>
    public static string Export()
    {
        Recording = false;

        string path = Path.Combine(CDTXMania.executableDirectory,
            $"frametrace-{DateTime.Now:yyyyMMdd-HHmmss}.csv");

        StringBuilder csv = new(count * 96);

        csv.Append("frame,totalMs,gen0,gen1,gen2,allocatedBytes,underruns");
        for (int i = 0; i < Sections; i++)
        {
            csv.Append(',').Append(FrameProfiler.SectionNames[i]);
        }

        csv.Append('\n');

        for (int frame = 0; frame < count; frame++)
        {
            csv.Append(frame).Append(',')
                .Append(totalMs[frame].ToString("0.###", CultureInfo.InvariantCulture)).Append(',')
                .Append(gen0[frame]).Append(',')
                .Append(gen1[frame]).Append(',')
                .Append(gen2[frame]).Append(',')
                .Append(allocated[frame]).Append(',')
                .Append(underruns[frame]);

            for (int i = 0; i < Sections; i++)
            {
                csv.Append(',').Append(sectionMs[frame * Sections + i]
                    .ToString("0.###", CultureInfo.InvariantCulture));
            }

            csv.Append('\n');
        }

        File.WriteAllText(path, csv.ToString());
        return path;
    }
}
