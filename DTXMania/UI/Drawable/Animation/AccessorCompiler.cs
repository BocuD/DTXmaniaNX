using System.Linq.Expressions;
using System.Reflection;

namespace DTXMania.UI.Animation;

/// <summary>
/// Builds compiled accessors from expression trees, so neither the animation system
/// (<see cref="PropertyAccessor"/>, whole-path get/set) nor the UI data-binding reflector
/// (<see cref="DynamicElements.DataFieldReflector"/>, single-member getters) pays per-read reflection.
/// </summary>
public static class AccessorCompiler
{
    public static Func<object, object?> BuildMemberGetter(Type declaringType, MemberInfo member)
        => BuildMemberGetter<object?>(declaringType, member);

    //a getter returning TValue directly, so reading a value-type member doesn't box
    public static Func<object, TValue> BuildMemberGetter<TValue>(Type declaringType, MemberInfo member)
    {
        ParameterExpression rootObj = Expression.Parameter(typeof(object), "root");
        Expression access = Expression.MakeMemberAccess(Expression.Convert(rootObj, declaringType), member);
        Expression converted = access.Type == typeof(TValue) ? access : Expression.Convert(access, typeof(TValue));
        return Expression.Lambda<Func<object, TValue>>(converted, rootObj).Compile();
    }

    /// <summary>
    /// A getter for one element of a collection member, returning TValue directly so reading
    /// <c>"Ranks[2]"</c> off an <c>int[]</c> doesn't box the element. The caller checks the bounds; this
    /// throws for an index outside them, exactly as indexing normally would.
    /// </summary>
    public static Func<object, int, TValue>? BuildIndexedGetter<TValue>(Type declaringType, MemberInfo member)
    {
        Type collectionType = MemberType(member);
        Type? elementType = collectionType.IsArray
            ? collectionType.GetElementType()
            : collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(List<>)
                ? collectionType.GetGenericArguments()[0]
                : null;

        if (elementType == null)
        {
            return null;
        }

        ParameterExpression rootObj = Expression.Parameter(typeof(object), "root");
        ParameterExpression index = Expression.Parameter(typeof(int), "index");

        Expression collection = Expression.MakeMemberAccess(Expression.Convert(rootObj, declaringType), member);
        Expression element = collectionType.IsArray
            ? Expression.ArrayIndex(collection, index)
            : Expression.Property(collection, "Item", index);

        Expression converted = element.Type == typeof(TValue) ? element : Expression.Convert(element, typeof(TValue));
        return Expression.Lambda<Func<object, int, TValue>>(converted, rootObj, index).Compile();
    }

    //walks a member chain from rootType, e.g. position.X
    public static Func<object, object?> BuildChainGetter(Type rootType, IReadOnlyList<MemberInfo> chain)
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

    public static Action<object, object?> BuildChainSetter(Type rootType, IReadOnlyList<MemberInfo> chain)
        => BuildChainSetter<object?>(rootType, chain);

    /// <summary>
    /// A setter taking TValue directly, so writing a value-type member doesn't box. The conversion to the
    /// member's own type happens inside the compiled lambda, so one <c>double</c> setter serves any
    /// numeric member.
    /// </summary>
    //for a chain like a.b.c = value with value-type intermediates, a naive member-assign would mutate a
    //copy, so each intermediate is read into a local, assigned through, and written back up
    public static Action<object, TValue> BuildChainSetter<TValue>(Type rootType, IReadOnlyList<MemberInfo> chain)
    {
        ParameterExpression rootObj = Expression.Parameter(typeof(object), "root");
        ParameterExpression newValue = Expression.Parameter(typeof(TValue), "value");

        ParameterExpression rootTyped = Expression.Variable(rootType, "rootTyped");
        List<ParameterExpression> locals = new() { rootTyped };
        List<Expression> body = new() { Expression.Assign(rootTyped, Expression.Convert(rootObj, rootType)) };

        //read intermediates into locals
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

        //assign the leaf on the deepest local, or directly on rootTyped for a single-member chain
        MemberInfo leaf = chain[^1];
        Expression leafTarget = chain.Count == 1
            ? Expression.MakeMemberAccess(rootTyped, leaf)
            : Expression.MakeMemberAccess(intermediates[^1], leaf);
        Type leafType = MemberType(leaf);
        Expression valueConverted = newValue.Type == leafType ? newValue : Expression.Convert(newValue, leafType);
        body.Add(Expression.Assign(leafTarget, valueConverted));

        //write the chain back up, each intermediate into its parent
        for (int i = intermediates.Count - 1; i >= 0; i--)
        {
            Expression parent = i == 0 ? (Expression)rootTyped : intermediates[i - 1];
            Expression parentMember = Expression.MakeMemberAccess(parent, chain[i]);
            body.Add(Expression.Assign(parentMember, intermediates[i]));
        }

        BlockExpression block = Expression.Block(locals, body);
        return Expression.Lambda<Action<object, TValue>>(block, rootObj, newValue).Compile();
    }

    public static Type MemberType(MemberInfo member) => member switch
    {
        FieldInfo f => f.FieldType,
        PropertyInfo p => p.PropertyType,
        _ => throw new InvalidOperationException()
    };
}
