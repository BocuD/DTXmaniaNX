using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DTXMania.UI.Animation;

public enum Easing
{
    Step,        // hold value until next keyframe
    Linear,
    EaseInQuad,
    EaseOutQuad,
    EaseInOutQuad,
    EaseInCubic,
    EaseOutCubic,
    EaseInOutCubic
}

public static class EasingFunctions
{
    public static float Apply(Easing easing, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return easing switch
        {
            Easing.Step => 0f,
            Easing.Linear => t,
            Easing.EaseInQuad => t * t,
            Easing.EaseOutQuad => 1f - (1f - t) * (1f - t),
            Easing.EaseInOutQuad => t < 0.5f ? 2f * t * t : 1f - MathF.Pow(-2f * t + 2f, 2f) / 2f,
            Easing.EaseInCubic => t * t * t,
            Easing.EaseOutCubic => 1f - MathF.Pow(1f - t, 3f),
            Easing.EaseInOutCubic => t < 0.5f ? 4f * t * t * t : 1f - MathF.Pow(-2f * t + 2f, 3f) / 2f,
            _ => t
        };
    }
}

/// <summary>
/// One keyframe on a track. Value is stored as a JToken so it round-trips through JSON without
/// the track needing to be bound to a target. It's converted to the typed value lazily by the
/// track once the target property type is known.
/// </summary>
public sealed class Keyframe
{
    [JsonProperty("t")] public float time;

    [JsonProperty("v")] public JToken? rawValue;

    [JsonProperty("ease", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public Easing easing = Easing.Linear;

    [JsonIgnore] public object? typedValue;
}
