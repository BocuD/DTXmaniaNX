using System.Drawing;
using DTXMania.Core;
using SharpDX;
using Color = System.Drawing.Color;

namespace DTXMania.UI.Drawable;

public class UIBasicButton : UIGroup, IUISelectable
{
    private UIText normalText;
    private UIText selectedText;
    private Action action;
        
    public UIBasicButton(FontFamily font, int size, string text, Action action) : base($"UIBasicButton: {text}")
    {
        this.action = action;
            
        normalText = AddChild(new UIText(font, size));
        normalText.SetText(text);
        normalText.RenderTexture();
        normalText.anchor = new Vector2(0.5f, 0f);
            
        selectedText = AddChild(new UIText(font, size));
        selectedText.SetText(text);
        selectedText.gradationTopColor = Color.Yellow;
        selectedText.gradationBottomColor = Color.OrangeRed;
        selectedText.drawMode = CPrivateFont.DrawMode.Edge | CPrivateFont.DrawMode.Gradation;
        selectedText.RenderTexture();
        selectedText.anchor = new Vector2(0.5f, 0f);
        selectedText.isVisible = false;
    }
        
    public void SetSelected(bool selected)
    {
        normalText.isVisible = !selected;
        selectedText.isVisible = selected;
    }

    public void RunAction()
    {
        action.Invoke();
    }
}