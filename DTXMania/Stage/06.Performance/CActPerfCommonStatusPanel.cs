using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using DTXMania.Core;
using DTXMania.UI.Drawable;
using FDK;

namespace DTXMania;

internal class CActPerfCommonStatusPanel : CActivity
{
    // コンストラクタ
    public CActPerfCommonStatusPanel()
    {
        stパネルマップ = null;
        stパネルマップ = new STATUSPANEL[12];		// yyagi: 以下、手抜きの初期化でスマン
        // { "DTXMANIA", 0 }, { "EXTREME", 1 }, ... みたいに書きたいが___

        //2013.09.07.kairera0467 画像の順番もこの並びになるので、難易度ラベルを追加する時は12以降に追加した方が画像編集でも助かります。
        string[] labels = new string[12] {
            "DTXMANIA",     //0
            "DEBUT",        //1
            "NOVICE",       //2
            "REGULAR",      //3
            "EXPERT",       //4
            "MASTER",       //5
            "BASIC",        //6
            "ADVANCED",     //7
            "EXTREME",      //8
            "RAW",          //9
            "RWS",          //10
            "REAL"          //11
        };
        int[] status = new int[12] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };

        for (int i = 0; i < 12; i++)
        {
            stパネルマップ[i] = default(STATUSPANEL);
            //this.stPanelMap[i] = new STATUSPANEL();
            stパネルマップ[i].status = status[i];
            stパネルマップ[i].label = labels[i];
        }

        //Initialize positions of character in lag text sprite
        int nWidth = 15;
        int nHeight = 19;
        Point ptRedTextOffset = new Point(64, 64);
        List<ST文字位置Ex> LagCountBlueTextList = new List<ST文字位置Ex>();
        List<ST文字位置Ex> LagCountRedTextList = new List<ST文字位置Ex>();
        int[] nPosXArray = { 0, 15, 30, 45, 0, 15, 30, 45, 0, 15 };
        int[] nPosYArray = { 0, 0, 0, 0, 19, 19, 19, 19, 38, 38 };
        for (int i = 0; i < nPosXArray.Length; i++)
        {
            ST文字位置Ex stCurrText = new ST文字位置Ex();
            stCurrText.ch = (char)('0' + i);
            stCurrText.rect = new Rectangle(nPosXArray[i], nPosYArray[i], nWidth, nHeight);
            LagCountBlueTextList.Add(stCurrText);

            ST文字位置Ex stNextCurrText = new ST文字位置Ex();
            stNextCurrText.ch = (char)('0' + i);
            stNextCurrText.rect = new Rectangle(nPosXArray[i] + ptRedTextOffset.X,
                nPosYArray[i] + ptRedTextOffset.Y, nWidth, nHeight);
            LagCountRedTextList.Add(stNextCurrText);
        }

        stLagCountBlueText = LagCountBlueTextList.ToArray();
        stLagCountRedText = LagCountRedTextList.ToArray();
        
        txNumberFontSheet = new BaseTexture[2];

        ST文字位置[] st文字位置Array = new ST文字位置[11];
        ST文字位置 st文字位置 = new()
        {
            ch = '0',
            pt = new Point(0, 0)
        };
        st文字位置Array[0] = st文字位置;
        ST文字位置 st文字位置2 = new()
        {
            ch = '1',
            pt = new Point(28, 0)
        };
        st文字位置Array[1] = st文字位置2;
        ST文字位置 st文字位置3 = new()
        {
            ch = '2',
            pt = new Point(56, 0)
        };
        st文字位置Array[2] = st文字位置3;
        ST文字位置 st文字位置4 = new()
        {
            ch = '3',
            pt = new Point(84, 0)
        };
        st文字位置Array[3] = st文字位置4;
        ST文字位置 st文字位置5 = new()
        {
            ch = '4',
            pt = new Point(112, 0)
        };
        st文字位置Array[4] = st文字位置5;
        ST文字位置 st文字位置6 = new()
        {
            ch = '5',
            pt = new Point(140, 0)
        };
        st文字位置Array[5] = st文字位置6;
        ST文字位置 st文字位置7 = new()
        {
            ch = '6',
            pt = new Point(168, 0)
        };
        st文字位置Array[6] = st文字位置7;
        ST文字位置 st文字位置8 = new()
        {
            ch = '7',
            pt = new Point(196, 0)
        };
        st文字位置Array[7] = st文字位置8;
        ST文字位置 st文字位置9 = new()
        {
            ch = '8',
            pt = new Point(224, 0)
        };
        st文字位置Array[8] = st文字位置9;
        ST文字位置 st文字位置10 = new()
        {
            ch = '9',
            pt = new Point(252, 0)
        };
        st文字位置Array[9] = st文字位置10;
        ST文字位置 st文字位置11 = new()
        {
            ch = '.',
            pt = new Point(280, 0)
        };
        st文字位置Array[10] = st文字位置11;
        stLargeTextRects = st文字位置Array;

