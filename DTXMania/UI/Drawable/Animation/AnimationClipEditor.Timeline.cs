using System.Numerics;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Animation.Editor;

public sealed partial class AnimationClipEditor
{
    /// <summary>
    /// Draws the floating timeline window if it's open. Call this every frame from
    /// Animator.DrawInspector — ImGui won't render anything if the open flag is false. Safe
    /// to call from anywhere; opens as a top-level window regardless of where Begin is called.
    /// </summary>
    public void DrawTimelineWindow(Animator animator, UIGroup root)
    {
        if (!timelineWindowOpen) return;
        if (selectedClip == null)
        {
            // Still draw a small placeholder so the user can see why nothing's there.
            bool open = timelineWindowOpen;
            if (ImGui.Begin("Animation Timeline", ref open))
            {
                ImGui.TextDisabled("Select a clip in the inspector.");
            }
            ImGui.End();
            timelineWindowOpen = open;
            return;
        }

        ImGui.SetNextWindowSize(new Vector2(700, 280), ImGuiCond.FirstUseEver);
        bool windowOpen = timelineWindowOpen;
        if (ImGui.Begin($"Animation Timeline — {selectedClip.name}", ref windowOpen))
        {
            DrawTimelineToolbar(animator);
            ImGui.Separator();
            DrawTimelineBody(animator, root);
            DrawKeyframeInspector(root);
        }
        ImGui.End();
        timelineWindowOpen = windowOpen;
    }

