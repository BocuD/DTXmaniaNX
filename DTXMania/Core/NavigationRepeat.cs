using FDK;
using SlimDXKey = SlimDX.DirectInput.Key;

namespace DTXMania.Core;

/// <summary>
/// Navigation along one axis with key repeat, owned by whatever reads it. The repeat state lives in these
/// counters, so there is one set per consumer: sharing them means whoever polls first takes the other's
/// repeats, which is what a single shared set used to do.
/// </summary>
public sealed class NavigationRepeat
{
    private const int FirstRepeatMs = 400;
    private const int RepeatIntervalMs = 25;

    private readonly SlimDXKey keyPrevious;
    private readonly SlimDXKey keyNext;
    private readonly EPad guitarPrevious;
    private readonly EPad guitarNext;
    private readonly EPad drumPrevious;
    private readonly EPad drumNext;

    //built on the first poll, since a consumer can be constructed before CDTXMania.Timer exists
    private CCounter? keyRepeatPrevious;
    private CCounter? keyRepeatNext;
    private CCounter? guitarRepeatPrevious;
    private CCounter? guitarRepeatNext;

    private NavigationRepeat(SlimDXKey keyPrevious, SlimDXKey keyNext,
        EPad guitarPrevious, EPad guitarNext, EPad drumPrevious, EPad drumNext)
    {
        this.keyPrevious = keyPrevious;
        this.keyNext = keyNext;
        this.guitarPrevious = guitarPrevious;
        this.guitarNext = guitarNext;
        this.drumPrevious = drumPrevious;
        this.drumNext = drumNext;
    }

    public static NavigationRepeat Vertical(bool useNeck = false) =>
        new(SlimDXKey.UpArrow, SlimDXKey.DownArrow,
            useNeck ? EPad.R : EPad.PickUp, useNeck ? EPad.G : EPad.PickDown,
            EPad.HT, EPad.LT);

    public static NavigationRepeat Horizontal() =>
        new(SlimDXKey.LeftArrow, SlimDXKey.RightArrow,
            EPad.PickDown, EPad.PickUp, EPad.SD, EPad.FT);

    /// <summary>
    /// Runs <paramref name="onPrevious"/>/<paramref name="onNext"/> for the arrow keys and the guitar
    /// (held, repeating), and for the drum pads (single press). The drums take
    /// <paramref name="onDrumsPrevious"/>/<paramref name="onDrumsNext"/> when given, which is how the
    /// settings list reverses the direction while editing a value.
    /// </summary>
    public void Poll(Action onPrevious, Action onNext, Action? onDrumsPrevious = null, Action? onDrumsNext = null)
    {
        if (keyRepeatPrevious == null)
        {
            keyRepeatPrevious = new CCounter(0, 0, 0, CDTXMania.Timer);
            keyRepeatNext = new CCounter(0, 0, 0, CDTXMania.Timer);
            guitarRepeatPrevious = new CCounter(0, 0, 0, CDTXMania.Timer);
            guitarRepeatNext = new CCounter(0, 0, 0, CDTXMania.Timer);
        }

        //passed straight through: wrapping them would allocate a closure per polled frame
        keyRepeatPrevious.tRepeatKey(CDTXMania.InputManager.Keyboard.bKeyPressing(keyPrevious),
            onPrevious, FirstRepeatMs, RepeatIntervalMs);
        keyRepeatNext!.tRepeatKey(CDTXMania.InputManager.Keyboard.bKeyPressing(keyNext),
            onNext, FirstRepeatMs, RepeatIntervalMs);

        //the neck has no double duty, but a strum held under P or Y is on its way to deciding or
        //cancelling and must not scroll the list out from under that
        bool strums = guitarPrevious is EPad.PickUp or EPad.PickDown;
        bool guitarScrolls = !strums || Input.StrumIsNavigation;

        guitarRepeatPrevious!.tRepeatKey(guitarScrolls && CDTXMania.Pad.bPressingGB(guitarPrevious),
            onPrevious, FirstRepeatMs, RepeatIntervalMs);
        guitarRepeatNext!.tRepeatKey(guitarScrolls && CDTXMania.Pad.bPressingGB(guitarNext),
            onNext, FirstRepeatMs, RepeatIntervalMs);

        if (CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, drumPrevious)) (onDrumsPrevious ?? onPrevious)();
        if (CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, drumNext)) (onDrumsNext ?? onNext)();
    }

    /// <summary>
    /// Drops any repeat in flight. Called by <see cref="DTXMania.UI.UIFocus"/> when focus moves, so a
    /// handler that gains focus with a key already held starts from a fresh press rather than inheriting
    /// a repeat that was meant for whoever had focus before.
    /// </summary>
    public void Reset()
    {
        if (keyRepeatPrevious == null)
        {
            return;
        }

        keyRepeatPrevious.nCurrentValue = 0;
        keyRepeatNext!.nCurrentValue = 0;
        guitarRepeatPrevious!.nCurrentValue = 0;
        guitarRepeatNext!.nCurrentValue = 0;
    }
}
