using System.Numerics;
using DTXMania.Core.Video;
using DTXMania.UI.Animation.Editor;
using DTXMania.UI.Drawable;
using Hexa.NET.ImGui;
using Newtonsoft.Json;

namespace DTXMania.UI.Animation;

public sealed partial class Animator
{
    // Lazily-created editor state. JsonIgnore'd so it never touches saved layouts.
    [JsonIgnore] private AnimationClipEditor? editor;

    [JsonIgnore] public UINewVideoRenderer? reference;
    [JsonIgnore] public int referenceStartFrame;
        
    /// <summary>
    /// Render the animator UI. Inline: playback controls + clip/track editor. Outside: the
    /// floating timeline window if it's been opened (drawn as a top-level ImGui window —
    /// this works fine even though we call it from within the inspector's draw scope).
    /// </summary>
    public void DrawInspector(UIGroup root)
    {
        DrawPlaybackControls();
        ImGui.Separator();
        editor ??= new AnimationClipEditor();
        editor.DrawInInspector(this, root);

        // The timeline window is a separate top-level ImGui window; it's safe to Begin/End
        // it here because ImGui.Begin always opens at the top level regardless of where in
        // the frame it's called from.
        editor.DrawTimelineWindow(this, root);
    }

    private void DrawPlaybackControls()
    {
        if (currentClip != null)
        {
            ImGui.Text($"Playing: {currentClip.name}");
            float dur = MathF.Max(currentClip.duration, 0.0001f);
            ImGui.ProgressBar(Math.Clamp(time / dur, 0f, 1f), new Vector2(-1, 0), $"{time:0.00}s / {dur:0.00}s");
        }
        else
        {
            ImGui.TextDisabled("No clip playing.");
        }

        if (ImGui.Button(isPlaying ? "Pause" : "Resume"))
        {
            if (isPlaying) Pause(); else Resume();
        }
        ImGui.SameLine();
        if (ImGui.Button("Stop"))
        {
            Stop();
        }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(120f);
        ImGui.SliderFloat("Speed", ref speed, 0f, 4f);
        
        ImGui.Separator();
        ImGui.Text("Reference");
        
        if (ImGui.Button("Scan for video renderers")) FindVideoRenderers();

        int index = -1;
        if (reference != null) index = videoRenderers.IndexOf(reference);
        if (ImGui.Combo("Video Renderer", ref index, videoRenderers.Select(v => v.name).ToArray(), videoRenderers.Count))
        {
            if (index >= 0 && index < videoRenderers.Count)
            {
                reference = videoRenderers[index];
            }
            else
            {
                reference = null;
            }
        }

        if (reference != null)
        {
            ImGui.InputInt("Start Frame", ref referenceStartFrame);
            if (ImGui.Button("Set Reference"))
            {
                // Set the reference to the current frame of the video renderer, so that users can scrub the video and see the clip update in real time.
                referenceStartFrame = (int)reference.Controller.CurrentFrame.FrameNumber;
            }
        }
    }

    private List<UINewVideoRenderer> videoRenderers = [];

    private void FindVideoRenderers()
    {
        List<KeyValuePair<string, WeakReference<UIDrawable>>> drawables = DrawableTracker.drawables.Where(d => d.Value.TryGetTarget(out UIDrawable? target) && target is UINewVideoRenderer).ToList();
        videoRenderers = drawables.Select(d => d.Value.TryGetTarget(out UIDrawable? target) ? (UINewVideoRenderer)target : null).Where(v => v != null).ToList()!;
    }
}
