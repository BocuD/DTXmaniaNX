using System.Diagnostics;
using System.Drawing;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using FDK;

namespace DTXMania;

internal partial class CStagePerfDrumsScreen
{
    protected override void UpdateChip_PatternOnly_Drums(CConfigIni configIni, ref CDTX dTX, ref CChip pChip)  // t進行描画_チップ_模様のみ_ドラムス
    {
        if (configIni.bDrumsEnabled)
        {
            #region [ Sudden処理 ]
            if ((CDTXMania.ConfigIni.nHidSud.Drums == 2) || (CDTXMania.ConfigIni.nHidSud.Drums == 3))
            {
                if (pChip.nDistanceFromBar.Drums < 200)
                {
                    pChip.bVisible = true;
                    pChip.nTransparency = 0xff;
                }
                else if (pChip.nDistanceFromBar.Drums < 250)
                {
                    pChip.bVisible = true;
                    pChip.nTransparency = 0xff - ((int)((((double)(pChip.nDistanceFromBar.Drums - 200)) * 255.0) / 50.0));
                }
                else
                {
                    pChip.bVisible = false;
                    pChip.nTransparency = 0;
                }
            }
            #endregion
            #region [ Hidden処理 ]
            if ((CDTXMania.ConfigIni.nHidSud.Drums == 1) || (CDTXMania.ConfigIni.nHidSud.Drums == 3))
            {
                if (pChip.nDistanceFromBar.Drums < 100)
                {
                    pChip.bVisible = false;
                }
                else if (pChip.nDistanceFromBar.Drums < 150)
                {
                    pChip.bVisible = true;
                    pChip.nTransparency = (int)((((double)(pChip.nDistanceFromBar.Drums - 100)) * 255.0) / 50.0);
                }
            }
            #endregion
            #region [ ステルス処理 ]
            if (CDTXMania.ConfigIni.nHidSud.Drums == 4)
            {
                pChip.bVisible = false;
            }
            #endregion

            return;
        }	// end of "if configIni.bDrumsEnabled"
        if (!pChip.bHit && (pChip.nDistanceFromBar.Drums < 0))
        {
            pChip.bHit = true;
        }
    }

