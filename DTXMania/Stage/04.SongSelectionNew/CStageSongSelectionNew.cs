using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using DiscordRPC;
using DTXMania.Core;
using DTXMania.Core.Video;
using DTXMania.SongDb;
using DTXMania.SongDb.Sorting;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Text;

namespace DTXMania;

public class CStageSongSelectionNew : CStage
{
    private SongDb.SongDb songDb => CDTXMania.SongDb;
    private SortMenuContainer sortMenuContainer;
    private UIImage bigAlbumArt;
    private CActSelectPresound actPresound;
    private StatusPanel statusPanel;
    private SongSearchMenu songSearchMenu;
    private QuickMenu quickMenu;
    private UIText commentText;

    private SongSelectionContainer selectionContainer;
    private DensityGraph densityGraph1;
    
    private ELoadPhase loadPhase = ELoadPhase.Initialize;
    
    private enum ELoadPhase
    {
        Initialize,
        Prepare,
        CacheThumbnails,
        ReadyToOpen,
        Complete
    }
    
    public enum EReturnValue : int  // E戻り値
    {
        Continue,      // 継続
        ReturnToTitle, // タイトルに戻る
        Selected,      // 選曲した
        CallConfig,    // コンフィグ呼び出し
        ChangeSking    // スキン変更
    }
    
    protected override RichPresence Presence => new CDTXRichPresence
    {
        State = "In Menu",
        Details = "Selecting a song",
    };
    
    public CStageSongSelectionNew()
    {
        eStageID = EStage.SongSelection_4;
        
        listChildActivities.Add(actPresound = new CActSelectPresound());
        
        currentSort = sorters[0];
    }

    private readonly SongDbSort[] sorters =
    [
        new SortDefault(),
        new SortByBox(),
        new SortByTitle(),
        new SortByArtist(),
        new SortByDifficulty(),
        new SortByLevel(),
        new SortByPlayer(),
        new SortByAllSongs(),
        new SortBySkill()
    ];
    
    public override void InitializeBaseUI()
    {
        bigAlbumArt = ui.AddChild(new UIImage());
        bigAlbumArt.position = new Vector3(320, 35, 0);
        bigAlbumArt.renderOrder = 10;
        bigAlbumArt.size = new Vector2(300, 300);
        bigAlbumArt.name  = "AlbumArt";
        
        sortMenuContainer = ui.AddChild(new SortMenuContainer(songDb, sorters));
        sortMenuContainer.position = new Vector3(1281, 35, 0);
        sortMenuContainer.renderOrder = 8;

        statusPanel = ui.AddChild(new StatusPanel());
        statusPanel.renderOrder = 6;
        
        commentText = ui.AddChild(new UIText("", 18));
        commentText.renderOrder = 11;
        commentText.position = new Vector3(0, 35, 0);
        commentText.textSource = TextSource.Dynamic;
        commentText.dynamicSource = "SongComment";
        commentText.name = "CommentText";
        
        songSearchMenu = ui.AddChild(new SongSearchMenu());
        songSearchMenu.renderOrder = 15;
        songSearchMenu.isVisible = false;
        songSearchMenu.anchor = new Vector2(0.5f, 0.5f);
        songSearchMenu.position = new Vector3(1280 / 2.0f, 720 / 2.0f, 0);

        quickMenu = ui.AddChild(new QuickMenu());
        quickMenu.renderOrder = 15;
        quickMenu.isVisible = false;
        quickMenu.anchor = new Vector2(0.5f, 0.5f);
        quickMenu.position = new Vector3(1280 / 2.0f, 720 / 2.0f, 0);

        //a single container is re-pointed at the active sort's root (see ApplySort)
        selectionContainer = ui.AddChild(new SongSelectionContainer(songDb, bigAlbumArt));
        selectionContainer.position = new Vector3(765, 320, 0);
        selectionContainer.name = "SongSelect";
        
        dynamicStringSources["SongName"] = new DynamicStringSource(() => selectedChart?.SongInformation.Title ?? "");
        dynamicStringSources["SongArtist"] = new DynamicStringSource(() => selectedChart?.SongInformation.ArtistName ?? "");
        dynamicStringSources["SongGenre"] = new DynamicStringSource(() => selectedChart?.SongInformation.Genre ?? "");
        dynamicStringSources["SongBPM"] = new DynamicStringSource(() => selectedChart?.SongInformation.Bpm.ToString("0.##", CultureInfo.InvariantCulture) ?? "");
        dynamicStringSources["SongDuration"] = new DynamicStringSource(() =>
        {
            int? ms = selectedChart?.SongInformation.DurationMs;
            return ms != null ? TimeSpan.FromMilliseconds(ms.Value).ToString(@"m\:ss") : "";
        });
        dynamicStringSources["SongComment"] = new DynamicStringSource(() => selectedChart?.SongInformation.Comment ?? "");
        dynamicStringSources["SongSkill"] = new DynamicStringSource(() =>
        {
            double? points = selectedNode?.GetTopSkillPoints().skillPoints;

            if (points != null && points > 0)
            {
                return points.Value.ToString("0.00");
            }

            return "";
        });
    }

