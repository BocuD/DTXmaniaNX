using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace DTXMania.UI.Animation;

/// <summary>
/// Save/load helpers for AnimationClip. Kept separate from the runtime data class so the
/// runtime stays free of file IO concerns.
/// </summary>
public static class AnimationClipIO
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore
    };

    /// <summary>
    /// Default directory for clip files, relative to the working directory. Used by the
    /// editor UI as the starting directory in file dialogs.
    /// </summary>
    public static string DefaultDirectory => Path.Combine(Environment.CurrentDirectory, "Animations");

    /// <summary>
    /// Serialize a clip to JSON and write it to <paramref name="path"/>. Returns true on
    /// success. Creates the parent directory if it doesn't exist.
    /// </summary>
    public static bool SaveToFile(AnimationClip clip, string path)
    {
        try
        {
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            string json = JsonConvert.SerializeObject(clip, Settings);
            File.WriteAllText(path, json);
            return true;
        }
        catch (Exception e)
        {
            Trace.TraceError($"AnimationClipIO.SaveToFile('{path}'): {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Read JSON from <paramref name="path"/> and deserialize into an AnimationClip. Returns
    /// null on failure. The returned clip has all track bindings invalidated so the next
    /// evaluation re-resolves drawables against the current tree.
    /// </summary>
    public static AnimationClip? LoadFromFile(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                Trace.TraceWarning($"AnimationClipIO.LoadFromFile: file not found '{path}'");
                return null;
            }
            string json = File.ReadAllText(path);
            AnimationClip? clip = JsonConvert.DeserializeObject<AnimationClip>(json, Settings);
            clip?.InvalidateBindings();
            return clip;
        }
        catch (Exception e)
        {
            Trace.TraceError($"AnimationClipIO.LoadFromFile('{path}'): {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Compute defaults for a Save dialog: starting directory and suggested filename. If
    /// <paramref name="lastPath"/> is provided, the dialog re-opens at that location. Otherwise
    /// we suggest <see cref="DefaultDirectory"/> and a filename derived from the clip's name.
    /// </summary>
    public static (string directory, string filename) GetSaveDialogDefaults(AnimationClip clip, string? lastPath)
    {
        if (!string.IsNullOrEmpty(lastPath))
        {
            string dir = Path.GetDirectoryName(lastPath) ?? DefaultDirectory;
            string name = Path.GetFileName(lastPath);
            return (dir, name);
        }

        string baseName = string.IsNullOrWhiteSpace(clip.name) ? "Untitled" : clip.name;
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            baseName = baseName.Replace(c, '_');
        }
        return (DefaultDirectory, baseName + ".json");
    }
}