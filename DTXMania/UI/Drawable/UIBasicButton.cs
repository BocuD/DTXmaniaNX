using System.Numerics;
using DTXMania.Core.Framework;
using DTXMania.UI.Text;

namespace DTXMania.UI.Drawable;

public class UIBasicButton : UIGroup, IUISelectable
{
    // two pre-rendered labels toggled by selection: plain white and a yellow->orange gradient
    private readonly UIText normalText;
    private readonly UIText selectedText;
    private readonly Action action;

    public UIBasicButton(int size, string text, Action action) : base($"UIBasicButton: {text}")
    {
        this.action = action;

        normalText = AddChild(new UIText(text, size));
        normalText.anchor = new Vector2(0.5f, 0f);
        normalText.position = new Vector3(-5, 2, 0);
        normalText.RenderTexture();

        selectedText = AddChild(new UIText(text, size));
        selectedText.anchor = new Vector2(0.5f, 0f);
        selectedText.position = new Vector3(-5, 2, 0);
        selectedText.isVisible = false;
        selectedText.fillGradientMode = UiTextGradientMode.Vertical;
        selectedText.fillGradientTopColor = new Color4(1f, 1f, 0f);     // FFFF00
        selectedText.fillGradientBottomColor = new Color4(1f, 0.27f, 0f); // FF4500
        selectedText.RenderTexture();
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