using DTXMania.UI.Drawable;

namespace DTXMania.UI.DynamicElements;

/// <summary>
/// Stand-in data for a component drawn outside a running game. A behaviour class with no stage behind it
/// still answers for its keys, just with nothing in them, so a preview outranks the tree's own contexts
/// rather than being fallen back on.
/// </summary>
public interface IUIPreview
{
    /// <summary>Values that stand in for the whole chain.</summary>
    IUIDataContext Root { get; }

    /// <summary>The stand-in for one group's own scope, such as a single slot of a list, or null when it
    /// has none. This is what gives each slot its own values instead of one set shared by all of them.
    /// </summary>
    IUIDataContext? ScopeFor(UIGroup group);
}
