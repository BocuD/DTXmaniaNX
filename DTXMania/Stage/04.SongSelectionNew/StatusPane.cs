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
    }

    private DTXTexture txLevelNumber;
    private UIImage difficultyFrame;

    private EInstrumentPart instrument;
    
    private float verticalSpacing = 74;
    private Vector3 textOffset = new(125, -45, 0);
    
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
                tDrawDifficulty(textTranslation, txLevelNumber, "-.--");
            }
            else
            {
                //get difficulty number
                double dbLevel = song.charts[c].SongInformation.GetLevel((int)instrument);
                tDrawDifficulty(textTranslation, txLevelNumber, $"{dbLevel:0.00}");
            }

            textTranslation *= Matrix.Translation(0, -verticalSpacing * CDTXMania.renderScale, 0);
        }
    }
    
    private static void tDrawDifficulty(Matrix textTranslation, DTXTexture txLevelNumber, string str)
    {
        Matrix characterTranslation = Matrix.Identity;
        foreach (char c in str)
        {
            if (stDifficultyNumber.TryGetValue(c, out RectangleF rectangle))
            {
                if (txLevelNumber != null)
                {
                    //translate matrix by x/y
                    Matrix matrix = textTranslation * characterTranslation;
                    txLevelNumber.tDraw2DMatrix(matrix, new Vector2(rectangle.Width, rectangle.Height), rectangle);
                }
                
                characterTranslation *= Matrix.Translation(rectangle.Width * CDTXMania.renderScale, 0, 0);
            }
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
        { '?', new RectangleF(12 * 20 - 10, 0, 20, 28) }
    };
}