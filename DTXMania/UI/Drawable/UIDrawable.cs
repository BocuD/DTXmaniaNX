using System.Numerics;
using DTXUIRenderer;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Newtonsoft.Json;
using SharpDX;
using Quaternion = SharpDX.Quaternion;
using Vector2 = SharpDX.Vector2;
using Vector3 = SharpDX.Vector3;

namespace DTXMania.UI.Drawable;

public abstract class UIDrawable : IDisposable
{
    public string type => GetType().FullName;
    public int renderOrder = 0;
    public Vector3 position = Vector3.Zero;
    public Vector2 anchor = Vector2.Zero; //pivot in 2D space
    public Vector2 size = Vector2.One;    //scale in 3D space
    public Vector3 scale = Vector3.One;
    public Vector3 rotation = Vector3.Zero; //euler angles in radians (X = pitch, Y = yaw, Z = roll)

    public string name = "";
    
    public bool isVisible = true;
        
    protected Matrix localTransformMatrix = Matrix.Identity;
    
    [JsonIgnore] public UIGroup? parent { get; private set; } = null;

    public void UpdateLocalTransformMatrix()
    {
        Vector3 anchorOffset = new(-anchor.X * size.X, -anchor.Y * size.Y, 0);
            
        Matrix translationMatrix = Matrix.Translation(position);
        Matrix rotationMatrix = Matrix.RotationYawPitchRoll(rotation.Y, rotation.X, rotation.Z);
        Matrix scaleMatrix = Matrix.Scaling(scale);
        Matrix anchorMatrix = Matrix.Translation(anchorOffset);

        //combine transformations: anchor * scale * rotation * translation
        localTransformMatrix = scaleMatrix * anchorMatrix * rotationMatrix * translationMatrix;
        //localTransformMatrix = scaleMatrix * rotationMatrix * anchorMatrix * translationMatrix;
    }

    public void SetParent(UIGroup? newParent, bool updateGroup = true)
    {
        if (updateGroup)
        {
            parent?.RemoveChild(this);
            newParent?.AddChild(this, false);
        }

        parent = newParent;
    }

    public abstract void Draw(Matrix parentMatrix);
        
    public abstract void Dispose();
    
    public Matrix GetFullTransformMatrix()
    {
        Matrix combined = localTransformMatrix;
        UIGroup? currentParent = parent;
        int iterations = 0;
        while (currentParent != null && iterations < 100)
        {
            combined *= currentParent.localTransformMatrix; // LOCAL * PARENT
            currentParent = currentParent.parent;
            iterations++;
        }

        return combined;
    }

