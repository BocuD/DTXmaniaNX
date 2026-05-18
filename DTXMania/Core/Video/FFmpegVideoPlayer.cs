using System.Diagnostics;
using System.Reflection;
using FFmpeg.AutoGen.Abstractions;
using DTXMania.UI.Drawable;

namespace DTXMania.Core.Video;

public abstract unsafe class FFmpegVideoPlayer : IDisposable
{
    public sealed class BackendDescriptor
    {
        public required Type Type { get; init; }
        public required string Name { get; init; }
        public required Func<bool, FFmpegVideoPlayer> Create { get; init; }
    }

    private static IReadOnlyList<BackendDescriptor>? cachedBackends;

    protected AVFormatContext* formatContext;
    protected AVCodecContext* codecContext;
    protected AVFrame* frame;
    protected int videoStreamIndex;
    
    public int Width => codecContext->width;
    public int Height => codecContext->height;

    public long TotalFrameCount
    {
        get
        {
            if (formatContext == null || videoStreamIndex < 0)
            {
                return 0;
            }

            AVStream* videoStream = formatContext->streams[videoStreamIndex];
            if (videoStream != null && videoStream->nb_frames > 0)
            {
                return videoStream->nb_frames;
            }

            // Fallback: calculate from duration and frame rate
            if (videoStream != null && videoStream->duration > 0)
            {
                AVRational frameRate = videoStream->avg_frame_rate;
                if (frameRate.num <= 0 || frameRate.den <= 0)
                {
                    frameRate = videoStream->r_frame_rate;
                }

                if (frameRate.num > 0 && frameRate.den > 0)
                {
                    double durationSeconds = videoStream->duration * ffmpeg.av_q2d(videoStream->time_base);
                    double fps = (double)frameRate.num / frameRate.den;
                    return (long)(durationSeconds * fps + 0.5);
                }
            }

            return 0;
        }
    }

    // Controls whether playback restarts automatically when EOF is reached.
    public bool LoopOnEof { get; set; } = true;

    /// <summary>
    /// The single source of truth for playback timing.
    /// When not paused, this clock advances to drive video playback.
    /// When paused, the clock is stopped at the current position.
    /// </summary>
    private readonly Stopwatch playbackClock = new();
    
    /// <summary>
    /// The playback time offset. Combined with playbackClock.Elapsed to get actual playback time.
    /// When seeking, this is set to the target time and the clock is reset.
    /// </summary>
    private double playbackTimeOffset;
    
    private bool hasQueuedFrame;
    private double queuedFramePtsSeconds = double.NaN;
    private bool isPaused;

    protected virtual int MaxFramesToDecodePerUpdate => 2;

    public virtual string BackendName => GetType().Name;

    public static IReadOnlyList<BackendDescriptor> GetAvailableBackends()
    {
        if (cachedBackends != null)
        {
            return cachedBackends;
        }

        Type baseType = typeof(FFmpegVideoPlayer);
        Type[] types = baseType.Assembly.GetTypes();
        List<BackendDescriptor> backends = [];

        foreach (Type type in types)
        {
            if (!baseType.IsAssignableFrom(type) || type.IsAbstract)
            {
                continue;
            }

            Func<bool, FFmpegVideoPlayer>? factory = TryBuildBackendFactory(type);
            if (factory == null)
            {
                continue;
            }

            string backendName = type.Name;
            backends.Add(new BackendDescriptor
            {
                Type = type,
                Name = backendName,
                Create = factory
            });
        }

        cachedBackends = backends
            .OrderBy(b => b.Name, StringComparer.Ordinal)
            .ToArray();

        return cachedBackends;
    }

    private static Func<bool, FFmpegVideoPlayer>? TryBuildBackendFactory(Type backendType)
    {
        ConstructorInfo? boolCtor = backendType.GetConstructor([typeof(bool)]);
        if (boolCtor != null)
        {
            return loopOnEof => (FFmpegVideoPlayer)boolCtor.Invoke([loopOnEof]);
        }

        ConstructorInfo? defaultCtor = backendType.GetConstructor(Type.EmptyTypes);
        if (defaultCtor != null)
        {
            return _ => (FFmpegVideoPlayer)defaultCtor.Invoke(null);
        }

        return null;
    }

