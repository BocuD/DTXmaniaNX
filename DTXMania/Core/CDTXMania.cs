﻿using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Windows.Forms;
using DTXMania.UI;
using DTXMania.UI.Skin;
using FDK;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.D3D9;
using SampleFramework;
using SharpDX;
using SharpDX.Direct3D9;
using GameWindow = DTXMania.UI.GameWindow;
using Point = System.Drawing.Point;
using Vector2 = System.Numerics.Vector2;

namespace DTXMania.Core;

internal class CDTXMania : Game
{
    // プロパティ
    //these get set when initializing the game
    public static string VERSION_DISPLAY;// = "DTX:NX:A:A:2024051900";
    public static string VERSION;// = "v1.4.2 20240519";
        
    public static string D3DXDLL = "d3dx9_43.dll";		// June 2010
        

    public static CDTXMania app
    {
        get;
        private set;
    }
    public static CCharacterConsole actDisplayString  // act文字コンソール
    {
        get;
        private set;
    }
    public static bool bCompactMode
    {
        get;
        private set;
    }
    public static CConfigIni ConfigIni
    {
        get;
        set;
    }

    /// <summary>
    /// The shared Rich Presence integration instance, or <see langword="null"/> if it is disabled.
    /// </summary>
    public static CDiscordRichPresence DiscordRichPresence { get; private set; }
        
