using System.Windows.Forms;
using DTXMania.Core;
using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

/// <summary>
/// The folders the song scan looks in, one row per path. Config.ini holds them as a single semicolon
/// separated string, which this page is the only thing to split and join.
/// </summary>
internal sealed class SongPathsPage : ConfigPage
{
    private const char Separator = ';';

    //a row's name has room for about this many characters before the value column starts
    private const int NameLength = 28;

    private readonly Action markDirty;

    public SongPathsPage(ConfigList list, Action markDirty) : base(list)
    {
        this.markDirty = markDirty;
    }

    public override bool RebuildsOnReturn => true;

    public int Count => Paths().Count;

    public string CountLabel
    {
        get
        {
            int count = Count;
            return CDTXMania.isJapanese ? $"{count}個" : count == 1 ? "1 path" : $"{count} paths";
        }
    }

    public string PathAt(int index)
    {
        List<string> paths = Paths();
        return index < paths.Count ? paths[index] : string.Empty;
    }

    public override List<CItemBase> Build()
    {
        List<CItemBase> items = [BackItem()];
        List<string> paths = Paths();

        for (int i = 0; i < paths.Count; i++)
        {
            int index = i;
            items.Add(FolderItem(Shorten(paths[index]), paths[index], paths[index],
                () => new SongPathEntryPage(list, this, index)));
        }

        items.Add(new CItemBase("Add Path", CItemBase.EPanelType.Normal,
            "曲を検索するフォルダを追加します。",
            "Add a folder to search for songs in.")
        {
            action = Add
        });

        return items;
    }

    public void Replace(int index, string path)
    {
        //an empty path is scanned as the drive root, so clearing one is not how a row goes away
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        List<string> paths = Paths();

        if (index >= paths.Count)
        {
            return;
        }

        paths[index] = path.Trim();
        Save(paths);
    }

    public void Remove(int index)
    {
        List<string> paths = Paths();

        if (index >= paths.Count)
        {
            return;
        }

        paths.RemoveAt(index);
        Save(paths);
    }

    /// <summary>Shows the system folder picker starting at <paramref name="start"/>; null if cancelled.</summary>
    public static string? PickFolder(string start)
    {
        using FolderBrowserDialog dialog = new()
        {
            UseDescriptionForTitle = true,
            Description = CDTXMania.isJapanese ? "曲フォルダの選択" : "Select a song folder",
            SelectedPath = start
        };

        return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : null;
    }

    private void Add()
    {
        if (PickFolder(string.Empty) is not { } picked)
        {
            return;
        }

        List<string> paths = Paths();
        paths.Add(picked);
        Save(paths);

        list.OpenFolder(new SongPathEntryPage(list, this, paths.Count - 1));
    }

    private static List<string> Paths() =>
    [
        ..CDTXMania.ConfigIni.strSongDataSearchPath.Split(Separator,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ];

    private void Save(List<string> paths)
    {
        string joined = string.Join(Separator, paths);

        if (joined == CDTXMania.ConfigIni.strSongDataSearchPath)
        {
            return;
        }

        CDTXMania.ConfigIni.strSongDataSearchPath = joined;
        markDirty();
    }

    private static string Shorten(string path)
        => path.Length <= NameLength ? path : "…" + path[^(NameLength - 1)..];
}
