using System.Diagnostics;
using DTXMania.Core;
using DTXMania.SongDb;
using DTXMania.UI.Config;
using DTXMania.UI.Drawable;
using DTXMania.UI.Item;

namespace DTXMania;

internal sealed class SongDbConfigPage : ConfigPage
{
    public SongDbConfigPage(ConfigList list) : base(list)
    {
    }

    private bool isDirty = false;

    public override List<CItemBase> Build()
    {
        isDirty = false;
        List<CItemBase> items = [];

        var paths = TextInput("Song Database Paths", CDTXMania.ConfigIni.strSongDataSearchPath, 
            "演奏データの格納されているフォルダへのパス。\n" +
            "セミコロン(;)で区切ることにより複数のパスを指定できます。\n例: d:\\DTXFiles1\\;e:\\DTXFiles2\\",
            "Path for DTX data.\n" +
            "You can add multiple paths separated with semicolon(;)\nFor example: d:\\DTXFiles1\\;e:\\DTXFiles2\\",
            () => CDTXMania.ConfigIni.strSongDataSearchPath, s => CDTXMania.ConfigIni.strSongDataSearchPath = s);
        items.Add(paths);
        
        var autoExtractSongs = EnumChoice("Auto Extract Songs", "ZIPファイルから楽曲を抽出する", "Whether to extract songs from ZIP files.", 
            () => CDTXMania.ConfigIni.eUnpackSongs, e => CDTXMania.ConfigIni.eUnpackSongs = e);
        items.Add(autoExtractSongs);
        
        var songSortMode = EnumChoice("Sort Mode", "曲リストのソート方法", "Sort mode for song list.",
            () => CDTXMania.ConfigIni.baseSortMode, e => CDTXMania.ConfigIni.baseSortMode = e);
        items.Add(songSortMode);
        songSortMode.action = () => isDirty = true;
        
        items.Add(new CItemBase("Reload Songs (Full)", CItemBase.EPanelType.Normal,
            "曲データのキャッシュをクリアして、曲データを完全に再読み込みします。",
            "Clear song data cache and perform full reload of song data.")
        {
            action = () =>
            {
                if (CDTXMania.SongDb.status == SongDbScanStatus.Idle)
                {
                    CDTXMania.SongDb.ClearCache();
                    CDTXMania.SongDb.StartScan(() => CDTXMania.StageManager.stageSongSelectionNew.Reload());
                }
            }
        });
        
        items.Add(BackItem());
        return items;
    }

    public override void ApplyPendingChanges()
    {
        base.ApplyPendingChanges();

        if (isDirty)
        {
            AskToReloadSongs();
        }
    }

    private async Task AskToReloadSongs()
    {
        string title = CDTXMania.isJapanese ? "曲データの再読み込み" : "Reload Songs";
        string description = CDTXMania.isJapanese ? "曲データのソート方法を変更しました。\n曲データを再読み込みしますか？" :
            "You have changed the sort mode for song data.\nDo you want to reload the song data?";
        string[] options = CDTXMania.isJapanese ? ["はい", "いいえ"] : ["Yes", "No"];
						
        int choice = await Modal.ShowAsync(
            CDTXMania.persistentUIGroup,
            title,
            description,
            options);

        if (choice == 0)
            CDTXMania.SongDb.StartScan(() => CDTXMania.StageManager.stageSongSelectionNew.Reload());
        else
            Trace.TraceInformation("User chose not to reload song data.");
    }
}
