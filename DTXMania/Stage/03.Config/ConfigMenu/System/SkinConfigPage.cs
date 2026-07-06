using System.Linq;
using DTXMania.Core;
using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

/// <summary>
/// The modern SkinManager-based skin selector plus the box-def skin toggle. The skin change is
/// applied on exit (see <see cref="ApplyPendingChanges"/>) to avoid rebuilding the whole UI mid-navigation.
/// NOTE: the legacy per-subfolder skin selector and its live sample preview are not ported here yet -
/// they trigger a full skin reload and need separate handling.
/// </summary>
internal sealed class SkinConfigPage : ConfigPage
{
    private CItemList skinSelector;

    public SkinConfigPage(ConfigList list) : base(list)
    {
    }

    public override List<CItemBase> Build()
    {
        List<CItemBase> items = [];

        CDTXMania.SkinManager.ScanSkinDirectory();
        string[] skinNames = CDTXMania.SkinManager.skins.Select(x => x.name).Prepend("None").ToArray();

        int currentIndex = 0; // 0 = None
        for (int i = 0; i < CDTXMania.SkinManager.skins.Count; i++)
        {
            if (CDTXMania.SkinManager.skins[i].name == CDTXMania.SkinManager.currentSkin?.name)
            {
                currentIndex = i + 1; // account for "None"
                break;
            }
        }

        skinSelector = new CItemList("Skin", CItemBase.EPanelType.Normal, currentIndex,
            "スキン切替：スキンを切り替えます。\n",
            "Choose skin (applied when leaving Config).",
            skinNames);
        skinSelector.BindConfig(() => { }); // applied on exit, no config write on change
        items.Add(skinSelector);

        CItemToggle useBoxDefSkin = new("Skin (Box)", CDTXMania.ConfigIni.bUseBoxDefSkin,
            "Music boxスキンの利用：\n特別なスキンが設定されたMusic box\nに出入りしたときに、自動でスキンを\n切り替えるかどうかを設定します。\n",
            "Box skin:\nAutomatically change skin as per box.def file.");
        useBoxDefSkin.BindConfig(
            () => useBoxDefSkin.bON = CDTXMania.ConfigIni.bUseBoxDefSkin,
            () =>
            {
                CDTXMania.ConfigIni.bUseBoxDefSkin = useBoxDefSkin.bON;
                CSkin.bUseBoxDefSkin = useBoxDefSkin.bON;
            });
        items.Add(useBoxDefSkin);

        items.Add(BackItem());
        return items;
    }

    public override void ApplyPendingChanges()
    {
        if (skinSelector == null) return; // page never opened

        int index = skinSelector.nCurrentlySelectedIndex;
        CDTXMania.SkinManager.ChangeSkin(index == 0 ? null : CDTXMania.SkinManager.skins[index - 1]); // 0 = None
    }
}
