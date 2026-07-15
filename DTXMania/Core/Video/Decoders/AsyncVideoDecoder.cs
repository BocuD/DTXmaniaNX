using System.Runtime.InteropServices;
using FFmpeg.AutoGen.Abstractions;

namespace DTXMania.Core.Video.Decoders;

public unsafe class AsyncVideoDecoder : VideoDecoder
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

    private double seekTargetMuteSeconds = double.NaN;

    // Async / Threading components
    private Thread? decoderThread;
    private volatile bool stopRequested;
    private readonly object decodeSync = new();
    private readonly object queueSync = new();
    private readonly Queue<DecodedFrameData> decodedFrames = new();
    private bool endOfStreamReached;
    private const int MaxBufferedFrames = 2; // Keep queue small for low latency and memory

    private readonly FrameBufferPool framePool = new();

    // Incremented under decodeSync on every SeekTo. Frames decoded under an older
    // generation are discarded at enqueue time, eliminating the race where the
    // worker thread decodes a frame just before a seek and enqueues it just after
    // the seek cleared the queue.
    private long seekGeneration;

    public override int Width => codecContext != null ? codecContext->width : 0;
    public override int Height => codecContext != null ? codecContext->height : 0;

    public override bool IsEndOfStream
    {
        get
        {
            lock (queueSync)
            {
                return endOfStreamReached && decodedFrames.Count == 0;
            }
        }
    }

    public override string Name { get; } = "Async Decoder";

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

        timelineOriginPtsSeconds = double.NaN;
        lastDecodedPtsSeconds = -1;
        seekGeneration = 0;
        
        // Start background decoding thread
        stopRequested = false;
        endOfStreamReached = false;
        decoderThread = new Thread(DecoderWorkerLoop)
        {
            Name = "AsyncVideoDecoder.Decode",
            IsBackground = true
        };
        decoderThread.Start();

        return true;
    }

    private void DecoderWorkerLoop()
    {
        while (!stopRequested)
        {
            bool shouldDecode = false;
            lock (queueSync)
            {
                shouldDecode = decodedFrames.Count < MaxBufferedFrames && !endOfStreamReached;
            }

            if (!shouldDecode)
            {
                Thread.Sleep(2);
                continue;
            }

            // Snapshot the seek generation BEFORE decoding. If SeekTo bumps the
            // counter while we're decoding, the frame we produce is stale and
            // must not be enqueued; the seek already cleared the queue and
            // anything we'd push would be a frame from a pre-seek timeline.
            long genAtStart;
            lock (decodeSync)
            {
                genAtStart = seekGeneration;
            }

            if (TryDecodeOneFrame(out DecodedFrameData data, out bool reachedEof))
            {
                bool enqueued = false;
                lock (queueSync)
                {
                    // Re-check generation under queueSync. We don't need decodeSync here
                    // because seekGeneration is only written under decodeSync AND the queue
                    // is cleared under queueSync within that same critical section
                    // (see SeekTo), so observing the current generation through the
                    // happens-before edge of acquiring queueSync is safe.
                    if (!stopRequested && Volatile.Read(ref seekGeneration) == genAtStart)
                    {
                        decodedFrames.Enqueue(data);
                        enqueued = true;
                    }
                }

                // Stale frame from a pre-seek timeline (or shutting down): recycle its
                // buffer rather than letting it become garbage.
                if (!enqueued) framePool.Return(data.RgbaData);
            }
            else if (reachedEof)
            {
                lock (queueSync)
                {
                    // Only mark EOF if no seek has happened since we started this decode pass.
                    // Otherwise the EOF is stale (e.g. we hit EOF reading the pre-seek timeline
                    // because the seek raced in mid-read) and the next decode will recover.
                    if (Volatile.Read(ref seekGeneration) == genAtStart)
                    {
                        endOfStreamReached = true;
                    }
                }
                Thread.Sleep(5);
            }
        }
    }

    private bool TryDecodeOneFrame(out DecodedFrameData data, out bool reachEof)
    {
        reachEof = false;
        data = default;

        if (packet == null) return false;

        lock (decodeSync)
        {
            if (formatContext == null || codecContext == null) return false;

            while (true)
            {
                int readResult = ffmpeg.av_read_frame(formatContext, packet);
                if (readResult < 0)
                {
                    reachEof = true;
                    return false;
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
                        continue;
                    }
                    else
                    {
                        seekTargetMuteSeconds = double.NaN;
                    }
                }

                if (ffmpeg.av_frame_make_writable(rgbaFrame) < 0) return false;

                ffmpeg.sws_scale(swsContext, frame->data, frame->linesize, 0, codecContext->height, rgbaFrame->data, rgbaFrame->linesize);

                int packedSize = codecContext->width * codecContext->height * 4;
                byte[] rgbaData = framePool.Rent(packedSize);
                
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
    }

    public override void SeekTo(double targetSeconds)
    {
        lock (decodeSync)
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

            // Bump the generation *and* clear the queue while still holding decodeSync.
            // This pins both operations into a single critical section so the worker
            // thread cannot observe a half-applied seek.
            Volatile.Write(ref seekGeneration, seekGeneration + 1);
            lock (queueSync)
            {
                // Recycle the buffers of any queued frames before discarding them.
                while (decodedFrames.Count > 0)
                {
                    framePool.Return(decodedFrames.Dequeue().RgbaData);
                }
                endOfStreamReached = false;
            }
        }
    }

    public override bool TryGetDecodedFrame(out DecodedFrameData data)
    {
        lock (queueSync)
        {
            if (decodedFrames.Count > 0)
            {
                data = decodedFrames.Dequeue();
                return true;
            }
        }
        data = default;
        return false;
    }

    public override void ReturnFrameBuffer(byte[] buffer) => framePool.Return(buffer);

    public override bool GetNextFrameBlocking(out DecodedFrameData data)
    {
        // First check if the async thread already resolved it
        if (TryGetDecodedFrame(out data)) return true;

        // If the queue is empty, synchronously decode on the calling thread to guarantee instant snap.
        // The inner lock(decodeSync) within TryDecodeOneFrame ensures thread safety alongside the background loop.
        const int maxSeekAttempts = 1000;
        for (int i = 0; i < maxSeekAttempts; i++)
        {
            if (TryDecodeOneFrame(out data, out bool eof)) return true;
            if (eof) break;
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
        stopRequested = true;
        if (decoderThread != null && decoderThread.IsAlive)
        {
            decoderThread.Join(500);
        }

        lock (decodeSync)
        {
            if (frame != null) { AVFrame* f = frame; ffmpeg.av_frame_free(&f); frame = null; }
            if (rgbaFrame != null) { AVFrame* rf = rgbaFrame; ffmpeg.av_frame_free(&rf); rgbaFrame = null; }
            if (packet != null) { AVPacket* p = packet; ffmpeg.av_packet_free(&p); packet = null; }
            if (swsContext != null) { ffmpeg.sws_freeContext(swsContext); swsContext = null; }
            if (codecContext != null) { AVCodecContext* cc = codecContext; ffmpeg.avcodec_free_context(&cc); codecContext = null; }
            if (formatContext != null) { AVFormatContext* fc = formatContext; ffmpeg.avformat_close_input(&fc); formatContext = null; }
        }
        GC.SuppressFinalize(this);
    }
}
