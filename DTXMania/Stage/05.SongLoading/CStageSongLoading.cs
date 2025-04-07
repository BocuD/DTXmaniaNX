using DiscordRPC;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing;
using DTXMania.Core;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using DTXUIRenderer;
using SharpDX;
using FDK;
using Color = System.Drawing.Color;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using RectangleF = SharpDX.RectangleF;
using SlimDXKey = SlimDX.DirectInput.Key;

namespace DTXMania;

internal class CStageSongLoading : CStage
{
    // retain presence from song select
    protected override RichPresence Presence => null;

    private CDTX cdtx;
    
    // コンストラクタ

    public CStageSongLoading()
    {
        eStageID = EStage.SongLoading_5;
        ePhaseID = EPhase.Common_DefaultState;
        bNotActivated = true;
        //			base.listChildActivities.Add( this.actFI = new CActFIFOBlack() );	// #27787 2012.3.10 yyagi 曲読み込み画面のフェードインの省略
        listChildActivities.Add(actFO = new CActFIFOBlackStart());

        #region[ 難易度数字 ]
        //大文字
        STCharacterPosition[] st文字位置Array3 = new STCharacterPosition[12];
        STCharacterPosition st文字位置23 = new()
        {
            ch = '.',
            pt = new Point(1000, 0)
        };
        st文字位置Array3[0] = st文字位置23;
        STCharacterPosition st文字位置24 = new()
        {
            ch = '1',
            pt = new Point(100, 0)
        };
        st文字位置Array3[1] = st文字位置24;
        STCharacterPosition st文字位置25 = new()
        {
            ch = '2',
            pt = new Point(200, 0)
        };
        st文字位置Array3[2] = st文字位置25;
        STCharacterPosition st文字位置26 = new()
        {
            ch = '3',
            pt = new Point(300, 0)
        };
        st文字位置Array3[3] = st文字位置26;
        STCharacterPosition st文字位置27 = new()
        {
            ch = '4',
            pt = new Point(400, 0)
        };
        st文字位置Array3[4] = st文字位置27;
        STCharacterPosition st文字位置28 = new()
        {
            ch = '5',
            pt = new Point(500, 0)
        };
        st文字位置Array3[5] = st文字位置28;
        STCharacterPosition st文字位置29 = new()
        {
            ch = '6',
            pt = new Point(600, 0)
        };
        st文字位置Array3[6] = st文字位置29;
        STCharacterPosition st文字位置30 = new()
        {
            ch = '7',
            pt = new Point(700, 0)
        };
        st文字位置Array3[7] = st文字位置30;
        STCharacterPosition st文字位置31 = new()
        {
            ch = '8',
            pt = new Point(800, 0)
        };
        st文字位置Array3[8] = st文字位置31;
        STCharacterPosition st文字位置32 = new()
        {
            ch = '9',
            pt = new Point(900, 0)
        };
        st文字位置Array3[9] = st文字位置32;
        STCharacterPosition st文字位置33 = new()
        {
            ch = '0',
            pt = new Point(0, 0)
        };
        st文字位置Array3[10] = st文字位置33;
        STCharacterPosition st文字位置34 = new()
        {
            ch = '-',
            pt = new Point(0, 0)
        };
        st文字位置Array3[11] = st文字位置34;
        st大文字位置 = st文字位置Array3;

        #endregion

        stPanelMap = new STATUSPANEL[12]; // yyagi: 以下、手抜きの初期化でスマン
        string[] labels =
        [
            "DTXMANIA", //0
            "DEBUT", //1
            "NOVICE", //2
            "REGULAR", //3
            "EXPERT", //4
            "MASTER", //5
            "BASIC", //6
            "ADVANCED", //7
            "EXTREME", //8
            "RAW", //9
            "RWS", //10
            "REAL" //11
        ];
        int[] status = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11];

