using System.Numerics;
using System.Runtime.InteropServices;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.Drawable;
using DTXMania.UI.Drawable;

namespace DTXMania;

public class ActPerfNewFire : UIGroup, IPerfFire
{
    private int instrument;
    private static readonly Dictionary<ELane, Color4> laneColors = new()
    {
        { ELane.LC, new Color4(1, 0, 0)},
        { ELane.HH, new Color4(0, 0.65f, 1)},
        { ELane.LBD, new Color4(0.7f, 0, 1)},
        { ELane.LP, new Color4(1, 0, 0)},
        { ELane.SD, new Color4(1, 1, 0)},
        { ELane.HT, new Color4(0, 1, 0)},
        { ELane.BD, new Color4(0.7f, 0, 1)},
        { ELane.LT, new Color4(1, 0, 0)},
        { ELane.FT, new Color4(1, 0.5f, 0)},
        { ELane.CY, new Color4(0, 0.65f, 1)},
        { ELane.RD, new Color4(0, 0, 1)},

        { ELane.GtR, new Color4(1, 0, 0)},
        { ELane.GtG, new Color4(0, 1, 0)},
        { ELane.GtB, new Color4(0, 0.65f, 1)},
        { ELane.GtY, new Color4(1, 1, 0)},
        { ELane.GtP, new Color4(1, 0, 1)},
        
        { ELane.BsR, new Color4(1, 0, 0)},
        { ELane.BsG, new Color4(0, 1, 0)},
        { ELane.BsB, new Color4(0, 0.65f, 1)},
        { ELane.BsY, new Color4(1, 1, 0)},
        { ELane.BsP, new Color4(1, 0, 1)},
    };
    private readonly NoteExplosion[] noteExplosions = new NoteExplosion[15];

    private static readonly ELane[] hasCircle =
    [
        ELane.SD,
        ELane.HT,
        ELane.LT,
        ELane.FT,
        
        ELane.GtR,
        ELane.GtG,
        ELane.GtB,
        ELane.GtY,
        ELane.GtP,
        
        ELane.BsR,
        ELane.BsG,
        ELane.BsB,
        ELane.BsY,
        ELane.BsP
    ];
    
    public ActPerfNewFire(int instrument)
    {
        this.instrument = instrument;
        InitializeLaneSizes();
        
        for (int lane = 0; lane < stLaneSize.Length; lane++)
        {
            ELane colorLane = instrument == 0 ? (ELane)lane : (lane + ELane.GtR);
            laneColors.TryGetValue(colorLane, out Color4 color);
            noteExplosions[lane] = AddChild(new NoteExplosion(color, hasCircle.Contains(colorLane)));
            noteExplosions[lane].position = new Vector3(stLaneSize[lane].x + (stLaneSize[lane].w / 2.0f), 0, 0);
        }
    }
    
    [StructLayout(LayoutKind.Sequential)]
    protected struct STLaneSize
    {
        public int x;
        public int w;
    }

    protected STLaneSize[] stLaneSize = [];
    
