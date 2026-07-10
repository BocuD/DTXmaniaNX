using System.Collections.Concurrent;
using System.Diagnostics;
using DTXMania.UI.Drawable;
using DTXMania.UI.Text;
using SkiaSharp;

namespace DTXMania.UI.OpenGL;

/// <summary>
/// Decodes images / rasterizes text on a background worker thread and uploads the resulting
/// pixels to GL textures on the main thread, throttled by a per-frame byte budget. This keeps
/// both the CPU decode/rasterize cost and the GPU upload cost off of the render thread.
///
/// Threading contract: the worker thread only produces <see cref="DecodedPixels"/> (no GL calls).
/// All GL work (<see cref="BaseTexture.LoadFromMemory"/>) and every <c>onUploaded</c> callback run
/// on the main thread inside <see cref="PumpUploads"/>.
/// </summary>
public sealed class AsyncTextureUploader
{
    public static AsyncTextureUploader Instance { get; } = new();

    private readonly record struct WorkItem(Func<DecodedPixels> Produce, Action<BaseTexture?> OnUploaded);
    private readonly record struct ReadyItem(DecodedPixels Pixels, Action<BaseTexture?> OnUploaded);

    private readonly BlockingCollection<WorkItem> _work = new();
    private readonly object _readySync = new();
    private readonly Queue<ReadyItem> _ready = new();

    private readonly object _workerSync = new();
    private Thread? _worker;

    /// <summary>
    /// Queues a preview image to be decoded from <paramref name="path"/> and downscaled so its
    /// largest dimension does not exceed <paramref name="maxDimension"/>. <paramref name="onUploaded"/>
    /// runs on the main thread with the uploaded texture, or null if the file was missing/undecodable.
    /// </summary>
    public void RequestImage(string path, int maxDimension, Action<BaseTexture?> onUploaded)
    {
        EnsureWorker();
        _work.Add(new WorkItem(() => DecodeImage(path, maxDimension), onUploaded));
    }

    /// <summary>
    /// Queues text to be rasterized on the worker thread. <paramref name="onUploaded"/> runs on the
    /// main thread with the uploaded texture (or null if it could not be rasterized).
    /// </summary>
    public void RequestText(UiTextParameters request, Action<BaseTexture?> onUploaded)
    {
        EnsureWorker();
        _work.Add(new WorkItem(
            () => BaseTexture.SkiaTextRenderer?.RenderToPixels(request) ?? default,
            onUploaded));
    }

    /// <summary>
    /// Main thread only. Uploads decoded results and invokes their callbacks until the byte budget
    /// is spent. Always processes at least one item so a single oversized texture cannot stall the
    /// queue indefinitely.
    /// </summary>
    public void PumpUploads(long maxBytesPerFrame)
    {
        long used = 0;

        while (true)
        {
            ReadyItem item;
            lock (_readySync)
            {
                if (_ready.Count == 0)
                {
                    break;
                }

                // Budget check before dequeue: process at least one, then stop once spent.
                if (used > 0 && used >= maxBytesPerFrame)
                {
                    break;
                }

                item = _ready.Dequeue();
            }

            BaseTexture? tex = null;
            if (item.Pixels.IsValid)
            {
                tex = BaseTexture.LoadFromMemory(item.Pixels.Rgba, item.Pixels.Width, item.Pixels.Height, item.Pixels.Name);
                used += item.Pixels.ByteCount;
            }

            try
            {
                item.OnUploaded?.Invoke(tex);
            }
            catch (Exception e)
            {
                Trace.TraceError($"AsyncTextureUploader upload callback failed: {e}");
                tex?.Dispose();
            }
        }
    }

    /// <summary>Drops any decoded-but-not-yet-uploaded results (no GL textures exist for them yet).</summary>
    public void Clear()
    {
        lock (_readySync)
        {
            _ready.Clear();
        }
    }

    public void Shutdown()
    {
        _work.CompleteAdding();
        _worker?.Join(500);
        _worker = null;
        Clear();
    }

    private void EnsureWorker()
    {
        if (_worker != null)
        {
            return;
        }

        lock (_workerSync)
        {
            if (_worker != null)
            {
                return;
            }

            _worker = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Name = "AsyncTextureUploader",
            };
            _worker.Start();
        }
    }

    private void WorkerLoop()
    {
        foreach (WorkItem item in _work.GetConsumingEnumerable())
        {
            DecodedPixels pixels;
            try
            {
                pixels = item.Produce();
            }
            catch (Exception e)
            {
                Trace.TraceWarning($"AsyncTextureUploader decode failed: {e}");
                pixels = default;
            }

            lock (_readySync)
            {
                _ready.Enqueue(new ReadyItem(pixels, item.OnUploaded));
            }
        }
    }

    /// <summary>
    /// Decodes <paramref name="path"/> with SkiaSharp and downscales it (preserving aspect ratio)
    /// so its largest dimension is at most <paramref name="maxDimension"/>. Output is straight-alpha
    /// RGBA. Safe to call from any thread. Returns an invalid <see cref="DecodedPixels"/> on failure.
    /// </summary>
    public static DecodedPixels DecodeImage(string path, int maxDimension)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return default;
        }

        using SKBitmap? decoded = SKBitmap.Decode(path);
        if (decoded == null)
        {
            return default;
        }

        (int targetWidth, int targetHeight) = ClampSize(decoded.Width, decoded.Height, maxDimension);

        // Resize also normalizes to RGBA8888 / Unpremul in a single step (identity resample when
        // the source is already within bounds).
        SKImageInfo info = new(targetWidth, targetHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using SKBitmap? resized = decoded.Resize(info, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));
        if (resized == null)
        {
            return default;
        }

        return new DecodedPixels(resized.Bytes, targetWidth, targetHeight, Path.GetFileName(path));
    }

    private static (int Width, int Height) ClampSize(int width, int height, int maxDimension)
    {
        if (maxDimension <= 0 || (width <= maxDimension && height <= maxDimension))
        {
            return (Math.Max(width, 1), Math.Max(height, 1));
        }

        float scale = maxDimension / (float)Math.Max(width, height);
        int targetWidth = Math.Max((int)MathF.Round(width * scale), 1);
        int targetHeight = Math.Max((int)MathF.Round(height * scale), 1);
        return (targetWidth, targetHeight);
    }
}
