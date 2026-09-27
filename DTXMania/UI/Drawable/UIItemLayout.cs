using System.Numerics;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Drawable;

public sealed class UIItemPath
{
    [Themable] public Vector3 position;
    [Themable] public Vector3 positionStep = new(100.0f, 0.0f, 0.0f);

    //degrees
    [Themable] public Vector3 rotation;
    [Themable] public Vector3 rotationStep;

    [Themable] public Vector3 scale = Vector3.One;
    [Themable] public Vector3 scaleStep;
}

/// <summary>Places a list's items from where the selection sits and the path they take away from it on either side.</summary>
public sealed class UIItemLayout
{
    [Themable] public bool enabled;

    [Themable] public Vector3 selectedPosition;
    [Themable] public Vector3 selectedRotation;
    [Themable] public Vector3 selectedScale = Vector3.One;

    [Themable] [SkinSerialize] public UIItemPath next = new();

    //previous takes next's path, mirrored across the list's own axis
    [Themable] public bool mirrorPrevious = true;
    [Themable] [SkinSerialize] public UIItemPath previous = new();

    private readonly record struct Place(Vector3 Position, Vector3 Rotation, Vector3 Scale);

    /// <summary><paramref name="distance"/> is in items from the selection, fractional while scrolling.</summary>
    public void Apply(UIDrawable item, float distance, Vector3 axis)
    {
        bool isPrevious = distance < 0.0f;
        bool mirrored = isPrevious && mirrorPrevious;
        float steps = MathF.Abs(distance);

        UIItemPath path = isPrevious && !mirrorPrevious ? previous : next;
        Place place = OnPath(path, MathF.Max(steps, 1.0f), MathF.Abs(axis.Y) > MathF.Abs(axis.X), mirrored);

        if (steps < 1.0f)
        {
            Place selected = new(selectedPosition, selectedRotation, selectedScale);
            place = new Place(
                Vector3.Lerp(selected.Position, place.Position, steps),
                Vector3.Lerp(selected.Rotation, place.Rotation, steps),
                Vector3.Lerp(selected.Scale, place.Scale, steps));
        }

        item.position = place.Position;
        item.rotation = Radians(place.Rotation);
        item.scale = place.Scale;
    }

    private static Place OnPath(UIItemPath path, float steps, bool column, bool mirrored)
    {
        Vector3 start = mirrored ? MirrorPosition(path.position, column) : path.position;
        Vector3 stride = mirrored ? MirrorPosition(path.positionStep, column) : path.positionStep;
        Vector3 rotation = mirrored ? MirrorRotation(path.rotation, column) : path.rotation;
        Vector3 spin = mirrored ? MirrorRotation(path.rotationStep, column) : path.rotationStep;

        float along = steps - 1.0f;
        int whole = (int)MathF.Floor(along);

        Vector3 position = start;
        for (int index = 0; index < whole; index++)
        {
            position += Step(stride, rotation, spin, index);
        }

        //a place between two items is the straight line between them
        position += Step(stride, rotation, spin, whole) * (along - whole);

        return new Place(position, rotation + spin * along, path.scale + path.scaleStep * along);
    }

    //a step goes the way the run faces halfway between the two items it joins, which is what curls a run
    //with a rotation step into a wheel
    private static Vector3 Step(Vector3 stride, Vector3 rotation, Vector3 spin, int index)
        => Vector3.Transform(stride, Rotation(rotation + spin * (index + 0.5f)));

    //the other side of the selection is this one reflected across the list's own axis
    private static Vector3 MirrorPosition(Vector3 value, bool column)
        => column ? value with { Y = -value.Y } : value with { X = -value.X };

    //reflecting turns every rotation around except the one about the axis reflected across
    private static Vector3 MirrorRotation(Vector3 value, bool column)
        => column ? new Vector3(-value.X, value.Y, -value.Z) : new Vector3(value.X, -value.Y, -value.Z);

    private static Matrix4x4 Rotation(Vector3 degrees)
        => Matrix4x4.CreateFromYawPitchRoll(Radians(degrees.Y), Radians(degrees.X), Radians(degrees.Z));

    private static Vector3 Radians(Vector3 degrees) => degrees * (MathF.PI / 180.0f);

    private static float Radians(float degrees) => degrees * (MathF.PI / 180.0f);

    public void DrawInspector()
    {
        ImGui.Checkbox("Use Item Layout", ref enabled);
        ImGui.BeginDisabled(!enabled);

        if (Inspector.Inspector.BeginSection("Selected"))
        {
            Inspector.Inspector.Inspect("Position##selected", ref selectedPosition);
            Inspector.Inspector.Inspect("Rotation##selected", ref selectedRotation);
            Inspector.Inspector.Inspect("Scale##selected", ref selectedScale);
            Inspector.Inspector.EndSection();
        }

        if (Inspector.Inspector.BeginSection("Next"))
        {
            DrawPath("next", next);
            Inspector.Inspector.EndSection();
        }

        ImGui.Checkbox("Mirror Previous", ref mirrorPrevious);

        if (!mirrorPrevious && Inspector.Inspector.BeginSection("Previous"))
        {
            DrawPath("previous", previous);
            Inspector.Inspector.EndSection();
        }

        ImGui.EndDisabled();
    }

    private static void DrawPath(string id, UIItemPath path)
    {
        if (Inspector.Inspector.BeginSection($"Fixed##{id}"))
        {
            Inspector.Inspector.Inspect($"Position##{id}", ref path.position);
            Inspector.Inspector.Inspect($"Rotation##{id}", ref path.rotation);
            Inspector.Inspector.Inspect($"Scale##{id}", ref path.scale);
            Inspector.Inspector.EndSection();
        }

        if (Inspector.Inspector.BeginSection($"Additive##{id}"))
        {
            Inspector.Inspector.Inspect($"Position##{id}Step", ref path.positionStep);
            Inspector.Inspector.Inspect($"Rotation##{id}Step", ref path.rotationStep);
            Inspector.Inspector.Inspect($"Scale##{id}Step", ref path.scaleStep);
            Inspector.Inspector.EndSection();
        }
    }
}
