using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using FDK;

namespace DTXMania;

internal partial class CStagePerfGuitarScreen
{
	protected override void UpdateChip_GuitarBass(CConfigIni configIni, ref CDTX dTX, ref CChip pChip,
		EInstrumentPart inst)
	{
		int instIndex = (int)inst;
		ApplyHiddenSudden(pChip, pChip.nDistanceFromBar[instIndex], CDTXMania.ConfigIni.nHidSud[instIndex], 250, 150, 75.0);

		//a long note whose end has passed is finished
		if (pChip.bIsLongNote && pChip.bVisible && pChip.chipLongNoteEndPosition.nDistanceFromBar[instIndex] <= 0)
		{
			return;
		}

		GuitarPerformance guitar = GuitarFor(inst)!;
		guitar.UpdateNote(pChip);
		ApplyGuitarEvents(guitar);

		if (CDTXMania.ConfigIni.bGraph有効[instIndex] && guitar.HasTargetGhost)
		{
			actGraph.dbGraphValue_Goal = guitar.TargetGhostGoal();
		}
	}

	protected override void DrawChip_GuitarBass(CConfigIni configIni, ref CDTX dTX, ref CChip pChip, EInstrumentPart inst)
	{
		if (!configIni.bGuitarEnabled)
		{
			return;
		}

		int barYNormal = nJudgeLinePosY[(int)inst] + 10;
		int barYReverse = nJudgeLinePosY[(int)inst] + 1;
		int instIndex = (int)inst;
		DetermineUsedChips(pChip, out bool bChipHasR, out bool bChipHasG, out bool bChipHasB, out bool bChipHasY, out bool bChipHasP, out bool bChipIsOpen);

		if ((!pChip.bHit || pChip.bIsLongNote) && pChip.bVisible)
		{
			float yBarPos = configIni.bReverse[instIndex] ? barYReverse : barYNormal;
			float y = configIni.bReverse[instIndex]
				? (barYReverse - pChip.nDistanceFromBar[instIndex])
				: (barYNormal + pChip.nDistanceFromBar[instIndex]);

			//
			float noteLength = 0;
			if (pChip.bIsLongNote)
			{
				if (pChip.chipLongNoteEndPosition.nDistanceFromBar[(int)inst] <= 0)
				{
					return;
				}

				noteLength = (float) (pChip.chipLongNoteEndPosition.nDistanceFromBar[(int)inst] -
				       pChip.nDistanceFromBar[(int)inst]);
				
				if (pChip.bHit && pChip.bIsHittingLongNote)
				{
					y = yBarPos - 5; //-5 because otherwise it doesn't align or some shit
					noteLength = (float) (pChip.chipLongNoteEndPosition.nDistanceFromBar[(int)inst] + 5f); //add 5 to compensate
				}
			}

			if (txChip != null)
			{
				if (bChipIsOpen)
				{
					int xo = (inst == EInstrumentPart.GUITAR) ? 88 : 959;
					Color4 col = Color4.White;
					col.Alpha = pChip.nTransparency / 255.0f;
					txChip.tDraw2D(xo, y - 2, new RectangleF(0, 10, 196, 10), col);
				}

				//Refactored code for drawing
				int[] nChipXPos =
				[
					inst == EInstrumentPart.GUITAR ? 88 : 959,
					inst == EInstrumentPart.GUITAR ? 127 : 998,
					inst == EInstrumentPart.GUITAR ? 166 : 1036,
					inst == EInstrumentPart.GUITAR ? 205 : 1076,
					inst == EInstrumentPart.GUITAR ? 244 : 1115
				];

				if (inst == EInstrumentPart.GUITAR && CDTXMania.ConfigIni.bLeft.Guitar)
				{
					Array.Reverse(nChipXPos);
				}

				if (inst == EInstrumentPart.BASS && CDTXMania.ConfigIni.bLeft.Bass)
				{
					Array.Reverse(nChipXPos);
				}

				RectangleF[] rChipTxRectArray =
				[
					new(0, 0, 38, 10),
					new(38, 0, 38, 10),
					new(76, 0, 38, 10),
					new(114, 0, 38, 10),
					new(152, 0, 38, 10)
				];

				bool[] bChipColorFlags =
				[
					bChipHasR,
					bChipHasG,
					bChipHasB,
					bChipHasY,
					bChipHasP
				];

				for (int i = 0; i < bChipColorFlags.Length; i++)
				{
					if (bChipColorFlags[i])
					{
						if (inst == EInstrumentPart.GUITAR || inst == EInstrumentPart.BASS)
						{
							int num8 = nChipXPos[i];
							RectangleF rect1 = rChipTxRectArray[i];

							if (!pChip.bHit)
							{
								Color4 color = Color4.White;
								color.Alpha = pChip.nTransparency / 255.0f;

								//normal chip draw i think (?)
								////yeeeep
								/// 
								//txChip.tDraw2D(num8, y - 10 / 2, rect1, color);
								txNote[i].tDraw2D(num8, y - 10 / 2.0f, color);
							}

							if (pChip.bIsLongNote)
							{
								RectangleF rectangle2 = rect1;
								rectangle2.Y += 3;
								rectangle2.Height = 5;
								Color4 col = Color4.White;
								col.Alpha = 0.5f;

								if (pChip.bHit && !pChip.bIsHittingLongNote)
								{
									col.Alpha = 0.25f;
								}
								
								txHoldNoteBg[i].tDraw2D(num8,
									y - (CDTXMania.ConfigIni.bReverse[(int)inst] ? noteLength : 0),
									new RectangleF(0, 0, 36, 8), col,
									new Vector2(36, noteLength));
								
								HoldNote holdNote = holdNotes[(int)inst - 1, i];
								
								holdNote.clipHeight = noteLength;
								holdNote.position.Y = y - (CDTXMania.ConfigIni.bReverse[(int)inst] ? noteLength : 0.0f);
								
								holdNote.Draw(ui.LocalTransformMatrix, pChip.bHit, pChip.bIsHittingLongNote);
							}
						}
					}
				}
			}
		}
	}

