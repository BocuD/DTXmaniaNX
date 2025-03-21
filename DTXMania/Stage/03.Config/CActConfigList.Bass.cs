﻿using DTXMania.Core;
using DTXMania.UI.Item;

namespace DTXMania;

internal partial class CActConfigList
{
    #region [ t項目リストの設定_Bass() ]
    private CItemInteger iBassScrollSpeed;
    private CItemToggle iBassGraph;
        
    public void tSetupItemList_Bass()
    {
        tRecordToConfigIni();
        listItems.Clear();
            
        iBassReturnToMenu = new CItemBase("<< Return To Menu", CItemBase.EPanelType.Other,
            "左側のメニューに戻ります。",
            "Return to left menu.");
        listItems.Add(iBassReturnToMenu);

        CItemThreeState? iBassAutoPlayAll = new("AutoPlay (All)", CItemThreeState.E状態.不定,
            "全ネック/ピックの自動演奏の ON/OFF を\n" +
            "まとめて切り替えます。",
            "You can change whether Auto or not\n" +
            " for all bass neck/pick at once.");
        listItems.Add(iBassAutoPlayAll);
            
        CItemToggle? iBassR = new("    R", CDTXMania.ConfigIni.bAutoPlay.BsR,
            "Rネックを自動で演奏します。",
            "To play R neck automatically.");
        iBassR.BindConfig(
            () => iBassR.bON = CDTXMania.ConfigIni.bAutoPlay.BsR,
            () => CDTXMania.ConfigIni.bAutoPlay.BsR = iBassR.bON);
        listItems.Add(iBassR);

        CItemToggle? iBassG = new("    G", CDTXMania.ConfigIni.bAutoPlay.BsG,
            "Gネックを自動で演奏します。",
            "To play G neck automatically.");
        iBassG.BindConfig(
            () => iBassG.bON = CDTXMania.ConfigIni.bAutoPlay.BsG,
            () => CDTXMania.ConfigIni.bAutoPlay.BsG = iBassG.bON);
        listItems.Add(iBassG);

        CItemToggle? iBassB = new("    B", CDTXMania.ConfigIni.bAutoPlay.BsB,
            "Bネックを自動で演奏します。",
            "To play B neck automatically.");
        iBassB.BindConfig(
            () => iBassB.bON = CDTXMania.ConfigIni.bAutoPlay.BsB,
            () => CDTXMania.ConfigIni.bAutoPlay.BsB = iBassB.bON);
        listItems.Add(iBassB);

        CItemToggle? iBassY = new("    Y", CDTXMania.ConfigIni.bAutoPlay.BsY,
            "Yネックを自動で演奏します。",
            "To play Y neck automatically.");
        iBassY.BindConfig(
            () => iBassY.bON = CDTXMania.ConfigIni.bAutoPlay.BsY,
            () => CDTXMania.ConfigIni.bAutoPlay.BsY = iBassY.bON);
        listItems.Add(iBassY);

        CItemToggle? iBassP = new("    P", CDTXMania.ConfigIni.bAutoPlay.BsP,
            "Pネックを自動で演奏します。",
            "To play P neck automatically.");
        iBassP.BindConfig(
            () => iBassP.bON = CDTXMania.ConfigIni.bAutoPlay.BsP,
            () => CDTXMania.ConfigIni.bAutoPlay.BsP = iBassP.bON);
        listItems.Add(iBassP);

        CItemToggle? iBassPick = new("    Pick", CDTXMania.ConfigIni.bAutoPlay.BsPick,
            "ピックを自動で演奏します。",
            "To play Pick automatically.");
        iBassPick.BindConfig(
            () => iBassPick.bON = CDTXMania.ConfigIni.bAutoPlay.BsPick,
            () => CDTXMania.ConfigIni.bAutoPlay.BsPick = iBassPick.bON);
        listItems.Add(iBassPick);

        CItemToggle? iBassW = new("    Wailing", CDTXMania.ConfigIni.bAutoPlay.BsW,
            "ウェイリングを自動で演奏します。",
            "To play wailing automatically.");
        iBassW.BindConfig(
            () => iBassW.bON = CDTXMania.ConfigIni.bAutoPlay.BsW,
            () => CDTXMania.ConfigIni.bAutoPlay.BsW = iBassW.bON);
        listItems.Add(iBassW);
            
        //add the action for this later, as it needs to be able to change all of the above buttons
        iBassAutoPlayAll.action = () =>
        {
            bool bAutoOn = iBassAutoPlayAll.e現在の状態 == CItemThreeState.E状態.ON;

            iBassR.bON = bAutoOn;
            iBassG.bON = bAutoOn;
            iBassB.bON = bAutoOn;
            iBassY.bON = bAutoOn;
            iBassP.bON = bAutoOn;
            iBassPick.bON = bAutoOn;
            iBassW.bON = bAutoOn;
        };

        iBassScrollSpeed = new CItemInteger("ScrollSpeed", 0, 0x7cf, CDTXMania.ConfigIni.nScrollSpeed.Bass,
            "演奏時のベース譜面のスクロールの\n速度を指定します。\nx0.5 ～ x1000.0 までを指定可能です。",
            "To change the scroll speed for the\nbass lanes.\nYou can set it from x0.5 to x1000.0.\n(ScrollSpeed=x0.5 means half speed)");
        iBassScrollSpeed.BindConfig(
            () => iBassScrollSpeed.nCurrentValue = CDTXMania.ConfigIni.nScrollSpeed.Bass,
            () => CDTXMania.ConfigIni.nScrollSpeed.Bass = iBassScrollSpeed.nCurrentValue);
        listItems.Add(iBassScrollSpeed);

        CItemList? iBassHIDSUD = new("HID-SUD", CItemBase.EPanelType.Normal, (int)CDTXMania.ConfigIni.nHidSud.Bass,
            "HIDDEN:チップが途中から見えなくなります。\n" +
            "SUDDEN:チップが途中まで見えません。\n" +
            "HID-SUD:HIDDEN、SUDDENの両方が適用\n" +
            "されます。\n" +
            "STEALTH:チップがずっと表示されません。",
            "The display position for Drums Combo.\n" +
            "Note that it doesn't take effect\n" +
            " at Autoplay ([Left] is forcely used).",
            new string[] { "OFF", "Hidden", "Sudden", "HidSud", "Stealth" });
        iBassHIDSUD.BindConfig(
            () => iBassHIDSUD.n現在選択されている項目番号 = CDTXMania.ConfigIni.nHidSud.Bass,
            () => CDTXMania.ConfigIni.nHidSud.Bass = iBassHIDSUD.n現在選択されている項目番号);
        listItems.Add(iBassHIDSUD);

        //----------DisplayOption----------
        CItemList? iBassDark = new("       Dark", CItemBase.EPanelType.Normal, (int)CDTXMania.ConfigIni.eDark,
            "レーン表示のオプションをまとめて切り替えます。\n" +
            "HALF: レーンが表示されなくなります。\n" +
            "FULL: さらに小節線、拍線、判定ラインも\n" +
            "表示されなくなります。",
            "OFF: all display parts are shown.\nHALF: lanes and gauge are\n disappeared.\nFULL: additionaly to HALF, bar/beat\n lines, hit bar are disappeared.",
            new string[] { "OFF", "HALF", "FULL" });
        listItems.Add(iBassDark);
            
        CItemList? iBassLaneDisp = new("LaneDisp", CItemBase.EPanelType.Normal, (int)CDTXMania.ConfigIni.nLaneDisp.Bass,
            "レーンの縦線と小節線の表示を切り替えます。\n" +
            "ALL  ON :レーン背景、小節線を表示します。\n" +
            "LANE OFF:レーン背景を表示しません。\n" +
            "LINE OFF:小節線を表示しません。\n" +
            "ALL  OFF:レーン背景、小節線を表示しません。",
            "",
            new string[] { "ALL ON", "LANE OFF", "LINE OFF", "ALL OFF" });
        iBassLaneDisp.BindConfig(
            () => iBassLaneDisp.n現在選択されている項目番号 = CDTXMania.ConfigIni.nLaneDisp.Bass,
            () => CDTXMania.ConfigIni.nLaneDisp.Bass = iBassLaneDisp.n現在選択されている項目番号);
        listItems.Add(iBassLaneDisp);

        CItemToggle? iBassJudgeLineDisp = new("JudgeLineDisp", CDTXMania.ConfigIni.bJudgeLineDisp.Bass,
            "判定ラインの表示 / 非表示を切り替えます。",
            "Toggle JudgeLine");
        iBassJudgeLineDisp.BindConfig(
            () => iBassJudgeLineDisp.bON = CDTXMania.ConfigIni.bJudgeLineDisp.Bass,
            () => CDTXMania.ConfigIni.bJudgeLineDisp.Bass = iBassJudgeLineDisp.bON);
        listItems.Add(iBassJudgeLineDisp);

        CItemToggle? iBassLaneFlush = new("LaneFlush", CDTXMania.ConfigIni.bLaneFlush.Bass,
            "レーンフラッシュの表示 / 非表示を\n" +
            "切り替えます。",
            "Toggle LaneFlush");
        iBassLaneFlush.BindConfig(
            () => iBassLaneFlush.bON = CDTXMania.ConfigIni.bLaneFlush.Bass,
            () => CDTXMania.ConfigIni.bLaneFlush.Bass = iBassLaneFlush.bON);
        listItems.Add(iBassLaneFlush);
            
        //add the action for this later, as it needs to be able to change all of the above buttons
        iBassDark.action = () =>
        {
            if (iBassDark.n現在選択されている項目番号 == (int)EDarkMode.FULL)
            {
                iBassLaneDisp.n現在選択されている項目番号 = 3;
                iBassJudgeLineDisp.bON = false;
                iBassLaneFlush.bON = false;
            }
            else if (iBassDark.n現在選択されている項目番号 == (int)EDarkMode.HALF)
            {
                iBassLaneDisp.n現在選択されている項目番号 = 1;
                iBassJudgeLineDisp.bON = true;
                iBassLaneFlush.bON = true;
            }
            else
            {
                iBassLaneDisp.n現在選択されている項目番号 = 0;
                iBassJudgeLineDisp.bON = true;
                iBassLaneFlush.bON = true;
            }
        };

        CItemList? iBassAttackEffect = new("AttackEffect", CItemBase.EPanelType.Normal, (int)CDTXMania.ConfigIni.eAttackEffect.Bass,
            "アタックエフェクトの表示 / 非表示を\n" +
            "切り替えます。",
            "",
            new string[] { "ON", "OFF" });
        iBassAttackEffect.BindConfig(
            () => iBassAttackEffect.n現在選択されている項目番号 = (int)CDTXMania.ConfigIni.eAttackEffect.Bass,
            () => CDTXMania.ConfigIni.eAttackEffect.Bass = (EType)iBassAttackEffect.n現在選択されている項目番号);
        listItems.Add(iBassAttackEffect);


        CItemToggle? iBassReverse = new("Reverse", CDTXMania.ConfigIni.bReverse.Bass,
            "ベースチップが譜面の上から下に\n流れるようになります。",
            "The scroll way is reversed. Bass chips\nflow from the top to the bottom.");
        iBassReverse.BindConfig(
            () => iBassReverse.bON = CDTXMania.ConfigIni.bReverse.Bass,
            () => CDTXMania.ConfigIni.bReverse.Bass = iBassReverse.bON);
        listItems.Add(iBassReverse);

        CItemList? iBassPosition = new("Position", CItemBase.EPanelType.Normal,
            (int)CDTXMania.ConfigIni.JudgementStringPosition.Bass,
            "ベースの判定文字の表示位置を指定します。\n  P-A: OnTheLane\n  P-B: COMBO の下\n  P-C: 判定ライン上\n  OFF: 表示しない",
            "The position to show judgement mark.\n(Perfect, Great, ...)\n\n P-A: on the lanes.\n P-B: under the COMBO indication.\n P-C: on the JudgeLine.\n OFF: no judgement mark.",
            new string[] { "P-A", "P-B", "P-C", "OFF" });
        iBassPosition.BindConfig(
            () => iBassPosition.n現在選択されている項目番号 = (int)CDTXMania.ConfigIni.JudgementStringPosition.Bass,
            () => CDTXMania.ConfigIni.JudgementStringPosition.Bass = (EType)iBassPosition.n現在選択されている項目番号);
        listItems.Add(iBassPosition);

        CItemList? iBassRandom = new("Random", CItemBase.EPanelType.Normal,
            (int)CDTXMania.ConfigIni.eRandom.Bass,
            "ベースのチップがランダムに降ってきます。\n  Mirror: ミラーをかけます\n  Part: 小節_レーン単位で交換\n  Super: チップ単位で交換\n  Hyper: 全部完全に変更",
            "Bass chips come randomly.\n Mirror: \n Part: swapping lanes randomly for each\n  measures.\n Super: swapping chip randomly\n Hyper: swapping randomly\n  (number of lanes also changes)",
            new string[] { "OFF", "Mirror", "Part", "Super", "Hyper" });
        iBassRandom.BindConfig(
            () => iBassRandom.n現在選択されている項目番号 = (int)CDTXMania.ConfigIni.eRandom.Bass,
            () => CDTXMania.ConfigIni.eRandom.Bass = (ERandomMode)iBassRandom.n現在選択されている項目番号);
        listItems.Add(iBassRandom);

        CItemToggle? iBassLight = new("Light", CDTXMania.ConfigIni.bLight.Bass,
            "ベースチップのないところでピッキングしても\n BAD になりません。",
            "Even if you pick without any chips,\nit doesn't become BAD.");
        iBassLight.BindConfig(
            () => iBassLight.bON = CDTXMania.ConfigIni.bLight.Bass,
            () => CDTXMania.ConfigIni.bLight.Bass = iBassLight.bON);
        listItems.Add(iBassLight);

        CItemList? iBassSpecialist = new("Performance Mode", CItemBase.EPanelType.Normal, CDTXMania.ConfigIni.bSpecialist.Bass ? 1 : 0,
            "ベースの演奏・モード\nします。\n  Normal: 通常の演奏モードです\n  Specialist: 間違えると違う音が流れます",
            "Turn on/off Specialist Mode for Bass\n Normal: Default Performance mode\n Specialist: Different sound is played when you make a mistake",
            new string[] { "Normal", "Specialist" });
        iBassSpecialist.BindConfig(
            () => iBassSpecialist.n現在選択されている項目番号 = CDTXMania.ConfigIni.bSpecialist.Bass ? 1 : 0,
            () => CDTXMania.ConfigIni.bSpecialist.Bass = iBassSpecialist.n現在選択されている項目番号 == 1);
        listItems.Add(iBassSpecialist);

        CItemToggle? iBassLeft = new("Left", CDTXMania.ConfigIni.bLeft.Bass,
            "ベースの RGBYP の並びが左右反転します。\n（左利きモード）",
            "Lane order 'R-G-B-Y-P' becomes 'P-Y-B-G-R'\nfor lefty.");
        iBassLeft.BindConfig(
            () => iBassLeft.bON = CDTXMania.ConfigIni.bLeft.Bass,
            () => CDTXMania.ConfigIni.bLeft.Bass = iBassLeft.bON);
        listItems.Add(iBassLeft);

        CItemToggle? iSystemSoundMonitorBass = new("BassMonitor", CDTXMania.ConfigIni.b演奏音を強調する.Bass,
            "ベース音モニタ：\nベース音を他の音より大きめの音量で\n発声します。\nただし、オートプレイの場合は通常音量で\n発声されます。",
            "To enhance the bass chip sound\n(except autoplay).");
        iSystemSoundMonitorBass.BindConfig(
            () => iSystemSoundMonitorBass.bON = CDTXMania.ConfigIni.b演奏音を強調する.Bass,
            () => CDTXMania.ConfigIni.b演奏音を強調する.Bass = iSystemSoundMonitorBass.bON);
        listItems.Add(iSystemSoundMonitorBass);

        CItemInteger? iSystemMinComboBass = new("B-MinCombo", 0, 0x1869f, CDTXMania.ConfigIni.n表示可能な最小コンボ数.Bass,
            "表示可能な最小コンボ数（ベース）：\n画面に表示されるコンボの最小の数\nを指定します。\n1 ～ 99999 の値が指定可能です。\n0にするとコンボを表示しません。",
            "Initial number to show the combo\n for the bass.\nYou can specify from 1 to 99999.");
        iSystemMinComboBass.BindConfig(
            () => iSystemMinComboBass.nCurrentValue = CDTXMania.ConfigIni.n表示可能な最小コンボ数.Bass,
            () => CDTXMania.ConfigIni.n表示可能な最小コンボ数.Bass = iSystemMinComboBass.nCurrentValue);
        listItems.Add(iSystemMinComboBass);

        CItemInteger? iBassJudgeLinePos = new("JudgeLinePos", 0, 100, CDTXMania.ConfigIni.nJudgeLine.Bass,
            "演奏時の判定ラインの高さを変更します。\n" +
            "0～100の間で指定できます。",
            "To change the judgeLinePosition for the\n" +
            "You can set it from 0 to 100.");
        iBassJudgeLinePos.BindConfig(
            () => iBassJudgeLinePos.nCurrentValue = CDTXMania.ConfigIni.nJudgeLine.Bass,
            () => CDTXMania.ConfigIni.nJudgeLine.Bass = iBassJudgeLinePos.nCurrentValue);
        listItems.Add(iBassJudgeLinePos);

        CItemInteger? iBassShutterInPos = new("ShutterInPos", 0, 100, CDTXMania.ConfigIni.nShutterInSide.Bass,
            "演奏時のノーツが現れる側のシャッターの\n" +
            "位置を変更します。",
            "\n" +
            "\n" +
            "");
        iBassShutterInPos.BindConfig(
            () => iBassShutterInPos.nCurrentValue = CDTXMania.ConfigIni.nShutterInSide.Bass,
            () => CDTXMania.ConfigIni.nShutterInSide.Bass = iBassShutterInPos.nCurrentValue);
        listItems.Add(iBassShutterInPos);

        CItemInteger? iBassShutterOutPos = new("ShutterOutPos", 0, 100, CDTXMania.ConfigIni.nShutterOutSide.Bass,
            "演奏時のノーツが消える側のシャッターの\n" +
            "位置を変更します。",
            "\n" +
            "\n" +
            "");
        iBassShutterOutPos.BindConfig(
            () => iBassShutterOutPos.nCurrentValue = CDTXMania.ConfigIni.nShutterOutSide.Bass,
            () => CDTXMania.ConfigIni.nShutterOutSide.Bass = iBassShutterOutPos.nCurrentValue);
        listItems.Add(iBassShutterOutPos);

        iBassGraph = new CItemToggle( "Graph", CDTXMania.ConfigIni.bGraph有効.Bass,
            "最高スキルと比較できるグラフを表示します。\n" +
            "オートプレイだと表示されません。\n" +
            "この項目を有効にすると、ギターパートのグラフは\n" +
            "無効になります。",
            "To draw Graph or not." );
        iBassGraph.BindConfig(
            () => iBassGraph.bON = CDTXMania.ConfigIni.bGraph有効.Bass,
            () => CDTXMania.ConfigIni.bGraph有効.Bass = iBassGraph.bON);
        iBassGraph.action = () =>
        {
            if (iBassGraph.bON == true)
            {
                CDTXMania.ConfigIni.bGraph有効.Guitar = false;
                iGuitarGraph.bON = false;
            }
        };
        listItems.Add( iBassGraph );

        // #23580 2011.1.3 yyagi
        CItemInteger? iBassInputAdjustTimeMs = new("InputAdjust", -99, 99, CDTXMania.ConfigIni.nInputAdjustTimeMs.Bass,
            "ベースの入力タイミングの微調整を行います。\n-99 ～ 99ms まで指定可能です。\n入力ラグを軽減するためには、\n負の値を指定してください。",
            "To adjust the bass input timing.\nYou can set from -99 to 0ms.\nTo decrease input lag, set minus value.");
        iBassInputAdjustTimeMs.BindConfig(
            () => iBassInputAdjustTimeMs.nCurrentValue = CDTXMania.ConfigIni.nInputAdjustTimeMs.Bass,
            () => CDTXMania.ConfigIni.nInputAdjustTimeMs.Bass = iBassInputAdjustTimeMs.nCurrentValue);
        listItems.Add(iBassInputAdjustTimeMs);

        CItemBase? iBassGoToKeyAssign = new("Bass Keys", CItemBase.EPanelType.Normal,
            "ベースのキー入力に関する項目を設定します。",
            "Settings for the bass key/pad inputs.")
        {
            action = tSetupItemList_KeyAssignBass
        };
        listItems.Add(iBassGoToKeyAssign);

        OnListMenuの初期化();
        nCurrentSelection = 0;
        eMenuType = EMenuType.Bass;
    }
        
