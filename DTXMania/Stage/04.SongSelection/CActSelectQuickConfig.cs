﻿using System.Diagnostics;
using System.Drawing;
using DTXMania.Core;
using DTXMania.UI;
using DTXMania.UI.Item;
using DTXUIRenderer;
using SharpDX;
using FDK;

namespace DTXMania;

internal class CActSelectQuickConfig : CActSelectPopupMenu
{
    private readonly string QuickCfgTitle = "Quick Config";


    // コンストラクタ

    public CActSelectQuickConfig()
    {
        //Set Quick config default target to DRUMS
        CActSelectQuickConfigMain(EInstrumentPart.DRUMS);
    }

    private void CActSelectQuickConfigMain(EInstrumentPart inst)
    {
        /*
        •Target: Drums/Guitar/Bass
        •Auto Mode: All ON/All OFF/CUSTOM
        •Auto Lane:
        •Scroll Speed:
        •Play Speed:
        •Risky:
        •Hidden/Sudden: None/Hidden/Sudden/Both
        •Conf SET: SET-1/SET-2/SET-3
        •More...
        •EXIT
        */
        lci = new List<CItemBase>[3];									// この画面に来る度に、メニューを作り直す。

        //initialize each instrument's menu
        lci[(int)EInstrumentPart.DRUMS] = MakeListCItemBase((int)EInstrumentPart.DRUMS);
        lci[(int)EInstrumentPart.GUITAR] = MakeListCItemBase((int)EInstrumentPart.GUITAR);
        lci[(int)EInstrumentPart.BASS] = MakeListCItemBase((int)EInstrumentPart.BASS);
            
        Initialize(lci[(int)inst], QuickCfgTitle, 2);	// ConfSet=0, nInst=Drums
    }

