using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using FDK;

namespace DTXMania;

public class GuitarChipRenderer : ChipRenderer
{
    private static readonly int[] noteX = [88, 127, 166, 205, 244];
    private static readonly int[] holdNoteX = [107, 146, 185, 224, 264];

    private readonly EInstrumentPart part;

    private BaseTexture? chipArt;
    private readonly BaseTexture[] notes = new BaseTexture[5];
    private readonly BaseTexture[] holdBodies = new BaseTexture[5];
    private readonly HoldNote[] holdNotes = new HoldNote[5];

    public GuitarChipRenderer(EInstrumentPart part)
    {
        this.part = part;

        chipArt = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_Chips_Guitar.png"));
        for (int fret = 0; fret < 5; fret++)
        {
            notes[fret] = BaseTexture.LoadFromPath(CSkin.Path($@"Graphics\Note\Guitar\note0{fret}.png"));
            holdBodies[fret] = BaseTexture.LoadFromPath(CSkin.Path($@"Graphics\Note\Guitar\lnote{fret}1.png"));
            holdNotes[fret] = new HoldNote();
            holdNotes[fret].position = new Vector3(holdNoteX[fret], 0, 0);
        }
    }

    protected override void DrawChips(Matrix4x4 origin)
    {
        if (chipArt == null)
        {
            return;
        }

        CConfigIni config = CDTXMania.ConfigIni;
        int index = (int)part;
        bool reverse = config.bReverse[index];
        bool showsLines = config.nLaneDisp[index] == 0 || config.nLaneDisp[index] == 1;
        bool hasChips = CDTXMania.DTX.bHasChips[index];

        if (firstChip >= 0)
        {
            for (int i = firstChip; i < chips.Count; i++)
            {
                CChip chip = chips[i];
                float distance = chip.nDistanceFromBar[index];
                if (distance > DrawRange) break;

                if (chip.nChannelNumber != EChannel.BarLine || !chip.bVisible)
                {
                    continue;
                }

                float y = reverse ? judgeLineY - distance : judgeLineY + distance + 9;
                if (!hasChips || y <= 104 || y >= 670)
                {
                    continue;
                }

                if (showsLines)
                {
                    chipArt.tDraw2D(origin, 88, y, new RectangleF(0, 20, 193, 2));
                }

                if (config.bShowPerformanceInformation)
                {
                    int bar = chip.nPlaybackPosition / 384 - 1;
                    CDTXMania.actDisplayString.tPrint(origin, 60, (int)y - 16, CCharacterConsole.EFontType.White, bar.ToString());
                }
            }
        }

        if (firstChip < 0)
        {
            return;
        }

        EChannel wailing = part == EInstrumentPart.GUITAR ? EChannel.Guitar_Wailing : EChannel.Bass_Wailing;
        for (int i = firstChip; i < chips.Count; i++)
        {
            CChip chip = chips[i];
            if (chip.nDistanceFromBar[index] > DrawRange) break;

            if (chip.frets >= 0 && chip.eInstrumentPart == part)
            {
                DrawNote(origin, chip, reverse);
            }
            else if (chip.nChannelNumber == wailing)
            {
                DrawWailingChip(origin, chip, reverse);
            }
        }
    }

    private void DrawNote(Matrix4x4 origin, CChip chip, bool reverse)
    {
        if ((chip.bHit && !chip.bIsLongNote) || !chip.bVisible)
        {
            return;
        }

        CConfigIni config = CDTXMania.ConfigIni;
        int index = (int)part;
        float distance = chip.nDistanceFromBar[index];
        float alpha = NoteAlpha(distance, config.nHidSud[index], 250, 150, 75);
        if (alpha <= 0)
        {
            return;
        }

        int barY = reverse ? judgeLineY + 1 : judgeLineY + 10;
        float y = reverse ? barY - distance : barY + distance;
        float length = 0;
        if (chip.bIsLongNote)
        {
            float endDistance = chip.chipLongNoteEndPosition.nDistanceFromBar[index];
            if (endDistance <= 0)
            {
                return;
            }

            length = endDistance - distance;
            if (chip.bHit && chip.bIsHittingLongNote)
            {
                y = barY - 5;
                length = endDistance + 5f;
            }
        }

        Color4 color = Color4.White;
        color.Alpha = alpha;
        if (chip.frets == 0)
        {
            chipArt!.tDraw2D(origin, 88, y - 2, new RectangleF(0, 10, 196, 10), color);
        }

        bool left = config.bLeft[index];
        for (int fret = 0; fret < 5; fret++)
        {
            if ((chip.frets & (1 << fret)) == 0)
            {
                continue;
            }

            int x = left ? noteX[4 - fret] : noteX[fret];
            if (!chip.bHit)
            {
                notes[fret].tDraw2D(origin, x, y - 5.0f, color);
            }

            if (!chip.bIsLongNote)
            {
                continue;
            }

            Color4 bodyColor = Color4.White;
            bodyColor.Alpha = chip.bHit && !chip.bIsHittingLongNote ? 0.25f : 0.5f;
            float top = y - (reverse ? length : 0);
            holdBodies[fret].tDraw2D(origin, x, top, new RectangleF(0, 0, 36, 8), bodyColor, new Vector2(36, length));

            HoldNote holdNote = holdNotes[fret];
            holdNote.clipHeight = length;
            holdNote.position.Y = top;
            holdNote.Draw(origin, chip.bHit, chip.bIsHittingLongNote);
        }
    }

    //sits at a fixed height, not on the judge line
    private void DrawWailingChip(Matrix4x4 origin, CChip chip, bool reverse)
    {
        int index = (int)part;
        float distance = chip.nDistanceFromBar[index];
        if (chip.bHit || !chip.bVisible || NoteAlpha(distance, CDTXMania.ConfigIni.nHidSud[index], 250, 150, 75) <= 0)
        {
            return;
        }

        const int halfHeight = 34;
        float y = reverse ? 611 - distance : 154 + distance;
        if (y >= 709 + halfHeight || y <= -halfHeight)
        {
            return;
        }

        //cut off outside 0-709
        RectangleF source = new(0, 22, 54, 68);
        float clipTop = 0;
        if (y < halfHeight)
        {
            clipTop = halfHeight - y;
            source.Y += clipTop;
            source.Height -= clipTop;
        }

        if (y > 709 - halfHeight)
        {
            source.Height -= y - (709 - halfHeight);
        }

        if (source.Bottom > source.Top)
        {
            chipArt!.tDraw2D(origin, 287, y - halfHeight + clipTop, source);
        }
    }

    public override void Dispose()
    {
        base.Dispose();

        chipArt?.Dispose();
        chipArt = null;
        for (int fret = 0; fret < 5; fret++)
        {
            notes[fret]?.Dispose();
            holdBodies[fret]?.Dispose();
            holdNotes[fret]?.Dispose();
        }
    }
}
