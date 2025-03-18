using System.Runtime.InteropServices;
using System.Drawing;
//using SharpDX;
using FDK;

using Rectangle = System.Drawing.Rectangle;

namespace DTXMania
{
    internal class CActSelectStatusPanel : CActivity
    {
        // Methods

        public CActSelectStatusPanel()
        {
            bNotActivated = true;
        }
        public void tSelectedSongChanged()
        {
            CSongListNode c曲リストノード = CDTXMania.stageSongSelection.r現在選択中の曲;
            CScore cスコア = CDTXMania.stageSongSelection.rSelectedScore;
            if ((c曲リストノード != null) && (cスコア != null))
            {
                n現在選択中の曲の難易度 = CDTXMania.stageSongSelection.nSelectedSongDifficultyLevel;
                STDGBVALUE<double>[] dbCurrentSkillPointForAllDifficulty = new STDGBVALUE<double>[5];
                for (int i = 0; i < 3; i++)
                {
                    if (CDTXMania.ConfigIni.nSkillMode == 0)
                    {
                        n現在選択中の曲の最高ランク[i] = cスコア.SongInformation.BestRank[i];
                    }
                    else if (CDTXMania.ConfigIni.nSkillMode == 1)
                    {
                        n現在選択中の曲の最高ランク[i] = CScoreIni.tCalculateRank(0, cスコア.SongInformation.HighSkill[i]);
                    }

                    b現在選択中の曲がフルコンボ[i] = cスコア.SongInformation.FullCombo[i];
                    db現在選択中の曲の最高スキル値[i] = cスコア.SongInformation.HighSkill[i];
                    db現在選択中の曲の曲別スキル[i] = cスコア.SongInformation.HighSongSkill[i];
                    b現在選択中の曲の譜面[i] = cスコア.SongInformation.bScoreExists[i];
                    n現在選択中の曲のレベル[i] = cスコア.SongInformation.Level[i];
                    n現在選択中の曲のレベル小数点[ i ] = cスコア.SongInformation.LevelDec[ i ];

                    for (int j = 0; j < 5; j++)
                    {
                        if (c曲リストノード.arScore[j] != null)
                        {
                            n現在選択中の曲のレベル難易度毎DGB[j][i] = c曲リストノード.arScore[j].SongInformation.Level[i];
                            n現在選択中の曲のレベル小数点難易度毎DGB[j][i] = c曲リストノード.arScore[j].SongInformation.LevelDec[i];
                            //this.n現在選択中の曲の最高ランク難易度毎[j][i] = c曲リストノード.arScore[j].SongInformation.BestRank[i];
                            if (CDTXMania.ConfigIni.nSkillMode == 0)
                            {
                                n現在選択中の曲の最高ランク難易度毎[j][i] = c曲リストノード.arScore[j].SongInformation.BestRank[i];
                            }
                            else if (CDTXMania.ConfigIni.nSkillMode == 1)
                            {
                                // Fix github.com/limyz/DTXmaniaXG/issues/33
                                //this.n現在選択中の曲の最高ランク難易度毎[j][i] = (DTXMania.CScoreIni.tCalculateRank(0, c曲リストノード.arScore[j].SongInformation.HighSkill[i]) == (int)DTXMania.CScoreIni.ERANK.S && DTXMania.CScoreIni.tCalculateRank(0, c曲リストノード.arScore[j].SongInformation.HighSkill[i]) >= 95 ? DTXMania.CScoreIni.tCalculateRank(0, cスコア.SongInformation.HighSkill[i]) : c曲リストノード.arScore[j].SongInformation.BestRank[i]);
                                n現在選択中の曲の最高ランク難易度毎[j][i] = CScoreIni.tCalculateRank(0, c曲リストノード.arScore[j].SongInformation.HighSkill[i]);
                                dbCurrentSkillPointForAllDifficulty[j][i] = CScoreIni.tCalculateGameSkillFromPlayingSkill(
                                    c曲リストノード.arScore[j].SongInformation.Level[i],
                                    c曲リストノード.arScore[j].SongInformation.LevelDec[i],
                                    c曲リストノード.arScore[j].SongInformation.HighSkill[i],
                                    false
                                    );
                            }

                            db現在選択中の曲の最高スキル値難易度毎[j][i] = c曲リストノード.arScore[j].SongInformation.HighSkill[i];
                            b現在選択中の曲がフルコンボ難易度毎[j][i] = c曲リストノード.arScore[j].SongInformation.FullCombo[i];
                            b現在選択中の曲に譜面がある[j][i] = c曲リストノード.arScore[j].SongInformation.bScoreExists[i];
                        }
                    }
                }

                //Do SP comparison and return only highest per game type
                dbDrumSP = 0.0;
                nDrumSPDiffRank = -1;
                dbGBSP = 0.0;
                nGBSPDiffRank = -1;
                nSpInGuitarOrBass = 0;//G:0 B:1
                for (int i = 0; i < 5; i++)
                {
                    //Drum
                    if(dbCurrentSkillPointForAllDifficulty[i].Drums > dbDrumSP)
                    {
                        dbDrumSP = dbCurrentSkillPointForAllDifficulty[i].Drums;
                        nDrumSPDiffRank = i;
                    }

                    //Guitar/Bass
                    if (dbCurrentSkillPointForAllDifficulty[i].Guitar > dbGBSP)
                    {
                        dbGBSP = dbCurrentSkillPointForAllDifficulty[i].Guitar;
                        nGBSPDiffRank = i;
                        nSpInGuitarOrBass = 0;
                    }

                    if (dbCurrentSkillPointForAllDifficulty[i].Bass > dbGBSP)
                    {
                        dbGBSP = dbCurrentSkillPointForAllDifficulty[i].Bass;
                        nGBSPDiffRank = i;
                        nSpInGuitarOrBass = 1;
                    }
                }

                //Check arDifficultyLabel for all null
                bHasMultipleDiff = false;
                for (int i = 0; i < 5; i++)
                {
                    if(c曲リストノード.arDifficultyLabel[i] != null)
                    {
                        bHasMultipleDiff = true;
                        break;
                    }
                }
                
                for (int i = 0; i < 5; i++)
                {
                    if (c曲リストノード.arScore[i] != null)
                    {
                        int nLevel = c曲リストノード.arScore[i].SongInformation.Level.Drums;
                        if( nLevel < 0 )
                        {
                            nLevel = 0;
                        }
                        if( nLevel > 999 )
                        {
                            nLevel = 999;
                        }
                        n選択中の曲のレベル難易度毎[i] = nLevel;

                        db現在選択中の曲の曲別スキル値難易度毎[i] = c曲リストノード.arScore[i].SongInformation.HighSongSkill.Drums;
                    }
                    else
                    {
                        n選択中の曲のレベル難易度毎[i] = 0;
                    }
                    str難易度ラベル[i] = c曲リストノード.arDifficultyLabel[i];

                }
                if (r直前の曲 != c曲リストノード)
                {
                    n難易度開始文字位置 = 0;
                }
                r直前の曲 = c曲リストノード;
            }
        }


