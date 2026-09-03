namespace DTXMania.Core;

using SlimDXKey = SlimDX.DirectInput.Key;

/// <summary>
/// What the buttons mean. Nothing here decides whether the caller is allowed to read them — that is
/// <see cref="UI.UIFocus"/>'s job, and only the handler holding focus is polled.
/// </summary>
public class Input
{
    public bool ActionDecide()
    {
        return CDTXMania.Pad.bPressedDGB(EPad.Decide) ||
               CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.CY) ||
               CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.RD) ||
               (CDTXMania.Pad.bPressingGB(EPad.P) && Strummed()) ||
               CDTXMania.InputManager.Keyboard.bKeyPressed(SlimDXKey.Return) ||
               CDTXMania.InputManager.Keyboard.bKeyPressed(SlimDXKey.NumberPadEnter);
    }

    public bool ActionCancel()
    {
        return CDTXMania.InputManager.Keyboard.bKeyPressed(SlimDXKey.Escape) ||
               (CDTXMania.Pad.bPressingGB(EPad.Y) && Strummed()) ||
               CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.LC) || CDTXMania.Pad.bPressedGB(EPad.Cancel);
    }

    //a strum on guitar or bass, whichever way it was made
    private static bool Strummed() =>
        CDTXMania.Pad.bPressedGB(EPad.Pick) ||
        CDTXMania.Pad.bPressedGB(EPad.PickUp) ||
        CDTXMania.Pad.bPressedGB(EPad.PickDown);

    public static bool StrumIsNavigation =>
        !CDTXMania.Pad.bPressingGB(EPad.P) && !CDTXMania.Pad.bPressingGB(EPad.Y);
}