    public virtual TimeSpan CurrentTime
    {
        get => TimeSpan.FromSeconds(playbackTimeOffset + playbackClock.Elapsed.TotalSeconds);
    }

    public virtual bool IsPaused => isPaused;

    public TimeSpan Duration
    {
        get
        {
            if (formatContext == null || videoStreamIndex < 0)
            {
                return TimeSpan.Zero;
            }

            AVStream* videoStream = formatContext->streams[videoStreamIndex];
            if (videoStream != null && videoStream->duration > 0)
            {
                double streamSeconds = videoStream->duration * ffmpeg.av_q2d(videoStream->time_base);
                if (streamSeconds > 0)
                {
                    return TimeSpan.FromSeconds(streamSeconds);
                }
            }

            if (formatContext->duration > 0)
            {
                double containerSeconds = formatContext->duration / (double)ffmpeg.AV_TIME_BASE;
                if (containerSeconds > 0)
                {
                    return TimeSpan.FromSeconds(containerSeconds);
                }
            }

            return TimeSpan.Zero;
        }
    }

    public virtual bool Seek(TimeSpan timestamp)
    {
        if (formatContext == null || codecContext == null || videoStreamIndex < 0)
        {
            return false;
        }

        AVStream* videoStream = formatContext->streams[videoStreamIndex];
        if (videoStream == null)
        {
            return false;
        }

        long seekTarget = TimeSpanToStreamTimestamp(videoStream, timestamp);
        int seekResult = ffmpeg.av_seek_frame(formatContext, videoStreamIndex, seekTarget, ffmpeg.AVSEEK_FLAG_BACKWARD);
        if (seekResult < 0)
        {
            seekResult = ffmpeg.avformat_seek_file(formatContext, videoStreamIndex, long.MinValue, seekTarget, long.MaxValue, ffmpeg.AVSEEK_FLAG_BACKWARD);
        }

        if (seekResult < 0)
        {
            Trace.TraceError($"Failed to seek video playback: {FFmpegCore.AV_StrError(seekResult)}");
            return false;
        }

        ffmpeg.avcodec_flush_buffers(codecContext);
        
        // Set the clock to the target time
        playbackTimeOffset = Math.Max(0, timestamp.TotalSeconds);
        playbackClock.Restart();
        
        // If currently paused, keep clock stopped; otherwise start it
        if (isPaused)
        {
            playbackClock.Stop();
        }
        
        // Invalidate queued frame so we'll decode the frame at the new position
        hasQueuedFrame = false;
        queuedFramePtsSeconds = double.NaN;
        OnPlaybackReset();
        
        if (isPaused)
        {
            UpdateTextureForSeek();
        }

        return true;
    }

    public virtual void SetPaused(bool paused)
    {
        if (isPaused == paused)
        {
            return;
        }

        isPaused = paused;
        if (paused)
        {
            // Pause: stop the clock
            playbackClock.Stop();
        }
        else
        {
            // Resume: start the clock from where it was
            playbackClock.Start();
        }
    }

    public virtual bool SeekByFrame(long frameNumber)
    {
        if (!TryGetVideoFrameRate(out double fps))
        {
            return false;
        }

        long targetFrame = Math.Max(0, frameNumber);
        long totalFrames = TotalFrameCount;
        if (totalFrames > 0)
        {
            targetFrame = Math.Min(targetFrame, totalFrames - 1);
        }

        double targetSeconds = targetFrame / fps;
        
        // Seek will automatically call UpdateTextureForSeek if paused.
        return Seek(TimeSpan.FromSeconds(targetSeconds));
    }

    public virtual long GetCurrentFrameNumber()
    {
        if (formatContext == null || videoStreamIndex < 0)
        {
            return 0;
        }

        if (!TryGetVideoFrameRate(out double fps))
        {
            return 0;
        }

        long frameNumber = (long)Math.Round(CurrentTime.TotalSeconds * fps, MidpointRounding.AwayFromZero);
        frameNumber = Math.Max(0, frameNumber);

        long totalFrames = TotalFrameCount;
        if (totalFrames > 0)
        {
            frameNumber = Math.Min(frameNumber, totalFrames - 1);
        }

        return frameNumber;
    }

