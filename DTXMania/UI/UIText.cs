using DTXMania.Core;
using DTXUIRenderer;
using Hexa.NET.ImGui;
using SharpDX;
using System.Drawing;
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

    private bool fontDirty = true;
    private CPrivateFastFont font;
    public CPrivateFont.DrawMode drawMode = CPrivateFont.DrawMode.Edge;
    public FontFamily fontFamily;
    public int fontSize;
    public FontStyle fontStyle;
        
    public UIText(FontFamily font, int size) : base(null)
    {
        this.fontFamily = font;
        this.fontSize = size;
        
        UpdateFont();
    }
        
    public UIText(FontFamily font, int size, string text) : base(null)
    {
        this.fontFamily = font;
        this.fontSize = size;
        
        SetText(text);
        
        UpdateFont();
        RenderTexture();
    }
        
    public void SetText(string text)
    {
        this.text = text;
        dirty = true;
    }
        
    public override void Draw(Matrix parentMatrix)
    {
        if (fontDirty)
        {
            UpdateFont();
        }
        
        if (dirty)
        {
            RenderTexture();
        }
        
        base.Draw(parentMatrix);
    }

    public void UpdateFont()
    {
        if (font != null)
        {
            font.Dispose();
        }

        font = new CPrivateFastFont(fontFamily, fontSize, fontStyle);
        fontDirty = false;
    }

    public void RenderTexture()
    {
        if (texture != null)
        {
            texture.Dispose();
        }

        bool gradationEdge = drawMode == CPrivateFont.DrawMode.Gradation;
        
        var bmp = font.DrawPrivateFont(text, drawMode, fontColor, edgeColor, gradationTopColor, gradationBottomColor, gradationEdge);
        texture = new DTXTexture(CDTXMania.tGenerateTexture(bmp, false));
        size = new Vector2(texture.Width, texture.Height);
        bmp.Dispose();
        dirty = false;
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (ImGui.CollapsingHeader("UIText"))
        {
            if (ImGui.InputText("Text", ref text, 256))
            {
                dirty = true;
            }

            if (Inspector.Inspect("Text Color", ref fontColor))
            {
                dirty = true;
            }

            if (Inspector.Inspect("Edge Color", ref edgeColor))
            {
                dirty = true;
            }

            if (Inspector.Inspect("Gradation Top Color", ref gradationTopColor))
            {
                dirty = true;
            }

            if (Inspector.Inspect("Gradation Bottom Color", ref gradationBottomColor))
            {
                dirty = true;
            }

            int dm = (int)drawMode;
            string options = Enum.GetNames(typeof(CPrivateFont.DrawMode)).Aggregate((a, b) => $"{a}\0{b}");
            if (ImGui.Combo("Draw Mode", ref dm, options))
            {
                drawMode = (CPrivateFont.DrawMode)dm;
                dirty = true;
            }

            if (ImGui.CollapsingHeader("Font"))
            {
                ImGui.LabelText("Font Family", fontFamily.ToString());

                if (ImGui.InputInt("Font Size", ref fontSize))
                {
                    fontDirty = true;
                    dirty = true;
                }

                var fs = (int)fontStyle;
                string fsOptions = Enum.GetNames(typeof(FontStyle)).Aggregate((a, b) => $"{a}\0{b}");
                if (ImGui.Combo("Font Style", ref fs, fsOptions))
                {
                    fontStyle = (FontStyle)fs;
                    fontDirty = true;
                    dirty = true;
                }

                if (fontDirty)
                {
                    UpdateFont();
                    RenderTexture();
                }
                else if (dirty)
                {
                    RenderTexture();
                }
            }
        }
    }

    public override void Dispose()
    {
        texture.Dispose();
    }
}