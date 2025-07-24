using System.Drawing;
using DTXMania.Core;
using DTXMania.SongDb;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using DTXMania.UI.Inspector;
using SharpDX;
using Color = System.Drawing.Color;
using RectangleF = SharpDX.RectangleF;

namespace DTXMania;

public class SongSelectionElement : UIGroup
{
    [AddChildMenu]
    public SongSelectionElement() : base("SongElement")
    {
        size = new Vector2(400, 80);

        bar = DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_bar.png"));
        boxClosed = DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_box_closed.png"));
        boxOpen = DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_box_open.png"));
            
        backgroundImage = AddChild(new UIImage(bar));
        backgroundImage.anchor = new Vector2(0.0f, 0.5f);
        backgroundImage.position = new Vector3(-40.0f, 42.0f, 0.0f);
        backgroundImage.renderOrder = -1;
        backgroundImage.name = "background";
        
        albumArtImage = AddChild(new UIImage(DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_preimage default.png"))));
        albumArtImage.size = new Vector2(65, 65);
        albumArtImage.position = new Vector3(40, 40, 0);
        albumArtImage.anchor = new Vector2(0.5f, 0.5f);
        albumArtImage.renderOrder = 1;
        albumArtImage.name = "albumArt";
        
        FontFamily family = new(CDTXMania.ConfigIni.songListFont);
        
        songTitleText = AddChild(new HorizontallyScrollingText(family, 18));
        songTitleText.fontColor = Color.Black;
        songTitleText.edgeColor = Color.White;
        songTitleText.position = new Vector3(78, 38, 0);
        songTitleText.anchor = new Vector2(0, 0.5f);
        songTitleText.renderOrder = 1;
        songTitleText.name = "title";
        songTitleText.maximumWidth = 460.0f;
        
        songArtistText = AddChild(new HorizontallyScrollingText(family, 12));
        songArtistText.position = new Vector3(80, 60, 0);
        songArtistText.fontColor = Color.Black;
        songArtistText.edgeColor = Color.White;
        songArtistText.anchor = new Vector2(0, 0.5f);
        songArtistText.renderOrder = 1;
        songArtistText.name = "artist";
        songArtistText.maximumWidth = 460.0f;
        
        skillbar = AddChild(new UIImage(DTXTexture.LoadFromPath(CSkin.Path($@"Graphics\5_skillbar.png"))));
        skillbar.anchor = new Vector2(0.0f, 0.5f);
        skillbar.position = new Vector3(82.0f, 15.0f, 0.0f);
        skillbar.name = "skillbar";
        skillbar.renderOrder = 1;
        skillbar.isVisible = false;
        
        skillbarFill = AddChild(new UIImage(DTXTexture.LoadFromPath(CSkin.Path($@"Graphics\5_skillbar_fill.png"))));
        skillbarFill.anchor = new Vector2(0.0f, 0.5f);
        skillbarFill.position = new Vector3(161.0f, 16.0f, 0.0f);
        skillbarFill.name = "skillbarFill";
        skillbarFill.size = new Vector2(286, 8);
        skillbarFill.renderOrder = 2;
        skillbarFill.isVisible = false;
        
        skillText = AddChild(new UIText(family, 12));
        skillText.position = new Vector3(105.0f, 16.0f, 0.0f);
        skillText.fontColor = Color.White;
        skillText.drawMode = CPrivateFont.DrawMode.Normal;
        skillText.fontStyle = FontStyle.Italic;
        skillText.UpdateFont();
        skillText.anchor = new Vector2(0, 0.5f);
        skillText.name = "skilltext";
        skillbarFill.renderOrder = 2;
        skillText.isVisible = false;
    }

    private UIImage backgroundImage;
    private UIImage albumArtImage;
    private HorizontallyScrollingText songTitleText;
    private HorizontallyScrollingText songArtistText;
    private UIImage skillbar;
    private UIImage skillbarFill;
    private UIText skillText;

    private DTXTexture bar;
    private DTXTexture boxClosed;
    private DTXTexture boxOpen;

    public override void Dispose()
    {
        base.Dispose();
        
        if (bar != null) bar.Dispose();
        if (bar != null) boxClosed.Dispose();
        if (bar != null) boxOpen.Dispose();
    }

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
                    backgroundImage.SetTexture(bar);
                    break;
                
                case SongNode.ENodeType.BOX:
                    songTitleText.SetText(node.title);
                    songArtistText.SetText(node.childNodes.Count > 1
                        ? $"{node.childNodes.Count - 1} songs"
                        : "Empty collection");
                    backgroundImage.SetTexture(boxClosed);
                    break;
                
                case SongNode.ENodeType.BACKBOX:
                    songTitleText.SetText("<< BACK");
                    songArtistText.SetText(CDTXMania.isJapanese ? "BOX を出ます。" : "Exit from the BOX.");
                    backgroundImage.SetTexture(boxOpen);
                    break;
            }
            
            UpdateSkillbar();
            
            if (tex == null)
            {
                tex = SongSelectionContainer.fallbackPreImage;
            }

            albumArtImage.SetTexture(tex, false);
            albumArtImage.clipRect = new RectangleF(0, 0, tex.Width, tex.Height);
        }
    }

    private void UpdateSkillbar()
    {
        if (node.nodeType == SongNode.ENodeType.SONG)
        {
            var skill = node.GetTopSkillPoints();

            bool drawSkill = skill.skillPoints > 0;
            skillbar.isVisible = drawSkill;
            skillbarFill.isVisible = drawSkill;
            skillText.isVisible = drawSkill;

            if (drawSkill)
            {
                double ratio = skill.skillPoints / skill.maxSkillPoints;
                skillText.SetText($"{skill.skillPoints:0.00}");
                skillbarFill.size = new Vector2(286.0f * (float)ratio, 8);
            }
        }
        else
        {
            skillbar.isVisible = false;
            skillbarFill.isVisible = false;
            skillText.isVisible = false;
        }
    }

    public void UpdateSongThumbnail(DTXTexture? tex)
    {
        if (tex == null)
        {
            tex = SongSelectionContainer.fallbackPreImage;
        }

        albumArtImage.SetTexture(tex, false);
        albumArtImage.clipRect = new RectangleF(0, 0, tex.Width, tex.Height);
    }
}