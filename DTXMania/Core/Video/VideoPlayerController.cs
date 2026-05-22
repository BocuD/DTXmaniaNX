using System.Diagnostics;
using System.Numerics;
using DTXMania.Core.Video.Decoders;
using DTXMania.UI.Drawable;
using Hexa.NET.ImGui;
using NativeFileDialog.Extended;

namespace DTXMania.Core.Video;

public class VideoPlayerController : IDisposable
{
    private VideoDecoder decoder;
    private readonly Stopwatch clock = new();
    private double clockOffsetSeconds = 0;
    
    // Configurable state
    public bool IsPaused { get; set; }

    private float playbackSpeed = 1f;
    public float PlaybackSpeed
    {
        get => playbackSpeed;
        set
        {
            if (playbackSpeed != value)
            {
                // Anchor the clock target time at the exact moment of the speed change
                // so the timeline doesn't jump forwards or backwards artificially.
                clockOffsetSeconds += clock.Elapsed.TotalSeconds * playbackSpeed;
                clock.Restart();
                if (IsPaused) clock.Stop();
                
                playbackSpeed = value;
            }
        }
    }
    
    public bool LoopOnEof { get; set; } = true;
    public string? CurrentSourcePath { get; private set; }
    public bool UseSoftwareDecoder { get; set; } = false;
    
    // Core atomic state constraint
    public DisplayedFrame CurrentFrame { get; private set; } = DisplayedFrame.Empty;

    // Transient UI tracking
    private bool inspectorSeekDragging;
    private float inspectorSeekSeconds;
    private string errorMessage = string.Empty;

    public bool TryLoadVideo(string path)
    {
        VideoDecoder newDecoder = UseSoftwareDecoder ? new SoftwareVideoDecoder() : new AsyncVideoDecoder();
        if (!newDecoder.TryOpen(path))
        {
            newDecoder.Dispose();
            errorMessage = "Failed to load video format or allocate decoder contexts.";
            return false;
        }

        CleanupCurrentVideo();
        
        decoder = newDecoder;
        CurrentSourcePath = path;
        
        // Ensure atomic sync from second 0 immediately upon loading
        ForceSeekAndRender(0);
        errorMessage = string.Empty;

        return true;
    }

    public void Update()
    {
        if (decoder == null || !CurrentFrame.IsValid) return;

        if (!IsPaused)
        {
            if (!clock.IsRunning) clock.Start();
        }
        else
        {
            if (clock.IsRunning) clock.Stop();
            return; // If paused, we don't actively pump normal frames unless a seek explicitly tells us to
        }

        double realElapsed = clock.Elapsed.TotalSeconds;
        double currentTargetSeconds = clockOffsetSeconds + (realElapsed * PlaybackSpeed);

        if (CurrentFrame.TimeSeconds > currentTargetSeconds)
        {
            return; // Our currently displayed frame is already ahead of the current active playback target time, so wait.
        }

        const int maxSyncDecodesPerTick = 2; // Linear decode allowance per tick
        
        for (int i = 0; i < maxSyncDecodesPerTick; i++)
        {
            if (decoder.TryGetDecodedFrame(out DecodedFrameData data))
            {
                ApplyFrameDataAtomic(data);

                if (data.TimeSeconds >= currentTargetSeconds)
                {
                    break; 
                }
            }
            else if (decoder.IsEndOfStream)
            {
                // Real end-of-stream confirmed by the decoder.
                if (LoopOnEof)
                {
                    ForceSeekAndRender(0);
                }
                else
                {
                    IsPaused = true;
                }
                break;
            }
            else
            {
                // Async decoder: queue momentarily empty but more frames are coming.
                // Do nothing this tick and let the current frame linger; the next
                // Update will pick up the produced frame. The clock keeps running
                // so playback resumes seamlessly once the worker catches up.
                break;
            }
        }
    }

    public void SeekToSeconds(double seconds)
    {
        if (decoder == null) return;
        ForceSeekAndRender(seconds);
    }

    public void SeekToFrame(long frame)
    {
        if (decoder == null) return;
        if (frame < 0) return;
        if (CurrentFrame.TotalFrames > 0 && frame >= CurrentFrame.TotalFrames) return;

        double targetSeconds = frame / decoder.FrameRate;
        ForceSeekAndRender(targetSeconds);
    }

    public void SeekRelativeFrames(long frames)
    {
        if (decoder == null || !CurrentFrame.IsValid) return;
        if (decoder.FrameRate <= 0) return;

        long nextFrame = CurrentFrame.FrameNumber + frames;
        nextFrame = Math.Max(0, nextFrame);
        if (CurrentFrame.TotalFrames > 0)
        {
            nextFrame = Math.Min(CurrentFrame.TotalFrames - 1, nextFrame);
        }

        double targetSeconds = nextFrame / decoder.FrameRate;
        ForceSeekAndRender(targetSeconds);
    }