    protected void InitializeLaneSizes()
    {
        switch (instrument)
        {
            case 0:
                stLaneSize = new STLaneSize[15];
                int[,] sizeXW = new[,]
                {
                    { 290, 80 }, { 367, 46 }, { 470, 54 }, { 582, 60 }, { 528, 46 }, { 645, 46 }, { 694, 46 }, { 748, 64 },
                    { 419, 46 }, { 815, 80 }, { 815, 80 }, { 815, 80 }, { 815, 80 }, { 815, 80 }, { 815, 80 }
                };
                int[,] sizeXW_B = new[,]
                {
                    { 290, 80 }, { 367, 46 }, { 419, 54 }, { 534, 60 }, { 590, 46 }, { 645, 46 }, { 694, 46 }, { 748, 64 },
                    { 478, 46 }, { 815, 64 }, { 815, 80 }, { 507, 80 }, { 815, 80 }, { 815, 80 }, { 815, 80 }
                };
                int[,] sizeXW_C = new[,]
                {
                    { 290, 80 }, { 367, 46 }, { 470, 54 }, { 534, 60 }, { 590, 46 }, { 645, 46 }, { 694, 46 }, { 748, 64 },
                    { 419, 46 }, { 815, 64 }, { 815, 80 }, { 507, 80 }, { 815, 80 }, { 815, 80 }, { 815, 80 }
                };
                int[,] sizeXW_D = new[,]
                {
                    { 290, 80 }, { 367, 46 }, { 419, 54 }, { 582, 60 }, { 476, 46 }, { 645, 46 }, { 694, 46 }, { 748, 64 },
                    { 528, 46 }, { 815, 64 }, { 815, 80 }, { 507, 80 }, { 815, 80 }, { 815, 80 }, { 815, 80 }
                };

                for (int i = 0; i < 15; i++)
                {
                    stLaneSize[i] = new STLaneSize();
                    if (!CDTXMania.ConfigIni.bDrumsEnabled)
                    {
                        continue;
                    }

                    switch (CDTXMania.ConfigIni.eLaneType.Drums)
                    {
                        case EType.A:
                            stLaneSize[i].x = sizeXW[i, 0];
                            stLaneSize[i].w = sizeXW[i, 1];
                            break;
                        case EType.B:
                            stLaneSize[i].x = sizeXW_B[i, 0];
                            stLaneSize[i].w = sizeXW_B[i, 1];
                            break;
                        case EType.C:
                            stLaneSize[i].x = sizeXW_C[i, 0];
                            stLaneSize[i].w = sizeXW_C[i, 1];
                            break;
                        case EType.D:
                            stLaneSize[i].x = sizeXW_D[i, 0];
                            stLaneSize[i].w = sizeXW_D[i, 1];
                            break;
                    }

                    if (i == 7 && CDTXMania.ConfigIni.eRDPosition == ERDPosition.RDRC)
                    {
                        stLaneSize[i].x = sizeXW[9, 0] - 24;
                    }

                    if (i == 9 && CDTXMania.ConfigIni.eRDPosition == ERDPosition.RDRC)
                    {
                        stLaneSize[i].x = sizeXW[7, 0] - 18;
                    }
                }
                break;
            
            case 1:
                Vector2[] guitarPositions =
                [
                    new(107, 0), 
                    new(146, 0), 
                    new(185, 0), 
                    new(224, 0),
                    new(264, 0)
                ];
                
                stLaneSize = new STLaneSize[guitarPositions.Length];
                for (ELane lane = ELane.GtR; lane <= ELane.GtP; lane++)
                {
                    int index = (int)lane - (int)ELane.GtR;
                    stLaneSize[index] = new STLaneSize
                    {
                        x = (int)guitarPositions[index].X,
                        w = 0
                    };
                }
                break;
            
            case 2:
                Vector2[] bassPositions =
                [
                    new(978, 0),
                    new(1017, 0),
                    new(1056, 0),
                    new(1095, 0),
                    new(1134, 0)
                ];
                stLaneSize = new STLaneSize[bassPositions.Length];
                for (ELane lane = ELane.BsR; lane <= ELane.BsP; lane++)
                {
                    int index = (int)lane - (int)ELane.BsR;
                    stLaneSize[index] = new STLaneSize
                    {
                        x = (int)bassPositions[index].X,
                        w = 0
                    };
                }
                break;
        }
    }

    
    public void Start(int lane, CChip? chip = null)
    {
        noteExplosions[lane].Play(chip);
    }

    public void Start(ELane lane, bool bFillIn, bool b大波, bool b細波, int _nJudgeLinePosY_delta_Drums = 0, bool bDisplay = true)
    {
        if (bDisplay)
        {
            noteExplosions[(int)lane].Play();
        }
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        if (instrument == 0)
        {
            position.Y = CDTXMania.ConfigIni.bReverse.Drums
                ? 159 + CDTXMania.ConfigIni.nJudgeLine.Drums
                : 563 - CDTXMania.ConfigIni.nJudgeLine.Drums;
        }
        else
        {
            position.Y = CDTXMania.ConfigIni.bReverse[instrument]
                ? 611 - CDTXMania.ConfigIni.nJudgeLine[instrument]
                : 155 + CDTXMania.ConfigIni.nJudgeLine[instrument];
        }

        base.Draw(parentMatrix);
    }

    public int OnUpdateAndDraw()
    {
        return 0;
    }

    public int iPosY { get; set; }
}