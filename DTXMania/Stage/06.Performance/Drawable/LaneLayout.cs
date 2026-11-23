using DTXMania.Core;

namespace DTXMania;

public class LaneLayout
{
    private readonly float[] left = new float[(int)ELane.MAX];
    private readonly float[] width = new float[(int)ELane.MAX];

    //the shown lanes, from the first one's left line to the last one's right line, right exclusive
    public float SpanLeft { get; private set; }
    public float SpanRight { get; private set; }
    public float SpanWidth => SpanRight - SpanLeft;

    //false for a disabled lane, eg: RD with 9 lanes
    public bool Has(ELane lane) => width[(int)lane] > 0;

    public float Left(ELane lane) => left[(int)lane];
    public float Width(ELane lane) => width[(int)lane];
    public float Centre(ELane lane) => left[(int)lane] + width[(int)lane] / 2;

    //inside the lines, right exclusive
    public float InnerLeft(ELane lane) => left[(int)lane] + 1;
    public float InnerRight(ELane lane) => left[(int)lane] + width[(int)lane] - 1;

    public void Set(ELane lane, float x, float laneWidth)
    {
        left[(int)lane] = x;
        width[(int)lane] = laneWidth;
    }

    public void SetSpan(float spanLeft, float spanRight)
    {
        SpanLeft = spanLeft;
        SpanRight = spanRight;
    }

    public void Clear()
    {
        Array.Clear(left);
        Array.Clear(width);
        SpanLeft = 0;
        SpanRight = 0;
    }
}