        ST文字位置[] st文字位置Array2 = new ST文字位置[12];
        ST文字位置 st文字位置13 = new()
        {
            ch = '0',
            pt = new Point(0, 0)
        };
        st文字位置Array2[0] = st文字位置13;
        ST文字位置 st文字位置14 = new()
        {
            ch = '1',
            pt = new Point(20, 0)
        };
        st文字位置Array2[1] = st文字位置14;
        ST文字位置 st文字位置15 = new()
        {
            ch = '2',
            pt = new Point(40, 0)
        };
        st文字位置Array2[2] = st文字位置15;
        ST文字位置 st文字位置16 = new()
        {
            ch = '3',
            pt = new Point(60, 0)
        };
        st文字位置Array2[3] = st文字位置16;
        ST文字位置 st文字位置17 = new()
        {
            ch = '4',
            pt = new Point(80, 0)
        };
        st文字位置Array2[4] = st文字位置17;
        ST文字位置 st文字位置18 = new()
        {
            ch = '5',
            pt = new Point(100, 0)
        };
        st文字位置Array2[5] = st文字位置18;
        ST文字位置 st文字位置19 = new()
        {
            ch = '6',
            pt = new Point(120, 0)
        };
        st文字位置Array2[6] = st文字位置19;
        ST文字位置 st文字位置20 = new()
        {
            ch = '7',
            pt = new Point(140, 0)
        };
        st文字位置Array2[7] = st文字位置20;
        ST文字位置 st文字位置21 = new()
        {
            ch = '8',
            pt = new Point(160, 0)
        };
        st文字位置Array2[8] = st文字位置21;
        ST文字位置 st文字位置22 = new()
        {
            ch = '9',
            pt = new Point(180, 0)
        };
        st文字位置Array2[9] = st文字位置22;
        ST文字位置 st文字位置23 = new()
        {
            ch = '%',
            pt = new Point(200, 0)
        };
        st文字位置Array2[10] = st文字位置23;
        ST文字位置 st文字位置24 = new()
        {
            ch = '.',
            pt = new Point(210, 0)
        };
        st文字位置Array2[11] = st文字位置24;
        stSmallTextRects = st文字位置Array2;

        ST文字位置[] st難易度文字位置Ar = new ST文字位置[11];
        ST文字位置 st難易度文字位置 = new()
        {
            ch = '0',
            pt = new Point(0, 0)
        };
        st難易度文字位置Ar[0] = st難易度文字位置;
        ST文字位置 st難易度文字位置2 = new()
        {
            ch = '1',
            pt = new Point(16, 0)
        };
        st難易度文字位置Ar[1] = st難易度文字位置2;
        ST文字位置 st難易度文字位置3 = new()
        {
            ch = '2',
            pt = new Point(32, 0)
        };
        st難易度文字位置Ar[2] = st難易度文字位置3;
        ST文字位置 st難易度文字位置4 = new()
        {
            ch = '3',
            pt = new Point(48, 0)
        };
        st難易度文字位置Ar[3] = st難易度文字位置4;
        ST文字位置 st難易度文字位置5 = new()
        {
            ch = '4',
            pt = new Point(64, 0)
        };
        st難易度文字位置Ar[4] = st難易度文字位置5;
        ST文字位置 st難易度文字位置6 = new()
        {
            ch = '5',
            pt = new Point(80, 0)
        };
        st難易度文字位置Ar[5] = st難易度文字位置6;
        ST文字位置 st難易度文字位置7 = new()
        {
            ch = '6',
            pt = new Point(96, 0)
        };
        st難易度文字位置Ar[6] = st難易度文字位置7;
        ST文字位置 st難易度文字位置8 = new()
        {
            ch = '7',
            pt = new Point(112, 0)
        };
        st難易度文字位置Ar[7] = st難易度文字位置8;
        ST文字位置 st難易度文字位置9 = new()
        {
            ch = '8',
            pt = new Point(128, 0)
        };
        st難易度文字位置Ar[8] = st難易度文字位置9;
        ST文字位置 st難易度文字位置10 = new()
        {
            ch = '9',
            pt = new Point(144, 0)
        };
        st難易度文字位置Ar[9] = st難易度文字位置10;
        ST文字位置 st難易度文字位置11 = new()
        {
            ch = '.',
            pt = new Point(160, 0)
        };
        st難易度文字位置Ar[10] = st難易度文字位置11;
        stDifficultyNumberRects = st難易度文字位置Ar;
        
