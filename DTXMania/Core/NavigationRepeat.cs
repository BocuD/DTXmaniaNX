using DTXMania.UI.Drawable;
using FDK;
using SlimDXKey = SlimDX.DirectInput.Key;

namespace DTXMania.Core;

/// <summary>
/// Navigation with key repeat, owned by whatever reads it. Each key and guitar pad has its own counter,
/// so one instance can poll both axes.
/// </summary>
public sealed class NavigationRepeat
{
    private const int FirstRepeatMs = 400;
    private const int RepeatIntervalMs = 25;

    //built on first use, since a consumer can be constructed before CDTXMania.Timer exists
    private readonly Dictionary<SlimDXKey, CCounter> keyRepeats = new();
    private readonly Dictionary<EPad, CCounter> guitarRepeats = new();

    public bool Repeating { get; private set; }

    /// <summary>
    /// Runs <paramref name="onPrevious"/>/<paramref name="onNext"/> for the arrow keys and the guitar
    /// (held, repeating), and for the drum pads (single press).
    /// </summary>
    public void Poll(UINavigationAxis axis, UIGuitarNavigation guitar, Action onPrevious, Action onNext)
    {
        bool vertical = axis == UINavigationAxis.Vertical;
        SlimDXKey keyPrevious = vertical ? SlimDXKey.UpArrow : SlimDXKey.LeftArrow;
        SlimDXKey keyNext = vertical ? SlimDXKey.DownArrow : SlimDXKey.RightArrow;

        //passed straight through: wrapping them would allocate a closure per polled frame
        Repeat(Counter(keyRepeats, keyPrevious), CDTXMania.InputManager.Keyboard.bKeyPressing(keyPrevious),
            onPrevious);
        Repeat(Counter(keyRepeats, keyNext), CDTXMania.InputManager.Keyboard.bKeyPressing(keyNext), onNext);

        (EPad guitarPrevious, EPad guitarNext) = GuitarPads(vertical, guitar);

        //the neck has no double duty, but a strum held under P or Y is on its way to deciding or
        //cancelling and must not scroll the list out from under that
        bool strums = guitarPrevious is EPad.PickUp or EPad.PickDown;
        bool guitarScrolls = !strums || Input.StrumIsNavigation;

        Repeat(Counter(guitarRepeats, guitarPrevious),
            guitarScrolls && CDTXMania.Pad.bPressingGuitars(guitarPrevious), onPrevious);
        Repeat(Counter(guitarRepeats, guitarNext),
            guitarScrolls && CDTXMania.Pad.bPressingGuitars(guitarNext), onNext);

        if (CDTXMania.Pad.bPressed(EPlayer.Drums, vertical ? EPad.HT : EPad.SD))
        {
            onPrevious();
        }

        if (CDTXMania.Pad.bPressed(EPlayer.Drums, vertical ? EPad.LT : EPad.FT))
        {
            onNext();
        }
    }

    private void Repeat(CCounter counter, bool pressing, Action action)
    {
        Repeating = counter.nCurrentValue != 0;
        counter.tRepeatKey(pressing, action, FirstRepeatMs, RepeatIntervalMs);
        Repeating = false;
    }

    private static CCounter Counter<TInput>(Dictionary<TInput, CCounter> counters, TInput input)
        where TInput : notnull
    {
        if (!counters.TryGetValue(input, out CCounter? counter))
        {
            counter = new CCounter(0, 0, 0, CDTXMania.Timer);
            counters.Add(input, counter);
        }

        return counter;
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
        foreach (CCounter counter in keyRepeats.Values)
        {
            counter.nCurrentValue = 0;
        }

        foreach (CCounter counter in guitarRepeats.Values)
        {
            counter.nCurrentValue = 0;
        }
    }
}
