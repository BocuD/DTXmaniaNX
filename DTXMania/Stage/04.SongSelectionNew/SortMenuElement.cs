using System.Drawing;
using DTXMania.Core;
using DTXMania.SongDb;
using DTXMania.UI.Drawable;
using SharpDX;

namespace DTXMania;

public class SortMenuElement : UIGroup
{
    public SortMenuElement(SongDb.SongDb songDb, SongDbSort sorter) : base("SortMenuElement")
    {
        this.sorter = sorter;
        this.songDb = songDb;
        textElement = AddChild(new UIText(new FontFamily(CDTXMania.ConfigIni.songListFont), 18));
        textElement.SetText(sorter.Name);
        textElement.anchor = new Vector2(0.5f, 0.5f);
    }
    
    private UIText textElement;
    private SongDbSort sorter;
    private SongDb.SongDb songDb;

    public async Task<SongNode> Sort()
    {
        return await sorter.Sort(songDb);
    }
}