        bActivated = false;
    }


    // CActivity 実装

    public override void OnActivate()
    {
        nCurrentScore = 0L;
        n現在のスコアGuitar = 0L;
        n現在のスコアBass = 0L;
        nStatus = 0;
        nIndex = 0;

        for( int i = 0; i < 3; i++ )
        {
            db現在の達成率[ i ] = 0.0;
        }

        base.OnActivate();
    }
    
    public virtual void InitUI(UIGroup ui)
    {
        
    }

    public override void OnManagedCreateResources()
    {
        if (bActivated)
        {
            txNumberFontSheet[0] = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_Ratenumber_s.png"));
            txNumberFontSheet[1] = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_Ratenumber_l.png"));
            
            txSkillPanel = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_SkillPanel.png"));

            txDifficultyBadge = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_Difficulty.png"));
            txLevelNumber = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_LevelNumber.png"));
            
            txLagHitCount = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_lag numbers.png"));

            base.OnManagedCreateResources();
        }
    }

    public void tSetDifficultyLabelFromScript( string strラベル名)  // tスクリプトから難易度ラベルを取得する
    {
        string strRawScriptFile;

        //ファイルの存在チェック
        if( File.Exists( CSkin.Path( @"Script\difficult.dtxs" ) ) )
        {
            //スクリプトを開く
            StreamReader reader = new StreamReader( CSkin.Path( @"Script\difficult.dtxs" ), Encoding.GetEncoding( "Shift_JIS" ) );
            strRawScriptFile = reader.ReadToEnd();

            strRawScriptFile = strRawScriptFile.Replace( Environment.NewLine, "\n" );
            string[] delimiter = { "\n" };
            string[] strSingleLine = strRawScriptFile.Split( delimiter, StringSplitOptions.RemoveEmptyEntries );

            for( int i = 0; i < strSingleLine.Length; i++ )
            {
                if( strSingleLine[ i ].StartsWith( "//" ) )
                    continue; //コメント行の場合は無視

                //まずSplit
                string[] arScriptLine = strSingleLine[ i ].Split( ',' );

                if( ( arScriptLine.Length >= 4 && arScriptLine.Length <= 5 ) == false )
                    continue; //引数が4つか5つじゃなければ無視。

                if( arScriptLine[ 0 ] != "7" )
                    continue; //使用するシーンが違うなら無視。

                if( arScriptLine.Length == 4 )
                {
                    if( String.Compare( arScriptLine[ 1 ], strラベル名, true ) != 0 )
                        continue; //ラベル名が違うなら無視。大文字小文字区別しない
                }
                else if( arScriptLine.Length == 5 )
                {
                    if( arScriptLine[ 4 ] == "1" )
                    {
                        if( arScriptLine[ 1 ] != strラベル名 )
                            continue; //ラベル名が違うなら無視。
                    }
                    else
                    {
                        if( String.Compare( arScriptLine[ 1 ], strラベル名, true ) != 0 )
                            continue; //ラベル名が違うなら無視。大文字小文字区別しない
                    }
                }
                rectDiffPanelPoint.X = Convert.ToInt32( arScriptLine[ 2 ] );
                rectDiffPanelPoint.Y = Convert.ToInt32( arScriptLine[ 3 ] );

                reader.Close();
                break;
            }
        }
    }

    #region [ protected ]
    //-----------------
    [StructLayout(LayoutKind.Sequential)]
    public struct STATUSPANEL
    {
        public string label;
        public int status;
    }
    //-----------------
    [StructLayout(LayoutKind.Sequential)]
    public struct ST文字位置Ex
    {
        public char ch;
        public Rectangle rect;
    }
    public long nCurrentScore;
    public long n現在のスコアGuitar;
    public long n現在のスコアBass;
    public STDGBVALUE<double> db現在の達成率;
    public int nIndex;
    public int nStatus;
    protected Rectangle rectDiffPanelPoint;
    public STATUSPANEL[] stパネルマップ;
    protected readonly ST文字位置Ex[] stLagCountBlueText;//15x19 start at 0,0
    protected readonly ST文字位置Ex[] stLagCountRedText;//15x19 start at 64,64

    
    protected BaseTexture txSkillPanel;
    
    protected BaseTexture txDifficultyBadge;
    protected BaseTexture txLevelNumber;

    [StructLayout(LayoutKind.Sequential)]
    protected struct ST文字位置
    {
        public char ch;
        public Point pt;
    }
    
    protected readonly ST文字位置[] stSmallTextRects;
    protected readonly ST文字位置[] stLargeTextRects;
    protected readonly ST文字位置[] stDifficultyNumberRects;
    
    protected void tDisplayLevelNumber(int x, int y, string str)
    {
        foreach (char ch in str)
        {
            for (int i = 0; i < stDifficultyNumberRects.Length; i++)
            {
                if (stDifficultyNumberRects[i].ch == ch)
                {
                    RectangleF rectangle = new(stDifficultyNumberRects[i].pt.X, stDifficultyNumberRects[i].pt.Y, 16, 32);
                    if (ch == '.')
                    {
                        rectangle.Width -= 11;
                    }
                    if (txLevelNumber != null)
                    {
                        txLevelNumber.tDraw2D(x, y, rectangle);
                    }
                    break;
                }
            }
            x += (ch == '.' ? 5 : 16);
        }
    }
    
    protected BaseTexture[] txNumberFontSheet;
    
    protected void tDrawSmallNumber(int x, int y, string str)
    {
        foreach (char ch in str)
        {
            for (int i = 0; i < stSmallTextRects.Length; i++)
            {
                if (stSmallTextRects[i].ch == ch)
                {
                    RectangleF rectangle = new(stSmallTextRects[i].pt.X, stSmallTextRects[i].pt.Y, 20, 26);
                    if (txNumberFontSheet[0] != null)
                    {
                        txNumberFontSheet[0].tDraw2D(x, y, rectangle);
                    }
                    break;
                }
            }
            x += 20;
        }
    }
    
    protected void tDrawLargeNumber(int x, int y, string str)
    {
        foreach (char ch in str)
        {
            for (int i = 0; i < stLargeTextRects.Length; i++)
            {
                if (stLargeTextRects[i].ch == ch)
                {
                    RectangleF rectangle = new(stLargeTextRects[i].pt.X, stLargeTextRects[i].pt.Y, 28, 42);
                    if (ch == '.')
                    {
                        rectangle.Width -= 18;
                    }
                    if (txNumberFontSheet[1] != null)
                    {
                        txNumberFontSheet[1].tDraw2D(x, y, rectangle);
                    }
                    break;
                }
            }
            x += (ch == '.' ? 12 : 29);
        }
    }

    private BaseTexture txLagHitCount;

    //Note: Lag Text is draw right-justified
    //i.e. x,y is the top right corner of rect
    protected void tDrawLagCounterText(int x, int y, string str, bool isRed) 
    {
        ST文字位置Ex[] currTextPosStructArray = isRed ? stLagCountRedText : stLagCountBlueText;
            
        for (int j = str.Length - 1; j >= 0; j--)
        {
            for (int i = 0; i < currTextPosStructArray.Length; i++)
            {
                if (currTextPosStructArray[i].ch == str[j])
                {                        
                    RectangleF rectangle = new(
                        currTextPosStructArray[i].rect.X,
                        currTextPosStructArray[i].rect.Y,
                        currTextPosStructArray[i].rect.Width,
                        currTextPosStructArray[i].rect.Height);
                        
                    if (txLagHitCount != null)
                    {
                        txLagHitCount.tDraw2D(x - currTextPosStructArray[i].rect.Width, y, rectangle);
                    }
                    break;
                }
            }
            //15 is width of char in txLag
            x -= 15;
        }
    }
    
    //-----------------
    #endregion
}