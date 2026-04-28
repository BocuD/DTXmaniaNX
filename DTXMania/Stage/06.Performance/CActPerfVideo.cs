using System.Drawing;
using System.Numerics;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DTXMania.Core;
using DTXMania.Core.Video;
using DTXMania.UI.Drawable;
using FDK;

namespace DTXMania;

internal class CActPerfVideo : CActivity
{
    #region Public Methods

    public void Start(EChannel channel, CAVI avi, int startSizeW, int startSizeH, int endSizeW, int endSizeH,
        int imageStartX, int imageStartY, int imageEndX, int imageEndY,
        int displayStartX, int displayStartY, int displayEndX, int displayEndY,
        int totalMoveTimeMs, int moveStartTimeMs, bool playFromBeginning = false)
    {
        Trace.TraceInformation("CActPerfAVI: Start(): " + avi.strFileName);

        this.avi = avi;
        this.moveStartTimeMs = (moveStartTimeMs != -1) ? moveStartTimeMs : CSoundManager.rcPerformanceTimer.nCurrentTime;

        if (avi != null && avi.avi != null)
        {
            frameWidth = (uint)avi.avi.nフレーム幅;
            frameHeight = (uint)avi.avi.nフレーム高さ;
            aspectRatio = (float)frameWidth / (float)frameHeight;

            CreateVideoPlayer(avi.strFileName);

            //frame dimensions are known now, so we can lay out the renderers.
            ApplyLayout();
        }
    }

    public void SkipStart(int moveStartTimeMs)
    {
        if (CDTXMania.DTX == null)
            return;

        foreach (CChip chip in CDTXMania.DTX.listChip)
        {
            if (chip.nPlaybackTimeMs > moveStartTimeMs)
                break;

            switch (chip.eAVI種別)
            {
                case EAVIType.AVI:
                    if (chip.rAVI != null)
                    {
                        Start(chip.nChannelNumber, chip.rAVI, 1280, 720, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, chip.nPlaybackTimeMs);
                        SeekVideo(moveStartTimeMs - chip.nPlaybackTimeMs);
                    }
                    break;

                case EAVIType.AVIPAN:
                    if (chip.rAVIPan != null)
                    {
                        Start(chip.nChannelNumber, chip.rAVI,
                            chip.rAVIPan.sz開始サイズ.Width, chip.rAVIPan.sz開始サイズ.Height,
                            chip.rAVIPan.sz終了サイズ.Width, chip.rAVIPan.sz終了サイズ.Height,
                            chip.rAVIPan.pt動画側開始位置.X, chip.rAVIPan.pt動画側開始位置.Y,
                            chip.rAVIPan.pt動画側終了位置.X, chip.rAVIPan.pt動画側終了位置.Y,
                            chip.rAVIPan.pt表示側開始位置.X, chip.rAVIPan.pt表示側開始位置.Y,
                            chip.rAVIPan.pt表示側終了位置.X, chip.rAVIPan.pt表示側終了位置.Y,
                            chip.n総移動時間, chip.nPlaybackTimeMs);
                        SeekVideo(moveStartTimeMs - chip.nPlaybackTimeMs);
                    }
                    break;
            }
        }
    }

    public void Stop()
    {
        Trace.TraceInformation("CActPerfAVI: Stop()");
        moveStartTimeMs = -1;

        if (videoPlayer != null)
        {
            videoPlayer.Seek(TimeSpan.Zero);
        }
    }

    public void MovieMode()
    {
        currentMovieMode = CDTXMania.ConfigIni.nMovieMode;
        isFullScreen = (currentMovieMode == 1) || (currentMovieMode == 3);
        isWindowed = (currentMovieMode == 2) || (currentMovieMode == 3);

        //if a video is already loaded, re-apply layout for the new mode.
        //during OnActivate() this is a no-op since frame dimensions aren't known yet.
        if (frameWidth > 0 && frameHeight > 0)
            ApplyLayout();
    }

    public void Cont(int resumeTimeMs)
    {
        moveStartTimeMs = resumeTimeMs;
    }

    public void Start(bool fillIn)
    {
        //stop any running fill-in effects, then start a fresh one in slot 0.
        for (int i = 0; i < fillInEffects.Length; i++)
        {
            if (fillInEffects[i].isInUse)
            {
                fillInEffects[i].counter.tStop();
                fillInEffects[i].isInUse = false;
            }
        }

        fillInEffects[0].isInUse = true;
        fillInEffects[0].counter = new CCounter(0, 30, 30, CDTXMania.Timer);
    }

