namespace DTXMania.Core.Framework;

/// <summary>
/// Bytes this thread allocates inside named scopes, per frame.
///
/// <see cref="FrameProfiler"/> reports the same per section, but its sections are the frame's own
/// structure and stop at whole stages. A probe goes around anything smaller, and shows up beside the
/// sections in the profiler window.
///
/// Registration is once per scope and the measurement is two thread-local reads, so nothing here
/// allocates while it runs.
/// </summary>
public static class AllocationProbe
{
    private const int MaxScopes = 64;

    //a struct in an array rather than a class, so a slot is written through the array without a
    //dereference and the whole set stays in one allocation made at startup
    private struct Scope
    {
        public string Name;
        public int Depth;

        public long StartedAt;
        public long ThisFrame;
        public long LastFrame;
        public long Total;
    }

    private static readonly Scope[] scopes = new Scope[MaxScopes];

    private static int count;
    private static long frames;

    public static int Count => count;

    /// <summary>Frames measured since the last <see cref="Reset"/>.</summary>
    public static long Frames => frames;

    public static string NameOf(int slot) => scopes[slot].Name;

    /// <summary>Zero is a scope that is not inside another one.</summary>
    public static int DepthOf(int slot) => scopes[slot].Depth;

    public static long BytesLastFrame(int slot) => scopes[slot].LastFrame;

    //allocation is bursty, so a single frame says little; a scope is only clean if its mean is zero
    public static long BytesPerFrame(int slot) => frames == 0 ? 0 : scopes[slot].Total / frames;

    public static void Reset()
    {
        for (int i = 0; i < count; i++)
        {
            scopes[i].Total = 0;
        }

        frames = 0;
    }

    /// <summary>
    /// Claims a slot for <paramref name="name"/>. Meant for a static readonly field, so the cost is paid
    /// once and the caller holds an index rather than looking a name up every frame.
    /// </summary>
    /// <param name="depth">
    /// How far inside the scope registered before it this one sits. Registration order is display order,
    /// so a parent has to claim its slot before its children.
    /// </param>
    public static int Register(string name, int depth = 0)
    {
        if (count >= MaxScopes)
        {
            return MaxScopes - 1;
        }

        scopes[count].Name = name;
        scopes[count].Depth = depth;
        return count++;
    }

    /// <summary>Follows <see cref="FrameProfiler.Enabled"/>, which sets it at each frame boundary.</summary>
    public static bool Enabled;

    public static void Begin(int slot)
    {
        if (Enabled)
        {
            scopes[slot].StartedAt = GC.GetAllocatedBytesForCurrentThread();
        }
    }

    /// <summary>Several begin and end pairs of the same slot within a frame accumulate.</summary>
    public static void End(int slot)
    {
        if (Enabled)
        {
            scopes[slot].ThisFrame += GC.GetAllocatedBytesForCurrentThread() - scopes[slot].StartedAt;
        }
    }

    internal static void NewFrame()
    {
        for (int i = 0; i < count; i++)
        {
            ref Scope scope = ref scopes[i];

            scope.LastFrame = scope.ThisFrame;
            scope.Total += scope.ThisFrame;
            scope.ThisFrame = 0;
        }

        frames++;
    }
}
