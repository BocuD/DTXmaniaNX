using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;

namespace DTXMania;

/// <summary>
/// The per-difficulty view-model a <see cref="ChartRow"/> exposes as <c>"Row"</c>. The pane pushes one of
/// these per frame and the component's children bind to it declaratively: level/rate text via
/// bindings, the rank icon via <c>resource</c>.
/// </summary>
public sealed record ChartRowData
{
    public static readonly ChartRowData Empty = new();

    //pre-formatted, e.g. "7.50" or the "-.--" placeholder for a difficulty this song doesn't have
    [DataField] public string Level { get; init; } = "-.--";

    //what the charter called this difficulty, e.g. "TV SIZE" — free text, and not necessarily one of the
    //names the difficulty art covers. Empty for a song that names nothing
    [DataField] public string Name { get; init; } = string.Empty;

    //whether that name says something the difficulty art cannot: a chart called "MASTER" is already drawn
    //as MASTER, but nothing shows "TV SIZE" unless the name itself is drawn
    [DataField] public bool HasCustomName { get; init; }

    //pre-formatted completion rate, e.g. "87.50%"; empty when there is no record to show
    [DataField] public string Rate { get; init; } = string.Empty;

    [DataField] public BaseTexture? Rank { get; init; }
    [DataField] public bool ShowSkill { get; init; }

    [DataField] public bool HasRank => Rank != null;
    [DataField] public bool ShowRate => Rate.Length > 0;
}
