using Hexa.NET.ImGui;
using SharpDX;
using Color = System.Drawing.Color;

namespace DTXUIRenderer;

public class Inspector
{
    internal static UIDrawable? inspectorTarget;

    public void Draw()
    {
        try
        {
            ImGui.Begin("Inspector");
            
            if (inspectorTarget != null)
            {
                inspectorTarget.DrawInspector();
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
}