    //current language
    public static bool isJapanese
    {
        get;
        private set;
    }
        
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
                app.listTopLevelActivities.Remove(dtx);
            }
            dtx = value;
            if ((dtx != null) && (app != null))
            {
                app.listTopLevelActivities.Add(dtx);
            }
        }
    }
    public static CFPS FPS
    {
        get;
        private set;
    }
    public static CInputManager InputManager
    {
        get;
        private set;
    }
    public static int nSongDifficulty
    {
        get;
        set;
    }

    /// <summary>
    /// The <see cref="STHitRanges"/> for all drum chips, except pedals, composed from the confirmed <see cref="CSongListNode"/> and <see cref="CConfigIni"/> settings.
    /// </summary>
    public static STHitRanges stDrumHitRanges
    {
        get
        {
            CSongListNode confirmedNode = stageSongSelection.rConfirmedSong?.r親ノード;
            if (confirmedNode?.eNodeType == CSongListNode.ENodeType.BOX)
                return STHitRanges.tCompose(confirmedNode.stDrumHitRanges, ConfigIni.stDrumHitRanges);

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
            CSongListNode confirmedNode = stageSongSelection.rConfirmedSong?.r親ノード;
            if (confirmedNode?.eNodeType == CSongListNode.ENodeType.BOX)
                return STHitRanges.tCompose(confirmedNode.stDrumPedalHitRanges, ConfigIni.stDrumPedalHitRanges);

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
            CSongListNode confirmedNode = stageSongSelection.rConfirmedSong?.r親ノード;
            if (confirmedNode?.eNodeType == CSongListNode.ENodeType.BOX)
                return STHitRanges.tCompose(confirmedNode.stGuitarHitRanges, ConfigIni.stGuitarHitRanges);

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
            CSongListNode confirmedNode = stageSongSelection.rConfirmedSong?.r親ノード;
            if (confirmedNode?.eNodeType == CSongListNode.ENodeType.BOX)
                return STHitRanges.tCompose(confirmedNode.stBassHitRanges, ConfigIni.stBassHitRanges);

            return ConfigIni.stBassHitRanges;
        }
    }

    public static CPad Pad
    {
        get;
        private set;
    }

    public static Input Input
    {
        get;
        private set;
    }
    public static Random Random
    {
        get;
        private set;
    }
    public static CSkin Skin
    {
        get;
        private set;
    }

    public static SkinManager SkinManager
    {
        get;
        private set;
    } 
    public static CSongManager SongManager
    {
        get;
        set;	// 2012.1.26 yyagi private解除 CStage起動でのdesirialize読み込みのため
    }
    public static CEnumSongs EnumSongs
    {
        get;
        private set;
    }
    public static CActEnumSongs actEnumSongs
    {
        get;
        private set;
    }
    public static CActFlushGPU actFlushGPU
    {
        get;
        private set;
    }
    public static CSoundManager SoundManager
    {
        get;
        private set;
    }
    public static CStageStartup stageStartup
    {
        get;
        private set;
    }
    public static CStageTitle stageTitle
    {
        get;
        private set;
    }
    public static CStageConfig stageConfig
    {
        get;
        private set;
    }
    public static CStageSongSelection stageSongSelection
    {
        get;
        private set;
    }
    public static CStageSongLoading stageSongLoading
    {
        get;
        private set;
    }
    public static CStagePerfGuitarScreen stagePerfGuitarScreen
    {
        get;
        private set;
    }
    public static CStagePerfDrumsScreen stagePerfDrumsScreen
    {
        get;
        private set;
    }
    public static CStagePerfCommonScreen stagePlayingScreenCommon
    {
        get;
        private set;
    }
    public static CStageResult stageResult
    {
        get;
        private set;
    }
    public static CStageChangeSkin stageChangeSkin
    {
        get;
        private set;
    }
    public static CStageEnd stageEnd
    {
        get;
        private set;
    }
    public static CStage rCurrentStage = null;
    public static CStage rPreviousStage = null;
    public bool b汎用ムービーである = false;
    public static string executableDirectory
    {
        get;
        private set;
    }
    public static string strCompactModeFile
    {
        get;
        private set;
    }
    public static CTimer Timer
    {
        get;
        private set;
    }
    public static Format TextureFormat = Format.A8R8G8B8;
    public bool bApplicationActive
    {
        get;
        private set;
    }
    public bool changeVSyncModeOnNextFrame
    {
        get;
        set;
    }
    public bool changeFullscreenModeOnNextFrame
    {
        get;
        set;
    }

    public Device Device => GraphicsDeviceManager.Direct3D9.Device;

    private static Size currentClientSize		// #23510 2010.10.27 add yyagi to keep current window size
    {
        get;
        set;
    }
    
    //		public static CTimer ct;
    public IntPtr WindowHandle => Window.Handle; // 2012.10.24 yyagi; to add ASIO support
    public static CDTXVmode DTXVmode;                       // #28821 2014.1.23 yyagi
    public static CDTX2WAVmode DTX2WAVmode;
    public static CCommandParse CommandParse;
    //fork
    public static STDGBVALUE< List<int> > listAutoGhostLag = new STDGBVALUE<List<int>>();
    public static STDGBVALUE< List<int> > listTargetGhsotLag = new STDGBVALUE<List<int>>();
    public static STDGBVALUE< CScoreIni.CPerformanceEntry > listTargetGhostScoreData = new STDGBVALUE< CScoreIni.CPerformanceEntry >();

    
    //stuff to render game inside window
    private Texture gameRenderTargetTexture;
    private Surface gameRenderTargetSurface;
    private Surface mainRenderTarget;
    public static bool renderGameToSurface = false;
    
    
    // Constructor

    public CDTXMania()
    {
        app = this;
        tStartProcess();
    }


    // Methods

    public void tSwitchFullScreenMode()
    {
        if (ConfigIni != null)
        {
            if (ConfigIni.bFullScreenMode)	// #23510 2010.10.27 yyagi: backup current window size before going fullscreen mode
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
                    if (ConfigIni.bWindowMode)    // #23510 2010.10.27 yyagi: to resume window size from backuped value
                    {
                        Window.ClientSize = new Size(currentClientSize.Width, currentClientSize.Height);
                        //FDK.CTaskBar.ShowTaskBar( true );
                    }
                }
            }
            else
            {
                // Only use windows maximized/restored sizes
                if (ConfigIni.bWindowMode)    // #23510 2010.10.27 yyagi: to resume window size from backuped value
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


    #region [ #24609 リザルト画像をpngで保存する ]		// #24609 2011.3.14 yyagi; to save result screen in case BestRank or HiSkill.
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
        //			new GCBeep();
        if (listTopLevelActivities != null)
        {
            foreach (CActivity activity in listTopLevelActivities)
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

        if (listTopLevelActivities != null)
        {
            foreach (CActivity activity in listTopLevelActivities)
                activity.OnUnmanagedCreateResources();
        }

        //init imgui
        var context = ImGui.CreateContext();
        ImGui.StyleColorsDark();
            
        var io = ImGui.GetIO();

        io.DisplaySize = new Vector2(GameWindowSize.Width, GameWindowSize.Height);
        io.DisplayFramebufferScale = new Vector2(1, 1);

        ImGuiImplD3D9.SetCurrentContext(context);

        unsafe
        {
            ImGuiImplD3D9.Init(new IDirect3DDevice9Ptr((IDirect3DDevice9*)app.Device.NativePointer));
        }
        
        gameRenderTargetTexture = new Texture(Device, 1280, 720, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default);
        gameRenderTargetSurface = gameRenderTargetTexture.GetSurfaceLevel(0);
        
        mainRenderTarget = Device.GetRenderTarget(0);
    }

    protected override void UnloadContent()
    {
        if (listTopLevelActivities != null)
        {
            foreach (CActivity activity in listTopLevelActivities)
                activity.OnUnmanagedReleaseResources();
        }
        
        gameRenderTargetSurface.Dispose();
        gameRenderTargetTexture.Dispose();
        mainRenderTarget.Dispose();

        ImGuiImplD3D9.InvalidateDeviceObjects();
        ImGuiImplD3D9.Shutdown();
            
        ImGui.DestroyContext();
    }
    protected override void OnExiting(EventArgs e)
    {
        CPowerManagement.tEnableMonitorSuspend();		// スリープ抑止状態を解除
        tTerminate();
        base.OnExiting(e);
    }
    protected override void Update(GameTime gameTime)
    {
    }
    protected override void Draw(GameTime gameTime)
    {
        //Do not draw until SoundManager is initialized
        //Fixed issue where exception is raised upon loading when Japanese IME is enabled
        if(SoundManager == null)
        {
            return;
        }

        SoundManager.t再生中の処理をする();

        if (Timer != null)
            Timer.tUpdate();
        if (CSoundManager.rcPerformanceTimer != null)
            CSoundManager.rcPerformanceTimer.tUpdate();

        if (InputManager != null)
            InputManager.tPolling(bApplicationActive, ConfigIni.bバッファ入力を行う);

        if (FPS != null)
            FPS.tカウンタ更新();

        if (Device == null)
            return;

        if (bApplicationActive)	// DTXMania本体起動中の本体/モニタの省電力モード移行を抑止
            CPowerManagement.tDisableMonitorSuspend();

        // #xxxxx 2013.4.8 yyagi; sleepの挿入位置を、EndScnene～Present間から、BeginScene前に移動。描画遅延を小さくするため。
        #region [ スリープ ]
        if (ConfigIni.nフレーム毎スリープms >= 0)			// #xxxxx 2011.11.27 yyagi
        {
            Thread.Sleep(ConfigIni.nフレーム毎スリープms);
        }
        #endregion

        #region [ DTXCreator/DTX2WAVからの指示 ]
        if (Window.IsReceivedMessage)  // ウインドウメッセージで、
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
                    Window.TopMost = true;  // important
                    Window.TopMost = false; // important
                    Window.Focus();         // important

                    bCompactMode = true;
                    strCompactModeFile = DTXVmode.filename;
                }
                if (DTX2WAVmode.Enabled)
                {
                    if (DTX2WAVmode.Command == CDTX2WAVmode.ECommand.Cancel)
                    {
                        Trace.TraceInformation("録音のCancelコマンドをDTXMania本体が受信しました。");
                     
                        if (DTX != null)    // 曲読み込みの前に録音Cancelされると、DTXがnullのままここにきてでGPFとなる→nullチェック追加
                        {
                            DTX.tStopPlayingAllChips();
                            DTX.OnDeactivate();
                        }
                        rCurrentStage.OnDeactivate();

                        Environment.Exit(10010);            // このやり方ならばOK
                    }
                }
            }
        }
        #endregion

        //cache this value to prevent it from changing during the frame
        bool renderToSurface = renderGameToSurface;
        if (renderToSurface)
        {
            Device.SetRenderTarget(0, gameRenderTargetSurface);
        }

        Device.Clear(ClearFlags.ZBuffer | ClearFlags.Target, SharpDX.Color.Black, 1f, 0);
        Device.BeginScene();

        ImGuiImplD3D9.NewFrame();
        ImGui.NewFrame();

        DrawStage();

        InspectorManager.Draw();
        GameStatus.Draw();

        Device.EndScene();

        if (renderToSurface)
        {
            Device.SetRenderTarget(0, mainRenderTarget);
            Device.Clear(ClearFlags.ZBuffer | ClearFlags.Target, SharpDX.Color.Black, 1f, 0);

            GameWindow.Draw(gameRenderTargetTexture);
        }

        ImGui.EndFrame();
        ImGui.Render();
        ImGuiImplD3D9.RenderDrawData(ImGui.GetDrawData());
        
        // Present()は game.csのOnFrameEnd()に登録された、GraphicsDeviceManager.game_FrameEnd() 内で実行されるので不要
        // (つまり、Present()は、Draw()完了後に実行される)
        actFlushGPU.OnUpdateAndDraw();		// Flush GPU	// EndScene()～Present()間 (つまりVSync前) でFlush実行
        
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
            bool bIsMaximized = Window.IsMaximized;											// #23510 2010.11.3 yyagi: to backup current window mode before changing VSyncWait
            currentClientSize = Window.ClientSize;												// #23510 2010.11.3 yyagi: to backup current window size before changing VSyncWait
            DeviceSettings currentSettings = app.GraphicsDeviceManager.CurrentSettings;
            currentSettings.EnableVSync = ConfigIni.bVerticalSyncWait;
            app.GraphicsDeviceManager.ChangeDevice(currentSettings);
            changeVSyncModeOnNextFrame = false;
            Window.ClientSize = new Size(currentClientSize.Width, currentClientSize.Height);	// #23510 2010.11.3 yyagi: to resume window size after changing VSyncWait
            if (bIsMaximized)
            {
                Window.WindowState = FormWindowState.Maximized;								// #23510 2010.11.3 yyagi: to resume window mode after changing VSyncWait
            }
        }
        #endregion
    }

    private void DrawStage()
    {
        if (rCurrentStage != null)
        {
            nUpdateAndDrawReturnValue = (rCurrentStage != null) ? rCurrentStage.OnUpdateAndDraw() : 0;
                
            CScoreIni scoreIni = null;
                
            #region [ Handle enumerating songs ]					// ここに"Enumerating Songs..."表示を集約
            if (!bCompactMode)
            {
                actEnumSongs.OnUpdateAndDraw();							// "Enumerating Songs..."アイコンの描画
            }							// "Enumerating Songs..."アイコンの描画
            switch (rCurrentStage.eStageID)
            {
                case CStage.EStage.Title_2:
                case CStage.EStage.Config_3:
                case CStage.EStage.SongSelection_4:
                case CStage.EStage.SongLoading_5:
                    if (EnumSongs != null)
                    {
                        #region [ (特定条件時) 曲検索スレッドの起動_開始 ]
                        if (rCurrentStage.eStageID == CStage.EStage.Title_2 &&
                            rPreviousStage.eStageID == CStage.EStage.Startup_1 &&
                            nUpdateAndDrawReturnValue == (int)CStageTitle.E戻り値.継続 &&
                            !EnumSongs.IsSongListEnumStarted)
                        {
                            actEnumSongs.OnActivate();
                            stageSongSelection.bIsEnumeratingSongs = true;
                            EnumSongs.Init(SongManager.listSongsDB, SongManager.nNbScoresFromSongsDB);	// songs.db情報と、取得した曲数を、新インスタンスにも与える
                            EnumSongs.StartEnumFromDisk(false);		// 曲検索スレッドの起動_開始
                            if (SongManager.nNbScoresFromSongsDB == 0)	// もし初回起動なら、検索スレッドのプライオリティをLowestでなくNormalにする
                            {
                                EnumSongs.ChangeEnumeratePriority(ThreadPriority.Normal);
                            }
                        }
                        #endregion

                        #region [ 曲検索の中断と再開 ]
                        if (rCurrentStage.eStageID == CStage.EStage.SongSelection_4 && !EnumSongs.IsSongListEnumCompletelyDone)
                        {
                            switch (nUpdateAndDrawReturnValue)
                            {
                                case 0:		// 何もない
                                    //if ( CDTXMania.stageSongSelection.bIsEnumeratingSongs )
                                    if (!stageSongSelection.bIsPlayingPremovie)
                                    {
                                        EnumSongs.Resume();						// #27060 2012.2.6 yyagi 中止していたバックグランド曲検索を再開
                                        EnumSongs.IsSlowdown = false;
                                    }
                                    else
                                    {
                                        // EnumSongs.Suspend();					// #27060 2012.3.2 yyagi #PREMOVIE再生中は曲検索を低速化
                                        EnumSongs.IsSlowdown = true;
                                    }
                                    actEnumSongs.OnActivate();
                                    break;

                                case 2:		// 曲決定
                                    EnumSongs.Suspend();						// #27060 バックグラウンドの曲検索を一時停止
                                    actEnumSongs.OnDeactivate();
                                    break;
                            }
                        }
                        #endregion

                        #region [ 曲探索中断待ち待機 ]
                        if (rCurrentStage.eStageID == CStage.EStage.SongLoading_5 && !EnumSongs.IsSongListEnumCompletelyDone &&
                            EnumSongs.thDTXFileEnumerate != null)							// #28700 2012.6.12 yyagi; at Compact mode, enumerating thread does not exist.
                        {
                            EnumSongs.WaitUntilSuspended();									// 念のため、曲検索が一時中断されるまで待機
                        }
                        #endregion

                        #region [ 曲検索が完了したら、実際の曲リストに反映する ]
                        // CStageSongSelection.OnActivate() に回した方がいいかな？
                        if (EnumSongs.IsSongListEnumerated)
                        {
                            actEnumSongs.OnDeactivate();
                            stageSongSelection.bIsEnumeratingSongs = false;

                            bool bRemakeSongTitleBar = (rCurrentStage.eStageID == CStage.EStage.SongSelection_4) ? true : false;
                            stageSongSelection.Refresh(EnumSongs.SongManager, bRemakeSongTitleBar);
                            EnumSongs.SongListEnumCompletelyDone();
                        }
                        #endregion
                    }
                    break;
            }
            #endregion

            //handle stage changes
            switch (rCurrentStage.eStageID)
            {
                case CStage.EStage.DoNothing_0:
                    break;

                case CStage.EStage.Startup_1:
                    #region [ *** ]
                    //-----------------------------
                    if (nUpdateAndDrawReturnValue != 0)
                    {
                        if (!bCompactMode)
                        {
                            tChangeStage(stageTitle);
                        }
                        else
                        {
                            tChangeStage(stageSongLoading);
                        }
                    }
                    //-----------------------------
                    #endregion
                    break;

                case CStage.EStage.Title_2:
                    #region [ *** ]
                    //-----------------------------
                    if( nUpdateAndDrawReturnValue != 0 )
                    {
                        switch (nUpdateAndDrawReturnValue)
                        {
                            case (int)CStageTitle.E戻り値.GAMESTART:
                                tChangeStage(stageSongSelection);
                                break;

                            case (int)CStageTitle.E戻り値.CONFIG:
                                tChangeStage(stageConfig);
                                break;

                            case (int)CStageTitle.E戻り値.EXIT:
                                tChangeStage(stageEnd);
                                break;
                        }
                    }

                    //-----------------------------
                    #endregion
                    break;

                case CStage.EStage.Config_3:
                    #region [ *** ]
                    //-----------------------------
                    if (nUpdateAndDrawReturnValue != 0)
                    {
                        switch (rPreviousStage.eStageID)
                        {
                            case CStage.EStage.Title_2:
                                tChangeStage(stageTitle);
                                break;

                            case CStage.EStage.SongSelection_4:
                                tChangeStage(stageSongSelection);
                                break;
                        }
                    }
                    //-----------------------------
                    #endregion
                    break;

                case CStage.EStage.SongSelection_4:
                    #region [ *** ]
                    //-----------------------------
                    switch (nUpdateAndDrawReturnValue)
                    {
                        case (int)CStageSongSelection.EReturnValue.ReturnToTitle:
                            tChangeStage(stageTitle);
                            break;

                        case (int)CStageSongSelection.EReturnValue.Selected:
                            tChangeStage(stageSongLoading);
                            break;

                        case (int)CStageSongSelection.EReturnValue.CallConfig:
                            tChangeStage(stageConfig);
                            break;

                        case (int)CStageSongSelection.EReturnValue.ChangeSking:
                            tChangeStage(stageChangeSkin);
                            break;
                    }
                    //-----------------------------
                    #endregion
                    break;

                case CStage.EStage.SongLoading_5:
                    #region [ *** ]
                    //-----------------------------
                    if (nUpdateAndDrawReturnValue != 0)
                    {
                        Pad.stDetectedDevice.Clear();	// 入力デバイスフラグクリア(2010.9.11)
                            
                        #region [ ESC押下時は、曲の読み込みを中止して選曲画面に戻る ]
                        if (nUpdateAndDrawReturnValue == (int)ESongLoadingScreenReturnValue.LoadingStopped)
                        {
                            //DTX.tStopPlayingAllChips();
                            DTX.OnDeactivate();
                            Trace.TraceInformation("曲の読み込みを中止しました。");
                            tRunGarbageCollector();
                                
                            tChangeStage(stageSongSelection);
                            break;
                        }
                        #endregion


                        if (!ConfigIni.bGuitarRevolutionMode)
                        { 
                            tChangeStage(stagePerfDrumsScreen, false);
                        }
                        else
                        {
                            tChangeStage(stagePerfGuitarScreen, false);
                        }
                    }
                    //-----------------------------
                    #endregion
                    break;

                case CStage.EStage.Performance_6:
                    #region [ *** ]
                    //-----------------------------
                    #region [ DTXVモード中にDTXCreatorから指示を受けた場合の処理 ]
                    if (DTXVmode.Enabled && DTXVmode.Refreshed)
                    {
                        DTXVmode.Refreshed = false;

                        if (DTXVmode.Command == CDTXVmode.ECommand.Stop)
                        {
                            ((CStagePerfCommonScreen)rCurrentStage).t停止();

                            //if (previewSound != null)
                            //{
                            //    this.previewSound.tサウンドを停止する();
                            //    this.previewSound.Dispose();
                            //    this.previewSound = null;
                            //}
                        }
                        else if (DTXVmode.Command == CDTXVmode.ECommand.Play)
                        {
                            if (DTXVmode.NeedReload)
                            {
                                ((CStagePerfCommonScreen)rCurrentStage).t再読込();
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
                                ConfigIni.bTimeStretch = DTXVmode.TimeStretch;
                                CSoundManager.bIsTimeStretch = DTXVmode.TimeStretch;
                                if (ConfigIni.bVerticalSyncWait != DTXVmode.VSyncWait)
                                {
                                    ConfigIni.bVerticalSyncWait = DTXVmode.VSyncWait;
                                    //CDTXMania.b次のタイミングで垂直帰線同期切り替えを行う = true;
                                }
                            }
                            else
                            {
                                ((CStagePerfCommonScreen)rCurrentStage).tJumpInSongToBar(DTXVmode.nStartBar);
                            }
                        }
                    }
                    #endregion

                    switch (nUpdateAndDrawReturnValue)
                    {
                        case (int)EPerfScreenReturnValue.Continue:
                            break;

                        case (int)EPerfScreenReturnValue.Interruption:
                        case (int)EPerfScreenReturnValue.Restart:
                            #region [ Cancel performance ]
                            //-----------------------------
                            if (!DTXVmode.Enabled && !DTX2WAVmode.Enabled)
                            {
                                scoreIni = tScoreIniへBGMAdjustとHistoryとPlayCountを更新("Play cancelled");
                            }

                            DTX.tStopPlayingAllChips();
                            DTX.OnDeactivate();
                            rCurrentStage.OnDeactivate();
                            if (bCompactMode && !DTXVmode.Enabled && !DTX2WAVmode.Enabled)
                            {
                                Window.Close();
                            }
                            else if (nUpdateAndDrawReturnValue == (int)EPerfScreenReturnValue.Restart)
                            {
                                tChangeStage(stageSongLoading, true, false);
                            }
                            else
                            {
                                tChangeStage(stageSongSelection, true, false);
                            }
                            break;
                        //-----------------------------
                        #endregion

                        case (int)EPerfScreenReturnValue.StageFailure:
                            #region [ 演奏失敗(StageFailed) ]
                            //-----------------------------
                        {
                            //New extract performance record
                            CScoreIni.CPerformanceEntry cPerf_Drums, cPerf_Guitar, cPerf_Bass;
                            bool bTrainingMode = false;
                            CChip[] chipsArray = new CChip[10];
                            if (ConfigIni.bGuitarRevolutionMode)
                            {
                                stagePerfGuitarScreen.tStorePerfResults(out cPerf_Drums, out cPerf_Guitar, out cPerf_Bass, out bTrainingMode);
                            }
                            else
                            {
                                stagePerfDrumsScreen.tStorePerfResults(out cPerf_Drums, out cPerf_Guitar, out cPerf_Bass, out chipsArray, out bTrainingMode);
                            }
                            //Original
                            //scoreIni = this.tScoreIniへBGMAdjustとHistoryとPlayCountを更新("Stage failed");

                            //Save Performance Records if necessary
                            if (!bTrainingMode) 
                            {
                                //Swap if required
                                if (ConfigIni.bIsSwappedGuitarBass)       // #24063 2011.1.24 yyagi Gt/Bsを入れ替えていたなら、演奏結果も入れ替える
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
                                if (!cPerf_Drums.b全AUTOである && cPerf_Drums.nTotalChipsCount > 0)
                                {
                                    //Drums played
                                    strInstrument = " Drums";
                                    bToSaveProgressBarRecord.Drums = true;
                                }
                                else if (!cPerf_Guitar.b全AUTOである && cPerf_Guitar.nTotalChipsCount > 0)
                                {
                                    if (!cPerf_Bass.b全AUTOである && cPerf_Bass.nTotalChipsCount > 0)
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
                                if (ConfigIni.nPlaySpeed != 20)
                                {
                                    double d = (double)(ConfigIni.nPlaySpeed / 20.0);
                                    strSpeed = (bGuitarAndBass ? " x" : " Speed x") + d.ToString("0.00");
                                }
                                str = string.Format("Stage failed{0} {1}", strInstrument, strSpeed);

                                scoreIni = tScoreIniへBGMAdjustとHistoryとPlayCountを更新(str);

                                CScore cScore = stageSongSelection.rChosenScore;

                                if (bToSaveProgressBarRecord.Drums)
                                {
                                    scoreIni.stSection.LastPlayDrums.strProgress = cPerf_Drums.strProgress;
                                            
                                    if(CScoreIni.tCheckIfUpdateProgressBarRecordOrNot(cScore.SongInformation.progress.Drums, cPerf_Drums.strProgress))
                                    {
                                        scoreIni.stSection.HiSkillDrums.strProgress = cPerf_Drums.strProgress;
                                        bNewProgressBarRecord.Drums = true;
                                    }
                                }
                                if (bToSaveProgressBarRecord.Guitar)
                                {
                                    scoreIni.stSection.LastPlayGuitar.strProgress = cPerf_Guitar.strProgress;
                                    if (CScoreIni.tCheckIfUpdateProgressBarRecordOrNot(cScore.SongInformation.progress.Guitar, cPerf_Guitar.strProgress))
                                    {
                                        scoreIni.stSection.HiSkillGuitar.strProgress = cPerf_Guitar.strProgress;
                                        bNewProgressBarRecord.Guitar = true;
                                    }
                                }
                                if (bToSaveProgressBarRecord.Bass)
                                {
                                    scoreIni.stSection.LastPlayBass.strProgress = cPerf_Bass.strProgress;
                                    if (CScoreIni.tCheckIfUpdateProgressBarRecordOrNot(cScore.SongInformation.progress.Bass, cPerf_Bass.strProgress))
                                    {
                                        scoreIni.stSection.HiSkillBass.strProgress = cPerf_Bass.strProgress;
                                        bNewProgressBarRecord.Bass = true;
                                    }
                                }

                                scoreIni.tExport(DTX.strFileNameFullPath + ".score.ini");

                                if (!bCompactMode)
                                {                                            
                                    bool[] b更新が必要か否か = new bool[3];
                                    CScoreIni.tGetIsUpdateNeeded(out b更新が必要か否か[0], out b更新が必要か否か[1], out b更新が必要か否か[2]);
                                    if (bNewProgressBarRecord.Drums)
                                    {
                                        // New Song Progress
                                        cScore.SongInformation.progress.Drums = cPerf_Drums.strProgress;
                                    }
                                    if (bNewProgressBarRecord.Guitar)
                                    {
                                        // New Song Progress
                                        cScore.SongInformation.progress.Guitar = cPerf_Guitar.strProgress;
                                    }
                                    if (bNewProgressBarRecord.Bass)
                                    {
                                        // New Song Progress
                                        cScore.SongInformation.progress.Bass = cPerf_Bass.strProgress;
                                    }
                                }
                            }
                        }

                            DTX.tStopPlayingAllChips();
                            DTX.OnDeactivate();
                            if (bCompactMode)
                            {
                                Window.Close();
                            }
                            else
                            {
                                tChangeStage(stageSongSelection);
                            }
                            break;
                        //-----------------------------
                        #endregion

                        case (int)EPerfScreenReturnValue.StageClear:
                            #region [ 演奏クリア ]
                            //-----------------------------
                            CScoreIni.CPerformanceEntry cPerfEntry_Drums, cPerfEntry_Guitar, cPerfEntry_Bass;
                            bool bIsTrainingMode = false;
                            CChip[] chipArray = new CChip[10];
                            if (ConfigIni.bGuitarRevolutionMode)
                            {
                                stagePerfGuitarScreen.tStorePerfResults(out cPerfEntry_Drums, out cPerfEntry_Guitar, out cPerfEntry_Bass, out bIsTrainingMode);
                                //Transfer nTimingHitCount to stageResult
                                stageResult.nTimingHitCount = stagePerfGuitarScreen.nTimingHitCount;
                            }
                            else
                            {
                                stagePerfDrumsScreen.tStorePerfResults(out cPerfEntry_Drums, out cPerfEntry_Guitar, out cPerfEntry_Bass, out chipArray, out bIsTrainingMode);
                                //Transfer nTimingHitCount to stageResult
                                stageResult.nTimingHitCount = stagePerfDrumsScreen.nTimingHitCount;
                            }

                            if (!bIsTrainingMode)
                            {
                                if (ConfigIni.bIsSwappedGuitarBass)		// #24063 2011.1.24 yyagi Gt/Bsを入れ替えていたなら、演奏結果も入れ替える
                                {
                                    CScoreIni.CPerformanceEntry t;
                                    t = cPerfEntry_Guitar;
                                    cPerfEntry_Guitar = cPerfEntry_Bass;
                                    cPerfEntry_Bass = t;

                                    DTX.SwapGuitarBassInfos();			// 譜面情報も元に戻す
                                    ConfigIni.SwapGuitarBassInfos_AutoFlags(); // #24415 2011.2.27 yyagi
                                    // リザルト集計時のみ、Auto系のフラグも元に戻す。
                                    // これを戻すのは、リザルト集計後。
                                }													// "case CStage.EStage.Result:"のところ。

                                double ps = 0.0;
                                int nRank = 0;
                                string strInstrument = "";
                                string strPerfSkill = "";
                                bool bGuitarAndBass = false;
                                if (!cPerfEntry_Drums.b全AUTOである && cPerfEntry_Drums.nTotalChipsCount > 0)
                                {
                                    //Drums played
                                    strPerfSkill = String.Format(" {0:F2}", cPerfEntry_Drums.dbPerformanceSkill);
                                    nRank = (ConfigIni.nSkillMode == 0) ? CScoreIni.tCalculateRankOld(cPerfEntry_Drums) : CScoreIni.tCalculateRank(0, cPerfEntry_Drums.dbPerformanceSkill);
                                }
                                else if (!cPerfEntry_Guitar.b全AUTOである && cPerfEntry_Guitar.nTotalChipsCount > 0)
                                {
                                    if (!cPerfEntry_Bass.b全AUTOである && cPerfEntry_Bass.nTotalChipsCount > 0)
                                    {
                                        // Guitar and bass played together
                                        bGuitarAndBass = true;
                                        strPerfSkill = String.Format("{0:F2}/{1:F2}", cPerfEntry_Guitar.dbPerformanceSkill, cPerfEntry_Bass.dbPerformanceSkill);
                                        nRank = CScoreIni.tCalculateOverallRankValue(cPerfEntry_Drums, cPerfEntry_Guitar, cPerfEntry_Bass);
                                        strInstrument = " G+B";
                                    }
                                    else 
                                    {
                                        // Guitar only played
                                        strPerfSkill = String.Format(" {0:F2}", cPerfEntry_Guitar.dbPerformanceSkill);
                                        nRank = (ConfigIni.nSkillMode == 0) ? CScoreIni.tCalculateRankOld(cPerfEntry_Guitar) : CScoreIni.tCalculateRank(0, cPerfEntry_Guitar.dbPerformanceSkill);
                                        strInstrument = " Guitar";
                                    }                                        
                                }
                                else
                                {
                                    //Bass only played
                                    strPerfSkill = String.Format(" {0:F2}", cPerfEntry_Bass.dbPerformanceSkill);
                                    nRank = (ConfigIni.nSkillMode == 0) ? CScoreIni.tCalculateRankOld(cPerfEntry_Bass) : CScoreIni.tCalculateRank(0, cPerfEntry_Bass.dbPerformanceSkill);
                                    strInstrument = " Bass";
                                }

                                string str = "";
                                if (nRank == (int)CScoreIni.ERANK.UNKNOWN)
                                {
                                    str = "Cleared (No chips)";
                                }
                                else
                                {
                                    string strSpeed = "";
                                    if (ConfigIni.nPlaySpeed != 20)
                                    {
                                        double d = (double)(ConfigIni.nPlaySpeed / 20.0);
                                        strSpeed = (bGuitarAndBass ? " x" : " Speed x") + d.ToString("0.00");
                                    }
                                    str = string.Format("Cleared{0} ({1}:{2}{3})", strInstrument, Enum.GetName(typeof(CScoreIni.ERANK), nRank), strPerfSkill, strSpeed);
                                }
                                    
                                scoreIni = tScoreIniへBGMAdjustとHistoryとPlayCountを更新(str);
                            }
                                
                            stageResult.stPerformanceEntry.Drums = cPerfEntry_Drums;
                            stageResult.stPerformanceEntry.Guitar = cPerfEntry_Guitar;
                            stageResult.stPerformanceEntry.Bass = cPerfEntry_Bass;
                            stageResult.rEmptyDrumChip = chipArray;
                            stageResult.bIsTrainingMode = bIsTrainingMode;

                            tChangeStage(stageResult);
                            break;
                        //-----------------------------
                        #endregion
                    }
                    //-----------------------------
                    #endregion
                    break;

                case CStage.EStage.Result_7:
                    #region [ *** ]
                    //-----------------------------
                    if (nUpdateAndDrawReturnValue != 0)
                    {
                        if (ConfigIni.bIsSwappedGuitarBass)		// #24415 2011.2.27 yyagi Gt/Bsを入れ替えていたなら、Auto状態をリザルト画面終了後に元に戻す
                        {
                            ConfigIni.SwapGuitarBassInfos_AutoFlags(); // Auto入れ替え
                        }

                        DTX.tPausePlaybackForAllChips();
                        DTX.OnDeactivate();
                        rCurrentStage.OnDeactivate();
                        if (!bCompactMode)
                        {
                            tChangeStage(stageSongSelection);
                        }
                        else
                        {
                            Window.Close();
                        }
                    }
                    //-----------------------------
                    #endregion
                    break;

                case CStage.EStage.ChangeSkin_9:
                    #region [ *** ]
                    //-----------------------------
                    if (nUpdateAndDrawReturnValue != 0)
                    {
                        tChangeStage(stageSongSelection);
                    }
                    //-----------------------------
                    #endregion
                    break;

                case CStage.EStage.End_8:
                    #region [ *** ]
                    //-----------------------------
                    if (nUpdateAndDrawReturnValue != 0)
                    {
                        Exit();
                    }
                    //-----------------------------
                    #endregion
                    break;
            }
        }
    }

    public void tChangeStage(CStage newStage, bool activateNewStage = true, bool deactivateOldStage = true)
    {
        if (deactivateOldStage)
        {
            rCurrentStage.OnDeactivate();
        }

        Trace.TraceInformation("----------------------");
        Trace.TraceInformation($"■ {newStage.eStageID}");
            
        if (activateNewStage)
        {
            newStage.OnActivate();
        }

        rPreviousStage = rCurrentStage;
        rCurrentStage = newStage;
            
        tRunGarbageCollector();
    }


    // Other

    #region [ 汎用ヘルパー ]
    //-----------------
    public static CTexture tGenerateTexture( string fileName )
    {
        return tGenerateTexture( fileName, false );
    }
    public static CTexture tGenerateTexture( string fileName, bool b黒を透過する )
    {
        if ( app == null )
        {
            return null;
        }
        try
        {
            //Trace.WriteLine("CTextureをFileから生成 + Filename:" + fileName);
            return new CTexture( app.Device, fileName, TextureFormat, b黒を透過する );
        }
        catch ( CTextureCreateFailedException )
        {
            Trace.TraceError( "テクスチャの生成に失敗しました。({0})", fileName );
            return null;
        }
        catch ( FileNotFoundException )
        {
            Trace.TraceError( "テクスチャファイルが見つかりませんでした。({0})", fileName );
            return null;
        }
    }
    public static void tReleaseTexture( ref CTexture? tx )
    {
        if (tx != null) {
            //Trace.WriteLine( "CTextureを解放 Size W:" + tx.szImageSize.Width + " H:" + tx.szImageSize.Height );
            tDisposeSafely( ref tx );
        }
    }
    public static void tReleaseTexture( ref CTextureAf tx )
    {
        tDisposeSafely( ref tx );
    }
    public static CTexture tGenerateTexture( byte[] txData )
    {
        return tGenerateTexture( txData, false );
    }
    public static CTexture tGenerateTexture( byte[] txData, bool b黒を透過する )
    {
        if ( app == null )
        {
            return null;
        }
        try
        {
            return new CTexture( app.Device, txData, TextureFormat, b黒を透過する );
        }
        catch ( CTextureCreateFailedException )
        {
            Trace.TraceError( "テクスチャの生成に失敗しました。(txData)" );
            return null;
        }
    }

    public static CTexture tGenerateTexture( Bitmap bitmap )
    {
        return tGenerateTexture( bitmap, false );
    }
    public static CTexture tGenerateTexture( Bitmap bitmap, bool b黒を透過する )
    {
        if ( app == null )
        {
            return null;
        }
        try
        {
            //Trace.WriteLine( "CTextureをBitmapから生成" );
            return new CTexture( app.Device, bitmap, TextureFormat, b黒を透過する );
        }
        catch ( CTextureCreateFailedException )
        {
            Trace.TraceError( "テクスチャの生成に失敗しました。(txData)" );
            return null;
        }
    }

    public static CTextureAf tテクスチャの生成Af( string fileName )
    {
        return tテクスチャの生成Af( fileName, false );
    }
    public static CTextureAf tテクスチャの生成Af( string fileName, bool b黒を透過する )
    {
        if ( app == null )
        {
            return null;
        }
        try
        {
            return new CTextureAf( app.Device, fileName, TextureFormat, b黒を透過する );
        }
        catch ( CTextureCreateFailedException )
        {
            Trace.TraceError( "テクスチャの生成に失敗しました。({0})", fileName );
            return null;
        }
        catch ( FileNotFoundException )
        {
            Trace.TraceError( "テクスチャファイルが見つかりませんでした。({0})", fileName );
            return null;
        }
    }

    /// <summary>プロパティ、インデクサには ref は使用できないので注意。</summary>
    public static void tDisposeSafely<T>( ref T obj )
    {
        if ( obj == null )
            return;

        if ( obj is IDisposable d )
            d.Dispose();

        obj = default( T );
    }
        
    //https://stackoverflow.com/questions/1600962/displaying-the-build-date
    private static DateTime? GetAssemblyBuildDateTime()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var attr = Attribute.GetCustomAttribute(assembly, typeof(BuildDateTimeAttribute)) as BuildDateTimeAttribute;
        return attr?.Built;
    }
    //-----------------
    #endregion
    #region [ private ]
    //-----------------
    private bool bMouseCursorShown = true;
    public bool bウィンドウがアクティブである = true;
    private bool b終了処理完了済み;
    private static CDTX dtx;
    private List<CActivity> listTopLevelActivities;
    public int nUpdateAndDrawReturnValue;
    private MouseButtons mb = MouseButtons.Left;
    private string strWindowTitle = "";

    //
    public CIMEHook cIMEHook;
    public CActTextBox textboxテキスト入力中;
    public bool bテキスト入力中 => textboxテキスト入力中 != null;

    private void tStartProcess()
    {
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
            strEXEのあるフォルダ = Path.GetDirectoryName(Application.ExecutablePath) + @"\";	// #23629 2010.11.9 yyagi: set correct pathname where DTXManiaGR.exe is.
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
        Window.EnableSystemMenu = ConfigIni.bIsEnabledSystemMenu;	// #28200 2011.5.1 yyagi
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
            catch (UnauthorizedAccessException)			// #24481 2011.2.20 yyagi
            {
                int c = isJapanese ? 0 : 1;
                string[] mes_writeErr = {
                    "DTXManiaLog.txtへの書き込みができませんでした。書き込みできるようにしてから、再度起動してください。",
                    "Failed to write DTXManiaLog.txt. Please set it writable and try again."
                };
                MessageBox.Show(mes_writeErr[c], "DTXMania boot error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }
        }
        Trace.WriteLine("");
        Trace.WriteLine("DTXMania powered by YAMAHA Silent Session Drums");
        Trace.WriteLine(string.Format("Release: {0}", VERSION));
        Trace.WriteLine("");
        Trace.TraceInformation("----------------------");
        Trace.TraceInformation("■ アプリケーションの初期化");
        Trace.TraceInformation("OS Version: " + Environment.OSVersion);
        Trace.TraceInformation("ProcessorCount: " + Environment.ProcessorCount.ToString());
        Trace.TraceInformation("CLR Version: " + Environment.Version.ToString());
        //---------------------
        #endregion
        #region [ Detect compact mode ]
        //---------------------
        /*bCompactMode = false;
        strCompactModeFile = "";
        string[] commandLineArgs = Environment.GetCommandLineArgs();
        if ((commandLineArgs != null) && (commandLineArgs.Length > 1))
        {
            bCompactMode = true;
            strCompactModeFile = commandLineArgs[1];
            Trace.TraceInformation("Start in compact mode. [{0}]", strCompactModeFile);
        }*/
        //---------------------
        #endregion

        #region [ Initialize DTXVmode, DTX2WAVmode, CommandParse classes ]
        //Trace.TraceInformation( "DTXVモードの初期化を行います。" );
        //Trace.Indent();
        try
        {
            DTXVmode = new CDTXVmode();
            DTXVmode.Enabled = false;
            //Trace.TraceInformation( "DTXVモードの初期化を完了しました。" );

            DTX2WAVmode = new CDTX2WAVmode();
            //Trace.TraceInformation( "DTX2WAVモードの初期化を完了しました。" );

            CommandParse = new CCommandParse();
            //Trace.TraceInformation( "CommandParseの初期化を完了しました。" );
        }
        finally
        {
            //Trace.Unindent();
        }
        #endregion
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
                DTXVmode.Refreshed = false;                             // 初回起動時は再読み込みに走らせない
                strCompactModeFile = DTXVmode.filename;
                switch (DTXVmode.soundDeviceType)                       // サウンド再生方式の設定
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
                ConfigIni.nScrollSpeed.Drums = 4;//2.0
                ConfigIni.nScrollSpeed.Guitar = 4;//2.0
                ConfigIni.nScrollSpeed.Bass = 4;//2.0

                //全オート
                for (int i = 0; i < (int)ELane.MAX; i++)
                {
                    ConfigIni.bAutoPlay[i] = true;
                }

                /*CDTXMania.ConfigIni.rcWindow_backup = CDTXMania.ConfigIni.rcWindow;       // #36612 2016.9.12 yyagi
                CDTXMania.ConfigIni.rcWindow.W = CDTXMania.ConfigIni.rcViewerWindow.W;
                CDTXMania.ConfigIni.rcWindow.H = CDTXMania.ConfigIni.rcViewerWindow.H;
                CDTXMania.ConfigIni.rcWindow.X = CDTXMania.ConfigIni.rcViewerWindow.X;
                CDTXMania.ConfigIni.rcWindow.Y = CDTXMania.ConfigIni.rcViewerWindow.Y;*/
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
                ConfigIni.nMasterVolume = DTX2WAVmode.nMixerVolume[(int)CSound.EInstType.Unknown];    // [5](Unknown)のところにMasterVolumeが入ってくるので注意
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
                /*CDTXMania.ConfigIni.rcWindow_backup = CDTXMania.ConfigIni.rcWindow;
                CDTXMania.ConfigIni.rcWindow.W = CDTXMania.ConfigIni.rcViewerWindow.W;
                CDTXMania.ConfigIni.rcWindow.H = CDTXMania.ConfigIni.rcViewerWindow.H;
                CDTXMania.ConfigIni.rcWindow.X = CDTXMania.ConfigIni.rcViewerWindow.X;
                CDTXMania.ConfigIni.rcWindow.Y = CDTXMania.ConfigIni.rcViewerWindow.Y;*/

                //全オート
                for (int i = 0; i < (int)ELane.MAX; i++)
                {
                    ConfigIni.bAutoPlay[i] = true;
                }

                //FillInオフ, 歓声オフ
                ConfigIni.bFillInEnabled = false;
                ConfigIni.b歓声を発声する = false;  // bAudience
                //ストイックモード
                ConfigIni.bストイックモード = false;  // bStoicMode
                //チップ非表示
                ConfigIni.nHidSud.Drums = 4;   // ESudHidInv.FullInv;
                ConfigIni.nHidSud.Guitar = 4;  // ESudHidInv.FullInv;
                ConfigIni.nHidSud.Bass = 4;    // ESudHidInv.FullInv;

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
                ConfigIni.n自動再生音量 = 100;  // nAutoVolume
                ConfigIni.n手動再生音量 = 100;  // nChipVolume

                //マスターボリューム100
                //CDTXMania.ConfigIni.nMasterVolume.Value = 100;	// DTX2WAV側から設定するので、ここでは触らない

                //StageFailedオフ
                ConfigIni.bSTAGEFAILEDEnabled = false;

                //グラフ無効
                ConfigIni.bGraph有効.Drums = false;
                ConfigIni.bGraph有効.Guitar = false;
                ConfigIni.bGraph有効.Bass = false;

                //コンボ非表示,判定非表示
                ConfigIni.bドラムコンボ文字の表示 = false;  // bドラムコンボ文字の表示
                ConfigIni.n表示可能な最小コンボ数.Drums = 0;
                ConfigIni.n表示可能な最小コンボ数.Guitar = 0;  // CDTXMania.ConfigIni.bDisplayCombo.Guitar.Value = false;
                ConfigIni.n表示可能な最小コンボ数.Bass = 0;    // CDTXMania.ConfigIni.bDisplayCombo.Bass.Value = false;
                ConfigIni.bDisplayJudge.Drums = false;
                ConfigIni.bDisplayJudge.Guitar = false;
                ConfigIni.bDisplayJudge.Bass = false;


                //デバッグ表示オフ
                //CDTXMania.ConfigIni.b演奏情報を表示する = false;
                ConfigIni.bHidePerformanceInformation = true;  // bDebugInfo = false

                //BGAオフ, AVIオフ
                ConfigIni.bBGAEnabled = false;
                ConfigIni.bAVIEnabled = false;

                //BGMオン、チップ音オン
                ConfigIni.bBGM音を発声する = true;  // bBGMPlay
                ConfigIni.bドラム打音を発声する = true;  // bDrumsHitSound

                //パート強調オフ
                //CDTXMania.ConfigIni.bEmphasizePlaySound.Drums.Value = false;
                //CDTXMania.ConfigIni.bEmphasizePlaySound.Guitar.Value = false;
                //CDTXMania.ConfigIni.bEmphasizePlaySound.Bass.Value = false;

                // パッド入力等、基本操作の無効化 (ESCを除く)
                //CDTXMania.ConfigIni.KeyAssign[][];

                #endregion
            }
            else                                                        // 通常のコンパクトモード
            {
                strCompactModeFile = commandLineArgs[1];
            }

            if (!File.Exists(strCompactModeFile))      // #32985 2014.1.23 yyagi 
            {
                Trace.TraceError("The file specified in compact mode cannot be found. Terminating DTXMania. [{0}]", strCompactModeFile);
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
        Window.StartPosition = FormStartPosition.Manual;                                                       // #30675 2013.02.04 ikanick add
        Window.Location = new Point(ConfigIni.n初期ウィンドウ開始位置X, ConfigIni.n初期ウィンドウ開始位置Y);   // #30675 2013.02.04 ikanick add

        Window.Text = strWindowTitle;
        Window.ClientSize = new Size(ConfigIni.nWindowWidth, ConfigIni.nWindowHeight);	// #34510 yyagi 2010.10.31 to change window size got from Config.ini
        if (!ConfigIni.bFullScreenExclusive || ConfigIni.bFullScreenMode)						// #23510 2010.11.02 yyagi: add; to recover window size in case bootup with fullscreen mode
        {
            currentClientSize = new Size(ConfigIni.nWindowWidth, ConfigIni.nWindowHeight);
        }
        Window.MaximizeBox = true;							// #23510 2010.11.04 yyagi: to support maximizing window
        Window.FormBorderStyle = FormBorderStyle.Sizable;	// #23510 2010.10.27 yyagi: changed from FixedDialog to Sizable, to support window resize
        Window.ShowIcon = true;
        base.Window.Icon = Properties.Resources.dtx;
        Window.KeyDown += Window_KeyDown;
        Window.KeyUp += Window_KeyUp;
        Window.KeyPress += Window_KeyPress;
        Window.MouseMove += Window_MouseMove;
        Window.MouseDown += Window_MouseDown;
        Window.MouseUp += Window_MouseUp;
        Window.MouseWheel += Window_MouseWheel;
        Window.MouseDoubleClick += Window_MouseDoubleClick;	// #23510 2010.11.13 yyagi: to go fullscreen mode
        Window.ResizeEnd += Window_ResizeEnd;						// #23510 2010.11.20 yyagi: to set resized window size in Config.ini
        Window.ApplicationActivated += Window_ApplicationActivated;
        Window.ApplicationDeactivated += Window_ApplicationDeactivated;
        //Add CIMEHook
        Window.Controls.Add(cIMEHook = new CIMEHook());
        //---------------------
        #endregion
        #region [ Generate Direct3D9 device ]
        //---------------------
        DeviceSettings settings = new DeviceSettings();
        if (ConfigIni.bFullScreenExclusive)
        {
            settings.Windowed = ConfigIni.bWindowMode;
        }
        else
        {
            settings.Windowed = true;								// #30666 2013.2.2 yyagi: Fullscreenmode is "Maximized window" mode
        }
        settings.BackBufferWidth = GameWindowSize.Width;
        settings.BackBufferHeight = GameWindowSize.Height;
        //			settings.BackBufferCount = 3;
        settings.EnableVSync = ConfigIni.bVerticalSyncWait;
        //			settings.BackBufferFormat = Format.A8R8G8B8;
        //			settings.MultisampleType = MultisampleType.FourSamples;
        //			settings.MultisampleQuality = 4;
        //			settings.MultisampleType = MultisampleType.None;
        //			settings.MultisampleQuality = 0;

        try
        {
            GraphicsDeviceManager.ChangeDevice(settings);
        }
        catch (DeviceCreationException e)
        {
            Trace.TraceError(e.ToString());
            MessageBox.Show(e.Message + e.ToString(), "DTXMania failed to boot: DirectX9 Initialize Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(-1);
        }

        IsFixedTimeStep = false;
        //			base.TargetElapsedTime = TimeSpan.FromTicks( 10000000 / 75 );
        Window.ClientSize = new Size(ConfigIni.nWindowWidth, ConfigIni.nWindowHeight);	// #23510 2010.10.31 yyagi: to recover window size. width and height are able to get from Config.ini.
        InactiveSleepTime = TimeSpan.FromMilliseconds((float)(ConfigIni.n非フォーカス時スリープms));	// #23568 2010.11.3 yyagi: to support valiable sleep value when !IsActive
        // #23568 2010.11.4 ikanick changed ( 1 -> ConfigIni )
        if (!ConfigIni.bFullScreenExclusive)
        {
            tSwitchFullScreenMode();               // #30666 2013.2.2 yyagi: finalize settings for "Maximized window mode"
        }
        actFlushGPU = new CActFlushGPU();
        //---------------------
        #endregion
        DTX = null;
        #region [ Initialize Skin Manager ]
        SkinManager = new SkinManager();
        #endregion
        #region [ Initialize Skin ]
        //---------------------
        Trace.TraceInformation("スキンの初期化を行います。");
        Trace.Indent();
        try
        {
            Skin = new CSkin(ConfigIni.strSystemSkinSubfolderFullName, ConfigIni.bUseBoxDefSkin);
            ConfigIni.strSystemSkinSubfolderFullName = Skin.GetCurrentSkinSubfolderFullName(true);	// 旧指定のSkinフォルダが消滅していた場合に備える
            Trace.TraceInformation("スキンの初期化を完了しました。");
        }
        catch
        {
            Trace.TraceInformation("スキンの初期化に失敗しました。");
            throw;
        }
        finally
        {
            Trace.Unindent();
        }
        //---------------------
        #endregion
        #region [ Initialize Timer ]
        //---------------------
        Trace.TraceInformation("タイマの初期化を行います。");
        Trace.Indent();
        try
        {
            Timer = new CTimer(CTimer.EType.MultiMedia);
            Trace.TraceInformation("タイマの初期化を完了しました。");
        }
        finally
        {
            Trace.Unindent();
        }
        //---------------------
        #endregion
        #region [ FPS カウンタの初期化 ]
        //---------------------
        Trace.TraceInformation("FPSカウンタの初期化を行います。");
        Trace.Indent();
        try
        {
            FPS = new CFPS();
            Trace.TraceInformation("FPSカウンタを生成しました。");
        }
        finally
        {
            Trace.Unindent();
        }
        //---------------------
        #endregion
        #region [ act文字コンソールの初期化 ]
        //---------------------
        Trace.TraceInformation("文字コンソールの初期化を行います。");
        Trace.Indent();
        try
        {
            actDisplayString = new CCharacterConsole();
            Trace.TraceInformation("文字コンソールを生成しました。");
            actDisplayString.OnActivate();
            Trace.TraceInformation("文字コンソールを活性化しました。");
            Trace.TraceInformation("文字コンソールの初期化を完了しました。");
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.Message);
            Trace.TraceError("文字コンソールの初期化に失敗しました。");
        }
        finally
        {
            Trace.Unindent();
        }
        //---------------------
        #endregion
        #region [ Initialize Input Manager ]
        //---------------------
        Trace.TraceInformation("DirectInput, MIDI入力の初期化を行います。");
        Trace.Indent();
        try
        {
            InputManager = new CInputManager(Window.Handle);
            foreach (IInputDevice device in InputManager.listInputDevices)
            {
                if ((device.eInputDeviceType == EInputDeviceType.Joystick) && !ConfigIni.dicJoystick.ContainsValue(device.GUID))
                {
                    int key = 0;
                    while (ConfigIni.dicJoystick.ContainsKey(key))
                    {
                        key++;
                    }
                    ConfigIni.dicJoystick.Add(key, device.GUID);
                }
            }
            foreach (IInputDevice device2 in InputManager.listInputDevices)
            {
                if (device2.eInputDeviceType == EInputDeviceType.Joystick)
                {
                    foreach (KeyValuePair<int, string> pair in ConfigIni.dicJoystick)
                    {
                        if (device2.GUID.Equals(pair.Value))
                        {
                            ((CInputJoystick)device2).SetID(pair.Key);
                            break;
                        }
                    }
                    continue;
                }
            }
            Trace.TraceInformation("DirectInput の初期化を完了しました。");
        }
        catch (Exception exception2)
        {
            Trace.TraceError(exception2.Message);
            Trace.TraceError("DirectInput, MIDI入力の初期化に失敗しました。");
            throw;
        }
        finally
        {
            Trace.Unindent();
        }
        //---------------------
        #endregion
        #region [ Initialize Pad ]
        //---------------------
        Trace.TraceInformation("パッドの初期化を行います。");
        Trace.Indent();
        try
        {
            Pad = new CPad(ConfigIni, InputManager);
            Trace.TraceInformation("パッドの初期化を完了しました。");
        }
        catch (Exception exception3)
        {
            Trace.TraceError(exception3.Message);
            Trace.TraceError("パッドの初期化に失敗しました。");
        }
        finally
        {
            Trace.Unindent();
        }
        //---------------------
        #endregion
        #region [ Initialize Sound Manager ]
        //---------------------
        Trace.TraceInformation("サウンドデバイスの初期化を行います。");
        Trace.Indent();
        try
        {
            {
                ESoundDeviceType soundDeviceType;
                switch (ConfigIni.nSoundDeviceType)
                {
                    case 0:
                        soundDeviceType = ESoundDeviceType.DirectSound;
                        break;
                    case 1:
                        soundDeviceType = ESoundDeviceType.ASIO;
                        break;
                    case 2:
                        soundDeviceType = ESoundDeviceType.ExclusiveWASAPI;
                        break;
                    case 3:
                        soundDeviceType = ESoundDeviceType.SharedWASAPI;
                        break;
                    default:
                        soundDeviceType = ESoundDeviceType.Unknown;
                        break;
                }
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
                //FDK.CSound管理.bIsMP3DecodeByWindowsCodec = CDTXMania.ConfigIni.bNoMP3Streaming;


                string strDefaultSoundDeviceBusType = CSoundManager.strDefaultDeviceBusType;
                Trace.TraceInformation($"Bus type of the default sound device = {strDefaultSoundDeviceBusType}");

                Trace.TraceInformation("サウンドデバイスの初期化を完了しました。");
            }
        }
        catch (Exception e)
        {
            Trace.TraceError(e.Message);
            throw;
        }
        finally
        {
            Trace.Unindent();
        }
        //---------------------
        #endregion
        #region [ Initialize Song Manager ]
        //---------------------
        Trace.TraceInformation("曲リストの初期化を行います。");
        Trace.Indent();
        try
        {
            SongManager = new CSongManager();
            //				Songs管理_裏読 = new CSongManager();
            EnumSongs = new CEnumSongs();
            actEnumSongs = new CActEnumSongs();
            Trace.TraceInformation("曲リストの初期化を完了しました。");
        }
        catch (Exception e)
        {
            Trace.TraceError(e.Message);
            Trace.TraceError("曲リストの初期化に失敗しました。");
        }
        finally
        {
            Trace.Unindent();
        }
        //---------------------
        #endregion
        #region [ Initialize Random ]
        //---------------------
        Random = new Random((int)Timer.nシステム時刻);
        //---------------------
        #endregion
        #region [ Initialize Stage ]
        //---------------------
        rCurrentStage = null;
        rPreviousStage = null;
        stageStartup = new CStageStartup();
        stageTitle = new CStageTitle();
        stageConfig = new CStageConfig();
        stageSongSelection = new CStageSongSelection();
        stageSongLoading = new CStageSongLoading();
        stagePerfDrumsScreen = new CStagePerfDrumsScreen();
        stagePerfGuitarScreen = new CStagePerfGuitarScreen();
        stageResult = new CStageResult();
        stageChangeSkin = new CStageChangeSkin();
        stageEnd = new CStageEnd();
        listTopLevelActivities = new List<CActivity>();
        listTopLevelActivities.Add(actEnumSongs);
        listTopLevelActivities.Add(actDisplayString);
        listTopLevelActivities.Add(stageStartup);
        listTopLevelActivities.Add(stageTitle);
        listTopLevelActivities.Add(stageConfig);
        listTopLevelActivities.Add(stageSongSelection);
        listTopLevelActivities.Add(stageSongLoading);
        listTopLevelActivities.Add(stagePerfDrumsScreen);
        listTopLevelActivities.Add(stagePerfGuitarScreen);
        listTopLevelActivities.Add(stageResult);
        listTopLevelActivities.Add(stageChangeSkin);
        listTopLevelActivities.Add(stageEnd);
        listTopLevelActivities.Add(actFlushGPU);
        //---------------------
        #endregion

        Input = new Input();

        #region [ Discord Rich Presence ]
        if (ConfigIni.bDiscordRichPresenceEnabled && !bCompactMode)
            DiscordRichPresence = new CDiscordRichPresence(ConfigIni.strDiscordRichPresenceApplicationID);
        #endregion

        Trace.TraceInformation("アプリケーションの初期化を完了しました。");

        #region [ Launch First stage ]
        //---------------------
        Trace.TraceInformation("----------------------");
        Trace.TraceInformation("■ Startup");

        if (bCompactMode)
        {
            rCurrentStage = stageSongLoading;
        }
        else
        {
            rCurrentStage = stageStartup;
        }
        rCurrentStage.OnActivate();
        //---------------------
        #endregion
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

    private void tTerminate()  // t終了処理
    {
        if (!b終了処理完了済み)
        {
            Trace.TraceInformation("----------------------");
            Trace.TraceInformation("■ アプリケーションの終了");
            #region [ 曲検索の終了処理 ]
            //---------------------
            if (actEnumSongs != null)
            {
                Trace.TraceInformation("曲検索actの終了処理を行います。");
                Trace.Indent();
                try
                {
                    actEnumSongs.OnDeactivate();
                    actEnumSongs = null;
                    Trace.TraceInformation("曲検索actの終了処理を完了しました。");
                }
                catch (Exception e)
                {
                    Trace.TraceError(e.Message);
                    Trace.TraceError("曲検索actの終了処理に失敗しました。");
                }
                finally
                {
                    Trace.Unindent();
                }
            }
            //---------------------
            #endregion
            #region [ 現在のステージの終了処理 ]
            //---------------------
            if (rCurrentStage != null && rCurrentStage.bActivated)		// #25398 2011.06.07 MODIFY FROM
            {
                Trace.TraceInformation("現在のステージを終了します。");
                Trace.Indent();
                try
                {
                    rCurrentStage.OnDeactivate();
                    Trace.TraceInformation("現在のステージの終了処理を完了しました。");
                }
                finally
                {
                    Trace.Unindent();
                }
            }
            //---------------------
            #endregion
            #region [ 曲リストの終了処理 ]
            //---------------------
            if (SongManager != null)
            {
                Trace.TraceInformation("曲リストの終了処理を行います。");
                Trace.Indent();
                try
                {
                    SongManager = null;
                    Trace.TraceInformation("曲リストの終了処理を完了しました。");
                }
                catch (Exception exception)
                {
                    Trace.TraceError(exception.Message);
                    Trace.TraceError("曲リストの終了処理に失敗しました。");
                }
                finally
                {
                    Trace.Unindent();
                }
            }
            //---------------------
            #endregion
            #region [ スキンの終了処理 ]
            //---------------------
            if (Skin != null)
            {
                Trace.TraceInformation("スキンの終了処理を行います。");
                Trace.Indent();
                try
                {
                    Skin.tSaveSkinConfig(); //2016.07.30 kairera0467 #36413
                    Skin.Dispose();
                    Skin = null;
                    Trace.TraceInformation("スキンの終了処理を完了しました。");
                }
                catch (Exception exception2)
                {
                    Trace.TraceError(exception2.Message);
                    Trace.TraceError("スキンの終了処理に失敗しました。");
                }
                finally
                {
                    Trace.Unindent();
                }
            }
            //---------------------
            #endregion
            #region [ DirectSoundの終了処理 ]
            //---------------------
            if (SoundManager != null)
            {
                Trace.TraceInformation("DirectSound の終了処理を行います。");
                Trace.Indent();
                try
                {
                    SoundManager.Dispose();
                    SoundManager = null;
                    Trace.TraceInformation("DirectSound の終了処理を完了しました。");
                }
                catch (Exception exception3)
                {
                    Trace.TraceError(exception3.Message);
                    Trace.TraceError("DirectSound の終了処理に失敗しました。");
                }
                finally
                {
                    Trace.Unindent();
                }
            }
            //---------------------
            #endregion
            #region [ パッドの終了処理 ]
            //---------------------
            if (Pad != null)
            {
                Trace.TraceInformation("パッドの終了処理を行います。");
                Trace.Indent();
                try
                {
                    Pad = null;
                    Trace.TraceInformation("パッドの終了処理を完了しました。");
                }
                catch (Exception exception4)
                {
                    Trace.TraceError(exception4.Message);
                    Trace.TraceError("パッドの終了処理に失敗しました。");
                }
                finally
                {
                    Trace.Unindent();
                }
            }
            //---------------------
            #endregion
            #region [ DirectInput, MIDI入力の終了処理 ]
            //---------------------
            if (InputManager != null)
            {
                Trace.TraceInformation("DirectInput, MIDI入力の終了処理を行います。");
                Trace.Indent();
                try
                {
                    InputManager.Dispose();
                    InputManager = null;
                    Trace.TraceInformation("DirectInput, MIDI入力の終了処理を完了しました。");
                }
                catch (Exception exception5)
                {
                    Trace.TraceError(exception5.Message);
                    Trace.TraceError("DirectInput, MIDI入力の終了処理に失敗しました。");
                }
                finally
                {
                    Trace.Unindent();
                }
            }
            //---------------------
            #endregion
            #region [ 文字コンソールの終了処理 ]
            //---------------------
            if (actDisplayString != null)
            {
                Trace.TraceInformation("文字コンソールの終了処理を行います。");
                Trace.Indent();
                try
                {
                    actDisplayString.OnDeactivate();
                    actDisplayString = null;
                    Trace.TraceInformation("文字コンソールの終了処理を完了しました。");
                }
                catch (Exception exception6)
                {
                    Trace.TraceError(exception6.Message);
                    Trace.TraceError("文字コンソールの終了処理に失敗しました。");
                }
                finally
                {
                    Trace.Unindent();
                }
            }
            //---------------------
            #endregion
            #region [ FPSカウンタの終了処理 ]
            //---------------------
            Trace.TraceInformation("FPSカウンタの終了処理を行います。");
            Trace.Indent();
            try
            {
                if (FPS != null)
                {
                    FPS = null;
                }
                Trace.TraceInformation("FPSカウンタの終了処理を完了しました。");
            }
            finally
            {
                Trace.Unindent();
            }
            //---------------------
            #endregion

            //				ct.Dispose();

            #region [ タイマの終了処理 ]
            //---------------------
            Trace.TraceInformation("タイマの終了処理を行います。");
            Trace.Indent();
            try
            {
                if (Timer != null)
                {
                    Timer.Dispose();
                    Timer = null;
                    Trace.TraceInformation("タイマの終了処理を完了しました。");
                }
                else
                {
                    Trace.TraceInformation("タイマは使用されていません。");
                }
            }
            finally
            {
                Trace.Unindent();
            }
            //---------------------
            #endregion
            #region [ Config.iniの出力 ]
            //---------------------
            Trace.TraceInformation("Config.ini を出力します。");
            //				if ( ConfigIni.bIsSwappedGuitarBass )			// #24063 2011.1.16 yyagi ギターベースがスワップしているときは元に戻す
            if (ConfigIni.bIsSwappedGuitarBass_AutoFlagsAreSwapped)	// #24415 2011.2.21 yyagi FLIP中かつ演奏中にalt-f4で終了したときは、AUTOのフラグをswapして戻す
            {
                ConfigIni.SwapGuitarBassInfos_AutoFlags();
            }
            string str = executableDirectory + "Config.ini";
            Trace.Indent();
            try
            {
                if (DTXVmode.Enabled)
                {
                    //TODO
                    //DTXVmode.tUpdateConfigIni();
                    //Trace.TraceInformation("DTXVモードの設定情報を、Config.xmlに保存しました。");
                }
                else if (DTX2WAVmode.Enabled)
                {
                    //TODO
                    //DTX2WAVmode.tUpdateConfigIni();
                    //Trace.TraceInformation("DTX2WAVモードの設定情報を、Config.xmlに保存しました。");
                    DTX2WAVmode.SendMessage2DTX2WAV("TERM");
                }
                else
                {
                    ConfigIni.tWrite(str);
                    Trace.TraceInformation("保存しました。({0})", new object[] { str });
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);
                Trace.TraceError("Config.ini の出力に失敗しました。({0})", new object[] { str });
            }
            finally
            {
                Trace.Unindent();
            }
            //---------------------
            #endregion

            #region [ Discord Rich Presence ]
            DiscordRichPresence?.Dispose();
            DiscordRichPresence = null;
            #endregion

            Trace.TraceInformation("アプリケーションの終了処理を完了しました。");


            b終了処理完了済み = true;
        }
    }

    private CScoreIni tScoreIniへBGMAdjustとHistoryとPlayCountを更新(string str新ヒストリ行)
    {
        bool bIsUpdatedDrums, bIsUpdatedGuitar, bIsUpdatedBass;
        string strFilename = DTX.strFileNameFullPath + ".score.ini";
        CScoreIni ini = new CScoreIni(strFilename);
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
        CScoreIni.tGetIsUpdateNeeded(out bIsUpdatedDrums, out bIsUpdatedGuitar, out bIsUpdatedBass);
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
                stageSongSelection.rSelectedScore.SongInformation.NbPerformances.Drums = ini.stFile.PlayCountDrums;
                stageSongSelection.rSelectedScore.SongInformation.NbPerformances.Guitar = ini.stFile.PlayCountGuitar;
                stageSongSelection.rSelectedScore.SongInformation.NbPerformances.Bass = ini.stFile.PlayCountBass;
                for (int j = 0; j < ini.stFile.History.Length; j++)
                {
                    stageSongSelection.rSelectedScore.SongInformation.PerformanceHistory[j] = ini.stFile.History[j];
                }
            }
        }
        if (ConfigIni.bScoreIniを出力する)
        {
            ini.tExport(strFilename);
        }

        return ini;
    }
    private void tRunGarbageCollector()
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
                var captureCode = (SlimDX.DirectInput.Key)ConfigIni.KeyAssign.System[(int)EKeyConfigPad.Capture][i].Code;

                if ((int)captureCode > 0 &&
                    e.KeyCode == DeviceConstantConverter.KeyToKeys(captureCode))
                {
                    // Debug.WriteLine( "capture: " + string.Format( "{0:2x}", (int) e.KeyCode ) + " " + (int) e.KeyCode );
                    string strFullPath =
                        Path.Combine(executableDirectory, "Capture_img");
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
            
        // //take window scale into account (since the render resolution for imgui is fixed 1280x720)
        // var windowScale = new Vector2((float)Window.ClientSize.Width / 1280, (float)Window.ClientSize.Height / 720);
        // pos.X = (int)(pos.X / windowScale.X);
        // pos.Y = (int)(pos.Y / windowScale.Y);
            
        ImGui.GetIO().MousePos = new Vector2(pos.X, pos.Y);
    }
        
    private void Window_MouseDown(object? sender, MouseEventArgs e)
    {
        switch (e.Button)
        {
            case MouseButtons.Left:
                ImGui.GetIO().MouseDown[0] = true;
                break;
                
            case MouseButtons.Middle:
                ImGui.GetIO().MouseDown[1] = true;
                break;
                
            case MouseButtons.Right:
                ImGui.GetIO().MouseDown[2] = true;
                break;
        }
    }
        
    private void Window_MouseUp(object? sender, MouseEventArgs e)
    {
        mb = e.Button;
            
        switch (e.Button)
        {
            case MouseButtons.Left:
                ImGui.GetIO().MouseDown[0] = false;
                break;
                
            case MouseButtons.Middle:
                ImGui.GetIO().MouseDown[1] = false;
                break;
                
            case MouseButtons.Right:
                ImGui.GetIO().MouseDown[2] = false;
                break;
        }
    }
        
    private void Window_MouseWheel(object? sender, MouseEventArgs e)
    {
        ImGui.GetIO().MouseWheel = e.Delta / 120.0f;
    }

    private void Window_MouseDoubleClick(object? sender, MouseEventArgs e)	// #23510 2010.11.13 yyagi: to go full screen mode
    {
        if (mb.Equals(MouseButtons.Left) && ConfigIni.bIsAllowedDoubleClickFullscreen)	// #26752 2011.11.27 yyagi
        {
            ConfigIni.bWindowMode = false;
            tSwitchFullScreenMode();
        }
    }
    private void Window_ResizeEnd(object? sender, EventArgs e)				// #23510 2010.11.20 yyagi: to get resized window size
    {
        if (ConfigIni.bWindowMode)
        {
            ConfigIni.n初期ウィンドウ開始位置X = Window.Location.X;	// #30675 2013.02.04 ikanick add
            ConfigIni.n初期ウィンドウ開始位置Y = Window.Location.Y;	//
        }
        
        ConfigIni.nWindowWidth = (ConfigIni.bWindowMode) ? Window.ClientSize.Width : currentClientSize.Width;	// #23510 2010.10.31 yyagi add
        ConfigIni.nWindowHeight = (ConfigIni.bWindowMode) ? Window.ClientSize.Height : currentClientSize.Height;
    }
    #endregion

    //internal sealed class GCBeep	// GC発生の度にbeep
    //{
    //    ~GCBeep()
    //    {
    //        Console.Beep();
    //        if ( !AppDomain.CurrentDomain.IsFinalizingForUnload()
    //            && !Environment.HasShutdownStarted )
    //        {
    //            new GCBeep();
    //        }
    //    }
    //}

    //-----------------
    #endregion
}