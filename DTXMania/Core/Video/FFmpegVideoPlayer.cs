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

    // Controls whether playback restarts automatically when EOF is reached.
    public bool LoopOnEof { get; set; } = true;

    private readonly Stopwatch playbackClock = new();
    private bool playbackClockStarted;
    private double playbackStartSeconds;
    private bool hasQueuedFrame;
    private double queuedFramePtsSeconds = double.NaN;

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

    public virtual TimeSpan CurrentTime => TimeSpan.FromSeconds(playbackStartSeconds + playbackClock.Elapsed.TotalSeconds);

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
        ResetPlaybackState(Math.Max(0, timestamp.TotalSeconds));
        return true;
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

    public bool Open(string path)
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

        if (!CreateResources())
        {
            Trace.TraceError($"Loading video file ({path}) failed: Couldn't create resources for player");
            return false;
        }

        ResetPlaybackState(0);
        return true;
    }

    protected abstract bool CreateResources();
    protected abstract BaseTexture OutputTexture { get; }
    protected abstract bool TryDecodeAndStageFrame(out double framePtsSeconds, out bool reachedEndOfStream);
    protected abstract void PresentStagedFrame();

    protected virtual void OnPlaybackReset()
    {
    }

    public virtual BaseTexture GetUpdatedTexture()
    {
        StartPlaybackClockIfNeeded();

        BaseTexture outputTexture = OutputTexture;
        if (!outputTexture.IsValid())
        {
            return BaseTexture.None;
        }

        double playbackTimeSeconds = playbackClock.Elapsed.TotalSeconds;

        if (hasQueuedFrame)
        {
            if (playbackTimeSeconds >= queuedFramePtsSeconds)
            {
                PresentStagedFrame();
                hasQueuedFrame = false;
                queuedFramePtsSeconds = double.NaN;
            }

            return outputTexture;
        }

        for (int decodedFrames = 0; decodedFrames < MaxFramesToDecodePerUpdate; decodedFrames++)
        {
            bool reachedEndOfStream;
            if (!TryDecodeAndStageFrame(out double framePtsSeconds, out reachedEndOfStream))
            {
                if (reachedEndOfStream)
                {
                    HandleEndOfStream();
                }

                break;
            }

            if (framePtsSeconds <= playbackTimeSeconds)
            {
                PresentStagedFrame();
                continue;
            }

            hasQueuedFrame = true;
            queuedFramePtsSeconds = framePtsSeconds;
            break;
        }

        return outputTexture;
    }

    private void HandleEndOfStream()
    {
        if (!LoopOnEof)
        {
            return;
        }

        Seek(TimeSpan.Zero);
    }

    private void ResetPlaybackState(double startSeconds)
    {
        playbackStartSeconds = Math.Max(0, startSeconds);
        hasQueuedFrame = false;
        queuedFramePtsSeconds = double.NaN;
        playbackClock.Reset();
        playbackClockStarted = false;
        OnPlaybackReset();
    }

    private void StartPlaybackClockIfNeeded()
    {
        if (playbackClockStarted)
        {
            return;
        }

        playbackClock.Start();
        playbackClockStarted = true;
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
        playbackClockStarted = false;
        
        GC.SuppressFinalize(this);
    }
}