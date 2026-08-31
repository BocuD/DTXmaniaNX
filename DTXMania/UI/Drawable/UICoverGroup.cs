using System.Numerics;
using DTXMania.UI.Inspector;

namespace DTXMania.UI.Drawable;

/// <summary>
/// A group that covers the window. Its box stays the design size and scales up around the middle until
/// both axes reach the window edge, so what is inside is authored in design space and gets cropped rather
/// than stretched. The centring is measured from the parent, so put one directly under a stage root.
/// </summary>
public class UICoverGroup : UIGroup
{
    [AddChildMenu("Cover Group")]
    public new static UIDrawable Create()
    {
        return new UICoverGroup();
    }

    public UICoverGroup() : this("Cover")
    {
    }

    public UICoverGroup(string name) : base(name)
    {
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        //the axis with further to go decides, so the other overflows
        Vector2 canvas = UICanvas.canvasSize;
        Vector2 design = UICanvas.logicalSize;
        float cover = MathF.Max(canvas.X / design.X, canvas.Y / design.Y);

        size = design;
        pivot = UICanvas.Center;
        parentAnchor = UICanvas.Center;
        position = Vector3.Zero;
        scale = new Vector3(cover, cover, 1f);

        base.Draw(parentMatrix);
    }
}
