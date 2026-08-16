using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;

namespace DTXMania.UI.Inspector;

/// <summary>
/// Resolves against a live element's context chain, so a component opened on its own can be shown with the
/// values a real instance of it is seeing. The element is looked up per read: it belongs to a stage that
/// can be torn down while the editor stays open.
/// </summary>
public sealed class BorrowedContext(Func<UIDrawable?> element) : IUIDataContext
{
    public bool TryGetString(string key, out string value)
    {
        foreach (IUIDataContext context in Contexts())
        {
            if (context.TryGetString(key, out value))
            {
                return true;
            }
        }

        value = string.Empty;
        return false;
    }

    public bool TryGetTexture(string key, out BaseTexture texture)
    {
        foreach (IUIDataContext context in Contexts())
        {
            if (context.TryGetTexture(key, out texture))
            {
                return true;
            }
        }

        texture = BaseTexture.None;
        return false;
    }

    public bool TryGetBool(string key, out bool value)
    {
        foreach (IUIDataContext context in Contexts())
        {
            if (context.TryGetBool(key, out value))
            {
                return true;
            }
        }

        value = false;
        return false;
    }

    public bool TryGetNumber(string key, out double value)
    {
        foreach (IUIDataContext context in Contexts())
        {
            if (context.TryGetNumber(key, out value))
            {
                return true;
            }
        }

        value = 0.0;
        return false;
    }

    public IEnumerable<string> AvailableKeys(DataBindingKind kind)
        => Contexts().SelectMany(context => context.AvailableKeys(kind)).Distinct();

    private IEnumerable<IUIDataContext> Contexts()
        => element()?.DataContexts() ?? [];
}
