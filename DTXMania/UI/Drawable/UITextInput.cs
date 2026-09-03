using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Inspector;
using DTXMania.UI.Text;
using Hexa.NET.GLFW;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Drawable;

/// <summary>
/// A text field drawn as text: it rasterizes through <see cref="UIText"/> like everything else, so every
/// font, outline and gradient setting applies to what is being typed, and adds a caret, a selection and
/// the editing keys on top.
///
/// Characters come from the window already committed, so an IME phrase arrives the same way a Latin
/// keystroke does and nothing here knows the difference.
/// </summary>
public class UITextInput : UIText, IUIInputHandler
{
    [Themable] public string placeholder = "Input text...";
    [Themable] public int maxLength = 128;
    [Themable] public bool selectAllOnFocus = true;
    [Themable] public float placeholderOpacity = 0.45f;
    [Themable] public Color4 selectionColor = new(0.30f, 0.55f, 1.0f, 0.45f);
    [Themable] public float caretWidth = 2f;

    private const long BlinkMilliseconds = 530;

    //canvas pixels a second, for a drag held outside the field
    private const float DragScrollSpeed = 400f;

    //holding focus is what makes a field the one being typed in, so a field that has lost it — because
    //whatever opened it went away — is not editing, whatever its own state says
    private bool IsEditing => isActive && UIFocus.Holds(this);

    public bool IsActive => IsEditing;
    public static bool IsAnyActive => activeInput is { IsEditing: true };

    private static UITextInput? activeInput;

    //one white pixel, shared by every field and never disposed, for the caret and the selection
    private static BaseTexture? block;

    private readonly TextEditBuffer buffer = new();

    private bool isActive;
    private bool isDraggingSelection;

    //what the field was last drawn with, which is what puts the pointer into its own coordinates
    private Matrix4x4 drawMatrix = Matrix4x4.Identity;

    private string textBeforeSession = string.Empty;
    private Action<string>? onCommit;
    private Action? onCancel;
    private long lastEditTime;
    private long lastDragTime;

    //the part of the rendered text that is on screen, in texture pixels, for text too long for the field
    private float scrollOffset;

    private string? renderedText;
    private BaseTexture placeholderTexture = BaseTexture.None;
    private string? renderedPlaceholder;
    private float renderedPlaceholderScale;

    [AddChildMenu]
    public new static UIDrawable Create()
    {
        return new UITextInput();
    }

    public UITextInput()
    {
        name = "TextInput";
        size = new Vector2(320, 30);
        text = string.Empty;
    }

    public void ActivateTextInput(string? initialText = null, Action<string>? onCommit = null, Action? onCancel = null)
    {
        //re-activating the field that is already open restarts its session rather than cancelling it,
        //which is what a search that found nothing does
        if (activeInput != null && activeInput != this)
        {
            activeInput.Cancel();
        }

        textBeforeSession = text;
        buffer.maxLength = maxLength;
        buffer.Set(initialText ?? text, selectAllOnFocus);

        this.onCommit = onCommit;
        this.onCancel = onCancel;
        isActive = true;
        activeInput = this;
        Edited();

        UIFocus.Push(this);
    }

    public void DeactivateTextInput(bool commit)
    {
        if (commit)
        {
            Commit();
        }
        else
        {
            Cancel();
        }
    }

    public string FocusName => $"TextInput ({name})";

    public void HandleInput()
    {
        if (!IsEditing)
        {
            return;
        }

        HandlePointer();

        foreach (TextInputEvent input in TextInput.events)
        {
            if (input.IsCharacter)
            {
                buffer.Insert(input.character);
                Edited();
                continue;
            }

            HandleKey(input.key, input.mods);

            //the session can end on any key, and what follows it is not ours to read
            if (!isActive)
            {
                return;
            }
        }
    }

