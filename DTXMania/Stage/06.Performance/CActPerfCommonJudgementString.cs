using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using DTXMania.Core;
using DTXMania.UI.Drawable;
using FDK;

namespace DTXMania;

internal abstract class CActPerfCommonJudgementString : CActivity
{

	private BaseTexture judgementGraphicSheet;
	
	// プロパティ
	protected JudgementStatus[] judgementStatus = new JudgementStatus[15];
	
	[StructLayout( LayoutKind.Sequential )]
	protected struct JudgementStatus
	{
		public CCounter ctFrameProgress;
		public EJudgement judge;
		public float fZRotationDegrees_Bar;
		public float fXScaleRatio_Bar;
		public float fYScaleRatio_Bar;
		public int nRelativeX_Bar;
		public int nRelativeY_Bar;

		public float fZRotationDegrees;
		public float fXScaleRatio;
		public float fYScaleRatio;
		public int nRelativeX;
		public int nRelativeY;

		public float fXScaleRatioB;
		public float fYScaleRatioB;
		public int nRelativeXB;
		public int nRelativeYB;
		public int nOpacityB;

		public int nOpacity;
		public int nLag; // #25370 2011.2.1 yyagi
		public int nRect;
	}

	protected readonly JudgementStringInfo[] judgementStringInfoArray;
	[StructLayout( LayoutKind.Sequential )]
	protected struct JudgementStringInfo
	{
		public int imageNumber;
		public RectangleF rc;
	}

	protected readonly LagNumberInfo[] lagNumberInfoArray; // #25370 2011.2.1 yyagi
	[StructLayout( LayoutKind.Sequential )]
	protected struct LagNumberInfo
	{
		public RectangleF rc;
	}

	protected BaseTexture? txLagNumbers; // #25370 2011.2.1 yyagi

	public int lagDisplayMode // #25370 2011.6.3 yyagi
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

	public CActPerfCommonJudgementString()
	{
		judgementStringInfoArray = new JudgementStringInfo[7];
		RectangleF[] r =
		[
			new(0, 0, 0x80, 0x2a), // Perfect
			new(0, 0x2b, 0x80, 0x2a), // Great
			new(0, 0x56, 0x80, 0x2a), // Good
			new(0, 0, 0x80, 0x2a), // Poor
			new(0, 0x2b, 0x80, 0x2a), // Miss
			new(0, 0x56, 0x80, 0x2a), // Bad
			new(0, 0, 0x80, 0x2a) // Auto
		];

		for (int i = 0; i < 7; i++)
		{
			judgementStringInfoArray[i] = new JudgementStringInfo();
			judgementStringInfoArray[i].imageNumber = i / 3;
			judgementStringInfoArray[i].rc = r[i];
		}

		lagNumberInfoArray = new LagNumberInfo[12 * 2]; // #25370 2011.2.1 yyagi
		bActivated = false;

		judgementGraphicSheet = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_judge strings.png"));
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
			judgementStatus[ nLane ].ctFrameProgress = new CCounter( 0, CDTXMania.ConfigIni.nJudgeFrames - 1, CDTXMania.ConfigIni.nJudgeInterval, CDTXMania.Timer );

			judgementStatus[ nLane ].judge = judge;
			judgementStatus[ nLane ].fXScaleRatio = 1f;
			judgementStatus[ nLane ].fYScaleRatio = 1f;
			judgementStatus[ nLane ].fZRotationDegrees = 0f;
			judgementStatus[ nLane ].nRelativeX = 0;
			judgementStatus[ nLane ].nRelativeY = 0;
			judgementStatus[ nLane ].nOpacity = 0xff;

			judgementStatus[ nLane ].fXScaleRatioB = 1f;
			judgementStatus[ nLane ].fYScaleRatioB = 1f;
			judgementStatus[ nLane ].nRelativeXB = 0;
			judgementStatus[ nLane ].nRelativeYB = 0;
			judgementStatus[ nLane ].nOpacityB = 0xff;

			judgementStatus[ nLane ].fZRotationDegrees_Bar = 0f;
			judgementStatus[ nLane ].fXScaleRatio_Bar = 0;
			judgementStatus[ nLane ].fYScaleRatio_Bar = 0;
			judgementStatus[ nLane ].nRelativeX_Bar = 0;
			judgementStatus[ nLane ].nRelativeY_Bar = 0;

			judgementStatus[ nLane ].nLag = lag;
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
			if ( !judgementStatus[ i ].ctFrameProgress.bStopped )
			{
				judgementStatus[ i ].ctFrameProgress.tUpdate();
				if ( judgementStatus[ i ].ctFrameProgress.bReachedEndValue )
				{
					judgementStatus[ i ].ctFrameProgress.tStop();
				}
				judgementStatus[ i ].nRect = judgementStatus[ i ].ctFrameProgress.nCurrentValue;
			}
		}

