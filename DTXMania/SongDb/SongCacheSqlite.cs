using System.Diagnostics;
using Newtonsoft.Json;
using SQLite;

namespace DTXMania.SongDb;

public class SongCacheSqlite : IDisposable
{
    private const int CACHE_VERSION = 1;
    private readonly string cachePath;
    private SQLiteConnection db;
    private bool isInitialized = false;

    public SongCacheSqlite(string cacheFilePath = "")
    {
        cachePath = string.IsNullOrEmpty(cacheFilePath)
            ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "songs.cache.db")
            : cacheFilePath;
    }

    public void Initialize()
    {
        if (isInitialized) return;

        try
        {
            db = new SQLiteConnection(cachePath);
            db.ExecuteScalar<string>("PRAGMA journal_mode=WAL;");

            db.CreateTable<CacheMetadataEntry>();
            db.CreateTable<ChartCacheEntry>();

            if (db.Table<CacheMetadataEntry>().FirstOrDefault() == null)
            {
                db.Insert(new CacheMetadataEntry
                {
                    Version = CACHE_VERSION,
                    LastUpdated = DateTime.Now
                });
            }

            isInitialized = true;
            Trace.TraceInformation($"Song cache initialized at: {cachePath}");
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Failed to initialize song cache: {ex.Message}");
            throw;
        }
    }

    public bool TryGetCachedChart(string filePath, string scoreIniPath, out CChartData cachedChartData)
    {
        cachedChartData = null;
        if (!isInitialized || !File.Exists(filePath)) return false;

        try
        {
            var fileInfo = new FileInfo(filePath);
            var scoreIniInfo = string.IsNullOrEmpty(scoreIniPath) || !File.Exists(scoreIniPath)
                ? null : new FileInfo(scoreIniPath);

            var entry = db.Table<ChartCacheEntry>()
                          .FirstOrDefault(c => c.FilePath == filePath);

            if (entry == null) return false;
            if (entry.FileSize != fileInfo.Length) return false;
            if (entry.FileLastModified != fileInfo.LastWriteTime.ToString("O")) return false;

            if (scoreIniInfo != null)
            {
                if (entry.ScoreIniFileSize != scoreIniInfo.Length) return false;
                if (entry.ScoreIniLastModified != scoreIniInfo.LastWriteTime.ToString("O")) return false;
            }
            else if (entry.ScoreIniFileSize != null || !string.IsNullOrEmpty(entry.ScoreIniLastModified))
            {
                return false;
            }

            cachedChartData = JsonConvert.DeserializeObject<CChartData>(entry.ChartDataJson);
            if (cachedChartData != null)
            {
                cachedChartData.bHadACacheInSongDB = true;
                return true;
            }
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"Error retrieving cached chart data for {filePath}: {ex.Message}");
        }

        return false;
    }

    public bool SaveChartData(string filePath, string scoreIniPath, CChartData chartData)
    {
        if (!isInitialized) return false;

        try
        {
            var fileInfo = new FileInfo(filePath);
            var scoreIniInfo = string.IsNullOrEmpty(scoreIniPath) || !File.Exists(scoreIniPath)
                ? null : new FileInfo(scoreIniPath);

            var existing = db.Table<ChartCacheEntry>()
                             .FirstOrDefault(c => c.FilePath == filePath);

            var entry = existing ?? new ChartCacheEntry();
            entry.FilePath = filePath;
            entry.FolderPath = chartData.FileInformation.AbsoluteFolderPath;
            entry.FileSize = fileInfo.Length;
            entry.FileLastModified = fileInfo.LastWriteTime.ToString("O");
            entry.ScoreIniFileSize = scoreIniInfo?.Length;
            entry.ScoreIniLastModified = scoreIniInfo?.LastWriteTime.ToString("O");
            entry.ChartDataJson = JsonConvert.SerializeObject(chartData);
            entry.CacheDate = DateTime.Now;

            if (existing == null)
                db.Insert(entry);
            else
                db.Update(entry);

            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"Error saving chart data to cache for {filePath}: {ex.Message}");
            return false;
        }
    }

    public void ClearAllCache()
    {
        if (!isInitialized) return;
        try
        {
            db.DeleteAll<ChartCacheEntry>();
            db.Execute("VACUUM;");
            Trace.TraceInformation("Song cache cleared successfully");
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"Error clearing cache: {ex.Message}");
        }
    }

    public int GetCacheSize()
    {
        if (!isInitialized) return 0;
        try
        {
            return db.Table<ChartCacheEntry>().Count();
        }
        catch (Exception e)
        {
            Trace.TraceWarning($"Error getting cache size: {e.Message}");
            return 0;
        }
    }

    public void Dispose()
    {
        try
        {
            db?.Close(); 
            db?.Dispose();
        } 
        catch { }
    }
}