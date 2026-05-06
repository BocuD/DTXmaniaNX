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
            //get the first chart for each song
            CChartData chartDataA = a.charts.FirstOrDefault(x => x != null);
            CChartData chartDataB = b.charts.FirstOrDefault(x => x != null);

            if (chartDataA == null || chartDataB == null)
            {
                return 0; // skip if no valid chart
            }
            
            int instrumentA = (int) a.filteredInstrumentPart;
            int instrumentB = (int) b.filteredInstrumentPart;

            double chartALevel = chartDataA.SongInformation.GetLevel(instrumentA);
            double chartBLevel = chartDataB.SongInformation.GetLevel(instrumentB);

            //compare by difficulty number
            return chartALevel - chartBLevel > 0 ? 1 : chartALevel - chartBLevel < 0 ? -1 : 0;
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