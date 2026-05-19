using System.Collections.Concurrent;
using System.Numerics;
using DTXMania.Core.Framework;

namespace DTXMania.UI.Animation;

/// <summary>
/// Registry of interpolation functions by value type. Unregistered types fall back to step.
/// </summary>
public static class Interpolator
{
    public delegate object LerpFn(object a, object b, float t);

    private static readonly ConcurrentDictionary<Type, LerpFn> Registry = new();

    static Interpolator()
    {
        Register<float>((a, b, t) => a + (b - a) * t);
        Register<double>((a, b, t) => a + (b - a) * t);
        Register<int>((a, b, t) => (int)MathF.Round(a + (b - a) * t));
        Register<bool>((a, b, t) => t < 1f ? a : b);
        Register<Vector2>(Vector2.Lerp);
        Register<Vector3>(Vector3.Lerp);
        Register<Vector4>(Vector4.Lerp);
        Register<Quaternion>(Quaternion.Slerp);
        Register<Color4>((a, b, t) => new Color4(
            a.Red + (b.Red - a.Red) * t,
            a.Green + (b.Green - a.Green) * t,
            a.Blue + (b.Blue - a.Blue) * t,
            a.Alpha + (b.Alpha - a.Alpha) * t));
    }

    public static void Register<T>(Func<T, T, float, T> fn)
    {
        Registry[typeof(T)] = (a, b, t) => fn((T)a, (T)b, t)!;
    }

    public static object Lerp(Type type, object a, object b, float t)
    {
        if (Registry.TryGetValue(type, out LerpFn? fn))
        {
            return fn(a, b, t);
        }
        // Step fallback for types we don't know how to blend.
        return t < 1f ? a : b;
    }

    public static bool IsRegistered(Type type) => Registry.ContainsKey(type);
}