    protected bool TryGetVideoFrameRate(out double fps)
    {
        fps = 0;

        if (formatContext == null || videoStreamIndex < 0)
        {
            return false;
        }

        AVStream* videoStream = formatContext->streams[videoStreamIndex];
        if (videoStream == null)
        {
            return false;
        }

        AVRational frameRate = videoStream->avg_frame_rate;
        if (frameRate.num <= 0 || frameRate.den <= 0)
        {
            frameRate = videoStream->r_frame_rate;
        }

        if (frameRate.num <= 0 || frameRate.den <= 0)
        {
            return false;
        }

        fps = (double)frameRate.num / frameRate.den;
        return fps > 0;
    }

    protected static long TimeSpanToStreamTimestamp(AVStream* videoStream, TimeSpan timestamp)
    {
        double seconds = Math.Max(0, timestamp.TotalSeconds);
        if (videoStream->time_base.num == 0 || videoStream->time_base.den == 0)
        {
            return 0;
        }

        double ticksInStreamBase = seconds * videoStream->time_base.den / videoStream->time_base.num;
        long streamOffset = (long)Math.Round(ticksInStreamBase);
        long streamStart = videoStream->start_time != ffmpeg.AV_NOPTS_VALUE ? videoStream->start_time : 0;
        return streamStart + streamOffset;
    }

    protected static double StreamTimestampToSeconds(AVStream* videoStream, long timestamp)
    {
        if (videoStream->time_base.num == 0 || videoStream->time_base.den == 0)
        {
            return 0;
        }

        return timestamp * ffmpeg.av_q2d(videoStream->time_base);
    }

    public bool Open(string path, bool metadataOnly = false)
    {
        AVFormatContext* pFormatContext = ffmpeg.avformat_alloc_context();

        if (ffmpeg.avformat_open_input(&pFormatContext, path, null, null) != 0)
        {
            Trace.TraceWarning($"Loading video file ({path}) failed: Couldn't open file");
            return false;
        }

        formatContext = pFormatContext;

        if (ffmpeg.avformat_find_stream_info(formatContext, null) != 0)
        {
            Trace.TraceError($"Loading video file ({path}) failed: Couldn't find stream info");
            return false;
        }

        AVCodec* codec = null;
        videoStreamIndex = ffmpeg.av_find_best_stream(formatContext, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, &codec, 0);

        if (videoStreamIndex < 0)
        {
            Trace.TraceError($"Loading video file ({path}) failed: Couldn't find video stream");
            return false;
        }

        codecContext = ffmpeg.avcodec_alloc_context3(codec);
        ffmpeg.avcodec_parameters_to_context(codecContext, formatContext->streams[videoStreamIndex]->codecpar);

        if (ffmpeg.avcodec_open2(codecContext, codec, null) != 0)
        {
            Trace.TraceError($"Loading video file ({path}) failed: Couldn't open codec");
            return false;
        }

        frame = ffmpeg.av_frame_alloc();
        
        if (frame == null)
        {
            Trace.TraceError($"Loading video file ({path}) failed: Couldn't allocate frame");
            return false;
        }

        if (!metadataOnly)
        {
            if (!CreateResources())
            {
                Trace.TraceError($"Loading video file ({path}) failed: Couldn't create resources for player");
                return false;
            }

            ResetPlaybackState(0);
        }

        return true;
    }

    protected abstract bool CreateResources();
    protected abstract BaseTexture OutputTexture { get; }
    protected abstract bool TryDecodeAndStageFrame(out double framePtsSeconds, out bool reachedEndOfStream);
    protected abstract void PresentStagedFrame();

    protected virtual void OnPlaybackReset()
    {
    }

