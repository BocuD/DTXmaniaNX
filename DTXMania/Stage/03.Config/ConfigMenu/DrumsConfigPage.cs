using DTXMania.Core;
using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

//Shared (STDGBVALUE-backed) options come from InstrumentConfigPage
internal sealed class DrumsConfigPage : InstrumentConfigPage
{
    private readonly DrumsVelocityConfigPage velocity;

    public DrumsConfigPage(ConfigList list) : base(list, EInstrumentPart.DRUMS)
    {
        velocity = new DrumsVelocityConfigPage(list);
    }
    
    public override List<CItemBase> Build()
    {
        List<CItemBase> items = [];

        items.Add(BackItem());

        items.Add(CreateCardNameInputItem());
        items.Add(CreateGroupNameInputItem());
        
        // ---- Auto play: preset selector (cycles Off / presets / Custom) + per-lane toggles ----
        List<(ELane, CItemToggle)> autoLanes =
        [
            (ELane.LC, Toggle("    LeftCymbal", "左シンバルを自動で演奏します。", "Play Left Cymbal automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.LC, v => CDTXMania.ConfigIni.bAutoPlay.LC = v)),
            (ELane.HH, Toggle("    HiHat", "ハイハットを自動で演奏します。", "Play HiHat automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.HH, v => CDTXMania.ConfigIni.bAutoPlay.HH = v)),
            (ELane.LP, Toggle("    LeftPedal", "左ペダルを自動で演奏します。", "Play Left Pedal automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.LP, v => CDTXMania.ConfigIni.bAutoPlay.LP = v)),
            (ELane.LBD, Toggle("    LBassDrum", "左バスドラムを自動で演奏します。", "Play Left Bass Drum automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.LBD, v => CDTXMania.ConfigIni.bAutoPlay.LBD = v)),
            (ELane.SD, Toggle("    Snare", "スネアを自動で演奏します。", "Play Snare automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.SD, v => CDTXMania.ConfigIni.bAutoPlay.SD = v)),
            (ELane.BD, Toggle("    BassDrum", "バスドラムを自動で演奏します。", "Play Bass Drum automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.BD, v => CDTXMania.ConfigIni.bAutoPlay.BD = v)),
            (ELane.HT, Toggle("    HighTom", "ハイタムを自動で演奏します。", "Play High Tom automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.HT, v => CDTXMania.ConfigIni.bAutoPlay.HT = v)),
            (ELane.LT, Toggle("    LowTom", "ロータムを自動で演奏します。", "Play Low Tom automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.LT, v => CDTXMania.ConfigIni.bAutoPlay.LT = v)),
            (ELane.FT, Toggle("    FloorTom", "フロアタムを自動で演奏します。", "Play Floor Tom automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.FT, v => CDTXMania.ConfigIni.bAutoPlay.FT = v)),
            (ELane.CY, Toggle("    Cymbal", "右シンバルを自動で演奏します。", "Play Right Cymbal automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.CY, v => CDTXMania.ConfigIni.bAutoPlay.CY = v)),
            (ELane.RD, Toggle("    Ride", "ライドシンバルを自動で演奏します。", "Play Ride Cymbal automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.RD, v => CDTXMania.ConfigIni.bAutoPlay.RD = v)),
        ];
        AddAutoPlayBlock(items, autoLanes);

        // ---- Standard / display options (shared) ----
        items.Add(ScrollSpeedItem());
        items.Add(HidSudItem());
        AddDisplayBlock(items); // Dark + LaneDisp + JudgeLineDisp + LaneFlush

        items.Add(Choice("AttackEffect",
            "アタックエフェクトの表示。\nALL ON: 全て表示。\nChipOFF: チップエフェクトのみ非表示。\nEffectOnly: エフェクト画像以外を非表示。\nALL OFF: 全て非表示。",
            "Attack effect display.\nALL ON: show all.\nChipOFF: hide the chip effect only.\nEffectOnly: hide all but the effect image.\nALL OFF: hide all.",
            ["ALL ON", "ChipOFF", "EffectOnly", "ALL OFF"],
            () => (int)CDTXMania.ConfigIni.eAttackEffect.Drums, v => CDTXMania.ConfigIni.eAttackEffect.Drums = (EType)v));
        items.Add(ReverseItem("判定ラインが上になり、ノーツが下から上へ流れます。", "Chips flow from the bottom to the top."));
        items.Add(Choice("JudgePosition",
            "判定文字(Perfect/Great等)の表示位置。\nP-A: レーン上。\nP-B: 判定ライン下。\nOFF: 表示しない。",
            "Where the judgement text (Perfect/Great/...) shows.\nP-A: on the lanes.\nP-B: under the hit bar.\nOFF: hidden.",
            ["P-A", "P-B", "OFF"],
            () => (int)CDTXMania.ConfigIni.JudgementStringPosition.Drums, v => CDTXMania.ConfigIni.JudgementStringPosition.Drums = (EType)v));
        items.Add(Toggle("Combo", "OFFにするとコンボが表示されなくなります。", "Turn ON the Drums Combo Display.",
            () => CDTXMania.ConfigIni.bドラムコンボ文字の表示, v => CDTXMania.ConfigIni.bドラムコンボ文字の表示 = v));
        items.Add(Choice("LaneType",
            "ドラムのレーン配置。\nType-A: 通常。\nType-B: 2ペダルとタムをまとめる。\nType-C: 3タムのみまとめる。\nType-D: 左右対称配置。",
            "Drum lane layout.\nType-A: default.\nType-B: 2 pedals and toms grouped.\nType-C: 3 toms grouped only.\nType-D: fully symmetric layout.",
            ["Type-A", "Type-B", "Type-C", "Type-D"],
            () => (int)CDTXMania.ConfigIni.eLaneType.Drums, v => CDTXMania.ConfigIni.eLaneType.Drums = (EType)v));
        items.Add(EnumChoice("RDPosition",
            "ライドシンバルレーンの位置。\nRD RC: 最右端がRC。\nRC RD: 最右端がRD。",
            "Ride cymbal lane placement.\nRD RC: rightmost lane is RC.\nRC RD: rightmost lane is RD.",
            () => CDTXMania.ConfigIni.eRDPosition, v => CDTXMania.ConfigIni.eRDPosition = v));

        // ---- Special options (drums-specific) ----
        items.Add(Toggle("HAZARD",
            "ドSハザードモード。\nGREAT以下の判定でも残り回数が減ります。",
            "Super Hazard mode.\nEven GREAT-or-lower judgements cost a life.",
            () => CDTXMania.ConfigIni.bHAZARD, v => CDTXMania.ConfigIni.bHAZARD = v));
        items.Add(Toggle("Tight", "チップのないところで叩くとミスになります。", "Hitting pad without chip is a MISS.",
            () => CDTXMania.ConfigIni.bTight, v => CDTXMania.ConfigIni.bTight = v));
        items.Add(EnumChoice("HH Group",
            "左シンバル/HHクローズ/HHオープンの打ち分け(| = 別、& = 共通)。\nHH-0: LC | HC | HO (全て別)\nHH-1: LC & (HC | HO)\nHH-2: LC | (HC & HO)\nHH-3: LC & HC & HO (全て共通)",
            "How LeftCymbal / HH-Close / HH-Open share lanes (| = separate, & = shared).\nHH-0: LC | HC | HO (all separate)\nHH-1: LC & (HC | HO)\nHH-2: LC | (HC & HO)\nHH-3: LC & HC & HO (all shared)",
            () => CDTXMania.ConfigIni.eHHGroup, v => CDTXMania.ConfigIni.eHHGroup = v));
        items.Add(EnumChoice("FT Group",
            "ロータム/フロアタムの打ち分け(| = 別、& = 共通)。\nFT-0: LT | FT (別)\nFT-1: LT & FT (共通)",
            "Low Tom / Floor Tom grouping (| = separate, & = shared).\nFT-0: LT | FT (separate)\nFT-1: LT & FT (shared)",
            () => CDTXMania.ConfigIni.eFTGroup, v => CDTXMania.ConfigIni.eFTGroup = v));
        items.Add(EnumChoice("CY Group",
            "右シンバル/ライドの打ち分け(| = 別、& = 共通)。\nCY-0: CY | RD (別)\nCY-1: CY & RD (共通)",
            "Right Cymbal / Ride grouping (| = separate, & = shared).\nCY-0: CY | RD (separate)\nCY-1: CY & RD (shared)",
            () => CDTXMania.ConfigIni.eCYGroup, v => CDTXMania.ConfigIni.eCYGroup = v));
        items.Add(EnumChoice("BD Group",
            "左ペダル/左バスドラ/右バスドラの打ち分け(| = 別、& = 共通)。\nBD-0: LP | LBD | BD\nBD-1: LP | (LBD & BD)\nBD-2: LP & (LBD | BD)\nBD-3: LP & LBD & BD",
            "Left Pedal / Left Bass / Bass grouping (| = separate, & = shared).\nBD-0: LP | LBD | BD\nBD-1: LP | (LBD & BD)\nBD-2: LP & (LBD | BD)\nBD-3: LP & LBD & BD",
            () => CDTXMania.ConfigIni.eBDGroup, v => CDTXMania.ConfigIni.eBDGroup = v));
        items.Add(Toggle("CymbalFree",
            "左シンバルと右シンバルを同一レーン/音に統合します。\nライドまで統合するかは CY Group に従います。",
            "Groups Left and Right cymbals into one.\nWhether Ride is also grouped follows the 'CY Group' setting.",
            () => CDTXMania.ConfigIni.bシンバルフリー, v => CDTXMania.ConfigIni.bシンバルフリー = v));
        items.Add(EnumChoice("HH Priority",
            "HHの打ち分け時、発声音の決め方。\nC>P: チップの音を優先。\nP>C: 叩いたパッドの音を優先。",
            "When HH lanes are grouped, which sound plays.\nC>P: the chip's sound wins.\nP>C: the hit pad's sound wins.",
            () => CDTXMania.ConfigIni.eHitSoundPriorityHH, v => CDTXMania.ConfigIni.eHitSoundPriorityHH = v));
        items.Add(EnumChoice("FT Priority",
            "FTの打ち分け時、発声音の決め方。\nC>P: チップの音を優先。\nP>C: 叩いたパッドの音を優先。",
            "When FT lanes are grouped, which sound plays.\nC>P: the chip's sound wins.\nP>C: the hit pad's sound wins.",
            () => CDTXMania.ConfigIni.eHitSoundPriorityFT, v => CDTXMania.ConfigIni.eHitSoundPriorityFT = v));
        items.Add(EnumChoice("CY Priority",
            "CYの打ち分け時、発声音の決め方。\nC>P: チップの音を優先。\nP>C: 叩いたパッドの音を優先。",
            "When CY lanes are grouped, which sound plays.\nC>P: the chip's sound wins.\nP>C: the hit pad's sound wins.",
            () => CDTXMania.ConfigIni.eHitSoundPriorityCY, v => CDTXMania.ConfigIni.eHitSoundPriorityCY = v));
        items.Add(Toggle("FillIn", "フィルインエフェクトの使用。", "Show bursting effects at the fill-in zone.",
            () => CDTXMania.ConfigIni.bFillInEnabled, v => CDTXMania.ConfigIni.bFillInEnabled = v));
        items.Add(Toggle("HitSound", "打撃音の再生（ドラムのみ）。", "Play hitting chip sound (drums only).",
            () => CDTXMania.ConfigIni.bドラム打音を発声する, v => CDTXMania.ConfigIni.bドラム打音を発声する = v));

        items.Add(MonitorItem("DrumsMonitor"));
        items.Add(MinComboItem("D-MinCombo", 1));

        items.Add(Choice("HHOGraphics",
            "オープンハイハットの表示画像。\nType A: 標準(○あり)。\nType B: ○なし。\nType C: クローズと同じ。",
            "Open hi-hat graphic.\nType A: default (with circle).\nType B: without the circle.\nType C: same as closed hi-hat.",
            ["Type A", "Type B", "Type C"],
            () => (int)CDTXMania.ConfigIni.eHHOGraphics.Drums, v => CDTXMania.ConfigIni.eHHOGraphics.Drums = (EType)v));
        items.Add(Choice("LBDGraphics",
            "左バスドラチップの画像。\nType A: LPと同じ画像。\nType B: LPと色分け。",
            "Left-bass chip graphic.\nType A: same image as LP.\nType B: color-coded, distinct from LP.",
            ["Type A", "Type B"],
            () => (int)CDTXMania.ConfigIni.eLBDGraphics.Drums, v => CDTXMania.ConfigIni.eLBDGraphics.Drums = (EType)v));

        items.Add(JudgeLinePosItem());
        items.Add(ShutterInItem());
        items.Add(ShutterOutItem());

        items.Add(Toggle("Muting LP", "LP入力で発声中のHHを消音します。", "LP chips mute ringing HH chips.",
            () => CDTXMania.ConfigIni.bMutingLP, v => CDTXMania.ConfigIni.bMutingLP = v));
        items.Add(Toggle("AssignToLBD",
            "旧仕様のツーバス譜面の一部バスチップをLBDレーンに振り分けます。\nLP/LBDチップを持つ譜面では無効。",
            "Moves some bass chips of old-style 2-bass charts onto the LBD lane.\nIgnored on charts that already have LP/LBD chips.",
            () => CDTXMania.ConfigIni.bAssignToLBD.Drums, v => CDTXMania.ConfigIni.bAssignToLBD.Drums = v));
        items.Add(Choice("DkdkType",
            "ツーバス(ドコドコ)譜面の割り当て方。\nL R: 標準。\nR L: 始動足を反転。\nR Only: 1レーンにまとめる。",
            "How double-bass chips are laid out.\nL R: default.\nR L: swaps the starting foot.\nR Only: puts them on a single lane.",
            ["L R", "R L", "R Only"],
            () => (int)CDTXMania.ConfigIni.eDkdkType.Drums, v => CDTXMania.ConfigIni.eDkdkType.Drums = (EType)v));
        items.Add(Choice("NumOfLanes",
            "レーン数。\n10: 標準。\n9: XG仕様。\n6: CLASSIC仕様。",
            "Number of lanes.\n10: default.\n9: XG style.\n6: classic style.",
            ["10", "9", "6"],
            () => (int)CDTXMania.ConfigIni.eNumOfLanes.Drums, v => CDTXMania.ConfigIni.eNumOfLanes.Drums = (EType)v));
        items.Add(RandomItem());
        items.Add(RandomPedalItem());

        items.Add(GraphItem()); // drums graph has no mutual exclusion
        items.Add(InputAdjustItem());

        items.Add(FolderItem("Drum Hit Velocity",
            "ドラムのヒットベロシティに関する項目を設定します。",
            "Settings for the drums hit velocity.", velocity));

        items.Add(FolderItem("Key Assignment",
            "ドラムのキー割り当てを設定します。",
            "Assign keys/pads for the drums.", KeyAssignPage.ForDrums(list)));

        return items;
    }
}
