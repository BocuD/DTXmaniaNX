using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

internal class QuickConfigPlayerSwitcher(ConfigList list, QuickMenuPage[] playerPages) : ConfigPage(list)
{
    public override List<CItemBase> Build()
    {
        var items = new List<CItemBase>
        {
            PlayerFolder("Drums", playerPages[0]), 
            PlayerFolder("Guitar P1", playerPages[1]), 
            PlayerFolder("Guitar P2", playerPages[2]), 
            BackItem()
        };

        return items;
    }
    
    protected CItemBase PlayerFolder(string name, ConfigPage target)
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