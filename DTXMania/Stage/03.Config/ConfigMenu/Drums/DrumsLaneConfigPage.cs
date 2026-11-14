using DTXMania.Core;
using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

internal sealed class DrumsLaneConfigPage : PlayerConfigPage
{
    public DrumsLaneConfigPage(ConfigList list) : base(list, EPlayer.Drums)
    {
    }

    public override List<CItemBase> Build()
    {
        List<CItemBase> items = [];

        items.Add(BackItem());

        items.Add(Choice("LaneType",
            "ドラムのレーン配置。\nType-A: 通常。\nType-B: 2ペダルとタムをまとめる。\nType-C: 3タムのみまとめる。\nType-D: 左右対称配置。",
            "Drum lane layout.\nType-A: default.\nType-B: 2 pedals and toms grouped.\nType-C: 3 toms grouped only.\nType-D: fully symmetric layout.",
            ["Type-A", "Type-B", "Type-C", "Type-D"],
            () => (int)CDTXMania.ConfigIni.eLaneType.Drums, v => CDTXMania.ConfigIni.eLaneType.Drums = (EType)v));
        items.Add(Choice("NumOfLanes",
            "レーン数。\n10: 標準。\n9: XG仕様。\n6: CLASSIC仕様。",
            "Number of lanes.\n10: default.\n9: XG style.\n6: classic style.",
            ["10", "9", "6"],
            () => (int)CDTXMania.ConfigIni.eNumOfLanes.Drums, v => CDTXMania.ConfigIni.eNumOfLanes.Drums = (EType)v));
        items.Add(Toggle("LaneGaps",
            "レーン数を減らしたとき、使わないレーンの幅を隙間として残します。",
            "Keeps the width of the lanes a smaller lane count leaves out as a gap,\nrather than closing the lanes up.",
            () => CDTXMania.ConfigIni.bDrumsLaneGaps, v => CDTXMania.ConfigIni.bDrumsLaneGaps = v));
        items.Add(EnumChoice("RDPosition",
            "ライドシンバルレーンの位置。\nRD RC: 最右端がRC。\nRC RD: 最右端がRD。",
            "Ride cymbal lane placement.\nRD RC: rightmost lane is RC.\nRC RD: rightmost lane is RD.",
            () => CDTXMania.ConfigIni.eRDPosition, v => CDTXMania.ConfigIni.eRDPosition = v));
        items.Add(Choice("DkdkType",
            "ツーバス(ドコドコ)譜面の割り当て方。\nL R: 標準。\nR L: 始動足を反転。\nR Only: 1レーンにまとめる。",
            "How double-bass chips are laid out.\nL R: default.\nR L: swaps the starting foot.\nR Only: puts them on a single lane.",
            ["L R", "R L", "R Only"],
            () => (int)CDTXMania.ConfigIni.eDkdkType.Drums, v => CDTXMania.ConfigIni.eDkdkType.Drums = (EType)v));
        items.Add(Toggle("AssignToLBD",
            "旧仕様のツーバス譜面の一部バスチップをLBDレーンに振り分けます。\nLP/LBDチップを持つ譜面では無効。",
            "Moves some bass chips of old-style 2-bass charts onto the LBD lane.\nIgnored on charts that already have LP/LBD chips.",
            () => CDTXMania.ConfigIni.bAssignToLBD.Drums, v => CDTXMania.ConfigIni.bAssignToLBD.Drums = v));

        items.Add(JudgeLinePosItem());

        return items;
    }
}
