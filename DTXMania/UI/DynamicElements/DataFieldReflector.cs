using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using DTXMania.UI.Animation;

namespace DTXMania.UI.DynamicElements;

//an exposed leaf path on a type, for the inspector's binding dropdowns. Indexable marks array/list
//leaves, whose index the author fills in, and whose Path ends in "[]"
public readonly record struct DataFieldPath(string Path, Type ValueType, bool Indexable);

//one hop of a parsed binding path; Index is -1 unless the segment was written as "Name[3]"
public readonly record struct PathSegment(string Name, int Index);

/// <summary>
/// A binding key parsed once into the parts needed to resolve it: the registered object it starts from,
/// the hops to walk, and an optional <c>:format</c> override. Bound elements resolve every frame, so the
/// parse is cached by <see cref="DataFieldReflector.ParseKey"/> rather than repeated per read.
/// </summary>
public sealed class BindingPath
{
    public static readonly BindingPath Invalid = new(string.Empty, [], null);

    public readonly string ObjectName;
    public readonly PathSegment[] Segments;
    public readonly string? Format;

    public BindingPath(string objectName, PathSegment[] segments, string? format)
    {
        ObjectName = objectName;
        Segments = segments;
        Format = format;
    }
}

/// <summary>
/// Reads <see cref="DataFieldAttribute"/>-annotated object graphs: resolves a parsed path such as
/// <c>"Info.BestRank[0]"</c> against a root object, and enumerates a type's exposed paths for the
/// inspector. Member maps are cached per type and each hop uses a compiled getter.
/// </summary>
public static class DataFieldReflector
{
    private sealed record Member(string Name, DataFieldAttribute Meta, Type ValueType, Func<object, object?> Get)
    {
        //typed getters for the kinds that would otherwise box on every read; null when not applicable
        public Func<object, bool>? GetBool { get; init; }
        public Func<object, double>? GetNumber { get; init; }

        //the same for one element of a collection member, so "Ranks[2]" doesn't box either
        public Func<object, int, bool>? GetIndexedBool { get; init; }
        public Func<object, int, double>? GetIndexedNumber { get; init; }
    }

    private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, Member>> Maps = new();
    private static readonly ConcurrentDictionary<string, BindingPath> ParsedKeys = new();

    //cached so GetOrAdd doesn't allocate a delegate per lookup
    private static readonly Func<string, BindingPath> ParseKeyFactory = ParseKeyUncached;

    /// <summary>Parses (and caches) a binding key of the form <c>"Object.sub.path[2]:format"</c>.</summary>
    public static BindingPath ParseKey(string key)
        => string.IsNullOrEmpty(key) ? BindingPath.Invalid : ParsedKeys.GetOrAdd(key, ParseKeyFactory);

    private static BindingPath ParseKeyUncached(string key)
    {
        string path = key;
        string? format = null;

        int colon = path.IndexOf(':');
        if (colon >= 0)
        {
            format = path[(colon + 1)..];
            path = path[..colon];
        }

        int dot = path.IndexOf('.');
        string objectName = dot >= 0 ? path[..dot] : path;
        string subPath = dot >= 0 ? path[(dot + 1)..] : string.Empty;

        if (subPath.Length == 0)
        {
            return new BindingPath(objectName, [], format);
        }

        string[] raw = subPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
        PathSegment[] segments = new PathSegment[raw.Length];

        for (int i = 0; i < raw.Length; i++)
        {
            string name = raw[i];
            int index = -1;

            int bracket = name.IndexOf('[');
            if (bracket >= 0 && name.EndsWith(']'))
            {
                if (!int.TryParse(name[(bracket + 1)..^1], NumberStyles.Integer, CultureInfo.InvariantCulture, out index))
                {
                    return BindingPath.Invalid;
                }

                name = name[..bracket];
            }

            segments[i] = new PathSegment(name, index);
        }

        return new BindingPath(objectName, segments, format);
    }

    //returns the leaf value plus the leaf member's attribute, which carries the default format. A key with
    //no sub-path ("Song" rather than "Song.Title") resolves to the registered object itself.
    public static bool TryResolve(object? root, PathSegment[] segments, out object? value, out DataFieldAttribute? meta)
    {
        value = null;
        meta = null;

        if (segments.Length == 0)
        {
            value = root;
            return root != null;
        }

        if (!TryWalkToOwner(root, segments, out object owner, out Member? leaf))
        {
            return false;
        }

        meta = leaf.Meta;
        return TryReadLeaf(owner, leaf, segments[^1].Index, out value);
    }

