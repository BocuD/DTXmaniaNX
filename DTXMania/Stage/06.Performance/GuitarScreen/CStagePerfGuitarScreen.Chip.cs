using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using FDK;

namespace DTXMania;

internal partial class CStagePerfGuitarScreen
{
	protected override void tUpdateAndDraw_Chip_GuitarBass(CConfigIni configIni, ref CDTX dTX, ref CChip pChip,
		EInstrumentPart inst)
	{
		int barYNormal = nJudgeLinePosY[(int)inst] + 10;
		int barYReverse = nJudgeLinePosY[(int)inst] + 1;
		int instIndex = (int)inst;
		if (configIni.bGuitarEnabled)
		{
			#region [ Hidden/Sudden処理 ]

			#region [ Sudden処理 ]

			if ((CDTXMania.ConfigIni.nHidSud[instIndex] == 2) || (CDTXMania.ConfigIni.nHidSud[instIndex] == 3))
			{
				if (pChip.nDistanceFromBar[instIndex] < 250)
				{
					pChip.bVisible = true;
					pChip.nTransparency = 0xff;
				}
				else if (pChip.nDistanceFromBar[instIndex] < 300)
				{
					pChip.bVisible = true;
					pChip.nTransparency = 0xff - ((int)(((pChip.nDistanceFromBar[instIndex] - 250) * 255.0) / 75.0));
				}
				else
				{
					pChip.bVisible = false;
					pChip.nTransparency = 0;
				}
			}

			#endregion

			#region [ Hidden処理 ]

			if ((CDTXMania.ConfigIni.nHidSud[instIndex] == 1) || (CDTXMania.ConfigIni.nHidSud[instIndex] == 3))
			{
				if (pChip.nDistanceFromBar[instIndex] < 150)
				{
					pChip.bVisible = false;
				}
				else if (pChip.nDistanceFromBar[instIndex] < 200)
				{
					pChip.bVisible = true;
					pChip.nTransparency = (int)(((pChip.nDistanceFromBar[instIndex] - 150) * 255.0) / 75.0);
				}
			}

			#endregion

			#region [ ステルス処理 ]

			if (CDTXMania.ConfigIni.nHidSud[instIndex] == 4)
			{
				pChip.bVisible = false;
			}

			if (txChip != null)
			{
				//txChip.nTransparency = pChip.nTransparency;
			}

			#endregion

			#endregion

			DetermineUsedChips(pChip, 
				out bool bChipHasR, 
				out bool bChipHasG, 
				out bool bChipHasB, 
				out bool bChipHasY,
				out bool bChipHasP, 
				out bool bChipIsOpen);

			#region [ chip描画 ]

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

			#endregion

			//if ( ( configIni.bAutoPlay.Guitar && !pChip.bHit ) && ( pChip.nDistanceFromBar.Guitar < 0 ) )
			//if ( ( !pChip.bHit ) && ( pChip.nDistanceFromBar[ instIndex ] < 0 ) )

			// #35411 2015.08.20 chnmr0 modified
			// 従来のAUTO処理に加えてプレーヤーゴーストの再生機能を追加
			bool autoPlayCondition = (!pChip.bHit) && (pChip.nDistanceFromBar[instIndex] < 0);
			if (autoPlayCondition)
			{
				//cInvisibleChip.StartSemiInvisible( inst );
			}

			bool autoPick = (inst == EInstrumentPart.GUITAR) ? bIsAutoPlay.GtPick : bIsAutoPlay.BsPick;
			autoPlayCondition = !pChip.bHit && autoPick;
			long ghostLag = 0;
			bool bUsePerfectGhost = true;

			if ((pChip.eInstrumentPart == EInstrumentPart.GUITAR || pChip.eInstrumentPart == EInstrumentPart.BASS) &&
			    CDTXMania.ConfigIni.eAutoGhost[(int)(pChip.eInstrumentPart)] != EAutoGhostData.PERFECT &&
			    CDTXMania.listAutoGhostLag[(int)pChip.eInstrumentPart] != null &&
			    0 <= pChip.n楽器パートでの出現順 &&
			    pChip.n楽器パートでの出現順 < CDTXMania.listAutoGhostLag[(int)pChip.eInstrumentPart].Count)
			{
				// #35411 (mod) Ghost data が有効なので 従来のAUTOではなくゴーストのラグを利用
				// 発生時刻と現在時刻からこのタイミングで演奏するかどうかを決定
				ghostLag = CDTXMania.listAutoGhostLag[(int)pChip.eInstrumentPart][pChip.n楽器パートでの出現順];
				bool resetCombo = ghostLag > 255;
				ghostLag = (ghostLag & 255) - 128;
				ghostLag -= (pChip.eInstrumentPart == EInstrumentPart.GUITAR
					? nInputAdjustTimeMs.Guitar
					: nInputAdjustTimeMs.Bass);
				autoPlayCondition &= (pChip.nPlaybackTimeMs + ghostLag <= CSoundManager.rcPerformanceTimer.n現在時刻ms);
				if (resetCombo && autoPlayCondition)
				{
					actCombo.nCurrentCombo[(int)pChip.eInstrumentPart] = 0;
				}

				bUsePerfectGhost = false;
			}

			if (bUsePerfectGhost)
			{
				// 従来のAUTOを使用する場合
				autoPlayCondition &= (pChip.nDistanceFromBar[instIndex] < 0);
			}

			if (autoPlayCondition)
			{
				int lo = (inst == EInstrumentPart.GUITAR) ? 0 : 5; // lane offset
				bool autoR = (inst == EInstrumentPart.GUITAR) ? bIsAutoPlay.GtR : bIsAutoPlay.BsR;
				bool autoG = (inst == EInstrumentPart.GUITAR) ? bIsAutoPlay.GtG : bIsAutoPlay.BsG;
				bool autoB = (inst == EInstrumentPart.GUITAR) ? bIsAutoPlay.GtB : bIsAutoPlay.BsB;
				bool autoY = (inst == EInstrumentPart.GUITAR) ? bIsAutoPlay.GtY : bIsAutoPlay.BsY;
				bool autoP = (inst == EInstrumentPart.GUITAR) ? bIsAutoPlay.GtP : bIsAutoPlay.BsP;
				bool pushingR = CDTXMania.Pad.bPressing(inst, EPad.R);
				bool pushingG = CDTXMania.Pad.bPressing(inst, EPad.G);
				bool pushingB = CDTXMania.Pad.bPressing(inst, EPad.B);
				bool pushingY = CDTXMania.Pad.bPressing(inst, EPad.Y);
				bool pushingP = CDTXMania.Pad.bPressing(inst, EPad.P);

				#region [ Chip Fire effects (auto時用) ]

				// autoPickでない時の処理は、 tHandleInput_GuitarBass(EInstrumentPart) で行う
				bool bSuccessOPEN = bChipIsOpen && (autoR || !pushingR) && (autoG || !pushingG) &&
				                    (autoB || !pushingB) && (autoY || !pushingY) && (autoP || !pushingP);
				if ((bChipHasR && (autoR || pushingR) && autoPick) || bSuccessOPEN && autoPick)
				{
					actChipFireGB[(int)inst - 1].Start(0, pChip);
				}

				if ((bChipHasG && (autoG || pushingG) && autoPick) || bSuccessOPEN && autoPick)
				{
					actChipFireGB[(int)inst - 1].Start(1, pChip);
				}

				if ((bChipHasB && (autoB || pushingB) && autoPick) || bSuccessOPEN && autoPick)
				{
					actChipFireGB[(int)inst - 1].Start(2, pChip);
				}

				if ((bChipHasY && (autoY || pushingY) && autoPick) || bSuccessOPEN && autoPick)
				{
					actChipFireGB[(int)inst - 1].Start(3, pChip);
				}

				if ((bChipHasP && (autoP || pushingP) && autoPick) || bSuccessOPEN && autoPick)
				{
					actChipFireGB[(int)inst - 1].Start(4, pChip);
				}

				#endregion

				#region [ autopick ]

				if (autoPick)
				{
					bool bMiss = true;
					if (bChipHasR == autoR && bChipHasG == autoG && bChipHasB == autoB && bChipHasY == autoY &&
					    bChipHasP == autoP) // autoレーンとチップレーン一致時はOK
					{
						// この条件を加えないと、同時に非autoレーンを押下している時にNGとなってしまう。
						bMiss = false;
					}
					else if ((autoR || (bChipHasR == pushingR)) && (autoG || (bChipHasG == pushingG)) &&
					         (autoB || (bChipHasB == pushingB)) && (autoY || (bChipHasY == pushingY)) &&
					         (autoP || bChipHasP == pushingP))
						// ( bChipHasR == ( pushingR | autoR ) ) && ( bChipHasG == ( pushingG | autoG ) ) && ( bChipHasB == ( pushingB | autoB ) ) )
					{
						bMiss = false;
					}
					else if ((bChipIsOpen && (!pushingR | autoR) && (!pushingG | autoG) && (!pushingB | autoB) &&
					          (!pushingY | autoY) && (!pushingP | autoP))) // OPEN時
					{
						bMiss = false;
					}

					bool bCurrInstrumentSpecialist = (inst == EInstrumentPart.GUITAR)
						? CDTXMania.ConfigIni.bSpecialist.Guitar
						: CDTXMania.ConfigIni.bSpecialist.Bass;
					pChip.bHit = true;
					tPlaySound(pChip,
						CSoundManager.rcPerformanceTimer.n前回リセットした時のシステム時刻 + pChip.nPlaybackTimeMs + ghostLag, inst,
						dTX.nモニタを考慮した音量(inst), false, bMiss && bCurrInstrumentSpecialist);
					rNextGuitarChip = null;
					if (!bMiss)
					{
						tProcessChipHit(pChip.nPlaybackTimeMs + ghostLag, pChip);
					}
					else
					{
						pChip.nLag = 0; // tProcessChipHit()の引数最後がfalseの時はpChip.nLagを計算しないため、ここでAutoPickかつMissのLag=0を代入
						tProcessChipHit(pChip.nPlaybackTimeMs + ghostLag, pChip, false);
					}

					//int chWailingChip = ( inst == EInstrumentPart.GUITAR ) ? (int)EChannel.Guitar_Wailing : (int)EChannel.Bass_Wailing;
					//CChip item = this.r指定時刻に一番近い未ヒットChip( pChip.nPlaybackTimeMs + ghostLag, chWailingChip, this.nInputAdjustTimeMs[ instIndex ], 140 );

					//New method for Guitar and Bass
					EChannel search = ((inst == EInstrumentPart.GUITAR)
						? EChannel.Guitar_Wailing
						: EChannel.Bass_Wailing);
					CChip item = r指定時刻に一番近いChip(pChip.nPlaybackTimeMs + ghostLag, search, nInputAdjustTimeMs[instIndex],
						140);

					if (item != null && !bMiss)
					{
						queWailing[instIndex].Enqueue(item);
					}
				}

				#endregion

				// #35411 modify end
			}

			if (pChip.eInstrumentPart == EInstrumentPart.GUITAR && CDTXMania.ConfigIni.bGraph有効.Guitar)
			{
				#region[ ギターゴースト ]

				if (CDTXMania.ConfigIni.eTargetGhost.Guitar != ETargetGhostData.NONE &&
				    CDTXMania.listTargetGhsotLag.Guitar != null)
				{
					double val = 0;
					if (CDTXMania.ConfigIni.eTargetGhost.Guitar == ETargetGhostData.ONLINE)
					{
						if (CDTXMania.DTX.nVisibleChipsCount.Guitar > 0)
						{
							// Online Stats の計算式
							val = 100 *
								(nヒット数_TargetGhost.Guitar.Perfect * 17 +
								 nヒット数_TargetGhost.Guitar.Great * 7 +
								 n最大コンボ数_TargetGhost.Guitar * 3) / (20.0 * CDTXMania.DTX.nVisibleChipsCount.Guitar);
						}
					}
					else
					{
						if (CDTXMania.ConfigIni.nSkillMode == 0)
						{
							val = CScoreIni.tCalculatePlayingSkillOld(
								CDTXMania.DTX.nVisibleChipsCount.Guitar,
								nヒット数_TargetGhost.Guitar.Perfect,
								nヒット数_TargetGhost.Guitar.Great,
								nヒット数_TargetGhost.Guitar.Good,
								nヒット数_TargetGhost.Guitar.Poor,
								nヒット数_TargetGhost.Guitar.Miss,
								n最大コンボ数_TargetGhost.Guitar,
								EInstrumentPart.GUITAR, new STAUTOPLAY());
						}
						else
						{
							val = CScoreIni.tCalculatePlayingSkill(
								CDTXMania.DTX.nVisibleChipsCount.Guitar,
								nヒット数_TargetGhost.Guitar.Perfect,
								nヒット数_TargetGhost.Guitar.Great,
								nヒット数_TargetGhost.Guitar.Good,
								nヒット数_TargetGhost.Guitar.Poor,
								nヒット数_TargetGhost.Guitar.Miss,
								n最大コンボ数_TargetGhost.Guitar,
								EInstrumentPart.GUITAR, new STAUTOPLAY());
						}

					}

					if (val < 0) val = 0;
					if (val > 100) val = 100;
					actGraph.dbGraphValue_Goal = val;
				}

				#endregion
			}
			else if (pChip.eInstrumentPart == EInstrumentPart.BASS && CDTXMania.ConfigIni.bGraph有効.Bass)
			{
				#region[ ベースゴースト ]

				if (CDTXMania.ConfigIni.eTargetGhost.Bass != ETargetGhostData.NONE &&
				    CDTXMania.listTargetGhsotLag.Bass != null)
				{
					double val = 0;
					if (CDTXMania.ConfigIni.eTargetGhost.Bass == ETargetGhostData.ONLINE)
					{
						if (CDTXMania.DTX.nVisibleChipsCount.Bass > 0)
						{
							// Online Stats の計算式
							val = 100 *
								(nヒット数_TargetGhost.Bass.Perfect * 17 +
								 nヒット数_TargetGhost.Bass.Great * 7 +
								 n最大コンボ数_TargetGhost.Bass * 3) / (20.0 * CDTXMania.DTX.nVisibleChipsCount.Bass);
						}
					}
					else
					{
						if (CDTXMania.ConfigIni.nSkillMode == 0)
						{
							val = CScoreIni.tCalculatePlayingSkillOld(
								CDTXMania.DTX.nVisibleChipsCount.Bass,
								nヒット数_TargetGhost.Bass.Perfect,
								nヒット数_TargetGhost.Bass.Great,
								nヒット数_TargetGhost.Bass.Good,
								nヒット数_TargetGhost.Bass.Poor,
								nヒット数_TargetGhost.Bass.Miss,
								n最大コンボ数_TargetGhost.Bass,
								EInstrumentPart.BASS, new STAUTOPLAY());
						}
						else
						{
							val = CScoreIni.tCalculatePlayingSkill(
								CDTXMania.DTX.nVisibleChipsCount.Bass,
								nヒット数_TargetGhost.Bass.Perfect,
								nヒット数_TargetGhost.Bass.Great,
								nヒット数_TargetGhost.Bass.Good,
								nヒット数_TargetGhost.Bass.Poor,
								nヒット数_TargetGhost.Bass.Miss,
								n最大コンボ数_TargetGhost.Bass,
								EInstrumentPart.BASS, new STAUTOPLAY());
						}

					}

					if (val < 0) val = 0;
					if (val > 100) val = 100;
					actGraph.dbGraphValue_Goal = val;
				}

				#endregion
			}

			return;
		} // end of "if configIni.bGuitarEnabled"

		if (!pChip.bHit && (pChip.nDistanceFromBar[instIndex] < 0)) // Guitar/Bass無効の場合は、自動演奏する
		{
			pChip.bHit = true;
			tPlaySound(pChip, CSoundManager.rcPerformanceTimer.n前回リセットした時のシステム時刻 + pChip.nPlaybackTimeMs, inst,
				dTX.nモニタを考慮した音量(inst));
		}
	}

	private static void DetermineUsedChips(CChip pChip, out bool bChipHasR, out bool bChipHasG, out bool bChipHasB, out bool bChipHasY,
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