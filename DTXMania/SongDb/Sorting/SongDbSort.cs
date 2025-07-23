namespace DTXMania.SongDb;

public abstract class SongDbSort
{
    public abstract string Name { get; }
    public abstract Task<SongNode> Sort(SongDb songDb);
}