using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;

namespace DTXMania.UI.Item;

internal class CItemTextInput : CItemBase, IDisposable
{
    public string strCurrentValue = "";
    
    public UITextInput drawableTextInput;

    public CItemTextInput()
    {
        eType = EType.TextInput;

        drawableTextInput = new UITextInput();
        drawableTextInput.fillColor = new Color4(1f, 0.27f, 0f);
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
        drawableTextInput.ActivateTextInput(strCurrentValue, newValue =>
        {
            strCurrentValue = newValue;
            action?.Invoke();
        });
    }

    public void Dispose()
    {
        drawableTextInput.Dispose();
    }
}