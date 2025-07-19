using System.Drawing;
using DTXMania.Core;

namespace DTXMania.SongDb;

public class SongNode
{
    public enum ENodeType
    {
        SONG,
        BOX
    }
    
    public ENodeType nodeType = ENodeType.SONG;
    public string skinPath = string.Empty;
    
    public string title = string.Empty;
    public string path = string.Empty;
    public Color color = Color.White;

    public SongNode? parent;
    public List<SongNode>? childNodes;

    public int chartCount;
    public CScore[] charts = new CScore[5];
    public string[] difficultyLabel = new string[5];
    
    public STHitRanges stDrumHitRanges = new(nDefaultSizeMs: -1);
    public STHitRanges stDrumPedalHitRanges = new(nDefaultSizeMs: -1);
    public STHitRanges stGuitarHitRanges = new(nDefaultSizeMs: -1);
    public STHitRanges stBassHitRanges = new(nDefaultSizeMs: -1);
}