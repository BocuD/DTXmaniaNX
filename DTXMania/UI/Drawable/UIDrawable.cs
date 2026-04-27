using System.Numerics;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Newtonsoft.Json;

namespace DTXMania.UI.Drawable;

public abstract class UIDrawable : IDisposable
{
    public string id;
    public string type => GetType().FullName ?? GetType().Name;
    [Themable] public int renderOrder = 0;
    [Themable] public Vector3 position = Vector3.Zero;
    [Themable] public Vector2 anchor = Vector2.Zero;
    [Themable] public Vector2 size = Vector2.One;
    [Themable] public Vector3 scale = Vector3.One;
    [Themable] public Vector3 rotation = Vector3.Zero;
    [Themable] public string name = string.Empty;
    [Themable] public bool isVisible = true;
    public bool dontSerialize = false;

    protected Matrix4x4 localTransformMatrix = Matrix4x4.Identity;

    [JsonIgnore]
    public UIGroup? parent { get; private set; }

    protected UIDrawable()
    {
        id = Guid.NewGuid().ToString();
        DrawableTracker.Register(this);
    }

    public void UpdateLocalTransformMatrix()
    {
        Vector3 anchorOffset = new(-anchor.X * size.X, -anchor.Y * size.Y, 0f);
        Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(position);
        Matrix4x4 rotationMatrix = Matrix4x4.CreateFromYawPitchRoll(rotation.Y, rotation.X, rotation.Z);
        Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(scale);
        Matrix4x4 anchorMatrix = Matrix4x4.CreateTranslation(anchorOffset * scale);
        localTransformMatrix = scaleMatrix * anchorMatrix * rotationMatrix * translationMatrix;
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

    public abstract void Draw(Matrix4x4 parentMatrix);

    public virtual void Dispose()
    {
        DrawableTracker.Remove(this);
    }

    public virtual void OnDeserialize()
    {
        DrawableTracker.Register(this);
    }

    public Matrix4x4 GetFullTransformMatrix()
    {
        Matrix4x4 combined = localTransformMatrix;
        UIGroup? currentParent = parent;
        int iterations = 0;

        while (currentParent != null && iterations < 100)
        {
            combined *= currentParent.localTransformMatrix;
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
        Inspector.Inspector.Inspect("Position", ref position);
        Inspector.Inspector.Inspect("Anchor", ref anchor);
        Inspector.Inspector.Inspect("Size", ref size);
        Inspector.Inspector.Inspect("Scale", ref scale);
        Inspector.Inspector.Inspect("Rotation", ref rotation);
        ImGui.Checkbox("Is Visible", ref isVisible);
        ImGui.TextColored(new Vector4(1f, 0.5f, 0f, 1f), "Warning: Enabling 'NonSerialized' will prevent this drawable from being saved in the UI layout.");
        ImGui.Checkbox("NonSerialized", ref dontSerialize);
    }

    public void DrawTransformGizmo()
    {
        var gizmoRect = InspectorManager.gizmoRect;
        if (gizmoRect.Width <= 0 || gizmoRect.Height <= 0)
        {
            return;
        }

        DrawBoundsGizmo();

        Matrix4x4 viewMatrix = InspectorManager.GetViewMatrix();
        Matrix4x4 projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, gizmoRect.Width, gizmoRect.Height, 0, -1f, 1f);

        Matrix4x4 localWithoutAnchor =
            Matrix4x4.CreateScale(scale) *
            Matrix4x4.CreateFromYawPitchRoll(rotation.Y, rotation.X, rotation.Z) *
            Matrix4x4.CreateTranslation(position);

        Matrix4x4 parentMatrix = parent?.GetFullTransformMatrix() ?? Matrix4x4.Identity;
        Matrix4x4 worldMatrix = localWithoutAnchor * parentMatrix;
        Matrix4x4 deltaMatrix = Matrix4x4.Identity;

        ImGuizmoOperation operations =
            ImGuizmoOperation.TranslateX | ImGuizmoOperation.TranslateY |
            ImGuizmoOperation.RotateZ |
            ImGuizmoOperation.ScaleX | ImGuizmoOperation.ScaleY;

        if (ImGuizmo.Manipulate(ref viewMatrix, ref projectionMatrix, operations, ImGuizmoMode.World, ref worldMatrix, ref deltaMatrix))
        {
            if (!Matrix4x4.Invert(parentMatrix, out Matrix4x4 inverseParent))
            {
                inverseParent = Matrix4x4.Identity;
            }

            Matrix4x4 localMatrix = worldMatrix * inverseParent;
            if (Matrix4x4.Decompose(localMatrix, out Vector3 newScale, out Quaternion newRotation, out Vector3 newPosition))
            {
                scale = newScale;
                position = newPosition;
                rotation = QuaternionToEuler(newRotation);
            }
        }
    }

    private void DrawBoundsGizmo()
    {
        Vector3 quadTopLeft = new(0, 0, 0);
        Vector3 quadTopRight = new(size.X, 0, 0);
        Vector3 quadBottomLeft = new(0, size.Y, 0);
        Vector3 quadBottomRight = new(size.X, size.Y, 0);

        Matrix4x4 transform = GetFullTransformMatrix();

        Vector3 worldTopLeft = Vector3.Transform(quadTopLeft, transform);
        Vector3 worldTopRight = Vector3.Transform(quadTopRight, transform);
        Vector3 worldBottomLeft = Vector3.Transform(quadBottomLeft, transform);
        Vector3 worldBottomRight = Vector3.Transform(quadBottomRight, transform);

        InspectorManager.DrawGizmoQuad(
            new Vector2(worldTopLeft.X, worldTopLeft.Y),
            new Vector2(worldTopRight.X, worldTopRight.Y),
            new Vector2(worldBottomLeft.X, worldBottomLeft.Y),
            new Vector2(worldBottomRight.X, worldBottomRight.Y),
            0xFF00FF00);

        Vector3 center = (quadTopLeft + quadTopRight + quadBottomLeft + quadBottomRight) / 4f;
        Vector3 transformedCenter = Vector3.Transform(center, transform);
        InspectorManager.DrawGizmoPoint(new Vector2(transformedCenter.X, transformedCenter.Y), 15, 0xFFFF0000, 2.5f);

        Vector3 anchorPoint = new(anchor.X * size.X, anchor.Y * size.Y, 0f);
        Vector3 transformedAnchor = Vector3.Transform(anchorPoint, transform);
        InspectorManager.DrawGizmoPoint(new Vector2(transformedAnchor.X, transformedAnchor.Y), 20, 0xFF0000FF, 2.5f);
    }

    private static Vector3 QuaternionToEuler(Quaternion quaternion)
    {
        Vector3 angles = Vector3.Zero;

        float sinrCosp = 2f * (quaternion.W * quaternion.X + quaternion.Y * quaternion.Z);
        float cosrCosp = 1f - 2f * (quaternion.X * quaternion.X + quaternion.Y * quaternion.Y);
        angles.X = MathF.Atan2(sinrCosp, cosrCosp);

        float sinp = 2f * (quaternion.W * quaternion.Y - quaternion.Z * quaternion.X);
        angles.Y = MathF.Abs(sinp) >= 1f ? MathF.CopySign(MathF.PI / 2f, sinp) : MathF.Asin(sinp);

        float sinyCosp = 2f * (quaternion.W * quaternion.Z + quaternion.X * quaternion.Y);
        float cosyCosp = 1f - 2f * (quaternion.Y * quaternion.Y + quaternion.Z * quaternion.Z);
        angles.Z = MathF.Atan2(sinyCosp, cosyCosp);

        return angles;
    }
}
