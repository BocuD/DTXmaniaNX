using System;
using System.Numerics;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using SharpDX;
using Matrix3x2 = System.Numerics.Matrix3x2;
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

    public void UpdateLocalTransformMatrix()
    {
        Vector3 anchorOffset = new(-anchor.X * size.X, -anchor.Y * size.Y, 0);
            
        Matrix translationMatrix = Matrix.Translation(position);
        Matrix rotationMatrix = Matrix.RotationYawPitchRoll(rotation.Y, rotation.X, rotation.Z);
        Matrix scaleMatrix = Matrix.Scaling(scale);
        Matrix anchorMatrix = Matrix.Translation(anchorOffset);

        //combine transformations: anchor * scale * rotation * translation
        localTransformMatrix = scaleMatrix * rotationMatrix * anchorMatrix * translationMatrix;
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
        
        //create orthographic projection matrix
        Matrix4x4 projection = Matrix.OrthoLH(1280, -720, -1, 1).ToMatrix4x4();
        
        //apply screen space transform to view matrix
        Vector2 windowSize = new(1280, 720);
        Vector2 centerOffset = -windowSize / 2f;
        
        //apply window size and position to view matrix
        var screenSpaceTransform = Matrix4x4.CreateTranslation(centerOffset.X, centerOffset.Y, 0);// * scaleMatrix;
        Matrix4x4.Invert(screenSpaceTransform, out Matrix4x4 inverseScreenSpaceTransform);
        
        //transform matrix
        Matrix4x4 transform4x4 = localTransformMatrix.ToMatrix4x4();
        
        //apply scale
        transform4x4 *= screenSpaceTransform;
        
        Matrix4x4 deltaMatrix = Matrix4x4.Identity;

        if (ImGuizmo.Manipulate(ref view, ref projection, ImGuizmoOperation.TranslateX | ImGuizmoOperation.TranslateY | ImGuizmoOperation.RotateZ, ImGuizmoMode.Local,
                ref transform4x4,
                ref deltaMatrix))
        {
            var mat = (transform4x4 * inverseScreenSpaceTransform);
            //update local transform matrix
            localTransformMatrix = mat.ToMatrix();
            
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