    public void tSetupItemList_KeyAssignBass()
    {
        listItems.Clear();
            
        CItemBase? iKeyAssignBassReturnToMenu = new("<< ReturnTo Menu", CItemBase.EPanelType.Other,
            "左側のメニューに戻ります。",
            "Return to left menu.")
        {
            action = tSetupItemList_Bass
        };
        listItems.Add(iKeyAssignBassReturnToMenu);

        CItemBase? iKeyAssignBassR = new("R",
            "ベースのキー設定：\nRボタンへのキーの割り当てを設定し\nます。",
            "Bass key assign:\nTo assign key/pads for R button.");
        listItems.Add(iKeyAssignBassR);

        CItemBase? iKeyAssignBassG = new("G",
            "ベースのキー設定：\nGボタンへのキーの割り当てを設定し\nます。",
            "Bass key assign:\nTo assign key/pads for G button.");
        listItems.Add(iKeyAssignBassG);

        CItemBase? iKeyAssignBassB = new("B",
            "ベースのキー設定：\nBボタンへのキーの割り当てを設定し\nます。",
            "Bass key assign:\nTo assign key/pads for B button.");
        listItems.Add(iKeyAssignBassB);

        CItemBase? iKeyAssignBassY = new("Y",
            "ベースのキー設定：\nYボタンへのキーの割り当てを設定し\nます。",
            "Bass key assign:\nTo assign key/pads for Y button.");
        listItems.Add(iKeyAssignBassY);

        CItemBase? iKeyAssignBassP = new("P",
            "ベースのキー設定：\nPボタンへのキーの割り当てを設定し\nます。",
            "Bass key assign:\nTo assign key/pads for P button.");
        listItems.Add(iKeyAssignBassP);

        CItemBase? iKeyAssignBassPick = new("Pick",
            "ベースのキー設定：\nピックボタンへのキーの割り当てを設\n定します。",
            "Bass key assign:\nTo assign key/pads for Pick button.");
        listItems.Add(iKeyAssignBassPick);

        CItemBase? iKeyAssignBassWail = new("Wailing",
            "ベースのキー設定：\nWailingボタンへのキーの割り当てを設\n定します。",
            "Bass key assign:\nTo assign key/pads for Wailing button.");
        listItems.Add(iKeyAssignBassWail);

        CItemBase? iKeyAssignBassDecide = new("Decide",
            "ベースのキー設定：\n決定ボタンへのキーの割り当てを設\n定します。",
            "Bass key assign:\nTo assign key/pads for Decide button.");
        listItems.Add(iKeyAssignBassDecide);

        CItemBase? iKeyAssignBassCancel = new("Cancel",
            "ベースのキー設定：\n取消ボタンへのキーの割り当てを設\n定します。",
            "Bass key assign:\nTo assign key/pads for Cancel button.");
        listItems.Add(iKeyAssignBassCancel);

        OnListMenuの初期化();
        nCurrentSelection = 0;
        eMenuType = EMenuType.KeyAssignBass;
    }
    #endregion
}