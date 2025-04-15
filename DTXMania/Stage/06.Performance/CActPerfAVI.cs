using System.Runtime.InteropServices;
using System.Drawing;
using System.Diagnostics;
using DTXMania.Core;
using SharpDX;
using SharpDX.Direct3D9;
using FDK;

using Rectangle = System.Drawing.Rectangle;
using Point = System.Drawing.Point;

namespace DTXMania;

internal class CActPerfAVI : CActivity
{
    // コンストラクタ

    public CActPerfAVI(bool bIsDuringPerformance = true)
    {
        this.bIsDuringPerformance = bIsDuringPerformance;
        if (this.bIsDuringPerformance)
        {
            //base.listChildActivities.Add(this.actFill = new CActPerfDrumsFillingEffect());
            listChildActivities.Add(actPanel = new CActPerfPanelString());
        }
            
        bActivated = false;
    }


    // メソッド
        
    public void Start(EChannel nチャンネル番号, CAVI rAVI, int n開始サイズW, int n開始サイズH, int n終了サイズW, int n終了サイズH, int n画像側開始位置X, int n画像側開始位置Y, int n画像側終了位置X, int n画像側終了位置Y, int n表示側開始位置X, int n表示側開始位置Y, int n表示側終了位置X, int n表示側終了位置Y, int n総移動時間ms, int n移動開始時刻ms, bool bPlayFromBeginning = false)
    {
        //2016.01.21 kairera0467 VfW時代のコードを除去+大改造
        Trace.TraceInformation("CActPerfAVI: Start(): " + rAVI.strファイル名);

        this.rAVI = rAVI;
            
        #region[ アスペクト比からどっちを使うか判別 ]
        // 旧DShowモードを使っていて、旧規格クリップだったら新DShowモードを使う。
        //if( CDTXMania.ConfigIni.bDirectShowMode == false )
        {
            fClipアスペクト比 = ( (float)rAVI.avi.nフレーム幅 / (float)rAVI.avi.nフレーム高さ );
            //this.bUseMRenderer = false;
        }
            
        #endregion

        if( nチャンネル番号 == EChannel.Movie || nチャンネル番号 == EChannel.MovieFull)
        {
            if( bUseCAviDS )
            {
                //CAviDS
                this.rAVI = rAVI;
                this.n開始サイズW = n開始サイズW;
                this.n開始サイズH = n開始サイズH;
                this.n終了サイズW = n終了サイズW;
                this.n終了サイズH = n終了サイズH;
                this.n画像側開始位置X = n画像側開始位置X;
                this.n画像側開始位置Y = n画像側開始位置Y;
                this.n画像側終了位置X = n画像側終了位置X;
                this.n画像側終了位置Y = n画像側終了位置Y;
                this.n表示側開始位置X = n表示側開始位置X;
                this.n表示側開始位置Y = n表示側開始位置Y;
                this.n表示側終了位置X = n表示側終了位置X;
                this.n表示側終了位置Y = n表示側終了位置Y;
                this.n総移動時間ms = n総移動時間ms;
                this.n移動開始時刻ms = ( n移動開始時刻ms != -1 ) ? n移動開始時刻ms : CSoundManager.rcPerformanceTimer.nCurrentTime;

                if( ( this.rAVI != null ) && ( this.rAVI.avi != null ) )
                {
                    float f拡大率x;
                    float f拡大率y;
                    framewidth = (uint)this.rAVI.avi.nフレーム幅;
                    frameheight = (uint)this.rAVI.avi.nフレーム高さ;
                    if( tx描画用 == null )
                    {
                        tx描画用 = new CTexture( CDTXMania.app.Device, (int)framewidth, (int)frameheight, CDTXMania.app.GraphicsDeviceManager.CurrentSettings.BackBufferFormat, Pool.Managed );
                    }

                    if( fClipアスペクト比 < 1.77f )
                    {
                        //旧規格クリップだった場合
                        ratio1 = 720f / ( (float)frameheight );
                        position = (int)( ( 1280f - ( framewidth * ratio1 ) ) / 2f );
                        int num = (int)( framewidth * ratio1 );
                        if( num <= 565 )
                        {
                            position = 295 + ( (int)( ( 565f - ( framewidth * ratio1 ) ) / 2f ) );
                            i1 = 0;
                            i2 = (int)framewidth;
                            rec = new Rectangle( 0, 0, 0, 0 );
                            rec3 = new Rectangle( 0, 0, 0, 0 );
                            rec2 = new Rectangle( 0, 0, (int)framewidth, (int)frameheight );
                        }
                        else
                        {
                            position = 295 - ( (int)( ( ( framewidth * ratio1 ) - 565f ) / 2f ) );
                            i1 = (int)( ( (float)( 295 - position ) ) / ratio1 );
                            i2 = (int)( ( 565f / ( (float)num ) ) * framewidth );
                            rec = new Rectangle( 0, 0, i1, (int)frameheight );
                            rec3 = new Rectangle( i1 + i2, 0, ( ( (int)framewidth ) - i1 ) - i2, (int)frameheight );
                            rec2 = new Rectangle( i1, 0, i2, (int)frameheight );
                        }
                        tx描画用.vcScaleRatio.X = ratio1;
                        tx描画用.vcScaleRatio.Y = ratio1;
                    }
                    else
                    {
                        //ワイドクリップの処理
                        ratio1 = 1280f / ( (float)framewidth );
                        position = (int)( ( 720f - ( frameheight * ratio1 ) ) / 2f );
                        i1 = (int)( framewidth * 0.23046875 );
                        i2 = (int)( framewidth * 0.44140625 );
                        rec = new Rectangle( 0, 0, i1, (int)frameheight );
                        rec2 = new Rectangle( i1, 0, i2, (int)frameheight );
                        rec3 = new Rectangle( i1 + i2, 0, ( ( (int)framewidth ) - i1 ) - i2, (int)frameheight );
                        tx描画用.vcScaleRatio.X = ratio1;
                        tx描画用.vcScaleRatio.Y = ratio1;
                    }


                    if( framewidth > 420 )
                    {
                        f拡大率x = 420f / ( (float)framewidth );
                    }
                    else
                    {
                        f拡大率x = 1f;
                    }
                    if( frameheight > 580 )
                    {
                        f拡大率y = 580f / ( (float)frameheight );
                    }
                    else
                    {
                        f拡大率y = 1f;
                    }
                    if( f拡大率x > f拡大率y )
                    {
                        f拡大率x = f拡大率y;
                    }
                    else
                    {
                        f拡大率y= f拡大率x;
                    }

                    smallvc = new Vector3( f拡大率x, f拡大率y, 1f );
                    vclip = new Vector3( 1.42f, 1.42f, 1f );
                    this.rAVI.avi.Run();
                }
            }
            else
            {
                this.rAVI = rAVI;
                this.n開始サイズW = n開始サイズW;
                this.n開始サイズH = n開始サイズH;
                this.n終了サイズW = n終了サイズW;
                this.n終了サイズH = n終了サイズH;
                this.n画像側開始位置X = n画像側開始位置X;
                this.n画像側開始位置Y = n画像側開始位置Y;
                this.n画像側終了位置X = n画像側終了位置X;
                this.n画像側終了位置Y = n画像側終了位置Y;
                this.n表示側開始位置X = n表示側開始位置X;
                this.n表示側開始位置Y = n表示側開始位置Y;
                this.n表示側終了位置X = n表示側終了位置X;
                this.n表示側終了位置Y = n表示側終了位置Y;
                this.n総移動時間ms = n総移動時間ms;
                this.n移動開始時刻ms = ( n移動開始時刻ms != -1 ) ? n移動開始時刻ms : CSoundManager.rcPerformanceTimer.nCurrentTime;
                n前回表示したフレーム番号 = -1;
                if( ( this.rAVI != null ) && ( this.rAVI.avi != null ) )
                {
                    float f拡大率x;
                    float f拡大率y;
                    framewidth = this.rAVI.avi.nフレーム幅;
                    frameheight = this.rAVI.avi.nフレーム高さ;
                    if( tx描画用 == null )
                    {
                        tx描画用 = new CTexture( CDTXMania.app.Device, (int)framewidth, (int)frameheight, CDTXMania.app.GraphicsDeviceManager.CurrentSettings.BackBufferFormat, Pool.Managed );
                    }
                    if( fClipアスペクト比 < 1.77f )
                    {
                        //旧規格クリップだった場合
                        ratio1 = 720.0f / frameheight;
                        position = (int)( ( 1280.0f - ( framewidth * ratio1 ) ) / 2.0f );
                        int num = (int)( framewidth * ratio1 );
                        if( num <= 565 )
                        {
                            position = 295 + ( (int)( ( 565f - ( framewidth * ratio1 ) ) / 2f ) );
                            i1 = 0;
                            i2 = (int)framewidth;
                            rec = new Rectangle(0, 0, 0, 0);
                            rec3 = new Rectangle(0, 0, 0, 0);
                            rec2 = new Rectangle(0, 0, (int)framewidth, (int)frameheight);
                        }
                        else
                        {
                            position = 295 - ((int)(((framewidth * ratio1) - 565f) / 2f));
                            i1 = (int)(((float)(295 - position)) / ratio1);
                            i2 = (int)((565f / ((float)num)) * framewidth);
                            rec = new Rectangle(0, 0, i1, (int)frameheight);
                            rec3 = new Rectangle(i1 + i2, 0, (((int)framewidth) - i1) - i2, (int)frameheight);
                            rec2 = new Rectangle(i1, 0, i2, (int)frameheight);
                        }
                        tx描画用.vcScaleRatio.X = ratio1;
                        tx描画用.vcScaleRatio.Y = ratio1;
                    }
                    else
                    {
                        //ワイドクリップの処理
                        ratio1 = 1280f / ((float)framewidth);
                        position = (int)((720f - (frameheight * ratio1)) / 2f);
                        i1 = (int)(framewidth * 0.23046875);
                        i2 = (int)(framewidth * 0.44140625);
                        rec = new Rectangle(0, 0, i1, (int)frameheight);
                        rec2 = new Rectangle(i1, 0, i2, (int)frameheight);
                        rec3 = new Rectangle(i1 + i2, 0, (((int)framewidth) - i1) - i2, (int)frameheight);
                        tx描画用.vcScaleRatio.X = ratio1;
                        tx描画用.vcScaleRatio.Y = ratio1;
                    }


                    if (framewidth > 420)
                    {
                        f拡大率x = 420f / ((float)framewidth);
                    }
                    else
                    {
                        f拡大率x = 1f;
                    }
                    if (frameheight > 580)
                    {
                        f拡大率y = 580f / ((float)frameheight);
                    }
                    else
                    {
                        f拡大率y = 1f;
                    }
                    if (f拡大率x > f拡大率y)
                    {
                        f拡大率x = f拡大率y;
                    }
                    else
                    {
                        f拡大率y = f拡大率x;
                    }

                    smallvc = new Vector3(f拡大率x, f拡大率y, 1f);
                    vclip = new Vector3(1.42f, 1.42f, 1f);
                }
            }
        }

    }
    public void SkipStart(int n移動開始時刻ms)
    {
        if (CDTXMania.DTX != null) {
            foreach (CChip chip in CDTXMania.DTX.listChip)
            {
                if (chip.nPlaybackTimeMs > n移動開始時刻ms)
                {
                    break;
                }
                switch (chip.eAVI種別)
                {
                    case EAVIType.AVI:
                    {
                        if (chip.rAVI != null)
                        {
                            if (chip.rAVI.avi != null) {
                                chip.rAVI.avi.Seek(n移動開始時刻ms - chip.nPlaybackTimeMs);
                            }
                            Start(chip.nChannelNumber, chip.rAVI, 1280, 720, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, chip.nPlaybackTimeMs);
                        }
                        continue;
                    }
                    case EAVIType.AVIPAN:
                    {
                        if (chip.rAVIPan != null)
                        {
                            if (chip.rAVI != null && chip.rAVI.avi != null)
                            {
                                chip.rAVI.avi.Seek(n移動開始時刻ms - chip.nPlaybackTimeMs);
                            }
                            Start(chip.nChannelNumber, chip.rAVI, chip.rAVIPan.sz開始サイズ.Width, chip.rAVIPan.sz開始サイズ.Height, chip.rAVIPan.sz終了サイズ.Width, chip.rAVIPan.sz終了サイズ.Height, chip.rAVIPan.pt動画側開始位置.X, chip.rAVIPan.pt動画側開始位置.Y, chip.rAVIPan.pt動画側終了位置.X, chip.rAVIPan.pt動画側終了位置.Y, chip.rAVIPan.pt表示側開始位置.X, chip.rAVIPan.pt表示側開始位置.Y, chip.rAVIPan.pt表示側終了位置.X, chip.rAVIPan.pt表示側終了位置.Y, chip.n総移動時間, chip.nPlaybackTimeMs);
                        }
                        continue;
                    }
                }
            }
        }
            
    }
    public void Stop()
    {
        Trace.TraceInformation("CActPerfAVI: Stop()");
        if ((rAVI != null) && (rAVI.avi != null))
        {  
            n移動開始時刻ms = -1;
            rAVI.avi.Stop();
            rAVI.avi.Seek(0);
        }
        //if (this.dsBGV != null && CDTXMania.ConfigIni.bDirectShowMode == true)
        //{
        //    this.dsBGV.dshow.MediaCtrl.Stop();
        //    this.bDShowクリップを再生している = false;
        //}
    }
    public void MovieMode()
    {
        nCurrentMovieMode = CDTXMania.ConfigIni.nMovieMode;
        if ((nCurrentMovieMode == 1) || (nCurrentMovieMode == 3))
        {
            bFullScreen = true;
        }
        else
        {
            bFullScreen = false;
        }
        if ((nCurrentMovieMode == 2) || (nCurrentMovieMode == 3))
        {
            bWindowMode = true;
        }
        else
        {
            bWindowMode = false;
        }
    }

