namespace DTXMania.Core.Video.Decoders;

internal sealed class FrameBufferPool
{
    private readonly object sync = new();
    private readonly Stack<byte[]> free = new();
    private readonly int maxRetained;
    private int bufferSize = -1;

    public FrameBufferPool(int maxRetained = 6)
    {
        this.maxRetained = maxRetained;
    }

    /// <summary>Gets a buffer of exactly <paramref name="size"/> bytes, reusing a pooled one when possible.</summary>
    public byte[] Rent(int size)
    {
        lock (sync)
        {
            if (size != bufferSize)
            {
                // Frame dimensions changed (or first use): pooled buffers are the wrong
                // size. Drop them so they get collected once, then size to the new frame.
                free.Clear();
                bufferSize = size;
            }
            else if (free.Count > 0)
            {
                return free.Pop();
            }
        }

        return new byte[size];
    }

    /// <summary>Returns a buffer for reuse once its contents have been consumed (e.g. uploaded to the GPU).</summary>
    public void Return(byte[]? buffer)
    {
        if (buffer == null) return;

        lock (sync)
        {
            // Ignore stale-sized buffers (from before a resolution change) and cap retention.
            if (buffer.Length == bufferSize && free.Count < maxRetained)
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
            bufferSize = -1;
        }
    }
}
