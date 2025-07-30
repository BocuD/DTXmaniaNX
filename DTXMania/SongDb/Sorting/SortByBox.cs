namespace DTXMania.SongDb;

public class SortByBox : SongDbSort
{
    public override string Name => "BOX";
    public override string IconName => "recommended";

    public override Task<SongNode> Sort(SongDb songDb)
    {
        //create a new root node
        SongNode root = new(null, SongNode.ENodeType.ROOT)
        {
            title = "Box"
        };

        foreach (SongNode node in songDb.songNodeRoot.childNodes)
        {
            if (node.nodeType == SongNode.ENodeType.BOX)
            {
                SongNode.Clone(node, root);
            }
        }
        
        //create node for songs without box
        SongNode noBoxNode = new(root, SongNode.ENodeType.BOX)
        {
            title = "No Box"
        };
        
        foreach (SongNode song in songDb.songNodeRoot.childNodes)
        {
            if (song.nodeType == SongNode.ENodeType.SONG)
            {
                SongNode.Clone(song, noBoxNode);
            }
        }
        
        //order nodes by path
        root.childNodes.Sort((a, b) => string.Compare(a.path, b.path, StringComparison.OrdinalIgnoreCase));
        
        return Task.FromResult(root);
    }
}