using System.Diagnostics;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen.Abstractions;
using DTXMania.UI.Drawable;

namespace DTXMania.Core.Video;

public unsafe class SoftwareVideoPlayer : FFmpegVideoPlayer
{
    private SwsContext* swsContext;
    private AVFrame* rgbaFrame;
    private AVPacket* packet;
    private double fallbackFrameDurationSeconds = 1.0 / 30.0;
    private double timelineOriginPtsSeconds = double.NaN;
    private double lastDecodedPtsSeconds = double.NaN;
    private byte[] stagedFrameBuffer = [];
    private int stagedFrameLength;

    private BaseTexture texture = BaseTexture.None;

    protected override int MaxFramesToDecodePerUpdate => 1;

    public SoftwareVideoPlayer(bool loopOnEof = true)
    {
        LoopOnEof = loopOnEof;
    }

    protected override bool CreateResources()
    {
        rgbaFrame = ffmpeg.av_frame_alloc();
        if (rgbaFrame == null)
        {
            Trace.TraceError("Failed to allocate RGBA frame.");
            return false;
        }

        packet = ffmpeg.av_packet_alloc();
        if (packet == null)
        {
            Trace.TraceError("Failed to allocate packet for video decoding.");
            return false;
        }

        rgbaFrame->format = (int)AVPixelFormat.AV_PIX_FMT_RGBA;
        rgbaFrame->width = codecContext->width;
        rgbaFrame->height = codecContext->height;

        if (ffmpeg.av_frame_get_buffer(rgbaFrame, 0) < 0)
        {
            Trace.TraceError("Failed to allocate frame buffer for RGBA frame.");
            return false;
        }

        swsContext = ffmpeg.sws_getContext(
            codecContext->width,
            codecContext->height,
            codecContext->pix_fmt,
            codecContext->width,
            codecContext->height,
            AVPixelFormat.AV_PIX_FMT_RGBA,
            ffmpeg.SWS_BILINEAR,
            null, null, null
        );

        if (swsContext == null)
        {
            Trace.TraceError("Failed to create swsContext for video conversion.");
            return false;
        }

        texture = BaseTexture.CreateEmpty(codecContext->width, codecContext->height, "VideoFrameTexture");
        if (!texture.isValid())
        {
            Trace.TraceError("Failed to create BaseTexture for video playback.");
            return false;
        }

        stagedFrameBuffer = [];
        stagedFrameLength = 0;
        timelineOriginPtsSeconds = double.NaN;
        lastDecodedPtsSeconds = double.NaN;

        AVStream* videoStream = formatContext->streams[videoStreamIndex];
        if (videoStream != null)
        {
            AVRational frameRate = videoStream->avg_frame_rate;
            if (frameRate.num <= 0 || frameRate.den <= 0)
            {
                frameRate = videoStream->r_frame_rate;
            }

            if (frameRate.num > 0 && frameRate.den > 0)
            {
                fallbackFrameDurationSeconds = (double)frameRate.den / frameRate.num;
            }
        }

        return true;
    }

    protected override BaseTexture OutputTexture => texture;

