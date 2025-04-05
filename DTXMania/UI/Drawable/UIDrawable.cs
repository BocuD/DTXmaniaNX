using System.Numerics;
using DTXMania.UI;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using SharpDX;
using Quaternion = SharpDX.Quaternion;
using Vector2 = SharpDX.Vector2;
using Vector3 = SharpDX.Vector3;
using Vector4 = SharpDX.Vector4;

namespace DTXUIRenderer;

public abstract class UIDrawable : IDisposable
{
    public int renderOrder = 0;
    public Vector3 position = Vector3.Zero;
    public Vector2 anchor = Vector2.Zero; //pivot in 2D space
    public Vector2 size = Vector2.One;    //scale in 3D space
    public Vector3 scale = Vector3.One;
    public Vector3 rotation = Vector3.Zero; //euler angles in radians (X = pitch, Y = yaw, Z = roll)

    public string name = "";
    
    public bool isVisible = true;
        
    protected Matrix localTransformMatrix = Matrix.Identity;
    
    public UIGroup? parent { get; private set; } = null;

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
        int iterations = 0;
        Matrix parentMatrix = Matrix.Identity;
        UIGroup? currentParent = parent;
        while (currentParent != null && iterations < 100)
        {
            parentMatrix *= currentParent.localTransformMatrix;
            currentParent = currentParent.parent;
            iterations++;
        }
        
        return parentMatrix * localTransformMatrix;
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
        Matrix4x4 view = GameWindow.GetViewMatrix();
        
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
        //create a quad in local space
        Vector2 quadTopLeft = Vector2.Zero;
        Vector2 quadTopRight = new(size.X, 0);
        Vector2 quadBottomLeft = new(0, size.Y);
        Vector2 quadBottomRight = size;
        
        //transform the quad to world space
        Matrix t = GetFullTransformMatrix();
        Vector2.Transform(ref quadTopLeft, ref t, out Vector4 topLeft);
        Vector2.Transform(ref quadTopRight, ref t, out Vector4 topRight);
        Vector2.Transform(ref quadBottomLeft, ref t, out Vector4 bottomLeft);
        Vector2.Transform(ref quadBottomRight, ref t, out Vector4 bottomRight);
        
        //draw the quad
        InspectorManager.DrawGizmoQuad(
            new Vector2(topLeft.X, topLeft.Y),
            new Vector2(topRight.X, topRight.Y),
            new Vector2(bottomLeft.X, bottomLeft.Y),
            new Vector2(bottomRight.X, bottomRight.Y),
            0xFF00FF00);
        
        //draw point at the center
        InspectorManager.DrawGizmoPoint(new Vector2(position.X, position.Y), 0xFFFF0000);
    }
}