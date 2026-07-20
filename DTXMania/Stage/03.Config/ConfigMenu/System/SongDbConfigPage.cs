using DTXMania.Core;
using DTXMania.SongDb;
using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

internal sealed class SongDbConfigPage : ConfigPage
{
    public SongDbConfigPage(ConfigList list) : base(list)
    {
    }

    public override List<CItemBase> Build()
    {
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
}
