using System.Diagnostics;

namespace DTXMania.SongDb;

public class SortByTitle : SongDbSort
{
    public override string Name => "Title";
    public override string IconName => "title";

    public override async Task<SongNode> Sort(SongDb songDb)
    {
        //create a new root node
        SongNode root = new(null, SongNode.ENodeType.ROOT);
        
        Dictionary<char, SongNode> letterNodes = new();
        
        //create sub nodes for each letter
        //A-Z, #, あ　か　さ　た　な　は　ま　や　ら　わ and other
        for (char c = 'a'; c <= 'z'; c++)
        {
            SongNode node = new(root, SongNode.ENodeType.BOX)
            {
                title = c.ToString().ToUpper()
            };
            letterNodes[c] = node;
        }
        
        char[] japaneseChars = { 'あ', 'か', 'さ', 'た', 'な', 'は', 'ま', 'や', 'ら', 'わ' };
        foreach (char c in japaneseChars)
        {
            SongNode node = new(root, SongNode.ENodeType.BOX)
            {
                title = c.ToString()
            };
            letterNodes[c] = node;
        }

        SongNode noData = new(root, SongNode.ENodeType.BOX)
        {
            title = $"No {Name}"
        };
        letterNodes.Add('-', noData);
        
        SongNode other = new(root, SongNode.ENodeType.BOX)
        {
            title = "Other"
        };

        SongNode? error = null;

        foreach (SongNode song in songDb.flattenedSongList)
        {
            try
            {
                //this shouldn't be needed but just in case
                if (song.nodeType != SongNode.ENodeType.SONG) continue;

                char key = GetSortKey(song);

                if (letterNodes.TryGetValue(key, out SongNode? letterNode))
                {
                    SongNode.Clone(song, letterNode);
                }
                else
                {
                    SongNode.Clone(song, other);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error sorting song {0}: {1}", song.title, ex.Message);

                if (error == null)
                {
                    error = new SongNode(root, SongNode.ENodeType.BOX)
                    {
                        title = "Sorting Error"
                    };
                }
                
                SongNode.Clone(song, error);
            }
        }

        PostInitialSort(root);

        return root;
    }
    
    protected virtual char GetSortKey(SongNode song)
    {
        //get song title
        CScore chart = song.charts.FirstOrDefault(x => x != null);

        if (chart.SongInformation.TitleHasJapanese)
        {
            string title = chart.SongInformation.TitleKana;
            return ConvertKanaToSortKana(title[0]);
        }

        if (string.IsNullOrWhiteSpace(chart.SongInformation.TitleRoman))
        {
            return '-';
        }
        
        //returning '-' for no data, this will force it into other
        if (chart.SongInformation.TitleRoman[0] == '-') return '#';

        return chart.SongInformation.TitleRoman[0];
    }
    
    protected virtual void PostInitialSort(SongNode root)
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
                    
                    if (chartA.SongInformation.TitleHasJapanese && chartB.SongInformation.TitleHasJapanese)
                    {
                        return string.Compare(chartA.SongInformation.TitleKana, chartB.SongInformation.TitleKana, StringComparison.OrdinalIgnoreCase);
                    }
                    
                    return string.Compare(chartA.SongInformation.TitleRoman, chartB.SongInformation.TitleRoman, StringComparison.OrdinalIgnoreCase);
                });
            }
        }
    }
    
    private static readonly char[] a = ['あ', 'い', 'う', 'え', 'お'];
    private static readonly char[] ka = ['か', 'き', 'く', 'け', 'こ', 'が', 'ぎ', 'ぐ', 'げ', 'ご'];
    private static readonly char[] sa = ['さ', 'し', 'す', 'せ', 'そ', 'ざ', 'じ', 'ず', 'ぜ', 'ぞ'];
    private static readonly char[] ta = ['た', 'ち', 'つ', 'て', 'と', 'だ', 'ぢ', 'づ', 'で', 'ど'];
    private static readonly char[] na = ['な', 'に', 'ぬ', 'ね', 'の'];
    private static readonly char[] ha = ['は', 'ひ', 'ふ', 'へ', 'ほ', 'ば', 'び', 'ぶ', 'べ', 'ぼ', 'ぱ', 'ぴ', 'ぷ', 'ぺ', 'ぽ'];
    private static readonly char[] ma = ['ま', 'み', 'む', 'め', 'も'];
    private static readonly char[] ya = ['や', 'ゆ', 'よ'];
    private static readonly char[] ra = ['ら', 'り', 'る', 'れ', 'ろ'];
    private static readonly char[] wa = ['わ', 'を', 'ん'];
    
    //convert a i u e o to a, ka ki ku ke ko to ka, etc
    protected static char ConvertKanaToSortKana(char input)
    {
        if (a.Contains(input))
            return 'あ';
        if (ka.Contains(input))
            return 'か';
        if (sa.Contains(input))
            return 'さ';
        if (ta.Contains(input))
            return 'た';
        if (na.Contains(input))
            return 'な';
        if (ha.Contains(input))
            return 'は';
        if (ma.Contains(input))
            return 'ま';
        if (ya.Contains(input))
            return 'や';
        if (ra.Contains(input))
            return 'ら';
        if (wa.Contains(input))
            return 'わ';
        
        return '#';
    }
}