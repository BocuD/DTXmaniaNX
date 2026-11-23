using DTXMania.Core;
using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

internal sealed class DrumsDisplayConfigPage : PlayerConfigPage
{
    public DrumsDisplayConfigPage(ConfigList list) : base(list, EPlayer.Drums)
    {
    }

    public override List<CItemBase> Build()
    {
        List<CItemBase> items = [];

        items.Add(BackItem());

        AddDisplayBlock(items); // Dark + LaneDisp + JudgeLineDisp + LaneFlush

        items.Add(Choice("JudgePosition",
            "判定文字(Perfect/Great等)の表示位置。\nP-A: レーン上。\nP-B: 判定ライン下。\nOFF: 表示しない。",
            "Where the judgement text (Perfect/Great/...) shows.\nP-A: on the lanes.\nP-B: under the hit bar.\nOFF: hidden.",
            ["P-A", "P-B", "OFF"],
            () => (int)CDTXMania.ConfigIni.JudgementStringPosition.Drums, v => CDTXMania.ConfigIni.JudgementStringPosition.Drums = (EType)v));
        items.Add(ClassicJudgementItem());
        items.Add(Toggle("Combo", "OFFにするとコンボが表示されなくなります。", "Turn ON the Drums Combo Display.",
            () => CDTXMania.ConfigIni.bドラムコンボ文字の表示, v => CDTXMania.ConfigIni.bドラムコンボ文字の表示 = v));
        items.Add(GraphItem());

        items.Add(Choice("AttackEffect",
            "アタックエフェクトの表示。\nALL ON: 全て表示。\nChipOFF: チップエフェクトのみ非表示。\nEffectOnly: エフェクト画像以外を非表示。\nALL OFF: 全て非表示。",
            "Attack effect display.\nALL ON: show all.\nChipOFF: hide the chip effect only.\nEffectOnly: hide all but the effect image.\nALL OFF: hide all.",
            ["ALL ON", "ChipOFF", "EffectOnly", "ALL OFF"],
            () => (int)CDTXMania.ConfigIni.eAttackEffect.Drums, v => CDTXMania.ConfigIni.eAttackEffect.Drums = (EType)v));
        items.Add(Toggle("ClassicFire", "チップファイアを旧表示にします。", "Shows the classic chip fire.",
            () => CDTXMania.ConfigIni.bClassicDrumsFire, v => CDTXMania.ConfigIni.bClassicDrumsFire = v));
        items.Add(Toggle("FillIn", "フィルインエフェクトの使用。", "Show bursting effects at the fill-in zone.",
            () => CDTXMania.ConfigIni.bFillInEnabled, v => CDTXMania.ConfigIni.bFillInEnabled = v));

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

        return items;
    }
}