    private List<CItemBase> MakeListCItemBase(int nInstrument)
    {
        List<CItemBase> itemList = new List<CItemBase>();
            
        #region [ 共通 Target/AutoMode/AutoLane ]
        var target = new CSwitchItemList("Target", CItemBase.EPanelType.Normal, nInstrument, "", "",
            new string[] { "Drums", "Guitar", "Bass" })
        {
            action = () =>
            {
                nCurrentTarget = (nCurrentTarget + 1) % 3;
                
                // eInst = (EInstrumentPart) nCurrentTarget;	// ここではeInstは変えない。メニューを開いたタイミングでのみeInstを使う
                Initialize(lci[nCurrentTarget], QuickCfgTitle, nCurrentSelection);
                MakeAutoPanel();
            }
        };
        itemList.Add(target);
            
        List<int> automode = tConfigureAuto_DefaultSettings();
        if (nInstrument == (int)EInstrumentPart.DRUMS)
        {
            var autoMode = new CItemList("Auto Mode", CItemBase.EPanelType.Normal, automode[nInstrument], "", "",
                new string[] { "All Auto", "Auto LP", "Auto BD", "2PedalAuto", "XGLaneAuto", "Custom", "OFF" })
            {
                action = MakeAutoPanel
            };
            itemList.Add(autoMode);
        }
        else
        {
            var autoMode = new CItemList("Auto Mode", CItemBase.EPanelType.Normal, automode[nInstrument], "", "",
                new string[] { "All Auto", "Auto Neck", "Auto Pick", "Custom", "OFF" })
            {
                action = MakeAutoPanel
            };
            itemList.Add(autoMode);
        }
        #endregion
            
        #region [ 個別 ScrollSpeed ]
        var scrollSpeed = new CItemInteger("ScrollSpeed", 0, 1999, CDTXMania.ConfigIni.nScrollSpeed[nInstrument],
            "演奏時のドラム譜面のスクロールの\n" +
            "速度を指定します。\n" +
            "x0.5 ～ x1000.0 を指定可能です。",
            "To change the scroll speed for the\n" +
            "drums lanes.\n" +
            "You can set it from x0.5 to x1000.0.\n" +
            "(ScrollSpeed=x0.5 means half speed)")
        {
            action = () =>
            {
                CDTXMania.ConfigIni.nScrollSpeed[nInstrument] = (int)GetObj現在値((int)EOrder.ScrollSpeed);
            }
        };
        itemList.Add(scrollSpeed);
        #endregion
            
        #region [ 共通 Dark/Risky/PlaySpeed ]

        var dark = new CItemList("Dark", CItemBase.EPanelType.Normal, (int)CDTXMania.ConfigIni.eDark,
            "HALF: 背景、レーン、ゲージが表示\n" +
            "されなくなります。\n" +
            "FULL: さらに小節線、拍線、判定ラ\n" +
            "イン、パッドも表示されなくなります。",
            "OFF: all display parts are shown.\n" +
            "HALF: wallpaper, lanes and gauge are\n" +
            " disappeared.\n" +
            "FULL: additionaly to HALF, bar/beat\n" +
            " lines, hit bar, pads are disappeared.",
            new string[] { "OFF", "HALF", "FULL" })
        {
            action = () =>
            {
                EDarkMode d = (EDarkMode)GetIndex((int)EOrder.Dark);
                CDTXMania.ConfigIni.eDark = d;
                SetValueToAllTarget((int)EOrder.Dark, (int)d); // 全楽器で共有する値のため、全targetに値を展開する

                if (d == EDarkMode.FULL)
                {
                    CDTXMania.ConfigIni.nLaneDisp[nCurrentTarget] = 3;
                    CDTXMania.ConfigIni.bJudgeLineDisp[nCurrentTarget] = false;
                    CDTXMania.ConfigIni.bLaneFlush[nCurrentTarget] = false;
                }
                else if (d == EDarkMode.HALF)
                {
                    CDTXMania.ConfigIni.nLaneDisp[nCurrentTarget] = 1;
                    CDTXMania.ConfigIni.bJudgeLineDisp[nCurrentTarget] = true;
                    CDTXMania.ConfigIni.bLaneFlush[nCurrentTarget] = true;
                }
                else
                {
                    CDTXMania.ConfigIni.nLaneDisp[nCurrentTarget] = 0;
                    CDTXMania.ConfigIni.bJudgeLineDisp[nCurrentTarget] = true;
                    CDTXMania.ConfigIni.bLaneFlush[nCurrentTarget] = true;
                }
            }
        };
        itemList.Add(dark);

        var risky = new CItemInteger("Risky", 0, 10, CDTXMania.ConfigIni.nRisky,
            "Riskyモードの設定:\n" +
            "1以上の値にすると、その回数分の\n" +
            "Poor/MissでFAILEDとなります。\n" +
            "0にすると無効になり、\n" +
            "DamageLevelに従ったゲージ増減と\n" +
            "なります。\n" +
            "StageFailedの設定と併用できます。",
            "Risky mode:\n" +
            "Set over 1, in case you'd like to specify\n" +
            " the number of Poor/Miss times to be\n" +
            " FAILED.\n" +
            "Set 0 to disable Risky mode.")
        {
            action = () =>
            {
                int r = (int)GetObj現在値((int)EOrder.Risky);
                CDTXMania.ConfigIni.nRisky = r;
                SetValueToAllTarget((int)EOrder.Risky, r); // 全楽器で共有する値のため、全targetに値を展開する
            }
        };
        itemList.Add(risky);

        var playSpeed = new CItemInteger("PlaySpeed", CConstants.PLAYSPEED_MIN, CConstants.PLAYSPEED_MAX,
            CDTXMania.ConfigIni.nPlaySpeed,
            "曲の演奏速度を、速くしたり遅くした\n" +
            "りすることができます。\n" +
            "（※一部のサウンドカードでは正しく\n" +
            "再生できない可能性があります。）",
            "It changes the song speed.\n" +
            "For example, you can play in half\n" +
            " speed by setting PlaySpeed = 0.500\n" +
            " for your practice.\n" +
            "Note: It also changes the songs' pitch.")
        {
            action = () => CDTXMania.ConfigIni.nPlaySpeed = (int)GetObj現在値((int)EOrder.PlaySpeed)
        };
        itemList.Add(playSpeed);

        #endregion
            
        #region [ 個別 Sud/Hid ]

        var suddenHidden = new CItemList("HID/SUD", CItemBase.EPanelType.Normal, CDTXMania.ConfigIni.nHidSud[nInstrument],
            "",
            "",
            new string[] { "OFF", "HIDDEN", "SUDDEN", "HID/SUD", "STEALTH" })
        {
            action = () =>
            {
                CDTXMania.ConfigIni.nHidSud[nCurrentTarget] = (CDTXMania.ConfigIni.nHidSud[nCurrentTarget] + 1) % 5;

                if (CDTXMania.ConfigIni.nHidSud[nCurrentTarget] == 0)
                {
                    CDTXMania.ConfigIni.bHidden[nCurrentTarget] = false;
                    CDTXMania.ConfigIni.bSudden[nCurrentTarget] = false;
                }
                else if (CDTXMania.ConfigIni.nHidSud[nCurrentTarget] == 1)
                {
                    CDTXMania.ConfigIni.bHidden[nCurrentTarget] = true;
                    CDTXMania.ConfigIni.bSudden[nCurrentTarget] = false;
                }
                else if (CDTXMania.ConfigIni.nHidSud[nCurrentTarget] == 2)
                {
                    CDTXMania.ConfigIni.bHidden[nCurrentTarget] = false;
                    CDTXMania.ConfigIni.bSudden[nCurrentTarget] = true;
                }
                else if (CDTXMania.ConfigIni.nHidSud[nCurrentTarget] == 3)
                {
                    CDTXMania.ConfigIni.bHidden[nCurrentTarget] = true;
                    CDTXMania.ConfigIni.bSudden[nCurrentTarget] = true;
                }
                else if (CDTXMania.ConfigIni.nHidSud[nCurrentTarget] == 4)
                {
                    CDTXMania.ConfigIni.bHidden[nCurrentTarget] = true;
                    CDTXMania.ConfigIni.bSudden[nCurrentTarget] = true;
                }
            }
        };
        itemList.Add(suddenHidden);
            
        #endregion
            
        #region [ 個別 Ghost ]

        var autoGhost = new CItemList("AUTO Ghost", CItemBase.EPanelType.Normal,
            (int)CDTXMania.ConfigIni.eAutoGhost[nInstrument],
            "AUTOプレーのゴーストを指定します。\n",
            "Specify Play Ghost data.\n",
            new string[] { "Perfect", "Last Play", "Hi Skill", "Hi Score", "Online" }
        )
        {
            action = () =>
            {
                EAutoGhostData gd = (EAutoGhostData)GetIndex((int)EOrder.AutoGhost);
                CDTXMania.ConfigIni.eAutoGhost[nCurrentTarget] = gd;
            }
        };
        itemList.Add(autoGhost);

        var targetGhost = new CItemList("Target Ghost", CItemBase.EPanelType.Normal,
            (int)CDTXMania.ConfigIni.eTargetGhost[nInstrument],
            "ターゲットゴーストを指定します。\n",
            "Specify Target Ghost data.\n",
            new string[] { "None", "Perfect", "Last Play", "Hi Skill", "Hi Score", "Online" }
        )
        {
            action = () =>
            {
                ETargetGhostData gtd = (ETargetGhostData)GetIndex((int)EOrder.TargetGhost);
                CDTXMania.ConfigIni.eTargetGhost[nCurrentTarget] = gtd;
            }
        };
        itemList.Add(targetGhost);
        #endregion
            
        #region [ 共通 SET切り替え/More/Return ]
        //l.Add(new CSwitchItemList("Config Set", CItemBase.EPanelType.Normal, nCurrentConfigSet, "", "", new string[] { "SET-1", "SET-2", "SET-3" }));
        var more = new CSwitchItemList("More...", CItemBase.EPanelType.Normal, 0, "", "", new string[] { "" })
        {
            action = () =>
            {
                SetAutoParameters(); // 簡易CONFIGメニュー脱出に伴い、簡易CONFIG内のAUTOの設定をConfigIniクラスに反映する
                bGotoDetailConfig = true;
                tDeativatePopupMenu();
            }
        };
        itemList.Add(more);

        var returnBtn = new CSwitchItemList("Return", CItemBase.EPanelType.Normal, 0, "", "", new string[] { "", "" })
        {
            action = () =>
            {
                SetAutoParameters(); // 簡易CONFIGメニュー脱出に伴い、簡易CONFIG内のAUTOの設定をConfigIniクラスに反映する
                tDeativatePopupMenu();
            }
        };
        itemList.Add(returnBtn);
        #endregion

        return itemList;
    }

