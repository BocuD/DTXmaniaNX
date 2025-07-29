using System.Diagnostics;
using FFmpeg.AutoGen.Abstractions;
using SharpDX.Direct3D9;

namespace DTXMania.Core.Video;

public abstract unsafe class FFmpegVideoPlayer : IDisposable
{
    protected AVFormatContext* formatContext;
    protected AVCodecContext* codecContext;
    protected AVFrame* frame;
    protected int videoStreamIndex;
    
    public int Width => codecContext->width;
    public int Height => codecContext->height;

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
        return true;
    }

    protected abstract bool CreateResources();
    public abstract Texture GetUpdatedTexture();

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
        
        GC.SuppressFinalize(this);
    }
}