    public static bool TryResolveBool(object? root, PathSegment[] segments, out bool value)
    {
        value = false;

        if (segments.Length == 0)
        {
            return TryCoerceBool(root, out value);
        }

        if (!TryWalkToOwner(root, segments, out object owner, out Member? leaf))
        {
            return false;
        }

        int index = segments[^1].Index;
        if (leaf.GetBool != null && index < 0)
        {
            value = leaf.GetBool(owner);
            return true;
        }

        if (leaf.GetIndexedBool != null && IsInRange(leaf.Get(owner), index))
        {
            value = leaf.GetIndexedBool(owner, index);
            return true;
        }

        return TryReadLeaf(owner, leaf, index, out object? raw) && TryCoerceBool(raw, out value);
    }

    public static bool TryResolveNumber(object? root, PathSegment[] segments, out double value)
    {
        value = 0.0;

        if (segments.Length == 0)
        {
            return TryCoerceNumber(root, out value);
        }

        if (!TryWalkToOwner(root, segments, out object owner, out Member? leaf))
        {
            return false;
        }

        int index = segments[^1].Index;
        if (leaf.GetNumber != null && index < 0)
        {
            value = leaf.GetNumber(owner);
            return true;
        }

        if (leaf.GetIndexedNumber != null && IsInRange(leaf.Get(owner), index))
        {
            value = leaf.GetIndexedNumber(owner, index);
            return true;
        }

        return TryReadLeaf(owner, leaf, index, out object? raw) && TryCoerceNumber(raw, out value);
    }

    //walks every hop but the last, so the caller can read the leaf with a typed getter. Intermediate
    //values are read as object; a struct intermediate therefore still boxes, which is why deep paths
    //through structs are best avoided in per-frame bindings.
    private static bool TryWalkToOwner(object? root, PathSegment[] segments,
        out object owner, [NotNullWhen(true)] out Member? leaf)
    {
        owner = null!;
        leaf = null;

        if (root == null || segments.Length == 0)
        {
            return false;
        }

        object? current = root;
        for (int i = 0; i < segments.Length - 1; i++)
        {
            if (!TryGetMember(current, segments[i].Name, out Member? member))
            {
                return false;
            }

            if (!TryReadLeaf(current, member, segments[i].Index, out current) || current == null)
            {
                return false;
            }
        }

        if (!TryGetMember(current, segments[^1].Name, out leaf))
        {
            return false;
        }

        owner = current!;
        return true;
    }

    private static bool TryGetMember(object? instance, string name, [NotNullWhen(true)] out Member? member)
    {
        if (instance == null)
        {
            member = null;
            return false;
        }

        return GetMap(instance.GetType()).TryGetValue(name, out member);
    }

    //an index that falls outside the collection is a failure to resolve, not an empty value: the binding
    //should fall through to the next context rather than shadow it with nothing
    private static bool TryReadLeaf(object owner, Member member, int index, out object? value)
    {
        value = member.Get(owner);

        if (index < 0)
        {
            return true;
        }

        return TryGetIndexed(value, index, out value);
    }

    public static bool TryCoerceBool(object? raw, out bool value)
    {
        value = false;
        switch (raw)
        {
            case null:
                return false;
            case bool b:
                value = b;
                return true;
            case string s:
                return bool.TryParse(s, out value);
            case IConvertible convertible:
                try { value = convertible.ToBoolean(CultureInfo.InvariantCulture); return true; }
                catch { return false; }
            default:
                return false;
        }
    }

    public static bool TryCoerceNumber(object? raw, out double value)
    {
        value = 0.0;
        switch (raw)
        {
            case null:
                return false;
            case double d:
                value = d;
                return true;
            case string s:
                return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
            case IConvertible convertible:
                try { value = convertible.ToDouble(CultureInfo.InvariantCulture); return true; }
                catch { return false; }
            default:
                return false;
        }
    }

