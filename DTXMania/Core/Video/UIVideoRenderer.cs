using System.Drawing;
using System.Numerics;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using DTXMania.UI.Inspector;
using Hexa.NET.ImGui;
using NativeFileDialog.Extended;

namespace DTXMania.Core.Video;

public class UIVideoRenderer : UIDrawable
{
    private FFmpegVideoPlayer player;
    private RectangleF clipRect;
    private string? sourcePath;
    private readonly IReadOnlyList<FFmpegVideoPlayer.BackendDescriptor> availableBackends;
    private readonly string[] backendNames;
    private Type selectedBackendType;
    private string backendSwitchError = string.Empty;
    private float inspectorSeekSeconds;
    private bool inspectorSeekInitialized;
    private bool inspectorSeekDragging;
    private BaseTexture lastRenderedTexture = BaseTexture.None;

    [AddChildMenu]
    public static UIVideoRenderer Create()
    {
        SoftwareVideoPlayer player = new();
        return new UIVideoRenderer(player);
    }
    
    public UIVideoRenderer(FFmpegVideoPlayer videoPlayer, string? videoSourcePath = null)
    {
        player = videoPlayer;
        sourcePath = videoSourcePath;
        availableBackends = FFmpegVideoPlayer.GetAvailableBackends();
        backendNames = availableBackends.Select(b => b.Name).ToArray();
        selectedBackendType = videoPlayer.GetType();
        
        if (videoSourcePath != null)
        {
            size = new Vector2(videoPlayer.Width, videoPlayer.Height);
            clipRect = new RectangleF(0, 0, videoPlayer.Width, videoPlayer.Height);
        }
    }
    
    public override void Draw(Matrix4x4 parentMatrix)
    {
        if (!isVisible)
        {
            return;
        }

        UpdateLocalTransformMatrix();
        Matrix4x4 combinedMatrix = localTransformMatrix * parentMatrix;

        player.UpdatePlayback();
        BaseTexture tex = player.GetUpdatedTexture();
        lastRenderedTexture = tex;
        if (!tex.IsValid())
        {
            return;
        }

        tex.tDraw2DMatrix(combinedMatrix, size, clipRect, Color4.White);
    }

    public bool Seek(TimeSpan timestamp)
    {
        return player.Seek(timestamp);
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        // Scope all widgets to this drawable instance to avoid cross-instance ID collisions.
        ImGui.PushID(id);

        if (!ImGui.CollapsingHeader("Video"))
        {
            ImGui.PopID();
            return;
        }

        TimeSpan current = player.CurrentTime;
        TimeSpan duration = player.Duration;
        long currentFrame = player.GetCurrentFrameNumber();
        long totalFrames = player.TotalFrameCount;
        ImGui.Text($"Current: {current:mm\\:ss\\.fff}");
        ImGui.Text(duration > TimeSpan.Zero
            ? $"Duration: {duration:mm\\:ss\\.fff}"
            : "Duration: Unknown");
        ImGui.Text(totalFrames > 0
            ? $"Frame: {currentFrame + 1} / {totalFrames}"
            : $"Frame: {currentFrame + 1} / ?");

        if (!string.IsNullOrWhiteSpace(sourcePath))
        {
            int backendIndex = FindBackendIndex(selectedBackendType);
            if (backendIndex < 0 && availableBackends.Count > 0)
            {
                backendIndex = 0;
            }

            if (backendIndex >= 0 && ImGui.Combo("Backend", ref backendIndex, backendNames, backendNames.Length))
            {
                FFmpegVideoPlayer.BackendDescriptor selectedBackend = availableBackends[backendIndex];
                if (selectedBackend.Type != selectedBackendType)
                {
                    TimeSpan restoreTimestamp = current;
                    bool loopOnSwitch = player.LoopOnEof;
                    if (!SwitchBackend(selectedBackend, restoreTimestamp, loopOnSwitch, out string error))
                    {
                        backendSwitchError = error;
                    }
                    else
                    {
                        backendSwitchError = string.Empty;
                        current = player.CurrentTime;
                        duration = player.Duration;
                    }
                }
            }
        }
        
        ImGui.LabelText("Source path: ", sourcePath ?? "(none)");
        ImGui.SameLine();
        if (ImGui.Button("Browse..."))
        {
            //open file dialog
            Dictionary<string, string> filterList = new()
            {
                { "Videos", "mp4,mov,avi,mkv,wmv,flv,webm" }
            };

            string path = NFD.OpenDialog("", filterList);
            
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                TimeSpan restoreTimestamp = current;
                bool loopOnSwitch = player.LoopOnEof;
                sourcePath = path;
                if (!SwitchBackend(availableBackends[0], restoreTimestamp, loopOnSwitch, out string error))
                {
                    backendSwitchError = error;
                }
                else
                {
                    backendSwitchError = string.Empty;
                    current = player.CurrentTime;
                    duration = player.Duration;
                    size = new Vector2(player.Width, player.Height);
                    clipRect = new RectangleF(0, 0, player.Width, player.Height);
                }
            }
        }

