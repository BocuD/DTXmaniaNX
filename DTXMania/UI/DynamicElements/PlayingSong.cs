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

    //looked up once per loaded chart, since checking the disk on every draw costs a real part of a frame
    private CDTX? preimageChart;
    private bool preimageChecked;
    private string preimagePath = string.Empty;

    public BaseTexture AlbumArt
    {
        get
        {
            CDTX? chart = CDTXMania.DTX;

            if (!preimageChecked || !ReferenceEquals(chart, preimageChart))
            {
                preimageChecked = true;
                preimageChart = chart;
                preimagePath = chart == null ? string.Empty : chart.strFolderName + chart.PREIMAGE;

                if (!File.Exists(preimagePath))
                {
                    preimagePath = string.Empty;
                }
            }

            string path = preimagePath.Length > 0 ? preimagePath : CSkin.Path(@"Graphics\5_preimage default.png");

            if (path != albumArtPath)
            {
                albumArtPath = path;
                albumArt = BaseTexture.LoadFromPath(path);
            }

            return albumArt ?? BaseTexture.None;
        }
    }

    private static string StageNumberText(int stage)
    {
        if (stage <= 0)
        {
            return "EXTRA STAGE";
        }

        return $"{stage}{StageSuffix(stage)} STAGE";
    }

    private static string StageSuffix(int n) => (n % 100) switch
    {
        11 or 12 or 13 => "th",
        _ => (n % 10) switch
        {
            1 => "st",
            2 => "nd",
            3 => "rd",
            _ => "th"
        }
    };
}
