using System.Drawing;
using DiscordRPC;
using DTXMania.Core;
using DTXMania.SongDb;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using SharpDX;

namespace DTXMania;

public class CStageSongSelectionNew : CStage
{
    private SongDb.SongDb songDb = new();
    private SongSelectionContainer selectionContainer;
    private SortMenuContainer sortMenuContainer;
    private CActSelectPresound actPresound;
    private StatusPanel statusPanel;
    
    protected override RichPresence Presence => new CDTXRichPresence
    {
        State = "In Menu",
        Details = "Selecting a song",
    };
    
    public CStageSongSelectionNew()
    {
        eStageID = EStage.SongSelection_4;
        
        listChildActivities.Add(actPresound = new CActSelectPresound());
    }

    public override void InitializeBaseUI()
    {
        FontFamily family = new(CDTXMania.ConfigIni.songListFont); 
        statusText = ui.AddChild(new UIText(family, 18));
        statusText.renderOrder = 100;

        UIImage bigAlbumArt = ui.AddChild(new UIImage());
        bigAlbumArt.position = new Vector3(320, 35, 0);
        bigAlbumArt.renderOrder = 1;
        bigAlbumArt.size = new Vector2(300, 300);

        selectionContainer = ui.AddChild(new SongSelectionContainer(songDb, bigAlbumArt));
        selectionContainer.position = new Vector3(765, 320, 0);

        SongDbSort[] sorters =
        [
            new SortDefault(),
            new SortByBox(),
            new SortByTitle(),
            new SortByArtist(),
            new SortByDifficulty(),
            new SortByLevel(),
            new SortByLastPlayed(),
            new SortByAllSongs()
        ];
        sortMenuContainer = ui.AddChild(new SortMenuContainer(songDb, sorters));
        sortMenuContainer.position = new Vector3(1280, 35, 0);

        statusPanel = ui.AddChild(new StatusPanel());
    }

    public override void InitializeDefaultUI()
    {
        DTXTexture bgTex = DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_background.jpg"));
        UIImage bg = ui.AddChild(new UIImage(bgTex));
        bg.renderOrder = -100;
        bg.position = Vector3.Zero;
        bg.name = "Background";
    }

    private UIText statusText;
    private bool hasScanned = false;

    public override void FirstUpdate()
    {
        CDTXMania.Skin.soundTitle.tStop();
        
        if (hasScanned)
        {
            RequestUpdateRoot(songDb.songNodeRoot);
            return;
        }
        
        Task.Run(() => songDb.ScanAsync(() => RequestUpdateRoot(songDb.songNodeRoot)));
        hasScanned = true;
    }

    public override int OnUpdateAndDraw()
    {
        base.OnUpdateAndDraw();

        if (updateRootRequested)
        {
            selectionContainer.UpdateRoot(newSongRoot);
            updateRootRequested = false;
        }
        
        UpdateSongDbStatus();

        if (songDb.status == SongDbScanStatus.Idle)
        {
            if (songDb.totalSongs == 0)
            {
                selectionContainer.isVisible = false;
                statusText.isVisible = true;
                statusText.SetText("No songs found.");
            }
            else
            {
                selectionContainer.isVisible = true;
                
                actPresound.OnUpdateAndDraw();
                
                sortMenuContainer.HandleNavigation();
                statusPanel.HandleNavigation();
                return selectionContainer.HandleNavigation();
            }
        }

        return 0;
    }

    private bool updateRootRequested;
    private SongNode? newSongRoot;
    
    public void RequestUpdateRoot(SongNode newRoot)
    {
        updateRootRequested = true;
        newSongRoot = newRoot;
    }
    
    public SongNode node { get; private set; }
    public CScore chart { get; private set; }
    public void ChangeSelection(SongNode node, CScore chart)
    {
        this.node = node;
        this.chart = chart;
        actPresound.tSelectionChanged(chart);
        statusPanel.SelectionChanged(node, chart);
    }
    
    private void UpdateSongDbStatus()
    {
        switch (songDb.status)
        {
            case SongDbScanStatus.Scanning:
                statusText.SetText("Scanning song database...");
                break;
            
            case SongDbScanStatus.Processing:
                statusText.SetText(
                    $"Processing songs... {songDb.processDoneCount} / {songDb.processTotalCount}\n{songDb.processSongDataPath}");
                break;
        }
        
        statusText.isVisible = songDb.status != SongDbScanStatus.Idle;
    }

    public int targetDifficultyLevel { get; private set; } = 0;
    public void IncrementDifficultyLevel()
    {
        var nextAvailableLevel = targetDifficultyLevel;
        
        //find first available new level
        for (int i = 0; i < 5; i++)
        {
            int newLevel = (targetDifficultyLevel + i) % 5;
            if (newLevel == targetDifficultyLevel) continue;

            int currentInstrument =
                CDTXMania.ConfigIni.bDrumsEnabled ? 0
                : CDTXMania.ConfigIni.bIsSwappedGuitarBass ? 2 : 1;
            
            //check if this chart is valid
            var chart = node.charts[newLevel];
            if (chart == null) continue;

            if (chart.SongInformation.chipCountByInstrument[currentInstrument] > 0)
            {
                nextAvailableLevel = newLevel;
                break;
            }
        }

        if (nextAvailableLevel == targetDifficultyLevel) return;
        
        targetDifficultyLevel = nextAvailableLevel;
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
}