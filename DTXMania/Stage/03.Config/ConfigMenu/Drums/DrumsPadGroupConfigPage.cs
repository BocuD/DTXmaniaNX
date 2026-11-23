using DTXMania.Core;
using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

internal sealed class DrumsPadGroupConfigPage : PlayerConfigPage
{
    public DrumsPadGroupConfigPage(ConfigList list) : base(list, EPlayer.Drums)
    {
    }

    public override List<CItemBase> Build()
    {
        List<CItemBase> items = [];

        items.Add(BackItem());

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

        return items;
    }
}
