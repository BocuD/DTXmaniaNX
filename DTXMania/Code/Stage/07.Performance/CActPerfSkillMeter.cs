using System.Runtime.InteropServices;
using System.Drawing;
using SharpDX;
using FDK;

using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using Color = System.Drawing.Color;

namespace DTXMania
{
    internal class CActPerfSkillMeter : CActivity
    {
        // グラフ仕様
        // _ギターとベースで同時にグラフを出すことはない。
        //
        // _目標のメーター画像
        //   →ゴーストがあった
        // 　　_ゴーストに基づいたグラフ(リアルタイム比較)
        // 　→なかった
        // 　　_ScoreIniの自己ベストのグラフ
        //

		private STDGBVALUE<int> nGraphBG_XPos = new STDGBVALUE<int>(); //ドラムにも座標指定があるためDGBVALUEとして扱う。
		private int nGraphBG_YPos = 200;
		private int DispHeight = 400;
		private int DispWidth = 60;
		private CCounter counterYposInImg = null;
		private readonly int slices = 10;
        private int nGraphUsePart = 0;
        private int[] nGraphGauge_XPos = new int[ 2 ];
        private int nPart = 0;

        public bool bIsTrainingMode = false;

		// プロパティ

        public double dbグラフ値現在_渡
        {
            get => dbグラフ値現在;
            set => dbグラフ値現在 = value;
        }
        public double dbGraphValue_Goal
        {
            get => dbグラフ値目標;
            set => dbグラフ値目標 = value;
        }
        public int[] n現在のAutoを含まない判定数_渡
        {
            get => n現在のAutoを含まない判定数;
            set => n現在のAutoを含まない判定数 = value;
        }
		
		// コンストラクタ

		public CActPerfSkillMeter()
		{
			bNotActivated = true;
		}


		// CActivity 実装

