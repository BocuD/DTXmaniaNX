using DTXMania.Core;
using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

/// <summary>
/// Base for the Drums / Guitar / Bass pages. Most of their options are the same setting stored per
/// instrument in an <see cref="STDGBVALUE{T}"/> (indexed by <see cref="EInstrumentPart"/>). This
/// class builds those shared items once, indexed by <see cref="instrument"/>; each page composes
/// them in its own order and adds its instrument-specific items.
/// </summary>
internal abstract class InstrumentConfigPage : ConfigPage
{
    protected readonly EInstrumentPart instrument;

    protected InstrumentConfigPage(ConfigList list, EInstrumentPart instrument) : base(list)
    {
        this.instrument = instrument;
    }

    private int Idx => (int)instrument;
    
    public CItemTextInput CreateCardNameInputItem()
    {
        CItemTextInput iCardName = TextInput($"Card name ({instrument})", CDTXMania.ConfigIni.strCardName[Idx],
            "カード名を入力します。\n",
            "Input your card name.\n",
            () => CDTXMania.ConfigIni.strCardName[Idx],
            s => CDTXMania.ConfigIni.strCardName[Idx] = s);
        return iCardName;
    }
    
    public CItemTextInput CreateGroupNameInputItem()
    {
        CItemTextInput iGroupName = TextInput($"Group name ({instrument})", CDTXMania.ConfigIni.strGroupName[Idx], 
            "グループ名を入力します。\n",
            "Input your Group name.\n",
            () => CDTXMania.ConfigIni.strGroupName[Idx],
            s => CDTXMania.ConfigIni.strGroupName[Idx] = s);
        return iGroupName;
    }
    
    // ---- shared STDGBVALUE-backed items ----

    protected CItemInteger ScrollSpeedItem()
    {
        CItemInteger item = Integer("ScrollSpeed", 0, 0x7cf,
            "ノーツの流れるスピードを変更します。",
            "Change the scroll speed (x0.5 to x1000.0).",
            () => CDTXMania.ConfigIni.nScrollSpeed[Idx], v => CDTXMania.ConfigIni.nScrollSpeed[Idx] = v);
        item.formatValue = () => $"x{(item.nCurrentValue + 1) * 0.5f:0.0}";
        return item;
    }

    protected CItemList HidSudItem() => Choice("HID-SUD",
        "チップ表示制御 (Hidden/Sudden/HidSud/Stealth)。", "Chip visibility control.",
        ["OFF", "Hidden", "Sudden", "HidSud", "Stealth"],
        () => CDTXMania.ConfigIni.nHidSud[Idx], v => CDTXMania.ConfigIni.nHidSud[Idx] = v);

    protected CItemList LaneDispItem() => Choice("LaneDisp",
        "レーン背景と小節線の表示。", "Toggle lane background and bar lines.",
        ["ALL ON", "LANE OFF", "LINE OFF", "ALL OFF"],
        () => CDTXMania.ConfigIni.nLaneDisp[Idx], v => CDTXMania.ConfigIni.nLaneDisp[Idx] = v);

    protected CItemToggle JudgeLineDispItem() => Toggle("JudgeLineDisp",
        "判定ラインの表示 / 非表示。", "Toggle JudgeLine.",
        () => CDTXMania.ConfigIni.bJudgeLineDisp[Idx], v => CDTXMania.ConfigIni.bJudgeLineDisp[Idx] = v);

    protected CItemToggle LaneFlushItem() => Toggle("LaneFlush",
        "レーンフラッシュの表示 / 非表示。", "Toggle LaneFlush.",
        () => CDTXMania.ConfigIni.bLaneFlush[Idx], v => CDTXMania.ConfigIni.bLaneFlush[Idx] = v);

    /// <summary>
    /// Adds the shared display block: "Dark" (a preset reading the shared eDark) followed by the
    /// LaneDisp / JudgeLineDisp / LaneFlush items it drives.
    /// </summary>
    protected void AddDisplayBlock(List<CItemBase> items)
    {
        CItemList laneDisp = LaneDispItem();
        CItemToggle judgeLineDisp = JudgeLineDispItem();
        CItemToggle laneFlush = LaneFlushItem();

        CItemList dark = new("       Dark", CItemBase.EPanelType.Normal, (int)CDTXMania.ConfigIni.eDark[(int)instrument],
            "レーン表示オプションをまとめて切り替えます (HALF/FULL)。",
            "OFF: all shown. HALF: lanes/gauge hidden. FULL: also bar lines and hit bar hidden.",
            ["OFF", "HALF", "FULL"]);
        dark.action = () =>
        {
            if (dark.nCurrentlySelectedIndex == (int)EDarkMode.FULL)
            {
                laneDisp.nCurrentlySelectedIndex = 3;
                judgeLineDisp.bON = false;
                laneFlush.bON = false;
            }
            else if (dark.nCurrentlySelectedIndex == (int)EDarkMode.HALF)
            {
                laneDisp.nCurrentlySelectedIndex = 1;
                judgeLineDisp.bON = true;
                laneFlush.bON = true;
            }
            else
            {
                laneDisp.nCurrentlySelectedIndex = 0;
                judgeLineDisp.bON = true;
                laneFlush.bON = true;
            }
        };

        items.Add(dark);
        items.Add(laneDisp);
        items.Add(judgeLineDisp);
        items.Add(laneFlush);
    }