    //a press inside the field moves the caret; holding drags a selection, and the drag carries on wherever
    //the pointer goes from there
    private void HandlePointer()
    {
        if (PointerInput.leftPressed && ContainsPointer())
        {
            isDraggingSelection = true;
            lastDragTime = CDTXMania.Timer.nCurrentTime;

            switch (PointerInput.clickCount)
            {
                case 1:
                    buffer.MoveTo(CaretAtPointer(), PointerInput.mods.HasFlag(GlfwMod.Shift));
                    break;

                case 2:
                    buffer.SelectWordAt(CaretAtPointer());
                    break;

                default:
                    buffer.SelectAll();
                    break;
            }

            Edited();
        }

        if (isDraggingSelection && PointerInput.leftDown && PointerInput.clickCount == 1)
        {
            ScrollWhileDraggingOutside();
            buffer.MoveTo(CaretAtPointer(), extend: true);
            Edited();
        }

        if (!PointerInput.leftDown)
        {
            isDraggingSelection = false;
        }
    }

    private bool ContainsPointer()
    {
        Vector2 local = PointerInLocalSpace();
        Vector2 area = HitArea();

        return local.X >= 0f && local.X <= area.X && local.Y >= 0f && local.Y <= area.Y;
    }

    //the claimed box is what can be clicked, not the text: an empty field is still a field
    private Vector2 HitArea()
        => new(size.xMode == UiSizeMode.Fixed ? size.X : FieldWidth(),
               size.yMode == UiSizeMode.Fixed ? size.Y : LineHeight());

    private Vector2 PointerInLocalSpace()
        => Matrix4x4.Invert(drawMatrix, out Matrix4x4 inverse) ? Vector2.Transform(PointerInput.position, inverse) : Vector2.Zero;

    //the caret can only go where the text is on screen, so a pointer past the field asks for the edge of
    //the view and it is the view that has to move
    private int CaretAtPointer()
    {
        if (BaseTexture.SkiaTextRenderer is not { } renderer)
        {
            return buffer.caret;
        }

        float offset = PointerInLocalSpace().X * textureRenderScale + scrollOffset;

        if (Overflowing())
        {
            float margin = CaretMargin();
            offset = Math.Clamp(offset, scrollOffset + margin, scrollOffset + ClipWidth() - margin);
        }

        return renderer.CaretIndexAt(CreateRenderRequest(), offset);
    }

    //a steady rate, because the alternative is the view jumping to wherever the pointer is and a drag an
    //inch outside the field crossing the whole text in a frame
    private void ScrollWhileDraggingOutside()
    {
        long now = CDTXMania.Timer.nCurrentTime;
        float elapsed = MathF.Min((now - lastDragTime) / 1000f, 0.1f);
        lastDragTime = now;

        if (!Overflowing())
        {
            return;
        }

        float pointer = PointerInLocalSpace().X;
        float step = DragScrollSpeed * textureRenderScale * elapsed;

        if (pointer < 0f)
        {
            scrollOffset -= step;
        }
        else if (pointer > HitArea().X)
        {
            scrollOffset += step;
        }

        scrollOffset = Math.Clamp(scrollOffset, 0f, texture.Width - ClipWidth());
    }

    private void HandleKey(GlfwKey key, GlfwMod mods)
    {
        bool extend = mods.HasFlag(GlfwMod.Shift);
        bool wholeWord = mods.HasFlag(GlfwMod.Control);

        switch (key)
        {
            case GlfwKey.Backspace:
                buffer.Backspace(wholeWord);
                break;

            case GlfwKey.Delete:
                buffer.Delete(wholeWord);
                break;

            case GlfwKey.Left:
                buffer.MoveLeft(extend, wholeWord);
                break;

            case GlfwKey.Right:
                buffer.MoveRight(extend, wholeWord);
                break;

            case GlfwKey.Home:
                buffer.MoveHome(extend);
                break;

            case GlfwKey.End:
                buffer.MoveEnd(extend);
                break;

            case GlfwKey.A when wholeWord:
                buffer.SelectAll();
                break;

            case GlfwKey.C when wholeWord:
                if (buffer.HasSelection)
                {
                    TextInput.SetClipboard(buffer.SelectedText);
                }
                break;

            case GlfwKey.X when wholeWord:
                if (buffer.HasSelection)
                {
                    TextInput.SetClipboard(buffer.CutSelection());
                }
                break;

            case GlfwKey.V when wholeWord:
                buffer.Insert(TextInput.GetClipboard());
                break;

            case GlfwKey.Enter:
            case GlfwKey.KpEnter:
                Commit();
                return;

            case GlfwKey.Escape:
                Cancel();
                return;
        }

        Edited();
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        if (!isVisible)
        {
            Cancel();
            return;
        }

        if (IsEditing)
        {
            text = buffer.text;
            RenderNow();
        }

        UpdateLocalTransformMatrix();
        Matrix4x4 combined = localTransformMatrix * parentMatrix;
        drawMatrix = combined;

        if (ShowsPlaceholder())
        {
            DrawPlaceholder(combined);
        }
        else
        {
            FollowCaret();
            DrawSelection(combined);
            base.Draw(parentMatrix);
        }

        if (IsEditing)
        {
            ReportCaretToIme(combined);

            if (CaretIsOn())
            {
                DrawCaret(combined);
            }
        }
    }

