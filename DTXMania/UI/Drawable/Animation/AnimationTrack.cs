using System.Diagnostics;
using System.Runtime.Serialization;
using DTXMania.UI.Drawable;
using Newtonsoft.Json;

namespace DTXMania.UI.Animation;

/// <summary>
/// A single track targets one property on one drawable, identified by a slash-separated path
/// relative to the animator's root group. The last path segment is the property (and may itself
/// be dot-separated for nested members, e.g. "title/position.X").
/// </summary>
public sealed class AnimationTrack
{
    [JsonProperty("path")] public string path = string.Empty;

    [JsonProperty("keyframes")] public List<Keyframe> keyframes = new();

    // Cached bindings. Cleared by Invalidate() when the tree changes.
    [JsonIgnore] private UIDrawable? cachedTarget;
    [JsonIgnore] private PropertyAccessor? cachedAccessor;
    [JsonIgnore] private bool bindAttempted;
    [JsonIgnore] private bool keyframesConverted;

    public void Invalidate()
    {
        cachedTarget = null;
        cachedAccessor = null;
        bindAttempted = false;
        // Keep keyframesConverted as-is — the typed values are still good if the property type
        // hasn't changed. They'll be re-converted on the next Bind() if the new accessor's
        // ValueType differs.
    }

    // ----------------------------------------------------------------------------------------
    // Editor support: explicit binding, live-value read, keyframe insert/remove/set.
    // These operations let the timeline editor capture and manipulate keyframes without
    // reaching into the private binding fields.
    // ----------------------------------------------------------------------------------------

    /// <summary>
    /// The value type of the bound property, or null if this track hasn't been bound yet.
    /// </summary>
    [JsonIgnore] public Type? BoundValueType => cachedAccessor?.ValueType;

    /// <summary>
    /// Try to resolve the track against the given root without sampling. Returns true if the
    /// track is now bound. Idempotent.
    /// </summary>
    public bool EnsureBound(UIGroup root)
    {
        if (!bindAttempted)
        {
            Bind(root);
        }
        return cachedTarget != null && cachedAccessor != null;
    }

    /// <summary>
    /// Read the current live value of the bound property. Used by the editor to seed a new
    /// keyframe with the property's present-moment value (which after Evaluate is the
    /// interpolated value at the current time, or the user's manually-edited value).
    /// </summary>
    public bool TryGetCurrentValue(UIGroup root, out object? value)
    {
        if (!EnsureBound(root))
        {
            value = null;
            return false;
        }
        try
        {
            value = cachedAccessor!.Getter(cachedTarget!);
            return true;
        }
        catch (Exception e)
        {
            Trace.TraceError($"AnimationTrack.TryGetCurrentValue '{path}': {e.Message}");
            value = null;
            return false;
        }
    }

    /// <summary>
    /// Insert a keyframe at the given time with the given typed value, keeping the list
    /// sorted. If a keyframe already exists at the same time (within a small tolerance) its
    /// value and easing are overwritten instead of adding a duplicate. Returns the resulting
    /// (new or modified) keyframe.
    /// </summary>
    public Keyframe InsertKeyframe(float time, object? typedValue, Easing easing = Easing.Linear)
    {
        const float epsilon = 1e-4f;

        // Look for an existing keyframe at this time.
        for (int i = 0; i < keyframes.Count; i++)
        {
            if (MathF.Abs(keyframes[i].time - time) < epsilon)
            {
                SetKeyframeValue(keyframes[i], typedValue);
                keyframes[i].easing = easing;
                return keyframes[i];
            }
        }

        // No match — insert at the right position to keep the list sorted.
        Keyframe kf = new() { time = time, easing = easing };
        SetKeyframeValue(kf, typedValue);

        int insertIndex = keyframes.FindIndex(k => k.time > time);
        if (insertIndex < 0) keyframes.Add(kf);
        else keyframes.Insert(insertIndex, kf);
        return kf;
    }

    /// <summary>
    /// Remove a keyframe by reference. Returns true if it was present.
    /// </summary>
    public bool RemoveKeyframe(Keyframe kf) => keyframes.Remove(kf);

    /// <summary>
    /// Update a keyframe's value, keeping rawValue (JToken for save) and typedValue (live) in
    /// sync. The track must be bound or have at least one previously-converted keyframe so we
    /// know what type to write. If we can't determine the type, only typedValue is set.
    /// </summary>
    public void SetKeyframeValue(Keyframe kf, object? typedValue)
    {
        kf.typedValue = typedValue;
        kf.rawValue = typedValue == null
            ? null
            : Newtonsoft.Json.Linq.JToken.FromObject(typedValue);
    }

