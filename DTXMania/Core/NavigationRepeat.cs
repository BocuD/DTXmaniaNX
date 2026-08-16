using FDK;
using SlimDXKey = SlimDX.DirectInput.Key;

namespace DTXMania.Core;

/// <summary>
/// Up/down navigation with key repeat, owned by whatever reads it. The repeat state lives in these
/// counters, so there is one set per consumer: sharing them means whoever polls first takes the other's
/// repeats, which is what a single shared set used to do.
/// </summary>
public sealed class NavigationRepeat
{
    private const int FirstRepeatMs = 400;
    private const int RepeatIntervalMs = 25;

    //built on the first poll, since a consumer can be constructed before CDTXMania.Timer exists
    private CCounter? up;
    private CCounter? down;
    private CCounter? neckR;
    private CCounter? neckG;

    /// <summary>
    /// Runs <paramref name="onUp"/>/<paramref name="onDown"/> for the keyboard arrows and the GB neck
    /// (held, repeating), and for the drum toms (single press). The toms take
    /// <paramref name="onDrumsUp"/>/<paramref name="onDrumsDown"/> when given, which is how the settings
    /// list reverses the direction while editing a value.
    /// </summary>
    public void Poll(Action onUp, Action onDown, Action? onDrumsUp = null, Action? onDrumsDown = null)
    {
        if (up == null)
        {
            up = new CCounter(0, 0, 0, CDTXMania.Timer);
            down = new CCounter(0, 0, 0, CDTXMania.Timer);
            neckR = new CCounter(0, 0, 0, CDTXMania.Timer);
            neckG = new CCounter(0, 0, 0, CDTXMania.Timer);
        }

        //passed straight through: wrapping them would allocate a closure per polled frame
        up.tRepeatKey(CDTXMania.InputManager.Keyboard.bKeyPressing(SlimDXKey.UpArrow),
            onUp, FirstRepeatMs, RepeatIntervalMs);
        down!.tRepeatKey(CDTXMania.InputManager.Keyboard.bKeyPressing(SlimDXKey.DownArrow),
            onDown, FirstRepeatMs, RepeatIntervalMs);

        neckR!.tRepeatKey(CDTXMania.Pad.bPressingGB(EPad.R), onUp, FirstRepeatMs, RepeatIntervalMs);
        neckG!.tRepeatKey(CDTXMania.Pad.bPressingGB(EPad.G), onDown, FirstRepeatMs, RepeatIntervalMs);

        if (CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.HT)) (onDrumsUp ?? onUp)();
        if (CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.LT)) (onDrumsDown ?? onDown)();
    }

    /// <summary>
    /// Drops any repeat in flight. Called by <see cref="DTXMania.UI.UIFocus"/> when focus moves, so a
    /// handler that gains focus with a key already held starts from a fresh press rather than inheriting
    /// a repeat that was meant for whoever had focus before.
    /// </summary>
    public void Reset()
    {
        if (up == null)
        {
            return;
        }

        up.nCurrentValue = 0;
        down!.nCurrentValue = 0;
        neckR!.nCurrentValue = 0;
        neckG!.nCurrentValue = 0;
    }
}
