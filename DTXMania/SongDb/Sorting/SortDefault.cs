namespace DTXMania.SongDb;

public class SortDefault : SongDbSort
{
    public override string Name => "Default";
    public override Task<SongNode> Sort(SongDb songDb)
    {
        return Task.FromResult(songDb.songNodeRoot);
    }
}