    /// <summary>
    /// Re-sort the keyframe list by time. Call after editing a keyframe's time directly.
    /// </summary>
    public void SortKeyframes()
    {
        keyframes.Sort((a, b) => a.time.CompareTo(b.time));
    }

    public void Evaluate(UIGroup root, float time)
    {
        if (!bindAttempted)
        {
            Bind(root);
        }

        if (cachedTarget == null || cachedAccessor == null || keyframes.Count == 0)
        {
            return;
        }

        if (!keyframesConverted)
        {
            ConvertKeyframes(cachedAccessor.ValueType);
        }

        object? value = Sample(time);
        if (value == null)
        {
            return;
        }

        try
        {
            cachedAccessor.Setter(cachedTarget, value);
        }
        catch (Exception e)
        {
            Trace.TraceError($"Animation: failed to set '{path}': {e.Message}");
        }
    }

    private void Bind(UIGroup root)
    {
        bindAttempted = true;
        cachedTarget = null;
        cachedAccessor = null;

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        // Split into drawable-segments and a property-segment. The property is the last
        // slash-separated segment; everything before is name-based child navigation.
        int lastSlash = path.LastIndexOf('/');
        string drawableSegments = lastSlash >= 0 ? path[..lastSlash] : string.Empty;
        string propertyPath = lastSlash >= 0 ? path[(lastSlash + 1)..] : path;

        UIDrawable? target = ResolveDrawable(root, drawableSegments);
        if (target == null)
        {
            Trace.TraceWarning($"Animation: could not resolve drawable for path '{path}'");
            return;
        }

        PropertyAccessor? accessor = PropertyAccessor.GetOrBuild(target.GetType(), propertyPath);
        if (accessor == null)
        {
            Trace.TraceWarning($"Animation: could not resolve property '{propertyPath}' on {target.GetType().Name}");
            return;
        }

        cachedTarget = target;
        cachedAccessor = accessor;
        keyframesConverted = false;
    }

    private static UIDrawable? ResolveDrawable(UIGroup root, string drawableSegments)
    {
        if (string.IsNullOrEmpty(drawableSegments))
        {
            return root;
        }

        string[] parts = drawableSegments.Split('/', StringSplitOptions.RemoveEmptyEntries);
        UIDrawable current = root;
        foreach (string part in parts)
        {
            if (current is not UIGroup group)
            {
                return null;
            }
            UIDrawable? next = group.children.FirstOrDefault(c => c.name == part);
            if (next == null)
            {
                return null;
            }
            current = next;
        }
        return current;
    }

    private void ConvertKeyframes(Type valueType)
    {
        foreach (Keyframe kf in keyframes)
        {
            if (kf.rawValue == null)
            {
                kf.typedValue = null;
                continue;
            }
            try
            {
                kf.typedValue = kf.rawValue.ToObject(valueType);
            }
            catch (Exception e)
            {
                Trace.TraceError($"Animation: failed to convert keyframe value to {valueType.Name}: {e.Message}");
                kf.typedValue = null;
            }
        }
        keyframesConverted = true;
    }

    private object? Sample(float time)
    {
        // Assume keyframes are sorted by time. We could sort defensively, but doing it once
        // at bind/load time is cheaper than every frame. We sort in OnDeserialized below.
        if (keyframes.Count == 0)
        {
            return null;
        }
        if (time <= keyframes[0].time)
        {
            return keyframes[0].typedValue;
        }
        if (time >= keyframes[^1].time)
        {
            return keyframes[^1].typedValue;
        }

        // Linear scan is fine for typical track sizes; switch to binary search if you ever
        // see tracks with hundreds of keys.
        for (int i = 0; i < keyframes.Count - 1; i++)
        {
            Keyframe a = keyframes[i];
            Keyframe b = keyframes[i + 1];
            if (time >= a.time && time <= b.time)
            {
                if (a.typedValue == null || b.typedValue == null || cachedAccessor == null)
                {
                    return a.typedValue;
                }
                float span = b.time - a.time;
                float u = span > 0f ? (time - a.time) / span : 0f;
                float eased = EasingFunctions.Apply(a.easing, u);
                return Interpolator.Lerp(cachedAccessor.ValueType, a.typedValue, b.typedValue, eased);
            }
        }
        return keyframes[^1].typedValue;
    }

    [OnDeserialized]
    internal void OnDeserialized(System.Runtime.Serialization.StreamingContext _)
    {
        keyframes.Sort((a, b) => a.time.CompareTo(b.time));
    }
}