        if (!string.IsNullOrEmpty(backendSwitchError))
        {
            ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), backendSwitchError);
        }

        bool loopOnEof = player.LoopOnEof;
        if (ImGui.Checkbox("Loop on EOF", ref loopOnEof))
        {
            player.LoopOnEof = loopOnEof;
        }

        // Pause toggle button
        bool isPaused = player.IsPaused;
        string pauseButtonLabel = isPaused ? "Resume" : "Pause";
        if (ImGui.Button(pauseButtonLabel))
        {
            player.SetPaused(!isPaused);
        }
        ImGui.SameLine();

        // Frame advance/retreat buttons (only useful when paused)
        if (player.IsPaused)
        {
            bool previousFramePressed = ImGui.Button("< Frame");
            if (previousFramePressed)
            {
                player.SeekByFrame(Math.Max(0, currentFrame - 1));
            }
            ImGui.SameLine();
            bool nextFramePressed = ImGui.Button("Frame >");
            if (nextFramePressed)
            {
                long nextFrame = currentFrame + 1;
                if (totalFrames > 0)
                {
                    nextFrame = Math.Min(totalFrames - 1, nextFrame);
                }

                player.SeekByFrame(nextFrame);
            }
        }

        if (!inspectorSeekInitialized)
        {
            inspectorSeekSeconds = (float)current.TotalSeconds;
            inspectorSeekInitialized = true;
        }

        float maxSeek = duration > TimeSpan.Zero
            ? Math.Max((float)duration.TotalSeconds, 0.001f)
            : Math.Max(inspectorSeekSeconds + 1f, 1f);

        if (!inspectorSeekDragging)
        {
            inspectorSeekSeconds = (float)current.TotalSeconds;
        }

        inspectorSeekSeconds = Math.Clamp(inspectorSeekSeconds, 0f, maxSeek);
        ImGui.SliderFloat("Progress", ref inspectorSeekSeconds, 0f, maxSeek, "%.3f s");

        bool sliderActive = ImGui.IsItemActive();
        if (sliderActive)
        {
            if (!inspectorSeekDragging)
            {
                inspectorSeekDragging = true;
                // When starting to drag, seek to the new position and update texture
                player.Seek(TimeSpan.FromSeconds(inspectorSeekSeconds));
                player.UpdateTextureForSeek();
            }
            else
            {
                // While dragging, continuously update the seek position and texture
                player.Seek(TimeSpan.FromSeconds(inspectorSeekSeconds));
                player.UpdateTextureForSeek();
            }
        }
        else if (inspectorSeekDragging)
        {
            // Released the slider - ensure final position is set
            inspectorSeekDragging = false;
            player.Seek(TimeSpan.FromSeconds(inspectorSeekSeconds));
            player.UpdateTextureForSeek();
        }

        BaseTexture texture = lastRenderedTexture;
        if (!texture.IsValid())
        {
            ImGui.Text("No video frame available.");
            ImGui.PopID();
            return;
        }

        float windowWidth = ImGui.GetWindowWidth();
        float previewWidth = MathF.Min(windowWidth - 64f, texture.Width * 2f);
        previewWidth = Math.Max(previewWidth, 64f);
        float previewHeight = texture.Height * (previewWidth / MathF.Max(texture.Width, 1f));
        ImGui.Dummy(new Vector2(previewWidth, previewHeight));

        Vector2 pMin = ImGui.GetItemRectMin();
        Vector2 pMax = ImGui.GetItemRectMax();
        ImTextureID? textureId = texture.GetImTextureID();
        if (textureId is { } textureHandle)
        {
            unsafe
            {
                ImTextureRef textureRef = new(null, textureHandle);
                ImGui.GetWindowDrawList().AddImage(textureRef, pMin, pMax);
            }

            ImGui.GetWindowDrawList().AddRect(pMin, pMax, 0xFF00FF00, 0, 0, 2);
        }

        ImGui.PopID();
    }

    public override void Dispose()
    {
        if (player != null)
        {
            player.Dispose();
        }

        lastRenderedTexture = BaseTexture.None;
        base.Dispose();
    }

    private bool SwitchBackend(FFmpegVideoPlayer.BackendDescriptor targetBackend, TimeSpan restoreTimestamp, bool loopOnEof, out string error)
    {
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            error = "Cannot switch backend: source path is unavailable.";
            return false;
        }

        FFmpegVideoPlayer newPlayer = targetBackend.Create(loopOnEof);
        if (!newPlayer.Open(sourcePath))
        {
            newPlayer.Dispose();
            error = $"Failed to open video using {targetBackend.Name}.";
            return false;
        }

        TimeSpan duration = newPlayer.Duration;
        if (duration > TimeSpan.Zero && restoreTimestamp > duration)
        {
            restoreTimestamp = duration;
        }

        newPlayer.Seek(restoreTimestamp);

        FFmpegVideoPlayer oldPlayer = player;
        player = newPlayer;
        selectedBackendType = targetBackend.Type;
        size = new Vector2(player.Width, player.Height);
        clipRect = new RectangleF(0, 0, player.Width, player.Height);
        lastRenderedTexture = BaseTexture.None;
        inspectorSeekInitialized = false;
        inspectorSeekDragging = false;
        oldPlayer.Dispose();
        return true;
    }

    private int FindBackendIndex(Type backendType)
    {
        for (int i = 0; i < availableBackends.Count; i++)
        {
            if (availableBackends[i].Type == backendType)
            {
                return i;
            }
        }

        return -1;
    }
}