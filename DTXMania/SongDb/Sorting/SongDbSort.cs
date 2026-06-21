namespace DTXMania.SongDb.Sorting;

public abstract class SongDbSort
{
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
            CChartData? chartA = a.charts.FirstOrDefault(x => x != null && x.ScoreIniInformation.FileSize != 0);
            CChartData? chartB = b.charts.FirstOrDefault(x => x != null && x.ScoreIniInformation.FileSize != 0);
            
            DateTime lastPlayedA = chartA == null ? DateTime.MinValue : chartA.ScoreIniInformation.LastModified;
            DateTime lastPlayedB = chartB == null ? DateTime.MinValue : chartB.ScoreIniInformation.LastModified;

            return lastPlayedA.CompareTo(lastPlayedB);
        });
    }
}