    private void DrawTimelineToolbar(Animator animator)
    {
        if (ImGui.Button(animator.isPlaying ? "Pause" : "Play"))
        {
            if (animator.isPlaying)
            {
                animator.Pause();
            }
            else if (animator.currentClip == selectedClip)
            {
                previewEnabled = true;
                animator.Resume();
            }
            else if (selectedClip != null)
            {
                previewEnabled = true;
                animator.Play(selectedClip.name);
            }
        }
        ImGui.SameLine();
        if (ImGui.Button("Stop")) animator.Stop();

        ImGui.SameLine();
        bool wasPreviewEnabled = previewEnabled;
        if (ImGui.Checkbox("Preview", ref previewEnabled))
        {
            // Toggling preview off pauses any active playback so the animator stops writing
            // to the scene. Toggling it back on leaves the animator paused — user pressing
            // Play again is the intentional resume.
            if (wasPreviewEnabled && !previewEnabled && animator.isPlaying)
            {
                animator.Pause();
            }
        }

        // --- Frame stepping & counter --------------------------------------------------
        // Step buttons pause playback first (we can't sensibly step while it's running) and
        // move the scrubber by one frame at the clip's frameRate. The counter shows the
        // current frame derived from whichever time the playhead is displaying.
        if (selectedClip != null && selectedClip.frameRate > 0f)
        {
            ImGui.SameLine();
            ImGui.Text("|");
            ImGui.SameLine();

            float frameDuration = 1f / selectedClip.frameRate;
            float playheadTime = animator.isPlaying && animator.currentClip == selectedClip
                ? animator.time
                : scrubTime;
            int currentFrame = (int)MathF.Round(playheadTime * selectedClip.frameRate);
            int totalFrames = (int)MathF.Round(selectedClip.duration * selectedClip.frameRate);

            if (ImGui.SmallButton("◀##prevframe"))
            {
                if (animator.isPlaying) animator.Pause();
                scrubTime = MathF.Max(0f, (currentFrame - 1) * frameDuration);
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Previous frame");

            ImGui.SameLine();
            ImGui.Text($"{currentFrame} / {totalFrames}");

            ImGui.SameLine();
            if (ImGui.SmallButton("▶##nextframe"))
            {
                if (animator.isPlaying) animator.Pause();
                scrubTime = MathF.Min(selectedClip.duration, (currentFrame + 1) * frameDuration);
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Next frame");
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(140f);
        ImGui.SliderFloat("Zoom (px/s)", ref timelinePixelsPerSecond, 20f, 2000f);

        if (selectedClip != null)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(60f);
            float fr = selectedClip.frameRate;
            if (ImGui.InputFloat("FPS", ref fr, 0f, 0f, "%.0f"))
            {
                selectedClip.frameRate = MathF.Max(1f, fr);
            }
        }
    }

    private void DrawTimelineBody(Animator animator, UIGroup root)
    {
        if (selectedClip == null) return;

        // Live-scrub evaluation. Disabled when the preview toggle is off, so the user can
        // edit property values manually without the animator overwriting them on the next
        // frame. (Playback evaluation runs from UIGroup.Draw — we pause the animator when
        // preview is off so it can't write either; see toolbar checkbox handler.)
        if (previewEnabled && !animator.isPlaying)
        {
            selectedClip.Evaluate(root, scrubTime);
        }

        const float labelColumnWidth = 200f;
        const float trackRowHeight = 24f;
        const float dotRadius = 7f;
        const float dotHitRadius = 10f;
        const float rulerHeight = 28f;

        float duration = MathF.Max(selectedClip.duration, 0.0001f);
        Vector2 origin = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList();

        float contentWidth = duration * timelinePixelsPerSecond;
        float totalWidth = labelColumnWidth + contentWidth + 8f;
        float totalHeight = rulerHeight + 2f + MathF.Max(selectedClip.tracks.Count, 1) * trackRowHeight + 8f;

        // Reserve total layout area first so other widgets below the timeline land in the right
        // place. We'll position ImGui widgets into this region using SetCursorScreenPos and
        // place an InvisibleButton over the content area for hit-testing.
        ImGui.Dummy(new Vector2(totalWidth, totalHeight));

        Vector2 tracksOrigin = new(origin.X, origin.Y + rulerHeight + 2f);
        Vector2 contentMin = new(origin.X + labelColumnWidth, origin.Y);

        // --- Row backgrounds (drawlist) ----------------------------------------------------
        for (int i = 0; i < selectedClip.tracks.Count; i++)
        {
            AnimationTrack track = selectedClip.tracks[i];
            Vector2 rowMin = new(tracksOrigin.X, tracksOrigin.Y + i * trackRowHeight);
            Vector2 rowMax = new(rowMin.X + labelColumnWidth + contentWidth, rowMin.Y + trackRowHeight);
            uint bg = (uint)((i & 1) == 0 ? 0x10FFFFFF : 0x18FFFFFF);
            if (track == selectedTrack) bg = 0x40FFFF00u;
            drawList.AddRectFilled(rowMin, rowMax, bg);
        }

        // --- Label column buttons (real ImGui widgets) -------------------------------------
        // For each track, place a "+" button followed by the path label. Clicking "+" inserts
        // a keyframe at the current scrub time using the live property value.
        for (int i = 0; i < selectedClip.tracks.Count; i++)
        {
            AnimationTrack track = selectedClip.tracks[i];
            float rowTop = tracksOrigin.Y + i * trackRowHeight;

            // Vertically center the button within the row by offsetting from rowTop.
            float buttonOffsetY = (trackRowHeight - ImGui.GetFrameHeight()) * 0.5f;
            ImGui.SetCursorScreenPos(new Vector2(tracksOrigin.X + 4, rowTop + buttonOffsetY));

            ImGui.PushID(i);
            if (ImGui.SmallButton("+"))
            {
                AddKeyframeAtScrub(track, animator, root);
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Add keyframe at current scrub time using the property's current value");
            ImGui.PopID();

            // Label text — drawn after the button to sit alongside it.
            string label = string.IsNullOrEmpty(track.path) ? "(unbound)" : track.path;
            float labelX = ImGui.GetItemRectMax().X + 6f;
            drawList.AddText(new Vector2(labelX, rowTop + (trackRowHeight - ImGui.GetTextLineHeight()) * 0.5f),
                0xFFFFFFFF, label);
        }

        // --- Invisible hit area over content (ruler + track lanes, right of label col) -----
        ImGui.SetCursorScreenPos(contentMin);
        ImGui.InvisibleButton("timeline_content", new Vector2(contentWidth, rulerHeight + selectedClip.tracks.Count * trackRowHeight + 2f));
        bool contentHovered = ImGui.IsItemHovered();
        Vector2 mouse = ImGui.GetMousePos();

        // --- Ruler -------------------------------------------------------------------------
        Vector2 rulerMin = contentMin;
        Vector2 rulerMax = new(rulerMin.X + contentWidth, rulerMin.Y + rulerHeight);
        drawList.AddRectFilled(rulerMin, rulerMax, 0x30FFFFFF);
        for (float t = 0f; t <= duration + 0.0001f; t += 0.25f)
        {
            float x = rulerMin.X + t * timelinePixelsPerSecond;
            bool major = MathF.Abs(t - MathF.Round(t)) < 0.001f;
            drawList.AddLine(new Vector2(x, rulerMin.Y), new Vector2(x, rulerMax.Y), major ? 0xFFAAAAAA : 0x60AAAAAA);
            if (major)
            {
                drawList.AddText(new Vector2(x + 2, rulerMin.Y + 2), 0xFFCCCCCC, $"{t:0.##}s");
            }
        }
        // Ruler bottom border so it's visually distinct from the tracks below.
        drawList.AddLine(new Vector2(rulerMin.X, rulerMax.Y), new Vector2(rulerMax.X, rulerMax.Y), 0xFF666666);

        // --- Keyframe dots + hover detection -----------------------------------------------
        Keyframe? hoveredKeyframe = null;
        AnimationTrack? hoveredKeyframeTrack = null;
        int hoveredTrackIndex = -1;
        bool rulerHovered = false;

        if (contentHovered)
        {
            rulerHovered = mouse.Y >= rulerMin.Y && mouse.Y < rulerMax.Y;
            if (!rulerHovered)
            {
                int idx = (int)((mouse.Y - tracksOrigin.Y) / trackRowHeight);
                if (idx >= 0 && idx < selectedClip.tracks.Count)
                {
                    hoveredTrackIndex = idx;
                }
            }
        }

        for (int i = 0; i < selectedClip.tracks.Count; i++)
        {
            AnimationTrack track = selectedClip.tracks[i];
            float rowTop = tracksOrigin.Y + i * trackRowHeight;
            float dotY = rowTop + trackRowHeight * 0.5f;

            foreach (Keyframe kf in track.keyframes)
            {
                float x = contentMin.X + kf.time * timelinePixelsPerSecond;

                if (contentHovered && hoveredKeyframe == null && hoveredTrackIndex == i)
                {
                    float dx = mouse.X - x;
                    float dy = mouse.Y - dotY;
                    if (dx * dx + dy * dy <= dotHitRadius * dotHitRadius)
                    {
                        hoveredKeyframe = kf;
                        hoveredKeyframeTrack = track;
                    }
                }

                bool isSel = kf == selectedKeyframe;
                bool isHov = kf == hoveredKeyframe;
                uint fill = isSel ? 0xFFFFFF00u : (isHov ? 0xFF66DDFFu : 0xFF00CCFFu);
                drawList.AddCircleFilled(new Vector2(x, dotY), dotRadius, fill);
                drawList.AddCircle(new Vector2(x, dotY), dotRadius, 0xFF000000, 0, 1.5f);
            }
        }

        // --- Playhead ----------------------------------------------------------------------
        float playheadTime = animator.isPlaying && animator.currentClip == selectedClip ? animator.time : scrubTime;
        float playheadX = contentMin.X + playheadTime * timelinePixelsPerSecond;
        float playheadBottomY = tracksOrigin.Y + selectedClip.tracks.Count * trackRowHeight;
        drawList.AddLine(new Vector2(playheadX, rulerMin.Y), new Vector2(playheadX, playheadBottomY + 4f), 0xFFFF4444, 2f);
        // Playhead handle in the ruler so it's clear where to grab to scrub.
        drawList.AddTriangleFilled(
            new Vector2(playheadX - 5f, rulerMin.Y),
            new Vector2(playheadX + 5f, rulerMin.Y),
            new Vector2(playheadX, rulerMin.Y + 8f),
            0xFFFF4444);

        // --- Interactions ------------------------------------------------------------------
        HandleTimelineInteractions(
            animator, root, contentHovered, rulerHovered, mouse,
            tracksOrigin, contentMin, contentWidth, duration,
            hoveredKeyframe, hoveredKeyframeTrack, hoveredTrackIndex);

        // Restore cursor below the timeline so subsequent widgets (keyframe inspector) flow
        // normally.
        ImGui.SetCursorScreenPos(new Vector2(origin.X, origin.Y + totalHeight));
    }

    private void AddKeyframeAtScrub(AnimationTrack track, Animator animator, UIGroup root)
    {
        if (selectedClip == null) return;
        if (!track.TryGetCurrentValue(root, out object? value))
        {
            ShowStatus("Cannot add keyframe: track not bound. Check the path.");
            return;
        }

        float playheadTime = animator.isPlaying && animator.currentClip == selectedClip
            ? animator.time
            : scrubTime;
        Keyframe kf = track.InsertKeyframe(SnapTime(playheadTime), value);
        selectedTrack = track;
        selectedKeyframe = kf;
    }

    /// <summary>
    /// Snap a time value to the nearest frame boundary based on the selected clip's frameRate.
    /// Returns the input unchanged if no clip is selected or frameRate isn't positive.
    /// </summary>
    private float SnapTime(float time)
    {
        if (selectedClip == null || selectedClip.frameRate <= 0f) return time;
        float frameDuration = 1f / selectedClip.frameRate;
        return MathF.Round(time / frameDuration) * frameDuration;
    }

    private void HandleTimelineInteractions(
        Animator animator, UIGroup root, bool contentHovered, bool rulerHovered, Vector2 mouse,
        Vector2 tracksOrigin, Vector2 contentMin, float contentWidth, float duration,
        Keyframe? hoveredKeyframe, AnimationTrack? hoveredKeyframeTrack, int hoveredTrackIndex)
    {
        // Convert mouse X to a clamped clip time (in the track area).
        float MouseTime() => Math.Clamp(
            (mouse.X - contentMin.X) / timelinePixelsPerSecond,
            0f, duration);

        // ---- End drags on mouse release ---------------------------------------------------
        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            if (draggingKeyframe != null && draggingTrack != null)
            {
                draggingTrack.SortKeyframes();
                draggingKeyframe = null;
                draggingTrack = null;
            }
            draggingScrubber = false;
        }

        // ---- Active drags (mouse held) ----------------------------------------------------
        // These short-circuit other interactions to keep the active drag stable.
        if (draggingKeyframe != null && draggingTrack != null && ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            draggingKeyframe.time = Math.Clamp(SnapTime(MouseTime()), 0f, duration);
        }
        else if (draggingScrubber && ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            scrubTime = MouseTime();
        }
        else if (contentHovered)
        {
            // ---- Click in ruler → start scrubbing -----------------------------------------
            if (rulerHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                draggingScrubber = true;
                scrubTime = MouseTime();
                // Pause any active playback so the user owns the timeline while scrubbing.
                if (animator.isPlaying) animator.Pause();
            }
            // ---- Click on a keyframe ------------------------------------------------------
            else if (hoveredKeyframe != null && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                // Two-step interaction: clicking a keyframe selects it. Only an already-selected
                // keyframe can be dragged — protects against accidental drags when scanning.
                if (selectedKeyframe == hoveredKeyframe)
                {
                    draggingKeyframe = hoveredKeyframe;
                    draggingTrack = hoveredKeyframeTrack;
                }
                else
                {
                    selectedKeyframe = hoveredKeyframe;
                    selectedTrack = hoveredKeyframeTrack;
                }
            }
            // ---- Right-click on a keyframe → context menu --------------------------------
            else if (hoveredKeyframe != null && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                selectedKeyframe = hoveredKeyframe;
                selectedTrack = hoveredKeyframeTrack;
                ImGui.OpenPopup("keyframectx");
            }
            // ---- Click on empty track area → insert keyframe at that time ----------------
            else if (hoveredKeyframe == null && hoveredTrackIndex >= 0 && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                AnimationTrack track = selectedClip!.tracks[hoveredTrackIndex];
                float t = SnapTime(MouseTime());
                if (track.TryGetCurrentValue(root, out object? value))
                {
                    Keyframe kf = track.InsertKeyframe(t, value);
                    selectedTrack = track;
                    selectedKeyframe = kf;
                }
                else
                {
                    ShowStatus("Cannot add keyframe: track not bound. Check the path.");
                }
            }
        }

        // ---- Keyframe context menu (must be called every frame regardless of hover) ------
        if (ImGui.BeginPopup("keyframectx"))
        {
            if (selectedKeyframe != null && selectedTrack != null)
            {
                if (ImGui.MenuItem("Delete"))
                {
                    selectedTrack.RemoveKeyframe(selectedKeyframe);
                    selectedKeyframe = null;
                }
                if (ImGui.BeginMenu("Easing"))
                {
                    foreach (Easing e in Enum.GetValues<Easing>())
                    {
                        if (ImGui.MenuItem(e.ToString(), selectedKeyframe.easing == e))
                        {
                            selectedKeyframe.easing = e;
                        }
                    }
                    ImGui.EndMenu();
                }
            }
            ImGui.EndPopup();
        }
    }

    /// <summary>
    /// Inspector pane for the currently selected keyframe. Drawn below the timeline. Shows
    /// time, value (typed), easing, and a delete button. Editing the value or time writes
    /// directly to the keyframe; the change is visible immediately because the next Evaluate
    /// uses the new value.
    /// </summary>
    private void DrawKeyframeInspector(UIGroup root)
    {
        if (selectedClip == null || selectedTrack == null || selectedKeyframe == null) return;

        // Invariant: the selected keyframe must belong to the selected track. Some code paths
        // change selectedTrack without also clearing selectedKeyframe — in those cases the
        // value-editor would try to unbox an old keyframe's value as the new track's property
        // type and crash. Clear silently here and bail; selection will re-stabilize on the
        // user's next interaction.
        if (!selectedTrack.keyframes.Contains(selectedKeyframe))
        {
            selectedKeyframe = null;
            return;
        }

        ImGui.Separator();
        ImGui.Text("Keyframe");

        Keyframe kf = selectedKeyframe;
        AnimationTrack track = selectedTrack;

        // Time field — clamp to clip duration, re-sort after edit.
        float t = kf.time;
        if (ImGui.InputFloat("Time", ref t))
        {
            kf.time = Math.Clamp(t, 0f, selectedClip.duration);
            track.SortKeyframes();
        }

        // Value field — typed widget based on the bound property type.
        // Bind the track if necessary so we know what type to draw.
        track.EnsureBound(root);
        Type? valueType = track.BoundValueType;
        // Only attempt to draw the value editor when the keyframe's stored value matches the
        // bound property type. A mismatch shouldn't occur if selection invariants hold, but
        // this is a cheap last-line defense — better an unhelpful label for one frame than a
        // hard crash.
        if (valueType != null && kf.typedValue != null && valueType.IsInstanceOfType(kf.typedValue))
        {
            object current = kf.typedValue;
            if (DrawTypedValueEditor(valueType, ref current))
            {
                track.SetKeyframeValue(kf, current);
            }
        }
        else
        {
            ImGui.TextDisabled("(value type unknown — track not bound)");
        }

        // Easing dropdown.
        Easing easing = kf.easing;
        if (ImGui.BeginCombo("Easing", easing.ToString()))
        {
            foreach (Easing e in Enum.GetValues<Easing>())
            {
                bool isSel = e == easing;
                if (ImGui.Selectable(e.ToString(), isSel))
                {
                    kf.easing = e;
                }
            }
            ImGui.EndCombo();
        }

        if (ImGui.Button("Delete Keyframe"))
        {
            track.RemoveKeyframe(kf);
            selectedKeyframe = null;
        }
    }

    /// <summary>
    /// Type-aware editor for a single value. Returns true if the value changed this frame.
    /// </summary>
    private static bool DrawTypedValueEditor(Type type, ref object value)
    {
        if (type == typeof(float))
        {
            float v = (float)value;
            if (ImGui.InputFloat("Value", ref v)) { value = v; return true; }
            return false;
        }
        if (type == typeof(int))
        {
            int v = (int)value;
            if (ImGui.InputInt("Value", ref v)) { value = v; return true; }
            return false;
        }
        if (type == typeof(bool))
        {
            bool v = (bool)value;
            if (ImGui.Checkbox("Value", ref v)) { value = v; return true; }
            return false;
        }
        if (type == typeof(Vector2))
        {
            Vector2 v = (Vector2)value;
            if (ImGui.InputFloat2("Value", ref v)) { value = v; return true; }
            return false;
        }
        if (type == typeof(Vector3))
        {
            Vector3 v = (Vector3)value;
            if (ImGui.InputFloat3("Value", ref v)) { value = v; return true; }
            return false;
        }
        if (type == typeof(Vector4))
        {
            Vector4 v = (Vector4)value;
            if (ImGui.InputFloat4("Value", ref v)) { value = v; return true; }
            return false;
        }
        if (type == typeof(Color4))
        {
            Color4 c = (Color4)value;
            Vector4 rgba = new(c.Red, c.Green, c.Blue, c.Alpha);
            if (ImGui.ColorEdit4("Value", ref rgba))
            {
                value = new Color4(rgba.X, rgba.Y, rgba.Z, rgba.W);
                return true;
            }
            return false;
        }
        ImGui.TextDisabled($"(no editor for {type.Name})");
        return false;
    }
}