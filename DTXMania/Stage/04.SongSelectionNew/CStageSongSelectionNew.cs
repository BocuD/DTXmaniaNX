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
    private UIText commentText;

    private SongSelectionContainer currentSelectionContainer;
    private DensityGraph densityGraph1;
    private DensityGraph densityGraph2;
    
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

        var selectionContainerGroup = ui.AddChild(new UIGroup("Selection Containers"));
        selectionContainerGroup.position = new Vector3(765, 320, 0);
        
        //create songselectioncontainer for each sorter
        foreach (SongDbSort sorter in sorters)
        {
            SongSelectionContainer container = selectionContainerGroup.AddChild(new SongSelectionContainer(songDb, bigAlbumArt));
            container.name = "SongSelect " + sorter.Name;
            selectionContainers[sorter] = container;
        }
        
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
        bg.renderOrder = -100;
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
        SongNode? selectedRootBackup = currentSelectionContainer?.CurrentRoot;
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
        
        foreach (SongDbSort sorter in sorters)
        {
            // try
            // {
                if (!sortCache.TryGetValue(sorter, out SongNode? rootNode) || sorter.requireResort)
                {
                    DateTime now = DateTime.Now;
                    rootNode = sorter.Sort(songDb).Result;
                    TimeSpan sortTime = DateTime.Now - now;
                    Trace.TraceInformation($"{sorter.Name} finished sorting in {sortTime.TotalMilliseconds} ms");
                    sortCache[sorter] = rootNode;
                };
                
                if (!selectionContainers.TryGetValue(sorter, out SongSelectionContainer? container))
                {
                    container = ui.AddChild(new SongSelectionContainer(songDb, bigAlbumArt));
                    container.name = "SongSelect " + sorter.Name;
                    container.position = new Vector3(765, 320, 0);
                    selectionContainers[sorter] = container;
                }
                
                container.UpdateRoot(rootNode, false);
                container.isVisible = false;

                Trace.TraceInformation($"Containers prepared for {sorter.Name}");
            // }
            // catch (Exception e)
            // {
            //     Trace.TraceError($"Failed to prepare container for {sorter.Name}: {e.Message}");
            // }
        }
        
        //enable the current sort
        ApplySort(currentSort);
        
        //try to restore the last selected song if possible
        if (selectedRootBackup != null && selectedNodeBackup != null && selectedChartBackup != null)
        {
            RestoreSelection(selectedRootBackup, selectedNodeBackup, selectedChartBackup);
        }
        
        Trace.TraceInformation("Sort cache preparation complete.");
        
        TimeSpan elapsed = DateTime.Now - startTime;
        Trace.TraceInformation($"Sort cache prepared in {elapsed} s.");
        
        loadPhase = ELoadPhase.CacheThumbnails;
    }

    private void RestoreSelection(SongNode selectedRootBackup, SongNode selectedNodeBackup, CChartData selectedChartDataBackup)
    {
        //walk down the tree recursively to find the node
        SongNode currentRoot = currentSelectionContainer.CurrentRoot;
        SongNode? targetRoot = null;
        SongNode? targetNode = null;
            
        FindRoot(currentRoot);
        
        if (targetRoot != null && targetNode != null)
        {
            if (targetRoot.parent != null)
            {
                targetRoot.parent.CurrentSelection = targetRoot;
            }
            
            currentSelectionContainer.UpdateRoot(targetRoot);
            currentSelectionContainer.UpdateSelection(targetNode);
        }
        
        void FindRoot(SongNode node)
        {
            foreach (var child in node.childNodes)
            {
                if (child == null) continue;
                    
                if (child.path.Equals(selectedNodeBackup.path, StringComparison.InvariantCulture))
                {
                    if (node.title.Equals(selectedRootBackup.title, StringComparison.InvariantCulture))
                    {
                        targetRoot = node;
                        targetNode = child;
                        return;
                    }
                }

                if (child.nodeType is SongNode.ENodeType.BOX or SongNode.ENodeType.ROOT)
                {
                    FindRoot(child);
                }

                if (targetRoot != null) return;
            }
        }
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
                foreach (var container in selectionContainers.Values)
                {
                    //cache thumbnails for this container
                    container.UpdateImageCache(true);
                    container.PreRenderText();
                }
                TimeSpan elapsed = DateTime.Now - start;
                Trace.TraceInformation($"Thumbnail cache updated in {elapsed} s.");
                loadPhase = ELoadPhase.ReadyToOpen;
                return 0;

            case ELoadPhase.ReadyToOpen:
                GitaDoraTransition.Open(20);
                loadPhase = ELoadPhase.Complete;
                return 0;
        }
        
        actPresound.OnUpdateAndDraw();
        
        sortMenuContainer.HandleNavigation();
        statusPanel.HandleNavigation();
        songSearchMenu.HandleNavigation();
        return currentSelectionContainer.HandleNavigation();
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
        //densityGraph2.SelectionChanged(node, chart);
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
    
    private bool sortLocked = false;
    private SongDbSort currentSort;
    private Dictionary<SongDbSort, SongNode> sortCache = new();
    private Dictionary<SongDbSort, SongSelectionContainer> selectionContainers = new();
    private int lastInstrument;
    public bool isScrolling => currentSelectionContainer.isScrolling;

    public void ApplySort(SongDbSort sorter)
    {
        //check if we have a container for this sorter
        if (!selectionContainers.TryGetValue(sorter, out SongSelectionContainer? container))
        {
            Trace.TraceError("Sort cache does not contain a container for sorter: " + sorter.Name);
            return;
        }
        
        currentSort = sorter;
        
        //hide the current selection container
        if (currentSelectionContainer != null)
        {
            currentSelectionContainer.isVisible = false;
        }

        //set the new container
        currentSelectionContainer = container;
        currentSelectionContainer.isVisible = true;
        
        SongNode newSelection = currentSelectionContainer.currentSelection;
        int closestLevelToTarget = GetClosestLevelToTargetForSong(currentSelectionContainer.currentSelection);
        CChartData? chart = null;
        
        //check if the closest level is valid
        chart = closestLevelToTarget > newSelection.charts.Length - 1 
            ? newSelection.charts.FirstOrDefault()
            : newSelection.charts[closestLevelToTarget];
        
        ChangeSelection(currentSelectionContainer.currentSelection, chart);   
    }

    //reload current view
    public void Reload()
    {
        sortCache.Clear();
        
        //song selection stage might not have been loaded yet
        if (ui == null) return;
        PrepareSelectionContainers();
        ApplySort(currentSort);
    }

    public int UpdateSearch(string searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            //if search query is empty, reset to current sort
            currentSelectionContainer.RequestUpdateRoot(currentSelectionContainer.UnfilteredRoot);
            return 0;
        }
        
        SongNode? searchResult = currentSelectionContainer.UnfilteredRoot.GetSearchResult(searchQuery);
        if (searchResult != null)
        {
            if (searchResult.childNodes.Count > 0)
            {
                currentSelectionContainer.RequestUpdateRoot(searchResult, true);
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