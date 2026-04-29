namespace DTXMania.UI.Skin;

public enum ResourceType
{
    Image,
    Font
}

public partial class SkinDescriptor
{
    public string AddResource(ResourceType type, string path)
    {
        //copy the file into the skin directory
        string fileName = Path.GetFileName(path);
        string targetPath = Path.Combine(basePath, GetResourceFolder(type), fileName);
        
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
    
    public string GetResource(ResourceType type, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return "";
        
        string targetPath = Path.Combine(basePath, GetResourceFolder(type), fileName);
        
        if (File.Exists(targetPath))
        {
            return targetPath;
        }
        
        //if the file does not exist, return empty string
        return "";
    }

    public static string GetResourceFolder(ResourceType type)
    {
        string folder = "Resources";
        
        switch (type)
        {
            case ResourceType.Image:
                folder = "Resources";
                break;
            case  ResourceType.Font:
                folder = "Fonts";
                break;
        }

        return folder;
    }
}