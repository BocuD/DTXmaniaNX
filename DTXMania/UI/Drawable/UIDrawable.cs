using System.Numerics;
using DTXMania.UI;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using SharpDX;
using Quaternion = SharpDX.Quaternion;
using Vector2 = SharpDX.Vector2;
using Vector3 = SharpDX.Vector3;

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

    public virtual void DrawInspector()
    {
        ImGui.Text(GetType().Name);
        ImGui.InputInt("Render Order", ref renderOrder);
        Inspector.Inspect("Position", ref position);
        Inspector.Inspect("Anchor", ref anchor);
        Inspector.Inspect("Size", ref size);
        Inspector.Inspect("Scale", ref scale);
        Inspector.Inspect("Rotation", ref rotation);
        ImGui.Checkbox("Is Visible", ref isVisible);
        
        //draw imguizmo
        Matrix4x4 view = Matrix.LookAtLH(new Vector3(0, 0, -1), new Vector3(0, 0, 0), new Vector3(0, 1, 0)).ToMatrix4x4();
        
        //create orthographic "projection" matrix
        var gizmoRect = InspectorManager.gizmoRect;
        Matrix4x4 projection = Matrix.OrthoLH(gizmoRect.Width, -gizmoRect.Height, -1, 1).ToMatrix4x4();
        
        //apply screen space transform to view matrix
        Vector2 windowSize = new(gizmoRect.Width, gizmoRect.Height);
        Vector2 centerOffset = -windowSize / 2f;
        
        //apply window size and position to view matrix
        var screenSpaceTransform = Matrix4x4.CreateTranslation(centerOffset.X, centerOffset.Y, 0);// * scaleMatrix;
        Matrix4x4.Invert(screenSpaceTransform, out Matrix4x4 inverseScreenSpaceTransform);
        
        //construct transform matrix
        Matrix translationMatrix = Matrix.Translation(position);
        Matrix rotationMatrix = Matrix.RotationYawPitchRoll(rotation.Y, rotation.X, rotation.Z);
        Matrix scaleMatrix = Matrix.Scaling(scale);
        Matrix anchorMatrix = Matrix.Identity; //Matrix.Translation(anchorOffset);

        //combine transformations (but skip anchor, since we don't want it to affect the gizmo)
        Matrix transform = (scaleMatrix * rotationMatrix * anchorMatrix * translationMatrix);
        
        //construct parent matrix
        int iterations = 0;
        Matrix parentMatrix = Matrix.Identity;
        UIGroup? currentParent = parent;
        while (currentParent != null && iterations < 100)
        {
            parentMatrix *= currentParent.localTransformMatrix;
            currentParent = currentParent.parent;
            iterations++;
        }
        
        Matrix combined = transform * parentMatrix;
        Matrix4x4 transform4x4 = combined.ToMatrix4x4();
        
        //apply scale
        transform4x4 *= screenSpaceTransform;
        
        Matrix4x4 deltaMatrix = Matrix4x4.Identity;

        ImGuizmoOperation operations = ImGuizmoOperation.TranslateX | ImGuizmoOperation.TranslateY |
                                       ImGuizmoOperation.RotateZ | 
                                       ImGuizmoOperation.ScaleX | ImGuizmoOperation.ScaleY;
        
        if (ImGuizmo.Manipulate(ref view, ref projection, operations, ImGuizmoMode.World, ref transform4x4, ref deltaMatrix))
        {
            var mat = (transform4x4 * inverseScreenSpaceTransform);
            
            //remove parent transform
            parentMatrix.Invert();
            
            //update local transform matrix
            localTransformMatrix = mat.ToMatrix() * parentMatrix;
            
            mat.ToMatrix().Decompose(out scale, out Quaternion rot, out position);
            
            //update position, rotation, scale
            position = localTransformMatrix.TranslationVector;
            
            //update rotation: convert quaternion to euler angles
            //for now we only care about z
            rotation.Z = (float)MathF.Atan2(2 * (rot.W * rot.Z + rot.X * rot.Y), 1 - 2 * (rot.Y * rot.Y + rot.Z * rot.Z));
        }

        if (ImGuizmo.IsUsing())
        {
            //mutliply with inverse
            localTransformMatrix *= (deltaMatrix * inverseScreenSpaceTransform).ToMatrix();
        }
    }
}