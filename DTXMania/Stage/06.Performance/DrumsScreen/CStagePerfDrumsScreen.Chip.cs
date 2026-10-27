using System.Numerics;
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

    protected override void DrawChip_PatternOnly_Drums(CConfigIni configIni, ref CDTX dTX, ref CChip pChip, Matrix4x4 origin)
    {
        if (configIni.bDrumsEnabled && !pChip.bHit && pChip.bVisible)
        {
            DrawDrumChip(pChip, 128 + ctChipPatternAnimation.Drums.nCurrentValue * 64, false, origin);
        }
    }

    protected override void UpdateChip_Drums( CConfigIni configIni, ref CDTX dTX, ref CChip pChip )
    {
        ApplyHiddenSudden(pChip, pChip.nDistanceFromBar.Drums, CDTXMania.ConfigIni.nHidSud.Drums, 200, 100, 50.0);

        drums!.UpdateNote(pChip);
        ApplyDrumsEvents();

        if (DrumsData.HasTargetGhost)
        {
            actGraph.dbGraphValue_Goal = DrumsData.CalculateTargetGhostGoal();
        }
    }

    protected override void DrawChip_Drums( CConfigIni configIni, ref CDTX dTX, ref CChip pChip, Matrix4x4 origin )
    {
        if (configIni.bDrumsEnabled && !pChip.bHit && pChip.bVisible)
        {
            DrawDrumChip(pChip, 0, pChip.bBonusChip, origin);
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

    private void DrawDrumChip(CChip chip, int row, bool bonus, Matrix4x4 origin)
    {
        if (txChip == null || ArtFor(chip.nChannelNumber) is not { } art)
        {
            return;
        }

        ELane lane = (ELane)DrumsPerformance.LaneOf(chip);
        int x = (int)(DrumLanes.Centre(lane) - art.SourceWidth / 2f);
        float distance = chip.nDistanceFromBar.Drums;
        float y = (CDTXMania.ConfigIni.bReverse.Drums ? nJudgeLinePosY.Drums + distance : nJudgeLinePosY.Drums - distance) - 32;
        Color4 col = Color4.White;
        col.Alpha = chip.nTransparency / 255.0f;

        txChip.tDraw2D(origin, x, y, new RectangleF(art.SourceX, row, art.SourceWidth, 64), col);
        if (bonus)
        {
            txChip.tDraw2D(origin, x, y, new RectangleF(art.SourceX, 64, art.SourceWidth, 64), col);
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
                        if (sequencer.FillInSound != null)
                        {
                            dTX.tPlayChip(sequencer.FillInSound, AudioMixer.Timer.nSystemTimeMs, (int)ELane.BGM, dTX.nモニタを考慮した音量(EInstrumentPart.UNKNOWN));
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
                        if (sequencer.FillInSound != null)
                        {
                            dTX.tPlayChip(sequencer.FillInSound, AudioMixer.Timer.nSystemTimeMs, (int)ELane.BGM, dTX.nモニタを考慮した音量(EInstrumentPart.UNKNOWN));
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
                        if (sequencer.FillInSound != null)
                        {
                            dTX.tPlayChip(sequencer.FillInSound, AudioMixer.Timer.nSystemTimeMs, (int)ELane.BGM, dTX.nモニタを考慮した音量(EInstrumentPart.UNKNOWN));
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

        
    protected override void UpdateChip_NoSound_Drums(CConfigIni configIni, ref CDTX dTX, ref CChip pChip)  // t進行描画_チップ_空打ち音設定_ドラム
    {
        drums!.UpdateEmptyHitChip(pChip);
    }

    private bool bフィルイン区間の最後のChipである( CChip pChip )
    {
        if ( pChip == null )
        {
            return false;
        }
        int num = pChip.nPlaybackPosition;
        for (int i = timeline.Chips.IndexOf(pChip) + 1; i < timeline.Chips.Count; i++)
        {
            pChip = timeline.Chips[i];
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
