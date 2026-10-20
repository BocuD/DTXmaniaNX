using System.Numerics;
using DTXMania.UI.DynamicElements;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;

namespace DTXMania.UI.Drawable;

public abstract partial class UIDrawable
{
    public virtual void DrawInspector()
    {
        DrawIdentity();

        if (ImGui.CollapsingHeader("Layout", ImGuiTreeNodeFlags.DefaultOpen))
        {
            //an inheriting axis is placed by its margins, so these fields would be lies
            bool xDriven = size.xMode == UiSizeMode.Inherit;
            bool yDriven = size.yMode == UiSizeMode.Inherit;

            Inspector.Inspector.Inspect("Size", ref size);
            Inspector.Inspector.InspectAxes("Position", ref position, xDriven, yDriven);
            Inspector.Inspector.AnchorField("Pivot", ref pivot, false, false);
            Inspector.Inspector.AnchorField("Parent Anchor", ref parentAnchor, xDriven, yDriven);
        }

        if (ImGui.CollapsingHeader("Transform", ImGuiTreeNodeFlags.DefaultOpen))
        {
            Inspector.Inspector.Inspect("Scale", ref scale);
            Inspector.Inspector.InspectAngles("Rotation", ref rotation);
        }

        if (ImGui.CollapsingHeader("Display", ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (ImGui.InputInt("Render Order", ref renderOrder))
            {
                parent?.InvalidateOrder();
            }

            ImGui.Checkbox("Is Visible", ref isVisible);
            ImGui.Checkbox("NonSerialized", ref dontSerialize);
        }

        Inspector.BindingsEditor.Draw(this);
        DrawDataContextSection();
    }

    private void DrawIdentity()
    {
        ImGui.Text(string.IsNullOrWhiteSpace(name) ? GetType().Name : name);
        ImGui.SameLine();
        string renameId = GetHashCode() + "Rename";
        if (ImGui.Button("Rename"))
        {
            ImGui.OpenPopup(renameId);
        }

        Inspector.Inspector.DrawRenamePopup(renameId, this);

        ImGui.TextDisabled(GetType().Name);
    }

    private void DrawDataContextSection()
    {
        IUIDataContext? dataContext = this.DataContexts().FirstOrDefault(c => c != UIDataContext.Global);
        if (dataContext == null)
        {
            return;
        }

        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.4f, 1f, 0.5f, 1f));
        bool open = ImGui.CollapsingHeader("Data Context");
        ImGui.PopStyleColor();

        if (open)
        {
            Inspector.Inspector.DrawDataContextTree(dataContext);
        }
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
            Matrix4x4.CreateTranslation(LocalTranslation);

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

                //the gizmo works in parent space, which is where the pivot offset already is. An
                //inheriting axis keeps its position, since the margins are what place it
                Vector3 dragged = newPosition - ParentAnchorOffset;

                if (size.xMode != UiSizeMode.Inherit)
                {
                    position.X = dragged.X;
                }

                if (size.yMode != UiSizeMode.Inherit)
                {
                    position.Y = dragged.Y;
                }

                position.Z = dragged.Z;
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
        InspectorManager.DrawGizmoPoint(new Vector2(transformedCenter.X, transformedCenter.Y), 15, CentreColour, 2.5f);

        Vector3 pivotPoint = new(pivot.X * size.X, pivot.Y * size.Y, 0f);
        Vector3 transformedPivot = Vector3.Transform(pivotPoint, transform);
        Vector2 pivotScreen = new(transformedPivot.X, transformedPivot.Y);
        InspectorManager.DrawGizmoPoint(pivotScreen, PivotRadius, PivotColour, 2.5f);
        InspectorManager.DrawGizmoText(pivotScreen, "Pivot", PivotColour, PivotRadius + 2.0f);

        if (parent == null)
        {
            return;
        }

        Vector3 anchorPoint = new(parentAnchor.X * parent.size.X, parentAnchor.Y * parent.size.Y, 0f);
        Vector3 transformedAnchor = Vector3.Transform(anchorPoint, parent.GetFullTransformMatrix());
        Vector2 anchorScreen = new(transformedAnchor.X, transformedAnchor.Y);
        InspectorManager.DrawGizmoPoint(anchorScreen, AnchorRadius, AnchorColour, 2.5f);
        InspectorManager.DrawGizmoText(anchorScreen, "Parent Anchor", AnchorColour, -AnchorRadius - ImGui.GetTextLineHeight() - 2.0f);
    }

    private const uint CentreColour = 0xFFBBBBBB;
    private const uint PivotColour = 0xFF44FF44;
    private const uint AnchorColour = 0xFFFF7744;
    private const float PivotRadius = 18.0f;
    private const float AnchorRadius = 26.0f;

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
