namespace DTXMania.Core;

using SlimDXKey = SlimDX.DirectInput.Key;

public class Input
{
    public bool ActionDecide()
    {
        return (CDTXMania.Pad.bPressedDGB(EPad.Decide) || CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.CY) ||
                CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.RD)) ||
               (CDTXMania.Pad.bPressingGB(EPad.P) && CDTXMania.Pad.bPressedGB(EPad.Pick)) ||
               (CDTXMania.ConfigIni.bEnterがキー割り当てのどこにも使用されていない &&
                CDTXMania.InputManager.Keyboard.bKeyPressed((int)SlimDXKey.Return));
    }

    public bool ActionCancel()
    {
        return (CDTXMania.InputManager.Keyboard.bKeyPressed((int)SlimDXKey.Escape) || 
                (CDTXMania.Pad.bPressingGB(EPad.Y) && CDTXMania.Pad.bPressedGB(EPad.Pick)) ||
                CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.LC)) || CDTXMania.Pad.bPressedGB(EPad.Cancel);
    }
}