using System.Diagnostics;
using DTXMania.Core;
using DTXMania.UI.Drawable;

namespace DTXMania.UI.Skin;

public class SkinManager
{
    private static string skinDirectory => Path.Combine(CDTXMania.executableDirectory, "Skins");
    public List<SkinDescriptor> skins { get; } = [];
    public SkinDescriptor? currentSkin { get; private set; }
    
    public SkinManager()
    {
        ScanSkinDirectory();
    }
    
    public void ScanSkinDirectory()
    {
        skins.Clear();

        if (!Directory.Exists(skinDirectory))
        {
            Directory.CreateDirectory(skinDirectory);
        }
        
        foreach (string directory in Directory.GetDirectories(skinDirectory))
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

    public void CreateNewSkin(string newSkinName, string newSkinAuthor)
    {
        //create new skin directory
        string newSkinPath = Path.Combine(skinDirectory, newSkinName);
        Directory.CreateDirectory(newSkinPath);
        
        //create skin descriptor
        SkinDescriptor newSkin = new()
        {
            name = newSkinName,
            author = newSkinAuthor
        };
        
        foreach (CStage.EStage stage in Enum.GetValues<CStage.EStage>())
        {
            if (stage == CStage.EStage.DoNothing_0) continue;

            UIGroup stageGroup = new(stage.ToString());

            string path = Path.Combine(newSkinPath, $"{stage}.json");
            File.WriteAllText(path, SkinHierarchySerializer.SerializeToJson(stageGroup));

            newSkin.stageSkins[stage] = $"{stage}.json";
        }

        newSkin.Save(newSkinPath);
        
        //reload skins
        ScanSkinDirectory();
        
        SkinDescriptor? skin = skins.FirstOrDefault(s => s.name == newSkinName);

        if (skin == null)
        {
            Trace.TraceError("Failed to create new skin");
            return;
        }
        
        //select new skin
        ChangeSkin(skin);
    }

    public void ChangeSkin(SkinDescriptor? skin)
    {
        currentSkin = skin == null ? null : SkinDescriptor.LoadSkin(skin.basePath);

        CDTXMania.StageManager.rCurrentStage.LoadUI();
    }

    public UIGroup? LoadStageSkin(CStage.EStage stageId)
    {
        return currentSkin?.LoadStageSkin(stageId);
    }

    internal void ApplySkin(UIGroup target, CStage.EStage eStageID)
    {
        UIGroup? loadedSkin = CDTXMania.SkinManager.LoadStageSkin(eStageID);

        // loadedSkin is a UIGroup that was deserialized from the skin file; apply it to the target hierarchy.
        SkinHierarchyMerger.ApplySkin(target, loadedSkin);
    }
}