using Newtonsoft.Json;

namespace DTXMania.UI.Skin;

public class SkinDescriptor
{
    public required string name { get; set; }
    public required string author { get; set; }

    public Dictionary<CStage.EStage, string> stageSkins { get; set; } = new();

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
        
        return descriptor;
    }
}