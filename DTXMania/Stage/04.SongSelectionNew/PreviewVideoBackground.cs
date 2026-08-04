using System.Diagnostics;
using System.Numerics;
using System.Text;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.Core.Video;
using DTXMania.UI.Drawable;
using FDK;

namespace DTXMania;

/// <summary>
/// Song-select background layer that follows the current selection, the way
/// <see cref="CActSelectPresound"/> does for the preview sound: it debounces on the same wait timer, only
/// committing once the list has stopped scrolling, then picks a background for the selected chart:
///   1. the chart's preview movie (PREMOVIE), played from the start,
///   2. otherwise its background video (the AVI chip on the movie channel), seeked past the intro,
///   3. otherwise its static background image,
///   4. otherwise nothing.
///
/// Two independent layers ping-pong: the incoming source loads on the idle "back" layer and fades in over
/// the still-visible "front" layer, so the ambient background never shows through mid-load. When the fade
/// completes the front is hidden and the two swap roles. A selection with no background fades the front out.
/// </summary>
public class PreviewVideoBackground : UIGroup
{
    //where a full background video starts, to skip intros and initial black frames. Preview movies are
    //short and deliberate, so they play from the very start
    private const double BackgroundVideoStartSeconds = 5.0;
    private const int FadeDurationMs = 400;

    //the background fills the virtual render area regardless of the source's native resolution
    private static readonly Vector2 BackgroundSize = new(1280, 720);

    private enum SourceKind
    {
        None,
        Video,
        Image
    }

    private readonly struct BackgroundSource
    {
        public readonly SourceKind Kind;
        public readonly string Path;
        public readonly bool SeekIntoClip; //video only

        public BackgroundSource(SourceKind kind, string path, bool seekIntoClip)
        {
            Kind = kind;
            Path = path;
            SeekIntoClip = seekIntoClip;
        }

        public static readonly BackgroundSource None = new(SourceKind.None, string.Empty, false);

        public string Key => $"{Kind}|{Path}";
    }

    private enum FadeMode
    {
        None,
        CrossIn, //back layer fades 0 -> 1 on top of front
        FadeOut  //front layer fades 1 -> 0, with nothing coming in
    }

    //one layer owns a video player and an image, of which at most one is shown at a time
    private sealed class Layer
    {
        private readonly UINewVideoRenderer video;
        private readonly UIImage image;

        private SourceKind kind = SourceKind.None;
        private string sourceKey = string.Empty;
        //backs the static-image case; disposed when replaced so selections don't leak
        private BaseTexture? imageTexture;

        public Layer(string name)
        {
            video = new UINewVideoRenderer
            {
                name = name + "Video",
                isVisible = false,
                dontSerialize = true,
                size = BackgroundSize,
            };
            video.color.Alpha = 0f;

            image = new UIImage
            {
                name = name + "Image",
                isVisible = false,
                dontSerialize = true,
                size = BackgroundSize,
            };
            image.color.Alpha = 0f;
        }

        public SourceKind Kind => kind;
        public string SourceKey => sourceKey;

        public bool Show(BackgroundSource source)
        {
            bool ok = source.Kind switch
            {
                SourceKind.Video => LoadVideo(source.Path, source.SeekIntoClip),
                SourceKind.Image => LoadImage(source.Path),
                _ => false
            };

            if (!ok)
            {
                Hide();
                return false;
            }

            sourceKey = source.Key;
            return true;
        }

        private bool LoadVideo(string path, bool seekIntoClip)
        {
            //LoadVideo also returns false when video playback is disabled in the config
            if (!video.LoadVideo(path))
            {
                return false;
            }

            //LoadVideo resets the size to the video's native resolution
            video.size = BackgroundSize;

            //SyncToSeconds rather than SeekToSeconds/ForceSeekAndRender, which decode up to the target on
            //the main thread and cause a song-switch hitch. This only flushes and issues the demuxer seek,
            //leaving the catch-up to the decoder thread; the fade hides the brief transition
            if (seekIntoClip)
            {
                double duration = video.Controller.CurrentFrame.TotalDurationSeconds;
                if (duration > BackgroundVideoStartSeconds + 1.0)
                {
                    video.Controller.SyncToSeconds(BackgroundVideoStartSeconds);
                }
            }

            kind = SourceKind.Video;
            image.isVisible = false;
            video.isVisible = true;
            return true;
        }

        private bool LoadImage(string path)
        {
            BaseTexture tex = BaseTexture.LoadFromPath(path);
            if (!tex.IsValid())
            {
                tex.Dispose();
                return false;
            }

            image.SetTexture(tex, updateRects: true, updateSize: false);
            image.size = BackgroundSize;
            imageTexture?.Dispose();
            imageTexture = tex;

            kind = SourceKind.Image;
            video.isVisible = false;
            image.isVisible = true;
            return true;
        }

        public void Hide()
        {
            video.isVisible = false;
            image.isVisible = false;
            video.color.Alpha = 0f;
            image.color.Alpha = 0f;
            kind = SourceKind.None;
            sourceKey = string.Empty;
            imageTexture?.Dispose();
            imageTexture = null;
        }

