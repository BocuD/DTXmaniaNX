using System.Runtime.InteropServices;
using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using DTXMania.UI.Text;

using Point = System.Drawing.Point;
using Color = System.Drawing.Color;

namespace DTXMania;

internal class CActPerfDrumsStatusPanel : CActPerfCommonStatusPanel
{

    public CActPerfDrumsStatusPanel()
    {
        txパネル文字 = new BaseTexture[2];
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
        st大文字位置 = st文字位置Array;

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
        st小文字位置 = st文字位置Array2;

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
        st難易度数字位置 = st難易度文字位置Ar;

        bActivated = false;
    }

    public override void OnActivate()
    {
        #region [ 本体位置 ]
        for (int i = 0; i < 3; i++)
        {
            nBodyX[i] = 0;
        }

        nBodyX[0] = 22;
        nBodyY = 250;
            
        #endregion
                        
        base.OnActivate();
    }

    public override void OnManagedCreateResources()
    {
        if (bActivated)
        {
            // nameFont = new CPrivateFastFont(new FontFamily(CDTXMania.ConfigIni.str曲名表示フォント), 20, FontStyle.Regular);
            // titleFont = new CPrivateFastFont(new FontFamily(CDTXMania.ConfigIni.str曲名表示フォント), 12, FontStyle.Regular);
            txSkillPanel = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_SkillPanel.png"));
            txパネル文字[0] = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_Ratenumber_s.png"));
            txパネル文字[1] = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_Ratenumber_l.png"));
            tx難易度パネル = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_Difficulty.png"));
            tx難易度用数字 = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_LevelNumber.png"));
            //Load new textures
            txPercent = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_RatePercent_l.png"));
            txSkillMax = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_skill max.png"));
            txLagHitCount = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_lag numbers.png"));

            strPlayerName = string.IsNullOrEmpty(CDTXMania.ConfigIni.strCardName[0])
                ? "GUEST"
                : CDTXMania.ConfigIni.strCardName[0];
            strTitleName = string.IsNullOrEmpty(CDTXMania.ConfigIni.strGroupName[0])
                ? ""
                : CDTXMania.ConfigIni.strGroupName[0];

            #region[ ネームカラー ]

            //--------------------
            Color clNameColor = Color.White;
            Color clNameColorLower = Color.White;
            switch (CDTXMania.ConfigIni.nNameColor[0])
            {
                case 0:
                    clNameColor = Color.White;
                    break;
                case 1:
                    clNameColor = Color.LightYellow;
                    break;
                case 2:
                    clNameColor = Color.Yellow;
                    break;
                case 3:
                    clNameColor = Color.Green;
                    break;
                case 4:
                    clNameColor = Color.Blue;
                    break;
                case 5:
                    clNameColor = Color.Purple;
                    break;
                case 6:
                    clNameColor = Color.Red;
                    break;
                case 7:
                    clNameColor = Color.Brown;
                    break;
                case 8:
                    clNameColor = Color.Silver;
                    break;
                case 9:
                    clNameColor = Color.Gold;
                    break;

                case 10:
                    clNameColor = Color.White;
                    break;
                case 11:
                    clNameColor = Color.LightYellow;
                    clNameColorLower = Color.White;
                    break;
                case 12:
                    clNameColor = Color.Yellow;
                    clNameColorLower = Color.White;
                    break;
                case 13:
                    clNameColor = Color.FromArgb(0, 255, 33);
                    clNameColorLower = Color.White;
                    break;
                case 14:
                    clNameColor = Color.FromArgb(0, 38, 255);
                    clNameColorLower = Color.White;
                    break;
                case 15:
                    clNameColor = Color.FromArgb(72, 0, 255);
                    clNameColorLower = Color.White;
                    break;
                case 16:
                    clNameColor = Color.FromArgb(255, 255, 0, 0);
                    clNameColorLower = Color.White;
                    break;
                case 17:
                    clNameColor = Color.FromArgb(255, 232, 182, 149);
                    clNameColorLower = Color.FromArgb(255, 122, 69, 26);
                    break;
                case 18:
                    clNameColor = Color.FromArgb(246, 245, 255);
                    clNameColorLower = Color.FromArgb(125, 128, 137);
                    break;
                case 19:
                    clNameColor = Color.FromArgb(255, 238, 196, 85);
                    clNameColorLower = Color.FromArgb(255, 255, 241, 200);
                    break;
            }

            var nameRequest = new UiTextRenderRequest
            {
                Name = strPlayerName,
                Text = strPlayerName,
                FontPath = UiFontDefaults.TryGetDefaultUiFontPath() ?? "",
                FontSize = 20,
                OutlineGradientMode = CDTXMania.ConfigIni.nNameColor[0] > 11
                    ? UiTextGradientMode.Vertical
                    : UiTextGradientMode.None,
                OutlineColor = clNameColor,
                OutlineGradientTopColor = clNameColor,
                OutlineGradientBottomColor = clNameColorLower,
                FillColor = Color.White,
                Backend = UiTextRenderBackend.Skia
            };

            var titleRequest = new UiTextRenderRequest
            {
                Name = strTitleName,
                Text = strTitleName,
                FontPath = UiFontDefaults.TryGetDefaultUiFontPath() ?? "",
                FontSize = 12,
                FillColor = Color.White,
                Backend = UiTextRenderBackend.Skia
            };

            //--------------------

            #endregion

            txNameplateText = BaseTexture.SkiaTextRenderer.Render(nameRequest);
            txTitleText = BaseTexture.SkiaTextRenderer.Render(titleRequest);

            base.OnManagedCreateResources();
        }
    }

