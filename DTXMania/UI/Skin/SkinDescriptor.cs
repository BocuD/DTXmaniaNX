using DTXMania.UI.Drawable;
using DTXMania.UI.Drawable.Serialization;
using DTXUIRenderer;
using Hexa.NET.ImGui;
using Newtonsoft.Json;

namespace DTXMania.UI.Skin;

public class SkinDescriptor
{
    public required string name { get; set; }
    public required string author { get; set; }
    [JsonIgnore] private string basePath = "";

    public Dictionary<CStage.EStage, string> stageSkins { get; set; } = new();
    [JsonIgnore] private Dictionary<CStage.EStage, UIGroup?> stageSkinCache = new();
    
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
        
        foreach (CStage.EStage stage in Enum.GetValues<CStage.EStage>())
        {
            if (stage == CStage.EStage.DoNothing_0) continue;

            descriptor.stageSkinCache[stage] = descriptor.LoadStageSkin(stage);
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
        
        Console.WriteLine($"Saving skin to {targetPath}");
        
        var json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(Path.Combine(targetPath, "skin.json"), json);
        
        //write all stage skins
        foreach (KeyValuePair<CStage.EStage, string> stageSkin in stageSkins)
        {
            if (string.IsNullOrWhiteSpace(stageSkin.Value)) continue;
            
            string stagePath = Path.Combine(targetPath, stageSkin.Value);
            stageSkinCache.TryGetValue(stageSkin.Key, out UIGroup? group);

            if (group == null) continue;
            
            json = JsonConvert.SerializeObject(group, Formatting.Indented);
            File.WriteAllText(stagePath, json);
        }
    }

    public UIGroup? LoadStageSkin(CStage.EStage stageId)
    {
        bool available = stageSkins.TryGetValue(stageId, out string? uiGroupJson);
        
        if (available && uiGroupJson != null)
        {
            string path = Path.Combine(basePath, uiGroupJson);
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<UIGroup>(json);
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
}