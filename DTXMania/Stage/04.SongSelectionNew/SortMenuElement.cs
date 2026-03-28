using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.SongDb.Sorting;
using DTXMania.UI.Drawable;
using FDK;

namespace DTXMania;

public class SortMenuElement : UIGroup
{
    public SortMenuElement(SongDb.SongDb songDb, SongDbSort sorter) : base("SortMenuElement")
    {
        this.sorter = sorter;
        this.songDb = songDb;
        
        textElement = AddChild(new UIText(sorter.Name, 18));
        textElement.anchor = new Vector2(0.5f, 0.5f);
        textElement.isVisible = false;
        
        icon = AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path($@"Graphics\Sorting\{sorter.IconName}.png"))));
        icon.anchor = new Vector2(0.5f, 0.5f);
        
        string path = CSkin.Path($@"Graphics\Sorting\{sorter.IconName}.wav");
        sound = CDTXMania.SoundManager.tGenerateSound(path);
        sound.nVolume = 80;
    }
    
    private UIText textElement;
    private UIImage icon;
    public SongDbSort sorter { get; private set; }
    private SongDb.SongDb songDb;
    private CSound sound;

    public void PlaySound()
    {
        if (sound != null)
        {
            sound.tStartPlaying(false);
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        
        CDTXMania.SoundManager.tDiscard(sound);
        sound = null;
    }
}