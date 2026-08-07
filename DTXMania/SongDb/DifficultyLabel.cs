namespace DTXMania.SongDb;

/// <summary>
/// What a chart's difficulty is called. A charter names difficulties freely — "TV SIZE", "FULL" — but the
/// art only covers a known set of names, so a name outside it falls back to what the chart's slot is
/// conventionally called. Shared, because the song-loading screen and the performance screen draw this
/// through different mechanisms and must not disagree about which difficulty a chart is.
/// </summary>
public static class DifficultyLabel
{
    //the names there is art for, in the order they sit in the difficulty sheet
    public static readonly string[] Rows =
    [
        "DTXMANIA", "DEBUT", "NOVICE", "REGULAR", "EXPERT", "MASTER",
        "BASIC", "ADVANCED", "EXTREME", "RAW", "RWS", "REAL"
    ];

    //what each of a song's five difficulty slots is called when its own name is not one there is art for
    public static readonly string[] SlotNames = ["BASIC", "ADVANCED", "EXTREME", "MASTER", "DTX"];

    /// <summary>The name to draw for a chart: the charter's own where there is art for it, its slot's
    /// conventional name otherwise.</summary>
    public static string Resolve(string? label, int slot)
    {
        if (RowFor(label) >= 0)
        {
            return label!;
        }

        return slot >= 0 && slot < SlotNames.Length ? SlotNames[slot] : Rows[0];
    }

    /// <summary>The row of the difficulty sheet a name draws, or -1 for a name with no art.</summary>
    public static int RowFor(string? name) => string.IsNullOrEmpty(name)
        ? -1
        : Array.FindIndex(Rows, row => row.Equals(name, StringComparison.CurrentCultureIgnoreCase));
}
