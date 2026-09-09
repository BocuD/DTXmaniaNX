using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

/// <summary>One folder from <see cref="SongPathsPage"/>: the path itself, a picker for it, and a delete.</summary>
internal sealed class SongPathEntryPage : ConfigPage
{
    private readonly SongPathsPage paths;
    private readonly int index;

    public SongPathEntryPage(ConfigList list, SongPathsPage paths, int index) : base(list)
    {
        this.paths = paths;
        this.index = index;
    }

    public override List<CItemBase> Build()
    {
        CItemTextInput path = TextInput("Path", paths.PathAt(index),
            "曲を検索するフォルダのパス。",
            "The folder to search for songs in.",
            () => paths.PathAt(index), value => paths.Replace(index, value));

        //the row shows what is stored, so an edit that is refused does not leave a stale value on it
        path.formatValue = () => paths.PathAt(index);
        path.action = () => paths.Replace(index, path.strCurrentValue);

        return
        [
            BackItem(),
            path,
            new CItemBase("Pick Folder", CItemBase.EPanelType.Normal,
                "フォルダ選択ダイアログでパスを選びます。",
                "Choose the folder with the system folder picker.")
            {
                action = () => Pick(path)
            },
            new CItemBase("Delete", CItemBase.EPanelType.Normal,
                "このフォルダを一覧から削除します。",
                "Remove this folder from the list.")
            {
                action = Delete
            }
        ];
    }

    private void Pick(CItemTextInput path)
    {
        if (SongPathsPage.PickFolder(paths.PathAt(index)) is not { } picked)
        {
            return;
        }

        paths.Replace(index, picked);

        //the field opens on its own copy of the value, so the pick has to reach that too
        path.strCurrentValue = paths.PathAt(index);
        list.RefreshValues();
    }

    private void Delete()
    {
        paths.Remove(index);
        list.Back();
    }
}