    public override void Dispose()
    {
        if (isActive)
        {
            Cancel();
        }

        placeholderTexture.Dispose();
        base.Dispose();
    }

    private bool ShowsPlaceholder() => text.Length == 0 && placeholder.Length > 0;

    //while a field is open the caret is measured against the text, so the two cannot be a frame apart
    private void RenderNow()
    {
        if (text == renderedText && textureRenderScale == CDTXMania.renderScale)
        {
            return;
        }

        RenderTexture();
        renderedText = text;
    }

    private void Commit()
    {
        if (!isActive)
        {
            return;
        }

        text = buffer.text;

        Action<string>? callback = onCommit;
        EndSession();

        CDTXMania.Skin.soundDecide.tPlay();
        callback?.Invoke(text);
    }

    private void Cancel()
    {
        if (!isActive)
        {
            return;
        }

        text = textBeforeSession;

        Action? callback = onCancel;
        EndSession();

        callback?.Invoke();
    }

    //cleared before the callback runs, so a handler that closes the field it was called from finds
    //nothing left to close
    private void EndSession()
    {
        isActive = false;
        onCommit = null;
        onCancel = null;

        if (activeInput == this)
        {
            activeInput = null;
        }

        UIFocus.Remove(this);
    }

    private void Edited() => lastEditTime = CDTXMania.Timer.nCurrentTime;

    private bool CaretIsOn() => (CDTXMania.Timer.nCurrentTime - lastEditTime) / BlinkMilliseconds % 2 == 0;

    //in texture pixels, which is what the clip window and the scroll offset are in
    private float CaretPixels(int index)
        => BaseTexture.SkiaTextRenderer is { } renderer ? renderer.CaretOffset(CreateRenderRequest(), index) : 0f;

    private float ToLocal(float texturePixels) => (texturePixels - scrollOffset) / textureRenderScale;

    private float ClipWidth() => size.xMode == UiSizeMode.Fixed ? size.X * textureRenderScale : 0f;

    //how close to the edge of the view the caret is allowed to sit, so there is always a little text
    //ahead of it to read
    private float CaretMargin() => MathF.Min(caretWidth * 4f * textureRenderScale, ClipWidth() * 0.25f);

    private bool Overflowing() => ClipWidth() > 0f && texture.IsValid() && texture.Width > ClipWidth();

    private float FieldWidth() => Overflowing() ? size.X : MeasuredSize.X;

    private float LineHeight()
    {
        if (texture.IsValid())
        {
            return MeasuredSize.Y;
        }

        return placeholderTexture.IsValid() ? placeholderTexture.Height / textureRenderScale : fontSize;
    }

    private void FollowCaret()
    {
        if (!IsEditing || !Overflowing())
        {
            scrollOffset = 0f;
            return;
        }

        //a drag places the caret at the edge of the view on purpose, so following it as well would fight
        //the steady scroll for the last half character
        if (isDraggingSelection)
        {
            return;
        }

        float clip = ClipWidth();
        float caret = CaretPixels(buffer.caret);
        float margin = CaretMargin();

        scrollOffset = MathF.Min(scrollOffset, caret - margin);
        scrollOffset = MathF.Max(scrollOffset, caret + margin - clip);
        scrollOffset = Math.Clamp(scrollOffset, 0f, texture.Width - clip);
    }

