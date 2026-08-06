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
using DTXMania.UI.Skin;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Text;

namespace DTXMania;

public class CStageSongSelectionNew : CStage
{
    private SongDb.SongDb songDb => CDTXMania.SongDb;
    private SortMenuContainer? sortMenuContainer;
    private CActSelectPresound actPresound;
    private PreviewVideoBackground previewVideo;
    private StatusPanel statusPanel;
    private SongSearchMenu songSearchMenu;
    private QuickMenu quickMenu;

    private SongSelectionContainer selectionContainer;
    private DensityGraph densityGraph1;

    //game rules rather than UI, so a skin cannot move or remove them
    private readonly CCommandHistory commandHistory = new();

    //what the frame's input decided the stage should do, read back in OnUpdateAndDraw
    private int pendingResult = (int)EReturnValue.Continue;

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
        
        currentSort = SongDbSort.All[0];
    }
    
    //the selected song's display values, pushed on change: formatting them per frame would allocate
    private readonly UIDataContext songInfo = new();

    public override void RegisterBindings()
    {
        foreach (string key in SongInfoKeys)
        {
            songInfo.DeclareString(key);
        }

        //also expose the raw selection, so a skin can reach anything these keys don't cover
        songInfo.RegisterObject("Song", () => selectedNode);
        songInfo.RegisterObject("Chart", () => selectedChart);

        //pulled rather than pushed: the thumbnail can finish loading after the selection changed
        songInfo.RegisterTexture("AlbumArt", () => selectionContainer?.CurrentBigAlbumArt);

        ui.dataContext = songInfo;
    }

    private static readonly string[] SongInfoKeys =
        ["SongName", "SongArtist", "SongGenre", "SongBPM", "SongDuration", "SongComment", "SongSkill"];

    private void RefreshSongInfo()
    {
        CChartData? chart = selectedChart;

        songInfo.SetString("SongName", chart?.SongInformation.Title ?? "");
        songInfo.SetString("SongArtist", chart?.SongInformation.ArtistName ?? "");
        songInfo.SetString("SongGenre", chart?.SongInformation.Genre ?? "");
        songInfo.SetString("SongComment", chart?.SongInformation.Comment ?? "");

        songInfo.SetString("SongBPM", chart != null
            ? chart.SongInformation.Bpm.ToString("0.##", CultureInfo.InvariantCulture)
            : "");

        int? durationMs = chart?.SongInformation.DurationMs;
        songInfo.SetString("SongDuration", durationMs != null
            ? TimeSpan.FromMilliseconds(durationMs.Value).ToString(@"m\:ss")
            : "");

        double points = selectedNode?.GetTopSkillPoints().skillPoints ?? 0;
        songInfo.SetString("SongSkill", points > 0 ? points.ToString("0.00") : "");
    }

    public override void BuildDefaultLayout()
    {
        ui.AddChild(new UIImage
        {
            imageSource = ImageSource.System, resource = @"Graphics\5_background.jpg",
            renderOrder = -101, position = Vector3.Zero, name = "Background"
        });

        ui.AddChild(new UIImage
        {
            imageSource = ImageSource.System, resource = @"Graphics\SongSelect\back1.png",
            renderOrder = 1, position = new Vector3(174, 393, 0), rotation = new Vector3(0, 0, 1.63f), name = "Back1"
        });

        ui.AddChild(new UIImage
        {
            imageSource = ImageSource.System, resource = @"Graphics\SongSelect\back2.png",
            renderOrder = 2, position = new Vector3(126, 336, 0), rotation = new Vector3(0, 0, -0.06f), name = "Back2"
        });

        ui.AddChild(new UIImage
        {
            imageSource = ImageSource.System, resource = @"Graphics\SongSelect\top_bar.png",
            renderOrder = 12, name = "TopBar", size = new Vector2(1280, 1) //width stretched, height from texture
        });

        ui.AddChild(new UIImage
        {
            imageSource = ImageSource.System, resource = @"Graphics\SongSelect\panel_skill.png",
            renderOrder = 9, position = new Vector3(96, 225, 0), name = "PanelSkill"
        });

        ui.AddChild(new UIImage
        {
            imageSource = ImageSource.System, resource = @"Graphics\SongSelect\panel_bpm.png",
            renderOrder = 9, position = new Vector3(96, 300, 0), name = "PanelBpm"
        });

        ui.AddChild(new UIImage
        {
            imageSource = ImageSource.Dynamic, resource = "AlbumArt",
            position = new Vector3(320, 35, 0), renderOrder = 10, size = new Vector2(300, 300), name = "AlbumArt"
        });

        var skillText = ui.AddChild(new UIText("", 48));
        skillText.renderOrder = 11;
        skillText.bindings.Add(new UIBinding("text", "SongSkill"));
        skillText.outlineWidth = 0;
        skillText.style = UiTextStyle.Italic;
        skillText.fontSource = FontSource.System;
        skillText.font = "Futura PT Medium.otf";
        skillText.anchor = new Vector2(1, 1);
        skillText.position = new Vector3(315, 291, 0);
        skillText.name = "SkillText";

        var bpmText = ui.AddChild(new UIText("", 28));
        bpmText.renderOrder = 11;
        bpmText.bindings.Add(new UIBinding("text", "SongBPM"));
        bpmText.outlineWidth = 0;
        bpmText.style = UiTextStyle.Italic;
        bpmText.fontSource = FontSource.System;
        bpmText.font = "Futura PT Medium.otf";
        bpmText.anchor = new Vector2(1, 1);
        bpmText.position = new Vector3(315, 338, 0);
        bpmText.name = "BPMText";

        var commentText = ui.AddChild(new UIText("", 18));
        commentText.renderOrder = 11;
        commentText.position = new Vector3(0, 35, 0);
        commentText.bindings.Add(new UIBinding("text", "SongComment"));
        commentText.name = "CommentText";

        //ambient looping background video; the per-song preview movie is PreviewVideoBackground below
        ui.AddChild(new UINewVideoRenderer
        {
            video = SkinResourceRef.System(@"Graphics\5_background.mp4"),
            renderOrder = -100,
            name = "BackgroundVideo"
        });

        //the container is serializable (position + which row component it uses) but its rows are runtime
        var songSelect = ui.AddChild(new SongSelectionContainer());
        songSelect.position = new Vector3(765, 320, 0);
        songSelect.name = "SongSelect";

        //StatusPanel -> 3 pane component instances -> 5 rows each; a skin's json carries the panes instead
        var statusPanel = ui.AddChild(new StatusPanel());
        statusPanel.renderOrder = 6;
        statusPanel.BuildDefaultPanes();

        var sortMenu = ui.AddChild(new SortMenuContainer());
        sortMenu.component = "Components/SortMenu.json";
        sortMenu.position = new Vector3(1281, 35, 0);
        sortMenu.renderOrder = 8;
    }

    public override void OnLayoutReady()
    {
        //swaps to the selected chart's PREMOVIE as the selection changes, like the preview sound
        previewVideo = ui.AddChild(new PreviewVideoBackground());
        previewVideo.renderOrder = -99;
        previewVideo.position = Vector3.Zero;

        //the status panel, sort menu and selection container are part of the layout, so they may have
        //come from json
        statusPanel = ui.GetChild<StatusPanel>("StatusPanel")!;
        sortMenuContainer = ui.GetChild<SortMenuContainer>("SortMenuContainer")!;

        densityGraph1 = ui.AddChild(new DensityGraph((EInstrumentPart)CDTXMania.GetCurrentInstrument()));
        densityGraph1.position = new Vector3(CDTXMania.GetCurrentInstrument() == 0 ? 212 : 64, 720, 0);
        densityGraph1.renderOrder = 4;
        densityGraph1.name = "DensityGraph";
        densityGraph1.dontSerialize = true;

        songSearchMenu = ui.AddChild(new SongSearchMenu());
        songSearchMenu.renderOrder = 15;
        songSearchMenu.isVisible = false;
        songSearchMenu.anchor = new Vector2(0.5f, 0.5f);
        songSearchMenu.position = new Vector3(1280 / 2.0f, 720 / 2.0f, 0);
        songSearchMenu.dontSerialize = true;

        quickMenu = ui.AddChild(new QuickMenu());
        quickMenu.component = "Components/QuickMenu.json";
        quickMenu.renderOrder = 15;
        quickMenu.isVisible = false;
        quickMenu.anchor = new Vector2(0.5f, 0.5f);
        quickMenu.position = new Vector3(1280 / 2.0f, 720 / 2.0f, 0);
        quickMenu.dontSerialize = true;

        selectionContainer = ui.GetChild<SongSelectionContainer>("SongSelect");

        //a skin reload recreates the container empty; the first load runs from the loadPhase machine
        if (selectionContainer != null && sortCache.Count > 0)
        {
            SongNode? previousSelection = selectedNode;
            ApplySort(currentSort);
            if (previousSelection != null)
            {
                RestoreSelection(previousSelection);
            }
        }
    }

    public override void FirstUpdate()
    {
        //set initial sort menu container position to be default,
        //or in case of reloading the menu, whatever was last selected
        sortMenuContainer?.ShowSort(currentSort);
        
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
        
        //the container is pointed at one of these on demand in ApplySort
        foreach (SongDbSort sorter in SongDbSort.All)
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
        HandleGestures();

        int result = pendingResult;
        pendingResult = (int)EReturnValue.Continue;
        return result;
    }

    public override void HandleInput()
    {
        if (loadPhase != ELoadPhase.Complete)
        {
            return;
        }

        //on the arrow keys, so a submenu holding focus takes them with it
        sortMenuContainer?.HandleNavigation();
        pendingResult = selectionContainer.HandleNavigation();
    }

    /// <summary>
    /// The pad commands that are game rules rather than list navigation: opening the quick menu, the
    /// search key, changing difficulty and swapping guitar and bass. They are polled whatever holds focus,
    /// which is why they may only ever be pad commands — a navigation or decide key here would fire
    /// underneath whatever the player is actually driving.
    /// </summary>
    private void HandleGestures()
    {
        quickMenu.PollToggleGesture();
        songSearchMenu.PollOpenGesture();

        PollDifficultyCommand(EInstrumentPart.DRUMS, EPad.HH, EPadFlag.HH);
        PollDifficultyCommand(EInstrumentPart.DRUMS, EPad.HHO, EPadFlag.HH);
        PollDifficultyCommand(EInstrumentPart.GUITAR, EPad.B, EPadFlag.B);
        PollDifficultyCommand(EInstrumentPart.BASS, EPad.B, EPadFlag.B);

        PollSwapCommand(EInstrumentPart.GUITAR);
        PollSwapCommand(EInstrumentPart.BASS);
    }

    //two hits on the same pad change difficulty
    private void PollDifficultyCommand(EInstrumentPart part, EPad pad, EPadFlag flag)
    {
        if (!CDTXMania.Pad.bPressed(part, pad))
        {
            return;
        }

        commandHistory.Add(part, flag);

        if (commandHistory.CheckCommand([flag, flag], part))
        {
            IncrementDifficultyLevel();
        }
    }

    //two Y hits swap which instrument the guitar keys play
    private void PollSwapCommand(EInstrumentPart part)
    {
        if (!CDTXMania.Pad.bPressed(part, EPad.Y))
        {
            return;
        }

        commandHistory.Add(part, EPadFlag.Y);

        if (!commandHistory.CheckCommand([EPadFlag.Y, EPadFlag.Y], part))
        {
            return;
        }

        CDTXMania.Skin.soundChange.tPlay();
        CDTXMania.ConfigIni.bIsSwappedGuitarBass = !CDTXMania.ConfigIni.bIsSwappedGuitarBass;
        ChangeSelection(selectedNode, selectedChart);
    }

    public SongNode? selectedNode { get; private set; }
    public CChartData? selectedChart { get; private set; }
    public void ChangeSelection(SongNode? node, CChartData? chart)
    {
        selectedNode = node;
        selectedChart = chart;

        RefreshSongInfo();
        selectionContainer.UpdateSelectedSongAlbumArt();
        actPresound.tSelectionChanged(chart);
        previewVideo?.SelectionChanged(chart);
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

        //each sorted root remembers its own selection, so every sort keeps its scroll position
        selectionContainer.UpdateRoot(root);
    }

    //so switching sorts shows thumbnails immediately; the uploader throttles the GPU work
    private void PrewarmOtherSorts()
    {
        foreach (SongDbSort sorter in SongDbSort.All)
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