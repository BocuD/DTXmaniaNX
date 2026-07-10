using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using FDK;
using SlimDXKey = SlimDX.DirectInput.Key;
using STKEYASSIGN = DTXMania.Core.CConfigIni.CKeyAssign.STKEYASSIGN;

namespace DTXMania.UI.Config;

/// <summary>
/// Editor for a single pad's input bindings, shown as a stage overlay by the config stage. Replaces
/// the old bitmap-font CActConfigKeyAssign: rows are <see cref="UIText"/>, and the host stage drives
/// it via <see cref="MoveUp"/>/<see cref="MoveDown"/>/<see cref="Confirm"/>/<see cref="Cancel"/>/
/// <see cref="DeleteCurrent"/> plus <see cref="PollCapture"/> each frame while waiting for input.
///
/// Bindings are written straight into <see cref="CConfigIni.KeyAssign"/>; the config stage persists
/// Config.ini on exit, so nothing extra is needed here.
/// </summary>
internal sealed class KeyAssignPanel : UIGroup
{
    private const int SlotCount = CConfigIni.CKeyAssign.KeyAssignsPerPad; // 16
    private const int IndexClearAll = SlotCount;      // 16
    private const int IndexReset = SlotCount + 1;     // 17
    private const int IndexReturn = SlotCount + 2;    // 18
    private const int RowCount = SlotCount + 3;       // 19

    private const float RowSpacing = 24f;
    private const float SlotStartY = 40f;
    private const float FooterGap = 12f;

    private static readonly Color4 NormalColor = Color4.White;
    private static readonly Color4 HighlightColor = new(1f, 0.85f, 0.2f, 1f);

    private readonly UIText title;
    private readonly UIText[] rows = new UIText[RowCount]; // 0..15 slots, then ClearAll / Reset / Return
    private readonly UIText note;

    private readonly UIGroup overlay;
    private readonly UIText overlayPrompt;
    private readonly UIText overlayEcho;

    private EKeyConfigPart part;
    private EKeyConfigPad pad;
    private string padName = "";
    private readonly STKEYASSIGN[] snapshot = new STKEYASSIGN[SlotCount];

    private int selectedRow;

    /// <summary>Invoked when the user leaves the editor (Return / Cancel), to hand focus back to the list.</summary>
    public Action? onClose;

    public bool IsOpen => isVisible;
    public bool IsWaiting { get; private set; }

    public KeyAssignPanel() : base("KeyAssignPanel")
    {
        dontSerialize = true;

        title = AddChild(new UIText("", 22f));
        title.position = new Vector3(0, 0, 0);

        for (int i = 0; i < RowCount; i++)
        {
            UIText row = AddChild(new UIText("", 18f));
            row.position = new Vector3(20, RowY(i), 0);
            rows[i] = row;
        }

        note = AddChild(new UIText("", 15f));
        note.position = new Vector3(20, RowY(IndexReturn) + RowSpacing + FooterGap, 0);
        note.fillColor = new Color4(0.7f, 0.85f, 1f, 1f);

        // "press an input" overlay (hidden unless waiting)
        overlay = AddChild(new UIGroup("KeyAssignOverlay"));
        overlay.position = new Vector3(20, 220, 0);
        overlay.renderOrder = 10;
        overlay.isVisible = false;

        overlayPrompt = overlay.AddChild(new UIText("Press an input to assign  (Esc to cancel)", 22f));
        overlayPrompt.fillColor = HighlightColor;
        overlayEcho = overlay.AddChild(new UIText("", 18f));
        overlayEcho.position = new Vector3(0, 32, 0);
    }

    private static float RowY(int index)
    {
        // slots are contiguous; the three footer rows sit below a small gap
        float y = SlotStartY + index * RowSpacing;
        if (index >= IndexClearAll)
        {
            y += FooterGap;
        }
        return y;
    }

    private STKEYASSIGN[] Bindings => CDTXMania.ConfigIni.KeyAssign[(int)part][(int)pad];

