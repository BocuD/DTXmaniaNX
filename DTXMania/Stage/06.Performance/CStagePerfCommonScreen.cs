using DiscordRPC;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Diagnostics;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.Core.Video;
using DTXMania.Drawable;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Skin;
using DTXMania.UI.Text;
using FDK;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;
using SlimDXKey = SlimDX.DirectInput.Key;

namespace DTXMania;

/// <summary>
/// 演奏画面の共通クラス (ドラム演奏画面, ギター演奏画面の継承元)
/// </summary>
internal abstract class CStagePerfCommonScreen : CStage
{
    protected override RichPresence Presence
    {
        get
        {
            // if presence is displayed before this point then it will use
            // the unitialised timer time, displaying an incorrect timestamp
            // so dont display any presence until initialisation has occurred
            if (bJustStartedUpdate || CDTXMania.bCompactMode)
                return null;
            
            var nEndTimeMs = CDTXMania.DTX.listChip.OrderBy(c => c.nPlaybackTimeMs).LastOrDefault()?.nPlaybackTimeMs ?? 0;

            //Shorten details string to avoid hitting max of 128 bytes
            string detailsString = $"{CDTXMania.chosenSong.title}";
            if (detailsString.Length > 50)
            {
                detailsString = detailsString.Substring(0, 50);
            }
            detailsString += $" [{CDTXMania.chosenSong.difficultyLabel[CDTXMania.confirmedSongDifficulty]}]";
            return new CDTXRichPresence
            {
                State = "In Game",

                // title [difficulty]
                // some songs omit the artist, so dont include the dash separator in such cases
                Details = detailsString,

                // playback speed is automatically applied as chip timings are modified,
                // but the current time must be accounted for in start/end to display correct timestamps when seeking around
                Timestamps = Timestamps.FromTimeSpan((nEndTimeMs - AudioMixer.Timer.nCurrentTime) / 1000.0),
            };
        }
    }

    // プロパティ

    public CStagePerfCommonScreen()
    {
    }

    protected WailingEffect[] wailingEffect;
    
    // メソッド

    //drums only on the drums screen; a part without chips gets an empty entry
    public void tStorePerfResults(out CScoreIni.CPerformanceEntry drums, out CScoreIni.CPerformanceEntry guitar, out CScoreIni.CPerformanceEntry bass, out bool trainingMode)
    {
        drums = CDTXMania.DTX.bHasChips.Drums && !CDTXMania.ConfigIni.bGuitarRevolutionMode ? ResultsFor(DrumsData) : new CScoreIni.CPerformanceEntry();
        guitar = CDTXMania.DTX.bHasChips.Guitar ? ResultsFor(GuitarData) : new CScoreIni.CPerformanceEntry();
        bass = CDTXMania.DTX.bHasChips.Bass ? ResultsFor(BassData) : new CScoreIni.CPerformanceEntry();
        trainingMode = bIsTrainingMode;
    }

    private CScoreIni.CPerformanceEntry ResultsFor(PerformanceData data) =>
        PerformanceResults.Create(data, actProgressBar.GetScoreIniString(data.Part));

    private UIText playspeedText;
    
    public bool bIsFinishedPlaying;
    
    public override void RegisterBindings()
    {
        UIDataContext context = new();
        context.RegisterObject("Video", () => video);
        context.RegisterTexture("Video.Frame", () => video.Player.CurrentFrame.IsValid ? video.Player.CurrentFrame.Texture : null);
        ui.dataContext = context;
    }

    protected const string PlayersGroup = "Players";
    protected const string HudGroup = "HUD";
    protected const string OverlaysGroup = "Overlays";

    //a skin layout without the group gets the part at the root
    protected UIGroup LayoutGroup(string name) => ui.GetChild<UIGroup>(name) ?? ui;

    internal const int LaneArtLayer = 1;
    internal const int LegacyLanesLayer = 2;
    internal const int LaneOverlayLayer = 3;
    internal const int LegacyOverlayLayer = 4;
    internal const int EffectsLayer = 5;
    internal const int ScoreLayer = 6;
    internal const int PanelLayer = 7;

    public override void BuildDefaultLayout()
    {
        UIGroup videos = ui.AddChild(new UIGroup("Video"));
        videos.size = new Vector2(1280, 720);
        videos.bindings.Add(new UIBinding("isVisible", "Video.IsPlaying"));

        //MovieMode: 0 off, 1 fullscreen, 2 windowed, 3 both
        UIGroup fullscreen = videos.AddChild(new UIGroup("Fullscreen"));
        fullscreen.bindings.Add(new UIBinding("isVisible", "Config.MovieMode")
        {
            condition = BindingCondition.Equal, value = 1, match = BindingMatch.Any,
            clauses = [new BindingClause { source = "Config.MovieMode", condition = BindingCondition.Equal, value = 3 }]
        });
        LinkedVideo(fullscreen, "Wide", 0, 0, new Vector2(1280, 720)).bindings.Add(new UIBinding("isVisible", "Video.IsWide"));

        UIGroup classic = Shown(fullscreen, "Classic", "Video.IsWide", invert: true);
        LinkedVideo(Shown(classic, "Drums", "Game.IsDrums"), "Video", 882, 0).scale = new Vector3(1.42f, 1.42f, 1);
        LinkedVideo(Shown(classic, "Guitar", "Game.IsDrums", invert: true), "Video", 640, 0).pivot = new Vector2(0.5f, 0);

        UIGroup windowed = videos.AddChild(new UIGroup("Windowed"));
        windowed.bindings.Add(new UIBinding("isVisible", "Config.MovieMode") { condition = BindingCondition.GreaterOrEqual, value = 2 });

        UIGroup drums = Shown(windowed, "Drums", "Game.IsDrums");
        Window(drums, "Beside Lanes", 854, 142, "7_ClipPanel.png", 5, 30, new Vector2(416, 234))
            .bindings.Add(new UIBinding("isVisible", "Config.Graph.Drums") { invert = true });
        Window(drums, "Below Graph", 2, 402, "7_ClipPanelB.png", 5, 20, new Vector2(260, 270))
            .bindings.Add(new UIBinding("isVisible", "Config.Graph.Drums"));

        UIGroup guitar = Shown(windowed, "Guitar", "Game.IsDrums", invert: true);
        UIGroup intoBassSlot = GraphShift(guitar, "Into Bass Graph", "Bass", 267);
        UIGroup intoGuitarSlot = GraphShift(intoBassSlot, "Into Guitar Graph", "Guitar", -267);
        Window(intoGuitarSlot, "Window", 380, 50, "7_ClipPanelC.png", 30, 5, new Vector2(460, 258));

        UIGroup hud = Group(HudGroup, 2);
        UIGroup overlays = Group(OverlaysGroup, 4);

        InfoBox info = hud.AddChild(new InfoBox());
        info.position = new Vector3(1277, 3, 0);
        info.renderOrder = LaneArtLayer;

        //with two guitar players it would cover the right player's lane
        info.bindings.Add(new UIBinding("isVisible", "Game.Playing.Guitar2") { invert = true });

        UIGroup players = Group(PlayersGroup, 3);
        Section(players, EInstrument.Drums, 0);
        Section(players, EInstrument.Guitar1, 0);
        Section(players, EInstrument.Guitar2, 870);

        StageFailedCover failed = overlays.AddChild(new StageFailedCover());
        failed.renderOrder = 1000;

        static void Section(UIGroup parent, EInstrument player, float x)
        {
            PlayerSection section = parent.AddChild(new PlayerSection(player));
            section.name = player.ToString();
            section.position = new Vector3(x, 0, 0);
            section.bindings.Add(new UIBinding("isVisible", $"Game.Playing.{player}"));
        }

        static UIImage LinkedVideo(UIGroup parent, string name, float x, float y, Vector2? box = null)
        {
            UIImage video = parent.AddChild(UINewVideoRenderer.CreateLinked());
            video.name = name;
            video.position = new Vector3(x, y, 0);

            if (box is { } size)
            {
                video.size = size;
            }

            return video;
        }

        static UIGroup Window(UIGroup parent, string name, float x, float y, string panel, float videoX, float videoY, Vector2 box)
        {
            UIGroup window = parent.AddChild(new UIGroup(name));
            window.position = new Vector3(x, y, 0);
            window.AddChild(new UIImage { name = "Panel", imageSource = ImageSource.File, image = SkinResource.System($@"Graphics\{panel}") });
            LinkedVideo(window, "Video", videoX, videoY, box);
            return window;
        }

        static UIGroup GraphShift(UIGroup parent, string name, string part, float x)
        {
            UIGroup group = parent.AddChild(new UIGroup(name));
            group.bindings.Add(new UIBinding("position.X", $"Config.Graph.{part}")
            {
                condition = BindingCondition.IsTrue, whenTrue = x, whenFalse = 0,
                clauses = [new BindingClause { source = $"Game.HasChips.{part}", condition = BindingCondition.IsFalse }]
            });
            return group;
        }

        static UIGroup Shown(UIGroup parent, string name, string source, bool invert = false)
        {
            UIGroup group = parent.AddChild(new UIGroup(name));
            group.bindings.Add(new UIBinding("isVisible", source) { invert = invert });
            return group;
        }

        UIGroup Group(string name, int renderOrder)
        {
            UIGroup group = ui.AddChild(new UIGroup(name));
            group.size = new Vector2(1280, 720);
            group.renderOrder = renderOrder;
            return group;
        }
    }

