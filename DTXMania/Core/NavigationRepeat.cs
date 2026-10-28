using DTXMania.UI.Drawable;
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

    //built on the first poll, since a consumer can be constructed before CDTXMania.Timer exists
    private CCounter? keyRepeatPrevious;
    private CCounter? keyRepeatNext;
    private CCounter? guitarRepeatPrevious;
    private CCounter? guitarRepeatNext;

    /// <summary>
    /// Runs <paramref name="onPrevious"/>/<paramref name="onNext"/> for the arrow keys and the guitar
    /// (held, repeating), and for the drum pads (single press). The drums take
    /// <paramref name="onDrumsPrevious"/>/<paramref name="onDrumsNext"/> when given, which is how the
    /// settings list reverses the direction while editing a value.
    /// </summary>
    public void Poll(UINavigationAxis axis, UIGuitarNavigation guitar, Action onPrevious, Action onNext,
        Action? onDrumsPrevious = null, Action? onDrumsNext = null)
    {
        if (keyRepeatPrevious == null)
        {
            keyRepeatPrevious = new CCounter(0, 0, 0, CDTXMania.Timer);
            keyRepeatNext = new CCounter(0, 0, 0, CDTXMania.Timer);
            guitarRepeatPrevious = new CCounter(0, 0, 0, CDTXMania.Timer);
            guitarRepeatNext = new CCounter(0, 0, 0, CDTXMania.Timer);
        }

        bool vertical = axis == UINavigationAxis.Vertical;

        //passed straight through: wrapping them would allocate a closure per polled frame
        keyRepeatPrevious.tRepeatKey(
            CDTXMania.InputManager.Keyboard.bKeyPressing(vertical ? SlimDXKey.UpArrow : SlimDXKey.LeftArrow),
            onPrevious, FirstRepeatMs, RepeatIntervalMs);
        keyRepeatNext!.tRepeatKey(
            CDTXMania.InputManager.Keyboard.bKeyPressing(vertical ? SlimDXKey.DownArrow : SlimDXKey.RightArrow),
            onNext, FirstRepeatMs, RepeatIntervalMs);

        (EPad guitarPrevious, EPad guitarNext) = GuitarPads(vertical, guitar);

        //the neck has no double duty, but a strum held under P or Y is on its way to deciding or
        //cancelling and must not scroll the list out from under that
        bool strums = guitarPrevious is EPad.PickUp or EPad.PickDown;
        bool guitarScrolls = !strums || Input.StrumIsNavigation;

        guitarRepeatPrevious!.tRepeatKey(guitarScrolls && CDTXMania.Pad.bPressingGB(guitarPrevious),
            onPrevious, FirstRepeatMs, RepeatIntervalMs);
        guitarRepeatNext!.tRepeatKey(guitarScrolls && CDTXMania.Pad.bPressingGB(guitarNext),
            onNext, FirstRepeatMs, RepeatIntervalMs);

        if (CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, vertical ? EPad.HT : EPad.SD))
        {
            (onDrumsPrevious ?? onPrevious)();
        }

        if (CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, vertical ? EPad.LT : EPad.FT))
        {
            (onDrumsNext ?? onNext)();
        }
    }

    private static (EPad Previous, EPad Next) GuitarPads(bool vertical, UIGuitarNavigation guitar)
    {
        bool neck = guitar switch
        {
            UIGuitarNavigation.Neck => true,
            UIGuitarNavigation.Strum => false,

            //the setting only covers moving up and down
            _ => vertical && !CDTXMania.ConfigIni.bStrumScrollsMenus
        };

        if (neck)
        {
            return (EPad.R, EPad.G);
        }

        return vertical ? (EPad.PickUp, EPad.PickDown) : (EPad.PickDown, EPad.PickUp);
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
