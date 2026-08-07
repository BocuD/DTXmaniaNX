using DTXMania.UI.Drawable;

namespace DTXMania.UI.DynamicElements;

/// <summary>
/// The keys a component asks its data context for, and the kind of value each one has to be. Everything a
/// component reads comes in through a binding or a dynamic image, so this is the complete list.
/// </summary>
public static class ComponentKeys
{
    public static void Collect(UIDrawable element, Dictionary<string, DataBindingKind> keys)
    {
        foreach (UIBinding binding in element.bindings)
        {
            if (!string.IsNullOrEmpty(binding.source))
            {
                keys.TryAdd(binding.source, binding.KindFor(element));
            }
        }

        if (element is UIImage { imageSource: ImageSource.Dynamic } image && !string.IsNullOrWhiteSpace(image.dynamicSource))
        {
            keys.TryAdd(image.dynamicSource, DataBindingKind.Texture);
        }

        if (element is UIGroup group)
        {
            foreach (UIDrawable child in group.children)
            {
                Collect(child, keys);
            }
        }
    }

    /// <summary>
    /// The values those keys currently resolve to, as text. Each key is resolved through the element that
    /// reads it, since a list's slot supplies keys that its owner cannot see. Textures are left out: there
    /// is nothing to write down for one.
    /// </summary>
    public static void Capture(UIDrawable element, Dictionary<string, string> values)
    {
        foreach (UIBinding binding in element.bindings)
        {
            if (!string.IsNullOrEmpty(binding.source) && element.TryResolveContextString(binding.source, out string value))
            {
                values[binding.source] = value;
            }
        }

        if (element is UIGroup group)
        {
            foreach (UIDrawable child in group.children)
            {
                Capture(child, values);
            }
        }
    }
}
