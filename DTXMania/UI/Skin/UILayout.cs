using System.Diagnostics;
using DTXMania.UI.Drawable;

namespace DTXMania.UI.Skin;

/// <summary>
/// File IO for a stage's UI layout, stored as compact json (only non-default properties). Takes
/// fully-resolved paths; path resolution and skin selection live in <see cref="SkinManager"/>.
/// </summary>
public static class UILayout
{
    public static UIGroup? Load(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return SkinHierarchySerializer.DeserializeFromJson(File.ReadAllText(path));
        }
        catch (Exception e)
        {
            Trace.TraceError($"Failed to load layout '{path}': {e.Message}");
            return null;
        }
    }

    public static void Save(string path, UIGroup group)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, SkinHierarchySerializer.SerializeToJsonCompact(group));
        }
        catch (Exception e)
        {
            Trace.TraceError($"Failed to save layout '{path}': {e.Message}");
        }
    }
}