    public override void InitializeDefaultUI()
    {
        BaseTexture bgTex = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\5_background.jpg"));
        UIImage bg = ui.AddChild(new UIImage(bgTex));
        bg.renderOrder = -101;
        bg.position = Vector3.Zero;
        bg.name = "Background";
        
        string videoPath = CSkin.Path(@"Graphics\5_background.mp4");

        UINewVideoRenderer videoPlayer = new();
        if (videoPlayer.LoadVideo(videoPath))
        {
            ui.AddChild(videoPlayer);
            videoPlayer.renderOrder = -100;
            videoPlayer.name = "BackgroundVideo";
        }
        else
        {
            videoPlayer.Dispose();
        }
        
        //create panel elements
        var back1 = ui.AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\SongSelect\back1.png"))));
        back1.renderOrder = 1;
        back1.position = new Vector3(174, 393, 0);
        back1.rotation = new Vector3(0, 0, 1.63f);
        back1.name = "Back1";
        
        var back2 = ui.AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\SongSelect\back2.png"))));
        back2.renderOrder = 2;
        back2.position = new Vector3(126, 336, 0);
        back2.rotation = new Vector3(0, 0, -0.06f);
        back2.name = "Back2";

        densityGraph1 = ui.AddChild(new DensityGraph((EInstrumentPart)CDTXMania.GetCurrentInstrument()));
        densityGraph1.position = new Vector3(CDTXMania.GetCurrentInstrument() == 0 ? 212 : 64, 720, 0);
        densityGraph1.renderOrder = 4;
        densityGraph1.name = "DensityGraph";
        
        var topBar = ui.AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\SongSelect\top_bar.png"))));
        topBar.renderOrder = 12;
        topBar.name = "TopBar";
        topBar.size.X = 1280;
        
        var panelSkill = ui.AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\SongSelect\panel_skill.png"))));
        panelSkill.renderOrder = 9;
        panelSkill.name = "PanelSkill";
        panelSkill.position = new Vector3(96, 225, 0);
        
        var skillText = ui.AddChild(new UIText("", 48));
        skillText.renderOrder = 11;
        skillText.textSource = TextSource.Dynamic;
        skillText.dynamicSource = "SongSkill";
        skillText.outlineWidth = 0;
        skillText.style = UiTextStyle.Italic;
        skillText.fontSource = FontSource.System;
        skillText.font = "Futura PT Medium.otf";
        skillText.anchor = new Vector2(1, 1);
        skillText.position = new Vector3(315, 291, 0);
        skillText.name = "SkillText";
        
        var panelBpm = ui.AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\SongSelect\panel_bpm.png"))));
        panelBpm.renderOrder = 9;
        panelBpm.name = "PanelBpm";
        panelBpm.position = new Vector3(96, 300, 0);
        
        var bpmText = ui.AddChild(new UIText("", 28));
        bpmText.renderOrder = 11;
        bpmText.textSource = TextSource.Dynamic;
        bpmText.dynamicSource = "SongBPM";
        bpmText.outlineWidth = 0;
        bpmText.style = UiTextStyle.Italic;
        bpmText.fontSource = FontSource.System;
        bpmText.font = "Futura PT Medium.otf";
        bpmText.anchor = new Vector2(1, 1);
        bpmText.position = new Vector3(315, 338, 0);
        bpmText.name = "BPMText";
    }