    //the performance screens are built entirely in code: bespoke chip/lane rendering, not a skinnable layout
    public override void OnLayoutReady()
    {
        actStageFailed.cover = ui.FindChild<StageFailedCover>();

        if (CDTXMania.ConfigIni.nShowPlaySpeed == (int)EShowPlaySpeed.ON
            || CDTXMania.ConfigIni.nShowPlaySpeed == (int)EShowPlaySpeed.IF_CHANGED_IN_GAME)
        {
            //the reference isn't cleared automatically when the stage is unloaded
            playspeedText = null;
            tUpdatePlayspeedText();
        }
    }

    // CStage 実装

    // Remembers the GC latency mode in effect before the song started so it can be restored.
    private System.Runtime.GCLatencyMode _previousGcLatencyMode;

    public override void OnActivate()
    {
        // Rhythm games are latency-sensitive: ask the runtime to avoid disruptive
        // blocking gen2 collections while a song is playing. With the video frame
        // buffers now pooled, allocation pressure is low enough that the GC can
        // comfortably defer full collections until OnDeactivate.
        _previousGcLatencyMode = System.Runtime.GCSettings.LatencyMode;
        System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.SustainedLowLatency;

        timeline.Load(CDTXMania.DTX.listChip);
        listWAV = CDTXMania.DTX.listWAV;

        eReturnValue = EPerfScreenReturnValue.Continue;
        sequencer = new ChartSequencer();
        nJudgeLinePosY.Drums = (CDTXMania.ConfigIni.bReverse.Drums ? 159 + CDTXMania.ConfigIni.nJudgeLine.Drums : 561 - CDTXMania.ConfigIni.nJudgeLine.Drums);
        nJudgeLinePosY.Guitar = (CDTXMania.ConfigIni.bReverse.Guitar ? 611 - CDTXMania.ConfigIni.nJudgeLine.Guitar : 154 + CDTXMania.ConfigIni.nJudgeLine.Guitar);
        nJudgeLinePosY.Bass = (CDTXMania.ConfigIni.bReverse.Bass ? 611 - CDTXMania.ConfigIni.nJudgeLine.Bass : 154 + CDTXMania.ConfigIni.nJudgeLine.Bass);

        nShutterInPosY.Drums = CDTXMania.ConfigIni.nShutterInSide.Drums;
        nShutterOutPosY.Drums = CDTXMania.ConfigIni.nShutterOutSide.Drums;
        nShutterInPosY.Guitar = CDTXMania.ConfigIni.nShutterInSide.Guitar;
        nShutterOutPosY.Guitar = CDTXMania.ConfigIni.nShutterOutSide.Guitar;
        nShutterInPosY.Bass = CDTXMania.ConfigIni.nShutterInSide.Bass;
        nShutterOutPosY.Bass = CDTXMania.ConfigIni.nShutterOutSide.Bass;

        nJudgeLinePosY_delta.Drums = CDTXMania.ConfigIni.nJudgeLinePosOffset.Drums;    // #31602 2013.6.23 yyagi
        nJudgeLinePosY_delta.Guitar = CDTXMania.ConfigIni.nJudgeLinePosOffset.Guitar;  //
        nJudgeLinePosY_delta.Bass = CDTXMania.ConfigIni.nJudgeLinePosOffset.Bass;      //
        bIsAutoPlay = CDTXMania.ConfigIni.bAutoPlay;									// #24239 2011.1.23 yyagi
        foreach (PerformanceData data in partData)
        {
            data.Reset(bIsAutoPlay, CDTXMania.ConfigIni.nRisky);
        }
        drums = IsPlayed(EInstrumentPart.DRUMS) ? new DrumsPerformance(DrumsData, timeline, bIsAutoPlay) : null;
        guitars[0] = IsPlayed(EInstrumentPart.GUITAR) ? new GuitarPerformance(EInstrument.Guitar1, EChartPart.Guitar, GuitarData, timeline, bIsAutoPlay) : null;
        guitars[1] = IsPlayed(EInstrumentPart.BASS) ? new GuitarPerformance(EInstrument.Guitar2, EChartPart.Bass, BassData, timeline, bIsAutoPlay) : null;


        //let the loading sound gracefully fade out
        CDTXMania.Skin.tRemoveMixerAll(CDTXMania.Skin.soundNowLoading);

        bNeedsDriftCorrection = AudioMixer.Device.NeedsDriftCorrection;
        dbPlaySpeed = CDTXMania.ConfigIni.nPlaySpeed / 20.0;
        bValidScore = true;

        LoopBeginMs = -1;
        LoopEndMs = -1;
        //do not write a score for a chart played in the editor
        bIsTrainingMode = previewMode;
        bPAUSE = false;

        #region [ Sounds that should be registered in the mixer before starting playing (chip sounds that will be played immediately after the start of the performance) ]
        foreach (CChip pChip in timeline.Chips)
        {
            if (pChip.nPlaybackTimeMs <= 0)
            {
                if (pChip.nChannelNumber == EChannel.MixChannel1_unc)
                {
                    pChip.bHit = true;
                    if (listWAV.ContainsKey(pChip.nIntegerValue_InternalNumber))
                    {
                        CDTX.CWAV wc = listWAV[pChip.nIntegerValue_InternalNumber];
                        if (wc.clip != null)
                        {
                            AudioMixer.AttachToMixer(wc.clip);
                        }
                    }
                }
            }
            else
            {
                break;
            }
        }
        #endregion
        if (CDTXMania.ConfigIni.bIsSwappedGuitarBass)	// #24063 2011.1.24 yyagi Gt/Bsの譜面情報入れ替え
        {
            CDTXMania.DTX.SwapGuitarBassInfos();
        }
        base.OnActivate();
    }
    public override void OnDeactivate()
    {
        // Restore the previous GC latency mode so menus and results can reclaim
        // memory freely again.
        System.Runtime.GCSettings.LatencyMode = _previousGcLatencyMode;

        drums = null;
        Array.Clear(guitars);
        ctWailingChipPatternAnimation = null;
        ctBPMBar = null;
        ctChipPatternAnimation.Drums = null;
        ctChipPatternAnimation.Guitar = null;
        ctChipPatternAnimation.Bass = null;
        listWAV = null;
        timeline.Load(null);
        
        base.OnDeactivate();
    }
    public override void OnManagedCreateResources()
    {
        if (bActivated)
        {
            base.OnManagedCreateResources();

            tGenerateBackgroundTexture();

            txWailingFrame = BaseTexture.LoadFromPath( CSkin.Path( @"Graphics\ScreenPlay wailing cursor.png" ) );
            txBonusEffect = BaseTexture.LoadFromPath( CSkin.Path( @"Graphics\7_Fillin Effect.png" ) );
        }
    }

    // Other

    #region [ protected ]
    //-----------------
    static CStagePerfCommonScreen()
    {
        nJudgeLineMinPosY = 461;//(CDTXMania.ConfigIni.bReverse.Drums ? 259 : 461);
        nJudgeLineMaxPosY = 561;//(CDTXMania.ConfigIni.bReverse.Drums ? 159 : 561);
        nShutterMinPosY = 0;
        nShutterMaxPosY = 100;
    }

    //todo: reintroduce background video and art
    public CActPerfVideo video;
    public CActPerfBGA actBGA;
    

    //one buffer for every pad read in a frame; the reads are sequential and each is done with before the
    //next one starts
    protected readonly List<STInputEvent> listPadEvents = [];
    protected ActPerfNewFire[] actChipFireGB;
    public CActPerfCommonCombo actCombo;
    protected CActPerfCommonJudgementString actJudgeString;
    protected CActPerfDrumsLaneFlushD actLaneFlushD;
    protected CActPerfGuitarLaneFlushGB actLaneFlushGB;
    protected CActPerformanceInformation actPlayInfo;
    public CActPerfCommonScore actScore;
    protected CActPerfStageFailure actStageFailed;
    protected CActPerfGuitarWailingBonus actWailingBonus;
    public CActPerfScrollSpeed actScrollSpeed;
    protected CActPerfSkillMeter actGraph;
    protected CActPerfGuitarBonus actGuitarBonus;
    protected CActPerfProgressBar actProgressBar;
    protected bool bPAUSE;
    protected CCounter ctWailingChipPatternAnimation;
    public CCounter ctBPMBar;
    public CCounter ct登場用;
    public CCounter ctComboTimer;

    protected STDGBVALUE<CCounter> ctChipPatternAnimation;
    protected abstract void tJudgeLineMovingUpandDown();
    protected EPerfScreenReturnValue eReturnValue;
    protected CCounter ctResultDelay;
    protected readonly EChannel[,] nBGAスコープチャンネルマップ = new[,] { { EChannel.BGALayer1_Swap, EChannel.BGALayer2_Swap, EChannel.BGALayer3_Swap, EChannel.BGALayer4_Swap, EChannel.BGALayer5_Swap, EChannel.BGALayer6_Swap, EChannel.BGALayer7_Swap, EChannel.BGALayer8_Swap }, { EChannel.BGALayer1, EChannel.BGALayer2, EChannel.BGALayer3, EChannel.BGALayer4, EChannel.BGALayer5, EChannel.BGALayer6, EChannel.BGALayer7, EChannel.BGALayer8 } };

    public STDGBVALUE<int> nJudgeLinePosY;//(CDTXMania.ConfigIni.bReverse.Drums ? 159 : 561);
    public STDGBVALUE<int> nShutterInPosY;
    public STDGBVALUE<int> nShutterOutPosY;
    //indexed by EInstrumentPart: drums, guitar, bass
    protected readonly PerformanceData[] partData =
        [new(EInstrumentPart.DRUMS), new(EInstrumentPart.GUITAR), new(EInstrumentPart.BASS)];

