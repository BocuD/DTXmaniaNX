using System.Diagnostics;

namespace DTXMania.SongDb;

public class SortByTitle : SongDbSort
{
    public override async Task<SongNode> Sort(List<SongNode> flattenedNodes)
    {
        //create a new root node
        SongNode root = new(null)
        {
            title = "Sorted by Title",
            nodeType = SongNode.ENodeType.ROOT
        };
        
        Dictionary<char, SongNode> letterNodes = new();
        
        //create sub nodes for each letter
        //A-Z, #, あ　か　さ　た　な　は　ま　や　ら　わ and other
        for (char c = 'a'; c <= 'z'; c++)
        {
            SongNode node = new(root)
            {
                title = c.ToString().ToUpper(),
                nodeType = SongNode.ENodeType.BOX
            };
            letterNodes[c] = node;
            root.childNodes.Add(node);
        }
        
        char[] japaneseChars = { 'あ', 'か', 'さ', 'た', 'な', 'は', 'ま', 'や', 'ら', 'わ' };
        foreach (char c in japaneseChars)
        {
            SongNode node = new(root)
            {
                title = c.ToString(),
                nodeType = SongNode.ENodeType.BOX
            };
            letterNodes[c] = node;
            root.childNodes.Add(node);
        }

        SongNode other = new(root)
        {
            title = "Other",
            nodeType = SongNode.ENodeType.BOX
        };
        root.childNodes.Add(other);

        SongNode? error = null;

        foreach (SongNode song in flattenedNodes)
        {
            try
            {
                //this shouldn't be needed but just in case
                if (song.nodeType != SongNode.ENodeType.SONG) continue;

                char key = GetSortKey(song);

                if (letterNodes.TryGetValue(key, out SongNode? letterNode))
                {
                    letterNode.childNodes.Add(song);
                }
                else
                {
                    other.childNodes.Add(song);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error sorting song {0}: {1}", song.title, ex.Message);

                if (error == null)
                {
                    error = new SongNode(root)
                    {
                        title = "Sorting Error",
                        nodeType = SongNode.ENodeType.BOX
                    };
                }
                
                error.childNodes.Add(song);
            }
        }

        return root;
    }

    private char GetSortKey(SongNode song)
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
            return '#';
        }

        return chart.SongInformation.TitleRoman[0];
    }
    
    private readonly char[] a = ['あ', 'い', 'う', 'え', 'お'];
    private readonly char[] ka = ['か', 'き', 'く', 'け', 'こ', 'が', 'ぎ', 'ぐ', 'げ', 'ご'];
    private readonly char[] sa = ['さ', 'し', 'す', 'せ', 'そ', 'ざ', 'じ', 'ず', 'ぜ', 'ぞ'];
    private readonly char[] ta = ['た', 'ち', 'つ', 'て', 'と', 'だ', 'ぢ', 'づ', 'で', 'ど'];
    private readonly char[] na = ['な', 'に', 'ぬ', 'ね', 'の'];
    private readonly char[] ha = ['は', 'ひ', 'ふ', 'へ', 'ほ', 'ば', 'び', 'ぶ', 'べ', 'ぼ', 'ぱ', 'ぴ', 'ぷ', 'ぺ', 'ぽ'];
    private readonly char[] ma = ['ま', 'み', 'む', 'め', 'も'];
    private readonly char[] ya = ['や', 'ゆ', 'よ'];
    private readonly char[] ra = ['ら', 'り', 'る', 'れ', 'ろ'];
    private readonly char[] wa = ['わ', 'を', 'ん'];
    
    //convert a i u e o to a, ka ki ku ke ko to ka, etc
    private char ConvertKanaToSortKana(char input)
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