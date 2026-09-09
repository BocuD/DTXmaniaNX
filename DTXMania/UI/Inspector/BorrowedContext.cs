using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;

namespace DTXMania.UI.Inspector;

/// <summary>
/// Resolves against a live element's context chain, so a component opened on its own can be shown with the
/// values a real instance of it is seeing. The element is looked up per read: it belongs to a stage that
/// can be torn down while the editor stays open.
///
/// What it reads is the element's own chain, never a preview standing in for it: a preview reads through
/// here, so consulting one would mean asking it to resolve itself.
/// </summary>
public sealed class BorrowedContext(Func<UIDrawable?> element) : IUIDataContext
{
    public bool TryGetString(string key, out string value)
    {
        if (element() is { } live)
        {
            foreach (IUIDataContext context in DataContextChain.Real(live))
            {
                if (context.TryGetString(key, out value))
                {
                    return true;
                }
            }
        }

        value = string.Empty;
        return false;
    }

    public bool TryGetTexture(string key, out BaseTexture texture)
    {
        if (element() is { } live)
        {
            foreach (IUIDataContext context in DataContextChain.Real(live))
            {
                if (context.TryGetTexture(key, out texture))
                {
                    return true;
                }
            }
        }

        texture = BaseTexture.None;
        return false;
    }

    public bool TryGetBool(string key, out bool value)
    {
        if (element() is { } live)
        {
            foreach (IUIDataContext context in DataContextChain.Real(live))
            {
                if (context.TryGetBool(key, out value))
                {
                    return true;
                }
            }
        }

        value = false;
        return false;
    }

    public bool TryGetNumber(string key, out double value)
    {
        if (element() is { } live)
        {
            foreach (IUIDataContext context in DataContextChain.Real(live))
            {
                if (context.TryGetNumber(key, out value))
                {
                    return true;
                }
            }
        }

        value = 0.0;
        return false;
    }

    public IEnumerable<string> AvailableKeys(DataBindingKind kind)
    {
        HashSet<string> keys = [];

        if (element() is { } live)
        {
            foreach (IUIDataContext context in DataContextChain.Real(live))
            {
                keys.UnionWith(context.AvailableKeys(kind));
            }
        }

        return keys;
    }
}
