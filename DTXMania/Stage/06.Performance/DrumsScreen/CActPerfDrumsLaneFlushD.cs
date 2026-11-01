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

    private readonly CCounter?[] ctFlush = new CCounter?[LaneCount];
    private readonly BaseTexture?[] txFlush = new BaseTexture?[LaneCount];
    private readonly BaseTexture?[] txFlushReverse = new BaseTexture?[LaneCount];

    public LaneLayout lanes = new();

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

        for (int i = 0; i < LaneCount; i++)
        {
            txFlush[i] = BaseTexture.LoadFromPath(CSkin.Path($@"Graphics\ScreenPlayDrums lane flush {FlushNames[i]}.png"));
            txFlushReverse[i] = BaseTexture.LoadFromPath(CSkin.Path($@"Graphics\ScreenPlayDrums lane flush {FlushNames[i]} reverse.png"));
        }
        base.OnManagedCreateResources();
    }

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

    //three 42px columns of the texture are layered on top of each other, tiled across the lane width
    private void DrawFlush(Matrix4x4 origin, int lane, int progress)
    {
        int x = (int)lanes.InnerLeft((ELane)lane);
        int width = (int)lanes.InnerRight((ELane)lane) - x;
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