    /// <summary>
    /// 簡易CONFIGのAUTO設定値の初期値を、ConfigIniクラスから取得_推測する
    /// </summary>
    /// <returns>Drums,Guitar,BassのAutoMode値のリスト</returns>
    private List<int> tConfigureAuto_DefaultSettings()
    {
        List<int> l = new List<int>();
        int automode;
        #region [ Drums ]
        // "All Auto", "Auto LP", "Auto BD", "2Pedal Auto", "3 Auto", "Custom", "OFF"
        if (CDTXMania.ConfigIni.bAllDrumsAreAutoPlay)
        {
            automode = 0;	// All Auto
        }
        else if (CDTXMania.ConfigIni.bAutoPlay.LC == false && CDTXMania.ConfigIni.bAutoPlay.HH == false &&
                 CDTXMania.ConfigIni.bAutoPlay.BD == false && CDTXMania.ConfigIni.bAutoPlay.SD == false &&
                 CDTXMania.ConfigIni.bAutoPlay.HT == false && CDTXMania.ConfigIni.bAutoPlay.LT == false &&
                 CDTXMania.ConfigIni.bAutoPlay.FT == false && CDTXMania.ConfigIni.bAutoPlay.CY == false &&
                 CDTXMania.ConfigIni.bAutoPlay.LP == true && CDTXMania.ConfigIni.bAutoPlay.LBD == true)
        {
            automode = 1;	// Auto LP
        }
        else if (CDTXMania.ConfigIni.bAutoPlay.LC == false && CDTXMania.ConfigIni.bAutoPlay.HH == false &&
                 CDTXMania.ConfigIni.bAutoPlay.BD == true && CDTXMania.ConfigIni.bAutoPlay.SD == false &&
                 CDTXMania.ConfigIni.bAutoPlay.HT == false && CDTXMania.ConfigIni.bAutoPlay.LT == false &&
                 CDTXMania.ConfigIni.bAutoPlay.FT == false && CDTXMania.ConfigIni.bAutoPlay.CY == false &&
                 CDTXMania.ConfigIni.bAutoPlay.LP == false && CDTXMania.ConfigIni.bAutoPlay.LBD == false)
        {
            automode = 2;	// Auto BD
        }
        else if (CDTXMania.ConfigIni.bAutoPlay.LC == false && CDTXMania.ConfigIni.bAutoPlay.HH == false &&
                 CDTXMania.ConfigIni.bAutoPlay.BD == true && CDTXMania.ConfigIni.bAutoPlay.SD == false &&
                 CDTXMania.ConfigIni.bAutoPlay.HT == false && CDTXMania.ConfigIni.bAutoPlay.LT == false &&
                 CDTXMania.ConfigIni.bAutoPlay.FT == false && CDTXMania.ConfigIni.bAutoPlay.CY == false &&
                 CDTXMania.ConfigIni.bAutoPlay.LP == true && CDTXMania.ConfigIni.bAutoPlay.LBD == true)
        {
            automode = 3;	// 2Pedal Auto
        }
        else if (CDTXMania.ConfigIni.bAutoPlay.LC == true && CDTXMania.ConfigIni.bAutoPlay.HH == true &&
                 CDTXMania.ConfigIni.bAutoPlay.BD == false && CDTXMania.ConfigIni.bAutoPlay.SD == false &&
                 CDTXMania.ConfigIni.bAutoPlay.HT == false && CDTXMania.ConfigIni.bAutoPlay.LT == false &&
                 CDTXMania.ConfigIni.bAutoPlay.FT == true && CDTXMania.ConfigIni.bAutoPlay.CY == true &&
                 CDTXMania.ConfigIni.bAutoPlay.LP == true && CDTXMania.ConfigIni.bAutoPlay.LBD == true)
        {
            automode = 4;	// 3 Auto
        }
        else if (CDTXMania.ConfigIni.bAutoPlay.LC == false && CDTXMania.ConfigIni.bAutoPlay.HH == false &&
                 CDTXMania.ConfigIni.bAutoPlay.BD == false && CDTXMania.ConfigIni.bAutoPlay.SD == false &&
                 CDTXMania.ConfigIni.bAutoPlay.HT == false && CDTXMania.ConfigIni.bAutoPlay.LT == false &&
                 CDTXMania.ConfigIni.bAutoPlay.FT == false && CDTXMania.ConfigIni.bAutoPlay.CY == false &&
                 CDTXMania.ConfigIni.bAutoPlay.LP == false && CDTXMania.ConfigIni.bAutoPlay.LBD == false)
        {
            automode = 6;	// OFF
        }
        else
        {
            automode = 5;	// Custom
        }
        l.Add(automode);
        #endregion
        #region [ Guitar ]
        if (CDTXMania.ConfigIni.bAllGuitarsAreAutoPlay)
        {
            automode = 0;	// All Auto
        }
        else if (CDTXMania.ConfigIni.bAutoPlay.GtR == true && CDTXMania.ConfigIni.bAutoPlay.GtG == true &&
                 CDTXMania.ConfigIni.bAutoPlay.GtB == true && CDTXMania.ConfigIni.bAutoPlay.GtY == true && CDTXMania.ConfigIni.bAutoPlay.GtP == true && CDTXMania.ConfigIni.bAutoPlay.GtPick == false //&&
                 /*CDTXMania.ConfigIni.bAutoPlay.GtW == false*/)
        {
            automode = 1;	// Auto Neck
        }
        else if (CDTXMania.ConfigIni.bAutoPlay.GtR == false && CDTXMania.ConfigIni.bAutoPlay.GtG == false &&
                 CDTXMania.ConfigIni.bAutoPlay.GtB == false && CDTXMania.ConfigIni.bAutoPlay.GtY == false && CDTXMania.ConfigIni.bAutoPlay.GtP == false && CDTXMania.ConfigIni.bAutoPlay.GtPick == true //&&
                 /*CDTXMania.ConfigIni.bAutoPlay.GtW == false*/)
        {
            automode = 2;	// Auto Pick
        }
        else if (CDTXMania.ConfigIni.bAutoPlay.GtR == false && CDTXMania.ConfigIni.bAutoPlay.GtB == false &&
                 CDTXMania.ConfigIni.bAutoPlay.GtB == false && CDTXMania.ConfigIni.bAutoPlay.GtY == false && CDTXMania.ConfigIni.bAutoPlay.GtP == false && CDTXMania.ConfigIni.bAutoPlay.GtPick == false &&
                 CDTXMania.ConfigIni.bAutoPlay.GtW == false)
        {
            automode = 4;	// OFF
        }
        else
        {
            automode = 3;	// Custom
        }
        l.Add(automode);
        #endregion
        #region [ Bass ]
        if (CDTXMania.ConfigIni.bAllBassAreAutoPlay)
        {
            automode = 0;	// All Auto
        }
        else if (CDTXMania.ConfigIni.bAutoPlay.BsR == true && CDTXMania.ConfigIni.bAutoPlay.BsB == true &&
                 CDTXMania.ConfigIni.bAutoPlay.BsB == true && CDTXMania.ConfigIni.bAutoPlay.BsPick == false //&&
                 /*CDTXMania.ConfigIni.bAutoPlay.BsW == false*/)
        {
            automode = 1;	// Auto Neck
        }
        else if (CDTXMania.ConfigIni.bAutoPlay.BsR == false && CDTXMania.ConfigIni.bAutoPlay.BsB == false &&
                 CDTXMania.ConfigIni.bAutoPlay.BsB == false && CDTXMania.ConfigIni.bAutoPlay.BsPick == true //&&
                 /*CDTXMania.ConfigIni.bAutoPlay.BsW == false*/)
        {
            automode = 2;	// Auto Pick
        }
        else if (CDTXMania.ConfigIni.bAutoPlay.BsR == false && CDTXMania.ConfigIni.bAutoPlay.BsB == false &&
                 CDTXMania.ConfigIni.bAutoPlay.BsB == false && CDTXMania.ConfigIni.bAutoPlay.BsPick == false &&
                 CDTXMania.ConfigIni.bAutoPlay.BsW == false)
        {
            automode = 4;	// OFF
        }
        else
        {
            automode = 3;	// Custom
        }
        l.Add(automode);
        #endregion
        return l;
    }

