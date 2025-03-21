using DTXMania.Core;
using DTXMania.UI.Skin;
using Hexa.NET.ImGui;

namespace DTXMania.UI;

public class GameStatus
{
    public static void Draw()
    {
        InspectorManager.hierarchyWindow.target = CDTXMania.rCurrentStage.ui;

        ImGui.Begin("Game Status");
        if (ImGui.CollapsingHeader("Game Status"))
        {
            ImGui.Text("Current Stage: " + CDTXMania.rCurrentStage.GetType());
        }

        if (ImGui.CollapsingHeader("Skin"))
        {
            DrawSkinInspector();
        }
        ImGui.End();
    }

    private static string newSkinName = "";
    private static string newSkinAuthor = "";
    private static void DrawSkinInspector()
    {
        string skinName = CDTXMania.SkinManager.currentSkin?.name ?? "No skin selected";
        ImGui.Text("Currently loaded skin: " + skinName);
        
        //display list of skins
        foreach (SkinDescriptor skin in CDTXMania.SkinManager.skins)
        {
            ImGui.Text(skin.name);
            
            ImGui.SameLine();
            
            int hash = skin.GetHashCode();
            if (ImGui.Button("Load##" + hash))
            {
                CDTXMania.SkinManager.ChangeSkin(skin);
            }
        }
        
        if (ImGui.Button("Scan skin directory"))
        {
            CDTXMania.SkinManager.ScanSkinDirectory();
        }

        if (ImGui.Button("Create new skin"))
        {
            //create modal
            ImGui.OpenPopup("Create new skin");
        }
            
        if (ImGui.BeginPopupModal("Create new skin"))
        {
            ImGui.Text("Skin Options");
            ImGui.InputText("Name", ref newSkinName, 100);
            ImGui.InputText("Author", ref newSkinAuthor, 100);
            if (ImGui.Button("Create"))
            {
                CDTXMania.SkinManager.CreateNewSkin(newSkinName, newSkinAuthor);
                ImGui.CloseCurrentPopup();
            }
            
            ImGui.SameLine();
            
            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
    }
}