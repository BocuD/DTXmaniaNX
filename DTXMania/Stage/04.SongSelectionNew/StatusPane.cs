using DTXMania.Core;
using DTXMania.SongDb;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using SharpDX;

namespace DTXMania;

public class StatusPane : UIGroup
{
    public StatusPane(EInstrumentPart instrument)
    {
        this.instrument = instrument;
        name = instrument.ToString();
        
        UIImage background = AddChild(new UIImage(DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_difficulty_panel.png"))));
        background.anchor = new Vector2(0.0f, 1.0f);
        
        txLevelNumber = DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_level number.png"));

        difficultyFrame = AddChild(new UIImage(DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_difficultyframe.png"))));
        difficultyFrame.anchor = new Vector2(0.0f, 1.0f);
        difficultyFrame.position = new Vector3(-7.0f, 5.0f, 0.0f);
        
        skillIcon = DTXTexture.LoadFromPath(CSkin.Path($@"Graphics\Rank\skill.png"));
        rankIcons = new DTXTexture[7];
        for (int i = 0; i < rankIcons.Length; i++) 
        {
            rankIcons[i] = DTXTexture.LoadFromPath(CSkin.Path($@"Graphics\Rank\rank_{i}.png"));
        }
        
        //create image holders for left/right icons
        rankIconHolders = new UIImage[5];
        skillIconHolders = new UIImage[5];
        
        for (int i = 0; i < 5; i++)
        {
            skillIconHolders[i] = AddChild(new UIImage(skillIcon));
            skillIconHolders[i].anchor = new Vector2(0.0f, 1.0f);
            skillIconHolders[i].position = new Vector3(14.0f, -49.0f - verticalSpacing * i, 0.0f);
            skillIconHolders[i].size = new Vector2(27.0f, 27.0f);
            skillIconHolders[i].name = $"Skill_{i}";
            skillIconHolders[i].isVisible = false;
            
            rankIconHolders[i] = AddChild(new UIImage(rankIcons[0]));
            rankIconHolders[i].anchor = new Vector2(0.0f, 1.0f);
            rankIconHolders[i].position = new Vector3(60.0f, -49.0f - verticalSpacing * i, 0.0f);
            rankIconHolders[i].size = new Vector2(27.0f, 27.0f);
            rankIconHolders[i].name = $"Rank_{i}";
            rankIconHolders[i].isVisible = false;
        }
    }

    private DTXTexture skillIcon;
    private DTXTexture[] rankIcons;
    private DTXTexture txLevelNumber;
    
    private UIImage difficultyFrame;
    private UIImage[] skillIconHolders;
    private UIImage[] rankIconHolders;
    
    private EInstrumentPart instrument;
    
    private float verticalSpacing = 74;
    private Vector3 textOffset = new(125, -41, 0);
    
    public SongNode? song;

    public override void Draw(Matrix parentMatrix)
    {
        base.Draw(parentMatrix);

        Matrix textTranslation = Matrix.Translation(textOffset) * localTransformMatrix * parentMatrix;
        
        difficultyFrame.position = new Vector3(-7.0f, 5.0f - verticalSpacing * CDTXMania.StageManager.stageSongSelectionNew.GetClosestLevelToTargetForSong(song), 0.0f);

        for (int c = 0; c < 5; c++)
        {
            bool hideDifficulty = song != null && (song.filteredInstrumentPart != EInstrumentPart.UNKNOWN &&
                                  song.filteredInstrumentPart != instrument);
            if (song == null
                || song.nodeType != SongNode.ENodeType.SONG
                || song.charts[c] == null 
                || song.charts[c].SongInformation.chipCountByInstrument[(int)instrument] == 0
                || hideDifficulty)
            {
                tDrawDifficulty(textTranslation, txLevelNumber, 1.0f, "-.--");
                rankIconHolders[c].isVisible = false;
                skillIconHolders[c].isVisible = false;
            }
            else
            {
                //get difficulty number
                double dbLevel = song.charts[c].SongInformation.GetLevel((int)instrument);
                tDrawDifficulty(textTranslation, txLevelNumber, 1.0f, $"{dbLevel:0.00}");
                
                int rank = song.charts[c].SongInformation.BestRank[(int)instrument];
                if (rank != (int)CScoreIni.ERANK.UNKNOWN)
                {
                    rankIconHolders[c].SetTexture(rankIcons[rank], false);
                    rankIconHolders[c].isVisible = true;

                    skillIconHolders[c].isVisible = song.charts[c].countSkill;
                    
                    //get score as well
                    double score = song.charts[c].SongInformation.HighCompletionRate[(int)instrument];
                    
                    var completionRate = textTranslation * Matrix.Translation(-102 * CDTXMania.renderScale, 11 * CDTXMania.renderScale, 0);
                    tDrawDifficulty(completionRate, txLevelNumber, 0.6f, $"{score:0.00}%");
                }
                else
                {
                    rankIconHolders[c].isVisible = false;
                    skillIconHolders[c].isVisible = false;
                }
            }

            textTranslation *= Matrix.Translation(0, -verticalSpacing * CDTXMania.renderScale, 0);
        }
    }
    
    private static void tDrawDifficulty(Matrix textTranslation, DTXTexture txLevelNumber, float scale, string str)
    {
        //compensate for actual size of texture
        float multiplier = txLevelNumber.Height / 28.0f;

        bool foundDecimal = false;
        
        Matrix characterTranslation = Matrix.Identity;
        for (int index = 0; index < str.Length; index++)
        {
            char c = str[index];
            if (!stDifficultyNumber.TryGetValue(c, out RectangleF rectangle)) continue;
            
            if (c == '.') foundDecimal = true;

            float characterScale = scale;
            if (!foundDecimal) characterScale *= 1.35f;
            
            if (txLevelNumber != null)
            {
                RectangleF scaledRect = new(
                    rectangle.X * multiplier,
                    rectangle.Y * multiplier,
                    rectangle.Width * multiplier,
                    rectangle.Height * multiplier
                );

                //translate matrix by x/y
                Matrix matrix = textTranslation * characterTranslation;
                
                //calculate if we need to vertically offset upwards to compensate for larger first character
                if (!foundDecimal)
                {
                    float offsetY = (rectangle.Height * characterScale - rectangle.Height * scale);
                    offsetY *= 111.0f / 128.0f; //character is about 111/128 as tall as the texture height
                    matrix *= Matrix.Translation(0, -offsetY * CDTXMania.renderScale, 0);
                }
                txLevelNumber.tDraw2DMatrix(matrix, new Vector2(rectangle.Width, rectangle.Height) * characterScale, scaledRect, Color4.White);
            }

            characterTranslation *= Matrix.Translation(rectangle.Width * CDTXMania.renderScale * characterScale, 0, 0);
        }
    }
	
    private static readonly Dictionary<char, RectangleF> stDifficultyNumber = new()
    {
        { '0', new RectangleF(0 * 20, 0, 20, 28) },
        { '1', new RectangleF(1 * 20, 0, 20, 28) },
        { '2', new RectangleF(2 * 20, 0, 20, 28) },
        { '3', new RectangleF(3 * 20, 0, 20, 28) },
        { '4', new RectangleF(4 * 20, 0, 20, 28) },
        { '5', new RectangleF(5 * 20, 0, 20, 28) },
        { '6', new RectangleF(6 * 20, 0, 20, 28) },
        { '7', new RectangleF(7 * 20, 0, 20, 28) },
        { '8', new RectangleF(8 * 20, 0, 20, 28) },
        { '9', new RectangleF(9 * 20, 0, 20, 28) },
        { '.', new RectangleF(10 * 20, 0, 10, 28) },
        { '-', new RectangleF(11 * 20 - 10, 0, 20, 28) },
        { '?', new RectangleF(12 * 20 - 10, 0, 20, 28) },
        { '%', new RectangleF(13 * 20 - 10, 0, 20, 28) }
    };
}