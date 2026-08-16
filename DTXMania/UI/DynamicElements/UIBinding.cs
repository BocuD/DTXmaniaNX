using DTXMania.UI.Animation;
using DTXMania.UI.Drawable;
using Newtonsoft.Json;

namespace DTXMania.UI.DynamicElements;

/// <summary>
/// Drives one <c>[Themable]</c> member of an element from a data context key, e.g. <c>size.X</c> from
/// <c>"Row.SkillWidth"</c>. This is what lets a component read what it needs instead of having code find
/// it by name and push values into it.
///
/// The member path is the same shape the animation system uses, and resolves through the same compiled
/// <see cref="PropertyAccessor"/>. Bindings are applied before the animator, so an animation on the same
/// member wins for that frame.
/// </summary>

public sealed class UIBinding
{
    //member path on the element being bound, e.g. "isVisible", "position.X", "color.Alpha"
    public string target = string.Empty;

    //data context key, e.g. "Row.Level" or "Song.Chart.SongInformation.Genre:0.00"
    public string source = string.Empty;

    //flips a bool on the way through, so "hide this when selected" needs no second context key for the
    //opposite of one that already exists
    public bool invert;

    /// <summary>
    /// Whether the last apply found a value. A binding that resolves nothing leaves its target untouched,
    /// so this is the only way to tell a key that is missing from one that happens to match.
    /// </summary>
    [JsonIgnore] public bool resolved = true;

    [JsonIgnore] private PropertyAccessor? accessor;
    [JsonIgnore] private string? resolvedTarget;

    public UIBinding()
    {
    }

    public UIBinding(string target, string source)
    {
        this.target = target;
        this.source = source;
    }

    public void Apply(UIDrawable element)
    {
        if (string.IsNullOrEmpty(target) || string.IsNullOrEmpty(source))
        {
            return;
        }

        //re-resolve only when the target path changes, e.g. edited in the inspector
        if (resolvedTarget != target)
        {
            resolvedTarget = target;
            accessor = PropertyAccessor.GetOrBuild(element.GetType(), target);
        }

        if (accessor == null)
        {
            return;
        }

        if (accessor.StringSetter != null)
        {
            resolved = element.TryResolveContextString(source, out string text);

            if (resolved)
            {
                accessor.StringSetter(element, text);
            }
        }
        else if (accessor.BoolSetter != null)
        {
            resolved = element.TryResolveContextBool(source, out bool flag);

            if (resolved)
            {
                accessor.BoolSetter(element, flag ^ invert);
            }
        }
        else if (accessor.NumberSetter != null)
        {
            resolved = element.TryResolveContextNumber(source, out double number);

            if (resolved)
            {
                accessor.NumberSetter(element, number);
            }
        }
    }

    //the kind of value this binding's target needs, so the inspector can offer only keys that fit
    public DataBindingKind KindFor(UIDrawable element)
    {
        PropertyAccessor? target = PropertyAccessor.GetOrBuild(element.GetType(), this.target);

        if (target?.BoolSetter != null) return DataBindingKind.Bool;
        if (target?.NumberSetter != null) return DataBindingKind.Number;

        return DataBindingKind.String;
    }
}