        // CActivity 実装

        public override void OnActivate()
        {

            n現在選択中の曲の難易度 = 0;
            for( int i = 0; i < 3; i++ )
            {
                n現在選択中の曲のレベル[ i ] = 0;
                n現在選択中の曲のレベル小数点[ i ] = 0;
                db現在選択中の曲の曲別スキル[ i ] = 0.0;
                n現在選択中の曲の最高ランク[ i ] = (int)CScoreIni.ERANK.UNKNOWN;
                b現在選択中の曲がフルコンボ[ i ] = false;
                db現在選択中の曲の最高スキル値[ i ] = 0.0;
                for( int j = 0; j < 5; j++ )
                {
                    n現在選択中の曲のレベル難易度毎DGB[ j ][ i ] = 0;
                    n現在選択中の曲のレベル小数点難易度毎DGB[ j ][ i ] = 0;
                    db現在選択中の曲の最高スキル値難易度毎[ j ][ i ] = 0.0;
                    n現在選択中の曲の最高ランク難易度毎[ j ][ i ] = (int)CScoreIni.ERANK.UNKNOWN;
                    b現在選択中の曲がフルコンボ難易度毎[ j ][ i ] = false;
                }
            }
            for( int j = 0; j < 5; j++ )
            {
                str難易度ラベル[ j ] = "";
                n選択中の曲のレベル難易度毎[ j ] = 0;

                db現在選択中の曲の曲別スキル値難易度毎[ j ] = 0.0;
            }
            n難易度開始文字位置 = 0;
            r直前の曲 = null;
            base.OnActivate();
        }
        public override void OnDeactivate()
        {
            ct登場アニメ用 = null;
            ct難易度スクロール用 = null;
            ct難易度矢印用 = null;
            strCurrentProgressBar = "";
            base.OnDeactivate();
        }
        public override void OnManagedCreateResources()
        {
            if (!bNotActivated)
            {
                txパネル本体 = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\5_status panel.png"));
                tx難易度パネル = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\5_difficulty panel.png"));
                tx難易度枠 = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\5_difficulty frame.png"));
                txランク = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\5_skill icon.png"));
                tx達成率MAX = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\5_skill max.png"));
                txDifficultyNumber = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\5_level number.png"));
                txAchievementRateNumber = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\5_skill number.png"));
                txBPM数字 = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\5_bpm font.png"));
                txDrumsGraphPanel = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\5_graph panel drums.png"));
                txGuitarBassGraphPanel = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\5_graph panel guitar bass.png"));
                txSkillPointPanel = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\5_skill point panel.png"));
                txGenerateGraphBarLine();
                txGenerateProgressBarLine("");
                base.OnManagedCreateResources();
            }
        }
        public override void OnManagedReleaseResources()
        {
            if (!bNotActivated)
            {
                CDTXMania.tReleaseTexture(ref txパネル本体);
                CDTXMania.tReleaseTexture(ref tx難易度パネル);
                CDTXMania.tReleaseTexture(ref tx難易度枠);
                CDTXMania.tReleaseTexture(ref txランク);
                CDTXMania.tReleaseTexture(ref tx達成率MAX);
                CDTXMania.tReleaseTexture(ref txDifficultyNumber);
                CDTXMania.tReleaseTexture(ref txAchievementRateNumber);
                CDTXMania.tReleaseTexture(ref txBPM数字);
                CDTXMania.tReleaseTexture(ref txDrumsGraphPanel);
                CDTXMania.tReleaseTexture(ref txGuitarBassGraphPanel);
                for (int i = 0; i < txDrumChipsBarLine.Length; i++)
                {
                    CDTXMania.tReleaseTexture(ref txDrumChipsBarLine[i]);
                }
                for (int i = 0; i < txGBChipsBarLine.Length; i++)
                {
                    CDTXMania.tReleaseTexture(ref txGBChipsBarLine[i]);
                }
                CDTXMania.tReleaseTexture(ref txSkillPointPanel);
                CDTXMania.tReleaseTexture(ref txProgressBar);
                base.OnManagedReleaseResources();
            }
        }
        public override int OnUpdateAndDraw()
        {

            if (!bNotActivated)
            {
                #region [ 初めての進行描画 ]
                //-----------------
                if (bJustStartedUpdate)
                {
                    ct登場アニメ用 = new CCounter(0, 100, 5, CDTXMania.Timer);
                    ct難易度スクロール用 = new CCounter(0, 20, 1, CDTXMania.Timer);
                    ct難易度矢印用 = new CCounter(0, 5, 80, CDTXMania.Timer);
                    bJustStartedUpdate = false;
                }
                //-----------------
                #endregion

                // 進行

                ct登場アニメ用.tUpdate();

                ct難易度スクロール用.tUpdate();
                if (ct難易度スクロール用.bReachedEndValue)
                {
                    int num = nCheckDifficultyLabelDisplayAndReturnScrollDirection();
                    if (num < 0)
                    {
                        n難易度開始文字位置--;
                    }
                    else if (num > 0)
                    {
                        n難易度開始文字位置++;
                    }
                    ct難易度スクロール用.nCurrentValue = 0;
                }

                ct難易度矢印用.tUpdateLoop();

                // 描画

                CScore cスコア = CDTXMania.stageSongSelection.rSelectedScore;
                int nPanelNoteCount = 0;
                //9 lane for drums, 6 for guitar/bass
                int[] arrChipsByLane = null;
                //0 for Drums, 1 for GuitarBass
                int nDGmode = (CDTXMania.ConfigIni.bGuitarEnabled ? 1 : 1) + (CDTXMania.ConfigIni.bDrumsEnabled ? 0 : 1) - 1;
                string strSP = "";
                string strProgressText = "";
                #region [ 選択曲の BPM の描画 ]
                if (CDTXMania.stageSongSelection.r現在選択中の曲 != null)
                {

                    int nBPM位置X = 490;
                    int nBPM位置Y = 385;

                    if (txパネル本体 != null)
                    {
                        nBPM位置X = 90;
                        nBPM位置Y = 275;
                    }

                    string strBPM;
                    //string strDrumNotes = "";
                    //string strGuitarNotes = "";
                    //string strBassNotes = "";
                    //string strHighestSP = "";
                    string strDuration;
                    switch (CDTXMania.stageSongSelection.r現在選択中の曲.eNodeType)
                    {
                        case CSongListNode.ENodeType.SCORE:
                            {
                                int bpm_int = (int)Math.Round(cスコア.SongInformation.Bpm);
                                strBPM = bpm_int.ToString();
                                int duration = cスコア.SongInformation.Duration;
                                TimeSpan timeSpan = new TimeSpan(0, 0, 0, 0, duration);
                                strDuration = timeSpan.ToString(@"m\:ss");
                                
                                //DrOnly always show Drum
                                //GROnly show either Bass or Guitar
                                if (nDGmode == 0)
                                {
                                    if (cスコア.SongInformation.chipCountByInstrument.Drums > 0)
                                    {
                                        nPanelNoteCount = cスコア.SongInformation.chipCountByInstrument.Drums;
                                        arrChipsByLane = new int[] {
                                            cスコア.SongInformation.chipCountByLane[ELane.LC],
                                            cスコア.SongInformation.chipCountByLane[ELane.HH],
                                            cスコア.SongInformation.chipCountByLane[ELane.LP],
                                            cスコア.SongInformation.chipCountByLane[ELane.SD],
                                            cスコア.SongInformation.chipCountByLane[ELane.HT],
                                            cスコア.SongInformation.chipCountByLane[ELane.BD],
                                            cスコア.SongInformation.chipCountByLane[ELane.LT],
                                            cスコア.SongInformation.chipCountByLane[ELane.FT],
                                            cスコア.SongInformation.chipCountByLane[ELane.CY]
                                        };
                                        if (dbDrumSP > 0.00)
                                        {                                            
                                            strSP = string.Format("{0,6:##0.00}", dbDrumSP);
                                        }

                                        //Get Progress data here
                                        strProgressText = cスコア.SongInformation.progress.Drums;
                                    }
                                }
                                else
                                {
                                    if (dbGBSP > 0.00)
                                    {                                       
                                        strSP = string.Format("{0,6:##0.00}", dbGBSP);
                                    }

                                    if (CDTXMania.ConfigIni.bIsSwappedGuitarBass)
                                    {
                                        if (cスコア.SongInformation.chipCountByInstrument.Bass > 0)
                                        {
                                            nPanelNoteCount = cスコア.SongInformation.chipCountByInstrument.Bass;
                                            arrChipsByLane = new int[] {
                                                cスコア.SongInformation.chipCountByLane[ELane.BsR],
                                                cスコア.SongInformation.chipCountByLane[ELane.BsG],
                                                cスコア.SongInformation.chipCountByLane[ELane.BsB],
                                                cスコア.SongInformation.chipCountByLane[ELane.BsY],
                                                cスコア.SongInformation.chipCountByLane[ELane.BsP],
                                                cスコア.SongInformation.chipCountByLane[ELane.BsPick]
                                            };
                                            //Get Progress data here
                                            strProgressText = cスコア.SongInformation.progress.Bass;
                                        }
                                    }
                                    else
                                    {
                                        if (cスコア.SongInformation.chipCountByInstrument.Guitar > 0)
                                        {
                                            nPanelNoteCount = cスコア.SongInformation.chipCountByInstrument.Guitar;
                                            arrChipsByLane = new int[] {
                                                cスコア.SongInformation.chipCountByLane[ELane.GtR],
                                                cスコア.SongInformation.chipCountByLane[ELane.GtG],
                                                cスコア.SongInformation.chipCountByLane[ELane.GtB],
                                                cスコア.SongInformation.chipCountByLane[ELane.GtY],
                                                cスコア.SongInformation.chipCountByLane[ELane.GtP],
                                                cスコア.SongInformation.chipCountByLane[ELane.GtPick]
                                            };
                                            //Get Progress data here
                                            strProgressText = cスコア.SongInformation.progress.Guitar;
                                        }
                                    }
                                }
                                break;
                            }
                        default:
                            {
                                strBPM = "";
                                strDuration = "";
                                break;
                            }
                    }

                    //this.txBPM画像.tDraw2D(CDTXMania.app.Device, nBPM位置X, nBPM位置Y);
                    tDrawBPM(nBPM位置X + 45, nBPM位置Y + 23, string.Format("{0,3:###}", strBPM));
                    //Length of Song
                    tDrawBPM(nBPM位置X + 42, nBPM位置Y - 7, strDuration);                
                }
                #endregion

                #region [Skill Point Panel]
                if(txSkillPointPanel != null)
                {
                    txSkillPointPanel.tDraw2D(CDTXMania.app.Device, 32, 180);
                }
                
                if(strSP != "")
                {
                    tDrawSkillPoints(32 + 60, 200, strSP);
                }

                #endregion

                

                #region [Draw Graphs Panels]

                int nGraphBaseX = 15;
                int nGraphBaseY = 368; // 350 + 18

                if (CDTXMania.ConfigIni.bGuitarEnabled)
                {
                    if(txGuitarBassGraphPanel != null)
                    {
                        txGuitarBassGraphPanel.tDraw2D(CDTXMania.app.Device, nGraphBaseX, nGraphBaseY);
                    }                    
                }
                else
                {
                    if(txDrumsGraphPanel != null)
                    {
                        txDrumsGraphPanel.tDraw2D(CDTXMania.app.Device, nGraphBaseX, nGraphBaseY);
                    }                    
                }

                //Draw total notes
                if(nPanelNoteCount > 0)
                {
                    string strPanelNoteCount = string.Format("{0}", nPanelNoteCount);
                    int nTotalNotesPosX = nGraphBaseX + 66 - (strPanelNoteCount.Length - 1) * (st数字[0].rc.Width / 2);
                    int nTotalNotesPosY = nGraphBaseY + 298;
                    tDrawBPM(nTotalNotesPosX, nTotalNotesPosY, strPanelNoteCount);
                }
                //Draw Bar Graph for Chips per lane
                if (arrChipsByLane != null)
                {
                    int nBarMaxHeight = 252;
                    int[] chipsBarHeights = nCalculateChipsBarPxHeight(arrChipsByLane, nBarMaxHeight);

                    if (CDTXMania.ConfigIni.bGuitarEnabled)
                    {
                        if(chipsBarHeights.Length == txGBChipsBarLine.Length)
                        {
                            for (int i = 0; i < txGBChipsBarLine.Length; i++)
                            {
                                txGBChipsBarLine[i].tDraw2D(CDTXMania.app.Device,
                                    nGraphBaseX + 38 + i * 10, nGraphBaseY + 21 + (nBarMaxHeight - chipsBarHeights[i]), new Rectangle(0, 0, 4, chipsBarHeights[i]));
                            }
                        }                        
                    }
                    else
                    {
                        if(chipsBarHeights.Length == txDrumChipsBarLine.Length)
                        {
                            for (int i = 0; i < txDrumChipsBarLine.Length; i++)
                            {
                                txDrumChipsBarLine[i].tDraw2D(CDTXMania.app.Device,
                                    nGraphBaseX + 31 + i * 8, nGraphBaseY + 21 + (nBarMaxHeight - chipsBarHeights[i]), new Rectangle(0, 0, 4, chipsBarHeights[i]));
                            }
                        }
                    }

                }

                //Draw Progress Bar
                tDrawProgressBar(strProgressText, nGraphBaseX + 18, nGraphBaseY + 21);
                
                #endregion
                //-----------------

                int[] nPart = { 0, CDTXMania.ConfigIni.bIsSwappedGuitarBass ? 2 : 1, CDTXMania.ConfigIni.bIsSwappedGuitarBass ? 1 : 2 };

                int nBaseX = 130;
                int nBaseY = 350;//350

                int n難易度文字X = 70;
                int n難易度文字Y = 75;

                if (txパネル本体 != null)
                {
                    n難易度文字X = nBaseX + 10;
                    n難易度文字Y = nBaseY + 2;
                }

                #region [ ステータスパネルの描画 ]
                //-----------------
                if (txパネル本体 != null)
                {
                    txパネル本体.tDraw2D(CDTXMania.app.Device, nBaseX, nBaseY);

                    int nPanelW = 187;
                    int nPanelH = 60;

                    for (int j = 0; j < 3; j++)
                    {

                        if (tx難易度パネル != null)
                        {
                            nPanelW = tx難易度パネル.szImageSize.Width / 3;
                            nPanelH = tx難易度パネル.szImageSize.Height * 2 / 11;
                        }

                        int nPanelX = nBaseX + txパネル本体.szImageSize.Width + (nPanelW * (nPart[j] - 3));
                        int nPanelY = nBaseY + txパネル本体.szImageSize.Height - (nPanelH * 11 / 2) - 5; // Note: Effectively not in use

                        int nRankW;

                        int flag = 0;
                        int n変数;
                        double db変数;

                        if (tx難易度パネル != null)
                            tx難易度パネル.tDraw2D(CDTXMania.app.Device, nPanelX, nPanelY, new Rectangle(nPanelW * j, 0, nPanelW, tx難易度パネル.szImageSize.Height));

                        int[] n難易度整数 = new int[5];
                        int[] n難易度小数 = new int[5];
                        for (int i = 0; i < 5; i++)
                        {
                            if (str難易度ラベル[i] != null || CDTXMania.stageSongSelection.r現在選択中の曲.eNodeType == CSongListNode.ENodeType.RANDOM)
                            {

                                int nBoxX = nPanelX;
                                int nBoxY = ( 391 + ( ( 4 - i ) * 60 ) ) - 2;// Note: does not use nPanelY

                                if (n現在選択中の曲の難易度 == i && tx難易度枠 != null)
                                {
                                    if ((CDTXMania.ConfigIni.bDrumsEnabled && j == 0) || (CDTXMania.ConfigIni.bGuitarEnabled && j != 0))
                                        tx難易度枠.tDraw2D(CDTXMania.app.Device, nBoxX, nBoxY);
                                }

                                #region [ 選択曲の Lv の描画 ]
                                if ((cスコア != null) && (txDifficultyNumber != null))
                                {
                                    // convert the level back into a whole number (0-999) to make it easier to work with
                                    int nLevel = (n現在選択中の曲のレベル難易度毎DGB[i][j] * 10) + n現在選択中の曲のレベル小数点難易度毎DGB[i][j];
                                    bool bHasSong = b現在選択中の曲に譜面がある[i][j];
                                    bool bClassChartModeSet = CDTXMania.ConfigIni.bCLASSIC譜面判別を有効にする;
                                    if(CDTXMania.DTX != null)
                                    {
                                        bClassChartModeSet = bClassChartModeSet && (
                                            !CDTXMania.DTX.bHasChips.LeftCymbal &&
                                            !CDTXMania.DTX.bHasChips.LP &&
                                            !CDTXMania.DTX.bHasChips.LBD &&
                                            !CDTXMania.DTX.bHasChips.FT &&
                                            !CDTXMania.DTX.bHasChips.Ride &&
                                            !CDTXMania.DTX.bForceXGChart
                                            );
                                    }
                                    
                                    bool bShowClassicLevel = CDTXMania.ConfigIni.nSkillMode == 0 || bClassChartModeSet;
                                    
                                    int nX = nBoxX + nPanelW - 77;
                                    int nY = nBoxY + nPanelH - 35;
                                    if (bHasSong)
                                    {
                                        if (bShowClassicLevel)
                                            tDrawDifficulty(nX, nY, string.Format(@"{0,2:00}", nLevel / 10));
                                        else
                                            tDrawDifficulty(nX, nY, string.Format(@"{0,1:0}.{1,2:00}", nLevel / 100, nLevel % 100));
                                    }
                                    else
                                    {
                                        if (bShowClassicLevel)
                                            tDrawDifficulty(nX, nY, @"--");
                                        else
                                            tDrawDifficulty(nX, nY, @"-.--");
                                    }
                                }
                                #endregion
                                db変数 = db現在選択中の曲の最高スキル値難易度毎[i][j];

                                if (db変数 < 0)
                                    db変数 = 0;

                                if (db変数 > 100)
                                    db変数 = 100;

                                if (db変数 != 0.00)
                                {
                                    if (txランク != null)
                                    {
                                        nRankW = 35;// this.txランク.szImageSize.Width / 9;

                                        #region [ 選択曲の FullCombo Excellent の 描画 ]
                                        if (db現在選択中の曲の最高スキル値難易度毎[i][j] == 100)
                                            txランク.tDraw2D(CDTXMania.app.Device, nBoxX + 42, nBoxY + 5, new Rectangle(nRankW * 8, 0, nRankW, txランク.szImageSize.Height));
                                        else if (b現在選択中の曲がフルコンボ難易度毎[i][j])
                                            txランク.tDraw2D(CDTXMania.app.Device, nBoxX + 42, nBoxY + 5, new Rectangle(nRankW * 7, 0, nRankW, txランク.szImageSize.Height));
                                        #endregion
                                        #region [ 選択曲の 最高ランクの描画 ]
                                        n変数 = n現在選択中の曲の最高ランク難易度毎[i][j];

                                        if (n変数 != 99)
                                        {
                                            if (n変数 < 0)
                                                n変数 = 0;

                                            if (n変数 > 6)
                                                n変数 = 6;

                                            txランク.tDraw2D(CDTXMania.app.Device, nBoxX + 7, nBoxY + 5, new Rectangle(nRankW * n変数, 0, nRankW, txランク.szImageSize.Height));
                                        }
                                        #endregion
                                    }
                                    #region [ 選択曲の 最高スキル値ゲージ＋数値の描画 ]
                                    if (tx達成率MAX != null && db変数 == 100)
                                        tx達成率MAX.tDraw2D(CDTXMania.app.Device, nBoxX + nPanelW - 142, nBoxY + nPanelH - 27);
                                    else
                                        tDrawAchievementRate(nBoxX + nPanelW - 157, nBoxY + nPanelH - 27, string.Format("{0,6:##0.00}%", db変数));
                                    #endregion
                                }

                            }
                            else if (CDTXMania.stageSongSelection.r現在選択中の曲.eNodeType == CSongListNode.ENodeType.SCORE)
                            {
                                flag = flag + 1;
                            }
                        }
                        if (flag == 5)
                        {
                            int nBoxX = nPanelX;
                            int nBoxY = 389;//Equal to ( 391 + ( ( 4 - i ) * 60 ) ) - 2 where i is 4

                            if (tx難易度枠 != null)
                            {
                                if ((CDTXMania.ConfigIni.bDrumsEnabled && j == 0) || (CDTXMania.ConfigIni.bGuitarEnabled && j != 0))
                                    tx難易度枠.tDraw2D(CDTXMania.app.Device, nBoxX, nBoxY);
                            }

                            #region [ 選択曲の Lv の描画 ]
                            if ((cスコア != null) && (txDifficultyNumber != null))
                            {
                                n難易度整数[0] = (int)n現在選択中の曲のレベル[ j ] / 10;
                                n難易度小数[0] = (n現在選択中の曲のレベル[ j ] - ( n難易度整数[ 0 ] * 10 ) ) * 10;
                                n難易度小数[0] += n現在選択中の曲のレベル小数点[ j ];

                                if (b現在選択中の曲の譜面[j] && CDTXMania.stageSongSelection.r現在選択中の曲.eNodeType == CSongListNode.ENodeType.SCORE)
                                {
                                    tDrawDifficulty(nBoxX + nPanelW - 77, nBoxY + nPanelH - 35, string.Format("{0,4:0.00}", ((double)n難易度整数[ 0 ]) + (((double)n難易度小数[ 0 ]) / 100)));
                                }
                                else if (!b現在選択中の曲の譜面[j] && CDTXMania.stageSongSelection.r現在選択中の曲.eNodeType == CSongListNode.ENodeType.SCORE)
                                {
                                    tDrawDifficulty(nBoxX + nPanelW - 77, nBoxY + nPanelH - 35, ("-.--"));
                                }
                            }
                            #endregion
                            db変数 = db現在選択中の曲の最高スキル値[j];

                            if (db変数 < 0)
                                db変数 = 0;

                            if (db変数 > 100)
                                db変数 = 100;

                            if (db変数 != 0.00)
                            {
                                if (txランク != null)
                                {
                                    nRankW = 35;// this.txランク.szImageSize.Width / 9;

                                    #region [ 選択曲の FullCombo Excellent の 描画 ]
                                    if (db現在選択中の曲の最高スキル値[j] == 100)
                                        txランク.tDraw2D(CDTXMania.app.Device, nBoxX + 42, nBoxY + 5, new Rectangle(nRankW * 8, 0, nRankW, txランク.szImageSize.Height));
                                    else if (b現在選択中の曲がフルコンボ[j])
                                        txランク.tDraw2D(CDTXMania.app.Device, nBoxX + 42, nBoxY + 5, new Rectangle(nRankW * 7, 0, nRankW, txランク.szImageSize.Height));
                                    #endregion
                                    #region [ 選択曲の 最高ランクの描画 ]
                                    n変数 = n現在選択中の曲の最高ランク[j];

                                    if (n変数 != 99)
                                    {
                                        if (n変数 < 0)
                                            n変数 = 0;

                                        if (n変数 > 6)
                                            n変数 = 6;

                                        txランク.tDraw2D(CDTXMania.app.Device, nBoxX + 7, nBoxY + 5, new Rectangle(nRankW * n変数, 0, nRankW, txランク.szImageSize.Height));
                                    }
                                    #endregion
                                }
                                #region [ 選択曲の 最高スキル値ゲージ＋数値の描画 ]
                                if (tx達成率MAX != null && db現在選択中の曲の最高スキル値[j] == 100.00)
                                    tx達成率MAX.tDraw2D(CDTXMania.app.Device, nBoxX + nPanelW - 155, nBoxY + nPanelH - 27);
                                else
                                    tDrawAchievementRate(nBoxX + nPanelW - 157, nBoxY + nPanelH - 27, string.Format("{0,6:##0.00}%", db変数));
                                #endregion
                                
                            }

                        }
                    }

                    #region [Draw Skill Badge on matched Difficulty for Drum]
                    int nBadgeWidth = 35;
                    if (nDrumSPDiffRank != -1)
                    {
                        int nDiffOffset = nDrumSPDiffRank;
                        if (!bHasMultipleDiff)
                        {
                            //For songs without multiple diff defined in set.def, set offset to highest 
                            nDiffOffset = 4;
                        }
                        int nDGBIndex = 0;
                        int nBoxX = 130 + txパネル本体.szImageSize.Width + (nPanelW * (nPart[nDGBIndex] - 3));
                        int nBoxY = (391 + ((4 - nDiffOffset) * 60)) - 2;
                        txランク.tDraw2D(CDTXMania.app.Device, nBoxX + 75, nBoxY + 5, new Rectangle(nBadgeWidth * 9, 0, nBadgeWidth, txランク.szImageSize.Height));                       
                    }
                    #endregion

                    #region [Draw Skill Badge on matched Difficulty for Guitar Bass]
                    if (nGBSPDiffRank != -1)
                    {
                        int nDiffOffset = nGBSPDiffRank;
                        if (!bHasMultipleDiff)
                        {
                            //For songs without multiple diff defined in set.def, set offset to highest 
                            nDiffOffset = 4;
                        }
                        int nDGBIndex = 1 + nSpInGuitarOrBass;
                        int nBoxX = 130 + txパネル本体.szImageSize.Width + (nPanelW * (nPart[nDGBIndex] - 3));
                        int nBoxY = (391 + ((4 - nDiffOffset) * 60)) - 2;
                        txランク.tDraw2D(CDTXMania.app.Device, nBoxX + 75, nBoxY + 5, new Rectangle(nBadgeWidth * 9, 0, nBadgeWidth, txランク.szImageSize.Height));
                    }
                    #endregion

                }
                #endregion
                #region [ 難易度文字列の描画 ]
                //-----------------
                for (int i = 0; i < 5; i++)
                {
                    CDTXMania.actDisplayString.tPrint(n難易度文字X + (i * 110), n難易度文字Y, (n現在選択中の曲の難易度 == i) ? CCharacterConsole.EFontType.Red : CCharacterConsole.EFontType.White, str難易度ラベル[i]);
                }
                #endregion
            }
            return 0;
        }


