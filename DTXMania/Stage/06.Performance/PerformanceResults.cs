using DTXMania.Core;

namespace DTXMania;

internal static class PerformanceResults
{
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
