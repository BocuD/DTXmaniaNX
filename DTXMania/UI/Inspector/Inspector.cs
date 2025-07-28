using DTXMania.UI.Drawable;
using Hexa.NET.ImGui;
using SharpDX;
using Color = System.Drawing.Color;

namespace DTXMania.UI.Inspector;

public class Inspector
{
    internal static string inspectorTarget;
    internal static string dragDropPayload;
    internal static Type dragDropType;

    public void Draw()
    {
        try
        {
            ImGui.Begin("Inspector");
            
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
        System.Numerics.Vector2 v = new(vector.X, vector.Y);
        
        bool changed = ImGui.InputFloat2(label, ref v);
        
        vector.X = v.X;
        vector.Y = v.Y;

        return changed;
    }

    public static bool Inspect(string label, ref Vector3 vector)
    {
        System.Numerics.Vector3 v = new(vector.X, vector.Y, vector.Z);
        
        bool changed = ImGui.InputFloat3(label, ref v);
        
        vector.X = v.X;
        vector.Y = v.Y;
        vector.Z = v.Z;

        return changed;
    }

    public static bool Inspect(string label, ref RectangleF vector)
    {
        System.Numerics.Vector4 v = new(vector.X, vector.Y, vector.Width, vector.Height);
        
        bool changed = ImGui.InputFloat4(label, ref v);
        
        vector.X = v.X;
        vector.Y = v.Y;
        vector.Width = v.Z;
        vector.Height = v.W;

        return changed;
    }

    public static bool Inspect(string label, ref Color vector)
    {
        System.Numerics.Vector4 v = new(vector.R / 255f, vector.G / 255f, vector.B / 255f, vector.A / 255f);
        
        bool changed = ImGui.ColorEdit4(label, ref v);

        if (changed)
        {
            vector = Color.FromArgb((int)(v.W * 255), (int)(v.X * 255), (int)(v.Y * 255), (int)(v.Z * 255));
        }

        return changed;
    }
    
    //enum inspect
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
    
    //drawablereference inspect
    public static bool Inspect<T>(string label, ref DrawableReference<T> value) where T : UIDrawable
    {
        T? currentValue = value.Get();
        string name = currentValue?.name ?? "null";
        if (string.IsNullOrEmpty(name))
        {
            if (currentValue != null) 
            {
                name = currentValue.GetType().Name;
            }
            else
            {
                name = "null";
            }
        }
        
        ImGui.Text(label);
        ImGui.SameLine();
        ImGui.Text(name);
        
        //draw box around the text
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(min, max, ImGui.GetColorU32(ImGuiCol.BorderShadow), 5);

        bool modified = false;
        
        //drag and drop target
        if (ImGui.BeginDragDropTarget())
        {
            ImGuiPayloadPtr ptr = ImGui.AcceptDragDropPayload(nameof(UIDrawable));
            
            //check the type of the payload
            if (dragDropType == typeof(T))
            
            //check if delivery
            if (ptr.IsNull)
            {
                ImGui.EndDragDropTarget();
                return modified;
            }
            
            string id = dragDropPayload;
            value = new DrawableReference<T>(id);
            modified = true;
            
            ImGui.EndDragDropTarget();
        }

        if (value.Get() != null)
        {
            ImGui.SameLine();
            var id = value.Get().GetHashCode().ToString();
            if (ImGui.Button($"Select##{id}"))
            {
                inspectorTarget = value.Get().id;
            }
        }

        return modified;
    }
}