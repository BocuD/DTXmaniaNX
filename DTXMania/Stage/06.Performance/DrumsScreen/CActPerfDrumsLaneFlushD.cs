using System.Drawing;
using System.Runtime.InteropServices;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using FDK;

namespace DTXMania;

internal class CActPerfDrumsLaneFlushD : CActivity
{
    // コンストラクタ

    public CActPerfDrumsLaneFlushD()
    {
        STLaneSize[] stレーンサイズArray = new STLaneSize[11];
        STLaneSize stLaneSize = new()
        {
            x = 298,
            w = 64
        };
        stレーンサイズArray[0] = stLaneSize;
        STLaneSize stレーンサイズ2 = new()
        {
            x = 370,
            w = 46
        };
        stレーンサイズArray[1] = stレーンサイズ2;
        STLaneSize stレーンサイズ3 = new()
        {
            x = 470,
            w = 54
        };
        stレーンサイズArray[2] = stレーンサイズ3;
        STLaneSize stレーンサイズ4 = new()
        {
            x = 582,
            w = 60
        };
        stレーンサイズArray[3] = stレーンサイズ4;
        STLaneSize stレーンサイズ5 = new()
        {
            x = 528,
            w = 46
        };
        stレーンサイズArray[4] = stレーンサイズ5;
        STLaneSize stレーンサイズ6 = new()
        {
            x = 645,
            w = 46
        };
        stレーンサイズArray[5] = stレーンサイズ6;
        STLaneSize stレーンサイズ7 = new()
        {
            x = 694,
            w = 46
        };
        stレーンサイズArray[6] = stレーンサイズ7;
        STLaneSize stレーンサイズ8 = new()
        {
            x = 748,
            w = 64
        };
        stレーンサイズArray[7] = stレーンサイズ8;
        STLaneSize stレーンサイズ9 = new()
        {
            x = 419,
            w = 48
        };
        stレーンサイズArray[8] = stレーンサイズ9;
        STLaneSize stレーンサイズ10 = new()
        {
            x = 815,
            w = 38
        };
        stレーンサイズArray[9] = stレーンサイズ10;
        STLaneSize stレーンサイズ11 = new()
        {
            x = 419,
            w = 48
        };
        stレーンサイズArray[10] = stレーンサイズ11;
        stレーンサイズ = stレーンサイズArray;
        strファイル名 =
        [
            @"Graphics\ScreenPlayDrums lane flush leftcymbal.png",
            @"Graphics\ScreenPlayDrums lane flush hihat.png",
            @"Graphics\ScreenPlayDrums lane flush snare.png",
            @"Graphics\ScreenPlayDrums lane flush bass.png",
            @"Graphics\ScreenPlayDrums lane flush hitom.png",
            @"Graphics\ScreenPlayDrums lane flush lowtom.png",
            @"Graphics\ScreenPlayDrums lane flush floortom.png",
            @"Graphics\ScreenPlayDrums lane flush cymbal.png",
            @"Graphics\ScreenPlayDrums lane flush leftpedal.png",
            @"Graphics\ScreenPlayDrums lane flush ridecymbal.png",
            @"Graphics\ScreenPlayDrums lane flush leftpedal.png",

            @"Graphics\ScreenPlayDrums lane flush leftcymbal reverse.png",
            @"Graphics\ScreenPlayDrums lane flush hihat reverse.png",
            @"Graphics\ScreenPlayDrums lane flush snare reverse.png",
            @"Graphics\ScreenPlayDrums lane flush bass reverse.png",
            @"Graphics\ScreenPlayDrums lane flush hitom reverse.png", 
            @"Graphics\ScreenPlayDrums lane flush lowtom reverse.png",
            @"Graphics\ScreenPlayDrums lane flush floortom reverse.png",
            @"Graphics\ScreenPlayDrums lane flush cymbal reverse.png",
            @"Graphics\ScreenPlayDrums lane flush leftpedal reverse.png",
            @"Graphics\ScreenPlayDrums lane flush ridecymbal reverse.png",
            @"Graphics\ScreenPlayDrums lane flush leftpedal reverse.png"
        ];
        
        bActivated = false;
    }
		
		
    // メソッド

    public void Start( ELane lane, float f強弱度合い )
    {
        int num = (int) ( ( 1f - f強弱度合い ) * 55f );
        ct進行[ (int) lane ] = new CCounter( num, 90, 3, CDTXMania.Timer );
    }


    // CActivity 実装

