using DTXMania.Core;
using DTXMania.UI.Skin;
using Hexa.NET.ImGui;

namespace DTXMania.UI;

public class GameStatus
{
    private static bool demoWindowShown = false;
    private static bool preventGameKeyboardInput = false;
    
    public static void Draw()
    {
        CDTXMania.InputManager.Keyboard.preventKeyboardInput = ImGui.GetIO().WantCaptureKeyboard || preventGameKeyboardInput;
        
        InspectorManager.hierarchyWindow.target = CDTXMania.rCurrentStage.ui;

        ImGui.Begin("Game State");
        if (ImGui.CollapsingHeader("Game State"))
        {
            ImGui.Text("Current Stage: " + CDTXMania.rCurrentStage.GetType());
            
            ImGui.Checkbox("Prevent game keyboard input", ref preventGameKeyboardInput);

            ImGui.Checkbox("Render game viewport to window", ref CDTXMania.renderGameToSurface);
        }

        if (ImGui.CollapsingHeader("Skin"))
        {
            DrawSkinInspector();
        }

        if (ImGui.CollapsingHeader("Other"))
        {
            //put a button at the bottom of the window to open demo window
            if (ImGui.Button("Toggle Demo Window"))
            {
                demoWindowShown = !demoWindowShown;
            }
        }

        ImGui.End();
        
        if (demoWindowShown)
        {
            ImGui.ShowDemoWindow(ref demoWindowShown);
        }
    }

    private static string newSkinName = "";
    private static string newSkinAuthor = "";
    private static void DrawSkinInspector()
    {
        var currentSkin = CDTXMania.SkinManager.currentSkin;
        
        string skinName = currentSkin?.name ?? "No skin selected";

        if (ImGui.CollapsingHeader("Currently loaded skin: " + skinName))
        {
            if (currentSkin != null)
            {
                currentSkin.DrawInspector();
                
                if (ImGui.Button("Save Skin Changes"))
                {
                    currentSkin.Save();
                }
            }
        }

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

        ImGui.SameLine();
        
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