    /// <summary>Opens the editor for the given pad and snapshots its bindings for Reset.</summary>
    public void Open(EKeyConfigPart part, EKeyConfigPad pad, string padName)
    {
        this.part = part;
        this.pad = pad;
        this.padName = padName;

        for (int i = 0; i < SlotCount; i++)
        {
            snapshot[i] = Bindings[i];
        }

        IsWaiting = false;
        overlay.isVisible = false;
        note.SetText("");
        selectedRow = 0;
        isVisible = true;

        RefreshRows();
        for (int i = 0; i < RowCount; i++)
        {
            rows[i].fillColor = i == selectedRow ? HighlightColor : NormalColor;
            rows[i].MarkDirty();
        }
    }

    private void RefreshRows()
    {
        title.SetText(CDTXMania.isJapanese ? $"{padName} のキー割り当て" : $"{padName} — Key Assignment");

        STKEYASSIGN[] bindings = Bindings;
        for (int i = 0; i < SlotCount; i++)
        {
            STKEYASSIGN a = bindings[i];
            string label = a.InputDevice == EInputDevice.Unknown ? "(unassigned)" : KeyCodeNames.FormatBinding(a);
            rows[i].SetText($"{i + 1,2}. {label}");
        }

        rows[IndexClearAll].SetText("Clear All");
        rows[IndexReset].SetText("Reset");
        rows[IndexReturn].SetText("<< Return to List");
    }

    #region Input (called by the host stage)

    public void MoveUp() => SetSelected((selectedRow - 1 + RowCount) % RowCount);
    public void MoveDown() => SetSelected((selectedRow + 1) % RowCount);

    private void SetSelected(int newRow)
    {
        if (IsWaiting || newRow == selectedRow) return;

        rows[selectedRow].fillColor = NormalColor;
        rows[selectedRow].MarkDirty();
        selectedRow = newRow;
        rows[selectedRow].fillColor = HighlightColor;
        rows[selectedRow].MarkDirty();

        CDTXMania.Skin.soundCursorMovement.tPlay();
    }

    public void Confirm()
    {
        if (IsWaiting) return;

        if (selectedRow < SlotCount)
        {
            CDTXMania.Skin.soundDecide.tPlay();
            IsWaiting = true;
            overlayEcho.SetText("");
            overlay.isVisible = true;
            note.SetText("");
            return;
        }

        switch (selectedRow)
        {
            case IndexClearAll:
                CDTXMania.Skin.soundDecide.tPlay();
                for (int i = 0; i < SlotCount; i++)
                {
                    Bindings[i] = new STKEYASSIGN(EInputDevice.Unknown, 0, 0);
                }
                RefreshRows();
                note.SetText(CDTXMania.isJapanese ? "すべて消去しました。" : "Cleared all bindings.");
                break;

            case IndexReset:
                CDTXMania.Skin.soundDecide.tPlay();
                for (int i = 0; i < SlotCount; i++)
                {
                    Bindings[i] = snapshot[i];
                }
                RefreshRows();
                note.SetText(CDTXMania.isJapanese ? "元に戻しました。" : "Reset to entry state.");
                break;

            case IndexReturn:
                CDTXMania.Skin.soundDecide.tPlay();
                Close();
                break;
        }
    }

    public void Cancel()
    {
        if (IsWaiting)
        {
            StopWaiting();
            CDTXMania.Skin.soundCancel.tPlay();
            return;
        }

        Close();
    }

    public void DeleteCurrent()
    {
        if (IsWaiting || selectedRow >= SlotCount) return;

        CDTXMania.Skin.soundDecide.tPlay();
        Bindings[selectedRow] = new STKEYASSIGN(EInputDevice.Unknown, 0, 0);
        RefreshRows();
    }

    /// <summary>While waiting, captures the first input pressed (or echoes what's held). Runs every frame.</summary>
    public void PollCapture()
    {
        if (!IsWaiting) return;

        if (CDTXMania.InputManager.Keyboard.bKeyPressed((int)SlimDXKey.Escape))
        {
            StopWaiting();
            CDTXMania.Skin.soundCancel.tPlay();
            return;
        }

        if (TryCapture(pressing: false, out EInputDevice dev, out int id, out int code))
        {
            CommitAssignment(dev, id, code);
            return;
        }

        // live echo of whatever is currently held, before it's committed
        overlayEcho.SetText(TryCapture(pressing: true, out EInputDevice ed, out int eid, out int ecode)
            ? KeyCodeNames.FormatBinding(new STKEYASSIGN(ed, eid, ecode))
            : "...");
    }