    protected override void DrawChip_PatternOnly_Drums(CConfigIni configIni, ref CDTX dTX, ref CChip pChip)
    {
        if (!configIni.bDrumsEnabled)
        {
            return;
        }

        if (!pChip.bHit && pChip.bVisible)
        {
            Color4 col = Color4.White;
            if (txChip != null)
            {
                col.Alpha = pChip.nTransparency / 255.0f;
            }
            int x = nチャンネルtoX座標[pChip.nChannelNumber - EChannel.HiHatClose];

            if (configIni.eLaneType.Drums == EType.A)
            {
                if (configIni.eRDPosition == ERDPosition.RCRD)
                {
                    x = nチャンネルtoX座標[pChip.nChannelNumber - EChannel.HiHatClose];
                }
                else if (configIni.eRDPosition == ERDPosition.RDRC)
                {
                    x = nチャンネルtoX座標改[pChip.nChannelNumber - EChannel.HiHatClose];
                }
            }
            else if (configIni.eLaneType.Drums == EType.B)
            {
                if (configIni.eRDPosition == ERDPosition.RCRD)
                {
                    x = nチャンネルtoX座標B[pChip.nChannelNumber - EChannel.HiHatClose];
                }
                else if (configIni.eRDPosition == ERDPosition.RDRC)
                {
                    x = nチャンネルtoX座標B改[pChip.nChannelNumber - EChannel.HiHatClose];
                }
            }
            else if (configIni.eLaneType.Drums == EType.C)
            {
                if (configIni.eRDPosition == ERDPosition.RCRD)
                {
                    x = nチャンネルtoX座標C[pChip.nChannelNumber - EChannel.HiHatClose];
                }
                else if (configIni.eRDPosition == ERDPosition.RDRC)
                {
                    x = nチャンネルtoX座標C改[pChip.nChannelNumber - EChannel.HiHatClose];
                }
            }
            else if (configIni.eLaneType.Drums == EType.D)
            {
                if (configIni.eRDPosition == ERDPosition.RCRD)
                {
                    x = nチャンネルtoX座標D[pChip.nChannelNumber - EChannel.HiHatClose];
                }
                else if (configIni.eRDPosition == ERDPosition.RDRC)
                {
                    x = nチャンネルtoX座標D改[pChip.nChannelNumber - EChannel.HiHatClose];
                }
            }

            if (configIni.eRDPosition == ERDPosition.RDRC)
            {
                if (configIni.eLaneType.Drums == EType.A)
                {
                    x = nチャンネルtoX座標改[pChip.nChannelNumber - EChannel.HiHatClose];
                }
                else if (configIni.eLaneType.Drums == EType.B)
                {
                    x = nチャンネルtoX座標B改[pChip.nChannelNumber - EChannel.HiHatClose];
                }
            }

            float y = configIni.bReverse.Drums ? (nJudgeLinePosY.Drums + pChip.nDistanceFromBar.Drums) : (nJudgeLinePosY.Drums - pChip.nDistanceFromBar.Drums);
            int num9 = ctChipPatternAnimation.Drums.nCurrentValue;

            switch (pChip.nChannelNumber)
            {
                case EChannel.HiHatClose:
                    x = (x + 0x10) - ((int)((32.0 * pChip.dbChipSizeRatio) / 2.0));
                    if (txChip != null)
                    {
                        txChip.tDraw2D(x - 5, y - 0x20, new RectangleF(60 + 10, 128 + (num9 * 64), 0x2e + 10, 64), col);
                    }
                    break;

                case EChannel.Snare:
                    x = (x + 0x10) - ((int)((32.0 * pChip.dbChipSizeRatio) / 2.0));
                    if (txChip != null)
                    {
                        txChip.tDraw2D(x - 5, y - 32, new RectangleF(0x6a + 20, 128 + (num9 * 64), 0x36 + 10, 64), col);
                    }
                    break;

                case EChannel.BassDrum:
                    x = (x + 0x16) - ((int)((44.0 * pChip.dbChipSizeRatio) / 2.0));
                    if (txChip != null)
                    {
                        txChip.tDraw2D(x - 5, y - 0x20, new RectangleF(0, 128 + (num9 * 0x40), 60 + 10, 0x40), col);
                    }
                    break;

                case EChannel.HighTom:
                    x = (x + 0x10) - ((int)((32.0 * pChip.dbChipSizeRatio) / 2.0));
                    if (txChip != null)
                    {
                        txChip.tDraw2D(x - 5, y - 32, new RectangleF(160 + 30, 128 + (num9 * 0x40), 0x2e + 10, 64), col);
                    }
                    break;

                case EChannel.LowTom:
                    x = (x + 0x10) - ((int)((32.0 * pChip.dbChipSizeRatio) / 2.0));
                    if (txChip != null)
                    {
                        txChip.tDraw2D(x - 5, y - 32, new RectangleF(0xce + 40, 128 + (num9 * 0x40), 0x2e + 10, 64), col);
                    }
                    break;

                case EChannel.Cymbal:
                    x = (x + 19) - ((int)((38.0 * pChip.dbChipSizeRatio) / 2.0));
                    if (txChip != null)
                    {
                        txChip.tDraw2D(x - 5, y - 0x20, new RectangleF(298 + 60, 128 + (num9 * 64), 64 + 10, 64), col);
                    }
                    break;

                case EChannel.FloorTom:
                    x = (x + 0x10) - ((int)((32.0 * pChip.dbChipSizeRatio) / 2.0));
                    if (txChip != null)
                    {
                        txChip.tDraw2D(x - 5, y - 0x20, new RectangleF(0xfc + 50, 128 + (num9 * 64), 0x2e + 10, 0x40), col);
                    }
                    break;

                case EChannel.HiHatOpen:
                    x = (x + 13) - ((int)((26.0 * pChip.dbChipSizeRatio) / 2.0));
                    if (txChip != null)
                    {
                        switch (configIni.eHHOGraphics.Drums)
                        {
                            case EType.A:
                                x = (x + 14) - ((int)((26.0 * pChip.dbChipSizeRatio) / 2.0));
                                txChip.tDraw2D(x - 5, y - 0x20, new RectangleF(0x200 + 100, 128 + (num9 * 64), 0x26 + 10, 64), col);
                                break;


                            case EType.C:
                                x = (x + 13) - ((int)((32.0 * pChip.dbChipSizeRatio) / 2.0));
                                txChip.tDraw2D(x - 5, y - 0x20, new RectangleF(60 + 10, 128 + (num9 * 64), 0x2e + 10, 64), col);
                                break;
                        }
                    }
                    break;

                case EChannel.RideCymbal:
                    x = (x + 13) - ((int)((26.0 * pChip.dbChipSizeRatio) / 2.0));
                    if (txChip != null)
                    {
                        txChip.tDraw2D(x - 5, y - 0x20, new RectangleF(0x16a + 70, 128 + (num9 * 64), 0x26 + 10, 0x40), col);
                    }
                    break;

                case EChannel.LeftCymbal:
                    x = (x + 0x13) - ((int)((38.0 * pChip.dbChipSizeRatio) / 2.0));
                    if (txChip != null)
                    {
                        txChip.tDraw2D(x - 5, y - 0x20, new RectangleF(448 + 90, 128 + (num9 * 64), 64 + 10, 64), col);
                    }
                    break;

                case EChannel.LeftPedal:
                    x = (x + 0x13) - ((int)((38.0 * pChip.dbChipSizeRatio) / 2.0));
                    if (txChip != null)
                    {
                        txChip.tDraw2D(x - 5, y - 0x20, new RectangleF(550 + 110, 128 + (num9 * 64), 0x30 + 10, 64), col);
                    }
                    break;

                case EChannel.LeftBassDrum:
                    x = (x + 0x13) - ((int)((38.0 * pChip.dbChipSizeRatio) / 2.0));
                    if (txChip != null)
                    {

                        if (configIni.eLBDGraphics.Drums == EType.A)
                        {
                            txChip.tDraw2D(x - 5, y - 0x20, new RectangleF(550 + 110, 128 + (num9 * 64), 0x30, 0x40), col);
                        }
                        else if (configIni.eLBDGraphics.Drums == EType.B)
                        {
                            txChip.tDraw2D(x - 5, y - 0x20, new RectangleF(400 + 80, 128 + (num9 * 64), 0x30 + 10, 0x40), col);
                        }
                    }
                    break;
            }
        }
    }
    protected override void UpdateChip_Drums( CConfigIni configIni, ref CDTX dTX, ref CChip pChip )
    {
        if ( configIni.bDrumsEnabled )
        {
            #region [ Sudden処理 ]
            if ( ( CDTXMania.ConfigIni.nHidSud.Drums == 2 ) || ( CDTXMania.ConfigIni.nHidSud.Drums == 3 ) )
            {
                if ( pChip.nDistanceFromBar.Drums < 200 )
                {
                    pChip.bVisible = true;
                    pChip.nTransparency = 0xff;
                }
                else if ( pChip.nDistanceFromBar.Drums < 250 )
                {
                    pChip.bVisible = true;
                    pChip.nTransparency = 0xff - ( (int) ( ( ( (double) ( pChip.nDistanceFromBar.Drums - 200 ) ) * 255.0 ) / 50.0 ) );
                }
                else
                {
                    pChip.bVisible = false;
                    pChip.nTransparency = 0;
                }
            }
            #endregion
            #region [ Hidden処理 ]
            if ( ( CDTXMania.ConfigIni.nHidSud.Drums == 1 ) || ( CDTXMania.ConfigIni.nHidSud.Drums == 3 ) )
            {
                if ( pChip.nDistanceFromBar.Drums < 100 )
                {
                    pChip.bVisible = false;
                }
                else if ( pChip.nDistanceFromBar.Drums < 150 )
                {
                    pChip.bVisible = true;
                    pChip.nTransparency = (int) ( ( ( (double) ( pChip.nDistanceFromBar.Drums - 100 ) ) * 255.0 ) / 50.0 );
                }
            }
            #endregion
            #region [ ステルス処理 ]
            if ( CDTXMania.ConfigIni.nHidSud.Drums == 4 )
            {
                pChip.bVisible = false;
            }
            #endregion

            int indexSevenLanes = nチャンネル0Atoレーン07[ pChip.nChannelNumber - EChannel.HiHatClose];
            // #35411 chnmr0 modified
            bool autoPlayCondition = ( configIni.bAutoPlay[ indexSevenLanes ] && !pChip.bHit );
            bool UsePerfectGhost = true;
            long ghostLag = 0;

            if ( CDTXMania.ConfigIni.eAutoGhost.Drums != EAutoGhostData.PERFECT &&
                CDTXMania.listAutoGhostLag.Drums != null &&
                0 <= pChip.n楽器パートでの出現順 && pChip.n楽器パートでの出現順 < CDTXMania.listAutoGhostLag.Drums.Count)

            {
                // ゴーストデータが有効 : ラグに合わせて判定
                ghostLag = CDTXMania.listAutoGhostLag.Drums[pChip.n楽器パートでの出現順];
                ghostLag = (ghostLag & 255) - 128;
                ghostLag -= nInputAdjustTimeMs.Drums;
                autoPlayCondition &= !pChip.bHit && (ghostLag + pChip.nPlaybackTimeMs <= AudioMixer.Timer.nCurrentTime);
                UsePerfectGhost = false;
            }
            if ( UsePerfectGhost )
            {
                // 従来の AUTO : バー下で判定
                autoPlayCondition &= ( pChip.nDistanceFromBar.Drums < 0 );
            }

            if ( autoPlayCondition )
            {
                pChip.bHit = true;
                actLaneFlushD.Start( (ELane) indexSevenLanes, ( (float) CInputManager.nDefaultVelocity ) / 127f );
                bool flag = bInFillIn;
                bool flag2 = bInFillIn && bフィルイン区間の最後のChipである( pChip );
                // #31602 2013.6.24 yyagi 判定ラインの表示位置をずらしたら、チップのヒットエフェクトの表示もずらすために、nJudgeLine..を追加
                actChipFireD.Start( (ELane)indexSevenLanes, flag, flag2, flag2, nJudgeLinePosY_delta.Drums );
                actPad.Hit( nチャンネル0Atoパッド08[ pChip.nChannelNumber - EChannel.HiHatClose] );
                tPlaySound( pChip, AudioMixer.Timer.nResetAtMs + pChip.nPlaybackTimeMs + ghostLag, EInstrumentPart.DRUMS, dTX.nモニタを考慮した音量( EInstrumentPart.DRUMS ) );
                tProcessChipHit(pChip.nPlaybackTimeMs + ghostLag, pChip);
            }
            // #35411 modify end
                
            // #35411 2015.08.21 chnmr0 add
            // 目標値グラフにゴーストの達成率を渡す
            if (CDTXMania.ConfigIni.eTargetGhost.Drums != ETargetGhostData.NONE &&
                CDTXMania.listTargetGhsotLag.Drums != null)
            {
                double val = 0;
                if (CDTXMania.ConfigIni.eTargetGhost.Drums == ETargetGhostData.ONLINE)
                {
                    if (CDTXMania.DTX.nVisibleChipsCount.Drums > 0)
                    {
                        // Online Stats の計算式
                        val = 100 *
                            (nヒット数_TargetGhost.Drums.Perfect * 17 +
                             nヒット数_TargetGhost.Drums.Great * 7 +
                             n最大コンボ数_TargetGhost.Drums * 3) / (20.0 * CDTXMania.DTX.nVisibleChipsCount.Drums);
                    }
                }
                else
                {
                    if ( CDTXMania.ConfigIni.nSkillMode == 0 )
                    {
                        val = CScoreIni.tCalculatePlayingSkillOld(
                            CDTXMania.DTX.nVisibleChipsCount.Drums,
                            nヒット数_TargetGhost.Drums.Perfect,
                            nヒット数_TargetGhost.Drums.Great,
                            nヒット数_TargetGhost.Drums.Good,
                            nヒット数_TargetGhost.Drums.Poor,
                            nヒット数_TargetGhost.Drums.Miss,
                            n最大コンボ数_TargetGhost.Drums,
                            EInstrumentPart.DRUMS, new STAUTOPLAY());
                    }
                    else
                    {
                        val = CScoreIni.tCalculatePlayingSkill(
                            CDTXMania.DTX.nVisibleChipsCount.Drums,
                            nヒット数_TargetGhost.Drums.Perfect,
                            nヒット数_TargetGhost.Drums.Great,
                            nヒット数_TargetGhost.Drums.Good,
                            nヒット数_TargetGhost.Drums.Poor,
                            nヒット数_TargetGhost.Drums.Miss,
                            n最大コンボ数_TargetGhost.Drums,
                            EInstrumentPart.DRUMS, new STAUTOPLAY());
                    }

                }
                if (val < 0) val = 0;
                if (val > 100) val = 100;
                actGraph.dbGraphValue_Goal = val;
            }
            return;
        }	// end of "if configIni.bDrumsEnabled"
        if ( !pChip.bHit && ( pChip.nDistanceFromBar.Drums < 0 ) )
        {
            tPlaySound( pChip, AudioMixer.Timer.nResetAtMs + pChip.nPlaybackTimeMs, EInstrumentPart.DRUMS, dTX.nモニタを考慮した音量( EInstrumentPart.DRUMS ) );
            pChip.bHit = true;
        }
    }

