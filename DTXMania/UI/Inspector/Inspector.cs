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
    internal static string inspectorTarget = string.Empty;
    internal static string dragDropPayload = string.Empty;

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

        RenderContextNode(root, context);
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

    private static void RenderContextNode(ContextTreeNode node, IUIDataContext context)
    {
        foreach ((string name, ContextTreeNode child) in node.Children)
        {
            if (child.Children.Count > 0)
            {
                if (ImGui.TreeNode(name))
                {
                    RenderContextNode(child, context);
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

            if (!string.IsNullOrEmpty(inspectorTarget))
            {
                UIDrawable? drawable = DrawableTracker.GetDrawable(inspectorTarget);
                if (drawable != null)
                {
                    drawable.DrawInspector();
                }
                else
                {
                    ImGui.Text("Target not found");
                }
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

        return changed;
    }

    public static bool Inspect(string label, ref Vector3 vector)
    {
        Vector3 v = vector;
        bool changed = ImGui.InputFloat3(label, ref v);
        vector = v;
        return changed;
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
