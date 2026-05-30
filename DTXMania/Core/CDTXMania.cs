using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Windows.Forms;
using DTXMania.Core.Video;
using DTXMania.SongDb;
using DTXMania.UI;
using FDK;
using Hexa.NET.ImGui;
using ResourceManager = DTXMania.UI.ResourceManager;
using Vector2 = System.Numerics.Vector2;
using DTXMania.UI.Drawable;
using DTXMania.UI.Inspector;
using DTXMania.UI.Skin;
using Hexa.NET.GLFW;

namespace DTXMania.Core;

internal class CDTXMania
{
    //these get set when initializing the game
    public static string VERSION_DISPLAY; // = "DTX:NX:A:A:2024051900";
    public static string VERSION; // = "v1.4.2 20240519";

    public DTXManiaGL maniaGl;
    
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
    public static CDiscordRichPresence? DiscordRichPresence { get; set; }

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
            }

            dtx = value;
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
            SongNode parentNode = chosenSong?.parent;
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
            SongNode parentNode = chosenSong?.parent;
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
            SongNode parentNode = chosenSong?.parent;
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
            SongNode parentNode = chosenSong?.parent;
            if (parentNode?.nodeType == SongNode.ENodeType.BOX)
                return STHitRanges.tCompose(parentNode.stBassHitRanges, ConfigIni.stBassHitRanges);

            return ConfigIni.stBassHitRanges;
        }
    }

    public static CPad Pad { get; private set; }

    public static Input Input { get; private set; }
    public static Random Random { get; private set; }
    public static CSkin Skin { get; private set; }
    
    public static CStagePerfGuitarScreen stagePerfGuitarScreen => StageManager.stagePerfGuitarScreen;
    public static CStagePerfDrumsScreen stagePerfDrumsScreen => StageManager.stagePerfDrumsScreen;
    
    public static StageManager StageManager { get; private set; }
    public static SkinManager SkinManager { get; private set; }

    public static ResourceManager Resources { get; private set; }

    public static SongDb.SongDb SongDb { get; private set; }

    public static CSoundManager SoundManager { get; private set; }

    public static string executableDirectory { get; private set; }
    public static string strCompactModeFile { get; private set; }
    public static CTimer Timer { get; private set; }

    public bool bApplicationActive => maniaGl.isFocused;

    private ImGuiContextPtr context;
    
    //fork
    public static STDGBVALUE<List<int>> listAutoGhostLag = new();
    public static STDGBVALUE<List<int>> listTargetGhsotLag = new();

    public static STDGBVALUE<CScoreIni.CPerformanceEntry> listTargetGhostScoreData = new();
    
    //new
    public static UIGroup persistentUIGroup { get; private set; } = new("PersistentUIGroup");
    public static GitaDoraTransition gitadoraTransition { get; private set; }

    //how many songs have we played, gets incremented whenever we transition from StageLoading to StagePerformance
    public static int nStageNumber = 0;

    private bool startupFinished = false;
    private readonly List<(string name, Action initialize)> initializers = [];
    
    void RunInitializer((string name, Action initializer) initializer)
    {
        try
        {
            Trace.TraceInformation($"Initializing {initializer.name}");
            initializer.initializer();
        }
        catch (Exception e)
        {
            Trace.TraceError($"Failed to initialize {initializer.name}: {e}\n{e.StackTrace}");
            MessageBox.Show($"Failed to initialize {initializer.name}: {e}\n{e.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw;
        }
    }

    private void StartupTick()
    {
        //tick through initializers and initialize them one by one
        if (initializers.Count > 0)
        {
            var initializer = initializers.First();
            initializers.RemoveAt(0);
            RunInitializer(initializer);
        }
        
        if (initializers.Count == 0)
        {
            startupFinished = true;

            Trace.TraceInformation("Finished game initialization");
            
            Trace.TraceInformation("----------------------");
            Trace.TraceInformation("■ Startup");
        
            SongDb.StartScan();
        }
    }
    
    // Constructor
    public CDTXMania(DTXManiaGL dtxManiaGl)
    {
        maniaGl = dtxManiaGl;
        app = this;

        void AddInitializer(string name, Action action)
        {
            initializers.Add((name, action));
        }

        //Update version information
        Assembly assembly = Assembly.GetExecutingAssembly();
        DateTime? buildDate = GetAssemblyBuildDateTime() ?? DateTime.UnixEpoch;
        string appName = "DTXManiaNX";
        VERSION = $"v{assembly.GetName().Version.ToString().Substring(0, 5)} Beta ({buildDate:yyyyMMdd})";
        VERSION_DISPLAY = $"DTX:NX:A:A:{buildDate:yyyyMMdd}00 Beta";

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
        
        strWindowTitle = appName + " " + VERSION;
        
        DTX = null;

        Resources = new ResourceManager();
        SkinManager = new SkinManager();
        
        ConfigIni.SyncGraphicsSettings(maniaGl.host);
        UpdateWindowTitle();
        maniaGl.host.InitializeGraphics();
        
        AddInitializer("Skin", () =>
        {
            Skin = new CSkin(ConfigIni.strSystemSkinSubfolderFullName, ConfigIni.bUseBoxDefSkin);
            ConfigIni.strSystemSkinSubfolderFullName =
                Skin.GetCurrentSkinSubfolderFullName(true); // 旧指定のSkinフォルダが消滅していた場合に備える
        });
        
        AddInitializer("StageManager", () => StageManager = new StageManager());
        
        AddInitializer("LoadingStage", () =>
        {
            StageManager.LoadInitialStage();
        });

        AddInitializer("FFmpeg", FFmpegCore.Initialize);

        AddInitializer("Timer", () =>
        {
            Timer = new CTimer(CTimer.EType.MultiMedia); 
            Random = new Random((int)Timer.nシステム時刻);
        });

        AddInitializer("FPS Counter", () => { FPS = new CFPS(); });

        AddInitializer("Character Console", () =>
        {
            actDisplayString = new CCharacterConsole();
            actDisplayString.OnActivate();
        });

        AddInitializer("Input Manager (DirectInput, MIDI)", () =>
        {
            InputManager = new CInputManager(maniaGl.host.GetWindowHandle());
            foreach (IInputDevice device in InputManager.listInputDevices)
            {
                if (device.eInputDeviceType == EInputDeviceType.Joystick &&
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

        AddInitializer("Pad", () => { Pad = new CPad(ConfigIni, InputManager); });

        AddInitializer("Sound Manager", () =>
        {
            ESoundDeviceType soundDeviceType = ConfigIni.nSoundDriverType switch
            {
                0 => ESoundDeviceType.DirectSound,
                1 => ESoundDeviceType.ASIO,
                2 => ESoundDeviceType.ExclusiveWASAPI,
                3 => ESoundDeviceType.SharedWASAPI,
                _ => ESoundDeviceType.Unknown
            };

            SoundManager = new CSoundManager(maniaGl.host.GetWindowHandle(),
                soundDeviceType,
                ConfigIni.nWASAPIBufferSizeMs,
                ConfigIni.bEventDrivenWASAPI,
                0,
                ConfigIni.nASIODevice,
                ConfigIni.bUseOSTimer
            );
            UpdateWindowTitle();
            CSoundManager.bIsTimeStretch = ConfigIni.bTimeStretch;
            SoundManager.nMasterVolume = ConfigIni.nMasterVolume;

            string strDefaultSoundDeviceBusType = CSoundManager.strDefaultDeviceBusType;
            Trace.TraceInformation($"Bus type of the default sound device = {strDefaultSoundDeviceBusType}");
        });
        
        AddInitializer("Input", () => Input = new Input());

        AddInitializer("DiscordRichPresence", () =>
        {
            if (ConfigIni.bDiscordRichPresenceEnabled && !bCompactMode)
                DiscordRichPresence = new CDiscordRichPresence(ConfigIni.strDiscordRichPresenceApplicationID);
        });
       
        AddInitializer("SongDb", () => SongDb = new SongDb.SongDb());
        
        AddInitializer("SongDBStatus", () =>
        {
            SongDBStatus songDbStatus = persistentUIGroup.AddChild(new SongDBStatus());
            songDbStatus.position = new Vector3(0, 720, 0);
            songDbStatus.anchor = new Vector2(0.0f, 1.0f);
        });
        
        AddInitializer("AnimatedTransition", () => gitadoraTransition = persistentUIGroup.AddChild(new GitaDoraTransition()));
        
        AddInitializer("Load Skin Sounds", () =>
        {
            Skin.bgmTitleScreen.tPlay();
            Skin.ReloadSkin();
        });
    }

    // Methods

    #region [ #24609 リザルト画像をpngで保存する ] // #24609 2011.3.14 yyagi; to save result screen in case BestRank or HiSkill.

    /// <summary>
    /// リザルト画像のキャプチャと保存。
    /// </summary>
    /// <param name="strFilename">保存するファイル名(フルパス)</param>
    public bool SaveResultScreen(string strFullPath)
    {
        Trace.TraceInformation($"Saving result screen to {strFullPath}");
        Trace.TraceError("Saving result screen is currently disabled because of moving to OpenGL.");
        return false;
        // string strSavePath = Path.GetDirectoryName(strFullPath);
        // if (!Directory.Exists(strSavePath))
        // {
        //     try
        //     {
        //         Directory.CreateDirectory(strSavePath);
        //     }
        //     catch
        //     {
        //         return false;
        //     }
        // }
        //
        // // http://www.gamedev.net/topic/594369-dx9slimdxati-incorrect-saving-surface-to-file/
        // using (Surface pSurface = app.Device.GetRenderTarget(0))
        // {
        //     Surface.ToFile(pSurface, strFullPath, ImageFileFormat.Png);
        // }
        //
        // return true;
    }

    #endregion

    public static float renderScale = 1.0f;

    public void Update()
    {
    }

    public void Draw()
    {
        persistentUIGroup.scale.X = renderScale;
        persistentUIGroup.scale.Y = renderScale;
        
        if (!startupFinished)
        {
            StartupTick();
            
            StageManager?.DrawStage();
            persistentUIGroup.Draw(Matrix4x4.Identity);
            return;
        }
        
        //....????
        if (SoundManager == null)
        {
            return;
        }

        SoundManager.t再生中の処理をする();

        Timer.tUpdate();
        CSoundManager.rcPerformanceTimer.tUpdate();
        
        //don't constantly scan unless we lost a midi device
        if (StageManager.rCurrentStage.eStageID == CStage.EStage.Performance_6)
        {
            if (InputManager.lostMidiDevice)
            {
                InputManager.ScanDevices();
            }
        }
        else
        {
            InputManager.ScanDevices();
        }
        
        bool inspectorCapturingKeyboard = InspectorManager.inspectorEnabled && ImGui.GetIO().WantCaptureKeyboard;
        bool textInputDrawableActive = UIImGuiTextInput.IsAnyInputActive;
        InputManager.Keyboard.preventKeyboardInput = inspectorCapturingKeyboard || textInputDrawableActive || GameStatus.preventGameKeyboardInput;
        
        //poll input
        InputManager.tPolling(bApplicationActive, ConfigIni.bBufferedInput);
        
        //todo: replace
        FPS.tUpdateCounter();
        
        if (bApplicationActive) // DTXMania本体起動中の本体/モニタの省電力モード移行を抑止
            CPowerManagement.tDisableMonitorSuspend();
        
        if (ConfigIni.nSleepNMsEveryFrame >= 0) // #xxxxx 2011.11.27 yyagi
        {
            Thread.Sleep(ConfigIni.nSleepNMsEveryFrame); ///?????
        }
        
        StageManager.DrawStage();
        persistentUIGroup.Draw(Matrix4x4.Identity);

        StageManager.HandleStageChanges();
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

    public static CScoreIni UpdateBGMAdjustHistoryPlayCountIntScoreIni(string strNewHistoryLine)
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
        STDGBVALUE<bool> isUpdateNeeded = CScoreIni.tGetIsUpdateNeeded();
        if (isUpdateNeeded.Drums || isUpdateNeeded.Guitar || isUpdateNeeded.Bass)
        {
            if (isUpdateNeeded.Drums)
            {
                ini.stFile.PlayCountDrums++;
            }

            if (isUpdateNeeded.Guitar)
            {
                ini.stFile.PlayCountGuitar++;
            }

            if (isUpdateNeeded.Bass)
            {
                ini.stFile.PlayCountBass++;
            }

            ini.tAddHistory(strNewHistoryLine);
            if (!bCompactMode)
            {
                chosenChartData.SongInformation.NbPerformances.Drums =
                    ini.stFile.PlayCountDrums;
                chosenChartData.SongInformation.NbPerformances.Guitar =
                    ini.stFile.PlayCountGuitar;
                chosenChartData.SongInformation.NbPerformances.Bass =
                    ini.stFile.PlayCountBass;
                for (int j = 0; j < ini.stFile.History.Length; j++)
                {
                    chosenChartData.SongInformation.PerformanceHistory[j] =
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
    
    public void UpdateWindowTitle()
    {
        if (SoundManager == null)
        {
            maniaGl.SetWindowTitle(strWindowTitle);
            return;
        }
        
        string delay = "";
        if (SoundManager.GetCurrentSoundDeviceType() != "DirectSound")
        {
            delay = "(" + SoundManager.GetSoundDelay() + "ms)";
        }
        maniaGl.SetWindowTitle(strWindowTitle + " (" + SoundManager.GetCurrentSoundDeviceType() + delay + ")");
    }
    
    public static SongNode chosenSong { get; private set; }
    public static CChartData chosenChartData { get; private set; }
    public static int confirmedSongDifficulty { get; private set; }
    
    public static void UpdateSelection(SongNode song, CChartData chartData, int difficulty)
    {
        chosenSong = song;
        chosenChartData = chartData;
        confirmedSongDifficulty = difficulty;
    }

    #region [ private ]

    //-----------------
    private bool bTerminated;
    private static CDTX dtx;
    private string strWindowTitle = "";

    public void tTerminate() // t終了処理
    {
        if (bTerminated) return;
        
        CPowerManagement.tEnableMonitorSuspend();

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

        SafeTerminate("Persistent UI Group", () =>
        {
            if (persistentUIGroup != null)
            {
                persistentUIGroup.Dispose();
            }
        });
            
        SafeTerminate("Current Stage", () =>
        {
            if (StageManager.rCurrentStage is { bActivated: true })
                StageManager.rCurrentStage.OnDeactivate();
        });
        SafeTerminate("SongDb", () =>
        {
            SongDb = null;
        });
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
                
            ConfigIni.tWrite(path);
            Trace.TraceInformation("保存しました。({0})", path);
        });
        SafeTerminate("ResourceManager", () => { Resources.Dispose(); });
        SafeTerminate("Discord Rich Presence", () => { DiscordRichPresence?.Dispose(); });
        Trace.TraceInformation("Finished shutting down application");
        bTerminated = true;
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

    public void KeyPress(GlfwKey key, GlfwMod mods)
    {
        SlimDX.DirectInput.Key dxKey = Glue.SlimDXGLFWGlue.GLFWKeyToSlimDXKey(key);
        for (int i = 0; i < 0x10; i++)
        {
            var captureCode = (SlimDX.DirectInput.Key)ConfigIni.KeyAssign.System[(int)EKeyConfigPad.Capture][i].Code;

            if ((int)captureCode > 0 && dxKey == captureCode)
            {
                string strFullPath = Path.Combine(executableDirectory, "Capture_img");
                strFullPath = Path.Combine(strFullPath, DateTime.Now.ToString("yyyyMMddHHmmss") + ".png");
                SaveResultScreen(strFullPath);
            }
        }
    }

    #endregion
    #endregion

    public static int GetCurrentInstrument()
    {
        return ConfigIni.bDrumsEnabled ? 0
            : ConfigIni.bIsSwappedGuitarBass ? 2 : 1;
    }
}