    // メソッド
    public override void tActivatePopupMenu(EInstrumentPart einst)
    {
        //2024.2.23 fisyher: Add in update to nCurrentTarget and AutoPlay config panel here to fix state de-sync
        nCurrentTarget = (int)einst;
        MakeAutoPanel();
        CActSelectQuickConfigMain(einst);
        base.tActivatePopupMenu(einst);
    }
        
    /// <summary>
    /// Auto Modeにフォーカスを合わせているときだけ、AUTOの設定状態を表示する。
    /// 現状はDrumでのみ表示。
    /// </summary>
    public override void tDrawSub()
    {
        if (nCurrentSelection == (int)EOrder.AutoMode)
        {
            if (txAutoStatus == null) // TagetとAuto Modeを全く変更せずにAuto Modeまで動かした場合限り、ここに来る
            {
                MakeAutoPanel();
            }

            int x = ( nCurrentTarget == (int) EInstrumentPart.DRUMS ) ? 0 : 39;
            autoStatus.position.X = x + 25;
            autoStatus.position.Y = 10;
                
            subMenu.isVisible = true;
        }
        else
        {
            subMenu.isVisible = false;
        }
    }

    /// <summary>
    /// DrumsのAUTOパラメータを一覧表示するパネルを作成する
    /// </summary>
    private void MakeAutoPanel()
    {
        Bitmap image = new Bitmap(300, 130);
        Graphics graphics = Graphics.FromImage(image);

        string header = "", s = "";
        switch (nCurrentTarget)
        {
            case (int)EInstrumentPart.DRUMS:
                header = "LHSBHLFCPRB";
                break;
            case (int)EInstrumentPart.GUITAR:
            case (int)EInstrumentPart.BASS:
                header = "RGBYPPW";
                break;
            default:
                break;
        }
        s = GetAutoParameters(nCurrentTarget);
        for (int i = 0; i < header.Length; i++)
        {
            graphics.DrawString(header[i].ToString(), ft表示用フォント, Brushes.White, (float)i * 24, (float)0f);
            graphics.DrawString(s[i].ToString(), ft表示用フォント, Brushes.White, (float)i * 24, (float)24f);
        }
        graphics.Dispose();

        try
        {
            if (txAutoStatus != null)
            {
                txAutoStatus.Dispose();
            }
            txAutoStatus = new CTexture(CDTXMania.app.Device, image, CDTXMania.TextureFormat);
            txAutoStatus.vcScaleRatio = new Vector3(1f, 1f, 1f);
            image.Dispose();
        }
        catch (CTextureCreateFailedException)
        {
            Trace.TraceError("演奏履歴文字列テクスチャの作成に失敗しました。");
            txAutoStatus = null;
        }
            
        autoStatus.SetTexture(new DTXTexture(txAutoStatus));
    }