    protected override void DrawChip_Drums( CConfigIni configIni, ref CDTX dTX, ref CChip pChip )
    {
        if (!configIni.bDrumsEnabled)
        {
            return;
        }

        if ( !pChip.bHit && pChip.bVisible )
        {
            Color4 col = Color4.White;
            if ( txChip != null )
            {
                col.Alpha = pChip.nTransparency / 255.0f;
            }
            int x = nチャンネルtoX座標[ pChip.nChannelNumber - EChannel.HiHatClose];

            if ( configIni.eLaneType.Drums == EType.A )
            {
                if (configIni.eRDPosition == ERDPosition.RCRD)
                {
                    x = nチャンネルtoX座標[ pChip.nChannelNumber - EChannel.HiHatClose];
                }
                else if ( configIni.eRDPosition == ERDPosition.RDRC )
                {
                    x = nチャンネルtoX座標改[ pChip.nChannelNumber - EChannel.HiHatClose];
                }
            }
            else if ( configIni.eLaneType.Drums == EType.B )
            {
                if ( configIni.eRDPosition == ERDPosition.RCRD )
                {
                    x = nチャンネルtoX座標B[ pChip.nChannelNumber - EChannel.HiHatClose];
                }
                else if ( configIni.eRDPosition == ERDPosition.RDRC )
                {
                    x = nチャンネルtoX座標B改[ pChip.nChannelNumber - EChannel.HiHatClose];
                }
            }
            else if ( configIni.eLaneType.Drums == EType.C )
            {
                if ( configIni.eRDPosition == ERDPosition.RCRD )
                {
                    x = nチャンネルtoX座標C[ pChip.nChannelNumber - EChannel.HiHatClose];
                }
                else if ( configIni.eRDPosition == ERDPosition.RDRC )
                {
                    x = nチャンネルtoX座標C改[ pChip.nChannelNumber - EChannel.HiHatClose];
                }
            }
            else if ( configIni.eLaneType.Drums == EType.D )
            {
                if ( configIni.eRDPosition == ERDPosition.RCRD )
                {
                    x = nチャンネルtoX座標D[ pChip.nChannelNumber - EChannel.HiHatClose];
                }
                else if ( configIni.eRDPosition == ERDPosition.RDRC )
                {
                    x = nチャンネルtoX座標D改[ pChip.nChannelNumber - EChannel.HiHatClose];
                }
            }

            if ( configIni.eRDPosition == ERDPosition.RDRC )
            {
                if ( configIni.eLaneType.Drums == EType.A )
                {
                    x = nチャンネルtoX座標改[ pChip.nChannelNumber - EChannel.HiHatClose];
                }
                else if ( configIni.eLaneType.Drums == EType.B )
                {
                    x = nチャンネルtoX座標B改[ pChip.nChannelNumber - EChannel.HiHatClose];
                }
            }

            float y = configIni.bReverse.Drums ? ( nJudgeLinePosY.Drums + pChip.nDistanceFromBar.Drums ) : ( nJudgeLinePosY.Drums - pChip.nDistanceFromBar.Drums );
            //todo: scale the chip by pChip.dbChipSizeRatio
            int num9 = ctChipPatternAnimation.Drums.nCurrentValue;

            switch( pChip.nChannelNumber )
            {
                case EChannel.HiHatClose:
                    x = ( x + 0x10 ) - ( ( int )( ( 32.0 * pChip.dbChipSizeRatio ) / 2.0 ) );
                    if ( txChip != null )
                    {
                        txChip.tDraw2D(x - 5, y - 32, new RectangleF( 60 + 10, 0, 0x2e + 10, 64 ), col);

                        if ( pChip.bBonusChip )
                            txChip.tDraw2D(x - 5, y - 32, new RectangleF( 60 + 10, 64, 0x2e + 10, 64 ), col);
                    }
                    break;

                case EChannel.Snare:
                    x = ( x + 0x10 ) - ( ( int )( ( 32.0 * pChip.dbChipSizeRatio ) / 2.0 ) );
                    if (txChip != null)
                    {
                        txChip.tDraw2D(x - 5, y - 32, new RectangleF( 0x6a + 20, 0, 0x36 +10, 64 ), col);

                        if ( pChip.bBonusChip )
                            txChip.tDraw2D(x - 5, y - 32, new RectangleF( 0x6a + 20, 64, 0x36 + 10, 64 ), col);
                    }
                    break;

                case EChannel.BassDrum:
                    x = ( x + 0x16 ) - ( ( int )( ( 44.0 * pChip.dbChipSizeRatio ) / 2.0 ) );
                    if ( txChip != null )
                    {
                        txChip.tDraw2D(x - 5, y - 32, new RectangleF( 0, 0, 60 + 10, 64 ), col);

                        if ( pChip.bBonusChip )
                            txChip.tDraw2D(x - 5, y - 32, new RectangleF( 0, 64, 60 + 10, 64 ), col);
                    }
                    break;

                case EChannel.HighTom:
                    x = ( x + 0x10 ) - ( ( int )( ( 32.0 * pChip.dbChipSizeRatio ) / 2.0 ) );
                    if ( txChip != null )
                    {
                        txChip.tDraw2D(x - 5, y - 32, new RectangleF( 160 + 30, 0, 0x2e + 10, 64 ), col);

                        if ( pChip.bBonusChip )
                            txChip.tDraw2D(x - 5, y - 32, new RectangleF( 160 + 30, 64, 0x2e + 10, 64 ), col);
                    }
                    break;

                case EChannel.LowTom:
                    x = ( x + 0x10 ) - ( ( int )( ( 32.0 * pChip.dbChipSizeRatio ) / 2.0 ) );
                    if (txChip != null)
                    {
                        txChip.tDraw2D(x - 5, y - 32, new RectangleF( 0xce + 40, 0, 0x2e + 10, 64 ), col);

                        if ( pChip.bBonusChip )
                            txChip.tDraw2D(x - 5, y - 32, new RectangleF( 0xce + 40, 64, 0x2e + 10, 64 ), col);
                    }
                    break;

                case EChannel.Cymbal:
                    x = ( x + 19 ) - ( ( int )( ( 38.0 * pChip.dbChipSizeRatio ) / 2.0 ) );
                    if ( txChip != null )
                    {
                        txChip.tDraw2D(x - 5, y - 32, new RectangleF( 298 + 60, 0, 0x40 + 10, 64 ), col);

                        if ( pChip.bBonusChip )
                            txChip.tDraw2D(x - 5, y - 32, new RectangleF( 298 + 60, 64, 0x40 + 10, 64 ), col);
                    }
                    break;

                case EChannel.FloorTom:
                    x = ( x + 0x10 ) - ( ( int )( ( 32.0 * pChip.dbChipSizeRatio ) / 2.0 ) );
                    if ( txChip != null )
                    {
                        txChip.tDraw2D(x - 5, y - 32, new RectangleF( 0xfc + 50, 0, 0x2e + 10, 64 ), col);

                        if ( pChip.bBonusChip )
                            txChip.tDraw2D(x - 5, y - 32, new RectangleF( 0xfc + 50, 64, 0x2e + 10, 64 ), col);
                    }
                    break;

                case EChannel.HiHatOpen:
                    x = ( x + 13 ) - ( ( int )( ( 26.0 * pChip.dbChipSizeRatio ) / 2.0 ) );
                    if ( txChip != null )
                    {
                        switch( configIni.eHHOGraphics.Drums )
                        {
                            case EType.A:
                                x = ( x + 14 ) - ( ( int )( ( 26.0 * pChip.dbChipSizeRatio ) / 2.0 ) );
                                txChip.tDraw2D(x - 5, y - 32, new RectangleF( 0x200 + 100, 0, 0x26 + 10, 64 ), col);
                                if ( pChip.bBonusChip )
                                    txChip.tDraw2D(x - 5, y - 32, new RectangleF( 0x200 + 100, 64, 0x26 + 10, 64 ), col);
                                break;

                            case EType.B:
                                x = ( x + 14 ) - ( ( int )( ( 26.0 * pChip.dbChipSizeRatio ) / 2.0 ) );
                                txChip.tDraw2D(x - 5, y - 32, new RectangleF( 0x200 + 100, 0, 0x26 + 10, 64 ), col);
                                if ( pChip.bBonusChip )
                                    txChip.tDraw2D(x - 5, y - 32, new RectangleF( 0x200 + 100, 64, 0x26 + 10, 64 ), col);
                                break;

                            case EType.C:
                                x = ( x + 13 ) - ( ( int )( ( 32.0 * pChip.dbChipSizeRatio ) / 2.0 ) );
                                txChip.tDraw2D(x - 5, y - 32, new RectangleF( 60 + 10, 0, 0x2e + 10, 64 ), col);
                                if ( pChip.bBonusChip )
                                    txChip.tDraw2D(x - 5, y - 32, new RectangleF( 60 + 100, 64, 0x2e + 10, 64 ), col);
                                break;
                        }
                    }
                    break;

                case EChannel.RideCymbal:
                    x = ( x + 13 ) - ( ( int )( ( 26.0 * pChip.dbChipSizeRatio ) / 2.0 ) );
                    if ( txChip != null )
                    {
                        txChip.tDraw2D(x - 5, y - 32, new RectangleF( 0x16a + 70, 0, 0x26 + 10, 64 ), col);

                        if ( pChip.bBonusChip )
                            txChip.tDraw2D(x - 5, y - 0x20, new RectangleF( 0x16a + 70, 64, 0x26 + 10, 0x40 ), col);
                    }
                    break;

                case EChannel.LeftCymbal:
                    x = ( x + 0x13 ) - ( ( int )( ( 38.0 * pChip.dbChipSizeRatio ) / 2.0 ) );
                    if (txChip != null)
                    {
                        txChip.tDraw2D(x - 5, y - 32, new RectangleF( 448 + 90, 0, 64 + 10, 64 ), col);

                        if ( pChip.bBonusChip )
                            txChip.tDraw2D(x - 5, y - 0x20, new RectangleF( 448 + 90, 64, 64 + 10, 64 ), col);
                    }
                    break;

                case EChannel.LeftPedal:
                    x = ( x + 0x13 ) - ( ( int )( ( 38.0 * pChip.dbChipSizeRatio ) / 2.0 ) );
                    if (txChip != null)
                    {
                        txChip.tDraw2D(x - 5, y - 32, new RectangleF( 550 + 110, 0, 0x30 + 10, 64 ), col);
                            
                        if ( pChip.bBonusChip )
                            txChip.tDraw2D(x - 5, y - 32, new RectangleF( 550 + 110, 64, 0x30 + 10, 64 ), col);
                    }
                    break;

                case EChannel.LeftBassDrum:
                    x = ( x + 0x13 ) - ( ( int )( ( 38.0 * pChip.dbChipSizeRatio ) / 2.0 ) );
                    if ( txChip != null )
                    {
                        if ( configIni.eLBDGraphics.Drums == EType.A )
                        {
                            txChip.tDraw2D(x - 5, y - 32, new RectangleF( 550 + 110, 0, 0x30 + 10, 64 ), col);
                            if ( pChip.bBonusChip )
                                txChip.tDraw2D(x - 5, y - 32, new RectangleF( 550 + 110, 64, 0x30 + 10, 64 ), col);
                        }
                        else if ( configIni.eLBDGraphics.Drums == EType.B )
                        {
                            txChip.tDraw2D(x - 5, y - 32, new RectangleF( 400 + 80, 0, 0x30 + 10, 64 ), col);
                            if ( pChip.bBonusChip )
                                txChip.tDraw2D(x - 5, y - 32, new RectangleF( 400 + 80, 64, 0x30 + 10, 64 ), col);
                        }
                    }
                    break;
            }
        }
    }
    protected override void UpdateChip_GuitarBass(CConfigIni configIni, ref CDTX dTX, ref CChip pChip, EInstrumentPart inst)
    {
        int instIndex = (int)inst;
        
        //auto play guitar and bass chips
        if ( !pChip.bHit && ( pChip.nDistanceFromBar[ instIndex ] < 0 ) )
        {
            pChip.bHit = true;
            tPlaySound( pChip, AudioMixer.Timer.nResetAtMs + pChip.nPlaybackTimeMs, inst, dTX.nモニタを考慮した音量( inst ) );
        }
    }
    
