using DTXMania.Core;
using FDK;
using STKEYASSIGN = DTXMania.Core.CConfigIni.CKeyAssign.STKEYASSIGN;

namespace DTXMania.UI.Config;

internal static class KeyCodeNames
{
    //Ported from CActConfigKeyAssign.keyLabel
    private static readonly Dictionary<int, string> KeyboardNames = new()
    {
        [53] = "Esc",
        [1] = "1", [2] = "2", [3] = "3", [4] = "4", [5] = "5",
        [6] = "6", [7] = "7", [8] = "8", [9] = "9", [0] = "0",
        [83] = "-", [52] = "=", [42] = "Backspace", [129] = "Tab",
        [26] = "Q", [32] = "W", [14] = "E", [27] = "R", [29] = "T",
        [34] = "Y", [30] = "U", [18] = "I", [24] = "O", [25] = "P",
        [74] = "[", [115] = "]", [117] = "Enter", [75] = "L-Ctrl",
        [10] = "A", [28] = "S", [13] = "D", [15] = "F", [16] = "G",
        [17] = "H", [19] = "J", [20] = "K", [21] = "L", [123] = ";",
        [38] = "'", [69] = "`", [78] = "L-Shift", [43] = "\\",
        [35] = "Z", [33] = "X", [12] = "C", [31] = "V", [11] = "B",
        [23] = "N", [22] = "M", [47] = ",", [111] = ".", [124] = "/",
        [120] = "R-Shift", [106] = "NumPad *", [77] = "L-Alt", [126] = "Space", [45] = "CapsLock",
        [54] = "F1", [55] = "F2", [56] = "F3", [57] = "F4", [58] = "F5",
        [59] = "F6", [60] = "F7", [61] = "F8", [62] = "F9", [63] = "F10",
        [88] = "NumLock", [122] = "ScrollLock",
        [96] = "NumPad 7", [97] = "NumPad 8", [98] = "NumPad 9", [102] = "NumPad -",
        [93] = "NumPad 4", [94] = "NumPad 5", [95] = "NumPad 6", [104] = "NumPad +",
        [90] = "NumPad 1", [91] = "NumPad 2", [92] = "NumPad 3", [89] = "NumPad 0", [103] = "NumPad .",
        [64] = "F11", [65] = "F12", [66] = "F13", [67] = "F14", [68] = "F15",
        [72] = "Kana", [36] = "?", [48] = "Henkan", [87] = "MuHenkan", [143] = "\\",
        [37] = "NumPad .", [101] = "NumPad =", [114] = "^", [40] = "@", [46] = ":",
        [130] = "_", [73] = "Kanji", [127] = "Stop", [41] = "AX", [100] = "NumPad Enter",
        [116] = "R-Ctrl", [84] = "Mute", [44] = "Calc", [112] = "Play/Pause", [82] = "Media Stop",
        [133] = "Volume -", [134] = "Volume +", [139] = "Web Home", [99] = "NumPad ,", [105] = "NumPad /",
        [128] = "PrintScreen", [119] = "R-Alt", [110] = "Pause",
        [70] = "Home", [132] = "Up", [109] = "PageUp", [76] = "Left", [118] = "Right",
        [51] = "End", [50] = "Down", [108] = "PageDown", [71] = "Insert", [49] = "Delete",
        [79] = "L-Win", [121] = "R-Win", [39] = "App", [113] = "Power", [125] = "Sleep", [135] = "Wake",
    };

    public static string KeyboardName(int code) =>
        KeyboardNames.TryGetValue(code, out string? name) ? name : $"0x{code:X2}";

    //Formatted readable label for a binding, e.g. "Key Z", "Xbox Controller Button 1", "Mouse Button 0"
    public static string FormatBinding(STKEYASSIGN a)
    {
        switch (a.InputDevice)
        {
            case EInputDevice.Keyboard:
                return $"[Key {KeyboardName(a.Code)}]";
            case EInputDevice.MIDI入力:
                return $"[{DeviceName(EInputDeviceType.MidiIn, a.ID, "MidiIn")} note {a.Code}]";
            case EInputDevice.Joypad:
                return $"[{DeviceName(EInputDeviceType.Joystick, a.ID, "Joypad")} {JoypadCode(a.Code)}]";
            case EInputDevice.Mouse:
                return $"[Mouse Button {a.Code}]";
            default:
                return "";
        }
    }

    //Friendly device name from the connected devices, falling back to "<fallback> #id".
    private static string DeviceName(EInputDeviceType type, int id, string fallback)
    {
        foreach (IInputDevice device in CDTXMania.InputManager.listInputDevices)
        {
            if (device.eInputDeviceType == type && device.ID == id)
            {
                return device.strDeviceName;
            }
        }
        return $"{fallback} #{id}";
    }

    //Joystick axis / button / POV code -> readable name (ported from CActConfigKeyAssign.tDrawAssignedCodeJoypad)
    private static string JoypadCode(int code) => code switch
    {
        0 => "Left",
        1 => "Right",
        2 => "Up",
        3 => "Down",
        4 => "Forward",
        5 => "Back",
        >= 6 and < 6 + 128 => $"Button {code - 5}",
        >= 6 + 128 and < 6 + 128 + 8 => $"POV {(code - 6 - 128) * 45}",
        _ => $"Code {code}",
    };
}