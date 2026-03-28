using System.Diagnostics;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen.Abstractions;
using DTXMania.UI.Drawable;

namespace DTXMania.Core.Video;

public unsafe class SoftwareVideoPlayer : FFmpegVideoPlayer
{
    private SwsContext* swsContext;
    private AVFrame* rgbaFrame;
    private bool hasLoopedAfterEof;

    private BaseTexture texture = BaseTexture.None;
    private byte[] uploadBuffer = [];

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

        uploadBuffer = new byte[codecContext->width * codecContext->height * 4];
        return true;
    }

    public override BaseTexture GetUpdatedTexture()
    {
        if (!texture.isValid())
        {
            return BaseTexture.None;
        }

        if (TryDecodeNextFrame(out byte[] frameData))
        {
            texture.UpdateRgba32(frameData, codecContext->width, codecContext->height);
        }

        return texture;
    }

    public bool TryDecodeNextFrame(out byte[] rgbaFrameData)
    {
        AVPacket* packet = ffmpeg.av_packet_alloc();
        if (packet == null)
        {
            rgbaFrameData = [];
            return false;
        }

        try
        {
            while (true)
            {
                int readResult = ffmpeg.av_read_frame(formatContext, packet);
                if (readResult < 0)
                {
                    if (LoopOnEof && !hasLoopedAfterEof && TryRestartFromBeginning())
                    {
                        hasLoopedAfterEof = true;
                        continue;
                    }

                    rgbaFrameData = [];
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
                    rgbaFrameData = [];
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

                if (uploadBuffer.Length != packedSize)
                {
                    uploadBuffer = new byte[packedSize];
                }

                IntPtr src = (IntPtr)rgbaFrame->data[0];
                if (stride == rowBytes)
                {
                    Marshal.Copy(src, uploadBuffer, 0, packedSize);
                }
                else
                {
                    for (int y = 0; y < height; y++)
                    {
                        IntPtr srcRow = src + (y * stride);
                        Marshal.Copy(srcRow, uploadBuffer, y * rowBytes, rowBytes);
                    }
                }

                rgbaFrameData = uploadBuffer;
                hasLoopedAfterEof = false;
                return true;
            }
        }
        finally
        {
            ffmpeg.av_packet_free(&packet);
        }
    }

    private bool TryRestartFromBeginning()
    {
        ffmpeg.avcodec_flush_buffers(codecContext);

        long seekTarget = 0;
        AVStream* videoStream = formatContext->streams[videoStreamIndex];
        if (videoStream != null && videoStream->start_time != ffmpeg.AV_NOPTS_VALUE)
        {
            seekTarget = videoStream->start_time;
        }

        int seekResult = ffmpeg.av_seek_frame(formatContext, videoStreamIndex, seekTarget, ffmpeg.AVSEEK_FLAG_BACKWARD);
        if (seekResult < 0)
        {
            seekResult = ffmpeg.avformat_seek_file(formatContext, videoStreamIndex, long.MinValue, seekTarget, long.MaxValue, ffmpeg.AVSEEK_FLAG_BACKWARD);
        }

        if (seekResult < 0)
        {
            Trace.TraceError($"Failed to loop video playback: {FFmpegCore.AV_StrError(seekResult)}");
            return false;
        }

        ffmpeg.avcodec_flush_buffers(codecContext);
        return true;
    }

    public override void Dispose()
    {
        if (rgbaFrame != null)
        {
            AVFrame* tmp = rgbaFrame;
            ffmpeg.av_frame_free(&tmp);
            rgbaFrame = null;
        }

        if (swsContext != null)
        {
            ffmpeg.sws_freeContext(swsContext);
            swsContext = null;
        }

        texture.Dispose();
        texture = BaseTexture.None;

        base.Dispose();
    }
}