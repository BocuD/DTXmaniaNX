using System.Diagnostics;
using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Drawable;
using DTXMania.UI.Skin;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Inspector;

/// <summary>
/// Editor window for the skin system: shows the active skin, saves/unloads/reloads it, switches between
/// installed skins, creates new ones, generates layouts, and resets the current stage's UI.
/// </summary>
public class SkinEditorWindow
{
    //read by UIDrawableConverter to log every serialization decision
    public static bool logThemeApplyDetails = false;

    private string newSkinName = "";
    private string newSkinAuthor = "";

    public void Draw()
    {
        try
        {
            ImGui.Begin("Skin Editor", ImGuiWindowFlags.NoFocusOnAppearing);

            DrawContents();
        }
        finally
        {
            ImGui.End();
        }
    }

    private void DrawContents()
    {
        SkinManager skinManager = CDTXMania.SkinManager;
        SkinDescriptor? currentSkin = skinManager.currentSkin;

        //belongs to every tab
        DrawActiveSkinSection(skinManager, currentSkin);

        if (ImGui.BeginTabBar("SkinEditorTabs"))
        {
            if (ImGui.BeginTabItem("Stage"))
            {
                InspectorManager.skinPreview.DrawContents();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Layout"))
            {
                DrawCurrentStageSection(skinManager);

                if (currentSkin != null)
                {
                    DrawComponentsSection(currentSkin);
                    DrawSkinnedStagesSection(currentSkin);
                }

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Skins"))
            {
                DrawAvailableSkinsSection(skinManager, currentSkin);
                DrawAdvancedSection();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        DrawCreateSkinModal(skinManager);
    }

    private static void DrawActiveSkinSection(SkinManager skinManager, SkinDescriptor? currentSkin)
    {
        if (!Section("Active Skin"))
        {
            return;
        }

        if (currentSkin != null)
        {
            ImGui.Text(currentSkin.name);
            ImGui.SameLine();
            ImGui.TextDisabled($"by {currentSkin.author}");
            ImGui.TextDisabled(currentSkin.basePath);

            if (!string.IsNullOrWhiteSpace(currentSkin.description))
            {
                ImGui.TextWrapped(currentSkin.description);
            }
        }
        else
        {
            ImGui.Text("System (default)");
            ImGui.TextDisabled(SkinManager.SystemRoot);
        }

        ImGui.BeginDisabled(currentSkin == null);

        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.16f, 0.5f, 0.22f, 1f));
        bool save = ImGui.Button("Save Changes");
        ImGui.PopStyleColor();
        if (save && currentSkin != null)
        {
            currentSkin.Save();
            currentSkin.SaveCurrentStageChanges();
            CDTXMania.tRunGarbageCollector();
            CDTXMania.StageManager.rCurrentStage.LoadUI(true);
        }

        ImGui.SameLine();
        if (ImGui.Button("Reload from Disk") && currentSkin != null)
        {
            //re-read the skin, picking up json edited outside the editor, and rebuild the stage
            skinManager.ChangeSkin(currentSkin);
            CDTXMania.tRunGarbageCollector();
        }

        ImGui.SameLine();
        if (ImGui.Button("Unload"))
        {
            skinManager.ChangeSkin(null);
            CDTXMania.tRunGarbageCollector();
        }

        ImGui.EndDisabled();

        ImGui.SameLine();
        if (ImGui.Button("Open Folder"))
        {
            OpenFolder(currentSkin?.basePath ?? SkinManager.SystemRoot);
        }
    }

    private static void DrawCurrentStageSection(SkinManager skinManager)
    {
        if (!Section("Current Stage"))
        {
            return;
        }

        CStage stage = CDTXMania.StageManager.rCurrentStage;
        ImGui.Text($"Stage: {stage.eStageID}");
        ImGui.SameLine();

        string? layoutPath = skinManager.LayoutPathFor(stage.eStageID);
        bool hasLayout = layoutPath != null && File.Exists(layoutPath);
        ImGui.TextDisabled(skinManager.currentSkin == null
            ? "(System — code)"
            : hasLayout ? "(skinned)" : "(code default)");

        ImGui.BeginDisabled(skinManager.currentSkin == null);
        if (ImGui.Button("Generate Layout from Code"))
        {
            StageLayoutGenerator.GenerateForCurrentStage();
        }
        ImGui.EndDisabled();

        ImGui.SameLine();
        if (ImGui.Button("Reload Stage UI"))
        {
            stage.LoadUI(true);
        }

        ImGui.SameLine();
        if (ImGui.Button("Reset (ignore skin)"))
        {
            stage.LoadUI(false);
        }
    }

    private static void DrawSkinnedStagesSection(SkinDescriptor skin)
    {
        if (!Section("Skinned Stages"))
        {
            return;
        }

        static int StageOrder(string file)
            => Enum.TryParse(Path.GetFileNameWithoutExtension(file), out CStage.EStage stage)
                ? (int)stage
                : int.MaxValue;

        string folder = skin.layoutFolder;
        string[] files = Directory.Exists(folder) ? Directory.GetFiles(folder, "*.json") : [];

        //a layout file is named after its stage, so they list in the order the game runs them
        Array.Sort(files, (left, right) => StageOrder(left).CompareTo(StageOrder(right)));

        if (files.Length == 0)
        {
            ImGui.TextDisabled("None");
            return;
        }

        string currentStageName = CDTXMania.StageManager.rCurrentStage.eStageID.ToString();

        foreach (string file in files)
        {
            string stageName = Path.GetFileNameWithoutExtension(file);
            bool isCurrent = stageName == currentStageName;

            if (isCurrent)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.4f, 1f, 0.5f, 1f));
            }

            ImGui.BulletText(stageName);

            if (isCurrent)
            {
                ImGui.PopStyleColor();
            }

            ImGui.SameLine(ImGui.GetWindowWidth() - 70);
            if (ImGui.Button("Delete##skinnedstage" + stageName))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception e)
                {
                    Trace.TraceError($"Failed to delete layout {file}: {e.Message}");
                }

