using DTXMania.UI.DynamicElements;

namespace DTXMania;

/// <summary>
/// One row of the result parameter panel, exposed as <c>"Item"</c> to the ResultRow component. The
/// component's children bind these fields, so styling a row is a layout change rather than a code change.
/// </summary>
public sealed record ResultRowData
{
    [DataField] public string Label { get; init; } = string.Empty;
    [DataField] public long Value { get; init; }
    [DataField] public int Padding { get; init; } = 4;
    [DataField] public long Percent { get; init; }
    [DataField] public bool ShowPercent { get; init; }
}