		public override void OnActivate()
        {
            dbグラフ値目標 = 0f;
            dbグラフ値現在 = 0f;

            n現在のAutoを含まない判定数 = new int[ 6 ];

			base.OnActivate();
		}
		public override void OnDeactivate()
		{
			base.OnDeactivate();
		}
		public override void OnManagedCreateResources()
		{
			if( !bNotActivated )
			{
                //this.pfNameFont = new CPrivateFastFont( new FontFamily( "Arial" ), 16, FontStyle.Bold );
                txグラフ = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\7_Graph_Main.png" ) );
                txグラフ_ゲージ = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\7_Graph_Gauge.png" ) );

                //if( this.pfNameFont != null )
                //{
                //    if( CDTXMania.ConfigIni.eTargetGhost.Drums == ETargetGhostData.PERFECT )
                //    {
                //        this.txPlayerName = this.t指定された文字テクスチャを生成する( "DJ AUTO" );
                //    }
                //    else if( CDTXMania.ConfigIni.eTargetGhost.Drums == ETargetGhostData.LAST_PLAY )
                //    {
                //        this.txPlayerName = this.t指定された文字テクスチャを生成する( "LAST PLAY" );
                //    }
                //}
				base.OnManagedCreateResources();
			}
		}
		public override void OnManagedReleaseResources()
		{
			if( !bNotActivated )
			{
				CDTXMania.tReleaseTexture( ref txグラフ );
                CDTXMania.tReleaseTexture( ref txグラフ_ゲージ );
                CDTXMania.tReleaseTexture( ref txグラフ値自己ベストライン );
				base.OnManagedReleaseResources();
			}
		}
		public override int OnUpdateAndDraw()
		{
			if( !bNotActivated )
			{
				if( bJustStartedUpdate )
				{
                    //座標などの定義は初回だけにする。
                    //2016.03.29 kairera0467 非セッション譜面で、譜面が無いパートでグラフを有効にしている場合、譜面があるパートに一時的にグラフを切り替える。
                    //                       時間がなくて雑なコードになったため、後日最適化を行う。
                    if( CDTXMania.ConfigIni.bDrumsEnabled )
                    {
                        nPart = 0;
                        nGraphUsePart = 0;
                    }
                    else if( CDTXMania.ConfigIni.bGuitarEnabled )
                    {
                        nGraphUsePart = ( CDTXMania.ConfigIni.bGraph有効.Guitar == true ) ? 1 : 2;
                        if( CDTXMania.DTX.bHasChips.Guitar )
                            nPart = CDTXMania.ConfigIni.bGraph有効.Guitar ? 0 : 1;
                        else if( !CDTXMania.DTX.bHasChips.Guitar && CDTXMania.ConfigIni.bGraph有効.Guitar )
                        {
                            nPart = 1;
                            nGraphUsePart = 2;
                        }

                        if( !CDTXMania.DTX.bHasChips.Bass && CDTXMania.ConfigIni.bGraph有効.Bass )
                            nPart = 0;
                    }

                    nGraphBG_XPos.Drums = (CDTXMania.ConfigIni.bSmallGraph ? 880 : 900);//850 : 870
                    nGraphBG_XPos.Guitar = 356;
                    nGraphBG_XPos.Bass = 647;
                    nGraphBG_YPos = nGraphUsePart == 0 ? 50 : 110;
                    //2016.06.24 kairera0467 StatusPanelとSkillMaterの場合はX座標を調整する。
                    if ( CDTXMania.ConfigIni.nInfoType == 1 )
                    {
                        nGraphBG_XPos.Guitar = 629 + 9;
                        nGraphBG_XPos.Bass = 403;
                    }

                    if( CDTXMania.ConfigIni.eTargetGhost[ nGraphUsePart ] != ETargetGhostData.NONE )
                    {
                        if( CDTXMania.listTargetGhostScoreData[ nGraphUsePart ] != null )
                        {
                            //this.dbグラフ値目標 = CDTXMania.listTargetGhostScoreData[ this.nGraphUsePart ].dbPerformanceSkill;
                            dbグラフ値目標_表示 = CDTXMania.listTargetGhostScoreData[ nGraphUsePart ].dbPerformanceSkill;
                        }
                    }

                    nGraphGauge_XPos = new int[] { 3, 205 };

					bJustStartedUpdate = false;
                }

				int stYposInImg = 0;
                int nGraphSizeOffset = (CDTXMania.ConfigIni.bSmallGraph ? -19 : 0);


                if ( txグラフ != null )
                {
                    //背景
					txグラフ.vcScaleRatio = new Vector3( 1f, 1f, 1f );
                    if (CDTXMania.ConfigIni.bSmallGraph)
                    {
                        txグラフ.tDraw2D(CDTXMania.app.Device, nGraphBG_XPos[nGraphUsePart], nGraphBG_YPos, new Rectangle(448, 2, 111, 584));
                    }
                    else
                    {
                        txグラフ.tDraw2D(CDTXMania.app.Device, nGraphBG_XPos[nGraphUsePart], nGraphBG_YPos, new Rectangle(2, 2, 251, 584));
                    }

                    //自己ベスト数値表示
                    if (CDTXMania.ConfigIni.bSmallGraph)
                    {
                        t達成率文字表示( nGraphBG_XPos[ nGraphUsePart ] - 3, nGraphBG_YPos + 531, string.Format( "{0,6:##0.00}" + "%", dbGraphValue_PersonalBest ) );
                    }
                    else
                    {
                        t達成率文字表示(nGraphBG_XPos[nGraphUsePart] + 136, nGraphBG_YPos + 501, string.Format("{0,6:##0.00}" + "%", dbGraphValue_PersonalBest));
                    }
                }

                //ゲージ現在
                if (txグラフ_ゲージ != null)
                {
                    //ゲージ本体
                    int nGaugeSize = (int)(434.0f * (float)dbグラフ値現在 / 100.0f);
                    int nPosY = nGraphUsePart == 0 ? 527 - nGaugeSize : 587 - nGaugeSize;
                    if (!bIsTrainingMode)
                    {
                        txグラフ_ゲージ.nTransparency = 255;
                        txグラフ_ゲージ.tDraw2D(CDTXMania.app.Device, nGraphBG_XPos[nGraphUsePart] + 45 + nGraphSizeOffset, nPosY, new Rectangle(2, 2, 30, nGaugeSize));
                    }
                    //ゲージ比較
                    int nTargetGaugeSize = (int)( 434.0f * ( (float)dbグラフ値目標 / 100.0f ) );
                    int nTargetGaugePosY = nGraphUsePart == 0 ? 527 - nTargetGaugeSize : 587 - nTargetGaugeSize;
                    int nTargetGaugeRectX = dbグラフ値現在 > dbグラフ値目標 ? 38 : 74;
                    txグラフ_ゲージ.nTransparency = 255;
                    txグラフ_ゲージ.tDraw2D( CDTXMania.app.Device, nGraphBG_XPos[ nGraphUsePart ] + 75 + nGraphSizeOffset, nTargetGaugePosY, new Rectangle( nTargetGaugeRectX, 2, 30, nTargetGaugeSize ) );
                    if( txグラフ != null )
                    {
                        //ターゲット達成率数値

                        //ターゲット名
                        //現在
                        txグラフ.tDraw2D( CDTXMania.app.Device, nGraphBG_XPos[ nGraphUsePart ] + 45 + nGraphSizeOffset, nGraphBG_YPos + 357, new Rectangle( 260, 2, 30, 120 ) );
                        //比較対象
                        txグラフ.tDraw2D( CDTXMania.app.Device, nGraphBG_XPos[ nGraphUsePart ] + 75 + nGraphSizeOffset, nGraphBG_YPos + 357, new Rectangle( 260 + ( 30 * ( (int)CDTXMania.ConfigIni.eTargetGhost[ nGraphUsePart ] ) ), 2, 30, 120 ) );

                        //以下使用予定
                        if (!CDTXMania.ConfigIni.bSmallGraph)
                        {
                            //最終プレイ
                            txグラフ.tDraw2D(CDTXMania.app.Device, nGraphBG_XPos[nGraphUsePart] + 106, nGraphBG_YPos + 357, new Rectangle(260 + 60, 2, 30, 120));
                            //自己ベスト
                            txグラフ.tDraw2D(CDTXMania.app.Device, nGraphBG_XPos[nGraphUsePart] + 136, nGraphBG_YPos + 357, new Rectangle(260 + 90, 2, 30, 120));
                            //最高スコア
                            txグラフ.tDraw2D(CDTXMania.app.Device, nGraphBG_XPos[nGraphUsePart] + 164, nGraphBG_YPos + 357, new Rectangle(260 + 120, 2, 30, 120));
                        }
                    }
                    t比較文字表示( nGraphBG_XPos[ nGraphUsePart ] + 44 + nGraphSizeOffset, nPosY - 10, string.Format( "{0,5:##0.00}", Math.Abs( dbグラフ値現在 ) ) );
                    t比較文字表示( nGraphBG_XPos[ nGraphUsePart ] + 74 + nGraphSizeOffset, nTargetGaugePosY - 10, string.Format( "{0,5:##0.00}", Math.Abs( dbグラフ値目標 ) ) );
                }


			}
			return 0;
		}


