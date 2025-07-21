using System.Diagnostics;
using DTXMania.Core;

namespace DTXMania;

internal class CEnumSongs							// #27060 2011.2.7 yyagi 曲リストを取得するクラス
{													// ファイルキャッシュ(songslist.db)からの取得と、ディスクからの取得を、この一つのクラスに集約。
    public CSongManager SongManager						// 曲の探索結果はこのSongs管理に読み込まれる
    {
        get;
        private set;
    }

    public bool IsSongListEnumCompletelyDone => (state == DTXEnumState.CompletelyDone); // 曲リスト探索と、実際の曲リストへの反映が完了した？

    public bool IsEnumerating => thDTXFileEnumerate is { IsAlive: true };
    public bool IsSongListEnumerated => (state == DTXEnumState.Enumeratad); // 曲リスト探索が完了したが、実際の曲リストへの反映はまだ？

    public bool IsSongListEnumStarted => (state != DTXEnumState.None); // 曲リスト探索開始後？(探索完了も含む)

    public void SongListEnumCompletelyDone()
    {
        state = DTXEnumState.CompletelyDone;
        SongManager = null;						// GCはOSに任せる
    }
    public bool IsSlowdown							// #PREMOVIE再生中は検索負荷を落とす
    {
        get => SongManager.bIsSlowdown;
        set => SongManager.bIsSlowdown = value;
    }

    public void ChangeEnumeratePriority(ThreadPriority tp)
    {
        if (thDTXFileEnumerate is { IsAlive: true })
        {
            thDTXFileEnumerate.Priority = tp;
        }
    }
    private readonly string strPathSongsDb = CDTXMania.executableDirectory + "songs.db";
    private readonly string strPathSongList = CDTXMania.executableDirectory + "songlist.db";

    public Thread? thDTXFileEnumerate
    {
        get;
        private set;
    }
    private enum DTXEnumState
    {
        None,
        Ongoing,
        Suspended,
        Enumeratad,				// 探索完了、現在の曲リストに未反映
        CompletelyDone			// 探索完了、現在の曲リストに反映完了
    }
    private DTXEnumState state = DTXEnumState.None;