        // Other

        #region [ private ]
        //-----------------
        [StructLayout(LayoutKind.Sequential)]
        private struct ST数字
        {
            public char ch;
            public Rectangle rc;
            public ST数字(char ch, Rectangle rc)
            {
                this.ch = ch;
                this.rc = rc;
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct ST達成率数字
        {
            public char ch;
            public Rectangle rc;
            public ST達成率数字(char ch, Rectangle rc)
            {
                this.ch = ch;
                this.rc = rc;
            }
        }
        private struct ST難易度数字
        {
            public char ch;
            public Rectangle rc;
            public ST難易度数字(char ch, Rectangle rc)
            {
                this.ch = ch;
                this.rc = rc;
            }
        }

        private STDGBVALUE<bool> b現在選択中の曲がフルコンボ;
        private STDGBVALUE<bool> b現在選択中の曲の譜面;
        private STDGBVALUE<bool>[] b現在選択中の曲がフルコンボ難易度毎 = new STDGBVALUE<bool>[5];
        private STDGBVALUE<bool>[] b現在選択中の曲に譜面がある = new STDGBVALUE<bool>[5];
        private STDGBVALUE<int>[] n現在選択中の曲のレベル難易度毎DGB = new STDGBVALUE<int>[5];
        private STDGBVALUE<int>[] n現在選択中の曲のレベル小数点難易度毎DGB = new STDGBVALUE<int>[5];
        private CCounter ct登場アニメ用;
        private CCounter ct難易度スクロール用;
        private CCounter ct難易度矢印用;
        private STDGBVALUE<double> db現在選択中の曲の最高スキル値;
        private STDGBVALUE<double>[] db現在選択中の曲の最高スキル値難易度毎 = new STDGBVALUE<double>[5];
        private double[] db現在選択中の曲の曲別スキル値難易度毎 = new double[5];
        private STDGBVALUE<double> db現在選択中の曲の曲別スキル;
        private STDGBVALUE<int> n現在選択中の曲のレベル;
        private STDGBVALUE<int> n現在選択中の曲のレベル小数点;
        private int[] n選択中の曲のレベル難易度毎 = new int[5];
        private STDGBVALUE<int> n現在選択中の曲の最高ランク;
        private STDGBVALUE<int>[] n現在選択中の曲の最高ランク難易度毎 = new STDGBVALUE<int>[5];
        //
        private double dbDrumSP = 0.0;
        private int nDrumSPDiffRank = -1;
        private double dbGBSP = 0.0;
        private int nGBSPDiffRank = -1;
        private int nSpInGuitarOrBass = 0;//G:0 B:1
        private bool bHasMultipleDiff = false;
        private string strCurrentProgressBar = "";

        private int n現在選択中の曲の難易度;
        private int n難易度開始文字位置;
        private const int n難易度表示可能文字数 = 0x24;
        /*
        private readonly Rectangle[] rcランク = new Rectangle[]
        {
            new Rectangle(0 * 34, 0, 34, 50),
            new Rectangle(1 * 34, 0, 34, 50),
            new Rectangle(2 * 34, 0, 34, 50),
            new Rectangle(3 * 34, 0, 34, 50),
            new Rectangle(4 * 34, 0, 34, 50),
            new Rectangle(5 * 34, 0, 34, 50),
            new Rectangle(6 * 34, 0, 34, 50),
            new Rectangle(7 * 34, 0, 34, 50),
            new Rectangle(8 * 34, 0, 34, 50),
            new Rectangle(9 * 34, 0, 34, 50)
        };
         */
        private CSongListNode r直前の曲;
        public string[] str難易度ラベル = new string[] { "", "", "", "", "" };
        
        private readonly ST数字[] st数字 = new ST数字[]
        {
            new ST数字('0', new Rectangle(0 * 12, 0, 12, 20)),
            new ST数字('1', new Rectangle(1 * 12, 0, 12, 20)),
            new ST数字('2', new Rectangle(2 * 12, 0, 12, 20)),
            new ST数字('3', new Rectangle(3 * 12, 0, 12, 20)),
            new ST数字('4', new Rectangle(4 * 12, 0, 12, 20)),
            new ST数字('5', new Rectangle(5 * 12, 0, 12, 20)),
            new ST数字('6', new Rectangle(6 * 12, 0, 12, 20)),
            new ST数字('7', new Rectangle(7 * 12, 0, 12, 20)),
            new ST数字('8', new Rectangle(8 * 12, 0, 12, 20)),
            new ST数字('9', new Rectangle(9 * 12, 0, 12, 20)),
            new ST数字(':', new Rectangle(10 * 12 + 3, 0, 6, 20)),
            new ST数字('p', new Rectangle(11 * 12, 0, 12, 20)),
        };
        private readonly ST難易度数字[] stDifficultyNumber = new ST難易度数字[]
        {
            new ST難易度数字('0', new Rectangle(0 * 20, 0, 20, 28)),
            new ST難易度数字('1', new Rectangle(1 * 20, 0, 20, 28)),
            new ST難易度数字('2', new Rectangle(2 * 20, 0, 20, 28)),
            new ST難易度数字('3', new Rectangle(3 * 20, 0, 20, 28)),
            new ST難易度数字('4', new Rectangle(4 * 20, 0, 20, 28)),
            new ST難易度数字('5', new Rectangle(5 * 20, 0, 20, 28)),
            new ST難易度数字('6', new Rectangle(6 * 20, 0, 20, 28)),
            new ST難易度数字('7', new Rectangle(7 * 20, 0, 20, 28)),
            new ST難易度数字('8', new Rectangle(8 * 20, 0, 20, 28)),
            new ST難易度数字('9', new Rectangle(9 * 20, 0, 20, 28)),
            new ST難易度数字('.', new Rectangle(10 * 20, 0, 10, 28)),
            new ST難易度数字('-', new Rectangle(11 * 20 - 10, 0, 20, 28)),
            new ST難易度数字('?', new Rectangle(12 * 20 - 10, 0, 20, 28))
        };
        private readonly ST達成率数字[] st達成率数字 = new ST達成率数字[]
        {
            new ST達成率数字('0', new Rectangle(0 * 12, 0, 12, 20)),
            new ST達成率数字('1', new Rectangle(1 * 12, 0, 12, 20)),
            new ST達成率数字('2', new Rectangle(2 * 12, 0, 12, 20)),
            new ST達成率数字('3', new Rectangle(3 * 12, 0, 12, 20)),
            new ST達成率数字('4', new Rectangle(4 * 12, 0, 12, 20)),
            new ST達成率数字('5', new Rectangle(5 * 12, 0, 12, 20)),
            new ST達成率数字('6', new Rectangle(6 * 12, 0, 12, 20)),
            new ST達成率数字('7', new Rectangle(7 * 12, 0, 12, 20)),
            new ST達成率数字('8', new Rectangle(8 * 12, 0, 12, 20)),
            new ST達成率数字('9', new Rectangle(9 * 12, 0, 12, 20)),
            new ST達成率数字('.', new Rectangle(10 * 12, 0, 6, 20)),
            new ST達成率数字('%', new Rectangle(11 * 12 - 6, 0, 12, 20))
        };
        private readonly ST数字[] stLargeCharPositions = new ST数字[]
        {
            new ST数字('0', new Rectangle(0 * 28, 0, 28, 42)),
            new ST数字('1', new Rectangle(1 * 28, 0, 28, 42)),
            new ST数字('2', new Rectangle(2 * 28, 0, 28, 42)),
            new ST数字('3', new Rectangle(3 * 28, 0, 28, 42)),
            new ST数字('4', new Rectangle(4 * 28, 0, 28, 42)),
            new ST数字('5', new Rectangle(5 * 28, 0, 28, 42)),
            new ST数字('6', new Rectangle(6 * 28, 0, 28, 42)),
            new ST数字('7', new Rectangle(7 * 28, 0, 28, 42)),
            new ST数字('8', new Rectangle(8 * 28, 0, 28, 42)),
            new ST数字('9', new Rectangle(9 * 28, 0, 28, 42)),
            new ST数字('.', new Rectangle(10 * 28, 0, 10, 42))
        };

        private readonly Rectangle rcunused = new Rectangle(0, 0x21, 80, 15);
        public CTexture txパネル本体;
        private CTexture txランク;
        private CTexture tx達成率MAX;
        private CTexture tx難易度パネル;
        private CTexture tx難易度枠;
        private CTexture txDifficultyNumber;
        private CTexture txAchievementRateNumber;
        private CTexture txBPM数字;
        private CTexture txDrumsGraphPanel;
        private CTexture txGuitarBassGraphPanel;
        //private CTexture txBPM画像;
        //private CTexture txTestSolidLine;
        private CTexture[] txDrumChipsBarLine = new CTexture[9];
        private Color[] clDrumChipsBarColors = new Color[9]
        {
            Color.PaleVioletRed,
            Color.DeepSkyBlue,
            Color.HotPink,
            Color.Yellow,
            Color.Green,
            Color.MediumPurple,
            Color.Red,
            Color.Orange,
            Color.DeepSkyBlue
        };
        private CTexture[] txGBChipsBarLine = new CTexture[6];
        private Color[] clGBChipsBarColors = new Color[6]
        {
            Color.Red,
            Color.Green,
            Color.DeepSkyBlue,
            Color.Yellow,
            Color.HotPink,
            Color.White
        };
        private CTexture txSkillPointPanel;
        private CTexture txProgressBar;
        private Color[] clProgressBarColors = new Color[4]
        {
            Color.Black,
            Color.DeepSkyBlue,
            Color.Yellow,
            Color.Yellow
        };
        private int nCheckDifficultyLabelDisplayAndReturnScrollDirection()
        {
            int num = 0;
            int length = 0;
            for (int i = 0; i < 5; i++)
            {
                if ((str難易度ラベル[i] != null) && (str難易度ラベル[i].Length > 0))
                {
                    length = str難易度ラベル[i].Length;
                }
                if (n現在選択中の曲の難易度 == i)
                {
                    break;
                }
                if ((str難易度ラベル[i] != null) && (str難易度ラベル.Length > 0))
                {
                    num += length + 2;
                }
            }
            if (num >= (n難易度開始文字位置 + 0x24))
            {
                return 1;
            }
            if ((num + length) <= n難易度開始文字位置)
            {
                return -1;
            }
            if (((num + length) - 1) >= (n難易度開始文字位置 + 0x24))
            {
                return 1;
            }
            if (num < n難易度開始文字位置)
            {
                return -1;
            }
            return 0;
        }

        private void txGenerateProgressBarLine(string strProgressBar) 
        {
            int nBarWidth = 4;
            int nBarHeight = 294; //294;

            CActPerfProgressBar.txGenerateProgressBarHelper(ref txProgressBar, strProgressBar, nBarWidth, nBarHeight, CActPerfProgressBar.nSectionIntervalCount);

        }

        private void tDrawProgressBar(string strProgressBar, int nPosX, int nPosY) 
        { 
            if(!strCurrentProgressBar.Equals(strProgressBar))
            {
                //Recreate texture only if string is different
                CDTXMania.tReleaseTexture(ref txProgressBar);
                txGenerateProgressBarLine(strProgressBar);
                strCurrentProgressBar = strProgressBar;
            }

            if (txProgressBar != null)
            {
                txProgressBar.tDraw2D(CDTXMania.app.Device, nPosX, nPosY);
            }
        }

        private void txGenerateGraphBarLine()
        {
            int nBarWidth = 4;
            int nBarHeight = 252;

            for (int i = 0; i < txDrumChipsBarLine.Length; i++)
            {
                using (Bitmap tempBarBitmap = new Bitmap(nBarWidth, nBarHeight))
                {
                    using (Graphics barGraphics = Graphics.FromImage(tempBarBitmap))
                    {
                        barGraphics.FillRectangle(new SolidBrush(clDrumChipsBarColors[i]), 0, 0, tempBarBitmap.Width, tempBarBitmap.Height);
                    }
                    txDrumChipsBarLine[i] = CDTXMania.tGenerateTexture(tempBarBitmap);
                }
            }

            for (int i = 0; i < txGBChipsBarLine.Length; i++)
            {
                using (Bitmap tempBarBitmap = new Bitmap(nBarWidth, nBarHeight))
                {
                    using (Graphics barGraphics = Graphics.FromImage(tempBarBitmap))
                    {
                        barGraphics.FillRectangle(new SolidBrush(clGBChipsBarColors[i]), 0, 0, tempBarBitmap.Width, tempBarBitmap.Height);
                    }
                    txGBChipsBarLine[i] = CDTXMania.tGenerateTexture(tempBarBitmap);
                }
            }

        }

        private int[] nCalculateChipsBarPxHeight(int[] arrChipCount, int nMaxBarLength)
        {
            if(arrChipCount != null)
            {
                int[] nChipsBarPxHeight = new int[arrChipCount.Length];

                //Official formula to compute bar Height is unknown (Need to RE)
                //Use a Placeholder formula for now
                //int nMaxFactor = nTotalNoteCount / arrChipCount.Length;
                int nMaxFactor = 300;
                //Capped by upper and lower bound
                //nMaxFactor = nMaxFactor < nLowerBound ? nLowerBound : nMaxFactor;
                //nMaxFactor = nMaxFactor > nUpperBound ? nUpperBound : nMaxFactor;

                for (int i = 0; i < nChipsBarPxHeight.Length; i++)
                {
                    int nChipPxHeight = arrChipCount[i] * nMaxBarLength / nMaxFactor;
                    nChipPxHeight = nChipPxHeight > nMaxBarLength ? nMaxBarLength : nChipPxHeight;
                    nChipsBarPxHeight[i] = nChipPxHeight;
                }

                return nChipsBarPxHeight;
            }

            return null;
        }
        private void tDrawAchievementRate(int x, int y, string str)
        {
            for (int j = 0; j < str.Length; j++)
            {
                char c = str[j];
                for (int i = 0; i < st達成率数字.Length; i++)
                {
                    if (st達成率数字[i].ch == c)
                    {
                        Rectangle rectangle = new Rectangle(st達成率数字[i].rc.X, st達成率数字[i].rc.Y, 12, 20);
                        if (c == '.')
                        {
                            rectangle.Width -= 6;
                        }
                        if (txAchievementRateNumber != null)
                        {
                            txAchievementRateNumber.tDraw2D(CDTXMania.app.Device, x, y, rectangle);
                        }
                        break;
                    }
                }
                if (c == '.')
                {
                    x += 6;
                }
                else
                {
                    x += 12;
                }
            }
        }

        private void tDrawSkillPoints(int x, int y, string str)
        {
            for (int j = 0; j < str.Length; j++)
            {
                char c = str[j];
                int currCharWidth = stDifficultyNumber[0].rc.Width;//Initialize to first char width of 28
                for (int i = 0; i < stDifficultyNumber.Length; i++)
                {
                    if (stDifficultyNumber[i].ch == c)
                    {
                        Rectangle rectangle = new Rectangle(stDifficultyNumber[i].rc.X, stDifficultyNumber[i].rc.Y, stDifficultyNumber[i].rc.Width, stDifficultyNumber[i].rc.Height);
                        if (txDifficultyNumber != null)
                        {
                            txDifficultyNumber.tDraw2D(CDTXMania.app.Device, x, y, rectangle);
                        }
                        currCharWidth = stDifficultyNumber[i].rc.Width;
                        break;
                    }
                }
                x += currCharWidth;
            }
        }

        private void tDrawDifficulty(int x, int y, string str)
        {
            for (int j = 0; j < str.Length; j++)
            {
                char c = str[j];
                for (int i = 0; i < stDifficultyNumber.Length; i++)
                {
                    if (stDifficultyNumber[i].ch == c)
                    {
                        Rectangle rectangle = new Rectangle(stDifficultyNumber[i].rc.X, stDifficultyNumber[i].rc.Y, 20, 28);
                        if (c == '.')
                        {
                            rectangle.Width -= 10;
                        }
                        if (txDifficultyNumber != null)
                        {
                            txDifficultyNumber.tDraw2D(CDTXMania.app.Device, x, y, rectangle);
                        }
                        break;
                    }
                }
                if (c == '.')
                {
                    x += 10;
                }
                else
                {
                    x += 20;
                }
            }
        }
        private void tDrawBPM(int x, int y, string str)
        {
            for (int j = 0; j < str.Length; j++)
            {
                int currCharWidth = 12;
                for (int i = 0; i < st数字.Length; i++)
                {
                    if (st数字[i].ch == str[j])
                    {
                        Rectangle rectangle = new Rectangle(st数字[i].rc.X, st数字[i].rc.Y, st数字[i].rc.Width, st数字[i].rc.Height);
                        if (txBPM数字 != null)
                        {
                            txBPM数字.tDraw2D(CDTXMania.app.Device, x, y, rectangle);
                        }
                        currCharWidth = st数字[i].rc.Width;
                        break;
                    }
                }
                x += currCharWidth;
            }
        }
        //-----------------
        #endregion
    }
}
