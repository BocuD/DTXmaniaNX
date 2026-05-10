using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Drawable;

namespace DTXMania.SongDb;

public class SongDBStatus : UIText
{
    private SongDb songDb => CDTXMania.SongDb;

    public SongDBStatus()
    {
        fontSize = 18;
        renderOrder = 100;
        text = "";
    }

    private SongDbScanStatus lastStatus;
    private DateTime lastFinishTime;

    public override void Draw(Matrix4x4 parentMatrix)
    {
        bool statusChanged = lastStatus != songDb.status || lastFinishTime != songDb.lastFinishTime;
        switch (songDb.status)
        {
            case SongDbScanStatus.Scanning:
                if (statusChanged)
                    SetText("Scanning song database...");
                break;
            
            case SongDbScanStatus.Unpacking:
                SetText($"Unpacking songs... {songDb.processDoneCount} / {songDb.processTotalCount}\n{songDb.processUnpackZipFile}");
                break;
            
            case SongDbScanStatus.Processing:
                SetText($"Processing songs... {songDb.processDoneCount} / {songDb.processTotalCount}\n{songDb.processSongDataPath}");
                break;
            
            case SongDbScanStatus.Idle:
                if (statusChanged)
                {
                    TimeSpan total = songDb.statusDuration[SongDbScanStatus.Scanning] +
                                     songDb.statusDuration[SongDbScanStatus.Unpacking] +
                                     songDb.statusDuration[SongDbScanStatus.Processing];
                    
                    if (total.TotalSeconds > 60)
                    {
                        SetText($"Song database scan completed in {(total.TotalSeconds / 60):F3} minutes");
                    }
                    else
                    {
                        SetText($"Song database scan completed in {(total.TotalMilliseconds / 1000):F3} seconds");
                    }
                }

                if (DateTime.Now - songDb.lastFinishTime > TimeSpan.FromSeconds(5))
                {
                    SetText("");
                }
                break;
        }

        lastStatus = songDb.status;
        lastFinishTime = songDb.lastFinishTime;
        
        base.Draw(parentMatrix);
    }
}