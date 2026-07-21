using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using FDK;
using SlimDXKey = SlimDX.DirectInput.Key;
using STKEYASSIGN = DTXMania.Core.CConfigIni.CKeyAssign.STKEYASSIGN;

namespace DTXMania.UI.Config;

internal sealed class MidiTestPanel : UIGroup
{
    private const int SlotCount = CConfigIni.CKeyAssign.KeyAssignsPerPad;

    private static readonly Color4 MappedColor = new(0.45f, 1f, 0.45f, 1f);
    private static readonly Color4 UnmappedColor = new(1f, 0.45f, 0.45f, 1f);

    private readonly UIText title;
    private readonly UIText hint;
    private readonly ScrollingLog log;

    public Action? onClose;
    public bool IsOpen => isVisible;

    public MidiTestPanel() : base("MidiTestPanel")
    {
        dontSerialize = true;

        title = AddChild(new UIText("", 22f));

        hint = AddChild(new UIText("", 15f));
        hint.position = new Vector3(0, 28, 0);
        hint.fillColor = new Color4(0.7f, 0.85f, 1f, 1f);

        log = AddChild(new ScrollingLog(18));
        log.position = new Vector3(20, 60, 0);
    }

    public void Open()
    {
        title.SetText(CDTXMania.isJapanese ? "MIDI テスト" : "MIDI Test");
        hint.SetText(CDTXMania.isJapanese
            ? "MIDI入力を押してください。緑=割り当て済み / 赤=未割り当て。 Esc で戻ります。"
            : "Play your MIDI inputs.  green = mapped / red = unmapped.  (Esc to return)");
        log.Clear();
        isVisible = true;
    }

    public void UpdatePreview()
    {
        if (!isVisible) return;

        foreach (IInputDevice device in CDTXMania.InputManager.listInputDevices)
        {
            if (device.eInputDeviceType != EInputDeviceType.MidiIn) continue;

            for (int code = 0; code < 256; code++)
            {
                if (!device.bKeyPressed(code)) continue;

                string midiLabel = KeyCodeNames.FormatBinding(new STKEYASSIGN(EInputDevice.MIDI入力, device.ID, code));
                string? mapped = FindMappedPad(device.ID, code);

                if (mapped != null)
                {
                    log.Add(CDTXMania.isJapanese ? $"{midiLabel}  ->  {mapped}" : $"{midiLabel}  ->  {mapped}", MappedColor);
                }
                else
                {
                    log.Add(CDTXMania.isJapanese ? $"{midiLabel}  (未割り当て)" : $"{midiLabel}  (unmapped)", UnmappedColor);
                }
            }
        }
    }

    public void PollClose()
    {
        if (isVisible && CDTXMania.InputManager.Keyboard.bKeyPressed((int)SlimDXKey.Escape))
        {
            CDTXMania.Skin.soundCancel.tPlay();
            Close();
        }
    }

    private static string? FindMappedPad(int id, int code)
    {
        for (int p = 0; p <= (int)EKeyConfigPart.SYSTEM; p++)
        {
            for (int b = 0; b < (int)EKeyConfigPad.MAX; b++)
            {
                STKEYASSIGN[] slots = CDTXMania.ConfigIni.KeyAssign[p][b];
                for (int s = 0; s < SlotCount; s++)
                {
                    if (slots[s].InputDevice == EInputDevice.MIDI入力 && slots[s].ID == id && slots[s].Code == code)
                    {
                        return $"{(EKeyConfigPart)p} / {(EKeyConfigPad)b}";
                    }
                }
            }
        }
        return null;
    }

    private void Close()
    {
        isVisible = false;
        onClose?.Invoke();
    }
}
