using System.Drawing;
using System.Numerics;
using DTXMania.Core.Framework;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using Hexa.NET.ImGui;

namespace DTXMania.Core.Video;

public class UIVideoRenderer : UIDrawable
{
    private FFmpegVideoPlayer player;
    private RectangleF clipRect;
    private readonly string? sourcePath;
    private readonly IReadOnlyList<FFmpegVideoPlayer.BackendDescriptor> availableBackends;
    private readonly string[] backendNames;
    private Type selectedBackendType;
    private string backendSwitchError = string.Empty;
    private float inspectorSeekSeconds;
    private bool inspectorSeekInitialized;
    private bool inspectorSeekDragging;
    private BaseTexture lastRenderedTexture = BaseTexture.None;
    
    public UIVideoRenderer(FFmpegVideoPlayer videoPlayer, string? videoSourcePath = null)
    {
        player = videoPlayer;
        sourcePath = videoSourcePath;
        availableBackends = FFmpegVideoPlayer.GetAvailableBackends();
        backendNames = availableBackends.Select(b => b.Name).ToArray();
        selectedBackendType = videoPlayer.GetType();

        size = new Vector2(videoPlayer.Width, videoPlayer.Height);
        clipRect = new RectangleF(0, 0, videoPlayer.Width, videoPlayer.Height);
    }
    
    public override void Draw(Matrix4x4 parentMatrix)
    {
        if (!isVisible)
        {
            return;
        }

        UpdateLocalTransformMatrix();
        Matrix4x4 combinedMatrix = localTransformMatrix * parentMatrix;

        BaseTexture tex = player.GetUpdatedTexture();
        lastRenderedTexture = tex;
        if (!tex.isValid())
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
        ImGui.Text($"Current: {current:mm\\:ss\\.fff}");
        ImGui.Text(duration > TimeSpan.Zero
            ? $"Duration: {duration:mm\\:ss\\.fff}"
            : "Duration: Unknown");

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

        if (!string.IsNullOrEmpty(backendSwitchError))
        {
            ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), backendSwitchError);
        }

        bool loopOnEof = player.LoopOnEof;
        if (ImGui.Checkbox("Loop on EOF", ref loopOnEof))
        {
            player.LoopOnEof = loopOnEof;
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
            inspectorSeekDragging = true;
        }
        else if (inspectorSeekDragging)
        {
            inspectorSeekDragging = false;
            player.Seek(TimeSpan.FromSeconds(inspectorSeekSeconds));
        }

        BaseTexture texture = lastRenderedTexture;
        if (!texture.isValid())
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