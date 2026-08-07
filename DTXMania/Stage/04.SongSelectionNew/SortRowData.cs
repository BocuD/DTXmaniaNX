using DTXMania.UI.DynamicElements;

namespace DTXMania;

/// <summary>
/// What one sort-menu entry displays, exposed as <c>"Item"</c> to the SortItem component. The icon is the
/// component's, listed in the same order as <see cref="SongDb.Sorting.SongDbSort.All"/>, so an entry only
/// has to say which one it is.
/// </summary>
public sealed class SortRowData
{
    [DataField] public string Name { get; init; } = string.Empty;
    [DataField] public int IconIndex { get; init; }
}
