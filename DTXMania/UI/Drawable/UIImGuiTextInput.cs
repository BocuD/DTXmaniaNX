using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Inspector;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Drawable;

/// <summary>
/// Temporary text input drawable that piggybacks on ImGui input handling (including IME)
/// </summary>
public class UIImGuiTextInput : UIText
{
    [Themable] public string placeholder = "Input text...";
    [Themable] public int maxLength = 128;
    [Themable] public bool autoSelectAllOnFocus = true;
    [Themable] public float placeholderOpacity = 0.45f;
    [Themable] public bool enableImGuiTextRendering;
    [Themable] public bool keepFocusEachFrame;

    private bool _isActive;
    private bool _requestKeyboardFocus;
    private string _textBeforeSession = string.Empty;
    private string _editText = string.Empty;
    private string _lastRenderedText = string.Empty;
    private bool _lastRenderedPlaceholder;
    private Action<string>? _onCommit;
    private Action? _onCancel;
    private ImFontPtr _scaledFont;
    private bool _scaledFontOwned;

    private static UIImGuiTextInput? _activeInput;

    [AddChildMenu]
    public new static UIDrawable Create()
    {
        return new UIImGuiTextInput();
    }

    public UIImGuiTextInput()
    {
        name = "ImGuiTextInput";
        textSource = TextSource.String;
        size = new Vector2(320, 30);
        // start with empty text so placeholder is shown by default
        text = string.Empty;
    }

    public bool IsActive => _isActive;

    public static bool IsAnyInputActive => _activeInput is { _isActive: true };

    public static void CancelActiveInput()
    {
        if (_activeInput != null)
        {
            _activeInput.CancelInternal();
            _activeInput = null;
        }
    }
    
    public void ActivateTextInput(string? initialText = null, Action<string>? onCommit = null, Action? onCancel = null)
    {
        if (_activeInput != null && _activeInput != this)
        {
            _activeInput.CancelInternal();
        }

        _textBeforeSession = text;
        _editText = initialText ?? text;
        _onCommit = onCommit;
        _onCancel = onCancel;
        _isActive = true;
        _requestKeyboardFocus = true;
        _activeInput = this;

        // Create a scaled font based on UIText's font settings
        CreateScaledFont();
    }

