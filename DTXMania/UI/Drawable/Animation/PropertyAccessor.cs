using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

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

        Func<object, object?> getter = BuildGetter(rootType, chain);
        Action<object, object?> setter = BuildSetter(rootType, chain);

        return new PropertyAccessor(valueType, getter, setter);
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

    private static Func<object, object?> BuildGetter(Type rootType, List<MemberInfo> chain)
    {
        ParameterExpression rootObj = Expression.Parameter(typeof(object), "root");
        Expression current = Expression.Convert(rootObj, rootType);
        foreach (MemberInfo member in chain)
        {
            current = Expression.MakeMemberAccess(current, member);
        }
        Expression boxed = Expression.Convert(current, typeof(object));
        return Expression.Lambda<Func<object, object?>>(boxed, rootObj).Compile();
    }

    private static Action<object, object?> BuildSetter(Type rootType, List<MemberInfo> chain)
    {
        // For chains like a.b.c = value where intermediates are value types, naive
        // member-assign would mutate a copy. We have to read each intermediate into a local,
        // assign through, and write back up the chain.
        ParameterExpression rootObj = Expression.Parameter(typeof(object), "root");
        ParameterExpression newValue = Expression.Parameter(typeof(object), "value");

        ParameterExpression rootTyped = Expression.Variable(rootType, "rootTyped");
        List<ParameterExpression> locals = new() { rootTyped };
        List<Expression> body = new() { Expression.Assign(rootTyped, Expression.Convert(rootObj, rootType)) };

        // Read intermediates into locals.
        Expression accessExpr = rootTyped;
        List<ParameterExpression> intermediates = new();
        for (int i = 0; i < chain.Count - 1; i++)
        {
            MemberInfo m = chain[i];
            Type t = MemberType(m);
            ParameterExpression local = Expression.Variable(t, $"v{i}");
            intermediates.Add(local);
            locals.Add(local);
            accessExpr = Expression.MakeMemberAccess(accessExpr, m);
            body.Add(Expression.Assign(local, accessExpr));
            accessExpr = local;
        }

        // Assign the leaf on the deepest local (or directly on rootTyped if chain length == 1).
        MemberInfo leaf = chain[^1];
        Expression leafTarget = chain.Count == 1
            ? Expression.MakeMemberAccess(rootTyped, leaf)
            : Expression.MakeMemberAccess(intermediates[^1], leaf);
        Expression valueConverted = Expression.Convert(newValue, MemberType(leaf));
        body.Add(Expression.Assign(leafTarget, valueConverted));

        // Write the chain back up. For chain length n, write intermediates[i] back into the
        // appropriate parent (intermediates[i-1] or rootTyped at i==0).
        for (int i = intermediates.Count - 1; i >= 0; i--)
        {
            Expression parent = i == 0 ? (Expression)rootTyped : intermediates[i - 1];
            Expression parentMember = Expression.MakeMemberAccess(parent, chain[i]);
            body.Add(Expression.Assign(parentMember, intermediates[i]));
        }

        // If root is a reference type (UIDrawable always is), we don't need to write back to
        // the boxed object — the assignments above mutated the same instance.
        BlockExpression block = Expression.Block(locals, body);
        return Expression.Lambda<Action<object, object?>>(block, rootObj, newValue).Compile();
    }
}
