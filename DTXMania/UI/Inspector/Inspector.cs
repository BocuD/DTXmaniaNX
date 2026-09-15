using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using Hexa.NET.ImGui;
using Color = System.Drawing.Color;

namespace DTXMania.UI.Inspector;

public class Inspector
{
    internal static DrawableRef inspectorTarget = DrawableRef.None;
    internal static DrawableRef dragDropPayload = DrawableRef.None;

    //a dotted binding key ("Song.Chart.SongInformation.Genre") becomes nested nodes; intermediate
    //segments get collapsible headers, leaves show their live value
    private sealed class ContextTreeNode
    {
        public readonly SortedDictionary<string, ContextTreeNode> Children = new(StringComparer.Ordinal);
        public string? Key;
        public bool IsTexture;
    }

    public static void DrawDataContextTree(IUIDataContext context)
    {
        ContextTreeNode root = new();
        foreach (string key in context.AvailableKeys(DataBindingKind.String))
        {
            Insert(root, key, isTexture: false);
        }

        foreach (string key in context.AvailableKeys(DataBindingKind.Texture))
        {
            Insert(root, key, isTexture: true);
        }

        RenderContextNode(root, context, string.Empty);
    }

    private static void Insert(ContextTreeNode root, string key, bool isTexture)
    {
        ContextTreeNode node = root;
        foreach (string part in key.Split('.'))
        {
            if (!node.Children.TryGetValue(part, out ContextTreeNode? child))
            {
                child = new ContextTreeNode();
                node.Children[part] = child;
            }

            node = child;
        }

        node.Key = key;
        node.IsTexture = isTexture;
    }

    private static void RenderContextNode(ContextTreeNode node, IUIDataContext context, string path)
    {
        foreach ((string name, ContextTreeNode child) in node.Children)
        {
            string childPath = $"{path}.{name}";

            if (child.Children.Count > 0)
            {
                if (ImGui.TreeNode($"{name}##{childPath}"))
                {
                    RenderContextNode(child, context, childPath);
                    ImGui.TreePop();
                }
            }
            else if (child.IsTexture)
            {
                ImGui.BulletText($"{name}  (texture)");
            }
            else
            {
                context.TryGetString(child.Key ?? name, out string value);
                ImGui.BulletText($"{name} = \"{value}\"");
            }
        }
    }

    /// <summary>
    /// Binding-key picker for a dynamic source: a dropdown of the keys reachable from <paramref name="element"/>
    /// filtered to <paramref name="kind"/>, plus an editable field for concrete indices
    /// (<c>Info.BestRank[0]</c>), <c>:format</c> suffixes, or hand-typed keys. "(none)" clears the binding.
    /// </summary>
    public static bool DrawBindingDropdown(string label, ref string value, UIDrawable element, DataBindingKind kind)
    {
        List<string> options = ["(none)"];
        HashSet<string> seen = new();

        foreach (IUIDataContext context in element.DataContexts())
        {
            foreach (string key in context.AvailableKeys(kind))
            {
                if (seen.Add(key)) options.Add(key);
            }
        }

        //keep the current value selectable when its context isn't live in the editor, or it's an
        //indexed / :format variant of an enumerated template
        if (!string.IsNullOrEmpty(value) && !options.Contains(value))
        {
            options.Add(value);
        }

        options.RemoveAt(0);

        return PathPicker.Draw(label, ref value, options);
    }

    public void Draw()
    {
        try
        {
            ImGui.Begin("Inspector", ImGuiWindowFlags.NoFocusOnAppearing);

            if (inspectorTarget.Target is { } drawable)
            {
                drawable.DrawInspector();
            }
            else
            {
                ImGui.Text("No target selected");
            }
        }
        finally
        {
            ImGui.End();
        }
    }

    public static bool Inspect(string label, ref Vector2 vector)
    {
        Vector2 v = vector;
        bool changed = ImGui.InputFloat2(label, ref v);
        vector = v;
        return changed;
    }

