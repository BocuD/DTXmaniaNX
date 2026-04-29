using DTXMania.Core;
using DTXMania.UI.Drawable;
using FDK;

namespace DTXMania;

internal class CActPerfCommonLaneFlushGB : CActivity
{
	// プロパティ

	protected CCounter[] ctUpdate = new CCounter[ 10 ];
	protected BaseTexture[] txFlush = new BaseTexture[ 10 ];


	// コンストラクタ

	public CActPerfCommonLaneFlushGB()
	{
		bActivated = false;
	}


	// メソッド

	public void Start( int nLane )
	{
		if( ( nLane < 0 ) || ( nLane > 10 ) )
		{
			throw new IndexOutOfRangeException( "有効範囲は 0～10 です。" );
		}
		ctUpdate[ nLane ] = new CCounter( 0, 70, 1, CDTXMania.Timer );
	}


	// CActivity 実装

	public override void OnActivate()
	{
		for( int i = 0; i < 10; i++ )
		{
			ctUpdate[ i ] = new CCounter();
		}
		base.OnActivate();
	}
	public override void OnDeactivate()
	{
		for( int i = 0; i < 10; i++ )
		{
			ctUpdate[ i ] = null;
		}
		base.OnDeactivate();
	}
	public override void OnManagedCreateResources()
	{
		if( bActivated )
		{
			txFlush[ 0 ] = BaseTexture.LoadFromPath( CSkin.Path( @"Graphics\ScreenPlay lane flush red.png" ) );
			txFlush[ 1 ] = BaseTexture.LoadFromPath( CSkin.Path( @"Graphics\ScreenPlay lane flush green.png" ) );
			txFlush[ 2 ] = BaseTexture.LoadFromPath( CSkin.Path( @"Graphics\ScreenPlay lane flush blue.png" ) );
			txFlush[ 3 ] = BaseTexture.LoadFromPath( CSkin.Path( @"Graphics\ScreenPlay lane flush yellow.png" ) );
			txFlush[ 4 ] = BaseTexture.LoadFromPath( CSkin.Path( @"Graphics\ScreenPlay lane flush purple.png" ) );

			txFlush[ 5 ] = BaseTexture.LoadFromPath( CSkin.Path( @"Graphics\ScreenPlay lane flush red reverse.png" ) );
			txFlush[ 6 ] = BaseTexture.LoadFromPath( CSkin.Path( @"Graphics\ScreenPlay lane flush green reverse.png" ) );
			txFlush[ 7 ] = BaseTexture.LoadFromPath( CSkin.Path( @"Graphics\ScreenPlay lane flush blue reverse.png" ) );
			txFlush[ 8 ] = BaseTexture.LoadFromPath( CSkin.Path( @"Graphics\ScreenPlay lane flush yellow reverse.png" ) );
			txFlush[ 9 ] = BaseTexture.LoadFromPath( CSkin.Path( @"Graphics\ScreenPlay lane flush purple reverse.png" ) );
			base.OnManagedCreateResources();
		}
	}
}