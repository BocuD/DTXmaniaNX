using System.Drawing;
using System.Numerics;

namespace DTXMania.UI.Drawable;

public class SpriteSheet : UITexture
{
    public BaseTexture[] textures = [];

    public SpriteSheet() : base(BaseTexture.None)
    {
        
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        if (!isVisible)
        {
            return;
        }

        UpdateLocalTransformMatrix();
        Matrix4x4 combinedMatrix = localTransformMatrix * parentMatrix;
        texture.tDraw2DMatrix(combinedMatrix, size, new RectangleF(0, 0, texture.Width, texture.Height), color);
    }
}