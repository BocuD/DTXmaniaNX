using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Drawable;

namespace DTXMania;

internal class CActPerfDrumsStatusPanel : CActPerfCommonStatusPanel
{
    public CActPerfDrumsStatusPanel()
    {
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
            txPercent = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_RatePercent_l.png"));
            txSkillMax = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_skill max.png"));
            
            base.OnManagedCreateResources();
        }
    }

    public override void InitUI(UIGroup ui)
    {
        base.InitUI(ui);
        
        var nameplate = ui.AddChild(new UIPlayerNameplate());
        nameplate.position = new Vector3(22, 250, 0);
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

            string str =
                $"{((float)CDTXMania.DTX.LEVEL[i]) / 10.0f + (CDTXMania.DTX.LEVELDEC[i] != 0 ? CDTXMania.DTX.LEVELDEC[i] / 100.0f : 0):0.00}";
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
                str = $"{CDTXMania.DTX.LEVEL[i]:00}";
                bCLASSIC = true;
            }

            Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(CDTXMania.renderScale);

            Matrix4x4 skillPanelMat = Matrix4x4.CreateTranslation(nBodyX[i], nBodyY, 0f);
            txSkillPanel.tDraw2DMatrix(skillPanelMat * scaleMatrix);

            tDrawSmallNumber(80 + nBodyX[i], 72 + nBodyY,
                $"{CDTXMania.stagePerfDrumsScreen.nHitCount_ExclAuto[i].Perfect,4:###0}");
            tDrawSmallNumber(80 + nBodyX[i], 102 + nBodyY,
                $"{CDTXMania.stagePerfDrumsScreen.nHitCount_ExclAuto[i].Great,4:###0}");
            tDrawSmallNumber(80 + nBodyX[i], 132 + nBodyY,
                $"{CDTXMania.stagePerfDrumsScreen.nHitCount_ExclAuto[i].Good,4:###0}");
            tDrawSmallNumber(80 + nBodyX[i], 162 + nBodyY,
                $"{CDTXMania.stagePerfDrumsScreen.nHitCount_ExclAuto[i].Poor,4:###0}");
            tDrawSmallNumber(80 + nBodyX[i], 192 + nBodyY,
                $"{CDTXMania.stagePerfDrumsScreen.nHitCount_ExclAuto[i].Miss,4:###0}");
            tDrawSmallNumber(80 + nBodyX[i], 222 + nBodyY,
                $"{CDTXMania.stagePerfDrumsScreen.actCombo.nCurrentCombo.HighestValue[i],4:###0}");

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
                dbPERFECT率 = Math.Round((100.0 * CDTXMania.stagePerfDrumsScreen.nHitCount_ExclAuto[i].Perfect) /
                                        n現在のノーツ数);
                dbGREAT率 = Math.Round((100.0 * CDTXMania.stagePerfDrumsScreen.nHitCount_ExclAuto[i].Great / n現在のノーツ数));
                dbGOOD率 = Math.Round((100.0 * CDTXMania.stagePerfDrumsScreen.nHitCount_ExclAuto[i].Good / n現在のノーツ数));
                dbPOOR率 = Math.Round((100.0 * CDTXMania.stagePerfDrumsScreen.nHitCount_ExclAuto[i].Poor / n現在のノーツ数));
                dbMISS率 = Math.Round((100.0 * CDTXMania.stagePerfDrumsScreen.nHitCount_ExclAuto[i].Miss / n現在のノーツ数));
                dbMAXCOMBO率 =
                    Math.Round(
                        (100.0 * CDTXMania.stagePerfDrumsScreen.actCombo.nCurrentCombo.HighestValue[i] / n現在のノーツ数));
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

            tDrawSmallNumber(167 + nBodyX[i], 72 + nBodyY, $"{dbPERFECT率,3:##0}%");
            tDrawSmallNumber(167 + nBodyX[i], 102 + nBodyY, $"{dbGREAT率,3:##0}%");
            tDrawSmallNumber(167 + nBodyX[i], 132 + nBodyY, $"{dbGOOD率,3:##0}%");
            tDrawSmallNumber(167 + nBodyX[i], 162 + nBodyY, $"{dbPOOR率,3:##0}%");
            tDrawSmallNumber(167 + nBodyX[i], 192 + nBodyY, $"{dbMISS率,3:##0}%");
            tDrawSmallNumber(167 + nBodyX[i], 222 + nBodyY, $"{dbMAXCOMBO率,3:##0}%");

            //this.tDrawStringLarge(58 + this.n本体X[i], 277 + this.n本体Y, string.Format("{0,6:##0.00}", CDTXMania.stagePerfDrumsScreen.actStatusPanel.db現在の達成率.Drums ) );
            //Conditional checks for MAX
            if (txSkillMax != null && CDTXMania.stagePerfDrumsScreen.actStatusPanel.db現在の達成率.Drums >= 100.0)
            {
                txSkillMax.tDraw2D(127 + nBodyX[i], 277 + nBodyY);
            }
            else
            {
                tDrawLargeNumber(58 + nBodyX[i], 277 + nBodyY,
                    $"{CDTXMania.stagePerfDrumsScreen.actStatusPanel.db現在の達成率.Drums,6:##0.00}");
                if (txPercent != null)
                    txPercent.tDraw2D(217 + nBodyX[i], 287 + nBodyY);
            }

            //Draw Lag Counters if Lag Display is on
            if (CDTXMania.ConfigIni.bShowLagHitCount)
            {
                //Type-A is Early-Blue, Late-Red
                bool bTypeAColor = CDTXMania.ConfigIni.nShowLagTypeColor == 0;

                tDrawLagCounterText(nBodyX[i] + 170, nBodyY + 335,
                    $"{CDTXMania.stagePerfDrumsScreen.nTimingHitCount[i].nEarly,4:###0}", !bTypeAColor);
                tDrawLagCounterText(nBodyX[i] + 245, nBodyY + 335,
                    $"{CDTXMania.stagePerfDrumsScreen.nTimingHitCount[i].nLate,4:###0}", bTypeAColor);
            }

            if (bCLASSIC)
            {
                tDrawLargeNumber(88 + nBodyX[i], 363 + nBodyY,
                    $"{CDTXMania.stagePerfDrumsScreen.actStatusPanel.db現在の達成率.Drums * (CDTXMania.DTX.LEVEL[i] * 0.0033),6:##0.00}");
            }
            else
            {
                tDrawLargeNumber(88 + nBodyX[i], 363 + nBodyY,
                    $"{CScoreIni.tCalculateGameSkillFromPlayingSkill(CDTXMania.DTX.LEVEL[i], CDTXMania.DTX.LEVELDEC[i], CDTXMania.stagePerfDrumsScreen.actStatusPanel.db現在の達成率.Drums),6:##0.00}");
            }

            if (txDifficultyBadge != null)
                txDifficultyBadge.tDraw2D(14 + nBodyX[i], 266 + nBodyY,
                    new RectangleF(rectDiffPanelPoint.X, rectDiffPanelPoint.Y, 60, 60));
            tDisplayLevelNumber((bCLASSIC == true ? 26 : 18) + nBodyX[i], 290 + nBodyY, str);
        }

        return 0;
    }


    // Other

    #region [ private ]
    //-----------------
    
    private STDGBVALUE<int> nBodyX;
    private int nBodyY;

    //New texture % and MAX
    private BaseTexture txPercent;
    private BaseTexture txSkillMax;

    //-----------------
    #endregion
}