using System.Diagnostics;
using System.Numerics;
using DTXMania.Core;
using DTXMania.SongDb;
using DTXMania.UI.Drawable;
using DTXMania.UI.Skin;
using DTXMania.UI.Skin.Preview;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Inspector;

/// <summary>
/// Editor window for the skin system. Reads top down: which skin is loaded, what to do with the stage on
/// screen, and the stages and components that stage is built from.
/// </summary>
public class SkinEditorWindow
{
    //read by UIDrawableConverter to log every serialization decision
    public static bool logThemeApplyDetails = false;

    private static readonly Vector4 Accent = new(0.16f, 0.5f, 0.22f, 1f);
    private static readonly Vector4 Danger = new(0.55f, 0.18f, 0.18f, 1f);
    private static readonly Vector4 Live = new(0.4f, 1f, 0.5f, 1f);

    //what comes from the built-in skin rather than the one being edited
    private static readonly Vector4 Default = new(0.55f, 0.75f, 1.0f, 1f);

    //the popup a button asked for, opened once the id stack is back where the popup itself is begun
    private string openPopup = string.Empty;

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

        //a negative height is how a child leaves room for what comes after it, so the footer keeps its
        //place without anything being measured or padded to reach it
        ImGui.BeginChild("skineditor", new Vector2(0.0f, -ImGui.GetFrameHeightWithSpacing()));

        DrawHeader(skinManager, currentSkin);
        ImGui.Separator();

        //nothing below is about anything until a skin is loaded, so until then this is a chooser
        if (currentSkin == null)
        {
            DrawSkinChooser(skinManager);
        }
        else
        {
            DrawSongRow();
            ImGui.Spacing();

            DrawStageActions(skinManager, currentSkin);
            DrawStageBar(skinManager, currentSkin);
            ImGui.Spacing();

            DrawStageComponents();
        }

        //opened here rather than where the button is: a popup is found by an id, and every child and
        //table column the button sits inside pushes one of its own
        if (openPopup.Length > 0)
        {
            ImGui.OpenPopup(openPopup);
            openPopup = string.Empty;
        }

        DrawCreateSkinModal(skinManager);
        DrawDeleteSkinModal(skinManager);
        DrawRemoveStageModal(skinManager);

        ImGui.EndChild();

