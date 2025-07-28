using System.Runtime.InteropServices;
using FFmpeg.AutoGen.Abstractions;
using SharpDX.Direct3D9;

namespace DTXMania.Core.Video;

public unsafe class SoftwareVideoPlayer : FFmpegVideoPlayer
{
    private SwsContext* swsContext;
    private AVFrame* bgraFrame;
    
    private Texture texture;

    public override void DeviceCreated()
    {
        texture = new Texture(CDTXMania.app.Device, codecContext->width, codecContext->height, 1,
            Usage.Dynamic, Format.A8R8G8B8, Pool.Default);
    }

    public override void DeviceReset()
    {
        if (texture != null)
        {
            texture.Dispose();
            texture = null;
        }
    }

    public override void CreateResources()
    {
        bgraFrame = ffmpeg.av_frame_alloc();
        
        swsContext = ffmpeg.sws_getContext(
            codecContext->width,
            codecContext->height,
            codecContext->pix_fmt,
            codecContext->width,
            codecContext->height,
            AVPixelFormat.AV_PIX_FMT_BGRA,
            ffmpeg.SWS_BILINEAR,
            null, null, null
        );
        
        //setup bgraFrame
        bgraFrame->format = (int)AVPixelFormat.AV_PIX_FMT_BGRA;
        bgraFrame->width = codecContext->width;
        bgraFrame->height = codecContext->height;
        if (ffmpeg.av_frame_get_buffer(bgraFrame, 0) < 0)
            throw new Exception("Couldn't allocate frame buffer");
        
        // Allocate memory for the BGRA frame data
        int numBytes = ffmpeg.av_image_get_buffer_size(AVPixelFormat.AV_PIX_FMT_BGRA, codecContext->width, codecContext->height, 1);
        byte* rgbBuffer = (byte*)ffmpeg.av_malloc((ulong)numBytes);
        byte_ptr8 data_ptr8 = bgraFrame->data;
        int8 linesize8 = bgraFrame->linesize;
        var data = *(byte_ptr4*) &data_ptr8;
        var linesize = *(int4*) &linesize8;
        ffmpeg.av_image_fill_arrays(ref data, ref linesize, rgbBuffer, AVPixelFormat.AV_PIX_FMT_BGRA, codecContext->width, codecContext->height, 1);
        
        DeviceCreated();
    }

    public override Texture GetUpdatedTexture()
    {
        if (TryDecodeNextFrame(out byte[] frameData))
        {
            UploadToD3D9Texture(texture, frameData);
        }

        return texture;
    }
    
    public bool TryDecodeNextFrame(out byte[] bgraFrameData)
    {
        bgraFrameData = null;

        AVPacket* packet = ffmpeg.av_packet_alloc();

        while (ffmpeg.av_read_frame(formatContext, packet) >= 0)
        {
            if (packet->stream_index == videoStreamIndex)
            {
                int send = ffmpeg.avcodec_send_packet(codecContext, packet);
                if (send < 0)
                {
                    ffmpeg.av_packet_unref(packet);
                    continue;
                }

                int receive = ffmpeg.avcodec_receive_frame(codecContext, frame);
                
                if (receive == 0)
                {
                    // Convert to BGRA
                    ffmpeg.sws_scale(
                        swsContext,
                        frame->data,
                        frame->linesize,
                        0,
                        codecContext->height,
                        bgraFrame->data,
                        bgraFrame->linesize
                    );

                    int stride = bgraFrame->linesize[0];
                    int height = codecContext->height;
                    int dataSize = stride * height;

                    bgraFrameData = new byte[dataSize];
                    Marshal.Copy((IntPtr)bgraFrame->data[0], bgraFrameData, 0, dataSize);

                    ffmpeg.av_packet_unref(packet);
                    return true;
                }
            }

            ffmpeg.av_packet_unref(packet);
        }

        return false;
    }
    
    public void UploadToD3D9Texture(Texture texture, byte[] data)
    {
        var rect = texture.LockRectangle(0, LockFlags.Discard);
        Marshal.Copy(data, 0, rect.DataPointer, data.Length);
        texture.UnlockRectangle(0);
    }

    public override void Dispose()
    {
        if (bgraFrame != null)
        {
            AVFrame* tmp = bgraFrame;
            ffmpeg.av_frame_free(&tmp);
            bgraFrame = null;
        }

        if (swsContext != null)
        {
            ffmpeg.sws_freeContext(swsContext);
            swsContext = null;
        }
        
        base.Dispose();
    }
}