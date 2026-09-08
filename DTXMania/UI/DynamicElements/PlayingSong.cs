using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;

namespace DTXMania.UI.DynamicElements;

/// <summary>
/// The chart on its way to being played, on the global data context as <c>"Song"</c>. Read live, so an
/// element bound to it shows what is loaded now rather than what was loaded when the layout was saved.
/// </summary>
public sealed class PlayingSong
{
    //the list's own name wins when the chart has no title of its own, or when the config asks for it
    [DataField]
    public string Title
    {
        get
        {
            CDTX? chart = CDTXMania.DTX;

            if (chart == null)
            {
                return string.Empty;
            }

            bool preferListName = !CDTXMania.bCompactMode && CDTXMania.ConfigIni.b曲名表示をdefのものにする;

            return string.IsNullOrEmpty(chart.TITLE) || preferListName
                ? CDTXMania.chosenSong?.title ?? string.Empty
                : chart.TITLE;
        }
    }

    [DataField] public string Artist => CDTXMania.DTX?.ARTIST ?? string.Empty;

    [DataField] public bool HasTitle => Title.Length > 0;
    [DataField] public bool HasArtist => Artist.Length > 0;

    //eg: 1st STAGE, 2nd STAGE
    [DataField] public string StageNumber => StageNumberText(CDTXMania.nStageNumber);

    //the preview image, or the stand-in for a song without one. Cached by path: a provider is read on
    //every draw that binds to it
    private BaseTexture? albumArt;
    private string albumArtPath = string.Empty;

    public BaseTexture AlbumArt
    {
        get
        {
            string path = CDTXMania.DTX == null
                ? string.Empty
                : CDTXMania.DTX.strFolderName + CDTXMania.DTX.PREIMAGE;

            if (!File.Exists(path))
            {
                path = CSkin.Path(@"Graphics\5_preimage default.png");
            }

            if (path != albumArtPath)
            {
                albumArtPath = path;
                albumArt = BaseTexture.LoadFromPath(path);
            }

            return albumArt ?? BaseTexture.None;
        }
    }

    private static string StageNumberText(int stage) => stage switch
    {
        1 => "1st STAGE",
        2 => "2nd STAGE",
        3 => "3rd STAGE",
        > 3 => $"{stage}th STAGE",
        _ => "EXTRA STAGE"
    };
}
