using DTXMania.Core;

namespace DTXMania.SongDb.Sorting;

public class SortBySkill : SongDbSort
{
    public override string Name => "skill";
    public override string IconName => "skill";
    public override bool requireResort => true;

    public override Task<SongNode> Sort(SongDb songDb)
    {
        SongNode root = new(null, SongNode.ENodeType.ROOT)
        {
            title = "Skill"
        };

        SongNode includedInSkill = GetSkillSongs(songDb);
        root.childNodes.Add(includedInSkill);
        includedInSkill.parent = root;
        
        return Task.FromResult(root);
    }

    private SongNode GetSkillSongs(SongDb songDb)
    {
        //create a new root node
        SongNode skillSongRoot = new(null, SongNode.ENodeType.BOX)
        {
            title = "Top 50 Skill Songs"
        };
        
        //force sort songdb skill
        songDb.RecalculateSkill();
        
        foreach (var song in songDb.skillSongs)
        {
            SongNode newNode = SongNode.Clone(song.node, skillSongRoot, false);
            
            //check index of the chart in the original node
            int chartIndex = Array.IndexOf(song.node.charts, song.chart);
            newNode.charts[chartIndex] = song.chart;
            newNode.filteredInstrumentPart = (EInstrumentPart)song.inst;
        }

        return skillSongRoot;
    }
}