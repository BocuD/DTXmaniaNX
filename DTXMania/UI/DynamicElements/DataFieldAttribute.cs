namespace DTXMania.UI.DynamicElements;

/// <summary>
/// Opt-in marker exposing a property or field to the UI data-binding layer. Only members carrying this
/// attribute are reachable through a data context path, e.g. <c>"Song.Title"</c>, and traversal into a
/// nested object requires the intermediate member to carry it too. See <see cref="DataFieldReflector"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
public sealed class DataFieldAttribute : Attribute
{
    //the exposed path segment, defaulting to the member name
    public string? Name { get; init; }

    //default format for IFormattable values, overridable per binding via a ":format" key suffix
    public string? Format { get; init; }

    public DataFieldAttribute()
    {
    }

    public DataFieldAttribute(string name)
    {
        Name = name;
    }
}
