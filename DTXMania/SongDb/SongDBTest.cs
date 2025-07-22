using System.Numerics;
using Hexa.NET.ImGui;
using NativeFileDialog.Extended;

namespace DTXMania.SongDb;

public class SongDBTester
{
    private static SongDb songDb = new();

    private static SongNode currentRoot;
    private static SongNode? selectedNode;
    
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
            Task.Run(() => songDb.ScanAsync(() => currentRoot = songDb.songNodeRoot));
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

        if (selectedNode != null)
        {
            ImGui.Separator();

            CScore chart = selectedNode.charts.FirstOrDefault(x => x != null);
            if (chart != null)
            {
                ImGui.Text("Selected Node: " + selectedNode.title);
                ImGui.Text("Path: " + selectedNode.path);
                ImGui.Text("Title: " + chart.SongInformation.Title);
                ImGui.Text("Title has japanese: " + chart.SongInformation.TitleHasJapanese);
                ImGui.Text("Title (kana): " + chart.SongInformation.TitleKana);
                ImGui.Text("Title (roman): " + chart.SongInformation.TitleRoman);
                ImGui.Text("Artist: " + chart.SongInformation.ArtistName);
                ImGui.Text("Artist has japanese: " + chart.SongInformation.ArtistNameHasJapanese);
                ImGui.Text("Artist (kana): " + chart.SongInformation.ArtistNameKana);
                ImGui.Text("Artist (roman): " + chart.SongInformation.ArtistNameRoman);
                ImGui.Text("Comment: " + chart.SongInformation.Comment);
            }
            else
            {
                ImGui.Text("Selected Node: " + selectedNode.title);
                ImGui.Text("Path: " + selectedNode.path);
                ImGui.Text("No valid charts found.");
            }
        }

        ImGui.Separator();
        DrawSortingOptions();
        ImGui.Separator();
        
        ImGui.Text("Total Song Nodes: " + songDb.totalSongs);
        ImGui.Text("Total Charts: " + songDb.totalCharts);
        
        ImGui.Separator();

        if (currentRoot != null)
        {
            foreach (SongNode node in currentRoot.childNodes)
            {
                DrawNode(node);
            }
        }
        else
        {
            ImGui.Text("No song database selected");
        }

        ImGui.End();
    }

    private static void DrawSortingOptions()
    {
        if (ImGui.Button("Title"))
        {
            SortByTitle sortByTitle = new();
            Task.Run(async () =>
            {
                List<SongNode> flattened = await songDb.FlattenSongList(songDb.songNodeRoot.childNodes); 
                currentRoot = await sortByTitle.Sort(flattened);
            });
        }
        
        ImGui.SameLine();

        if (ImGui.Button("Artist"))
        {
            SortByArtist sortByArtist = new();
            Task.Run(async () =>
            {
                List<SongNode> flattened = await songDb.FlattenSongList(songDb.songNodeRoot.childNodes); 
                currentRoot = await sortByArtist.Sort(flattened);
            });
        }
        
        ImGui.SameLine();
        
        if (ImGui.Button("Version"))
        {
            
        }
        
        ImGui.SameLine();

        if (ImGui.Button("Difficulty"))
        {
            
        }

        ImGui.SameLine();

        if (ImGui.Button("Level"))
        {
            
        }
        
        ImGui.SameLine();

        if (ImGui.Button("NEW"))
        {
            
        }
        
        ImGui.SameLine();

        if (ImGui.Button("All Songs"))
        {
            currentRoot = songDb.songNodeRoot;
        }
    }

    private static void DrawNode(SongNode node)
    {
        if (node.nodeType == SongNode.ENodeType.BACKBOX) return;
        
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

        if (ImGui.IsItemClicked())
        {
            if (node == selectedNode) selectedNode = null;
            selectedNode = node;
        }

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
        List<SongNode> flattened = await songDb.FlattenSongList(songDb.songNodeRoot.childNodes);

        await using StreamWriter writer = new(filePath);
        await writer.WriteLineAsync("Title,Artist,Comment");
        
        foreach (SongNode node in flattened)
        {
            string title = node.title.Replace(",", " ");
            
            CScore? chart = node.charts.FirstOrDefault(x => x != null);
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