    /// <summary>One field per axis, so an axis something else drives can be greyed out on its own.</summary>
    public static bool InspectAxes(string label, ref Vector3 vector, bool xDriven, bool yDriven)
    {
        BeginAxes(label, 3);
        bool changed = Axis(0, ref vector.X, xDriven);
        changed |= Axis(1, ref vector.Y, yDriven);
        changed |= Axis(2, ref vector.Z, false);
        EndAxes(label);
        return changed;
    }

    public static bool InspectAxes(string label, ref Vector2 vector, bool xDriven, bool yDriven)
    {
        BeginAxes(label, 2);
        bool changed = Axis(0, ref vector.X, xDriven);
        changed |= Axis(1, ref vector.Y, yDriven);
        EndAxes(label);
        return changed;
    }

    private static readonly float[] AnchorStops = [0.0f, 0.5f, 1.0f];

    /// <summary>Axis fields with a button on the left that picks one of the nine stops.</summary>
    public static bool AnchorField(string label, ref Vector2 value, bool xDriven, bool yDriven)
    {
        ImGui.PushID(label);

        float button = ImGui.GetFrameHeight();

        ImGui.BeginDisabled(xDriven && yDriven);

        if (AnchorButton(value, button))
        {
            ImGui.OpenPopup("stops");
        }

        ImGui.EndDisabled();

        bool changed = AnchorStopsPopup(ref value);
        ImGui.SameLine(0.0f, ImGui.GetStyle().ItemInnerSpacing.X);

        BeginAxes(label, 2, button + ImGui.GetStyle().ItemInnerSpacing.X);
        changed |= Axis(0, ref value.X, xDriven);
        changed |= Axis(1, ref value.Y, yDriven);
        EndAxes(label);

        ImGui.PopID();

        return changed;
    }

    private static bool AnchorButton(Vector2 value, float size)
    {
        Vector2 origin = ImGui.GetCursorScreenPos();
        bool pressed = ImGui.Button("##stops", new Vector2(size, size));

        ImDrawListPtr draw = ImGui.GetWindowDrawList();
        float step = size / 4.0f;
        float dot = MathF.Max(1.5f, size / 12.0f);

        for (int y = 0; y < AnchorStops.Length; y++)
        {
            for (int x = 0; x < AnchorStops.Length; x++)
            {
                Vector2 centre = origin + new Vector2(step * (x + 1), step * (y + 1));
                bool active = value == new Vector2(AnchorStops[x], AnchorStops[y]);

                draw.AddRectFilled(centre - new Vector2(dot), centre + new Vector2(dot),
                    ImGui.GetColorU32(active ? ImGuiCol.CheckMark : ImGuiCol.TextDisabled));
            }
        }

        return pressed;
    }

    private static bool AnchorStopsPopup(ref Vector2 value)
    {
        if (!ImGui.BeginPopup("stops"))
        {
            return false;
        }

        bool changed = false;

        for (int y = 0; y < AnchorStops.Length; y++)
        {
            for (int x = 0; x < AnchorStops.Length; x++)
            {
                Vector2 stop = new(AnchorStops[x], AnchorStops[y]);
                bool active = value == stop;

                if (x > 0)
                {
                    ImGui.SameLine();
                }

                if (active)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.16f, 0.5f, 0.22f, 1.0f));
                }

                if (ImGui.Button($"##stop{x}{y}", new Vector2(24, 24)))
                {
                    value = stop;
                    changed = true;
                    ImGui.CloseCurrentPopup();
                }

