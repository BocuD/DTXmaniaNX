using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using DTXMania.UI.Inspector;
using DTXMania.UI.Skin;
using Hexa.NET.ImGui;
using Newtonsoft.Json;

namespace DTXMania;

public class Lane
{
    public ELane lane;
    public SkinResource background;
    //includes the 1px line on each side. Neighbouring lanes share that line
    public float width;
    //empty space outside the lines
    public float spaceLeft;
    public float spaceRight;

    //a disabled lane takes no space, eg: RD with 9 lanes. Set by the playfield
    [JsonIgnore] public bool enabled = true;
}

//the background art of a player's lanes. The skin sets each lane's art and width.
//The playfield sets the order and which lanes are enabled
public class LaneGroup : UIDrawable
{
    [AddChildMenu("Game/Performance/Lane Group")]
    public static UIDrawable Create() => new LaneGroup();

    [Themable] [SkinSerialize] public List<Lane> lanes = [];
    //fills the space between lanes, and the whole group when lines are off
    [Themable] public Color4 spaceColor = new(0, 0, 0, 1);
    //how much of its neighbour each lane covers, eg: art that ends in a 1px line on both edges
    [Themable] public float lineOverlap = 1;

    //set by the playfield
    public ELane[] order = [];
    public bool showsArt = true;
    public float opacity = 1;

    //filled by Arrange. The playfield gives it the one it owns
    public LaneLayout layout = new();

    //by ELane, the index in lanes or -1. Built again only when the list changes
    private readonly int[] laneIndex = new int[(int)ELane.MAX];
    private int indexedCount = -1;
    private BaseTexture?[] art = [];
    private ELane[] listOrder = [];

    public LaneGroup()
    {
        name = "Lanes";
    }

    public void SetEnabled(ELane lane, bool enabled)
    {
        int index = IndexOf(lane);
        if (index >= 0)
        {
            lanes[index].enabled = enabled;
        }
    }

    public void Arrange()
    {
        layout.Clear();

        float x = 0;
        float spanLeft = float.MaxValue;
        float spanRight = float.MinValue;
        Lane? previous = null;
        foreach (ELane column in Columns())
        {
            int index = IndexOf(column);
            if (index < 0 || !lanes[index].enabled)
            {
                continue;
            }

            Lane lane = lanes[index];
            if (previous != null)
            {
                bool spaced = previous.spaceRight > 0 || lane.spaceLeft > 0;
                x = spaced ? x + previous.width + previous.spaceRight + lane.spaceLeft : x + previous.width - lineOverlap;
            }

            layout.Set(lane.lane, position.X + x, lane.width);
            //LBD chips are played in the LP lane
            if (lane.lane == ELane.LP)
            {
                layout.Set(ELane.LBD, position.X + x, lane.width);
            }

            spanLeft = Math.Min(spanLeft, position.X + x);
            spanRight = Math.Max(spanRight, position.X + x + lane.width);
            previous = lane;
        }

        if (spanRight > spanLeft)
        {
            layout.SetSpan(spanLeft, spanRight);
            size.X = spanRight - position.X;
        }
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        LoadArt();

        UpdateLocalTransformMatrix();
        Matrix4x4 origin = localTransformMatrix * parentMatrix;
        Color4 space = spaceColor;
        space.Alpha *= opacity;
        space = UITint.Current * space;

        //one panel over the lanes, inside their outer lines
        if (!showsArt)
        {
            BaseTexture.White.tDraw2DMatrix(Matrix4x4.CreateTranslation(layout.SpanLeft + 1 - position.X, 0, 0) * origin,
                new Vector2(layout.SpanWidth - 2, size.Y), new RectangleF(0, 0, 1, 1), space);
            return;
        }

        Color4 tint = UITint.Current * new Color4(1, 1, 1, opacity);
        float previousRight = float.MaxValue;
        foreach (ELane column in Columns())
        {
            int index = IndexOf(column);
            if (index < 0 || !lanes[index].enabled)
            {
                continue;
            }

            Lane lane = lanes[index];
            float left = layout.Left(column) - position.X;
            if (left > previousRight)
            {
                BaseTexture.White.tDraw2DMatrix(Matrix4x4.CreateTranslation(previousRight, 0, 0) * origin,
                    new Vector2(left - previousRight, size.Y), new RectangleF(0, 0, 1, 1), space);
            }

            previousRight = left + lane.width;
            BaseTexture? texture = art[index];
            if (texture != null)
            {
                texture.tDraw2DMatrix(Matrix4x4.CreateTranslation(left, 0, 0) * origin, new Vector2(lane.width, size.Y),
                    new RectangleF(0, 0, texture.Width, texture.Height), tint);
            }
        }
    }

