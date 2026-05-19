using Hexa.NET.ImGui;

namespace DTXMania.UI.Animation.Editor;

/// <summary>
/// Editor UI state for one Animator. The class is partial and split across files:
///   - AnimationClipEditor.cs (this file): shared fields and common helpers.
///   - AnimationClipEditor.Inspector.cs: DrawInInspector + everything the inspector needs
///     (clip list, file IO, clip header, track list, property-picker menu).
///   - AnimationClipEditor.Timeline.cs: DrawTimelineWindow + everything the timeline needs
///     (toolbar, body, interactions, keyframe inspector pane).
/// </summary>
public sealed partial class AnimationClipEditor
{
    // ---- Selection / view state ----------------------------------------------------------

    public AnimationClip? selectedClip;
    public AnimationTrack? selectedTrack;
    public Keyframe? selectedKeyframe;
    public float scrubTime;

    // Timeline window toggle + view params.
    public bool timelineWindowOpen;
    public float timelinePixelsPerSecond = 800f;

    /// <summary>
    /// When false, the editor neither scrub-evaluates nor lets the animator play — so the user
    /// can manually edit property values without the animator immediately overwriting them.
    /// </summary>
    public bool previewEnabled = true;

    // Drag-in-progress state for keyframes. While dragging we mutate kf.time on each frame and
    // call SortKeyframes only on mouse release (sorting mid-drag would re-order under us).
    private Keyframe? draggingKeyframe;
    private AnimationTrack? draggingTrack;

    // Scrubber drag (when the user is click-dragging in the ruler area).
    private bool draggingScrubber;

    // File IO state. We remember the last successful save/load path per clip so re-saves
    // can default to that path in the NFD dialog.
    private readonly Dictionary<AnimationClip, string> lastPathByClip = new();
    private string? fileStatusMessage;
    private double fileStatusUntilTime;

    /// <summary>
    /// Show a brief status message in the inspector's file section. Visible for 3 seconds,
    /// then fades. Used by both file IO and timeline operations.
    /// </summary>
    private void ShowStatus(string message)
    {
        fileStatusMessage = message;
        fileStatusUntilTime = ImGui.GetTime() + 3.0;
    }
}
