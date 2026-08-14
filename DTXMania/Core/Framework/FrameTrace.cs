using System.Globalization;
using System.Text;

namespace DTXMania.Core.Framework;

public static class FrameTrace
{
    //a couple of minutes at the rates this runs at. The buffer wraps rather than filling up, because what
    //is worth catching is rare: play until it happens, then stop, and the frames before it are still here
    private const int Capacity = 60_000;

    private static readonly int Sections = FrameProfiler.Sections.Length;

    private static readonly float[] totalMs = new float[Capacity];
    private static readonly float[] sectionMs = new float[Capacity * FrameProfiler.SectionCount];
    private static readonly int[] gen0 = new int[Capacity];
    private static readonly int[] gen1 = new int[Capacity];
    private static readonly int[] gen2 = new int[Capacity];
    private static readonly long[] allocated = new long[Capacity];
    private static readonly int[] underruns = new int[Capacity];

    //where the next frame goes, and how many have ever been written; the two differ once it has wrapped
    private static int next;
    private static long written;

    private static long previousTimestamp;
    private static long previousAllocated;
    private static int previousGen0;
    private static int previousGen1;
    private static int previousGen2;
    private static int previousUnderruns;

    public static bool Recording { get; private set; }

    /// <summary>How many frames are held, which stops climbing once the buffer has wrapped.</summary>
    public static int Frames => (int)Math.Min(written, Capacity);

    public static bool Full => written >= Capacity;

    public static void Start()
    {
        next = 0;
        written = 0;
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

        int at = next;

        totalMs[at] = (float)((now - previousTimestamp) * 1000.0
                              / System.Diagnostics.Stopwatch.Frequency);
        previousTimestamp = now;

        for (int i = 0; i < Sections; i++)
        {
            sectionMs[at * Sections + i] = FrameProfiler.GetLastMs(FrameProfiler.Sections[i]);
        }

        int collected0 = GC.CollectionCount(0);
        int collected1 = GC.CollectionCount(1);
        int collected2 = GC.CollectionCount(2);
        long allocatedNow = GC.GetAllocatedBytesForCurrentThread();
        int underrunsNow = Audio.AudioUnderruns.Count;

        gen0[at] = collected0 - previousGen0;
        gen1[at] = collected1 - previousGen1;
        gen2[at] = collected2 - previousGen2;
        allocated[at] = allocatedNow - previousAllocated;
        underruns[at] = underrunsNow - previousUnderruns;

        previousGen0 = collected0;
        previousGen1 = collected1;
        previousGen2 = collected2;
        previousAllocated = allocatedNow;
        previousUnderruns = underrunsNow;

        next = at + 1 == Capacity ? 0 : at + 1;
        written++;
    }

    /// <summary>Writes what has been recorded next to the executable and answers the path.</summary>
    public static string Export()
    {
        Recording = false;

        string path = Path.Combine(CDTXMania.executableDirectory,
            $"frametrace-{DateTime.Now:yyyyMMdd-HHmmss}.csv");

        int held = Frames;

        //oldest first once it has wrapped, so a row number still reads as time going forwards
        int oldest = written > Capacity ? next : 0;

        StringBuilder csv = new(held * 96);

        csv.Append("frame,totalMs,gen0,gen1,gen2,allocatedBytes,underruns");
        for (int i = 0; i < Sections; i++)
        {
            csv.Append(',').Append(FrameProfiler.SectionNames[i]);
        }

        csv.Append('\n');

        for (int frame = 0; frame < held; frame++)
        {
            int at = (oldest + frame) % Capacity;

            csv.Append(frame).Append(',')
                .Append(totalMs[at].ToString("0.###", CultureInfo.InvariantCulture)).Append(',')
                .Append(gen0[at]).Append(',')
                .Append(gen1[at]).Append(',')
                .Append(gen2[at]).Append(',')
                .Append(allocated[at]).Append(',')
                .Append(underruns[at]);

            for (int i = 0; i < Sections; i++)
            {
                csv.Append(',').Append(sectionMs[at * Sections + i]
                    .ToString("0.###", CultureInfo.InvariantCulture));
            }

            csv.Append('\n');
        }

        File.WriteAllText(path, csv.ToString());
        return path;
    }
}
