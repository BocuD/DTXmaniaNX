namespace DTXMania.Core;

using FDK;
using SlimDXKey = SlimDX.DirectInput.Key;

public class Input
{
    public bool ActionDecide()
    {
        return CDTXMania.Pad.bPressedDGB(EPad.Decide) ||
               CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.CY) ||
               CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.RD) ||
               (CDTXMania.Pad.bPressingGB(EPad.P) && CDTXMania.Pad.bPressedGB(EPad.Pick)) ||
               CDTXMania.InputManager.Keyboard.bKeyPressed(SlimDXKey.Return) ||
               CDTXMania.InputManager.Keyboard.bKeyPressed(SlimDXKey.NumberPadEnter);
    }

    public bool ActionCancel()
    {
        return CDTXMania.InputManager.Keyboard.bKeyPressed(SlimDXKey.Escape) ||
                (CDTXMania.Pad.bPressingGB(EPad.Y) && CDTXMania.Pad.bPressedGB(EPad.Pick)) ||
                CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.LC) || CDTXMania.Pad.bPressedGB(EPad.Cancel);
    }

    // ---- shared up/down list navigation (keyboard arrows, GB neck R/G, drums toms HT/LT) ----
    // The repeat counters are static: there is a single Input instance and only one navigation
    // consumer polls per frame (stages don't overlap in time). They are created in the constructor,
    // which runs after CDTXMania.Timer is initialized (the "Input" initializer follows "Timer").

    private const int FirstRepeatMs = 400;
    private const int RepeatIntervalMs = 25;

    private static CCounter navUp;
    private static CCounter navDown;
    private static CCounter navR;
    private static CCounter navG;

    public Input()
    {
        navUp = new CCounter(0, 0, 0, CDTXMania.Timer);
        navDown = new CCounter(0, 0, 0, CDTXMania.Timer);
        navR = new CCounter(0, 0, 0, CDTXMania.Timer);
        navG = new CCounter(0, 0, 0, CDTXMania.Timer);
    }

    /// <summary>
    /// Runs <paramref name="onUp"/>/<paramref name="onDown"/> for up/down navigation from the keyboard
    /// arrows, GB neck (R/G, held-repeat) and drums toms (HT/LT, single press). The drums toms use
    /// <paramref name="onDrumsUp"/>/<paramref name="onDrumsDown"/> when given (e.g. the config list
    /// reverses the integer-edit direction for drums); otherwise they fall back to the standard actions.
    /// </summary>
    public void Navigate(Action onUp, Action onDown, Action? onDrumsUp = null, Action? onDrumsDown = null)
    {
        navUp.tRepeatKey(CDTXMania.InputManager.Keyboard.bKeyPressing(SlimDXKey.UpArrow),
            () => onUp(), FirstRepeatMs, RepeatIntervalMs);
        navDown.tRepeatKey(CDTXMania.InputManager.Keyboard.bKeyPressing(SlimDXKey.DownArrow),
            () => onDown(), FirstRepeatMs, RepeatIntervalMs);

        navR.tRepeatKey(CDTXMania.Pad.bPressingGB(EPad.R), () => onUp(), FirstRepeatMs, RepeatIntervalMs);
        navG.tRepeatKey(CDTXMania.Pad.bPressingGB(EPad.G), () => onDown(), FirstRepeatMs, RepeatIntervalMs);

        if (CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.HT)) (onDrumsUp ?? onUp)();
        if (CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.LT)) (onDrumsDown ?? onDown)();
    }

    /// <summary>
    /// Clears the navigation repeat state. Because the counters are shared, a consumer that can
    /// become active alongside another (the quick menu overlays the song-select list) resets them
    /// on open/close so a held-key repeat can't carry over between the two.
    /// </summary>
    public void ResetNavigation()
    {
        navUp.nCurrentValue = 0;
        navDown.nCurrentValue = 0;
        navR.nCurrentValue = 0;
        navG.nCurrentValue = 0;
    }
}