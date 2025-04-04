using System;
using Hexa.NET.ImGui;
using SharpDX;

namespace DTXUIRenderer;

public abstract class BaseTexture : IDisposable
{
    internal static BaseTexture None => new NoneTexture();
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

    public abstract ImTextureID? GetImTextureID();
}

public class NoneTexture : BaseTexture
{
    public override float transparency { get; set; }
    public override float Width => 0;
    public override float Height => 0;
    public override string name => "None";

    public override void tDraw2DMatrix(Matrix transformMatrix, Vector2 size, RectangleF clipRect)
    {
    }

    public override void tDraw2DMatrixSliced(Matrix transformMatrix, Vector2 size, RectangleF clipRect, RectangleF sliceRect)
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