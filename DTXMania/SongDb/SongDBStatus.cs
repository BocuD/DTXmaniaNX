using System.Drawing;
using DTXMania.Core;
using DTXMania.UI.Drawable;
using SharpDX;

namespace DTXMania.SongDb;

public class SongDBStatus : UIText
{
    private SongDb songDb => CDTXMania.SongDb;

    public SongDBStatus()
    {
        fontFamily = new FontFamily(CDTXMania.ConfigIni.songListFont);
        fontSize = 18;
        renderOrder = 100;
        UpdateFont();
    }

    private SongDbScanStatus lastStatus;
    public override void Draw(Matrix parentMatrix)
    {
        switch (songDb.status)
        {
            case SongDbScanStatus.Scanning:
                if (lastStatus != SongDbScanStatus.Scanning)
                    SetText("Scanning song database...");
                break;
            
            case SongDbScanStatus.Processing:
                SetText($"Processing songs... {songDb.processDoneCount} / {songDb.processTotalCount}\n{songDb.processSongDataPath}");
                break;
            
            case SongDbScanStatus.Idle:
                if (lastStatus != SongDbScanStatus.Idle)
                    SetText("");
                break;
        }

        lastStatus = songDb.status;
        
        base.Draw(parentMatrix);
    }
}