    protected CItemToggle ReverseItem(string jp, string en) => Toggle("Reverse", jp, en,
        () => CDTXMania.ConfigIni.bReverse[Idx], v => CDTXMania.ConfigIni.bReverse[Idx] = v);

    protected CItemToggle MonitorItem(string label) => Toggle(label,
        "演奏音を強調して発声します。", "Enhance the chip sound (except autoplay).",
        () => CDTXMania.ConfigIni.b演奏音を強調する[Idx], v => CDTXMania.ConfigIni.b演奏音を強調する[Idx] = v);

    protected CItemInteger MinComboItem(string label, int min) => Integer(label, min, 0x1869f,
        "表示可能な最小コンボ数。", "Initial number to show the combo.",
        () => CDTXMania.ConfigIni.n表示可能な最小コンボ数[Idx], v => CDTXMania.ConfigIni.n表示可能な最小コンボ数[Idx] = v);

    protected CItemInteger JudgeLinePosItem() => Integer("JudgeLinePos", 0, 100,
        "判定ラインの位置。", "Judge line position (0-100).",
        () => CDTXMania.ConfigIni.nJudgeLine[Idx], v => CDTXMania.ConfigIni.nJudgeLine[Idx] = v);

    protected CItemInteger ShutterInItem() => Integer("ShutterInPos", 0, 100,
        "ノーツ出現側のシャッター位置。", "Shutter on the appearing side.",
        () => CDTXMania.ConfigIni.nShutterInSide[Idx], v => CDTXMania.ConfigIni.nShutterInSide[Idx] = v);

    protected CItemInteger ShutterOutItem() => Integer("ShutterOutPos", 0, 100,
        "ノーツが消える側のシャッター位置。", "Shutter on the disappearing side.",
        () => CDTXMania.ConfigIni.nShutterOutSide[Idx], v => CDTXMania.ConfigIni.nShutterOutSide[Idx] = v);

    protected CItemInteger InputAdjustItem() => Integer("InputAdjust", -99, 99,
        "入力タイミングの微調整。", "Adjust input timing (-99 to 99ms).",
        () => CDTXMania.ConfigIni.nInputAdjustTimeMs[Idx], v => CDTXMania.ConfigIni.nInputAdjustTimeMs[Idx] = v);

    /// <summary>Graph toggle; when <paramref name="disables"/> is given, enabling it turns that part's graph off.</summary>
    protected CItemToggle GraphItem(EInstrumentPart? disables = null)
    {
        CItemToggle graph = Toggle("Graph",
            "スキル比較グラフを表示します。", "Draw the skill comparison graph.",
            () => CDTXMania.ConfigIni.bGraph有効[Idx], v => CDTXMania.ConfigIni.bGraph有効[Idx] = v);

        if (disables is { } other)
        {
            graph.action = () =>
            {
                if (graph.bON) CDTXMania.ConfigIni.bGraph有効[(int)other] = false;
            };
        }

        return graph;
    }

    // ---- shared items used by Guitar/Bass only ----

    protected CItemToggle LightItem() => Toggle("Light",
        "チップのないところでピッキングしてもBADになりません。", "Picking without a chip doesn't become BAD.",
        () => CDTXMania.ConfigIni.bLight[Idx], v => CDTXMania.ConfigIni.bLight[Idx] = v);

    protected CItemList PerformanceModeItem() => Choice("Performance Mode",
        "演奏モード (Normal/Specialist)。", "Normal / Specialist performance mode.",
        ["Normal", "Specialist"],
        () => CDTXMania.ConfigIni.bSpecialist[Idx] ? 1 : 0, v => CDTXMania.ConfigIni.bSpecialist[Idx] = v == 1);

    protected CItemToggle LeftItem() => Toggle("Left",
        "RGBYPの並びが左右反転します（左利きモード）。", "Reverse lane order for lefty.",
        () => CDTXMania.ConfigIni.bLeft[Idx], v => CDTXMania.ConfigIni.bLeft[Idx] = v);
}
