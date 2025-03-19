namespace DTXMania;

internal partial class CActConfigList
{
    #region [ t項目リストの設定_Guitar() ]
    private CItemInteger iGuitarScrollSpeed;
    private CItemToggle iGuitarGraph;
        
    public void tSetupItemList_Guitar()
    {
        tRecordToConfigIni();
        listItems.Clear();
            
        iGuitarReturnToMenu = new CItemBase("<< Return To Menu", CItemBase.EPanelType.Other,
            "左側のメニューに戻ります。",
            "Return to left menu.");
        listItems.Add(iGuitarReturnToMenu);

        CItemThreeState iGuitarAutoPlayAll = new("AutoPlay (All)", CItemThreeState.E状態.不定,
            "全ネック/ピックの自動演奏の ON/OFF を\n" +
            "まとめて切り替えます。",
            "You can change whether Auto or not\n" +
            " for all guitar neck/pick at once.");
        listItems.Add(iGuitarAutoPlayAll);

        CItemToggle iGuitarR = new("    R", CDTXMania.ConfigIni.bAutoPlay.GtR,
            "Rネックを自動で演奏します。",
            "To play R neck automatically.");
        iGuitarR.BindConfig(
            () => iGuitarR.bON = CDTXMania.ConfigIni.bAutoPlay.GtR,
            () => CDTXMania.ConfigIni.bAutoPlay.GtR = iGuitarR.bON);
        listItems.Add(iGuitarR);

        CItemToggle iGuitarG = new("    G", CDTXMania.ConfigIni.bAutoPlay.GtG,
            "Gネックを自動で演奏します。",
            "To play G neck automatically.");
        iGuitarG.BindConfig(
            () => iGuitarG.bON = CDTXMania.ConfigIni.bAutoPlay.GtG,
            () => CDTXMania.ConfigIni.bAutoPlay.GtG = iGuitarG.bON);
        listItems.Add(iGuitarG);

        CItemToggle iGuitarB = new("    B", CDTXMania.ConfigIni.bAutoPlay.GtB,
            "Bネックを自動で演奏します。",
            "To play B neck automatically.");
        iGuitarB.BindConfig(
            () => iGuitarB.bON = CDTXMania.ConfigIni.bAutoPlay.GtB,
            () => CDTXMania.ConfigIni.bAutoPlay.GtB = iGuitarB.bON);
        listItems.Add(iGuitarB);

        CItemToggle iGuitarY = new("    Y", CDTXMania.ConfigIni.bAutoPlay.GtY,
            "Yネックを自動で演奏します。",
            "To play Y neck automatically.");
        iGuitarY.BindConfig(
            () => iGuitarY.bON = CDTXMania.ConfigIni.bAutoPlay.GtY,
            () => CDTXMania.ConfigIni.bAutoPlay.GtY = iGuitarY.bON);
        listItems.Add(iGuitarY);

        CItemToggle iGuitarP = new("    P", CDTXMania.ConfigIni.bAutoPlay.GtP,
            "Pネックを自動で演奏します。",
            "To play P neck automatically.");
        iGuitarP.BindConfig(
            () => iGuitarP.bON = CDTXMania.ConfigIni.bAutoPlay.GtP,
            () => CDTXMania.ConfigIni.bAutoPlay.GtP = iGuitarP.bON);
        listItems.Add(iGuitarP);

        CItemToggle iGuitarPick = new("    Pick", CDTXMania.ConfigIni.bAutoPlay.GtPick,
            "ピックを自動で演奏します。",
            "To play Pick automatically.");
        iGuitarPick.BindConfig(
            () => iGuitarPick.bON = CDTXMania.ConfigIni.bAutoPlay.GtPick,
            () => CDTXMania.ConfigIni.bAutoPlay.GtPick = iGuitarPick.bON);
        listItems.Add(iGuitarPick);

        CItemToggle iGuitarW = new("    Wailing", CDTXMania.ConfigIni.bAutoPlay.GtW,
            "ウェイリングを自動で演奏します。",
            "To play wailing automatically.");
        iGuitarW.BindConfig(
            () => iGuitarW.bON = CDTXMania.ConfigIni.bAutoPlay.GtW,
            () => CDTXMania.ConfigIni.bAutoPlay.GtW = iGuitarW.bON);
        listItems.Add(iGuitarW);
            
        //add the action for this later, as it needs to be able to change all of the above buttons
        iGuitarAutoPlayAll.action = () =>
        {
            bool bAutoOn = iGuitarAutoPlayAll.e現在の状態 == CItemThreeState.E状態.ON;
                
            iGuitarR.bON = bAutoOn;
            iGuitarG.bON = bAutoOn;
            iGuitarB.bON = bAutoOn;
            iGuitarY.bON = bAutoOn;
            iGuitarP.bON = bAutoOn;
            iGuitarPick.bON = bAutoOn;
            iGuitarW.bON = bAutoOn;
        };

        iGuitarScrollSpeed = new CItemInteger("ScrollSpeed", 0, 0x7cf, CDTXMania.ConfigIni.nScrollSpeed.Guitar,
            "演奏時のギター譜面のスクロールの\n速度を指定します。\nx0.5 ～ x1000.0 までを指定可能です。",
            "To change the scroll speed for the\nguitar lanes.\nYou can set it from x0.5 to x1000.0.\n(ScrollSpeed=x0.5 means half speed)");
        iGuitarScrollSpeed.BindConfig(
            () => iGuitarScrollSpeed.nCurrentValue = CDTXMania.ConfigIni.nScrollSpeed.Guitar,
            () => CDTXMania.ConfigIni.nScrollSpeed.Guitar = iGuitarScrollSpeed.nCurrentValue);
            
        listItems.Add(iGuitarScrollSpeed);

        CItemList iGuitarHIDSUD = new("HID-SUD", CItemBase.EPanelType.Normal, (int)CDTXMania.ConfigIni.nHidSud.Guitar,
            "HIDDEN:チップが途中から見えなくなります。\n" +
            "SUDDEN:チップが途中まで見えません。\n" +
            "HID-SUD:HIDDEN、SUDDENの両方が適用\n" +
            "されます。\n" +
            "STEALTH:チップがずっと表示されません。",
            "The display position for Drums Combo.\n" +
            "Note that it doesn't take effect\n" +
            " at Autoplay ([Left] is forcely used).",
            new string[] { "OFF", "Hidden", "Sudden", "HidSud", "Stealth" });
        iGuitarHIDSUD.BindConfig(
            () => iGuitarHIDSUD.n現在選択されている項目番号 = CDTXMania.ConfigIni.nHidSud.Guitar,
            () => CDTXMania.ConfigIni.nHidSud.Guitar = iGuitarHIDSUD.n現在選択されている項目番号);
        listItems.Add(iGuitarHIDSUD);
            
        //----------DisplayOption----------
        CItemList iGuitarDark = new("       Dark", CItemBase.EPanelType.Normal, (int)CDTXMania.ConfigIni.eDark,
            "レーン表示のオプションをまとめて切り替えます。\n" +
            "HALF: レーンが表示されなくなります。\n" +
            "FULL: さらに小節線、拍線、判定ラインも\n" +
            "表示されなくなります。",
            "OFF: all display parts are shown.\nHALF: lanes and gauge are\n disappeared.\nFULL: additionaly to HALF, bar/beat\n lines, hit bar are disappeared.",
            new string[] { "OFF", "HALF", "FULL" });
        listItems.Add(iGuitarDark);
            
        CItemList iGuitarLaneDisp = new("LaneDisp", CItemBase.EPanelType.Normal, (int)CDTXMania.ConfigIni.nLaneDisp.Guitar,
            "レーンの縦線と小節線の表示を切り替えます。\n" +
            "ALL  ON :レーン背景、小節線を表示します。\n" +
            "LANE OFF:レーン背景を表示しません。\n" +
            "LINE OFF:小節線を表示しません。\n" +
            "ALL  OFF:レーン背景、小節線を表示しません。",
            "",
            new string[] { "ALL ON", "LANE OFF", "LINE OFF", "ALL OFF" });
        iGuitarLaneDisp.BindConfig(
            () => iGuitarLaneDisp.n現在選択されている項目番号 = CDTXMania.ConfigIni.nLaneDisp.Guitar,
            () => CDTXMania.ConfigIni.nLaneDisp.Guitar = iGuitarLaneDisp.n現在選択されている項目番号);
        listItems.Add(iGuitarLaneDisp);

        CItemToggle iGuitarJudgeLineDisp = new("JudgeLineDisp", CDTXMania.ConfigIni.bJudgeLineDisp.Guitar,
            "判定ラインの表示 / 非表示を切り替えます。",
            "Toggle JudgeLine");
        iGuitarJudgeLineDisp.BindConfig(
            () => iGuitarJudgeLineDisp.bON = CDTXMania.ConfigIni.bJudgeLineDisp.Guitar,
            () => CDTXMania.ConfigIni.bJudgeLineDisp.Guitar = iGuitarJudgeLineDisp.bON);
        listItems.Add(iGuitarJudgeLineDisp);

        CItemToggle iGuitarLaneFlush = new("LaneFlush", CDTXMania.ConfigIni.bLaneFlush.Guitar,
            "レーンフラッシュの表示の表示 / 非表示を\n" +
            "切り替えます。",
            "Toggle LaneFlush");
        iGuitarLaneFlush.BindConfig(
            () => iGuitarLaneFlush.bON = CDTXMania.ConfigIni.bLaneFlush.Guitar,
            () => CDTXMania.ConfigIni.bLaneFlush.Guitar = iGuitarLaneFlush.bON);
        listItems.Add(iGuitarLaneFlush);
            
        //add the action for this later, as it needs to be able to change all of the above buttons
        iGuitarDark.action = () =>
        {
            if (iGuitarDark.n現在選択されている項目番号 == (int)EDarkMode.FULL)
            {
                iGuitarLaneDisp.n現在選択されている項目番号 = 3;
                iGuitarJudgeLineDisp.bON = false;
                iGuitarLaneFlush.bON = false;
            }
            else if (iGuitarDark.n現在選択されている項目番号 == (int)EDarkMode.HALF)
            {
                iGuitarLaneDisp.n現在選択されている項目番号 = 1;
                iGuitarJudgeLineDisp.bON = true;
                iGuitarLaneFlush.bON = true;
            }
            else
            {
                iGuitarLaneDisp.n現在選択されている項目番号 = 0;
                iGuitarJudgeLineDisp.bON = true;
                iGuitarLaneFlush.bON = true;
            }
        };

        CItemList iGuitarAttackEffect = new("AttackEffect", CItemBase.EPanelType.Normal, (int)CDTXMania.ConfigIni.eAttackEffect.Guitar,
            "アタックエフェクトの表示 / 非表示を\n" +
            "切り替えます。",
            "",
            new string[] { "ON", "OFF" });
        iGuitarAttackEffect.BindConfig(
            () => iGuitarAttackEffect.n現在選択されている項目番号 = (int)CDTXMania.ConfigIni.eAttackEffect.Guitar,
            () => CDTXMania.ConfigIni.eAttackEffect.Guitar = (EType)iGuitarAttackEffect.n現在選択されている項目番号);
        listItems.Add(iGuitarAttackEffect);

        CItemToggle iGuitarReverse = new("Reverse", CDTXMania.ConfigIni.bReverse.Guitar,
            "ギターチップが譜面の上から下に\n流れるようになります。",
            "The scroll way is reversed. Guitar chips\nflow from the top to the bottom.");
        iGuitarReverse.BindConfig(
            () => iGuitarReverse.bON = CDTXMania.ConfigIni.bReverse.Guitar,
            () => CDTXMania.ConfigIni.bReverse.Guitar = iGuitarReverse.bON);
        listItems.Add(iGuitarReverse);

        //コンボ表示

        //RISKY

        CItemList iGuitarPosition = new("Position", CItemBase.EPanelType.Normal, (int)CDTXMania.ConfigIni.JudgementStringPosition.Guitar,
            "ギターの判定文字の表示位置を指定\nします。\n  P-A: OnTheLane\n  P-B: COMBO の下\n  P-C: 判定ライン上\n  OFF: 表示しない",
            "The position to show judgement mark.\n(Perfect, Great, ...)\n\n P-A: on the lanes.\n P-B: under the COMBO indication.\n P-C: on the JudgeLine.\n OFF: no judgement mark.",
            new string[] { "P-A", "P-B", "P-C", "OFF" });
        iGuitarPosition.BindConfig(
            () => iGuitarPosition.n現在選択されている項目番号 = (int)CDTXMania.ConfigIni.JudgementStringPosition.Guitar,
            () => CDTXMania.ConfigIni.JudgementStringPosition.Guitar = (EType)iGuitarPosition.n現在選択されている項目番号);
        listItems.Add(iGuitarPosition);

        //実機ではここにオートオプションが入る。

        CItemToggle iGuitarLight = new("Light", CDTXMania.ConfigIni.bLight.Guitar,
            "ギターチップのないところでピッキングしても\n BAD になりません。",
            "Even if you pick without any chips,\nit doesn't become BAD.");
        iGuitarLight.BindConfig(
            () => iGuitarLight.bON = CDTXMania.ConfigIni.bLight.Guitar,
            () => CDTXMania.ConfigIni.bLight.Guitar = iGuitarLight.bON);
        listItems.Add(iGuitarLight);

        CItemList iGuitarSpecialist = new("Performance Mode", CItemBase.EPanelType.Normal, CDTXMania.ConfigIni.bSpecialist.Guitar ? 1 : 0,
            "ギターの演奏・モード\nします。\n  Normal: 通常の演奏モードです\n  Specialist: 間違えると違う音が流れます",
            "Turn on/off Specialist Mode for Guitar\n Normal: Default Performance mode\n Specialist: Different sound is played when you make a mistake",
            new string[] { "Normal", "Specialist" });
        iGuitarSpecialist.BindConfig(
            () => iGuitarSpecialist.n現在選択されている項目番号 = CDTXMania.ConfigIni.bSpecialist.Guitar ? 1 : 0,
            () => CDTXMania.ConfigIni.bSpecialist.Guitar = iGuitarSpecialist.n現在選択されている項目番号 == 1);
        listItems.Add(iGuitarSpecialist);

        CItemList iGuitarRandom = new("Random", CItemBase.EPanelType.Normal, (int)CDTXMania.ConfigIni.eRandom.Guitar,
            "ギターのチップがランダムに降ってきます。\n  Mirror: ミラーをかけます\n  Part: 小節_レーン単位で交換\n  Super: チップ単位で交換\n  Hyper: 全部完全に変更",
            "Guitar chips come randomly.\n Mirror: \n Part: swapping lanes randomly for each\n  measures.\n Super: swapping chip randomly\n Hyper: swapping randomly\n  (number of lanes also changes)",
            new string[] { "OFF", "Mirror", "Part", "Super", "Hyper" });
        iGuitarRandom.BindConfig(
            () => iGuitarRandom.n現在選択されている項目番号 = (int)CDTXMania.ConfigIni.eRandom.Guitar,
            () => CDTXMania.ConfigIni.eRandom.Guitar = (ERandomMode)iGuitarRandom.n現在選択されている項目番号);
        listItems.Add(iGuitarRandom);

        //NumOfLanes(ここではレーンオ－トに相当する。)
        //バイブオプション(実装不可)
        //StageEffect

        CItemToggle iGuitarLeft = new("Left", CDTXMania.ConfigIni.bLeft.Guitar,
            "ギターの RGBYP の並びが左右反転します。\n（左利きモード）",
            "Lane order 'R-G-B-Y-P' becomes 'P-Y-B-G-R'\nfor lefty.");
        iGuitarLeft.BindConfig(
            () => iGuitarLeft.bON = CDTXMania.ConfigIni.bLeft.Guitar,
            () => CDTXMania.ConfigIni.bLeft.Guitar = iGuitarLeft.bON);
        listItems.Add(iGuitarLeft);

        CItemInteger iGuitarJudgeLinePos = new("JudgeLinePos", 0, 100, CDTXMania.ConfigIni.nJudgeLine.Guitar,
            "演奏時の判定ラインの高さを変更します。\n" +
            "0～100の間で指定できます。",
            "To change the judgeLinePosition for the\n" +
            "You can set it from 0 to 100.");
        iGuitarJudgeLinePos.BindConfig(
            () => iGuitarJudgeLinePos.nCurrentValue = CDTXMania.ConfigIni.nJudgeLine.Guitar,
            () => CDTXMania.ConfigIni.nJudgeLine.Guitar = iGuitarJudgeLinePos.nCurrentValue);
        listItems.Add(iGuitarJudgeLinePos);

        //比較対象(そもそも比較グラフさえ完成していない)
        //シャッタータイプ
        CItemInteger iGuitarShutterInPos = new("ShutterInPos", 0, 100, CDTXMania.ConfigIni.nShutterInSide.Guitar,
            "演奏時のノーツが現れる側のシャッターの\n" +
            "位置を変更します。",
            "\n" +
            "\n" +
            "");
        iGuitarShutterInPos.BindConfig(
            () => iGuitarShutterInPos.nCurrentValue = CDTXMania.ConfigIni.nShutterInSide.Guitar,
            () => CDTXMania.ConfigIni.nShutterInSide.Guitar = iGuitarShutterInPos.nCurrentValue);
        listItems.Add(iGuitarShutterInPos);

        CItemInteger iGuitarShutterOutPos = new("ShutterOutPos", 0, 100, CDTXMania.ConfigIni.nShutterOutSide.Guitar,
            "演奏時のノーツが消える側のシャッターの\n" +
            "位置を変更します。",
            "\n" +
            "\n" +
            "");
        iGuitarShutterOutPos.BindConfig(
            () => iGuitarShutterOutPos.nCurrentValue = CDTXMania.ConfigIni.nShutterOutSide.Guitar,
            () => CDTXMania.ConfigIni.nShutterOutSide.Guitar = iGuitarShutterOutPos.nCurrentValue);
        listItems.Add(iGuitarShutterOutPos);
            
        CItemToggle iSystemSoundMonitorGuitar = new("GuitarMonitor", CDTXMania.ConfigIni.b演奏音を強調する.Guitar,
            "ギター音モニタ：\nギター音を他の音より大きめの音量で\n発声します。\nただし、オートプレイの場合は通常音量で\n発声されます。",
            "To enhance the guitar chip sound\n(except autoplay).");
        iSystemSoundMonitorGuitar.BindConfig(
            () => iSystemSoundMonitorGuitar.bON = CDTXMania.ConfigIni.b演奏音を強調する.Guitar,
            () => CDTXMania.ConfigIni.b演奏音を強調する.Guitar = iSystemSoundMonitorGuitar.bON);
        listItems.Add(iSystemSoundMonitorGuitar);

        CItemInteger iSystemMinComboGuitar = new("G-MinCombo", 0, 0x1869f, CDTXMania.ConfigIni.n表示可能な最小コンボ数.Guitar,
            "表示可能な最小コンボ数（ギター）：\n画面に表示されるコンボの最小の数を\n指定します。\n1 ～ 99999 の値が指定可能です。\n0にするとコンボを表示しません。",
            "Initial number to show the combo\n for the guitar.\nYou can specify from 1 to 99999.");
        iSystemMinComboGuitar.BindConfig(
            () => iSystemMinComboGuitar.nCurrentValue = CDTXMania.ConfigIni.n表示可能な最小コンボ数.Guitar,
            () => CDTXMania.ConfigIni.n表示可能な最小コンボ数.Guitar = iSystemMinComboGuitar.nCurrentValue);
        listItems.Add(iSystemMinComboGuitar);

        iGuitarGraph = new CItemToggle( "Graph", CDTXMania.ConfigIni.bGraph有効.Guitar,
            "最高スキルと比較できるグラフを表示します。\n" +
            "オートプレイだと表示されません。\n" +
            "この項目を有効にすると、ベースパートのグラフは\n" +
            "無効になります。",
            "To draw Graph or not." )
        {
            action = () =>
            {
                if (iGuitarGraph.bON)
                {
                    CDTXMania.ConfigIni.bGraph有効.Bass = false;
                    iBassGraph.bON = false;
                }
            }
        };
        iGuitarGraph.BindConfig(
            () => iGuitarGraph.bON = CDTXMania.ConfigIni.bGraph有効.Guitar,
            () => CDTXMania.ConfigIni.bGraph有効.Guitar = iGuitarGraph.bON);
        listItems.Add(iGuitarGraph);

        // #23580 2011.1.3 yyagi
        CItemInteger iGuitarInputAdjustTimeMs = new("InputAdjust", -99, 99, CDTXMania.ConfigIni.nInputAdjustTimeMs.Guitar,
            "ギターの入力タイミングの微調整を行います。\n-99 ～ 99ms まで指定可能です。\n入力ラグを軽減するためには、\n負の値を指定してください。",
            "To adjust the guitar input timing.\nYou can set from -99 to 0ms.\nTo decrease input lag, set minus value.");
        iGuitarInputAdjustTimeMs.BindConfig(
            () => iGuitarInputAdjustTimeMs.nCurrentValue = CDTXMania.ConfigIni.nInputAdjustTimeMs.Guitar,
            () => CDTXMania.ConfigIni.nInputAdjustTimeMs.Guitar = iGuitarInputAdjustTimeMs.nCurrentValue);
        listItems.Add(iGuitarInputAdjustTimeMs);

        CItemBase iGuitarGoToKeyAssign = new("Guitar Keys", CItemBase.EPanelType.Normal,
            "ギターのキー入力に関する項目を設定します。",
            "Settings for the guitar key/pad inputs.")
        {
            action = tSetupItemList_KeyAssignGuitar
        };
        listItems.Add(iGuitarGoToKeyAssign);

        OnListMenuの初期化();
        nCurrentSelection = 0;
        eMenuType = EMenuType.Guitar;
    }
        
