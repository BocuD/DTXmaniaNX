using System.Drawing;
using System.Numerics;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using Hexa.NET.ImGui;
using Color = System.Drawing.Color;

namespace DTXMania.UI.Inspector;

public class Inspector
{
    internal static string inspectorTarget = string.Empty;
    internal static string dragDropPayload = string.Empty;
    internal static Type dragDropType = typeof(UIDrawable);

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

    public static string GetDrawableDragDropType(Type t)
    {
        return "UIDrawable" + t.Name;
    }

    public static bool Inspect(string label, ref Vector2 vector)
    {
        Vector2 v = vector;
        bool changed = ImGui.InputFloat2(label, ref v);
        vector = v;
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

    public static bool Inspect<T>(string label, ref DrawableReference<T> value) where T : UIDrawable
    {
        T? currentValue = value.Get();
        string name = currentValue?.name ?? currentValue?.GetType().Name ?? "null";

        ImGui.Text(label);
        ImGui.SameLine();
        ImGui.Text(name);

        Vector2 min = ImGui.GetItemRectMin();
        Vector2 max = ImGui.GetItemRectMax();
        ImDrawListPtr drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(min, max, ImGui.GetColorU32(ImGuiCol.BorderShadow), 5);

        bool modified = false;

        if (ImGui.BeginDragDropTarget())
        {
            ImGuiPayloadPtr ptr = ImGui.AcceptDragDropPayload(nameof(UIDrawable));
            if (!ptr.IsNull)
            {
                value = new DrawableReference<T>(dragDropPayload);
                modified = true;
            }

            ImGui.EndDragDropTarget();
        }

        if (value.Get() != null)
        {
            ImGui.SameLine();
            string id = value.Get()!.GetHashCode().ToString();
            if (ImGui.Button($"Select##{id}"))
            {
                inspectorTarget = value.Get()!.id;
            }
        }

        return modified;
    }
}
