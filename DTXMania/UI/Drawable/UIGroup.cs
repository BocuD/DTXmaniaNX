using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using DTXMania.Core;
using DTXMania.UI.Animation;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Inspector;
using Hexa.NET.ImGui;
using Newtonsoft.Json;

namespace DTXMania.UI.Drawable;

public class UIGroup : UIDrawable
{
    [Themable] public bool sortByRenderOrder = true;
    public List<UIDrawable> children = [];

    private bool dirty = false;

    //drawables already reported as failing to draw, so one broken element does not fill the log
    [JsonIgnore] private static readonly ConditionalWeakTable<UIDrawable, object> reportedDrawFailures = [];

    //an animating property is written every frame for as long as it animates, so a per-write cost here
    //scales with the frame rate
    [JsonIgnore] private static readonly int probeAnimators =
        Core.Framework.AllocationProbe.Register("Animators (whole tree)");

    //clips are part of what a skin describes: a cursor that pulses is animation, not code. Replace rather
    //than populate, so a type that builds an animator in its constructor does not end up with the loaded
    //clips appended to the ones it made
    [SkinSerialize]
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public Animator? animator;

    //per-instance data source for this subtree, runtime-only; descendants resolve their binding keys
    //against it before falling back to ancestors and the global context. See UIDrawable.DataContexts
    [JsonIgnore] public IUIDataContext? dataContext;

    //what this component's keys resolved to when it was last saved from a running instance. Written on a
    //component file so it can be edited on its own with values that make sense; nothing at runtime reads it
    [SkinSerialize]
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public Dictionary<string, string>? sampleContext;

    //skin-relative, eg: Components/ChartRow.json. Empty until a stage is saved into a skin
    [Themable] public string component = string.Empty;

    //set only where code makes a group a component; one placed by a skin is named by its file instead
    private string explicitComponentName = string.Empty;

    //a file is written as Components/<name>.json, so the path names the component it came from
    [JsonIgnore] public string componentName => explicitComponentName.Length > 0
        ? explicitComponentName
        : Path.GetFileNameWithoutExtension(component);

    [JsonIgnore] public bool IsComponent => componentName.Length > 0;

    //builds this component until a skin has a file of its own
    [JsonIgnore] public Func<UIGroup>? componentSource;

    //re-deserializing per instance is how a copy is made: it gives every one a full OnDeserialize pass
    [JsonIgnore] private static readonly Dictionary<string, string> jsonCache = new();

    [JsonIgnore] private bool contentLoaded;

    public static void ClearComponentCache() => jsonCache.Clear();