                //reverts to the code base if we removed the layout for the stage we're on
                if (isCurrent)
                {
                    CDTXMania.StageManager.rCurrentStage.LoadUI(true);
                }
            }
        }
    }

    private static void DrawComponentsSection(SkinDescriptor skin)
    {
        if (!Section("Components"))
        {
            return;
        }

        string folder = skin.componentFolder;
        string[] files = Directory.Exists(folder) ? Directory.GetFiles(folder, "*.json") : [];

        //component files are seeded from code the first time a stage that uses one is shown
        if (files.Length == 0)
        {
            ImGui.TextDisabled("None");
            return;
        }

        foreach (string file in files)
        {
            string componentName = Path.GetFileNameWithoutExtension(file);
            ImGui.BulletText(componentName);

            ImGui.SameLine(ImGui.GetWindowWidth() - 130);
            if (ImGui.Button("Edit##component" + componentName))
            {
                ComponentEditor.Open($"Components/{Path.GetFileName(file)}");
            }

            ImGui.SameLine(ImGui.GetWindowWidth() - 70);
            if (ImGui.Button("Delete##component" + componentName))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception e)
                {
                    Trace.TraceError($"Failed to delete component {file}: {e.Message}");
                }

                //rebuild so the deleted file re-seeds from code
                ComponentInstance.ClearCache();
                CDTXMania.StageManager.rCurrentStage.LoadUI(true);
            }
        }
    }

    private void DrawAvailableSkinsSection(SkinManager skinManager, SkinDescriptor? currentSkin)
    {
        if (!Section("Available Skins"))
        {
            return;
        }

        if (ImGui.Button("Create New Skin"))
        {
            ImGui.OpenPopup("Create new skin");
        }

        ImGui.SameLine();
        if (ImGui.Button("Rescan"))
        {
            skinManager.ScanSkinDirectory();
        }

        ImGui.SameLine();
        if (ImGui.Button("Open Skins Folder"))
        {
            OpenFolder(SkinManager.SkinsDirectory);
        }

        ImGui.Spacing();

        if (skinManager.skins.Count == 0)
        {
            ImGui.TextDisabled("No skins installed");
            return;
        }

        foreach (SkinDescriptor skin in skinManager.skins)
        {
            bool active = currentSkin != null && currentSkin.name == skin.name;

            if (active)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.4f, 1f, 0.5f, 1f));
            }

            ImGui.Text(skin.name);

            if (active)
            {
                ImGui.PopStyleColor();
            }

            ImGui.SameLine();
            ImGui.TextDisabled($"by {skin.author}");

            ImGui.SameLine(ImGui.GetWindowWidth() - 70);
            if (active)
            {
                ImGui.TextDisabled("active");
            }
            else if (ImGui.Button("Load##" + skin.GetHashCode()))
            {
                skinManager.ChangeSkin(skin);
            }
        }
    }

    private static void DrawAdvancedSection()
    {
        ImGui.Spacing();
        if (ImGui.CollapsingHeader("Advanced"))
        {
            ImGui.Checkbox("Debug Theme Serializer", ref logThemeApplyDetails);
        }
    }

    private void DrawCreateSkinModal(SkinManager skinManager)
    {
        if (!ImGui.BeginPopupModal("Create new skin"))
        {
            return;
        }

        ImGui.Text("Skin Options");
        ImGui.InputText("Name", ref newSkinName, 100);
        ImGui.InputText("Author", ref newSkinAuthor, 100);

        ImGui.BeginDisabled(string.IsNullOrWhiteSpace(newSkinName));
        if (ImGui.Button("Create"))
        {
            skinManager.CreateNewSkin(newSkinName, newSkinAuthor);
            newSkinName = "";
            newSkinAuthor = "";
            ImGui.CloseCurrentPopup();
        }
        ImGui.EndDisabled();

        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

    internal static bool Section(string title)
        => ImGui.CollapsingHeader(title, ImGuiTreeNodeFlags.DefaultOpen);

    private static void OpenFolder(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            }
            else
            {
                Trace.TraceWarning($"Cannot open folder, path does not exist: {path}");
            }
        }
        catch (Exception e)
        {
            Trace.TraceError($"Failed to open folder {path}: {e.Message}");
        }
    }
}
