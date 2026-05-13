using System.Drawing;
using System.Runtime.InteropServices;
using DTXMania.Core;
using DTXMania.UI.Drawable;
using FDK;

namespace DTXMania;

internal abstract class CActPerfCommonJudgementString : CActivity
{
	// プロパティ
	protected STSTATUS[] stStatus = new STSTATUS[ 15 ];
	
	[StructLayout( LayoutKind.Sequential )]
	protected struct STSTATUS
	{
		public CCounter ct進行;
		public EJudgement judge;
		public float fZ軸回転度_棒;
		public float fX方向拡大率_棒;
		public float fY方向拡大率_棒;
		public int n相対X座標_棒;
		public int n相対Y座標_棒;

		public float fZ軸回転度;
		public float fX方向拡大率;
		public float fY方向拡大率;
		public int n相対X座標;
		public int n相対Y座標;

		public float fX方向拡大率B;
		public float fY方向拡大率B;
		public int n相対X座標B;
		public int n相対Y座標B;
		public int n透明度B;

		public int n透明度;
		public int nLag; // #25370 2011.2.1 yyagi
		public int nRect;
	}

	protected readonly ST判定文字列[] st判定文字列;
	[StructLayout( LayoutKind.Sequential )]
	protected struct ST判定文字列
	{
		public int n画像番号;
		public RectangleF rc;
	}

	protected readonly STlag数値[] stLag数値; // #25370 2011.2.1 yyagi
	[StructLayout( LayoutKind.Sequential )]
	protected struct STlag数値
	{
		public RectangleF rc;
	}

	protected BaseTexture? txlag数値; // #25370 2011.2.1 yyagi

	public int nShowLagType // #25370 2011.6.3 yyagi
	{
		get;
		set;
	}

	[StructLayout(LayoutKind.Sequential)]
	protected struct STLaneSize
	{
		public int x;
		public int w;
	}

	protected STLaneSize[] stLaneSize = [];

	protected abstract int LaneCount { get; }
	protected abstract void InitializeLaneSizes();
	protected abstract bool TryGetLanePosition( int lane, out int x, out int y );
	protected abstract BaseTexture GetJudgeTexture();
	// コンストラクタ

	public CActPerfCommonJudgementString()
	{
		st判定文字列 = new ST判定文字列[ 7 ];
		RectangleF[] r =
		[
			new( 0, 0,    0x80, 0x2a ), // Perfect
			new( 0, 0x2b, 0x80, 0x2a ), // Great
			new( 0, 0x56, 0x80, 0x2a ), // Good
			new( 0, 0,    0x80, 0x2a ), // Poor
			new( 0, 0x2b, 0x80, 0x2a ), // Miss
			new( 0, 0x56, 0x80, 0x2a ), // Bad
			new( 0, 0,    0x80, 0x2a )  // Auto
		];

		for ( int i = 0; i < 7; i++ )
		{
			st判定文字列[ i ] = new ST判定文字列();
			st判定文字列[ i ].n画像番号 = i / 3;
			st判定文字列[ i ].rc = r[ i ];
		}

		stLag数値 = new STlag数値[ 12 * 2 ]; // #25370 2011.2.1 yyagi
		bActivated = false;
	}

	// メソッド

	public virtual void Start( int nLane, EJudgement judge, int lag )
	{
		if ( ( nLane < 0 ) || ( nLane > 14 ) )
		{
			throw new IndexOutOfRangeException( "有効範囲は 0～14 です。" );
		}
		if ( ( ( nLane >= 10 ) || ( CDTXMania.ConfigIni.JudgementStringPosition.Drums != EType.C ) ) && ( ( ( nLane != 13 ) || ( CDTXMania.ConfigIni.JudgementStringPosition.Guitar != EType.D ) ) && ( ( nLane != 14 ) || ( CDTXMania.ConfigIni.JudgementStringPosition.Bass != EType.D ) ) ) )
		{
			stStatus[ nLane ].ct進行 = new CCounter( 0, CDTXMania.ConfigIni.nJudgeFrames - 1, CDTXMania.ConfigIni.nJudgeInterval, CDTXMania.Timer );

			stStatus[ nLane ].judge = judge;
			stStatus[ nLane ].fX方向拡大率 = 1f;
			stStatus[ nLane ].fY方向拡大率 = 1f;
			stStatus[ nLane ].fZ軸回転度 = 0f;
			stStatus[ nLane ].n相対X座標 = 0;
			stStatus[ nLane ].n相対Y座標 = 0;
			stStatus[ nLane ].n透明度 = 0xff;

			stStatus[ nLane ].fX方向拡大率B = 1f;
			stStatus[ nLane ].fY方向拡大率B = 1f;
			stStatus[ nLane ].n相対X座標B = 0;
			stStatus[ nLane ].n相対Y座標B = 0;
			stStatus[ nLane ].n透明度B = 0xff;

			stStatus[ nLane ].fZ軸回転度_棒 = 0f;
			stStatus[ nLane ].fX方向拡大率_棒 = 0;
			stStatus[ nLane ].fY方向拡大率_棒 = 0;
			stStatus[ nLane ].n相対X座標_棒 = 0;
			stStatus[ nLane ].n相対Y座標_棒 = 0;

			stStatus[ nLane ].nLag = lag;
		}
	}

