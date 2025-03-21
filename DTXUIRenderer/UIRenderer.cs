using System;
using SharpDX;

namespace DTXUIRenderer;

public abstract class BaseTexture : IDisposable
{
    public abstract float transparency { get; set; }
    public abstract float Width { get; }
    public abstract float Height { get; }
    public abstract string name { get; }
        
    public abstract void tDraw2DMatrix(Matrix transformMatrix, Vector2 size, RectangleF clipRect);
    public abstract void tDraw2DMatrixSliced(Matrix transformMatrix, Vector2 size, RectangleF clipRect, RectangleF sliceRect);

    public virtual void Dispose()
    {
            
    }

    public abstract bool isValid();
}