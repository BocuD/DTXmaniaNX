using System.Globalization;
using DTXMania.Core;
using DTXMania.SongDb;
using DTXMania.UI.DynamicElements;

namespace DTXMania;

public sealed class PerformanceStats
{
    [DataField] public int Instrument { get; set; }

    [DataField] public EInstrument Player
    {
        get => (EInstrument)Instrument;
        set => Instrument = (int)value;
    }

    [DataField] public string InstrumentName => Instrument switch { 0 => "DRUM", 1 => "GUITAR", _ => "BASS" };

    //null outside a performance screen, eg in the editor
    private static CStagePerfCommonScreen? Stage => CDTXMania.StageManager?.rCurrentStage as CStagePerfCommonScreen;

    private HitCounts? Hits => Stage?.DataFor((EInstrumentPart)Instrument).HitCountExclAuto;

    [DataField] public int Perfect => Hits?.Perfect ?? 0;
    [DataField] public int Great => Hits?.Great ?? 0;
    [DataField] public int Good => Hits?.Good ?? 0;
    [DataField] public int Poor => Hits?.Poor ?? 0;
    [DataField] public int Miss => Hits?.Miss ?? 0;
    [DataField] public int MaxCombo => Stage?.DataFor((EInstrumentPart)Instrument).MaxCombo ?? 0;

    [DataField] public int PerfectPercent => Percent(Perfect);
    [DataField] public int GreatPercent => Percent(Great);
    [DataField] public int GoodPercent => Percent(Good);
    [DataField] public int PoorPercent => Percent(Poor);
    [DataField] public int MissPercent => Percent(Miss);
    [DataField] public int MaxComboPercent => Percent(MaxCombo);

    [DataField] public double Rate => Stage?.DataFor((EInstrumentPart)Instrument).AchievementRate ?? 0.0;
    [DataField] public long RateWhole => (long)Math.Truncate(Rate);
    [DataField] public long RateHundredths => Hundredths(Rate);

    [DataField] public double Skill => IsClassic
        ? Rate * (Level * 0.0033)
        : CScoreIni.tCalculateGameSkillFromPlayingSkill(Level, LevelDec, Rate);

    [DataField] public long SkillWhole => (long)Math.Truncate(Skill);
    [DataField] public long SkillHundredths => Hundredths(Skill);

    //6.50, or 65 for a classic chart
    [DataField] public string LevelText
    {
        get
        {
            RefreshSongValues();
            return levelText;
        }
    }

    //row in the difficulty sheet, 60px per badge
    [DataField] public float DifficultyBadgeY
    {
        get
        {
            RefreshSongValues();
            return difficultyBadgeY;
        }
    }

    [DataField] public int Early => Stage?.DataFor((EInstrumentPart)Instrument).TimingHitCount.Early ?? 0;
    [DataField] public int Late => Stage?.DataFor((EInstrumentPart)Instrument).TimingHitCount.Late ?? 0;

    //0 to 1, clamped
    [DataField] public double Gauge => Math.Max(0.0, Stage?.DataFor((EInstrumentPart)Instrument).Gauge ?? 0.0);
    [DataField] public bool IsDanger => Stage?.DataFor((EInstrumentPart)Instrument).IsDanger() ?? false;

    //row in the scroll speed sheet, 48px per speed
    [DataField] public float ScrollSpeedIconY => Math.Min(CDTXMania.ConfigIni.nScrollSpeed[Instrument], 20) * 48.0f;

    [DataField] public bool IsLaneDark => CDTXMania.ConfigIni.nLaneDisp[Instrument] is not (0 or 2);
    [DataField] public bool IsLeft => CDTXMania.ConfigIni.bLeft[Instrument];

    [DataField] public bool ShowsJudgeLine => CDTXMania.ConfigIni.bJudgeLineDisp[Instrument];
    [DataField] public int JudgeLine => CDTXMania.ConfigIni.nJudgeLine[Instrument];

    [DataField] public float JudgeLineY
    {
        get
        {
            int y = Stage?.nJudgeLinePosY[Instrument] ?? 0;

            if (Instrument != 0)
            {
                return IsReversed ? y : y - 1;
            }

            int offset = CDTXMania.ConfigIni.nJudgeLinePosOffset.Drums;
            return IsReversed ? y - offset : y + offset;
        }
    }

    //drums label the line from below and guitar and bass from above, until reversed
    [DataField] public float JudgeLineNumberY => JudgeLineY + ((Instrument == 0) != IsReversed ? 8 : -20);

    //the drums line spans only the lanes in use
    [DataField] public float JudgeLineX => Instrument != 0 ? 0 : CDTXMania.ConfigIni.eNumOfLanes.Drums switch
    {
        EType.C => 75,
        _ => 3
    };

    [DataField] public float JudgeLineWidth => Instrument != 0 ? 252 : CDTXMania.ConfigIni.eNumOfLanes.Drums switch
    {
        EType.B => 514,
        EType.C => 442,
        _ => 554
    };