	public override int OnUpdateAndDraw()
	{
		if ( !bActivated || !ShouldDrawJudgementString() )
		{
			return 0;
		}

		for ( int i = 0; i < LaneCount; i++ )
		{
			if ( !stStatus[ i ].ct進行.bStopped )
			{
				stStatus[ i ].ct進行.tUpdate();
				if ( stStatus[ i ].ct進行.bReachedEndValue )
				{
					stStatus[ i ].ct進行.tStop();
				}
				stStatus[ i ].nRect = stStatus[ i ].ct進行.nCurrentValue;
			}
		}

		for ( int lane = 0; lane < LaneCount; lane++ )
		{
			if ( stStatus[ lane ].ct進行.bStopped )
			{
				continue;
			}

			if ( !TryGetLanePosition( lane, out int baseX, out int baseY ) )
			{
				continue;
			}

			DrawJudgementString( lane, baseX, baseY );
		}

		return 0;
	}

	// CActivity 実装

	public override void OnActivate()
	{
		for ( int i = 0; i < 15; i++ )
		{
			stStatus[ i ].ct進行 = new CCounter();
		}

		InitializeLaneSizes();

		for ( int i = 0; i < 12; i++ )
		{
			if ( CDTXMania.ConfigIni.nShowLagTypeColor == 0 )
			{
				stLag数値[ i ].rc = new RectangleF( ( i % 4 ) * 15f, ( i / 4f ) * 19f, 15f, 19f ); // plus numbers
				stLag数値[ i + 12 ].rc = new RectangleF( ( i % 4 ) * 15f + 64f, ( i / 4f ) * 19f + 64f, 15f, 19f ); // minus numbers
			}
			else
			{
				stLag数値[ i ].rc = new RectangleF( ( i % 4 ) * 15f + 64f, ( i / 4f ) * 19f + 64f, 15f, 19f ); // minus numbers
				stLag数値[ i + 12 ].rc = new RectangleF( ( i % 4 ) * 15f, ( i / 4f ) * 19f, 15f, 19f ); // plus numbers
			}
		}
		base.OnActivate();
		nShowLagType = CDTXMania.ConfigIni.nShowLagType;
	}
	public override void OnDeactivate()
	{
		for ( int i = 0; i < 15; i++ )
		{
			stStatus[ i ].ct進行 = null!;
		}
		base.OnDeactivate();
	}
	public override void OnManagedCreateResources()
	{
		if ( bActivated )
		{
			txlag数値 = BaseTexture.LoadFromPath( CSkin.Path( @"Graphics\7_lag numbers.png" ) );
			base.OnManagedCreateResources();
		}
	}

	protected virtual bool ShouldDrawJudgementString()
	{
		return true;
	}

	protected void DrawJudgementString( int lane, int baseX, int baseY )
	{
		int nRectX = CDTXMania.ConfigIni.nJudgeWidgh;
		int nRectY = CDTXMania.ConfigIni.nJudgeHeight;

		int xc = ( baseX + stStatus[ lane ].n相対X座標 ) + ( stLaneSize[ lane ].w / 2 );
		float x = xc - ( 110f * stStatus[ lane ].fX方向拡大率 ) - ( ( nRectX - 225 ) / 2f );
		float y = baseY + stStatus[ lane ].n相対Y座標 - ( 140f * stStatus[ lane ].fY方向拡大率 / 2f ) - ( ( nRectY - 135 ) / 2f );

		DrawJudgeFrame( lane, x, y, nRectX, nRectY );
		DrawLag( lane, xc, y );
	}

	protected void DrawJudgeFrame( int lane, float x, float y, int nRectX, int nRectY )
	{
		if ( CDTXMania.ConfigIni.nJudgeFrames <= 1 )
		{
			return;
		}

		BaseTexture txJudge = GetJudgeTexture();

		float frameY = stStatus[ lane ].nRect * nRectY;
		switch ( stStatus[ lane ].judge )
		{
			case EJudgement.Perfect:
				txJudge.tDraw2D( x, y, new RectangleF( 0, frameY, nRectX, nRectY ) );
				break;
			case EJudgement.Great:
				txJudge.tDraw2D( x, y, new RectangleF( nRectX * 1, frameY, nRectX, nRectY ) );
				break;
			case EJudgement.Good:
				txJudge.tDraw2D( x, y, new RectangleF( nRectX * 2, frameY, nRectX, nRectY ) );
				break;
			case EJudgement.Poor:
				txJudge.tDraw2D( x, y, new RectangleF( nRectX * 3, frameY, nRectX, nRectY ) );
				break;
			case EJudgement.Miss:
				txJudge.tDraw2D( x, y, new RectangleF( nRectX * 4, frameY, nRectX, nRectY ) );
				break;
			case EJudgement.Auto:
				txJudge.tDraw2D( x, y, new RectangleF( nRectX * 5, frameY, nRectX, nRectY ) );
				break;
		}
	}

	protected void DrawLag( int lane, int xc, float y )
	{
		if ( nShowLagType != (int) EShowLagType.ON &&
			!( ( nShowLagType == (int) EShowLagType.GREAT_POOR ) && ( stStatus[ lane ].judge != EJudgement.Perfect ) ) )
		{
			return;
		}

		if ( stStatus[ lane ].judge == EJudgement.Auto || txlag数値 == null )
		{
			return;
		}

		bool minus = stStatus[ lane ].nLag < 0;
		int offsetX = 0;
		string strDispLag = stStatus[ lane ].nLag.ToString();
		float x = xc - strDispLag.Length * 15 / 2f;
		for ( int i = 0; i < strDispLag.Length; i++ )
		{
			int p = ( strDispLag[ i ] == '-' ) ? 11 : strDispLag[ i ] - '0';
			p += minus ? 0 : 12;
			txlag数値.tDraw2D( x + offsetX, y + 34, stLag数値[ p ].rc );
			offsetX += 15;
		}
	}
}

