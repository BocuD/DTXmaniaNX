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
        ApplyHiddenSudden(pChip, pChip.nDistanceFromBar.Drums, CDTXMania.ConfigIni.nHidSud.Drums, 200, 100, 50.0);
    }

    protected override void DrawChip_PatternOnly_Drums(CConfigIni configIni, ref CDTX dTX, ref CChip pChip)
    {
        if (configIni.bDrumsEnabled && !pChip.bHit && pChip.bVisible)
        {
            DrawDrumChip(pChip, 128 + ctChipPatternAnimation.Drums.nCurrentValue * 64, false);
        }
    }

    protected override void UpdateChip_Drums( CConfigIni configIni, ref CDTX dTX, ref CChip pChip )
    {
        ApplyHiddenSudden(pChip, pChip.nDistanceFromBar.Drums, CDTXMania.ConfigIni.nHidSud.Drums, 200, 100, 50.0);

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
                        (DrumsData.GhostHitCount.Perfect * 17 +
                         DrumsData.GhostHitCount.Great * 7 +
                         DrumsData.GhostMaxCombo * 3) / (20.0 * CDTXMania.DTX.nVisibleChipsCount.Drums);
                }
            }
            else
            {
                if ( CDTXMania.ConfigIni.nSkillMode == 0 )
                {
                    val = CScoreIni.tCalculatePlayingSkillOld(
                        CDTXMania.DTX.nVisibleChipsCount.Drums,
                        DrumsData.GhostHitCount.Perfect,
                        DrumsData.GhostHitCount.Great,
                        DrumsData.GhostHitCount.Good,
                        DrumsData.GhostHitCount.Poor,
                        DrumsData.GhostHitCount.Miss,
                        DrumsData.GhostMaxCombo,
                        EInstrumentPart.DRUMS, new STAUTOPLAY());
                }
                else
                {
                    val = CScoreIni.tCalculatePlayingSkill(
                        CDTXMania.DTX.nVisibleChipsCount.Drums,
                        DrumsData.GhostHitCount.Perfect,
                        DrumsData.GhostHitCount.Great,
                        DrumsData.GhostHitCount.Good,
                        DrumsData.GhostHitCount.Poor,
                        DrumsData.GhostHitCount.Miss,
                        DrumsData.GhostMaxCombo,
                        EInstrumentPart.DRUMS, new STAUTOPLAY());
                }

            }
            if (val < 0) val = 0;
            if (val > 100) val = 100;
            actGraph.dbGraphValue_Goal = val;
        }
    }

    protected override void DrawChip_Drums( CConfigIni configIni, ref CDTX dTX, ref CChip pChip )
    {
        if (configIni.bDrumsEnabled && !pChip.bHit && pChip.bVisible)
        {
            DrawDrumChip(pChip, 0, pChip.bBonusChip);
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

    private void DrawDrumChip(CChip chip, int row, bool bonus)
    {
        if (txChip == null || ArtFor(chip.nChannelNumber) is not { } art)
        {
            return;
        }

        ELane lane = (ELane)nチャンネル0Atoレーン07[chip.nChannelNumber - EChannel.HiHatClose];
        int x = (int)(DrumLanes.Centre(lane) - art.SourceWidth / 2f);
        float distance = chip.nDistanceFromBar.Drums;
        float y = (CDTXMania.ConfigIni.bReverse.Drums ? nJudgeLinePosY.Drums + distance : nJudgeLinePosY.Drums - distance) - 32;
        Color4 col = Color4.White;
        col.Alpha = chip.nTransparency / 255.0f;

        txChip.tDraw2D(x, y, new RectangleF(art.SourceX, row, art.SourceWidth, 64), col);
        if (bonus)
        {
            txChip.tDraw2D(x, y, new RectangleF(art.SourceX, 64, art.SourceWidth, 64), col);
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
                    if (((DrumsData.Combo > 0) || configIni.bAllDrumsAreAutoPlay) && configIni.b歓声を発声する)
                    {
                        video.StartFillInEffect();
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
                    if (((DrumsData.Combo > 0) || configIni.bAllDrumsAreAutoPlay) && configIni.b歓声を発声する && configIni.DisplayBonusEffects)
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
                    if (((DrumsData.Combo > 0) || configIni.bAllDrumsAreAutoPlay) && configIni.b歓声を発声する && configIni.DisplayBonusEffects)
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
            actPad.ShowBonus();
            if ( configIni.DisplayBonusEffects )
            {
                CDTXMania.Skin.soundAudience.tPlay();
            }
            if ( CDTXMania.ConfigIni.nSkillMode == 1 && ( !CDTXMania.ConfigIni.bAllDrumsAreAutoPlay || CDTXMania.ConfigIni.bAutoAddGage ) )
                DrumsData.AddScore(500L );
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