    public void Cont(int n再開時刻ms)
    {
        if ((rAVI != null) && (rAVI.avi != null))
        {
            n移動開始時刻ms = n再開時刻ms;
        }
    }


    // CActivity 実装
    public override void OnActivate()
    {
        //this.rAVI = null;
        //this.dsBGV = null;
        n移動開始時刻ms = -1;
        n前回表示したフレーム番号 = -1;
        bフレームを作成した = false;
        b再生トグル = false;
        bDShowクリップを再生している = false;
        pBmp = IntPtr.Zero;
        MovieMode();
        nAlpha = 255 - ((int)(((float)(CDTXMania.ConfigIni.nMovieAlpha * 255)) / 10f));
            
        base.OnActivate();
    }
    public override void OnDeactivate()
    {            
        base.OnDeactivate();
    }
    public override void OnManagedCreateResources()
    {
        if (bActivated)
        {
            //this.txドラム = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\7_Drums.png"));
            if (CDTXMania.ConfigIni.bGuitarEnabled)
            {
                txクリップパネル = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\7_ClipPanelC.png"));
            }
            else if (CDTXMania.ConfigIni.bGraph有効.Drums && CDTXMania.ConfigIni.bDrumsEnabled)
            {
                txクリップパネル = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\7_ClipPanelB.png"));
            }
            else
            {
                txクリップパネル = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\7_ClipPanel.png"));
            }
            txDShow汎用 = new CTexture(CDTXMania.app.Device, 1280, 720, CDTXMania.app.GraphicsDeviceManager.CurrentSettings.BackBufferFormat, Pool.Managed);

            for (int i = 0; i < 1; i++)
            {
                stFillIn[i] = new STフィルイン();
                stFillIn[i].ctUpdate = new CCounter(0, 30, 30, CDTXMania.Timer);
                stFillIn[i].bInUse = false;
            }
            txフィルインエフェクト = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\7_Fillin Effect.png" ) );

            //this.txフィルインエフェクト = new CTexture[ 31 ];
            //for( int fill = 0; fill < 31; fill++ )
            //{
            //    this.txフィルインエフェクト[ fill ] = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\StageEffect\7_StageEffect_" + fill.ToString() + ".png" ) );
            //    if( this.txフィルインエフェクト[ fill ] == null )
            //        continue;

            //    this.txフィルインエフェクト[ fill ].bAdditiveBlending = true;
            //    this.txフィルインエフェクト[ fill ].vcScaleRatio = new Vector3( 2.0f, 2.0f, 1.0f );
            //}

            base.OnManagedCreateResources();
        }
    }
    public override void OnManagedReleaseResources()
    {
        if (bActivated)
        {                
            //特殊テクスチャ 3枚
            if (tx描画用 != null)
            {
                tx描画用.Dispose();
                tx描画用 = null;
            }
            if (tx描画用2 != null)
            {
                tx描画用2.Dispose();
                tx描画用2 = null;
            }
            if (txDShow汎用 != null)
            {
                txDShow汎用.Dispose();
                txDShow汎用 = null;
            }
            if (txlanes != null)
            {
                txlanes.Dispose();
                txlanes = null;
            }
            //CDTXMania.t安全にDisposeする(ref this.ds汎用);
            //テクスチャ 17枚
            //CDTXMania.tReleaseTexture(ref this.txドラム);
            CDTXMania.tReleaseTexture(ref txクリップパネル);
            CDTXMania.tReleaseTexture( ref txフィルインエフェクト );
            //for( int ar = 0; ar < 31; ar++ )
            //{
            //    CDTXMania.tReleaseTexture( ref this.txフィルインエフェクト[ ar ] );
            //}
            base.OnManagedReleaseResources();
        }
    }
    public unsafe int tUpdateAndDraw(int x, int y)
    {   
        if ((bActivated))
        {
            #region[ムービーのフレーム作成処理]
            if ( ( ( tx描画用 != null )) && ( rAVI != null ) ) //クリップ無し曲での進入防止。
            {
                Rectangle rectangle;
                Rectangle rectangle2;

                #region[ frameNoFromTime ]
                int time = (int)( ( CSoundManager.rcPerformanceTimer.nCurrentTime - n移動開始時刻ms ) * ( ( (double)CDTXMania.ConfigIni.nPlaySpeed ) / 20.0 ) );
                int frameNoFromTime = 0;
                if( bUseCAviDS )
                    frameNoFromTime = time;
                //else
                //    frameNoFromTime = this.rAVI.avi.GetFrameNoFromTime( time );
                #endregion

                if( ( n総移動時間ms != 0 ) && ( n総移動時間ms < time ) )
                {
                    n総移動時間ms = 0;
                    n移動開始時刻ms = -1L;
                }

                //Loop
                if (n総移動時間ms == 0 && time >= rAVI.avi.GetDuration())
                {
                    if (!bIsPreviewMovie && !bLoop)
                    {
                        n移動開始時刻ms = -1L;
                        //return 0;
                    }
                    else 
                    {
                        n移動開始時刻ms = CSoundManager.rcPerformanceTimer.nCurrentTime;
                        time = (int)((CSoundManager.rcPerformanceTimer.nCurrentTime - n移動開始時刻ms) * (((double)CDTXMania.ConfigIni.nPlaySpeed) / 20.0));
                        rAVI.avi.Seek(0);
                    }
                        
                }

                if ((((n前回表示したフレーム番号 != frameNoFromTime) || !bフレームを作成した)) && ( fClipアスペクト比 < 1.77f ))
                {
                    n前回表示したフレーム番号 = frameNoFromTime;
                    bフレームを作成した = true;
                }
                    
                Size size = new Size( (int)framewidth, (int)frameheight );
                Size sz720pサイズ = new Size( 1280, 720);
                Size sz開始サイズ = new Size( n開始サイズW, n開始サイズH );
                Size sz終了サイズ = new Size( n終了サイズW, n終了サイズH );
                Point location = new Point( n画像側開始位置X, n画像側終了位置Y );
                Point point2 = new Point( n画像側終了位置X, n画像側終了位置Y );
                Point point3 = new Point( n表示側開始位置X, n表示側開始位置Y );
                Point point4 = new Point( n表示側終了位置X, n表示側終了位置Y );
                if( CSoundManager.rcPerformanceTimer.nCurrentTime < n移動開始時刻ms )
                {
                    n移動開始時刻ms = CSoundManager.rcPerformanceTimer.nCurrentTime;
                }
                if( n総移動時間ms == 0 )
                {
                    rectangle = new Rectangle( location, sz開始サイズ );
                    rectangle2 = new Rectangle( point3, sz開始サイズ );
                }
                else
                {
                    double db経過時間倍率 = ( (double)time ) / ( (double)n総移動時間ms );
                    Size size5 = new Size( sz開始サイズ.Width + ( (int)( ( sz終了サイズ.Width - sz開始サイズ.Width ) * db経過時間倍率 ) ), sz開始サイズ.Height + ( (int)( ( sz終了サイズ.Height - sz開始サイズ.Height ) * db経過時間倍率 ) ) );
                    rectangle = new Rectangle( (int)( (point2.X - location.X ) * db経過時間倍率 ), (int)( ( point2.Y - location.Y ) * db経過時間倍率 ), ( (int)( ( point2.X - location.X ) * db経過時間倍率 ) ) + size5.Width, ( (int)((point2.Y - location.Y) * db経過時間倍率 ) ) + size5.Height );
                    rectangle2 = new Rectangle( (int)( (point4.X - point3.X ) * db経過時間倍率 ), (int)( ( point4.Y - point3.Y ) * db経過時間倍率 ), ( (int)( (point4.X - point3.X ) * db経過時間倍率 ) ) + size5.Width, ( (int)( ( point4.Y - point3.Y ) * db経過時間倍率 ) ) + size5.Height );
                    if( rectangle.X < 0 )
                    {
                        int num6 = -rectangle.X;
                        rectangle2.X += num6;
                        rectangle2.Width -= num6;
                        rectangle.X = 0;
                        rectangle.Width -= num6;
                    }
                    if( rectangle.Y < 0 )
                    {
                        int num7 = -rectangle.Y;
                        rectangle2.Y += num7;
                        rectangle2.Height -= num7;
                        rectangle.Y = 0;
                        rectangle.Height -= num7;
                    }
                    if( rectangle.Right > size.Width )
                    {
                        int num8 = rectangle.Right - size.Width;
                        rectangle2.Width -= num8;
                        rectangle.Width -= num8;
                    }
                    if( rectangle.Bottom > size.Height )
                    {
                        int num9 = rectangle.Bottom - size.Height;
                        rectangle2.Height -= num9;
                        rectangle.Height -= num9;
                    }
                    if( rectangle2.X < 0 )
                    {
                        int num10 = -rectangle2.X;
                        rectangle.X += num10;
                        rectangle.Width -= num10;
                        rectangle2.X = 0;
                        rectangle2.Width -= num10;
                    }
                    if( rectangle2.Y < 0 )
                    {
                        int num11 = -rectangle2.Y;
                        rectangle.Y += num11;
                        rectangle.Height -= num11;
                        rectangle2.Y = 0;
                        rectangle2.Height -= num11;
                    }
                    if( rectangle2.Right > sz720pサイズ.Width )
                    {
                        int num12 = rectangle2.Right - sz720pサイズ.Width;
                        rectangle.Width -= num12;
                        rectangle2.Width -= num12;
                    }
                    if( rectangle2.Bottom > sz720pサイズ.Height )
                    {
                        int num13 = rectangle2.Bottom - sz720pサイズ.Height;
                        rectangle.Height -= num13;
                        rectangle2.Height -= num13;
                    }
                }
                                        
                if( bUseCAviDS  )
                {
                    if( ( tx描画用 != null ) && ( n総移動時間ms != -1 ) )
                    {
                        #region[ フレームの生成 ]
                        rAVI.avi.tGetBitmap( CDTXMania.app.Device, tx描画用, time );
                        #endregion

                        if( bFullScreen )
                        {
                            #region[ 動画の描画 ]
                            if( fClipアスペクト比 > 1.77f )
                            {
                                tx描画用.tDraw2DUpsideDown( CDTXMania.app.Device, position, 0 );
                                //this.tx描画用.tDraw2D(CDTXMania.app.Device, this.position, 0);
                            }
                            else
                            {
                                if( CDTXMania.ConfigIni.bDrumsEnabled )
                                {
                                    tx描画用.vcScaleRatio = vclip;
                                    //this.tx描画用.tDraw2D( CDTXMania.app.Device, 882, 0 );
                                    tx描画用.tDraw2DUpsideDown( CDTXMania.app.Device, 882, 0 );
                                }
                                else if( CDTXMania.ConfigIni.bGuitarEnabled )
                                {
                                    tx描画用.vcScaleRatio = new Vector3( 1f, 1f, 1f );
                                    PositionG = (int)( ( 1280f - (float)( framewidth ) ) / 2f);
                                    //this.tx描画用.tDraw2D( CDTXMania.app.Device, this.PositionG, 0 );
                                    tx描画用.tDraw2DUpsideDown( CDTXMania.app.Device, PositionG, 0);
                                }
                            }
                            #endregion
                        }
                    }
                }
                    
            }


            #endregion

            if (bIsDuringPerformance) 
            {
                if (CDTXMania.DTX != null && CDTXMania.DTX.listBMP.Count >= 1 && CDTXMania.ConfigIni.bBGAEnabled == true)
                {
                    if (CDTXMania.ConfigIni.bDrumsEnabled)
                        CDTXMania.stagePerfDrumsScreen.actBGA.tUpdateAndDraw(980, 0);
                    else
                        CDTXMania.stagePerfGuitarScreen.actBGA.tUpdateAndDraw(501, 0);
                }
            }

            if( CDTXMania.ConfigIni.DisplayBonusEffects == true )
            {
                for( int i = 0; i < 1; i++ )
                {
                    if (stFillIn[ i ].bInUse)
                    {
                        int numf = stFillIn[ i ].ctUpdate.nCurrentValue;
                        stFillIn[ i ].ctUpdate.tUpdate();
                        if( stFillIn[ i ].ctUpdate.bReachedEndValue )
                        {
                            stFillIn[ i ].ctUpdate.tStop();
                            stFillIn[ i ].bInUse = false;
                        }
                        //if ( this.txフィルインエフェクト != null )
                        CStagePerfDrumsScreen stageDrum = CDTXMania.stagePerfDrumsScreen;
                        //CStagePerfGuitarScreen stageGuitar = CDTXMania.stagePerfGuitarScreen;

                        //if( ( CDTXMania.ConfigIni.bDrumsEnabled ? stageDrum.txBonusEffect : stageGuitar.txBonusEffect ) != null )
                        {
                            //this.txフィルインエフェクト.vcScaleRatio.X = 2.0f;
                            //this.txフィルインエフェクト.vcScaleRatio.Y = 2.0f;
                            //this.txフィルインエフェクト.bAdditiveBlending = true;
                            //this.txフィルインエフェクト.tDraw2D(CDTXMania.app.Device, 0, -2, new Rectangle(0, 0 + (360 * numf), 640, 360));
                            if( CDTXMania.ConfigIni.bDrumsEnabled && stageDrum.txBonusEffect != null)
                            {
                                stageDrum.txBonusEffect.vcScaleRatio = new Vector3( 2.0f, 2.0f, 1.0f );
                                stageDrum.txBonusEffect.bAdditiveBlending = true;
                                stageDrum.txBonusEffect.tDraw2D( CDTXMania.app.Device, 0, -2, new Rectangle(0, 0 + ( 360 * numf ), 640, 360 )) ;
                                try
                                {
                                    //if( this.txフィルインエフェクト[ this.stFillIn[ i ].ctUpdate.nCurrentValue ] != null )
                                    //    this.txフィルインエフェクト[ this.stFillIn[ i ].ctUpdate.nCurrentValue ].tDraw2D( CDTXMania.app.Device, 0, 0 );
                                }
                                catch( Exception ex )
                                {
                                }
                            }
                        }

                    }
                }
            }

            if (CDTXMania.ConfigIni.bShowMusicInfo && bIsDuringPerformance)
                actPanel.tUpdateAndDraw();

            if( ( ( bWindowMode ) && tx描画用 != null && ( CDTXMania.ConfigIni.bAVIEnabled ) ) )
            {
                vector = tx描画用.vcScaleRatio;
                tx描画用.vcScaleRatio = smallvc;
                tx描画用.nTransparency = 0xff;

                if( CDTXMania.ConfigIni.bDrumsEnabled )
                {
                    if( CDTXMania.ConfigIni.bGraph有効.Drums )
                    {
                        #region[ スキルメーター有効 ]
                        n本体X = 2;
                        n本体Y = 402;

                        if( fClipアスペクト比 > 0.96f )
                        {
                            ratio2 = 260f / ( (float)framewidth );
                            position2 = 20 + n本体Y + (int)( (270f - ( frameheight * ratio2 ) ) / 2f );
                        }
                        else
                        {
                            ratio2 = 270f / ( (float)frameheight );
                            position2 = 5 + n本体X + (int)( ( 260 - ( framewidth * ratio2 ) ) / 2f );
                        }
                        if( txクリップパネル != null )
                            txクリップパネル.tDraw2D( CDTXMania.app.Device, n本体X, n本体Y );

                        smallvc = new Vector3( ratio2, ratio2, 1f );
                                                        
                        {
                            if( n総移動時間ms != -1 && rAVI != null )
                            {
                                if( fClipアスペクト比 < 0.96f )
                                    tx描画用.tDraw2DUpsideDown( CDTXMania.app.Device, position2, 20 + n本体Y );
                                else
                                    tx描画用.tDraw2DUpsideDown( CDTXMania.app.Device, 5 + n本体X, position2 );
                            }
                        }
                    }
                    #endregion
                    else
                    {
                        #region[ スキルメーター無効 ]
                        n本体X = 854;
                        n本体Y = 142;

                        if( fClipアスペクト比 > 1.77f )
                        {
                            ratio2 = 416f / ((float)framewidth);
                            position2 = 30 + n本体Y + (int)((234f - (frameheight * ratio2)) / 2f);
                        }
                        else
                        {
                            ratio2 = 234f / ((float)frameheight);
                            position2 = 5 + n本体X + (int)((416f - (framewidth * ratio2)) / 2f);
                        }
                        if( txクリップパネル != null )
                            txクリップパネル.tDraw2D( CDTXMania.app.Device, n本体X, n本体Y ); 
                        smallvc = new Vector3( ratio2, ratio2, 1f );
                        tx描画用.vcScaleRatio = smallvc;
                        {
                            if( n総移動時間ms != -1 && rAVI != null )
                            {
                                if( fClipアスペクト比 < 1.77f )
                                    tx描画用.tDraw2DUpsideDown( CDTXMania.app.Device, position2, 30 + n本体Y );
                                else
                                    tx描画用.tDraw2DUpsideDown( CDTXMania.app.Device, 5 + n本体X, position2 );
                            }
                        }
                        #endregion
                    }
                }
                else if( CDTXMania.ConfigIni.bGuitarEnabled )
                {
                    #region[ ギター時 ]
                    #region[ 本体位置 ]
                    n本体X = 380;
                    n本体Y = 50;
                    int nグラフX = 267;

                    if( CDTXMania.ConfigIni.bGraph有効.Bass && CDTXMania.DTX != null && !CDTXMania.DTX.bHasChips.Bass )
                        n本体X = n本体X + nグラフX;
                    if( CDTXMania.ConfigIni.bGraph有効.Guitar && CDTXMania.DTX != null && !CDTXMania.DTX.bHasChips.Guitar )
                        n本体X = n本体X - nグラフX;
                    #endregion

                    if( fClipアスペクト比 > 1.77f )
                    {
                        ratio2 = 460f / ( (float)framewidth );
                        position2 = 5 + n本体Y + (int)( ( 258f - ( frameheight * ratio2 ) ) / 2f );
                    }
                    else
                    {
                        ratio2 = 258f / ( (float)frameheight );
                        position2 = 30 + n本体X + (int)( ( 460f - ( framewidth * ratio2 ) ) / 2f );
                    }
                    if( txクリップパネル != null )
                        txクリップパネル.tDraw2D( CDTXMania.app.Device, n本体X, n本体Y );
                    smallvc = new Vector3( ratio2, ratio2, 1f );
                    tx描画用.vcScaleRatio = smallvc;
                        
                    {
                        if( rAVI != null )
                        {
                            if( fClipアスペクト比 < 1.77f )
                                tx描画用.tDraw2D( CDTXMania.app.Device, position2, 5 + n本体Y );
                            else
                                tx描画用.tDraw2D( CDTXMania.app.Device, 30 + n本体X, position2 );
                        }
                    }
                    #endregion
                }
                tx描画用.vcScaleRatio = vector;
            }
            IInputDevice keyboard = CDTXMania.InputManager.Keyboard;
            if( CDTXMania.Pad.bPressed( EInstrumentPart.BASS, EPad.Help ) )
            {
                if( b再生トグル == false )
                {                        
                    if( bUseCAviDS )
                    {
                        if(rAVI != null && rAVI.avi != null )
                        {
                            rAVI.avi.Pause();
                        }
                    }
                    b再生トグル = true;
                }
                else if( b再生トグル == true )
                {
                    if( bUseCAviDS )
                    {
                        if(rAVI != null && rAVI.avi != null )
                        {
                            rAVI.avi.Run();
                        }
                    }
                    b再生トグル = false;
                }
            }
        }

        return 0;
    }
    public void Start(bool bフィルイン)
    {
        for (int j = 0; j < 1; j++)
        {
            if (stFillIn[j].bInUse)
            {
                stFillIn[j].ctUpdate.tStop();
                stFillIn[j].bInUse = false;
            }
        }
        for (int i = 0; i < 1; i++)
        {
            for (int j = 0; j < 1; j++)
            {
                if (!stFillIn[j].bInUse)
                {
                    stFillIn[j].bInUse = true;
                    stFillIn[j].ctUpdate = new CCounter(0, 30, 30, CDTXMania.Timer);
                    break;
                }
            }
        }
    }
    public override int OnUpdateAndDraw()
    {
        throw new InvalidOperationException("tUpdateAndDraw(int,int)のほうを使用してください。");
    }


