using System.Diagnostics;
using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Skin;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Drawable;

/// <summary>
/// A drawable representing one instance of a reusable component. It serializes as a lightweight element
/// (its own transform/visibility/name plus a skin-relative <see cref="component"/> path) — its children
/// are NOT part of the layout, they come from the component file at load. On the System skin, or with no
/// path set, the code default is used instead.
///
/// Behaviour-backed components (a song row, a difficulty pane) subclass this: they supply the code default
/// via <see cref="BuildDefault"/>, grab references to the loaded children in <see cref="OnContentLoaded"/>,
/// and add their own runtime behaviour.
/// </summary>
public abstract class ComponentInstance : UIGroup
{
    //skin-relative path to the component json, e.g. "Components/ChartRow.json"
    [Themable] public string component = string.Empty;

    //component json cached per resolved full path and re-deserialized per instance. Re-deserializing is
    //the codebase's clone mechanism: fresh ids plus a full OnDeserialize pass per copy
    private static readonly Dictionary<string, string> jsonCache = new();

    private bool contentLoaded;

    protected ComponentInstance()
    {
    }

    protected ComponentInstance(string name) : base(name)
    {
    }

    public static void ClearCache() => jsonCache.Clear();

    //the code-built default: the fallback on the System skin, and the seed for a missing component file
    protected abstract UIGroup BuildDefault();

    //runs once, right after the component content is loaded as children
    protected virtual void OnContentLoaded()
    {
    }

    /// <summary>
    /// Loads the component content as this instance's children exactly once. Lazy, so it runs after
    /// <see cref="component"/> is known (ctor default, or deserialization). Subclasses call this before
    /// touching their children.
    /// </summary>
    public void EnsureContent()
    {
        if (contentLoaded)
        {
            return;
        }

        contentLoaded = true;

        UIGroup tree = ResolveComponentTree();
        sampleContext = tree.sampleContext;

        //a component's animation belongs to the component, not to whoever placed an instance of it
        animator = tree.animator ?? animator;

        foreach (UIDrawable child in tree.children.ToArray())
        {
            AddChild(child);
        }

        OnContentLoaded();
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        EnsureContent();
        base.Draw(parentMatrix);
    }

    /// <summary>Serializes this instance's current children back into its component file so inspector
    /// edits to the component persist. No-op on the System skin / when no component path is set.</summary>
    public void SaveComponent()
    {
        if (ComponentPath() is not { } fullPath)
        {
            Trace.TraceWarning("Save component ignored: System skin or no component path.");
            return;
        }

        //wrap the live children in a throwaway root to serialize them; they are referenced, not
        //reparented, so the live instance is untouched
        UIGroup root = new(Path.GetFileNameWithoutExtension(component));
        root.children.AddRange(children);
        root.sampleContext = CaptureSampleContext();
        root.animator = animator;
        string json = SkinHierarchySerializer.SerializeToJsonCompact(root);
        root.children.Clear();

        sampleContext = root.sampleContext;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, json);
            jsonCache[fullPath] = json;
            Trace.TraceInformation($"Saved component to {fullPath}.");
        }
        catch (Exception e)
        {
            Trace.TraceError($"Failed to save component to {fullPath}: {e.Message}");
        }
    }

    //what this instance's keys resolve to right now, so the component can later be edited on its own with
    //values that make sense. Keeps the previous sample when nothing resolves, which is the case for an
    //instance whose data has not arrived yet
    private Dictionary<string, string>? CaptureSampleContext()
    {
        Dictionary<string, string> captured = new();
        ComponentKeys.Capture(this, captured);

        return captured.Count > 0 ? captured : sampleContext;
    }

    /// <summary>Drops this instance's loaded content and the cached json, then reloads from the component
    /// file — picking up external edits, or discarding unsaved ones for this instance.</summary>
    public void ReloadComponent()
    {
        if (ComponentPath() is { } fullPath)
        {
            jsonCache.Remove(fullPath);
        }

        ClearChildren();
        contentLoaded = false;
        EnsureContent();
    }

    public override void DrawInspector()
    {
        //load the content up front so inspecting an instance that hasn't drawn yet (a hidden pane, or
        //right after a skin reload) still shows its real children and data context
        EnsureContent();

        base.DrawInspector();

        if (!ImGui.CollapsingHeader("Component Instance"))
        {
            return;
        }

        bool fromFile = ComponentPath() != null;
        ImGui.LabelText("Component", fromFile ? component : "(code default)");
        ImGui.LabelText("Loaded children", children.Count.ToString());

        //repointing a behaviour-backed instance at an unrelated component won't match the driving code,
        //so this is mainly for generic hand-placed ones
        if (CDTXMania.SkinManager.currentSkin is { } skin)
        {
            string[] paths = SkinManager.ComponentPaths(skin);
            if (paths.Length > 0)
            {
                int current = Array.IndexOf(paths, component);
                if (ImGui.Combo("Set Component", ref current, paths, paths.Length) && current >= 0)
                {
                    component = paths[current];
                    ReloadComponent();
                }
            }
        }

        ImGui.BeginDisabled(!fromFile);
        if (ImGui.Button("Save Component"))
        {
            SaveComponent();
        }

        ImGui.SameLine();
        if (ImGui.Button("Reload Component"))
        {
            ReloadComponent();
        }

        ImGui.EndDisabled();
    }

    //full path of this instance's component file, or null when the code default applies (System skin,
    //or no path set)
    private string? ComponentPath()
    {
        SkinDescriptor? skin = CDTXMania.SkinManager.currentSkin;
        return skin == null || string.IsNullOrWhiteSpace(component) ? null : Path.Combine(skin.basePath, component);
    }

    private UIGroup ResolveComponentTree()
    {
        if (ComponentPath() is not { } fullPath)
        {
            return BuildDefault();
        }

        if (!jsonCache.TryGetValue(fullPath, out string? json))
        {
            json = LoadOrSeed(fullPath);
            jsonCache[fullPath] = json;
        }

        return SkinHierarchySerializer.DeserializeFromJson(json) ?? BuildDefault();
    }

    //reads the component file, or seeds it once from the code default so it exists and is editable
    private string LoadOrSeed(string fullPath)
    {
        try
        {
            if (File.Exists(fullPath))
            {
                return File.ReadAllText(fullPath);
            }
        }
        catch (Exception e)
        {
            Trace.TraceError($"Failed to load component at {fullPath}: {e.Message}");
        }

        UIGroup def = BuildDefault();
        string json = SkinHierarchySerializer.SerializeToJsonCompact(def);
        def.Dispose(); //only needed to author the file; instances come from re-deserializing the json

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, json);
            Trace.TraceInformation($"Seeded component from code at {fullPath}.");
        }
        catch (Exception e)
        {
            Trace.TraceError($"Failed to seed component at {fullPath}: {e.Message}");
        }

        return json;
    }
}
