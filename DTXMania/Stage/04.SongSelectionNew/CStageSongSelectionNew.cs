using System.Diagnostics;
using DiscordRPC;
using DTXMania.Core;
using DTXMania.SongDb;
using DTXMania.SongDb.Sorting;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using SharpDX;

namespace DTXMania;

public class CStageSongSelectionNew : CStage
{
    private SongDb.SongDb songDb => CDTXMania.SongDb;
    //private SongSelectionContainer selectionContainer;
    private SortMenuContainer sortMenuContainer;
    private UIImage bigAlbumArt;
    private CActSelectPresound actPresound;
    private StatusPanel statusPanel;
    private CActSelectBackgroundAVI actBackgroundVideoAVI;
    private CAVI cAviBackgroundVideo;

    private SongSelectionContainer currentSelectionContainer;
    
    private ELoadPhase loadPhase = ELoadPhase.Initialize;
    
    private enum ELoadPhase
    {
        Initialize,
        Prepare,
        CacheThumbnails,
        ReadyToOpen,
        Complete
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
        listChildActivities.Add(actBackgroundVideoAVI = new CActSelectBackgroundAVI());
        
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
        bigAlbumArt.renderOrder = 2;
        bigAlbumArt.size = new Vector2(300, 300);
        
        sortMenuContainer = ui.AddChild(new SortMenuContainer(songDb, sorters));
        sortMenuContainer.position = new Vector3(1281, 35, 0);
        sortMenuContainer.renderOrder = 1;

        statusPanel = ui.AddChild(new StatusPanel());

        //create songselectioncontainer for each sorter
        foreach (SongDbSort sorter in sorters)
        {
            SongSelectionContainer container = ui.AddChild(new SongSelectionContainer(songDb, bigAlbumArt));
            container.name = "SongSelect " + sorter.Name;
            container.position = new Vector3(765, 320, 0);
            selectionContainers[sorter] = container;
        }
    }

    public override void InitializeDefaultUI()
    {
        DTXTexture bgTex = DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_background.jpg"));
        UIImage bg = ui.AddChild(new UIImage(bgTex));
        bg.renderOrder = -100;
        bg.position = Vector3.Zero;
        bg.name = "Background";
        
        LegacyDrawable backgroundVideo = ui.AddChild(new LegacyDrawable(() => actBackgroundVideoAVI.tUpdateAndDraw()));
        backgroundVideo.renderOrder = -99;
        backgroundVideo.name = "BackgroundVideo";
    }

    public override void OnManagedCreateResources()
    {
        cAviBackgroundVideo = new CAVI(1290, CSkin.Path(@"Graphics\5_background.mp4"), "", 20.0);
        cAviBackgroundVideo.OnDeviceCreated();
        if (cAviBackgroundVideo.avi != null)
        {
            actBackgroundVideoAVI.bLoop = true;
            actBackgroundVideoAVI.Start(EChannel.MovieFull, cAviBackgroundVideo, 0, -1);
            Trace.TraceInformation("Started song select background video");
        }
        
        SongSelectionElement.LoadSongSelectElementAssets();
        
        base.OnManagedCreateResources();
    }

    public override void OnManagedReleaseResources()
    {
        if (cAviBackgroundVideo != null)
        {
            cAviBackgroundVideo.Dispose();
            cAviBackgroundVideo = null;
        }
        actBackgroundVideoAVI.Stop();
        
        base.OnManagedReleaseResources();
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
            try
            {
                if (!sortCache.TryGetValue(sorter, out SongNode? rootNode))
                {
                    rootNode = sorter.Sort(songDb).Result;
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
            }
            catch (Exception e)
            {
                Trace.TraceError($"Failed to prepare container for {sorter.Name}: {e.Message}");
            }
        }
        
        //enable the current sort
        ApplySort(currentSort);
        
        Trace.TraceInformation("Sort cache preparation complete.");
        
        TimeSpan elapsed = DateTime.Now - startTime;
        Trace.TraceInformation($"Sort cache prepared in {elapsed} s.");
        
        loadPhase = ELoadPhase.CacheThumbnails;
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
        return currentSelectionContainer.HandleNavigation();
    }
    
    public SongNode? selectedNode { get; private set; }
    public CScore? selectedChart { get; private set; }
    public void ChangeSelection(SongNode? node, CScore? chart)
    {
        selectedNode = node;
        selectedChart = chart;
        
        actPresound.tSelectionChanged(chart);
        statusPanel.SelectionChanged(node, chart);
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
                if( !File.Exists( strbsc ) )
                    CDTXMania.Skin.soundChange.tPlay();
                break;
            case 1:
                CDTXMania.Skin.soundAdvanced.tPlay();
                string stradv = CSkin.Path( @"Sounds\Advanced.ogg" );
                if( !File.Exists( stradv ) )
                    CDTXMania.Skin.soundChange.tPlay();
                break;
            case 2:
                CDTXMania.Skin.soundExtreme.tPlay();
                string strext = CSkin.Path( @"Sounds\Extreme.ogg" );
                if( !File.Exists( strext ) )
                    CDTXMania.Skin.soundChange.tPlay();
                break;
            case 3:
                CDTXMania.Skin.soundMaster.tPlay();
                string strmas = CSkin.Path( @"Sounds\Master.ogg" );
                if( !File.Exists( strmas ) )
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
        CScore? chart = null;
        
        //check if the closest level is valid
        chart = closestLevelToTarget > newSelection.charts.Length - 1 
            ? newSelection.charts.FirstOrDefault()
            : newSelection.charts[closestLevelToTarget];
        
        ChangeSelection(currentSelectionContainer.currentSelection, chart);   
    }

    //reload current view
    public void Reload()
    {
        PrepareSelectionContainers();
        ApplySort(currentSort);
    }
}