using System.Runtime.InteropServices;
using FFmpeg.AutoGen.Abstractions;

namespace DTXMania.Core.Video.Decoders;

public unsafe class SoftwareVideoDecoder : VideoDecoder
{
    private AVFormatContext* formatContext;
    private AVCodecContext* codecContext;
    private AVFrame* frame;
    private AVFrame* rgbaFrame;
    private AVPacket* packet;
    private SwsContext* swsContext;
    private int videoStreamIndex = -1;

    private double fallbackFrameDurationSeconds = 1.0 / 30.0;
    private double timelineOriginPtsSeconds = double.NaN;
    private double lastDecodedPtsSeconds = -1;

    private double seekTargetMuteSeconds = double.NaN; // Bypass sws_scale until reached

    private bool endOfStream;

    public override int Width => codecContext != null ? codecContext->width : 0;
    public override int Height => codecContext != null ? codecContext->height : 0;

    public override bool IsEndOfStream => endOfStream;
    public override string Name { get; } = "SoftwareDecoder";

    public override double DurationSeconds
    {
        get
        {
            if (formatContext == null || videoStreamIndex < 0) return 0;
            var stream = formatContext->streams[videoStreamIndex];
            if (stream != null && stream->duration > 0)
            {
                return stream->duration * ffmpeg.av_q2d(stream->time_base);
            }
            if (formatContext->duration > 0)
            {
                return formatContext->duration / (double)ffmpeg.AV_TIME_BASE;
            }
            return 0;
        }
    }

    public override long TotalFrames
    {
        get
        {
            if (formatContext == null || videoStreamIndex < 0) return 0;
            var stream = formatContext->streams[videoStreamIndex];
            if (stream != null && stream->nb_frames > 0) return stream->nb_frames;
            if (DurationSeconds > 0 && FrameRate > 0) return (long)(DurationSeconds * FrameRate + 0.5);
            return 0;
        }
    }

    public override double FrameRate
    {
        get
        {
            if (formatContext == null || videoStreamIndex < 0) return 0;
            var stream = formatContext->streams[videoStreamIndex];
            if (stream != null)
            {
                AVRational fr = stream->avg_frame_rate;
                if (fr.num <= 0 || fr.den <= 0) fr = stream->r_frame_rate;
                if (fr.num > 0 && fr.den > 0) return (double)fr.num / fr.den;
            }
            return 0;
        }
    }

    public override bool TryOpen(string path)
    {
        AVFormatContext* pFormatContext = ffmpeg.avformat_alloc_context();
        if (ffmpeg.avformat_open_input(&pFormatContext, path, null, null) != 0) return false;
        formatContext = pFormatContext;

        if (ffmpeg.avformat_find_stream_info(formatContext, null) != 0) return false;

        AVCodec* codec = null;
        videoStreamIndex = ffmpeg.av_find_best_stream(formatContext, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, &codec, 0);
        if (videoStreamIndex < 0) return false;

        codecContext = ffmpeg.avcodec_alloc_context3(codec);
        ffmpeg.avcodec_parameters_to_context(codecContext, formatContext->streams[videoStreamIndex]->codecpar);

        if (ffmpeg.avcodec_open2(codecContext, codec, null) != 0) return false;

        frame = ffmpeg.av_frame_alloc();
        rgbaFrame = ffmpeg.av_frame_alloc();
        packet = ffmpeg.av_packet_alloc();

        rgbaFrame->format = (int)AVPixelFormat.AV_PIX_FMT_RGBA;
        rgbaFrame->width = codecContext->width;
        rgbaFrame->height = codecContext->height;
        ffmpeg.av_frame_get_buffer(rgbaFrame, 0);

        swsContext = ffmpeg.sws_getContext(codecContext->width, codecContext->height, codecContext->pix_fmt,
                                           codecContext->width, codecContext->height, AVPixelFormat.AV_PIX_FMT_RGBA,
                                           ffmpeg.SWS_BILINEAR, null, null, null);

        var stream = formatContext->streams[videoStreamIndex];
        if (stream != null)
        {
            AVRational fr = stream->avg_frame_rate;
            if (fr.num <= 0 || fr.den <= 0) fr = stream->r_frame_rate;
            if (fr.num > 0 && fr.den > 0) fallbackFrameDurationSeconds = (double)fr.den / fr.num;
        }

        // Initialize lastDecodedPtsSeconds so it properly starts at 0 without a timeline origin regression
        timelineOriginPtsSeconds = double.NaN;
        lastDecodedPtsSeconds = -1;
        endOfStream = false;

        return true;
    }

    public override void SeekTo(double targetSeconds)
    {
        if (formatContext == null || codecContext == null || videoStreamIndex < 0) return;
        var stream = formatContext->streams[videoStreamIndex];
        if (stream == null) return;

        long targetPts = 0;
        if (stream->time_base.num != 0 && stream->time_base.den != 0)
        {
            double ticksInStreamBase = targetSeconds * stream->time_base.den / stream->time_base.num;
            long streamOffset = (long)Math.Round(ticksInStreamBase);
            long streamStart = stream->start_time != ffmpeg.AV_NOPTS_VALUE ? stream->start_time : 0;
            targetPts = streamStart + streamOffset;
        }

        int seekResult = ffmpeg.av_seek_frame(formatContext, videoStreamIndex, targetPts, ffmpeg.AVSEEK_FLAG_BACKWARD);
        if (seekResult < 0)
        {
            ffmpeg.avformat_seek_file(formatContext, videoStreamIndex, long.MinValue, targetPts, long.MaxValue, ffmpeg.AVSEEK_FLAG_BACKWARD);
        }
        
        ffmpeg.avcodec_flush_buffers(codecContext);

        seekTargetMuteSeconds = Math.Max(0, targetSeconds);
        lastDecodedPtsSeconds = -1; // Reset monotonic clamping tracker on seek
        endOfStream = false;
    }

