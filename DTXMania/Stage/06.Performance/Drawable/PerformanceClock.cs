using DTXMania.Core;

namespace DTXMania;

public static class PerformanceClock
{
    public static long TimeMs => CDTXMania.Timer.nCurrentTime;

    public static float Seconds => CDTXMania.Timer.nCurrentTime / 1000f;

    public static int Frame(int frameMs, int frames) => (int)(TimeMs / frameMs % frames);
}
