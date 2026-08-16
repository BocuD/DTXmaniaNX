using System.Diagnostics;
using DTXMania.Core;
using DTXMania.UI.Drawable;
using Newtonsoft.Json;

namespace DTXMania.UI.Skin;

public partial class SkinDescriptor
{
    //bump when the on-disk format changes so old skins can be detected and later migrated
    public const int CurrentFormatVersion = 1;

    public string name { get; set; } = "New skin";
    public string author { get; set; } = "Unknown Author";
    public int formatVersion { get; set; } = CurrentFormatVersion;
    public string description { get; set; } = "";

    //game version this skin was authored against, informational only
    public string gameVersion { get; set; } = "";

    [JsonIgnore] public string basePath { get; private set; } = "";

    //layouts and components live at fixed paths inside the skin, so no manifest is needed
    [JsonIgnore] public string layoutFolder => Path.Combine(basePath, "Layout");
    [JsonIgnore] public string componentFolder => Path.Combine(basePath, "Components");

    public string LayoutPath(CStage.EStage stageId) => Path.Combine(layoutFolder, $"{stageId}.json");

    //Load a skin.json file from disk
    public static SkinDescriptor? LoadSkin(string path)
    {
        Trace.TraceInformation($"Loading skin {path}");
        
        string json = File.ReadAllText(Path.Combine(path, "skin.json"));

        SkinDescriptor? descriptor = JsonConvert.DeserializeObject<SkinDescriptor>(json, new JsonSerializerSettings
        {
            Error = (sender, args) =>
            {
                Trace.TraceError(args.ErrorContext.Error.Message);
                args.ErrorContext.Handled = true;
            }
        });
        
        if (descriptor == null) return null;

        descriptor.basePath = path;

        if (descriptor.formatVersion != CurrentFormatVersion)
        {
            Trace.TraceWarning($"Skin '{descriptor.name}' is format version {descriptor.formatVersion}, " +
                               $"but the current version is {CurrentFormatVersion}. It may not load correctly.");
        }

        return descriptor;
    }
    
    //Write the skin to disk. Providing basePathOverride will save skin.json and all stage files to that folder
    public void Save(string basePathOverride = "")
    {
        string targetPath = basePath;
        if (!string.IsNullOrWhiteSpace(basePathOverride))
        {
            targetPath = basePathOverride;
        }
        
        Trace.TraceInformation($"Saving skin to {targetPath}");
        
        var json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(Path.Combine(targetPath, "skin.json"), json);
    }

    //persists inspector edits: writes the current stage's live tree into this skin's layout json
    public void SaveCurrentStageChanges()
    {
        CStage stage = CDTXMania.StageManager.rCurrentStage;
        if (stage?.ui != null)
        {
            UILayout.Save(LayoutPath(stage.eStageID), stage.ui);
        }
    }
}