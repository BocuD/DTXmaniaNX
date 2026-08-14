using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Text;

namespace DTXMania.UI.Drawable;

public class Modal : UIGroup
{
    private const float PanelWidth = 640f;
    private const float ButtonSpacing = 46f;

    private const int TitleFontSize = 30;
    private const int DescriptionFontSize = 22;
    private const int OptionFontSize = 25;

    private readonly UIMenu optionList;
    private readonly bool cancellable;

    //whether this dialog is still on the focus stack, so tearing it down any other way still pops it
    private bool focused;

    //set by ShowAsync; resolves the awaited task with the chosen option index (or -1)
    private TaskCompletionSource<int>? completionSource;

    //deferred: activating an option disposes this dialog, which must not happen mid-draw
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

        const float topPadding = 20f;
        const float titleToDescriptionGap = 18f;
        const float descriptionToOptionsGap = 20f;
        const float bottomPadding = 20f;

        float optionsBlockHeight = options.Length * ButtonSpacing;
        float panelHeight = topPadding + TitleFontSize + titleToDescriptionGap + DescriptionFontSize * 3
                            + descriptionToOptionsGap + optionsBlockHeight + bottomPadding;
        float panelTop = (screenHeight - panelHeight) / 2f;

        UIImage backdrop = AddChild(new UIImage(BaseTexture.CreateSolidColor(new Color4(0f, 0f, 0f, 0.6f))));
        backdrop.name = "Backdrop";
        backdrop.position = new Vector3(0f, 0f, 0f);
        backdrop.size = new Vector2(screenWidth, screenHeight);
        backdrop.renderOrder = 0;

        UIImage panel = AddChild(new UIImage(BaseTexture.CreateSolidColor(new Color4(0.11f, 0.11f, 0.11f, 0.96f))));
        panel.name = "Panel";
        panel.anchor = new Vector2(0.5f, 0f);
        panel.position = new Vector3(centerX, panelTop, 0f);
        panel.size = new Vector2(PanelWidth, panelHeight);
        panel.renderOrder = 1;

        float titleY = panelTop + topPadding;
        UIText titleText = AddChild(new UIText(title, TitleFontSize));
        titleText.name = "Title";
        titleText.anchor = new Vector2(0.5f, 0f);
        titleText.position = new Vector3(centerX, titleY, 0f);
        titleText.renderOrder = 2;
        titleText.outlineWidth = 0;
        titleText.RenderTexture();

        float descriptionY = titleY + TitleFontSize + titleToDescriptionGap;
        UIText descriptionText = AddChild(new UIText(description, DescriptionFontSize));
        descriptionText.name = "Description";
        descriptionText.anchor = new Vector2(0.5f, 0f);
        descriptionText.position = new Vector3(centerX, descriptionY, 0f);
        descriptionText.renderOrder = 2;
        descriptionText.outlineWidth = 0;
        descriptionText.RenderTexture();

        panel.size.X = descriptionText.size.X + 50f;

        optionList = AddChild(new UIMenu($"{title} options"));
        optionList.renderOrder = 3;
        optionList.position = new Vector3(centerX, descriptionY + DescriptionFontSize * 3 + descriptionToOptionsGap, 0f);
        optionList.itemOffset = new Vector3(0f, ButtonSpacing, 0f);
        optionList.itemDefault = BuildOptionDefault;
        optionList.dontSerialize = true;

        if (cancellable)
        {
            optionList.onCancel = () => RequestClose(null);
        }

        UIMenuItem[] entries = new UIMenuItem[options.Length];
        for (int i = 0; i < options.Length; i++)
        {
            Action action = actions[i];
            entries[i] = new UIMenuItem(options[i], () => RequestClose(action));
        }

        optionList.SetEntries(entries);

        //holding focus is what stops everything under it reading input
        focused = true;
        UIFocus.Push(optionList);
    }

    //one option: white normally, a yellow-to-orange gradient when it is the selected one
    private static UIGroup BuildOptionDefault()
    {
        UIGroup root = new("Option");

        UIText label = root.AddChild(new UIText(string.Empty, OptionFontSize));
        label.name = "Label";
        label.anchor = new Vector2(0.5f, 0f);
        label.position = new Vector3(-5f, 2f, 0f);
        label.bindings.Add(new UIBinding("text", "Item.Label"));
        label.bindings.Add(new UIBinding("isVisible", "IsSelected") { invert = true });

        UIText selected = root.AddChild(new UIText(string.Empty, OptionFontSize));
        selected.name = "LabelSelected";
        selected.anchor = new Vector2(0.5f, 0f);
        selected.position = new Vector3(-5f, 2f, 0f);
        selected.bindings.Add(new UIBinding("text", "Item.Label"));
        selected.fillGradientMode = UiTextGradientMode.Vertical;
        selected.fillGradientTopColor = new Color4(1f, 1f, 0f);
        selected.fillGradientBottomColor = new Color4(1f, 0.27f, 0f);
        selected.bindings.Add(new UIBinding("isVisible", "IsSelected"));

        return root;
    }

    /// <summary>
    /// Shows a modal and returns a task that completes with the index of the chosen option, or -1
    /// if it is cancelled/dismissed. Safe to call from any thread: the dialog (which creates GL
    /// textures) is built on the main thread via <see cref="CDTXMania.RunOnMainThread"/>, and the
    /// task's continuation runs on the thread pool, so a background task can simply do:
    /// <c>int choice = await Modal.ShowAsync(CDTXMania.persistentUIGroup, title, desc, options);</c>
    /// </summary>
    public static Task<int> ShowAsync(UIGroup parent, string title, string description, string[] options, bool cancellable = true)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(options);

        //RunContinuationsAsynchronously: when the choice is made on the main thread, the awaiting
        //scan code resumes on the thread pool instead of running inline during the frame's Draw
        TaskCompletionSource<int> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        Action[] actions = new Action[options.Length];
        for (int i = 0; i < options.Length; i++)
        {
            int index = i;
            actions[i] = () => completion.TrySetResult(index);
        }

        CDTXMania.RunOnMainThread(() =>
        {
            Modal modal = new(title, description, options, actions, cancellable) { completionSource = completion };
            parent.AddChild(modal);
        });

        return completion.Task;
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        //deferred to here: an option's action disposes this dialog, which is unsafe mid-draw
        if (closeRequested)
        {
            FinishClose();
            return;
        }

        base.Draw(parentMatrix);
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

        Action? action = pendingAction;
        pendingAction = null;

        parent?.RemoveChild(this);

        // Run the chosen option's action first (for ShowAsync this resolves the task with the
        // chosen index), then dispose. Dispose resolves the task with -1 as a fallback, which is a
        // no-op once the action has already resolved it (this is how Cancel produces -1).
        action?.Invoke();
        Dispose();
    }

    public override void Dispose()
    {
        //covers both the normal close path and being torn down from outside, e.g. the parent group being
        //cleared: either way focus must go back to whoever had it
        if (focused)
        {
            focused = false;
            UIFocus.Pop(optionList);
        }

        // Release any ShowAsync awaiter even if the dialog is torn down without a choice. No-op if
        // an option's action already set the result.
        completionSource?.TrySetResult(-1);

        base.Dispose();
    }
}