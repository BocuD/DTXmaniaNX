using Hexa.NET.ImGui;
using SharpDX;

namespace DTXMania.UI.Drawable;

public abstract class BaseTexture : IDisposable
{
    public static BaseTexture None => new NoneTexture();
    public abstract float Width { get; }
    public abstract float Height { get; }
    public abstract string name { get; }
        
    public abstract void tDraw2DMatrix(Matrix transformMatrix, Vector2 size, RectangleF clipRect, Color4 color);
    public abstract void tDraw2DMatrixSliced(Matrix transformMatrix, Vector2 size, RectangleF clipRect, Color4 color, RectangleF sliceRect);

    public virtual void Dispose()
    {
        
    }

    public abstract bool isValid();

    public abstract ImTextureID? GetImTextureID();
}

public class NoneTexture : BaseTexture
{
    public override float Width => 0;
    public override float Height => 0;
    public override string name => "None";

    public override void tDraw2DMatrix(Matrix transformMatrix, Vector2 size, RectangleF clipRect, Color4 color)
    {
    }

    public override void tDraw2DMatrixSliced(Matrix transformMatrix, Vector2 size, RectangleF clipRect, Color4 color, RectangleF sliceRect)
    {
    }

    public override bool isValid()
    {
        return false;
    }

    public override ImTextureID? GetImTextureID()
    {
        return null;
    }
}