using DTXMania.Core;

namespace DTXMania;

internal static class PerformanceResults
{
    internal enum EPlayed { Drums, Guitar, Bass, GuitarAndBass }

    internal static bool WasPlayed(CScoreIni.CPerformanceEntry entry) => !entry.bHasAnyAutoAtAll && entry.nTotalChipsCount > 0;

    internal static EPlayed PlayedPart(CScoreIni.CPerformanceEntry drums, CScoreIni.CPerformanceEntry guitar, CScoreIni.CPerformanceEntry bass)
    {
        if (WasPlayed(drums))
        {
            return EPlayed.Drums;
        }

        if (WasPlayed(guitar))
        {
            return WasPlayed(bass) ? EPlayed.GuitarAndBass : EPlayed.Guitar;
        }

        return EPlayed.Bass;
    }

    private static void SwapGuitarAndBass(ref CScoreIni.CPerformanceEntry guitar, ref CScoreIni.CPerformanceEntry bass)
    {
        CScoreIni.CPerformanceEntry swapped = guitar;
        guitar = bass;
        bass = swapped;
    }

    //part: 0 drums, 1 guitar, 2 bass
    internal static bool SavesProgress(EPlayed played, int part) => part switch
    {
        0 => played == EPlayed.Drums,
        1 => played is EPlayed.Guitar or EPlayed.GuitarAndBass,
        _ => played is EPlayed.Bass or EPlayed.GuitarAndBass
    };

    //eg: " Speed x1.50", or " x1.50" with guitar and bass together
    internal static string SpeedSuffix(bool guitarAndBass)
    {
        if (CDTXMania.ConfigIni.nPlaySpeed == 20)
        {
            return "";
        }

        double d = CDTXMania.ConfigIni.nPlaySpeed / 20.0;
        return (guitarAndBass ? " x" : " Speed x") + d.ToString("0.00");
    }

    //eg: "Stage failed Guitar  Speed x1.50"
    internal static string FailedHistoryLine(CScoreIni.CPerformanceEntry drums, CScoreIni.CPerformanceEntry guitar, CScoreIni.CPerformanceEntry bass)
    {
        EPlayed played = PlayedPart(drums, guitar, bass);
        string strInstrument = played switch
        {
            EPlayed.Drums => " Drums",
            EPlayed.GuitarAndBass => " G+B",
            EPlayed.Guitar => " Guitar",
            _ => " Bass"
        };

        return $"Stage failed{strInstrument} {SpeedSuffix(played == EPlayed.GuitarAndBass)}";
    }

    //eg: "Cleared Guitar (A: 82.50 Speed x1.50)", or "Cleared (No chips)"
    internal static string ClearedHistoryLine(CScoreIni.CPerformanceEntry drums, CScoreIni.CPerformanceEntry guitar, CScoreIni.CPerformanceEntry bass)
    {
        EPlayed played = PlayedPart(drums, guitar, bass);
        bool guitarAndBass = played == EPlayed.GuitarAndBass;
        string strInstrument = played switch
        {
            EPlayed.Drums => "",
            EPlayed.GuitarAndBass => " G+B",
            EPlayed.Guitar => " Guitar",
            _ => " Bass"
        };

        CScoreIni.CPerformanceEntry scored = played switch
        {
            EPlayed.Drums => drums,
            EPlayed.Bass => bass,
            _ => guitar
        };

        string strPerfSkill = guitarAndBass
            ? $"{guitar.dbPerformanceSkill:F2}/{bass.dbPerformanceSkill:F2}"
            : $" {scored.dbPerformanceSkill:F2}";

        int nRank = guitarAndBass
            ? CScoreIni.tCalculateOverallRankValue(drums, guitar, bass)
            : CDTXMania.ConfigIni.nSkillMode == 0
                ? CScoreIni.tCalculateRankOld(scored)
                : CScoreIni.tCalculateRank(scored.dbPerformanceSkill);

        if (nRank == (int)CScoreIni.ERANK.UNKNOWN)
        {
            return "Cleared (No chips)";
        }

        return $"Cleared{strInstrument} ({Enum.GetName(typeof(CScoreIni.ERANK), nRank)}:{strPerfSkill}{SpeedSuffix(guitarAndBass)})";
    }

    public static CScoreIni? SaveStageFailed(CStagePerfCommonScreen screen)
    {
        CScoreIni? scoreIni = null;
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
                SwapGuitarAndBass(ref cPerf_Guitar, ref cPerf_Bass);
            }

            EPlayed played = PlayedPart(cPerf_Drums, cPerf_Guitar, cPerf_Bass);
            CScoreIni.CPerformanceEntry[] entries = [cPerf_Drums, cPerf_Guitar, cPerf_Bass];

            scoreIni = CDTXMania.UpdateBGMAdjustHistoryPlayCountIntScoreIni(FailedHistoryLine(cPerf_Drums, cPerf_Guitar, cPerf_Bass));

            CChartData cChartData = CDTXMania.chosenChartData;
            bool[] isRecord = new bool[3];

            for (int part = 0; part < entries.Length; part++)
            {
                if (!SavesProgress(played, part))
                {
                    continue;
                }

                string progress = entries[part].strProgress;
                scoreIni.stSection[(int)CScoreIni.ESectionType.LastPlayDrums + part].strProgress = progress;

                if (CScoreIni.tCheckIfUpdateProgressBarRecordOrNot(cChartData.SongInformation.progress[part], progress))
                {
                    scoreIni.stSection[(int)CScoreIni.ESectionType.HiSkillDrums + part * 2].strProgress = progress;
                    isRecord[part] = true;
                }
            }

            scoreIni.tExport(CDTXMania.DTX.strFileNameFullPath + ".score.ini");

            if (!CDTXMania.bCompactMode)
            {
                for (int part = 0; part < entries.Length; part++)
                {
                    if (isRecord[part])
                    {
                        cChartData.SongInformation.progress[part] = entries[part].strProgress;
                    }
                }
            }
        }
        return scoreIni;
    }

    public static CScoreIni? SaveStageClear(CStagePerfCommonScreen screen, out CScoreIni.CPerformanceEntry cPerfEntry_Drums,
        out CScoreIni.CPerformanceEntry cPerfEntry_Guitar, out CScoreIni.CPerformanceEntry cPerfEntry_Bass,
        out CChip[] chipArray, out bool bIsTrainingMode)
    {
        CScoreIni? scoreIni = null;
        //-----------------------------
        screen.tStorePerfResults(out cPerfEntry_Drums, out cPerfEntry_Guitar, out cPerfEntry_Bass, out bIsTrainingMode);
        chipArray = screen.EmptyDrumChips();

        if (!bIsTrainingMode)
        {
            if (CDTXMania.ConfigIni
                .bIsSwappedGuitarBass) // #24063 2011.1.24 yyagi Gt/Bsを入れ替えていたなら、演奏結果も入れ替える
            {
                SwapGuitarAndBass(ref cPerfEntry_Guitar, ref cPerfEntry_Bass);

                CDTXMania.DTX.SwapGuitarBassInfos(); // 譜面情報も元に戻す
                CDTXMania.ConfigIni.SwapGuitarBassInfos_AutoFlags(); // #24415 2011.2.27 yyagi
                // リザルト集計時のみ、Auto系のフラグも元に戻す。
                // これを戻すのは、リザルト集計後。
            } // "case CStage.EStage.Result:"のところ。

            scoreIni = CDTXMania.UpdateBGMAdjustHistoryPlayCountIntScoreIni(
                ClearedHistoryLine(cPerfEntry_Drums, cPerfEntry_Guitar, cPerfEntry_Bass));
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
