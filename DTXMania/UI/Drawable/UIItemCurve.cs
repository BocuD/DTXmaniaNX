using System.Numerics;

namespace DTXMania.UI.Drawable;

public enum UICurveShape
{
    //falls off in a straight line from the focus to the edge of the range
    Linear,

    //eases in and out, so items settle rather than snap as they approach the focus
    Smooth,

    //a rounded bulge, steepest halfway out
    Cosine
}

/// <summary>
/// Displaces an item along one axis by how far it sits from the list's focus, which is what makes a song
/// list bow towards the selected row. Kept separate from the list itself so the shape of the movement is
/// data: it serializes into the layout, every parameter is <c>[Themable]</c> so the animation system can
/// drive it, and anything else that positions items by distance can reuse it.
/// </summary>
public sealed class UIItemCurve
{
    //which axis the displacement is applied to; the list's own axis stays untouched
    [Themable] public UIAxis axis = UIAxis.X;

    [Themable] public UICurveShape shape = UICurveShape.Linear;

    //displacement at the focus, falling to zero at the edge of the range. Signed.
    [Themable] public float distance;

    //how far from the focus, in the list's own units, the displacement reaches zero
    [Themable] public float range = 90.0f;

    //where the peak sits relative to the list's own origin, along the axis the list runs on. An item's
    //artwork is rarely centred on its origin, so this is what lines the bulge up with what is drawn
    //rather than with the item's top-left.
    [Themable] public float focus;

    public UIItemCurve()
    {
    }

    public UIItemCurve(UIAxis axis, float distance, float range, UICurveShape shape = UICurveShape.Linear)
    {
        this.axis = axis;
        this.distance = distance;
        this.range = range;
        this.shape = shape;
    }

    public bool IsActive => distance != 0.0f && range > 0.0f;

    /// <summary>The displacement for an item sitting at <paramref name="positionAlongAxis"/>, measured
    /// along the axis the list runs on, relative to the list's origin.</summary>
    public Vector3 Evaluate(float positionAlongAxis)
    {
        float amount = EvaluateAmount(positionAlongAxis);

        return axis switch
        {
            UIAxis.X => new Vector3(amount, 0, 0),
            UIAxis.Y => new Vector3(0, amount, 0),
            _ => new Vector3(0, 0, amount)
        };
    }

    /// <summary>The displacement as a single number, for readouts and tests.</summary>
    public float EvaluateAmount(float positionAlongAxis)
    {
        if (!IsActive)
        {
            return 0f;
        }

        //1 at the focus, 0 at the edge of the range and beyond
        float t = Math.Clamp(1.0f - Math.Abs(positionAlongAxis - focus) / range, 0.0f, 1.0f);

        t = shape switch
        {
            UICurveShape.Smooth => t * t * (3.0f - 2.0f * t),
            UICurveShape.Cosine => (1.0f - MathF.Cos(t * MathF.PI)) * 0.5f,
            _ => t
        };

        return t * distance;
    }
}
