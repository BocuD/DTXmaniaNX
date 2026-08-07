using System.Numerics;
using DTXMania.UI.Drawable;

namespace DTXMania.UI.Inspector;

/// <summary>
/// The box a drawn tree occupies, and the canvas that shows all of it. Both work in the space the tree is
/// drawn in, and <see cref="Fit"/> returns where the tree's own origin has to sit for its content to be
/// inside — feeding that origin back must produce the same canvas, or the canvas chases itself.
/// </summary>
public static class ComponentBounds
{
    private static readonly Vector2 Padding = new(32, 32);

    /// <summary>Grows <paramref name="min"/>/<paramref name="max"/> to cover the tree's drawn quads. Seed
    /// them with a point that is already in the tree's space, not with zero.</summary>
    public static void Measure(UIDrawable node, ref Vector2 min, ref Vector2 max)
    {
        Matrix4x4 transform = node.GetFullTransformMatrix();

        for (int corner = 0; corner < 4; corner++)
        {
            Vector3 local = new((corner & 1) == 0 ? 0.0f : node.size.X, (corner & 2) == 0 ? 0.0f : node.size.Y, 0.0f);
            Vector3 world = Vector3.Transform(local, transform);

            min = Vector2.Min(min, new Vector2(world.X, world.Y));
            max = Vector2.Max(max, new Vector2(world.X, world.Y));
        }

        if (node is not UIGroup group)
        {
            return;
        }

        foreach (UIDrawable child in group.children)
        {
            Measure(child, ref min, ref max);
        }
    }

    /// <summary>The canvas that holds a content box measured around the origin, and where that origin ends
    /// up on it. The origin is always inside: it is the one point the axes have to mark.</summary>
    public static (Vector2 canvas, Vector2 origin) Fit(Vector2 contentMin, Vector2 contentMax)
    {
        Vector2 min = Vector2.Min(contentMin, Vector2.Zero) - Padding;
        Vector2 max = Vector2.Max(contentMax, Vector2.Zero) + Padding;

        return (new Vector2(MathF.Ceiling(max.X - min.X), MathF.Ceiling(max.Y - min.Y)), -min);
    }
}
