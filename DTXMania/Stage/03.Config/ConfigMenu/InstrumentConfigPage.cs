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
        "チップの見え方を制御します。\nHidden: 手前で消える。\nSudden: 途中から現れる。\nHidSud: 中央付近のみ表示。\nStealth: 常に非表示。",
        "Chip visibility.\nHidden: chips disappear at mid-screen.\nSudden: chips appear at mid-screen.\nHidSud: visible only around mid-screen.\nStealth: chips never visible.",
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

    // ---- Random (shared) ----

    // Guitar & bass expose five random modes; drums add Master/Another.
    private static readonly string[] RandomOptions = ["OFF", "Mirror", "Part", "Super", "Hyper"];
    private static readonly string[] RandomOptionsDrums = ["OFF", "Mirror", "Part", "Super", "Hyper", "Master", "Another"];

    /// <summary>The main random chip-shuffle item for this instrument (named "RandomPad" on drums).</summary>
    protected CItemList RandomItem()
    {
        (string jp, string en) = instrument switch
        {
            EInstrumentPart.DRUMS => (
                "パッドチップをランダム化します。\nMirror: 左右反転。\nPart: レーン単位で入替。\nSuper: 小節単位で入替。\nHyper: 1拍ごとに入替。\nMaster: 激しく入替。\nAnother: 程よくばらける。",
                "Randomize pad chips.\nMirror: flip left/right.\nPart: swap by lane.\nSuper: swap per measure.\nHyper: swap per beat.\nMaster: extreme swapping.\nAnother: moderate spread."),
            EInstrumentPart.GUITAR => (
                "ギターのチップの並びをランダム化します。\n  Mirror: 左右反転\n  Part: 小節ごとにレーンを入れ替え\n  Super: チップごとに入れ替え(本数は不変)\n  Hyper: 本数も変わる",
                "Randomize the guitar chip lanes.\n  Mirror: flip left/right\n  Part: swap lanes each measure\n  Super: swap per chip (lane count kept)\n  Hyper: swap per chip (lane count changes too)"),
            _ => (
                "ベースのチップの並びをランダム化します。\n  Mirror: 左右反転\n  Part: 小節ごとにレーンを入れ替え\n  Super: チップごとに入れ替え(本数は不変)\n  Hyper: 本数も変わる",
                "Randomize the bass chip lanes.\n  Mirror: flip left/right\n  Part: swap lanes each measure\n  Super: swap per chip (lane count kept)\n  Hyper: swap per chip (lane count changes too)")
        };

        return Choice(instrument == EInstrumentPart.DRUMS ? "RandomPad" : "Random", jp, en,
            instrument == EInstrumentPart.DRUMS ? RandomOptionsDrums : RandomOptions,
            () => (int)CDTXMania.ConfigIni.eRandom[Idx], v => CDTXMania.ConfigIni.eRandom[Idx] = (ERandomMode)v);
    }

    /// <summary>Drums-only: the separate random mode for the foot/pedal chips.</summary>
    protected CItemList RandomPedalItem() => Choice("RandomPedal",
        "足(ペダル)チップをランダム化します。\nMirror: 左右反転。\nPart: レーン単位で入替。\nSuper: 小節単位で入替。\nHyper: 1拍ごとに入替。\nMaster: 激しく入替。\nAnother: 程よくばらける。",
        "Randomize pedal chips.\nMirror: flip left/right.\nPart: swap by lane.\nSuper: swap per measure.\nHyper: swap per beat.\nMaster: extreme swapping.\nAnother: moderate spread.",
        RandomOptionsDrums,
        () => (int)CDTXMania.ConfigIni.eRandomPedal[Idx], v => CDTXMania.ConfigIni.eRandomPedal[Idx] = (ERandomMode)v);

    // ---- auto-play preset selector (shared) ----

    /// <summary>
    /// Builds the auto-play preset selector for this instrument (Off / presets... / Custom, from
    /// <see cref="AutoMode.AutoModes"/>). Cycling it applies the chosen preset to <c>bAutoPlay</c>;
    /// "Custom" applies the separately-stored <c>bAutoPlayCustom</c> so cycling never clears it.
    ///
    /// When <paramref name="laneToggles"/> is given (full config) the selector also updates those
    /// toggle items, and manually toggling any lane flips the selector to "Custom" and saves the
    /// flags to <c>bAutoPlayCustom</c>. In the quick menu there are no lane toggles, so it just
    /// applies <c>bAutoPlay</c> directly.
    /// </summary>
    protected CItemList AutoPlaySelectorItem(IReadOnlyList<(ELane lane, CItemToggle toggle)>? laneToggles = null)
    {
        List<AutoMode> presets = AutoMode.AutoModes[instrument];
        List<ELane> lanes = presets.First(p => !p.isCustom).state.Keys.ToList();
        int customIndex = presets.FindIndex(p => p.isCustom);

        // If the current auto-play doesn't match any preset, it's effectively custom: capture it so
        // the "Custom" option reflects the current state on entry.
        int initialIndex = MatchingPresetIndex(lanes);
        if (initialIndex == customIndex)
        {
            SaveCustomFromConfig(lanes);
        }

        CItemList selector = new("AutoPlay", CItemBase.EPanelType.Normal, initialIndex,
            "オートプレイのプリセットを切り替えます。",
            "Cycle through the auto-play presets (Off / presets / Custom).",
            presets.Select(p => p.label).ToArray());
        // derived from bAutoPlay; only used on an explicit re-read (e.g. config import)
        selector.BindConfig(() => selector.nCurrentlySelectedIndex = MatchingPresetIndex(lanes));
        selector.action = () => ApplyAutoPlayPreset(presets[selector.nCurrentlySelectedIndex], lanes, laneToggles);
        selector.formatDescription = () => AutoPlayDescription(lanes);

        if (laneToggles != null)
        {
            foreach ((ELane _, CItemToggle toggle) in laneToggles)
            {
                toggle.action = () =>
                {
                    // a manual lane edit is a custom set: remember it and mark the selector "Custom"
                    selector.nCurrentlySelectedIndex = customIndex;
                    SaveCustomFromItems(laneToggles);
                };
            }
        }

        return selector;
    }

    /// <summary>
    /// Adds the auto-play preset selector followed by an "Auto Lanes" folder. The per-lane toggles
    /// live on that folder's sub-page (rather than inline) so the main page stays compact. Both the
    /// selector and the folder preview the current auto-play state in their description panel.
    /// </summary>
    protected void AddAutoPlayBlock(List<CItemBase> items, IReadOnlyList<(ELane lane, CItemToggle toggle)> laneToggles)
    {
        List<ELane> lanes = AutoMode.AutoModes[instrument].First(p => !p.isCustom).state.Keys.ToList();

        items.Add(AutoPlaySelectorItem(laneToggles));

        // per-lane toggles on their own sub-page, opened by the folder below the selector
        List<CItemBase> lanePage = [BackItem()];
        foreach ((ELane _, CItemToggle toggle) in laneToggles)
        {
            lanePage.Add(toggle);
        }

        CItemBase folder = new("    Auto Lanes", CItemBase.EPanelType.Folder,
            "各レーンごとのオートプレイ設定。", "Per-lane auto-play settings.")
        {
            action = () => list.OpenFolder(lanePage),
            formatDescription = () => AutoPlayDescription(lanes)
        };
        items.Add(folder);
    }

    // A live text preview of the auto-play state: the matching preset name + which lanes are on auto.
    private string AutoPlayDescription(List<ELane> lanes)
    {
        string mode = AutoMode.AutoModes[instrument][MatchingPresetIndex(lanes)].label;

        List<string> on = lanes.Where(l => CDTXMania.ConfigIni.bAutoPlay[(int)l]).Select(LaneShortName).ToList();
        string lanesText = on.Count == 0
            ? (CDTXMania.isJapanese ? "なし" : "none")
            : string.Join(" ", on);

        return CDTXMania.isJapanese
            ? $"オートプレイ: {mode}\nレーン: {lanesText}"
            : $"Auto: {mode}\nLanes: {lanesText}";
    }

    // Short lane label for the preview (drops the "Gt"/"Bs" instrument prefix on guitar/bass lanes).
    private static string LaneShortName(ELane lane)
    {
        string name = lane.ToString();
        return name.StartsWith("Gt") || name.StartsWith("Bs") ? name[2..] : name;
    }

    // index of the preset matching the current bAutoPlay, or the Custom index if none match
    private int MatchingPresetIndex(List<ELane> lanes)
    {
        List<AutoMode> presets = AutoMode.AutoModes[instrument];
        for (int i = 0; i < presets.Count; i++)
        {
            AutoMode preset = presets[i];
            if (preset.isCustom) continue;

            if (lanes.All(l => preset.state.TryGetValue(l, out bool v) && CDTXMania.ConfigIni.bAutoPlay[(int)l] == v))
            {
                return i;
            }
        }
        return presets.FindIndex(p => p.isCustom);
    }

    private void ApplyAutoPlayPreset(AutoMode preset, List<ELane> lanes,
        IReadOnlyList<(ELane lane, CItemToggle toggle)>? laneToggles)
    {
        foreach (ELane lane in lanes)
        {
            bool value = preset.isCustom
                ? CDTXMania.ConfigIni.bAutoPlayCustom[(int)lane]
                : preset.state[lane];
            CDTXMania.ConfigIni.bAutoPlay[(int)lane] = value;
        }

        // keep the visible lane toggle items in sync so CommitPage's write-back stays consistent
        if (laneToggles != null)
        {
            foreach ((ELane lane, CItemToggle toggle) in laneToggles)
            {
                toggle.bON = CDTXMania.ConfigIni.bAutoPlay[(int)lane];
            }
        }
    }

    private void SaveCustomFromConfig(List<ELane> lanes)
    {
        foreach (ELane lane in lanes)
        {
            CDTXMania.ConfigIni.bAutoPlayCustom[(int)lane] = CDTXMania.ConfigIni.bAutoPlay[(int)lane];
        }
    }

    private static void SaveCustomFromItems(IReadOnlyList<(ELane lane, CItemToggle toggle)> laneToggles)
    {
        foreach ((ELane lane, CItemToggle toggle) in laneToggles)
        {
            CDTXMania.ConfigIni.bAutoPlayCustom[(int)lane] = toggle.bON;
        }
    }
}
