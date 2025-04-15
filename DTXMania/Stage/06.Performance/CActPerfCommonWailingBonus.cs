using DTXMania.Core;
using FDK;

namespace DTXMania;

internal abstract class CActPerfCommonWailingBonus : CActivity
{
	// メソッド

	public CActPerfCommonWailingBonus()
	{
		bActivated = false;
	}

	public void Start( EInstrumentPart part )
	{
		Start( part, null );
	}
	public abstract void Start( EInstrumentPart part, CChip r歓声Chip );



	// CActivity 実装

	public override void OnActivate()
	{
		base.OnActivate();
	}
	public override void OnDeactivate()
	{
		base.OnDeactivate();
	}

	public override void OnManagedCreateResources()
	{
		if ( bActivated )
		{
			txWailingBonus = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\ScreenPlay wailing bonus.png" ) );
			txWailingFlush = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\7_WailingFlush.png" ) );
			txWailingFire = CDTXMania.tテクスチャの生成Af( CSkin.Path( @"Graphics\7_WailingFire.png" ) );
			base.OnManagedCreateResources();
		}
	}
	public override void OnManagedReleaseResources()
	{
		if ( bActivated )
		{
			CDTXMania.tReleaseTexture( ref txWailingBonus );
			CDTXMania.tReleaseTexture( ref txWailingFlush );
			CDTXMania.tReleaseTexture( ref txWailingFire );
			base.OnManagedReleaseResources();
		}
	}


	// Other

	#region [ private ]
	//-----------------
	protected CCounter[,] ct進行用 = new CCounter[ 3, 4 ];
	protected CCounter[,] ctWailing炎 = new CCounter[ 3, 4 ];
	protected CTexture txWailingBonus;
	protected CTexture txWailingFlush;
	protected CTextureAf txWailingFire;
	//-----------------
	#endregion
}