using DTXUIRenderer;
using Newtonsoft.Json;

namespace DTXMania.UI.Skin;

public class SkinDescriptor
{
    public required string name { get; set; }
    public required string author { get; set; }
    private string basePath;

    public Dictionary<CStage.EStage, string> stageSkins { get; set; } = new();
    [JsonIgnore] private Dictionary<CStage.EStage, UIGroup?> stageSkinCache = new();

    public void Save(string path)
    {
        Console.WriteLine($"Saving skin {path}");
        
        var json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(Path.Combine(path, "skin.json"), json);
    }
    
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
}