    protected override void UpdateChip_FillIn( CConfigIni configIni, ref CDTX dTX, ref CChip pChip )
    {
        if ( !pChip.bHit && ( pChip.nDistanceFromBar.Drums < 0 ) )
        {
            pChip.bHit = true;
            switch ( pChip.nIntegerValue )
            {
                case 0x01:	// フィルイン開始
                    bEndFillIn = true;
                    if ( configIni.bFillInEnabled )
                    {
                        bInFillIn = true;
                    }
                    break;

                case 0x02:	// フィルイン終了
                    bEndFillIn = true;
                    if ( configIni.bFillInEnabled )
                    {
                        bInFillIn = false;
                    }
                    if (((actCombo.nCurrentCombo.Drums > 0) || configIni.bAllDrumsAreAutoPlay) && configIni.b歓声を発声する)
                    {
                        video.StartFillIn();
                        if (r現在の歓声Chip.Drums != null)
                        {
                            dTX.tPlayChip(r現在の歓声Chip.Drums, AudioMixer.Timer.nSystemTimeMs, (int)ELane.BGM, dTX.nモニタを考慮した音量(EInstrumentPart.UNKNOWN));
                        }
                        else
                        {
                            CDTXMania.Skin.soundAudience.tPlay();
                        }
                    }
                    break;
                case 0x03:
                    bChorusSection = true;
                    break;
                case 0x04:
                    bChorusSection = false;
                    break;
                case 0x05:
                    if (configIni.bFillInEnabled)
                    {
                        bChorusSection = true;
                    }
                    if (((actCombo.nCurrentCombo.Drums > 0) || configIni.bAllDrumsAreAutoPlay) && configIni.b歓声を発声する && configIni.DisplayBonusEffects)
                    {
                        if (r現在の歓声Chip.Drums != null)
                        {
                            dTX.tPlayChip(r現在の歓声Chip.Drums, AudioMixer.Timer.nSystemTimeMs, (int)ELane.BGM, dTX.nモニタを考慮した音量(EInstrumentPart.UNKNOWN));
                        }
                        else
                        {
                            CDTXMania.Skin.soundAudience.tPlay();
                        }
                    }
                    break;
                case 0x06:
                    if (configIni.bFillInEnabled)
                    {
                        bChorusSection = false;
                    }
                    if (((actCombo.nCurrentCombo.Drums > 0) || configIni.bAllDrumsAreAutoPlay) && configIni.b歓声を発声する && configIni.DisplayBonusEffects)
                    {
                        if (r現在の歓声Chip.Drums != null)
                        {
                            dTX.tPlayChip(r現在の歓声Chip.Drums, AudioMixer.Timer.nSystemTimeMs, (int)ELane.BGM, dTX.nモニタを考慮した音量(EInstrumentPart.UNKNOWN));
                        }
                        else
                        {
                            CDTXMania.Skin.soundAudience.tPlay();
                        }
                    }
                    break;
            }
        }
    }

        
    public void tProcessChipHit_BonusChip( CConfigIni configIni, CDTX dTX, CChip pChip)  // tボーナスチップのヒット処理
    {
        pChip.bHit = true;

        if ( pChip.bBonusChip )
        {
            bBonus = true;
            switch( pChip.nChannelNumber )
            {

                case EChannel.LeftCymbal: //LC
                    actPad.Start( 0, true, pChip.nChannelNumber );
                    break;

                case EChannel.HiHatClose: //HH
                case EChannel.HiHatOpen:
                    actPad.Start( 1, true, pChip.nChannelNumber );
                    break;

                case EChannel.LeftPedal: //LP
                case EChannel.LeftBassDrum:
                    actPad.Start( 2, true, pChip.nChannelNumber );
                    break;

                case EChannel.Snare: //SD
                    actPad.Start( 3, true, pChip.nChannelNumber );
                    break;

                case EChannel.HighTom: //HT
                    actPad.Start( 4, true, pChip.nChannelNumber );
                    break;

                case EChannel.BassDrum: //BD
                    actPad.Start( 5, true, pChip.nChannelNumber );
                    break;

                case EChannel.LowTom: //LT
                    actPad.Start( 6, true, pChip.nChannelNumber );
                    break;

                case EChannel.FloorTom: //FT
                    actPad.Start( 7, true, pChip.nChannelNumber );
                    break;

                case EChannel.Cymbal: //CY
                    actPad.Start( 8, true, pChip.nChannelNumber );
                    break;

                case EChannel.RideCymbal: //RD
                    actPad.Start(9, true, pChip.nChannelNumber);
                    break;
                default:
                    break;
            }
            if ( configIni.DisplayBonusEffects )
            {
                CDTXMania.Skin.soundAudience.tPlay();
            }
            if ( CDTXMania.ConfigIni.nSkillMode == 1 && ( !CDTXMania.ConfigIni.bAllDrumsAreAutoPlay || CDTXMania.ConfigIni.bAutoAddGage ) )
                actScore.Add( EInstrumentPart.DRUMS, bIsAutoPlay, 500L );
        }


    }