    public override void tPressEnterMain(int nSortOrder)
    {
        lci[nCurrentTarget][nCurrentSelection].RunAction();
    }

    public override void tCancel()
    {
        SetAutoParameters();
        // Autoの設定値保持のロジックを書くこと！
        // (Autoのパラメータ切り替え時は実際に値設定していないため、キャンセルまたはRetern, More...時に値設定する必要有り)
    }
    public override void tBDContinuity()
    {
        SetAutoParameters();
        // Autoの設定値保持のロジックを書くこと！
        // (Autoのパラメータ切り替え時は実際に値設定していないため、キャンセルまたはRetern, More...時に値設定する必要有り)
    }

    /// <summary>
    /// 1つの値を、全targetに適用する。RiskyやDarkなど、全tatgetで共通の設定となるもので使う。
    /// </summary>
    /// <param name="order">設定項目リストの順番</param>
    /// <param name="index">設定値(index)</param>
    private void SetValueToAllTarget(int order, int index)
    {
        for (int i = 0; i < 3; i++)
        {
            lci[i][order].SetIndex(index);
        }
    }


    /// <summary>
    /// ConfigIni.bAutoPlayに簡易CONFIGの状態を反映する
    /// </summary>
    private void SetAutoParameters()
    {
        for (int target = 0; target < 3; target++)
        {
            string str = GetAutoParameters(target);
            int[] pa = { (int)ELane.LC, (int)ELane.GtR, (int)ELane.BsR };
            int start = pa[target];

            for (int i = 0; i < str.Length; i++)
            {
                CDTXMania.ConfigIni.bAutoPlay[i + start] = (str[i] == 'A');
            }
        }
    }

