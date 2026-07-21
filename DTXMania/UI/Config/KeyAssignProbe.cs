using DTXMania.Core;
using FDK;
using STKEYASSIGN = DTXMania.Core.CConfigIni.CKeyAssign.STKEYASSIGN;

namespace DTXMania.UI.Config;

internal static class KeyAssignProbe
{
    public static bool Probe(IInputDevice device, int code, bool pressing) =>
        pressing ? device.bKeyPressing(code) : device.bKeyPressed(code);

    public static bool IsBindingPressed(STKEYASSIGN a, bool pressing)
    {
        switch (a.InputDevice)
        {
            case EInputDevice.Keyboard:
                return Probe(CDTXMania.InputManager.Keyboard, a.Code, pressing);

            case EInputDevice.Mouse:
                return Probe(CDTXMania.InputManager.Mouse, a.Code, pressing);

            case EInputDevice.MIDI入力:
            case EInputDevice.Joypad:
                EInputDeviceType wanted = a.InputDevice == EInputDevice.MIDI入力
                    ? EInputDeviceType.MidiIn
                    : EInputDeviceType.Joystick;
                foreach (IInputDevice device in CDTXMania.InputManager.listInputDevices)
                {
                    if (device.ID == a.ID && device.eInputDeviceType == wanted)
                    {
                        return Probe(device, a.Code, pressing);
                    }
                }
                return false;

            default:
                return false;
        }
    }
}