    protected override void UpdateChip_NoSound_Drums(CConfigIni configIni, ref CDTX dTX, ref CChip pChip)  // t進行描画_チップ_空打ち音設定_ドラム
    {
        if (!pChip.bHit && (pChip.nDistanceFromBar.Drums < 0))
        {
            try
            {
                pChip.bHit = true;
                r現在の空うちドラムChip[(int)eChannelToPad[(int)pChip.nChannelNumber - (int)EChannel.HiHatClose_NoChip]] = pChip;
                pChip.nChannelNumber = ((pChip.nChannelNumber < EChannel.LeftCymbal_NoChip) || (pChip.nChannelNumber > EChannel.LeftBassDrum_NoChip)) ? ((pChip.nChannelNumber - 0xb1) + 0x11) : ((pChip.nChannelNumber - 0xb3) + 0x11);
            }
            catch
            {
                return;
            }
        }

    }

    private bool bフィルイン区間の最後のChipである( CChip pChip )
    {
        if ( pChip == null )
        {
            return false;
        }
        int num = pChip.nPlaybackPosition;
        for (int i = listChip.IndexOf(pChip) + 1; i < listChip.Count; i++)
        {
            pChip = listChip[i];
            if ( ( pChip.nChannelNumber == EChannel.FillIn) && ( pChip.nIntegerValue == 2 ) )
            {
                return true;
            }
            if ( ( ( pChip.nChannelNumber >= EChannel.HiHatClose) && ( pChip.nChannelNumber <= EChannel.LeftBassDrum) ) && ( ( pChip.nPlaybackPosition - num ) > 0x18 ) )
            {
                return false;
            }
        }
        return true;
    }
}