    public override void OnActivate()
    {
        for( int i = 0; i < 11; i++ )
        {
            ct進行[ i ] = new CCounter();
        }
        base.OnActivate();
    }
    public override void OnDeactivate()
    {
        for( int i = 0; i < 11; i++ )
        {
            ct進行[ i ] = null;
        }
        base.OnDeactivate();
    }
    public override void OnManagedCreateResources()
    {
        if( bActivated )
        {
            if (CDTXMania.ConfigIni.nLaneDisp.Drums == 0 || CDTXMania.ConfigIni.nLaneDisp.Drums == 2)
            {
                txLine = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_Paret.png")); 
            }
            else
            {
                txLine = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_Paret_Dark.png"));
            }

            for( int i = 0; i < 22; i++ )
            {
                txFlush[ i ] = BaseTexture.LoadFromPath( CSkin.Path( strファイル名[ i ] ) );
            }
            base.OnManagedCreateResources();
        }
    }
    
    public override int OnUpdateAndDraw()
    {
        if( bActivated )
        {
            for( int i = 0; i < 11; i++ )
            {
                if( !ct進行[ i ].bStopped )
                {
                    ct進行[ i ].tUpdate();
                    if( ct進行[ i ].bReachedEndValue )
                    {
                        ct進行[ i ].tStop();
                    }
                }
            }
            for ( int i = 0; i < 10; i++ )
            {
                int index = n描画順[i];
                int numOfLanesflagIndex = (int)CDTXMania.ConfigIni.eNumOfLanes.Drums;

                int x2 = (CDTXMania.stagePerfDrumsScreen.actPad.st基本位置[index].x + 32);
                int x3 = (CDTXMania.stagePerfDrumsScreen.actPad.st基本位置[index].x + (CDTXMania.ConfigIni.bReverse.Drums ? 32 : 32));
                int xHH = (CDTXMania.stagePerfDrumsScreen.actPad.st基本位置[index].x + 32);
                int xLC = (CDTXMania.stagePerfDrumsScreen.actPad.st基本位置[index].x + (CDTXMania.ConfigIni.bReverse.Drums ? 32 : 32));
                int xCY = (CDTXMania.stagePerfDrumsScreen.actPad.st基本位置[index].x + (CDTXMania.ConfigIni.bReverse.Drums ? 79 : 79));
                int nAlpha = 255 - ((int)(((float)(CDTXMania.ConfigIni.nMovieAlpha * 255)) / 10f));
                //if (CDTXMania.ConfigIni.eDark == EDarkMode.OFF) //2013.02.17 kairera0467 ダークOFF以外でも透明度を有効にした。
                //26072020: Check flag before drawing
                if (nDrawFlags[numOfLanesflagIndex, index] == 1 && txLine != null)
                {
                    Color4 color = Color4.White;
                    color.Alpha = nAlpha / 255.0f;
                    
                    #region[ 動くレーン ]
                    if (CDTXMania.ConfigIni.nLaneDisp.Drums == 0 || CDTXMania.ConfigIni.nLaneDisp.Drums == 2)
                    {
                        if (index == 0) //LC
                        {
                            txLine.tDraw2D(CDTXMania.app.Device, 295, 0, new RectangleF(0, 0, 72, 720), color); //左の棒
                        }
                        if (index == 1) //HH
                        {
                            txLine.tDraw2D(CDTXMania.app.Device, 367, 0, new RectangleF(72, 0, 49, 720), color); //左の棒
                        }
                        if (index == 2) //SD
                        {
                            if (CDTXMania.ConfigIni.eLaneType.Drums == EType.A || CDTXMania.ConfigIni.eLaneType.Drums == EType.C)
                            {
                                txLine.tDraw2D(CDTXMania.app.Device, 467, 0, new RectangleF(172, 0, 57, 720), color);
                            }
                            else if (CDTXMania.ConfigIni.eLaneType.Drums == EType.B || CDTXMania.ConfigIni.eLaneType.Drums == EType.D)
                            {
                                txLine.tDraw2D(CDTXMania.app.Device, 416, 0, new RectangleF(172, 0, 57, 720), color);
                            }
                        }
                        if (index == 3) //BD
                        {
                            if (CDTXMania.ConfigIni.eLaneType.Drums == EType.A || CDTXMania.ConfigIni.eLaneType.Drums == EType.D)
                            {
                                txLine.tDraw2D(CDTXMania.app.Device, 573, 0, new RectangleF(278, 0, 69, 720), color);
                            }
                            else if (CDTXMania.ConfigIni.eLaneType.Drums == EType.B || CDTXMania.ConfigIni.eLaneType.Drums == EType.C)
                            {
                                txLine.tDraw2D(CDTXMania.app.Device, 524, 0, new RectangleF(278, 0, 69, 720), color);
                            }
                        }
                        if (index == 4) //HT
                        {
                            if (CDTXMania.ConfigIni.eLaneType.Drums == EType.A)
                            {
                                txLine.tDraw2D(CDTXMania.app.Device, 524, 0, new RectangleF(229, 0, 49, 720), color);
                            }
                            else if (CDTXMania.ConfigIni.eLaneType.Drums == EType.B || CDTXMania.ConfigIni.eLaneType.Drums == EType.C)
                            {
                                txLine.tDraw2D(CDTXMania.app.Device, 593, 0, new RectangleF(229, 0, 49, 720), color);
                            }
                            else if (CDTXMania.ConfigIni.eLaneType.Drums == EType.D)
                            {
                                txLine.tDraw2D(CDTXMania.app.Device, 473, 0, new RectangleF(229, 0, 49, 720), color);
                            }
                        }

                        if (index == 5) //LT
                        {
                            txLine.tDraw2D(CDTXMania.app.Device, 642, 0, new RectangleF(347, 0, 49, 720), color);
                        }
                        if (index == 6) //FT
                        {
                            txLine.tDraw2D(CDTXMania.app.Device, 691, 0, new RectangleF(396, 0, 54, 720), color);
                            //if (CDTXMania.ConfigIni.eRDPosition == ERDPosition.RCRD)
                            {
                                //this.txLine.tDraw2D(CDTXMania.app.Device, 742, 0, new  SharpDX.RectangleF(447, 0, 5, 720));
                            }
                            //else
                            {
                                //this.txLine.tDraw2D(CDTXMania.app.Device, 742, 0, new  SharpDX.RectangleF(447, 0, 4, 720));
                            }

                        }

                        if (index == 7) //CY
                        {
                            if (CDTXMania.ConfigIni.eRDPosition == ERDPosition.RCRD)
                            {
                                txLine.tDraw2D(CDTXMania.app.Device, 745, 0, new RectangleF(450, 0, 70, 720), color);
                            }
                            else if (CDTXMania.ConfigIni.eRDPosition == ERDPosition.RDRC)
                            {
                                txLine.tDraw2D(CDTXMania.app.Device, xCY - 31, 0, new RectangleF(450, 0, 70, 720), color);
                            }
                        }
                        if (index == 8) //RD
                        {
                            if (CDTXMania.ConfigIni.eRDPosition == ERDPosition.RCRD)
                            {
                                txLine.tDraw2D(CDTXMania.app.Device, xCY - 55, 0, new RectangleF(520, 0, 38, 720), color);
                            }
                            else if (CDTXMania.ConfigIni.eRDPosition == ERDPosition.RDRC)
                            {
                                txLine.tDraw2D(CDTXMania.app.Device, xCY - 124, 0, new RectangleF(520, 0, 38, 720), color);
                            }                                
                        }
                        if (index == 9) //LP
                        {
                            if (CDTXMania.ConfigIni.eLaneType.Drums == EType.A || CDTXMania.ConfigIni.eLaneType.Drums == EType.C)
                            {
                                txLine.tDraw2D(CDTXMania.app.Device, x2 - 12, 0, new RectangleF(121, 0, 51, 720), color);
                            }
                            else if (CDTXMania.ConfigIni.eLaneType.Drums == EType.B)
                            {
                                txLine.tDraw2D(CDTXMania.app.Device, x2 + 45, 0, new RectangleF(121, 0, 51, 720), color);
                                //this.txLine.tDraw2D(CDTXMania.app.Device, 524, 0, new  SharpDX.RectangleF(278, 0, 6, 720));
                            }
                            else if (CDTXMania.ConfigIni.eLaneType.Drums == EType.D)
                            {
                                txLine.tDraw2D(CDTXMania.app.Device, 522, 0, new RectangleF(121, 0, 51, 720), color);
                            }
                        }
                    }
                    else
                    {

                        if (index == 1) //HH
                        {
                            int l_drumPanelWidth = 558;
                            int l_xOffset = 0;
                            if (CDTXMania.ConfigIni.eNumOfLanes.Drums == EType.B)
                            {
                                l_drumPanelWidth = 519; // 0x207
                            }
                            else if(CDTXMania.ConfigIni.eNumOfLanes.Drums == EType.C)
                            {
                                l_drumPanelWidth = 447;
                                l_xOffset = 72;
                            }
                            txLine.tDraw2D(CDTXMania.app.Device, 295 + l_xOffset, 0, new RectangleF(0, 0, l_drumPanelWidth, 720), color);
                        }
                    }                        
                }

                #endregion
            }
            for (int j = 0; j < 11; j++)
            {
                if (CDTXMania.ConfigIni.bLaneFlush.Drums != false)
                {
                    if (!ct進行[j].bStopped)
                    {
                        int x = stレーンサイズ[j].x;
                        int w = stレーンサイズ[j].w;
                        #region[レーン切り替え関連]
                        if (j == 2)
                        {
                            //SD
                            if (CDTXMania.ConfigIni.eLaneType.Drums == EType.B || CDTXMania.ConfigIni.eLaneType.Drums == EType.D)
                            {
                                x = stレーンサイズ[9].x - 396;
                            }
                        }
                        if (j == 3)
                        {
                            //BD
                            if ((CDTXMania.ConfigIni.eLaneType.Drums == EType.B) || (CDTXMania.ConfigIni.eLaneType.Drums == EType.C))
                            {
                                x = stレーンサイズ[4].x + 6;
                            }
                            else if (CDTXMania.ConfigIni.eLaneType.Drums == EType.D)
                            {
                                x = stレーンサイズ[4].x + 54;
                            }
                        }

                        if (j == 4)
                        {
                            //HT
                            if ((CDTXMania.ConfigIni.eLaneType.Drums == EType.B) || (CDTXMania.ConfigIni.eLaneType.Drums == EType.C))
                            {
                                x = stレーンサイズ[3].x + 13;
                            }
                            else if (CDTXMania.ConfigIni.eLaneType.Drums == EType.D)
                            {
                                x = stレーンサイズ[3].x - 106;
                            }
                        }

                        if (j == 7)
                        {
                            if ((CDTXMania.ConfigIni.eRDPosition == ERDPosition.RDRC))
                            {
                                x = stレーンサイズ[9].x - 29;
                            }
                        }

                        if (j == 9)
                        {
                            if ((CDTXMania.ConfigIni.eRDPosition == ERDPosition.RDRC))
                            {
                                x = stレーンサイズ[7].x;
                            }
                        }

                        if ((j == 8) || (j == 10))
                        {
                            if (CDTXMania.ConfigIni.eLaneType.Drums == EType.B)
                            {
                                x = stレーンサイズ[2].x + 5;
                            }
                            else if (CDTXMania.ConfigIni.eLaneType.Drums == EType.D)
                            {
                                x = stレーンサイズ[2].x + 56;
                            }

                        }
                        #endregion
                        for (int k = 0; k < 3; k++)
                        {
                            if (CDTXMania.ConfigIni.bReverse.Drums)
                            {
                                int y = 32 + ((ct進行[j].nCurrentValue * 740) / 100);
                                for (int m = 0; m < w; m += 42)
                                {
                                    if (txFlush[j + 11] != null)
                                    {
                                        txFlush[j + 11].tDraw2D(CDTXMania.app.Device, x + m, y, new RectangleF((k * 42), 0, ((w - m) < 42) ? (w - m) : 42, 128));
                                    }
                                }
                            }
                            else
                            {
                                int num8 = (200 + (500)) - ((ct進行[j].nCurrentValue * 740) / 100);
                                if (num8 < 720)
                                {
                                    for (int n = 0; n < w; n += 42)
                                    {
                                        if (txFlush[j] != null)
                                        {
                                            Color4 col = Color4.White;
                                            col.Alpha = num8 / 255.0f;
                                            txFlush[j].tDraw2D(CDTXMania.app.Device, x + n, num8, new RectangleF(k * 42, 0, ((w - n) < 42) ? (w - n) : 42, 128), col);
                                        }
                                    }
                                }
                            }

                        }
                    }
                }
            }
        }
        return 0;
    }

		
    // Other

    #region [ private ]
    //-----------------
    [StructLayout( LayoutKind.Sequential )]
    private struct STLaneSize
    {
        public int x;
        public int w;
    }
    private readonly STLaneSize[] stレーンサイズ;
    private CCounter[] ct進行 = new CCounter[ 11 ];
    private readonly string[] strファイル名;
    private readonly int[] n描画順 = [9, 2, 4, 6, 5, 3, 1, 8, 7, 0]; //new int[] { 9, 3, 2, 6, 5, 4, 8, 7, 1, 0 };
    //26072020: New array Fisyher
    private readonly int[,] nDrawFlags = new int[3, 10] { { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }, { 1, 1, 1, 1, 1, 1, 1, 1, 0, 1 }, { 0, 1, 1, 1, 1, 1, 1, 1, 0, 1 } };
    private BaseTexture[] txFlush = new BaseTexture[ 22 ];
    private BaseTexture txLC;
    private BaseTexture txLine;
    //-----------------
    #endregion
}