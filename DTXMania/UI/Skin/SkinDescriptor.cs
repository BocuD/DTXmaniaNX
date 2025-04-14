using DTXMania.Core;
using DTXMania.UI.Drawable;
using DTXMania.UI.Drawable.Serialization;
using Hexa.NET.ImGui;
using Newtonsoft.Json;

namespace DTXMania.UI.Skin;

public class SkinDescriptor
{
    public required string name { get; set; }
    public required string author { get; set; }
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
            if (CDTXMania.rCurrentStage.eStageID != stageSkin.Key) continue;
            
            if (string.IsNullOrWhiteSpace(stageSkin.Value)) continue;

            var group = CDTXMania.rCurrentStage.ui;
            
            if (group == null) continue;
            
            //create a copy of the group to avoid modifying the original
            var groupCopy = new UIGroup(group.name);
            groupCopy.children = new List<UIDrawable>(group.children);
            
            //remove any base elements
            groupCopy.children.RemoveAll(x => x.dontSerialize);

            try
            {
                var json = JsonConvert.SerializeObject(groupCopy, Formatting.Indented);
                string stagePath = Path.Combine(basePath, stageSkin.Value);
                File.WriteAllText(stagePath, json);
            }
            catch (Exception e)
            {
                string stackTrace = e.StackTrace ?? "No stack trace";
                Console.WriteLine($"Failed to save stage skin: {e} Stacktrace: {stackTrace}");
                return;
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
            UIGroup? loadedGroup = JsonConvert.DeserializeObject<UIGroup>(json, new UIDrawableConverter());
            if (loadedGroup != null)
            {
                //register elements
                void Register(UIDrawable? drawable)
                {
                    if (drawable == null) return;
                    
                    DrawableTracker.Register(drawable);

                    if (drawable is UIGroup group)
                    {
                        var nullChildren = new List<UIDrawable>();
                        foreach (var d in group.children)
                        {
                            if (d == null)
                            {
                                nullChildren.Add(d);
                                continue;
                            }
                            Register(d);
                            d.SetParent(group, false);
                        }
                        foreach (var d in nullChildren)
                        {
                            group.children.Remove(d);
                        }
                    }
                }
                
                Register(loadedGroup);
                
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