using System.Text.Json;
using DTXMania.Core;
using DTXUIRenderer;

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
                SkinDescriptor? skin = LoadSkin(directory);

                if (skin != null)
                {
                    skins.Add(skin);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to load skin from {directory}: {e.Message}");
            }
        }
    }

    private SkinDescriptor? LoadSkin(string path)
    {
        Console.WriteLine($"Loading skin {path}");
        
        string json = File.ReadAllText(Path.Combine(path, "skin.json"));

        return JsonSerializer.Deserialize<SkinDescriptor>(json);
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
            
            string stageJson = JsonSerializer.Serialize(stageGroup);
            string path = Path.Combine(newSkinPath, $"{stage}.json");
            File.WriteAllText(Path.Combine(newSkinPath, path), stageJson);
            
            newSkin.stageSkins[stage] = path;
        }
        
        string json = JsonSerializer.Serialize(newSkin);
        File.WriteAllText(Path.Combine(newSkinPath, "skin.json"), json);
        
        //reload skins
        ScanSkinDirectory();
        
        SkinDescriptor? skin = skins.FirstOrDefault(s => s.name == newSkinName);

        if (skin == null)
        {
            Console.WriteLine("Failed to create new skin");
            return;
        }
        
        //select new skin
        ChangeSkin(skin);
    }

    public void ChangeSkin(SkinDescriptor skin)
    {
        currentSkin = skin;
    }
}