    /// <summary>
    /// Constractor
    /// </summary>
    public CEnumSongs()
    {
        SongManager = new CSongManager();
    }

    
    
    
    public void HandleEnumSongs(int nUpdateAndDrawReturnValue)
    {
        if (!CDTXMania.bCompactMode)
        {
            CDTXMania.actEnumSongs.OnUpdateAndDraw();
        }

        switch (CDTXMania.StageManager.rCurrentStage.eStageID)
        {
            case CStage.EStage.Title_2:
            case CStage.EStage.Config_3:
            case CStage.EStage.SongSelection_4:
            case CStage.EStage.SongLoading_5:

                #region [ (特定条件時) 曲検索スレッドの起動_開始 ]

                if (CDTXMania.StageManager.rCurrentStage.eStageID == CStage.EStage.Title_2 &&
                    CDTXMania.StageManager.rPreviousStage.eStageID == CStage.EStage.Startup_1 &&
                    nUpdateAndDrawReturnValue == (int)CStageTitle.EReturnResult.CONTINUE &&
                    !IsSongListEnumStarted)
                {
                    CDTXMania.actEnumSongs.OnActivate();
                    CDTXMania.StageManager.stageSongSelection.bIsEnumeratingSongs = true;
                    Init(SongManager.listSongsDB,
                        SongManager.nNbScoresFromSongsDB); // songs.db情報と、取得した曲数を、新インスタンスにも与える
                    StartEnumFromDisk(false); // 曲検索スレッドの起動_開始
                    if (SongManager.nNbScoresFromSongsDB == 0) // もし初回起動なら、検索スレッドのプライオリティをLowestでなくNormalにする
                    {
                        ChangeEnumeratePriority(ThreadPriority.Normal);
                    }
                }

                #endregion

                #region [ 曲検索の中断と再開 ]

                if (CDTXMania.StageManager.rCurrentStage.eStageID == CStage.EStage.SongSelection_4 &&
                    !IsSongListEnumCompletelyDone)
                {
                    switch (nUpdateAndDrawReturnValue)
                    {
                        case 0: // 何もない
                            //if ( CDTXMania.stageSongSelection.bIsEnumeratingSongs )
                            if (!CDTXMania.StageManager.stageSongSelection.bIsPlayingPremovie)
                            {
                                Resume(); // #27060 2012.2.6 yyagi 中止していたバックグランド曲検索を再開
                                IsSlowdown = false;
                            }
                            else
                            {
                                // EnumSongs.Suspend();					// #27060 2012.3.2 yyagi #PREMOVIE再生中は曲検索を低速化
                                IsSlowdown = true;
                            }

                            CDTXMania.actEnumSongs.OnActivate();
                            break;

                        case 2: // 曲決定
                            Suspend(); // #27060 バックグラウンドの曲検索を一時停止
                            CDTXMania.actEnumSongs.OnDeactivate();
                            break;
                    }
                }

                #endregion

                #region [ 曲探索中断待ち待機 ]

                if (CDTXMania.StageManager.rCurrentStage.eStageID == CStage.EStage.SongLoading_5 &&
                    !IsSongListEnumCompletelyDone &&
                    thDTXFileEnumerate !=
                    null) // #28700 2012.6.12 yyagi; at Compact mode, enumerating thread does not exist.
                {
                    WaitUntilSuspended(); // 念のため、曲検索が一時中断されるまで待機
                }

                #endregion

                #region [ 曲検索が完了したら、実際の曲リストに反映する ]

                // CStageSongSelection.OnActivate() に回した方がいいかな？
                if (IsSongListEnumerated)
                {
                    CDTXMania.actEnumSongs.OnDeactivate();
                    CDTXMania.StageManager.stageSongSelection.bIsEnumeratingSongs = false;

                    bool bRemakeSongTitleBar = CDTXMania.StageManager.rCurrentStage.eStageID == CStage.EStage.SongSelection_4;
                    CDTXMania.StageManager.stageSongSelection.Refresh(SongManager, bRemakeSongTitleBar);
                    SongListEnumCompletelyDone();
                }

                #endregion

                break;
        }
    }
    
    
    
    
    public void Init(List<CScore> ls, int n)
    {
        if (state == DTXEnumState.None)
        {
            SongManager.listSongsDB = ls;
            SongManager.nNbScoresFromSongsDB = n;
        }
    }

    /// <summary>
    /// 曲リストのキャッシュ(songlist.db)取得スレッドの開始
    /// </summary>
    public void StartEnumFromCacheStartup(CStageStartup stageStartup)
    {
        thDTXFileEnumerate = new Thread(() => _ = BuildSongListFromCache(stageStartup))
        {
            Name = "Loading song database",
            IsBackground = true
        };
        thDTXFileEnumerate.Start();
    }

    /// <summary>
    /// 曲検索スレッドの開始
    /// </summary>
    public void StartEnumFromDisk(bool readCache)
    {
        if (state is DTXEnumState.None or DTXEnumState.CompletelyDone)
        {
            Trace.TraceInformation("★曲データ検索スレッドを起動しました。");
            lock (this)
            {
                state = DTXEnumState.Ongoing;
            }

            if (SongManager == null)		// Enumerating Songs完了後、CONFIG画面から再スキャンしたときにこうなる
            {
                SongManager = new CSongManager();
            }
                
            thDTXFileEnumerate = new Thread(() => _ = BuildSongListFromDisk(readCache))
            {
                Name = "Enumerating songs",
                IsBackground = true
            };
                
            thDTXFileEnumerate.Start();
        }
    }


    /// <summary>
    /// 曲探索スレッドのサスペンド
    /// </summary>
    public void Suspend()
    {
        if (state != DTXEnumState.CompletelyDone &&
            ((thDTXFileEnumerate.ThreadState & (System.Threading.ThreadState.Background)) != 0))
        {
            // this.thDTXFileEnumerate.Suspend();		// obsoleteにつき使用中止
            SongManager.bIsSuspending = true;
            state = DTXEnumState.Suspended;
            Trace.TraceInformation("★曲データ検索スレッドを中断しました。");
        }
    }

