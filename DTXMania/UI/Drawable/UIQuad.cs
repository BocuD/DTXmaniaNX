using System.Drawing;
using System.Numerics;
using DTXMania.Core.Framework;
using DTXMania.UI.Inspector;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Drawable;

public class UIQuad : UIDrawable
{
    [Themable] public Color4 color = Color4.White;

    [AddChildMenu]
    public static UIDrawable Create() => new UIQuad();

    public UIQuad()
    {
        name = "Quad";
    }

    public UIQuad(Color4 color) : this()
    {
        this.color = color;
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        if (!isVisible)
        {
            return;
        }

        UpdateLocalTransformMatrix();
        BaseTexture.White.tDraw2DMatrix(localTransformMatrix * parentMatrix, size, new RectangleF(0, 0, 1, 1), UITint.Current * color);
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (ImGui.CollapsingHeader("Quad"))
        {
            Inspector.Inspector.Inspect("Color", ref color);
        }
    }
}
