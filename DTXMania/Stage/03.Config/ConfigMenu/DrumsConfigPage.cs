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

        items.Add(Choice("AttackEffect", "アタックエフェクトの表示方法。", "Attack effect display.",
            ["ALL ON", "ChipOFF", "EffectOnly", "ALL OFF"],
            () => (int)CDTXMania.ConfigIni.eAttackEffect.Drums, v => CDTXMania.ConfigIni.eAttackEffect.Drums = (EType)v));
        items.Add(ReverseItem("判定ラインが上になり、ノーツが下から上へ流れます。", "Chips flow from the bottom to the top."));
        items.Add(Choice("JudgePosition", "判定文字の位置を変更します。", "Position of the judgement mark.",
            ["P-A", "P-B", "OFF"],
            () => (int)CDTXMania.ConfigIni.JudgementStringPosition.Drums, v => CDTXMania.ConfigIni.JudgementStringPosition.Drums = (EType)v));
        items.Add(Toggle("Combo", "OFFにするとコンボが表示されなくなります。", "Turn ON the Drums Combo Display.",
            () => CDTXMania.ConfigIni.bドラムコンボ文字の表示, v => CDTXMania.ConfigIni.bドラムコンボ文字の表示 = v));
        items.Add(Choice("LaneType", "ドラムのレーン配置を変更します。", "Change the drum lane layout.",
            ["Type-A", "Type-B", "Type-C", "Type-D"],
            () => (int)CDTXMania.ConfigIni.eLaneType.Drums, v => CDTXMania.ConfigIni.eLaneType.Drums = (EType)v));
        items.Add(Choice("RDPosition", "ライドシンバルレーンの表示位置。", "Ride cymbal lane position.",
            ["RD RC", "RC RD"],
            () => (int)CDTXMania.ConfigIni.eRDPosition, v => CDTXMania.ConfigIni.eRDPosition = (ERDPosition)v));

        // ---- Special options (drums-specific) ----
        items.Add(Toggle("HAZARD", "ドSハザードモード。", "Super Hazard Mode.",
            () => CDTXMania.ConfigIni.bHAZARD, v => CDTXMania.ConfigIni.bHAZARD = v));
        items.Add(Toggle("Tight", "チップのないところで叩くとミスになります。", "Hitting pad without chip is a MISS.",
            () => CDTXMania.ConfigIni.bTight, v => CDTXMania.ConfigIni.bTight = v));
        items.Add(Choice("HH Group", "ハイハットレーン打ち分け設定。", "HiHat lane grouping.",
            ["HH-0", "HH-1", "HH-2", "HH-3"],
            () => (int)CDTXMania.ConfigIni.eHHGroup, v => CDTXMania.ConfigIni.eHHGroup = (EHHGroup)v));
        items.Add(Choice("FT Group", "フロアタム打ち分け設定。", "Floor tom grouping.",
            ["FT-0", "FT-1"],
            () => (int)CDTXMania.ConfigIni.eFTGroup, v => CDTXMania.ConfigIni.eFTGroup = (EFTGroup)v));
        items.Add(Choice("CY Group", "シンバルレーン打ち分け設定。", "Cymbal lane grouping.",
            ["CY-0", "CY-1"],
            () => (int)CDTXMania.ConfigIni.eCYGroup, v => CDTXMania.ConfigIni.eCYGroup = (ECYGroup)v));
        items.Add(Choice("BD Group", "フットペダル打ち分け設定。", "Foot pedal grouping.",
            ["BD-0", "BD-1", "BD-2", "BD-3"],
            () => (int)CDTXMania.ConfigIni.eBDGroup, v => CDTXMania.ConfigIni.eBDGroup = (EBDGroup)v));
        items.Add(Toggle("CymbalFree", "左右シンバルの区別をなくします。", "Group left/right cymbals.",
            () => CDTXMania.ConfigIni.bシンバルフリー, v => CDTXMania.ConfigIni.bシンバルフリー = v));
        items.Add(Choice("HH Priority", "ハイハット発声音の優先順位。", "HiHat playing-sound priority.",
            ["C>P", "P>C"],
            () => (int)CDTXMania.ConfigIni.eHitSoundPriorityHH, v => CDTXMania.ConfigIni.eHitSoundPriorityHH = (EPlaybackPriority)v));
        items.Add(Choice("FT Priority", "フロアタム発声音の優先順位。", "Floor tom playing-sound priority.",
            ["C>P", "P>C"],
            () => (int)CDTXMania.ConfigIni.eHitSoundPriorityFT, v => CDTXMania.ConfigIni.eHitSoundPriorityFT = (EPlaybackPriority)v));
        items.Add(Choice("CY Priority", "シンバル発声音の優先順位。", "Cymbal playing-sound priority.",
            ["C>P", "P>C"],
            () => (int)CDTXMania.ConfigIni.eHitSoundPriorityCY, v => CDTXMania.ConfigIni.eHitSoundPriorityCY = (EPlaybackPriority)v));
        items.Add(Toggle("FillIn", "フィルインエフェクトの使用。", "Show bursting effects at the fill-in zone.",
            () => CDTXMania.ConfigIni.bFillInEnabled, v => CDTXMania.ConfigIni.bFillInEnabled = v));
        items.Add(Toggle("HitSound", "打撃音の再生（ドラムのみ）。", "Play hitting chip sound (drums only).",
            () => CDTXMania.ConfigIni.bドラム打音を発声する, v => CDTXMania.ConfigIni.bドラム打音を発声する = v));

        items.Add(MonitorItem("DrumsMonitor"));
        items.Add(MinComboItem("D-MinCombo", 1));

        items.Add(Choice("HHOGraphics", "オープンハイハットの表示画像。", "Open hihat graphics.",
            ["Type A", "Type B", "Type C"],
            () => (int)CDTXMania.ConfigIni.eHHOGraphics.Drums, v => CDTXMania.ConfigIni.eHHOGraphics.Drums = (EType)v));
        items.Add(Choice("LBDGraphics", "LBDチップの表示画像。", "Left bass graphics.",
            ["Type A", "Type B"],
            () => (int)CDTXMania.ConfigIni.eLBDGraphics.Drums, v => CDTXMania.ConfigIni.eLBDGraphics.Drums = (EType)v));

        items.Add(JudgeLinePosItem());
        items.Add(ShutterInItem());
        items.Add(ShutterOutItem());

        items.Add(Toggle("Muting LP", "LP入力で発声中のHHを消音します。", "LP chips mute ringing HH chips.",
            () => CDTXMania.ConfigIni.bMutingLP, v => CDTXMania.ConfigIni.bMutingLP = v));
        items.Add(Toggle("AssignToLBD", "旧仕様のドコドコチップをLBDレーンに振り分けます。", "Move some bass chips to the LBD lane.",
            () => CDTXMania.ConfigIni.bAssignToLBD.Drums, v => CDTXMania.ConfigIni.bAssignToLBD.Drums = v));
        items.Add(Choice("DkdkType", "ツーバス譜面の仕様。", "Double-bass chip style.",
            ["L R", "R L", "R Only"],
            () => (int)CDTXMania.ConfigIni.eDkdkType.Drums, v => CDTXMania.ConfigIni.eDkdkType.Drums = (EType)v));
        items.Add(Choice("NumOfLanes", "レーン数を変更します。", "Number of lanes.",
            ["10", "9", "6"],
            () => (int)CDTXMania.ConfigIni.eNumOfLanes.Drums, v => CDTXMania.ConfigIni.eNumOfLanes.Drums = (EType)v));
        items.Add(Choice("RandomPad", "パッドチップがランダムに降ってきます。", "Drums pad chips come randomly.",
            ["OFF", "Mirror", "Part", "Super", "Hyper", "Master", "Another"],
            () => (int)CDTXMania.ConfigIni.eRandom.Drums, v => CDTXMania.ConfigIni.eRandom.Drums = (ERandomMode)v));
        items.Add(Choice("RandomPedal", "足チップがランダムに降ってきます。", "Drums pedal chips come randomly.",
            ["OFF", "Mirror", "Part", "Super", "Hyper", "Master", "Another"],
            () => (int)CDTXMania.ConfigIni.eRandomPedal.Drums, v => CDTXMania.ConfigIni.eRandomPedal.Drums = (ERandomMode)v));

        items.Add(GraphItem()); // drums graph has no mutual exclusion
        items.Add(InputAdjustItem());

        items.Add(FolderItem("Drum Hit Velocity",
            "ドラムのヒットベロシティに関する項目を設定します。",
            "Settings for the drums hit velocity.", velocity));

        return items;
    }
}