    public virtual void DrawInspector()
    {
        ImGui.Text(string.IsNullOrWhiteSpace(name) ? GetType().Name : name);
        ImGui.SameLine();
        string renameId = GetHashCode() + "Rename";
        if (ImGui.Button("Rename"))
        {
            ImGui.OpenPopup(renameId);
        }
        
        if (ImGui.BeginPopup(renameId))
        {
            ImGui.InputText("Name", ref name, 256);
            if (ImGui.Button("OK"))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
        
        ImGui.InputInt("Render Order", ref renderOrder);
        Inspector.Inspect("Position", ref position);
        Inspector.Inspect("Anchor", ref anchor);
        Inspector.Inspect("Size", ref size);
        Inspector.Inspect("Scale", ref scale);
        Inspector.Inspect("Rotation", ref rotation);
        ImGui.Checkbox("Is Visible", ref isVisible);
        
        DrawTransformGizmo();
        
        var gizmoRect = InspectorManager.gizmoRect;
        
        // Get the view matrix (camera transform) from GameWindow
        Matrix4x4 view = InspectorManager.GetViewMatrix();
        
        view *= Matrix4x4.CreateScale(1, -1, 1); // flip Y axis
        
        Matrix4x4 screenOffset = Matrix4x4.CreateTranslation(new System.Numerics.Vector3(-gizmoRect.Width / 2.0f, gizmoRect.Height / 2.0f, 0));
        view *= screenOffset;
        
        // Center-origin orthographic projection
        float width = gizmoRect.Width;
        float height = gizmoRect.Height;
        Matrix4x4 projection = Matrix4x4.CreateOrthographic(width, height, -1f, 1f);
        
        //construct transform matrix
        Matrix translationMatrix = Matrix.Translation(position);
        Matrix rotationMatrix = Matrix.RotationYawPitchRoll(rotation.Y, rotation.X, rotation.Z);
        Matrix scaleMatrix = Matrix.Scaling(scale);
        Matrix anchorMatrix = Matrix.Identity; //Matrix.Translation(anchorOffset);

        //combine transformations (but skip anchor, since we don't want it to affect the gizmo)
        Matrix transform = (scaleMatrix * rotationMatrix * anchorMatrix * translationMatrix);
        
        //construct parent matrix
        Matrix parentMatrix = parent?.GetFullTransformMatrix() ?? Matrix.Identity;
        
        Matrix combined = transform * parentMatrix;
        Matrix4x4 transform4x4 = combined.ToMatrix4x4();
        
        Matrix4x4 deltaMatrix = Matrix4x4.Identity;

        ImGuizmoOperation operations = ImGuizmoOperation.TranslateX | ImGuizmoOperation.TranslateY |
                                       ImGuizmoOperation.RotateZ | 
                                       ImGuizmoOperation.ScaleX | ImGuizmoOperation.ScaleY;
        
        if (ImGuizmo.Manipulate(ref view, ref projection, operations, ImGuizmoMode.World, ref transform4x4, ref deltaMatrix))
        {
            var mat = (transform4x4);// * inverseScreenSpaceTransform);
            
            //remove parent transform
            parentMatrix.Invert();
            
            //update local transform matrix
            localTransformMatrix = mat.ToMatrix() * parentMatrix;
            
            localTransformMatrix.Decompose(out scale, out Quaternion rot, out position);
            
            //update rotation: convert quaternion to euler angles
            //for now we only care about z
            rotation.Z = MathF.Atan2(2 * (rot.W * rot.Z + rot.X * rot.Y), 1 - 2 * (rot.Y * rot.Y + rot.Z * rot.Z));
        }

        if (ImGuizmo.IsUsing())
        {
            //mutliply with inverse
            localTransformMatrix *= (deltaMatrix).ToMatrix();
        }
    }

    private void DrawTransformGizmo()
    {
        // Create a quad in local 3D space (Z=0)
        Vector3 quadTopLeft = new(0, 0, 0);
        Vector3 quadTopRight = new(size.X, 0, 0);
        Vector3 quadBottomLeft = new(0, size.Y, 0);
        Vector3 quadBottomRight = new(size.X, size.Y, 0);

        // Full world transform
        Matrix t = GetFullTransformMatrix();

        // Transform points using full matrix
        Vector3.TransformCoordinate(ref quadTopLeft, ref t, out Vector3 worldTopLeft);
        Vector3.TransformCoordinate(ref quadTopRight, ref t, out Vector3 worldTopRight);
        Vector3.TransformCoordinate(ref quadBottomLeft, ref t, out Vector3 worldBottomLeft);
        Vector3.TransformCoordinate(ref quadBottomRight, ref t, out Vector3 worldBottomRight);

        // Draw the quad in 2D screen space (ignore Z after transformation)
        InspectorManager.DrawGizmoQuad(
            new Vector2(worldTopLeft.X, worldTopLeft.Y),
            new Vector2(worldTopRight.X, worldTopRight.Y),
            new Vector2(worldBottomLeft.X, worldBottomLeft.Y),
            new Vector2(worldBottomRight.X, worldBottomRight.Y),
            0xFF00FF00);

        // Draw the center point
        Vector3 center = (quadTopLeft + quadTopRight + quadBottomLeft + quadBottomRight) / 4;
        Vector3.TransformCoordinate(ref center, ref t, out Vector3 transformedCenter);
        InspectorManager.DrawGizmoPoint(new Vector2(transformedCenter.X, transformedCenter.Y), 15, 0xFFFF0000, 2.5f);

        // Draw anchor point
        Vector3 anchorPoint = new(anchor.X * size.X, anchor.Y * size.Y, 0);
        Vector3.TransformCoordinate(ref anchorPoint, ref t, out Vector3 transformedAnchor);
        InspectorManager.DrawGizmoPoint(new Vector2(transformedAnchor.X, transformedAnchor.Y), 20, 0xFF0000FF, 2.5f);
    }
}