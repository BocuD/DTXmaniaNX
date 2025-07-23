namespace DTXMania.SongDb;

public class SortByAllSongs : SongDbSort
{
    public override string Name => "All Songs";

    public override Task<SongNode> Sort(SongDb songDb)
    {
        SongNode root = new(null, SongNode.ENodeType.ROOT);

        foreach (SongNode node in songDb.flattenedSongList.Where(node => node.nodeType == SongNode.ENodeType.SONG))
        {
            SongNode.Clone(node, root);
        }

        return Task.FromResult(root);
    }
}