        public void SetAlpha(float alpha)
        {
            switch (kind)
            {
                case SourceKind.Video:
                    video.color.Alpha = alpha;
                    break;
                case SourceKind.Image:
                    image.color.Alpha = alpha;
                    break;
            }
        }

        public void Draw(Matrix4x4 combined)
        {
            switch (kind)
            {
                case SourceKind.Video:
                    video.Draw(combined);
                    break;
                case SourceKind.Image:
                    image.Draw(combined);
                    break;
            }
        }

        public void Dispose()
        {
            video.Dispose();
            image.Dispose();
            imageTexture?.Dispose();
            imageTexture = null;
        }
    }

    //front is fully shown or empty; back is the idle buffer the next source loads into
    private Layer front;
    private Layer back;

    //the chart we want once the debounce elapses, versus the one currently shown
    private CChartData? pendingChart;
    private string? pendingChartPath;
    private string? loadedChartPath;

    private FadeMode fadeMode = FadeMode.None;
    private CCounter? ctWaitForPlayback;
    private CCounter? ctFade;

    public PreviewVideoBackground() : base("PreviewVideoBackground")
    {
        dontSerialize = true;

        front = new Layer("PreviewA");
        back = new Layer("PreviewB");
    }

    //arms the debounce, so the potentially heavy resolve and load only happens once scrolling has settled
    public void SelectionChanged(CChartData? chart)
    {
        string? chartPath = chart?.FileInformation.AbsoluteFilePath;

        //already showing, and heading toward, this exact chart
        if (string.Equals(chartPath, loadedChartPath, StringComparison.Ordinal) &&
            string.Equals(chartPath, pendingChartPath, StringComparison.Ordinal))
        {
            return;
        }

        pendingChart = chart;
        pendingChartPath = chartPath;

        //the same wait the preview sound uses, so video and sound settle together
        ctWaitForPlayback = new CCounter(0, CDTXMania.ConfigIni.nSongSelectSoundPreviewWaitTimeMs, 1, CDTXMania.Timer);
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        if (!isVisible)
        {
            return;
        }

        UpdateDebounce();
        UpdateFade();

        UpdateLocalTransformMatrix();
        Matrix4x4 combined = localTransformMatrix * parentMatrix;

        //front first, so during a CrossIn the incoming layer draws over the outgoing one
        front.Draw(combined);
        back.Draw(combined);
    }

    private void UpdateDebounce()
    {
        if (ctWaitForPlayback == null || ctWaitForPlayback.bStopped)
        {
            return;
        }

        ctWaitForPlayback.tUpdate();
        if (!ctWaitForPlayback.bReachedEndValue)
        {
            return;
        }

        ctWaitForPlayback.tStop();

        //don't churn backgrounds while the list is still moving
        if (CDTXMania.StageManager.stageSongSelectionNew.isScrolling)
        {
            ctWaitForPlayback = new CCounter(0, CDTXMania.ConfigIni.nSongSelectSoundPreviewWaitTimeMs, 1, CDTXMania.Timer);
            return;
        }

        CommitPending();
    }

    private void CommitPending()
    {
        loadedChartPath = pendingChartPath;

        BackgroundSource source = ResolveSource(pendingChart);

        //snap an in-progress fade to completion, so front is the fully-shown layer before we decide what
        //to do next. Keeps the two roles consistent under rapid selection changes
        if (fadeMode != FadeMode.None)
        {
            FinishFade();
        }

        //already showing exactly this source
        if (string.Equals(source.Key, front.SourceKey, StringComparison.Ordinal))
        {
            return;
        }

        //nothing to show, or the incoming source failed to load, so fade the current front out
        if (source.Kind == SourceKind.None || !back.Show(source))
        {
            if (front.Kind == SourceKind.None)
            {
                return; //already empty
            }

            fadeMode = FadeMode.FadeOut;
            ctFade = new CCounter(0, Math.Max(1, FadeDurationMs), 1, CDTXMania.Timer);
            return;
        }

        //the incoming source is loaded on the back layer, so fade it in over the front
        back.SetAlpha(0f);
        fadeMode = FadeMode.CrossIn;
        ctFade = new CCounter(0, Math.Max(1, FadeDurationMs), 1, CDTXMania.Timer);
    }

    private void UpdateFade()
    {
        if (fadeMode == FadeMode.None || ctFade == null || ctFade.bStopped)
        {
            return;
        }

        ctFade.tUpdate();
        float t = Math.Clamp(ctFade.nCurrentValue / (float)ctFade.nEndValue, 0f, 1f);

        if (fadeMode == FadeMode.CrossIn)
        {
            back.SetAlpha(t);
        }
        else //FadeOut
        {
            front.SetAlpha(1f - t);
        }

        if (ctFade.bReachedEndValue)
        {
            FinishFade();
        }
    }

