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
    protected override void UpdateChip_Drums( CConfigIni configIni, ref CDTX dTX, ref CChip pChip )
    {
        drums!.UpdateNote(pChip);
        ApplyDrumsEvents();

        if (DrumsData.HasTargetGhost)
        {
            actGraph.dbGraphValue_Goal = DrumsData.CalculateTargetGhostGoal();
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