    public void DeactivateTextInput(bool commit)
    {
        if (commit)
        {
            CommitInternal();
            return;
        }

        CancelInternal();
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        if (!isVisible)
        {
            if (_isActive)
            {
                CancelInternal();
            }

            return;
        }

        string displayText = _isActive ? _editText : text;
        bool isPlaceholder = string.IsNullOrEmpty(displayText);
        string renderText = isPlaceholder && !string.IsNullOrWhiteSpace(placeholder) ? placeholder : displayText;
        bool isPlaceholderRender = isPlaceholder && !string.IsNullOrWhiteSpace(placeholder);

        if (!_isActive)
        {
            if (!string.IsNullOrEmpty(renderText))
            {
                EnsureDisplayedTexture(renderText, isPlaceholderRender);
                DrawDisplayedText(parentMatrix, isPlaceholderRender);
            }

            return;
        }

        UpdateLocalTransformMatrix();
        Matrix4x4 combinedMatrix = localTransformMatrix * parentMatrix;

        Vector2 screenPosition = new(combinedMatrix.M41, combinedMatrix.M42);
        
        // compute a more accurate desired height so IME/cursor positioning and input area
        // match the UIText font size and the UI render scale
        float desiredHeight = MathF.Max(size.Y, fontSize * CDTXMania.renderScale);
        desiredHeight = MathF.Max(desiredHeight, ImGui.GetFrameHeight());
        Vector2 windowSize = new(MathF.Max(size.X + 100f, 120f), desiredHeight);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        
        // Push the scaled font if available
        if (!_scaledFont.IsNull)
        {
            ImGui.PushFont(_scaledFont, fontSize * CDTXMania.renderScale);
        }
        
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0f, 0f, 0f, 0f));
        ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0f, 0f, 0f, 0f));
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0f, 0f, 0f, 0f));
        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0f, 0f, 0f, 0f));
        ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0f, 0f, 0f, 0f));
        
        //make text cursor color match text
        ImGui.PushStyleColor(ImGuiCol.InputTextCursor, new Vector4(fillColor.Red, fillColor.Green, fillColor.Blue, fillColor.Alpha));
        
        // Use the text color from the UI theme, preserving the actual color for visibility
        // If ImGui text rendering is disabled, push a fully transparent text color so
        // ImGui still handles input/IME but visual text is drawn by UIText instead.
        Vector4 textColor = enableImGuiTextRendering
            ? new(fillColor.Red, fillColor.Green, fillColor.Blue, fillColor.Alpha)
            : new Vector4(0f, 0f, 0f, 0f);
        
        ImGui.PushStyleColor(ImGuiCol.Text, textColor);
        ImGui.PushStyleColor(ImGuiCol.TextDisabled, textColor);

        bool submitted;
        bool canceled;

        try
        {
            Vector2 outlineOffset = new(outlineWidth);
            Vector2 offset = new(3, 3);
            ImGui.SetNextWindowPos(screenPosition + offset + outlineOffset, ImGuiCond.Always);
            ImGui.SetNextWindowSize(windowSize, ImGuiCond.Always);
            ImGui.SetNextWindowBgAlpha(0f);

            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoDecoration |
                                           ImGuiWindowFlags.NoSavedSettings |
                                           ImGuiWindowFlags.NoMove |
                                           ImGuiWindowFlags.NoScrollbar |
                                           ImGuiWindowFlags.NoScrollWithMouse |
                                           ImGuiWindowFlags.NoNav;

            if (!ImGui.Begin($"##TextInputOverlay_{id}", windowFlags))
            {
                ImGui.End();
                return;
            }

            if (_requestKeyboardFocus)
            {
                ImGui.SetKeyboardFocusHere();
                _requestKeyboardFocus = false;
            }

            // Keep focus on this element every frame if the option is enabled
            if (keepFocusEachFrame)
            {
                ImGui.SetKeyboardFocusHere();
            }

            ImGui.SetNextItemWidth(windowSize.X);
            ImGuiInputTextFlags inputFlags = ImGuiInputTextFlags.None;
            if (autoSelectAllOnFocus)
            {
                inputFlags |= ImGuiInputTextFlags.AutoSelectAll;
            }

            nuint capacity = (nuint)Math.Max(maxLength + 1, 2);
            _ = ImGui.InputText($"##TextInputValue_{id}", ref _editText, capacity, inputFlags);
            bool enterPressed = ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter);
            submitted = ImGui.IsItemFocused() && enterPressed;
            canceled = ImGui.IsKeyPressed(ImGuiKey.Escape);

            ImGui.End();
        }
        finally
        {
            if (!_scaledFont.IsNull)
            {
                ImGui.PopFont();
            }

            ImGui.PopStyleColor(8);
            ImGui.PopStyleVar(4);
        }

        // Sync UIText with the current ImGui edit buffer so visual text updates live.
        string currentDisplay = string.IsNullOrEmpty(_editText) && !string.IsNullOrWhiteSpace(placeholder)
            ? placeholder
            : _editText;
        bool currentIsPlaceholder = string.IsNullOrEmpty(_editText) && !string.IsNullOrWhiteSpace(placeholder);

        if (text != currentDisplay)
        {
            SetText(currentDisplay);
        }

        if (!texture.IsValid() || _lastRenderedText != currentDisplay || _lastRenderedPlaceholder != currentIsPlaceholder)
        {
            RenderTexture();
            _lastRenderedText = currentDisplay;
            _lastRenderedPlaceholder = currentIsPlaceholder;
        }

        // Draw UIText every active frame so typed text is visible immediately.
        if (texture.IsValid())
        {
            DrawDisplayedText(parentMatrix, currentIsPlaceholder);
        }

        // handle submission / cancel after syncing visuals
        if (submitted)
        {
            CommitInternal();
        }
        else if (canceled)
        {
            CancelInternal();
        }
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (!ImGui.CollapsingHeader("UIImGuiTextInput"))
        {
            return;
        }

        ImGui.InputText("Placeholder", ref placeholder, 256);
        ImGui.InputInt("Max Length", ref maxLength);
        maxLength = Math.Max(maxLength, 1);
        ImGui.Checkbox("Auto Select All On Focus", ref autoSelectAllOnFocus);
        ImGui.Checkbox("Keep Focus Each Frame", ref keepFocusEachFrame);
        ImGui.InputFloat("Placeholder Opacity", ref placeholderOpacity, 0.05f, 0.1f, "%.2f");
        placeholderOpacity = Math.Clamp(placeholderOpacity, 0f, 1f);

        ImGui.Checkbox("Enable ImGui Text Rendering", ref enableImGuiTextRendering);

        //debug/readout fields
        float calculatedImGuiFontSize = fontSize * CDTXMania.renderScale;
        float overlayHeight = MathF.Max(size.Y, calculatedImGuiFontSize);
        overlayHeight = MathF.Max(overlayHeight, ImGui.GetFrameHeight());
        ImGui.Text($"Calculated ImGui Font Size: {calculatedImGuiFontSize:F2}");
        ImGui.Text($"Overlay Window Height: {overlayHeight:F2}");

        if (!_isActive)
        {
            if (ImGui.Button("Activate Text Input"))
            {
                ActivateTextInput();
            }
        }
        else
        {
            if (ImGui.Button("Commit (Enter)"))
            {
                CommitInternal();
            }

            ImGui.SameLine();

            if (ImGui.Button("Cancel (Escape)"))
            {
                CancelInternal();
            }
        }
    }

    private void CommitInternal()
    {
        SetText(_editText);
        _isActive = false;
        _requestKeyboardFocus = false;

        Action<string>? onCommit = _onCommit;
        DisposeScaledFont();
        RestorePreviousFocus();
        ClearCallbacks();
        if (_activeInput == this)
        {
            _activeInput = null;
        }

        onCommit?.Invoke(text);
    }

    private void CancelInternal()
    {
        _editText = _textBeforeSession;
        _isActive = false;
        _requestKeyboardFocus = false;

        Action? onCancel = _onCancel;
        DisposeScaledFont();
        RestorePreviousFocus();
        ClearCallbacks();
        if (_activeInput == this)
        {
            _activeInput = null;
        }

        // restore the original UIText value
        SetText(_textBeforeSession);
        RenderTexture();

        onCancel?.Invoke();
    }

    private void ClearCallbacks()
    {
        _onCommit = null;
        _onCancel = null;
    }

    private void RestorePreviousFocus()
    {
        // Clear ImGui focus to prevent capturing game input when inspector is not active.
        // This returns input control to the game layer.
        ImGui.SetWindowFocus((string?)null);
    }

    private void CreateScaledFont()
    {
        DisposeScaledFont();

        // Resolve the font path based on UIText's font settings
        string fontPath = UIFonts.ResolveFontPath(fontSource, font);
        if (string.IsNullOrWhiteSpace(fontPath) || !File.Exists(fontPath))
        {
            return;
        }

        // Calculate the scaled font size
        float scaledFontSize = fontSize * CDTXMania.renderScale;

        // Create the font at the scaled size
        ImFontPtr newFont = ImGui.AddFontFromFileTTF(ImGui.GetIO().Fonts, fontPath, scaledFontSize);
        if (!newFont.IsNull)
        {
            _scaledFont = newFont;
            _scaledFontOwned = true;
            // Rebuild the font atlas to apply the new font
            //ImGui.GetIO().Fonts.Build();
        }
    }

    private void DisposeScaledFont()
    {
        if (!_scaledFont.IsNull && _scaledFontOwned)
        {
            // Remove the font from the ImGui font atlas (ImGui 1.92+).
            // This properly cleans up the font without leaving a dangling reference.
            ImGui.RemoveFont(ImGui.GetIO().Fonts, _scaledFont);
            _scaledFont = default;
            _scaledFontOwned = false;
        }
    }

    public override void Dispose()
    {
        DisposeScaledFont();
        base.Dispose();
    }


    private void EnsureDisplayedTexture(string displayText, bool isPlaceholderRender)
    {
        if (texture.IsValid() && _lastRenderedText == displayText && _lastRenderedPlaceholder == isPlaceholderRender)
        {
            return;
        }

        string originalText = text;
        text = displayText;
        RenderTexture();
        text = originalText;

        _lastRenderedText = displayText;
        _lastRenderedPlaceholder = isPlaceholderRender;
    }

    private void DrawDisplayedText(Matrix4x4 parentMatrix, bool isPlaceholderRender)
    {
        UpdateLocalTransformMatrix();
        Matrix4x4 combinedMatrix = localTransformMatrix * parentMatrix;

        Color4 drawColor = isPlaceholderRender
            ? new Color4(color.Red, color.Green, color.Blue, color.Alpha * Math.Clamp(placeholderOpacity, 0f, 1f))
            : color;

        texture.tDraw2DMatrix(combinedMatrix, size, new RectangleF(0, 0, texture.Width, texture.Height), drawColor);
    }
}

