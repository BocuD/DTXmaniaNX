using System.Numerics;
using System.Reflection;
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

        ImGui.SetNextWindowSize(new Vector2(900, 500), ImGuiCond.FirstUseEver);
        bool windowOpen = timelineWindowOpen;
        if (ImGui.Begin($"Animation Timeline — {selectedClip.name}", ref windowOpen))
        {
            DrawTimelineToolbar(animator);
            ImGui.Separator();
            DrawAddTrackToolbarRow(root);

            // Body: a frozen-label-column-plus-scrolling-content split. The label column
            // child handles the per-track widgets (+ button, label, selection click). The
            // content child handles the ruler, keyframe dots, and timeline interactions.
            // Vertical scroll is synced between the two so rows stay aligned.
            float reserveForInspector = ComputeInspectorPaneHeight();
            DrawTimelineSplitBody(animator, root, reserveForInspector);

            ImGui.Separator();
            DrawSelectionInspectorPane(root);
        }
        ImGui.End();
        timelineWindowOpen = windowOpen;
    }

    /// <summary>
    /// Compact toolbar row between the main toolbar and the scrolling timeline body. Holds the
    /// "Add Property" button which opens the same recursive property-picker menu the inspector
    /// used to expose. Placed outside the scroll region so it stays visible.
    /// </summary>
    private void DrawAddTrackToolbarRow(UIGroup root)
    {
        if (selectedClip == null) return;

        if (ImGui.Button("Add Property"))
        {
            ImGui.OpenPopup("addtrackpopup");
        }
        if (ImGui.BeginPopup("addtrackpopup"))
        {
            DrawAddTrackMenu(selectedClip, root, parentPath: "");
            ImGui.EndPopup();
        }

        ImGui.SameLine();
        ImGui.TextDisabled($"{selectedClip.tracks.Count} track(s)");
    }

    /// <summary>
    /// Reserve enough space for whichever inspector pane will render below the body — keyframe
    /// inspector if a keyframe is selected, otherwise a smaller track inspector if a track is
    /// selected, otherwise nothing.
    /// </summary>
    private float ComputeInspectorPaneHeight()
    {
        if (selectedClip == null) return 0f;
        if (selectedKeyframe != null && selectedTrack != null
            && selectedTrack.keyframes.Contains(selectedKeyframe))
        {
            return 220f;
        }
        if (selectedTrack != null) return 80f;
        return 0f;
    }

    private void DrawSelectionInspectorPane(UIGroup root)
    {
        // Keyframe inspector takes priority — it already includes the track's value editor.
        if (selectedKeyframe != null && selectedTrack != null
            && selectedTrack.keyframes.Contains(selectedKeyframe))
        {
            DrawKeyframeInspector(root);
            return;
        }
        if (selectedTrack != null)
        {
            DrawTrackInspector();
        }
    }

    private void DrawTrackInspector()
    {
        if (selectedTrack == null) return;
        ImGui.Text("Track");
        string path = selectedTrack.path;
        if (ImGui.InputText("Path", ref path, 256))
        {
            selectedTrack.path = path;
            selectedTrack.Invalidate();
        }
        ImGui.TextDisabled($"Keyframes: {selectedTrack.keyframes.Count}");
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

    // Shared layout constants used by both the label column and the content area.
    private const float LabelColumnWidth = 200f;
    private const float TrackRowHeight = 24f;
    private const float RulerHeight = 28f;
    private const float DotRadius = 7f;
    private const float DotHitRadius = 10f;

    private const float TimelineEdgePadding = 12f;

    /// <summary>
    /// Renders the timeline body as two side-by-side children: a frozen label column on the
    /// left and a scrolling content area on the right. Vertical scroll position is synced
    /// between the two so rows stay aligned; horizontal scroll only affects the content side.
    /// </summary>
    private void DrawTimelineSplitBody(Animator animator, UIGroup root, float reserveForInspector)
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

        float duration = MathF.Max(selectedClip.duration, 0.0001f);
        float contentWidth = duration * timelinePixelsPerSecond;
        int trackCount = Math.Max(selectedClip.tracks.Count, 1);
        float bodyContentHeight = RulerHeight + 2f + trackCount * TrackRowHeight + 8f;

        // The body's overall vertical extent: fill remaining vertical space minus what the
        // selection inspector pane will use below.
        float availY = -reserveForInspector;

        // --- Labels child (fixed width, scrollY synced) ----------------------------------
        if (ImGui.BeginChild("timeline_labels", new Vector2(LabelColumnWidth, availY),
                ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar))
        {
            SyncChildScroll(ref labelsLastSetScroll);
            DrawTimelineLabelChild(animator, root, bodyContentHeight);
        }
        ImGui.EndChild();

        ImGui.SameLine(0f, 0f);

        // --- Content child (remaining width, both scrollbars) ----------------------------
        if (ImGui.BeginChild("timeline_content", new Vector2(0f, availY),
                ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar))
        {
            SyncChildScroll(ref contentLastSetScroll);
            DrawTimelineContentChild(animator, root, contentWidth, bodyContentHeight);
        }
        ImGui.EndChild();
    }

    /// <summary>
    /// Sync helper called inside each child's BeginChild block. Compares the child's current
    /// scroll to what we last set on it — if they differ, the user wheeled or dragged the
    /// scrollbar this frame, so adopt that value as authoritative. Then force the synced
    /// value back onto the child (a no-op if we just adopted) and remember it for next frame.
    /// </summary>
    private void SyncChildScroll(ref float lastSetForThisChild)
    {
        float current = ImGui.GetScrollY();
        if (MathF.Abs(current - lastSetForThisChild) > 0.5f)
        {
            // User-driven change since the last frame.
            syncedScrollY = current;
        }
        ImGui.SetScrollY(syncedScrollY);
        lastSetForThisChild = syncedScrollY;
    }

    /// <summary>
    /// Renders the label column for each track row: "+" button, clickable label area for
    /// selection, right-click context menu for delete. Lives inside its own scrolling child.
    /// </summary>
    private void DrawTimelineLabelChild(Animator animator, UIGroup root, float bodyContentHeight)
    {
        if (selectedClip == null) return;

        // Snap origin to integer pixels — ImGui rasterizes drawlist primitives to nearest pixel
        // but interactive item bounds are floats, so any fractional Y here causes the hover
        // tint, the row backgrounds, and the InvisibleButton's hit region to drift relative to
        // each other by ~1 pixel. Rounding here makes them all agree on the same pixel rows.
        Vector2 cursor = ImGui.GetCursorScreenPos();
        Vector2 origin = new(MathF.Round(cursor.X), MathF.Round(cursor.Y));
        var drawList = ImGui.GetWindowDrawList();

        // Reserve the full virtual height so the child's scrollable area matches the content
        // side. The label column doesn't paint a ruler, but we still skip the same vertical
        // gap so labels line up with the first track in the content child.
        ImGui.Dummy(new Vector2(LabelColumnWidth, bodyContentHeight));

        float tracksTop = MathF.Round(origin.Y + RulerHeight + 2f);

        // Row backgrounds — alternating stripes plus a selection tint matching the content side.
        for (int i = 0; i < selectedClip.tracks.Count; i++)
        {
            AnimationTrack track = selectedClip.tracks[i];
            float rowTop = tracksTop + i * TrackRowHeight;
            Vector2 rowMin = new(origin.X, rowTop);
            Vector2 rowMax = new(origin.X + LabelColumnWidth, rowTop + TrackRowHeight);
            uint bg = (uint)((i & 1) == 0 ? 0x10FFFFFF : 0x18FFFFFF);
            if (track == selectedTrack) bg = 0x40FFFF00u;
            drawList.AddRectFilled(rowMin, rowMax, bg);
        }

        // Per-row widgets — "+" button + clickable label area, both as InvisibleButtons with
        // manually-drawn visuals. This avoids SmallButton's CurrentLineTextBaseOffset shift
        // and Selectable's internal padding, both of which broke alignment with the row tint.
        const float plusButtonSize = 16f;
        const float plusPadX = 4f;
        int? trackToDelete = null;
        for (int i = 0; i < selectedClip.tracks.Count; i++)
        {
            AnimationTrack track = selectedClip.tracks[i];
            float rowTop = tracksTop + i * TrackRowHeight;

            ImGui.PushID(i);

            // "+" button — manually rendered so we get pixel-perfect placement and a hit
            // region that's identical to the visual rect.
            float plusX = origin.X + plusPadX;
            float plusY = rowTop + (TrackRowHeight - plusButtonSize) * 0.5f;
            ImGui.SetCursorScreenPos(new Vector2(plusX, plusY));
            ImGui.InvisibleButton("plus", new Vector2(plusButtonSize, plusButtonSize));
            bool plusHovered = ImGui.IsItemHovered();
            bool plusActive = ImGui.IsItemActive();
            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            {
                AddKeyframeAtScrub(track, animator, root);
            }
            if (plusHovered) ImGui.SetTooltip("Add keyframe at current scrub time using the property's current value");

            // Draw the "+" visual: a rounded rect filled in the standard button colors, with a
            // centered glyph.
            uint plusFill = plusActive
                ? ImGui.GetColorU32(ImGuiCol.ButtonActive)
                : (plusHovered
                    ? ImGui.GetColorU32(ImGuiCol.ButtonHovered)
                    : ImGui.GetColorU32(ImGuiCol.Button));
            Vector2 plusMin = new(plusX, plusY);
            Vector2 plusMax = new(plusX + plusButtonSize, plusY + plusButtonSize);
            drawList.AddRectFilled(plusMin, plusMax, plusFill, 3f);
            Vector2 plusTextSize = ImGui.CalcTextSize("+");
            drawList.AddText(
                new Vector2(plusMin.X + (plusButtonSize - plusTextSize.X) * 0.5f,
                            plusMin.Y + (plusButtonSize - plusTextSize.Y) * 0.5f),
                ImGui.GetColorU32(ImGuiCol.Text), "+");

            // Clickable label area — InvisibleButton sized exactly to the row, with manual
            // hover tint and label text via drawlist.
            float labelX = plusMax.X + 6f;
            float labelW = MathF.Max(20f, origin.X + LabelColumnWidth - labelX);
            ImGui.SetCursorScreenPos(new Vector2(labelX, rowTop));
            ImGui.InvisibleButton("trackrow", new Vector2(labelW, TrackRowHeight));
            if (ImGui.IsItemHovered())
            {
                drawList.AddRectFilled(new Vector2(labelX, rowTop),
                    new Vector2(labelX + labelW, rowTop + TrackRowHeight),
                    0x20FFFFFFu);
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            {
                if (selectedTrack != track)
                {
                    selectedTrack = track;
                    selectedKeyframe = null;
                }
            }
            string label = string.IsNullOrEmpty(track.path) ? "(unbound)" : track.path;
            drawList.AddText(
                new Vector2(labelX + 2f, MathF.Round(rowTop + (TrackRowHeight - ImGui.GetTextLineHeight()) * 0.5f)),
                0xFFFFFFFF, label);
            if (ImGui.BeginPopupContextItem("trackrowctx"))
            {
                if (ImGui.MenuItem("Delete"))
                {
                    trackToDelete = i;
                }
                ImGui.EndPopup();
            }

            ImGui.PopID();
        }

        if (trackToDelete is int idx)
        {
            AnimationTrack t = selectedClip.tracks[idx];
            selectedClip.tracks.RemoveAt(idx);
            if (selectedTrack == t) { selectedTrack = null; selectedKeyframe = null; }
        }
    }

    /// <summary>
    /// Renders the scrolling content area: ruler, per-track lanes, keyframe dots, playhead,
    /// plus the InvisibleButton that captures clicks and drags for scrubbing, keyframe
    /// selection/movement, and click-to-insert.
    /// </summary>
    private void DrawTimelineContentChild(Animator animator, UIGroup root, float contentWidth, float bodyContentHeight)
    {
        if (selectedClip == null) return;

        float duration = MathF.Max(selectedClip.duration, 0.0001f);
        // Match the label child's pixel-snapping policy so rows on either side land on the
        // same scanline. Without this, the row-tint rect, the ruler bottom border, and the
        // invisible hit-test region can each rasterize to slightly different pixels.
        Vector2 cursor = ImGui.GetCursorScreenPos();
        Vector2 origin = new(MathF.Round(cursor.X), MathF.Round(cursor.Y));
        var drawList = ImGui.GetWindowDrawList();

        // The reservable width includes padding on both ends so keyframe dots at t=0 and
        // t=duration don't get clipped by the child's edges.
        float fullWidth = contentWidth + 2f * TimelineEdgePadding;
        ImGui.Dummy(new Vector2(fullWidth, bodyContentHeight));

        Vector2 contentMin = origin;
        // The X coordinate at which t=0 should land. Everything that maps a time value to a
        // pixel — ruler ticks, keyframe dots, playhead — uses this as its anchor. Anything
        // that spans the full visual width (row tints, ruler background, hit area) uses
        // contentMin.X / fullWidth instead.
        float timeOriginX = MathF.Round(origin.X + TimelineEdgePadding);
        float tracksTop = MathF.Round(origin.Y + RulerHeight + 2f);
        Vector2 tracksOrigin = new(origin.X, tracksTop);

        // --- Row backgrounds (content side) -----------------------------------------------
        for (int i = 0; i < selectedClip.tracks.Count; i++)
        {
            AnimationTrack track = selectedClip.tracks[i];
            float rowTop = tracksTop + i * TrackRowHeight;
            Vector2 rowMin = new(tracksOrigin.X, rowTop);
            Vector2 rowMax = new(rowMin.X + fullWidth, rowTop + TrackRowHeight);
            uint bg = (uint)((i & 1) == 0 ? 0x10FFFFFF : 0x18FFFFFF);
            if (track == selectedTrack) bg = 0x40FFFF00u;
            drawList.AddRectFilled(rowMin, rowMax, bg);
        }

        // --- Invisible hit area covers ruler + track lanes --------------------------------
        ImGui.SetCursorScreenPos(contentMin);
        ImGui.InvisibleButton("timeline_content_hit",
            new Vector2(fullWidth, RulerHeight + selectedClip.tracks.Count * TrackRowHeight + 2f));
        bool contentHovered = ImGui.IsItemHovered();
        Vector2 mouse = ImGui.GetMousePos();

        // --- Ruler ------------------------------------------------------------------------
        Vector2 rulerMin = contentMin;
        Vector2 rulerMax = new(rulerMin.X + fullWidth, rulerMin.Y + RulerHeight);
        drawList.AddRectFilled(rulerMin, rulerMax, 0x30FFFFFF);
        for (float t = 0f; t <= duration + 0.0001f; t += 0.25f)
        {
            float x = timeOriginX + t * timelinePixelsPerSecond;
            bool major = MathF.Abs(t - MathF.Round(t)) < 0.001f;
            drawList.AddLine(new Vector2(x, rulerMin.Y), new Vector2(x, rulerMax.Y), major ? 0xFFAAAAAA : 0x60AAAAAA);
            if (major)
            {
                drawList.AddText(new Vector2(x + 2, rulerMin.Y + 2), 0xFFCCCCCC, $"{t:0.##}s");
            }
        }
        drawList.AddLine(new Vector2(rulerMin.X, rulerMax.Y), new Vector2(rulerMax.X, rulerMax.Y), 0xFF666666);

        // --- Keyframe dots + hover detection ---------------------------------------------
        Keyframe? hoveredKeyframe = null;
        AnimationTrack? hoveredKeyframeTrack = null;
        int hoveredTrackIndex = -1;
        bool rulerHovered = false;

        if (contentHovered)
        {
            rulerHovered = mouse.Y >= rulerMin.Y && mouse.Y < rulerMax.Y;
            if (!rulerHovered)
            {
                int idx = (int)((mouse.Y - tracksOrigin.Y) / TrackRowHeight);
                if (idx >= 0 && idx < selectedClip.tracks.Count)
                {
                    hoveredTrackIndex = idx;
                }
            }
        }

        // First pass: find the nearest keyframe under the hit radius across all tracks. We
        // compare squared distances so two near-overlapping dots resolve to whichever is
        // genuinely closest to the cursor rather than whichever happened to be earliest.
        float bestDistSq = DotHitRadius * DotHitRadius;
        for (int i = 0; i < selectedClip.tracks.Count; i++)
        {
            if (!contentHovered || hoveredTrackIndex != i) continue;
            AnimationTrack track = selectedClip.tracks[i];
            float rowTop = tracksOrigin.Y + i * TrackRowHeight;
            float dotY = rowTop + TrackRowHeight * 0.5f;

            foreach (Keyframe kf in track.keyframes)
            {
                float x = timeOriginX + kf.time * timelinePixelsPerSecond;
                float dx = mouse.X - x;
                float dy = mouse.Y - dotY;
                float distSq = dx * dx + dy * dy;
                if (distSq <= bestDistSq)
                {
                    bestDistSq = distSq;
                    hoveredKeyframe = kf;
                    hoveredKeyframeTrack = track;
                }
            }
        }

        // Second pass: render.
        for (int i = 0; i < selectedClip.tracks.Count; i++)
        {
            AnimationTrack track = selectedClip.tracks[i];
            float rowTop = tracksOrigin.Y + i * TrackRowHeight;
            float dotY = rowTop + TrackRowHeight * 0.5f;

            foreach (Keyframe kf in track.keyframes)
            {
                float x = timeOriginX + kf.time * timelinePixelsPerSecond;
                bool isSel = kf == selectedKeyframe;
                bool isHov = kf == hoveredKeyframe;
                uint fill = isSel ? 0xFFFFFF00u : (isHov ? 0xFF66DDFFu : 0xFF00CCFFu);
                drawList.AddCircleFilled(new Vector2(x, dotY), DotRadius, fill);
                drawList.AddCircle(new Vector2(x, dotY), DotRadius, 0xFF000000, 0, 1.5f);
            }
        }

        // --- Playhead ---------------------------------------------------------------------
        float playheadTime = animator.isPlaying && animator.currentClip == selectedClip ? animator.time : scrubTime;
        float playheadX = timeOriginX + playheadTime * timelinePixelsPerSecond;
        float playheadBottomY = tracksOrigin.Y + selectedClip.tracks.Count * TrackRowHeight;
        drawList.AddLine(new Vector2(playheadX, rulerMin.Y), new Vector2(playheadX, playheadBottomY + 4f), 0xFFFF4444, 2f);
        drawList.AddTriangleFilled(
            new Vector2(playheadX - 5f, rulerMin.Y),
            new Vector2(playheadX + 5f, rulerMin.Y),
            new Vector2(playheadX, rulerMin.Y + 8f),
            0xFFFF4444);

        // --- Interactions ----------------------------------------------------------------
        HandleTimelineInteractions(
            animator, root, contentHovered, rulerHovered, mouse,
            tracksOrigin, timeOriginX, duration,
            hoveredKeyframe, hoveredKeyframeTrack, hoveredTrackIndex);
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
        Vector2 tracksOrigin, float timeOriginX, float duration,
        Keyframe? hoveredKeyframe, AnimationTrack? hoveredKeyframeTrack, int hoveredTrackIndex)
    {
        // Convert mouse X to a clamped clip time. The anchor is timeOriginX (where t=0 sits
        // visually), not the child's left edge, so clicks inside the left edge padding map
        // to t=0 rather than to negative times.
        float MouseTime() => Math.Clamp(
            (mouse.X - timeOriginX) / timelinePixelsPerSecond,
            0f, duration);

        // ---- End drags on mouse release ---------------------------------------------------
        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            if (draggingKeyframe != null && draggingTrack != null)
            {
                // Collision check: if another keyframe on this track shares the new time
                // (within half a frame, the snap quantum), revert to the start position
                // rather than silently stacking two keyframes at the same time. Half-frame
                // tolerance is safe because both times are already frame-snapped — equal
                // frames produce identical floats and differ-by-one frames are a full frame
                // apart.
                float collisionEpsilon = selectedClip is { frameRate: > 0f } c
                    ? 0.5f / c.frameRate
                    : 1e-4f;

                bool collision = false;
                foreach (Keyframe other in draggingTrack.keyframes)
                {
                    if (other == draggingKeyframe) continue;
                    if (MathF.Abs(other.time - draggingKeyframe.time) < collisionEpsilon)
                    {
                        collision = true;
                        break;
                    }
                }

                if (collision)
                {
                    draggingKeyframe.time = dragStartTime;
                    ShowStatus("Reverted: another keyframe is at that time");
                }

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
                    dragStartTime = hoveredKeyframe.time;
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

    // =========================================================================================
    // Add Property menu + helpers — moved from the inspector since track management now lives here.
    // =========================================================================================

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