    public static string FormatAsString(object? value, string? format)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrEmpty(format) && value is IFormattable formattable)
        {
            return formattable.ToString(format, CultureInfo.InvariantCulture);
        }

        return value.ToString() ?? string.Empty;
    }

    //walks nested DataField objects; arrays and lists surface as "Name[]" templates
    public static IEnumerable<DataFieldPath> EnumeratePaths(Type type, int maxDepth = 3)
    {
        List<DataFieldPath> paths = new();
        Collect(type, string.Empty, 0, maxDepth, paths, new HashSet<Type>());
        return paths;
    }

    private static void Collect(Type type, string prefix, int depth, int maxDepth, List<DataFieldPath> outPaths, HashSet<Type> visiting)
    {
        if (depth > maxDepth || !visiting.Add(type))
        {
            return;
        }

        foreach (Member member in GetMap(type).Values)
        {
            string path = prefix.Length == 0 ? member.Name : $"{prefix}.{member.Name}";
            Type? element = ElementType(member.ValueType);

            if (element != null)
            {
                outPaths.Add(new DataFieldPath($"{path}[]", element, true));
                if (HasDataFields(element))
                {
                    Collect(element, $"{path}[]", depth + 1, maxDepth, outPaths, visiting);
                }
            }
            else if (HasDataFields(member.ValueType))
            {
                Collect(member.ValueType, path, depth + 1, maxDepth, outPaths, visiting);
            }
            else
            {
                outPaths.Add(new DataFieldPath(path, member.ValueType, false));
            }
        }

        visiting.Remove(type);
    }

    private static bool HasDataFields(Type type) => GetMap(type).Count > 0;

    private static IReadOnlyDictionary<string, Member> GetMap(Type type) => Maps.GetOrAdd(type, BuildMap);

    private static IReadOnlyDictionary<string, Member> BuildMap(Type type)
    {
        Dictionary<string, Member> map = new(StringComparer.Ordinal);
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        foreach (MemberInfo info in type.GetMembers(flags))
        {
            DataFieldAttribute? attr = info.GetCustomAttribute<DataFieldAttribute>();
            if (attr == null)
            {
                continue;
            }

            Type? valueType = info switch
            {
                PropertyInfo property when property.CanRead && property.GetIndexParameters().Length == 0 => property.PropertyType,
                FieldInfo field => field.FieldType,
                _ => null
            };

            if (valueType == null)
            {
                continue;
            }

            string name = attr.Name ?? info.Name;
            Type? elementType = ElementTypeOf(valueType);

            map[name] = new Member(name, attr, valueType, AccessorCompiler.BuildMemberGetter(type, info))
            {
                GetBool = valueType == typeof(bool) ? AccessorCompiler.BuildMemberGetter<bool>(type, info) : null,
                GetNumber = IsNumeric(valueType) ? AccessorCompiler.BuildMemberGetter<double>(type, info) : null,
                GetIndexedBool = elementType == typeof(bool) ? AccessorCompiler.BuildIndexedGetter<bool>(type, info) : null,
                GetIndexedNumber = elementType != null && IsNumeric(elementType)
                    ? AccessorCompiler.BuildIndexedGetter<double>(type, info)
                    : null
            };
        }

        return map;
    }

    //what one element of an indexable member is, for the typed indexed getters
    private static Type? ElementTypeOf(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)
            ? type.GetGenericArguments()[0]
            : null;
    }

    public static bool IsNumeric(Type type) =>
        type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) ||
        type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong) ||
        type == typeof(float) || type == typeof(double) || type == typeof(decimal);

    //reading an element of a value-type array boxes it; see the known issues in SKINNING.md
    //an out-of-range index resolves to nothing rather than throwing out of the compiled getter. Reading the
    //collection reference itself costs nothing: arrays and lists are reference types
    private static bool IsInRange(object? collection, int index) => collection switch
    {
        Array array => index >= 0 && index < array.Length,
        IList list => index >= 0 && index < list.Count,
        _ => false
    };

    private static bool TryGetIndexed(object? collection, int index, out object? value)
    {
        switch (collection)
        {
            case Array array when index >= 0 && index < array.Length:
                value = array.GetValue(index);
                return true;

            case IList list when index >= 0 && index < list.Count:
                value = list[index];
                return true;

            default:
                value = null;
                return false;
        }
    }

    //the element type if the value is an array or IList<T>, else null
    private static Type? ElementType(Type type)
    {
        if (type == typeof(string))
        {
            return null;
        }

        if (type.IsArray)
        {
            return type.GetElementType();
        }

        foreach (Type i in type.GetInterfaces())
        {
            if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>))
            {
                return i.GetGenericArguments()[0];
            }
        }

        return null;
    }
}
