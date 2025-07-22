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
    
    protected override RichPresence Presence => new CDTXRichPresence
    {
        State = "In Menu",
        Details = "Selecting a song",
    };
    
    public CStageSongSelectionNew()
    {
        eStageID = EStage.SongSelection_4;
    }

    public override void InitializeBaseUI()
    {
        var family = new FontFamily(CDTXMania.ConfigIni.songListFont); 
        statusText = ui.AddChild(new UIText(family, 18));
        statusText.renderOrder = 100;

        UIImage bigAlbumArt = ui.AddChild(new UIImage());
        bigAlbumArt.position = new Vector3(300, 200, 0);
        bigAlbumArt.renderOrder = 1;
        bigAlbumArt.size = new Vector2(300, 300);

        selectionContainer = ui.AddChild(new SongSelectionContainer(songDb, bigAlbumArt));
        selectionContainer.position = new Vector3(800, 320, 0);
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
        if (hasScanned) return;
        
        Task.Run(() => songDb.ScanAsync(() => selectionContainer.UpdateRoot()));
        hasScanned = true;
    }

    public override int OnUpdateAndDraw()
    {
        base.OnUpdateAndDraw();

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
                return selectionContainer.HandleNavigation();
            }
        }

        return 0;
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
}