    public override int OnUpdateAndDraw()
    {
        if (bActivated)
        {
            double dbPERFECT率 = 0;
            double dbGREAT率 = 0;
            double dbGOOD率 = 0;
            double dbPOOR率 = 0;
            double dbMISS率 = 0;
            double dbMAXCOMBO率 = 0;

            int i = 0;

            string str = string.Format( "{0:0.00}", ( (float)CDTXMania.DTX.LEVEL[ i ]) / 10.0f + ( CDTXMania.DTX.LEVELDEC[ i ] != 0 ? CDTXMania.DTX.LEVELDEC[ i ] / 100.0f : 0 ) );
            bool bCLASSIC = false;
            //If Skill Mode is CLASSIC, always display lvl as Classic Style
            if (CDTXMania.ConfigIni.nSkillMode == 0 || (CDTXMania.ConfigIni.bClassicScoreDisplay &&
                                                        (CDTXMania.DTX.bHasChips.LeftCymbal == false) &&
                                                        (CDTXMania.DTX.bHasChips.LP == false) &&
                                                        (CDTXMania.DTX.bHasChips.LBD == false) &&
                                                        (CDTXMania.DTX.bHasChips.FT == false) &&
                                                        (CDTXMania.DTX.bHasChips.Ride == false) &&
                                                        (CDTXMania.DTX.bForceXGChart == false)))
            {
                str = string.Format("{0:00}", CDTXMania.DTX.LEVEL[i]);
                bCLASSIC = true;
            }
            
            Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(CDTXMania.renderScale);
            
            Matrix4x4 skillPanelMat = Matrix4x4.CreateTranslation(nBodyX[i], nBodyY, 0f);
            txSkillPanel.tDraw2DMatrix(skillPanelMat * scaleMatrix);
            
            Matrix4x4 matBase = Matrix4x4.CreateTranslation(nBodyX[i], nBodyY, 0f);
            Matrix4x4 matName = matBase * Matrix4x4.CreateTranslation(-2f, 26f, 0f);
            Matrix4x4 matPlate  = matBase * Matrix4x4.CreateTranslation(6f, 8f, 0f);
            txNameplateText.tDraw2DMatrix(matName * scaleMatrix);
            txTitleText.tDraw2DMatrix(matPlate * scaleMatrix);

            t小文字表示(80 + nBodyX[i], 72 + nBodyY, $"{CDTXMania.stagePerfDrumsScreen.nHitCount_ExclAuto[i].Perfect,4:###0}");
            t小文字表示(80 + nBodyX[i], 102 + nBodyY, $"{CDTXMania.stagePerfDrumsScreen.nHitCount_ExclAuto[i].Great,4:###0}");
            t小文字表示(80 + nBodyX[i], 132 + nBodyY, $"{CDTXMania.stagePerfDrumsScreen.nHitCount_ExclAuto[i].Good,4:###0}");
            t小文字表示(80 + nBodyX[i], 162 + nBodyY, $"{CDTXMania.stagePerfDrumsScreen.nHitCount_ExclAuto[i].Poor,4:###0}");
            t小文字表示(80 + nBodyX[i], 192 + nBodyY, $"{CDTXMania.stagePerfDrumsScreen.nHitCount_ExclAuto[i].Miss,4:###0}");
            t小文字表示(80 + nBodyX[i], 222 + nBodyY, $"{CDTXMania.stagePerfDrumsScreen.actCombo.nCurrentCombo.HighestValue[i],4:###0}");

            int n現在のノーツ数 =
                CDTXMania.stagePerfDrumsScreen.nHitCount_IncAuto[i].Perfect +
                CDTXMania.stagePerfDrumsScreen.nHitCount_IncAuto[i].Great +
                CDTXMania.stagePerfDrumsScreen.nHitCount_IncAuto[i].Good +
                CDTXMania.stagePerfDrumsScreen.nHitCount_IncAuto[i].Poor +
                CDTXMania.stagePerfDrumsScreen.nHitCount_IncAuto[i].Miss;

            if (CDTXMania.stagePerfDrumsScreen.bIsTrainingMode)
            {
                CDTXMania.stagePerfDrumsScreen.actStatusPanel.db現在の達成率.Drums = 0;
            }
            else
            {
                dbPERFECT率 = Math.Round((100.0 * CDTXMania.stagePerfDrumsScreen.nHitCount_ExclAuto[i].Perfect) / n現在のノーツ数);
                dbGREAT率 = Math.Round((100.0 * CDTXMania.stagePerfDrumsScreen.nHitCount_ExclAuto[i].Great / n現在のノーツ数));
                dbGOOD率 = Math.Round((100.0 * CDTXMania.stagePerfDrumsScreen.nHitCount_ExclAuto[i].Good / n現在のノーツ数));
                dbPOOR率 = Math.Round((100.0 * CDTXMania.stagePerfDrumsScreen.nHitCount_ExclAuto[i].Poor / n現在のノーツ数));
                dbMISS率 = Math.Round((100.0 * CDTXMania.stagePerfDrumsScreen.nHitCount_ExclAuto[i].Miss / n現在のノーツ数));
                dbMAXCOMBO率 = Math.Round((100.0 * CDTXMania.stagePerfDrumsScreen.actCombo.nCurrentCombo.HighestValue[i] / n現在のノーツ数));
            }

            if (double.IsNaN(dbPERFECT率))
                dbPERFECT率 = 0;
            if (double.IsNaN(dbGREAT率))
                dbGREAT率 = 0;
            if (double.IsNaN(dbGOOD率))
                dbGOOD率 = 0;
            if (double.IsNaN(dbPOOR率))
                dbPOOR率 = 0;
            if (double.IsNaN(dbMISS率))
                dbMISS率 = 0;
            if (double.IsNaN(dbMAXCOMBO率))
                dbMAXCOMBO率 = 0;

            t小文字表示(167 + nBodyX[i], 72 + nBodyY, string.Format("{0,3:##0}%", dbPERFECT率));
            t小文字表示(167 + nBodyX[i], 102 + nBodyY, string.Format("{0,3:##0}%", dbGREAT率));
            t小文字表示(167 + nBodyX[i], 132 + nBodyY, string.Format("{0,3:##0}%", dbGOOD率));
            t小文字表示(167 + nBodyX[i], 162 + nBodyY, string.Format("{0,3:##0}%", dbPOOR率));
            t小文字表示(167 + nBodyX[i], 192 + nBodyY, string.Format("{0,3:##0}%", dbMISS率));
            t小文字表示(167 + nBodyX[i], 222 + nBodyY, string.Format("{0,3:##0}%", dbMAXCOMBO率));

            //this.tDrawStringLarge(58 + this.n本体X[i], 277 + this.n本体Y, string.Format("{0,6:##0.00}", CDTXMania.stagePerfDrumsScreen.actStatusPanel.db現在の達成率.Drums ) );
            //Conditional checks for MAX
            if (txSkillMax != null && CDTXMania.stagePerfDrumsScreen.actStatusPanel.db現在の達成率.Drums >= 100.0)
            {
                txSkillMax.tDraw2D(CDTXMania.app.Device, 127 + nBodyX[i], 277 + nBodyY);
            }
            else
            {
                t大文字表示(58 + nBodyX[i], 277 + nBodyY, string.Format("{0,6:##0.00}", CDTXMania.stagePerfDrumsScreen.actStatusPanel.db現在の達成率.Drums));
                if (txPercent != null)
                    txPercent.tDraw2D(CDTXMania.app.Device, 217 + nBodyX[i], 287 + nBodyY);
            }

            //Draw Lag Counters if Lag Display is on
            if (CDTXMania.ConfigIni.bShowLagHitCount)
            {
                //Type-A is Early-Blue, Late-Red
                bool bTypeAColor = CDTXMania.ConfigIni.nShowLagTypeColor == 0;

                tDrawLagCounterText(nBodyX[i] + 170, nBodyY + 335,
                    string.Format("{0,4:###0}", CDTXMania.stagePerfDrumsScreen.nTimingHitCount[i].nEarly), !bTypeAColor);
                tDrawLagCounterText(nBodyX[i] + 245, nBodyY + 335,
                    string.Format("{0,4:###0}", CDTXMania.stagePerfDrumsScreen.nTimingHitCount[i].nLate), bTypeAColor);
            }

            if (bCLASSIC)
            {
                t大文字表示(88 + nBodyX[i], 363 + nBodyY, string.Format("{0,6:##0.00}", CDTXMania.stagePerfDrumsScreen.actStatusPanel.db現在の達成率.Drums * (CDTXMania.DTX.LEVEL[i] * 0.0033) ));
            }
            else
            {
                t大文字表示(88 + nBodyX[i], 363 + nBodyY, string.Format("{0,6:##0.00}", CScoreIni.tCalculateGameSkillFromPlayingSkill(CDTXMania.DTX.LEVEL[i], CDTXMania.DTX.LEVELDEC[i], CDTXMania.stagePerfDrumsScreen.actStatusPanel.db現在の達成率.Drums)));
            }

            if ( tx難易度パネル != null )
                tx難易度パネル.tDraw2D( CDTXMania.app.Device, 14 + nBodyX[ i ], 266 + nBodyY, new RectangleF( rectDiffPanelPoint.X, rectDiffPanelPoint.Y, 60, 60 ) );
            tレベル数字描画((bCLASSIC == true ? 26 : 18) + nBodyX[i], 290 + nBodyY, str);
        }
        return 0;

    }


