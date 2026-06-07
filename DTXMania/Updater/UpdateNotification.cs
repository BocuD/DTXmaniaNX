using DTXMania.UI.Drawable;

namespace DTXMania.Updater;

public class UpdateNotification : UIText, IProgress<double>
{
    //make sure we don't update this more than every second
    DateTime lastUpdate = DateTime.MinValue;
    
    public void Report(double value)
    {
        if (lastUpdate.AddSeconds(1) > DateTime.Now)
        {
            return;
        }
        
        lastUpdate = DateTime.Now;
        
        SetText($"Downloading update... {value:P0}");
    }
}