    /// <summary>
    /// 曲探索スレッドのレジューム
    /// </summary>
    public void Resume()
    {
        if (state == DTXEnumState.Suspended)
        {
            if ((thDTXFileEnumerate.ThreadState & (System.Threading.ThreadState.WaitSleepJoin | System.Threading.ThreadState.StopRequested)) != 0)	//
            {
                SongManager.bIsSuspending = false;
                SongManager.AutoReset.Set();
                state = DTXEnumState.Ongoing;
                Trace.TraceInformation("★曲データ検索スレッドを再開しました。");
            }
        }
    }

    /// <summary>
    /// 曲探索スレッドにサスペンド指示を出してから、本当にサスペンド状態に遷移するまでの間、ブロックする
    /// 500ms * 10回＝5秒でタイムアウトし、サスペンド完了して無くてもブロック解除する
    /// </summary>
    public void WaitUntilSuspended()
    {
        // 曲検索が一時中断されるまで待機
        for (int i = 0; i < 10; i++)
        {
            if (state == DTXEnumState.CompletelyDone ||
                (thDTXFileEnumerate.ThreadState & (System.Threading.ThreadState.WaitSleepJoin | System.Threading.ThreadState.Background | System.Threading.ThreadState.Stopped)) != 0)
            {
                break;
            }
            Trace.TraceInformation("★曲データ検索スレッドの中断待ちです: {0}", thDTXFileEnumerate.ThreadState.ToString());
            Thread.Sleep(500);
        }

    }

    /// <summary>
    /// 曲探索スレッドを強制終了する
    /// </summary>
    public void Abort()
    {
        if (thDTXFileEnumerate != null)
        {
            thDTXFileEnumerate.Abort();
            thDTXFileEnumerate = null;
            state = DTXEnumState.None;

            SongManager = null;					// Songs管理を再初期化する (途中まで作った曲リストの最後に、一から重複して追記することにならないようにする。)
            SongManager = new CSongManager();
        }
    }

    public enum SongEnumProgress : int
    {
        ReadSongListDB = 0,
        ReadSongsDB = 1,
        ScanSongs = 2,
        UpdateScoreCache = 3,
        ReadSongData = 4,
        ApplyPostProcessing = 5,
        SaveSongsDB = 6,
        SaveSongListDB = 7
    }

    public SongEnumProgress? EnumProgress { get; private set; }

