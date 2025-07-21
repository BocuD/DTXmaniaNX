using System.Drawing;
using DTXMania.Core;

namespace DTXMania.SongDb;

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
    
    public string title = string.Empty;
    public string path = string.Empty;
    public Color color = Color.White;

    public SongNode parent;
    public List<SongNode> childNodes = [];

    public int chartCount;
    public CScore[] charts = new CScore[5];
    public string[] difficultyLabel = new string[5];
    
    public STHitRanges stDrumHitRanges = new(nDefaultSizeMs: -1);
    public STHitRanges stDrumPedalHitRanges = new(nDefaultSizeMs: -1);
    public STHitRanges stGuitarHitRanges = new(nDefaultSizeMs: -1);
    public STHitRanges stBassHitRanges = new(nDefaultSizeMs: -1);

    public SongNode(SongNode parent)
    {
        this.parent = parent;
    }

    public string GetImagePath()
    {
        var chart = charts.FirstOrDefault(x => x != null);
        
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

    public static SongNode rNextSong(SongNode song)
    {
        List<SongNode> list = song.parent.childNodes;

        int index = list.IndexOf(song);

        if (index < 0)
            return null;

        if (index == (list.Count - 1))
            return list[0];

        return list[index + 1];
    }

    public static SongNode rPreviousSong(SongNode song)
    {
        List<SongNode> list = song.parent.childNodes;

        int index = list.IndexOf(song);

        if (index < 0)
            return null;

        if (index == 0)
            return list[list.Count - 1];

        return list[index - 1];
    }
}