    public PerformanceData DataFor(EInstrumentPart part) => partData[(int)part];

    //by EInstrument - 1, null when not playing
    protected readonly GuitarPerformance?[] guitars = new GuitarPerformance?[2];

    //for now Guitar1 always plays the guitar chart and Guitar2 the bass chart
    protected DrumsPerformance? drums;

    public PlayerFlags Playing => new()
    {
        Drums = drums != null,
        Guitar1 = guitars[0] != null,
        Guitar2 = guitars[1] != null
    };

    protected GuitarPerformance? GuitarFor(EInstrumentPart part) =>
        part == EInstrumentPart.GUITAR ? guitars[0] : part == EInstrumentPart.BASS ? guitars[1] : null;
    protected PerformanceData DrumsData => partData[(int)EInstrumentPart.DRUMS];
    protected PerformanceData GuitarData => partData[(int)EInstrumentPart.GUITAR];
    protected PerformanceData BassData => partData[(int)EInstrumentPart.BASS];

    protected static int nJudgeLineMaxPosY;
    protected static int nJudgeLineMinPosY;
    protected static int nShutterMaxPosY;
    protected static int nShutterMinPosY;

    protected bool bNeedsDriftCorrection;
    protected double dbPlaySpeed;
    protected bool bValidScore;
    protected STDGBVALUE<int> nJudgeLinePosY_delta; // #31602 2013.6.23 yyagi 表示遅延対策として、判定ラインの表示位置をずらす機能を追加する

    protected ChartSequencer sequencer = new();
    protected BaseTexture txWailingFrame;

    public BaseTexture txBonusEffect;

    //fork

    public STAUTOPLAY bIsAutoPlay;		// #24239 2011.1.23 yyagi
    //		protected int nRisky_InitialVar, nRiskyTime;		// #23559 2011.7.28 yyagi → CAct演奏ゲージ共通クラスに隠蔽

    protected readonly ChipTimeline timeline = new();
    protected Dictionary<int, CDTX.CWAV> listWAV;

    private readonly Stopwatch laneKeyRepeat = new();
    private long laneKeyRepeatDelayMs;

    protected long LoopBeginMs;
    protected long LoopEndMs;
    
    // Use a property instead of a field to automatically set training mode on the graph too
    private bool _bIsTrainingMode;
    public bool bIsTrainingMode
    {
        get => _bIsTrainingMode;
        set
        {
            _bIsTrainingMode = value;
            if (actGraph != null)
            {
                actGraph.bIsTrainingMode = value;
            }

            //training has no achievement rate
            if (value)
            {
                foreach (PerformanceData data in partData)
                {
                    data.SetAchievementRate(0);
                }
            }
        }
    }




    protected EJudgement e指定時刻からChipのJUDGEを返す( long nTime, CChip pChip, int nInputAdjustTime, bool saveLag = true )
    {
        if (pChip == null)
            return EJudgement.Miss;

        int lag = (int)(nTime + nInputAdjustTime - pChip.nPlaybackTimeMs);
        if (saveLag)
        {
            pChip.nLag = lag;
            if (pChip.eInstrumentPart != EInstrumentPart.UNKNOWN)
            {
                pChip.nCurrentComboForGhost = partData[(int)pChip.eInstrumentPart].Combo;
            }
        }
        return ChipJudge.GetJudgementForLag(pChip, lag);
    }

    protected abstract void tHandleInput_Drums();

    protected void tHandleKeyInput()
    {
        IInputKeyboard keyboard = CDTXMania.InputManager.Keyboard;
        if (CDTXMania.Pad.bPressed(EInstrumentPart.BASS, EPad.Help))
        {
            TogglePause();
        }

        if (ePhaseID is EPhase.PERFORMANCE_STAGE_FAILED or EPhase.PERFORMANCE_STAGE_FAILED_END)
        {
            return;
        }

        if (!bPAUSE)
        {
            tHandleInput_Drums();
            tHandleInput_GuitarBass(EInstrumentPart.GUITAR);
            tHandleInput_GuitarBass(EInstrumentPart.BASS);
        }

        HandleArrowKeys(keyboard);
        HandleSystemKeys();
        HandleDrumsLaneKeys(keyboard);
        HandleDisplayKeys(keyboard);

        if (!bPAUSE && !StageManager.dropStageChanges && ePhaseID == EPhase.Common_DefaultState && keyboard.bKeyPressed(SlimDXKey.Escape))
        {
            Leave();
        }
    }

    //up, down: scroll speed. With shift: BGM timing
    //left, right: input adjust
    private void HandleArrowKeys(IInputKeyboard keyboard)
    {
        bool shift = keyboard.bKeyPressing(SlimDXKey.LeftShift) || keyboard.bKeyPressing(SlimDXKey.RightShift);
        bool ctrl = keyboard.bKeyPressing(SlimDXKey.LeftControl) || keyboard.bKeyPressing(SlimDXKey.RightControl);

        int vertical = keyboard.bKeyPressed(SlimDXKey.UpArrow) ? 1 : keyboard.bKeyPressed(SlimDXKey.DownArrow) ? -1 : 0;
        if (vertical != 0 && shift && !bPAUSE)
        {
            AdjustBgmTiming(vertical * (ctrl ? 1 : 10));
        }
        else if (vertical != 0 && !shift)
        {
            ChangeScrollSpeed(CDTXMania.ConfigIni.bGuitarRevolutionMode ? EInstrumentPart.GUITAR : EInstrumentPart.DRUMS, vertical);
        }

        int horizontal = keyboard.bKeyPressed(SlimDXKey.RightArrow) ? 1 : keyboard.bKeyPressed(SlimDXKey.LeftArrow) ? -1 : 0;
        if (horizontal != 0 && !bPAUSE)
        {
            ChangeInputAdjust(keyboard, horizontal);
        }
    }

    private void HandleSystemKeys()
    {
        if (SystemKey(EKeyConfigPad.Restart))
        {
            Restart();
        }
        if (SystemKey(EKeyConfigPad.SkipForward))
        {
            SkipBy(CDTXMania.ConfigIni.nSkipTimeMs);
        }
        if (SystemKey(EKeyConfigPad.SkipBackward))
        {
            SkipBy(-CDTXMania.ConfigIni.nSkipTimeMs);
        }
        if (SystemKey(EKeyConfigPad.LoopCreate))
        {
            MarkLoop();
        }
        if (SystemKey(EKeyConfigPad.LoopDelete))
        {
            Trace.TraceInformation("REMOVE LOOP AudioMixer.Timer.nCurrentTime=" + AudioMixer.Timer.nCurrentTime);
            LoopBeginMs = -1;
            LoopEndMs = -1;
        }
        if (SystemKey(EKeyConfigPad.DecreasePlaySpeed))
        {
            StepPlaySpeed(-1);
        }
        if (SystemKey(EKeyConfigPad.IncreasePlaySpeed))
        {
            StepPlaySpeed(1);
        }
    }

    private static bool SystemKey(EKeyConfigPad pad) => CDTXMania.Pad.bPressed(EKeyConfigPart.SYSTEM, pad);

    private void HandleDisplayKeys(IInputKeyboard keyboard)
    {
        if (CDTXMania.Pad.bPressed(EInstrumentPart.GUITAR, EPad.Help))
        {
            CDTXMania.ConfigIni.bShowPerformanceInformation = !CDTXMania.ConfigIni.bShowPerformanceInformation;
        }
        if (keyboard.bKeyPressed(SlimDXKey.F5))
        {
            video.CycleMovieMode();
        }
        if (keyboard.bKeyPressed(SlimDXKey.F6))
        {
            CDTXMania.ConfigIni.nInfoType = (CDTXMania.ConfigIni.nInfoType + 1) % 2;
        }
    }

    private void Leave()
    {
        GitaDoraTransition.Close();
        ePhaseID = EPhase.Common_FadeOut;
        eReturnValue = EPerfScreenReturnValue.Interruption;
    }

    private void TogglePause()
    {
        bPAUSE = !bPAUSE;
        if (bPAUSE)
        {
            AudioMixer.Timer.tPause();
            CDTXMania.Timer.tPause();
            CDTXMania.DTX.tPausePlaybackForAllChips();
        }
        else
        {
            AudioMixer.Timer.tResume();
            CDTXMania.Timer.tResume();
            CDTXMania.DTX.tResumePlaybackForAllChips();
        }
    }

    private static void AdjustBgmTiming(int deltaMs)
    {
        CDTXMania.DTX.t各自動再生音チップの再生時刻を変更する(deltaMs);
        CDTXMania.DTX.tAutoCorrectWavPlaybackPosition();
    }

    protected static void ChangeScrollSpeed(EInstrumentPart part, int delta)
    {
        int index = (int)part;
        CDTXMania.ConfigIni.nScrollSpeed[index] = Math.Clamp(CDTXMania.ConfigIni.nScrollSpeed[index] + delta, 0, 1999);
    }

