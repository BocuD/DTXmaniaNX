using DTXMania.Core;
using DTXMania.SongDb;
using DTXMania.UI.Config;
using DTXMania.UI.Drawable;
using DTXMania.UI.Item;

namespace DTXMania;

internal sealed class QuickMenuPage : PlayerConfigPage
{
    private readonly InstrumentOptionsPage options;

    public QuickMenuPage(ConfigList list, EPlayer player) : base(list, player)
    {
        options = InstrumentOptionsPage.For(list, player);
    }

    public override List<CItemBase> Build()
    {
        List<CItemBase> items = [CloseItem()];

        items.AddRange(options.Build());

        //not per player, so they are not among the shared options. This is where they get changed
        items.Add(GameplayConfigPage.RiskyItem());
        items.Add(GameplayConfigPage.PlaySpeedItem());

        items.Add(FullConfigItem());
        items.Add(ReloadSongsItem());

        return items;
    }

    private CItemBase CloseItem() => new("Close", CItemBase.EPanelType.Return,
        "メニューを閉じる",
        "Close the quick config menu.")
    {
        action = list.Back
    };

    private static CItemBase FullConfigItem() => new("Open Full Config", CItemBase.EPanelType.Folder,
        "コンフィグ画面を開きます。",
        "Open the full configuration screen.")
    {
        action = () => GitaDoraTransition.Close(0,
            () => CDTXMania.StageManager.tChangeStage(CDTXMania.StageManager.stageConfig))
    };

    private static CItemBase ReloadSongsItem() => new("Reload Songs", CItemBase.EPanelType.Normal,
        "曲データの一覧情報を取得し直します。\n新しい曲を追加したときや、曲データを\n更新したときに使用してください。",
        "Scan for new songs and update song list.")
    {
        action = ReloadSongs
    };

    private static void ReloadSongs()
    {
        SongDb.SongDb? songDb = CDTXMania.SongDb;

        //a scan already running is left to finish
        if (songDb == null || songDb.status != SongDbScanStatus.Idle)
        {
            return;
        }

        GitaDoraTransition.Close(0,
            () => songDb.StartScan(() => CDTXMania.StageManager.stageSongSelectionNew.Reload()));
    }
}