    public override void FirstUpdate()
    {
        //set initial sort menu container position to be default,
        //or in case of reloading the menu, whatever was last selected
        sortMenuContainer.SetCurrentSelection(currentSort);
        
        //every time we load the stage, containers need to be recreated
        loadPhase = ELoadPhase.Initialize;
    }

    private void PrepareSelectionContainers()
    {
        //backup our current selection
        SongNode? selectedRootBackup = selectionContainer?.CurrentRoot;
        SongNode? selectedNodeBackup = selectedNode;
        CChartData? selectedChartBackup = selectedChart;
        
        //determine if we need to rebuild sort cache or not
        if (CDTXMania.GetCurrentInstrument() != lastInstrument)
        {
            //force a recreation of sort cache if instrument has changed
            sortCache.Clear();
        }
        
        lastInstrument = CDTXMania.GetCurrentInstrument();
        
        Trace.TraceInformation("Preparing sort cache...");
        DateTime startTime = DateTime.Now;
        
        //build (or refresh) the sorted root for every sorter; the single container is pointed at
        //one of these on demand in ApplySort.
        foreach (SongDbSort sorter in sorters)
        {
            if (!sortCache.TryGetValue(sorter, out SongNode? rootNode) || sorter.requireResort)
            {
                DateTime now = DateTime.Now;
                rootNode = sorter.Sort(songDb).Result;
                TimeSpan sortTime = DateTime.Now - now;
                Trace.TraceInformation($"{sorter.Name} finished sorting in {sortTime.TotalMilliseconds} ms");
                sortCache[sorter] = rootNode;
            }
        }

        //point the container at the current sort
        ApplySort(currentSort);
        
        //try to restore the last selected song if possible
        if (selectedRootBackup != null && selectedNodeBackup != null && selectedChartBackup != null)
        {
            RestoreSelection(selectedNodeBackup);
        }
        
        Trace.TraceInformation("Sort cache preparation complete.");
        
        TimeSpan elapsed = DateTime.Now - startTime;
        Trace.TraceInformation($"Sort cache prepared in {elapsed} s.");
        
        loadPhase = ELoadPhase.CacheThumbnails;
    }

    private void RestoreSelection(SongNode selectedNodeBackup)
    {
        string? previousBoxTitle = selectedNodeBackup.parent?.title;

        SongNode? fallback = null;
        SongNode? preferred = null;

        void Find(SongNode container)
        {
            foreach (SongNode child in container.childNodes)
            {
                if (child == null) continue;

                switch (child.nodeType)
                {
                    case SongNode.ENodeType.SONG
                        when child.path.Equals(selectedNodeBackup.path, StringComparison.InvariantCulture):
                        fallback ??= child;
                        if (previousBoxTitle != null &&
                            container.title.Equals(previousBoxTitle, StringComparison.InvariantCulture))
                        {
                            preferred = child;
                            return;
                        }
                        break;

                    case SongNode.ENodeType.BOX or SongNode.ENodeType.ROOT:
                        Find(child);
                        if (preferred != null) return;
                        break;
                }
            }
        }

        Find(selectionContainer.CurrentRoot);

        SongNode? targetNode = preferred ?? fallback;
        if (targetNode?.parent == null)
            return;

        SongNode targetRoot = targetNode.parent;

        //highlight the target's container within each ancestor so backing out lands on the right row
        for (SongNode node = targetRoot; node.parent != null; node = node.parent)
        {
            node.parent.CurrentSelection = node;
        }

        selectionContainer.UpdateRoot(targetRoot);
        selectionContainer.UpdateSelection(targetNode);
    }