    public void IntegrateUI(UIGroup parentGroup)
    {
        if (parentGroup == null)
            return;

        uiGroup = new UIGroup("CActPerfAVI UI");

        //load clip panel texture (drums uses skill-meter-aware variants; guitar/bass use the C variant).
        bool isDrums = CDTXMania.GetCurrentInstrument() == 0;

        string clipPanelPath = !isDrums ? @"Graphics\7_ClipPanelC.png" :
                              CDTXMania.ConfigIni.bGraph有効.Drums ?
                              @"Graphics\7_ClipPanelB.png" : @"Graphics\7_ClipPanel.png";

        clipPanelTexture = BaseTexture.LoadFromPath(CSkin.Path(clipPanelPath));

        if (clipPanelTexture.isValid())
        {
            clipPanelImage = new UIImage(clipPanelTexture);
            clipPanelImage.isVisible = false;
            clipPanelImage.renderOrder = 1;
            uiGroup.AddChild(clipPanelImage);
        }
        
        for (int i = 0; i < fillInEffects.Length; i++)
        {
            fillInEffects[i] = new FillInEffect();
            fillInEffects[i].counter = new CCounter(0, 30, 30, CDTXMania.Timer);
            fillInEffects[i].isInUse = false;
        }

        parentGroup.AddChild(uiGroup);
    }

    #endregion

    #region CActivity Implementation

    public override void OnActivate()
    {
        moveStartTimeMs = -1;
        isPaused = false;
        MovieMode();

        base.OnActivate();
    }

