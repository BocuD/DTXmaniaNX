namespace DTXMania.SongDb;

public class SortByLastPlayed : SongDbSort
{
    public override string Name => "Recently\nPlayed";
    public override string IconName => "newmusic";

    public override Task<SongNode> Sort(SongDb songDb)
    {
        //create a new root node
        SongNode root = new(null, SongNode.ENodeType.ROOT);
        
        //create groups for today, yesterday, this week, this month, this year, older, never
        SongNode todayNode = new(root, SongNode.ENodeType.BOX) { title = "Today" };
        SongNode yesterdayNode = new(root, SongNode.ENodeType.BOX) { title = "Yesterday" };
        SongNode thisWeekNode = new(root, SongNode.ENodeType.BOX) { title = "This Week" };
        SongNode thisMonthNode = new(root, SongNode.ENodeType.BOX) { title = "This Month" };
        SongNode thisYearNode = new(root, SongNode.ENodeType.BOX) { title = "This Year" };
        SongNode olderNode = new(root, SongNode.ENodeType.BOX) { title = "Older" };
        SongNode neverNode = new(root, SongNode.ENodeType.BOX) { title = "Never Played" };
        
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

        foreach (SongNode box in root.childNodes)
        {
            OrderByLastPlayedDate(box.childNodes);
        }
        
        return Task.FromResult(root);
    }
}