using System.Diagnostics;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.OpenGL;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.OpenGL;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Inspector;

/// <summary>
/// Edits one component on its own: it is instantiated away from any stage, drawn into its own render
/// target, and shown in a window with its own viewport. Its data context is either made up here or
/// borrowed from a live instance, so a component that only exists inside a song row can still be worked on
/// without the stage that owns it.
///
/// The edited tree is this editor's own copy: saving writes the component file, which is what live
/// instances reload from.
/// </summary>
public sealed class ComponentEditor : IDisposable
{
    public static List<ComponentEditor> open { get; } = [];

    public string componentPath { get; }
    public UIGroup root { get; } = new("ComponentEditor");

    private readonly ComponentInstance instance;
    private readonly GameRenderTarget target = new();
    private readonly Viewport viewport = new();
    private readonly UIDataContext dummy = new();
    private readonly BorrowedContext borrowed;

    //what the component reads, recollected from the tree as it is edited; the values are kept separately
    //so an edit survives a key disappearing and coming back
    private readonly Dictionary<string, DataBindingKind> keys = new();
    private readonly Dictionary<string, string> values = new();

    private Vector2 canvasSize = new(512, 512);

    //where the component's origin sits on the canvas, and the box its content occupies around that origin
    private Vector2 canvasOrigin = new(256, 256);
    private Vector2 contentMin;
    private Vector2 contentMax;

    //null follows the game window, so a component is shown at whatever the game is drawing at
    private float? renderScale;
    private float drawnScale;

    private bool autoFit = true;
    private int attachedContextGeneration = -1;
    private bool centered;
    private bool isOpen = true;

    //empty when the dummy context is in use, otherwise the drawable whose context is borrowed
    private string liveInstanceId = string.Empty;

    private string? drawError;

    private static BaseTexture? placeholderTexture;
    private static int nextSerial;
    private readonly int serial = nextSerial++;

    private ComponentEditor(string componentPath, Type? behaviour)
    {
        this.componentPath = componentPath;
        borrowed = new BorrowedContext(() => DrawableTracker.GetDrawable(liveInstanceId));
        instance = CreateInstance(behaviour);
        root.AddChild(instance);
    }

    /// <summary>Opens (or focuses) an editor for a component file. The behaviour class is what makes a list
    /// inside the component fill itself, so it is instantiated when known.</summary>
    public static void Open(string componentPath, Type? behaviour = null)
    {
        if (open.FirstOrDefault(e => e.componentPath == componentPath) is { } existing)
        {
            ImGui.SetWindowFocus(existing.WindowTitle());
            return;
        }

        open.Add(new ComponentEditor(componentPath, behaviour ?? LiveInstances(componentPath).FirstOrDefault()?.GetType()));
    }

    public static void DrawAll(UIDrawable? selected)
    {
        for (int i = open.Count - 1; i >= 0; i--)
        {
            if (!open[i].Draw(selected))
            {
                open[i].Dispose();
                open.RemoveAt(i);
            }
        }
    }

    /// <summary>The editor whose tree contains this drawable, if any. Selection is global, so this is what
    /// decides which viewport a gizmo is drawn in.</summary>
    public static ComponentEditor? OwnerOf(UIDrawable? drawable)
    {
        while (drawable != null)
        {
            if (open.FirstOrDefault(e => e.root == drawable) is { } editor)
            {
                return editor;
            }

            drawable = drawable.parent;
        }

        return null;
    }

    public void Dispose()
    {
        root.Dispose();
        target.Dispose();
    }

    private string WindowTitle() => $"{Path.GetFileNameWithoutExtension(componentPath)}##component{serial}";