    public int tUpdateAndDraw(int x, int y)
    {
        if (!bActivated)
            return 0;

        UpdateVideoSync();
        UpdateFillInEffects();
        UpdateVideoVisibility();

        HandlePauseToggle();

        return 0;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Returns true when the active instrument is guitar or bass.
    /// In DTXMania the perf screen treats them identically (the original used
    /// bGuitarEnabled which covered both); only drums has a distinct layout.
    /// </summary>
    private static bool IsGuitarOrBass()
    {
        int inst = CDTXMania.GetCurrentInstrument();
        return inst is 1 or 2;
    }

    private static bool IsDrums()
    {
        return CDTXMania.GetCurrentInstrument() == 0;
    }

    private void CreateVideoPlayer(string videoFileName)
    {
        videoPlayer = new ThreadedSoftwareVideoPlayer();

        string videoPath = GetVideoPath(videoFileName);
        if (File.Exists(videoPath) && videoPlayer.Open(videoPath))
        {
            videoPlayer.LoopOnEof = loop;

            //both renderers share the same player
            windowedRenderer = new UIVideoRenderer(videoPlayer, videoPath);
            windowedRenderer.isVisible = false;
            windowedRenderer.renderOrder = 2;
            windowedRenderer.name = "WindowedVideo";
            uiGroup.AddChild(windowedRenderer);

            fullscreenRenderer = new UIVideoRenderer(videoPlayer, videoPath);
            fullscreenRenderer.isVisible = false;
            fullscreenRenderer.name = "FullscreenVideo";
            uiGroup.AddChild(fullscreenRenderer);
        }
    }

    /// <summary>
    /// Computes positions and sizes for both renderers and the clip panel,
    /// and pushes them onto the UI elements. Called once after the video opens
    /// and again whenever MovieMode() changes the mode. Visibility is left to
    /// UpdateVideoVisibility() per frame.
    /// </summary>
    private void ApplyLayout()
    {
        if (avi == null || frameWidth == 0 || frameHeight == 0)
            return;

        //windowed layout (clip panel + small video).
        if (windowedRenderer != null && clipPanelImage != null)
        {
            var (panelX, panelY, scale, videoX, videoY) = CalculateWindowedPosition();
            clipPanelImage.position = new Vector3(panelX, panelY, 0);
            windowedRenderer.position = new Vector3(videoX, videoY, 0);
            windowedRenderer.size = new Vector2(frameWidth * scale, frameHeight * scale);
        }

        //fullscreen layout (large video, no panel).
        if (fullscreenRenderer != null)
        {
            var (positionX, positionY, scaleX, scaleY) = CalculateFullscreenPosition();
            fullscreenRenderer.position = new Vector3(positionX, positionY, 0);
            fullscreenRenderer.size = new Vector2(frameWidth * scaleX, frameHeight * scaleY);
        }
    }

    private void SeekVideo(long timeMs)
    {
        if (videoPlayer != null)
        {
            videoPlayer.Seek(TimeSpan.FromMilliseconds(timeMs));
        }
    }

    private void UpdateVideoSync()
    {
        if (videoPlayer == null || (windowedRenderer == null && fullscreenRenderer == null))
            return;

        long currentGameTime = CSoundManager.rcPerformanceTimer.nCurrentTime;
        long videoStartTime = moveStartTimeMs;

        if (videoStartTime == -1 || currentGameTime < videoStartTime)
            return;

        long videoPlayTimeMs = currentGameTime - videoStartTime;
        TimeSpan currentVideoTime = videoPlayer.CurrentTime;
        long currentVideoTimeMs = (long)currentVideoTime.TotalMilliseconds;

        //resync if drift exceeds 100ms.
        if (Math.Abs(videoPlayTimeMs - currentVideoTimeMs) > 100)
        {
            videoPlayer.Seek(TimeSpan.FromMilliseconds(videoPlayTimeMs));
        }

        //handle end-of-clip: stop or loop.
        TimeSpan duration = videoPlayer.Duration;
        if (duration > TimeSpan.Zero && currentVideoTime >= duration)
        {
            if (!isPreviewMovie && !loop)
            {
                moveStartTimeMs = -1;
            }
            else
            {
                videoPlayer.Seek(TimeSpan.Zero);
            }
        }
    }

    private void UpdateFillInEffects()
    {
        if (!CDTXMania.ConfigIni.DisplayBonusEffects)
            return;

        for (int i = 0; i < fillInEffects.Length; i++)
        {
            if (!fillInEffects[i].isInUse)
                continue;

            int frame = fillInEffects[i].counter.nCurrentValue;
            fillInEffects[i].counter.tUpdate();

            if (fillInEffects[i].counter.bReachedEndValue)
            {
                fillInEffects[i].counter.tStop();
                fillInEffects[i].isInUse = false;
                continue;
            }

            CStagePerfDrumsScreen stageDrum = CDTXMania.stagePerfDrumsScreen;

            if (IsDrums() && stageDrum.txBonusEffect != null)
            {
                stageDrum.txBonusEffect.blendMode = BlendMode.Additive;
                stageDrum.txBonusEffect.tDraw2D(0, -2,
                    new RectangleF(0, 0 + (360 * frame), 640, 360));
            }
        }
    }

    private void UpdateVideoVisibility()
    {
        bool haveRenderers = windowedRenderer != null || fullscreenRenderer != null;
        bool shouldShow = CDTXMania.ConfigIni.bAVIEnabled
                          && haveRenderers
                          && moveStartTimeMs != -1
                          && CSoundManager.rcPerformanceTimer.nCurrentTime >= moveStartTimeMs;

        if (windowedRenderer != null)
            windowedRenderer.isVisible = shouldShow && isWindowed;
        if (fullscreenRenderer != null)
            fullscreenRenderer.isVisible = shouldShow && isFullScreen;
        if (clipPanelImage != null)
            clipPanelImage.isVisible = shouldShow && isWindowed;
    }

    private (int panelX, int panelY, float scale, int videoX, int videoY) CalculateWindowedPosition()
    {
        int panelX = 0, panelY = 0;
        float scale = 1.0f;
        int videoX = 0, videoY = 0;

        if (IsDrums())
        {
            if (CDTXMania.ConfigIni.bGraph有効.Drums)
            {
                //drums + skill meter enabled.
                panelX = 2;
                panelY = 402;

                if (aspectRatio > 0.96f)
                {
                    scale = 260f / frameWidth;
                    videoX = panelX + 20;
                    videoY = panelY + (int)((270f - (frameHeight * scale)) / 2f);
                }
                else
                {
                    scale = 270f / frameHeight;
                    videoX = panelX + 5 + (int)((260 - (frameWidth * scale)) / 2f);
                    videoY = panelY + 20;
                }
            }
            else
            {
                //drums + skill meter disabled.
                panelX = 854;
                panelY = 142;

                if (aspectRatio > 1.77f)
                {
                    scale = 416f / frameWidth;
                    videoX = panelX + 5;
                    videoY = panelY + 30 + (int)((234f - (frameHeight * scale)) / 2f);
                }
                else
                {
                    scale = 234f / frameHeight;
                    videoX = panelX + 5 + (int)((416 - (frameWidth * scale)) / 2f);
                    videoY = panelY + 30;
                }
            }
        }
        else if (IsGuitarOrBass())
        {
            //guitar / bass mode.
            panelX = 380;
            panelY = 50;
            const int graphOffset = 267;

            //shift the panel right when the bass graph slot is enabled but
            //bass has no chips (so its graph isn't being drawn), and shift
            //left when the guitar graph slot is enabled but guitar has no
            //chips. Independent of which part the user is currently playing.
            if (CDTXMania.ConfigIni.bGraph有効.Bass && CDTXMania.DTX != null && !CDTXMania.DTX.bHasChips.Bass)
                panelX += graphOffset;
            if (CDTXMania.ConfigIni.bGraph有効.Guitar && CDTXMania.DTX != null && !CDTXMania.DTX.bHasChips.Guitar)
                panelX -= graphOffset;

            if (aspectRatio > 1.77f)
            {
                //wide clip: scale to 460px width, center vertically inside the 258px slot.
                //X gets the simple +30 inset; Y gets the centering math + 5 inset.
                scale = 460f / frameWidth;
                videoX = panelX + 30;
                videoY = panelY + 5 + (int)((258f - (frameHeight * scale)) / 2f);
            }
            else
            {
                //narrow clip: scale to 258px height, center horizontally inside the 460px slot.
                //X gets the centering math + 30 inset; Y gets the simple +5 inset.
                scale = 258f / frameHeight;
                videoX = panelX + 30 + (int)((460f - (frameWidth * scale)) / 2f);
                videoY = panelY + 5;
            }
        }

        return (panelX, panelY, scale, videoX, videoY);
    }

    private (int positionX, int positionY, float scaleX, float scaleY) CalculateFullscreenPosition()
    {
        int positionX = 0, positionY = 0;
        float scaleX = 1.0f, scaleY = 1.0f;

        if (aspectRatio > 1.77f)
        {
            //widescreen video: scale to fit width, center vertically.
            scaleX = 1280f / frameWidth;
            scaleY = scaleX;
            positionY = (int)((720f - (frameHeight * scaleY)) / 2f);
            positionX = 0;
        }
        else
        {
            //old-format video: position depends on instrument.
            if (IsDrums())
            {
                //drums: position at x=882, scale by 1.42f (matches original "vclip").
                scaleX = 1.42f;
                scaleY = 1.42f;
                positionX = 882;
                positionY = 0;
            }
            else
            {
                //guitar / bass: center horizontally, no scaling.
                scaleX = 1.0f;
                scaleY = 1.0f;
                positionX = (int)((1280f - frameWidth) / 2f);
                positionY = 0;
            }
        }

        return (positionX, positionY, scaleX, scaleY);
    }

    private void HandlePauseToggle()
    {
        if (!CDTXMania.Pad.bPressed(EInstrumentPart.BASS, EPad.Help))
            return;

        //pause/resume needs support from FFmpegVideoPlayer; this just toggles state for now.
        isPaused = !isPaused;
    }

    private string GetVideoPath(string filename)
    {
        if (CDTXMania.DTX == null || Path.IsPathRooted(filename))
            return filename;

        if (!string.IsNullOrEmpty(CDTXMania.DTX.PATH_WAV))
            return CDTXMania.DTX.PATH_WAV + filename;
        return CDTXMania.DTX.strFolderName + CDTXMania.DTX.PATH + filename;
    }

    #endregion

    #region Private Fields

    //ui components
    public UIGroup uiGroup;
    private UIImage clipPanelImage;

    //video renderers (both share the same videoPlayer)
    private UIVideoRenderer windowedRenderer;
    private UIVideoRenderer fullscreenRenderer;

    //video player
    private FFmpegVideoPlayer videoPlayer;

    //textures
    private BaseTexture clipPanelTexture;
    
    //state
    private bool isFullScreen;
    public bool isWindowed;
    private bool isPaused;
    public float aspectRatio;
    private uint frameHeight;
    private uint frameWidth;
    private int currentMovieMode;
    private long moveStartTimeMs;

    //video reference
    private CAVI avi;
    public bool isPreviewMovie { get; set; }
    public bool loop { get; set; }

    //fill-in effects
    [StructLayout(LayoutKind.Sequential)]
    public struct FillInEffect
    {
        public bool isInUse;
        public CCounter counter;
    }
    private FillInEffect[] fillInEffects = new FillInEffect[2];

    #endregion
}