using SQLite;

namespace DTXMania.SongDb;

[Table("CacheMetadata")]
internal class CacheMetadataEntry
{
    [PrimaryKey]
    public int Version { get; set; }
    public DateTime LastUpdated { get; set; }
}

[Table("ChartCache")]
internal class ChartCacheEntry
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    [Unique] public string FilePath { get; set; }

    [Indexed] public string FolderPath { get; set; }

    public long FileSize { get; set; }
    public string FileLastModified { get; set; }
    public long? ScoreIniFileSize { get; set; }
    public string ScoreIniLastModified { get; set; }
    public string ChartDataJson { get; set; }
    public DateTime CacheDate { get; set; } = DateTime.Now;
}