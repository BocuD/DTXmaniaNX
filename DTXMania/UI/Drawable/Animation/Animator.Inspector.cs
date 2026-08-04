using System.Numerics;
using DTXMania.Core;
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
        DrawClipStorage();
        ImGui.Separator();
        editor ??= new AnimationClipEditor();
        editor.DrawInInspector(this, root);

        // The timeline window is a separate top-level ImGui window; it's safe to Begin/End
        // it here because ImGui.Begin always opens at the top level regardless of where in
        // the frame it's called from.
        editor.DrawTimelineWindow(this, root);
    }

    //where each clip lives, and how to move it: a clip in a file is referenced by the layout, a clip
    //without one is written into it
    private void DrawClipStorage()
    {
        foreach (AnimationClip clip in clips)
        {
            ImGui.PushID(clip.name);

            bool skinLoaded = CDTXMania.SkinManager.currentSkin != null;

            if (clip.IsEmbedded)
            {
                ImGui.LabelText(clip.name, "Embedded");
            }
            else
            {
                ImGui.LabelText(clip.name, $"{clip.clipSource}: {clip.resource}");
            }

            //with a skin loaded, saving anything means giving the skin its own copy; without one there is
            //only the System file to write back to
            bool intoSkin = skinLoaded && clip.clipSource != ClipSource.Skin;

            if (ImGui.Button(intoSkin ? "Copy To Skin" : "Save"))
            {
                if (intoSkin)
                {
                    AnimationClipIO.MoveIntoSkin(clip, StageClipFile(clip));
                }
                else
                {
                    AnimationClipIO.SaveToResource(clip);
                }
            }

            ImGui.PopID();
        }
    }

    //clips are grouped by the stage that owns them, the way layouts are, so two stages can both have an
    //"open" without one overwriting the other
    private static string StageClipFile(AnimationClip clip)
    {
        string stage = CDTXMania.StageManager.rCurrentStage?.eStageID.ToString() ?? "Common";
        string name = string.IsNullOrWhiteSpace(clip.name) ? "Untitled" : clip.name;

        return System.IO.Path.Combine(stage, name + ".json");
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
