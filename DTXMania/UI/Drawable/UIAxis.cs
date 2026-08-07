using System.Numerics;

namespace DTXMania.UI.Drawable;

public enum UIAxis
{
    X,
    Y,
    Z
}

//which input direction drives a list, as opposed to which way it is laid out
public enum UINavigationAxis
{
    Vertical,
    Horizontal
}

public static class UIAxisExtensions
{
    public static Vector3 Unit(this UIAxis axis) => axis switch
    {
        UIAxis.X => Vector3.UnitX,
        UIAxis.Y => Vector3.UnitY,
        _ => Vector3.UnitZ
    };

    public static float Of(this UIAxis axis, Vector3 value) => axis switch
    {
        UIAxis.X => value.X,
        UIAxis.Y => value.Y,
        _ => value.Z
    };
}
