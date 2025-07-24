using DTXMania.Core;
using DTXMania.SongDb;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using SharpDX;

namespace DTXMania;

public class StatusPanel : UIGroup
{
    public StatusPanel() : base("StatusPanel")
    {
        drums = AddChild(new UIGroup("Drums"));
        drums.position = new Vector3(320, 333, 0);
        UIImage panelDrums = drums.AddChild(new UIImage(DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_difficulty_panel.png"))));
        panelDrums.name = "PanelDrums";
        
        guitar = AddChild(new UIGroup("Guitar"));
        guitar.position = new Vector3(200, 333, 0);
        UIImage panelGuitar = guitar.AddChild(new UIImage(DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_difficulty_panel.png"))));
        panelGuitar.name = "PanelGuitar";

        bass = AddChild(new UIGroup("Bass"));
        bass.position = new Vector3(430, 333, 0);
        UIImage panelBass = bass.AddChild(new UIImage(DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_difficulty_panel.png"))));
        panelBass.name = "PanelBass";

        instruments = [drums, guitar, bass];
        
        txLevelNumber = DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_level number.png"));
    }

    private SongNode song;
    private CScore chart;
    public void SelectionChanged(SongNode song, CScore chart)
    {
        this.song = song;
        this.chart = chart;
    }

    private UIGroup drums;
    private UIGroup guitar;
    private UIGroup bass;

    private DTXTexture txLevelNumber;

    private UIGroup[] instruments;

    public override void Draw(Matrix parentMatrix)
    {
        drums.isVisible = CDTXMania.ConfigIni.bDrumsEnabled;
        guitar.isVisible = !CDTXMania.ConfigIni.bDrumsEnabled;
        bass.isVisible = !CDTXMania.ConfigIni.bDrumsEnabled;
       
        base.Draw(parentMatrix);

        for (int instrument = 0; instrument < 3; instrument++)
        {
            if (drums.isVisible && instrument != 0) continue;
            if (!drums.isVisible && instrument == 0) continue;
            
            float y = 675;
            float x = instruments[instrument].position.X + 125;
            for (int c = 0; c < 5; c++)
            {
                bool hideDifficulty = song.filteredInstrumentPart != EInstrumentPart.UNKNOWN &&
                                  (int)song.filteredInstrumentPart != instrument;
                if (song.charts[c] == null 
                    || song.nodeType != SongNode.ENodeType.SONG
                    || song.charts[c].SongInformation.chipCountByInstrument[instrument] == 0
                    || hideDifficulty)
                {
                    tDrawDifficulty(x, y, "-.--");
                }
                else
                {
                    //get difficulty number
                    double dbLevel = song.charts[c].SongInformation.Level[instrument];
                    int levelDec = song.charts[c].SongInformation.LevelDec[instrument];
                    if (dbLevel >= 100)
                    {
                        dbLevel /= 100.0;
                    }
                    else if (dbLevel < 100)
                    {
                        dbLevel = dbLevel / 10.0 + levelDec / 100.0;
                    }
                    
                    tDrawDifficulty(x, y, $"{dbLevel:0.00}");
                }

                y -= 74;
            }
        }
    }
    
    private void tDrawDifficulty(float x, float y, string str)
    {
        for (int j = 0; j < str.Length; j++)
        {
            char c = str[j];
            for (int i = 0; i < stDifficultyNumber.Length; i++)
            {
                if (stDifficultyNumber[i].ch == c)
                {
                    RectangleF rectangle = new(stDifficultyNumber[i].rc.X, stDifficultyNumber[i].rc.Y, 20, 28);
                    if (c == '.')
                    {
                        rectangle.Width -= 10;
                    }
                    if (txLevelNumber != null)
                    {
                        //translate matrix by x/y
                        Matrix translationMatrix = Matrix.Translation(x, y, 0);
                        Matrix matrix = localTransformMatrix * translationMatrix;
                        txLevelNumber.tDraw2DMatrix(matrix, new Vector2(rectangle.Width, rectangle.Height), rectangle);
                    }
                    break;
                }
            }
            if (c == '.')
            {
                x += 10;
            }
            else
            {
                x += 20;
            }
        }
    }
    
    private struct STDifficultyDigits
    {
        public char ch;
        public Rectangle rc;
        public STDifficultyDigits(char ch, Rectangle rc)
        {
            this.ch = ch;
            this.rc = rc;
        }
    }
    
    private readonly STDifficultyDigits[] stDifficultyNumber =
    [
        new('0', new Rectangle(0 * 20, 0, 20, 28)),
        new('1', new Rectangle(1 * 20, 0, 20, 28)),
        new('2', new Rectangle(2 * 20, 0, 20, 28)),
        new('3', new Rectangle(3 * 20, 0, 20, 28)),
        new('4', new Rectangle(4 * 20, 0, 20, 28)),
        new('5', new Rectangle(5 * 20, 0, 20, 28)),
        new('6', new Rectangle(6 * 20, 0, 20, 28)),
        new('7', new Rectangle(7 * 20, 0, 20, 28)),
        new('8', new Rectangle(8 * 20, 0, 20, 28)),
        new('9', new Rectangle(9 * 20, 0, 20, 28)),
        new('.', new Rectangle(10 * 20, 0, 10, 28)),
        new('-', new Rectangle(11 * 20 - 10, 0, 20, 28)),
        new('?', new Rectangle(12 * 20 - 10, 0, 20, 28))
    ];
}