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
        
        // ---- Auto play (bass-specific) ----
        CItemThreeState autoPlayAll = new("AutoPlay (All)", CItemThreeState.EState.UNDEFINED,
            "全ネック/ピックの自動演奏の ON/OFF をまとめて切り替えます。",
            "Toggle Auto for all bass neck/pick at once.");
        items.Add(autoPlayAll);

        CItemToggle r = Toggle("    R", "Rネックを自動で演奏します。", "Play R neck automatically.",
            () => CDTXMania.ConfigIni.bAutoPlay.BsR, v => CDTXMania.ConfigIni.bAutoPlay.BsR = v);
        CItemToggle g = Toggle("    G", "Gネックを自動で演奏します。", "Play G neck automatically.",
            () => CDTXMania.ConfigIni.bAutoPlay.BsG, v => CDTXMania.ConfigIni.bAutoPlay.BsG = v);
        CItemToggle b = Toggle("    B", "Bネックを自動で演奏します。", "Play B neck automatically.",
            () => CDTXMania.ConfigIni.bAutoPlay.BsB, v => CDTXMania.ConfigIni.bAutoPlay.BsB = v);
        CItemToggle y = Toggle("    Y", "Yネックを自動で演奏します。", "Play Y neck automatically.",
            () => CDTXMania.ConfigIni.bAutoPlay.BsY, v => CDTXMania.ConfigIni.bAutoPlay.BsY = v);
        CItemToggle p = Toggle("    P", "Pネックを自動で演奏します。", "Play P neck automatically.",
            () => CDTXMania.ConfigIni.bAutoPlay.BsP, v => CDTXMania.ConfigIni.bAutoPlay.BsP = v);
        CItemToggle pick = Toggle("    Pick", "ピックを自動で演奏します。", "Play Pick automatically.",
            () => CDTXMania.ConfigIni.bAutoPlay.BsPick, v => CDTXMania.ConfigIni.bAutoPlay.BsPick = v);
        CItemToggle wailing = Toggle("    Wailing", "ウェイリングを自動で演奏します。", "Play wailing automatically.",
            () => CDTXMania.ConfigIni.bAutoPlay.BsW, v => CDTXMania.ConfigIni.bAutoPlay.BsW = v);

        autoPlayAll.action = () =>
        {
            bool on = autoPlayAll.eCurrentState == CItemThreeState.EState.ON;
            r.bON = g.bON = b.bON = y.bON = p.bON = pick.bON = wailing.bON = on;
        };
        items.Add(r);
        items.Add(g);
        items.Add(b);
        items.Add(y);
        items.Add(p);
        items.Add(pick);
        items.Add(wailing);

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
