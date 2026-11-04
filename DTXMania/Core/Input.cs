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
        return CDTXMania.Pad.bPressedAny(EPad.Decide) ||
               CDTXMania.Pad.bPressed(EPlayer.Drums, EPad.CY) ||
               CDTXMania.Pad.bPressed(EPlayer.Drums, EPad.RD) ||
               (CDTXMania.Pad.bPressingGuitars(EPad.P) && Strummed()) ||
               CDTXMania.InputManager.Keyboard.bKeyPressed(SlimDXKey.Return) ||
               CDTXMania.InputManager.Keyboard.bKeyPressed(SlimDXKey.NumberPadEnter);
    }

    public bool ActionCancel()
    {
        return CDTXMania.InputManager.Keyboard.bKeyPressed(SlimDXKey.Escape) ||
               (CDTXMania.Pad.bPressingGuitars(EPad.Y) && Strummed()) ||
               CDTXMania.Pad.bPressed(EPlayer.Drums, EPad.LC) || CDTXMania.Pad.bPressedGuitars(EPad.Cancel);
    }

    //a strum from either guitar player, whichever way it was made
    private static bool Strummed() =>
        CDTXMania.Pad.bPressedGuitars(EPad.Pick) ||
        CDTXMania.Pad.bPressedGuitars(EPad.PickUp) ||
        CDTXMania.Pad.bPressedGuitars(EPad.PickDown);

    public static bool StrumIsNavigation =>
        !CDTXMania.Pad.bPressingGuitars(EPad.P) && !CDTXMania.Pad.bPressingGuitars(EPad.Y);
}