        for (int i = 0; i < 12; i++)
        {
            stPanelMap[i] = default;
            stPanelMap[i].status = status[i];
            stPanelMap[i].label = labels[i];
        }
    }

    // CStage 実装

    private void tDetermineStatusLabelFromLabelName(string strLabelName)
    {
        if (string.IsNullOrEmpty(strLabelName))
        {
            nIndex = 0;
        }
        else
        {
            STATUSPANEL[] array = stPanelMap;
            foreach (STATUSPANEL sTATUSPANEL in array)
            {
                if (strLabelName.Equals(sTATUSPANEL.label, StringComparison.CurrentCultureIgnoreCase))
                {
                    nIndex = sTATUSPANEL.status;
                    CDTXMania.nSongDifficulty = sTATUSPANEL.status;
                    return;
                }

                nIndex++;
            }
        }
    }

    public override void InitializeBaseUI()
    {
        
    }
    
    public override void InitializeDefaultUI()
    {
        DTXTexture bgTex = new(CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\6_background.jpg")));
        UIImage bg = ui.AddChild(new UIImage(bgTex));
        bg.renderOrder = -100;
    }

    public override void OnActivate()
    {
        Trace.TraceInformation("曲読み込みステージを活性化します。");
        Trace.Indent();
        try
        {
            strSongTitle = "";
            strArtistName = "";
            strSTAGEFILE = "";

            nBGMPlayStartTime = -1L;
            nBGMTotalPlayTimeMs = 0;
            if (sdLoadingSound != null)
            {
                CDTXMania.SoundManager.tDiscard(sdLoadingSound);
                sdLoadingSound = null;
            }

            string strDTXFilePath = (CDTXMania.bCompactMode)
                ? CDTXMania.strCompactModeFile
                : CDTXMania.stageSongSelection.rChosenScore.FileInformation.AbsoluteFilePath;

            cdtx = new(strDTXFilePath, true);

            if (!CDTXMania.bCompactMode && CDTXMania.ConfigIni.b曲名表示をdefのものにする)
                strSongTitle = CDTXMania.stageSongSelection.rConfirmedSong.strTitle;
            else
                strSongTitle = cdtx.TITLE;

            strArtistName = cdtx.ARTIST;
            if (cdtx.SOUND_NOWLOADING is { Length: > 0 } && File.Exists(cdtx.strFolderName + cdtx.SOUND_NOWLOADING)
                                                         && (!CDTXMania.DTXVmode.Enabled)
                                                         && (!CDTXMania.DTX2WAVmode.Enabled))
            {
                string currentlyLoadingSoundFilePath = cdtx.strFolderName + cdtx.SOUND_NOWLOADING;
                try
                {
                    sdLoadingSound = CDTXMania.SoundManager.tGenerateSound(currentlyLoadingSoundFilePath);
                }
                catch
                {
                    Trace.TraceError("#SOUND_NOWLOADING に指定されたサウンドファイルの読み込みに失敗しました。({0})",
                        currentlyLoadingSoundFilePath);
                }
            }

            // 2015.12.26 kairera0467 本家DTXからつまみ食い。
            // #35411 2015.08.19 chnmr0 add
            // Read ghost data by config
            // It does not exist a ghost file for 'perfect' actually
            string[] inst = ["dr", "gt", "bs"];
            if (CDTXMania.ConfigIni.bIsSwappedGuitarBass)
            {
                inst[1] = "bs";
                inst[2] = "gt";
            }

            for (int instIndex = 0; instIndex < inst.Length; ++instIndex)
            {
                //break; //2016.01.03 kairera0467 以下封印。
                bool readAutoGhostCond = false;
                readAutoGhostCond |= instIndex == 0 && CDTXMania.ConfigIni.bAllDrumsAreAutoPlay;
                readAutoGhostCond |= instIndex == 1 && CDTXMania.ConfigIni.bAllGuitarsAreAutoPlay;
                readAutoGhostCond |= instIndex == 2 && CDTXMania.ConfigIni.bAllBassAreAutoPlay;

                CDTXMania.listTargetGhsotLag[instIndex] = null;
                CDTXMania.listAutoGhostLag[instIndex] = null;
                CDTXMania.listTargetGhostScoreData[instIndex] = null;
                nCurrentInst = instIndex;

                if (readAutoGhostCond)
                {
                    string[] prefix = ["perfect", "lastplay", "hiskill", "hiscore", "online"];
                    int indPrefix = (int)CDTXMania.ConfigIni.eAutoGhost[instIndex];
                    string filename = cdtx.strFolderName + "\\" + cdtx.strFileName + "." + prefix[indPrefix] + "." +
                                      inst[instIndex] + ".ghost";
                    if (File.Exists(filename))
                    {
                        CDTXMania.listAutoGhostLag[instIndex] = [];
                        CDTXMania.listTargetGhostScoreData[instIndex] = new CScoreIni.CPerformanceEntry();
                        ReadGhost(filename, CDTXMania.listAutoGhostLag[instIndex]);
                    }
                }

                if (CDTXMania.ConfigIni.eTargetGhost[instIndex] != ETargetGhostData.NONE)
                {
                    string[] prefix = ["none", "perfect", "lastplay", "hiskill", "hiscore", "online"];
                    int indPrefix = (int)CDTXMania.ConfigIni.eTargetGhost[instIndex];
                    string filename = cdtx.strFolderName + "\\" + cdtx.strFileName + "." + prefix[indPrefix] + "." +
                                      inst[instIndex] + ".ghost";
                    if (File.Exists(filename))
                    {
                        CDTXMania.listTargetGhsotLag[instIndex] = [];
                        CDTXMania.listTargetGhostScoreData[instIndex] = new CScoreIni.CPerformanceEntry();
                        stGhostLag[instIndex] = [];
                        ReadGhost(filename, CDTXMania.listTargetGhsotLag[instIndex]);
                    }
                    else if (CDTXMania.ConfigIni.eTargetGhost[instIndex] == ETargetGhostData.PERFECT)
                    {
                        // All perfect
                        CDTXMania.listTargetGhsotLag[instIndex] = [];
                    }
                }
            }

            cdtx.OnDeactivate();
            base.OnActivate();
            if (!CDTXMania.bCompactMode && !CDTXMania.DTXVmode.Enabled && !CDTXMania.DTX2WAVmode.Enabled)
                tDetermineStatusLabelFromLabelName(
                    CDTXMania.stageSongSelection.rConfirmedSong.arDifficultyLabel[
                        CDTXMania.stageSongSelection.nConfirmedSongDifficulty]);
            
            //add difficulty panel to ui here
            //todo: this should be moved when chart loading is moved
            DTXTexture difficultyPanelTex = new(CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\6_Difficulty.png")));
            UIImage difficultyPanel = ui.AddChild(new UIImage(difficultyPanelTex));
            difficultyPanel.renderMode = ERenderMode.Sliced;
            difficultyPanel.position = new Vector3(191, 102, 0);
            difficultyPanel.clipRect = new RectangleF(0, nIndex * 50, 262, 50);
            difficultyPanel.isVisible = false;
        }
        finally
        {
            Trace.TraceInformation("曲読み込みステージの活性化を完了しました。");
            Trace.Unindent();
        }
    }

    public override void OnDeactivate()
    {
        Trace.TraceInformation("曲読み込みステージを非活性化します。");
        Trace.Indent();
        try
        {
            base.OnDeactivate();
        }
        finally
        {
            Trace.TraceInformation("曲読み込みステージの非活性化を完了しました。");
            Trace.Unindent();
        }
    }

    public override void OnManagedCreateResources()
    {
        if (!bNotActivated)
        {
            txLevel = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\6_LevelNumber.png"));
            txDifficultyPanel = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\6_Difficulty.png"));
            txPartPanel = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\6_Part.png"));

            #region[ 曲名、アーティスト名テクスチャの生成 ]

            try
            {
                #region[ 曲名、アーティスト名テクスチャの生成 ]

                if (!string.IsNullOrWhiteSpace(strSongTitle))
                {
                    titleFont = new CPrivateFastFont(new FontFamily(CDTXMania.ConfigIni.songListFont), 40,
                        FontStyle.Regular);
                     Bitmap bmpSongName = titleFont.DrawPrivateFont(strSongTitle, CPrivateFont.DrawMode.Edge, Color.Black,
                        Color.Black, clGITADORAgradationTopColor, clGITADORAgradationBottomColor, true);
                    txTitle = CDTXMania.tGenerateTexture(bmpSongName, false);
                    CDTXMania.tDisposeSafely(ref bmpSongName);
                    CDTXMania.tDisposeSafely(ref titleFont);
                }
                else
                {
                    txTitle = null;
                }

                if (!string.IsNullOrWhiteSpace(strArtistName))
                {
                    artistNameFont = new CPrivateFastFont(new FontFamily(CDTXMania.ConfigIni.songListFont), 30,
                        FontStyle.Regular);
                    Bitmap bmpArtistName = artistNameFont.DrawPrivateFont(strArtistName, CPrivateFont.DrawMode.Edge, Color.Black,
                        Color.Black, clGITADORAgradationTopColor, clGITADORAgradationBottomColor, true);
                    txArtist = CDTXMania.tGenerateTexture(bmpArtistName, false);
                    CDTXMania.tDisposeSafely(ref bmpArtistName);
                    CDTXMania.tDisposeSafely(ref artistNameFont);
                }
                else
                {
                    txArtist = null;
                }

                #endregion
            }
            catch (CTextureCreateFailedException)
            {
                Trace.TraceError("テクスチャの生成に失敗しました。({0})", strSTAGEFILE);
                txTitle = null;
            }

            #endregion
            
            base.OnManagedCreateResources();
        }
    }

    public override void OnManagedReleaseResources()
    {
        if (!bNotActivated)
        {
            ui.Dispose();
            
            //テクスチャ11枚
            //2018.03.15 kairera0467 PrivateFontが抜けていた＆フォント生成直後に解放するようにしてみる
            CDTXMania.tReleaseTexture(ref txJacket);
            CDTXMania.tReleaseTexture(ref txTitle);
            CDTXMania.tReleaseTexture(ref txArtist);
            CDTXMania.tReleaseTexture(ref txDifficultyPanel);
            CDTXMania.tReleaseTexture(ref txPartPanel);
            CDTXMania.tReleaseTexture(ref txLevel);
            base.OnManagedReleaseResources();
        }
    }

    public override void FirstUpdate()
    {
        if (sdLoadingSound != null)
        {
            if (CDTXMania.Skin.soundNowLoading.bExclusive &&
                (CSkin.CSystemSound.rLastPlayedExclusiveSystemSound != null))
            {
                CSkin.CSystemSound.rLastPlayedExclusiveSystemSound.t停止する();
            }

            sdLoadingSound.tStartPlaying();
            nBGMPlayStartTime = CSoundManager.rcPerformanceTimer.nCurrentTime;
            nBGMTotalPlayTimeMs = sdLoadingSound.nTotalPlayTimeMs;
        }
        else if (!CDTXMania.DTXVmode.Enabled && !CDTXMania.DTX2WAVmode.Enabled)
        {
            CDTXMania.Skin.soundNowLoading.tPlay();
            nBGMPlayStartTime = CSoundManager.rcPerformanceTimer.nCurrentTime;
            nBGMTotalPlayTimeMs = CDTXMania.Skin.soundNowLoading.nLength_CurrentSound;
        }

        ePhaseID = EPhase.Common_FadeIn;
            
        nWAVcount = 1;
            
        try
        {
            string path = cdtx.strFolderName + cdtx.PREIMAGE;

            if (txJacket == null) // 2019.04.26 kairera0467
            {
                txJacket = CDTXMania.tGenerateTexture(!File.Exists(path) ? CSkin.Path(@"Graphics\5_preimage default.png") : path);
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.StackTrace);
        }
    }

    public override int OnUpdateAndDraw()
    {
        if (bNotActivated) return 0;

        base.OnUpdateAndDraw();

        #region [ If escape is pressed, stop the loading ]

        if (tHandleKeyInput())
        {
            if (sdLoadingSound != null)
            {
                sdLoadingSound.tStopSound();
                sdLoadingSound.tRelease();
            }

            return (int)ESongLoadingScreenReturnValue.LoadingStopped;
        }

        #endregion
        
        DrawLoadingScreenUI();

        switch (ePhaseID)
        {
            case EPhase.Common_FadeIn:
                //if( this.actFI.OnUpdateAndDraw() != 0 )					// #27787 2012.3.10 yyagi 曲読み込み画面のフェードインの省略
                // 必ず一度「CStaeg.EPhase.Common_FadeIn」フェーズを経由させること。
                // さもないと、曲読み込みが完了するまで、曲読み込み画面が描画されない。
                ePhaseID = EPhase.NOWLOADING_DTX_FILE_READING;
                return (int)ESongLoadingScreenReturnValue.Continue;

            case EPhase.NOWLOADING_DTX_FILE_READING:
            {
                timeBeginLoad = DateTime.Now;

                string songPath = !CDTXMania.bCompactMode ? CDTXMania.stageSongSelection.rChosenScore.FileInformation.AbsoluteFilePath : CDTXMania.strCompactModeFile;

                CScoreIni ini = new(songPath + ".score.ini");
                ini.tCheckIntegrity();

                if ((CDTXMania.DTX != null) && CDTXMania.DTX.bActivated)
                    CDTXMania.DTX.OnDeactivate();

                CDTXMania.DTX = new CDTX(songPath, false, CDTXMania.ConfigIni.nPlaySpeed / 20.0,
                    ini.stFile.BGMAdjust);
                Trace.TraceInformation("----曲情報-----------------");
                Trace.TraceInformation("TITLE: {0}", CDTXMania.DTX.TITLE);
                Trace.TraceInformation("FILE: {0}", CDTXMania.DTX.strFileNameFullPath);
                Trace.TraceInformation("---------------------------");

                // #35411 2015.08.19 chnmr0 add ゴースト機能のためList chip 読み込み後楽器パート出現順インデックスを割り振る
                int[] curCount = new int[(int)EInstrumentPart.UNKNOWN];
                for (int i = 0; i < curCount.Length; ++i)
                {
                    curCount[i] = 0;
                }

                foreach (CChip chip in CDTXMania.DTX.listChip.Where(chip => chip.eInstrumentPart != EInstrumentPart.UNKNOWN))
                {
                    chip.n楽器パートでの出現順 = curCount[(int)chip.eInstrumentPart]++;
                    if (CDTXMania.listTargetGhsotLag[(int)chip.eInstrumentPart] != null)
                    {
                        STGhostLag lag = new()
                        {
                            index = chip.n楽器パートでの出現順,
                            nJudgeTime = chip.nPlaybackTimeMs +
                                         CDTXMania.listTargetGhsotLag[(int)chip.eInstrumentPart][chip.n楽器パートでの出現順],
                            nLagTime = CDTXMania.listTargetGhsotLag[(int)chip.eInstrumentPart][chip.n楽器パートでの出現順]
                        };

                        stGhostLag[(int)chip.eInstrumentPart].Add(lag);
                    }
                }

                string[] inst = ["dr", "gt", "bs"];
                if (CDTXMania.ConfigIni.bIsSwappedGuitarBass)
                {
                    inst[1] = "bs";
                    inst[2] = "gt";
                }

                //演奏記録をゴーストから逆生成
                for (int i = 0; i < 3; i++)
                {
                    int nNowCombo = 0;
                    int nMaxCombo = 0;

                    //2016.06.18 kairera0467 「.ghost.score」ファイルが無かった場合ghostファイルから逆算を行う形に変更。
                    string[] prefix = ["none", "perfect", "lastplay", "hiskill", "hiscore", "online"];
                    int indPrefix = (int)CDTXMania.ConfigIni.eTargetGhost[i];
                    string filename = $"{cdtx.strFolderName}\\{cdtx.strFileName}.{prefix[indPrefix]}.{inst[i]}.ghost";

                    if (stGhostLag[i] == null || File.Exists(filename + ".score"))
                        continue;
                    CDTXMania.listTargetGhostScoreData[i] = new CScoreIni.CPerformanceEntry();

                    for (int n = 0; n < stGhostLag[i].Count; n++)
                    {
                        int ghostLag = 128;
                        ghostLag = stGhostLag[i][n].nLagTime;
                        
                        // 上位８ビットが１ならコンボが途切れている（ギターBAD空打ちでコンボ数を再現するための措置）
                        if (ghostLag > 255)
                        {
                            nNowCombo = 0;
                        }

                        ghostLag = (ghostLag & 255) - 128;

                        if (ghostLag <= 127)
                        {
                            EJudgement eJudge = e指定時刻からChipのJUDGEを返す(ghostLag, 0, (EInstrumentPart)i);

                            switch (eJudge)
                            {
                                case EJudgement.Perfect:
                                    CDTXMania.listTargetGhostScoreData[i].nPerfectCount++;
                                    break;
                                case EJudgement.Great:
                                    CDTXMania.listTargetGhostScoreData[i].nGreatCount++;
                                    break;
                                case EJudgement.Good:
                                    CDTXMania.listTargetGhostScoreData[i].nGoodCount++;
                                    break;
                                case EJudgement.Poor:
                                    CDTXMania.listTargetGhostScoreData[i].nPoorCount++;
                                    break;
                                case EJudgement.Miss:
                                case EJudgement.Bad:
                                    CDTXMania.listTargetGhostScoreData[i].nMissCount++;
                                    break;
                            }

                            switch (eJudge)
                            {
                                case EJudgement.Perfect:
                                case EJudgement.Great:
                                case EJudgement.Good:
                                    nNowCombo++;
                                    CDTXMania.listTargetGhostScoreData[i].nMaxCombo = Math.Max(nNowCombo,
                                        CDTXMania.listTargetGhostScoreData[i].nMaxCombo);
                                    break;
                                case EJudgement.Poor:
                                case EJudgement.Miss:
                                case EJudgement.Bad:
                                    CDTXMania.listTargetGhostScoreData[i].nMaxCombo = Math.Max(nNowCombo,
                                        CDTXMania.listTargetGhostScoreData[i].nMaxCombo);
                                    nNowCombo = 0;
                                    break;
                            }
                        }
                    }

                    int nTotal = i switch
                    {
                        1 => CDTXMania.DTX.nVisibleChipsCount.Guitar,
                        2 => CDTXMania.DTX.nVisibleChipsCount.Bass,
                        _ => CDTXMania.DTX.nVisibleChipsCount.Drums
                    };
                    
                    if (CDTXMania.ConfigIni.nSkillMode == 0)
                    {
                        CDTXMania.listTargetGhostScoreData[i].dbPerformanceSkill = CScoreIni.tCalculatePlayingSkillOld(
                            nTotal, CDTXMania.listTargetGhostScoreData[i].nPerfectCount,
                            CDTXMania.listTargetGhostScoreData[i].nGreatCount,
                            CDTXMania.listTargetGhostScoreData[i].nGoodCount,
                            CDTXMania.listTargetGhostScoreData[i].nPoorCount,
                            CDTXMania.listTargetGhostScoreData[i].nMissCount,
                            CDTXMania.listTargetGhostScoreData[i].nMaxCombo, (EInstrumentPart)i,
                            CDTXMania.listTargetGhostScoreData[i].bAutoPlay);
                    }
                    else
                    {
                        CDTXMania.listTargetGhostScoreData[i].dbPerformanceSkill = CScoreIni.tCalculatePlayingSkill(
                            nTotal, CDTXMania.listTargetGhostScoreData[i].nPerfectCount,
                            CDTXMania.listTargetGhostScoreData[i].nGreatCount,
                            CDTXMania.listTargetGhostScoreData[i].nGoodCount,
                            CDTXMania.listTargetGhostScoreData[i].nPoorCount,
                            CDTXMania.listTargetGhostScoreData[i].nMissCount,
                            CDTXMania.listTargetGhostScoreData[i].nMaxCombo, (EInstrumentPart)i,
                            CDTXMania.listTargetGhostScoreData[i].bAutoPlay);
                    }
                }

                TimeSpan span = DateTime.Now - timeBeginLoad;
                Console.WriteLine($"Time to load DTX file: {span}");
                
                if (CDTXMania.bCompactMode)
                    CDTXMania.DTX.MIDIレベル = 1;
                else
                    CDTXMania.DTX.MIDIレベル =
                        (CDTXMania.stageSongSelection.rConfirmedSong.eNodeType == CSongListNode.ENodeType.SCORE_MIDI)
                            ? CDTXMania.stageSongSelection.nSelectedSongDifficultyLevel : 0;

                ePhaseID = EPhase.NOWLOADING_WAV_FILE_READING;
                timeBeginLoadWAV = DateTime.Now;
                return (int)ESongLoadingScreenReturnValue.Continue;
            }

            case EPhase.NOWLOADING_WAV_FILE_READING:
            {
                if (nWAVcount == 1 && CDTXMania.DTX.listWAV.Count > 0) // #28934 2012.7.7 yyagi (added checking Count)
                {
                    //ShowProgressByFilename(CDTXMania.DTX.listWAV[nWAVcount].strFilename);
                }
                
                int looptime =
                    (CDTXMania.ConfigIni.bVerticalSyncWait) ? 3 : 1; // VSyncWait=ON時は1frame(1/60s)あたり3つ読むようにする
                for (int i = 0; i < looptime && nWAVcount <= CDTXMania.DTX.listWAV.Count; i++)
                {
                    if (CDTXMania.DTX.listWAV[nWAVcount].listこのWAVを使用するチャンネル番号の集合.Count > 0) // #28674 2012.5.8 yyagi
                    {
                        CDTXMania.DTX.tLoadWAV(CDTXMania.DTX.listWAV[nWAVcount]);
                    }

                    nWAVcount++;
                }

                if (nWAVcount <= CDTXMania.DTX.listWAV.Count)
                {
                    //ShowProgressByFilename(CDTXMania.DTX.listWAV[nWAVcount].strFilename);
                }

                if (nWAVcount > CDTXMania.DTX.listWAV.Count)
                {
                    TimeSpan span = DateTime.Now - timeBeginLoadWAV;
                    
                    Trace.TraceInformation($"WAV読込所要時間({CDTXMania.DTX.listWAV.Count, 4}):     {span.ToString()}");
                    timeBeginLoadWAV = DateTime.Now;

                    if (CDTXMania.ConfigIni.bDynamicBassMixerManagement)
                    {
                        CDTXMania.DTX.PlanToAddMixerChannel();
                    }

                    CDTXMania.DTX.t旧仕様のドコドコチップを振り分ける(EInstrumentPart.DRUMS, CDTXMania.ConfigIni.bAssignToLBD.Drums);
                    CDTXMania.DTX.tドコドコ仕様変更(EInstrumentPart.DRUMS, CDTXMania.ConfigIni.eDkdkType.Drums);
                    CDTXMania.DTX.tドラムのランダム化(EInstrumentPart.DRUMS, CDTXMania.ConfigIni.eRandom.Drums);
                    CDTXMania.DTX.tRandomizeDrumPedal(EInstrumentPart.DRUMS, CDTXMania.ConfigIni.eRandomPedal.Drums);
                    CDTXMania.DTX.t譜面仕様変更(EInstrumentPart.DRUMS, CDTXMania.ConfigIni.eNumOfLanes.Drums);
                    CDTXMania.DTX.tRandomizeGuitarAndBass(EInstrumentPart.GUITAR, CDTXMania.ConfigIni.eRandom.Guitar);
                    CDTXMania.DTX.tRandomizeGuitarAndBass(EInstrumentPart.BASS, CDTXMania.ConfigIni.eRandom.Bass);

                    // if (CDTXMania.ConfigIni.bGuitarRevolutionMode)
                    //     CDTXMania.stagePerfGuitarScreen.OnActivate();
                    // else
                    //     CDTXMania.stagePerfDrumsScreen.OnActivate();

                    span = DateTime.Now - timeBeginLoadWAV;
                    Trace.TraceInformation("WAV/譜面後処理時間({0,4}):  {1}",
                        CDTXMania.DTX.listBMP.Count + CDTXMania.DTX.listBMPTEX.Count + CDTXMania.DTX.listAVI.Count,
                        span.ToString());

                    ePhaseID = EPhase.NOWLOADING_BMP_FILE_READING;
                }

                return (int)ESongLoadingScreenReturnValue.Continue;
            }

            case EPhase.NOWLOADING_BMP_FILE_READING:
            {
                DateTime timeBeginLoadBMPAVI = DateTime.Now;
                if (CDTXMania.ConfigIni.bBGAEnabled)
                    CDTXMania.DTX.tLoadBMP_BMPTEX();

                if (CDTXMania.ConfigIni.bAVIEnabled)
                    CDTXMania.DTX.tLoadAVI();
                TimeSpan span = DateTime.Now - timeBeginLoadBMPAVI;
                Trace.TraceInformation("BMP/AVI読込所要時間({0,4}): {1}",
                    (CDTXMania.DTX.listBMP.Count + CDTXMania.DTX.listBMPTEX.Count + CDTXMania.DTX.listAVI.Count),
                    span.ToString());

                span = DateTime.Now - timeBeginLoad;
                Trace.TraceInformation("総読込時間:                {0}", span.ToString());
                CDTXMania.Timer.tUpdate();
                ePhaseID = EPhase.NOWLOADING_WAIT_BGM_SOUND_COMPLETION;
                return (int)ESongLoadingScreenReturnValue.Continue;
            }

            case EPhase.NOWLOADING_WAIT_BGM_SOUND_COMPLETION:
            {
                long nCurrentTime = CDTXMania.Timer.nCurrentTime;
                if (nCurrentTime < nBGMPlayStartTime)
                    nBGMPlayStartTime = nCurrentTime;
                
                if ((nCurrentTime - nBGMPlayStartTime) > (nBGMTotalPlayTimeMs)) // #27787 2012.3.10 yyagi 1000ms == フェードイン分の時間
                {
                    actFO.tStartFadeOut();
                    ePhaseID = EPhase.Common_FadeOut;
                }

                return (int)ESongLoadingScreenReturnValue.Continue;
            }

            case EPhase.Common_FadeOut:
                if (sdLoadingSound != null)
                {
                    sdLoadingSound.tRelease();
                }
                return (int)ESongLoadingScreenReturnValue.LoadingComplete;
        }

        return (int)ESongLoadingScreenReturnValue.Continue;
    }
    
    private void DrawLoadingScreenUI()
    {
        int y = 184;
        
        if (txJacket != null)
        {
            Matrix mat = Matrix.Identity;
            float fScalingFactor;
            float jacketOnScreenSize = 384.0f;
            //Maintain aspect ratio by scaling only to the smaller scalingFactor
            if (jacketOnScreenSize / txJacket.szImageSize.Width > jacketOnScreenSize / txJacket.szImageSize.Height)
            {
                fScalingFactor = jacketOnScreenSize / txJacket.szImageSize.Height;
            }
            else
            {
                fScalingFactor = jacketOnScreenSize / txJacket.szImageSize.Width;
            }
        
            mat *= Matrix.Scaling(fScalingFactor, fScalingFactor, 1f);
            mat *= Matrix.Translation(206f, 66f, 0f);
            mat *= Matrix.RotationZ(0.28f);
        
            txJacket.tDraw3D(CDTXMania.app.Device, mat);
        }
        
        if (txTitle != null)
        {
            if (txTitle.szImageSize.Width > 625)
                txTitle.vcScaleRatio.X = 625f / txTitle.szImageSize.Width;
        
            txTitle.tDraw2D(CDTXMania.app.Device, 190, 285);
        }
        
        if (txArtist != null)
        {
            if (txArtist.szImageSize.Width > 625)
                txArtist.vcScaleRatio.X = 625f / txArtist.szImageSize.Width;
        
            txArtist.tDraw2D(CDTXMania.app.Device, 190, 360);
        }
        
        int[] iPart = [0, CDTXMania.ConfigIni.bIsSwappedGuitarBass ? 2 : 1, CDTXMania.ConfigIni.bIsSwappedGuitarBass ? 1 : 2];

        int k = 0;
        
        for (int instrument = 0; instrument < 3; instrument++)
        {
            int j = iPart[instrument];
        
            int DTXLevel = cdtx.LEVEL[j];
            double DTXLevelDeci = cdtx.LEVELDEC[j];
            
            if ((CDTXMania.ConfigIni.bDrumsEnabled && instrument == 0) || (CDTXMania.ConfigIni.bGuitarEnabled && instrument != 0))
            {
                if (DTXLevel != 0 || DTXLevelDeci != 0)
                {
                    //Always display CLASSIC style if Skill Mode is Classic
                    if (CDTXMania.ConfigIni.nSkillMode == 0 || (CDTXMania.ConfigIni.bCLASSIC譜面判別を有効にする &&
                                                                CDTXMania.stageSongSelection.rChosenScore.SongInformation.b完全にCLASSIC譜面である[j] && 
                                                                !cdtx.bForceXGChart))
                    {
                        tDrawStringLarge(187 + k, 152, $"{DTXLevel:00}");
                    }
                    else
                    {
                        if (cdtx.LEVEL[j] > 99)
                        {
                            DTXLevel = cdtx.LEVEL[j] / 100;
                            DTXLevelDeci = cdtx.LEVEL[j] - (DTXLevel * 100);
                        }
                        else
                        {
                            DTXLevel = cdtx.LEVEL[j] / 10;
                            DTXLevelDeci = ((cdtx.LEVEL[j] - DTXLevel * 10) * 10) + cdtx.LEVELDEC[j];
                        }
        
                        txLevel.tDraw2D(CDTXMania.app.Device, 282 + k, 243, new Rectangle(1000, 92, 30, 38));
                        tDrawStringLarge(187 + k, 152, $"{DTXLevel:0}");
                        tDrawStringLarge(307 + k, 152, $"{DTXLevelDeci:00}");
                    }
        
                    if (txPartPanel != null)
                        txPartPanel.tDraw2D(CDTXMania.app.Device, 191 + k, 52, new Rectangle(0, j * 50, 262, 50));
        
                    //this.txJacket.Dispose();
                    if (!CDTXMania.bCompactMode && !CDTXMania.DTXVmode.Enabled && !CDTXMania.DTX2WAVmode.Enabled)
                        tDrawDifficultyPanel(CDTXMania.stageSongSelection.rConfirmedSong.arDifficultyLabel[
                                CDTXMania.stageSongSelection.nConfirmedSongDifficulty], 191 + k, 102);
        
                    k = 700;
                }
            }
        
            // //second guitar... ?????
            // if (instrument == 2 && k == 0)
            // {
            //     if (txPartPanel != null && CDTXMania.ConfigIni.bDrumsEnabled)
            //         txPartPanel.tDraw2D(CDTXMania.app.Device, 191, 52, new Rectangle(0, 0, 262, 50));
            //
            //     if (txDifficultyPanel != null)
            //         txDifficultyPanel.tDraw2D(CDTXMania.app.Device, 191, 102, new Rectangle(0, nIndex * 50, 262, 50));
            // }
        }
    }

    /// <summary>
    /// ESC押下時、trueを返す
    /// </summary>
    /// <returns></returns>
    private bool tHandleKeyInput()
    {
        CInputKeyboard keyboard = CDTXMania.InputManager.Keyboard;
        if (keyboard.bKeyPressed(SlimDXKey.Escape)) // escape (exit)
        {
            if (CDTXMania.ConfigIni.bGuitarRevolutionMode)
            {
                if (CDTXMania.stagePerfGuitarScreen.bActivated)
                    CDTXMania.stagePerfGuitarScreen.OnDeactivate();
            }
            else
            {
                if (CDTXMania.stagePerfDrumsScreen.bActivated)
                    CDTXMania.stagePerfDrumsScreen.OnDeactivate();
            }

            return true;
        }

        return false;
    }

    // Other

    #region [ private ]
    
    //-----------------
    [StructLayout(LayoutKind.Sequential)]
    private struct STCharacterPosition
    {
        public char ch;
        public Point pt;
    }

    //		private CActFIFOBlack actFI;
    private CActFIFOBlackStart actFO;

    private readonly STCharacterPosition[] st大文字位置;
    private int nCurrentInst;
    private long nBGMTotalPlayTimeMs;
    private long nBGMPlayStartTime;
    private CSound sdLoadingSound;
    private string strSTAGEFILE;
    private string strSongTitle;
    private string strArtistName;
    private CTexture? txTitle;
    private CTexture? txArtist;
    private CTexture? txJacket;
    private CTexture? txDifficultyPanel;
    private CTexture? txPartPanel;

    private CPrivateFastFont titleFont;
    private CPrivateFastFont artistNameFont;

    //2014.04.05.kairera0467 GITADORAグラデーションの色。
    //本当は共通のクラスに設置してそれを参照する形にしたかったが、なかなかいいメソッドが無いため、とりあえず個別に設置。
    //private Color clGITADORAgradationTopColor = Color.FromArgb(0, 220, 200);
    //private Color clGITADORAgradationBottomColor = Color.FromArgb(255, 250, 40);
    private Color clGITADORAgradationTopColor = Color.FromArgb(255, 255, 255);
    private Color clGITADORAgradationBottomColor = Color.FromArgb(255, 255, 255);

    private DateTime timeBeginLoad;
    private DateTime timeBeginLoadWAV;
    private int nWAVcount;
    private CTexture txLevel;

    [StructLayout(LayoutKind.Sequential)]
    private struct STATUSPANEL
    {
        public string label;
        public int status;
    }

    private int nIndex;
    private STATUSPANEL[] stPanelMap;

    private STDGBVALUE<List<STGhostLag>> stGhostLag;

    [StructLayout(LayoutKind.Sequential)]
    private struct STGhostLag
    {
        public int index;
        public int nJudgeTime;
        public int nLagTime;
    }

    private EJudgement e指定時刻からChipのJUDGEを返す(long nTime, int nInputAdjustTime, EInstrumentPart part)
    {
        int nDeltaTimeMs = Math.Abs((int)nTime + nInputAdjustTime);
        switch (part)
        {
            case EInstrumentPart.DRUMS:
                // TODO: ghosts do not track columns, so pedal ranges cannot be used
                return CDTXMania.stDrumHitRanges.tGetJudgement(nDeltaTimeMs);
            
            case EInstrumentPart.GUITAR:
                return CDTXMania.stGuitarHitRanges.tGetJudgement(nDeltaTimeMs);
            
            case EInstrumentPart.BASS:
                return CDTXMania.stBassHitRanges.tGetJudgement(nDeltaTimeMs);
            
            case EInstrumentPart.UNKNOWN:
            default:
                return STHitRanges.tCreateDefaultDTXHitRanges().tGetJudgement(nDeltaTimeMs);
        }
    }

    //-----------------
    private void ReadGhost(string filename, List<int> list) // #35411 2015.08.19 chnmr0 add
    {
        //return; //2015.12.31 kairera0467 以下封印

        if (File.Exists(filename))
        {
            using FileStream fs = new(filename, FileMode.Open, FileAccess.Read);
            using BinaryReader br = new(fs);
            
            try
            {
                int cnt = br.ReadInt32();
                for (int i = 0; i < cnt; ++i)
                {
                    short lag = br.ReadInt16();
                    list.Add(lag);
                }
            }
            catch (EndOfStreamException)
            {
                Trace.TraceInformation("ゴーストデータは正しく読み込まれませんでした。");
                list.Clear();
            }
        }

        if (File.Exists(filename + ".score"))
        {
            using FileStream fs = new(filename + ".score", FileMode.Open, FileAccess.Read);
            using StreamReader sr = new(fs);

            try
            {
                string strScoreDataFile = sr.ReadToEnd();

                strScoreDataFile = strScoreDataFile.Replace(Environment.NewLine, "\n");
                string[] delimiter = ["\n"];
                string[] strSingleLine = strScoreDataFile.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

                foreach (string t in strSingleLine)
                {
                    string[] strA = t.Split('=');
                    if (strA.Length != 2)
                        continue;

                    switch (strA[0])
                    {
                        case "Score":
                            CDTXMania.listTargetGhostScoreData[nCurrentInst].nスコア = Convert.ToInt32(strA[1]);
                            continue;
                        case "PlaySkill":
                            CDTXMania.listTargetGhostScoreData[nCurrentInst].dbPerformanceSkill = Convert.ToDouble(strA[1]);
                            continue;
                        case "Skill":
                            CDTXMania.listTargetGhostScoreData[nCurrentInst].dbGameSkill = Convert.ToDouble(strA[1]);
                            continue;
                        case "Perfect":
                            CDTXMania.listTargetGhostScoreData[nCurrentInst].nPerfectCount_ExclAuto = Convert.ToInt32(strA[1]);
                            continue;
                        case "Great":
                            CDTXMania.listTargetGhostScoreData[nCurrentInst].nGreatCount_ExclAuto = Convert.ToInt32(strA[1]);
                            continue;
                        case "Good":
                            CDTXMania.listTargetGhostScoreData[nCurrentInst].nGoodCount_ExclAuto = Convert.ToInt32(strA[1]);
                            continue;
                        case "Poor":
                            CDTXMania.listTargetGhostScoreData[nCurrentInst].nPoorCount_ExclAuto = Convert.ToInt32(strA[1]);
                            continue;
                        case "Miss":
                            CDTXMania.listTargetGhostScoreData[nCurrentInst].nMissCount_ExclAuto = Convert.ToInt32(strA[1]);
                            continue;
                        case "MaxCombo":
                            CDTXMania.listTargetGhostScoreData[nCurrentInst].nMaxCombo = Convert.ToInt32(strA[1]);
                            continue;
                        default:
                            continue;
                    }
                }
            }
            catch (NullReferenceException)
            {
                Trace.TraceInformation("ゴーストデータの記録が正しく読み込まれませんでした。");
            }
            catch (EndOfStreamException)
            {
                Trace.TraceInformation("ゴーストデータの記録が正しく読み込まれませんでした。");
            }
        }
        else
        {
            CDTXMania.listTargetGhostScoreData[nCurrentInst] = null;
        }
    }

    private void tDrawStringLarge(int x, int y, string str)
    {
        foreach (char c in str)
        {
            for (int j = 0; j < st大文字位置.Length; j++)
            {
                if (st大文字位置[j].ch != c) continue;

                Rectangle rc画像内の描画領域 = new(st大文字位置[j].pt.X, st大文字位置[j].pt.Y, 100, 130);
                if (txLevel != null)
                {
                    txLevel.tDraw2D(CDTXMania.app.Device, x, y, rc画像内の描画領域);
                }

                break;
            }

            if (c == '.')
            {
                x += 30;
            }
            else
            {
                x += 90;
            }
        }
    }

    private void tDrawDifficultyPanel(string strLabelName, int nX, int nY)
    {
        Rectangle rect = new(0, 0, 262, 50);

        //Check if the script file exists
        if (File.Exists(CSkin.Path(@"Script\difficult.dtxs")))
        {
            //スクリプトを開く
            StreamReader reader = new(CSkin.Path(@"Script\difficult.dtxs"), Encoding.GetEncoding("Shift_JIS"));
            string strRawScriptFile = reader.ReadToEnd();

            strRawScriptFile = strRawScriptFile.Replace(Environment.NewLine, "\n");
            string[] delimiter = ["\n"];
            string[] strSingleLine = strRawScriptFile.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

            foreach (string t in strSingleLine)
            {
                if (t.StartsWith("//"))
                    continue; //Ignore comments

                //まずSplit
                string[] arScriptLine = t.Split(',');

                if ((arScriptLine.Length >= 4 && arScriptLine.Length <= 5) == false)
                    continue; //引数が4つか5つじゃなければ無視。

                if (arScriptLine[0] != "6")
                    continue; //使用するシーンが違うなら無視。

                if (arScriptLine.Length == 4)
                {
                    if (String.Compare(arScriptLine[1], strLabelName, true) != 0)
                        continue; //ラベル名が違うなら無視。大文字小文字区別しない
                }
                else if (arScriptLine.Length == 5)
                {
                    if (arScriptLine[4] == "1")
                    {
                        if (arScriptLine[1] != strLabelName)
                            continue; //ラベル名が違うなら無視。
                    }
                    else
                    {
                        if (String.Compare(arScriptLine[1], strLabelName, true) != 0)
                            continue; //ラベル名が違うなら無視。大文字小文字区別しない
                    }
                }

                rect.X = Convert.ToInt32(arScriptLine[2]);
                rect.Y = Convert.ToInt32(arScriptLine[3]);

                break;
            }
        }

        if (txDifficultyPanel != null)
            txDifficultyPanel.tDraw2D(CDTXMania.app.Device, nX, nY, rect);
    }

    #endregion
}