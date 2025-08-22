using System.Diagnostics;
using DTXMania.Core;

namespace DTXMania.SongDb.Sorting;

public class SortByPlayer : SongDbSort
{
    public override string Name => "Player";
    public override string IconName => "player";

    public override Task<SongNode> Sort(SongDb songDb)
    {
        SongNode root = new(null, SongNode.ENodeType.ROOT)
        {
            title = "Player"
        };

        var recentlyPlayed = GetRecentlyPlayed(songDb);
        root.childNodes.Add(recentlyPlayed);
        recentlyPlayed.parent = root;

        var clearRank = GetByClearRank(songDb);
        root.childNodes.Add(clearRank);
        clearRank.parent = root;
        
        return Task.FromResult(root);
    }
    
    public SongNode GetRecentlyPlayed(SongDb songDb)
    {
        //create a new root node
        SongNode recentlyPlayed = new(null, SongNode.ENodeType.BOX)
        {
            title = "Recently Played"
        };

        //create groups for today, yesterday, this week, this month, this year, older, never
        SongNode todayNode = new(recentlyPlayed, SongNode.ENodeType.BOX) { title = "Today" };
        SongNode yesterdayNode = new(recentlyPlayed, SongNode.ENodeType.BOX) { title = "Yesterday" };
        SongNode thisWeekNode = new(recentlyPlayed, SongNode.ENodeType.BOX) { title = "This Week" };
        SongNode thisMonthNode = new(recentlyPlayed, SongNode.ENodeType.BOX) { title = "This Month" };
        SongNode thisYearNode = new(recentlyPlayed, SongNode.ENodeType.BOX) { title = "This Year" };
        SongNode olderNode = new(recentlyPlayed, SongNode.ENodeType.BOX) { title = "Older" };
        SongNode neverNode = new(recentlyPlayed, SongNode.ENodeType.BOX) { title = "Never Played" };
        
        //go through songs and add them based on if they have a score, and if yes, the last played date
        foreach (SongNode songNode in songDb.flattenedSongList)
        {
            CScore? lastPlayedChart = songNode.charts
                .Where(chart => chart != null && chart.ScoreIniInformation.FileSize != 0 && chart.HasChartForCurrentMode())
                .OrderByDescending(chart => chart.ScoreIniInformation.LastModified)
                .FirstOrDefault();

            if (lastPlayedChart == null)
            {
                SongNode.Clone(songNode, neverNode);
                continue; // skip if no last played chart
            }

            DateTime lastPlayed = lastPlayedChart.ScoreIniInformation.LastModified;
            TimeSpan timeSinceLastPlayed = DateTime.Now - lastPlayed;

            switch (timeSinceLastPlayed.TotalDays)
            {
                case < 1:
                    SongNode.Clone(songNode, todayNode);
                    break;
                
                case < 2:
                    SongNode.Clone(songNode, yesterdayNode);
                    break;
                
                case < 7:
                    SongNode.Clone(songNode, thisWeekNode);
                    break;
                
                case < 30:
                    SongNode.Clone(songNode, thisMonthNode);
                    break;
                
                case < 365:
                    SongNode.Clone(songNode, thisYearNode);
                    break;
                
                default:
                    SongNode.Clone(songNode, olderNode);
                    break;
            }
        }

        foreach (SongNode box in recentlyPlayed.childNodes)
        {
            OrderByLastPlayedDate(box.childNodes);
        }
        
        return recentlyPlayed;
    }
    
    public SongNode GetByClearRank(SongDb songDb)
    {
        //create a new root node
        SongNode clearRank = new(null, SongNode.ENodeType.BOX)
        {
            title = "By Clear Rank"
        };

        //create groups for each clear rank
        Dictionary<CScoreIni.ERANK, SongNode> clearRankNodes = new();
        foreach (CScoreIni.ERANK rank in Enum.GetValues(typeof(CScoreIni.ERANK)))
        {
            SongNode rankNode = new(clearRank, SongNode.ENodeType.BOX)
            {
                title = rank == CScoreIni.ERANK.UNKNOWN ? "Unknown" : rank.ToString()
            };
            clearRankNodes[rank] = rankNode;
        }

        //go through songs and add them based on their highest clear rank
        foreach (SongNode songNode in songDb.flattenedSongList)
        {
            for (int chartIndex = 0; chartIndex < songNode.charts.Length; chartIndex++)
            {
                CScore? chart = songNode.charts[chartIndex];
                if (chart == null || !chart.HasChartForCurrentMode()) continue;

                for (int instrument = 0; instrument < 3; instrument++)
                {
                    if (CDTXMania.ConfigIni.bDrumsEnabled && instrument != 0) continue;
                    if (!CDTXMania.ConfigIni.bDrumsEnabled && instrument == 0) continue;

                    int rank = chart.SongInformation.BestRank[instrument];
                    if (clearRankNodes.TryGetValue((CScoreIni.ERANK)rank, out SongNode? rankNode))
                    {
                        var newNode = SongNode.Clone(songNode, rankNode, false);

                        //add chart
                        newNode.filteredInstrumentPart = (EInstrumentPart)instrument;
                        newNode.charts[chartIndex] = chart;
                    }
                    else
                    {
                        Trace.TraceWarning($"Clear rank node not found for rank {rank} in song {songNode.title}");
                    }
                }
            }
        }

        foreach (SongNode box in clearRank.childNodes)
        {
            //OrderBySkill(box.childNodes);
        }

        return clearRank;
    }
}