		// Other

		#region [ private ]
		//----------------
        private double dbグラフ値目標;
        private double dbグラフ値目標_表示;
        private double dbグラフ値現在;
        private double dbグラフ値現在_表示;
        public double dbGraphValue_PersonalBest;
        private int[] n現在のAutoを含まない判定数;

        private CTexture txPlayerName;
		private CTexture txグラフ;
        private CTexture txグラフ_ゲージ;
        private CTexture txグラフ値自己ベストライン;

        private CPrivateFastFont pfNameFont;

        [StructLayout(LayoutKind.Sequential)]
        private struct ST文字位置
        {
            public char ch;
            public Point pt;
            public ST文字位置( char ch, Point pt )
            {
                this.ch = ch;
                this.pt = pt;
            }
        }

        private ST文字位置[] st比較数字位置 = new ST文字位置[]{
            new ST文字位置( '0', new Point( 0, 0 ) ),
            new ST文字位置( '1', new Point( 8, 0 ) ),
            new ST文字位置( '2', new Point( 16, 0 ) ),
            new ST文字位置( '3', new Point( 24, 0 ) ),
            new ST文字位置( '4', new Point( 32, 0 ) ),
            new ST文字位置( '5', new Point( 40, 0 ) ),
            new ST文字位置( '6', new Point( 48, 0 ) ),
            new ST文字位置( '7', new Point( 56, 0 ) ),
            new ST文字位置( '8', new Point( 64, 0 ) ),
            new ST文字位置( '9', new Point( 72, 0 ) ),
            new ST文字位置( '.', new Point( 80, 0 ) )
        };
        private ST文字位置[] st達成率数字位置 = new ST文字位置[]{
            new ST文字位置( '0', new Point( 0, 0 ) ),
            new ST文字位置( '1', new Point( 16, 0 ) ),
            new ST文字位置( '2', new Point( 32, 0 ) ),
            new ST文字位置( '3', new Point( 48, 0 ) ),
            new ST文字位置( '4', new Point( 64, 0 ) ),
            new ST文字位置( '5', new Point( 80, 0 ) ),
            new ST文字位置( '6', new Point( 96, 0 ) ),
            new ST文字位置( '7', new Point( 112, 0 ) ),
            new ST文字位置( '8', new Point( 128, 0 ) ),
            new ST文字位置( '9', new Point( 144, 0 ) ),
            new ST文字位置( '.', new Point( 160, 0 ) ),
            new ST文字位置( '%', new Point( 168, 0 ) ),
        };


