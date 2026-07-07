using DTXMania.Core;
using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

//Guitar P2 (bass). Shared (STDGBVALUE-backed) options come from InstrumentConfigPage
internal sealed class BassConfigPage(ConfigList list) : InstrumentConfigPage(list, EInstrumentPart.BASS)
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
            (ELane.BsR, Toggle("    R", "Rネックを自動で演奏します。", "Play R neck automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.BsR, v => CDTXMania.ConfigIni.bAutoPlay.BsR = v)),
            (ELane.BsG, Toggle("    G", "Gネックを自動で演奏します。", "Play G neck automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.BsG, v => CDTXMania.ConfigIni.bAutoPlay.BsG = v)),
            (ELane.BsB, Toggle("    B", "Bネックを自動で演奏します。", "Play B neck automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.BsB, v => CDTXMania.ConfigIni.bAutoPlay.BsB = v)),
            (ELane.BsY, Toggle("    Y", "Yネックを自動で演奏します。", "Play Y neck automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.BsY, v => CDTXMania.ConfigIni.bAutoPlay.BsY = v)),
            (ELane.BsP, Toggle("    P", "Pネックを自動で演奏します。", "Play P neck automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.BsP, v => CDTXMania.ConfigIni.bAutoPlay.BsP = v)),
            (ELane.BsPick, Toggle("    Pick", "ピックを自動で演奏します。", "Play Pick automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.BsPick, v => CDTXMania.ConfigIni.bAutoPlay.BsPick = v)),
            (ELane.BsW, Toggle("    Wailing", "ウェイリングを自動で演奏します。", "Play wailing automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.BsW, v => CDTXMania.ConfigIni.bAutoPlay.BsW = v)),
        ];
        AddAutoPlayBlock(items, autoLanes);

        // ---- Standard / display options ----
        items.Add(ScrollSpeedItem());
        items.Add(HidSudItem());
        AddDisplayBlock(items);

        items.Add(Choice("AttackEffect", "アタックエフェクトの表示 / 非表示。", "Toggle the attack effect.",
            ["ON", "OFF"],
            () => (int)CDTXMania.ConfigIni.eAttackEffect.Bass, v => CDTXMania.ConfigIni.eAttackEffect.Bass = (EType)v));
        items.Add(ReverseItem("ベースチップが上から下に流れます。", "Chips flow from the top to the bottom."));
        items.Add(Choice("Position", "判定文字の表示位置。", "Position of the judgement mark.",
            ["P-A", "P-B", "P-C", "OFF"],
            () => (int)CDTXMania.ConfigIni.JudgementStringPosition.Bass, v => CDTXMania.ConfigIni.JudgementStringPosition.Bass = (EType)v));
        items.Add(Choice("Random", "ベースのチップがランダムに降ってきます。", "Bass chips come randomly.",
            ["OFF", "Mirror", "Part", "Super", "Hyper"],
            () => (int)CDTXMania.ConfigIni.eRandom.Bass, v => CDTXMania.ConfigIni.eRandom.Bass = (ERandomMode)v));
        items.Add(LightItem());
        items.Add(PerformanceModeItem());
        items.Add(LeftItem());
        items.Add(MonitorItem("BassMonitor"));
        items.Add(MinComboItem("B-MinCombo", 0));
        items.Add(JudgeLinePosItem());
        items.Add(ShutterInItem());
        items.Add(ShutterOutItem());
        items.Add(GraphItem(EInstrumentPart.GUITAR)); //enabling disables the guitar graph
        items.Add(InputAdjustItem());

        return items;
    }
}