                if (active)
                {
                    ImGui.PopStyleColor();
                }
            }
        }

        ImGui.EndPopup();

        return changed;
    }

    private static void BeginAxes(string label, int fields, float usedWidth = 0.0f)
    {
        ImGui.PushID(label);
        float spacing = ImGui.GetStyle().ItemInnerSpacing.X;
        ImGui.PushItemWidth((ImGui.CalcItemWidth() - usedWidth - spacing * (fields - 1)) / fields);
    }

    private static bool Axis(int index, ref float value, bool driven)
    {
        if (index > 0)
        {
            ImGui.SameLine(0f, ImGui.GetStyle().ItemInnerSpacing.X);
        }

        ImGui.BeginDisabled(driven);
        bool changed = ImGui.InputFloat($"##axis{index}", ref value);
        ImGui.EndDisabled();
        return changed;
    }

    private static void EndAxes(string label)
    {
        ImGui.PopItemWidth();
        ImGui.SameLine(0f, ImGui.GetStyle().ItemInnerSpacing.X);
        ImGui.Text(label);
        ImGui.PopID();
    }

    public static bool Inspect(string label, ref UISize size)
    {
        Vector2 v = size;
        bool changed = ImGui.InputFloat2(label, ref v);

        //InputFloat2 reports a change for either field, so writing both would claim an untouched axis
        if (changed)
        {
            if (v.X != size.X)
            {
                size.X = v.X;
            }

            if (v.Y != size.Y)
            {
                size.Y = v.Y;
            }
        }

        ImGui.PushItemWidth(ImGui.CalcItemWidth() * 0.5f - 4f);
        changed |= Inspect($"##{label}X", ref size.xMode);
        ImGui.SameLine();
        changed |= Inspect($"##{label}Y", ref size.yMode);
        ImGui.PopItemWidth();

        //typing a size claims the axis, which takes its pair away again
        if (size.xMode == UiSizeMode.Inherit)
        {
            Vector2 horizontal = new(size.marginLeft, size.marginRight);

            if (ImGui.InputFloat2($"{label} Margin L/R", ref horizontal))
            {
                size.marginLeft = horizontal.X;
                size.marginRight = horizontal.Y;
                changed = true;
            }
        }

        if (size.yMode == UiSizeMode.Inherit)
        {
            Vector2 vertical = new(size.marginTop, size.marginBottom);

            if (ImGui.InputFloat2($"{label} Margin T/B", ref vertical))
            {
                size.marginTop = vertical.X;
                size.marginBottom = vertical.Y;
                changed = true;
            }
        }

        return changed;
    }

    public static bool Inspect(string label, ref Vector3 vector)
    {
        Vector3 v = vector;
        bool changed = ImGui.InputFloat3(label, ref v);
        vector = v;
        return changed;
    }

    //shown in degrees, stored in radians
    public static bool InspectAngles(string label, ref Vector3 radians)
    {
        Vector3 degrees = radians * (180.0f / MathF.PI);
        if (!ImGui.InputFloat3(label, ref degrees))
        {
            return false;
        }

        radians = degrees * (MathF.PI / 180.0f);
        return true;
    }

    public static bool Inspect(string label, ref RectangleF vector)
    {
        Vector4 v = new(vector.X, vector.Y, vector.Width, vector.Height);
        bool changed = ImGui.InputFloat4(label, ref v);
        vector = new RectangleF(v.X, v.Y, v.Z, v.W);
        return changed;
    }

    public static bool Inspect(string label, ref Color vector)
    {
        Vector4 v = new(vector.R / 255f, vector.G / 255f, vector.B / 255f, vector.A / 255f);
        bool changed = ImGui.ColorEdit4(label, ref v);
        if (changed)
        {
            vector = Color.FromArgb((int)(v.W * 255), (int)(v.X * 255), (int)(v.Y * 255), (int)(v.Z * 255));
        }

        return changed;
    }

    public static bool Inspect(string label, ref Color4 vector)
    {
        Vector4 v = vector.ToVector4();
        bool changed = ImGui.ColorEdit4(label, ref v);
        if (changed)
        {
            vector = new Color4(v.X, v.Y, v.Z, v.W);
        }

        return changed;
    }

    public static bool Inspect<T>(string label, ref T value) where T : Enum
    {
        int currentValue = Convert.ToInt32(value);
        string options = Enum.GetNames(typeof(T)).Aggregate((a, b) => $"{a}\0{b}");
        bool changed = ImGui.Combo(label, ref currentValue, options);
        if (changed)
        {
            value = (T)Enum.ToObject(typeof(T), currentValue);
        }

        return changed;
    }
}
