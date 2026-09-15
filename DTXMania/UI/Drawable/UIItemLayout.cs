using System.Numerics;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Drawable;

public sealed class UIItemPath
{
    //where the first item past the selection sits
    [Themable] public Vector3 start;

    //degrees away from the list's axis into depth; items lie along the turned path
    [Themable] public float turn;

    [Themable] public float spacing = 100.0f;

    //degrees added to the turn per item; 0 is straight
    [Themable] public float bend;

    [Themable] public float scale = 1.0f;
}

/// <summary>Places a list's items from where the selection sits and the path they take away from it on either side.</summary>
public sealed class UIItemLayout
{
    [Themable] public bool enabled;

    [Themable] public Vector3 selectedPosition;
    [Themable] public float selectedTurn;
    [Themable] public float selectedScale = 1.0f;

    [Themable] [SkinSerialize] public UIItemPath next = new();

    //previous takes next's path, flipped along the list's axis
    [Themable] public bool mirrorPrevious = true;
    [Themable] [SkinSerialize] public UIItemPath previous = new();

    private readonly record struct Place(Vector3 Position, float Turn, float Scale);

    /// <summary><paramref name="distance"/> is in items from the selection, fractional while scrolling.</summary>
    public void Apply(UIDrawable item, float distance, Vector3 axis)
    {
        bool column = MathF.Abs(axis.Y) > MathF.Abs(axis.X);
        bool isPrevious = distance < 0.0f;

        //which way along the list's axis this side of the selection runs
        float away = MathF.Sign(column ? axis.Y : axis.X) * (isPrevious ? -1.0f : 1.0f);
        float steps = MathF.Abs(distance);

        UIItemPath path = isPrevious && !mirrorPrevious ? previous : next;
        Place place = OnPath(path, MathF.Max(steps, 1.0f), column, away, flip: isPrevious && mirrorPrevious);

        if (steps < 1.0f)
        {
            Place selected = new(selectedPosition, Radians(selectedTurn), selectedScale);
            place = new Place(
                Vector3.Lerp(selected.Position, place.Position, steps),
                float.Lerp(selected.Turn, place.Turn, steps),
                float.Lerp(selected.Scale, place.Scale, steps));
        }

        item.position = place.Position;
        item.rotation = column ? new Vector3(place.Turn, 0.0f, 0.0f) : new Vector3(0.0f, -place.Turn, 0.0f);
        item.scale = new Vector3(place.Scale, place.Scale, 1.0f);
    }

    private static Place OnPath(UIItemPath path, float steps, bool column, float away, bool flip)
    {
        Vector3 start = path.start;
        if (flip)
        {
            start = column ? start with { Y = -start.Y } : start with { X = -start.X };
        }

        Vector3 forward = column ? new Vector3(0.0f, away, 0.0f) : new Vector3(away, 0.0f, 0.0f);
        float along = steps - 1.0f;
        float first = Radians(path.turn);
        float turn = first + along * Radians(path.bend);

        Vector3 position;
        if (MathF.Abs(path.bend) < 0.001f)
        {
            position = start + along * path.spacing * (forward * MathF.Cos(first) + Vector3.UnitZ * MathF.Sin(first));
        }
        else
        {
            //item centres lie on a circle whose chord between neighbours is the spacing
            float radius = path.spacing / (2.0f * MathF.Sin(Radians(path.bend) / 2.0f));
            position = start + radius * (forward * (MathF.Sin(turn) - MathF.Sin(first)) - Vector3.UnitZ * (MathF.Cos(turn) - MathF.Cos(first)));
        }

        return new Place(position, away * turn, path.scale);
    }

    private static float Radians(float degrees) => degrees * (MathF.PI / 180.0f);

    public void DrawInspector()
    {
        ImGui.Checkbox("Use Item Layout", ref enabled);
        ImGui.BeginDisabled(!enabled);

        ImGui.SeparatorText("Selected");
        Inspector.Inspector.Inspect("Position##selected", ref selectedPosition);
        ImGui.InputFloat("Turn##selected", ref selectedTurn, 1.0f, 10.0f, "%.1f");
        ImGui.InputFloat("Scale##selected", ref selectedScale, 0.05f, 0.25f, "%.2f");

        ImGui.SeparatorText("Next");
        DrawPath("next", next);

        ImGui.Checkbox("Mirror Previous", ref mirrorPrevious);
        if (!mirrorPrevious)
        {
            ImGui.SeparatorText("Previous");
            DrawPath("previous", previous);
        }

        ImGui.EndDisabled();
    }

    private static void DrawPath(string id, UIItemPath path)
    {
        ImGui.PushID(id);
        Inspector.Inspector.Inspect("Start", ref path.start);
        ImGui.InputFloat("Turn", ref path.turn, 1.0f, 10.0f, "%.1f");
        ImGui.InputFloat("Spacing", ref path.spacing, 1.0f, 10.0f, "%.1f");
        ImGui.InputFloat("Bend", ref path.bend, 0.5f, 5.0f, "%.1f");
        ImGui.InputFloat("Scale", ref path.scale, 0.05f, 0.25f, "%.2f");
        ImGui.PopID();
    }
}