    // Other

    #region [ private ]
    //-----------------
    [StructLayout(LayoutKind.Sequential)]
    private struct ST文字位置
    {
        public char ch;
        public Point pt;
    }
    private STDGBVALUE<int> nBodyX;
    private int nBodyY;
    private readonly ST文字位置[] st小文字位置;
    private readonly ST文字位置[] st大文字位置;
    private readonly ST文字位置[] st難易度数字位置;
    private BaseTexture txSkillPanel;
    private BaseTexture[] txパネル文字;
    
    private string strPlayerName;
    private string strTitleName;
    private BaseTexture txNameplateText;
    private BaseTexture txTitleText;
    private BaseTexture tx難易度パネル;
    private BaseTexture tx難易度用数字;
    //New texture % and MAX
    private BaseTexture txPercent;
    private BaseTexture txSkillMax;
    private BaseTexture txLagHitCount;

    private void t小文字表示(int x, int y, string str)
    {
        foreach (char ch in str)
        {
            for (int i = 0; i < st小文字位置.Length; i++)
            {
                if (st小文字位置[i].ch == ch)
                {
                    RectangleF rectangle = new(st小文字位置[i].pt.X, st小文字位置[i].pt.Y, 20, 26);
                    if (txパネル文字[0] != null)
                    {
                        txパネル文字[0].tDraw2D(CDTXMania.app.Device, x, y, rectangle);
                    }
                    break;
                }
            }
            x += 20;
        }
    }

