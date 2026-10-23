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

    protected const string PlayfieldGroup = "Playfield";
    protected const string HudGroup = "HUD";
    protected const string OverlaysGroup = "Overlays";

    //a skin layout without the group gets the part at the root
    protected UIGroup LayoutGroup(string name) => ui.GetChild<UIGroup>(name) ?? ui;

    internal const int LaneArtLayer = 1;
    internal const int LegacyLanesLayer = 2;
    internal const int LaneOverlayLayer = 3;
    internal const int LegacyOverlayLayer = 4;
    internal const int EffectsLayer = 5;
    internal const int PanelLayer = 6;

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

        UIGroup playfield = Group(PlayfieldGroup, 1);
        UIGroup hud = Group(HudGroup, 2);
        UIGroup overlays = Group(OverlaysGroup, 3);

        Add(playfield, new PerformanceLane(EInstrumentPart.GUITAR), "GuitarLane", 67, 42, LaneArtLayer);
        Add(playfield, new PerformanceLane(EInstrumentPart.BASS), "BassLane", 937, 42, LaneArtLayer);

        Add(playfield, new PerformanceLaneCaps(EInstrumentPart.GUITAR), "GuitarLaneCaps", 67, 42, LaneOverlayLayer);
        Add(playfield, new PerformanceLaneCaps(EInstrumentPart.BASS), "BassLaneCaps", 937, 42, LaneOverlayLayer);

        Add(playfield, new PerformanceShutters(EInstrumentPart.DRUMS), "DrumsShutters", 295, 0, LaneOverlayLayer);
        Add(playfield, new PerformanceShutters(EInstrumentPart.GUITAR), "GuitarShutters", 80, 0, LaneOverlayLayer);
        Add(playfield, new PerformanceShutters(EInstrumentPart.BASS), "BassShutters", 950, 0, LaneOverlayLayer);

        Add(playfield, new PerformanceJudgeLine(EInstrumentPart.DRUMS), "DrumsJudgeLine", 295, 0, LaneOverlayLayer);
        Add(playfield, new PerformanceJudgeLine(EInstrumentPart.GUITAR), "GuitarJudgeLine", 80, 0, LaneOverlayLayer);
        Add(playfield, new PerformanceJudgeLine(EInstrumentPart.BASS), "BassJudgeLine", 950, 0, LaneOverlayLayer);

        InfoBox info = hud.AddChild(new InfoBox());
        info.position = new Vector3(1277, 3, 0);
        info.renderOrder = LaneArtLayer;

        UIGroup perspective = hud.AddChild(new UIGroup("Perspective"));
        perspective.size = new Vector2(1280, 720);
        perspective.parentAnchor = new Vector2(0.5f, 0.5f);
        perspective.pivot = new Vector2(0.5f, 0.5f);
        perspective.perspective = 1410;
        perspective.vanishingPoint = new Vector2(640, 360);
        perspective.renderOrder = PanelLayer;

        PerformanceStatusPanel drumsStatus = perspective.AddChild(new PerformanceStatusPanel(EInstrumentPart.DRUMS));
        drumsStatus.name = "DrumsStatus";
        
        //3d version
        //drumsStatus.position = new Vector3(86, 186, -39);
        //drumsStatus.rotation = new Vector3(0, -61f * MathF.PI / 180, 0);

        //2d version
        drumsStatus.position = new Vector3(14, 237, 0);
        
        Add(hud, new PerformanceStatusPanel(EInstrumentPart.GUITAR), "GuitarStatus", 373, 254, PanelLayer);
        Add(hud, new PerformanceStatusPanel(EInstrumentPart.BASS), "BassStatus", 665, 254, PanelLayer);

        PerformanceElement drumsGauge = Add(hud, new PerformanceGauge(EInstrumentPart.DRUMS), "DrumsGauge", 294, 626, PanelLayer);
        drumsGauge.bindings.Add(new UIBinding("position.Y", "Config.Reverse.Drums")
        {
            condition = BindingCondition.IsTrue, whenTrue = 28, whenFalse = 626
        });
        Add(hud, new PerformanceGauge(EInstrumentPart.GUITAR), "GuitarGauge", 80, 0, PanelLayer);
        Add(hud, new PerformanceGauge(EInstrumentPart.BASS), "BassGauge", 950, 0, PanelLayer);

        StageFailedCover failed = overlays.AddChild(new StageFailedCover());
        failed.renderOrder = 1000;

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

        static PerformanceElement Add(UIGroup parent, PerformanceElement element, string name, float x, float y, int renderOrder)
        {
            parent.AddChild(element);
            element.name = name;
            element.position = new Vector3(x, y, 0);
            element.renderOrder = renderOrder;
            return element;
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

        nInputAdjustTimeMs.Drums = CDTXMania.ConfigIni.nInputAdjustTimeMs.Drums;		// #23580 2011.1.3 yyagi
        nInputAdjustTimeMs.Guitar = CDTXMania.ConfigIni.nInputAdjustTimeMs.Guitar;		//        2011.1.7 ikanick 修正
        nInputAdjustTimeMs.Bass = CDTXMania.ConfigIni.nInputAdjustTimeMs.Bass;			//
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
        sw = new Stopwatch();
        sw2 = new Stopwatch();
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
    protected CActPerfCommonLaneFlushGB actLaneFlushGB;
    protected CActPerformanceInformation actPlayInfo;
    public CActPerfCommonScore actScore;
    protected CActPerfStageFailure actStageFailed;
    protected CActPerfCommonWailingBonus actWailingBonus;
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
    protected BaseTexture txChip;  // txチップ

    protected BaseTexture[] txNote = [];
    protected BaseTexture[] txHoldNoteBg = [];

    public BaseTexture txBonusEffect;

    //fork

    protected STDGBVALUE<int> nInputAdjustTimeMs;		// #23580 2011.1.3 yyagi
    public STAUTOPLAY bIsAutoPlay;		// #24239 2011.1.23 yyagi
    //		protected int nRisky_InitialVar, nRiskyTime;		// #23559 2011.7.28 yyagi → CAct演奏ゲージ共通クラスに隠蔽

    protected readonly ChipTimeline timeline = new();
    protected Dictionary<int, CDTX.CWAV> listWAV;

    //key repeat for moving the judge line: a 100ms delay, then a step every 10ms
    protected Stopwatch sw;
    protected Stopwatch sw2;

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

    private void ChangeInputAdjustTimeInPlaying(IInputKeyboard keyboard, int plusminus)		// #23580 2011.1.16 yyagi UI for InputAdjustTime in playing screen.
    {
        int part, offset = plusminus;
        if (keyboard.bKeyPressing(SlimDXKey.LeftShift) || keyboard.bKeyPressing(SlimDXKey.RightShift))	// Guitar InputAdjustTime
        {
            part = (int)EInstrumentPart.GUITAR;
        }
        else if (keyboard.bKeyPressing(SlimDXKey.LeftAlt) || keyboard.bKeyPressing(SlimDXKey.RightAlt))	// Bass InputAdjustTime
        {
            part = (int)EInstrumentPart.BASS;
        }
        else	// Drums InputAdjustTime
        {
            part = (int)EInstrumentPart.DRUMS;
        }
        if (!keyboard.bKeyPressing(SlimDXKey.LeftControl) && !keyboard.bKeyPressing(SlimDXKey.RightControl))
        {
            offset *= 10;
        }

        nInputAdjustTimeMs[part] += offset;
        if (nInputAdjustTimeMs[part] > 99)
        {
            nInputAdjustTimeMs[part] = 99;
        }
        else if (nInputAdjustTimeMs[part] < -99)
        {
            nInputAdjustTimeMs[part] = -99;
        }
        CDTXMania.ConfigIni.nInputAdjustTimeMs[part] = nInputAdjustTimeMs[part];
    }

    protected abstract void tHandleInput_Drums();
    protected abstract void ScrollSpeedUp();
    protected abstract void ScrollSpeedDown();
    protected void tHandleKeyInput()
    {
        IInputKeyboard keyboard = CDTXMania.InputManager.Keyboard;
        if (CDTXMania.Pad.bPressed(EInstrumentPart.BASS, EPad.Help))
        {	// shift+f1 (pause)
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

        if (((ePhaseID == EPhase.PERFORMANCE_STAGE_FAILED)) || (ePhaseID == EPhase.PERFORMANCE_STAGE_FAILED_END)) return;
        
        if (!bPAUSE)
        {
            tHandleInput_Drums();
            tHandleInput_GuitarBass(EInstrumentPart.GUITAR);
            tHandleInput_GuitarBass(EInstrumentPart.BASS);
        }
        if (!bPAUSE && keyboard.bKeyPressed(SlimDXKey.UpArrow) && (keyboard.bKeyPressing(SlimDXKey.RightShift) || keyboard.bKeyPressing(SlimDXKey.LeftShift)))
        {	// shift (+ctrl) + UpArrow (BGMAdjust)
            CDTXMania.DTX.t各自動再生音チップの再生時刻を変更する((keyboard.bKeyPressing(SlimDXKey.LeftControl) || keyboard.bKeyPressing(SlimDXKey.RightControl)) ? 1 : 10);
            CDTXMania.DTX.tAutoCorrectWavPlaybackPosition();
        }
        else if (!bPAUSE && keyboard.bKeyPressed(SlimDXKey.DownArrow) && (keyboard.bKeyPressing(SlimDXKey.RightShift) || keyboard.bKeyPressing(SlimDXKey.LeftShift)))
        {	// shift + DownArrow (BGMAdjust)
            CDTXMania.DTX.t各自動再生音チップの再生時刻を変更する((keyboard.bKeyPressing(SlimDXKey.LeftControl) || keyboard.bKeyPressing(SlimDXKey.RightControl)) ? -1 : -10);
            CDTXMania.DTX.tAutoCorrectWavPlaybackPosition();
        }
        else if (keyboard.bKeyPressed(SlimDXKey.UpArrow))
        {	// UpArrow(scrollspeed up)
            ScrollSpeedUp();
        }
        else if (keyboard.bKeyPressed(SlimDXKey.DownArrow))
        {	// DownArrow (scrollspeed down)
            ScrollSpeedDown();
        }

        else if (CDTXMania.Pad.bPressed(EInstrumentPart.GUITAR, EPad.Help))
        {	// del (debug info)
            CDTXMania.ConfigIni.bShowPerformanceInformation = !CDTXMania.ConfigIni.bShowPerformanceInformation;
        }
        else if (!bPAUSE && keyboard.bKeyPressed(SlimDXKey.LeftArrow))		// #24243 2011.1.16 yyagi UI for InputAdjustTime in playing screen.
        {
            ChangeInputAdjustTimeInPlaying(keyboard, -1);
        }
        else if (!bPAUSE && keyboard.bKeyPressed(SlimDXKey.RightArrow))		// #24243 2011.1.16 yyagi UI for InputAdjustTime in playing screen.
        {
            ChangeInputAdjustTimeInPlaying(keyboard, +1);
        }
        //leaving deactivates this stage before the change runs, and in preview the change is dropped. The
        //skin editor is how you leave instead
        else if (!bPAUSE && !previewMode && (ePhaseID == EPhase.Common_DefaultState) && (keyboard.bKeyPressed(SlimDXKey.Escape)))
        {	// escape (exit)
            GitaDoraTransition.Close();
            ePhaseID = EPhase.Common_FadeOut;
            eReturnValue = EPerfScreenReturnValue.Interruption;
        }
        else if (CDTXMania.Pad.bPressed(EKeyConfigPart.SYSTEM, EKeyConfigPad.Restart))
        {
            if (bPAUSE)
            {
                AudioMixer.Timer.tResume();
                CDTXMania.Timer.tResume();
            }

            //restarting goes out through the loading screen, so in preview seek to the start instead
            if (previewMode)
            {
                tRestartForPreview();
                return;
            }

            ePhaseID = EPhase.PERFORMANCE_STAGE_RESTART;
            eReturnValue = EPerfScreenReturnValue.Restart;
        }
        else if (CDTXMania.Pad.bPressed(EKeyConfigPart.SYSTEM, EKeyConfigPad.SkipForward))
        {
            bIsTrainingMode = true;
            Trace.TraceInformation("SKIP FORWARD AudioMixer.Timer.nCurrentTime=" + AudioMixer.Timer.nCurrentTime + ", CDTXMania.Timer.nCurrentTime=" + CDTXMania.Timer.nCurrentTime);
            tJumpInSong(AudioMixer.Timer.nCurrentTime + CDTXMania.ConfigIni.nSkipTimeMs);
        }
        else if (CDTXMania.Pad.bPressed(EKeyConfigPart.SYSTEM, EKeyConfigPad.SkipBackward))
        {
            bIsTrainingMode = true;
            Trace.TraceInformation("SKIP BACKWARD AudioMixer.Timer.nCurrentTime=" + AudioMixer.Timer.nCurrentTime + ", CDTXMania.Timer.nCurrentTime=" + CDTXMania.Timer.nCurrentTime);
            tJumpInSong(Math.Max(0, AudioMixer.Timer.nCurrentTime - CDTXMania.ConfigIni.nSkipTimeMs));
        }
        else if (CDTXMania.Pad.bPressed(EKeyConfigPart.SYSTEM, EKeyConfigPad.LoopCreate))
        {
            bIsTrainingMode = true;
            if (LoopBeginMs == -1)
            {
                Trace.TraceInformation("INSERT LOOP BEGIN AudioMixer.Timer.nCurrentTime=" + AudioMixer.Timer.nCurrentTime + ", CDTXMania.Timer.nCurrentTime=" + CDTXMania.Timer.nCurrentTime);
                LoopBeginMs = AudioMixer.Timer.nCurrentTime;
            }
            else
            {
                if (LoopEndMs == -1)
                {
                    if (LoopBeginMs < AudioMixer.Timer.nCurrentTime)
                    {
                        Trace.TraceInformation("INSERT LOOP END AudioMixer.Timer.nCurrentTime=" + AudioMixer.Timer.nCurrentTime + ", CDTXMania.Timer.nCurrentTime=" + CDTXMania.Timer.nCurrentTime);
                        LoopEndMs = AudioMixer.Timer.nCurrentTime;
                    }
                    else
                    {
                        Trace.TraceInformation("INSERT LOOP BEGIN AND SWAP AudioMixer.Timer.nCurrentTime=" + AudioMixer.Timer.nCurrentTime + ", CDTXMania.Timer.nCurrentTime=" + CDTXMania.Timer.nCurrentTime);
                        LoopEndMs = LoopBeginMs;
                        LoopBeginMs = AudioMixer.Timer.nCurrentTime;
                    }
                }
                //Else loop already set, do nothing
            }
        }
        else if (CDTXMania.Pad.bPressed(EKeyConfigPart.SYSTEM, EKeyConfigPad.LoopDelete))
        {
            Trace.TraceInformation("REMOVE LOOP AudioMixer.Timer.nCurrentTime=" + AudioMixer.Timer.nCurrentTime + ", CDTXMania.Timer.nCurrentTime=" + CDTXMania.Timer.nCurrentTime);
            LoopBeginMs = -1;
            LoopEndMs = -1;
        }
        else if (CDTXMania.Pad.bPressed(EKeyConfigPart.SYSTEM, EKeyConfigPad.DecreasePlaySpeed))
        {
            // Decrease Play Speed
            if (CDTXMania.ConfigIni.nPlaySpeed > CConstants.PLAYSPEED_MIN)
            {
                bIsTrainingMode = true;
                tChangePlaySpeed(-1);
            }
        }
        else if (CDTXMania.Pad.bPressed(EKeyConfigPart.SYSTEM, EKeyConfigPad.IncreasePlaySpeed))
        {
            // Increase Play Speed
            if (CDTXMania.ConfigIni.nPlaySpeed < CConstants.PLAYSPEED_MAX)
            {
                bIsTrainingMode = true;
                tChangePlaySpeed(1);
            }
        }

        if (!CDTXMania.ConfigIni.bReverse.Drums && keyboard.bKeyPressing(SlimDXKey.PageUp))
        {
            if (!sw.IsRunning)
            {
                sw2 = Stopwatch.StartNew();
                sw = Stopwatch.StartNew();
                if (nJudgeLinePosY.Drums > nJudgeLineMinPosY)
                {
                    nJudgeLinePosY.Drums--;
                }
            }
            else
            {
                if (sw.ElapsedMilliseconds > 10L)
                {
                    if (sw2.IsRunning)
                    {
                        if (sw2.ElapsedMilliseconds > 100L)
                        {
                            sw2.Reset();
                        }
                    }
                    else if (nJudgeLinePosY.Drums > nJudgeLineMinPosY)
                    {
                        nJudgeLinePosY.Drums--;
                    }
                    sw.Reset();
                    sw.Start();
                }
            }
            CDTXMania.ConfigIni.nJudgeLine.Drums = nJudgeLineMaxPosY - nJudgeLinePosY.Drums;
            CDTXMania.stagePerfDrumsScreen.tJudgeLineMovingUpandDown();
        }
        if (!CDTXMania.ConfigIni.bReverse.Drums && keyboard.bKeyPressing(SlimDXKey.PageDown))
        {
            if (!sw.IsRunning)
            {
                sw2 = Stopwatch.StartNew();
                sw = Stopwatch.StartNew();
                if (nJudgeLinePosY.Drums < nJudgeLineMaxPosY)
                {
                    nJudgeLinePosY.Drums++;
                }
            }
            else if (sw.ElapsedMilliseconds > 10L)
            {
                if (sw2.IsRunning)
                {
                    if (sw2.ElapsedMilliseconds > 100L)
                    {
                        sw2.Reset();
                    }
                }
                else if (nJudgeLinePosY.Drums < nJudgeLineMaxPosY)
                {
                    nJudgeLinePosY.Drums++;
                }
                sw.Reset();
                sw.Start();
            }
            CDTXMania.ConfigIni.nJudgeLine.Drums = nJudgeLineMaxPosY - nJudgeLinePosY.Drums;
            CDTXMania.stagePerfDrumsScreen.tJudgeLineMovingUpandDown();
        }

        if (keyboard.bKeyPressing(SlimDXKey.NumberPad8))
        {
            if (!sw.IsRunning)
            {
                sw2 = Stopwatch.StartNew();
                sw = Stopwatch.StartNew();
                if (nShutterInPosY.Drums > 0)
                {
                    nShutterInPosY.Drums--;
                }
            }
            else if (sw.ElapsedMilliseconds > 10L)
            {
                if (sw2.IsRunning)
                {
                    if (sw2.ElapsedMilliseconds > 100L)
                    {
                        sw2.Reset();
                    }
                }
                else if (nShutterInPosY.Drums > 0)
                {
                    nShutterInPosY.Drums--;
                }
                sw.Reset();
                sw.Start();
            }
            CDTXMania.ConfigIni.nShutterInSide.Drums = nShutterInPosY.Drums;
        }
        if (keyboard.bKeyPressing(SlimDXKey.NumberPad2))
        {
            if (!sw.IsRunning)
            {
                sw2 = Stopwatch.StartNew();
                sw = Stopwatch.StartNew();
                if (nShutterInPosY.Drums < 100)
                {
                    if (nShutterInPosY.Drums + nShutterOutPosY.Drums <= 99)
                        nShutterInPosY.Drums++;
                }
            }
            else if (sw.ElapsedMilliseconds > 10L)
            {
                if (sw2.IsRunning)
                {
                    if (sw2.ElapsedMilliseconds > 100L)
                    {
                        sw2.Reset();
                    }
                }
                else if (nShutterInPosY.Drums < 100)
                {
                    if (nShutterInPosY.Drums + nShutterOutPosY.Drums <= 99)
                        nShutterInPosY.Drums++;
                }
                sw.Reset();
                sw.Start();
            }
            CDTXMania.ConfigIni.nShutterInSide = nShutterInPosY;
        }
        if (keyboard.bKeyPressing(SlimDXKey.NumberPad4))
        {
            if (!sw.IsRunning)
            {
                sw2 = Stopwatch.StartNew();
                sw = Stopwatch.StartNew();
                if (nShutterOutPosY.Drums < 100)
                {
                    if (nShutterInPosY.Drums + nShutterOutPosY.Drums <= 99)
                        nShutterOutPosY.Drums++;
                }
            }
            else if (sw.ElapsedMilliseconds > 10L)
            {
                if (sw2.IsRunning)
                {
                    if (sw2.ElapsedMilliseconds > 100L)
                    {
                        sw2.Reset();
                    }
                }
                else if (nShutterOutPosY.Drums < 100)
                {
                    if (nShutterInPosY.Drums + nShutterOutPosY.Drums <= 99)
                        nShutterOutPosY.Drums++;
                }
                sw.Reset();
                sw.Start();
            }
            CDTXMania.ConfigIni.nShutterOutSide.Drums = nShutterOutPosY.Drums;
        }
        if (keyboard.bKeyPressing(SlimDXKey.NumberPad6))
        {
            if (!sw.IsRunning)
            {
                sw2 = Stopwatch.StartNew();
                sw = Stopwatch.StartNew();
                if (nShutterOutPosY.Drums > 0)
                {
                    nShutterOutPosY.Drums--;
                }
            }
            else if (sw.ElapsedMilliseconds > 10L)
            {
                if (sw2.IsRunning)
                {
                    if (sw2.ElapsedMilliseconds > 100L)
                    {
                        sw2.Reset();
                    }
                }
                else if (nShutterOutPosY.Drums > 0)
                {
                    nShutterOutPosY.Drums--;
                }
                sw.Reset();
                sw.Start();
            }
            CDTXMania.ConfigIni.nShutterOutSide.Drums = nShutterOutPosY.Drums;
        }

        if (keyboard.bKeyPressed(SlimDXKey.F5))
        {
            video.CycleMovieMode();
        }
        if (keyboard.bKeyPressed(SlimDXKey.F6))
        {
            CConfigIni configIni = CDTXMania.ConfigIni;
            configIni.nInfoType++;
            if (CDTXMania.ConfigIni.nInfoType >= 2)
            {
                CDTXMania.ConfigIni.nInfoType = 0;
            }
        }
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
    protected void tUpdateAndDraw_WailingBonus()
    {
        if ((ePhaseID != EPhase.PERFORMANCE_STAGE_FAILED) && (ePhaseID != EPhase.PERFORMANCE_STAGE_FAILED_END))
        {
            actWailingBonus.OnUpdateAndDraw();
        }
    }

    protected void tUpdateAndDraw_GuitarBonus()
    {
        if ((ePhaseID != EPhase.PERFORMANCE_STAGE_FAILED) && (ePhaseID != EPhase.PERFORMANCE_STAGE_FAILED_END))
        {
            actGuitarBonus.OnUpdateAndDraw();
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
    

    protected void tUpdateAndDraw_Combo()
    {
        actCombo.OnUpdateAndDraw();
    }
    protected void tUpdateAndDraw_Score()
    {
        actScore.OnUpdateAndDraw();
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
                    UpdateChip_PatternOnly_Drums(configIni, ref dTX, ref pChip);
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
                    }
                    break;

                case EChannel.Bass_NoChip:
                    if (!pChip.bHit && (pChip.nDistanceFromBar.Bass < 0))
                    {
                        pChip.bHit = true;
                        SetEmptyPickSound(EInstrumentPart.BASS, pChip);
                        pChip.nChannelNumber = EChannel.Bass_Open;
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

    protected void DrawChips(EInstrumentPart ePlayMode)
    {
        bool drawsChips = ePhaseID is not (EPhase.PERFORMANCE_STAGE_FAILED or EPhase.PERFORMANCE_STAGE_FAILED_END)
                          && timeline.TopChip != -1 && timeline.TopChip < timeline.Chips.Count;

        CDTX dTX = CDTXMania.DTX;
        CConfigIni configIni = CDTXMania.ConfigIni;

        if (drawsChips)
        {
            for (int i = timeline.TopChip; i < dTX.listChip.Count; i++)
            {
                CChip pChip = dTX.listChip[i];
                if (IsBeyondDrawRange(pChip)) break;

                if (pChip.nChannelNumber == EChannel.BarLine)
                {
                    DrawChip_BarLine(configIni, ref dTX, ref pChip);
                }
                else if (pChip.nChannelNumber == EChannel.BeatLine)
                {
                    DrawBeatLine(ePlayMode, pChip);
                }
            }
        }

        tDraw_LoopLines();

        if (!drawsChips)
        {
            return;
        }

        if (ePlayMode == EInstrumentPart.DRUMS)
        {
            for (int i = timeline.TopChip; i < dTX.listChip.Count; i++)
            {
                CChip pChip = dTX.listChip[i];
                if (IsBeyondDrawRange(pChip)) break;

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
                        DrawChip_PatternOnly_Drums(configIni, ref dTX, ref pChip);
                        break;
                }
            }
        }

        for (int i = timeline.TopChip; i < dTX.listChip.Count; i++)
        {
            CChip pChip = dTX.listChip[i];
            if (IsBeyondDrawRange(pChip)) break;

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
                    DrawChip_Drums(configIni, ref dTX, ref pChip);
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
                    DrawChip_GuitarBass(configIni, ref dTX, ref pChip, EInstrumentPart.GUITAR);
                    break;

                case EChannel.Guitar_Wailing:
                    DrawChip_Guitar_Wailing(configIni, ref dTX, ref pChip);
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
                    DrawChip_GuitarBass(configIni, ref dTX, ref pChip, EInstrumentPart.BASS);
                    break;

                case EChannel.Bass_Wailing:
                    DrawChip_Bass_Wailing(configIni, ref dTX, ref pChip);
                    break;
            }
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

    //mode: 1 hidden, 2 sudden, 3 both, 4 stealth. Both fades are 50 units long
    protected static void ApplyHiddenSudden(CChip pChip, float distance, int mode, int suddenFrom, int hiddenFrom, double fadeDivisor)
    {
        if (mode is 2 or 3)
        {
            if (distance < suddenFrom)
            {
                pChip.bVisible = true;
                pChip.nTransparency = 0xff;
            }
            else if (distance < suddenFrom + 50)
            {
                pChip.bVisible = true;
                pChip.nTransparency = 0xff - (int)((distance - suddenFrom) * 255.0 / fadeDivisor);
            }
            else
            {
                pChip.bVisible = false;
                pChip.nTransparency = 0;
            }
        }

        if (mode is 1 or 3)
        {
            if (distance < hiddenFrom)
            {
                pChip.bVisible = false;
            }
            else if (distance < hiddenFrom + 50)
            {
                pChip.bVisible = true;
                pChip.nTransparency = (int)((distance - hiddenFrom) * 255.0 / fadeDivisor);
            }
        }

        if (mode == 4)
        {
            pChip.bVisible = false;
        }
    }

    //distances are worked out by UpdateChips earlier in the frame
    private static bool IsBeyondDrawRange(CChip pChip) =>
        Math.Min(Math.Min(pChip.nDistanceFromBar.Drums, pChip.nDistanceFromBar.Guitar), pChip.nDistanceFromBar.Bass) > 600;

    private void DrawBeatLine(EInstrumentPart ePlayMode, CChip pChip)
    {
        CConfigIni configIni = CDTXMania.ConfigIni;
        if ((ePlayMode == EInstrumentPart.DRUMS) && (configIni.nLaneDisp.Drums == 0 || configIni.nLaneDisp.Drums == 1) && pChip.bVisible && (txChip != null))
        {
            int l_drumPanelWidth = 0x22f;
            int l_xOffset = 0;
            if (configIni.eNumOfLanes.Drums == EType.B)
            {
                l_drumPanelWidth = 0x207;
            }
            else if (CDTXMania.ConfigIni.eNumOfLanes.Drums == EType.C)
            {
                l_drumPanelWidth = 447;
                l_xOffset = 72;
            }

            txChip.tDraw2D(0x127 + l_xOffset, configIni.bReverse.Drums ? ((nJudgeLinePosY.Drums + pChip.nDistanceFromBar.Drums) - 1) : ((nJudgeLinePosY.Drums - pChip.nDistanceFromBar.Drums) - 1), new RectangleF(0, 772, l_drumPanelWidth, 2));
        }
    }

    protected bool tDraw_LoopLines()
    {
        if (LoopBeginMs != -1)
        {
            tDraw_LoopLine(CDTXMania.ConfigIni, false);
            if (LoopEndMs != -1)
            {
                tDraw_LoopLine(CDTXMania.ConfigIni, true);
            }
        }
        return false;
    }


    protected virtual void UpdateChip_Drums(CConfigIni configIni, ref CDTX dTX, ref CChip pChip) { }
    protected virtual void UpdateChip_PatternOnly_Drums(CConfigIni configIni, ref CDTX dTX, ref CChip pChip) { }
    protected virtual void UpdateChip_GuitarBass(CConfigIni configIni, ref CDTX dTX, ref CChip pChip, EInstrumentPart inst) { }  // t進行描画_チップ_ギターベース
    
    protected virtual void UpdateChip_GuitarBass_Wailing(CConfigIni configIni, ref CDTX dTX, ref CChip pChip, EInstrumentPart inst)
    {
        GuitarPerformance? guitar = GuitarFor(inst);
        if (guitar == null)
        {
            pChip.bHit = true;
            return;
        }

        int indexInst = (int)inst;
        ApplyHiddenSudden(pChip, pChip.nDistanceFromBar[indexInst], CDTXMania.ConfigIni.nHidSud[indexInst], 250, 150, 75.0);
        guitar.UpdateWailingMarker(pChip);
        ApplyGuitarEvents(guitar);
    }
    protected virtual void UpdateChip_Guitar_Wailing(CConfigIni configIni, ref CDTX dTX, ref CChip pChip)
    {
        UpdateChip_GuitarBass_Wailing(configIni, ref dTX, ref pChip, EInstrumentPart.GUITAR);
    }
    protected abstract void UpdateChip_FillIn(CConfigIni configIni, ref CDTX dTX, ref CChip pChip);  // t進行描画_チップ_フィルイン
    protected abstract void UpdateChip_BarLine(CConfigIni configIni, ref CDTX dTX, ref CChip pChip);
    protected abstract void tDraw_LoopLine(CConfigIni configIni, bool bIsEnd);
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

    //a screen overrides the ones it draws
    protected virtual void DrawChip_Drums(CConfigIni configIni, ref CDTX dTX, ref CChip pChip) { }
    protected virtual void DrawChip_PatternOnly_Drums(CConfigIni configIni, ref CDTX dTX, ref CChip pChip) { }
    protected virtual void DrawChip_GuitarBass(CConfigIni configIni, ref CDTX dTX, ref CChip pChip, EInstrumentPart inst) { }
    protected virtual void DrawChip_Guitar_Wailing(CConfigIni configIni, ref CDTX dTX, ref CChip pChip) { }
    protected virtual void DrawChip_Bass_Wailing(CConfigIni configIni, ref CDTX dTX, ref CChip pChip) { }
    protected virtual void DrawChip_BarLine(CConfigIni configIni, ref CDTX dTX, ref CChip pChip) { }
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
    protected void tUpdateAndDraw_LaneFlushD()
    {
        if ((ePhaseID != EPhase.PERFORMANCE_STAGE_FAILED) && (ePhaseID != EPhase.PERFORMANCE_STAGE_FAILED_END))
        {
            actLaneFlushD.OnUpdateAndDraw();
        }
    }
    protected void tUpdateAndDraw_LaneFlushGB()
    {
        if (CDTXMania.ConfigIni.bGuitarEnabled)
        {
            actLaneFlushGB.OnUpdateAndDraw();
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

    protected void DrawScore()
    {
        if (CDTXMania.ConfigIni.bShowScore)
        {
            tUpdateAndDraw_Score();
        }
    }

    protected void AddLegacyPart(string group, string name, int layer, Action draw)
    {
        LegacyDrawable part = LayoutGroup(group).AddChild(new LegacyDrawable(name, draw));
        part.renderOrder = layer;
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
        #region [ スクロール速度変更 ]
        if (CDTXMania.Pad.bPressing(inst, EPad.Decide) && CDTXMania.Pad.bPressed(inst, EPad.B))
        {
            CDTXMania.ConfigIni.nScrollSpeed[indexInst] = Math.Min(CDTXMania.ConfigIni.nScrollSpeed[indexInst] + 1, 0x7cf);
        }
        if (CDTXMania.Pad.bPressing(inst, EPad.Decide) && CDTXMania.Pad.bPressed(inst, EPad.R))
        {
            CDTXMania.ConfigIni.nScrollSpeed[indexInst] = Math.Max(CDTXMania.ConfigIni.nScrollSpeed[indexInst] - 1, 0);
        }
        #endregion

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

    //the Restart key restarts via the loading screen, which would rebuild the layout
    public void tRestartForPreview()
    {
        tJumpInSong(0);

        //nobody asked for this restart, so clear the score
        foreach (PerformanceData data in partData)
        {
            data.Reset(bIsAutoPlay, CDTXMania.ConfigIni.nRisky);
        }
    }

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