using DTXMania.Core;
using DTXMania.SongDb;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;

namespace DTXMania;

/// <summary>
/// What one song-list row displays, exposed as <c>"Item"</c> to the SongRow component. Everything the row
/// shows is a field here, so the component decides the look and no code reaches into its children.
///
/// Mutable, unlike the other row models: a thumbnail finishes decoding after the row was filled, and the
/// selection highlight moves without the node changing. Both update the row in place rather than
/// rebuilding it, so scrolling allocates nothing.
/// </summary>
public sealed class SongRowData
{
    //the order the SongRow component lists its background arts in
    private const int Bar = 0;
    private const int BoxClosed = 1;
    private const int BoxOpen = 2;

    [DataField] public string Title { get; private set; } = string.Empty;
    [DataField] public string Artist { get; private set; } = string.Empty;
    [DataField] public string Skill { get; private set; } = string.Empty;

    //the album art is the one texture that belongs to the song rather than to the skin
    [DataField] public BaseTexture? AlbumArt { get; set; }

    //which of the SongRow component's arts this row uses; the arts themselves belong to the layout
    [DataField] public int LampIndex { get; private set; }
    [DataField] public int BackgroundIndex { get; private set; }

    //the bar texture needs a slightly different clip and offset than the two box textures
    [DataField] public double BackgroundClipX { get; private set; }
    [DataField] public double BackgroundOffsetX { get; private set; } = -40.0;

    [DataField] public double SkillBarWidth { get; private set; }

    [DataField] public bool HasTitle => Title.Length > 0;
    [DataField] public bool HasArtist => Artist.Length > 0;
    [DataField] public bool ShowSkill { get; private set; }
    [DataField] public bool HasLamp { get; private set; }

    public SongNode? Node { get; private set; }

    /// <summary>Fills the row from a node. Returns false when the node is unchanged, so the caller can
    /// skip the thumbnail lookup that would otherwise follow.</summary>
    public bool SetNode(SongNode? node)
    {
        if (ReferenceEquals(Node, node))
        {
            return false;
        }

        Node = node;

        switch (node?.nodeType)
        {
            case SongNode.ENodeType.SONG:
                Title = node.title;
                Artist = ArtistOf(node);
                BackgroundIndex = Bar;
                //dirty hacks to fix clipping issues with a bad texture (?)
                BackgroundClipX = 0;
                BackgroundOffsetX = -40.0;
                break;

            case SongNode.ENodeType.BOX:
                Title = node.title;
                Artist = BoxSubtitleOf(node);
                BackgroundIndex = BoxClosed;
                BackgroundClipX = 1;
                BackgroundOffsetX = -39.0;
                break;

            case SongNode.ENodeType.BACKBOX:
                Title = "<< BACK";
                Artist = CDTXMania.isJapanese ? "BOX を出ます。" : "Exit from the BOX.";
                BackgroundIndex = BoxOpen;
                BackgroundClipX = 1;
                BackgroundOffsetX = -39.0;
                break;

            default:
                Title = string.Empty;
                Artist = string.Empty;
                BackgroundIndex = Bar;
                BackgroundClipX = 0;
                BackgroundOffsetX = -40.0;
                break;
        }

        UpdateSkill();
        UpdateLamp();
        return true;
    }

    private void UpdateSkill()
    {
        ShowSkill = false;
        Skill = string.Empty;
        SkillBarWidth = 0;

        if (Node?.nodeType != SongNode.ENodeType.SONG)
        {
            return;
        }

        var skill = Node.GetTopSkillPoints();
        if (skill.skillPoints <= 0)
        {
            return;
        }

        ShowSkill = true;
        Skill = $"{skill.skillPoints:0.00}";
        SkillBarWidth = 286.0 * (skill.skillPoints / skill.maxSkillPoints);
    }

    private void UpdateLamp()
    {
        HasLamp = Node?.nodeType == SongNode.ENodeType.SONG;
        LampIndex = 0;

        if (!HasLamp)
        {
            return;
        }

        int best = 0;
        for (int index = 0; index < Node.charts.Length; index++)
        {
            CChartData? chart = Node.charts[index];
            if (chart == null || !chart.HasChartForCurrentMode()) continue;

            if (chart.SongInformation.BestRank[CDTXMania.GetCurrentInstrument()] != 99)
            {
                best = index + 1;
            }
        }

        LampIndex = best;
    }

    private static string ArtistOf(SongNode song)
        => song.charts.FirstOrDefault(c => c != null)?.SongInformation.ArtistName ?? "";

    private static string BoxSubtitleOf(SongNode box)
        => box.childNodes.Count > 1 ? $"{box.childNodes.Count - 1} songs" : "Empty collection";
}
