using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Drawable;

namespace DTXMania;

internal class CActPerfGuitarStatusPanel : CActPerfCommonStatusPanel
{
    public CActPerfGuitarStatusPanel()
    {
        bActivated = false;
    }

    public override void OnActivate()
    {
        #region [ 本体位置 ]
        nBodyX[0] = 0;
        nBodyX[1] = 373;
        nBodyX[2] = 665;
        nBodyY = 254;

        if (!CDTXMania.DTX.bHasChips.Bass)
        {
            //fisyher: No need to check bIsSwappedGuitarBass because guitar-bass info are already swapped at this point
            nBodyX[2] = 0;
                
        }
        else if (!CDTXMania.DTX.bHasChips.Guitar)
        {
            //fisyher: No need to check bIsSwappedGuitarBass because guitar-bass info are already swapped at this point
            nBodyX[1] = 0;                
        }
        else if (CDTXMania.ConfigIni.bGraph有効.Guitar || CDTXMania.ConfigIni.bGraph有効.Bass )
        {
            if (!CDTXMania.ConfigIni.bAllGuitarsAreAutoPlay && CDTXMania.ConfigIni.bAllBassAreAutoPlay)
            {
                nBodyX[2] = 0;
            }
            else if (CDTXMania.ConfigIni.bAllGuitarsAreAutoPlay && !CDTXMania.ConfigIni.bAllBassAreAutoPlay)
            {
                nBodyX[1] = 0;
            }
        }
        #endregion
            
        base.OnActivate();
    }

    public override void OnManagedCreateResources()
    {
        if (bActivated)
        {
            //Load new textures
            txPercent = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_RatePercent_l.png"));
            txSkillMax = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_skill max.png"));

            base.OnManagedCreateResources();
        }
    }

    public override void InitUI(UIGroup ui)
    {
        base.InitUI(ui);
        
        playerNameplates[0] = ui.AddChild(new UIPlayerNameplate(1));
        playerNameplates[0].position = new Vector3(373, 254, 0);
        playerNameplates[0].renderOrder = 2;
        playerNameplates[1] = ui.AddChild(new UIPlayerNameplate(2));
        playerNameplates[1].position = new Vector3(665, 254, 0);
        playerNameplates[1].renderOrder = 2;
    }