    //returns false once the window has been closed
    private bool Draw(UIDrawable? selected)
    {
        ImGui.SetNextWindowSize(new Vector2(720, 640), ImGuiCond.FirstUseEver);

        //nothing is rendered while the window is collapsed, so an editor left open in the background is free
        if (!ImGui.Begin(WindowTitle(), ref isOpen, ImGuiWindowFlags.NoFocusOnAppearing))
        {
            ImGui.End();
            return isOpen;
        }

        DrawToolbar();
        DrawContextPanel();

        if (drawError != null)
        {
            ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), drawError);
        }

        Render();
        viewport.Draw($"ComponentViewport{serial}", target.TextureId, new Vector2(target.Width, target.Height),
            ImGui.GetContentRegionAvail());

        //centred once the content has a size; before that there is nothing to centre on
        if (!centered && contentMax != contentMin)
        {
            centered = true;
            viewport.CenterOn(viewport.desiredRenderSize);
        }

        //the gizmo belongs to whichever viewport rendered the drawable
        if (selected != null && OwnerOf(selected) == this)
        {
            InspectorManager.SetGizmoTarget(viewport.rect, viewport.drawList, viewport.GetViewMatrix());
            selected.DrawTransformGizmo();
        }

        ImGui.End();

        return isOpen;
    }

    private void DrawToolbar()
    {
        if (ImGui.Button("Save"))
        {
            instance.SaveComponent();
        }

        ImGui.SameLine();
        if (ImGui.Button("Reload"))
        {
            instance.ReloadComponent();
            drawError = null;
        }

        //live instances keep the copy they loaded, so the stage is rebuilt to show a saved change
        ImGui.SameLine();
        if (ImGui.Button("Reload Stage"))
        {
            CDTXMania.StageManager.rCurrentStage.LoadUI(true);
        }

        ImGui.SameLine();
        ImGui.Checkbox("Fit", ref autoFit);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(160);
        ImGui.BeginDisabled(autoFit);
        ImGui.InputFloat2("Canvas", ref canvasSize);
        ImGui.EndDisabled();

        ImGui.SameLine();
        if (ImGui.Button("Center"))
        {
            viewport.CenterOn(viewport.desiredRenderSize);
        }

        ImGui.SameLine();
        RenderScalePicker.Draw("Scale", ref renderScale);

        ImGui.SameLine();
        ImGui.TextDisabled(instance.GetType().Name);
    }

    private static void InvalidateText(UIDrawable node)
    {
        if (node is UIText text)
        {
            text.MarkDirty();
        }

        if (node is UIGroup group)
        {
            foreach (UIDrawable child in group.children)
            {
                InvalidateText(child);
            }
        }
    }

    private void DrawContextPanel()
    {
        if (!ImGui.CollapsingHeader($"Data Context ({keys.Count} keys)", ImGuiTreeNodeFlags.DefaultOpen))
        {
            return;
        }

        DrawContextSourcePicker();

        if (liveInstanceId.Length > 0)
        {
            return;
        }

        foreach ((string key, DataBindingKind kind) in keys)
        {
            if (kind == DataBindingKind.Texture)
            {
                ImGui.LabelText(key, "(placeholder texture)");
                continue;
            }

            string value = values[key];
            if (ImGui.InputText($"{key} ({kind})", ref value, 256))
            {
                values[key] = value;
                dummy.SetString(key, value);
            }
        }

        // if (keys.Count > 0)
        // {
        //     ImGui.TextDisabled("An empty value is left unresolved, so the layout's own value stands.");
        // }
    }

    private void DrawContextSourcePicker()
    {
        string current = liveInstanceId.Length == 0 ? "Dummy values" : DescribeLive(DrawableTracker.GetDrawable(liveInstanceId));

        if (!ImGui.BeginCombo("Context", current))
        {
            return;
        }

        if (ImGui.Selectable("Dummy values", liveInstanceId.Length == 0))
        {
            liveInstanceId = string.Empty;
        }

        foreach (ComponentInstance candidate in LiveInstances(componentPath))
        {
            if (ImGui.Selectable(DescribeLive(candidate), liveInstanceId == candidate.id))
            {
                liveInstanceId = candidate.id;
            }
        }

        ImGui.EndCombo();
    }

    private static string DescribeLive(UIDrawable? drawable)
        => drawable == null ? "(instance is gone)" : $"{drawable.name} [{drawable.GetType().Name}]";

    private void Render()
    {
        if (OpenGlRenderer.Instance is not { } renderer || renderer.gl is not { } gl)
        {
            return;
        }

        if (attachedContextGeneration != renderer.contextGeneration)
        {
            attachedContextGeneration = renderer.contextGeneration;
            target.AttachGraphics(gl);
        }

        if (autoFit)
        {
            FitCanvas();
        }
        else
        {
            canvasOrigin = canvasSize * 0.5f;
        }

        int width = Math.Clamp((int)canvasSize.X, 1, 4096);
        int height = Math.Clamp((int)canvasSize.Y, 1, 4096);
        target.Resize(width, height);

        UpdateContext();

        //ImGui only records draw commands until the end of the frame, so rendering into our own target
        //here is safe as long as the default framebuffer and the renderer's cached state are put back
        renderer.Flush();
        target.BindForRendering();

        //transparent, so the canvas and its axes are drawn behind the image rather than over the component
        gl.ClearColor(0f, 0f, 0f, 0f);
        gl.Clear((uint)Silk.NET.OpenGL.ClearBufferMask.ColorBufferBit);
        renderer.SetProjection(width, height);

        //a component is authored around its own origin, which is rarely its top left corner
        viewport.showOrigin = true;
        viewport.origin = canvasOrigin;

        root.position = new Vector3(canvasOrigin, 0.0f);

        float scale = renderScale ?? CDTXMania.renderScale;

        //the canvas grows with the scale it is drawn at, so the view is held in the component's own pixels
        //and a change of scale leaves it where it was
        viewport.renderScale = scale;

        if (scale != drawnScale)
        {
            //text is rasterized at the scale it was last drawn at, so it has to be asked for again. Also
            //catches the game window being resized while this editor follows it
            drawnScale = scale;
            InvalidateText(root);
        }

        try
        {
            //a stage scales its root by renderScale, and elements that size themselves in pixels read it
            //while drawing, so both have to say the same thing for the whole subtree
            using (CDTXMania.PushRenderScale(scale))
            {
                root.scale = new Vector3(scale, scale, 1.0f);
                root.Draw(Matrix4x4.Identity);
            }

            MeasureContent();
            drawError = null;
        }
        catch (Exception e)
        {
            //a behaviour class can expect a stage that isn't running; keep the editor usable
            drawError = $"{e.GetType().Name}: {e.Message}";
        }
        finally
        {
            renderer.Flush();
            target.BindDefaultFramebuffer((int)InspectorManager.framebufferSize.X, (int)InspectorManager.framebufferSize.Y);
            renderer.SetProjection((int)InspectorManager.gameRenderSize.X, (int)InspectorManager.gameRenderSize.Y);
            renderer.InvalidateStateCache();
        }
    }

    //measured after drawing, when every element's transform is up to date and lazily loaded art has a size
    private void MeasureContent()
    {
        Vector2 origin = new(root.position.X, root.position.Y);
        Vector2 min = origin;
        Vector2 max = origin;
        ComponentBounds.Measure(root, ref min, ref max);

        contentMin = min - origin;
        contentMax = max - origin;
    }

    private void FitCanvas()
    {
        (Vector2 canvas, Vector2 origin) = ComponentBounds.Fit(contentMin, contentMax);

        //a moving element would otherwise resize the render target every frame
        if (MathF.Abs(canvas.X - canvasSize.X) < 8.0f && MathF.Abs(canvas.Y - canvasSize.Y) < 8.0f)
        {
            return;
        }

        canvasSize = canvas;
        canvasOrigin = origin;
    }

    //the context lives on the editor root rather than on the instance, so a behaviour class that pushes its
    //own values still wins for the keys it owns
    private void UpdateContext()
    {
        keys.Clear();
        ComponentKeys.Collect(root, keys);

        foreach ((string key, DataBindingKind kind) in keys)
        {
            if (values.ContainsKey(key))
            {
                continue;
            }

            //what the component was last saved with beats anything made up here
            values[key] = instance.sampleContext?.GetValueOrDefault(key) ?? DefaultValue(key, kind);

            if (kind == DataBindingKind.Texture)
            {
                dummy.SetTexture(key, PlaceholderTexture());
            }
            else
            {
                dummy.SetString(key, values[key]);
            }
        }

        root.dataContext = liveInstanceId.Length == 0 ? dummy : borrowed;
    }

    //a number left empty resolves to nothing, which leaves whatever the layout set: a made-up size or
    //offset would be more confusing than the authored one
    private static string DefaultValue(string key, DataBindingKind kind) => kind switch
    {
        DataBindingKind.Bool => "true",
        DataBindingKind.Number => string.Empty,
        _ => key[(key.LastIndexOf('.') + 1)..]
    };

    private ComponentInstance CreateInstance(Type? behaviour)
    {
        ComponentInstance created = Instantiate(behaviour);
        created.component = componentPath;
        created.name = Path.GetFileNameWithoutExtension(componentPath);
        return created;
    }

    private static ComponentInstance Instantiate(Type? behaviour)
    {
        if (behaviour == null || !typeof(ComponentInstance).IsAssignableFrom(behaviour))
        {
            return new PreviewComponent();
        }

        try
        {
            return (ComponentInstance)Activator.CreateInstance(behaviour)!;
        }
        catch (Exception e)
        {
            Trace.TraceWarning($"Component editor could not construct {behaviour.Name}: {e.Message}");
            return new PreviewComponent();
        }
    }

    private static IEnumerable<ComponentInstance> LiveInstances(string componentPath)
    {
        foreach (UIDrawable root in LiveRoots())
        {
            foreach (ComponentInstance instance in Descendants(root).OfType<ComponentInstance>())
            {
                if (instance.component == componentPath)
                {
                    yield return instance;
                }
            }
        }
    }

    //the trees the game itself draws; an editor's own tree is deliberately not among them
    private static IEnumerable<UIDrawable> LiveRoots()
    {
        yield return CDTXMania.persistentUIGroup;

        if (CDTXMania.StageManager.rCurrentStage?.ui is { } stageUi)
        {
            yield return stageUi;
        }
    }

    private static IEnumerable<UIDrawable> Descendants(UIDrawable node)
    {
        yield return node;

        if (node is not UIGroup group)
        {
            yield break;
        }

        foreach (UIDrawable child in group.children)
        {
            foreach (UIDrawable descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    private static BaseTexture PlaceholderTexture()
    {
        if (placeholderTexture != null)
        {
            return placeholderTexture;
        }

        const int size = 64;
        const int cell = 8;
        byte[] pixels = new byte[size * size * 4];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                byte shade = (byte)(((x / cell + y / cell) % 2 == 0) ? 0xC0 : 0x60);
                int offset = (y * size + x) * 4;
                pixels[offset] = shade;
                pixels[offset + 1] = shade;
                pixels[offset + 2] = shade;
                pixels[offset + 3] = 0xFF;
            }
        }

        placeholderTexture = BaseTexture.LoadFromMemory(pixels, size, size, "ComponentEditorPlaceholder");
        return placeholderTexture;
    }
}

//a component with no behaviour class of its own: the file is all there is to it
internal sealed class PreviewComponent : ComponentInstance
{
    protected override UIGroup BuildDefault() => new("Component");
}
