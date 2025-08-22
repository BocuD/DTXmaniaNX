using System.Diagnostics;
using System.Drawing;
using DTXMania.Core;
using DTXMania.SongDb;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using DTXMania.UI.Inspector;
using Hexa.NET.ImGui;
using SharpDX;
using Color = System.Drawing.Color;
using RectangleF = SharpDX.RectangleF;

namespace DTXMania;

[DebuggerDisplay("{GetTitle()} - {GetArtist()}")]
public class SongSelectionElement : UIGroup
{
    public string GetTitle() => node?.title ?? "Unknown Song";
    public string GetArtist()
    {
        if (node?.nodeType == SongNode.ENodeType.SONG)
        {
            CScore chart = node.charts.FirstOrDefault(x => x != null);
            return chart?.SongInformation.ArtistName ?? "Unknown Artist";
        }
        return "Unknown Artist";
    }

    public static void LoadSongSelectElementAssets()
    {
        bar = DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_bar.png"));
        boxClosed = DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_box_closed.png"));
        boxOpen = DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_box_open.png"));
        skillBarTex = DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_skillbar.png"));
        skillBarFillTex = DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_skillbar_fill.png"));
        
        //load lamp textures
        lampTextures = new DTXTexture[6];
        for (int i = 0; i < 6; i++)
        {
            lampTextures[i] = DTXTexture.LoadFromPath(CSkin.Path($@"Graphics\Lamp\{i:00}.png"));
        }
        lampGlow = DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\Lamp\GLOW.png"));
    }

    public static void DisposeSongSelectElementAssets()
    {
        bar?.Dispose();
        boxClosed?.Dispose();
        boxOpen?.Dispose();
        skillBarTex?.Dispose();
        skillBarFillTex?.Dispose();
        
        foreach (var tex in lampTextures)
        {
            tex?.Dispose();
        }

        lampTextures = [];
        lampGlow?.Dispose();
    }
    
    [AddChildMenu]
    public SongSelectionElement() : base("SongElement")
    {
        size = new Vector2(400, 80);
            
        backgroundImage = AddChild(new UIImage(bar));
        backgroundImage.anchor = new Vector2(0.0f, 0.5f);
        backgroundImage.position = new Vector3(-40.0f, 42.0f, 0.0f);
        backgroundImage.renderOrder = -1;
        backgroundImage.name = "background";
        
        albumArtImage = AddChild(new UIImage());
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
        songTitleText.isVisible = false;
        
        songArtistText = AddChild(new HorizontallyScrollingText(family, 12));
        songArtistText.position = new Vector3(80, 60, 0);
        songArtistText.fontColor = Color.Black;
        songArtistText.edgeColor = Color.White;
        songArtistText.anchor = new Vector2(0, 0.5f);
        songArtistText.renderOrder = 1;
        songArtistText.name = "artist";
        songArtistText.maximumWidth = 460.0f;
        songArtistText.isVisible = false;
        
        skillbar = AddChild(new UIImage(skillBarTex));
        skillbar.anchor = new Vector2(0.0f, 0.5f);
        skillbar.position = new Vector3(82.0f, 15.0f, 0.0f);
        skillbar.name = "skillbar";
        skillbar.renderOrder = 2;
        skillbar.isVisible = false;
        
        skillbarFill = AddChild(new UIImage(skillBarFillTex));
        skillbarFill.anchor = new Vector2(0.0f, 0.5f);
        skillbarFill.position = new Vector3(161.0f, 16.0f, 0.0f);
        skillbarFill.name = "skillbarFill";
        skillbarFill.size = new Vector2(286, 8);
        skillbarFill.renderOrder = 1;
        skillbarFill.isVisible = false;
        
        skillText = AddChild(new UIText(family, 12));
        skillText.position = new Vector3(105.0f, 16.0f, 0.0f);
        skillText.fontColor = Color.White;
        skillText.drawMode = CPrivateFont.DrawMode.Normal;
        skillText.fontStyle = FontStyle.Italic;
        skillText.UpdateFont();
        skillText.anchor = new Vector2(0, 0.5f);
        skillText.name = "skilltext";
        skillText.renderOrder = 2;
        skillText.isVisible = false;
        
        lamp = AddChild(new UIImage(lampTextures[0]));
        lamp.position = new Vector3(-40, 40, 0);
        lamp.anchor = new Vector2(0.5f, 0.5f);
        lamp.renderOrder = 1;
        lamp.name = "lamp";
        lamp.isVisible = false;
    }

