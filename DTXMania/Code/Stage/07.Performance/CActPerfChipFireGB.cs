using SharpDX;
using FDK;

using Point = System.Drawing.Point;

namespace DTXMania;

internal abstract class CActPerfChipFireGB : CActivity
{
	// コンストラクタ

	public CActPerfChipFireGB()
	{
		bNotActivated = true;
	}


	// メソッド

	public virtual void Start( int nLane, int n中央X, int n中央Y )
	{
		if( ( nLane >= 0 ) || ( nLane <= 5 ) )
		{
			pt中央位置[ nLane ].X = n中央X;
			pt中央位置[ nLane ].Y = n中央Y;
			ct進行[ nLane ].tStart( 28, 56, 8, CDTXMania.Timer );		// #24736 2011.2.17 yyagi: (0, 0x38, 4,..) -> (24, 0x38, 8) に変更 ギターチップの光り始めを早くするため
		}
	}

	public abstract void Start( int nLane );

	// CActivity 実装

	public override void OnActivate()
	{
		for( int i = 0; i < 10; i++ )
		{
			pt中央位置[ i ] = new Point( 0, 0 );
			ct進行[ i ] = new CCounter();
		}
		base.OnActivate();
	}
	public override void OnDeactivate()
	{
		for( int i = 0; i < 10; i++ )
		{
			ct進行[ i ] = null;
		}
		base.OnDeactivate();
	}
	public override void OnManagedCreateResources()
	{
		if( !bNotActivated )
		{
			tx火花[ 0 ] = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\ScreenPlay chip fire red.png" ) );
			if( tx火花[ 0 ] != null )
			{
				tx火花[ 0 ].bAdditiveBlending = true;
			}
			tx火花[ 1 ] = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\ScreenPlay chip fire green.png" ) );
			if( tx火花[ 1 ] != null )
			{
				tx火花[ 1 ].bAdditiveBlending = true;
			}
			tx火花[ 2 ] = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\ScreenPlay chip fire blue.png" ) );
			if( tx火花[ 2 ] != null )
			{
				tx火花[ 2 ].bAdditiveBlending = true;
			}
			tx火花[ 3 ] = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\ScreenPlay chip fire yellow.png" ) );
			if( tx火花[ 3 ] != null )
			{
				tx火花[ 3 ].bAdditiveBlending = true;
			}
			tx火花[ 4 ] = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\ScreenPlay chip fire purple.png" ) );
			if( tx火花[ 4 ] != null )
			{
				tx火花[ 4 ].bAdditiveBlending = true;
			}
			txレーンの線 = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\7_guitar line.png"));
			base.OnManagedCreateResources();
		}
	}
	public override void OnManagedReleaseResources()
	{
		if( !bNotActivated )
		{
			CDTXMania.tReleaseTexture( ref tx火花[ 0 ] );
			CDTXMania.tReleaseTexture( ref tx火花[ 1 ] );
			CDTXMania.tReleaseTexture( ref tx火花[ 2 ] );
			CDTXMania.tReleaseTexture( ref tx火花[ 3 ] );
			CDTXMania.tReleaseTexture( ref tx火花[ 4 ] );
			CDTXMania.tReleaseTexture( ref txレーンの線 );
			base.OnManagedReleaseResources();
		}
	}
	public override int OnUpdateAndDraw()
	{
		if( !bNotActivated )
		{
			for( int i = 0; i < 10; i++ )
			{
				ct進行[ i ].tUpdate();
				if( ct進行[ i ].bReachedEndValue )
				{
					ct進行[ i ].tStop();
				}
			}
			for( int j = 0; j < 10; j++ )
			{
				if( ( ct進行[ j ].nCurrentElapsedTimeMs != -1 ) && ( tx火花[ j % 5 ] != null ) )
				{
					float scale = (float) ( 3.0 * Math.Cos( ( Math.PI * ( 90.0 - ( 90.0 * ( ( (double) ct進行[ j ].nCurrentValue ) / 56.0 ) ) ) ) / 180.0 ) );
					int x = pt中央位置[ j ].X - ( (int) ( ( tx火花[ j % 3 ].szImageSize.Width * scale ) / 2f ) );
					int y = pt中央位置[ j ].Y - ( (int) ( ( tx火花[ j % 3 ].szImageSize.Height * scale ) / 2f ) );
					tx火花[ j % 5 ].nTransparency = ( ct進行[ j ].nCurrentValue < 0x1c ) ? 0xff : ( 0xff - ( (int) ( 255.0 * Math.Cos( ( Math.PI * ( 90.0 - ( 90.0 * ( ( (double) ( ct進行[ j ].nCurrentValue - 0x1c ) ) / 28.0 ) ) ) ) / 180.0 ) ) ) );
					tx火花[ j % 5 ].vcScaleRatio = new Vector3( scale, scale, 1f );
					tx火花[ j % 5 ].tDraw2D( CDTXMania.app.Device, x, y );
				}
			}
		}
		return 0;
	}


	// Other

	#region [ private ]
	//-----------------
	private CCounter[] ct進行 = new CCounter[ 10 ];
	private Point[] pt中央位置 = new Point[ 10 ];
	private CTexture[] tx火花 = new CTexture[ 5 ];
	private CTexture txレーンの線;
	//-----------------
	#endregion
}