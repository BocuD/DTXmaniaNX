using System.Globalization;
using DTXMania.Core;
using DTXMania.SongDb;
using DTXMania.UI.DynamicElements;

namespace DTXMania;

public sealed class PerformanceStats
{
    [DataField] public EPlayer Player { get; set; }

    //the nameplate takes the player as a number
    [DataField] public int Instrument => (int)Player;

    private EInstrumentPart Part => (EInstrumentPart)CDTXMania.ConfigIni.ChartFor(Player);

    //this player's settings are stored in the drums, guitar and bass config slots
    private int Slot => (int)Player;

    [DataField] public string InstrumentName => Player switch
    {
        EPlayer.Drums => "DRUM",
        EPlayer.Guitar1 => "GUITAR",
        _ => "BASS"
    };

    //null outside a performance screen, eg in the editor
    private static CStagePerfCommonScreen? Stage => CDTXMania.StageManager?.rCurrentStage as CStagePerfCommonScreen;

    private HitCounts? Hits => Stage?.DataFor(Part).HitCountExclAuto;

    [DataField] public int Perfect => Hits?.Perfect ?? 0;
    [DataField] public int Great => Hits?.Great ?? 0;
    [DataField] public int Good => Hits?.Good ?? 0;
    [DataField] public int Poor => Hits?.Poor ?? 0;
    [DataField] public int Miss => Hits?.Miss ?? 0;
    [DataField] public int MaxCombo => Stage?.DataFor(Part).MaxCombo ?? 0;

    [DataField] public int PerfectPercent => Percent(Perfect);
    [DataField] public int GreatPercent => Percent(Great);
    [DataField] public int GoodPercent => Percent(Good);
    [DataField] public int PoorPercent => Percent(Poor);
    [DataField] public int MissPercent => Percent(Miss);
    [DataField] public int MaxComboPercent => Percent(MaxCombo);

    [DataField] public double Rate => Stage?.DataFor(Part).AchievementRate ?? 0.0;
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

    [DataField] public int Early => Stage?.DataFor(Part).TimingHitCount.Early ?? 0;
    [DataField] public int Late => Stage?.DataFor(Part).TimingHitCount.Late ?? 0;

    //0 to 1, clamped
    [DataField] public double Gauge => Math.Max(0.0, Stage?.DataFor(Part).Gauge ?? 0.0);
    [DataField] public bool IsDanger => Stage?.DataFor(Part).IsDanger() ?? false;

    //row in the scroll speed sheet, 48px per speed
    [DataField] public float ScrollSpeedIconY => Math.Min(CDTXMania.ConfigIni.nScrollSpeed[Slot], 20) * 48.0f;

    [DataField] public bool IsLaneDark => CDTXMania.ConfigIni.nLaneDisp[Slot] is not (0 or 2);
    [DataField] public bool IsLeft => CDTXMania.ConfigIni.bLeft[Slot];

    [DataField] public bool ShowsJudgeLine => CDTXMania.ConfigIni.bJudgeLineDisp[Slot];
    [DataField] public int JudgeLine => CDTXMania.ConfigIni.nJudgeLine[Slot];

    [DataField] public float JudgeLineY
    {
        get
        {
            int y = Stage?.nJudgeLinePosY[Slot] ?? 0;

            if (Player != EPlayer.Drums)
            {
                return IsReversed ? y : y - 1;
            }

            int offset = CDTXMania.ConfigIni.nJudgeLinePosOffset.Drums;
            return IsReversed ? y - offset : y + offset;
        }
    }

    //drums label the line from below and guitar and bass from above, until reversed
    [DataField] public float JudgeLineNumberY => JudgeLineY + ((Player == EPlayer.Drums) != IsReversed ? 8 : -20);

    public LaneLayout? lanes;

    //playfield positions, inside the outer lines of the lanes. Without lanes, eg: guitar, the fixed guitar line
    [DataField] public float JudgeLineX => lanes != null ? lanes.SpanLeft + 1 : 80;
    [DataField] public float JudgeLineWidth => lanes != null ? lanes.SpanWidth - 2 : 252;

    [DataField] public int TopShutter => IsReversed ? CDTXMania.ConfigIni.nShutterOutSide[Slot] : CDTXMania.ConfigIni.nShutterInSide[Slot];
    [DataField] public int BottomShutter => IsReversed ? CDTXMania.ConfigIni.nShutterInSide[Slot] : CDTXMania.ConfigIni.nShutterOutSide[Slot];

    [DataField] public float TopShutterEdge => MathF.Ceiling(Player == EPlayer.Drums ? TopShutter * 7.2f : 108 + TopShutter * 6.14f);
    [DataField] public float BottomShutterEdge => MathF.Floor(Player == EPlayer.Drums ? 720 - BottomShutter * 7.2f : 670 - BottomShutter * 6.14f);

    private bool IsReversed => CDTXMania.ConfigIni.bReverse[Slot];

    [DataField] public bool ShowsPanel => CDTXMania.ConfigIni.nInfoType == 1 && IsPlayed && !HiddenBesideTheOther;

    [DataField] public bool HasLane => IsPlayed && (Player == EPlayer.Drums || (CDTXMania.DTX?.bHasChips[(int)Part] ?? false));

    private bool IsPlayed => Player == EPlayer.Drums ? Stage is CStagePerfDrumsScreen : Stage is CStagePerfGuitarScreen;

    //cached per song
    private object? songValuesFor;
    private EPlayer? songValuesPlayer;
    private string levelText = string.Empty;
    private float difficultyBadgeY;

    private int Level => CDTXMania.DTX?.LEVEL[(int)Part] ?? 0;
    private int LevelDec => CDTXMania.DTX?.LEVELDEC[(int)Part] ?? 0;

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
            
            return Part switch
            {
                EInstrumentPart.GUITAR => !chart.bHasChips.YPGuitar,
                EInstrumentPart.BASS => !chart.bHasChips.YPBass,
                _ => !chart.bHasChips.LeftCymbal && !chart.bHasChips.LP && !chart.bHasChips.LBD &&
                     !chart.bHasChips.FT && !chart.bHasChips.Ride
            };
        }
    }

    private void RefreshSongValues()
    {
        if (ReferenceEquals(songValuesFor, CDTXMania.DTX) && songValuesPlayer == Player)
        {
            return;
        }

        songValuesFor = CDTXMania.DTX;
        songValuesPlayer = Player;

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
            CDTX? chart = CDTXMania.DTX;
            if (Player == EPlayer.Drums || chart == null)
            {
                return false;
            }

            CConfigIni config = CDTXMania.ConfigIni;
            bool isGuitar = Player == EPlayer.Guitar1;
            EPlayer other = isGuitar ? EPlayer.Guitar2 : EPlayer.Guitar1;

            //a player makes way for the other one when its own part has no chips
            if (!chart.bHasChips[(int)Part])
            {
                return true;
            }

            if (!chart.bHasChips[(int)CDTXMania.ConfigIni.ChartFor(other)])
            {
                return false;
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

        HitCounts all = stage.DataFor(Part).HitCountIncAuto;
        int judged = all.Perfect + all.Great + all.Good + all.Poor + all.Miss;

        return judged == 0 ? 0 : (int)Math.Round(100.0 * count / judged);
    }

    private static long Hundredths(double value) => (long)((value - Math.Truncate(value)) * 100.0 + 1e-6);
}
