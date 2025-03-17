using DTXUIRenderer;
using FDK;
using SharpDX;

namespace DTXMania.Code.UI
{
    public class DTXTexture : BaseTexture
    {
        private CTexture texture;

        public override float transparency
        {
            get => texture.nTransparency / 255.0f;
            set => texture.nTransparency = (int)(value * 255.0f);
        }

        public override float Width => texture.szTextureSize.Width;
        public override float Height => texture.szTextureSize.Height;
        
        public DTXTexture(string texturePath)
        {
            texture = CDTXMania.tGenerateTexture(texturePath);
        }
        
        public DTXTexture(CTexture texture)
        {
            this.texture = texture;
        }

        public override void tDraw2DMatrix(Matrix transformMatrix, Vector2 size, RectangleF clipRect)
        {
            texture.tDraw2DMatrix(CDTXMania.app.Device, transformMatrix, size, clipRect);
        }

        public override void tDraw2DMatrixSliced(Matrix transformMatrix, Vector2 size, RectangleF clipRect, RectangleF sliceRect)
        {
            texture.tDraw2DMatrixSliced(CDTXMania.app.Device, transformMatrix, size, clipRect, sliceRect);
        }

        public override void Dispose()
        {
            CDTXMania.tReleaseTexture(ref texture);
        }

        public override bool isValid()
        {
            return texture != null;
        }
    }
}