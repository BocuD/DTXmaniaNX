using DTXMania.Core;

namespace DTXMania;

//specifies where drum lanes are located and what the spacing between them is
//chips, flush, pads, fire and judgement text are all derived from here, and are centered between lane divider lines
internal static class DrumLanes
{
    //a lane's column of 7_Paret.png. InnerLeft and InnerRight are the lane's edges, just inside its divider lines
    private readonly record struct Slice(int SourceX, int Width, int InnerLeft, int InnerRight);

    //by ELane: LC HH SD BD HT LT FT CY LP RD LBD. BD has a double divider on its left
    private static readonly Slice[] Slices =
    [
        new(0, 72, 3, 68), new(72, 49, 2, 50), new(172, 57, 2, 58), new(278, 69, 8, 70),
        new(229, 49, 2, 50), new(347, 49, 2, 50), new(396, 54, 2, 50), new(450, 70, 2, 68),
        new(121, 51, 2, 52), new(520, 38, 0, 38), new(121, 51, 2, 52)
    ];

    public static (int destX, int sourceX, int width) LineSlice(ELane lane)
    {
        Slice slice = Slices[(int)lane];
        return (SliceX(lane), slice.SourceX, slice.Width);
    }

    //right is exclusive
    public static (int left, int right) Inner(ELane lane)
    {
        Slice slice = Slices[(int)lane];
        int x = SliceX(lane);
        return (x + slice.InnerLeft, x + slice.InnerRight);
    }

    public static float Centre(ELane lane)
    {
        (int left, int right) = Inner(lane);
        return (left + right) / 2f;
    }

    private static int SliceX(ELane lane)
    {
        EType type = CDTXMania.ConfigIni.eLaneType.Drums;
        bool rdrc = CDTXMania.ConfigIni.eRDPosition == ERDPosition.RDRC;
        return lane switch
        {
            ELane.LC => 295,
            ELane.HH => 367,
            ELane.SD => type is EType.B or EType.D ? 416 : 467,
            ELane.BD => type is EType.B or EType.C ? 524 : 573,
            ELane.HT => type switch { EType.A => 524, EType.D => 473, _ => 593 },
            ELane.LT => 642,
            ELane.FT => 691,
            ELane.CY => rdrc ? 783 : 745,
            ELane.RD => rdrc ? 745 : 815,
            _ => type switch { EType.B => 473, EType.D => 522, _ => 416 }
        };
    }
}