    private UIImage backgroundImage;
    private UIImage albumArtImage;
    private HorizontallyScrollingText songTitleText;
    private HorizontallyScrollingText songArtistText;
    private UIImage skillbar;
    private UIImage skillbarFill;
    private UIText skillText;
    private UIImage lamp;

    private static DTXTexture bar;
    private static DTXTexture boxClosed;
    private static DTXTexture boxOpen;
    private static DTXTexture skillBarTex;
    private static DTXTexture skillBarFillTex;
    
    private static DTXTexture[] lampTextures = [];
    private static DTXTexture lampGlow;
    
    public SongNode? node { get; private set; }

    public void UpdateSongNode(SongNode newNode)
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
                    
                    //some dirty hacks to fix clipping issues with a bad texture (?)
                    backgroundImage.clipRect.X = 0;
                    backgroundImage.position.X = -40.0f;
                    break;
                
                case SongNode.ENodeType.BOX:
                    songTitleText.SetText(node.title);
                    songArtistText.SetText(node.childNodes.Count > 1
                        ? $"{node.childNodes.Count - 1} songs"
                        : "Empty collection");
                    backgroundImage.SetTexture(boxClosed);
                    
                    //some dirty hacks to fix clipping issues with a bad texture (?)
                    backgroundImage.clipRect.X = 1;
                    backgroundImage.position.X = -39.0f;
                    break;
                
                case SongNode.ENodeType.BACKBOX:
                    songTitleText.SetText("<< BACK");
                    songArtistText.SetText(CDTXMania.isJapanese ? "BOX を出ます。" : "Exit from the BOX.");
                    backgroundImage.SetTexture(boxOpen);
                    
                    //some dirty hacks to fix clipping issues with a bad texture (?)
                    backgroundImage.clipRect.X = 1;
                    backgroundImage.position.X = -39.0f;
                    break;
            }
            
            songTitleText.isVisible = !string.IsNullOrWhiteSpace(songTitleText.text);
            songArtistText.isVisible = !string.IsNullOrWhiteSpace(songArtistText.text);
            
            UpdateSkillbar();
            UpdateLamp();
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

    private void UpdateLamp()
    {
        if (node.nodeType != SongNode.ENodeType.SONG)
        {
            lamp.isVisible = false;
            return;
        }
        
        int bestLamp = 0;
        
        //get best lamp
        for (int index = 0; index < node.charts.Length; index++)
        {
            CScore? child = node.charts[index];
            if (child == null) continue;
            if (!child.HasChartForCurrentMode()) continue;

            if (child.SongInformation.BestRank[CDTXMania.GetCurrentInstrument()] != 99)
            {
                bestLamp = index + 1;
            }
        }
        
        lamp.SetTexture(lampTextures[bestLamp], false);
        lamp.isVisible = true;
    }

    public void PreRenderText()
    {
        //force render the text
        if (songTitleText.isVisible) songTitleText.RenderTexture();
        if (songArtistText.isVisible) songArtistText.RenderTexture();
        if (skillText.isVisible) skillText.RenderTexture();
    }

    public void UpdateSongThumbnail(DTXTexture? tex)
    {
        tex ??= SongSelectionContainer.fallbackPreImage;
        
        albumArtImage.SetTexture(tex, false);
        albumArtImage.clipRect = new RectangleF(0, 0, tex.Width, tex.Height);
    }
    
    public void SetHighlighted(bool highlighted)
    {
        songTitleText.scrollingEnabled = highlighted;
        songArtistText.scrollingEnabled = highlighted;
    }

    public override void DrawInspector()
    {
        base.DrawInspector();
        
        //open header by default
        if (ImGui.CollapsingHeader("Song Selection Element", ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (node != null)
            {
                CScore? chart = node.charts.FirstOrDefault(x => x != null);

                ImGui.Text($"Node Type: {node.nodeType}");
                ImGui.Text($"Title: {node.title}");
                
                if (chart != null)
                {
                    ImGui.Text($"Title Roman: {chart.SongInformation.TitleRoman}");
                    ImGui.Text($"Title Kana: {chart.SongInformation.TitleKana}");
                    
                    ImGui.Text($"Artist: {chart.SongInformation.ArtistName}");
                    ImGui.Text($"Artist Roman: {chart.SongInformation.ArtistNameRoman}");
                    ImGui.Text($"Artist Kana: {chart.SongInformation.ArtistNameKana}");
                }
            }
            else
            {
                ImGui.Text("Node: null");
            }
        }
    }
}