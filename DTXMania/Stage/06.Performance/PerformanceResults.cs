using DTXMania.Core;

namespace DTXMania;

internal static class PerformanceResults
{
    public static CScoreIni? SaveStageFailed(CStagePerfCommonScreen screen)
    {
        CScoreIni scoreIni = null;
        //New extract performance record
        screen.tStorePerfResults(out CScoreIni.CPerformanceEntry cPerf_Drums,
            out CScoreIni.CPerformanceEntry cPerf_Guitar, out CScoreIni.CPerformanceEntry cPerf_Bass,
            out bool bTrainingMode);
        //Original
        //scoreIni = this.tScoreIniへBGMAdjustとHistoryとPlayCountを更新("Stage failed");

        //Save Performance Records if necessary
        if (!bTrainingMode)
        {
            //Swap if required
            if (CDTXMania.ConfigIni
                .bIsSwappedGuitarBass) // #24063 2011.1.24 yyagi Gt/Bsを入れ替えていたなら、演奏結果も入れ替える
            {
                CScoreIni.CPerformanceEntry t;
                t = cPerf_Guitar;
                cPerf_Guitar = cPerf_Bass;
                cPerf_Bass = t;
            }

            string strInstrument = "";
            string strPerfSkill = "";
            //STDGBVALUE<string> strCurrProgressBars;
            STDGBVALUE<bool> bToSaveProgressBarRecord;
            bToSaveProgressBarRecord.Drums = false;
            bToSaveProgressBarRecord.Guitar = false;
            bToSaveProgressBarRecord.Bass = false;
            STDGBVALUE<bool> bNewProgressBarRecord;
            bNewProgressBarRecord.Drums = false;
            bNewProgressBarRecord.Guitar = false;
            bNewProgressBarRecord.Bass = false;
            bool bGuitarAndBass = false;
            if (!cPerf_Drums.bHasAnyAutoAtAll && cPerf_Drums.nTotalChipsCount > 0)
            {
                //Drums played
                strInstrument = " Drums";
                bToSaveProgressBarRecord.Drums = true;
            }
            else if (!cPerf_Guitar.bHasAnyAutoAtAll && cPerf_Guitar.nTotalChipsCount > 0)
            {
                if (!cPerf_Bass.bHasAnyAutoAtAll && cPerf_Bass.nTotalChipsCount > 0)
                {
                    // Guitar and bass played together
                    bGuitarAndBass = true;
                    strInstrument = " G+B";
                    bToSaveProgressBarRecord.Guitar = true;
                    bToSaveProgressBarRecord.Bass = true;
                }
                else
                {
                    // Guitar only played
                    strInstrument = " Guitar";
                    bToSaveProgressBarRecord.Guitar = true;
                }
            }
            else
            {
                //Bass only played
                strInstrument = " Bass";
                bToSaveProgressBarRecord.Bass = true;
            }

            string str = "";
            string strSpeed = "";
            if (CDTXMania.ConfigIni.nPlaySpeed != 20)
            {
                double d = CDTXMania.ConfigIni.nPlaySpeed / 20.0;
                strSpeed = (bGuitarAndBass ? " x" : " Speed x") + d.ToString("0.00");
            }

            str = $"Stage failed{strInstrument} {strSpeed}";

            scoreIni = CDTXMania.UpdateBGMAdjustHistoryPlayCountIntScoreIni(str);

            CChartData cChartData = CDTXMania.chosenChartData;

            if (bToSaveProgressBarRecord.Drums)
            {
                scoreIni.stSection.LastPlayDrums.strProgress = cPerf_Drums.strProgress;

                if (CScoreIni.tCheckIfUpdateProgressBarRecordOrNot(
                        cChartData.SongInformation.progress.Drums, cPerf_Drums.strProgress))
                {
                    scoreIni.stSection.HiSkillDrums.strProgress = cPerf_Drums.strProgress;
                    bNewProgressBarRecord.Drums = true;
                }
            }

            if (bToSaveProgressBarRecord.Guitar)
            {
                scoreIni.stSection.LastPlayGuitar.strProgress = cPerf_Guitar.strProgress;
                if (CScoreIni.tCheckIfUpdateProgressBarRecordOrNot(
                        cChartData.SongInformation.progress.Guitar, cPerf_Guitar.strProgress))
                {
                    scoreIni.stSection.HiSkillGuitar.strProgress = cPerf_Guitar.strProgress;
                    bNewProgressBarRecord.Guitar = true;
                }
            }

            if (bToSaveProgressBarRecord.Bass)
            {
                scoreIni.stSection.LastPlayBass.strProgress = cPerf_Bass.strProgress;
                if (CScoreIni.tCheckIfUpdateProgressBarRecordOrNot(
                        cChartData.SongInformation.progress.Bass, cPerf_Bass.strProgress))
                {
                    scoreIni.stSection.HiSkillBass.strProgress = cPerf_Bass.strProgress;
                    bNewProgressBarRecord.Bass = true;
                }
            }

            scoreIni.tExport(CDTXMania.DTX.strFileNameFullPath + ".score.ini");

            if (!CDTXMania.bCompactMode)
            {
                cChartData.UpdateScoreIniInformation();
                if (CDTXMania.chosenSong != null && CDTXMania.confirmedSongDifficulty >= 0 && CDTXMania.confirmedSongDifficulty < CDTXMania.chosenSong.charts.Length)
                {
                    CDTXMania.chosenSong.charts[CDTXMania.confirmedSongDifficulty]?.UpdateScoreIniInformation();
                }

                if (bNewProgressBarRecord.Drums)
                {
                    // New Song Progress
                    cChartData.SongInformation.progress.Drums = cPerf_Drums.strProgress;
                }

                if (bNewProgressBarRecord.Guitar)
                {
                    // New Song Progress
                    cChartData.SongInformation.progress.Guitar = cPerf_Guitar.strProgress;
                }

                if (bNewProgressBarRecord.Bass)
                {
                    // New Song Progress
                    cChartData.SongInformation.progress.Bass = cPerf_Bass.strProgress;
                }
            }
        }
        return scoreIni;
    }