    /// <summary>
    /// songlist.dbからの曲リスト構築
    /// </summary>
    /// <param name="stageStartup"></param>
    private async Task BuildSongListFromCache(CStageStartup stageStartup)
    {
        // ！注意！
        // 本メソッドは別スレッドで動作するが、プラグイン側でカレントディレクトリを変更しても大丈夫なように、
        // すべてのファイルアクセスは「絶対パス」で行うこと。(2010.9.16)
        // 構築が完了したら、DTXEnumerateState state を DTXEnumerateState.Done にすること。(2012.2.9)
        DateTime now = DateTime.Now;
            
        try
        {
            #region [ 00) songlist.dbの読み込みによる曲リストの構築  ]
            //-----------------------------
            EnumProgress = SongEnumProgress.ReadSongListDB;
            stageStartup.ePhaseID = CStage.EPhase.起動00_songlistから曲リストを作成する;
            DateTime start1 = DateTime.Now;
            Trace.TraceInformation("1) Loading songlist.db ...");
            Trace.Indent();

            try
            {
                if (!CDTXMania.ConfigIni.bConfigIniがないかDTXManiaのバージョンが異なる)
                {
                    CSongManager s = new();
                    s = await Deserialize(strPathSongList);		// 直接this.Songs管理にdeserialize()結果を代入するのは避ける。nullにされてしまうことがあるため。
                    if (s != null)
                    {
                        SongManager = s;
                    }

                    int scores = SongManager.nNbScoresFound;
                    Trace.TraceInformation("Loading songlist.db complete. [{0} scores]", scores);
                    lock (stageStartup.startupScreenConsole)
                    {
                        stageStartup.startupScreenConsole.Add("Loading songlist.db ... OK");
                    }
                }
                else
                {
                    Trace.TraceInformation("初回の起動であるかまたはDTXManiaのバージョンが上がったため、songlist.db の読み込みをスキップします。");
                    lock (stageStartup.startupScreenConsole)
                    {
                        stageStartup.startupScreenConsole.Add("Loading songlist.db ... Skip");
                    }
                }
            }
            finally
            {
                Trace.Unindent();
                TimeSpan currSpan = DateTime.Now - start1;
                Trace.TraceInformation("Duration of Loading songlist.db: {0}", currSpan.ToString());
            }

            #endregion
            #region [ 1) songs.db の読み込み ]
            //-----------------------------
            EnumProgress = SongEnumProgress.ReadSongsDB;
            stageStartup.ePhaseID = CStage.EPhase.起動1_SongsDBからスコアキャッシュを構築;
            start1 = DateTime.Now;
            Trace.TraceInformation("2) Loading songs.db ...");
            Trace.Indent();

            try
            {
                if (!CDTXMania.ConfigIni.bConfigIniがないかDTXManiaのバージョンが異なる)
                {
                    try
                    {
                        SongManager.tReadSongsDB(strPathSongsDb);
                    }
                    catch
                    {
                        Trace.TraceError("Fail to load songs.db");
                    }

                    int scores = (SongManager == null) ? 0 : SongManager.nNbScoresFromSongsDB;	// 読み込み途中でアプリ終了した場合など、CDTXMania.SongManager がnullの場合があるので注意
                    Trace.TraceInformation("Loading songs.db complete. [{0} scores]", scores);
                    lock (stageStartup.startupScreenConsole)
                    {
                        stageStartup.startupScreenConsole.Add("Loading songs.db ... OK");
                    }
                }
                else
                {
                    Trace.TraceInformation("初回の起動であるかまたはDTXManiaのバージョンが上がったため、songs.db の読み込みをスキップします。");
                    lock (stageStartup.startupScreenConsole)
                    {
                        stageStartup.startupScreenConsole.Add("Loading songs.db ... Skip");
                    }
                }
            }
            finally
            {
                Trace.Unindent();
                TimeSpan currSpan = DateTime.Now - start1;
                Trace.TraceInformation("Duration of Loading songs.db: {0}", currSpan.ToString());
            }
            //-----------------------------
            #endregion
        }
        finally
        {
            stageStartup.ePhaseID = CStage.EPhase.起動7_完了;
            TimeSpan span = DateTime.Now - now;
            Trace.TraceInformation("Initialization Time: {0}", span.ToString());
            lock (this)							// #28700 2012.6.12 yyagi; state change must be in finally{} for exiting as of compact mode.
            {
                state = DTXEnumState.CompletelyDone;
            }
        }
    }
        
