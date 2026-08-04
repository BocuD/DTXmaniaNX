using DTXMania.UI.Drawable;

namespace DTXMania.UI.DynamicElements;

//the kinds a bound value can be consumed as; a field may satisfy several (a number is also
//string-coercible), which drives which inspector dropdowns list it
public enum DataBindingKind
{
    String,
    Texture,
    Bool,
    Number
}

/// <summary>
/// A per-instance data source for a UI subtree, e.g. one instantiated component. Elements inside the
/// subtree resolve their binding key against the nearest ancestor context before falling back to the
/// stage's global dynamic dictionaries, which is what lets one component layout ("a song row") show
/// different data per instance.
/// </summary>
public interface IUIDataContext
{
    //bound elements query per frame, so implementations should return cheap precomputed values
    bool TryGetString(string key, out string value);
    bool TryGetTexture(string key, out BaseTexture texture);
    bool TryGetBool(string key, out bool value);
    bool TryGetNumber(string key, out double value);

    //keys bindable as 'kind', declared up front (independent of current values) so the inspector's
    //dropdowns can list what an element inside this context can bind to
    IEnumerable<string> AvailableKeys(DataBindingKind kind);
}
