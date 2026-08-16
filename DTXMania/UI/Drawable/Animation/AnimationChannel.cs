using System.Diagnostics;

namespace DTXMania.UI.Animation;

/// <summary>
/// Samples a track and writes the result without boxing.
///
/// Keyframes hold their values as <c>object</c> because they arrive from JSON before anything knows the
/// target property's type, and reading one back out is only a copy. The cost is in interpolating: that
/// produces a new value, which an <c>Action&lt;object, object&gt;</c> setter can only take boxed. The
/// channel is built once at bind time with the property's type as its generic argument, so the lerp and
/// the write both stay in terms of that type.
/// </summary>
internal abstract class AnimationChannel
{
    public abstract void Apply(object target, List<Keyframe> frames, float time);

    /// <summary>
    /// Null for a type with no interpolation registered. The caller's object path costs nothing for one
    /// of those, since it only ever steps between values a keyframe already holds.
    /// </summary>
    public static AnimationChannel? TryCreate(PropertyAccessor accessor)
    {
        if (!Interpolator.IsRegistered(accessor.ValueType))
        {
            return null;
        }

        try
        {
            Type channel = typeof(AnimationChannel<>).MakeGenericType(accessor.ValueType);
            return (AnimationChannel?)Activator.CreateInstance(channel, accessor);
        }
        catch (Exception e)
        {
            Trace.TraceWarning($"Animation: no typed channel for {accessor.ValueType.Name}: {e.Message}");
            return null;
        }
    }
}

internal sealed class AnimationChannel<T> : AnimationChannel
{
    private readonly Action<object, T> setter;
    private readonly Func<T, T, float, T> lerp;

    public AnimationChannel(PropertyAccessor accessor)
    {
        setter = accessor.GetTypedSetter<T>()!;
        lerp = Interpolator.TypedLerp<T>()!;
    }

    //keyframes are in time order, so the ends are checked before the span between them is looked for
    public override void Apply(object target, List<Keyframe> frames, float time)
    {
        if (frames.Count == 0)
        {
            return;
        }

        if (time <= frames[0].time)
        {
            Hold(target, frames[0]);
            return;
        }

        if (time >= frames[^1].time)
        {
            Hold(target, frames[^1]);
            return;
        }

        for (int i = 0; i < frames.Count - 1; i++)
        {
            Keyframe a = frames[i];
            Keyframe b = frames[i + 1];

            if (time < a.time || time > b.time)
            {
                continue;
            }

            if (a.typedValue is not T from || b.typedValue is not T to)
            {
                Hold(target, a);
                return;
            }

            float span = b.time - a.time;
            float u = span > 0f ? (time - a.time) / span : 0f;
            setter(target, lerp(from, to, EasingFunctions.Apply(a.easing, u)));
            return;
        }

        Hold(target, frames[^1]);
    }

    private void Hold(object target, Keyframe frame)
    {
        if (frame.typedValue is T value)
        {
            setter(target, value);
        }
    }
}