    //without an order from the playfield, eg: a component preview, the list order
    private ELane[] Columns()
    {
        if (order.Length > 0)
        {
            return order;
        }

        if (listOrder.Length != lanes.Count)
        {
            listOrder = new ELane[lanes.Count];
            for (int index = 0; index < lanes.Count; index++)
            {
                listOrder[index] = lanes[index].lane;
            }
        }

        return listOrder;
    }

    private int IndexOf(ELane lane)
    {
        if (indexedCount != lanes.Count)
        {
            Array.Fill(laneIndex, -1);
            for (int index = 0; index < lanes.Count; index++)
            {
                laneIndex[(int)lanes[index].lane] = index;
            }

            indexedCount = lanes.Count;
        }

        return laneIndex[(int)lane];
    }

    private void LoadArt()
    {
        if (art.Length == lanes.Count)
        {
            return;
        }

        DisposeArt();
        art = new BaseTexture?[lanes.Count];
        for (int index = 0; index < lanes.Count; index++)
        {
            SkinResource background = lanes[index].background;
            art[index] = background.IsEmpty ? null : BaseTexture.LoadFromPath(background.Resolve(ResourceType.Image));
        }
    }

    private void DisposeArt()
    {
        foreach (BaseTexture? texture in art)
        {
            texture?.Dispose();
        }

        art = [];
    }

    protected override void DrawElementGizmos()
    {
        const uint laneColour = 0xFF00C8FF;
        Matrix4x4 transform = GetFullTransformMatrix();
        foreach (ELane column in Columns())
        {
            int index = IndexOf(column);
            if (index < 0 || !lanes[index].enabled)
            {
                continue;
            }

            float left = layout.Left(column) - position.X;
            float right = left + lanes[index].width;
            Vector3 topLeft = Vector3.Transform(new Vector3(left, 0, 0), transform);
            Vector3 topRight = Vector3.Transform(new Vector3(right, 0, 0), transform);
            Vector3 bottomLeft = Vector3.Transform(new Vector3(left, size.Y, 0), transform);
            Vector3 bottomRight = Vector3.Transform(new Vector3(right, size.Y, 0), transform);
            InspectorManager.DrawGizmoQuad(new Vector2(topLeft.X, topLeft.Y), new Vector2(topRight.X, topRight.Y),
                new Vector2(bottomLeft.X, bottomLeft.Y), new Vector2(bottomRight.X, bottomRight.Y), laneColour);

            Vector3 label = Vector3.Transform(new Vector3((left + right) / 2, size.Y / 2, 0), transform);
            InspectorManager.DrawGizmoText(new Vector2(label.X, label.Y), column.ToString(), laneColour, 0);
        }
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (!ImGui.CollapsingHeader("Lanes"))
        {
            return;
        }

        Inspector.Inspect("Space Color", ref spaceColor);
        ImGui.InputFloat("Line Overlap", ref lineOverlap);

        ELane[] columns = Columns();
        for (int position = 0; position < columns.Length; position++)
        {
            int index = IndexOf(columns[position]);
            if (index >= 0)
            {
                DrawLaneRow(index, position, columns.Length);
            }
        }
    }

    private void DrawLaneRow(int index, int position, int count)
    {
        Lane lane = lanes[index];
        ImGui.PushID(index);

        float right = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X;
        bool open = ImGui.TreeNodeEx(lane.lane.ToString(), ImGuiTreeNodeFlags.AllowOverlap);

        float buttonWidth = ImGui.GetFrameHeight();
        float spacing = ImGui.GetStyle().ItemSpacing.X;
        ImGui.SameLine(right - buttonWidth * 3 - spacing * 2);
        ImGui.Checkbox("##enabled", ref lane.enabled);
        ImGui.SameLine();
        ImGui.BeginDisabled(position == 0);
        if (ImGui.ArrowButton("up", ImGuiDir.Up))
        {
            Move(position, position - 1);
        }

        ImGui.EndDisabled();
        ImGui.SameLine();
        ImGui.BeginDisabled(position == count - 1);
        if (ImGui.ArrowButton("down", ImGuiDir.Down))
        {
            Move(position, position + 1);
        }

        ImGui.EndDisabled();

        if (open)
        {
            ImGui.InputFloat("Width", ref lane.width);
            ImGui.InputFloat("Space Left", ref lane.spaceLeft);
            ImGui.InputFloat("Space Right", ref lane.spaceRight);
            ResourceEditor.Draw("Background", ResourceType.Image, lane.background, chosen =>
            {
                lane.background = chosen;
                DisposeArt();
            });
            ImGui.TreePop();
        }

        ImGui.PopID();
    }

    private void Move(int from, int to)
    {
        if (order.Length == 0)
        {
            order = (ELane[])Columns().Clone();
        }

        ELane moved = order[from];
        order[from] = order[to];
        order[to] = moved;
    }

    public override void Dispose()
    {
        base.Dispose();
        DisposeArt();
    }
}