    protected override bool TryDecodeAndStageFrame(out double framePtsSeconds, out bool reachedEndOfStream)
    {
        if (packet == null)
        {
            reachedEndOfStream = false;
            framePtsSeconds = 0;
            return false;
        }

        reachedEndOfStream = false;

        while (true)
        {
            int readResult = ffmpeg.av_read_frame(formatContext, packet);
            if (readResult < 0)
            {
                reachedEndOfStream = true;
                framePtsSeconds = 0;
                return false;
            }

            if (packet->stream_index != videoStreamIndex)
            {
                ffmpeg.av_packet_unref(packet);
                continue;
            }

            int send = ffmpeg.avcodec_send_packet(codecContext, packet);
            ffmpeg.av_packet_unref(packet);
            if (send < 0)
            {
                continue;
            }

            int receive = ffmpeg.avcodec_receive_frame(codecContext, frame);
            if (receive != 0)
            {
                continue;
            }

            if (ffmpeg.av_frame_make_writable(rgbaFrame) < 0)
            {
                framePtsSeconds = 0;
                return false;
            }

            ffmpeg.sws_scale(
                swsContext,
                frame->data,
                frame->linesize,
                0,
                codecContext->height,
                rgbaFrame->data,
                rgbaFrame->linesize
            );

            int width = codecContext->width;
            int height = codecContext->height;
            int rowBytes = width * 4;
            int stride = rgbaFrame->linesize[0];
            int packedSize = rowBytes * height;

            if (stagedFrameBuffer.Length != packedSize)
            {
                stagedFrameBuffer = new byte[packedSize];
            }

            IntPtr src = (IntPtr)rgbaFrame->data[0];
            if (stride == rowBytes)
            {
                Marshal.Copy(src, stagedFrameBuffer, 0, packedSize);
            }
            else
            {
                for (int y = 0; y < height; y++)
                {
                    IntPtr srcRow = src + (y * stride);
                    Marshal.Copy(srcRow, stagedFrameBuffer, y * rowBytes, rowBytes);
                }
            }

            framePtsSeconds = ResolveFrameTimestampSeconds();
            stagedFrameLength = packedSize;
            return true;
        }
    }

    protected override void PresentStagedFrame()
    {
        if (stagedFrameLength <= 0 || !texture.isValid())
        {
            return;
        }

        texture.UpdateRgba32(stagedFrameBuffer, codecContext->width, codecContext->height);
    }

    private double ResolveFrameTimestampSeconds()
    {
        AVStream* videoStream = formatContext->streams[videoStreamIndex];
        long bestEffortTimestamp = frame->best_effort_timestamp;

        if (videoStream != null && bestEffortTimestamp != ffmpeg.AV_NOPTS_VALUE)
        {
            double rawPtsSeconds = StreamTimestampToSeconds(videoStream, bestEffortTimestamp);
            if (!double.IsNaN(rawPtsSeconds) && !double.IsInfinity(rawPtsSeconds))
            {
                if (double.IsNaN(timelineOriginPtsSeconds))
                {
                    timelineOriginPtsSeconds = rawPtsSeconds;
                }

                double relativePtsSeconds = Math.Max(0, rawPtsSeconds - timelineOriginPtsSeconds);
                if (!double.IsNaN(lastDecodedPtsSeconds) && relativePtsSeconds < lastDecodedPtsSeconds)
                {
                    // Avoid running too fast on jittery/non-monotonic streams.
                    relativePtsSeconds = lastDecodedPtsSeconds;
                }

                lastDecodedPtsSeconds = relativePtsSeconds;
                return relativePtsSeconds;
            }
        }

        if (double.IsNaN(lastDecodedPtsSeconds))
        {
            lastDecodedPtsSeconds = 0;
            return 0;
        }

        lastDecodedPtsSeconds += fallbackFrameDurationSeconds;
        return lastDecodedPtsSeconds;
    }

    protected override void OnPlaybackReset()
    {
        stagedFrameLength = 0;
        timelineOriginPtsSeconds = double.NaN;
        lastDecodedPtsSeconds = double.NaN;
    }

    public override void Dispose()
    {
        if (rgbaFrame != null)
        {
            AVFrame* tmp = rgbaFrame;
            ffmpeg.av_frame_free(&tmp);
            rgbaFrame = null;
        }

        if (packet != null)
        {
            AVPacket* tmp = packet;
            ffmpeg.av_packet_free(&tmp);
            packet = null;
        }

        if (swsContext != null)
        {
            ffmpeg.sws_freeContext(swsContext);
            swsContext = null;
        }

        texture.Dispose();
        texture = BaseTexture.None;
        stagedFrameBuffer = [];
        stagedFrameLength = 0;

        base.Dispose();
    }
}