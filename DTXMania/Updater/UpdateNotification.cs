using DTXMania.UI.Drawable;

namespace DTXMania.Updater;

public class UpdateNotification : UIText, IProgress<DownloadProgress>
{
    //throttle redraws
    DateTime lastUpdate = DateTime.MinValue;
    int lastStep = -1;

    public void Report(DownloadProgress value)
    {
        bool stepChanged = value.Step != lastStep;
        if (!stepChanged && lastUpdate.AddMilliseconds(200) > DateTime.Now)
        {
            return;
        }

        lastUpdate = DateTime.Now;
        lastStep = value.Step;

        SetText($"Downloading update ({value.Step} / {value.StepCount})... {value.StepFraction:P0}");
    }
}