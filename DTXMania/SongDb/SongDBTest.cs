using System.Numerics;
using Hexa.NET.ImGui;

namespace DTXMania.SongDb;

public class SongDBTester
{
    private static SongDb songDb = new();
    
    public static void DrawWindow()
    {
        ImGui.Begin("SongDB");

        if (ImGui.Button("Scan"))
        {
            songDb.ScanAsync();
        }

        if (!songDb.scanning)
        {
            foreach (SongNode node in songDb.songNodeRoot)
            {
                DrawNode(node);
            }
        }
        else
        {
            ImGui.Text("Scanning...");
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
}