using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;

namespace DTXMania.UI.Drawable;

public class Modal : UIGroup
{
    private const float PanelWidth = 640f;
    private const float ButtonSpacing = 46f;

    private const int TitleFontSize = 40;
    private const int DescriptionFontSize = 24;
    private const int OptionFontSize = 30;

    private readonly UISelectList optionList;
    private readonly bool cancellable;
    
    public static int OpenCount { get; private set; }
    public static bool IsAnyOpen => OpenCount > 0;

    private bool counted;

    //close is deferred out of the input/child-draw phase because activating an option disposes this
    //dialog, and we must not dispose children while we're in the middle of drawing/iterating them
    private bool closeRequested;
    private Action? pendingAction;

    public Modal(string title, string description, string[] options, Action[] actions, bool cancellable = true)
        : base($"Modal: {title}")
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(actions);
        if (options.Length != actions.Length)
        {
            throw new ArgumentException("options and actions must have the same length.", nameof(actions));
        }

        this.cancellable = cancellable;
        
        renderOrder = int.MaxValue;
        dontSerialize = true;

        float screenWidth = GameWindowSize.Width;
        float screenHeight = GameWindowSize.Height;
        float centerX = screenWidth / 2f;

        //layout fluff
        const float topPadding = 44f;
        const float titleToDescriptionGap = 18f;
        const float descriptionToOptionsGap = 30f;
        const float bottomPadding = 36f;

        float optionsBlockHeight = options.Length * ButtonSpacing;
        float panelHeight = topPadding + TitleFontSize + titleToDescriptionGap + DescriptionFontSize
                            + descriptionToOptionsGap + optionsBlockHeight + bottomPadding;
        float panelTop = (screenHeight - panelHeight) / 2f;

        //dimmed full-screen backdrop
        UIImage backdrop = AddChild(new UIImage(BaseTexture.CreateSolidColor(new Color4(0f, 0f, 0f, 0.6f))));
        backdrop.name = "Backdrop";
        backdrop.position = new Vector3(0f, 0f, 0f);
        backdrop.size = new Vector2(screenWidth, screenHeight);
        backdrop.renderOrder = 0;

        //panel
        UIImage panel = AddChild(new UIImage(BaseTexture.CreateSolidColor(new Color4(0.11f, 0.12f, 0.16f, 0.96f))));
        panel.name = "Panel";
        panel.anchor = new Vector2(0.5f, 0f);
        panel.position = new Vector3(centerX, panelTop, 0f);
        panel.size = new Vector2(PanelWidth, panelHeight);
        panel.renderOrder = 1;

        //title
        float titleY = panelTop + topPadding;
        UIText titleText = AddChild(new UIText(title, TitleFontSize));
        titleText.name = "Title";
        titleText.anchor = new Vector2(0.5f, 0f);
        titleText.position = new Vector3(centerX, titleY, 0f);
        titleText.renderOrder = 2;
        titleText.RenderTexture();

        //description
        float descriptionY = titleY + TitleFontSize + titleToDescriptionGap;
        UIText descriptionText = AddChild(new UIText(description, DescriptionFontSize));
        descriptionText.name = "Description";
        descriptionText.anchor = new Vector2(0.5f, 0f);
        descriptionText.position = new Vector3(centerX, descriptionY, 0f);
        descriptionText.renderOrder = 2;
        descriptionText.RenderTexture();

        //options
        optionList = AddChild(new UISelectList($"{title} options"));
        optionList.renderOrder = 3;
        optionList.position = new Vector3(centerX, descriptionY + DescriptionFontSize + descriptionToOptionsGap, 0f);

        for (int i = 0; i < options.Length; i++)
        {
            Action action = actions[i];
            optionList.AddSelectableChild(new UIBasicButton(OptionFontSize, options[i], () => RequestClose(action)));
        }

        optionList.UpdateLayout((int)ButtonSpacing);
        if (options.Length > 0)
        {
            optionList.SetSelectedIndex(0);
        }
        
        CDTXMania.Input.ResetNavigation();

        counted = true;
        OpenCount++;
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        if (closeRequested)
        {
            FinishClose();
            return;
        }

        HandleInput();

        if (closeRequested)
        {
            FinishClose();
            return;
        }

        base.Draw(parentMatrix);
    }

    private void HandleInput()
    {
        Input input = CDTXMania.Input;

        input.Navigate(() => optionList.SelectPrevious(), () => optionList.SelectNext());

        if (input.ActionDecide())
        {
            //runs the highlighted option's action, which calls RequestClose(...)
            optionList.RunAction();
        }
        else if (cancellable && input.ActionCancel())
        {
            RequestClose(null);
        }
    }

    private void RequestClose(Action? action)
    {
        if (closeRequested)
        {
            return;
        }

        closeRequested = true;
        pendingAction = action;
    }

    private void FinishClose()
    {
        isVisible = false;
        CDTXMania.Input.ResetNavigation();

        Action? action = pendingAction;
        pendingAction = null;

        //remove from the parent and dispose children, then run the option's action
        //this order means an action that opens another dialog on the same parent works cleanly
        parent?.RemoveChild(this);
        Dispose();

        action?.Invoke();
    }

    public override void Dispose()
    {
        //covers both the normal close path (FinishClose disposes) and external
        //disposal (e.g. the parent group being cleared/torn down) without double-counting
        if (counted)
        {
            counted = false;
            OpenCount--;
        }

        base.Dispose();
    }
}