    private void ForceSeekAndRender(double targetSeconds)
    {
        if (decoder == null) return;
        
        targetSeconds = Math.Max(0, targetSeconds);
        
        decoder.SeekTo(targetSeconds);
        
        if (decoder.GetNextFrameBlocking(out DecodedFrameData data))
        {
            ApplyFrameDataAtomic(data);
        }

        // Align internal time state to the newly resolved display time exactly
        clockOffsetSeconds = CurrentFrame.TimeSeconds;
        clock.Restart();
        if (IsPaused) clock.Stop();
    }

    private void ApplyFrameDataAtomic(DecodedFrameData data)
    {
        BaseTexture activeTexture = CurrentFrame.Texture;
        
        // Only alloc if empty / invalid size
        if (activeTexture == null || !activeTexture.IsValid() || activeTexture.Width != decoder.Width || activeTexture.Height != decoder.Height)
        {
            if (activeTexture != null && activeTexture.IsValid()) activeTexture.Dispose();
            activeTexture = BaseTexture.CreateEmpty(decoder.Width, decoder.Height, "VideoPlayerFrame");
        }

        activeTexture.UpdateRgba32(data.RgbaData, decoder.Width, decoder.Height);

        CurrentFrame = new DisplayedFrame(
            texture: activeTexture,
            timeSeconds: data.TimeSeconds,
            frameNumber: data.FrameNumber,
            totalFrames: decoder.TotalFrames,
            totalDurationSeconds: decoder.DurationSeconds
        );
    }

    public void DrawInspector(string id)
    {
        ImGui.PushID(id);

        if (ImGui.CollapsingHeader("Video Player Controller", ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (decoder != null)
            {
                ImGui.Text("Decoder: " + decoder.Name);
            }

            ImGui.Text($"Source path: {CurrentSourcePath ?? "(none)"}");
            if (ImGui.Button("Change video (Browse)..."))
            {
                string path = NFD.OpenDialog("", new Dictionary<string, string> { { "Videos", "mp4,mov,avi,mkv,wmv,flv,webm" } });
                if (!string.IsNullOrWhiteSpace(path))
                {
                    TryLoadVideo(path);
                }
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), errorMessage);
            }

            if (CurrentFrame.IsValid)
            {
                ImGui.Text($"Current: {CurrentFrame.TimeSeconds:0.000}s / {CurrentFrame.TotalDurationSeconds:0.000}s");
                ImGui.Text($"Frame: {CurrentFrame.FrameNumber} / {(CurrentFrame.TotalFrames > 0 ? CurrentFrame.TotalFrames.ToString() : "?")}");
            }
            else
            {
                ImGui.Text("Current: -- / --");
                ImGui.Text("Frame: -- / --");
            }

            bool loop = LoopOnEof;
            if (ImGui.Checkbox("Loop on EOF", ref loop)) LoopOnEof = loop;

            float speed = PlaybackSpeed;
            if (ImGui.SliderFloat("Speed", ref speed, 0.1f, 3.0f, "%.1fx")) PlaybackSpeed = speed;

            string pauseBtn = IsPaused ? "Resume" : "Pause";
            if (ImGui.Button(pauseBtn)) IsPaused = !IsPaused;
            
            ImGui.SameLine();
            
            // Frame stepping only enabled when paused for fine-grained control
            ImGui.BeginDisabled(!IsPaused);
            if (ImGui.Button("< Frame")) SeekRelativeFrames(-1);
            ImGui.SameLine();
            if (ImGui.Button("Frame >")) SeekRelativeFrames(1);
            ImGui.EndDisabled();

            if (CurrentFrame.IsValid)
            {
                float totalSeconds = (float)Math.Max(CurrentFrame.TotalDurationSeconds, 0.001);

                if (!inspectorSeekDragging)
                {
                    inspectorSeekSeconds = (float)CurrentFrame.TimeSeconds;
                }

                inspectorSeekSeconds = Math.Clamp(inspectorSeekSeconds, 0f, totalSeconds);
                ImGui.SliderFloat("##Progress", ref inspectorSeekSeconds, 0f, totalSeconds, "%.3f s");

                bool isDraggingSlider = ImGui.IsItemActive();
                if (isDraggingSlider)
                {
                    inspectorSeekDragging = true;
                    SeekToSeconds(inspectorSeekSeconds);
                }
                else if (inspectorSeekDragging)
                {
                    inspectorSeekDragging = false;
                    SeekToSeconds(inspectorSeekSeconds);
                }
            }
        }

        ImGui.PopID();
    }

    private void CleanupCurrentVideo()
    {
        if (decoder != null)
        {
            decoder.Dispose();
            decoder = null;
        }

        if (CurrentFrame.Texture != null && CurrentFrame.Texture.IsValid())
        {
            CurrentFrame.Texture.Dispose();
        }

        CurrentFrame = DisplayedFrame.Empty;
        clock.Stop();
        clock.Reset();
        clockOffsetSeconds = 0;
    }

    public void Dispose()
    {
        CleanupCurrentVideo();
        GC.SuppressFinalize(this);
    }
}