    /// <summary>
    /// 簡易CONFIG内のAUTO状態を、文字列で返す。
    /// </summary>
    /// <param name="target">対象楽器</param>
    /// <returns>AutoならA,さもなくば_。この文字が複数並んだ文字列。</returns>
    private string GetAutoParameters(int target)
    {
        string s = "";
        switch (target)
        {
            #region [ DRUMS ]
            case (int)EInstrumentPart.DRUMS:
                switch (lci[target][(int)EOrder.AutoMode].GetIndex())
                {
                    //LHPSBHLFCR
                    case 0:	// All Auto
                        s = "AAAAAAAAAAA";
                        break;
                    case 1:	// Auto LP
                        s = "________A_A";
                        break;
                    case 2:	// Auto BD
                        s = "___A_______";
                        break;
                    case 3:	// 2Pedal Auto
                        s = "___A____A_A";
                        break;
                    case 4:	// 3 Auto
                        s = "A_____A_A_A";
                        break;
                    case 5:	// Custom
                        for (int i = 0; i < 11; i++)
                        {
                            s += (CDTXMania.ConfigIni.bAutoPlay[i]) ? "A" : "_";
                        }
                        break;
                    case 6:	// OFF
                        s = "___________";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                break;
            #endregion
            #region [ Guitar / Bass ]
            case (int)EInstrumentPart.GUITAR:
            case (int)EInstrumentPart.BASS:
                //					s = ( lci[ nCurrentConfigSet ][ target ][ (int) EOrder.AutoMode ].GetIndex() ) == 1 ? "A" : "_";
                switch (lci[target][(int)EOrder.AutoMode].GetIndex())
                {
                    case 0:	// All Auto
                        s = "AAAAAAA";
                        break;
                    case 1:	// Auto Neck
                        s = "AAAAA__";
                        break;
                    case 2:	// Auto Pick
                        s = "_____A_";
                        break;
                    case 3:	// Custom
                        int p = (target == (int)EInstrumentPart.GUITAR) ? (int)ELane.GtR : (int)ELane.BsR;
                        int len = (int)ELane.GtW - (int)ELane.GtR + 1;
                        for (int i = p; i < p + len; i++)
                        {
                            s += (CDTXMania.ConfigIni.bAutoPlay[i]) ? "A" : "_";
                        }
                        break;
                    case 4:	// OFF
                        s = "_______";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                break;
            default:
                break;
            #endregion
        }
        return s;
    }


    // CActivity 実装

    public override void OnActivate()
    {
        ft表示用フォント = new Font("Arial", 26f, FontStyle.Bold, GraphicsUnit.Pixel);
        base.OnActivate();
        bGotoDetailConfig = false;
    }
    public override void OnDeactivate()
    {
        if (ft表示用フォント != null)
        {
            ft表示用フォント.Dispose();
            ft表示用フォント = null;
        }
        base.OnDeactivate();
    }
    public override void OnManagedCreateResources()
    {
        if (!bNotActivated)
        {
            subMenu = new UIGroup("Quick Options Menu");
            subMenu.renderOrder = 100;
            subMenu.anchor = new Vector2(0.5f, 0.5f);

            var popupTex = new DTXTexture(CSkin.Path(@"Graphics\ScreenSelect popup auto settings.png"));
            var popup = new UIImage(popupTex);
            subMenu.AddChild(popup);
            subMenu.size = popup.size;
                
            var autoStatusTex = new DTXTexture(txAutoStatus);
            autoStatus = subMenu.AddChild(new UIImage(autoStatusTex));
            MakeAutoPanel();
                
            //ui gets created here, we can add the subMenu to the ui afterwards
            base.OnManagedCreateResources();
            
            ui.AddChild(subMenu);

            subMenu.position.X = ui.size.X / 2.0f;
            subMenu.position.Y = ui.size.Y / 2.0f;
        }
    }
    public override void OnManagedReleaseResources()
    {
        if (!bNotActivated)
        {
            CDTXMania.tReleaseTexture(ref txAutoStatus);
                
            base.OnManagedReleaseResources();
        }
    }

    #region [ private ]
    //-----------------
    private int nCurrentTarget = 0;
    private List<CItemBase>[] lci;		// DrGtBs, ConfSet, 選択肢一覧。都合、3次のListとなる。
    private enum EOrder : int
    {
        Target = 0,
        AutoMode,
        //	AutoLane,
        ScrollSpeed,
        Dark,
        Risky,
        PlaySpeed,
        SuddenHidden,
        AutoGhost,
        TargetGhost,
        //ConfSet,
        More,
        Return, END,
        Default = 99
    };

    private UIGroup subMenu;
    private UIImage autoStatus;
        
    private Font ft表示用フォント;
    private CTexture txAutoStatus;
    //-----------------
    #endregion
}