    public override int OnUpdateAndDraw()
    {
        base.OnUpdateAndDraw();

        switch (loadPhase)
        {
            //don't do anything until the sort cache is prepared
            case ELoadPhase.Initialize:
                if (songDb.status == SongDbScanStatus.Idle)
                {
                    loadPhase = ELoadPhase.Prepare;
                    PrepareSelectionContainers();
                }
                return 0;
            
            case ELoadPhase.Prepare:
                return 0;
            
            case ELoadPhase.CacheThumbnails:
                DateTime start = DateTime.Now;
                //only the active view is warmed synchronously before opening; the other sorts are
                //prewarmed in the background once we're open (see ReadyToOpen).
                selectionContainer.UpdateImageCache(true);
                selectionContainer.PreRenderText();
                TimeSpan elapsed = DateTime.Now - start;
                Trace.TraceInformation($"Thumbnail cache updated in {elapsed} s.");
                loadPhase = ELoadPhase.ReadyToOpen;
                return 0;

            case ELoadPhase.ReadyToOpen:
                GitaDoraTransition.Open(2);
                PrewarmOtherSorts();
                loadPhase = ELoadPhase.Complete;
                return 0;
        }
        
        actPresound.OnUpdateAndDraw();

        bool isSubMenuActive = HandleSubMenus();

        if (isSubMenuActive)
        {
            return (int)EReturnValue.Continue;
        }
        
        statusPanel.HandleNavigation();
        sortMenuContainer.HandleNavigation();
        return selectionContainer.HandleNavigation();
    }

    private bool HandleSubMenus()
    {
        //cache this before running input handlers for submenus,
        //so enter to close it doesn't get consumed by other menus
        bool isActive = quickMenu.isVisible || songSearchMenu.isVisible;
        
        quickMenu.HandleNavigation();
        songSearchMenu.HandleNavigation();

        return isActive;
    }

    public SongNode? selectedNode { get; private set; }
    public CChartData? selectedChart { get; private set; }
    public void ChangeSelection(SongNode? node, CChartData? chart)
    {
        selectedNode = node;
        selectedChart = chart;
        
        actPresound.tSelectionChanged(chart);
        statusPanel.SelectionChanged(node, chart);
        densityGraph1.SelectionChanged(node, chart);
    }

    public int targetDifficultyLevel { get; private set; } = 0;
    public void IncrementDifficultyLevel()
    {
        if (selectedNode.nodeType != SongNode.ENodeType.SONG)
        {
            targetDifficultyLevel = (targetDifficultyLevel + 1) % 5;
        }
        else
        {
            var nextAvailableLevel = targetDifficultyLevel;

            //find first available new level
            for (int i = 0; i < 5; i++)
            {
                int newLevel = (targetDifficultyLevel + i) % 5;
                if (newLevel == targetDifficultyLevel) continue;

                int currentInstrument = CDTXMania.GetCurrentInstrument();

                //check if this chart is valid
                var chart = selectedNode.charts[newLevel];
                if (chart == null) continue;

                if (chart.SongInformation.chipCountByInstrument[currentInstrument] > 0)
                {
                    ChangeSelection(selectedNode, chart);
                    nextAvailableLevel = newLevel;
                    break;
                }
            }

            if (nextAvailableLevel == targetDifficultyLevel) return;

            targetDifficultyLevel = nextAvailableLevel;
        }

        switch (targetDifficultyLevel)
        {
            case 0:
                CDTXMania.Skin.soundBasic.tPlay();
                string strbsc = CSkin.Path( @"Sounds\Basic.ogg" );
                if ( !File.Exists( strbsc ) )
                    CDTXMania.Skin.soundChange.tPlay();
                break;
            case 1:
                CDTXMania.Skin.soundAdvanced.tPlay();
                string stradv = CSkin.Path( @"Sounds\Advanced.ogg" );
                if ( !File.Exists( stradv ) )
                    CDTXMania.Skin.soundChange.tPlay();
                break;
            case 2:
                CDTXMania.Skin.soundExtreme.tPlay();
                string strext = CSkin.Path( @"Sounds\Extreme.ogg" );
                if ( !File.Exists( strext ) )
                    CDTXMania.Skin.soundChange.tPlay();
                break;
            case 3:
                CDTXMania.Skin.soundMaster.tPlay();
                string strmas = CSkin.Path( @"Sounds\Master.ogg" );
                if ( !File.Exists( strmas ) )
                    CDTXMania.Skin.soundChange.tPlay();
                break;
            case 4:
                CDTXMania.Skin.soundChange.tPlay();
                break;
        }
    }
    
