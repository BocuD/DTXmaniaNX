namespace DTXMania.SongDb.Sorting;

public abstract class SongDbSort
{
    /// <summary>Every sort the game offers, in the order the sort menu shows them. One instance each: a
    /// sorter is a strategy, and the sort cache keys on it.</summary>
    public static readonly SongDbSort[] All =
    [
        new SortDefault(),
        new SortByBox(),
        new SortByTitle(),
        new SortByArtist(),
        new SortByDifficulty(),
        new SortByLevel(),
        new SortByPlayer(),
        new SortByAllSongs(),
        new SortBySkill()
    ];

    public abstract string Name { get; }
    public abstract string IconName { get; }
    public abstract Task<SongNode> Sort(SongDb songDb);
    public virtual bool requireResort => false;
    
    protected static void OrderByDifficulty(List<SongNode> nodes)
    {
        nodes.Sort((a, b) =>
        {
            CChartData chartDataA = a.charts.FirstOrDefault(x => x != null);
            CChartData chartDataB = b.charts.FirstOrDefault(x => x != null);

            //push null-chart nodes to a consistent end instead of "equal"
            if (chartDataA == null && chartDataB == null) return 0;
            if (chartDataA == null) return 1;
            if (chartDataB == null) return -1;

            double levelA = chartDataA.SongInformation.GetLevel((int)a.filteredInstrumentPart);
            double levelB = chartDataB.SongInformation.GetLevel((int)b.filteredInstrumentPart);

            int cmp = levelA.CompareTo(levelB);
            if (cmp != 0) return cmp;

            //tie-break 1: title
            cmp = string.Compare(a.title, b.title, StringComparison.OrdinalIgnoreCase);
            if (cmp != 0) return cmp;

            //tie-break 2: use path so identical titles stay put
            return string.Compare(a.path, b.path, StringComparison.Ordinal);
        });
    }
    
    protected static void OrderByLastPlayedDate(List<SongNode> nodes)
    {
        nodes.Sort((a, b) =>
        {
            if (a.nodeType == SongNode.ENodeType.BACKBOX) return -1;
            if (b.nodeType == SongNode.ENodeType.BACKBOX) return 1;

            CChartData? chartA = a.charts
                .Where(x => x != null && x.HasChartForCurrentMode())
                .Select(x => x.ForCurrentInstrument())
                .Where(x => x != null && x.ScoreIniInformation.FileSize != 0)
                .OrderByDescending(x => x.ScoreIniInformation.LastModified)
                .FirstOrDefault();

            CChartData? chartB = b.charts
                .Where(x => x != null && x.HasChartForCurrentMode())
                .Select(x => x.ForCurrentInstrument())
                .Where(x => x != null && x.ScoreIniInformation.FileSize != 0)
                .OrderByDescending(x => x.ScoreIniInformation.LastModified)
                .FirstOrDefault();

            DateTime lastPlayedA = chartA == null ? DateTime.MinValue : chartA.ScoreIniInformation.LastModified;
            DateTime lastPlayedB = chartB == null ? DateTime.MinValue : chartB.ScoreIniInformation.LastModified;

            int cmp = lastPlayedB.CompareTo(lastPlayedA);
            if (cmp != 0) return cmp;

            cmp = string.Compare(a.title, b.title, StringComparison.OrdinalIgnoreCase);
            if (cmp != 0) return cmp;

            return string.Compare(a.path, b.path, StringComparison.Ordinal);
        });
    }
}