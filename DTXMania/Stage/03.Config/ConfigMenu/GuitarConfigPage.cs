using DTXMania.Core;
using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

//Guitar P1. Shared (STDGBVALUE-backed) options come from InstrumentConfigPage
internal sealed class GuitarConfigPage(ConfigList list) : InstrumentConfigPage(list, EInstrumentPart.GUITAR)
{
    public override List<CItemBase> Build()
    {
        List<CItemBase> items = [];

        items.Add(BackItem());
        
        items.Add(CreateCardNameInputItem());
        items.Add(CreateGroupNameInputItem());

        // ---- Auto play: preset selector (cycles Off / presets / Custom) + per-lane toggles ----
        List<(ELane, CItemToggle)> autoLanes =
        [
            (ELane.GtR, Toggle("    R", "Rネックを自動で演奏します。", "Play R neck automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.GtR, v => CDTXMania.ConfigIni.bAutoPlay.GtR = v)),
            (ELane.GtG, Toggle("    G", "Gネックを自動で演奏します。", "Play G neck automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.GtG, v => CDTXMania.ConfigIni.bAutoPlay.GtG = v)),
            (ELane.GtB, Toggle("    B", "Bネックを自動で演奏します。", "Play B neck automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.GtB, v => CDTXMania.ConfigIni.bAutoPlay.GtB = v)),
            (ELane.GtY, Toggle("    Y", "Yネックを自動で演奏します。", "Play Y neck automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.GtY, v => CDTXMania.ConfigIni.bAutoPlay.GtY = v)),
            (ELane.GtP, Toggle("    P", "Pネックを自動で演奏します。", "Play P neck automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.GtP, v => CDTXMania.ConfigIni.bAutoPlay.GtP = v)),
            (ELane.GtPick, Toggle("    Pick", "ピックを自動で演奏します。", "Play Pick automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.GtPick, v => CDTXMania.ConfigIni.bAutoPlay.GtPick = v)),
            (ELane.GtW, Toggle("    Wailing", "ウェイリングを自動で演奏します。", "Play wailing automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.GtW, v => CDTXMania.ConfigIni.bAutoPlay.GtW = v)),
        ];
        AddAutoPlayBlock(items, autoLanes);

        // ---- Standard / display options ----
        items.Add(ScrollSpeedItem());
        items.Add(HidSudItem());
        AddDisplayBlock(items);

        items.Add(Choice("AttackEffect", "アタックエフェクトの表示 / 非表示。", "Toggle the attack effect.",
            ["ON", "OFF"],
            () => (int)CDTXMania.ConfigIni.eAttackEffect.Guitar, v => CDTXMania.ConfigIni.eAttackEffect.Guitar = (EType)v));
        items.Add(ReverseItem("ギターチップが上から下に流れます。", "Chips flow from the top to the bottom."));
        items.Add(Choice("Position", "判定文字の表示位置。", "Position of the judgement mark.",
            ["P-A", "P-B", "P-C", "OFF"],
            () => (int)CDTXMania.ConfigIni.JudgementStringPosition.Guitar, v => CDTXMania.ConfigIni.JudgementStringPosition.Guitar = (EType)v));
        items.Add(LightItem());
        items.Add(PerformanceModeItem());
        items.Add(Choice("Random", "ギターのチップがランダムに降ってきます。", "Guitar chips come randomly.",
            ["OFF", "Mirror", "Part", "Super", "Hyper"],
            () => (int)CDTXMania.ConfigIni.eRandom.Guitar, v => CDTXMania.ConfigIni.eRandom.Guitar = (ERandomMode)v));
        items.Add(LeftItem());
        items.Add(JudgeLinePosItem());
        items.Add(ShutterInItem());
        items.Add(ShutterOutItem());
        items.Add(MonitorItem("GuitarMonitor"));
        items.Add(MinComboItem("G-MinCombo", 0));
        items.Add(GraphItem(EInstrumentPart.BASS)); //enabling disables the bass graph
        items.Add(InputAdjustItem());

        items.Add(FolderItem("Key Assignment",
            "ギターのキー割り当てを設定します。",
            "Assign keys/pads for the guitar.", KeyAssignPage.ForGuitar(list)));

        return items;
    }
}
