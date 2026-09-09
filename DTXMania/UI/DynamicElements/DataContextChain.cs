using DTXMania.UI.Drawable;

namespace DTXMania.UI.DynamicElements;

/// <summary>
/// The contexts a binding on an element resolves against, nearest first: a preview's stand-in values, the
/// element's own (if it roots one), then every ancestor's, then the app-wide global context. The nearest
/// context that HAS the key wins.
/// </summary>
public static class DataContextChain
{
    /// <summary>The chain for one element, as a struct walk: this runs once per bound element per frame,
    /// so it must not allocate.</summary>
    public static Walk For(UIDrawable element) => new(element, withPreview: true);

    /// <summary>The element's own contexts, with nothing standing in for them. A preview resolving through
    /// an element reads it this way, so the two can never be left asking each other the same question.</summary>
    public static Walk Real(UIDrawable element) => new(element, withPreview: false);

    public static bool TryResolveContextString(this UIDrawable element, string key, out string value)
    {
        foreach (IUIDataContext context in For(element))
        {
            if (context.TryGetString(key, out value))
            {
                return true;
            }
        }

        value = string.Empty;
        return false;
    }

    public static bool TryResolveContextTexture(this UIDrawable element, string key, out BaseTexture texture)
    {
        foreach (IUIDataContext context in For(element))
        {
            if (context.TryGetTexture(key, out texture))
            {
                return true;
            }
        }

        texture = BaseTexture.None;
        return false;
    }

    public static bool TryResolveContextBool(this UIDrawable element, string key, out bool value)
    {
        foreach (IUIDataContext context in For(element))
        {
            if (context.TryGetBool(key, out value))
            {
                return true;
            }
        }

        value = false;
        return false;
    }

    public static bool TryResolveContextNumber(this UIDrawable element, string key, out double value)
    {
        foreach (IUIDataContext context in For(element))
        {
            if (context.TryGetNumber(key, out value))
            {
                return true;
            }
        }

        value = 0.0;
        return false;
    }

    //the inspector wants LINQ over the chain, which the struct walk cannot give it. Once per inspector
    //frame, so the state machine it allocates does not matter
    public static IEnumerable<IUIDataContext> DataContexts(this UIDrawable element)
    {
        foreach (IUIDataContext context in For(element))
        {
            yield return context;
        }
    }

    public readonly struct Walk(UIDrawable element, bool withPreview)
    {
        public Enumerator GetEnumerator() => new(element, withPreview);
    }

    public struct Enumerator
    {
        //nearest preview scope first, then the preview's own values, then the real chain, then global
        private enum Phase
        {
            PreviewScopes,
            PreviewRoot,
            Tree,
            Global,
            Done
        }

        private readonly UIGroup? start;
        private UIGroup? next;
        private Phase phase;

        internal Enumerator(UIDrawable element, bool withPreview)
        {
            //an element that roots a context resolves against its own first
            start = element as UIGroup ?? element.parent;
            next = start;
            phase = withPreview && UIDataContext.preview != null ? Phase.PreviewScopes : Phase.Tree;
            Current = null!;
        }

        public IUIDataContext Current { get; private set; }

        public bool MoveNext()
        {
            while (true)
            {
                switch (phase)
                {
                    case Phase.PreviewScopes:
                        while (next != null)
                        {
                            UIGroup group = next;
                            next = group.parent;

                            if (UIDataContext.preview!.ScopeFor(group) is { } scope)
                            {
                                Current = scope;
                                return true;
                            }
                        }

                        phase = Phase.PreviewRoot;
                        continue;

                    case Phase.PreviewRoot:
                        phase = Phase.Tree;
                        next = start;
                        Current = UIDataContext.preview!.Root;
                        return true;

                    case Phase.Tree:
                        while (next != null)
                        {
                            UIGroup group = next;
                            next = group.parent;

                            if (group.dataContext != null)
                            {
                                Current = group.dataContext;
                                return true;
                            }
                        }

                        phase = Phase.Global;
                        continue;

                    case Phase.Global:
                        phase = Phase.Done;
                        Current = UIDataContext.Global;
                        return true;

                    default:
                        return false;
                }
            }
        }
    }
}