		private void t比較文字表示( int x, int y, string str )
		{
			foreach( char ch in str )
			{
				for( int i = 0; i < st比較数字位置.Length; i++ )
				{
					if( st比較数字位置[ i ].ch == ch )
					{
                        int RectX = 8;
                        if( ch == '.' ) RectX = 2;
						Rectangle rectangle = new Rectangle( 260 + st比較数字位置[ i ].pt.X, 162, RectX, 10 );
						if( txグラフ != null )
						{
                            txグラフ.nTransparency = 255;
							txグラフ.tDraw2D( CDTXMania.app.Device, x, y, rectangle );
						}
						break;
					}
				}
                if( ch == '.' ) x += 2;
                else x += 7;
			}
		}
		private void t達成率文字表示( int x, int y, string str )
		{
			foreach( char ch in str )
			{
				for( int i = 0; i < st達成率数字位置.Length; i++ )
				{
					if( st達成率数字位置[ i ].ch == ch )
					{
                        int RectX = 16;
                        if( ch == '.' ) RectX = 8;
						Rectangle rectangle = new Rectangle( 260 + st達成率数字位置[ i ].pt.X, 128, RectX, 28 );
						if( txグラフ != null )
						{
                            txグラフ.nTransparency = 255;
							txグラフ.tDraw2D( CDTXMania.app.Device, x, y, rectangle );
						}
						break;
					}
				}
                if( ch == '.' ) x += 8;
				else x += 16;
			}
		}
        private CTexture t指定された文字テクスチャを生成する( string str文字 )
        {
            Bitmap bmp;
            bmp = pfNameFont.DrawPrivateFont( str文字, Color.White, Color.Transparent );

            CTexture tx文字テクスチャ = CDTXMania.tGenerateTexture( bmp, false );

            if( tx文字テクスチャ != null )
                tx文字テクスチャ.vcScaleRatio = new Vector3( 1.0f, 1.0f, 1f );

            bmp.Dispose();

            return tx文字テクスチャ;
        }
        private void t折れ線を描画する( int nBoardPosA, int nBoardPosB )
        {
            //やる気がまるでない線
            //2016.03.28 kairera0467 ギター画面では1Pと2Pで向きが変わるが、そこは残念ながら未対応。
            //参考 http://dobon.net/vb/dotnet/graphics/drawline.html
            if( txグラフ値自己ベストライン == null )
            {
                Bitmap canvas = new Bitmap( 280, 720 );

                Graphics g = Graphics.FromImage( canvas );
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                int nMybestGaugeSize = (int)( 560.0f * (float)dbGraphValue_PersonalBest / 100.0f );
                int nMybestGaugePosY = 600 - nMybestGaugeSize;

                int nTargetGaugeSize = (int)( 560.0f * (float)dbグラフ値目標_表示 / 100.0f );
                int nTargetGaugePosY = 600 - nTargetGaugeSize;

                Point[] posMybest = {
                    new Point( 3, nMybestGaugePosY ),
                    new Point( 75, nMybestGaugePosY ),
                    new Point( 94, nBoardPosA + 31 ),
                    new Point( 102, nBoardPosA + 31 )
                };

                Point[] posTarget = {
                    new Point( 3, nTargetGaugePosY ),
                    new Point( 75, nTargetGaugePosY ),
                    new Point( 94, nBoardPosB + 59 ),
                    new Point( 102, nBoardPosB + 59 )
                };

                if( nGraphUsePart == 2 )
                {
                    posMybest = new Point[]{
                        new Point( 271, nMybestGaugePosY ),
                        new Point( 206, nMybestGaugePosY ),
                        new Point( 187, nBoardPosA + 31 ),
                        new Point( 178, nBoardPosA + 31 )
                    };

                    posTarget = new Point[]{
                        new Point( 271, nTargetGaugePosY ),
                        new Point( 206, nTargetGaugePosY ),
                        new Point( 187, nBoardPosB + 59 ),
                        new Point( 178, nBoardPosB + 59 )
                    };
                }

                Pen penMybest = new Pen( Color.Pink, 2 );
                g.DrawLines( penMybest, posMybest );

                if( CDTXMania.listTargetGhsotLag[ nGraphUsePart ] != null && CDTXMania.listTargetGhostScoreData[ nGraphUsePart ] != null )
                {
                    Pen penTarget = new Pen( Color.Orange, 2 );
                    g.DrawLines( penTarget, posTarget );
                }

                g.Dispose();

                txグラフ値自己ベストライン = new CTexture( CDTXMania.app.Device, canvas, CDTXMania.TextureFormat, false );
            }
            if( txグラフ値自己ベストライン != null )
                txグラフ値自己ベストライン.tDraw2D( CDTXMania.app.Device, nGraphBG_XPos[ nGraphUsePart ], nGraphBG_YPos );
        }
		//-----------------
		#endregion
	}
}