    /// <summary>
    /// 起動してタイトル画面に遷移した後にバックグラウンドで発生させる曲検索
    /// #27060 2012.2.6 yyagi
    /// </summary>
    private async Task BuildSongListFromDisk(bool bReadCache)
    {
        // ！注意！
        // 本メソッドは別スレッドで動作するが、プラグイン側でカレントディレクトリを変更しても大丈夫なように、
        // すべてのファイルアクセスは「絶対パス」で行うこと。(2010.9.16)
        // 構築が完了したら、DTXEnumerateState state を DTXEnumerateState.Done にすること。(2012.2.9)

        DateTime startTime = DateTime.Now;

        try
        {
            Trace.TraceInformation("Read cache option: " + bReadCache);
            if (bReadCache)
            {
                try
                {
                    #region [ 00) songlist.dbの読み込みによる曲リストの構築  ]
                    //-----------------------------
                    EnumProgress = SongEnumProgress.ReadSongListDB;

                    DateTime start1 = DateTime.Now;
                    Trace.TraceInformation("1) Loading songlist.db ...");
                    Trace.Indent();

                    try
                    {
                        if (!CDTXMania.ConfigIni.bConfigIniがないかDTXManiaのバージョンが異なる)
                        {
                            CSongManager s = await Deserialize(strPathSongList); // 直接this.Songs管理にdeserialize()結果を代入するのは避ける。nullにされてしまうことがあるため。
                            if (s != null)
                            {
                                SongManager = s;
                            }

                            int scores = SongManager.nNbScoresFound;
                            Trace.TraceInformation("Loading songlist.db complete. [{0} scores]", scores);
                        }
                        else
                        {
                            Trace.TraceInformation("初回の起動であるかまたはDTXManiaのバージョンが上がったため、songlist.db の読み込みをスキップします。");
                        }
                    }
                    finally
                    {
                        Trace.Unindent();
                        TimeSpan currSpan = DateTime.Now - start1;
                        Trace.TraceInformation("Duration of Loading songlist.db: {0}", currSpan.ToString());
                    }

                    #endregion
                    #region [ 1) songs.db の読み込み ]
                    //-----------------------------
                    EnumProgress = SongEnumProgress.ReadSongsDB;
        
                    start1 = DateTime.Now;
                    Trace.TraceInformation("2) Loading songs.db ...");
                    Trace.Indent();

                    try
                    {
                        if (!CDTXMania.ConfigIni.bConfigIniがないかDTXManiaのバージョンが異なる)
                        {
                            try
                            {
                                SongManager.tReadSongsDB(strPathSongsDb);
                            }
                            catch
                            {
                                Trace.TraceError("Fail to load songs.db");
                            }

                            int scores = (SongManager == null) ? 0 : SongManager.nNbScoresFromSongsDB;    // 読み込み途中でアプリ終了した場合など、CDTXMania.SongManager がnullの場合があるので注意
                            Trace.TraceInformation("Loading songs.db complete. [{0} scores]", scores);
                        }
                        else
                        {
                            Trace.TraceInformation("初回の起動であるかまたはDTXManiaのバージョンが上がったため、songs.db の読み込みをスキップします。");
                        }
                    }
                    finally
                    {
                        Trace.Unindent();
                        TimeSpan currSpan = DateTime.Now - start1;
                        Trace.TraceInformation("Duration of Loading songs.db: {0}", currSpan.ToString());
                    }
                    //-----------------------------
                    #endregion
                }
                finally
                {
                    CDTXMania.SongManager = SongManager;
                    //Reset temporary SongManager
                    SongManager = new CSongManager();
                    //Init temporary SongManager with SongListDB and Count
                    SongManager.listSongsDB = CDTXMania.SongManager.listSongsDB;
                    SongManager.nNbScoresFromSongsDB = CDTXMania.SongManager.nNbScoresForSongsDB;
                }                    
            }

            #region [ 2) 曲データの検索 ]
            EnumProgress = SongEnumProgress.ScanSongs;

            //	base.ePhaseID = CStage.EPhase.起動2_曲を検索してリストを作成する;
            DateTime start = DateTime.Now;
            Trace.TraceInformation("enum2) Searching Song Data ...");
            
            try
            {
                if (!string.IsNullOrEmpty(CDTXMania.ConfigIni.strSongDataSearchPath))
                {
                    string[] strArray = CDTXMania.ConfigIni.strSongDataSearchPath.Split(new char[] { ';' });
                    if (strArray.Length > 0)
                    {
                        // 全パスについて…
                        foreach (string str in strArray)
                        {
                            string path = str;
                            if (!Path.IsPathRooted(path))
                            {
                                path = CDTXMania.executableDirectory + str;	// 相対パスの場合、絶対パスに直す(2010.9.16)
                            }

                            if (!string.IsNullOrEmpty(path))
                            {
                                Trace.TraceInformation("Search Path: " + path);
                                Trace.Indent();

                                try
                                {
                                    SongManager.tSearchSongsAndCreateList(path);
                                }
                                catch (Exception e)
                                {
                                    Trace.TraceError(e.Message);
                                    Trace.TraceError(e.StackTrace);
                                    Trace.TraceError("An exception occurred but processing will continue.");
                                }
                                finally
                                {
                                    Trace.Unindent();                                        
                                }
                            }
                        }
                    }
                }
                else
                {
                    Trace.TraceWarning("No song data search path (DTXPath) specified in config.ini file. ");
                }
            }
            finally
            {
                TimeSpan currSpan = DateTime.Now - start;
                
                Trace.TraceInformation("Song Data search complete. [{0} songs {1} scores]", SongManager.nNbSongNodesFound, SongManager.nNbScoresFound);
                Trace.TraceInformation("Duration of enum2) Searching Song Data : {0}", currSpan.ToString());
            }
            #endregion
            #region [ 3) songs.db 情報の曲リストへの反映 ]
            //-----------------------------
            EnumProgress = SongEnumProgress.UpdateScoreCache;

            //					base.ePhaseID = CStage.EPhase.起動3_スコアキャッシュをリストに反映する;
            start = DateTime.Now;
            Trace.TraceInformation("enum3) Loading score cache into songs.db. ");
            Trace.Indent();

            try
            {
                if (SongManager.listSongsDB != null)
                {
                    SongManager.tReflectScoreCacheInSongList();
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);
                Trace.TraceError(e.StackTrace);
                Trace.TraceError("An exception has occurred, but processing will continue.");
            }
            finally
            {
                Trace.TraceInformation("Score cache loading complete. [{0}/{1}スコア]", SongManager.nNbScoresFromScoreCache, SongManager.nNbScoresFound);
                Trace.Unindent();
                TimeSpan currSpan = DateTime.Now - start;
                Trace.TraceInformation("Duration of enum3) Loading score cache into songs.db. : {0}", currSpan.ToString());
            }
            #endregion
            #region [ 4) songs.db になかった曲データをファイルから読み込んで反映 ]
            EnumProgress = SongEnumProgress.ReadSongData;
            
            int num2 = SongManager.nNbScoresFound - SongManager.nNbScoresFromScoreCache;
                
            Trace.TraceInformation("{0}, {1}", SongManager.nNbScoresFound, SongManager.nNbScoresFromScoreCache);
            start = DateTime.Now;
            Trace.TraceInformation("enum4) Reads and copy song data information from files [{0} scores] that were not in songs.db. ", num2);
            Trace.Indent();

            try
            {
                await SongManager.PrepareProcessNewSongData();
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);
                Trace.TraceError(e.StackTrace);
                Trace.TraceError("An exception has occurred, but processing will continue.");
            }
            finally
            {
                Trace.TraceInformation("Copying into song data complete. [{0}/{1} scores]", SongManager.nNbScoresFromFile, num2);
                Trace.Unindent();
                TimeSpan currSpan = DateTime.Now - start;
                Trace.TraceInformation("Duration of enum4) Reads and copy song data: {0}", currSpan.ToString());
            }
            #endregion
            #region [ 5) 曲リストへの後処理の適用 ]
            //-----------------------------
            EnumProgress = SongEnumProgress.ApplyPostProcessing;

            //					base.ePhaseID = CStage.EPhase.起動5_曲リストへ後処理を適用する;
            start = DateTime.Now;
            Trace.TraceInformation("enum5) Apply post-processing to song list.");
            Trace.Indent();

            try
            {
                SongManager.t曲リストへ後処理を適用する();
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);
                Trace.TraceError(e.StackTrace);
                Trace.TraceError("An exception has occurred, but processing will continue.");
            }
            finally
            {
                Trace.TraceInformation("Post-processing to song list completed.");
                Trace.Unindent();
                TimeSpan currSpan = DateTime.Now - start;
                Trace.TraceInformation("Duration of enum5) Apply post-processing to song list: {0}", currSpan.ToString());
            }
            //					lock ( this.list進行文字列 )
            //					{
            //						this.list進行文字列.Add( string.Format( "{0} ... OK", "Building songlists" ) );
            //					}
            //-----------------------------
            #endregion
            #region [ 6) songs.db への保存 ]
            //-----------------------------
            EnumProgress = SongEnumProgress.SaveSongsDB;
            //					base.ePhaseID = CStage.EPhase.起動6_スコアキャッシュをSongsDBに出力する;
            start = DateTime.Now;
            Trace.TraceInformation("enum6) Saving song metadata into songs.db.");
            Trace.Indent();