    /// <summary>
    /// Updates the playback state, including clock management and frame decoding.
    /// Should be called once per frame/update cycle.
    /// </summary>
    public virtual void UpdatePlayback()
    {
        BaseTexture outputTexture = OutputTexture;
        if (!outputTexture.IsValid())
        {
            return;
        }

        if (!isPaused && !playbackClock.IsRunning)
        {
            playbackClock.Start();
        }

        // Use CurrentTime.TotalSeconds because the elapsed clock is only a delta
        // from the playbackTimeOffset.
        double playbackTimeSeconds = CurrentTime.TotalSeconds;

        if (hasQueuedFrame)
        {
            if (!isPaused && playbackTimeSeconds >= queuedFramePtsSeconds)
            {
                PresentStagedFrame();
                hasQueuedFrame = false;
                queuedFramePtsSeconds = double.NaN;
            }

            return;
        }

        int maxFramesToDecode = MaxFramesToDecodePerUpdate;

        for (int decodedFrames = 0; decodedFrames < maxFramesToDecode; decodedFrames++)
        {
            if (!TryDecodeAndStageFrame(out double framePtsSeconds, out bool reachedEndOfStream))
            {
                if (reachedEndOfStream)
                {
                    HandleEndOfStream();
                }
                break;
            }

            if (isPaused)
            {
                // We are paused, looking for the target frame after a seek
                if (framePtsSeconds >= playbackTimeSeconds - 0.001)
                {
                    PresentStagedFrame();
                    hasQueuedFrame = true;
                    // Provide a queued PTS in the future exactly at or after so we don't instantly dequeue
                    queuedFramePtsSeconds = framePtsSeconds; 
                    break;
                }
                // Otherwise silently discard past catch-up frames
            }
            else
            {
                if (framePtsSeconds <= playbackTimeSeconds)
                {
                    PresentStagedFrame();
                }
                else
                {
                    hasQueuedFrame = true;
                    queuedFramePtsSeconds = framePtsSeconds;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Forces a single frame update at the current playback position.
    /// Used for updating the texture after seeking, especially when paused.
    /// This does not advance the playback clock or affect pause state.
    /// </summary>
    public virtual void UpdateTextureForSeek()
    {
        BaseTexture outputTexture = OutputTexture;
        if (!outputTexture.IsValid())
        {
            return;
        }

        // Use the full CurrentTime (offset + elapsed) to ensure frame comparisons work after seeks
        double playbackTimeSeconds = CurrentTime.TotalSeconds;

        // Clear any queued frame to force a fresh decode
        hasQueuedFrame = false;
        queuedFramePtsSeconds = double.NaN;

        // Decode until we reach the current time target, which matters after frame seeks
        // because av_seek_frame typically lands on a preceding keyframe.
        const int maxSeekDecodeAttempts = 120;
        for (int i = 0; i < maxSeekDecodeAttempts; i++)
        {
            if (!TryDecodeAndStageFrame(out double framePtsSeconds, out _))
            {
                return;
            }

            if (framePtsSeconds >= playbackTimeSeconds - 0.001)
            {
                PresentStagedFrame();
                hasQueuedFrame = true;
                queuedFramePtsSeconds = framePtsSeconds;
                return;
            }
        }

        PresentStagedFrame();
        hasQueuedFrame = true;
        queuedFramePtsSeconds = playbackTimeSeconds;
    }

    /// <summary>
    /// Gets the current texture without updating playback state.
    /// Call UpdatePlayback() before this to advance frames.
    /// </summary>
    public virtual BaseTexture GetUpdatedTexture()
    {
        return OutputTexture;
    }

    private void HandleEndOfStream()
    {
        if (LoopOnEof)
        {
            Seek(TimeSpan.Zero);
        }
        else
        {
            SetPaused(true);
        }
    }

    private void ResetPlaybackState(double startSeconds)
    {
        hasQueuedFrame = false;
        queuedFramePtsSeconds = double.NaN;
        playbackTimeOffset = Math.Max(0, startSeconds);
        playbackClock.Restart();
        isPaused = false;
        // Clock starts running from the offset position
        OnPlaybackReset();
    }

    public virtual void Dispose()
    {
        if (frame != null)
        {
            AVFrame* tmp = frame;
            ffmpeg.av_frame_free(&tmp);
            frame = null;
        }

        if (codecContext != null)
        {
            AVCodecContext* tmp = codecContext;
            ffmpeg.avcodec_free_context(&tmp);
            codecContext = null;
        }

        if (formatContext != null)
        {
            AVFormatContext* tmp = formatContext;
            ffmpeg.avformat_close_input(&tmp);
            formatContext = null;
        }

        playbackClock.Stop();
        
        GC.SuppressFinalize(this);
    }
}