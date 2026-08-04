using System.Collections.Concurrent;
using System.Reflection;
using DTXMania.UI.DynamicElements;

namespace DTXMania.UI.Animation;

/// <summary>
/// Compiled, cached getter/setter for a property path on a given root type.
/// The path is the *property* portion only (e.g. "position", "position.X", "color.R").
/// Drawable navigation (the "child1/child2/" prefix) is handled separately by AnimationTrack.
/// </summary>
public sealed class PropertyAccessor
{
    private static readonly ConcurrentDictionary<(Type, string), PropertyAccessor?> Cache = new();

    public Type ValueType { get; }
    public Func<object, object?> Getter { get; }
    public Action<object, object?> Setter { get; }

    //typed setters for the kinds a data binding can supply, so writing one doesn't box. Exactly one is
    //non-null for a bindable member; all are null for a member no binding can drive (a Vector3, say).
    public Action<object, string>? StringSetter { get; private init; }
    public Action<object, bool>? BoolSetter { get; private init; }
    public Action<object, double>? NumberSetter { get; private init; }

    public bool IsBindable => StringSetter != null || BoolSetter != null || NumberSetter != null;

    private PropertyAccessor(Type valueType, Func<object, object?> getter, Action<object, object?> setter)
    {
        ValueType = valueType;
        Getter = getter;
        Setter = setter;
    }

    /// <summary>
    /// Get or build a PropertyAccessor for the given (root type, dot-separated property path).
    /// Returns null if the path is invalid or the leaf is not marked Themable.
    /// </summary>
    public static PropertyAccessor? GetOrBuild(Type rootType, string propertyPath)
    {
        return Cache.GetOrAdd((rootType, propertyPath), key => Build(key.Item1, key.Item2));
    }

    private static PropertyAccessor? Build(Type rootType, string propertyPath)
    {
        string[] segments = propertyPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return null;
        }

        // Walk the member chain, collecting each MemberInfo along the way so we can
        // build both a getter and a (read-modify-write) setter.
        List<MemberInfo> chain = new(segments.Length);
        Type currentType = rootType;
        foreach (string segment in segments)
        {
            MemberInfo? member = ResolveMember(currentType, segment);
            if (member == null)
            {
                return null;
            }
            chain.Add(member);
            currentType = MemberType(member);
        }

        // The leaf member must be a Themable field/property. Intermediate members do not
        // need to be Themable themselves (e.g. .X on a Themable Vector3 is fine).
        MemberInfo leaf = chain[^1];
        if (!HasThemable(chain[0]))
        {
            // The top-level member must be Themable. We treat that as the public-API contract:
            // "the property exposed to animation is the root field marked [Themable]".
            return null;
        }

        Type valueType = MemberType(leaf);

        Func<object, object?> getter = AccessorCompiler.BuildChainGetter(rootType, chain);
        Action<object, object?> setter = AccessorCompiler.BuildChainSetter(rootType, chain);

        return new PropertyAccessor(valueType, getter, setter)
        {
            StringSetter = valueType == typeof(string) ? AccessorCompiler.BuildChainSetter<string>(rootType, chain) : null,
            BoolSetter = valueType == typeof(bool) ? AccessorCompiler.BuildChainSetter<bool>(rootType, chain) : null,
            NumberSetter = DataFieldReflector.IsNumeric(valueType) ? AccessorCompiler.BuildChainSetter<double>(rootType, chain) : null
        };
    }

    private static MemberInfo? ResolveMember(Type type, string name)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        FieldInfo? field = type.GetField(name, flags);
        if (field != null)
        {
            return field;
        }
        PropertyInfo? prop = type.GetProperty(name, flags);
        return prop;
    }

    private static Type MemberType(MemberInfo member) => member switch
    {
        FieldInfo f => f.FieldType,
        PropertyInfo p => p.PropertyType,
        _ => throw new InvalidOperationException()
    };

    private static bool HasThemable(MemberInfo member)
    {
        // ThemableAttribute is defined elsewhere in your codebase — we look it up by name to
        // avoid a hard reference here. Adjust if you'd rather take a typed dependency.
        foreach (var attr in member.GetCustomAttributes(inherit: true))
        {
            if (attr.GetType().Name == "ThemableAttribute")
            {
                return true;
            }
        }
        return false;
    }
}
