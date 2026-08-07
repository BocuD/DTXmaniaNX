using DTXMania.Core;

namespace DTXMania.UI;

/// <summary>
/// Something that can hold input focus. Only the top of the <see cref="UIFocus"/> stack is polled, so an
/// implementation never has to ask whether it is allowed to read input: being called is the permission.
/// </summary>
public interface IUIInputHandler
{
    /// <summary>Shown in the focus window; a type name alone rarely says which list this is.</summary>
    string FocusName { get; }

    /// <summary>Reads this frame's input. Called once per frame while this holds focus.</summary>
    void HandleInput();

    /// <summary>Navigation state to clear when focus moves, for handlers that navigate.</summary>
    NavigationRepeat? Navigation => null;
}

/// <summary>
/// Who currently owns input. A stage pushes itself when it activates; anything that takes over — a modal,
/// a submenu, a panel, a row being edited — pushes on top and pops when it is done.
///
/// The rule is that only the top is polled and nothing falls through implicitly. A handler that wants to
/// pass input on calls the other handler itself, which keeps every hand-off visible in the code that
/// performs it rather than in the dispatcher.
/// </summary>
public static class UIFocus
{
    private static readonly List<IUIInputHandler> stack = [];

    public static IUIInputHandler? Current => stack.Count > 0 ? stack[^1] : null;

    /// <summary>The stack from the bottom up, for the focus window.</summary>
    public static IReadOnlyList<IUIInputHandler> Stack => stack;

    /// <summary>Whether <paramref name="handler"/> is the one being polled right now.</summary>
    public static bool Holds(IUIInputHandler? handler) => handler != null && ReferenceEquals(Current, handler);

    public static void Push(IUIInputHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (stack.Contains(handler))
        {
            return;
        }

        stack.Add(handler);
        handler.Navigation?.Reset();
    }

    /// <summary>
    /// Removes <paramref name="handler"/> along with anything pushed on top of it, since whatever is above
    /// it was opened from it: a stage being torn down must not leave its overlays holding focus.
    /// </summary>
    public static void Pop(IUIInputHandler handler)
    {
        int index = stack.IndexOf(handler);
        if (index < 0)
        {
            return;
        }

        stack.RemoveRange(index, stack.Count - index);
        Current?.Navigation?.Reset();
    }

    /// <summary>
    /// Drops everything <paramref name="handler"/> has above it, leaving it holding focus. For a tree
    /// being rebuilt underneath: whatever it had pushed is about to be replaced.
    /// </summary>
    public static void PopOverlays(IUIInputHandler handler)
    {
        int index = stack.IndexOf(handler);
        if (index < 0)
        {
            return;
        }

        stack.RemoveRange(index + 1, stack.Count - index - 1);
        Current?.Navigation?.Reset();
    }

    /// <summary>
    /// Takes one handler out and leaves everything above it where it is. For a handler that is being
    /// retired from underneath, rather than one closing: a stage handing over to the next while an overlay
    /// opened from elsewhere is still up.
    /// </summary>
    public static void Remove(IUIInputHandler handler)
    {
        if (stack.Remove(handler))
        {
            Current?.Navigation?.Reset();
        }
    }

    /// <summary>Gives the frame's input to whoever holds focus. Called once per frame, before the stage updates.</summary>
    public static void Dispatch() => Current?.HandleInput();
}
