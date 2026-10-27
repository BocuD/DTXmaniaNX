using System.Numerics;
using System.Drawing;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using FDK;

namespace DTXMania;

internal class CActPerfDrumsLaneFlushD : CActivity
{
    private const int LaneCount = 11;
    private static readonly string[] FlushNames =
        ["leftcymbal", "hihat", "snare", "bass", "hitom", "lowtom", "floortom", "cymbal", "leftpedal", "ridecymbal", "leftpedal"];

    private static readonly ELane[] LineLanes =
        [ELane.LC, ELane.HH, ELane.SD, ELane.BD, ELane.HT, ELane.LT, ELane.FT, ELane.CY, ELane.RD, ELane.LP];
    //per NumOfLanes
    private static readonly bool[][] LineShown =
    [
        [true, true, true, true, true, true, true, true, true, true],
        [true, true, true, true, true, true, true, true, false, true],
        [false, true, true, true, true, true, true, true, false, true]
    ];

    private readonly CCounter?[] ctFlush = new CCounter?[LaneCount];
    private readonly BaseTexture?[] txFlush = new BaseTexture?[LaneCount];
    private readonly BaseTexture?[] txFlushReverse = new BaseTexture?[LaneCount];
    private BaseTexture? txLine;

    public CActPerfDrumsLaneFlushD()
    {
        bActivated = false;
    }

    public void Start(ELane lane, float velocity)
    {
        ctFlush[(int)lane] = new CCounter((int)((1f - velocity) * 55f), 90, 3, CDTXMania.Timer);
    }

    public override void OnActivate()
    {
        for (int i = 0; i < LaneCount; i++)
        {
            ctFlush[i] = new CCounter();
        }
        base.OnActivate();
    }

    public override void OnDeactivate()
    {
        Array.Clear(ctFlush);
        base.OnDeactivate();
    }

    public override void OnManagedCreateResources()
    {
        if (!bActivated)
        {
            return;
        }

        txLine = BaseTexture.LoadFromPath(CSkin.Path(ShowsLaneLines ? @"Graphics\7_Paret.png" : @"Graphics\7_Paret_Dark.png"));
        for (int i = 0; i < LaneCount; i++)
        {
            txFlush[i] = BaseTexture.LoadFromPath(CSkin.Path($@"Graphics\ScreenPlayDrums lane flush {FlushNames[i]}.png"));
            txFlushReverse[i] = BaseTexture.LoadFromPath(CSkin.Path($@"Graphics\ScreenPlayDrums lane flush {FlushNames[i]} reverse.png"));
        }
        base.OnManagedCreateResources();
    }

    private static bool ShowsLaneLines => CDTXMania.ConfigIni.nLaneDisp.Drums is 0 or 2;

    public void Draw(Matrix4x4 origin)
    {
        if (!bActivated)
        {
            return;
        }

        foreach (CCounter? counter in ctFlush)
        {
            if (counter is { bStopped: false })
            {
                counter.tUpdate();
                if (counter.bReachedEndValue)
                {
                    counter.tStop();
                }
            }
        }

        DrawLaneLines(origin);

        if (CDTXMania.ConfigIni.bLaneFlush.Drums)
        {
            for (int lane = 0; lane < LaneCount; lane++)
            {
                if (ctFlush[lane] is { bStopped: false } counter)
                {
                    DrawFlush(origin, lane, counter.nCurrentValue);
                }
            }
        }

        return;
    }

    private void DrawLaneLines(Matrix4x4 origin)
    {
        if (txLine == null)
        {
            return;
        }

        int alpha = 255 - (int)(CDTXMania.ConfigIni.nMovieAlpha * 255 / 10f);
        Color4 color = Color4.White;
        color.Alpha = alpha / 255.0f;

        bool[] shown = LineShown[(int)CDTXMania.ConfigIni.eNumOfLanes.Drums];
        for (int index = 0; index < LineLanes.Length; index++)
        {
            if (!shown[index])
            {
                continue;
            }

            if (!ShowsLaneLines)
            {
                //the dark panel is one piece, drawn in the HH slot
                if (index == 1)
                {
                    (int x, int width) = CDTXMania.ConfigIni.eNumOfLanes.Drums switch
                    {
                        EType.B => (295, 519),
                        EType.C => (367, 447),
                        _ => (295, 558)
                    };
                    txLine.tDraw2D(origin, x, 0, new RectangleF(0, 0, width, 720), color);
                }
                continue;
            }

            (int destX, int sourceX, int sourceWidth) = DrumLanes.LineSlice(LineLanes[index]);
            txLine.tDraw2D(origin, destX, 0, new RectangleF(sourceX, 0, sourceWidth, 720), color);
        }
    }

    //three 42px columns of the texture are layered on top of each other, tiled across the lane width
    private void DrawFlush(Matrix4x4 origin, int lane, int progress)
    {
        (int x, int right) = DrumLanes.Inner((ELane)lane);
        int width = right - x;
        bool reverse = CDTXMania.ConfigIni.bReverse.Drums;
        BaseTexture? texture = reverse ? txFlushReverse[lane] : txFlush[lane];
        if (texture == null)
        {
            return;
        }

        int y = reverse ? 32 + progress * 740 / 100 : 700 - progress * 740 / 100;
        if (!reverse && y >= 720)
        {
            return;
        }

        Color4 color = Color4.White;
        if (!reverse)
        {
            color.Alpha = y / 255.0f;
        }

        for (int layer = 0; layer < 3; layer++)
        {
            for (int offset = 0; offset < width; offset += 42)
            {
                texture.tDraw2D(origin, x + offset, y, new RectangleF(layer * 42, 0, Math.Min(width - offset, 42), 128), color);
            }
        }
    }
}
