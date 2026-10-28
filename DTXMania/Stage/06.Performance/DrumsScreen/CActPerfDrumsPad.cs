using System.Numerics;
using System.Drawing;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using FDK;

namespace DTXMania;

internal class CActPerfDrumsPad : CActivity
{
    //pads are indexed LC HH SD BD HT LT FT CY RD LP
    private const int PadCount = 10;
    //with RD RC the ride pad reaches under the cymbal pad, so CY draws last
    private static readonly int[] DrawOrder = [0, 1, 2, 3, 4, 5, 6, 8, 9, 7];
    //per NumOfLanes
    private static readonly bool[][] Shown =
    [
        [true, true, true, true, true, true, true, true, true, true],
        [true, true, true, true, true, true, true, true, false, true],
        [false, true, true, true, true, true, false, true, false, false]
    ];
    private static readonly ELane[] Lanes =
        [ELane.LC, ELane.HH, ELane.SD, ELane.BD, ELane.HT, ELane.LT, ELane.FT, ELane.CY, ELane.RD, ELane.LP];
    //how far left of its lane's centre each 96px cell of 7_pads.png starts. Cymbal art hangs off to the right
    private static readonly float[] CellOffset = [67.5f, 57f, 51f, 47f, 40f, 46f, 45f, 45f, 43f, 47f];
    private static readonly PointF[] SourceOrigin =
    [
        new(0, 0), new(96, 0), new(0, 96), new(0, 192), new(96, 96),
        new(192, 96), new(288, 96), new(192, 0), new(288, 0), new(96, 192)
    ];

    //what a pad's colours are multiplied by at full brightness. Above 1 is drawn a second time, additively
    private static readonly Color4[] LitColour =
    [
        new(1.16f, 0.74f, 0.99f), new(0.5f, 0.85f, 1.16f), new(1.16f, 1.06f, 0.52f), new(0.89f, 0.63f, 1.16f),
        new(0.53f, 0.96f, 0.61f), new(1.16f, 0.8f, 0.76f), new(1.16f, 0.92f, 0.54f), new(0.5f, 0.85f, 1.16f),
        new(0.5f, 0.85f, 1.16f), new(1.16f, 0.77f, 0.99f)
    ];

    private struct PadState
    {
        public int brightness;
        public int bounce;
        public int bounceSpeed;
    }

    private readonly PadState[] pads = new PadState[PadCount];
    private long brightnessTimer;
    private long bounceTimer;
    private CCounter? bonusTimer;
    private BaseTexture? txPad;
    private BaseTexture? txBonus;

    public CActPerfDrumsPad()
    {
        bActivated = false;
    }

    public void Hit(int pad)
    {
        pads[pad].brightness = 6;
        pads[pad].bounceSpeed = 2;
    }

    public void ShowBonus()
    {
        bonusTimer = new CCounter(0, 1020, 1, CDTXMania.Timer);
    }

    public override void OnActivate()
    {
        Array.Clear(pads);
        bonusTimer = null;
        base.OnActivate();
    }

    public override void OnManagedCreateResources()
    {
        if (!bActivated)
        {
            return;
        }

        txPad = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_pads.png"));
        txBonus = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_Bonus.png"));
        base.OnManagedCreateResources();
    }

    public void Draw(Matrix4x4 origin)
    {
        if (!bActivated)
        {
            return;
        }

        Animate();

        bool[] shown = Shown[(int)CDTXMania.ConfigIni.eNumOfLanes.Drums];
        int baseY = 10 + (CDTXMania.ConfigIni.bReverse.Drums ? 60 : 560);
        foreach (int pad in DrawOrder)
        {
            if (!shown[pad])
            {
                continue;
            }

            int x = PadX(pad);
            int y = baseY + pads[pad].bounce;
            RectangleF source = new(SourceOrigin[pad], new SizeF(96, 96));

            if (txPad != null)
            {
                DrawPad(origin, txPad, pad, x, y, source);
            }
        }

        if (bonusTimer != null)
        {
            bonusTimer.tUpdate();
            if (txBonus != null && CDTXMania.ConfigIni.bShowScore)
            {
                txBonus.tDraw2D(origin, 160, 80);
            }
            if (bonusTimer.bReachedEndValue)
            {
                bonusTimer = null;
            }
        }

        return;
    }

    private void DrawPad(Matrix4x4 origin, BaseTexture texture, int pad, int x, int y, RectangleF source)
    {
        float lit = Math.Min(pads[pad].brightness * 50 / 255f, 1f);
        if (lit <= 0)
        {
            texture.tDraw2D(origin, x, y, source);
            return;
        }

        Color4 full = LitColour[pad];
        float r = 1 + (full.Red - 1) * lit;
        float g = 1 + (full.Green - 1) * lit;
        float b = 1 + (full.Blue - 1) * lit;
        texture.tDraw2D(origin, x, y, source, new Color4(Math.Min(r, 1), Math.Min(g, 1), Math.Min(b, 1)));

        texture.blendMode = BlendMode.Additive;
        texture.tDraw2D(origin, x, y, source, new Color4(Math.Max(r - 1, 0), Math.Max(g - 1, 0), Math.Max(b - 1, 0)));
        texture.blendMode = BlendMode.Alpha;
    }

    private void Animate()
    {
        long now = CDTXMania.Timer.nCurrentTime;
        if (bJustStartedUpdate)
        {
            brightnessTimer = now;
            bounceTimer = now;
            bJustStartedUpdate = false;
        }
        brightnessTimer = Math.Min(brightnessTimer, now);
        bounceTimer = Math.Min(bounceTimer, now);

        for (; now - brightnessTimer >= 18; brightnessTimer += 18)
        {
            for (int i = 0; i < PadCount; i++)
            {
                pads[i].brightness = Math.Max(pads[i].brightness - 1, 0);
            }
        }

        for (; now - bounceTimer >= 6; bounceTimer += 8)
        {
            for (int i = 0; i < PadCount; i++)
            {
                ref PadState pad = ref pads[i];
                pad.bounce += pad.bounceSpeed;
                if (pad.bounce > 11)
                {
                    pad.bounce = 11;
                    pad.bounceSpeed = -1;
                }
                else if (pad.bounce < 0)
                {
                    pad.bounce = 0;
                    pad.bounceSpeed = 0;
                }
            }
        }
    }

    private static int PadX(int pad) => (int)(DrumLanes.Centre(Lanes[pad]) - CellOffset[pad]);
}
