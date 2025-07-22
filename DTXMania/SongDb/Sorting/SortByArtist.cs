namespace DTXMania.SongDb;

public class SortByArtist : SortByTitle
{
    protected override char GetSortKey(SongNode song)
    {
        //get song title
        CScore chart = song.charts.FirstOrDefault(x => x != null);

        if (chart.SongInformation.ArtistNameHasJapanese)
        {
            string title = chart.SongInformation.ArtistNameKana;
            return ConvertKanaToSortKana(title[0]);
        }

        if (string.IsNullOrWhiteSpace(chart.SongInformation.ArtistNameRoman))
        {
            return '#';
        }

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