namespace DTXMania.SongDb;

public abstract class SongDbSort
{
    public abstract Task<SongNode> Sort(List<SongNode> flattenedNodes);
}