    protected void MakeComponent(string name, Func<UIGroup> source)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A component needs a name of its own.", nameof(name));
        }

        explicitComponentName = name;
        componentSource = source;
    }

    public void MakeComponent(string name, string componentPath)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A component needs a name of its own.", nameof(name));
        }

        explicitComponentName = name;
        component = componentPath;
    }

    [AddChildMenu]
    public static UIDrawable Create()
    {
        return new UIGroup("New UIGroup");
    }

    public UIGroup()
        : this("New UIGroup")
    {
    }

    public UIGroup(string name)
    {
        this.name = name;
    }

    public T AddChild<T>(T element, bool setParent = true) where T : UIDrawable
    {
        children.Add(element);
        if (setParent)
        {
            element.SetParent(this, false);
        }

        animator?.InvalidateBindings();

        dirty = true;
        
        return element;
    }

    public T GetChild<T>(int i) where T : UIDrawable
    {
        return (T)children[i];
    }

    public T? GetChild<T>(string name) where T : UIDrawable
    {
        //names come from json, so a mismatched type is a layout authoring error, not a crash
        return children.FirstOrDefault(x => x.name == name) as T;
    }

    /// <summary>The first descendant of this type, in draw order. For a behaviour a layout only ever holds
    /// one of, which is what the type already says.</summary>
    public T? FindChild<T>() where T : UIDrawable
    {
        foreach (UIDrawable child in children)
        {
            if (child is T match)
            {
                return match;
            }

            if (child is UIGroup group && group.FindChild<T>() is { } nested)
            {
                return nested;
            }
        }

        return null;
    }

    public UIDrawable GetChild(int i)
    {
        return children[i];
    }

    /// <summary>Asks for the children to be sorted again, after something changed a child's renderOrder.</summary>
    public void InvalidateOrder()
    {
        dirty = true;
    }

    public void RemoveChild(UIDrawable element)
    {
        children.Remove(element);
        animator?.InvalidateBindings();
        
        dirty = true;
    }

    public void ClearChildren()
    {
        foreach (UIDrawable element in children)
        {
            element.Dispose();
        }

        children.Clear();
    }

    /// <summary>Loads this component's content as its children, once. Lazy, so a path set by
    /// deserialization is in place before it runs.</summary>
    public void EnsureContent()
    {
        if (contentLoaded || !IsComponent)
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

    protected virtual void OnContentLoaded()
    {
    }

    //null when the code default applies: the System skin, or a stage not saved into this one yet
    public string? ComponentPath()
    {
        Skin.SkinDescriptor? skin = CDTXMania.SkinManager.currentSkin;
        return skin == null || string.IsNullOrWhiteSpace(component)
            ? null
            : Path.Combine(skin.basePath, component);
    }

    private UIGroup ResolveComponentTree()
    {
        if (ComponentPath() is not { } fullPath || !File.Exists(fullPath))
        {
            return componentSource?.Invoke() ?? new UIGroup(componentName);
        }

        if (!jsonCache.TryGetValue(fullPath, out string? json))
        {
            try
            {
                json = File.ReadAllText(fullPath);
            }
            catch (Exception e)
            {
                Trace.TraceError($"Failed to load component at {fullPath}: {e.Message}");
                return componentSource?.Invoke() ?? new UIGroup(componentName);
            }

            jsonCache[fullPath] = json;
        }

        return Skin.SkinHierarchySerializer.DeserializeFromJson(json)
               ?? componentSource?.Invoke()
               ?? new UIGroup(componentName);
    }

    /// <summary>Gives the skin its own file for this component and points this group at it. Keeps a path
    /// the skin already set.</summary>
    public void WriteIntoSkin()
    {
        if (!IsComponent || CDTXMania.SkinManager.currentSkin == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(component))
        {
            component = $"Components/{componentName}.json";
        }

        EnsureContent();
        SaveComponent();
    }

    /// <summary>No-op on the System skin, or before the component has a file.</summary>
    public void SaveComponent()
    {
        if (ComponentPath() is not { } fullPath)
        {
            Trace.TraceWarning("Save component ignored: System skin or no component set.");
            return;
        }

        //wrap the live children in a throwaway root to serialize them; they are referenced, not
        //reparented, so the live group is untouched
        UIGroup root = new(componentName);
        root.children.AddRange(children);
        root.sampleContext = CaptureSampleContext();
        root.animator = animator;
        string json = Skin.SkinHierarchySerializer.SerializeToJsonCompact(root);
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

    //what the keys resolve to right now, so the component can be edited on its own with sensible values.
    //Keeps the previous sample when nothing resolves
    private Dictionary<string, string>? CaptureSampleContext()
    {
        Dictionary<string, string> captured = new();
        DynamicElements.ComponentKeys.Capture(this, captured);

        return captured.Count > 0 ? captured : sampleContext;
    }

    /// <summary>Picks up external edits to the file, or discards unsaved ones.</summary>
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

    public override void Draw(Matrix4x4 parentMatrix)
    {
        EnsureContent();

        if (!isVisible)
        {
            return;
        }

        //bindings first, then the animator, so an animation targeting a bound member wins for this frame
        for (int index = 0; index < children.Count; index++)
        {
            children[index].ApplyBindings();
        }

        if (animator != null)
        {
            //TickAuto does not descend into child groups, so these never nest
            Core.Framework.AllocationProbe.Begin(probeAnimators);
            animator.TickAuto(this);
            Core.Framework.AllocationProbe.End(probeAnimators);
        }

        UpdateLocalTransformMatrix();
        Matrix4x4 combinedMatrix = localTransformMatrix * parentMatrix;

        if (sortByRenderOrder && dirty)
        {
            children.Sort((a, b) => a.renderOrder.CompareTo(b.renderOrder));
            dirty = false;
        }

        for (int index = 0; index < children.Count; index++)
        {
            UIDrawable element = children[index];
            if (!element.isVisible)
            {
                continue;
            }

            try
            {
                element.Draw(combinedMatrix);
            }
            catch (Exception e)
            {
                //a drawable that throws once throws every frame, so it is reported the first time and
                //then left alone: the log stays readable and the rest of the tree still draws
                if (reportedDrawFailures.TryAdd(element, null!))
                {
                    Trace.TraceError($"Error drawing {element.name}: {e} Stacktrace: {e.StackTrace ?? "No stack trace"}");
                }
            }
        }
    }

    public override void OnDeserialize()
    {
        base.OnDeserialize();

        foreach (UIDrawable? child in children)
        {
            child?.SetParent(this, false);
        }

        children.RemoveAll(x => x == null);
    }

    public override void Dispose()
    {
        base.Dispose();

        foreach (UIDrawable element in children)
        {
            element.Dispose();
        }

        children.Clear();
    }

    public override void DrawInspector()
    {
        base.DrawInspector();
        ImGui.Checkbox("Sort by Render Order", ref sortByRenderOrder);

        if (ImGui.CollapsingHeader("Animator"))
        {
            if (animator == null)
            {
                if (ImGui.Button("Add Animator"))
                {
                    animator = new Animator();
                }
            }
            else
            {
                animator.DrawInspector(this);

                ImGui.Separator();
                if (ImGui.Button("Remove Animator"))
                {
                    animator = null;
                }
            }
        }
    }
}
