using System.Drawing;
using System.Numerics;
using DTXMania.Core.Framework;
using DTXMania.UI.Text;
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
            
        normalText = AddChild(new UIText(text, size));
        normalText.RenderTexture();
        normalText.anchor = new Vector2(0.5f, 0f);
            
        selectedText = AddChild(new UIText(text, size));
        selectedText.fillGradientMode = UiTextGradientMode.Vertical;
        Color4 topColor = new Color4(1, 1, 0); //FFFF00
        Color4 bottomColor = new Color4(1, 0.27f, 0);//FF4500
        selectedText.fillGradientTopColor = topColor;
        selectedText.fillGradientBottomColor = bottomColor;
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