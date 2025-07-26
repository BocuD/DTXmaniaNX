using System.Diagnostics;
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
    private SongDb.SongDb songDb => CDTXMania.SongDb;
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
        new SortByLastPlayed(),
        new SortByAllSongs()
    ];
    
    public override void InitializeBaseUI()
    {
        UIImage bigAlbumArt = ui.AddChild(new UIImage());
        bigAlbumArt.position = new Vector3(320, 35, 0);
        bigAlbumArt.renderOrder = 1;
        bigAlbumArt.size = new Vector2(300, 300);

        selectionContainer = ui.AddChild(new SongSelectionContainer(songDb, bigAlbumArt));
        selectionContainer.position = new Vector3(765, 320, 0);
        
        sortMenuContainer = ui.AddChild(new SortMenuContainer(songDb, sorters));
        sortMenuContainer.position = new Vector3(1281, 35, 0);

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

    private bool hasScanned;

    public override void FirstUpdate()
    {
        CDTXMania.Skin.soundTitle.tStop();
        
        //set initial sort menu container position to be default,
        //or in case of reloading the menu, whatever was last selected
        sortMenuContainer.SetCurrentSelection(currentSort);

        if (hasScanned)
        if (songDb.hasEverScanned)
        {
            if (lastInstrument == CDTXMania.GetCurrentInstrument())
            {
                //restore existing root
                RequestUpdateRoot(currentSongRoot);
            }
            else
            {
                ApplySort(sortMenuContainer.currentSelection.sorter);
            }

            return;
        }

        if (songDb.status == SongDbScanStatus.Idle)
        {
            Task.Run(() => songDb.StartScan(() => { ApplySort(sortMenuContainer.currentSelection.sorter); }));
        }
    }

    public override int OnUpdateAndDraw()
    {
        base.OnUpdateAndDraw();

        if (updateRootRequested)
        {
            selectionContainer.UpdateRoot(currentSongRoot);
            updateRootRequested = false;
        }
        
        if (songDb.status == SongDbScanStatus.Idle)
        {
            if (songDb.totalSongs == 0)
            {
                selectionContainer.isVisible = false;
            }
            else
            {
                selectionContainer.isVisible = true;

                if (currentSongRoot == null)
                {
                    ApplySort(sortMenuContainer.currentSelection.sorter);
                }
                
                actPresound.OnUpdateAndDraw();
                
                sortMenuContainer.HandleNavigation();
                statusPanel.HandleNavigation();
                return selectionContainer.HandleNavigation();
            }
        }
        else
        {
            selectionContainer.isVisible = false;
        }

        return 0;
    }

    private bool updateRootRequested;
    private SongNode? currentSongRoot;
    
    public void RequestUpdateRoot(SongNode newRoot)
    {
        updateRootRequested = true;
        currentSongRoot = newRoot;
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

    public int targetDifficultyLevel { get; private set; } = 0;
    public void IncrementDifficultyLevel()
    {
        var nextAvailableLevel = targetDifficultyLevel;
        
        //find first available new level
        for (int i = 0; i < 5; i++)
        {
            int newLevel = (targetDifficultyLevel + i) % 5;
            if (newLevel == targetDifficultyLevel) continue;

            int currentInstrument = CDTXMania.GetCurrentInstrument();
            
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
    
    public int GetClosestLevelToTargetForSong(SongNode song)
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
    private int lastInstrument;
    public bool isScrolling => selectionContainer.isScrolling;

    public void ApplySort(SongDbSort sorter)
    {
        if (sortLocked)
        {
            Trace.TraceWarning("Sort operation skipped as another sort is in progress");
            return;
        }

        //invalidate cache if instrument changed
        if (CDTXMania.GetCurrentInstrument() != lastInstrument)
        {
            sortCache.Clear();
        }

        lastInstrument = CDTXMania.GetCurrentInstrument();
        currentSort = sorter;

        //apply sort
        Task.Run(async () =>
        {
            try
            {
                sortLocked = true;
                if (!sortCache.TryGetValue(sorter, out SongNode? newRoot))
                {
                    newRoot = await sorter.Sort(songDb);
                    sortCache[sorter] = newRoot;
                }
                else
                {
                    newRoot.CurrentSelection = newRoot.childNodes[0];
                }
                CDTXMania.StageManager.stageSongSelectionNew.RequestUpdateRoot(newRoot);
            }
            catch (Exception e)
            {
                Trace.TraceError("Sorting failed: " + e.Message);
            }
            finally
            {
                sortLocked = false;
            }
        });
    }

    //reload current view
    public void Reload()
    {
        sortCache.Clear();
        ApplySort(currentSort);
    }
}