using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using FDK;

namespace DTXMania;

public class DrumsChipRenderer : ChipRenderer
{
    public int patternFrame;

    private BaseTexture? chipArt = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_chips_drums.png"));

    protected override void DrawChips(Matrix4x4 origin)
    {
        if (chipArt == null)
        {
            return;
        }

        CConfigIni config = CDTXMania.ConfigIni;
        bool reverse = config.bReverse.Drums;
        bool showsLines = config.nLaneDisp.Drums == 0 || config.nLaneDisp.Drums == 1;

        int lineX = 295;
        int lineWidth = 559;
        if (config.eNumOfLanes.Drums == EType.B)
        {
            lineWidth = 519;
        }
        else if (config.eNumOfLanes.Drums == EType.C)
        {
            lineX = 367;
            lineWidth = 447;
        }

        if (firstChip >= 0)
        {
            for (int i = firstChip; i < chips.Count; i++)
            {
                CChip chip = chips[i];
                float distance = chip.nDistanceFromBar.Drums;
                if (distance > DrawRange) break;

                float y = reverse ? judgeLineY + distance : judgeLineY - distance;
                if (chip.nChannelNumber == EChannel.BarLine)
                {
                    if (config.bShowPerformanceInformation && showsLines)
                    {
                        int bar = chip.nPlaybackPosition / 384 - 1;
                        int textX = config.bGraph有効.Drums && config.bSmallGraph ? 828 : 858;
                        CDTXMania.actDisplayString.tPrint(origin, textX, (int)(y - 17), CCharacterConsole.EFontType.White, bar.ToString());
                    }

                    if (showsLines && chip.bVisible)
                    {
                        chipArt.tDraw2D(origin, lineX, y - 1, new RectangleF(0, 769, lineWidth, 2));
                    }
                }
                else if (chip.nChannelNumber == EChannel.BeatLine && showsLines && chip.bVisible)
                {
                    chipArt.tDraw2D(origin, lineX, y - 1, new RectangleF(0, 772, lineWidth, 2));
                }
            }
        }

        if (showsLoopBegin)
        {
            DrawLoopLine(origin, loopBeginDistance, false, reverse, showsLines, lineX, lineWidth);
        }

        if (showsLoopEnd)
        {
            DrawLoopLine(origin, loopEndDistance, true, reverse, showsLines, lineX, lineWidth);
        }

        if (firstChip < 0)
        {
            return;
        }

        //the animated pattern goes under every note
        for (int i = firstChip; i < chips.Count; i++)
        {
            CChip chip = chips[i];
            if (chip.nDistanceFromBar.Drums > DrawRange) break;

            DrawNote(origin, chip, reverse, 128 + patternFrame * 64, false);
        }

        for (int i = firstChip; i < chips.Count; i++)
        {
            CChip chip = chips[i];
            if (chip.nDistanceFromBar.Drums > DrawRange) break;

            DrawNote(origin, chip, reverse, 0, chip.bBonusChip);
        }
    }

    private static readonly int[] loopLineOffsets = [1, 3, 5, 11, 13, 19, 25];

    private void DrawLoopLine(Matrix4x4 origin, int distance, bool isEnd, bool reverse, bool showsLines, int lineX, int lineWidth)
    {
        int y = reverse ? judgeLineY + distance : judgeLineY - distance;
        CDTXMania.actDisplayString.tPrint(origin, 830, y - 17, CCharacterConsole.EFontType.White, isEnd ? "End loop" : "Begin loop");

        if (!showsLines)
        {
            return;
        }

        //the lines spread outwards, away from the looped part
        bool upwards = isEnd != reverse;
        foreach (int offset in loopLineOffsets)
        {
            int top = upwards ? y - offset : y + offset - 2;
            chipArt!.tDraw2D(origin, lineX, top, new RectangleF(0, 769, lineWidth, 2));
        }
    }

    //the art is centred in its source rect
    private readonly record struct DrumChipArt(int SourceX, int SourceWidth);

    private static DrumChipArt? ArtFor(EChannel channel) => channel switch
    {
        EChannel.BassDrum => new(0, 70),
        EChannel.HiHatClose => new(70, 56),
        EChannel.Snare => new(126, 64),
        EChannel.HighTom => new(190, 56),
        EChannel.LowTom => new(246, 56),
        EChannel.FloorTom => new(302, 56),
        EChannel.Cymbal => new(358, 74),
        EChannel.RideCymbal => new(442, 48),
        EChannel.LeftCymbal => new(538, 74),
        EChannel.LeftPedal => new(660, 58),
        EChannel.HiHatOpen => CDTXMania.ConfigIni.eHHOGraphics.Drums == EType.C ? new(70, 56) : new(612, 48),
        EChannel.LeftBassDrum => CDTXMania.ConfigIni.eLBDGraphics.Drums == EType.A ? new(660, 58) : new(480, 58),
        _ => null
    };

    private void DrawNote(Matrix4x4 origin, CChip chip, bool reverse, int row, bool bonus)
    {
        DrumChipArt? found = ArtFor(chip.nChannelNumber);
        if (found == null || chip.bHit || !chip.bVisible)
        {
            return;
        }

        float distance = chip.nDistanceFromBar.Drums;
        float alpha = NoteAlpha(distance, CDTXMania.ConfigIni.nHidSud.Drums, 200, 100, 50);
        if (alpha <= 0)
        {
            return;
        }

        DrumChipArt art = found.Value;
        ELane lane = (ELane)DrumsPerformance.LaneOf(chip);
        int x = (int)(DrumLanes.Centre(lane) - art.SourceWidth / 2f);
        float y = (reverse ? judgeLineY + distance : judgeLineY - distance) - 32;
        Color4 color = Color4.White;
        color.Alpha = alpha;

        chipArt!.tDraw2D(origin, x, y, new RectangleF(art.SourceX, row, art.SourceWidth, 64), color);
        if (bonus)
        {
            chipArt.tDraw2D(origin, x, y, new RectangleF(art.SourceX, 64, art.SourceWidth, 64), color);
        }
    }

    public override void Dispose()
    {
        base.Dispose();

        chipArt?.Dispose();
        chipArt = null;
    }
}
