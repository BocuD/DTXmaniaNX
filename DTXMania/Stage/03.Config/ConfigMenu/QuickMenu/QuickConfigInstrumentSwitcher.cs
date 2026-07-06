using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

internal class QuickConfigInstrumentSwitcher(ConfigList list, QuickMenuPage[] instrumentPages) : ConfigPage(list)
{
    public override List<CItemBase> Build()
    {
        var items = new List<CItemBase>
        {
            InstrumentFolder("Drums", instrumentPages[0]), 
            InstrumentFolder("Guitar P1", instrumentPages[1]), 
            InstrumentFolder("Guitar P2", instrumentPages[2]), 
            BackItem()
        };

        return items;
    }
    
    protected CItemBase InstrumentFolder(string name, ConfigPage target)
    {
        var switcherButton = new CItemBase(name, CItemBase.EPanelType.Folder,
            "モードを変更する",
            "Change modes")
        {
            action = () =>
            {
                list.SetItems(target.Build());
                list.pageStack.Clear();
            }
        };
        return switcherButton;
    }
}