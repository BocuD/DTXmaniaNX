using System.Diagnostics;
using System.Drawing;
using System.Numerics;

namespace DTXMania.UI.Drawable;

public class TextureArray : UITexture
{
    [Themable] public int textureIndex = 0;
    public BaseTexture[] textures = [];

    public TextureArray(BaseTexture[] textures) : base(textures[0])
    {
        this.textures = textures;
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        if (!isVisible)
        {
            return;
        }
        
        if (textures.Length <= textureIndex)
        {
            Trace.TraceWarning($"TextureArray: textureIndex {textureIndex} is out of bounds for textures array of length {textures.Length}");
            return;
        }
        
        var target = textures[textureIndex];

        UpdateLocalTransformMatrix();
        Matrix4x4 combinedMatrix = localTransformMatrix * parentMatrix;
        target.tDraw2DMatrix(combinedMatrix, size, new RectangleF(0, 0, texture.Width, texture.Height), color);
    }
}