	public static void DetermineUsedChips(CChip pChip, out bool bChipHasR, out bool bChipHasG, out bool bChipHasB, out bool bChipHasY,
		out bool bChipHasP, out bool bChipIsOpen)
	{
		bChipHasR = false;
		bChipHasG = false;
		bChipHasB = false;
		bChipHasY = false;
		bChipHasP = false;
		bChipIsOpen = false;
		EChannel nChannelNumber = pChip.nChannelNumber;

		switch (nChannelNumber)
		{
			case EChannel.Guitar_Open:
				bChipIsOpen = true;
				break;
			case EChannel.Guitar_xxBxx:
				bChipHasB = true;
				break;
			case EChannel.Guitar_xGxxx:
				bChipHasG = true;
				break;
			case EChannel.Guitar_xGBxx:
				bChipHasG = true;
				bChipHasB = true;
				break;
			case EChannel.Guitar_Rxxxx:
				bChipHasR = true;
				break;
			case EChannel.Guitar_RxBxx:
				bChipHasR = true;
				bChipHasB = true;
				break;
			case EChannel.Guitar_RGxxx:
				bChipHasR = true;
				bChipHasG = true;
				break;
			case EChannel.Guitar_RGBxx:
				bChipHasR = true;
				bChipHasG = true;
				bChipHasB = true;
				break;
			default:
				switch (nChannelNumber)
				{
					case EChannel.Guitar_xxxYx:
						bChipHasY = true;
						break;
					case EChannel.Guitar_xxBYx:
						bChipHasB = true;
						bChipHasY = true;
						break;
					case EChannel.Guitar_xGxYx:
						bChipHasG = true;
						bChipHasY = true;
						break;
					case EChannel.Guitar_xGBYx:
						bChipHasG = true;
						bChipHasB = true;
						bChipHasY = true;
						break;
					case EChannel.Guitar_RxxYx:
						bChipHasR = true;
						bChipHasY = true;
						break;
					case EChannel.Guitar_RxBYx:
						bChipHasR = true;
						bChipHasB = true;
						bChipHasY = true;
						break;
					case EChannel.Guitar_RGxYx:
						bChipHasR = true;
						bChipHasG = true;
						bChipHasY = true;
						break;
					case EChannel.Guitar_RGBYx:
						bChipHasR = true;
						bChipHasG = true;
						bChipHasB = true;
						bChipHasY = true;
						break;
					case EChannel.Guitar_xxxxP:
						bChipHasP = true;
						break;
					case EChannel.Guitar_xxBxP:
						bChipHasB = true;
						bChipHasP = true;
						break;
					case EChannel.Guitar_xGxxP:
						bChipHasG = true;
						bChipHasP = true;
						break;
					case EChannel.Guitar_xGBxP:
						bChipHasG = true;
						bChipHasB = true;
						bChipHasP = true;
						break;
					case EChannel.Guitar_RxxxP:
						bChipHasR = true;
						bChipHasP = true;
						break;

					case EChannel.Bass_Open:
						bChipIsOpen = true;
						break;
					case EChannel.Bass_xxBxx:
						bChipHasB = true;
						break;
					case EChannel.Bass_xGxxx:
						bChipHasG = true;
						break;
					case EChannel.Bass_xGBxx:
						bChipHasG = true;
						bChipHasB = true;
						break;
					case EChannel.Bass_Rxxxx:
						bChipHasR = true;
						break;
					case EChannel.Bass_RxBxx:
						bChipHasR = true;
						bChipHasB = true;
						break;
					case EChannel.Bass_RGxxx:
						bChipHasR = true;
						bChipHasG = true;
						break;
					case EChannel.Bass_RGBxx:
						bChipHasR = true;
						bChipHasG = true;
						bChipHasB = true;
						break;

					case EChannel.Guitar_RxBxP:
						bChipHasR = true;
						bChipHasB = true;
						bChipHasP = true;
						break;
					case EChannel.Guitar_RGxxP:
						bChipHasR = true;
						bChipHasG = true;
						bChipHasP = true;
						break;
					case EChannel.Guitar_RGBxP:
						bChipHasR = true;
						bChipHasG = true;
						bChipHasB = true;
						bChipHasP = true;
						break;
					case EChannel.Guitar_xxxYP:
						bChipHasY = true;
						bChipHasP = true;
						break;
					case EChannel.Guitar_xxBYP:
						bChipHasB = true;
						bChipHasY = true;
						bChipHasP = true;
						break;
					case EChannel.Guitar_xGxYP:
						bChipHasG = true;
						bChipHasY = true;
						bChipHasP = true;
						break;
					case EChannel.Guitar_xGBYP:
						bChipHasG = true;
						bChipHasB = true;
						bChipHasY = true;
						bChipHasP = true;
						break;
					case EChannel.Bass_xxxYx:
						bChipHasY = true;
						break;
					case EChannel.Bass_xxBYx:
						bChipHasB = true;
						bChipHasY = true;
						break;

					case EChannel.Bass_xGxYx:
						bChipHasG = true;
						bChipHasY = true;
						break;
					case EChannel.Bass_xGBYx:
						bChipHasG = true;
						bChipHasB = true;
						bChipHasY = true;
						break;
					case EChannel.Bass_RxxYx:
						bChipHasR = true;
						bChipHasY = true;
						break;
					case EChannel.Bass_RxBYx:
						bChipHasR = true;
						bChipHasB = true;
						bChipHasY = true;
						break;
					case EChannel.Bass_RGxYx:
						bChipHasR = true;
						bChipHasG = true;
						bChipHasY = true;
						break;
					case EChannel.Bass_RGBYx:
						bChipHasR = true;
						bChipHasG = true;
						bChipHasB = true;
						bChipHasY = true;
						break;
					case EChannel.Bass_xxxxP:
						bChipHasP = true;
						break;
					case EChannel.Bass_xxBxP:
						bChipHasB = true;
						bChipHasP = true;
						break;
					case EChannel.Guitar_RxxYP:
						bChipHasR = true;
						bChipHasY = true;
						bChipHasP = true;
						break;
					case EChannel.Guitar_RxBYP:
						bChipHasR = true;
						bChipHasB = true;
						bChipHasY = true;
						bChipHasP = true;
						break;
					case EChannel.Guitar_RGxYP:
						bChipHasR = true;
						bChipHasG = true;
						bChipHasY = true;
						bChipHasP = true;
						break;
					case EChannel.Guitar_RGBYP:
						bChipHasR = true;
						bChipHasG = true;
						bChipHasB = true;
						bChipHasY = true;
						bChipHasP = true;
						break;


					case EChannel.Bass_xGxxP:
						bChipHasG = true;
						bChipHasP = true;
						break;
					case EChannel.Bass_xGBxP:
						bChipHasG = true;
						bChipHasB = true;
						bChipHasP = true;
						break;

					case EChannel.Bass_RxxxP:
						bChipHasR = true;
						bChipHasP = true;
						break;

					case EChannel.Bass_RxBxP:
						bChipHasR = true;
						bChipHasB = true;
						bChipHasP = true;
						break;
					case EChannel.Bass_RGxxP:
						bChipHasR = true;
						bChipHasG = true;
						bChipHasP = true;
						break;
					case EChannel.Bass_RGBxP:
						bChipHasR = true;
						bChipHasG = true;
						bChipHasB = true;
						bChipHasP = true;
						break;
					case EChannel.Bass_xxxYP:
						bChipHasY = true;
						bChipHasP = true;
						break;
					case EChannel.Bass_xxBYP:
						bChipHasB = true;
						bChipHasY = true;
						bChipHasP = true;
						break;
					case EChannel.Bass_xGxYP:
						bChipHasG = true;
						bChipHasY = true;
						bChipHasP = true;
						break;
					case EChannel.Bass_xGBYP:
						bChipHasG = true;
						bChipHasB = true;
						bChipHasY = true;
						bChipHasP = true;
						break;
					case EChannel.Bass_RxxYP:
						bChipHasR = true;
						bChipHasY = true;
						bChipHasP = true;
						break;
					case EChannel.Bass_RxBYP:
						bChipHasR = true;
						bChipHasB = true;
						bChipHasY = true;
						bChipHasP = true;
						break;
					case EChannel.Bass_RGxYP:
						bChipHasR = true;
						bChipHasG = true;
						bChipHasY = true;
						bChipHasP = true;
						break;
					case EChannel.Bass_RGBYP:
						bChipHasR = true;
						bChipHasG = true;
						bChipHasB = true;
						bChipHasY = true;
						bChipHasP = true;
						break;
				}

				break;
		}
	}
}