    //default is drums, with shift: guitar, with alt: bass
    //default is 10ms steps, adding ctrl changes this to 1ms
    private static void ChangeInputAdjust(IInputKeyboard keyboard, int direction)
    {
        EInstrumentPart part = EInstrumentPart.DRUMS;
        if (keyboard.bKeyPressing(SlimDXKey.LeftShift) || keyboard.bKeyPressing(SlimDXKey.RightShift))
        {
            part = EInstrumentPart.GUITAR;
        }
        else if (keyboard.bKeyPressing(SlimDXKey.LeftAlt) || keyboard.bKeyPressing(SlimDXKey.RightAlt))
        {
            part = EInstrumentPart.BASS;
        }

        bool fine = keyboard.bKeyPressing(SlimDXKey.LeftControl) || keyboard.bKeyPressing(SlimDXKey.RightControl);
        int index = (int)part;
        CDTXMania.ConfigIni.nInputAdjustTimeMs[index] = Math.Clamp(CDTXMania.ConfigIni.nInputAdjustTimeMs[index] + direction * (fine ? 1 : 10), -99, 99);
    }

    //a held stage seeks to the start and clears the score. Otherwise the song reloads through the loading screen
    public void Restart()
    {
        if (StageManager.dropStageChanges)
        {
            tJumpInSong(0);
            foreach (PerformanceData data in partData)
            {
                data.Reset(bIsAutoPlay, CDTXMania.ConfigIni.nRisky);
            }
            return;
        }

        if (bPAUSE)
        {
            AudioMixer.Timer.tResume();
            CDTXMania.Timer.tResume();
        }

        ePhaseID = EPhase.PERFORMANCE_STAGE_RESTART;
        eReturnValue = EPerfScreenReturnValue.Restart;
    }

    private void SkipBy(long deltaMs)
    {
        bIsTrainingMode = true;
        Trace.TraceInformation($"SKIP {deltaMs} AudioMixer.Timer.nCurrentTime={AudioMixer.Timer.nCurrentTime}");
        tJumpInSong(Math.Max(0, AudioMixer.Timer.nCurrentTime + deltaMs));
    }

    //first press: loop start. Second press: loop end
    private void MarkLoop()
    {
        bIsTrainingMode = true;
        long now = AudioMixer.Timer.nCurrentTime;
        if (LoopBeginMs == -1)
        {
            Trace.TraceInformation("INSERT LOOP BEGIN AudioMixer.Timer.nCurrentTime=" + now);
            LoopBeginMs = now;
        }
        else if (LoopEndMs == -1)
        {
            Trace.TraceInformation("INSERT LOOP END AudioMixer.Timer.nCurrentTime=" + now);
            if (LoopBeginMs < now)
            {
                LoopEndMs = now;
            }
            else
            {
                LoopEndMs = LoopBeginMs;
                LoopBeginMs = now;
            }
        }
    }

    private void StepPlaySpeed(int delta)
    {
        int speed = CDTXMania.ConfigIni.nPlaySpeed + delta;
        if (speed < CConstants.PLAYSPEED_MIN || speed > CConstants.PLAYSPEED_MAX)
        {
            return;
        }

        bIsTrainingMode = true;
        tChangePlaySpeed(delta);
    }

    //page up, down: drums judge line
    //number pad 8, 2, 4, 6: lane cover shutters
    private void HandleDrumsLaneKeys(IInputKeyboard keyboard)
    {
        bool judgeUp = !CDTXMania.ConfigIni.bReverse.Drums && keyboard.bKeyPressing(SlimDXKey.PageUp);
        bool judgeDown = !CDTXMania.ConfigIni.bReverse.Drums && keyboard.bKeyPressing(SlimDXKey.PageDown);
        bool shutterInUp = keyboard.bKeyPressing(SlimDXKey.NumberPad8);
        bool shutterInDown = keyboard.bKeyPressing(SlimDXKey.NumberPad2);
        bool shutterOutDown = keyboard.bKeyPressing(SlimDXKey.NumberPad4);
        bool shutterOutUp = keyboard.bKeyPressing(SlimDXKey.NumberPad6);
        if (!judgeUp && !judgeDown && !shutterInUp && !shutterInDown && !shutterOutDown && !shutterOutUp)
        {
            laneKeyRepeat.Reset();
            return;
        }

        bool step = StepLaneKeyRepeat();
        bool shutterRoom = nShutterInPosY.Drums + nShutterOutPosY.Drums <= 99;
        if (judgeUp || judgeDown)
        {
            if (step)
            {
                nJudgeLinePosY.Drums = Math.Clamp(nJudgeLinePosY.Drums + (judgeUp ? -1 : 1), nJudgeLineMinPosY, nJudgeLineMaxPosY);
            }
            CDTXMania.ConfigIni.nJudgeLine.Drums = nJudgeLineMaxPosY - nJudgeLinePosY.Drums;
            tJudgeLineMovingUpandDown();
        }
        if (shutterInUp && step && nShutterInPosY.Drums > 0)
        {
            nShutterInPosY.Drums--;
        }
        if (shutterInDown && step && nShutterInPosY.Drums < 100 && shutterRoom)
        {
            nShutterInPosY.Drums++;
        }
        if (shutterOutDown && step && nShutterOutPosY.Drums < 100 && shutterRoom)
        {
            nShutterOutPosY.Drums++;
        }
        if (shutterOutUp && step && nShutterOutPosY.Drums > 0)
        {
            nShutterOutPosY.Drums--;
        }
        CDTXMania.ConfigIni.nShutterInSide.Drums = nShutterInPosY.Drums;
        CDTXMania.ConfigIni.nShutterOutSide.Drums = nShutterOutPosY.Drums;
    }

    //one step on press. After 100ms, one step every 10ms
    private bool StepLaneKeyRepeat()
    {
        if (!laneKeyRepeat.IsRunning)
        {
            laneKeyRepeat.Restart();
            laneKeyRepeatDelayMs = 100;
            return true;
        }
        if (laneKeyRepeat.ElapsedMilliseconds < laneKeyRepeatDelayMs)
        {
            return false;
        }

        laneKeyRepeat.Restart();
        laneKeyRepeatDelayMs = 10;
        return true;
    }

    private bool ShowsVideo => ePhaseID is not (EPhase.PERFORMANCE_STAGE_FAILED or EPhase.PERFORMANCE_STAGE_FAILED_END)
                               && !CDTXMania.ConfigIni.bストイックモード;

    protected void UpdateVideo()
    {
        if (ShowsVideo)
        {
            video.Update();
        }
    }

    protected void DrawFillInEffect()
    {
        if (ShowsVideo)
        {
            video.DrawFillInEffect();
        }
    }
    protected void UpdateStageFailed()
    {
        if (((ePhaseID == EPhase.PERFORMANCE_STAGE_FAILED) || (ePhaseID == EPhase.PERFORMANCE_STAGE_FAILED_END)) && ((actStageFailed.OnUpdateAndDraw() != 0) && (ePhaseID != EPhase.PERFORMANCE_STAGE_FAILED_END)))
        {
            eReturnValue = EPerfScreenReturnValue.StageFailure;
            ePhaseID = EPhase.PERFORMANCE_STAGE_FAILED_END;
            CDTXMania.DTX.tStopPlayingAllChips();
        }
    }
    protected void tUpdateAndDraw_WailingBonus(Matrix4x4 origin, EInstrumentPart part)
    {
        if ((ePhaseID != EPhase.PERFORMANCE_STAGE_FAILED) && (ePhaseID != EPhase.PERFORMANCE_STAGE_FAILED_END))
        {
            actWailingBonus.Draw(origin, part);
        }
    }

    protected void tUpdateAndDraw_GuitarBonus(Matrix4x4 origin, EInstrumentPart part)
    {
        if ((ePhaseID != EPhase.PERFORMANCE_STAGE_FAILED) && (ePhaseID != EPhase.PERFORMANCE_STAGE_FAILED_END))
        {
            actGuitarBonus.Draw(origin, part);
        }
    }
    protected abstract void tUpdateAndDraw_WailingFrame();
    protected void tUpdateAndDraw_WailingFrame(int GtWailingFrameX, int BsWailingFrameX, int GtWailingFrameY, int BsWailingFrameY)
    {
        if (txWailingFrame != null && CDTXMania.ConfigIni.bGuitarEnabled)
        {
            if (CDTXMania.DTX.bHasChips.Guitar)
            {
                txWailingFrame.tDraw2D(GtWailingFrameX, GtWailingFrameY);
            }
            if (CDTXMania.DTX.bHasChips.Bass)
            {
                txWailingFrame.tDraw2D(BsWailingFrameX, BsWailingFrameY);
            }
        }
    }
    

    protected void DrawCombo(Matrix4x4 origin, int part)
    {
        actCombo.Draw(origin, part);
    }
    public static bool IsPlayed(EInstrumentPart part)
    {
        bool guitarScreen = CDTXMania.ConfigIni.bGuitarRevolutionMode;
        return part switch
        {
            EInstrumentPart.DRUMS => !guitarScreen,
            EInstrumentPart.GUITAR => guitarScreen,
            EInstrumentPart.BASS => guitarScreen && !CDTXMania.ConfigIni.bSingleGuitar,
            _ => false
        };
    }

