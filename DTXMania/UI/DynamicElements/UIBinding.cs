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

    public BindingCondition condition = BindingCondition.None;
    public double value;
    public double whenTrue = 1;
    public double whenFalse;

    public bool ShouldSerializecondition() => condition != BindingCondition.None;
    public bool ShouldSerializevalue() => condition is not (BindingCondition.None or BindingCondition.IsTrue or BindingCondition.IsFalse);
    public bool ShouldSerializewhenTrue() => condition != BindingCondition.None;
    public bool ShouldSerializewhenFalse() => condition != BindingCondition.None;

    /// <summary>
    /// Whether the last apply found a value. A binding that resolves nothing leaves its target untouched,
    /// so this is the only way to tell a key that is missing from one that happens to match.
    /// </summary>
    [JsonIgnore] public bool resolved = true;

    [JsonIgnore] private PropertyAccessor? accessor;
    [JsonIgnore] private string? resolvedTarget;

    [JsonIgnore] private bool hasNumber;
    [JsonIgnore] private string? numberSource;
    [JsonIgnore] private double lastNumber;
    [JsonIgnore] private string lastText = string.Empty;

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

        if (condition != BindingCondition.None)
        {
            resolved = TryTest(element, out bool result);

            if (!resolved)
            {
                return;
            }

            if (accessor.BoolSetter != null)
            {
                accessor.BoolSetter(element, result);
            }
            else if (accessor.NumberSetter != null)
            {
                accessor.NumberSetter(element, result ? whenTrue : whenFalse);
            }
            else
            {
                accessor.StringSetter?.Invoke(element, result ? "true" : "false");
            }

            return;
        }

        if (accessor.StringSetter != null)
        {
            if (element.TryResolveContextNumber(source, out double number))
            {
                if (!hasNumber || number != lastNumber || numberSource != source)
                {
                    hasNumber = element.TryResolveContextString(source, out lastText);
                    lastNumber = number;
                    numberSource = source;
                }

                resolved = hasNumber;
            }
            else
            {
                hasNumber = false;
                resolved = element.TryResolveContextString(source, out lastText);
            }

            if (resolved)
            {
                accessor.StringSetter(element, lastText);
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

    public DataBindingKind SourceKindFor(UIDrawable element)
    {
        foreach (IUIDataContext context in element.DataContexts())
        {
            if (context.AvailableKeys(DataBindingKind.Bool).Contains(source)) return DataBindingKind.Bool;
            if (context.AvailableKeys(DataBindingKind.Number).Contains(source)) return DataBindingKind.Number;
        }

        return condition switch
        {
            BindingCondition.None => KindFor(element),
            BindingCondition.IsTrue or BindingCondition.IsFalse => DataBindingKind.Bool,
            _ => DataBindingKind.Number
        };
    }

    private bool TryTest(UIDrawable element, out bool result)
    {
        if (condition is BindingCondition.IsTrue or BindingCondition.IsFalse)
        {
            bool found = element.TryResolveContextBool(source, out bool flag);
            result = (flag == (condition == BindingCondition.IsTrue)) ^ invert;
            return found;
        }

        bool resolvedNumber = element.TryResolveContextNumber(source, out double number);

        result = condition switch
        {
            BindingCondition.Equal => number == value,
            BindingCondition.NotEqual => number != value,
            BindingCondition.Greater => number > value,
            BindingCondition.GreaterOrEqual => number >= value,
            BindingCondition.Less => number < value,
            _ => number <= value
        } ^ invert;

        return resolvedNumber;
    }
}

public enum BindingCondition
{
    None,
    Equal,
    NotEqual,
    Greater,
    GreaterOrEqual,
    Less,
    LessOrEqual,
    IsTrue,
    IsFalse
}
