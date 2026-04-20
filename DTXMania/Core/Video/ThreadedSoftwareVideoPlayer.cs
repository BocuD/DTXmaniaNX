using System.Diagnostics;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen.Abstractions;
using DTXMania.UI.Drawable;

namespace DTXMania.Core.Video;

public unsafe class ThreadedSoftwareVideoPlayer : FFmpegVideoPlayer
{
    private sealed class DecodedFrame
    {
        public readonly byte[] Data;
        public readonly double PtsSeconds;

        public DecodedFrame(byte[] data, double ptsSeconds)
        {
            Data = data;
            PtsSeconds = ptsSeconds;
        }
    }

    private SwsContext* swsContext;
    private AVFrame* rgbaFrame;
    private double fallbackFrameDurationSeconds = 1.0 / 30.0;
    private double timelineOriginPtsSeconds = double.NaN;
    private double lastDecodedPtsSeconds = double.NaN;

    private readonly object decodeSync = new();
    private readonly object queueSync = new();
    private readonly Queue<DecodedFrame> decodedFrames = new();
    private Thread? decoderThread;
    private bool decoderStopRequested;
    private bool endOfStreamReached;

    private byte[] stagedFrameBuffer = [];
    private int stagedFrameLength;
    private BaseTexture texture = BaseTexture.None;
    private byte[] uploadBuffer = [];

    private const int MaxBufferedFrames = 1;

    public ThreadedSoftwareVideoPlayer(bool loopOnEof = true)
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

        texture = BaseTexture.CreateEmpty(codecContext->width, codecContext->height, "ThreadedVideoFrameTexture");
        if (!texture.isValid())
        {
            Trace.TraceError("Failed to create BaseTexture for video playback.");
            return false;
        }

        uploadBuffer = new byte[codecContext->width * codecContext->height * 4];
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

        StartDecoderThread();
        return true;
    }

    protected override BaseTexture OutputTexture => texture;

    public override bool Seek(TimeSpan timestamp)
    {
        lock (decodeSync)
        {
            return base.Seek(timestamp);
        }
    }

    protected override bool TryDecodeAndStageFrame(out double framePtsSeconds, out bool reachedEndOfStream)
    {
        lock (queueSync)
        {
            if (decodedFrames.Count > 0)
            {
                DecodedFrame frameData = decodedFrames.Dequeue();
                StageFrame(frameData.Data);
                framePtsSeconds = frameData.PtsSeconds;
                reachedEndOfStream = false;
                return true;
            }

            reachedEndOfStream = endOfStreamReached;
        }

        framePtsSeconds = 0;
        return false;
    }

    protected override void PresentStagedFrame()
    {
        if (stagedFrameLength <= 0 || !texture.isValid())
        {
            return;
        }

        texture.UpdateRgba32(stagedFrameBuffer, codecContext->width, codecContext->height);
    }

    protected override void OnPlaybackReset()
    {
        lock (queueSync)
        {
            decodedFrames.Clear();
            endOfStreamReached = false;
        }

        stagedFrameLength = 0;
        timelineOriginPtsSeconds = double.NaN;
        lastDecodedPtsSeconds = double.NaN;
    }

    public override void Dispose()
    {
        StopDecoderThread();

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
        stagedFrameBuffer = [];
        stagedFrameLength = 0;

        base.Dispose();
    }

    private void StartDecoderThread()
    {
        decoderStopRequested = false;
        decoderThread = new Thread(DecoderWorkerLoop)
        {
            Name = "ThreadedSoftwareVideoPlayer.Decode",
            IsBackground = true
        };
        decoderThread.Start();
    }

    private void StopDecoderThread()
    {
        decoderStopRequested = true;
        Thread? thread = decoderThread;
        if (thread != null && thread.IsAlive)
        {
            thread.Join(500);
        }

        decoderThread = null;
    }

    private void DecoderWorkerLoop()
    {
        while (!decoderStopRequested)
        {
            bool shouldDecode;
            lock (queueSync)
            {
                shouldDecode = decodedFrames.Count < MaxBufferedFrames && !endOfStreamReached;
            }

            if (!shouldDecode)
            {
                Thread.Sleep(2);
                continue;
            }

            if (!TryDecodeOneFrame(out byte[] frameData, out double ptsSeconds, out bool reachedEof))
            {
                if (reachedEof)
                {
                    lock (queueSync)
                    {
                        endOfStreamReached = true;
                    }
                }

                Thread.Sleep(reachedEof ? 5 : 1);
                continue;
            }

            lock (queueSync)
            {
                if (!decoderStopRequested)
                {
                    decodedFrames.Enqueue(new DecodedFrame(frameData, ptsSeconds));
                }
            }
        }
    }

    private bool TryDecodeOneFrame(out byte[] frameData, out double framePtsSeconds, out bool reachedEof)
    {
        AVPacket* packet = ffmpeg.av_packet_alloc();
        if (packet == null)
        {
            frameData = [];
            framePtsSeconds = 0;
            reachedEof = false;
            return false;
        }

        try
        {
            lock (decodeSync)
            {
                while (true)
                {
                    int readResult = ffmpeg.av_read_frame(formatContext, packet);
                    if (readResult < 0)
                    {
                        frameData = [];
                        framePtsSeconds = 0;
                        reachedEof = true;
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
                        frameData = [];
                        framePtsSeconds = 0;
                        reachedEof = false;
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

                    framePtsSeconds = ResolveFrameTimestampSeconds();
                    frameData = (byte[])uploadBuffer.Clone();
                    reachedEof = false;
                    return true;
                }
            }
        }
        finally
        {
            ffmpeg.av_packet_free(&packet);
        }
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

    private void StageFrame(byte[] frameData)
    {
        if (stagedFrameBuffer.Length != frameData.Length)
        {
            stagedFrameBuffer = new byte[frameData.Length];
        }

        Buffer.BlockCopy(frameData, 0, stagedFrameBuffer, 0, frameData.Length);
        stagedFrameLength = frameData.Length;
    }
}