    public static CScoreIni? SaveStageClear(CStagePerfCommonScreen screen, out CScoreIni.CPerformanceEntry cPerfEntry_Drums,
        out CScoreIni.CPerformanceEntry cPerfEntry_Guitar, out CScoreIni.CPerformanceEntry cPerfEntry_Bass,
        out CChip[] chipArray, out bool bIsTrainingMode)
    {
        CScoreIni scoreIni = null;
        //-----------------------------
        screen.tStorePerfResults(out cPerfEntry_Drums, out cPerfEntry_Guitar, out cPerfEntry_Bass, out bIsTrainingMode);
        chipArray = screen.EmptyDrumChips();

        if (!bIsTrainingMode)
        {
            if (CDTXMania.ConfigIni
                .bIsSwappedGuitarBass) // #24063 2011.1.24 yyagi Gt/Bsを入れ替えていたなら、演奏結果も入れ替える
            {
                CScoreIni.CPerformanceEntry t;
                t = cPerfEntry_Guitar;
                cPerfEntry_Guitar = cPerfEntry_Bass;
                cPerfEntry_Bass = t;

                CDTXMania.DTX.SwapGuitarBassInfos(); // 譜面情報も元に戻す
                CDTXMania.ConfigIni.SwapGuitarBassInfos_AutoFlags(); // #24415 2011.2.27 yyagi
                // リザルト集計時のみ、Auto系のフラグも元に戻す。
                // これを戻すのは、リザルト集計後。
            } // "case CStage.EStage.Result:"のところ。

            double ps = 0.0;
            int nRank;
            string strInstrument = "";
            string strPerfSkill;
            bool bGuitarAndBass = false;
            if (cPerfEntry_Drums is { bHasAnyAutoAtAll: false, nTotalChipsCount: > 0 })
            {
                //Drums played
                strPerfSkill = $" {cPerfEntry_Drums.dbPerformanceSkill:F2}";
                nRank = (CDTXMania.ConfigIni.nSkillMode == 0)
                    ? CScoreIni.tCalculateRankOld(cPerfEntry_Drums)
                    : CScoreIni.tCalculateRank(cPerfEntry_Drums.dbPerformanceSkill);
            }
            else if (cPerfEntry_Guitar is { bHasAnyAutoAtAll: false, nTotalChipsCount: > 0 })
            {
                if (cPerfEntry_Bass is { bHasAnyAutoAtAll: false, nTotalChipsCount: > 0 })
                {
                    // Guitar and bass played together
                    bGuitarAndBass = true;
                    strPerfSkill =
                        $"{cPerfEntry_Guitar.dbPerformanceSkill:F2}/{cPerfEntry_Bass.dbPerformanceSkill:F2}";
                    nRank = CScoreIni.tCalculateOverallRankValue(cPerfEntry_Drums,
                        cPerfEntry_Guitar, cPerfEntry_Bass);
                    strInstrument = " G+B";
                }
                else
                {
                    // Guitar only played
                    strPerfSkill = $" {cPerfEntry_Guitar.dbPerformanceSkill:F2}";
                    nRank = (CDTXMania.ConfigIni.nSkillMode == 0)
                        ? CScoreIni.tCalculateRankOld(cPerfEntry_Guitar)
                        : CScoreIni.tCalculateRank(cPerfEntry_Guitar.dbPerformanceSkill);
                    strInstrument = " Guitar";
                }
            }
            else
            {
                //Bass only played
                strPerfSkill = $" {cPerfEntry_Bass.dbPerformanceSkill:F2}";
                nRank = (CDTXMania.ConfigIni.nSkillMode == 0)
                    ? CScoreIni.tCalculateRankOld(cPerfEntry_Bass)
                    : CScoreIni.tCalculateRank(cPerfEntry_Bass.dbPerformanceSkill);
                strInstrument = " Bass";
            }

            string str;
            if (nRank == (int)CScoreIni.ERANK.UNKNOWN)
            {
                str = "Cleared (No chips)";
            }
            else
            {
                string strSpeed = "";
                if (CDTXMania.ConfigIni.nPlaySpeed != 20)
                {
                    double d = CDTXMania.ConfigIni.nPlaySpeed / 20.0;
                    strSpeed = (bGuitarAndBass ? " x" : " Speed x") + d.ToString("0.00");
                }

                str = $"Cleared{strInstrument} ({Enum.GetName(typeof(CScoreIni.ERANK), nRank)}:{strPerfSkill}{strSpeed})";
            }

            scoreIni = CDTXMania.UpdateBGMAdjustHistoryPlayCountIntScoreIni(str);
        }


        return scoreIni;
    }
    public static CScoreIni.CPerformanceEntry Create(PerformanceData data, string progress)
    {
        CConfigIni config = CDTXMania.ConfigIni;
        CDTX dtx = CDTXMania.DTX;
        EInstrumentPart part = data.Part;
        int index = (int)part;
        HitCounts hits = data.HitCountExclAuto;
        HitCounts shownHits = AllAuto(part) ? data.HitCountIncAuto : hits;

        CScoreIni.CPerformanceEntry entry = new()
        {
            nScore = (long)data.Score,
            nPerfectCount = shownHits.Perfect,
            nGreatCount = shownHits.Great,
            nGoodCount = shownHits.Good,
            nPoorCount = shownHits.Poor,
            nMissCount = shownHits.Miss,
            nPerfectCount_ExclAuto = hits.Perfect,
            nGreatCount_ExclAuto = hits.Great,
            nGoodCount_ExclAuto = hits.Good,
            nPoorCount_ExclAuto = hits.Poor,
            nMissCount_ExclAuto = hits.Miss,
            nFastCount = data.TimingHitCount.Early,
            nSlowCount = data.TimingHitCount.Late,
            nTotalOffsetLag = (int)data.TimingHitCount.TotalLag,
            nOffsetHitCount = data.TimingHitCount.LagHitCount,
            dbAverageOffset = data.TimingHitCount.AverageLag,
            nMaxCombo = data.MaxCombo,
            nTotalChipsCount = data.ChartChips,
            bTight = config.bTight,
            eDark = config.eDark[index],
            nPlaySpeedNumerator = config.nPlaySpeed,
            nPlaySpeedDenominator = 20,
            eHHGroup = config.eHHGroup,
            eFTGroup = config.eFTGroup,
            eCYGroup = config.eCYGroup,
            eHitSoundPriorityHH = config.eHitSoundPriorityHH,
            eHitSoundPriorityFT = config.eHitSoundPriorityFT,
            eHitSoundPriorityCY = config.eHitSoundPriorityCY,
            bGuitarEnabled = config.bGuitarEnabled,
            bDrumsEnabled = config.bDrumsEnabled,
            bSTAGEFAILEDEnabled = config.bSTAGEFAILEDEnabled,
            eDamageLevel = config.eDamageLevel,
            bKeyboardUsed = data.UsedKeyboard,
            bMIDIUsed = data.UsedMidi,
            bJoypadUsed = data.UsedJoypad,
            bMouseUsed = data.UsedMouse,
            stPrimaryHitRanges = part switch
            {
                EInstrumentPart.DRUMS => CDTXMania.stDrumHitRanges,
                EInstrumentPart.GUITAR => CDTXMania.stGuitarHitRanges,
                _ => CDTXMania.stBassHitRanges
            },
            stSecondaryHitRanges = part == EInstrumentPart.DRUMS ? CDTXMania.stDrumPedalHitRanges : new STHitRanges(),
            strDTXManiaVersion = CDTXMania.VERSION,
            strDateTime = DateTime.Now.ToString(),
            strProgress = progress
        };

        if (config.nSkillMode == 0)
        {
            entry.dbGameSkill = CScoreIni.tCalculateGameSkillOld(dtx.LEVEL[index], dtx.LEVELDEC[index], data.ChartChips, hits.Perfect, hits.Great,
                data.MaxCombo, part, data.AutoPlay);
            entry.dbPerformanceSkill = data.CalculateSkill();
        }
        else if (config.nSkillMode == 1)
        {
            entry.dbPerformanceSkill = data.CalculateSkill();
            entry.dbGameSkill = CScoreIni.tCalculateGameSkillFromPlayingSkill(dtx.LEVEL[index], dtx.LEVELDEC[index], entry.dbPerformanceSkill);
        }

        for (int i = 0; i < (int)ELane.MAX; i++)
        {
            entry.bAutoPlay[i] = data.AutoPlay[i];
        }

        for (int i = 0; i < 3; i++)
        {
            entry.bSudden[i] = config.bSudden[i];
            entry.bHidden[i] = config.bHidden[i];
            entry.bReverse[i] = config.bReverse[i];
            entry.eRandom[i] = config.eRandom[i];
            entry.bLight[i] = config.bLight[i];
            entry.bLeft[i] = config.bLeft[i];
            entry.fScrollSpeed[i] = (config.nScrollSpeed[i] + 1) * 0.5f;
        }

        entry.Hash = CScoreIni.tComputePerformanceSectionMD5(entry);
        return entry;
    }

    private static bool AllAuto(EInstrumentPart part) => part switch
    {
        EInstrumentPart.DRUMS => CDTXMania.ConfigIni.bAllDrumsAreAutoPlay,
        EInstrumentPart.GUITAR => CDTXMania.ConfigIni.bAllGuitarsAreAutoPlay,
        _ => CDTXMania.ConfigIni.bAllBassAreAutoPlay
    };
}
