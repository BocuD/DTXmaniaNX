using DTXMania.Core;
using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

internal sealed class GuitarDisplayConfigPage : PlayerConfigPage
{
    public GuitarDisplayConfigPage(ConfigList list, EPlayer player) : base(list, player)
    {
    }

    public override List<CItemBase> Build()
    {
        List<CItemBase> items = [];

        items.Add(BackItem());

        AddDisplayBlock(items); // Dark + LaneDisp + JudgeLineDisp + LaneFlush

        items.Add(Choice("Position",
            "判定文字(Perfect / Great …)の表示位置。\n  P-A: レーン上\n  P-B: COMBO の下\n  P-C: 判定ライン上\n  OFF: 表示しない",
            "Where the judgement mark (Perfect, Great, ...) is shown.\n  P-A: on the lanes\n  P-B: under the COMBO\n  P-C: on the judge line\n  OFF: hidden",
            ["P-A", "P-B", "P-C", "OFF"],
            () => (int)CDTXMania.ConfigIni.JudgementStringPosition[Idx],
            v => CDTXMania.ConfigIni.JudgementStringPosition[Idx] = (EType)v));
        items.Add(ClassicJudgementItem());
        items.Add(Choice("AttackEffect", "アタックエフェクトの表示 / 非表示。", "Toggle the attack effect.",
            ["ON", "OFF"],
            () => (int)CDTXMania.ConfigIni.eAttackEffect[Idx],
            v => CDTXMania.ConfigIni.eAttackEffect[Idx] = (EType)v));

        items.Add(GraphItem(player == EPlayer.Guitar1 ? EPlayer.Guitar2 : EPlayer.Guitar1));

        return items;
    }
}
