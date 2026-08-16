using System.Diagnostics;
using System.IO;
using DTXMania.Core;
using DTXMania.UI.Skin;
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
    public static string DefaultDirectory => Environment.CurrentDirectory;

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
    /// Loads the clip a reference points at. For clips an animator holds; a gameplay element never
    /// appears in a layout, so it loads its file through <see cref="LoadFromFile"/> and owns the clip.
    /// </summary>
    public static AnimationClip? Load(SkinResource resource)
    {
        string path = resource.Resolve(ResourceType.Animation);

        if (string.IsNullOrEmpty(path))
        {
            Trace.TraceError($"AnimationClipIO.Load: nowhere to load {resource} from");
            return null;
        }

        return LoadFromFile(path);
    }

    /// <summary>
    /// Copies a clip into the active skin so the skin owns it, and points the entry at that copy. This is
    /// what a stage save does with the System clips it references: a saved skin carries its own animations
    /// rather than depending on where it was saved from.
    /// </summary>
    public static bool MoveIntoSkin(AnimatorClip entry, string fileName)
    {
        if (CDTXMania.SkinManager.currentSkin is not { } skin)
        {
            return false;
        }

        entry.resource = new SkinResource(ResourceSource.Skin, fileName);

        return SaveToFile(entry.clip, Path.Combine(skin.basePath,
            SkinDescriptor.GetResourceFolder(ResourceType.Animation), fileName));
    }

    /// <summary>Writes a clip back to the file it came from.</summary>
    public static bool SaveToResource(AnimatorClip entry)
    {
        string path = entry.resource.Resolve(ResourceType.Animation);

        if (string.IsNullOrEmpty(path))
        {
            Trace.TraceError($"AnimationClipIO.SaveToResource: clip '{entry.clip.name}' has no file to write to");
            return false;
        }

        return SaveToFile(entry.clip, path);
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