    #endregion

    private void CommitAssignment(EInputDevice dev, int id, int code)
    {
        string? movedFrom = FindExistingBinding(dev, id, code);

        // remove this input from every other binding, then set it on the selected slot (old behaviour)
        CDTXMania.ConfigIni.tDeleteAlreadyAssignedInputs(dev, id, code);
        Bindings[selectedRow] = new STKEYASSIGN(dev, id, code);

        CDTXMania.Skin.soundDecide.tPlay();
        StopWaiting();
        RefreshRows();

        note.SetText(movedFrom == null
            ? ""
            : (CDTXMania.isJapanese ? $"{movedFrom} から移動しました。" : $"Moved from {movedFrom}."));
    }

    // Returns a label for another pad this exact input is already bound to, or null if none.
    private string? FindExistingBinding(EInputDevice dev, int id, int code)
    {
        for (int p = 0; p <= (int)EKeyConfigPart.SYSTEM; p++)
        {
            for (int b = 0; b < (int)EKeyConfigPad.MAX; b++)
            {
                STKEYASSIGN[] slots = CDTXMania.ConfigIni.KeyAssign[p][b];
                for (int s = 0; s < SlotCount; s++)
                {
                    if (slots[s].InputDevice == dev && slots[s].ID == id && slots[s].Code == code
                        && !(p == (int)part && b == (int)pad && s == selectedRow))
                    {
                        return $"{(EKeyConfigPart)p} {(EKeyConfigPad)b}";
                    }
                }
            }
        }
        return null;
    }

    private void StopWaiting()
    {
        IsWaiting = false;
        overlay.isVisible = false;
        // consume the press so it doesn't leak into the next frame's list input
        CDTXMania.InputManager.tPolling(CDTXMania.app.bApplicationActive, false);
    }

    private void Close()
    {
        IsWaiting = false;
        overlay.isVisible = false;
        isVisible = false;
        onClose?.Invoke();
    }

    // Scans keyboard / MIDI / joypad / mouse for a pressed (or, for echo, held) input.
    private bool TryCapture(bool pressing, out EInputDevice dev, out int id, out int code)
    {
        // keyboard (skip the navigation keys the editor itself uses)
        for (int i = 0; i < 256; i++)
        {
            if (i is (int)SlimDXKey.Escape or (int)SlimDXKey.UpArrow or (int)SlimDXKey.DownArrow
                or (int)SlimDXKey.LeftArrow or (int)SlimDXKey.RightArrow or (int)SlimDXKey.Delete
                or (int)SlimDXKey.Return)
            {
                continue;
            }
            if (Probe(CDTXMania.InputManager.Keyboard, i, pressing))
            {
                dev = EInputDevice.Keyboard; id = 0; code = i;
                return true;
            }
        }

        foreach (IInputDevice device in CDTXMania.InputManager.listInputDevices)
        {
            if (device.eInputDeviceType == EInputDeviceType.MidiIn)
            {
                for (int i = 0; i < 256; i++)
                {
                    if (Probe(device, i, pressing))
                    {
                        dev = EInputDevice.MIDI入力; id = device.ID; code = i;
                        return true;
                    }
                }
            }
            else if (device.eInputDeviceType == EInputDeviceType.Joystick)
            {
                for (int i = 0; i < 6 + 128 + 8; i++) // axes + buttons + POV hats
                {
                    if (Probe(device, i, pressing))
                    {
                        dev = EInputDevice.Joypad; id = device.ID; code = i;
                        return true;
                    }
                }
            }
        }

        for (int i = 0; i < 8; i++)
        {
            if (Probe(CDTXMania.InputManager.Mouse, i, pressing))
            {
                dev = EInputDevice.Mouse; id = 0; code = i;
                return true;
            }
        }

        dev = EInputDevice.Unknown; id = 0; code = 0;
        return false;
    }

    private static bool Probe(IInputDevice device, int code, bool pressing) =>
        pressing ? device.bKeyPressing(code) : device.bKeyPressed(code);
}