    private void DrawSelection(Matrix4x4 combined)
    {
        if (!IsEditing || !buffer.HasSelection)
        {
            return;
        }

        float from = Math.Clamp(ToLocal(CaretPixels(buffer.SelectionStart)), 0f, FieldWidth());
        float to = Math.Clamp(ToLocal(CaretPixels(buffer.SelectionEnd)), 0f, FieldWidth());

        if (to - from > 0.5f)
        {
            DrawBlock(combined, from, to - from, selectionColor);
        }
    }

    private void DrawCaret(Matrix4x4 combined)
    {
        DrawBlock(combined, CaretLocalX(), caretWidth, fillColor);
    }

    private float CaretLocalX()
    {
        float at = ToLocal(CaretPixels(buffer.caret));

        return Overflowing() ? Math.Clamp(at, 0f, size.X) : at;
    }

    //the IME draws its own composition and candidate list, and needs to be told in window pixels where
    //the text it is composing for is
    private void ReportCaretToIme(Matrix4x4 combined)
    {
        float at = CaretLocalX();
        Vector2 top = InspectorManager.GameToWindow(Vector2.Transform(new Vector2(at, 0f), combined));
        Vector2 bottom = InspectorManager.GameToWindow(Vector2.Transform(new Vector2(at, LineHeight()), combined));

        Ime.SetCaret(top, bottom.Y - top.Y);
    }

    private void DrawBlock(Matrix4x4 combined, float x, float width, Color4 blockColor)
    {
        block ??= BaseTexture.CreateSolidColor(Color4.White);

        Matrix4x4 placed = Matrix4x4.CreateTranslation(x, 0f, 0f) * combined;
        block.tDraw2DMatrix(placed, new Vector2(width, LineHeight()), new RectangleF(0, 0, block.Width, block.Height), blockColor);
    }

    private void DrawPlaceholder(Matrix4x4 combined)
    {
        if (BaseTexture.SkiaTextRenderer is not { } renderer)
        {
            return;
        }

        if (renderedPlaceholder != placeholder || renderedPlaceholderScale != CDTXMania.renderScale)
        {
            placeholderTexture.Dispose();
            placeholderTexture = renderer.Render(CreateRenderRequest() with { Text = placeholder });
            renderedPlaceholder = placeholder;
            renderedPlaceholderScale = textureRenderScale;
        }

        Vector2 drawSize = new Vector2(placeholderTexture.Width, placeholderTexture.Height) / textureRenderScale;
        Color4 faded = new(color.Red, color.Green, color.Blue, color.Alpha * Math.Clamp(placeholderOpacity, 0f, 1f));

        placeholderTexture.tDraw2DMatrix(combined, drawSize,
            new RectangleF(0, 0, placeholderTexture.Width, placeholderTexture.Height), faded);
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (!ImGui.CollapsingHeader("Text Input"))
        {
            return;
        }

        ImGui.InputText("Placeholder", ref placeholder, 256);
        ImGui.InputInt("Max length", ref maxLength);
        maxLength = Math.Max(maxLength, 1);
        ImGui.Checkbox("Select all on focus", ref selectAllOnFocus);
        ImGui.InputFloat("Placeholder opacity", ref placeholderOpacity, 0.05f, 0.1f, "%.2f");
        ImGui.InputFloat("Caret width", ref caretWidth, 0.5f, 1f, "%.1f");

        ImGui.Text(IsEditing ? $"Editing: caret {buffer.caret}, anchor {buffer.anchor}" : "Not editing");

        if (!isActive && ImGui.Button("Activate"))
        {
            ActivateTextInput();
        }
    }

    protected override RectangleF GetTextureSourceRect()
        => Overflowing() ? new RectangleF(scrollOffset, 0f, ClipWidth(), texture.Height) : base.GetTextureSourceRect();

    protected override Vector2 GetTextureDrawSize() => new(FieldWidth(), MeasuredSize.Y);
}
