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
    public SongSelectionElement() : base("SongElement")
    {
        size = new SharpDX.Vector2(400, 80);
        
        albumArtImage = AddChild(new UIImage(DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_preimage default.png"))));
        albumArtImage.size = new SharpDX.Vector2(65, 65);
        albumArtImage.position = new SharpDX.Vector3(40, 40, 0);
        albumArtImage.anchor = new SharpDX.Vector2(0.5f, 0.5f);
        
        FontFamily family = new(CDTXMania.ConfigIni.songListFont);
        
        songTitleText = AddChild(new UIText(family, 18));
        songTitleText.position = new SharpDX.Vector3(80, 40, 0);
        songTitleText.anchor = new SharpDX.Vector2(0, 0.5f);
        
        songArtistText = AddChild(new UIText(family, 12));
        songArtistText.position = new SharpDX.Vector3(80, 65, 0);
        songArtistText.anchor = new SharpDX.Vector2(0, 0.5f);
    }

    private UIImage albumArtImage;
    private UIText songTitleText;
    private UIText songArtistText;

    public SongNode? node { get; private set; }

    public void UpdateSongNode(SongNode newNode, DTXTexture? tex)
    {
        if (node != newNode)
        {
            node = newNode;
            
            switch (newNode.nodeType)
            {
                case SongNode.ENodeType.SONG:
                    songTitleText.SetText(node.title);
                    CScore chart = node.charts.FirstOrDefault(x => x != null);
                    songArtistText.SetText(chart != null ? chart.SongInformation.ArtistName : "");
                    break;
                
                case SongNode.ENodeType.BOX:
                    songTitleText.SetText(node.title);
                    songArtistText.SetText(node.childNodes.Count > 1
                        ? $"{node.childNodes.Count - 1} songs"
                        : "Empty collection");
                    break;
                
                case SongNode.ENodeType.BACKBOX:
                    songTitleText.SetText("<< BACK");
                    songArtistText.SetText(CDTXMania.isJapanese ? "BOX を出ます。" : "Exit from the BOX.");
                    break;
            }
            

            if (tex == null)
            {
                tex = SongSelectionContainer.fallbackPreImage;
            }

            albumArtImage.SetTexture(tex, false);
            albumArtImage.clipRect = new SharpDX.RectangleF(0, 0, tex.Width, tex.Height);
        }
    }
}