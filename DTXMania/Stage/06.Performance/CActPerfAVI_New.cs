using System.Drawing;
using System.Numerics;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DTXMania.Core;
using DTXMania.Core.Video;
using DTXMania.UI.Drawable;
using FDK;

namespace DTXMania;

internal class CActPerfAVI : CActivity
{
    #region Constructor

    public CActPerfAVI(bool isDuringPerformance = true)
    {
        this.isDuringPerformance = isDuringPerformance;
        if (this.isDuringPerformance)
        {
            listChildActivities.Add(panelString = new CActPerfPanelString());
        }

        bActivated = false;
    }

    #endregion

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
    }

    public void Cont(int resumeTimeMs)
    {
        moveStartTimeMs = resumeTimeMs;
    }

    public void Start(bool fillIn)
    {
        for (int i = 0; i < fillInEffects.Length; i++)
        {
            if (fillInEffects[i].isInUse)
            {
                fillInEffects[i].counter.tStop();
                fillInEffects[i].isInUse = false;
            }
        }

        for (int i = 0; i < fillInEffects.Length; i++)
        {
            if (!fillInEffects[i].isInUse)
            {
                fillInEffects[i].isInUse = true;
                fillInEffects[i].counter = new CCounter(0, 30, 30, CDTXMania.Timer);
                break;
            }
        }
    }

    public void IntegrateUI(UIGroup parentGroup)
    {
        if (parentGroup == null)
            return;

        // Create UI group for this activity
        uiGroup = new UIGroup("CActPerfAVI UI");

        // Load clip panel texture and create UIImage
        string clipPanelPath = CDTXMania.ConfigIni.bGuitarEnabled ? @"Graphics\7_ClipPanelC.png" :
                              CDTXMania.ConfigIni.bGraph有効.Drums && CDTXMania.ConfigIni.bDrumsEnabled ?
                              @"Graphics\7_ClipPanelB.png" : @"Graphics\7_ClipPanel.png";

        clipPanelTexture = BaseTexture.LoadFromPath(CSkin.Path(clipPanelPath));

        if (clipPanelTexture.isValid())
        {
            clipPanelImage = new UIImage(clipPanelTexture);
            clipPanelImage.isVisible = false;
            clipPanelImage.renderOrder = 1;
            uiGroup.AddChild(clipPanelImage);
        }

        // Load fill-in effect texture
        fillInTexture = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_Fillin Effect.png"));

        // Initialize fill-in effects
        for (int i = 0; i < fillInEffects.Length; i++)
        {
            fillInEffects[i] = new FillInEffect();
            fillInEffects[i].counter = new CCounter(0, 30, 30, CDTXMania.Timer);
            fillInEffects[i].isInUse = false;
        }

        // Add this activity's UI group to the parent
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

    public override void OnManagedCreateResources()
    {
        // UI resources are created in IntegrateUI() instead
        base.OnManagedCreateResources();
    }

    public override void OnManagedReleaseResources()
    {
        // UI cleanup is handled by GC
        base.OnManagedReleaseResources();
    }

    public int tUpdateAndDraw(int x, int y)
    {
        if (!bActivated)
            return 0;

        UpdateVideoSync();
        UpdateFillInEffects();
        UpdatePanelString();
        UpdateVideoVisibility();

        HandlePauseToggle();

        return 0;
    }

    #endregion

    #region Private Methods

    private void CreateVideoPlayer(string videoFileName)
    {
        // Create video player
        videoPlayer = new ThreadedSoftwareVideoPlayer();

        // Open video file
        string videoPath = GetVideoPath(videoFileName);
        if (File.Exists(videoPath) && videoPlayer.Open(videoPath))
        {
            videoPlayer.LoopOnEof = loop;

            // Create windowed video renderer
            windowedRenderer = new UIVideoRenderer(videoPlayer, videoPath);
            windowedRenderer.isVisible = true;
            windowedRenderer.renderOrder = 2;
            uiGroup.AddChild(windowedRenderer);

            // Create fullscreen video renderer (same video player)
            fullscreenRenderer = new UIVideoRenderer(videoPlayer, videoPath);
            fullscreenRenderer.isVisible = true;
            uiGroup.AddChild(fullscreenRenderer);
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

        // Calculate expected video time
        long videoPlayTimeMs = currentGameTime - videoStartTime;

        // Get current video time
        TimeSpan currentVideoTime = videoPlayer.CurrentTime;
        long currentVideoTimeMs = (long)currentVideoTime.TotalMilliseconds;

        // Sync if out of sync by more than 100ms
        long timeDelta = Math.Abs(videoPlayTimeMs - currentVideoTimeMs);
        if (timeDelta > 100)
        {
            videoPlayer.Seek(TimeSpan.FromMilliseconds(videoPlayTimeMs));
        }

        // Handle looping
        TimeSpan duration = videoPlayer.Duration;
        if (duration > TimeSpan.Zero && currentVideoTime >= duration)
        {
            if (!isPreviewMovie && !loop)
            {
                moveStartTimeMs = -1;
                SetVideoVisibility(false);
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

            if (CDTXMania.ConfigIni.bDrumsEnabled && stageDrum.txBonusEffect != null)
            {
                stageDrum.txBonusEffect.blendMode = BlendMode.Additive;
                stageDrum.txBonusEffect.tDraw2D(CDTXMania.app.Device, 0, -2,
                    new RectangleF(0, 0 + (360 * frame), 640, 360));
            }
        }
    }

    private void UpdatePanelString()
    {
        if (CDTXMania.ConfigIni.bShowMusicInfo && isDuringPerformance)
            panelString.tUpdateAndDraw();
    }

    private void UpdateVideoVisibility()
    {
        if (!CDTXMania.ConfigIni.bAVIEnabled ||
            (windowedRenderer == null && fullscreenRenderer == null))
        {
            HideAllVideo();
            return;
        }

        bool shouldShowVideo = (moveStartTimeMs != -1) &&
                              (CSoundManager.rcPerformanceTimer.nCurrentTime >= moveStartTimeMs);

        // Handle all 4 movie modes
        // Mode 0: none (isFullScreen=false, isWindowed=false)
        // Mode 1: fullscreen only (isFullScreen=true, isWindowed=false)
        // Mode 2: windowed only (isFullScreen=false, isWindowed=true)
        // Mode 3: both (isFullScreen=true, isWindowed=true)

        if (isWindowed && isFullScreen)
        {
            // Mode 3: both
            UpdateWindowedVideo(shouldShowVideo);
            UpdateFullscreenVideo(shouldShowVideo);
        }
        else if (isWindowed)
        {
            // Mode 2: windowed only
            UpdateWindowedVideo(shouldShowVideo);
            if (fullscreenRenderer != null)
                fullscreenRenderer.isVisible = false;
        }
        else if (isFullScreen)
        {
            // Mode 1: fullscreen only
            UpdateFullscreenVideo(shouldShowVideo);
            if (windowedRenderer != null)
                windowedRenderer.isVisible = false;
        }
        else
        {
            // Mode 0: none
            HideAllVideo();
        }

        // Update clip panel visibility
        if (clipPanelImage != null)
        {
            clipPanelImage.isVisible = isWindowed && shouldShowVideo;
        }
    }

    private void UpdateWindowedVideo(bool shouldShow)
    {
        if (windowedRenderer == null || clipPanelImage == null)
            return;

        if (!shouldShow)
        {
            windowedRenderer.isVisible = false;
            return;
        }

        // Calculate windowed positioning
        var (panelX, panelY, scale, videoX, videoY) = CalculateWindowedPosition();

        // Update clip panel
        clipPanelImage.isVisible = true;
        clipPanelImage.position = new Vector3(panelX, panelY, 0);

        // Update video renderer
        if (avi != null)
        {
            windowedRenderer.position = new Vector3(videoX, videoY, 0);
            windowedRenderer.size = new Vector2(frameWidth * scale, frameHeight * scale);
            windowedRenderer.isVisible = true;
        }
        else
        {
            windowedRenderer.isVisible = false;
        }
    }

    private void UpdateFullscreenVideo(bool shouldShow)
    {
        if (fullscreenRenderer == null)
            return;

        if (!shouldShow)
        {
            fullscreenRenderer.isVisible = false;
            return;
        }

        // Calculate fullscreen positioning based on aspect ratio
        var (positionX, positionY, scaleX, scaleY) = CalculateFullscreenPosition();

        fullscreenRenderer.position = new Vector3(positionX, positionY, 0);
        fullscreenRenderer.size = new Vector2(frameWidth * scaleX, frameHeight * scaleY);
        fullscreenRenderer.isVisible = true;
    }

    private (int panelX, int panelY, float scale, int videoX, int videoY) CalculateWindowedPosition()
    {
        int panelX = 0, panelY = 0;
        float scale = 1.0f;
        int videoX = 0, videoY = 0;

        if (CDTXMania.ConfigIni.bDrumsEnabled)
        {
            if (CDTXMania.ConfigIni.bGraph有効.Drums)
            {
                // Skill meter enabled
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
                // Skill meter disabled
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
        else if (CDTXMania.ConfigIni.bGuitarEnabled)
        {
            // Guitar mode
            panelX = 380;
            panelY = 50;
            int graphOffset = 267;

            if (CDTXMania.ConfigIni.bGraph有効.Bass && CDTXMania.DTX != null && !CDTXMania.DTX.bHasChips.Bass)
                panelX += graphOffset;
            if (CDTXMania.ConfigIni.bGraph有効.Guitar && CDTXMania.DTX != null && !CDTXMania.DTX.bHasChips.Guitar)
                panelX -= graphOffset;

            if (aspectRatio > 1.77f)
            {
                scale = 460f / frameWidth;
                videoX = panelX + 30;
                videoY = panelY + 5 + (int)((258f - (frameHeight * scale)) / 2f);
            }
            else
            {
                scale = 258f / frameHeight;
                videoX = panelX + 5 + (int)((460 - (frameWidth * scale)) / 2f);
                videoY = panelY + 30;
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
            // Widescreen video - scale to fit width, center vertically
            scaleX = 1280f / frameWidth;
            scaleY = scaleX;
            positionY = (int)((720f - (frameHeight * scaleY)) / 2f);
            positionX = 0;
        }
        else
        {
            // Old format video - position in top-right area
            if (CDTXMania.ConfigIni.bDrumsEnabled)
            {
                // Drums: position at x=882, scale by 1.42f
                scaleX = 1.42f;
                scaleY = 1.42f;
                positionX = 882;
                positionY = 0;
            }
            else if (CDTXMania.ConfigIni.bGuitarEnabled)
            {
                // Guitar: center horizontally, scale by 1.0f
                scaleX = 1.0f;
                scaleY = 1.0f;
                positionX = (int)((1280f - frameWidth) / 2f);
                positionY = 0;
            }
            else
            {
                // Default: center horizontally
                scaleX = 1.0f;
                scaleY = 1.0f;
                positionX = (int)((1280f - frameWidth) / 2f);
                positionY = 0;
            }
        }

        return (positionX, positionY, scaleX, scaleY);
    }

    private void SetVideoVisibility(bool visible)
    {
        if (windowedRenderer != null)
            windowedRenderer.isVisible = visible;
        if (fullscreenRenderer != null)
            fullscreenRenderer.isVisible = visible;
    }

    private void HideAllVideo()
    {
        if (windowedRenderer != null)
            windowedRenderer.isVisible = false;
        if (fullscreenRenderer != null)
            fullscreenRenderer.isVisible = false;
        if (clipPanelImage != null)
            clipPanelImage.isVisible = false;
    }

    private void HandlePauseToggle()
    {
        if (!CDTXMania.Pad.bPressed(EInstrumentPart.BASS, EPad.Help))
            return;

        if (isPaused == false)
        {
            // Pause functionality would need to be added to FFmpegVideoPlayer
            isPaused = true;
        }
        else if (isPaused == true)
        {
            // Resume functionality would need to be added to FFmpegVideoPlayer
            isPaused = false;
        }
    }

    private string GetVideoPath(string filename)
    {
        if (CDTXMania.DTX == null || Path.IsPathRooted(filename))
            return filename;

        if (!string.IsNullOrEmpty(CDTXMania.DTX.PATH_WAV))
            return CDTXMania.DTX.PATH_WAV + filename;
        else
            return CDTXMania.DTX.strFolderName + CDTXMania.DTX.PATH + filename;
    }

    #endregion

    #region Private Fields

    // UI components
    public UIGroup uiGroup;
    private UIImage clipPanelImage;

    // Video renderers (both share the same videoPlayer)
    private UIVideoRenderer windowedRenderer;
    private UIVideoRenderer fullscreenRenderer;

    // Video player
    private FFmpegVideoPlayer videoPlayer;

    // Textures
    private BaseTexture clipPanelTexture;
    private BaseTexture fillInTexture;

    // Activity components
    public CActPerfPanelString panelString;
    public bool isDuringPerformance = true;

    // State
    private bool isFullScreen;
    public bool isWindowed;
    private bool isPaused;
    public float aspectRatio;
    private uint frameHeight;
    private uint frameWidth;
    private int currentMovieMode;
    private long moveStartTimeMs;

    // Video reference
    private CAVI avi;
    public bool isPreviewMovie { get; set; }
    public bool loop { get; set; }

    // Fill-in effects
    [StructLayout(LayoutKind.Sequential)]
    public struct FillInEffect
    {
        public bool isInUse;
        public CCounter counter;
    }
    private FillInEffect[] fillInEffects = new FillInEffect[2];

    #endregion
}