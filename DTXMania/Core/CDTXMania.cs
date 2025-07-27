using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Windows.Forms;
using DTXMania.SongDb;
using FDK;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.D3D9;
using Hexa.NET.ImGuizmo;
using SampleFramework;
using SharpDX;
using SharpDX.Direct3D9;
using ImGui = Hexa.NET.ImGui.ImGui;
using Point = System.Drawing.Point;
using ResourceManager = DTXMania.UI.ResourceManager;
using Vector2 = System.Numerics.Vector2;

//#if INSPECTOR
using DTXMania.UI;
using DTXMania.UI.Drawable;
using DTXMania.UI.Inspector;
//#endif

using DTXMania.UI.Skin;
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;

namespace DTXMania.Core;

internal class CDTXMania : Game
{
    // プロパティ
    //these get set when initializing the game
    public static string VERSION_DISPLAY; // = "DTX:NX:A:A:2024051900";
    public static string VERSION; // = "v1.4.2 20240519";

    public const string D3DXDLL = "d3dx9_43.dll"; // June 2010

    public static CDTXMania app { get; private set; }

    public static CCharacterConsole actDisplayString // act文字コンソール
    {
        get;
        private set;
    }

    public static bool bCompactMode { get; private set; }
    public static CConfigIni ConfigIni { get; set; }

    /// <summary>
    /// The shared Rich Presence integration instance, or <see langword="null"/> if it is disabled.
    /// </summary>
    public static CDiscordRichPresence DiscordRichPresence { get; private set; }

    //current language
    public static bool isJapanese { get; private set; }

    public static void SetLanguage(bool jp)
    {
        isJapanese = jp;

        //todo: implement handling to switch language at runtime
    }

    public static CDTX DTX
    {
        get => dtx;
        set
        {
            if ((dtx != null) && (app != null))
            {
                dtx.OnDeactivate();
                app.mainActivities.Remove(dtx);
            }

            dtx = value;
            if ((dtx != null) && (app != null))
            {
                app.mainActivities.Add(dtx);
            }
        }
    }

    public static CFPS FPS { get; private set; }
    public static CInputManager InputManager { get; private set; }
    public static int nSongDifficulty { get; set; }

    /// <summary>
    /// The <see cref="STHitRanges"/> for all drum chips, except pedals, composed from the confirmed <see cref="CSongListNode"/> and <see cref="CConfigIni"/> settings.
    /// </summary>
    public static STHitRanges stDrumHitRanges
    {
        get
        {
            SongNode parentNode = confirmedSong?.parent;
            if (parentNode?.nodeType == SongNode.ENodeType.BOX)
                return STHitRanges.tCompose(parentNode.stDrumHitRanges, ConfigIni.stDrumHitRanges);

            return ConfigIni.stDrumHitRanges;
        }
    }

    /// <summary>
    /// The <see cref="STHitRanges"/> for all drum pedal chips, composed from the confirmed <see cref="CSongListNode"/> and <see cref="CConfigIni"/> settings.
    /// </summary>
    public static STHitRanges stDrumPedalHitRanges
    {
        get
        {
            SongNode parentNode = confirmedSong?.parent;
            if (parentNode?.nodeType == SongNode.ENodeType.BOX)
                return STHitRanges.tCompose(parentNode.stDrumPedalHitRanges, ConfigIni.stDrumPedalHitRanges);

            return ConfigIni.stDrumPedalHitRanges;
        }
    }

    /// <summary>
    /// The <see cref="STHitRanges"/> for guitar chips, composed from the confirmed <see cref="CSongListNode"/> and <see cref="CConfigIni"/> settings.
    /// </summary>
    public static STHitRanges stGuitarHitRanges
    {
        get
        {
            SongNode parentNode = confirmedSong?.parent;
            if (parentNode?.nodeType == SongNode.ENodeType.BOX)
                return STHitRanges.tCompose(parentNode.stGuitarHitRanges, ConfigIni.stGuitarHitRanges);

            return ConfigIni.stGuitarHitRanges;
        }
    }

    /// <summary>
    /// The <see cref="STHitRanges"/> for bass guitar chips, composed from the confirmed <see cref="CSongListNode"/> and <see cref="CConfigIni"/> settings.
    /// </summary>
    public static STHitRanges stBassHitRanges
    {
        get
        {
            SongNode parentNode = confirmedSong?.parent;
            if (parentNode?.nodeType == SongNode.ENodeType.BOX)
                return STHitRanges.tCompose(parentNode.stBassHitRanges, ConfigIni.stBassHitRanges);

            return ConfigIni.stBassHitRanges;
        }
    }

    public static CPad Pad { get; private set; }

    public static Input Input { get; private set; }
    public static Random Random { get; private set; }
    public static CSkin Skin { get; private set; }
    
    public static CStageSongSelection stageSongSelection => StageManager.stageSongSelection;
    public static CStagePerfGuitarScreen stagePerfGuitarScreen => StageManager.stagePerfGuitarScreen;
    public static CStagePerfDrumsScreen stagePerfDrumsScreen => StageManager.stagePerfDrumsScreen;
    
    public static StageManager StageManager { get; private set; }
    public static SkinManager SkinManager { get; private set; }

    public static ResourceManager Resources { get; private set; }

    public static CSongManager SongManager
    {
        get;
        set; // 2012.1.26 yyagi private解除 CStage起動でのdesirialize読み込みのため
    }

    public static SongDb.SongDb SongDb { get; private set; }

    public static CActFlushGPU actFlushGPU { get; private set; }
    public static CSoundManager SoundManager { get; private set; }

    public static string executableDirectory { get; private set; }
    public static string strCompactModeFile { get; private set; }
    public static CTimer Timer { get; private set; }
    public static Format TextureFormat = Format.A8R8G8B8;
    public bool bApplicationActive { get; private set; }
    public bool changeVSyncModeOnNextFrame { get; set; }
    public bool changeFullscreenModeOnNextFrame { get; set; }

    private ImGuiContextPtr context;

    public Device Device => GraphicsDeviceManager.Direct3D9.Device;

    private static Size currentClientSize // #23510 2010.10.27 add yyagi to keep current window size
    {
        get;
        set;
    }

    //		public static CTimer ct;
    public IntPtr WindowHandle => Window.Handle; // 2012.10.24 yyagi; to add ASIO support
    public static CDTXVmode DTXVmode; // #28821 2014.1.23 yyagi
    public static CDTX2WAVmode DTX2WAVmode;

    public static CCommandParse CommandParse;

    //fork
    public static STDGBVALUE<List<int>> listAutoGhostLag = new();
    public static STDGBVALUE<List<int>> listTargetGhsotLag = new();

    public static STDGBVALUE<CScoreIni.CPerformanceEntry> listTargetGhostScoreData = new();


    //stuff to render game inside window
    private Texture gameRenderTargetTexture;
    private Surface gameRenderTargetSurface;
    private Surface mainRenderTarget;
    public static bool renderGameToSurface = false;
    
    //new
    public static UIGroup persistentUIGroup { get; private set; } = new("PersistentUIGroup");
    public static GitaDoraTransition gitadoraTransition { get; private set; }
    public static LogWindow logWindow { get; private set; }

