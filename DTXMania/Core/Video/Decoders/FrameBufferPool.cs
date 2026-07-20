namespace DTXMania.Core.Video.Decoders;

internal sealed class FrameBufferPool
{
    // swscale (and other native pixel writers) can write a handful of bytes past the final
    // scanline when the row stride isn't SIMD-aligned: the vectorized store for the last row
    // rounds up beyond width*height*4 and clobbers whatever follows it on the managed heap. That
    // corruption typically isn't noticed until an unrelated allocation trips over the damaged heap
    // metadata and throws System.ExecutionEngineException
    // Intermediate rows self-heal (their overshoot is overwritten by the next row), so only the
    // last row can escape; over-allocate every buffer by this margin so that overshoot lands in
    // slack we own. 512 bytes comfortably covers a full SIMD block (<=32px * 4 bytes) of overshoot
    // with plenty of room to spare. Consumers only ever read the logical width*height*4 bytes.
    private const int NativeWritePadding = 512;

    private readonly object sync = new();
    private readonly Stack<byte[]> free = new();
    private readonly int maxRetained;
    private int logicalSize = -1;

    public FrameBufferPool(int maxRetained = 8)
    {
        this.maxRetained = maxRetained;
    }

    /// <summary>
    /// Gets a buffer with at least <paramref name="size"/> usable bytes (plus internal padding),
    /// reusing a pooled one when possible. The extra padding is never part of the logical frame;
    /// it only absorbs out-of-bounds writes from native pixel converters (see the note above).
    /// </summary>
    public byte[] Rent(int size)
    {
        lock (sync)
        {
            if (size != logicalSize)
            {
                // Frame dimensions changed (or first use): pooled buffers are the wrong
                // size. Drop them so they get collected once, then size to the new frame.
                free.Clear();
                logicalSize = size;
            }
            else if (free.Count > 0)
            {
                return free.Pop();
            }
        }

        return new byte[size + NativeWritePadding];
    }

    /// <summary>Returns a buffer for reuse once its contents have been consumed (e.g. uploaded to the GPU).</summary>
    public void Return(byte[]? buffer)
    {
        if (buffer == null) return;

        lock (sync)
        {
            // Ignore stale-sized buffers (from before a resolution change) and cap retention.
            if (logicalSize >= 0 && buffer.Length == logicalSize + NativeWritePadding && free.Count < maxRetained)
            {
                free.Push(buffer);
            }
        }
    }

    public void Clear()
    {
        lock (sync)
        {
            free.Clear();
            logicalSize = -1;
        }
    }
}
