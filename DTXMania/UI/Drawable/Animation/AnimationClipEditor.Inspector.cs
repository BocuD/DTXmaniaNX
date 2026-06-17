using System.IO;
using DTXMania.UI.Drawable;
using Hexa.NET.ImGui;
using NativeFileDialog.Extended;

namespace DTXMania.UI.Animation.Editor;

public sealed partial class AnimationClipEditor
{
    public void DrawInInspector(Animator animator, UIGroup root)
    {
        DrawClipList(animator);

        ImGui.Separator();
        DrawFileSection(animator);

        if (selectedClip == null)
        {
            ImGui.TextDisabled("No clip selected.");
            return;
        }

        ImGui.Separator();
        DrawClipHeader(selectedClip);

        if (ImGui.Button("Play"))
        {
            animator.Play(selectedClip.name);
        }

        ImGui.Separator();
        if (ImGui.Button(timelineWindowOpen ? "Close Timeline" : "Open Timeline"))
        {
            timelineWindowOpen = !timelineWindowOpen;
        }
    }

    private void DrawFileSection(Animator animator)
    {
        ImGui.Text("File");

        ImGui.BeginDisabled(selectedClip == null);
        if (ImGui.Button("Save"))
        {
            if (selectedClip != null)
            {
                lastPathByClip.TryGetValue(selectedClip, out string? known);
                (string defaultDir, string defaultName) = AnimationClipIO.GetSaveDialogDefaults(selectedClip, known);

                string chosen = NFD.SaveDialog(defaultDir, defaultName, new Dictionary<string, string> { { "Animation Clip", "json" } });
                if (!string.IsNullOrEmpty(chosen))
                {
                    // NFD may not auto-append the extension on all platforms.
                    if (!chosen.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        chosen += ".json";
                    }
                    if (AnimationClipIO.SaveToFile(selectedClip, chosen))
                    {
                        lastPathByClip[selectedClip] = chosen;
                        ShowStatus($"Saved '{Path.GetFileName(chosen)}'");
                    }
                    else
                    {
                        ShowStatus("Save failed (see log)");
                    }
                }
            }
        }
        ImGui.EndDisabled();

        ImGui.SameLine();
        if (ImGui.Button("Load"))
        {
            string chosen = NFD.OpenDialog("", new Dictionary<string, string> { { "Animation Clip", "json" } });
            if (!string.IsNullOrEmpty(chosen))
            {
                AnimationClip? loaded = AnimationClipIO.LoadFromFile(chosen);
                if (loaded != null)
                {
                    animator.clips.Add(loaded);
                    lastPathByClip[loaded] = chosen;
                    selectedClip = loaded;
                    selectedTrack = null;
                    selectedKeyframe = null;
                    scrubTime = 0f;
                    ShowStatus($"Loaded '{Path.GetFileName(chosen)}'");
                }
                else
                {
                    ShowStatus("Load failed (see log)");
                }
            }
        }

        // Status line (fades out by itself).
        if (fileStatusMessage != null)
        {
            double now = ImGui.GetTime();
            if (now < fileStatusUntilTime)
            {
                ImGui.TextDisabled(fileStatusMessage);
            }
            else
            {
                fileStatusMessage = null;
            }
        }
    }

    private void DrawClipList(Animator animator)
    {
        ImGui.Text("Clips");
        ImGui.SameLine();
        if (ImGui.SmallButton("+##addclip"))
        {
            AnimationClip clip = new() { name = $"Clip {animator.clips.Count + 1}", duration = 1f };
            animator.clips.Add(clip);
            selectedClip = clip;
            selectedTrack = null;
        }

        if (animator.clips.Count == 0)
        {
            ImGui.TextDisabled("(none)");
            return;
        }

        for (int i = 0; i < animator.clips.Count; i++)
        {
            AnimationClip clip = animator.clips[i];
            ImGui.PushID(i);
            bool isSelected = clip == selectedClip;
            if (ImGui.Selectable($"{clip.name}##sel", isSelected))
            {
                selectedClip = clip;
                selectedTrack = null;
                selectedKeyframe = null;
                scrubTime = 0f;
            }
            if (ImGui.BeginPopupContextItem("clipctx"))
            {
                if (ImGui.MenuItem("Play")) animator.Play(clip.name);
                if (ImGui.MenuItem("Delete"))
                {
                    if (animator.currentClip == clip) { animator.Stop(); animator.currentClip = null; }
                    animator.clips.RemoveAt(i);
                    lastPathByClip.Remove(clip);
                    if (selectedClip == clip) { selectedClip = null; selectedTrack = null; selectedKeyframe = null; }
                    ImGui.EndPopup();
                    ImGui.PopID();
                    return;
                }
                ImGui.EndPopup();
            }
            ImGui.PopID();
        }
    }

    private static void DrawClipHeader(AnimationClip clip)
    {
        ImGui.InputText("Name", ref clip.name, 128);
        ImGui.InputFloat("Duration", ref clip.duration);
        if (clip.duration < 0f) clip.duration = 0f;
        ImGui.Checkbox("Loop", ref clip.loop);
    }
}