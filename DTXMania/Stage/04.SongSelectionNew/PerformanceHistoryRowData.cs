using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;

namespace DTXMania;

public sealed record PerformanceHistoryRowData
{
    public static readonly PerformanceHistoryRowData Empty = new();

    [DataField] public string Date { get; init; } = string.Empty;

    [DataField] public string Outcome { get; init; } = string.Empty;

    [DataField] public string Instrument { get; init; } = string.Empty;

    [DataField] public string Skill { get; init; } = string.Empty;
    [DataField] public string Speed { get; init; } = string.Empty;

    [DataField] public string Raw { get; init; } = string.Empty;

    [DataField] public BaseTexture? Rank { get; init; }

    [DataField] public bool HasRank => Rank != null;
    [DataField] public bool HasSkill => Skill.Length > 0;
    [DataField] public bool HasSpeed => Speed.Length > 0;
    [DataField] public bool ShowRaw => Outcome.Length == 0 && Raw.Length > 0;
}
