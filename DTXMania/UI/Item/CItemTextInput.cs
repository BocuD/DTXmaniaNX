using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;

namespace DTXMania.UI.Item;

internal class CItemTextInput : CItemBase, IDisposable
{
    public string strCurrentValue = "";
    
    public UIImGuiTextInput drawableTextInput;

    public CItemTextInput()
    {
        eType = EType.TextInput;
        drawableTextInput = new UIImGuiTextInput();
        drawableTextInput.fillColor = Color4.Black;
        drawableTextInput.outlineWidth = 0;
        drawableTextInput.fontSize = 16;
    }
    
    internal CItemTextInput(string strItemName, string initialValue, string strDescJa, string strDescEn) : this()
    {
        tInitialize(strItemName, EPanelType.Normal, strDescJa, strDescEn);
        
        strCurrentValue = initialValue;
        drawableTextInput.SetText(initialValue);
    }

    public override string GetStringValue()
    {
        return strCurrentValue;
    }

    protected override void tEnterPressed()
    {
        drawableTextInput.fillColor = new Color4(1, 0.27f, 0);
        drawableTextInput.ActivateTextInput(strCurrentValue, 
            (newValue) => 
            {
                strCurrentValue = newValue;
                action?.Invoke();
                drawableTextInput.fillColor = Color4.Black;
                drawableTextInput.RenderTexture();
            },
            () => {
                drawableTextInput.fillColor = Color4.Black;
                drawableTextInput.RenderTexture();
            });
        drawableTextInput.RenderTexture();
    }

    public void Dispose()
    {
        drawableTextInput.Dispose();
    }
}