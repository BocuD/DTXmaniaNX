using SharpDX;

namespace DTXUIRenderer
{
    public class UIImage : UITexture
    {
        public RectangleF clipRect;
        public RectangleF sliceRect;
        
        public ERenderMode renderMode = ERenderMode.Stretched;

        public UIImage(BaseTexture texture) : base(texture)
        {
            if (texture != null && texture.isValid())
            {
                clipRect = new RectangleF(0, 0, texture.Width, texture.Height);
            }
        }

        public override void Draw(Matrix parentMatrix)
        {
            if (!isVisible) return;
            
            UpdateLocalTransformMatrix();
            
            Matrix combinedMatrix = localTransformMatrix * parentMatrix;

            switch (renderMode)
            {
                case ERenderMode.Stretched:
                    texture.tDraw2DMatrix(combinedMatrix, size, clipRect);
                    break;
                
                case ERenderMode.Sliced:
                    texture.tDraw2DMatrixSliced(combinedMatrix, size, clipRect, sliceRect);
                    break;
            }
        }

        public void SetTexture(BaseTexture newTexture, bool updateRects = true)
        {
            texture = newTexture;
            
            if (!texture.isValid()) return;

            if (updateRects)
            {
                size = new Vector2(texture.Width, texture.Height);
                clipRect = new RectangleF(0, 0, texture.Width, texture.Height);
            }
        }
    }
    
    public enum ERenderMode
    {
        Stretched,
        Sliced
    }
}