		for ( int lane = 0; lane < LaneCount; lane++ )
		{
			if ( judgementStatus[ lane ].ctFrameProgress.bStopped )
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
			judgementStatus[ i ].ctFrameProgress = new CCounter();
		}

		InitializeLaneSizes();

		for ( int i = 0; i < 12; i++ )
		{
			if ( CDTXMania.ConfigIni.nShowLagTypeColor == 0 )
			{
				lagNumberInfoArray[ i ].rc = new RectangleF( ( i % 4 ) * 15, ( i / 4 ) * 19, 15f, 19f ); // plus numbers
				lagNumberInfoArray[ i + 12 ].rc = new RectangleF( ( i % 4 ) * 15 + 64, ( i / 4 ) * 19 + 64, 15f, 19f ); // minus numbers
			}
			else
			{
				lagNumberInfoArray[ i ].rc = new RectangleF( ( i % 4 ) * 15 + 64, ( i / 4 ) * 19 + 64, 15f, 19f ); // minus numbers
				lagNumberInfoArray[ i + 12 ].rc = new RectangleF( ( i % 4 ) * 15, ( i / 4 ) * 19, 15f, 19f ); // plus numbers
			}
		}
		base.OnActivate();
		lagDisplayMode = CDTXMania.ConfigIni.nShowLagType;
	}
	public override void OnDeactivate()
	{
		for ( int i = 0; i < 15; i++ )
		{
			judgementStatus[ i ].ctFrameProgress = null!;
		}
		base.OnDeactivate();
	}
	public override void OnManagedCreateResources()
	{
		if ( bActivated )
		{
			txLagNumbers = BaseTexture.LoadFromPath( CSkin.Path( @"Graphics\7_lag numbers.png" ) );
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

		int xc = ( baseX + judgementStatus[ lane ].nRelativeX ) + ( stLaneSize[ lane ].w / 2 );
		float x = xc - ( 110f * judgementStatus[ lane ].fXScaleRatio ) - ( ( nRectX - 225 ) / 2f );
		float y = baseY + judgementStatus[ lane ].nRelativeY - ( 140f * judgementStatus[ lane ].fYScaleRatio / 2f ) - ( ( nRectY - 135 ) / 2f );

		DrawJudgeFrame( lane, x, y, nRectX, nRectY );
		DrawLag( lane, xc, y );
	}

	protected void DrawJudgeFrame( int lane, float x, float y, int nRectX, int nRectY )
	{
		if ( CDTXMania.ConfigIni.nJudgeFrames <= 1 )
		{
			return;
		}

		Trace.TraceInformation($"{x}, {y}");

		BaseTexture txJudge = judgementGraphicSheet;

		int frameY = judgementStatus[ lane ].nRect * nRectY;
		switch ( judgementStatus[ lane ].judge )
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
		if ( lagDisplayMode != (int) EShowLagType.ON &&
			!( ( lagDisplayMode == (int) EShowLagType.GREAT_POOR ) && ( judgementStatus[ lane ].judge != EJudgement.Perfect ) ) )
		{
			return;
		}

		if ( judgementStatus[ lane ].judge == EJudgement.Auto || txLagNumbers == null )
		{
			return;
		}

		bool minus = judgementStatus[ lane ].nLag < 0;
		int offsetX = 0;
		string strDispLag = judgementStatus[ lane ].nLag.ToString();
		float x = xc - strDispLag.Length * 15 / 2f;
		
		for ( int i = 0; i < strDispLag.Length; i++ )
		{
			int p = ( strDispLag[ i ] == '-' ) ? 11 : strDispLag[ i ] - '0';
			p += minus ? 0 : 12;
			txLagNumbers.tDraw2D( x + offsetX, y + 34, lagNumberInfoArray[ p ].rc );
			offsetX += 15;
		}
	}
}

