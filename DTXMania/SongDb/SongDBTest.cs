using System.Numerics;
using Hexa.NET.ImGui;
using NativeFileDialog.Extended;

namespace DTXMania.SongDb;

public class SongDBTester
{
    private static SongDb songDb = new();
    
    public static void DrawWindow()
    {
        ImGui.Begin("SongDB");
        
        if (ImGui.Button("Export to CSV"))
        {
            string defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string filePath = NFD.SaveDialog(defaultPath, "SongDb.csv");
            
            if (!string.IsNullOrEmpty(filePath))
            {
                ExportSongDb(filePath);
            }
        }

        if (ImGui.Button("Scan"))
        {
            Task.Run(songDb.ScanAsync);
        }
        
        ImGui.Text("Last scan time: " + songDb.statusDuration[SongDbScanStatus.Scanning]);
        ImGui.Text("Last process time: " + songDb.statusDuration[SongDbScanStatus.Processing]);

        if (songDb.status != SongDbScanStatus.Idle)
        {
            switch (songDb.status)
            {
                case SongDbScanStatus.Scanning:
                    ImGui.SameLine();
                    ImGui.Text("Scanning...");
                    break;

                case SongDbScanStatus.Processing:
                    ImGui.SameLine();
                    ImGui.Text("Processing...");
                    ImGui.Text(songDb.processSongDataPath);
                    ImGui.ProgressBar((float)songDb.processDoneCount / songDb.processTotalCount,
                        new Vector2(ImGui.GetWindowSize().X - 20.0f, 20),
                        $"{songDb.processDoneCount} / {songDb.processTotalCount}");
                    break;
            }
        }
        
        ImGui.Separator();
        ImGui.Text("Total Song Nodes: " + songDb.totalSongs);
        ImGui.Text("Total Charts: " + songDb.totalCharts);
        ImGui.Separator();
        foreach (SongNode node in songDb.songNodeRoot)
        {
            DrawNode(node);
        }

        ImGui.End();
    }

    private static void DrawNode(SongNode node)
    {
        //treenode ex
        ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None;
        if (node.childNodes != null && node.childNodes.Count > 0)
        {
            flags |= ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;
        }
        else 
        {
            flags |= ImGuiTreeNodeFlags.Leaf;
        }

        string id = node.title + "##" + node.GetHashCode();

        if (string.IsNullOrWhiteSpace(node.title))
        {
            id = node.path + "##" + node.GetHashCode();
        }
        
        bool isOpen = ImGui.TreeNodeEx(id, flags);

        if (isOpen)
        {
            if (node.childNodes != null)
            {
                foreach (SongNode child in node.childNodes)
                {
                    DrawNode(child);
                }
            }

            ImGui.TreePop();
        }
    }
    
    private static async Task ExportSongDb(string filePath)
    {
        List<SongNode> flattened = await songDb.FlattenSongList(songDb.songNodeRoot);

        await using StreamWriter writer = new(filePath);
        await writer.WriteLineAsync("Title,Artist,Comment");
        
        foreach (SongNode node in flattened)
        {
            string title = node.title.Replace(",", " ");
            
            var chart = node.charts.FirstOrDefault(x => x != null);
            if (chart == null)
            {
                continue; // Skip nodes without valid charts
            }
            
            string artist = chart.SongInformation.ArtistName.Replace(",", " ");
            string comment = chart.SongInformation.Comment.Replace(",", " ");
            
            await writer.WriteLineAsync($"{title},{artist},{comment}");
        }
    }
}