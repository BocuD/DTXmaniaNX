using System.Collections.Concurrent;
using System.Reflection;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Inspector;

/// <summary>
/// Picks one path out of many, shown as a menu that nests on the dots — <c>Song.Chart.Genre</c> becomes
/// Song > Chart > Genre — the way the add-child menu nests on slashes. A flat list of every context key is
/// unreadable once a song is registered.
/// </summary>
public static class PathPicker
{
    private sealed class Node
    {
        public readonly SortedDictionary<string, Node> children = new(StringComparer.OrdinalIgnoreCase);

        //set on a node that is itself a whole path, so a key can be both a value and a prefix
        public string? path;
    }

    /// <summary>Draws the picker and the raw field beneath it. Returns true when the value changed.</summary>
    public static bool Draw(string label, ref string value, IEnumerable<string> paths)
    {
        bool changed = false;

        if (ImGui.Button($"{(string.IsNullOrEmpty(value) ? "(none)" : value)}##{label}"))
        {
            ImGui.OpenPopup($"pick{label}");
        }

        ImGui.SameLine();
        ImGui.Text(label);

        if (ImGui.BeginPopup($"pick{label}"))
        {
            if (ImGui.Selectable("(none)"))
            {
                value = string.Empty;
                changed = true;
            }

            ImGui.Separator();

            if (DrawLevel(Build(paths), ref value))
            {
                changed = true;
            }

            ImGui.EndPopup();
        }

        //still typeable: an indexed or ":format" key has no entry of its own to pick
        if (ImGui.InputText($"{label} (key)", ref value, 256))
        {
            changed = true;
        }

        return changed;
    }

    private static Node Build(IEnumerable<string> paths)
    {
        Node root = new();

        foreach (string path in paths)
        {
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            Node node = root;
            foreach (string segment in path.Split('.'))
            {
                if (!node.children.TryGetValue(segment, out Node? child))
                {
                    child = new Node();
                    node.children[segment] = child;
                }

                node = child;
            }

            node.path = path;
        }

        return root;
    }

    private static bool DrawLevel(Node node, ref string value)
    {
        bool changed = false;

        foreach ((string segment, Node child) in node.children)
        {
            if (child.children.Count == 0)
            {
                if (child.path != null && ImGui.Selectable(segment))
                {
                    value = child.path;
                    changed = true;
                }

                continue;
            }

            if (!ImGui.BeginMenu(segment))
            {
                continue;
            }

            //a node that is both a value and a prefix can still be picked, from inside its own menu
            if (child.path != null && ImGui.Selectable($"{segment} (this)"))
            {
                value = child.path;
                changed = true;
            }

            if (DrawLevel(child, ref value))
            {
                changed = true;
            }

            ImGui.EndMenu();
        }

        return changed;
    }

    private static readonly ConcurrentDictionary<Type, string[]> TargetCache = new();

    /// <summary>
    /// Every member a binding can write on this element: its <c>[Themable]</c> members, plus one level
    /// into those that are structs, so <c>position.X</c> and <c>color.Alpha</c> are offered rather than
    /// remembered. Only members a binding can actually produce a value for are listed.
    /// </summary>
    public static string[] TargetsFor(UIDrawable element)
        => TargetCache.GetOrAdd(element.GetType(), Targets);

    private static string[] Targets(Type type)
    {
        List<string> paths = [];

        foreach (MemberInfo member in type.GetMembers(BindingFlags.Instance | BindingFlags.Public))
        {
            if (member.GetCustomAttribute<ThemableAttribute>() == null || MemberType(member) is not { } memberType)
            {
                continue;
            }

            if (IsBindable(memberType))
            {
                paths.Add(member.Name);
                continue;
            }

            //a struct member is a container: what a binding writes is one of its own values
            if (!memberType.IsValueType || memberType.IsPrimitive || memberType.IsEnum)
            {
                continue;
            }

            foreach (MemberInfo inner in memberType.GetMembers(BindingFlags.Instance | BindingFlags.Public))
            {
                if (MemberType(inner) is { } innerType && IsBindable(innerType) && IsWritable(inner))
                {
                    paths.Add($"{member.Name}.{inner.Name}");
                }
            }
        }

        paths.Sort(StringComparer.OrdinalIgnoreCase);
        return paths.ToArray();
    }

    //the kinds a binding resolves to; anything else has no value to write
    private static bool IsBindable(Type type)
        => type == typeof(string) || type == typeof(bool) || DataFieldReflector.IsNumeric(type);

    private static Type? MemberType(MemberInfo member) => member switch
    {
        FieldInfo field => field.FieldType,
        PropertyInfo property when property.GetIndexParameters().Length == 0 => property.PropertyType,
        _ => null
    };

    private static bool IsWritable(MemberInfo member) => member switch
    {
        FieldInfo field => !field.IsInitOnly,
        PropertyInfo property => property.CanWrite,
        _ => false
    };
}
