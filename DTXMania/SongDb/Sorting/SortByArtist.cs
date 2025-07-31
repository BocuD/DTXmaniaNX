using Kawazu;

namespace DTXMania.SongDb.Sorting;

public class SortByArtist : SortByTitle
{
    public override string Name => "Artist";
    public override string IconName => "artist";

    protected override char GetSortKey(SongNode song)
    {
        //get song title
        CScore chart = song.charts.FirstOrDefault(x => x != null);
        
        if (string.IsNullOrWhiteSpace(chart.SongInformation.ArtistNameRoman))
        {
            return '-';
        }
        
        if (Utilities.IsJapanese(chart.SongInformation.ArtistName[0]))
        {
            return ConvertKanaToSortKana(chart.SongInformation.ArtistNameKana[0]);
        }
        
        //returning '-' for no data, this will force it into other
        if (chart.SongInformation.ArtistNameRoman[0] == '-') return '#';

        return chart.SongInformation.ArtistNameRoman[0];
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
                    CScore chartA = a.charts.FirstOrDefault(x => x != null);
                    CScore chartB = b.charts.FirstOrDefault(x => x != null);
                    
                    if (chartA == null && chartB == null) return 0; //both null
                    if (chartA == null) return 1; //a is null, b is not, b comes first
                    if (chartB == null) return -1; //b is null, a is not, a comes first
                    
                    if (chartA.SongInformation.ArtistNameHasJapanese && chartB.SongInformation.ArtistNameHasJapanese)
                    {
                        return string.Compare(chartA.SongInformation.ArtistNameKana, chartB.SongInformation.ArtistNameKana, StringComparison.OrdinalIgnoreCase);
                    }
                    
                    return string.Compare(chartA.SongInformation.ArtistNameRoman, chartB.SongInformation.ArtistNameRoman, StringComparison.OrdinalIgnoreCase);
                });
            }
        }
    }
}