    // Other

    #region [ private ]
    //-----------------
//      public CActPerfBGA actBGA;
    //public CActPerfDrumsFillingEffect actFill;
    public CActPerfPanelString actPanel;
    public bool bIsDuringPerformance = true;

    private bool bFullScreen;
//      private Bitmap blanes;
    public bool bWindowMode;
    private bool bフレームを作成した;
    private bool b再生トグル;
    private bool bDShowクリップを再生している;
    //private bool bUseMRenderer = false;
    private bool bUseCAviDS = true;//
    public float fClipアスペクト比;
    private uint frameheight;
    private uint framewidth;
    private int i1;
    private int i2;
//      private Image ilanes;
    private int nAlpha;
    private int nCurrentMovieMode;
    private long n移動開始時刻ms;
    private int n画像側開始位置X;
    private int n画像側開始位置Y;
    private int n画像側終了位置X;
    private int n画像側終了位置Y;
    private int n開始サイズH;
    private int n開始サイズW;
    private int n終了サイズH;
    private int n終了サイズW;
    private int n前回表示したフレーム番号;
    private int n総移動時間ms;
    private int n表示側開始位置X;
    private int n表示側開始位置Y;
    private int n表示側終了位置X;
    private int n表示側終了位置Y;
    private int n本体X;
    private int n本体Y;
    private int PositionG;
    private long lDshowPosition;
    private long lStopPosition;
    public IntPtr pBmp;
    private int position;
    private int position2;
    //NOTE: This is a soft reference to externally initialized object
    //Do not call Dispose() for rAVI
    private CAVI rAVI;
    public bool bIsPreviewMovie { get; set; }
    public bool bHasBGA { get; set; }
    public bool bFullScreenMovie { get; set; }
    public bool bLoop { get; set; }
    //private CDirectShow ds汎用;

    //public CDTX.CDirectShow dsBGV;

    private CTexture txlanes;
    private CTexture txクリップパネル;
    //private CTexture txドラム;
    //private CTexture[] txフィルインエフェクト;
    private CTexture txフィルインエフェクト;
    private CTexture tx描画用;
    private CTexture tx描画用2;
    private CTexture txDShow汎用;

    private float ratio1;
    private float ratio2;
    private Rectangle rec;
    private Rectangle rec2;
    private Rectangle rec3;
    public Vector3 smallvc;
    private Vector3 vclip;
    public Vector3 vector;

    [StructLayout(LayoutKind.Sequential)]
    private struct STパッド状態
    {
        public int nY座標オフセットdot;
        public int nY座標加速度dot;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct STフィルイン
    {
        public bool bInUse;
        public CCounter ctUpdate;
    }
    private STパッド状態[] stパッド状態 = new STパッド状態[19];
    public STフィルイン[] stFillIn = new STフィルイン[2];
    //-----------------
    #endregion
}