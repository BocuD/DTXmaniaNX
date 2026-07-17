using System.Diagnostics;
using System.Drawing;
using DTXMania.Core;
using Kawazu;

namespace DTXMania.SongDb;

[DebuggerDisplay("{title} - {path}")]
public class SongNode
{
    public enum ENodeType
    {
        SONG,
        BOX,
        BACKBOX,
        ROOT
    }
    
    public ENodeType nodeType = ENodeType.SONG;
    public string skinPath = string.Empty;
    
    public EInstrumentPart filteredInstrumentPart = EInstrumentPart.UNKNOWN;
    
    public string title = string.Empty;
    public string path = string.Empty;
    public Color color = Color.White;

    public SongNode parent;
    public List<SongNode> childNodes = [];

    public SongNode CurrentSelection
    {
        get
        {
            if (currentSelection != null) return currentSelection;
            
            currentSelection = childNodes.FirstOrDefault();
            return currentSelection;
        }
        set => currentSelection = value;
    }

    private SongNode? currentSelection = null;

    public int chartCount;
    public CChartData[] charts = new CChartData[5];
    public string[] difficultyLabel = new string[5];
    
    public STHitRanges stDrumHitRanges = new(nDefaultSizeMs: -1);
    public STHitRanges stDrumPedalHitRanges = new(nDefaultSizeMs: -1);
    public STHitRanges stGuitarHitRanges = new(nDefaultSizeMs: -1);
    public STHitRanges stBassHitRanges = new(nDefaultSizeMs: -1);

    public SongNode(SongNode parent)
    {
        this.parent = parent;
        
        parent.childNodes.Add(this);
    }

    public SongNode(SongNode? parent, ENodeType type)
    {
        this.parent = parent!;
        nodeType = type;

        switch (type)
        {
            case ENodeType.ROOT:
                title = "Root";
                break;
            
            case ENodeType.BACKBOX:
                charts =
                [
                    new CChartData()
                ];
                break;
            
            case ENodeType.BOX:
                parent?.childNodes.Add(this);

                //add a return node
                SongNode backBox = new(this, ENodeType.BACKBOX);
                childNodes.Insert(0, backBox);
                break;
            
            default:
                parent?.childNodes.Add(this);
                break;
        }
    }

    public static SongNode Clone(SongNode original, SongNode parent, bool copyCharts = true)
    {
        //copy all properties except for charts
        SongNode clone = new(parent, original.nodeType)
        {
            nodeType = original.nodeType,
            skinPath = original.skinPath,
            title = original.title,
            path = original.path,
            color = original.color,
            stDrumHitRanges = original.stDrumHitRanges,
            stDrumPedalHitRanges = original.stDrumPedalHitRanges,
            stGuitarHitRanges = original.stGuitarHitRanges,
            stBassHitRanges = original.stBassHitRanges
        };

        if (original.nodeType == ENodeType.BOX)
        {
            foreach (SongNode node in original.childNodes)
            {
                Clone(node, clone);
            }
        }

        if (copyCharts)
        {
            clone.chartCount = original.chartCount;
            for (int i = 0; i < original.charts.Length; i++)
            {
                clone.charts[i] = original.charts[i];
                clone.difficultyLabel[i] = original.difficultyLabel[i];
            }
        }
        return clone;
    }

    public string GetImagePath()
    {
        var chart = charts.FirstOrDefault(x => x != null);

        if (chart == null) return "";
        
        string imagePath = "";
        string preImagePath = chart.SongInformation.Preimage;
        if (!string.IsNullOrWhiteSpace(preImagePath))
        {
            imagePath = Path.Combine(
                chart.FileInformation.AbsoluteFolderPath,
                preImagePath
            );
        }

        return imagePath;
    }

    public static SongNode? rNextSong(SongNode? song)
    {
        if (song == null) return null;
        List<SongNode> list = song.parent.childNodes;

        int index = list.IndexOf(song);

        if (index < 0)
            return null;

        if (index == (list.Count - 1))
            return list[0];

        return list[index + 1];
    }