    public void tSetupItemList_KeyAssignGuitar()
    { 
        listItems.Clear();
            
        CItemBase iKeyAssignGuitarReturnToMenu = new("<< ReturnTo Menu", CItemBase.EPanelType.Other,
            "左側のメニューに戻ります。",
            "Return to left menu.")
        {
            action = tSetupItemList_Guitar
        };
        listItems.Add(iKeyAssignGuitarReturnToMenu);

        CItemBase iKeyAssignGuitarR = new("R",
            "ギターのキー設定：\nRボタンへのキーの割り当てを設定し\nます。",
            "Guitar key assign:\nTo assign key/pads for R button.")
        {
            action = () => CDTXMania.stageConfig.tNotifyPadSelection(EKeyConfigPart.GUITAR, EKeyConfigPad.R)
        };
        listItems.Add(iKeyAssignGuitarR);

        CItemBase iKeyAssignGuitarG = new("G",
            "ギターのキー設定：\nGボタンへのキーの割り当てを設定し\nます。",
            "Guitar key assign:\nTo assign key/pads for G button.")
        {
            action = () => CDTXMania.stageConfig.tNotifyPadSelection(EKeyConfigPart.GUITAR, EKeyConfigPad.G)
        };
        listItems.Add(iKeyAssignGuitarG);

        CItemBase iKeyAssignGuitarB = new("B",
            "ギターのキー設定：\nBボタンへのキーの割り当てを設定し\nます。",
            "Guitar key assign:\nTo assign key/pads for B button.")
        {
            action = () => CDTXMania.stageConfig.tNotifyPadSelection(EKeyConfigPart.GUITAR, EKeyConfigPad.B)
        };
        listItems.Add(iKeyAssignGuitarB);

        CItemBase iKeyAssignGuitarY = new("Y",
            "ギターのキー設定：\nYボタンへのキーの割り当てを設定し\nます。",
            "Guitar key assign:\nTo assign key/pads for Y button.")
        {
            action = () => CDTXMania.stageConfig.tNotifyPadSelection(EKeyConfigPart.GUITAR, EKeyConfigPad.Y)
        };
        listItems.Add(iKeyAssignGuitarY);

        CItemBase iKeyAssignGuitarP = new("P",
            "ギターのキー設定：\nPボタンへのキーの割り当てを設定し\nます。",
            "Guitar key assign:\nTo assign key/pads for P button.")
        {
            action = () => CDTXMania.stageConfig.tNotifyPadSelection(EKeyConfigPart.GUITAR, EKeyConfigPad.P)
        };
        listItems.Add(iKeyAssignGuitarP);

        CItemBase iKeyAssignGuitarPick = new("Pick",
            "ギターのキー設定：\nピックボタンへのキーの割り当てを設\n定します。",
            "Guitar key assign:\nTo assign key/pads for Pick button.")
        {
            action = () => CDTXMania.stageConfig.tNotifyPadSelection(EKeyConfigPart.GUITAR, EKeyConfigPad.Pick)
        };
        listItems.Add(iKeyAssignGuitarPick);

        CItemBase iKeyAssignGuitarWail = new("Wailing",
            "ギターのキー設定：\nWailingボタンへのキーの割り当てを\n設定します。",
            "Guitar key assign:\nTo assign key/pads for Wailing button.")
        {
            action = () => CDTXMania.stageConfig.tNotifyPadSelection(EKeyConfigPart.GUITAR, EKeyConfigPad.Wail)
        };
        listItems.Add(iKeyAssignGuitarWail);

        CItemBase iKeyAssignGuitarDecide = new("Decide",
            "ギターのキー設定：\n決定ボタンへのキーの割り当てを設\n定します。",
            "Guitar key assign:\nTo assign key/pads for Decide button.")
        {
            action = () => CDTXMania.stageConfig.tNotifyPadSelection(EKeyConfigPart.GUITAR, EKeyConfigPad.Decide)
        };
        listItems.Add(iKeyAssignGuitarDecide);

        CItemBase iKeyAssignGuitarCancel = new("Cancel",
            "ギターのキー設定：\n取消ボタンへのキーの割り当てを設\n定します。",
            "Guitar key assign:\nTo assign key/pads for Cancel button.")
        {
            action = () => CDTXMania.stageConfig.tNotifyPadSelection(EKeyConfigPart.GUITAR, EKeyConfigPad.Cancel)
        };
        listItems.Add(iKeyAssignGuitarCancel);

        OnListMenuの初期化();
        nCurrentSelection = 0;
        eMenuType = EMenuType.KeyAssignGuitar;
    }
    #endregion
}