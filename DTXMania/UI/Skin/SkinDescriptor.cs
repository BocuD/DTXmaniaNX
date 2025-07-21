using DTXMania.Core;
using DTXMania.UI.Drawable;
using DTXMania.UI.Drawable.Serialization;
using Hexa.NET.ImGui;
using Newtonsoft.Json;

namespace DTXMania.UI.Skin;

public class SkinDescriptor
{
    public string name { get; set; } = "New skin";
    public string author { get; set; } = "Unknown Author";
        
    [JsonIgnore] public string basePath { get; private set; } = "";

    public Dictionary<CStage.EStage, string> stageSkins { get; set; } = new();
    
    //Load a skin.json file from disk
    public static SkinDescriptor? LoadSkin(string path)
    {
        Console.WriteLine($"Loading skin {path}");
        
        string json = File.ReadAllText(Path.Combine(path, "skin.json"));

        SkinDescriptor? descriptor = JsonConvert.DeserializeObject<SkinDescriptor>(json, new JsonSerializerSettings
        {
            Error = (sender, args) =>
            {
                Console.WriteLine(args.ErrorContext.Error.Message);
                args.ErrorContext.Handled = true;
            }
        });
        
        if (descriptor == null) return null;
        
        descriptor.basePath = path;

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
        
        Console.WriteLine($"Saving skin to {targetPath}");
        
        var json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(Path.Combine(targetPath, "skin.json"), json);
    }

    public void SaveCurrentStageChanges()
    {
        //write current stage skin
        foreach (KeyValuePair<CStage.EStage, string> stageSkin in stageSkins)
        {
            if (CDTXMania.StageManager.rCurrentStage.eStageID != stageSkin.Key) continue;
            
            if (string.IsNullOrWhiteSpace(stageSkin.Value)) continue;

            UIGroup? stageGroup = CDTXMania.StageManager.rCurrentStage.ui;
            
            if (stageGroup == null) continue;

            string json = stageGroup.SerializeToJSON();

            if (!string.IsNullOrWhiteSpace(json))
            {
                string stagePath = Path.Combine(basePath, stageSkin.Value);
                File.WriteAllText(stagePath, json);
            }
            else
            {
                Console.WriteLine($"Failed to save stage skin for stage {stageSkin.Key}, Serialization Failed!");
            }
        }
    }

    public UIGroup? LoadStageSkin(CStage.EStage stageId)
    {
        bool available = stageSkins.TryGetValue(stageId, out string? uiGroupJson);
        
        if (available && uiGroupJson != null)
        {
            string path = Path.Combine(basePath, uiGroupJson);
            if (!File.Exists(path))
            {
                Console.WriteLine($"Stage skin file at {path} does not exist, loading default");
                return null;
            }
            
            string json = File.ReadAllText(path);
            UIGroup? loadedGroup = UIGroup.DeserializeFromJSON(json);
            if (loadedGroup != null)
            {
                return loadedGroup;
            }
        }
        
        return null;
    }

    public void DrawInspector()
    {
        foreach (KeyValuePair<CStage.EStage,string> stage in stageSkins)
        {
            ImGui.Text("Stage " + stage.Key);
            ImGui.SameLine();
            ImGui.Text(stage.Value);
        }
    }

    public string AddResource(string path)
    {
        //copy the file into the skin directory
        string fileName = Path.GetFileName(path);
        string targetPath = Path.Combine(basePath, "Resources", fileName);
        
        if (File.Exists(targetPath))
        {
            return targetPath;
        }
        
        if (!Directory.Exists(Path.GetDirectoryName(targetPath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        }
        
        File.Copy(path, targetPath, true);
        return fileName;
    }

    public string GetResource(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return "";
        
        string targetPath = Path.Combine(basePath, "Resources", fileName);
        if (File.Exists(targetPath))
        {
            return targetPath;
        }
        
        //if the file does not exist, return empty string
        return "";
    }
}