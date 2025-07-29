using System.Runtime.InteropServices;
using DTXMania.Core;
using SharpDX;
using FDK;

using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

namespace DTXMania;

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

	private STDGBVALUE<int> nGraphBG_XPos; //ドラムにも座標指定があるためDGBVALUEとして扱う。
	private int nGraphBG_YPos = 200;
	private int DispHeight = 400;
	private int DispWidth = 60;
	private CCounter counterYposInImg = null;
	private readonly int slices = 10;
	private int nGraphUsePart;

	public bool bIsTrainingMode = false;

	// プロパティ

	public double dbグラフ値現在_渡 { get; set; }

	public double dbGraphValue_Goal { get; set; }

	public int[] n現在のAutoを含まない判定数_渡 { get; set; }

	// コンストラクタ

	public CActPerfSkillMeter()
	{
		bActivated = false;
	}


	// CActivity 実装

	public override void OnActivate()
	{
		dbGraphValue_Goal = 0f;
		dbグラフ値現在_渡 = 0f;

		n現在のAutoを含まない判定数_渡 = new int[ 6 ];

		base.OnActivate();
	}

	public override void OnManagedCreateResources()
	{
		if( bActivated )
		{
			txグラフ = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\7_Graph_Main.png" ) );
			txグラフ_ゲージ = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\7_Graph_Gauge.png" ) );
			
			base.OnManagedCreateResources();
		}
	}
	public override void OnManagedReleaseResources()
	{
		if( bActivated )
		{
			CDTXMania.tReleaseTexture( ref txグラフ );
			CDTXMania.tReleaseTexture( ref txグラフ_ゲージ );
			CDTXMania.tReleaseTexture( ref txグラフ値自己ベストライン );
			base.OnManagedReleaseResources();
		}
	}
	public override int OnUpdateAndDraw()
	{
		if( bActivated )
		{
			if( bJustStartedUpdate )
			{
				//座標などの定義は初回だけにする。
				//2016.03.29 kairera0467 非セッション譜面で、譜面が無いパートでグラフを有効にしている場合、譜面があるパートに一時的にグラフを切り替える。
				//                       時間がなくて雑なコードになったため、後日最適化を行う。
				if( CDTXMania.ConfigIni.bDrumsEnabled )
				{
					nGraphUsePart = 0;
				}
				else if( CDTXMania.ConfigIni.bGuitarEnabled )
				{
					nGraphUsePart = ( CDTXMania.ConfigIni.bGraph有効.Guitar == true ) ? 1 : 2;
					if( CDTXMania.DTX.bHasChips.Guitar )
					{
					}
					else if( !CDTXMania.DTX.bHasChips.Guitar && CDTXMania.ConfigIni.bGraph有効.Guitar )
					{
						nGraphUsePart = 2;
					}

					if( !CDTXMania.DTX.bHasChips.Bass && CDTXMania.ConfigIni.bGraph有効.Bass )
					{
					}
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
					txグラフ.tDraw2D(CDTXMania.app.Device, nGraphBG_XPos[nGraphUsePart], nGraphBG_YPos, new RectangleF(448, 2, 111, 584));
				}
				else
				{
					txグラフ.tDraw2D(CDTXMania.app.Device, nGraphBG_XPos[nGraphUsePart], nGraphBG_YPos, new RectangleF(2, 2, 251, 584));
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
				int nGaugeSize = (int)(434.0f * (float)dbグラフ値現在_渡 / 100.0f);
				int nPosY = nGraphUsePart == 0 ? 527 - nGaugeSize : 587 - nGaugeSize;
				if (!bIsTrainingMode)
				{
					txグラフ_ゲージ.nTransparency = 255;
					txグラフ_ゲージ.tDraw2D(CDTXMania.app.Device, nGraphBG_XPos[nGraphUsePart] + 45 + nGraphSizeOffset, nPosY, new RectangleF(2, 2, 30, nGaugeSize));
				}
				//ゲージ比較
				int nTargetGaugeSize = (int)( 434.0f * ( (float)dbGraphValue_Goal / 100.0f ) );
				int nTargetGaugePosY = nGraphUsePart == 0 ? 527 - nTargetGaugeSize : 587 - nTargetGaugeSize;
				int nTargetGaugeRectX = dbグラフ値現在_渡 > dbGraphValue_Goal ? 38 : 74;
				txグラフ_ゲージ.nTransparency = 255;
				txグラフ_ゲージ.tDraw2D( CDTXMania.app.Device, nGraphBG_XPos[ nGraphUsePart ] + 75 + nGraphSizeOffset, nTargetGaugePosY, new RectangleF( nTargetGaugeRectX, 2, 30, nTargetGaugeSize ) );
				if( txグラフ != null )
				{
					//ターゲット達成率数値

					//ターゲット名
					//現在
					txグラフ.tDraw2D( CDTXMania.app.Device, nGraphBG_XPos[ nGraphUsePart ] + 45 + nGraphSizeOffset, nGraphBG_YPos + 357, new RectangleF( 260, 2, 30, 120 ) );
					//比較対象
					txグラフ.tDraw2D( CDTXMania.app.Device, nGraphBG_XPos[ nGraphUsePart ] + 75 + nGraphSizeOffset, nGraphBG_YPos + 357, new RectangleF( 260 + ( 30 * ( (int)CDTXMania.ConfigIni.eTargetGhost[ nGraphUsePart ] ) ), 2, 30, 120 ) );

					//以下使用予定
					if (!CDTXMania.ConfigIni.bSmallGraph)
					{
						//最終プレイ
						txグラフ.tDraw2D(CDTXMania.app.Device, nGraphBG_XPos[nGraphUsePart] + 106, nGraphBG_YPos + 357, new RectangleF(260 + 60, 2, 30, 120));
						//自己ベスト
						txグラフ.tDraw2D(CDTXMania.app.Device, nGraphBG_XPos[nGraphUsePart] + 136, nGraphBG_YPos + 357, new RectangleF(260 + 90, 2, 30, 120));
						//最高スコア
						txグラフ.tDraw2D(CDTXMania.app.Device, nGraphBG_XPos[nGraphUsePart] + 164, nGraphBG_YPos + 357, new RectangleF(260 + 120, 2, 30, 120));
					}
				}
				t比較文字表示( nGraphBG_XPos[ nGraphUsePart ] + 44 + nGraphSizeOffset, nPosY - 10, string.Format( "{0,5:##0.00}", Math.Abs( dbグラフ値現在_渡 ) ) );
				t比較文字表示( nGraphBG_XPos[ nGraphUsePart ] + 74 + nGraphSizeOffset, nTargetGaugePosY - 10, string.Format( "{0,5:##0.00}", Math.Abs( dbGraphValue_Goal ) ) );
			}


		}
		return 0;
	}


	// Other

	#region [ private ]
	//----------------
	private double dbグラフ値目標_表示;
	private double dbグラフ値現在_表示;
	public double dbGraphValue_PersonalBest;

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

	private ST文字位置[] st比較数字位置 =
	[
		new( '0', new Point( 0, 0 ) ),
		new( '1', new Point( 8, 0 ) ),
		new( '2', new Point( 16, 0 ) ),
		new( '3', new Point( 24, 0 ) ),
		new( '4', new Point( 32, 0 ) ),
		new( '5', new Point( 40, 0 ) ),
		new( '6', new Point( 48, 0 ) ),
		new( '7', new Point( 56, 0 ) ),
		new( '8', new Point( 64, 0 ) ),
		new( '9', new Point( 72, 0 ) ),
		new( '.', new Point( 80, 0 ) )
	];
	private ST文字位置[] st達成率数字位置 =
	[
		new( '0', new Point( 0, 0 ) ),
		new( '1', new Point( 16, 0 ) ),
		new( '2', new Point( 32, 0 ) ),
		new( '3', new Point( 48, 0 ) ),
		new( '4', new Point( 64, 0 ) ),
		new( '5', new Point( 80, 0 ) ),
		new( '6', new Point( 96, 0 ) ),
		new( '7', new Point( 112, 0 ) ),
		new( '8', new Point( 128, 0 ) ),
		new( '9', new Point( 144, 0 ) ),
		new( '.', new Point( 160, 0 ) ),
		new( '%', new Point( 168, 0 ) )
	];


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
					RectangleF rectangle = new( 260 + st比較数字位置[ i ].pt.X, 162, RectX, 10 );
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
					RectangleF rectangle = new( 260 + st達成率数字位置[ i ].pt.X, 128, RectX, 28 );
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

	//-----------------
	#endregion
}