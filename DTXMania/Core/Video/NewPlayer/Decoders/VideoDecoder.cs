namespace DTXMania.Core.Video.NewPlayer.Decoders;

public abstract class VideoDecoder : IDisposable
{
    public abstract int Width { get; }
    public abstract int Height { get; }
    public abstract double DurationSeconds { get; }
    public abstract long TotalFrames { get; }
    public abstract double FrameRate { get; }

    public abstract bool TryOpen(string path);
    
    /// <summary>
    /// Flushes buffers and demuxes to the specified time.
    /// </summary>
    public abstract void SeekTo(double targetSeconds);
    
    /// <summary>
    /// Non-blocking frame retrieval for active playback.
    /// </summary>
    public abstract bool TryGetDecodedFrame(out DecodedFrameData data);
    
    /// <summary>
    /// Synchronously decodes until a frame is produced. Extensively used for seeking.
    /// </summary>
    public abstract bool GetNextFrameBlocking(out DecodedFrameData data);
    
    public abstract void Dispose();
}
