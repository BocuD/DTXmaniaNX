using System.Drawing;
using DTXMania.Core;
using DTXMania.SongDb;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using DTXMania.UI.Inspector;

namespace DTXMania;

public class SongSelectionElement : UIGroup
{
    [AddChildMenu]
    public static SongSelectionElement Create()
    {
        var element = new SongSelectionElement();
        element.size = new SharpDX.Vector2(400, 80);
        
        element.albumArtImage = element.AddChild(new UIImage(DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_preimage default.png"))));
        element.albumArtImage.size = new SharpDX.Vector2(65, 65);
        element.albumArtImage.position = new SharpDX.Vector3(40, 40, 0);
        element.albumArtImage.anchor = new SharpDX.Vector2(0.5f, 0.5f);
        
        var family = new FontFamily(CDTXMania.ConfigIni.songListFont);
        
        element.songTitleText = element.AddChild(new UIText(family, 18));
        element.songTitleText.position = new SharpDX.Vector3(80, 40, 0);
        element.songTitleText.anchor = new SharpDX.Vector2(0, 0.5f);
        
        element.songArtistText = element.AddChild(new UIText(family, 12));
        element.songArtistText.position = new SharpDX.Vector3(80, 65, 0);
        element.songArtistText.anchor = new SharpDX.Vector2(0, 0.5f);
        
        return element;
    }

    private UIImage albumArtImage;
    private UIText songTitleText;
    private UIText songArtistText;
    
    public SongSelectionElement() : base("SongElement")
    {
        
    }

    public SongNode node { get; private set; }

    public void UpdateSongNode(SongNode newNode, DTXTexture? tex)
    {
        if (node != newNode)
        {
            node = newNode;
            
            songTitleText.SetText(node.title);
            
            CScore chart = node.charts.FirstOrDefault(x => x != null);

            songArtistText.SetText(chart.SongInformation.ArtistName);

            if (tex == null)
            {
                tex = SongSelectionContainer.fallbackPreImage;
            }

            albumArtImage.SetTexture(tex, false);
            albumArtImage.clipRect = new SharpDX.RectangleF(0, 0, tex.Width, tex.Height);
        }
    }
}