    public override bool TryGetDecodedFrame(out DecodedFrameData data)
    {
        if (packet == null)
        {
            data = default;
            return false;
        }

        while (true)
        {
            int readResult = ffmpeg.av_read_frame(formatContext, packet);
            if (readResult < 0) 
            {
                endOfStream = true;
                data = default;
                return false; // EOF or error
            }

            if (packet->stream_index != videoStreamIndex)
            {
                ffmpeg.av_packet_unref(packet);
                continue;
            }

            int send = ffmpeg.avcodec_send_packet(codecContext, packet);
            ffmpeg.av_packet_unref(packet);
            if (send < 0) continue;

            int receive = ffmpeg.avcodec_receive_frame(codecContext, frame);
            if (receive != 0) continue;

            double currentPts = GetCurrentPtsSeconds();

            // Sws bypass for fast catching-up after seeking
            if (!double.IsNaN(seekTargetMuteSeconds))
            {
                if (currentPts < seekTargetMuteSeconds - 0.001)
                {
                    continue; // Skip image rendering natively within FFmpeg pipeline pumping
                }
                else
                {
                    seekTargetMuteSeconds = double.NaN;
                }
            }

            if (ffmpeg.av_frame_make_writable(rgbaFrame) < 0)
            {
                data = default;
                return false;
            }

            ffmpeg.sws_scale(swsContext, frame->data, frame->linesize, 0, codecContext->height, rgbaFrame->data, rgbaFrame->linesize);

            int packedSize = codecContext->width * codecContext->height * 4;
            byte[] rgbaData = new byte[packedSize];
            
            int stride = rgbaFrame->linesize[0];
            int rowBytes = codecContext->width * 4;
            IntPtr src = (IntPtr)rgbaFrame->data[0];

            if (stride == rowBytes)
            {
                Marshal.Copy(src, rgbaData, 0, packedSize);
            }
            else
            {
                for (int y = 0; y < codecContext->height; y++)
                {
                    IntPtr srcRow = src + (y * stride);
                    Marshal.Copy(srcRow, rgbaData, y * rowBytes, rowBytes);
                }
            }

            long frameNum = FrameRate > 0 ? (long)(currentPts * FrameRate + 0.5) : 0;
            data = new DecodedFrameData(rgbaData, currentPts, frameNum);
            return true;
        }
    }

    public override bool GetNextFrameBlocking(out DecodedFrameData data)
    {
        // For the synchronous software implementation, blocking is identical to TryGet.
        // In a threaded version, this would wait on an execution queue indefinitely.
        const int maxSeekAttempts = 1000;
        for (int i = 0; i < maxSeekAttempts; i++)
        {
            if (TryGetDecodedFrame(out data)) return true;
        }
        
        data = default;
        return false;
    }

    private double GetCurrentPtsSeconds()
    {
        var stream = formatContext->streams[videoStreamIndex];
        long pts = frame->best_effort_timestamp;
        
        if (stream != null && pts != ffmpeg.AV_NOPTS_VALUE && stream->time_base.num != 0 && stream->time_base.den != 0)
        {
            double rawPtsSeconds = pts * ffmpeg.av_q2d(stream->time_base);
            
            if (double.IsNaN(timelineOriginPtsSeconds))
            {
                timelineOriginPtsSeconds = rawPtsSeconds;
            }

            double relative = Math.Max(0, rawPtsSeconds - timelineOriginPtsSeconds);

            // Monotonic forcing 
            if (lastDecodedPtsSeconds >= 0 && relative < lastDecodedPtsSeconds)
            {
                relative = lastDecodedPtsSeconds;
            }
            
            lastDecodedPtsSeconds = relative;
            return relative;
        }
        
        if (lastDecodedPtsSeconds < 0) 
        {
            lastDecodedPtsSeconds = 0;
        }
        
        lastDecodedPtsSeconds += fallbackFrameDurationSeconds;
        return lastDecodedPtsSeconds;
    }

    public override void Dispose()
    {
        if (frame != null) { AVFrame* f = frame; ffmpeg.av_frame_free(&f); frame = null; }
        if (rgbaFrame != null) { AVFrame* rf = rgbaFrame; ffmpeg.av_frame_free(&rf); rgbaFrame = null; }
        if (packet != null) { AVPacket* p = packet; ffmpeg.av_packet_free(&p); packet = null; }
        if (swsContext != null) { ffmpeg.sws_freeContext(swsContext); swsContext = null; }
        if (codecContext != null) { AVCodecContext* cc = codecContext; ffmpeg.avcodec_free_context(&cc); codecContext = null; }
        if (formatContext != null) { AVFormatContext* fc = formatContext; ffmpeg.avformat_close_input(&fc); formatContext = null; }
        GC.SuppressFinalize(this);
    }
}