    //Note: Lag Text is draw right-justified
    //i.e. x,y is the top right corner of rect
    private void tDrawLagCounterText(int x, int y, string str, bool isRed)
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
                        txLagHitCount.tDraw2D(CDTXMania.app.Device, x - currTextPosStructArray[i].rect.Width, y, rectangle);
                    }
                    break;
                }
            }
            //15 is width of char in txLag
            x -= 15;
        }
    }
    private void t大文字表示(int x, int y, string str)
    {
        foreach (char ch in str)
        {
            for (int i = 0; i < st大文字位置.Length; i++)
            {
                if (st大文字位置[i].ch == ch)
                {
                    RectangleF rectangle = new(st大文字位置[i].pt.X, st大文字位置[i].pt.Y, 28, 42);
                    if (ch == '.')
                    {
                        rectangle.Width -= 18;
                    }
                    if (txパネル文字[1] != null)
                    {
                        txパネル文字[1].tDraw2D(CDTXMania.app.Device, x, y, rectangle);
                    }
                    break;
                }
            }
            x += (ch == '.' ? 12 : 29);
        }
    }
    private void tレベル数字描画(int x, int y, string str)
    {
        foreach (char ch in str)
        {
            for (int i = 0; i < st難易度数字位置.Length; i++)
            {
                if (st難易度数字位置[i].ch == ch)
                {
                    RectangleF rectangle = new(st難易度数字位置[i].pt.X, st難易度数字位置[i].pt.Y, 16, 32);
                    if (ch == '.')
                    {
                        rectangle.Width -= 11;
                    }
                    if (tx難易度用数字 != null)
                    {
                        tx難易度用数字.tDraw2D(CDTXMania.app.Device, x, y, rectangle);
                    }
                    break;
                }
            }
            x += (ch == '.' ? 5 : 16);
        }
    }
    //-----------------
    #endregion
}