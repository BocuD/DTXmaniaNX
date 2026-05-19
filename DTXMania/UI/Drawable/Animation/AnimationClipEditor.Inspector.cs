using System.Numerics;
using System.Reflection;
using DTXMania.Core.Framework;
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
        DrawTrackList(selectedClip, root);

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

                // Make sure the default directory exists so NFD has somewhere to open at.
                if (!Directory.Exists(defaultDir))
                {
                    try { Directory.CreateDirectory(defaultDir); } catch { /* ignored — NFD will fall back */ }
                }

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

    private void DrawTrackList(AnimationClip clip, UIGroup root)
    {
        ImGui.Text("Tracks");
        ImGui.SameLine();

        // The "+" track button opens a nested menu that walks the drawable tree and lists
        // themable properties on each. Clicking a leaf adds a new track with the resolved path.
        if (ImGui.BeginPopup("addtrackpopup"))
        {
            DrawAddTrackMenu(clip, root, parentPath: "");
            ImGui.EndPopup();
        }
        if (ImGui.SmallButton("+##addtrack"))
        {
            ImGui.OpenPopup("addtrackpopup");
        }

        for (int i = 0; i < clip.tracks.Count; i++)
        {
            AnimationTrack track = clip.tracks[i];
            ImGui.PushID(i);

            bool isSelected = track == selectedTrack;
            string label = string.IsNullOrEmpty(track.path) ? "(unbound)" : track.path;
            if (ImGui.Selectable($"{label}##track", isSelected))
            {
                if (selectedTrack != track)
                {
                    selectedTrack = track;
                    selectedKeyframe = null;
                }
            }
            if (ImGui.BeginPopupContextItem("trackctx"))
            {
                if (ImGui.MenuItem("Delete"))
                {
                    clip.tracks.RemoveAt(i);
                    if (selectedTrack == track) { selectedTrack = null; selectedKeyframe = null; }
                    ImGui.EndPopup();
                    ImGui.PopID();
                    return;
                }
                ImGui.EndPopup();
            }
            ImGui.PopID();
        }
    }

    // ---- Add Property menu ----------------------------------------------------------------

    /// <summary>
    /// Recursive menu builder. At each level we present, in order:
    ///   1. Submenus for each child of the current drawable (recursively populated).
    ///      Unnamed children are shown disabled, because a track path can't address them.
    ///   2. A separator.
    ///   3. Leaf items for each themable property on the current drawable.
    /// </summary>
    private void DrawAddTrackMenu(AnimationClip clip, UIDrawable current, string parentPath)
    {
        // 1) Children first.
        bool hasAnyChildren = false;
        if (current is UIGroup group && group.children.Count > 0)
        {
            for (int i = 0; i < group.children.Count; i++)
            {
                UIDrawable child = group.children[i];
                hasAnyChildren = true;

                if (string.IsNullOrEmpty(child.name))
                {
                    // Unnamed → not addressable. Show but disable so the user understands
                    // they need to name it first.
                    ImGui.BeginDisabled();
                    ImGui.MenuItem($"(unnamed {child.GetType().Name})");
                    ImGui.EndDisabled();
                    continue;
                }

                string childLabel = $"{child.name} ({child.GetType().Name})";
                ImGui.PushID(i);
                if (ImGui.BeginMenu(childLabel))
                {
                    string childPath = string.IsNullOrEmpty(parentPath)
                        ? child.name
                        : parentPath + "/" + child.name;
                    DrawAddTrackMenu(clip, child, childPath);
                    ImGui.EndMenu();
                }
                ImGui.PopID();
            }
        }

        // 2) Separator between children and own properties (only if both exist).
        List<PropertyEntry> ownProperties = EnumerateThemableProperties(current.GetType()).ToList();
        if (hasAnyChildren && ownProperties.Count > 0)
        {
            ImGui.Separator();
        }

        // 3) Own properties.
        foreach (PropertyEntry entry in ownProperties)
        {
            string basePath = string.IsNullOrEmpty(parentPath)
                ? entry.Name
                : parentPath + "/" + entry.Name;

            string[] subFields = GetAnimatableSubFields(entry.ValueType);
            bool wholeAnimatable = Interpolator.IsRegistered(entry.ValueType);

            if (subFields.Length == 0 && wholeAnimatable)
            {
                if (ImGui.MenuItem(entry.Name))
                {
                    AddTrack(clip, basePath);
                    ImGui.CloseCurrentPopup();
                }
            }
            else if (subFields.Length > 0)
            {
                if (ImGui.BeginMenu(entry.Name))
                {
                    if (wholeAnimatable && ImGui.MenuItem($"{entry.Name} (whole)"))
                    {
                        AddTrack(clip, basePath);
                        ImGui.CloseCurrentPopup();
                    }
                    foreach (string sub in subFields)
                    {
                        if (ImGui.MenuItem(sub))
                        {
                            AddTrack(clip, basePath + "." + sub);
                            ImGui.CloseCurrentPopup();
                        }
                    }
                    ImGui.EndMenu();
                }
            }
            else
            {
                // Themable but no known interpolator and no usable sub-fields. Greyed.
                ImGui.BeginDisabled();
                ImGui.MenuItem($"{entry.Name} ({entry.ValueType.Name})");
                ImGui.EndDisabled();
            }
        }
    }

    private void AddTrack(AnimationClip clip, string path)
    {
        AnimationTrack track = new() { path = path };
        clip.tracks.Add(track);
        selectedTrack = track;
        selectedKeyframe = null;
    }

    private readonly struct PropertyEntry
    {
        public PropertyEntry(string name, Type valueType) { Name = name; ValueType = valueType; }
        public string Name { get; }
        public Type ValueType { get; }
    }

    private static IEnumerable<PropertyEntry> EnumerateThemableProperties(Type type)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        // Fields
        foreach (FieldInfo f in type.GetFields(flags))
        {
            if (HasThemable(f))
                yield return new PropertyEntry(f.Name, f.FieldType);
        }
        // Properties (rare in your codebase but harmless to support)
        foreach (PropertyInfo p in type.GetProperties(flags))
        {
            if (p.GetIndexParameters().Length > 0) continue;
            if (HasThemable(p))
                yield return new PropertyEntry(p.Name, p.PropertyType);
        }
    }

    private static bool HasThemable(MemberInfo m)
    {
        foreach (var a in m.GetCustomAttributes(inherit: true))
            if (a is ThemableAttribute) return true;
        return false;
    }

    /// <summary>
    /// For a property value type, return the names of sub-fields we want to offer as separate
    /// animation targets. Vectors → X/Y/Z/W, Color4 → Red/Green/Blue/Alpha. Returns an empty
    /// array for types where the whole value is the only sensible target.
    /// </summary>
    private static string[] GetAnimatableSubFields(Type valueType)
    {
        if (valueType == typeof(Vector2)) return ["X", "Y"];
        if (valueType == typeof(Vector3)) return ["X", "Y", "Z"];
        if (valueType == typeof(Vector4)) return ["X", "Y", "Z", "W"];
        if (valueType == typeof(Color4)) return ["Red", "Green", "Blue", "Alpha"];
        return [];
    }
}