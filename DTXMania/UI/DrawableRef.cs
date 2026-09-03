using DTXMania.UI.Drawable;

namespace DTXMania.UI;

/// <summary>
/// A non-owning handle to one drawable, so a selection never keeps an element alive. A handle to a
/// destroyed element reads back as null.
/// </summary>
public readonly struct DrawableRef
{
    public static readonly DrawableRef None = default;

    private readonly WeakReference<UIDrawable>? reference;

    public DrawableRef(UIDrawable? drawable)
    {
        reference = drawable == null ? null : new WeakReference<UIDrawable>(drawable);
    }

    public UIDrawable? Target =>
        reference != null && reference.TryGetTarget(out UIDrawable? drawable) ? drawable : null;

    public bool HasTarget => Target != null;

    public bool Is(UIDrawable? drawable) => drawable != null && ReferenceEquals(Target, drawable);

    public static implicit operator DrawableRef(UIDrawable? drawable) => new(drawable);
}