        DrawAdvanced();
    }

    //the skin is what the window is about, so it is named once at the top rather than in a section
    //two columns: one that takes what is left, and one sized to its own content. That is what puts the
    //mode on the right edge, rather than anything here working out how wide it is
    private void DrawHeader(SkinManager skinManager, SkinDescriptor? currentSkin)
    {
        if (ImGui.BeginTable("skinheader", 2))
        {
            ImGui.TableSetupColumn("skin", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("mode", ImGuiTableColumnFlags.WidthFixed);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            DrawSkinLine(skinManager, currentSkin);

            //preview mode is not the skin's, so it stays reachable with none loaded
            ImGui.TableNextColumn();
            DrawPreviewMode();

            ImGui.EndTable();
        }
    }

    private void DrawSkinLine(SkinManager skinManager, SkinDescriptor? currentSkin)
    {
        ImGui.AlignTextToFramePadding();

        if (currentSkin == null)
        {
            ImGui.TextDisabled("No skin loaded.");
            return;
        }

        ImGui.TextColored(Live, currentSkin.name);
        ImGui.SameLine();
        ImGui.TextDisabled($"by {currentSkin.author}");

        ImGui.SameLine();
        if (ImGui.Button("Reload from Disk"))
        {
            //re-read the skin, picking up json edited outside the editor, and rebuild the stage
            skinManager.ChangeSkin(currentSkin);
            CDTXMania.tRunGarbageCollector();
        }

        ImGui.SameLine();
        if (ImGui.Button("Open Folder"))
        {
            OpenFolder(currentSkin.basePath);
        }

        ImGui.SameLine();
        if (ImGui.Button("Unload"))
        {
            skinManager.ChangeSkin(null);
            CDTXMania.tRunGarbageCollector();
        }
    }

    /// <summary>Preview mode belongs to the whole editing session rather than to one stage: it holds every
    /// stage still, stands in for a song library, and stops the transition covering what is being looked
    /// at.</summary>
    private static void DrawPreviewMode()
    {
        bool preview = SkinPreview.IsActive;
        if (ImGui.Checkbox("Preview mode", ref preview))
        {
            if (preview)
            {
                SkinPreview.Enter();
            }
            else
            {
                SkinPreview.Exit();
            }
        }

        ImGui.SameLine();
        StageOptionsWindow.HelpMarker(
            "Stops stage transitions and uses placeholders for game data "
            + "such as songs, charts and scores");
    }


    //what the dropdown is pointing at, which is only acted on when Load Stage is pressed. Following the
    //game when it moves itself, so the dropdown does not lie about where you are
    private CStage.EStage pendingStage = CStage.EStage.DoNothing_0;
    private CStage.EStage lastSeenStage = CStage.EStage.DoNothing_0;

    /// <summary>The stage on screen, whether this skin has it, and what to do with it. Loading is explicit:
    /// moving to another stage throws away whatever has been changed on this one.</summary>
    private void DrawStageBar(SkinManager skinManager, SkinDescriptor currentSkin)
    {
        CStage stage = CDTXMania.StageManager.rCurrentStage;

        if (stage.eStageID != lastSeenStage)
        {
            lastSeenStage = stage.eStageID;
            pendingStage = stage.eStageID;
        }

        ImGui.AlignTextToFramePadding();
        ImGui.Text("Change stage:");

        ImGui.SameLine();
        ImGui.SetNextItemWidth(200.0f);
        if (ImGui.BeginCombo("##stage", pendingStage.ToString()))
        {
            foreach (CStage.EStage option in SkinPreview.Stages)
            {
                if (ImGui.Selectable($"{option}##pick{option}", option == pendingStage))
                {
                    pendingStage = option;
                }

                ImGui.SameLine();
                ImGui.TextDisabled(StateOf(skinManager, option));
            }

            ImGui.EndCombo();
        }

        //the song is chosen just above, so a stage that reads one says so on the button rather than
        //sending anyone looking for it
        bool needsSong = SkinPreview.RequiresSong(pendingStage) && SkinPreview.SelectedChart == null;

        ImGui.SameLine();
        ImGui.BeginDisabled(needsSong);
        if (ImGui.Button("Load Stage"))
        {
            SkinPreview.LoadStage(pendingStage);
        }

        ImGui.EndDisabled();

        if (needsSong && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip("This stage plays a chart. Choose a song above first.");
        }
    }

    private void DrawStageActions(SkinManager skinManager, SkinDescriptor currentSkin)
    {
        CStage stage = CDTXMania.StageManager.rCurrentStage;
        bool inSkin = HasLayout(skinManager, stage.eStageID);

        //the same two columns the component rows use, so the buttons line up down the right edge
        if (!ImGui.BeginTable("stageactions", 2))
        {
            return;
        }

        ImGui.TableSetupColumn("stage", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("actions", ImGuiTableColumnFlags.WidthFixed);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        ImGui.AlignTextToFramePadding();
        ImGui.Text($"Current stage: {stage.eStageID}");

        ImGui.TableNextColumn();
        ImGui.PushStyleColor(ImGuiCol.Button, Accent);
        bool act = ImGui.Button(inSkin ? "Save" : "Add to Skin");
        ImGui.PopStyleColor();

        if (act)
        {
            if (inSkin)
            {
                currentSkin.Save();
                currentSkin.SaveCurrentStageChanges();
                CDTXMania.tRunGarbageCollector();
                stage.LoadUI(true);
            }
            else
            {
                StageLayoutGenerator.GenerateForCurrentStage();
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Reload"))
        {
            stage.LoadUI(true);
        }

        ImGui.SameLine();
        ImGui.BeginDisabled(!inSkin);
        ImGui.PushStyleColor(ImGuiCol.Button, Danger);
        if (ImGui.Button("Remove from Skin"))
        {
            openPopup = "Remove stage from skin";
        }

        ImGui.PopStyleColor();
        ImGui.EndDisabled();

        ImGui.EndTable();
    }

    //a stage is either this skin's or the built-in one's; nothing here is about where it came from
    private static string StateOf(SkinManager skinManager, CStage.EStage stage)
        => HasLayout(skinManager, stage) ? "skin" : "default";

    /// <summary>The chart a stage that plays one is opened with. Changing it loads nothing on its own.
    /// </summary>
    private void DrawSongRow()
    {
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Song");

        ImGui.SameLine();
        ImGui.SetNextItemWidth(200.0f);

        string title = SkinPreview.selectedSong?.title ?? "None selected";
        if (ImGui.BeginCombo("##song", title))
        {
            DrawSongOptions();
            ImGui.EndCombo();
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(140.0f);
        DrawDifficultyOptions();
    }

    //the names the song itself uses, so a chart called TV SIZE is offered as TV SIZE
    private static void DrawDifficultyOptions()
    {
        SongNode? song = SkinPreview.selectedSong;

        if (!ImGui.BeginCombo("##difficulty", DifficultyName(song, SkinPreview.difficulty)))
        {
            return;
        }

        for (int slot = 0; slot < DifficultyLabel.SlotNames.Length; slot++)
        {
            //a slot the song has no chart for would load as some other difficulty without saying so
            if (song != null && song.charts[slot] == null)
            {
                continue;
            }

            if (ImGui.Selectable($"{DifficultyName(song, slot)}##slot{slot}", slot == SkinPreview.difficulty))
            {
                SkinPreview.difficulty = slot;
            }
        }

        ImGui.EndCombo();
    }

    //DifficultyLabel.Resolve answers with a name there is art for, so a chart called something of its own
    //comes back as its slot instead. Here the charter's name is the useful one
    private static string DifficultyName(SongNode? song, int slot)
    {
        string? label = song?.difficultyLabel[slot];

        return string.IsNullOrWhiteSpace(label) ? DifficultyLabel.SlotNames[slot] : label;
    }

    private string songFilter = string.Empty;

    private void DrawSongOptions()
    {
        SongDb.SongDb library = CDTXMania.SongDb;
        if (library is not { hasEverScanned: true })
        {
            ImGui.TextDisabled("No songs found");
            return;
        }

        ImGui.SetNextItemWidth(-1.0f);
        ImGui.InputTextWithHint("##songfilter", "Search", ref songFilter, 128);

        int shown = 0;
        foreach (SongNode node in library.flattenedSongList)
        {
            if (shown >= MaxSongOptions)
            {
                ImGui.TextDisabled($"First {MaxSongOptions} matches");
                break;
            }

            if (node.nodeType != SongNode.ENodeType.SONG
                || !node.title.Contains(songFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            shown++;
            if (ImGui.Selectable($"{node.title}##{node.path}", ReferenceEquals(node, SkinPreview.selectedSong)))
            {
                SkinPreview.selectedSong = node;
            }
        }
    }

    private const int MaxSongOptions = 200;

    private static void DrawStageComponents()
    {
        if (!Section("Components"))
        {
            return;
        }

        CStage stage = CDTXMania.StageManager.rCurrentStage;

        List<UIGroup> components = [];
        CollectComponents(stage.ui, components);

        if (components.Count == 0)
        {
            ImGui.TextDisabled($"{stage.eStageID} does not use any components.");
            return;
        }

        if (!ImGui.BeginTable("components", 3, ImGuiTableFlags.RowBg))
        {
            return;
        }

        ImGui.TableSetupColumn("component", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("source", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("actions", ImGuiTableColumnFlags.WidthFixed);

        foreach (UIGroup component in components)
        {
            DrawComponentRow(component);
        }

        ImGui.EndTable();
    }

    private static void DrawComponentRow(UIGroup component)
    {
        //a component the skin has no file for is still in use; it is the built-in one until saved
        bool inSkin = component.ComponentPath() is { } path && File.Exists(path);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        ImGui.AlignTextToFramePadding();
        if (inSkin)
        {
            ImGui.Text(component.componentName);
        }
        else
        {
            ImGui.TextColored(Default, component.componentName);
        }

        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextDisabled(inSkin ? "skin" : "default");

        ImGui.TableNextColumn();
        ImGui.BeginDisabled(!inSkin);
        ImGui.BeginGroup();

        if (ImGui.Button($"Edit##component{component.componentName}"))
        {
            ComponentEditor.Open(component.component, component.GetType());
        }

        ImGui.SameLine();
        if (ImGui.Button($"Delete##component{component.componentName}"))
        {
            DeleteComponent(component.ComponentPath());
        }

        ImGui.EndGroup();
        ImGui.EndDisabled();

        if (!inSkin && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip("Save this stage to give the skin its own copy.");
        }
    }

    //one entry per name: a component placed three times is still one thing to edit, and a component
    //the skin has no file for yet is still one the stage uses
    private static void CollectComponents(UIDrawable node, List<UIGroup> found)
    {
        if (node is not UIGroup group)
        {
            return;
        }

        if (group.IsComponent && !found.Any(other => other.componentName == group.componentName))
        {
            found.Add(group);
        }

        foreach (UIDrawable child in group.children)
        {
            CollectComponents(child, found);
        }
    }

    private static void DeleteComponent(string? fullPath)
    {
        if (fullPath == null)
        {
            return;
        }

        try
        {
            File.Delete(fullPath);
        }
        catch (Exception e)
        {
            Trace.TraceError($"Failed to delete component {fullPath}: {e.Message}");
        }

        //rebuild so the deleted file re-seeds from code
        UIGroup.ClearComponentCache();
        CDTXMania.StageManager.rCurrentStage.LoadUI(true);
    }

    private static void DrawRemoveStageModal(SkinManager skinManager)
    {
        if (!ImGui.BeginPopupModal("Remove stage from skin", ImGuiWindowFlags.AlwaysAutoResize))
        {
            return;
        }

        CStage stage = CDTXMania.StageManager.rCurrentStage;

        ImGui.Text($"Remove {stage.eStageID} from skin?");
        ImGui.TextDisabled("It goes back to the default skin. Components it uses are kept.");
        ImGui.Spacing();

        ImGui.PushStyleColor(ImGuiCol.Button, Danger);
        if (ImGui.Button("Remove"))
        {
            DeleteLayout(skinManager, stage);
            ImGui.CloseCurrentPopup();
        }

        ImGui.PopStyleColor();

        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

    private static void DeleteLayout(SkinManager skinManager, CStage stage)
    {
        if (skinManager.LayoutPathFor(stage.eStageID) is { } path)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception e)
            {
                Trace.TraceError($"Failed to delete layout {path}: {e.Message}");
            }
        }

        UIGroup.ClearComponentCache();
        stage.LoadUI(true);
    }

    private void DrawSkinChooser(SkinManager skinManager)
    {
        if (ImGui.Button("Create New Skin"))
        {
            openPopup = "Create new skin";
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

        if (!ImGui.BeginTable("skins", 2, ImGuiTableFlags.RowBg))
        {
            return;
        }

        ImGui.TableSetupColumn("skin", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("actions", ImGuiTableColumnFlags.WidthFixed);

        foreach (SkinDescriptor skin in skinManager.skins)
        {
            string folder = SkinManager.FolderNameOf(skin);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            ImGui.AlignTextToFramePadding();
            ImGui.Text(skin.name);
            ImGui.SameLine();
            ImGui.TextDisabled($"by {skin.author}");

            ImGui.TableNextColumn();
            if (ImGui.Button($"Load##skin{folder}"))
            {
                skinManager.ChangeSkin(skin);
            }

            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Button, Danger);
            if (ImGui.Button($"Delete##skin{folder}"))
            {
                skinToDelete = skin;
                openPopup = "Delete skin";
            }

            ImGui.PopStyleColor();
        }

        ImGui.EndTable();
    }

    //held while the popup is up: the row it was pressed on is gone by the time the answer comes back
    private SkinDescriptor? skinToDelete;

    private void DrawDeleteSkinModal(SkinManager skinManager)
    {
        if (!ImGui.BeginPopupModal("Delete skin", ImGuiWindowFlags.AlwaysAutoResize))
        {
            return;
        }

        if (skinToDelete == null)
        {
            ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
            return;
        }

        ImGui.Text($"Delete {skinToDelete.name}?");
        ImGui.Spacing();

        ImGui.PushStyleColor(ImGuiCol.Button, Danger);
        if (ImGui.Button("Delete"))
        {
            DeleteSkin(skinManager, skinToDelete);
            skinToDelete = null;
            ImGui.CloseCurrentPopup();
        }

        ImGui.PopStyleColor();

        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            skinToDelete = null;
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

    private static void DeleteSkin(SkinManager skinManager, SkinDescriptor skin)
    {
        try
        {
            Directory.Delete(skin.basePath, recursive: true);
            Trace.TraceInformation($"Deleted skin {skin.basePath}");
        }
        catch (Exception e)
        {
            Trace.TraceError($"Failed to delete skin {skin.basePath}: {e.Message}");
        }

        skinManager.ScanSkinDirectory();
    }

    private void DrawCreateSkinModal(SkinManager skinManager)
    {
        if (!ImGui.BeginPopupModal("Create new skin", ImGuiWindowFlags.AlwaysAutoResize))
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

    private static bool HasLayout(SkinManager skinManager, CStage.EStage stage)

        => skinManager.LayoutPathFor(stage) is { } path && File.Exists(path);

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

    //pushed to the foot of the window: it is not part of the work, it just has to live somewhere
    //a popup rather than a header, so the footer is one line whether it is open or not
    private static void DrawAdvanced()
    {
        if (ImGui.Button("Advanced"))
        {
            ImGui.OpenPopup("advanced");
        }

        if (!ImGui.BeginPopup("advanced"))
        {
            return;
        }

        bool hold = SkinPreview.HoldStage;
        if (ImGui.Checkbox("Prevent stage changes", ref hold))
        {
            SkinPreview.HoldStage = hold;
        }

        ImGui.Checkbox("Log serializer decisions", ref logThemeApplyDetails);

        ImGui.EndPopup();
    }
}
