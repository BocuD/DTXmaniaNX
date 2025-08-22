using DTXMania.Core;

namespace DTXMania.SongDb.Sorting;

public class SortBySkill : SongDbSort
{
    public override string Name => "skill";
    public override string IconName => "skill";
    
    public override Task<SongNode> Sort(SongDb songDb)
    {
        SongNode root = new(null, SongNode.ENodeType.ROOT)
        {
            title = "Skill"
        };

        var includedInSkill = GetSkillSongs(songDb);
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
        
        List<(SongNode node, CScore chart, double skill, int inst)> skillSongs = [];

        foreach (SongNode node in songDb.flattenedSongList)
        {
            (CScore chart, double skill, double max, int inst) = node.GetTopSkillPoints();
            if (skill > 0)
            {
                skillSongs.Add((node, chart, skill, inst));
            }
        }
        
        //sort by skill descending
        skillSongs = skillSongs.OrderByDescending(s => s.skill).ToList();
        
        //cut to top 50
        skillSongs = skillSongs.Take(50).ToList();
        
        foreach (var song in skillSongs)
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