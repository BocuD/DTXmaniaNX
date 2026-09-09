using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;

namespace DTXMania.UI.Inspector;

/// <summary>One editable stand-in value.</summary>
public sealed class PreviewValue
{
    public DataBindingKind kind;
    public bool seeded;
    public string text = string.Empty;
}

/// <summary>
/// The values read inside one scope: the component as a whole, or one slot of a list within it. Each slot
/// keeps its own, which is what makes a list of rows show five different rows rather than five copies.
/// </summary>
public sealed class PreviewScope
{
    //keeps the editor's order stable rather than reshuffling as keys are found
    public readonly List<string> order = [];
    public readonly Dictionary<string, PreviewValue> values = new();

    public readonly UIDataContext context = new();

    public PreviewValue Declare(string key, DataBindingKind kind)
    {
        if (values.TryGetValue(key, out PreviewValue? existing))
        {
            return existing;
        }

        PreviewValue value = new() { kind = kind, text = string.Empty };
        values[key] = value;
        order.Add(key);
        return value;
    }

    public void Push(string key)
    {
        if (values.TryGetValue(key, out PreviewValue? value))
        {
            context.SetString(key, value.text);
        }
    }

    public void PushAll()
    {
        foreach (string key in order)
        {
            Push(key);
        }
    }
}

/// <summary>
/// Stand-in data for one component in the editor, shaped like the component itself: a root scope, plus one
/// scope per slot of every list inside it.
/// </summary>
public sealed class ComponentPreview : IUIPreview
{
    public PreviewScope root { get; } = new();

    //keyed by the list rather than by name, so slots being rebuilt does not lose what was typed
    private readonly Dictionary<UIItemsGroup, List<PreviewScope>> lists = new();

    //the lists in the order they were found, for the editor to draw
    public readonly List<UIItemsGroup> listOrder = [];

    //what the last walk reached
    private readonly HashSet<UIItemsGroup> seen = [];

    IUIDataContext IUIPreview.Root => root.context;

    public IUIDataContext? ScopeFor(UIGroup group)
    {
        if (group is not UIItemSlot slot || slot.parent is not UIItemsGroup list)
        {
            return null;
        }

        return lists.TryGetValue(list, out List<PreviewScope>? scopes)
               && slot.index >= 0
               && slot.index < scopes.Count
            ? scopes[slot.index].context
            : null;
    }

    public IReadOnlyList<PreviewScope> ScopesOf(UIItemsGroup list)
        => lists.TryGetValue(list, out List<PreviewScope>? scopes) ? scopes : [];

    public PreviewScope ScopeOf(UIItemsGroup list, int index)
    {
        if (!lists.TryGetValue(list, out List<PreviewScope>? scopes))
        {
            scopes = [];
            lists[list] = scopes;
            listOrder.Add(list);
        }

        while (scopes.Count <= index)
        {
            scopes.Add(new PreviewScope());
        }

        return scopes[index];
    }

    /// <summary>Finds every key the component reads, in the scope that reads it. Runs each frame: slots
    /// come and go as a list is edited, and a key only exists once the element carrying it is built.</summary>
    public void Collect(UIDrawable element)
    {
        seen.Clear();
        Collect(element, root);
        Prune();
    }

    private void Collect(UIDrawable element, PreviewScope scope)
    {
        if (element is UIItemsGroup found)
        {
            seen.Add(found);
        }

        foreach (UIBinding binding in element.bindings)
        {
            if (!string.IsNullOrEmpty(binding.source))
            {
                scope.Declare(binding.source, binding.KindFor(element));
            }
        }

        if (element is UIImage { imageSource: ImageSource.Dynamic } image
            && !string.IsNullOrWhiteSpace(image.dynamicSource))
        {
            scope.Declare(image.dynamicSource, DataBindingKind.Texture);
        }

        if (element is not UIGroup group)
        {
            return;
        }

        foreach (UIDrawable child in group.children)
        {
            //a slot reads the same keys as its siblings but answers them differently, so it gets its own
            if (child is UIItemSlot slot && group is UIItemsGroup list)
            {
                Collect(slot, ScopeOf(list, slot.index));
                continue;
            }

            Collect(child, scope);
        }
    }

    //a list the walk no longer reaches keeps nothing: its values could never be shown again. Detached
    //rather than reparented is the usual case, and a removed child keeps pointing at its old parent
    private void Prune()
    {
        for (int i = listOrder.Count - 1; i >= 0; i--)
        {
            if (!seen.Contains(listOrder[i]))
            {
                lists.Remove(listOrder[i]);
                listOrder.RemoveAt(i);
            }
        }
    }

    /// <summary>Pushes every value into the contexts the chain reads, ready for a draw.</summary>
    public void Apply()
    {
        root.PushAll();

        foreach (UIItemsGroup list in listOrder)
        {
            foreach (PreviewScope scope in lists[list])
            {
                scope.PushAll();
            }
        }
    }

    /// <summary>What the component is saved with, so it opens again showing the same thing. A slot's keys
    /// are written under its list and index, e.g. <c>Rows[2].Item.Level</c>.</summary>
    public Dictionary<string, string> ToSamples()
    {
        Dictionary<string, string> samples = new();

        foreach (string key in root.order)
        {
            samples[key] = root.values[key].text;
        }

        foreach (UIItemsGroup list in listOrder)
        {
            List<PreviewScope> scopes = lists[list];
            for (int i = 0; i < scopes.Count; i++)
            {
                foreach (string key in scopes[i].order)
                {
                    samples[SampleKey(list, i, key)] = scopes[i].values[key].text;
                }
            }
        }

        return samples;
    }

    public static string SampleKey(UIItemsGroup list, int index, string key) => $"{list.name}[{index}].{key}";
}
