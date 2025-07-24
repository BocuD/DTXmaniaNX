namespace DTXMania.SongDb;

public abstract class SongDbSort
{
    public abstract string Name { get; }
    public abstract Task<SongNode> Sort(SongDb songDb);
    
    protected static void OrderByDifficulty(List<SongNode> difficultyChildNodes)
    {
        difficultyChildNodes.Sort((a, b) =>
        {
            //get the first chart for each song
            CScore chartA = a.charts.FirstOrDefault(x => x != null);
            CScore chartB = b.charts.FirstOrDefault(x => x != null);

            if (chartA == null || chartB == null)
            {
                return 0; // skip if no valid chart
            }

            int instrument = (int) a.filteredInstrumentPart;

            double chartALevel = chartA.SongInformation.Level[instrument] / 10.0f;
            chartALevel += chartA.SongInformation.LevelDec[instrument] / 100.0f;
            double chartBLevel = chartB.SongInformation.Level[instrument] / 10.0f;
            chartALevel += chartB.SongInformation.LevelDec[instrument] / 100.0f;

            //compare by difficulty number
            return chartALevel - chartBLevel > 0 ? 1 : chartALevel - chartBLevel < 0 ? -1 : 0;
        });
    }
}