            try
            {
                SongManager.tスコアキャッシュをSongsDBに出力する(strPathSongsDb);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);
                Trace.TraceError(e.StackTrace);
                Trace.TraceError("An exception has occurred, but processing will continue.");
            }
            finally
            {
                Trace.TraceInformation("Saving into songs.db complete.[{0} scores]", SongManager.nNbScoresForSongsDB);
                Trace.Unindent();
                TimeSpan currSpan = DateTime.Now - start;
                Trace.TraceInformation("Duration of enum6) Saving song metadata into songs.db: {0}", currSpan.ToString());
            }
            //					lock ( this.list進行文字列 )
            //					{
            //						this.list進行文字列.Add( string.Format( "{0} ... OK", "Saving songs.db" ) );
            //					}
            #endregion
            #region [ 7) songs2.db への保存 ]		// #27060 2012.1.26 yyagi
            //-----------------------------
            EnumProgress = SongEnumProgress.SaveSongListDB;
            start = DateTime.Now;
            Trace.TraceInformation("enum7) Saving additional song data into songlist.db.");
            Trace.Indent();

            SerializeSongList(SongManager, strPathSongList);
                
            Trace.TraceInformation("Saving into songlist.db complete. [{0} scores]", SongManager.nNbScoresForSongsDB);
            Trace.Unindent();
            TimeSpan span = DateTime.Now - start;
            Trace.TraceInformation("Duration of enum7) Saving additional song data into songlist.db: {0}", span.ToString());
            //-----------------------------
            #endregion
        }
        finally
        {
            TimeSpan span = DateTime.Now - startTime;
            Trace.TraceInformation("Duration of full Song Enumerating: {0}", span.ToString());
                
            EnumProgress = null;
        }
        lock (this)
        {
            // state = DTXEnumState.Done;		// DoneにするのはCDTXMania.cs側にて。
            state = DTXEnumState.Enumeratad;
        }
    }
        
    /// <summary>
    /// 曲リストのserialize
    /// </summary>
    private static void SerializeSongList(CSongManager cs, string strPathSongList)
    {
        bool bSucceededSerialize = true;
        Stream output = null;
        try
        {
            Trace.TraceError("BinaryFormatter is no longer supported apparently, so we can't load songlist.db");

            // output = File.Create(strPathSongList);
            // BinaryFormatter formatter = new BinaryFormatter();
            // formatter.Serialize(output, cs);
            bSucceededSerialize = false;
        }
        catch (Exception e)
        {
            bSucceededSerialize = false;
            Trace.TraceError(e.Message);
            Trace.TraceError(e.StackTrace);
            Trace.TraceError("例外が発生しましたが処理を継続します。");
        }
        finally
        {
            //output.Close();
            if (!bSucceededSerialize)
            {
                try
                {
                    File.Delete(strPathSongList);	// serializeに失敗したら、songs2.dbファイルを消しておく
                }
                catch (Exception)
                {
                    // 特に何もしない
                }
            }
        }
    }

    /// <summary>
    /// 曲リストのdeserialize
    /// </summary>
    /// <param name="songs管理"></param>
    /// <param name="strPathSongList"></param>
    private static async Task<CSongManager> Deserialize(string strPathSongList)
    {
        CSongManager songs管理 = null;
        if (File.Exists(strPathSongList))
        {
            await Task.Run(delegate 
            {
                try
                {
                    #region [ SongListDB(songlist.db)を読み込む ]
                    //	byte[] buf = File.ReadAllBytes( SongListDBファイル名 );			// 一旦メモリにまとめ読みしてからdeserializeした方が高速かと思ったら全く変わらなかったので削除
                    //	using ( MemoryStream input = new MemoryStream(buf, false) )
                    using Stream input = File.OpenRead(strPathSongList);
                    try
                    {
                        Trace.TraceError("BinaryFormatter is no longer supported apparently, so we can't load songlist.db");
                        //BinaryFormatter formatter = new BinaryFormatter();
                        //songs管理 = (CSongManager)formatter.Deserialize(input);
                    }
                    catch (Exception)
                    {
                        // songs管理 = null;
                    }

                    #endregion
                }
                catch
                {
                    Trace.TraceError("Fail to load songlist.db");
                }
            });
        }
        else 
        {
            Trace.TraceInformation("songlist.db does not exist");
        }

        return songs管理;
    }
}