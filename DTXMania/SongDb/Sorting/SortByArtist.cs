using Kawazu;

namespace DTXMania.SongDb.Sorting;

public class SortByArtist : SortByTitle
{
    public override string Name => "Artist";
    public override string IconName => "artist";

    protected override char GetSortKey(SongNode song)
    {
        //get song title
        CChartData chartData = song.charts.FirstOrDefault(x => x != null);
        
        if (string.IsNullOrWhiteSpace(chartData.SongInformation.ArtistNameRoman))
        {
            return '-';
        }
        
        if (Utilities.IsJapanese(chartData.SongInformation.ArtistName[0]))
        {
            return ConvertKanaToSortKana(chartData.SongInformation.ArtistNameKana[0]);
        }
        
        //returning '-' for no data, this will force it into other
        if (chartData.SongInformation.ArtistNameRoman[0] == '-') return '#';

        return chartData.SongInformation.ArtistNameRoman[0];
    }
    
    protected override void PostInitialSort(SongNode root)
    {
        foreach (var node in root.childNodes)
        {
            //sort by title kana if available
            if (node is { nodeType: SongNode.ENodeType.BOX, childNodes.Count: > 0 })
            {
                node.childNodes.Sort((a, b) =>
                {
                    CChartData chartDataA = a.charts.FirstOrDefault(x => x != null);
                    CChartData chartDataB = b.charts.FirstOrDefault(x => x != null);
                    
                    if (chartDataA == null && chartDataB == null) return 0; //both null
                    if (chartDataA == null) return 1; //a is null, b is not, b comes first
                    if (chartDataB == null) return -1; //b is null, a is not, a comes first
                    
                    if (chartDataA.SongInformation.ArtistNameHasJapanese && chartDataB.SongInformation.ArtistNameHasJapanese)
                    {
                        return string.Compare(chartDataA.SongInformation.ArtistNameKana, chartDataB.SongInformation.ArtistNameKana, StringComparison.OrdinalIgnoreCase);
                    }
                    
                    return string.Compare(chartDataA.SongInformation.ArtistNameRoman, chartDataB.SongInformation.ArtistNameRoman, StringComparison.OrdinalIgnoreCase);
                });
            }
        }
    }
}