    public static SongNode? rPreviousSong(SongNode? song)
    {
        if (song == null) return null;
        List<SongNode> list = song.parent.childNodes;

        int index = list.IndexOf(song);

        if (index < 0)
            return null;

        if (index == 0)
            return list[list.Count - 1];

        return list[index - 1];
    }

    //loop over all charts on this node. find the one that has the highest skill points (if any)
    public (CChartData chart, double skillPoints, double maxSkillPoints, int instrument) GetTopSkillPoints()
    {
        double skill = 0;
        double maxSkill = 0;
        int inst = 0;
        CChartData skillChartData = null;
        foreach (CChartData chart in charts)
        {
            if (chart == null) continue;
            if (!chart.HasChartForCurrentMode()) continue;
            
            for (int instrument = 0; instrument < 3; instrument++)
            {
                if (instrument != 0 && CDTXMania.ConfigIni.bDrumsEnabled) continue;
                if (instrument == 0 && !CDTXMania.ConfigIni.bDrumsEnabled) continue;
                
                double chartSkill = CScoreIni.tCalculateGameSkillFromPlayingSkill(
                    chart.SongInformation.Level[instrument],
                    chart.SongInformation.LevelDec[instrument],
                    chart.SongInformation.HighCompletionRate[instrument],
                    false);
                
                if (chartSkill > skill)
                {
                    skill = chartSkill;
                    maxSkill = chart.SongInformation.GetMaxSkill(instrument);
                    skillChartData = chart;
                    inst = instrument;
                }
            }
        }

        return (skillChartData, skill, maxSkill, inst);
    }

    public SongNode? GetSearchResult(string searchQuery)
    {
        SongNode root = new(null, ENodeType.ROOT)
        {
            title = "Search result"
        };

        string[] queries;
        
        if (Utilities.HasJapanese(searchQuery))
        {
            var result = TextConversionCache.GetOrCacheTextConversion(searchQuery);
            string queryKana = result.kana;
            string queryRoman = result.romaji;
            
            queries = [searchQuery, searchQuery.Replace(" ", ""), queryKana, queryRoman];
        }
        else
        {
            queries = [searchQuery, searchQuery.Replace(" ", "")];
        }
        
        //replace hepburn notation (ē) and replace with singular vowel (e)
        string extraQuery = searchQuery.Replace("aa", "a");
        extraQuery = extraQuery.Replace("ii", "i");
        extraQuery = extraQuery.Replace("uu", "u");
        extraQuery = extraQuery.Replace("ee", "e");
        extraQuery = extraQuery.Replace("ei", "e");
        extraQuery = extraQuery.Replace("oo", "o");
        extraQuery = extraQuery.Replace("ou", "o");
        
        queries = queries.Append(extraQuery).ToArray();
        queries = queries.Append(extraQuery.Replace(" ", "")).ToArray();
        
        bool Matches(SongNode node)
        {
            //the actual comparison
            bool match = false;

            var metadata = node.charts.FirstOrDefault(x => x != null);

            void Match(string input)
            {
                //replace hepburn notation (ē) and replace with singular vowel (e)
                input = input.Replace("ā", "a");
                input = input.Replace("ī", "i");
                input = input.Replace("ū", "u");
                input = input.Replace("ē", "e");
                input = input.Replace("ō", "o");

                if (queries.Any(query => input.Contains(query, StringComparison.InvariantCultureIgnoreCase)))
                {
                    match = true;
                }
            }

            Match(node.title);
            if (metadata != null)
            {
                Match(metadata.SongInformation.Title);
                Match(metadata.SongInformation.TitleRoman);

                Match(metadata.SongInformation.ArtistName);
                Match(metadata.SongInformation.ArtistNameRoman);

                Match(metadata.SongInformation.Comment);
                Match(metadata.SongInformation.CommentRoman);
            }

            return match;
        }

        void SearchIn(SongNode container)
        {
            foreach (SongNode node in container.childNodes)
            {
                switch (node.nodeType)
                {
                    case ENodeType.SONG when Matches(node):
                        Clone(node, root);
                        break;

                    case ENodeType.BOX:
                        SearchIn(node);
                        break;
                }
            }
        }

        SearchIn(this);

        return root;
    }
}