using System.Diagnostics;
using DTXMania.Core;
using DTXMania.UI.Animation;
using DTXMania.UI.Drawable;

namespace DTXMania.UI.Skin;

public class SkinManager
{
    public static string SkinsDirectory => Path.Combine(CDTXMania.executableDirectory, "Skins");

    //root of the built-in base skin: ResourceSource.System and every stage without a custom layout resolve
    //here. Hardcoded to <exe>/System so the new skin system doesn't depend on the legacy CSkin paths
    public static string SystemRoot => Path.Combine(CDTXMania.executableDirectory, "System");

    public static string SystemPath(string relativePath) => Path.Combine(SystemRoot, relativePath);

    //skin-relative paths of a skin's components, as authored into ComponentInstance.component
    public static string[] ComponentPaths(SkinDescriptor skin)
        => Directory.Exists(skin.componentFolder)
            ? Directory.GetFiles(skin.componentFolder, "*.json")
                .Select(f => $"Components/{Path.GetFileName(f)}").ToArray()
            : [];

    public List<SkinDescriptor> skins { get; } = [];

    //the loaded custom skin, or null when the base (System) skin is active
    public SkinDescriptor? currentSkin { get; private set; }

    public SkinManager()
    {
        ScanSkinDirectory();
        RestoreSelectedSkin();
    }

    public static string FolderNameOf(SkinDescriptor skin) => new DirectoryInfo(skin.basePath).Name;

    private void RestoreSelectedSkin()
    {
        string folder = CDTXMania.ConfigIni.strSkinFolder;

        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        SkinDescriptor? saved = skins.FirstOrDefault(
            skin => string.Equals(FolderNameOf(skin), folder, StringComparison.OrdinalIgnoreCase));

        if (saved == null)
        {
            Trace.TraceWarning($"Skin \"{folder}\" is no longer in {SkinsDirectory}; using the built-in skin.");
            return;
        }

        currentSkin = SkinDescriptor.LoadSkin(saved.basePath);
    }

    public void ScanSkinDirectory()
    {
        skins.Clear();

        if (!Directory.Exists(SkinsDirectory))
        {
            Directory.CreateDirectory(SkinsDirectory);
        }

        foreach (string directory in Directory.GetDirectories(SkinsDirectory))
        {
            try
            {
                SkinDescriptor? skin = SkinDescriptor.LoadSkin(directory);

                if (skin != null)
                {
                    skins.Add(skin);
                }
            }
            catch (Exception e)
            {
                Trace.TraceError($"Failed to load skin from {directory}: {e.Message}");
            }
        }
    }

    //null on the System skin, which is code-defined by design and never loads layout json
    public string? LayoutPathFor(CStage.EStage stageId) => currentSkin?.LayoutPath(stageId);

    /// <summary>
    /// Builds a stage's UI tree from the active skin's layout json, or null if there is none (always so on
    /// the System skin). When a layout exists it fully defines the serializable part of the stage; there is
    /// no merging with the code default.
    /// </summary>
    public UIGroup? LoadStageLayout(CStage.EStage stageId)
        => LayoutPathFor(stageId) is { } path ? UILayout.Load(path) : null;

    public void SaveStageLayout(CStage.EStage stageId, UIGroup group)
    {
        if (LayoutPathFor(stageId) is { } path)
        {
            CopySystemClipsIntoSkin(stageId, group);
            UILayout.Save(path, group);
        }
        else
        {
            Trace.TraceWarning("SaveStageLayout ignored: no custom skin active (System is code-defined).");
        }
    }

    /// <summary>
    /// Gives the skin its own copy of every built-in clip the stage uses, so the saved layout references
    /// files the skin owns rather than depending on what the System folder happens to hold.
    /// </summary>
    private static void CopySystemClipsIntoSkin(CStage.EStage stageId, UIDrawable node)
    {
        if (node is UIGroup group)
        {
            foreach (AnimatorClip entry in group.animator?.clips ?? [])
            {
                //an embedded clip stays in the layout; only one that points at a built-in file needs copying
                if (!entry.IsEmbedded && entry.resource.source == ResourceSource.System)
                {
                    AnimationClipIO.MoveIntoSkin(entry, Path.Combine(stageId.ToString(), entry.clip.name + ".json"));
                }
            }

            foreach (UIDrawable child in group.children)
            {
                CopySystemClipsIntoSkin(stageId, child);
            }
        }
    }

    public void CreateNewSkin(string newSkinName, string newSkinAuthor)
    {
        string newSkinPath = Path.Combine(SkinsDirectory, newSkinName);
        Directory.CreateDirectory(newSkinPath);

        SkinDescriptor newSkin = new()
        {
            name = newSkinName,
            author = newSkinAuthor
        };
        newSkin.Save(newSkinPath);

        ScanSkinDirectory();

        SkinDescriptor? skin = skins.FirstOrDefault(s => s.name == newSkinName);
        if (skin == null)
        {
            Trace.TraceError("Failed to create new skin");
            return;
        }

        //a new skin starts empty, so it looks identical to System until the skinner generates a layout
        ChangeSkin(skin);
    }

    public void ChangeSkin(SkinDescriptor? skin)
    {
        currentSkin = skin == null ? null : SkinDescriptor.LoadSkin(skin.basePath);

        CDTXMania.ConfigIni.strSkinFolder = skin == null ? string.Empty : FolderNameOf(skin);

        //so a reload picks up edited component files, and re-seeds deleted ones
        ComponentInstance.ClearCache();

        CDTXMania.StageManager.rCurrentStage.LoadUI();
    }
}