    public int GetClosestLevelToTargetForSong(SongNode? song)
    {
        var targetDifficultyLevel = this.targetDifficultyLevel;

        if (song == null)
            return targetDifficultyLevel; // 曲がまったくないよ
        
        if (song.nodeType != SongNode.ENodeType.SONG) return 0;

        if (song.charts[targetDifficultyLevel] != null)
            return targetDifficultyLevel; // 難易度ぴったりの曲があったよ

        if ((song.nodeType == SongNode.ENodeType.BOX) || (song.nodeType == SongNode.ENodeType.BACKBOX))
            return 0; // BOX と BACKBOX は関係無いよ


        // 現在のアンカレベルから、難易度上向きに検索開始。

        int closestLevel = targetDifficultyLevel;

        for (int i = 0; i < 5; i++)
        {
            if (song.charts[closestLevel] != null)
                break; // 曲があった。

            closestLevel = (closestLevel + 1) % 5; // 曲がなかったので次の難易度レベルへGo。（5以上になったら0に戻る。）
        }


        // 見つかった曲がアンカより下のレベルだった場合……
        // アンカから下向きに検索すれば、もっとアンカに近い曲があるんじゃね？

        if (closestLevel < targetDifficultyLevel)
        {
            // 現在のアンカレベルから、難易度下向きに検索開始。

            closestLevel = targetDifficultyLevel;

            for (int i = 0; i < 5; i++)
            {
                if (song.charts[closestLevel] != null)
                    break; // 曲があった。

                closestLevel = ((closestLevel - 1) + 5) % 5; // 曲がなかったので次の難易度レベルへGo。（0未満になったら4に戻る。）
            }
        }

        return closestLevel;
    }
    
    private SongDbSort currentSort;
    private Dictionary<SongDbSort, SongNode> sortCache = new();
    private int lastInstrument;
    public bool isScrolling => selectionContainer.isScrolling;

    public void ApplySort(SongDbSort sorter)
    {
        if (!sortCache.TryGetValue(sorter, out SongNode? root))
        {
            Trace.TraceError("Sort cache does not contain a root for sorter: " + sorter.Name);
            return;
        }

        currentSort = sorter;

        //re-point the single container at the selected sort's root. Each sorted root remembers its
        //own selection (SongNode.CurrentSelection), so every sort keeps its own scroll position, and
        //the path-keyed image cache is shared so thumbnails are reused across sorts. UpdateRoot
        //propagates the selection back to the stage via HandleSelectionChanged -> ChangeSelection.
        selectionContainer.UpdateRoot(root);
    }

    //background-prewarm the initial thumbnails of every non-active sort so switching to them shows
    //images immediately. Fire-and-forget; the async uploader throttles the actual GPU uploads.
    private void PrewarmOtherSorts()
    {
        foreach (SongDbSort sorter in sorters)
        {
            if (sorter == currentSort) continue;
            if (sortCache.TryGetValue(sorter, out SongNode? root))
            {
                selectionContainer.PrewarmWindow(root);
            }
        }
    }

    //reload current view
    public void Reload()
    {
        sortCache.Clear();
        
        //song selection stage might not have been loaded yet
        if (ui == null) return;

        //PrepareSelectionContainers already re-points the container (ApplySort) and restores the
        //previous selection, so no extra ApplySort is needed here.
        PrepareSelectionContainers();
    }

    public int UpdateSearch(string searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            //if search query is empty, reset to current sort
            selectionContainer.RequestUpdateRoot(selectionContainer.UnfilteredRoot);
            return 0;
        }
        
        SongNode? searchResult = selectionContainer.UnfilteredRoot.GetSearchResult(searchQuery);
        if (searchResult != null)
        {
            if (searchResult.childNodes.Count > 0)
            {
                selectionContainer.RequestUpdateRoot(searchResult, true);
            }
            else
            {
                Trace.TraceInformation("No search results found for query: " + searchQuery);
            }
            return searchResult.childNodes.Count;
        }

        return -1;
    }
}