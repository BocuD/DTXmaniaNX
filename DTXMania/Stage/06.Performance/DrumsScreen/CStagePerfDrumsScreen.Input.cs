using System.Diagnostics;
using System.Drawing;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using FDK;

namespace DTXMania;

internal partial class CStagePerfDrumsScreen
{
    protected override void tHandleInput_Drums()
    {

        for (int nPad = 0; nPad < (int)EPad.MAX; nPad++)
        {
            List<STInputEvent> listInputEvent = listPadEvents;
            CDTXMania.Pad.GetEvents(EInstrumentPart.DRUMS, (EPad)nPad, listInputEvent);

            if (listInputEvent.Count == 0)
                continue;

            tSaveInputMethod(EInstrumentPart.DRUMS);

            #region [ 打ち分けグループ調整 ]
            //-----------------------------
            EHHGroup eHHGroup = CDTXMania.ConfigIni.eHHGroup;
            EFTGroup eFTGroup = CDTXMania.ConfigIni.eFTGroup;
            ECYGroup eCYGroup = CDTXMania.ConfigIni.eCYGroup;
            EBDGroup eBDGroup = CDTXMania.ConfigIni.eBDGroup;

            if (!CDTXMania.DTX.bHasChips.Ride && (eCYGroup == ECYGroup.打ち分ける))
            {
                eCYGroup = ECYGroup.共通;
            }
            if (!CDTXMania.DTX.bHasChips.HHOpen && (eHHGroup == EHHGroup.全部打ち分ける))
            {
                eHHGroup = EHHGroup.左シンバルのみ打ち分ける;
            }
            if (!CDTXMania.DTX.bHasChips.HHOpen && (eHHGroup == EHHGroup.ハイハットのみ打ち分ける))
            {
                eHHGroup = EHHGroup.全部共通;
            }
            if (!CDTXMania.DTX.bHasChips.LeftCymbal && (eHHGroup == EHHGroup.全部打ち分ける))
            {
                eHHGroup = EHHGroup.ハイハットのみ打ち分ける;
            }
            if (!CDTXMania.DTX.bHasChips.LeftCymbal && (eHHGroup == EHHGroup.左シンバルのみ打ち分ける))
            {
                eHHGroup = EHHGroup.全部共通;
            }
            //-----------------------------
            #endregion

            foreach (STInputEvent inputEvent in listInputEvent)
            {

                if (!inputEvent.b押された)
                    continue;

                long nTime = inputEvent.nTimeStamp - AudioMixer.Timer.nResetAtMs;
                int nInputAdjustTime = bIsAutoPlay[nチャンネル0Atoレーン07[nPad]] ? 0 : nInputAdjustTimeMs.Drums;
                int nPedalLagTime = CDTXMania.ConfigIni.nPedalLagTime;

                bool bHitted = false;

                #region [ (A) ヒットしていればヒット処理して次の inputEvent へ ]
                //-----------------------------
                switch (((EPad)nPad))
                {
                    case EPad.HH:
                        #region [ HHとLC(groupingしている場合) のヒット処理 ]
                        //-----------------------------
                    {
                        if (inputEvent.nVelocity <= CDTXMania.ConfigIni.nVelocityMin.HH)
                            continue;	// 電子ドラムによる意図的なクロストークを無効にする

                        CChip chipHC = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.HiHatClose, HitState.NotHit);
                        CChip chipHO = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.HiHatOpen, HitState.NotHit);
                        CChip chipLC = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.LeftCymbal, HitState.NotHit);
                        EJudgement e判定HC = (chipHC != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipHC, nInputAdjustTime) : EJudgement.Miss;
                        EJudgement e判定HO = (chipHO != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipHO, nInputAdjustTime) : EJudgement.Miss;
                        EJudgement e判定LC = (chipLC != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipLC, nInputAdjustTime) : EJudgement.Miss;
                        switch (eHHGroup)
                        {
                            case EHHGroup.ハイハットのみ打ち分ける:
                                #region [ HCとLCのヒット処理 ]
                                //-----------------------------
                                if ((e判定HC != EJudgement.Miss) && (e判定LC != EJudgement.Miss))
                                {
                                    if (chipHC.nPlaybackPosition < chipLC.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipHC, inputEvent.nVelocity);
                                    }
                                    else if (chipHC.nPlaybackPosition > chipLC.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipLC, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipHC, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.HH, chipLC, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if (e判定HC != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HH, chipHC, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定LC != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HH, chipLC, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (!bHitted)
                                    break;
                                continue;
                            //-----------------------------
                            #endregion

                            case EHHGroup.左シンバルのみ打ち分ける:
                                #region [ HCとHOのヒット処理 ]
                                //-----------------------------
                                if ((e判定HC != EJudgement.Miss) && (e判定HO != EJudgement.Miss))
                                {
                                    if (chipHC.nPlaybackPosition < chipHO.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipHC, inputEvent.nVelocity);
                                    }
                                    else if (chipHC.nPlaybackPosition > chipHO.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipHO, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipHC, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.HH, chipHO, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if (e判定HC != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HH, chipHC, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定HO != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HH, chipHO, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (!bHitted)
                                    break;
                                continue;
                            //-----------------------------
                            #endregion

                            case EHHGroup.全部共通:
                                #region [ HC,HO,LCのヒット処理 ]
                                //-----------------------------
                                if (((e判定HC != EJudgement.Miss) && (e判定HO != EJudgement.Miss)) && (e判定LC != EJudgement.Miss))
                                {
                                    CChip chip;
                                    CChip[] chipArray = new CChip[] { chipHC, chipHO, chipLC };
                                    // ここから、chipArrayをn発生位置の小さい順に並び替える
                                    if (chipArray[1].nPlaybackPosition > chipArray[2].nPlaybackPosition)
                                    {
                                        chip = chipArray[1];
                                        chipArray[1] = chipArray[2];
                                        chipArray[2] = chip;
                                    }
                                    if (chipArray[0].nPlaybackPosition > chipArray[1].nPlaybackPosition)
                                    {
                                        chip = chipArray[0];
                                        chipArray[0] = chipArray[1];
                                        chipArray[1] = chip;
                                    }
                                    if (chipArray[1].nPlaybackPosition > chipArray[2].nPlaybackPosition)
                                    {
                                        chip = chipArray[1];
                                        chipArray[1] = chipArray[2];
                                        chipArray[2] = chip;
                                    }
                                    tProcessDrumHit(nTime, EPad.HH, chipArray[0], inputEvent.nVelocity);
                                    if (chipArray[0].nPlaybackPosition == chipArray[1].nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipArray[1], inputEvent.nVelocity);
                                    }
                                    if (chipArray[0].nPlaybackPosition == chipArray[2].nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipArray[2], inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if ((e判定HC != EJudgement.Miss) && (e判定HO != EJudgement.Miss))
                                {
                                    if (chipHC.nPlaybackPosition < chipHO.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipHC, inputEvent.nVelocity);
                                    }
                                    else if (chipHC.nPlaybackPosition > chipHO.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipHO, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipHC, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.HH, chipHO, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if ((e判定HC != EJudgement.Miss) && (e判定LC != EJudgement.Miss))
                                {
                                    if (chipHC.nPlaybackPosition < chipLC.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipHC, inputEvent.nVelocity);
                                    }
                                    else if (chipHC.nPlaybackPosition > chipLC.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipLC, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipHC, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.HH, chipLC, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if ((e判定HO != EJudgement.Miss) && (e判定LC != EJudgement.Miss))
                                {
                                    if (chipHO.nPlaybackPosition < chipLC.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipHO, inputEvent.nVelocity);
                                    }
                                    else if (chipHO.nPlaybackPosition > chipLC.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipLC, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.HH, chipHO, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.HH, chipLC, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if (e判定HC != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HH, chipHC, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定HO != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HH, chipHO, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定LC != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HH, chipLC, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (!bHitted)
                                    break;
                                continue;
                            //-----------------------------
                            #endregion

                            default:
                                #region [ 全部打ち分け時のヒット処理 ]
                                //-----------------------------
                                if (e判定HC != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HH, chipHC, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (!bHitted)
                                    break;
                                continue;
                            //-----------------------------
                            #endregion
                        }
                        if (!bHitted)
                            break;
                        continue;
                    }
                    //-----------------------------
                    #endregion

                    case EPad.SD:
                        #region [ SDのヒット処理 ]
                        //-----------------------------
                        if (inputEvent.nVelocity <= CDTXMania.ConfigIni.nVelocityMin.SD)	// #23857 2010.12.12 yyagi: to support VelocityMin
                            continue;	// 電子ドラムによる意図的なクロストークを無効にする
                        if (!tProcessDrumHit(nTime, EPad.SD, timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.Snare, HitState.NotHit), inputEvent.nVelocity))
                            break;
                        continue;
                    //-----------------------------
                    #endregion

                    case EPad.BD:
                        #region [ BDとLPとLBD(ペアリングしている場合)のヒット処理 ]
                        //-----------------------------
                    {
                        if (inputEvent.nVelocity <= CDTXMania.ConfigIni.nVelocityMin.BD)	// #23857 2010.12.12 yyagi: to support VelocityMin
                            continue;	// 電子ドラムによる意図的なクロストークを無効にする

                        CChip chipBD  = timeline.NearestDrum(nTime + nInputAdjustTime + nPedalLagTime, EChannel.BassDrum, HitState.NotHit);
                        CChip chipLP  = timeline.NearestDrum(nTime + nInputAdjustTime + nPedalLagTime, EChannel.LeftPedal, HitState.NotHit);
                        CChip chipLBD = timeline.NearestDrum(nTime + nInputAdjustTime + nPedalLagTime, EChannel.LeftBassDrum, HitState.NotHit);
                        EJudgement e判定BD  = (chipBD  != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipBD, nInputAdjustTime + nPedalLagTime) : EJudgement.Miss;
                        EJudgement e判定LP  = (chipLP  != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipLP, nInputAdjustTime + nPedalLagTime) : EJudgement.Miss;
                        EJudgement e判定LBD = (chipLBD != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipLBD, nInputAdjustTime + nPedalLagTime) : EJudgement.Miss;
                        switch (eBDGroup)
                        {
                            case EBDGroup.BDとLPで打ち分ける:
                                #region[ BD & LBD | LP ]
                                if ( e判定BD != EJudgement.Miss && e判定LBD != EJudgement.Miss )
                                {
                                    if ( chipBD.nPlaybackPosition < chipLBD.nPlaybackPosition )
                                    {
                                        tProcessDrumHit( nTime, EPad.BD, chipBD, inputEvent.nVelocity );
                                    }
                                    else if ( chipBD.nPlaybackPosition > chipLBD.nPlaybackPosition )
                                    {
                                        tProcessDrumHit( nTime, EPad.BD, chipLBD, inputEvent.nVelocity );
                                    }
                                    else
                                    {
                                        tProcessDrumHit( nTime, EPad.BD, chipBD, inputEvent.nVelocity);
                                        tProcessDrumHit( nTime, EPad.BD, chipLBD, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if ( e判定BD != EJudgement.Miss)
                                {
                                    tProcessDrumHit( nTime, EPad.BD, chipBD, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if ( e判定LBD != EJudgement.Miss)
                                {
                                    tProcessDrumHit( nTime, EPad.BD, chipLBD, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if ( bHitted )
                                    continue;
                                else
                                    break;
                            #endregion

                            case EBDGroup.左右ペダルのみ打ち分ける:
                                #region[ BDのヒット処理]
                                if (e判定BD != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.BD, chipBD, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (bHitted)
                                {
                                    continue;
                                }
                                break;
                            #endregion

                            case EBDGroup.どっちもBD:
                                #region[ LP&LBD&BD ]
                                if (((e判定LP != EJudgement.Miss) && (e判定LBD != EJudgement.Miss)) && (e判定BD != EJudgement.Miss))
                                {
                                    CChip chip8;
                                    CChip[] chipArray2 = new CChip[] { chipLP, chipLBD, chipBD };
                                    if (chipArray2[1].nPlaybackPosition > chipArray2[2].nPlaybackPosition)
                                    {
                                        chip8 = chipArray2[1];
                                        chipArray2[1] = chipArray2[2];
                                        chipArray2[2] = chip8;
                                    }
                                    if (chipArray2[0].nPlaybackPosition > chipArray2[1].nPlaybackPosition)
                                    {
                                        chip8 = chipArray2[0];
                                        chipArray2[0] = chipArray2[1];
                                        chipArray2[1] = chip8;
                                    }
                                    if (chipArray2[1].nPlaybackPosition > chipArray2[2].nPlaybackPosition)
                                    {
                                        chip8 = chipArray2[1];
                                        chipArray2[1] = chipArray2[2];
                                        chipArray2[2] = chip8;
                                    }
                                    tProcessDrumHit(nTime, EPad.BD, chipArray2[0], inputEvent.nVelocity);
                                    if (chipArray2[0].nPlaybackPosition == chipArray2[1].nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.BD, chipArray2[1], inputEvent.nVelocity);
                                    }
                                    if (chipArray2[0].nPlaybackPosition == chipArray2[2].nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.BD, chipArray2[2], inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                //chip7 BD  chip6LBD  chip5LP
                                //判定6 BD  判定5　　 判定4
                                else if ((e判定LP != EJudgement.Miss) && (e判定LBD != EJudgement.Miss))
                                {
                                    if (chipLP.nPlaybackPosition < chipLBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.BD, chipLP, inputEvent.nVelocity);
                                    }
                                    else if (chipLP.nPlaybackPosition > chipLBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.BD, chipLBD, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.BD, chipLP, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.BD, chipLBD, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if ((e判定LP != EJudgement.Miss) && (e判定BD != EJudgement.Miss))
                                {
                                    if (chipLP.nPlaybackPosition < chipBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.BD, chipLP, inputEvent.nVelocity);
                                    }
                                    else if (chipLP.nPlaybackPosition > chipBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.BD, chipBD, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.BD, chipLP, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.BD, chipBD, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                //chip7 BD  chip6LBD  chip5LP
                                //判定6 BD  判定5　　 判定4
                                else if ((e判定LBD != EJudgement.Miss) && (e判定BD != EJudgement.Miss))
                                {
                                    if (chipLBD.nPlaybackPosition < chipBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.BD, chipLBD, inputEvent.nVelocity);
                                    }
                                    else if (chipLBD.nPlaybackPosition > chipBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.BD, chipBD, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.BD, chipLBD, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.BD, chipBD, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if (e判定LP != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.BD, chipLP, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定LBD != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.BD, chipLBD, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定BD != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.BD, chipBD, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (bHitted)
                                {
                                    continue;
                                }
                                break;
                            #endregion

                            default:
                                #region [ 全部打ち分け時のヒット処理 ]
                                //-----------------------------
                                if (e判定BD != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.BD, chipBD, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (!bHitted)
                                    break;
                                continue;
                            //-----------------------------
                            #endregion
                        }
                        if (!bHitted)
                            break;
                        continue;
                    }
                    //-----------------------------
                    #endregion

                    case EPad.HT:
                        #region [ HTのヒット処理 ]
                        //-----------------------------
                        if (inputEvent.nVelocity <= CDTXMania.ConfigIni.nVelocityMin.HT)	// #23857 2010.12.12 yyagi: to support VelocityMin
                            continue;	// 電子ドラムによる意図的なクロストークを無効にする
                        if (tProcessDrumHit(nTime, EPad.HT, timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.HighTom, HitState.NotHit), inputEvent.nVelocity))
                            continue;
                        break;
                    //-----------------------------
                    #endregion

                    case EPad.LT:
                        #region [ LTとFT(groupingしている場合)のヒット処理 ]
                        //-----------------------------
                    {
                        if (inputEvent.nVelocity <= CDTXMania.ConfigIni.nVelocityMin.LT)	// #23857 2010.12.12 yyagi: to support VelocityMin
                            continue;	// 電子ドラムによる意図的なクロストークを無効にする
                        CChip chipLT = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.LowTom, HitState.NotHit);
                        CChip chipFT = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.FloorTom, HitState.NotHit);
                        EJudgement e判定LT = (chipLT != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipLT, nInputAdjustTime) : EJudgement.Miss;
                        EJudgement e判定FT = (chipFT != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipFT, nInputAdjustTime) : EJudgement.Miss;
                        switch (eFTGroup)
                        {
                            case EFTGroup.打ち分ける:
                                #region [ LTのヒット処理 ]
                                //-----------------------------
                                if (e判定LT != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.LT, chipLT, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                break;
                            //-----------------------------
                            #endregion

                            case EFTGroup.共通:
                                #region [ LTとFTのヒット処理 ]
                                //-----------------------------
                                if ((e判定LT != EJudgement.Miss) && (e判定FT != EJudgement.Miss))
                                {
                                    if (chipLT.nPlaybackPosition < chipFT.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LT, chipLT, inputEvent.nVelocity);
                                    }
                                    else if (chipLT.nPlaybackPosition > chipFT.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LT, chipFT, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.LT, chipLT, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.LT, chipFT, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if (e判定LT != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.LT, chipLT, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定FT != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.LT, chipFT, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                break;
                            //-----------------------------
                            #endregion
                        }
                        if (!bHitted)
                            break;
                        continue;
                    }
                    //-----------------------------
                    #endregion

                    case EPad.FT:
                        #region [ FTとLT(groupingしている場合)のヒット処理 ]
                        //-----------------------------
                    {
                        if (inputEvent.nVelocity <= CDTXMania.ConfigIni.nVelocityMin.FT)	// #23857 2010.12.12 yyagi: to support VelocityMin
                            continue;	// 電子ドラムによる意図的なクロストークを無効にする
                        CChip chipLT = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.LowTom, HitState.NotHit);
                        CChip chipFT = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.FloorTom, HitState.NotHit);
                        EJudgement e判定LT = (chipLT != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipLT, nInputAdjustTime) : EJudgement.Miss;
                        EJudgement e判定FT = (chipFT != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipFT, nInputAdjustTime) : EJudgement.Miss;
                        switch (eFTGroup)
                        {
                            case EFTGroup.打ち分ける:
                                #region [ FTのヒット処理 ]
                                //-----------------------------
                                if (e判定FT != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.FT, chipFT, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                //-----------------------------
                                #endregion
                                break;

                            case EFTGroup.共通:
                                #region [ FTとLTのヒット処理 ]
                                //-----------------------------
                                if ((e判定LT != EJudgement.Miss) && (e判定FT != EJudgement.Miss))
                                {
                                    if (chipLT.nPlaybackPosition < chipFT.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.FT, chipLT, inputEvent.nVelocity);
                                    }
                                    else if (chipLT.nPlaybackPosition > chipFT.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.FT, chipFT, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.FT, chipLT, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.FT, chipFT, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if (e判定LT != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.FT, chipLT, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定FT != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.FT, chipFT, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                //-----------------------------
                                #endregion
                                break;
                        }
                        if (!bHitted)
                            break;
                        continue;
                    }
                    //-----------------------------
                    #endregion

                    case EPad.CY:
                        #region [ CY(とLCとRD:groupingしている場合)のヒット処理 ]
                        //-----------------------------
                    {
                        if (inputEvent.nVelocity <= CDTXMania.ConfigIni.nVelocityMin.CY)	// #23857 2010.12.12 yyagi: to support VelocityMin
                            continue;	// 電子ドラムによる意図的なクロストークを無効にする
                        CChip chipCY = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.Cymbal, HitState.NotHit);
                        CChip chipRD = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.RideCymbal, HitState.NotHit);
                        CChip chipLC = CDTXMania.ConfigIni.bシンバルフリー ? timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.LeftCymbal, HitState.NotHit) : null;
                        EJudgement e判定CY = (chipCY != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipCY, nInputAdjustTime) : EJudgement.Miss;
                        EJudgement e判定RD = (chipRD != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipRD, nInputAdjustTime) : EJudgement.Miss;
                        EJudgement e判定LC = (chipLC != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipLC, nInputAdjustTime) : EJudgement.Miss;
                        CChip[] chipArray = new CChip[] { chipCY, chipRD, chipLC };
                        EJudgement[] e判定Array = new EJudgement[] { e判定CY, e判定RD, e判定LC };
                        const int NumOfChips = 3;	// chipArray.GetLength(0)


                        // CY/RD/LC群を, n発生位置の小さい順に並べる + nullを大きい方に退かす
                        SortChipsByNTime(chipArray, e判定Array, NumOfChips);
                        switch (eCYGroup)
                        {
                            case ECYGroup.打ち分ける:
                                #region [打ち分ける]
                                if (!CDTXMania.ConfigIni.bシンバルフリー)
                                {
                                            
                                    if (e判定CY != EJudgement.Miss)
                                    {
                                        tProcessDrumHit(nTime, EPad.CY, chipCY, inputEvent.nVelocity);
                                        bHitted = true;
                                    }
                                    if (!bHitted)
                                        break;
                                    continue;
                                }
                                for (int i = 0; i < NumOfChips; i++)
                                {
                                    if ((e判定Array[i] != EJudgement.Miss) && ((chipArray[i] == chipCY) || (chipArray[i] == chipLC)))
                                    {
                                        tProcessDrumHit(nTime, EPad.CY, chipArray[i], inputEvent.nVelocity);
                                        bHitted = true;
                                        break;
                                    }
                                }
                                if (e判定CY != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.CY, chipCY, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (!bHitted)
                                    break;
                                continue;
                            #endregion

                            case ECYGroup.共通:
                                        
                                if (!CDTXMania.ConfigIni.bシンバルフリー)
                                {
                                    for (int i = 0; i < NumOfChips; i++)
                                    {
                                        if ((e判定Array[i] != EJudgement.Miss) && ((chipArray[i] == chipCY) || (chipArray[i] == chipRD)))
                                        {
                                            tProcessDrumHit(nTime, EPad.CY, chipArray[i], inputEvent.nVelocity);
                                            bHitted = true;
                                            break;
                                        }
                                    }
                                    if (!bHitted)
                                        break;
                                    continue;
                                }
                                for (int i = 0; i < NumOfChips; i++)
                                {
                                    if (e判定Array[i] != EJudgement.Miss)
                                    {
                                        tProcessDrumHit(nTime, EPad.CY, chipArray[i], inputEvent.nVelocity);
                                        bHitted = true;
                                        break;
                                    }
                                }
                                if (!bHitted)
                                    break;
                                continue;
                        }
                        if (!bHitted)
                            break;
                        continue;
                    }
                    //-----------------------------
                    #endregion

                    case EPad.HHO:
                        #region [ HO(とHCとLC:groupingしている場合)のヒット処理 ]
                        //-----------------------------
                    {
                        if (inputEvent.nVelocity <= CDTXMania.ConfigIni.nVelocityMin.HH)
                            continue;	// 電子ドラムによる意図的なクロストークを無効にする

                        CChip chipHC = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.HiHatClose, HitState.NotHit);
                        CChip chipHO = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.HiHatOpen, HitState.NotHit);
                        CChip chipLC = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.LeftCymbal, HitState.NotHit);
                        EJudgement e判定HC = (chipHC != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipHC, nInputAdjustTime) : EJudgement.Miss;
                        EJudgement e判定HO = (chipHO != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipHO, nInputAdjustTime) : EJudgement.Miss;
                        EJudgement e判定LC = (chipLC != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipLC, nInputAdjustTime) : EJudgement.Miss;
                        switch (eHHGroup)
                        {
                            case EHHGroup.全部打ち分ける:
                                if (e判定HO != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HHO, chipHO, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (!bHitted)
                                    break;
                                continue;

                            case EHHGroup.ハイハットのみ打ち分ける:
                                if ((e判定HO != EJudgement.Miss) && (e判定LC != EJudgement.Miss))
                                {
                                    if (chipHO.nPlaybackPosition < chipLC.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipHO, inputEvent.nVelocity);
                                    }
                                    else if (chipHO.nPlaybackPosition > chipLC.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipLC, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipHO, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.HHO, chipLC, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if (e判定HO != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HHO, chipHO, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定LC != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HHO, chipLC, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (!bHitted)
                                    break;
                                continue;

                            case EHHGroup.左シンバルのみ打ち分ける:
                                if ((e判定HC != EJudgement.Miss) && (e判定HO != EJudgement.Miss))
                                {
                                    if (chipHC.nPlaybackPosition < chipHO.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipHC, inputEvent.nVelocity);
                                    }
                                    else if (chipHC.nPlaybackPosition > chipHO.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipHO, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipHC, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.HHO, chipHO, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if (e判定HC != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HHO, chipHC, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定HO != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HHO, chipHO, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (!bHitted)
                                    break;
                                continue;

                            case EHHGroup.全部共通:
                                if (((e判定HC != EJudgement.Miss) && (e判定HO != EJudgement.Miss)) && (e判定LC != EJudgement.Miss))
                                {
                                    CChip chip;
                                    CChip[] chipArray = new CChip[] { chipHC, chipHO, chipLC };
                                    // ここから、chipArrayをn発生位置の小さい順に並び替える
                                    if (chipArray[1].nPlaybackPosition > chipArray[2].nPlaybackPosition)
                                    {
                                        chip = chipArray[1];
                                        chipArray[1] = chipArray[2];
                                        chipArray[2] = chip;
                                    }
                                    if (chipArray[0].nPlaybackPosition > chipArray[1].nPlaybackPosition)
                                    {
                                        chip = chipArray[0];
                                        chipArray[0] = chipArray[1];
                                        chipArray[1] = chip;
                                    }
                                    if (chipArray[1].nPlaybackPosition > chipArray[2].nPlaybackPosition)
                                    {
                                        chip = chipArray[1];
                                        chipArray[1] = chipArray[2];
                                        chipArray[2] = chip;
                                    }
                                    tProcessDrumHit(nTime, EPad.HHO, chipArray[0], inputEvent.nVelocity);
                                    if (chipArray[0].nPlaybackPosition == chipArray[1].nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipArray[1], inputEvent.nVelocity);
                                    }
                                    if (chipArray[0].nPlaybackPosition == chipArray[2].nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipArray[2], inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if ((e判定HC != EJudgement.Miss) && (e判定HO != EJudgement.Miss))
                                {
                                    if (chipHC.nPlaybackPosition < chipHO.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipHC, inputEvent.nVelocity);
                                    }
                                    else if (chipHC.nPlaybackPosition > chipHO.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipHO, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipHC, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.HHO, chipHO, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if ((e判定HC != EJudgement.Miss) && (e判定LC != EJudgement.Miss))
                                {
                                    if (chipHC.nPlaybackPosition < chipLC.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipHC, inputEvent.nVelocity);
                                    }
                                    else if (chipHC.nPlaybackPosition > chipLC.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipLC, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipHC, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.HHO, chipLC, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if ((e判定HO != EJudgement.Miss) && (e判定LC != EJudgement.Miss))
                                {
                                    if (chipHO.nPlaybackPosition < chipLC.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipHO, inputEvent.nVelocity);
                                    }
                                    else if (chipHO.nPlaybackPosition > chipLC.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipLC, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.HHO, chipHO, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.HHO, chipLC, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if (e判定HC != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HHO, chipHC, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定HO != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HHO, chipHO, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定LC != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.HHO, chipLC, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (!bHitted)
                                    break;
                                continue;
                        }
                        if (!bHitted)
                            break;
                        continue;
                    }
                    //-----------------------------
                    #endregion

                    case EPad.RD:
                        #region [ RD(とCYとLC:groupingしている場合)のヒット処理 ]
                        //-----------------------------
                    {
                        if (inputEvent.nVelocity <= CDTXMania.ConfigIni.nVelocityMin.RD)	// #23857 2010.12.12 yyagi: to support VelocityMin
                            continue;	// 電子ドラムによる意図的なクロストークを無効にする
                        CChip chipCY = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.Cymbal, HitState.NotHit);
                        CChip chipRD = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.RideCymbal, HitState.NotHit);
                        CChip chipLC = CDTXMania.ConfigIni.bシンバルフリー ? timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.LeftCymbal, HitState.NotHit) : null;
                        EJudgement e判定CY = (chipCY != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipCY, nInputAdjustTime) : EJudgement.Miss;
                        EJudgement e判定RD = (chipRD != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipRD, nInputAdjustTime) : EJudgement.Miss;
                        EJudgement e判定LC = (chipLC != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipLC, nInputAdjustTime) : EJudgement.Miss;
                        CChip[] chipArray = new CChip[] { chipCY, chipRD, chipLC };
                        EJudgement[] e判定Array = new EJudgement[] { e判定CY, e判定RD, e判定LC };
                        const int NumOfChips = 3;	// chipArray.GetLength(0)
                        SortChipsByNTime(chipArray, e判定Array, NumOfChips);
                        switch (eCYGroup)
                        {
                            case ECYGroup.打ち分ける:
                                if (e判定RD != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.RD, chipRD, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                break;

                            case ECYGroup.共通:
                                if (!CDTXMania.ConfigIni.bシンバルフリー)
                                {
                                    for (int i = 0; i < NumOfChips; i++)
                                    {
                                        if ((e判定Array[i] != EJudgement.Miss) && ((chipArray[i] == chipCY) || (chipArray[i] == chipRD)))
                                        {
                                            tProcessDrumHit(nTime, EPad.CY, chipArray[i], inputEvent.nVelocity);
                                            bHitted = true;
                                            break;
                                        }
                                    }
                                    break;
                                }
                                for (int i = 0; i < NumOfChips; i++)
                                {
                                    if (e判定Array[i] != EJudgement.Miss)
                                    {
                                        tProcessDrumHit(nTime, EPad.CY, chipArray[i], inputEvent.nVelocity);
                                        bHitted = true;
                                        break;
                                    }
                                }
                                break;
                        }
                        if (bHitted)
                        {
                            continue;
                        }
                        break;
                    }
                    //-----------------------------
                    #endregion

                    case EPad.LC:
                        #region [ LC(とHC/HOとCYと:groupingしている場合)のヒット処理 ]
                        //-----------------------------
                    {
                        if (inputEvent.nVelocity <= CDTXMania.ConfigIni.nVelocityMin.LC)	// #23857 2010.12.12 yyagi: to support VelocityMin
                            continue;	// 電子ドラムによる意図的なクロストークを無効にする
                        CChip chipHC = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.HiHatClose, HitState.NotHit);
                        CChip chipHO = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.HiHatOpen, HitState.NotHit);
                        CChip chipLC = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.LeftCymbal, HitState.NotHit);
                        CChip chipCY = CDTXMania.ConfigIni.bシンバルフリー ? timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.Cymbal, HitState.NotHit) : null;
                        CChip chipRD = CDTXMania.ConfigIni.bシンバルフリー ? timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.RideCymbal, HitState.NotHit) : null;
                        EJudgement e判定HC = (chipHC != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipHC, nInputAdjustTime) : EJudgement.Miss;
                        EJudgement e判定HO = (chipHO != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipHO, nInputAdjustTime) : EJudgement.Miss;
                        EJudgement e判定LC = (chipLC != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipLC, nInputAdjustTime) : EJudgement.Miss;
                        EJudgement e判定CY = (chipCY != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipCY, nInputAdjustTime) : EJudgement.Miss;
                        EJudgement e判定RD = (chipRD != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipRD, nInputAdjustTime) : EJudgement.Miss;
                        CChip[] chipArray = new CChip[] { chipHC, chipHO, chipLC, chipCY, chipRD };
                        EJudgement[] e判定Array = new EJudgement[] { e判定HC, e判定HO, e判定LC, e判定CY, e判定RD };
                        const int NumOfChips = 5;	// chipArray.GetLength(0)
                        SortChipsByNTime(chipArray, e判定Array, NumOfChips);

                        switch (eHHGroup)
                        {
                            case EHHGroup.全部打ち分ける:
                            case EHHGroup.左シンバルのみ打ち分ける:
                                #region[左シンバルのみ打ち分ける]
                                if (!CDTXMania.ConfigIni.bシンバルフリー)
                                {
                                    if (e判定LC != EJudgement.Miss)
                                    {
                                        tProcessDrumHit(nTime, EPad.LC, chipLC, inputEvent.nVelocity);
                                        bHitted = true;
                                    }
                                    if (!bHitted)
                                        break;
                                    continue;
                                }
                                for (int i = 0; i < NumOfChips; i++)
                                {
                                    if ((e判定Array[i] != EJudgement.Miss) && (((chipArray[i] == chipLC) || (chipArray[i] == chipCY)) || ((chipArray[i] == chipRD) && (CDTXMania.ConfigIni.eCYGroup == ECYGroup.共通))))
                                    {
                                        tProcessDrumHit(nTime, EPad.LC, chipArray[i], inputEvent.nVelocity);
                                        bHitted = true;
                                        break;
                                    }
                                }
                                if (!bHitted)
                                    break;
                                continue;
                            #endregion
                            case EHHGroup.ハイハットのみ打ち分ける:
                            case EHHGroup.全部共通:
                                if (!CDTXMania.ConfigIni.bシンバルフリー)
                                    #region[全部共通]
                                {
                                    for (int i = 0; i < NumOfChips; i++)
                                    {
                                        if ((e判定Array[i] != EJudgement.Miss) && (((chipArray[i] == chipLC) || (chipArray[i] == chipHC)) || (chipArray[i] == chipHO)))
                                        {
                                            tProcessDrumHit(nTime, EPad.LC, chipArray[i], inputEvent.nVelocity);
                                            bHitted = true;
                                            break;
                                        }
                                    }
                                    if (!bHitted)
                                        break;
                                    continue;
                                }
                                for (int i = 0; i < NumOfChips; i++)
                                {
                                    if ((e判定Array[i] != EJudgement.Miss) && ((chipArray[i] != chipRD) || (CDTXMania.ConfigIni.eCYGroup == ECYGroup.共通)))
                                    {
                                        tProcessDrumHit(nTime, EPad.LC, chipArray[i], inputEvent.nVelocity);
                                        bHitted = true;
                                        break;
                                    }
                                }
                                if (!bHitted)
                                    break;
                                continue;
                            #endregion
                        }
                        if (!bHitted)
                            break;

                        break;
                    }
                    //-----------------------------
                    #endregion

                    #region [rev030追加処理]
                    case EPad.LP:
                        #region [ LPのヒット処理 ]
                        //-----------------
                    {
                        if (inputEvent.nVelocity <= CDTXMania.ConfigIni.nVelocityMin.LP)
                            continue;
                        CChip chipBD  = timeline.NearestDrum(nTime + nInputAdjustTime + nPedalLagTime, EChannel.BassDrum, HitState.NotHit);
                        CChip chipLP  = timeline.NearestDrum(nTime + nInputAdjustTime + nPedalLagTime, EChannel.LeftPedal, HitState.NotHit);
                        CChip chipLBD = timeline.NearestDrum(nTime + nInputAdjustTime + nPedalLagTime, EChannel.LeftBassDrum, HitState.NotHit);
                        EJudgement e判定BD  = (chipBD  != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipBD,  nInputAdjustTime + nPedalLagTime) : EJudgement.Miss;
                        EJudgement e判定LP  = (chipLP  != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipLP,  nInputAdjustTime + nPedalLagTime) : EJudgement.Miss;
                        EJudgement e判定LBD = (chipLBD != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipLBD, nInputAdjustTime + nPedalLagTime) : EJudgement.Miss;
                        switch (eBDGroup)
                        {
                            case EBDGroup.左右ペダルのみ打ち分ける:
                                #region[ LPのヒット処理]
                                if (e判定LP != EJudgement.Miss && e判定LBD != EJudgement.Miss)
                                {
                                    if (chipLP.nPlaybackPosition < chipLBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LP, chipLP, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        if (chipLP.nPlaybackPosition > chipLBD.nPlaybackPosition)
                                        {
                                            tProcessDrumHit(nTime, EPad.LP, chipLBD, inputEvent.nVelocity);
                                        }
                                        else
                                        {
                                            tProcessDrumHit(nTime, EPad.LP, chipLP, inputEvent.nVelocity);
                                            tProcessDrumHit(nTime, EPad.LP, chipLBD, inputEvent.nVelocity);
                                        }
                                    }
                                    bHitted = true;
                                }
                                else
                                {
                                    if (e判定LP != EJudgement.Miss)
                                    {
                                        tProcessDrumHit(nTime, EPad.LP, chipLP, inputEvent.nVelocity);
                                        bHitted = true;
                                    }
                                    else
                                    {
                                        if (e判定LBD != EJudgement.Miss)
                                        {
                                            tProcessDrumHit(nTime, EPad.LP, chipLBD, inputEvent.nVelocity);
                                            bHitted = true;
                                        }
                                    }
                                }
                                if (bHitted)
                                {
                                    continue;
                                }
                                break;
                            #endregion

                            case EBDGroup.どっちもBD:
                                #region[ LP&LBD&BD ]
                                if (((e判定LP != EJudgement.Miss) && (e判定LBD != EJudgement.Miss)) && (e判定BD != EJudgement.Miss))
                                {
                                    CChip chip;
                                    CChip[] chipArray = new CChip[] { chipLP, chipLBD, chipBD };
                                    if (chipArray[1].nPlaybackPosition > chipArray[2].nPlaybackPosition)
                                    {
                                        chip = chipArray[1];
                                        chipArray[1] = chipArray[2];
                                        chipArray[2] = chip;
                                    }
                                    if (chipArray[0].nPlaybackPosition > chipArray[1].nPlaybackPosition)
                                    {
                                        chip = chipArray[0];
                                        chipArray[0] = chipArray[1];
                                        chipArray[1] = chip;
                                    }
                                    if (chipArray[1].nPlaybackPosition > chipArray[2].nPlaybackPosition)
                                    {
                                        chip = chipArray[1];
                                        chipArray[1] = chipArray[2];
                                        chipArray[2] = chip;
                                    }
                                    tProcessDrumHit(nTime, EPad.LP, chipArray[0], inputEvent.nVelocity);
                                    if (chipArray[0].nPlaybackPosition == chipArray[1].nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LP, chipArray[1], inputEvent.nVelocity);
                                    }
                                    if (chipArray[0].nPlaybackPosition == chipArray[2].nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LP, chipArray[2], inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if ((e判定LP != EJudgement.Miss) && (e判定LBD != EJudgement.Miss))
                                {
                                    if (chipLP.nPlaybackPosition < chipLBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LP, chipLP, inputEvent.nVelocity);
                                    }
                                    else if (chipLP.nPlaybackPosition > chipLBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LP, chipLBD, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.LP, chipLP, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.LP, chipLBD, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if ((e判定LP != EJudgement.Miss) && (e判定BD != EJudgement.Miss))
                                {
                                    if (chipLP.nPlaybackPosition < chipBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LP, chipLP, inputEvent.nVelocity);
                                    }
                                    else if (chipLP.nPlaybackPosition > chipBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LP, chipBD, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.LP, chipLP, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.LP, chipBD, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if ((e判定LBD != EJudgement.Miss) && (e判定BD != EJudgement.Miss))
                                {
                                    if (chipLBD.nPlaybackPosition < chipBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LP, chipLBD, inputEvent.nVelocity);
                                    }
                                    else if (chipLBD.nPlaybackPosition > chipBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LP, chipBD, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.LP, chipLBD, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.LP, chipBD, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if (e判定LP != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.LP, chipLP, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定LBD != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.LP, chipLBD, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定BD != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.LP, chipBD, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (bHitted)
                                {
                                    continue;
                                }
                                #endregion
                                break;

                            case EBDGroup.BDとLPで打ち分ける:
                            default:
                                #region [ 全部打ち分け時のヒット処理 ]
                                //-----------------------------
                                if (e判定LP != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.LP, chipLP, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (!bHitted)
                                    break;
                                continue;
                            //-----------------------------
                            #endregion
                        }
                        if (!bHitted)
                            break;
                        continue;
                    }
                    //-----------------
                    #endregion

                    case EPad.LBD:
                        #region [ LBDのヒット処理 ]
                        //-----------------
                    {
                        if (inputEvent.nVelocity <= CDTXMania.ConfigIni.nVelocityMin.LBD)
                            continue;
                        CChip chipBD  = timeline.NearestDrum(nTime + nInputAdjustTime + nPedalLagTime, EChannel.BassDrum, HitState.NotHit);
                        CChip chipLP  = timeline.NearestDrum(nTime + nInputAdjustTime + nPedalLagTime, EChannel.LeftPedal, HitState.NotHit);
                        CChip chipLBD = timeline.NearestDrum(nTime + nInputAdjustTime + nPedalLagTime, EChannel.LeftBassDrum, HitState.NotHit);
                        EJudgement e判定BD  = (chipBD  != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipBD,  nInputAdjustTime + nPedalLagTime) : EJudgement.Miss;
                        EJudgement e判定LP  = (chipLP  != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipLP,  nInputAdjustTime + nPedalLagTime) : EJudgement.Miss;
                        EJudgement e判定LBD = (chipLBD != null) ? e指定時刻からChipのJUDGEを返す(nTime, chipLBD, nInputAdjustTime + nPedalLagTime) : EJudgement.Miss;
                        switch (eBDGroup)
                        {
                            case EBDGroup.BDとLPで打ち分ける:
                                #region[ BD & LBD | LP ]
                                if ( e判定BD != EJudgement.Miss && e判定LBD != EJudgement.Miss )
                                {
                                    if ( chipBD.nPlaybackPosition < chipLBD.nPlaybackPosition )
                                    {
                                        tProcessDrumHit( nTime, EPad.LBD, chipBD, inputEvent.nVelocity );
                                    }
                                    else if ( chipBD.nPlaybackPosition > chipLBD.nPlaybackPosition )
                                    {
                                        tProcessDrumHit( nTime, EPad.LBD, chipLBD, inputEvent.nVelocity );
                                    }
                                    else
                                    {
                                        tProcessDrumHit( nTime, EPad.LBD, chipBD, inputEvent.nVelocity );
                                        tProcessDrumHit( nTime, EPad.LBD, chipLBD, inputEvent.nVelocity );
                                    }
                                    bHitted = true;
                                }
                                else if ( e判定BD != EJudgement.Miss )
                                {
                                    tProcessDrumHit( nTime, EPad.LBD, chipBD, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if ( e判定LBD != EJudgement.Miss )
                                {
                                    tProcessDrumHit( nTime, EPad.LBD, chipLBD, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if ( bHitted )
                                    continue;
                                else
                                    break;
                            #endregion

                            case EBDGroup.左右ペダルのみ打ち分ける:
                                #region[ LPのヒット処理]
                                if (e判定LP != EJudgement.Miss && e判定LBD != EJudgement.Miss)
                                {
                                    if (chipLP.nPlaybackPosition < chipLBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LBD, chipLP, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        if (chipLP.nPlaybackPosition > chipLBD.nPlaybackPosition)
                                        {
                                            tProcessDrumHit(nTime, EPad.LBD, chipLBD, inputEvent.nVelocity);
                                        }
                                        else
                                        {
                                            tProcessDrumHit(nTime, EPad.LBD, chipLP, inputEvent.nVelocity);
                                            tProcessDrumHit(nTime, EPad.LBD, chipLBD, inputEvent.nVelocity);
                                        }
                                    }
                                    bHitted = true;
                                }
                                else
                                {
                                    if (e判定LP != EJudgement.Miss)
                                    {
                                        tProcessDrumHit(nTime, EPad.LBD, chipLP, inputEvent.nVelocity);
                                        bHitted = true;
                                    }
                                    else
                                    {
                                        if (e判定LBD != EJudgement.Miss)
                                        {
                                            tProcessDrumHit(nTime, EPad.LBD, chipLBD, inputEvent.nVelocity);
                                            bHitted = true;
                                        }
                                    }
                                }
                                if (bHitted)
                                {
                                    continue;
                                }
                                break;
                            #endregion

                            case EBDGroup.どっちもBD:
                                #region[ LP&LBD&BD ]
                                if (((e判定LP != EJudgement.Miss) && (e判定LBD != EJudgement.Miss)) && (e判定BD != EJudgement.Miss))
                                {
                                    CChip chip;
                                    CChip[] chipArray = new CChip[] { chipLP, chipLBD, chipBD };
                                    if (chipArray[1].nPlaybackPosition > chipArray[2].nPlaybackPosition)
                                    {
                                        chip = chipArray[1];
                                        chipArray[1] = chipArray[2];
                                        chipArray[2] = chip;
                                    }
                                    if (chipArray[0].nPlaybackPosition > chipArray[1].nPlaybackPosition)
                                    {
                                        chip = chipArray[0];
                                        chipArray[0] = chipArray[1];
                                        chipArray[1] = chip;
                                    }
                                    if (chipArray[1].nPlaybackPosition > chipArray[2].nPlaybackPosition)
                                    {
                                        chip = chipArray[1];
                                        chipArray[1] = chipArray[2];
                                        chipArray[2] = chip;
                                    }
                                    tProcessDrumHit(nTime, EPad.LBD, chipArray[0], inputEvent.nVelocity);
                                    if (chipArray[0].nPlaybackPosition == chipArray[1].nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LBD, chipArray[1], inputEvent.nVelocity);
                                    }
                                    if (chipArray[0].nPlaybackPosition == chipArray[2].nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LBD, chipArray[2], inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if ((e判定LP != EJudgement.Miss) && (e判定LBD != EJudgement.Miss))
                                {
                                    if (chipLP.nPlaybackPosition < chipLBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LBD, chipLP, inputEvent.nVelocity);
                                    }
                                    else if (chipLP.nPlaybackPosition > chipLBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LBD, chipLBD, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.LBD, chipLP, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.LBD, chipLBD, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if ((e判定LP != EJudgement.Miss) && (e判定BD != EJudgement.Miss))
                                {
                                    if (chipLP.nPlaybackPosition < chipBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LBD, chipLP, inputEvent.nVelocity);
                                    }
                                    else if (chipLP.nPlaybackPosition > chipBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LBD, chipBD, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.LBD, chipLP, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.LBD, chipBD, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if ((e判定LBD != EJudgement.Miss) && (e判定BD != EJudgement.Miss))
                                {
                                    if (chipLBD.nPlaybackPosition < chipBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LBD, chipLBD, inputEvent.nVelocity);
                                    }
                                    else if (chipLBD.nPlaybackPosition > chipBD.nPlaybackPosition)
                                    {
                                        tProcessDrumHit(nTime, EPad.LBD, chipBD, inputEvent.nVelocity);
                                    }
                                    else
                                    {
                                        tProcessDrumHit(nTime, EPad.LBD, chipLBD, inputEvent.nVelocity);
                                        tProcessDrumHit(nTime, EPad.LBD, chipBD, inputEvent.nVelocity);
                                    }
                                    bHitted = true;
                                }
                                else if (e判定LP != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.LBD, chipLP, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定LBD != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.LBD, chipLBD, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                else if (e判定BD != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.LBD, chipBD, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (bHitted)
                                {
                                    continue;
                                }
                                #endregion
                                break;

                            default:
                                #region [ 全部打ち分け時のヒット処理 ]
                                //-----------------------------
                                if (e判定LBD != EJudgement.Miss)
                                {
                                    tProcessDrumHit(nTime, EPad.LBD, chipLBD, inputEvent.nVelocity);
                                    bHitted = true;
                                }
                                if (!bHitted)
                                    break;
                                continue;
                            //-----------------------------
                            #endregion
                        }
                        if (!bHitted)
                            break;
                        continue;
                    }
                    //-----------------
                    #endregion
                    #endregion

                }
                //-----------------------------
                #endregion
                #region [ (B) ヒットしてなかった場合は、レーンフラッシュ、パッドアニメ、空打ち音再生を実行 ]
                //-----------------------------
                actLaneFlushD.Start((ELane)nパッド0Atoレーン07[nPad], ((float)inputEvent.nVelocity) / 127f);
                actPad.Hit(nパッド0Atoパッド08[nPad]);

                if (CDTXMania.ConfigIni.bドラム打音を発声する)
                {
                    CChip rChip = r空うちChip(EInstrumentPart.DRUMS, (EPad)nPad);
                    if (rChip != null)
                    {
                        #region [ (B1) 空打ち音が譜面で指定されているのでそれを再生する。]
                        //-----------------
                        tPlaySound(rChip, AudioMixer.Timer.nSystemTimeMs, EInstrumentPart.DRUMS, CDTXMania.ConfigIni.n手動再生音量, CDTXMania.ConfigIni.b演奏音を強調する.Drums);
                        //-----------------
                        #endregion
                    }
                    else
                    {
                        #region [ (B2) 空打ち音が指定されていないので一番近いチップを探して再生する。]
                        //-----------------
                        switch (((EPad)nPad))
                        {
                            case EPad.HH:
                                #region [ *** ]
                                //-----------------------------
                            {
                                CChip chipHC = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.HiHatClose, HitState.DontCare);
                                CChip chipHO = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.HiHatOpen, HitState.DontCare);
                                CChip chipLC = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.LeftCymbal, HitState.DontCare);
                                switch (CDTXMania.ConfigIni.eHHGroup)
                                {
                                    case EHHGroup.ハイハットのみ打ち分ける:
                                        rChip = (chipHC != null) ? chipHC : chipLC;
                                        break;

                                    case EHHGroup.左シンバルのみ打ち分ける:
                                        rChip = (chipHC != null) ? chipHC : chipHO;
                                        break;

                                    case EHHGroup.全部共通:
                                        if (chipHC != null)
                                        {
                                            rChip = chipHC;
                                        }
                                        else if (chipHO == null)
                                        {
                                            rChip = chipLC;
                                        }
                                        else if (chipLC == null)
                                        {
                                            rChip = chipHO;
                                        }
                                        else if (chipHO.nPlaybackPosition < chipLC.nPlaybackPosition)
                                        {
                                            rChip = chipHO;
                                        }
                                        else
                                        {
                                            rChip = chipLC;
                                        }
                                        break;

                                    default:
                                        rChip = chipHC;
                                        break;
                                }
                            }
                                //-----------------------------
                                #endregion
                                break;

                            case EPad.LT:
                                #region [ *** ]
                                //-----------------------------
                            {
                                CChip chipLT = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.LowTom, HitState.DontCare);
                                CChip chipFT = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.FloorTom, HitState.DontCare);
                                if (CDTXMania.ConfigIni.eFTGroup != EFTGroup.打ち分ける)
                                    rChip = (chipLT != null) ? chipLT : chipFT;
                                else
                                    rChip = chipLT;
                            }
                                //-----------------------------
                                #endregion
                                break;

                            case EPad.FT:
                                #region [ *** ]
                                //-----------------------------
                            {
                                CChip chipLT = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.LowTom, HitState.DontCare);
                                CChip chipFT = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.FloorTom, HitState.DontCare);
                                if (CDTXMania.ConfigIni.eFTGroup != EFTGroup.打ち分ける)
                                    rChip = (chipFT != null) ? chipFT : chipLT;
                                else
                                    rChip = chipFT;
                            }
                                //-----------------------------
                                #endregion
                                break;

                            case EPad.CY:
                                #region [ *** ]
                                //-----------------------------
                            {

                                CChip chipCY = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.Cymbal, HitState.DontCare);
                                CChip chipRD = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.RideCymbal, HitState.DontCare);
                                if (CDTXMania.ConfigIni.eCYGroup != ECYGroup.打ち分ける)
                                    rChip = (chipCY != null) ? chipCY : chipRD;
                                else
                                    rChip = chipCY;
                            }
                                //-----------------------------
                                #endregion
                                break;

                            case EPad.HHO:
                                #region [ *** ]
                                //-----------------------------
                            {
                                CChip chipHC = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.HiHatClose, HitState.DontCare);
                                CChip chipHO = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.HiHatOpen, HitState.DontCare);
                                CChip chipLC = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.LeftCymbal, HitState.DontCare);
                                switch (CDTXMania.ConfigIni.eHHGroup)
                                {
                                    case EHHGroup.全部打ち分ける:
                                        rChip = chipHO;
                                        break;

                                    case EHHGroup.ハイハットのみ打ち分ける:
                                        rChip = (chipHO != null) ? chipHO : chipLC;
                                        break;

                                    case EHHGroup.左シンバルのみ打ち分ける:
                                        rChip = (chipHO != null) ? chipHO : chipHC;
                                        break;

                                    case EHHGroup.全部共通:
                                        if (chipHO != null)
                                        {
                                            rChip = chipHO;
                                        }
                                        else if (chipHC == null)
                                        {
                                            rChip = chipLC;
                                        }
                                        else if (chipLC == null)
                                        {
                                            rChip = chipHC;
                                        }
                                        else if (chipHC.nPlaybackPosition < chipLC.nPlaybackPosition)
                                        {
                                            rChip = chipHC;
                                        }
                                        else
                                        {
                                            rChip = chipLC;
                                        }
                                        break;
                                }
                            }
                                //-----------------------------
                                #endregion
                                break;

                            case EPad.RD:
                                #region [ *** ]
                                //-----------------------------
                            {
                                CChip chipCY = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.Cymbal, HitState.DontCare);
                                CChip chipRD = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.RideCymbal, HitState.DontCare);
                                if (CDTXMania.ConfigIni.eCYGroup != ECYGroup.打ち分ける)
                                    rChip = (chipRD != null) ? chipRD : chipCY;
                                else
                                    rChip = chipRD;
                            }
                                //-----------------------------
                                #endregion
                                break;

                            case EPad.LC:
                                #region [ *** ]
                                //-----------------------------
                            {
                                CChip chipHC = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.HiHatClose, HitState.DontCare);
                                CChip chipHO = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.HiHatOpen, HitState.DontCare);
                                CChip chipLC = timeline.NearestDrum(nTime + nInputAdjustTime, EChannel.LeftCymbal, HitState.DontCare);
                                switch (CDTXMania.ConfigIni.eHHGroup)
                                {
                                    case EHHGroup.全部打ち分ける:
                                    case EHHGroup.左シンバルのみ打ち分ける:
                                        rChip = chipLC;
                                        break;

                                    case EHHGroup.ハイハットのみ打ち分ける:
                                    case EHHGroup.全部共通:
                                        if (chipLC != null)
                                        {
                                            rChip = chipLC;
                                        }
                                        else if (chipHC == null)
                                        {
                                            rChip = chipHO;
                                        }
                                        else if (chipHO == null)
                                        {
                                            rChip = chipHC;
                                        }
                                        else if (chipHC.nPlaybackPosition < chipHO.nPlaybackPosition)
                                        {
                                            rChip = chipHC;
                                        }
                                        else
                                        {
                                            rChip = chipHO;
                                        }
                                        break;
                                }
                            }
                                //-----------------------------
                                #endregion
                                break;

                            case EPad.BD:
                                #region [ *** ]
                                //-----------------------------
                            {
                                CChip chipBD = timeline.NearestDrum(nTime + nInputAdjustTime + nPedalLagTime, EChannel.BassDrum, HitState.DontCare);
                                CChip chipLP = timeline.NearestDrum(nTime + nInputAdjustTime + nPedalLagTime, EChannel.LeftPedal, HitState.DontCare);
                                CChip chipLBD = timeline.NearestDrum(nTime + nInputAdjustTime + nPedalLagTime, EChannel.LeftBassDrum, HitState.DontCare);
                                switch (CDTXMania.ConfigIni.eBDGroup)
                                {
                                    case EBDGroup.打ち分ける:
                                        rChip = chipBD;
                                        break;

                                    case EBDGroup.左右ペダルのみ打ち分ける:
                                        rChip = (chipBD != null) ? chipBD : chipLP;
                                        break;

                                    case EBDGroup.BDとLPで打ち分ける:
                                        if ( chipBD != null && chipLBD != null )
                                        {
                                            if ( chipBD.nPlaybackTimeMs >= chipLBD.nPlaybackTimeMs )
                                                rChip = chipBD;
                                            else if ( chipBD.nPlaybackTimeMs < chipLBD.nPlaybackTimeMs )
                                                rChip = chipLBD;
                                        }
                                        else if ( chipLBD != null )
                                        {
                                            rChip = chipLBD;
                                        }
                                        else
                                        {
                                            rChip = chipBD;
                                        }
                                        break;

                                    case EBDGroup.どっちもBD:
                                        #region [ *** ]
                                        if (chipBD != null)
                                        {
                                            rChip = chipBD;
                                        }
                                        else if (chipLP == null)
                                        {
                                            rChip = chipLBD;
                                        }
                                        else if (chipLBD == null)
                                        {
                                            rChip = chipLP;
                                        }
                                        else if (chipLP.nPlaybackPosition < chipLBD.nPlaybackPosition)
                                        {
                                            rChip = chipLP;
                                        }
                                        else
                                        {
                                            rChip = chipLBD;
                                        }
                                        #endregion
                                        break;
                                }
                            }
                                //-----------------------------
                                #endregion
                                break;
                                    
                            case EPad.LP:
                                #region [ *** ]
                                //-----------------------------
                            {
                                CChip chipBD = timeline.NearestDrum(nTime + nInputAdjustTime + nPedalLagTime, EChannel.BassDrum, HitState.DontCare);
                                CChip chipLP = timeline.NearestDrum(nTime + nInputAdjustTime + nPedalLagTime, EChannel.LeftPedal, HitState.DontCare);
                                CChip chipLBD = timeline.NearestDrum(nTime + nInputAdjustTime + nPedalLagTime, EChannel.LeftBassDrum, HitState.DontCare);
                                switch (CDTXMania.ConfigIni.eBDGroup)
                                {
                                    case EBDGroup.打ち分ける:
                                        rChip = chipLP;
                                        break;
                                    case EBDGroup.左右ペダルのみ打ち分ける:
                                        #region[左右ペダル]
                                        rChip = (chipLP != null) ? chipLP : chipLBD;
                                        #endregion
                                        break;
                                    case EBDGroup.BDとLPで打ち分ける:
                                        #region[ BDとLP ]
                                        if ( chipLP != null ){ rChip = chipLP; }
                                        #endregion
                                        break;
                                    case EBDGroup.どっちもBD:
                                        #region[共通]
                                        if (chipLP != null)
                                        {
                                            rChip = chipLP;
                                        }
                                        else if (chipLBD == null)
                                        {
                                            rChip = chipBD;
                                        }
                                        else if (chipBD == null)
                                        {
                                            rChip = chipLBD;
                                        }
                                        else if (chipLBD.nPlaybackPosition < chipBD.nPlaybackPosition)
                                        {
                                            rChip = chipLBD;
                                        }
                                        else
                                        {
                                            rChip = chipBD;
                                        }
                                        #endregion
                                        break;

                                }
                            }
                                //-----------------------------
                                #endregion
                                break;

                            case EPad.LBD:
                                #region [ *** ]
                            {
                                CChip chipBD = timeline.NearestDrum(nTime + nInputAdjustTime + nPedalLagTime, EChannel.BassDrum, HitState.DontCare);
                                CChip chipLP = timeline.NearestDrum(nTime + nInputAdjustTime + nPedalLagTime, EChannel.LeftPedal, HitState.DontCare);
                                CChip chipLBD = timeline.NearestDrum(nTime + nInputAdjustTime + nPedalLagTime, EChannel.LeftBassDrum, HitState.DontCare);
                                switch (CDTXMania.ConfigIni.eBDGroup)
                                {
                                    case EBDGroup.打ち分ける:
                                        rChip = chipLBD;
                                        break;
                                    case EBDGroup.左右ペダルのみ打ち分ける:
                                        #region [ *** ]
                                        rChip = (chipLBD != null) ? chipLBD : chipBD;
                                        #endregion
                                        break;
                                    case EBDGroup.BDとLPで打ち分ける:
                                        #region[ BDとLBD ]
                                        if ( chipBD != null && chipLBD != null )
                                        {
                                            if ( chipBD.nPlaybackTimeMs <= chipLBD.nPlaybackTimeMs )
                                                rChip = chipLBD;
                                            else if ( chipBD.nPlaybackTimeMs > chipLBD.nPlaybackTimeMs )
                                                rChip = chipBD;
                                        }
                                        else if ( chipLBD != null )
                                        {
                                            rChip = chipLBD;
                                        }
                                        else
                                        {
                                            rChip = chipBD;
                                        }
                                        #endregion
                                        break;
                                    case EBDGroup.どっちもBD:
                                        #region[ *** ]
                                        if (chipLBD != null)
                                        {
                                            rChip = chipLBD;
                                        }
                                        else if (chipLP == null)
                                        {
                                            rChip = chipBD;
                                        }
                                        else if (chipBD == null)
                                        {
                                            rChip = chipLP;
                                        }
                                        else if (chipLP.nPlaybackPosition < chipBD.nPlaybackPosition)
                                        {
                                            rChip = chipLP;
                                        }
                                        else
                                        {
                                            rChip = chipBD;
                                        }
                                        #endregion
                                        break;
                                }
                            }
                                #endregion
                                break;

                            default:
                                #region [ *** ]
                                //-----------------------------
                                rChip = timeline.NearestDrum(nTime + nInputAdjustTime, nパッド0Atoチャンネル0A[nPad], HitState.DontCare);
                                //-----------------------------
                                #endregion
                                break;
                        }
                        if (rChip != null)
                        {
                            // 空打ち音が見つかったので再生する。
                            tPlaySound(rChip, AudioMixer.Timer.nSystemTimeMs, EInstrumentPart.DRUMS, CDTXMania.ConfigIni.n手動再生音量, CDTXMania.ConfigIni.b演奏音を強調する.Drums);
                        }
                        //-----------------
                        #endregion
                    }
                }

                // BAD or TIGHT 時の処理。
                if (CDTXMania.ConfigIni.bTight)
                    tチップのヒット処理_BadならびにTight時のMiss(EInstrumentPart.DRUMS, nパッド0Atoレーン07[nPad]);
                //-----------------------------
                #endregion
            }
        }
    }

    // tHandleInput_Drums()からメソッドを抽出したもの。
    /// <summary>
    /// chipArrayの中を, n発生位置の小さい順に並べる + nullを大きい方に退かす。セットでe判定Arrayも並べ直す。
    /// </summary>
    /// <param name="chipArray">ソート対象chip群</param>
    /// <param name="e判定Array">ソート対象e判定群</param>
    /// <param name="NumOfChips">チップ数</param>
    private static void SortChipsByNTime( CChip[] chipArray, EJudgement[] e判定Array, int NumOfChips )
    {
        for ( int i = 0; i < NumOfChips - 1; i++ )
        {
            for ( int j = NumOfChips - 1; j > i; j-- )
            {
                if ( ( chipArray[ j - 1 ] == null ) || ( ( chipArray[ j ] != null ) && ( chipArray[ j - 1 ].nPlaybackPosition > chipArray[ j ].nPlaybackPosition ) ) )
                {
                    // swap
                    CChip chipTemp = chipArray[ j - 1 ];
                    chipArray[ j - 1 ] = chipArray[ j ];
                    chipArray[ j ] = chipTemp;
                    EJudgement e判定Temp = e判定Array[ j - 1 ];
                    e判定Array[ j - 1 ] = e判定Array[ j ];
                    e判定Array[ j ] = e判定Temp;
                }
            }
        }
    }

    private bool tProcessDrumHit( long nHitTime, EPad type, CChip pChip, int n強弱度合い0to127)  // tドラムヒット処理
    {
        if ( pChip == null )
        {
            return false;
        }
        EChannel index = pChip.nChannelNumber;
        if ( ( index >= EChannel.HiHatClose) && ( index <= EChannel.LeftBassDrum) )
        {
            index -= 0x11;
        }
        else if ( ( index >= EChannel.HiHatClose_Hidden) && ( index <= EChannel.LeftBassDrum_Hidden) )
        {
            index -= 0x31;
        }
        int nLane = nチャンネル0Atoレーン07[ (int)index ];
        int nPad = nチャンネル0Atoパッド08[(int)index ];
        bool bPChipIsAutoPlay = bIsAutoPlay[ nLane ];
        int nInputAdjustTime = bPChipIsAutoPlay ? 0 : nInputAdjustTimeMs.Drums;
        EJudgement e判定 = e指定時刻からChipのJUDGEを返す( nHitTime, pChip, nInputAdjustTime );
        if ( e判定 == EJudgement.Miss )
        {
            return false;
        }
        tProcessChipHit( nHitTime, pChip );
        actLaneFlushD.Start( (ELane) nLane, ( (float) n強弱度合い0to127 ) / 127f );
        actPad.Hit( nPad );
        if ( ( e判定 != EJudgement.Poor ) && ( e判定 != EJudgement.Miss ) )
        {
            bool flag = bInFillIn;
            bool flag2 = bInFillIn && bフィルイン区間の最後のChipである( pChip );
            actChipFireD.Start( (ELane)nLane, flag, flag2, flag2, nJudgeLinePosY_delta.Drums );
            // #31602 2013.6.24 yyagi 判定ラインの表示位置をずらしたら、チップのヒットエフェクトの表示もずらすために、nJudgeLine..を追加
        }
        if ( CDTXMania.ConfigIni.bドラム打音を発声する )
        {
            CChip rChip = null;
            bool bIsChipsoundPriorToPad = true;
            if ( ( ( type == EPad.HH ) || ( type == EPad.HHO ) ) || ( type == EPad.LC ) )
            {
                bIsChipsoundPriorToPad = CDTXMania.ConfigIni.eHitSoundPriorityHH == EPlaybackPriority.ChipOverPadPriority;
            }
            else if ( ( type == EPad.LT ) || ( type == EPad.FT ) )
            {
                bIsChipsoundPriorToPad = CDTXMania.ConfigIni.eHitSoundPriorityFT == EPlaybackPriority.ChipOverPadPriority;
            }
            else if ( ( type == EPad.CY ) || ( type == EPad.RD ) )
            {
                bIsChipsoundPriorToPad = CDTXMania.ConfigIni.eHitSoundPriorityCY == EPlaybackPriority.ChipOverPadPriority;
            }
            else if (((type == EPad.LP) || (type == EPad.LBD)) || (type == EPad.BD))
            {
                bIsChipsoundPriorToPad = CDTXMania.ConfigIni.eHitSoundPriorityLP == EPlaybackPriority.ChipOverPadPriority;
            }

            if ( bIsChipsoundPriorToPad )
            {
                rChip = pChip;
            }
            else
            {
                EPad hH = type;
                if ( !CDTXMania.DTX.bHasChips.HHOpen && ( type == EPad.HHO ) )
                {
                    hH = EPad.HH;
                }
                if ( !CDTXMania.DTX.bHasChips.Ride && ( type == EPad.RD ) )
                {
                    hH = EPad.CY;
                }
                if ( !CDTXMania.DTX.bHasChips.LeftCymbal && ( type == EPad.LC ) )
                {
                    hH = EPad.HH;
                }
                rChip = timeline.NearestDrum(nHitTime + nInputAdjustTime, nパッド0Atoチャンネル0A[(int) hH], HitState.DontCare);
                if ( rChip == null )
                {
                    rChip = pChip;
                }
            }
            tPlaySound( rChip, AudioMixer.Timer.nSystemTimeMs, EInstrumentPart.DRUMS, CDTXMania.ConfigIni.n手動再生音量, CDTXMania.ConfigIni.b演奏音を強調する.Drums );
        }
        return true;
    }
}