    private void FinishFade()
    {
        switch (fadeMode)
        {
            case FadeMode.CrossIn:
                back.SetAlpha(1f);
                front.Hide();
                (front, back) = (back, front); //the incoming layer becomes the new front
                break;

            case FadeMode.FadeOut:
                front.Hide();
                break;
        }

        fadeMode = FadeMode.None;
        ctFade = null;
    }

    private static BackgroundSource ResolveSource(CChartData? chart)
    {
        if (chart == null)
        {
            return BackgroundSource.None;
        }

        string folder = chart.FileInformation.AbsoluteFolderPath;

        //video sources only when playback is enabled, otherwise fall through to the static image
        if (CDTXMania.ConfigIni.bAVIEnabled)
        {
            //1. preview movie, played from the start
            string premovie = chart.SongInformation.Premovie;
            if (!string.IsNullOrEmpty(premovie))
            {
                string path = folder + premovie;
                if (File.Exists(path))
                {
                    return new BackgroundSource(SourceKind.Video, path, seekIntoClip: false);
                }
            }

            //2. background video defined as an AVI chip on the movie channel, seeked past the intro
            string? backgroundVideo = FindBackgroundVideo(chart.FileInformation.AbsoluteFilePath, folder);
            if (backgroundVideo != null)
            {
                return new BackgroundSource(SourceKind.Video, backgroundVideo, seekIntoClip: true);
            }
        }

        //3. static background image
        string background = chart.SongInformation.Backgound;
        if (!string.IsNullOrEmpty(background))
        {
            string path = folder + background;
            if (File.Exists(path))
            {
                return new BackgroundSource(SourceKind.Image, path, seekIntoClip: false);
            }
        }

        //4. nothing
        return BackgroundSource.None;
    }

    //movie-channel codes as they appear in a chart body; EChannel.Movie = 0x54, MovieFull = 0x5A
    private const string MovieChannel = "54";
    private const string MovieFullChannel = "5A";

    //a text scan rather than the full CDTX parser, since all we need is the AVI definitions and the first
    //movie-channel placement. Returns null unless the referenced file exists
    private static string? FindBackgroundVideo(string chartFilePath, string folder)
    {
        if (string.IsNullOrEmpty(chartFilePath) || !File.Exists(chartFilePath))
        {
            return null;
        }

        string[] lines;
        try
        {
            //chart files are Shift-JIS; the provider is registered at startup in Program.cs
            lines = File.ReadAllLines(chartFilePath, Encoding.GetEncoding("shift-jis"));
        }
        catch
        {
            return null;
        }

        Dictionary<string, string> aviDefs = new(StringComparer.OrdinalIgnoreCase);
        string? referencedSlot = null;

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (line.Length < 6 || line[0] != '#')
            {
                continue;
            }

            //strip trailing comments
            int comment = line.IndexOf(';');
            if (comment >= 0)
            {
                line = line.Substring(0, comment).TrimEnd();
                if (line.Length < 6)
                {
                    continue;
                }
            }

            //#AVI<slot><sep><filename>, skipping #AVIPAN/#AVISEL etc, which have no separator at [6]
            if (line.Length >= 7 &&
                line.StartsWith("#AVI", StringComparison.OrdinalIgnoreCase) &&
                (line[6] == ':' || char.IsWhiteSpace(line[6])))
            {
                string slot = line.Substring(4, 2);
                if (IsAlphaNumeric(slot[0]) && IsAlphaNumeric(slot[1]))
                {
                    string value = line.Substring(6).Trim();
                    if (value.StartsWith(':'))
                    {
                        value = value.Substring(1).Trim();
                    }

                    if (value.Length > 0)
                    {
                        aviDefs[slot] = value;
                    }
                }

                continue;
            }

            //#<measure:3><channel:2><sep>data; take the first movie-channel placement
            if (referencedSlot == null &&
                char.IsDigit(line[1]) && char.IsDigit(line[2]) && char.IsDigit(line[3]))
            {
                string channel = line.Substring(4, 2);
                if (channel.Equals(MovieChannel, StringComparison.OrdinalIgnoreCase) ||
                    channel.Equals(MovieFullChannel, StringComparison.OrdinalIgnoreCase))
                {
                    int colon = line.IndexOf(':', 6);
                    string data = (colon >= 0 ? line.Substring(colon + 1) : line.Substring(6));
                    referencedSlot = FirstNonZeroObject(data);
                }
            }
        }

        if (referencedSlot != null && aviDefs.TryGetValue(referencedSlot, out string? filename))
        {
            string path = folder + filename;
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    //channel object data is a run of 2-char base-36 slots, where "00" means no object
    private static string? FirstNonZeroObject(string data)
    {
        var compact = new StringBuilder(data.Length);
        foreach (char c in data)
        {
            if (IsAlphaNumeric(c))
            {
                compact.Append(c);
            }
        }

        for (int i = 0; i + 1 < compact.Length; i += 2)
        {
            string obj = compact.ToString(i, 2);
            if (obj != "00")
            {
                return obj;
            }
        }

        return null;
    }

    private static bool IsAlphaNumeric(char c)
    {
        return (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
    }

    public override void Dispose()
    {
        front.Dispose();
        back.Dispose();
        base.Dispose();
    }
}