    protected bool UpdateChips()
    {
        if ((ePhaseID == EPhase.PERFORMANCE_STAGE_FAILED) || (ePhaseID == EPhase.PERFORMANCE_STAGE_FAILED_END))
        {
            return true;
        }
        if ((timeline.TopChip == -1) || (timeline.TopChip >= timeline.Chips.Count))
        {
            return true;
        }

        CDTX dTX = CDTXMania.DTX;
        CConfigIni configIni = CDTXMania.ConfigIni;
        for (int chipIndex = timeline.TopChip; chipIndex < dTX.listChip.Count; chipIndex++)
        {
            CChip pChip = dTX.listChip[chipIndex];
            pChip.ComputeDistanceFromBar(AudioMixer.Timer.nCurrentTime, actScrollSpeed.db現在の譜面スクロール速度);
            if (Math.Min(Math.Min(pChip.nDistanceFromBar.Drums, pChip.nDistanceFromBar.Guitar), pChip.nDistanceFromBar.Bass) > 600)
            {
                break;
            }
            if (chipIndex == timeline.TopChip && timeline.TryAdvance())
            {
                continue;
            }

            if (pChip.eInstrumentPart != EInstrumentPart.UNKNOWN && !IsPlayed(pChip.eInstrumentPart))
            {
                sequencer.UpdateUnplayedNote(pChip);
                continue;
            }

            int instIndex = (int) pChip.eInstrumentPart;

            // #35411 chnmr0 add (ターゲットゴースト)
            if ( CDTXMania.ConfigIni.eTargetGhost[instIndex] != ETargetGhostData.NONE &&
                 CDTXMania.listTargetGhsotLag[instIndex] != null &&
                 pChip.eInstrumentPart != EInstrumentPart.UNKNOWN &&
                 pChip.nDistanceFromBar[instIndex] < 0 )
            {
                if ( !pChip.bTargetGhost判定済み )
                {
                    pChip.bTargetGhost判定済み = true;

                    int ghostLag = 128;
                    if ( 0 <= pChip.n楽器パートでの出現順 && pChip.n楽器パートでの出現順 < CDTXMania.listTargetGhsotLag[instIndex].Count )
                    {
                        ghostLag = CDTXMania.listTargetGhsotLag[instIndex][pChip.n楽器パートでの出現順];
                        // 上位８ビットが１ならコンボが途切れている（ギターBAD空打ちでコンボ数を再現するための措置）
                        if ( ghostLag > 255 )
                        {
                            DataFor((EInstrumentPart)instIndex).BreakGhostCombo();
                        }
                        ghostLag = (ghostLag & 255) - 128;
                    }
                    else if ( CDTXMania.ConfigIni.eTargetGhost[instIndex] == ETargetGhostData.PERFECT )
                    {
                        ghostLag = 0;
                    }
                        
                    if ( ghostLag <= 127 )
                    {
                        EJudgement eJudge = e指定時刻からChipのJUDGEを返す(pChip.nPlaybackTimeMs + ghostLag, pChip, 0, false);
                        DataFor((EInstrumentPart)instIndex).CountGhost(eJudge);
                    }
                }
            }

            switch (pChip.nChannelNumber)
            {
                case EChannel.HiHatClose:
                case EChannel.Snare:
                case EChannel.BassDrum:
                case EChannel.HighTom:
                case EChannel.LowTom:
                case EChannel.Cymbal:
                case EChannel.FloorTom:
                case EChannel.HiHatOpen:
                case EChannel.RideCymbal:
                case EChannel.LeftCymbal:
                case EChannel.LeftPedal:
                case EChannel.LeftBassDrum:
                    UpdateChip_Drums(configIni, ref dTX, ref pChip);
                    break;

                case EChannel.Guitar_Open:
                case EChannel.Guitar_xxBxx:
                case EChannel.Guitar_xGxxx:
                case EChannel.Guitar_xGBxx:
                case EChannel.Guitar_Rxxxx:
                case EChannel.Guitar_RxBxx:
                case EChannel.Guitar_RGxxx:
                case EChannel.Guitar_RGBxx:
                case EChannel.Guitar_xxxYx:
                case EChannel.Guitar_xxBYx:
                case EChannel.Guitar_xGxYx:
                case EChannel.Guitar_xGBYx:
                case EChannel.Guitar_RxxYx:
                case EChannel.Guitar_RxBYx:
                case EChannel.Guitar_RGxYx:
                case EChannel.Guitar_RGBYx:
                case EChannel.Guitar_xxxxP:
                case EChannel.Guitar_xxBxP:
                case EChannel.Guitar_xGxxP:
                case EChannel.Guitar_xGBxP:
                case EChannel.Guitar_RxxxP:
                case EChannel.Guitar_RxBxP:
                case EChannel.Guitar_RGxxP:
                case EChannel.Guitar_RGBxP:
                case EChannel.Guitar_xxxYP:
                case EChannel.Guitar_xxBYP:
                case EChannel.Guitar_xGxYP:
                case EChannel.Guitar_xGBYP:
                case EChannel.Guitar_RxxYP:
                case EChannel.Guitar_RxBYP:
                case EChannel.Guitar_RGxYP:
                case EChannel.Guitar_RGBYP:
                    UpdateChip_GuitarBass(configIni, ref dTX, ref pChip, EInstrumentPart.GUITAR);
                    break;

                case EChannel.Guitar_Wailing:
                    UpdateChip_Guitar_Wailing(configIni, ref dTX, ref pChip);
                    break;

                case EChannel.FillIn:
                    UpdateChip_FillIn(configIni, ref dTX, ref pChip);
                    break;

                case EChannel.Bass_Open:
                case EChannel.Bass_xxBxx:
                case EChannel.Bass_xGxxx:
                case EChannel.Bass_xGBxx:
                case EChannel.Bass_Rxxxx:
                case EChannel.Bass_RxBxx:
                case EChannel.Bass_RGxxx:
                case EChannel.Bass_RGBxx:
                case EChannel.Bass_xxxYx:
                case EChannel.Bass_xxBYx:
                case EChannel.Bass_xGxYx:
                case EChannel.Bass_xGBYx:
                case EChannel.Bass_RxxYx:
                case EChannel.Bass_RxBYx:
                case EChannel.Bass_RGxYx:
                case EChannel.Bass_RGBYx:
                case EChannel.Bass_xxxxP:
                case EChannel.Bass_xxBxP:
                case EChannel.Bass_xGxxP:
                case EChannel.Bass_xGBxP:
                case EChannel.Bass_RxxxP:
                case EChannel.Bass_RxBxP:
                case EChannel.Bass_RGxxP:
                case EChannel.Bass_RGBxP:
                case EChannel.Bass_xxxYP:
                case EChannel.Bass_xxBYP:
                case EChannel.Bass_xGxYP:
                case EChannel.Bass_xGBYP:
                case EChannel.Bass_RxxYP:
                case EChannel.Bass_RxBYP:
                case EChannel.Bass_RGxYP:
                case EChannel.Bass_RGBYP:
                    UpdateChip_GuitarBass(configIni, ref dTX, ref pChip, EInstrumentPart.BASS);
                    break;

                case EChannel.Bass_Wailing:
                    UpdateChip_Bass_Wailing(configIni, ref dTX, ref pChip);
                    break;

                case EChannel.Guitar_LongNote:
                case EChannel.Bass_LongNote:
                {
                    if (!pChip.bHit && pChip.nDistanceFromBar.Drums <= 0)
                    {                                
                        pChip.bHit = true;
                        EInstrumentPart index = (pChip.nChannelNumber == EChannel.Guitar_LongNote ? EInstrumentPart.GUITAR : EInstrumentPart.BASS);
                        GuitarPerformance? guitar = GuitarFor(index);
                        if (guitar != null && guitar.HeldNote?.chipLongNoteEndPosition == pChip)
                        {
                            guitar.EndHold();
                        }
                    }
                }
                    break;

                case EChannel.HiHatClose_NoChip:
                case EChannel.Snare_NoChip:
                case EChannel.BassDrum_NoChip:
                case EChannel.HighTom_NoChip:
                case EChannel.LowTom_NoChip:
                case EChannel.Cymbal_NoChip:
                case EChannel.FloorTom_NoChip:
                case EChannel.HiHatOpen_NoChip:
                case EChannel.RideCymbal_NoChip:
                case EChannel.LeftCymbal_NoChip:
                case EChannel.LeftPedal_NoChip:
                case EChannel.LeftBassDrum_NoChip:
                    UpdateChip_NoSound_Drums(configIni, ref dTX, ref pChip);
                    break;

                case EChannel.Guitar_NoChip:
                    if (!pChip.bHit && (pChip.nDistanceFromBar.Guitar < 0))
                    {
                        pChip.bHit = true;
                        SetEmptyPickSound(EInstrumentPart.GUITAR, pChip);
                        pChip.nChannelNumber = EChannel.Guitar_Open;
                        pChip.frets = 0;
                    }
                    break;

                case EChannel.Bass_NoChip:
                    if (!pChip.bHit && (pChip.nDistanceFromBar.Bass < 0))
                    {
                        pChip.bHit = true;
                        SetEmptyPickSound(EInstrumentPart.BASS, pChip);
                        pChip.nChannelNumber = EChannel.Bass_Open;
                        pChip.frets = 0;
                    }
                    break;

                case EChannel.BarLine:
                    UpdateChip_BarLine(configIni, ref dTX, ref pChip);
                    break;
                default:
                    sequencer.Update(pChip);
                    ApplySequencerEvents();
                    break;
            }
        }
        return false;
    }

