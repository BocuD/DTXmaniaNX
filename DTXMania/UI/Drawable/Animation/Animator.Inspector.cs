using System.Numerics;
using DTXMania.UI.Animation.Editor;
using DTXMania.UI.Drawable;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Animation;

public sealed partial class Animator
{
    // Lazily-created editor state. JsonIgnore'd so it never touches saved layouts.
    [Newtonsoft.Json.JsonIgnore] private AnimationClipEditor? editor;

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
    }
}
