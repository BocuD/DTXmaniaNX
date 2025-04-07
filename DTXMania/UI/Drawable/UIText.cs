using System.Drawing;
using DTXMania.Core;
using DTXMania.UI.DynamicElements;
using DTXUIRenderer;
using Hexa.NET.ImGui;
using SharpDX;
using Color = System.Drawing.Color;

namespace DTXMania.UI.Drawable;

public enum TextSource
{
    String,
    Dynamic
}

public class UIText : UITexture
{
    private bool dirty = true;
    public string text = "";
    
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
    public TextSource textSource = TextSource.String;
    public string dynamicSource = "Not Set";

    [AddChildMenu]
    public UIText() : base(BaseTexture.None)
    {
        //set font to default
        fontFamily = new FontFamily(CDTXMania.ConfigIni.songListFont);
        fontSize = 20;
        fontStyle = FontStyle.Regular;
        text = "New UIText";
        
        UpdateFont();
        RenderTexture();
    }
    
    public UIText(FontFamily font, int size) : base(BaseTexture.None)
    {
        fontFamily = font;
        fontSize = size;
        
        UpdateFont();
    }
        
    public UIText(FontFamily font, int size, string text) : base(BaseTexture.None)
    {
        fontFamily = font;
        fontSize = size;
        
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
        if (textSource == TextSource.Dynamic)
        {
            UpdateDynamicText();
        }
        
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
    
    private void UpdateDynamicText()
    {
        CDTXMania.rCurrentStage.dynamicStringSources.TryGetValue(dynamicSource, out var source);
        if (source != null)
        {
            SetText(source.GetString());
        }
        else
        {
            SetText($"Dynamic source: {dynamicSource} not found");
        }
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
        if (texture.isValid())
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
            
            if (Inspector.Inspect("Draw Mode", ref drawMode))
            {
                dirty = true;
            }

            if (Inspector.Inspect("Text Source", ref textSource))
            {
                dirty = true;
            }

            switch (textSource)
            {
                case TextSource.String:
                {
                    if (ImGui.InputTextMultiline("String", ref text, 256))
                    {
                        dirty = true;
                    }

                    break;
                }
                case TextSource.Dynamic:
                {
                    string[] sources = CDTXMania.rCurrentStage.dynamicStringSources.Keys.ToArray();
                    int selectedIndex = Array.IndexOf(sources, dynamicSource);
                    if (ImGui.Combo("Dynamic Source", ref selectedIndex, sources, sources.Length))
                    {
                        dynamicSource = sources[selectedIndex];
                        dirty = true;
                    }
                    break;
                }
            }

            //dropdown
            if (ImGui.TreeNode("Font"))
            {
                ImGui.LabelText("Font Family", fontFamily.ToString());
                
                if (ImGui.InputInt("Font Size", ref fontSize))
                {
                    if (fontSize <= 0) fontSize = 1;
                    fontDirty = true;
                    dirty = true;
                }

                if(Inspector.Inspect("Font Style", ref fontStyle))
                {
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
                ImGui.TreePop();
            }
        }
    }

    public override void Dispose()
    {
        texture.Dispose();
    }
}