    // Constructor
    public CDTXMania()
    {
        app = this;

        void SafeInitialize(string name, Action action)
        {
            try
            {
                Trace.TraceInformation($"Initializing {name}");
                action();
            }
            catch (Exception e)
            {
                Trace.TraceError($"Failed to initialize {name}: {e}");
                MessageBox.Show($"Failed to initialize {name}: {e}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        //Update version information
        Assembly assembly = Assembly.GetExecutingAssembly();
        DateTime? buildDate = GetAssemblyBuildDateTime() ?? DateTime.UnixEpoch;
        VERSION = $"v{assembly.GetName().Version.ToString().Substring(0, 5)} ({buildDate:yyyyMMdd})";
        VERSION_DISPLAY = $"DTX:NX:A:A:{buildDate:yyyyMMdd}00";

        #region [ Determine strEXE folder ]

        //-----------------
        // BEGIN #23629 2010.11.13 from: デバッグ時は Application.ExecutablePath が ($SolutionDir)/bin/x86/Debug/ などになり System/ の読み込みに失敗するので、カレントディレクトリを採用する。（プロジェクトのプロパティ→デバッグ→作業ディレクトリが有効になる）
#if DEBUG
        executableDirectory = Environment.CurrentDirectory + @"\";
#else
        executableDirectory =
            Path.GetDirectoryName(Application.ExecutablePath) + @"\";	// #23629 2010.11.9 yyagi: set correct pathname where DTXManiaGR.exe is.
#endif
        // END #23629 2010.11.13 from
        //-----------------

        #endregion

        #region [ Read Config.ini ]

        //---------------------
        ConfigIni = new CConfigIni();
        string path = executableDirectory + "Config.ini";
        if (File.Exists(path))
        {
            try
            {
                ConfigIni.tReadFromFile(path);
            }
            catch
            {
                //ConfigIni = new CConfigIni();	// 存在してなければ新規生成
            }
        }

        Window.EnableSystemMenu = ConfigIni.bIsEnabledSystemMenu; // #28200 2011.5.1 yyagi
        // 2012.8.22 Config.iniが無いときに初期値が適用されるよう、この設定行をifブロック外に移動

        //---------------------

        #endregion

        #region [ Start output log ]

        //---------------------
        Trace.AutoFlush = true;
        if (ConfigIni.bOutputLogs)
        {
            try
            {
                Trace.Listeners.Add(new CTraceLogListener(new StreamWriter("DTXManiaLog.txt", false, Encoding.Unicode)));
                logWindow = new LogWindow();
                Trace.Listeners.Add(logWindow);
            }
            catch (UnauthorizedAccessException) // #24481 2011.2.20 yyagi
            {
                int c = isJapanese ? 0 : 1;
                string[] mes_writeErr =
                {
                    "DTXManiaLog.txtへの書き込みができませんでした。書き込みできるようにしてから、再度起動してください。",
                    "Failed to write DTXManiaLog.txt. Please set it writable and try again."
                };
                MessageBox.Show(mes_writeErr[c], "DTXMania boot error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }
        }

        Trace.WriteLine("");
        Trace.WriteLine("DTXMania powered by YAMAHA Silent Session Drums");
        Trace.WriteLine($"Release: {VERSION}");
        Trace.WriteLine("");
        Trace.TraceInformation("----------------------");
        Trace.TraceInformation("■ アプリケーションの初期化");
        Trace.TraceInformation("OS Version: " + Environment.OSVersion);
        Trace.TraceInformation("ProcessorCount: " + Environment.ProcessorCount);
        Trace.TraceInformation("CLR Version: " + Environment.Version);
        //---------------------

        #endregion

        SafeInitialize("DTXVMode, DTX2WAVMode, CommandParse", () =>
        {
            DTXVmode = new CDTXVmode
            {
                Enabled = false
            };

            DTX2WAVmode = new CDTX2WAVmode();
            CommandParse = new CCommandParse();
        });

        #region [ Detect compact mode、or start as DTXViewer/DTX2WAV ]

        bCompactMode = false;
        strCompactModeFile = "";
        string appName = "DTXManiaNX";
        string[] commandLineArgs = Environment.GetCommandLineArgs();
        if ((commandLineArgs != null) && (commandLineArgs.Length > 1))
        {
            bCompactMode = true;
            string arg = "";

            for (int i = 1; i < commandLineArgs.Length; i++)
            {
                if (i != 1)
                {
                    arg += " " + "\"" + commandLineArgs[i] + "\"";
                }
                else
                {
                    arg += commandLineArgs[i];
                }
            }

            Trace.TraceInformation("Parsing arguments: {0}。", arg);
            CommandParse.ParseArguments(arg, ref DTXVmode, ref DTX2WAVmode);
            if (DTXVmode.Enabled)
            {
                DTXVmode.Refreshed = false; // 初回起動時は再読み込みに走らせない
                strCompactModeFile = DTXVmode.filename;
                switch (DTXVmode.soundDeviceType) // サウンド再生方式の設定
                {
                    case ESoundDeviceType.DirectSound:
                        ConfigIni.nSoundDeviceType = (int)CConfigIni.ESoundDeviceTypeForConfig.ACM;
                        break;
                    case ESoundDeviceType.ExclusiveWASAPI:
                        ConfigIni.nSoundDeviceType = (int)CConfigIni.ESoundDeviceTypeForConfig.WASAPI;
                        break;
                    case ESoundDeviceType.SharedWASAPI:
                        ConfigIni.nSoundDeviceType = (int)CConfigIni.ESoundDeviceTypeForConfig.WASAPI_Share;
                        break;
                    case ESoundDeviceType.ASIO:
                        ConfigIni.nSoundDeviceType = (int)CConfigIni.ESoundDeviceTypeForConfig.ASIO;
                        ConfigIni.nASIODevice = DTXVmode.nASIOdevice;
                        break;
                }

                ConfigIni.bVerticalSyncWait = DTXVmode.VSyncWait;
                ConfigIni.bTimeStretch = DTXVmode.TimeStretch;
                if (DTXVmode.GRmode)
                {
                    ConfigIni.bDrumsEnabled = false;
                    ConfigIni.bGuitarEnabled = true;
                }
                else
                {
                    //Both in Original DTXMania, but we don't support that
                    ConfigIni.bDrumsEnabled = true;
                    ConfigIni.bGuitarEnabled = false;
                }

                //Disable Movie and FullScreen mode
                ConfigIni.bFullScreenMode = false;
                ConfigIni.nMovieMode = 2;

                //Set windows size to selected Window Size and set its position to a fixed location
                ConfigIni.nWindowWidth = DTXVmode.widthResolution;
                ConfigIni.nWindowHeight = DTXVmode.heightResolution;
                ConfigIni.n初期ウィンドウ開始位置X = 5;
                ConfigIni.n初期ウィンドウ開始位置Y = 100;

                //Disable Reverse options in DTXVMode
                ConfigIni.bReverse.Drums = false;
                ConfigIni.bReverse.Guitar = false;
                ConfigIni.bReverse.Bass = false;

                //Turn off all random mode settings in DTXVMode
                ConfigIni.eRandom.Drums = ERandomMode.OFF;
                ConfigIni.eRandom.Guitar = ERandomMode.OFF;
                ConfigIni.eRandom.Bass = ERandomMode.OFF;
                ConfigIni.eRandomPedal.Drums = ERandomMode.OFF;

                //Set scroll speed to fixed values
                ConfigIni.nScrollSpeed.Drums = 4; //2.0
                ConfigIni.nScrollSpeed.Guitar = 4; //2.0
                ConfigIni.nScrollSpeed.Bass = 4; //2.0

                //全オート
                for (int i = 0; i < (int)ELane.MAX; i++)
                {
                    ConfigIni.bAutoPlay[i] = true;
                }
            }
            else if (DTX2WAVmode.Enabled)
            {
                strCompactModeFile = DTX2WAVmode.dtxfilename;

                #region [ FDKへの録音設定 ]

                CSoundManager.strRecordInputDTXfilename = DTX2WAVmode.dtxfilename;
                CSoundManager.strRecordOutFilename = DTX2WAVmode.outfilename;
                CSoundManager.strRecordFileType = DTX2WAVmode.Format.ToString();
                CSoundManager.nBitrate = DTX2WAVmode.bitrate;
                for (int i = 0; i < (int)CSound.EInstType.Unknown; i++)
                {
                    CSoundManager.nMixerVolume[i] = DTX2WAVmode.nMixerVolume[i];
                }

                ConfigIni.nMasterVolume =
                    DTX2WAVmode.nMixerVolume[(int)CSound.EInstType.Unknown]; // [5](Unknown)のところにMasterVolumeが入ってくるので注意
                // CSound管理.nMixerVolume[5]は、結局ここからは変更しないため、
                // 事実上初期値=100で固定。

                #endregion

                #region [ 録音用の本体設定 ]

                // 本体プロセスの優先度を少し上げる (最小化状態で動作させると、処理性能が落ちるようなので
                // → ほとんど効果がなかったので止めます
                //Process thisProcess = System.Diagnostics.Process.GetCurrentProcess();
                //thisProcess.PriorityClass = ProcessPriorityClass.AboveNormal;

                // エンコーダーのパス設定 (=DLLフォルダ)
                CSoundManager.strEncoderPath = Path.Combine(executableDirectory, "DLL");

                ConfigIni.nSoundDeviceType = (int)CConfigIni.ESoundDeviceTypeForConfig.WASAPI;
                ConfigIni.bEventDrivenWASAPI = false;

                ConfigIni.bVerticalSyncWait = false;
                ConfigIni.bTimeStretch = false;

                //Both in Original DTXMania, but we don't support that
                ConfigIni.bDrumsEnabled = true;
                ConfigIni.bGuitarEnabled = false;

                ConfigIni.bFullScreenMode = false;

                //全オート
                for (int i = 0; i < (int)ELane.MAX; i++)
                {
                    ConfigIni.bAutoPlay[i] = true;
                }

                //FillInオフ, 歓声オフ
                ConfigIni.bFillInEnabled = false;
                ConfigIni.b歓声を発声する = false; // bAudience
                //ストイックモード
                ConfigIni.bストイックモード = false; // bStoicMode
                //チップ非表示
                ConfigIni.nHidSud.Drums = 4; // ESudHidInv.FullInv;
                ConfigIni.nHidSud.Guitar = 4; // ESudHidInv.FullInv;
                ConfigIni.nHidSud.Bass = 4; // ESudHidInv.FullInv;

                // Dark=Full
                ConfigIni.eDark = EDarkMode.FULL;

                //多重再生数=4
                ConfigIni.nPoliphonicSounds = 4;

                //再生速度x1
                ConfigIni.nPlaySpeed = 20;

                //メトロノーム音量0
                //CDTXMania.ConfigIni.eClickType.Value = EClickType.Off;
                //CDTXMania.ConfigIni.nClickHighVolume.Value = 0;
                //CDTXMania.ConfigIni.nClickLowVolume.Value = 0;

                //自動再生音量=100
                ConfigIni.n自動再生音量 = 100; // nAutoVolume
                ConfigIni.n手動再生音量 = 100; // nChipVolume

                //マスターボリューム100
                //CDTXMania.ConfigIni.nMasterVolume.Value = 100;	// DTX2WAV側から設定するので、ここでは触らない

                //StageFailedオフ
                ConfigIni.bSTAGEFAILEDEnabled = false;

                //グラフ無効
                ConfigIni.bGraph有効.Drums = false;
                ConfigIni.bGraph有効.Guitar = false;
                ConfigIni.bGraph有効.Bass = false;

                //コンボ非表示,判定非表示
                ConfigIni.bドラムコンボ文字の表示 = false; // bドラムコンボ文字の表示
                ConfigIni.n表示可能な最小コンボ数.Drums = 0;
                ConfigIni.n表示可能な最小コンボ数.Guitar = 0; // CDTXMania.ConfigIni.bDisplayCombo.Guitar.Value = false;
                ConfigIni.n表示可能な最小コンボ数.Bass = 0; // CDTXMania.ConfigIni.bDisplayCombo.Bass.Value = false;
                ConfigIni.bDisplayJudge.Drums = false;
                ConfigIni.bDisplayJudge.Guitar = false;
                ConfigIni.bDisplayJudge.Bass = false;


                //デバッグ表示オフ
                //CDTXMania.ConfigIni.b演奏情報を表示する = false;
                ConfigIni.bHidePerformanceInformation = true; // bDebugInfo = false

                //BGAオフ, AVIオフ
                ConfigIni.bBGAEnabled = false;
                ConfigIni.bAVIEnabled = false;

                //BGMオン、チップ音オン
                ConfigIni.bBGM音を発声する = true; // bBGMPlay
                ConfigIni.bドラム打音を発声する = true; // bDrumsHitSound

                //パート強調オフ
                //CDTXMania.ConfigIni.bEmphasizePlaySound.Drums.Value = false;
                //CDTXMania.ConfigIni.bEmphasizePlaySound.Guitar.Value = false;
                //CDTXMania.ConfigIni.bEmphasizePlaySound.Bass.Value = false;

                // パッド入力等、基本操作の無効化 (ESCを除く)
                //CDTXMania.ConfigIni.KeyAssign[][];

                #endregion
            }
            else // 通常のコンパクトモード
            {
                strCompactModeFile = commandLineArgs[1];
            }

            if (!File.Exists(strCompactModeFile)) // #32985 2014.1.23 yyagi 
            {
                Trace.TraceError("The file specified in compact mode cannot be found. Terminating DTXMania. [{0}]",
                    strCompactModeFile);
#if DEBUG
                Environment.Exit(-1);
#else
                if (strCompactModeFile == "")  // DTXMania未起動状態で、DTXCで再生停止ボタンを押した場合は、何もせず終了
                {
                    Environment.Exit(-1);
                }
                else
                {
                    throw new FileNotFoundException("The file specified in compact mode cannot be found. Terminating DTXMania.", strCompactModeFile);
                }
#endif
            }

            if (DTXVmode.Enabled)
            {
                Trace.TraceInformation("Start in DTXV mode. [{0}]", strCompactModeFile);
                appName = "DTXViewerNX";
            }
            else if (DTX2WAVmode.Enabled)
            {
                Trace.TraceInformation("Start in DTX2WAV mode. [{0}]", strCompactModeFile);
                DTX2WAVmode.SendMessage2DTX2WAV("BOOT");
                appName = "DTX2WAV";
            }
            else
            {
                Trace.TraceInformation("Start in compact mode. [{0}]", strCompactModeFile);
                appName = "DTXManiaNX (Compact)";
            }
        }
        else
        {
            Trace.TraceInformation("Start in normal mode。");
        }

        #endregion
        
        #region [ Initialize window ]

        //---------------------
        string process64bitText = Environment.Is64BitProcess ? "x64(64-bit) " : "";
        strWindowTitle = appName + " " + process64bitText + VERSION;
        Window.StartPosition = FormStartPosition.Manual; // #30675 2013.02.04 ikanick add
        Window.Location = new Point(ConfigIni.n初期ウィンドウ開始位置X, ConfigIni.n初期ウィンドウ開始位置Y); // #30675 2013.02.04 ikanick add

        Window.Text = strWindowTitle;
        Window.ClientSize =
            new Size(ConfigIni.nWindowWidth,
                ConfigIni.nWindowHeight); // #34510 yyagi 2010.10.31 to change window size got from Config.ini
        if (!ConfigIni.bFullScreenExclusive ||
            ConfigIni.bFullScreenMode) // #23510 2010.11.02 yyagi: add; to recover window size in case bootup with fullscreen mode
        {
            currentClientSize = new Size(ConfigIni.nWindowWidth, ConfigIni.nWindowHeight);
        }

        Window.MaximizeBox = true; // #23510 2010.11.04 yyagi: to support maximizing window
        Window.FormBorderStyle =
            FormBorderStyle
                .Sizable; // #23510 2010.10.27 yyagi: changed from FixedDialog to Sizable, to support window resize
        Window.ShowIcon = true;
        base.Window.Icon = Properties.Resources.dtx;
        Window.KeyDown += Window_KeyDown;
        Window.KeyUp += Window_KeyUp;
        Window.KeyPress += Window_KeyPress;
        Window.MouseMove += Window_MouseMove;
        Window.MouseDown += Window_MouseDown;
        Window.MouseUp += Window_MouseUp;
        Window.MouseWheel += Window_MouseWheel;
        Window.MouseDoubleClick += Window_MouseDoubleClick; // #23510 2010.11.13 yyagi: to go fullscreen mode
        Window.ResizeEnd += Window_ResizeEnd; // #23510 2010.11.20 yyagi: to set resized window size in Config.ini
        Window.ApplicationActivated += Window_ApplicationActivated;
        Window.ApplicationDeactivated += Window_ApplicationDeactivated;
        //Add CIMEHook
        Window.Controls.Add(cIMEHook = new CIMEHook());
        //---------------------

        #endregion

        //Init DX9
        SafeInitialize("DX9", () =>
        {
            DeviceSettings settings = new();
            if (ConfigIni.bFullScreenExclusive)
            {
                settings.Windowed = ConfigIni.bWindowMode;
            }
            else
            {
                settings.Windowed = true; // #30666 2013.2.2 yyagi: Fullscreenmode is "Maximized window" mode
            }

            settings.BackBufferWidth = GameWindowSize.Width;
            settings.BackBufferHeight = GameWindowSize.Height;

            settings.EnableVSync = ConfigIni.bVerticalSyncWait;

            GraphicsDeviceManager.ChangeDevice(settings);


            IsFixedTimeStep = false;
            Window.ClientSize = new Size(ConfigIni.nWindowWidth, ConfigIni.nWindowHeight);
            InactiveSleepTime = TimeSpan.FromMilliseconds((float)(ConfigIni.n非フォーカス時スリープms));

            // #23568 2010.11.4 ikanick changed ( 1 -> ConfigIni )
            if (!ConfigIni.bFullScreenExclusive)
            {
                tSwitchFullScreenMode(); // #30666 2013.2.2 yyagi: finalize settings for "Maximized window mode"
            }

            actFlushGPU = new CActFlushGPU();
        });

        DTX = null;

        Resources = new ResourceManager();
        SafeInitialize("SongDb", () =>
        {
            SongDb = new SongDb.SongDb();
            SongDb.StartScan();
        });
        SkinManager = new SkinManager();

        SafeInitialize("Skin", () =>
        {
            Skin = new CSkin(ConfigIni.strSystemSkinSubfolderFullName, ConfigIni.bUseBoxDefSkin);
            ConfigIni.strSystemSkinSubfolderFullName =
                Skin.GetCurrentSkinSubfolderFullName(true); // 旧指定のSkinフォルダが消滅していた場合に備える
        });

        SafeInitialize("Timer", () => { Timer = new CTimer(CTimer.EType.MultiMedia); });

        SafeInitialize("FPS Counter", () => { FPS = new CFPS(); });

        SafeInitialize("Character Console", () =>
        {
            actDisplayString = new CCharacterConsole();
            actDisplayString.OnActivate();
        });

        SafeInitialize("Input Manager (DirectInput, MIDI)", () =>
        {
            InputManager = new CInputManager(Window.Handle);
            foreach (IInputDevice device in InputManager.listInputDevices)
            {
                if ((device.eInputDeviceType == EInputDeviceType.Joystick) &&
                    !ConfigIni.joystickDict.ContainsValue(device.GUID))
                {
                    int key = 0;
                    while (ConfigIni.joystickDict.ContainsKey(key))
                    {
                        key++;
                    }

                    ConfigIni.joystickDict.Add(key, device.GUID);
                }
            }

            foreach (IInputDevice device2 in InputManager.listInputDevices
                         .Where(x => x.eInputDeviceType == EInputDeviceType.Joystick))
            {
                foreach (KeyValuePair<int, string> pair in ConfigIni.joystickDict.Where(pair =>
                             device2.GUID.Equals(pair.Value)))
                {
                    ((CInputJoystick)device2).SetID(pair.Key);
                    break;
                }
            }
        });

        SafeInitialize("Pad", () => { Pad = new CPad(ConfigIni, InputManager); });

        SafeInitialize("Sound Manager", () =>
        {
            ESoundDeviceType soundDeviceType = ConfigIni.nSoundDeviceType switch
            {
                0 => ESoundDeviceType.DirectSound,
                1 => ESoundDeviceType.ASIO,
                2 => ESoundDeviceType.ExclusiveWASAPI,
                3 => ESoundDeviceType.SharedWASAPI,
                _ => ESoundDeviceType.Unknown
            };

            SoundManager = new CSoundManager(Window.Handle,
                soundDeviceType,
                ConfigIni.nWASAPIBufferSizeMs,
                ConfigIni.bEventDrivenWASAPI,
                0,
                ConfigIni.nASIODevice,
                ConfigIni.bUseOSTimer
            );
            AddSoundTypeToWindowTitle();
            CSoundManager.bIsTimeStretch = ConfigIni.bTimeStretch;
            SoundManager.nMasterVolume = ConfigIni.nMasterVolume;

            string strDefaultSoundDeviceBusType = CSoundManager.strDefaultDeviceBusType;
            Trace.TraceInformation($"Bus type of the default sound device = {strDefaultSoundDeviceBusType}");
        });
        
        Random = new Random((int)Timer.nシステム時刻);

        #region [ Initialize Stage ]

        StageManager = new StageManager();

        mainActivities =
        [
            actDisplayString,

            StageManager.stageStartup,
            StageManager.stageTitle,
            StageManager.stageConfig,
            StageManager.stageSongSelection,
            StageManager.stageSongLoading,
            StageManager.stagePerfDrumsScreen,
            StageManager.stagePerfGuitarScreen,
            StageManager.stageResult,
            StageManager.stageChangeSkin,
            StageManager.stageEnd,

            actFlushGPU
        ];

        #endregion

        Input = new Input();

        #region [ Discord Rich Presence ]

        if (ConfigIni.bDiscordRichPresenceEnabled && !bCompactMode)
            DiscordRichPresence = new CDiscordRichPresence(ConfigIni.strDiscordRichPresenceApplicationID);

        #endregion
        
        Trace.TraceInformation("Finished game initialization");

        #region [ Launch First stage ]

        //---------------------
        Trace.TraceInformation("----------------------");
        Trace.TraceInformation("■ Startup");

        StageManager.LoadInitialStage();
        //---------------------

        #endregion
    }


    // Methods

    public void tSwitchFullScreenMode()
    {
        if (ConfigIni != null)
        {
            if (ConfigIni.bFullScreenMode) // #23510 2010.10.27 yyagi: backup current window size before going fullscreen mode
            {
                currentClientSize = Window.ClientSize;
                ConfigIni.nWindowWidth = Window.ClientSize.Width;
                ConfigIni.nWindowHeight = Window.ClientSize.Height;
                //FDK.CTaskBar.ShowTaskBar( false );
            }

            if (ConfigIni.bFullScreenExclusive)
            {
                // Full screen uses DirectX Exclusive mode
                DeviceSettings settings = GraphicsDeviceManager.CurrentSettings.Clone();
                if (ConfigIni.bWindowMode != settings.Windowed)
                {
                    settings.Windowed = ConfigIni.bWindowMode;
                    GraphicsDeviceManager.ChangeDevice(settings);
                    if (ConfigIni.bWindowMode) // #23510 2010.10.27 yyagi: to resume window size from backuped value
                    {
                        Window.ClientSize = new Size(currentClientSize.Width, currentClientSize.Height);
                        //FDK.CTaskBar.ShowTaskBar( true );
                    }
                }
            }
            else
            {
                // Only use windows maximized/restored sizes
                if (ConfigIni.bWindowMode) // #23510 2010.10.27 yyagi: to resume window size from backuped value
                {
                    // #30666 2013.2.2 yyagi Don't use Fullscreen mode becasue NVIDIA GeForce is
                    // tend to delay drawing on Fullscreen mode. So DTXMania uses Maximized window
                    // in spite of using fullscreen mode.
                    app.Window.WindowState = FormWindowState.Normal;
                    app.Window.FormBorderStyle = FormBorderStyle.Sizable;
                    app.Window.WindowState = FormWindowState.Normal;
                    Window.ClientSize = new Size(currentClientSize.Width, currentClientSize.Height);
                    //FDK.CTaskBar.ShowTaskBar( true );
                }
                else
                {
                    app.Window.WindowState = FormWindowState.Normal;
                    app.Window.FormBorderStyle = FormBorderStyle.None;
                    app.Window.WindowState = FormWindowState.Maximized;
                }

                if (ConfigIni.bWindowMode)
                {
                    if (!bMouseCursorShown)
                    {
                        Cursor.Show();
                        bMouseCursorShown = true;
                    }
                }
                else if (bMouseCursorShown)
                {
                    Cursor.Hide();
                    bMouseCursorShown = false;
                }
            }
        }
    }
    
    #region [ #24609 リザルト画像をpngで保存する ] // #24609 2011.3.14 yyagi; to save result screen in case BestRank or HiSkill.

    /// <summary>
    /// リザルト画像のキャプチャと保存。
    /// </summary>
    /// <param name="strFilename">保存するファイル名(フルパス)</param>
    public bool SaveResultScreen(string strFullPath)
    {
        string strSavePath = Path.GetDirectoryName(strFullPath);
        if (!Directory.Exists(strSavePath))
        {
            try
            {
                Directory.CreateDirectory(strSavePath);
            }
            catch
            {
                return false;
            }
        }

        // http://www.gamedev.net/topic/594369-dx9slimdxati-incorrect-saving-surface-to-file/
        using (Surface pSurface = app.Device.GetRenderTarget(0))
        {
            Surface.ToFile(pSurface, strFullPath, ImageFileFormat.Png);
        }

        return true;
    }

    #endregion

    // Game 実装
    protected override void Initialize()
    {
        if (mainActivities != null)
        {
            foreach (CActivity activity in mainActivities)
            {
                activity.OnManagedCreateResources();
            }
        }
    }

    protected override void LoadContent()
    {
        if (ConfigIni.bWindowMode)
        {
            if (!bMouseCursorShown)
            {
                Cursor.Show();
                bMouseCursorShown = true;
            }
        }
        else if (bMouseCursorShown)
        {
            Cursor.Hide();
            bMouseCursorShown = false;
        }

        Device.SetTransform(TransformState.View,
            Matrix.LookAtLH(new Vector3(0f, 0f, (float)(-GameFramebufferSize.Height / 2 * Math.Sqrt(3.0))),
                new Vector3(0f, 0f, 0f), new Vector3(0f, 1f, 0f)));
        Device.SetTransform(TransformState.Projection,
            Matrix.PerspectiveFovLH(CConversion.DegreeToRadian((float)60f),
                ((float)Device.Viewport.Width) / ((float)Device.Viewport.Height), -100f, 100f));
        Device.SetRenderState(RenderState.Lighting, false);
        Device.SetRenderState(RenderState.ZEnable, false);
        Device.SetRenderState(RenderState.AntialiasedLineEnable, false);
        Device.SetRenderState(RenderState.AlphaTestEnable, true);
        Device.SetRenderState(RenderState.AlphaRef, 10);

        Device.SetRenderState(RenderState.MultisampleAntialias, true);
        Device.SetSamplerState(0, SamplerState.MinFilter, TextureFilter.Linear);
        Device.SetSamplerState(0, SamplerState.MagFilter, TextureFilter.Linear);

        Device.SetRenderState(RenderState.AlphaFunc, Compare.Greater);
        Device.SetRenderState(RenderState.AlphaBlendEnable, true);
        Device.SetRenderState(RenderState.SourceBlend, Blend.SourceAlpha);
        Device.SetRenderState(RenderState.DestinationBlend, Blend.InverseSourceAlpha);
        Device.SetTextureStageState(0, TextureStage.AlphaOperation, TextureOperation.Modulate);
        Device.SetTextureStageState(0, TextureStage.AlphaArg1, 2);
        Device.SetTextureStageState(0, TextureStage.AlphaArg2, 1);

        //init imgui
        context = ImGui.CreateContext();

        ImGui.SetCurrentContext(context);
        ImGuiImplD3D9.SetCurrentContext(context);

        var io = ImGui.GetIO();

        ImGui.StyleColorsDark();

        unsafe
        {
            var font = ImGui.GetIO().Fonts.AddFontFromFileTTF(Path.Combine(executableDirectory, "Fonts", "NotoSansCJKjp-Regular.otf"), 16f, ImGui.GetIO().Fonts.GetGlyphRangesJapanese()); 
            ImGui.GetIO().Fonts.Build();
        }

        //todo: fix actual resizing
        //io.DisplaySize = new Vector2(Window.ClientSize.Width, Window.ClientSize.Height);
        io.DisplaySize = new Vector2(GameWindowSize.Width, GameWindowSize.Height);
        io.DisplayFramebufferScale = new Vector2(1, 1);

        //ImGuizmo.SetRect(0, 0, io.DisplaySize.X, io.DisplaySize.Y);
        ImGuizmo.SetRect(0, 0, GameWindowSize.Width, GameWindowSize.Height);

        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        unsafe
        {
            ImGuiImplD3D9.Init(new IDirect3DDevice9Ptr((IDirect3DDevice9*)app.Device.NativePointer));
        }

        ImGuizmo.SetImGuiContext(context);

        gameRenderTargetTexture = new Texture(Device, 1280, 720, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default);
        gameRenderTargetSurface = gameRenderTargetTexture.GetSurfaceLevel(0);

        mainRenderTarget = Device.GetRenderTarget(0);
        
        UpdateFallback();
        DTXTexture.UpdateFallback();

        SongDBStatus songDbStatus = persistentUIGroup.AddChild(new SongDBStatus());
        songDbStatus.position = new Vector3(0, 720, 0);
        songDbStatus.anchor = new SharpDX.Vector2(0.0f, 1.0f);
        
        gitadoraTransition = persistentUIGroup.AddChild(new GitaDoraTransition());
    }

    protected override void UnloadContent()
    {
        gameRenderTargetSurface.Dispose();
        gameRenderTargetTexture.Dispose();
        mainRenderTarget.Dispose();

        ImGuiImplD3D9.InvalidateDeviceObjects();
        ImGuiImplD3D9.Shutdown();

        ImGui.DestroyContext();
        
        persistentUIGroup.Dispose();
        
        if (fallbackTexture != null)
        {
            tReleaseTexture(ref fallbackTexture);
        }
    }

    protected override void OnExiting(EventArgs e)
    {
        CPowerManagement.tEnableMonitorSuspend(); // スリープ抑止状態を解除
        tTerminate();
        base.OnExiting(e);
    }

    protected override void Update(GameTime gameTime)
    {
    }

    private long lastDrawTime;
    protected override void Draw(GameTime gameTime)
    {
        float delta = (Timer.nCurrentTime - lastDrawTime) / 1000.0f;
        lastDrawTime = Timer.nCurrentTime;
        GameStatus.UpdatePerformanceGraph(delta);
        
        //....????
        if (SoundManager == null)
        {
            return;
        }

        SoundManager.t再生中の処理をする();

        if (Timer != null)
            Timer.tUpdate();
        if (CSoundManager.rcPerformanceTimer != null)
            CSoundManager.rcPerformanceTimer.tUpdate();

        if (InputManager != null)
            InputManager.tPolling(bApplicationActive, ConfigIni.bBufferedInput);

        if (FPS != null)
            FPS.tUpdateCounter();

        if (Device == null)
            return;

        if (bApplicationActive) // DTXMania本体起動中の本体/モニタの省電力モード移行を抑止
            CPowerManagement.tDisableMonitorSuspend();

        // #xxxxx 2013.4.8 yyagi; sleepの挿入位置を、EndScnene～Present間から、BeginScene前に移動。描画遅延を小さくするため。

        #region [ スリープ ]

        if (ConfigIni.nフレーム毎スリープms >= 0) // #xxxxx 2011.11.27 yyagi
        {
            Thread.Sleep(ConfigIni.nフレーム毎スリープms);
        }

        #endregion
        
        ProcessWindowMessages();
        
//#if INSPECTOR
        //cache this value to prevent it from changing during the frame
        bool renderGameToWindow = renderGameToSurface;

        if (renderGameToWindow)
        {
            Device.SetRenderTarget(0, gameRenderTargetSurface);
        }
//#endif

        Device.Clear(ClearFlags.ZBuffer | ClearFlags.Target, SharpDX.Color.Black, 1f, 0);
        Device.BeginScene();

//#if INSPECTOR
        ImGui.SetCurrentContext(context);
        ImGuizmo.SetImGuiContext(context);
        ImGuiImplD3D9.SetCurrentContext(context);

        ImGuiImplD3D9.NewFrame();
        ImGui.NewFrame();

        ImGuizmo.BeginFrame();

        ImGuizmo.Enable(true);
        ImGuizmo.SetOrthographic(true);

        if (!renderGameToWindow)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            ImGuizmo.SetRect(0, 0, io.DisplaySize.X, io.DisplaySize.Y);
            ImGuizmo.SetDrawlist(ImGui.GetBackgroundDrawList());
        }
//#endif

        StageManager.DrawStage();
        persistentUIGroup.Draw(Matrix.Identity);

//#if INSPECTOR
        if (renderGameToWindow)
        {
            Device.SetRenderTarget(0, mainRenderTarget);
            Device.Clear(ClearFlags.ZBuffer | ClearFlags.Target, SharpDX.Color.Black, 1f, 0);
        }

        InspectorManager.Draw(renderGameToWindow, gameRenderTargetTexture);

        Device.EndScene();

        ImGui.EndFrame();
        ImGui.Render();
        ImGuiImplD3D9.RenderDrawData(ImGui.GetDrawData());
//#else
// Device.EndScene();
//#endif

        // Present()は game.csのOnFrameEnd()に登録された、GraphicsDeviceManager.game_FrameEnd() 内で実行されるので不要
        // (つまり、Present()は、Draw()完了後に実行される)
        actFlushGPU.OnUpdateAndDraw(); // Flush GPU	// EndScene()～Present()間 (つまりVSync前) でFlush実行

        #region [ Fullscreen mode switching ]

        if (changeFullscreenModeOnNextFrame)
        {
            ConfigIni.bFullScreenMode = !ConfigIni.bFullScreenMode;
            app.tSwitchFullScreenMode();
            changeFullscreenModeOnNextFrame = false;
        }

        #endregion

        #region [ VSync switching ]

        if (changeVSyncModeOnNextFrame)
        {
            bool bIsMaximized =
                Window.IsMaximized; // #23510 2010.11.3 yyagi: to backup current window mode before changing VSyncWait
            currentClientSize =
                Window.ClientSize; // #23510 2010.11.3 yyagi: to backup current window size before changing VSyncWait
            DeviceSettings currentSettings = app.GraphicsDeviceManager.CurrentSettings;
            currentSettings.EnableVSync = ConfigIni.bVerticalSyncWait;
            app.GraphicsDeviceManager.ChangeDevice(currentSettings);
            changeVSyncModeOnNextFrame = false;
            Window.ClientSize =
                new Size(currentClientSize.Width,
                    currentClientSize.Height); // #23510 2010.11.3 yyagi: to resume window size after changing VSyncWait
            if (bIsMaximized)
            {
                Window.WindowState =
                    FormWindowState.Maximized; // #23510 2010.11.3 yyagi: to resume window mode after changing VSyncWait
            }
        }

        #endregion
    }

    private void ProcessWindowMessages()
    {
        if (Window.IsReceivedMessage) // ウインドウメッセージで、
        {
            //Received message from DTXCreator
            string strMes = Window.strMessage;
            Window.IsReceivedMessage = false;
            if (strMes != null)
            {
                Trace.TraceInformation("Received Message. ParseArguments {0}。", strMes);
                CommandParse.ParseArguments(strMes, ref DTXVmode, ref DTX2WAVmode);

                if (DTXVmode.Enabled)
                {
                    //Bring DTXViewer to the front whenever a DTXCreator Play DTX button is triggered
                    if (!Window.Visible)
                    {
                        Window.Show();
                    }

                    if (Window.WindowState == FormWindowState.Minimized)
                    {
                        Window.WindowState = FormWindowState.Normal;
                    }

                    Window.Activate();
                    Window.TopMost = true; // important
                    Window.TopMost = false; // important
                    Window.Focus(); // important

                    bCompactMode = true;
                    strCompactModeFile = DTXVmode.filename;
                }

                if (DTX2WAVmode.Enabled)
                {
                    if (DTX2WAVmode.Command == CDTX2WAVmode.ECommand.Cancel)
                    {
                        Trace.TraceInformation("録音のCancelコマンドをDTXMania本体が受信しました。");

                        if (DTX != null) // 曲読み込みの前に録音Cancelされると、DTXがnullのままここにきてでGPFとなる→nullチェック追加
                        {
                            DTX.tStopPlayingAllChips();
                            DTX.OnDeactivate();
                        }

                        StageManager.rCurrentStage.OnDeactivate();

                        Environment.Exit(10010); // このやり方ならばOK
                    }
                }
            }
        }
    }

    public static void tRunGarbageCollector()
    {
        //LOHに対するコンパクションを要求
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        //通常通り、LOHへのGCを抑制
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.Default;
    }

    // Other

    #region [ Texture Creation / Disposal (why is this in the main game class??) ]

    private static CTexture fallbackTexture;
    public static CTexture FallbackTexture => fallbackTexture;
    
    public static void UpdateFallback()
    {
        if (fallbackTexture != null)
        {
            tReleaseTexture(ref fallbackTexture);
        }

        Font fallbackFont = new("MS PGothic", 16f, GraphicsUnit.Pixel);
        Bitmap bitmap = new(64, 64);
        
        Graphics graphics = Graphics.FromImage(bitmap);
        
        //fill with black
        graphics.Clear(Color.Black);
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        graphics.DrawString("ERROR ERROR ERROR ERROR ERROR", fallbackFont, Brushes.White, 0f, 0f);
        graphics.DrawString("ERROR ERROR ERROR ERROR ERROR", fallbackFont, Brushes.White, 0f, 16f);
        graphics.DrawString("ERROR ERROR ERROR ERROR ERROR", fallbackFont, Brushes.White, 0f, 32f);
        graphics.DrawString("ERROR ERROR ERROR ERROR ERROR", fallbackFont, Brushes.White, 0f, 48f);
        graphics.Dispose();
        fallbackTexture = new CTexture(app.Device, bitmap, TextureFormat, false);
        bitmap.Dispose();
    }
    
    public static CTexture tGenerateTexture(string fileName)
    {
        return tGenerateTexture(fileName, false);
    }

    public static CTexture tGenerateTexture(string fileName, bool b黒を透過する)
    {
        if (app == null)
        {
            return fallbackTexture;
        }

        try
        {
            return new CTexture(app.Device, fileName, TextureFormat, b黒を透過する);
        }
        catch (CTextureCreateFailedException e)
        {
            Trace.TraceError($"Couldn't create texture: ({fileName}) {e.Message}");
            return fallbackTexture;
        }
        catch (FileNotFoundException e)
        {
            Trace.TraceError($"Couldn't find texture file: ({fileName}) {e.Message}");
            return fallbackTexture;
        }
    }

    public static void tReleaseTexture(ref CTexture? tx)
    {
        if (tx != null && tx != fallbackTexture)
        {
            //Trace.WriteLine( "CTextureを解放 Size W:" + tx.szImageSize.Width + " H:" + tx.szImageSize.Height );
            tx.Dispose();
            tx = null;
        }
    }

    public static CTexture tGenerateTexture(byte[] txData, bool b黒を透過する)
    {
        if (app == null)
        {
            return null;
        }

        try
        {
            return new CTexture(app.Device, txData, TextureFormat, b黒を透過する);
        }
        catch (CTextureCreateFailedException)
        {
            Trace.TraceError("テクスチャの生成に失敗しました。(txData)");
            return null;
        }
    }

    public static CTexture tGenerateTexture(Bitmap bitmap)
    {
        return tGenerateTexture(bitmap, false);
    }

    public static CTexture tGenerateTexture(Bitmap bitmap, bool b黒を透過する)
    {
        if (app == null)
        {
            return null;
        }

        try
        {
            //Trace.WriteLine( "CTextureをBitmapから生成" );
            return new CTexture(app.Device, bitmap, TextureFormat, b黒を透過する);
        }
        catch (CTextureCreateFailedException)
        {
            Trace.TraceError("テクスチャの生成に失敗しました。(txData)");
            return null;
        }
    }

    public static CTextureAf tテクスチャの生成Af(string fileName)
    {
        if (app == null)
        {
            return null;
        }

        try
        {
            return new CTextureAf(app.Device, fileName, TextureFormat, false);
        }
        catch (CTextureCreateFailedException)
        {
            Trace.TraceError("テクスチャの生成に失敗しました。({0})", fileName);
            return null;
        }
        catch (FileNotFoundException)
        {
            Trace.TraceError("テクスチャファイルが見つかりませんでした。({0})", fileName);
            return null;
        }
    }
    
    #endregion

    /// <summary>プロパティ、インデクサには ref は使用できないので注意。</summary>
    public static void tDisposeSafely<T>(ref T? obj)
    {
        if (obj == null)
            return;

        if (obj is IDisposable d)
            d.Dispose();

        obj = default;
    }

    //-----------------

    public static CScoreIni tScoreIniへBGMAdjustとHistoryとPlayCountを更新(string str新ヒストリ行)
    {
        string strFilename = DTX.strFileNameFullPath + ".score.ini";
        CScoreIni ini = new(strFilename);
        if (!File.Exists(strFilename))
        {
            ini.stFile.Title = DTX.TITLE;
            ini.stFile.Name = DTX.strFileName;
            ini.stFile.Hash = CScoreIni.tComputeFileMD5(DTX.strFileNameFullPath);

            // 0: hiscore drums
            // 1: hiskill drums
            // primary = all except pedals, secondary = pedals
            ini.stSection[0].stPrimaryHitRanges = stDrumHitRanges;
            ini.stSection[0].stSecondaryHitRanges = stDrumPedalHitRanges;
            ini.stSection[1].stPrimaryHitRanges = stDrumHitRanges;
            ini.stSection[1].stSecondaryHitRanges = stDrumPedalHitRanges;

            // 2: hiscore guitar
            // 3: hiskill guitar
            // primary = all, secondary = unused (zero out)
            ini.stSection[2].stPrimaryHitRanges = stGuitarHitRanges;
            ini.stSection[2].stSecondaryHitRanges = new STHitRanges();
            ini.stSection[3].stPrimaryHitRanges = stGuitarHitRanges;
            ini.stSection[3].stSecondaryHitRanges = new STHitRanges();

            // 4: hiscore bass guitar
            // 5: hiskill bass guitar
            // primary = all, secondary = unused (zero out)
            ini.stSection[4].stPrimaryHitRanges = stBassHitRanges;
            ini.stSection[4].stSecondaryHitRanges = new STHitRanges();
            ini.stSection[5].stPrimaryHitRanges = stBassHitRanges;
            ini.stSection[5].stSecondaryHitRanges = new STHitRanges();
        }

        ini.stFile.BGMAdjust = DTX.nBGMAdjust;
        CScoreIni.tGetIsUpdateNeeded(out bool bIsUpdatedDrums, out bool bIsUpdatedGuitar, out bool bIsUpdatedBass);
        if (bIsUpdatedDrums || bIsUpdatedGuitar || bIsUpdatedBass)
        {
            if (bIsUpdatedDrums)
            {
                ini.stFile.PlayCountDrums++;
            }

            if (bIsUpdatedGuitar)
            {
                ini.stFile.PlayCountGuitar++;
            }

            if (bIsUpdatedBass)
            {
                ini.stFile.PlayCountBass++;
            }

            ini.tAddHistory(str新ヒストリ行);
            if (!bCompactMode)
            {
                confirmedChart.SongInformation.NbPerformances.Drums =
                    ini.stFile.PlayCountDrums;
                confirmedChart.SongInformation.NbPerformances.Guitar =
                    ini.stFile.PlayCountGuitar;
                confirmedChart.SongInformation.NbPerformances.Bass =
                    ini.stFile.PlayCountBass;
                for (int j = 0; j < ini.stFile.History.Length; j++)
                {
                    confirmedChart.SongInformation.PerformanceHistory[j] =
                        ini.stFile.History[j];
                }
            }
        }

        if (ConfigIni.bScoreIniを出力する)
        {
            ini.tExport(strFilename);
        }

        return ini;
    }
    
    public void AddSoundTypeToWindowTitle()
    {
        string delay = "";
        if (SoundManager.GetCurrentSoundDeviceType() != "DirectSound")
        {
            delay = "(" + SoundManager.GetSoundDelay() + "ms)";
        }

        Window.Text = strWindowTitle + " (" + SoundManager.GetCurrentSoundDeviceType() + delay + ")";
    }
    
    public static SongNode confirmedSong { get; private set; }
    public static CScore confirmedChart { get; private set; }
    public static int confirmedSongDifficulty { get; private set; }
    
    public static void UpdateSelection(SongNode song, CScore chart, int difficulty)
    {
        confirmedSong = song;
        confirmedChart = chart;
        confirmedSongDifficulty = difficulty;
    }

    #region [ private ]

    //-----------------
    private bool bMouseCursorShown = true;
    private bool bTerminated;
    private static CDTX dtx;
    private List<CActivity> mainActivities;
    private MouseButtons mb = MouseButtons.Left;
    private string strWindowTitle = "";

    //
    public CIMEHook cIMEHook;
    public CActSearchBox textboxテキスト入力中;
    public bool bテキスト入力中 => textboxテキスト入力中 != null;

    private void tTerminate() // t終了処理
    {
        if (!bTerminated)
        {
            void SafeTerminate(string name, Action action)
            {
                Trace.TraceInformation($"Cleaning up {name}");
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Trace.TraceError(e.Message);
                }
            }

            Trace.TraceInformation("Shutting down application");

            SafeTerminate("Current Stage", () =>
            {
                if (StageManager.rCurrentStage is { bActivated: true })
                    StageManager.rCurrentStage.OnDeactivate();
            });
            SafeTerminate("SongManager", () => { SongManager = null; });
            SafeTerminate("SongDb", () => { SongDb = null; });
            SafeTerminate("Skin", () =>
            {
                if (Skin != null)
                {
                    Skin.tSaveSkinConfig();
                    Skin.Dispose();
                    Skin = null;
                }
            });
            SafeTerminate("SoundManager", () => { SoundManager.Dispose(); });
            SafeTerminate("Pad", () => { Pad = null; });
            SafeTerminate("InputManager", () => { InputManager.Dispose(); });
            SafeTerminate("ActDisplayString", () => { actDisplayString.OnDeactivate(); });
            SafeTerminate("FPS Counter", () => { FPS = null; });
            SafeTerminate("Timer", () => { Timer?.Dispose(); });
            SafeTerminate("Config.ini (and writing it to disk)", () =>
            {
                if (ConfigIni.bIsSwappedGuitarBass_AutoFlagsAreSwapped)
                {
                    ConfigIni.SwapGuitarBassInfos_AutoFlags();
                }

                string path = executableDirectory + "Config.ini";

                //no need to save if we are in DTXVmode
                if (DTXVmode.Enabled) return;

                if (DTX2WAVmode.Enabled)
                {
                    DTX2WAVmode.SendMessage2DTX2WAV("TERM");
                }
                else
                {
                    ConfigIni.tWrite(path);
                    Trace.TraceInformation("保存しました。({0})", path);
                }
            });
            SafeTerminate("ResourceManager", () => { Resources.Dispose(); });
            SafeTerminate("Discord Rich Presence", () => { DiscordRichPresence?.Dispose(); });
            Trace.TraceInformation("Finished shutting down application");
            bTerminated = true;
        }
    }
    
    //https://stackoverflow.com/questions/1600962/displaying-the-build-date
    private static DateTime? GetAssemblyBuildDateTime()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var attr = Attribute.GetCustomAttribute(assembly, typeof(BuildDateTimeAttribute)) as BuildDateTimeAttribute;
        return attr?.Built;
    }

    #region [ Windowイベント処理 ]

    //-----------------
    private void Window_ApplicationActivated(object? sender, EventArgs e)
    {
        bApplicationActive = true;
    }

    private void Window_ApplicationDeactivated(object? sender, EventArgs e)
    {
        bApplicationActive = false;
    }

    private void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        ImGuiKey keyCode = WindowsKeyCodeToImGui(e.KeyCode);

        //update key state
        if (keyCode != ImGuiKey.None)
        {
            ImGui.GetIO().AddKeyEvent(keyCode, true);
        }

        if (ImGui.GetIO().WantCaptureKeyboard)
        {
            return;
        }

        if (e.KeyCode == Keys.Menu)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if ((e.KeyCode == Keys.Return) && e.Alt)
        {
            if (ConfigIni != null)
            {
                ConfigIni.bWindowMode = !ConfigIni.bWindowMode;
                tSwitchFullScreenMode();
            }

            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else
        {
            for (int i = 0; i < 0x10; i++)
            {
                var captureCode =
                    (SlimDX.DirectInput.Key)ConfigIni.KeyAssign.System[(int)EKeyConfigPad.Capture][i].Code;

                if ((int)captureCode > 0 && e.KeyCode == DeviceConstantConverter.KeyToKeys(captureCode))
                {
                    string strFullPath = Path.Combine(executableDirectory, "Capture_img");
                    strFullPath = Path.Combine(strFullPath, DateTime.Now.ToString("yyyyMMddHHmmss") + ".png");
                    SaveResultScreen(strFullPath);
                }
            }
        }
    }

    private void Window_KeyPress(object? sender, KeyPressEventArgs e)
    {
        if (ImGui.GetIO().WantCaptureKeyboard)
        {
            ImGui.GetIO().AddInputCharacter(e.KeyChar);
            e.Handled = true;
        }
    }

    private void Window_KeyUp(object? sender, KeyEventArgs e)
    {
        //update key state
        ImGuiKey keyCode = WindowsKeyCodeToImGui(e.KeyCode);
        if (keyCode != ImGuiKey.None)
        {
            ImGui.GetIO().AddKeyEvent(keyCode, false);
        }
    }

    private ImGuiKey WindowsKeyCodeToImGui(Keys keyCode)
    {
        return keyCode switch
        {
            Keys.Back => ImGuiKey.Backspace,
            Keys.Tab => ImGuiKey.Tab,
            Keys.Enter => ImGuiKey.Enter,
            Keys.Pause => ImGuiKey.Pause,
            Keys.Escape => ImGuiKey.Escape,
            Keys.Space => ImGuiKey.Space,
            Keys.End => ImGuiKey.End,
            Keys.Home => ImGuiKey.Home,
            Keys.Left => ImGuiKey.LeftArrow,
            Keys.Up => ImGuiKey.UpArrow,
            Keys.Right => ImGuiKey.RightArrow,
            Keys.Down => ImGuiKey.DownArrow,
            Keys.PageUp => ImGuiKey.PageUp,
            Keys.PageDown => ImGuiKey.PageDown,
            Keys.Insert => ImGuiKey.Insert,
            Keys.Delete => ImGuiKey.Delete,
            Keys.LShiftKey => ImGuiKey.LeftShift,
            Keys.RShiftKey => ImGuiKey.RightShift,
            Keys.LControlKey => ImGuiKey.LeftCtrl,
            Keys.RControlKey => ImGuiKey.RightCtrl,
            Keys.LMenu => ImGuiKey.LeftAlt,
            Keys.RMenu => ImGuiKey.RightAlt,
            Keys.OemSemicolon => ImGuiKey.Semicolon,
            >= Keys.D0 and <= Keys.D9 => ImGuiKey.Key0 + (keyCode - Keys.D0),
            >= Keys.A and <= Keys.Z => ImGuiKey.A + (keyCode - Keys.A),
            >= Keys.F1 and <= Keys.F12 => ImGuiKey.F1 + (keyCode - Keys.F1),
            >= Keys.NumPad0 and <= Keys.NumPad9 => ImGuiKey.Keypad0 + (keyCode - Keys.NumPad0),
            _ => ImGuiKey.None
        };
    }

    private void Window_MouseMove(object? sender, MouseEventArgs e)
    {
        var pos = e.Location;

        //take window scale into account (since the render resolution for imgui is fixed 1280x720)
        var windowScale = new Vector2((float)Window.ClientSize.Width / GameWindowSize.Width,
            (float)Window.ClientSize.Height / GameWindowSize.Height);
        pos.X = (int)(pos.X / windowScale.X);
        pos.Y = (int)(pos.Y / windowScale.Y);

        ImGui.GetIO().MousePos = new Vector2(pos.X, pos.Y);
    }

    private void Window_MouseDown(object? sender, MouseEventArgs e)
    {
        switch (e.Button)
        {
            case MouseButtons.Left:
                ImGui.GetIO().MouseDown[(int)ImGuiMouseButton.Left] = true;
                break;

            case MouseButtons.Middle:
                ImGui.GetIO().MouseDown[(int)ImGuiMouseButton.Middle] = true;
                break;

            case MouseButtons.Right:
                ImGui.GetIO().MouseDown[(int)ImGuiMouseButton.Right] = true;
                break;
        }
    }

    private void Window_MouseUp(object? sender, MouseEventArgs e)
    {
        mb = e.Button;

        switch (e.Button)
        {
            case MouseButtons.Left:
                ImGui.GetIO().MouseDown[(int)ImGuiMouseButton.Left] = false;
                break;

            case MouseButtons.Middle:
                ImGui.GetIO().MouseDown[(int)ImGuiMouseButton.Middle] = false;
                break;

            case MouseButtons.Right:
                ImGui.GetIO().MouseDown[(int)ImGuiMouseButton.Right] = false;
                break;
        }
    }

    private void Window_MouseWheel(object? sender, MouseEventArgs e)
    {
        ImGui.GetIO().MouseWheel = e.Delta / 120.0f;
    }

    private void Window_MouseDoubleClick(object? sender, MouseEventArgs e) // #23510 2010.11.13 yyagi: to go full screen mode
    {
        if (mb.Equals(MouseButtons.Left) && ConfigIni.bIsAllowedDoubleClickFullscreen) // #26752 2011.11.27 yyagi
        {
            ConfigIni.bWindowMode = false;
            tSwitchFullScreenMode();
        }
    }

    private void Window_ResizeEnd(object? sender, EventArgs e) // #23510 2010.11.20 yyagi: to get resized window size
    {
        if (ConfigIni.bWindowMode)
        {
            ConfigIni.n初期ウィンドウ開始位置X = Window.Location.X; // #30675 2013.02.04 ikanick add
            ConfigIni.n初期ウィンドウ開始位置Y = Window.Location.Y; //
        }

        ConfigIni.nWindowWidth =
            (ConfigIni.bWindowMode) ? Window.ClientSize.Width : currentClientSize.Width; // #23510 2010.10.31 yyagi add
        ConfigIni.nWindowHeight = (ConfigIni.bWindowMode) ? Window.ClientSize.Height : currentClientSize.Height;
    }

    #endregion
    #endregion

    public static int GetCurrentInstrument()
    {
        return ConfigIni.bDrumsEnabled ? 0
            : ConfigIni.bIsSwappedGuitarBass ? 2 : 1;
    }
}