    [DataField] public int TopShutter => IsReversed ? CDTXMania.ConfigIni.nShutterOutSide[Instrument] : CDTXMania.ConfigIni.nShutterInSide[Instrument];
    [DataField] public int BottomShutter => IsReversed ? CDTXMania.ConfigIni.nShutterInSide[Instrument] : CDTXMania.ConfigIni.nShutterOutSide[Instrument];

    [DataField] public float TopShutterEdge => MathF.Ceiling(Instrument == 0 ? TopShutter * 7.2f : 108 + TopShutter * 6.14f);
    [DataField] public float BottomShutterEdge => MathF.Floor(Instrument == 0 ? 720 - BottomShutter * 7.2f : 670 - BottomShutter * 6.14f);

    private bool IsReversed => CDTXMania.ConfigIni.bReverse[Instrument];

    [DataField] public bool ShowsPanel => CDTXMania.ConfigIni.nInfoType == 1 && IsPlayed && !HiddenBesideTheOther;

    [DataField] public bool HasLane => IsPlayed && Instrument switch
    {
        1 => CDTXMania.DTX?.bHasChips.Guitar ?? false,
        2 => CDTXMania.DTX?.bHasChips.Bass ?? false,
        _ => true
    };

    private bool IsPlayed => Instrument == 0 ? Stage is CStagePerfDrumsScreen : Stage is CStagePerfGuitarScreen;

    //cached per song
    private object? songValuesFor;
    private int songValuesInstrument = -1;
    private string levelText = string.Empty;
    private float difficultyBadgeY;

    private int Level => CDTXMania.DTX?.LEVEL[Instrument] ?? 0;
    private int LevelDec => CDTXMania.DTX?.LEVELDEC[Instrument] ?? 0;

    //classic skill mode, or a classic score display for a chart that has none of the XG parts
    private bool IsClassic
    {
        get
        {
            CConfigIni config = CDTXMania.ConfigIni;
            if (config.nSkillMode == 0)
            {
                return true;
            }

            var chart = CDTXMania.DTX;
            if (chart == null) return false;
            
            if (!config.bClassicScoreDisplay || CDTXMania.DTX.bForceXGChart)
            {
                return false;
            }
            
            return Instrument switch
            {
                1 => !chart.bHasChips.YPGuitar,
                2 => !chart.bHasChips.YPBass,
                _ => !chart.bHasChips.LeftCymbal && !chart.bHasChips.LP && !chart.bHasChips.LBD &&
                     !chart.bHasChips.FT && !chart.bHasChips.Ride
            };
        }
    }

    private void RefreshSongValues()
    {
        if (ReferenceEquals(songValuesFor, CDTXMania.DTX) && songValuesInstrument == Instrument)
        {
            return;
        }

        songValuesFor = CDTXMania.DTX;
        songValuesInstrument = Instrument;

        levelText = IsClassic
            ? Level.ToString("00", CultureInfo.InvariantCulture)
            : (Level / 10.0f + LevelDec / 100.0f).ToString("0.00", CultureInfo.InvariantCulture);

        int slot = CDTXMania.confirmedSongDifficulty;
        string? label = CDTXMania.chosenSong?.difficultyLabel[slot];
        difficultyBadgeY = Math.Max(0, DifficultyLabel.RowFor(DifficultyLabel.Resolve(label, slot))) * 60.0f;
    }

    //hidden for an instrument without chips, or, with the graph on, for the one on autoplay beside a played one
    private bool HiddenBesideTheOther
    {
        get
        {
            if (Instrument == 0 || CDTXMania.DTX is not { } chart)
            {
                return false;
            }

            CConfigIni config = CDTXMania.ConfigIni;
            bool isGuitar = Instrument == 1;

            if (!chart.bHasChips.Bass)
            {
                return !isGuitar;
            }

            if (!chart.bHasChips.Guitar)
            {
                return isGuitar;
            }

            if (!config.bGraph有効.Guitar && !config.bGraph有効.Bass)
            {
                return false;
            }

            return isGuitar
                ? config.bAllGuitarsAreAutoPlay && !config.bAllBassAreAutoPlay
                : !config.bAllGuitarsAreAutoPlay && config.bAllBassAreAutoPlay;
        }
    }

    //against every note judged so far, autoplay included
    private int Percent(int count)
    {
        if (Stage is not { } stage || stage.bIsTrainingMode)
        {
            return 0;
        }

        HitCounts all = stage.DataFor((EInstrumentPart)Instrument).HitCountIncAuto;
        int judged = all.Perfect + all.Great + all.Good + all.Poor + all.Miss;

        return judged == 0 ? 0 : (int)Math.Round(100.0 * count / judged);
    }

    private static long Hundredths(double value) => (long)((value - Math.Truncate(value)) * 100.0 + 1e-6);
}