    public override int OnUpdateAndDraw()
    {
        if (bActivated)
        {
            bool draw = CDTXMania.ConfigIni.nInfoType == 1;

            double dbPERFECT率 = 0;
            double dbGREAT率 = 0;
            double dbGOOD率 = 0;
            double dbPOOR率 = 0;
            double dbMISS率 = 0;
            double dbMAXCOMBO率 = 0;

            //outside the loop: stackalloc inside one grows the frame on every iteration
            Span<char> levelText = stackalloc char[16];
            Span<char> text = stackalloc char[16];

            for (int i = 1; i < 3; i++)
            {
                playerNameplates[i - 1].isVisible = nBodyX[i] != 0 && draw;

                if (nBodyX[i] != 0 && draw)
                {
                    ReadOnlySpan<char> level = tFormat(levelText,
                        CDTXMania.DTX.LEVEL[i] / 10.0f
                        + (CDTXMania.DTX.LEVELDEC[i] != 0 ? CDTXMania.DTX.LEVELDEC[i] / 100.0f : 0), "0.00");

                    bool bCLASSIC = false;
                    //If Skill Mode is CLASSIC, always display lvl as Classic Style
                    if (CDTXMania.ConfigIni.nSkillMode == 0 || (CDTXMania.ConfigIni.bClassicScoreDisplay &&
                                                                (i == 1
                                                                    ? !CDTXMania.DTX.bHasChips.YPGuitar
                                                                    : !CDTXMania.DTX.bHasChips.YPBass) &&
                                                                (CDTXMania.DTX.bForceXGChart == false)))
                    {
                        level = tFormat(levelText, CDTXMania.DTX.LEVEL[i], "00");
                        bCLASSIC = true;
                    }

                    txSkillPanel.tDraw2D(nBodyX[i], nBodyY);

                    tDrawSmallNumber(80 + nBodyX[i], 72 + nBodyY,
                        tFormat(text, CDTXMania.stagePerfGuitarScreen.nHitCount_ExclAuto[i].Perfect, 4));
                    tDrawSmallNumber(80 + nBodyX[i], 102 + nBodyY,
                        tFormat(text, CDTXMania.stagePerfGuitarScreen.nHitCount_ExclAuto[i].Great, 4));
                    tDrawSmallNumber(80 + nBodyX[i], 132 + nBodyY,
                        tFormat(text, CDTXMania.stagePerfGuitarScreen.nHitCount_ExclAuto[i].Good, 4));
                    tDrawSmallNumber(80 + nBodyX[i], 162 + nBodyY,
                        tFormat(text, CDTXMania.stagePerfGuitarScreen.nHitCount_ExclAuto[i].Poor, 4));
                    tDrawSmallNumber(80 + nBodyX[i], 192 + nBodyY,
                        tFormat(text, CDTXMania.stagePerfGuitarScreen.nHitCount_ExclAuto[i].Miss, 4));
                    tDrawSmallNumber(80 + nBodyX[i], 222 + nBodyY,
                        tFormat(text, CDTXMania.stagePerfGuitarScreen.actCombo.nCurrentCombo.HighestValue[i], 4));

                    int n現在のノーツ数 =
                        CDTXMania.stagePerfGuitarScreen.nHitCount_IncAuto[i].Perfect +
                        CDTXMania.stagePerfGuitarScreen.nHitCount_IncAuto[i].Great +
                        CDTXMania.stagePerfGuitarScreen.nHitCount_IncAuto[i].Good +
                        CDTXMania.stagePerfGuitarScreen.nHitCount_IncAuto[i].Poor +
                        CDTXMania.stagePerfGuitarScreen.nHitCount_IncAuto[i].Miss;

                    if (CDTXMania.stagePerfGuitarScreen.bIsTrainingMode)
                    {
                        CDTXMania.stagePerfGuitarScreen.actStatusPanel.db現在の達成率.Guitar = 0;
                    }
                    else
                    {
                        dbPERFECT率 =
                            Math.Round((100.0 * CDTXMania.stagePerfGuitarScreen.nHitCount_ExclAuto[i].Perfect) /
                                       n現在のノーツ数);
                        dbGREAT率 = Math.Round((100.0 * CDTXMania.stagePerfGuitarScreen.nHitCount_ExclAuto[i].Great /
                                               n現在のノーツ数));
                        dbGOOD率 = Math.Round((100.0 * CDTXMania.stagePerfGuitarScreen.nHitCount_ExclAuto[i].Good /
                                              n現在のノーツ数));
                        dbPOOR率 = Math.Round((100.0 * CDTXMania.stagePerfGuitarScreen.nHitCount_ExclAuto[i].Poor /
                                              n現在のノーツ数));
                        dbMISS率 = Math.Round((100.0 * CDTXMania.stagePerfGuitarScreen.nHitCount_ExclAuto[i].Miss /
                                              n現在のノーツ数));
                        dbMAXCOMBO率 = Math.Round((100.0 *
                            CDTXMania.stagePerfGuitarScreen.actCombo.nCurrentCombo.HighestValue[i] / n現在のノーツ数));
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

                    tDrawSmallNumber(167 + nBodyX[i], 72 + nBodyY, tFormatPercent(text, dbPERFECT率));
                    tDrawSmallNumber(167 + nBodyX[i], 102 + nBodyY, tFormatPercent(text, dbGREAT率));
                    tDrawSmallNumber(167 + nBodyX[i], 132 + nBodyY, tFormatPercent(text, dbGOOD率));
                    tDrawSmallNumber(167 + nBodyX[i], 162 + nBodyY, tFormatPercent(text, dbPOOR率));
                    tDrawSmallNumber(167 + nBodyX[i], 192 + nBodyY, tFormatPercent(text, dbMISS率));
                    tDrawSmallNumber(167 + nBodyX[i], 222 + nBodyY, tFormatPercent(text, dbMAXCOMBO率));

                    //Draw achievement rate
                    if (txSkillMax != null && CDTXMania.stagePerfGuitarScreen.actStatusPanel.db現在の達成率[i] >= 100.0)
                    {
                        txSkillMax.tDraw2D(127 + nBodyX[i], 277 + nBodyY);
                    }
                    else
                    {
                        tDrawLargeNumber(58 + nBodyX[i], 277 + nBodyY, tFormat(text,
                            CDTXMania.stagePerfGuitarScreen.actStatusPanel.db現在の達成率[i], "##0.00", 6));
                        if (txPercent != null)
                            txPercent.tDraw2D(217 + nBodyX[i], 287 + nBodyY);
                    }

                    //Draw Lag Counters if Lag Display is on
                    if (CDTXMania.ConfigIni.bShowLagHitCount)
                    {
                        //Type-A is Early-Blue, Late-Red
                        bool bTypeAColor = CDTXMania.ConfigIni.nShowLagTypeColor == 0;

                        tDrawLagCounterText(nBodyX[i] + 170, nBodyY + 335,
                            tFormat(text, CDTXMania.stagePerfGuitarScreen.nTimingHitCount[i].nEarly, 4),
                            !bTypeAColor);
                        tDrawLagCounterText(nBodyX[i] + 245, nBodyY + 335,
                            tFormat(text, CDTXMania.stagePerfGuitarScreen.nTimingHitCount[i].nLate, 4),
                            bTypeAColor);
                    }

                    //Draw Game skill (Skill points)
                    if (bCLASSIC)
                    {
                        tDrawLargeNumber(88 + nBodyX[i], 363 + nBodyY, tFormat(text,
                            CDTXMania.stagePerfGuitarScreen.actStatusPanel.db現在の達成率[i]
                            * CDTXMania.DTX.LEVEL[i] * 0.0033, "##0.00", 6));
                    }
                    else
                    {
                        tDrawLargeNumber(88 + nBodyX[i], 363 + nBodyY, tFormat(text,
                            CScoreIni.tCalculateGameSkillFromPlayingSkill(CDTXMania.DTX.LEVEL[i],
                                CDTXMania.DTX.LEVELDEC[i],
                                CDTXMania.stagePerfGuitarScreen.actStatusPanel.db現在の達成率[i]), "##0.00", 6));
                    }


                    if (txDifficultyBadge != null)
                        txDifficultyBadge.tDraw2D(14 + nBodyX[i], 266 + nBodyY,
                            new RectangleF(rectDiffPanelPoint.X, rectDiffPanelPoint.Y, 60, 60));
                    tDisplayLevelNumber((bCLASSIC == true ? 26 : 18) + nBodyX[i], 290 + nBodyY, level);
                }
            }
        }

        return 0;
    }

    // Other

    #region [ private ]
    //-----------------
        
    private STDGBVALUE<int> nBodyX;
    private int nBodyY;
    
    private UIPlayerNameplate[] playerNameplates = new UIPlayerNameplate[2];
    
    //New texture % and MAX
    private BaseTexture txPercent;
    private BaseTexture txSkillMax;

    //-----------------
    #endregion
}