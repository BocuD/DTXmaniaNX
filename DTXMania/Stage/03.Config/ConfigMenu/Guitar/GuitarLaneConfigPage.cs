using DTXMania.Core;
using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

internal sealed class GuitarLaneConfigPage : PlayerConfigPage
{
    public GuitarLaneConfigPage(ConfigList list, EPlayer player) : base(list, player)
    {
    }

    public override List<CItemBase> Build()
    {
        List<CItemBase> items = [];

        items.Add(BackItem());

        items.Add(LeftItem());
        items.Add(JudgeLinePosItem());

        return items;
    }
}