    private void ApplySequencerEvents()
    {
        CConfigIni config = CDTXMania.ConfigIni;
        foreach (SequencerEvent e in sequencer.Events)
        {
            CChip chip = e.Chip;
            switch (e.Kind)
            {
                case ESequencerEvent.Bpm:
                    ChangeBPM(e.Bpm);
                    break;

                case ESequencerEvent.Bga:
                    if (config.bBGAEnabled)
                    {
                        StartBga(chip);
                    }
                    break;

                case ESequencerEvent.BgaSwap:
                    if ((config.bBGAEnabled && chip.eBGA種別 == EBGAType.BMP) || chip.eBGA種別 == EBGAType.BMPTEX)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            if (nBGAスコープチャンネルマップ[0, i] == chip.nChannelNumber)
                            {
                                actBGA.ChangeScope(nBGAスコープチャンネルマップ[1, i], chip.rBMP, chip.rBMPTEX);
                            }
                        }
                    }
                    break;

                case ESequencerEvent.Video:
                    if (config.bAVIEnabled && (chip.eAVI種別 == EAVIType.AVI && chip.rAVI != null || chip.eAVI種別 == EAVIType.AVIPAN && chip.rAVIPan != null))
                    {
                        video.Start(chip.rAVI, chip.nPlaybackTimeMs);
                    }
                    break;
            }
        }
        sequencer.Events.Clear();
    }

    private void StartBga(CChip chip)
    {
        switch (chip.eBGA種別)
        {
            case EBGAType.BMPTEX:
                if (chip.rBMPTEX != null)
                {
                    actBGA.Start(chip.nChannelNumber, null, chip.rBMPTEX, chip.rBMPTEX.txImage.Width, chip.rBMPTEX.txImage.Height, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                }
                break;

            case EBGAType.BGA:
                if ((chip.rBGA != null) && ((chip.rBMP != null) || (chip.rBMPTEX != null)))
                {
                    actBGA.Start(chip.nChannelNumber, chip.rBMP, chip.rBMPTEX, chip.rBGA.pt画像側右下座標.X - chip.rBGA.pt画像側左上座標.X, chip.rBGA.pt画像側右下座標.Y - chip.rBGA.pt画像側左上座標.Y, 0, 0, chip.rBGA.pt画像側左上座標.X, chip.rBGA.pt画像側左上座標.Y, 0, 0, chip.rBGA.pt表示座標.X, chip.rBGA.pt表示座標.Y, 0, 0, 0);
                }
                break;

            case EBGAType.BGAPAN:
                if ((chip.rBGAPan != null) && ((chip.rBMP != null) || (chip.rBMPTEX != null)))
                {
                    actBGA.Start(chip.nChannelNumber, chip.rBMP, chip.rBMPTEX, chip.rBGAPan.sz開始サイズ.Width, chip.rBGAPan.sz開始サイズ.Height, chip.rBGAPan.sz終了サイズ.Width, chip.rBGAPan.sz終了サイズ.Height, chip.rBGAPan.pt画像側開始位置.X, chip.rBGAPan.pt画像側開始位置.Y, chip.rBGAPan.pt画像側終了位置.X, chip.rBGAPan.pt画像側終了位置.Y, chip.rBGAPan.pt表示側開始位置.X, chip.rBGAPan.pt表示側開始位置.Y, chip.rBGAPan.pt表示側終了位置.X, chip.rBGAPan.pt表示側終了位置.Y, chip.n総移動時間);
                }
                break;

            default:
                if (chip.rBMP != null)
                {
                    actBGA.Start(chip.nChannelNumber, chip.rBMP, null, chip.rBMP.nWidth, chip.rBMP.nHeight, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                }
                break;
        }
    }

    //the combo timers tick 16 times a beat
    private void ChangeBPM(double chartBPM)
    {
        actPlayInfo.dbBPM = (chartBPM * (CDTXMania.ConfigIni.nPlaySpeed / 20.0)) + CDTXMania.DTX.BASEBPM;

        if (CDTXMania.ConfigIni.bDrumsEnabled)
        {
            CDTXMania.stagePerfDrumsScreen.UnitTime = ((60.0 / (CDTXMania.stagePerfDrumsScreen.actPlayInfo.dbBPM) / 14.0));
            CDTXMania.stagePerfDrumsScreen.ctComboTimer = new CCounter(1, 16, (int)((60.0 / (CDTXMania.stagePerfDrumsScreen.actPlayInfo.dbBPM) / 16) * 1000.0), CDTXMania.Timer);
        }
        else if (CDTXMania.ConfigIni.bGuitarEnabled)
        {
            CDTXMania.stagePerfGuitarScreen.ctComboTimer = new CCounter(1, 16, (int)((60.0 / (CDTXMania.stagePerfGuitarScreen.actPlayInfo.dbBPM) / 16) * 1000.0), CDTXMania.Timer);
        }
    }
    protected virtual void UpdateChip_Drums(CConfigIni configIni, ref CDTX dTX, ref CChip pChip) { }
    protected virtual void UpdateChip_GuitarBass(CConfigIni configIni, ref CDTX dTX, ref CChip pChip, EInstrumentPart inst) { }  // t進行描画_チップ_ギターベース
    
    protected virtual void UpdateChip_GuitarBass_Wailing(CConfigIni configIni, ref CDTX dTX, ref CChip pChip, EInstrumentPart inst)
    {
        GuitarPerformance? guitar = GuitarFor(inst);
        if (guitar == null)
        {
            pChip.bHit = true;
            return;
        }

        guitar.UpdateWailingMarker(pChip);
        ApplyGuitarEvents(guitar);
    }
    protected virtual void UpdateChip_Guitar_Wailing(CConfigIni configIni, ref CDTX dTX, ref CChip pChip)
    {
        UpdateChip_GuitarBass_Wailing(configIni, ref dTX, ref pChip, EInstrumentPart.GUITAR);
    }
    protected abstract void UpdateChip_FillIn(CConfigIni configIni, ref CDTX dTX, ref CChip pChip);  // t進行描画_チップ_フィルイン
    protected abstract void UpdateChip_BarLine(CConfigIni configIni, ref CDTX dTX, ref CChip pChip);
    protected virtual void UpdateChip_Bass_Wailing(CConfigIni configIni, ref CDTX dTX, ref CChip pChip)  // t進行描画_チップ_ベース_ウェイリング
    {
        UpdateChip_GuitarBass_Wailing(configIni, ref dTX, ref pChip, EInstrumentPart.BASS);
    }
    protected virtual void UpdateChip_NoSound_Drums(CConfigIni configIni, ref CDTX dTX, ref CChip pChip)
    {
        if (!pChip.bHit && pChip.nDistanceFromBar.Drums < 0)
        {
            pChip.bHit = true;
        }
    }  // t進行描画_チップ_空打ち音設定_ドラム

    protected void UpdateChipAnimation()
    {
        for (int i = 0; i < 3; i++)			// 0=drums, 1=guitar, 2=bass
        {
            if (ctChipPatternAnimation[i] != null)
            {
                ctChipPatternAnimation[i].tUpdateLoop();
            }
        }
        if (ctWailingChipPatternAnimation != null)
        {
            ctWailingChipPatternAnimation.tUpdateLoop();
        }
        if (ctBPMBar != null)
        {
            ctBPMBar.tUpdateLoop();
            ctComboTimer.tUpdateLoop();
        }
        if (CDTXMania.ConfigIni.bDrumsEnabled)  //2013.05.16.kairera0467 ギター側のアニメーションは未実装なのでとりあえず。
            ct登場用.tUpdate();
    }

    protected bool CanLeave => ePhaseID switch
    {
        EPhase.Common_FadeOut or EPhase.PERFORMANCE_STAGE_FAILED_END => !GitaDoraTransition.isAnimating,
        EPhase.PERFORMANCE_STAGE_CLEAR => ctResultDelay.bReachedEndValue,
        _ => false
    };

    protected void UpdateResultDelay()
    {
        if (ePhaseID == EPhase.PERFORMANCE_STAGE_CLEAR)
        {
            ctResultDelay.tUpdate();
        }
    }

    protected void tStartResultDelay()
    {
        ctResultDelay = new CCounter(0, CDTXMania.ConfigIni.nResultDelayMs, 1, CDTXMania.Timer);
    }
    protected void tUpdateAndDraw_LaneFlushD(Matrix4x4 origin)
    {
        if ((ePhaseID != EPhase.PERFORMANCE_STAGE_FAILED) && (ePhaseID != EPhase.PERFORMANCE_STAGE_FAILED_END))
        {
            actLaneFlushD.Draw(origin);
        }
    }
    protected void tUpdateAndDraw_LaneFlushGB(Matrix4x4 origin, EInstrumentPart part)
    {
        if (CDTXMania.ConfigIni.bGuitarEnabled)
        {
            actLaneFlushGB.Draw(origin, part);
        }
    }
    protected abstract void tUpdateAndDraw_PerformanceInformation();
    protected void tUpdateAndDraw_PerformanceInformation(int x, int y)
    {
        if (!CDTXMania.ConfigIni.bHidePerformanceInformation)
        {
            actPlayInfo.tUpdateAndDraw(x, y);
        }
    }
    
    protected UIImage background;

    protected void UpdateBackground()
    {
        if (background != null)
        {
            background.isVisible = ShowStillBackground();
        }
    }

    private bool ShowStillBackground() => !ChartWillPlayVideo() || video.loadFailed;

    protected void tUpdateAndDraw_JudgementString()  // t進行描画_判定文字列
    {
        actJudgeString.OnUpdateAndDraw();
    }
    protected void tUpdateAndDraw_JudgementString1_ForNormalPosition()  // t進行描画_判定文字列1_通常位置指定の場合
    {
        if (CDTXMania.ConfigIni.JudgementStringPosition.Drums != EType.B)
        {
            actJudgeString.OnUpdateAndDraw();
        }
    }
    protected void tUpdateAndDraw_JudgementString2_ForPositionOnJudgementLine()  // t進行描画_判定文字列2_判定ライン上指定の場合
    {
        if (CDTXMania.ConfigIni.JudgementStringPosition.Drums == EType.B)
        {
            actJudgeString.OnUpdateAndDraw();
        }
    }

    protected void DrawScore(Matrix4x4 origin, EInstrumentPart part)
    {
        if (CDTXMania.ConfigIni.bShowScore)
        {
            actScore.Draw(origin, part);
        }
    }

    //the root when a skin's layout has no section for this player
    protected UIGroup SectionFor(EInstrument player)
    {
        UIGroup section = LayoutGroup(PlayersGroup).GetChild<UIGroup>(player.ToString()) ?? ui;
        section.EnsureContent();
        return section;
    }

    protected LegacyDrawable AddLegacyPart(UIGroup parent, string name, int layer, Action<Matrix4x4> draw)
    {
        LegacyDrawable part = parent.AddChild(new LegacyDrawable(name, draw));
        part.renderOrder = layer;
        return part;
    }

    private bool ShowsChips => ePhaseID is not (EPhase.PERFORMANCE_STAGE_FAILED or EPhase.PERFORMANCE_STAGE_FAILED_END);

    protected void UpdateChipRenderer(ChipRenderer renderer, EInstrumentPart part)
    {
        renderer.chips = timeline.Chips;
        renderer.firstChip = ShowsChips ? timeline.TopChip : -1;
        renderer.judgeLineY = nJudgeLinePosY[(int)part];
    }

    //judgeLineY: where a chip at distance 0 is centred
    protected void UpdateLoopMarkers(LoopMarkers markers, EInstrumentPart part, float judgeLineY, bool scrollsDown)
    {
        double multiplier = CChip.ScrollMultiplier(part, actScrollSpeed.db現在の譜面スクロール速度[(int)part]);
        long now = AudioMixer.Timer.nCurrentTime;
        int laneDisplay = CDTXMania.ConfigIni.nLaneDisp[(int)part];

        markers.showsBegin = ShowsChips && LoopBeginMs != -1;
        markers.showsEnd = ShowsChips && LoopBeginMs != -1 && LoopEndMs != -1;
        markers.beginDistance = (float)((LoopBeginMs - now) * multiplier);
        markers.endDistance = (float)((LoopEndMs - now) * multiplier);
        markers.showsLines = laneDisplay == 0 || laneDisplay == 1;
        markers.judgeLineY = judgeLineY;
        markers.scrollsDown = scrollsDown;
    }

    protected void UpdateScrollSpeed()
    {
        actScrollSpeed.OnUpdateAndDraw();
    }

    //a chip only carries an AVI once the file behind it was found
    private static bool ChartWillPlayVideo()
    {
        if (!CDTXMania.ConfigIni.bAVIEnabled || CDTXMania.DTX?.listChip == null)
        {
            return false;
        }

        foreach (CChip chip in CDTXMania.DTX.listChip)
        {
            if (chip.rAVI != null)
            {
                return true;
            }
        }

        return false;
    }

    protected void tGenerateBackgroundTexture()
    {
        Rectangle bgrect;
        string DefaultBgFilename;
        
        //drums
        if (CDTXMania.GetCurrentInstrument() == 0)
        {
            bgrect = new (980, 0, 0, 0);

            if (CDTXMania.ConfigIni.bBGAEnabled)
            {
                bgrect = new(980, 0, 278, 355);
            }
            
            DefaultBgFilename = CSkin.Path(@"Graphics\7_background.jpg");
        }
        else //guitar
        {
            bgrect = new( 0, 0, 1280, 720 );
            DefaultBgFilename = CSkin.Path(@"Graphics\7_background_Guitar.jpg");
        }
         
        string BgFilename = "";
        string BACKGROUND = null;
        if ( ( CDTXMania.DTX.BACKGROUND_GR != null ) && ( CDTXMania.DTX.BACKGROUND_GR.Length > 0 ) )
        {
            BACKGROUND = CDTXMania.DTX.BACKGROUND_GR;
        }
        else if ( ( CDTXMania.DTX.BACKGROUND != null ) && ( CDTXMania.DTX.BACKGROUND.Length > 0 ) )
        {
            BACKGROUND = CDTXMania.DTX.BACKGROUND;
        }
        if ( ( BACKGROUND != null ) && ( BACKGROUND.Length > 0 ) )
        {
            BgFilename = CDTXMania.DTX.strFolderName + BACKGROUND;
        }
		
        //a chart can name a background this cannot read, which resolves to the magenta placeholder
        BaseTexture texture = BaseTexture.LoadFromPath(string.IsNullOrEmpty(BgFilename) ? DefaultBgFilename : BgFilename);
        if (texture.notFound)
        {
            texture.Dispose();
            texture = BaseTexture.LoadFromPath(DefaultBgFilename);
        }

        UICoverGroup backgroundCover = ui.AddChild(new UICoverGroup("Static Background"));
        backgroundCover.renderOrder = -1;

        background = backgroundCover.AddChild(new UIImage(texture));
        background.name = "Image";
        background.size = UISize.Inherited;

        background.isVisible = ShowStillBackground();

        //todo: maybe reimplement the more complex background texture behaviour
    }

    protected void tHandleInput_GuitarBass(EInstrumentPart inst)
    {
        GuitarPerformance? guitar = GuitarFor(inst);
        if (guitar == null)
        {
            return;
        }

        int indexInst = (int)inst;
        if (CDTXMania.Pad.bPressing(inst, EPad.Decide) && CDTXMania.Pad.bPressed(inst, EPad.B))
        {
            ChangeScrollSpeed(inst, 1);
        }
        if (CDTXMania.Pad.bPressing(inst, EPad.Decide) && CDTXMania.Pad.bPressed(inst, EPad.R))
        {
            ChangeScrollSpeed(inst, -1);
        }

        if (!CDTXMania.ConfigIni.bGuitarEnabled || !CDTXMania.DTX.bHasChips[indexInst])
        {
            return;
        }

        guitar.UpdateInput();
        guitar.Data.RecordInputDevices();

        int firstLane = inst == EInstrumentPart.GUITAR ? 0 : 5;
        for (int lane = 0; lane < 5; lane++)
        {
            if ((guitar.LitLanes & (1 << lane)) != 0)
            {
                actLaneFlushGB.Start(firstLane + lane);
            }
        }

        ApplyGuitarEvents(guitar);
    }

    private void SetEmptyPickSound(EInstrumentPart part, CChip chip)
    {
        GuitarPerformance? guitar = GuitarFor(part);
        if (guitar != null)
        {
            guitar.EmptyPickSound = chip;
        }
    }

    protected virtual void UpdateGraph(EInstrumentPart part) { }

    protected void ApplyGuitarEvents(GuitarPerformance guitar)
    {
        EInstrumentPart side = guitar.Side;
        int player = (int)guitar.Player - 1;
        foreach (GuitarEvent e in guitar.Events)
        {
            for (int lane = 0; lane < 5; lane++)
            {
                if ((e.FireLanes & (1 << lane)) != 0)
                {
                    actChipFireGB[player].Start(lane, e.Chip);
                }
            }

            switch (e.Kind)
            {
                case EGuitarEvent.AutoNote:
                    sequencer.PlaySound(e.Chip, AudioMixer.Timer.nResetAtMs + e.Time, side, CDTXMania.DTX.nモニタを考慮した音量(side), false, e.ShiftPitch);
                    break;

                case EGuitarEvent.PickedNote:
                    sequencer.PlaySound(e.Chip, AudioMixer.Timer.nSystemTimeMs, side, CDTXMania.ConfigIni.n手動再生音量, CDTXMania.ConfigIni.b演奏音を強調する[(int)side], e.ShiftPitch);
                    break;

                case EGuitarEvent.EmptyPick:
                    if (e.Chip != null)
                    {
                        sequencer.PlaySound(e.Chip, AudioMixer.Timer.nSystemTimeMs, side, CDTXMania.ConfigIni.n手動再生音量, CDTXMania.ConfigIni.b演奏音を強調する[(int)side], e.ShiftPitch);
                    }
                    if (e.Miss)
                    {
                        actJudgeString.Start(guitar.Chart == EChartPart.Guitar ? 13 : 14, EJudgement.Miss, 999);
                    }
                    break;

                case EGuitarEvent.Judged:
                    actJudgeString.Start(e.Chip.eInstrumentPart == EInstrumentPart.GUITAR ? 13 : 14, e.Auto ? EJudgement.Auto : e.Judgement, e.Chip.nLag);
                    if (!e.Auto && e.Judgement is EJudgement.Perfect or EJudgement.Great or EJudgement.Good)
                    {
                        actProgressBar.Hit(e.Chip.eInstrumentPart, e.Chip.nPlaybackTimeMs, e.Judgement);
                    }
                    UpdateGraph(e.Chip.eInstrumentPart);
                    break;

                case EGuitarEvent.HoldBonus:
                    actGuitarBonus.startBonus(side);
                    break;

                case EGuitarEvent.HoldRelease:
                    sequencer.StopGuitarSound(side);
                    break;

                case EGuitarEvent.Wail:
                    actWailingBonus.Start(side, e.Chip);
                    wailingEffect[player].Play();
                    break;
            }
        }
        guitar.Events.Clear();
    }

    #endregion
    public void tJumpInSongToBar(int nStartBar)
    {
        int nTopChip = 0;
        for (int i = 0; i < CDTXMania.DTX.listChip.Count; i++)
        {
            CChip pChip = CDTXMania.DTX.listChip[i];
            if (pChip.nPlaybackPosition >= 384 * nStartBar)
            {
                nTopChip = i;
                break;
            }
        }
        int nStartTime = CDTXMania.DTX.listChip[nTopChip].nPlaybackTimeMs;

        tJumpInSong(nStartTime);
    }
    #region [ skin editor transport ]

    //same paths the training keys use

    public long PreviewPositionMs => AudioMixer.Timer.nCurrentTime;

    //a chart has no length of its own, so use the last chip
    public long PreviewLengthMs => timeline.Chips is { Count: > 0 } ? timeline.Chips[^1].nPlaybackTimeMs : 0;

    public long PreviewLoopBeginMs => LoopBeginMs;
    public long PreviewLoopEndMs => LoopEndMs;

    public bool PreviewPaused
    {
        get => bPAUSE;
        set
        {
            if (bPAUSE == value)
            {
                return;
            }

            bPAUSE = value;

            if (value)
            {
                AudioMixer.Timer.tPause();
                CDTXMania.Timer.tPause();
                CDTXMania.DTX.tPausePlaybackForAllChips();
            }
            else
            {
                AudioMixer.Timer.tResume();
                CDTXMania.Timer.tResume();
                CDTXMania.DTX.tResumePlaybackForAllChips();
            }
        }
    }

    public void PreviewSetLoop(long beginMs, long endMs)
    {
        LoopBeginMs = Math.Max(0, beginMs);
        LoopEndMs = Math.Max(LoopBeginMs + 1, endMs);
    }

    public void PreviewClearLoop()
    {
        LoopBeginMs = -1;
        LoopEndMs = -1;
    }

    public void PreviewChangeSpeed(int offset) => tChangePlaySpeed(offset);

    #endregion

    public void tJumpInSong(long newPosition)
    {
        long nNewPosition = Math.Max(0, newPosition);
        Trace.TraceInformation("JUMP IN SONG currentPosition={0}, newPosition={1}", AudioMixer.Timer.nCurrentTime, nNewPosition);

        AudioMixer.Timer.tReset();
        AudioMixer.Timer.tPause();
        AudioMixer.Timer.nCurrentTime = nNewPosition;
        CDTXMania.Timer.tReset();
        CDTXMania.Timer.nCurrentTime = nNewPosition;

        //Stop any AVI and BGA
        video.Stop();
        actBGA.Stop();
        foreach (GuitarPerformance? guitar in guitars)
        {
            guitar?.EndHold();
        }

        //Reset Accumulated Score
        foreach (PerformanceData data in partData)
        {
            data.ResetLongNoteBonus();
        }

        timeline.Seek(nNewPosition);

        //Adjust BGM to new position

        #region [ BGMやギターなど、演奏開始のタイミングで再生がかかっているサウンドのの途中再生開始 ] // (CDTXのt入力・行解析・チップ配置()で小節番号が+1されているのを削っておくこと)
        for (int i = timeline.TopChip; i >= 0; i--)
        {
            CChip pChip = CDTXMania.DTX.listChip[i];
            int nDuration = pChip.GetDuration();

            if ((pChip.nPlaybackTimeMs + nDuration > 0) && (pChip.nPlaybackTimeMs <= nNewPosition) && (nNewPosition <= pChip.nPlaybackTimeMs + nDuration))
            {
                if (pChip.bWAVを使うチャンネルである /*&& !pChip.b空打ちチップである*/) // wav系チャンネル、且つ、空打ちチップではない
                {
                    CDTX.CWAV wc;
                    bool b = CDTXMania.DTX.listWAV.TryGetValue(pChip.nIntegerValue_InternalNumber, out wc);
                    if (!b) continue;

                    if ((wc.bIsBGMSound && CDTXMania.ConfigIni.bBGM音を発声する) || (!wc.bIsBGMSound))
                    {
                        CDTXMania.DTX.tチップの再生(pChip, AudioMixer.Timer.nResetAtMs + pChip.nPlaybackTimeMs, CDTXMania.DTX.nモニタを考慮した音量(EInstrumentPart.UNKNOWN));

                        //held rather than left running: everything that should already be sounding at the
                        //new position is set up first, then started together
                        if (wc.clip != null)
                        {
                            AudioMixer.HoldLastAt(wc.clip, nNewPosition - pChip.nPlaybackTimeMs);
                        }
                    }
                }
            }
        }
        #endregion
        #region [ 演奏開始時点で既に表示されているBGAとAVIの、シークと再生 ]
        //Re-enable SkipStart now that we have migrated to AVI renderer 
        actBGA.SkipStart((int)nNewPosition); 
        video.SkipStart((int)nNewPosition);
        #endregion
        #region [ PAUSEしていたサウンドを一斉に再生再開する(ただしタイマを止めているので、ここではまだ再生開始しない) ]
        AudioMixer.StartHeld();
        #endregion

        bPAUSE = false;
        AudioMixer.Timer.tResume();

        // re-display presence with new timestamps
        tDisplayPresence();
    }

    private void tChangePlaySpeed(int nSpeedOffset)
    {
        Trace.TraceInformation(((nSpeedOffset>0) ? "Increase" : "Decrease") + " Play Speed from " + CDTXMania.ConfigIni.nPlaySpeed + " to " + (CDTXMania.ConfigIni.nPlaySpeed+nSpeedOffset));

        double dbOldSpeed = ((double)CDTXMania.ConfigIni.nPlaySpeed) / 20;
        CDTXMania.ConfigIni.nPlaySpeed += nSpeedOffset;
        double dbNewSpeed = ((double)CDTXMania.ConfigIni.nPlaySpeed) / 20;

        foreach (CChip chip in CDTXMania.DTX.listChip)
        {
            chip.nPlaybackTimeMs = (int)((chip.nPlaybackTimeMs * dbOldSpeed) / dbNewSpeed);
        }
        if (LoopBeginMs != -1)
        {
            LoopBeginMs = (int)((LoopBeginMs * dbOldSpeed) / dbNewSpeed);
        }
        if (LoopEndMs != -1)
        {
            LoopEndMs = (int)((LoopEndMs * dbOldSpeed) / dbNewSpeed);
        }

        AudioMixer.Timer.nCurrentTime = (int)((AudioMixer.Timer.nCurrentTime * dbOldSpeed) / dbNewSpeed);

        tJumpInSong(AudioMixer.Timer.nCurrentTime);

        // Display new play speed
        if (CDTXMania.ConfigIni.nShowPlaySpeed == (int)EShowPlaySpeed.ON || CDTXMania.ConfigIni.nShowPlaySpeed == (int)EShowPlaySpeed.IF_CHANGED_IN_GAME)
        {
            tUpdatePlayspeedText();
        }

        // re-display presence with new timestamps
        tDisplayPresence();
    }

    private void tUpdatePlayspeedText()
    {
        if (playspeedText == null)
        {
            playspeedText = LayoutGroup(HudGroup).AddChild(new UIText("playspeed", 18));
            playspeedText.dontSerialize = true;
            playspeedText.fillColor = Color4.White;
            playspeedText.outlineGradientTopColor = Color4.Black;
            playspeedText.outlineGradientBottomColor = Color.Red;
            playspeedText.outlineGradientMode = UiTextGradientMode.Vertical;
            playspeedText.name = "PlaySpeedIndicator";
        }

        if (CDTXMania.GetCurrentInstrument() == 0)
        {
            playspeedText.position = new Vector3(25, 200, 0);
        }
        else
        {
            playspeedText.position = new Vector3(600, 687, 0);
        }

        bool showPlaySpeed = CDTXMania.ConfigIni.nShowPlaySpeed == (int)EShowPlaySpeed.ON
                             || (CDTXMania.ConfigIni.nShowPlaySpeed == (int)EShowPlaySpeed.IF_CHANGED_IN_GAME
                                 && CDTXMania.ConfigIni.nPlaySpeed != 20);
        playspeedText.isVisible = showPlaySpeed;

        if (showPlaySpeed)
        {
            playspeedText.text = "Play Speed: x" + (CDTXMania.ConfigIni.nPlaySpeed / 20.0).ToString("0.000");
        }
    }

    public void t再読込()
    {
        CDTXMania.DTX.tStopPlayingAllChips();
        eReturnValue = EPerfScreenReturnValue.Restart;
        ePhaseID = EPhase.PERFORMANCE_STAGE_RESTART;
        bPAUSE = false;

        // #34048 2014.7.16 yyagi
        #region [ 読み込み画面に遷移する前に、設定変更した可能性があるパラメータをConfigIniクラスに書き戻す ]
        //TODO CDTXMania.ConfigIni.bDebugInfo = CDTXMania.ConfigIni.bViewerShowDebugStatus;
        #endregion
    }

    public void t停止()
    {
        Trace.TraceInformation("Stop command received");
        CDTXMania.DTX.tStopPlayingAllChips();
        video.Stop();
        actBGA.Stop();
        CDTXMania.Timer.tPause();       // 再生時刻カウンタ停止

        timeline.SeekToEnd();
        foreach (GuitarPerformance? guitar in guitars)
        {
            guitar?.EndHold();
        }
        //
        foreach (PerformanceData data in partData)
        {
            data.ResetLongNoteBonus();
        }
        // 自分自身のOn活性化()相当の処理もすべき。
    }
}