using DTXMania.Core.Framework;

namespace DTXMania.UI.Drawable;

/// <summary>The tint a drawable multiplies into every colour it draws. A group pushes its own onto it
/// while its children draw.</summary>
public static class UITint
{
    public static Color4 Current { get; private set; } = Color4.White;

    public static Scope Push(Color4 tint) => new(tint);

    public readonly struct Scope : IDisposable
    {
        private readonly Color4 previous;

        internal Scope(Color4 tint)
        {
            previous = Current;
            Current = previous * tint;
        }

        public void Dispose() => Current = previous;
    }
}
