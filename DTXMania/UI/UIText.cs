using DTXMania.Core;
using DTXUIRenderer;
using SharpDX;
using Color = System.Drawing.Color;

namespace DTXMania.UI;

public class UIText : UITexture
{
    private bool dirty = true;
    private string text = "";
        
    public Color fontColor = Color.White;
    public Color edgeColor = Color.Black;
    public Color gradationTopColor = Color.White;
    public Color gradationBottomColor = Color.White;
        
    public CPrivateFont.DrawMode drawMode = CPrivateFont.DrawMode.Edge;
    public CPrivateFont font;
        
    public UIText(CPrivateFont font) : base(null)
    {
        this.font = font;
    }
        
    public UIText(CPrivateFont font, string text) : base(null)
    {
        this.font = font;
        SetText(text);
        RenderTexture();
    }
        
    public void SetText(string text)
    {
        this.text = text;
        dirty = true;
    }
        
    public override void Draw(Matrix parentMatrix)
    {
        if (dirty)
        {
            RenderTexture();
        }
            
        base.Draw(parentMatrix);
    }

    public void RenderTexture()
    {
        var bmp = font.DrawPrivateFont(text, drawMode, fontColor, edgeColor, gradationTopColor, gradationBottomColor);
        texture = new DTXTexture(CDTXMania.tGenerateTexture(bmp, false));
        size = new Vector2(texture.Width, texture.Height);
        bmp.Dispose();
        dirty = false;
    }

    public override void Dispose()
    {
        texture.Dispose();
    }
}