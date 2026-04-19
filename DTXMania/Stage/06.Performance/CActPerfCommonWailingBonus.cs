using DTXMania.Core;
using DTXMania.UI.Drawable;
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

	public override void OnManagedCreateResources()
	{
		if ( bActivated )
		{
			txWailingBonus = BaseTexture.LoadFromPath( CSkin.Path( @"Graphics\ScreenPlay wailing bonus.png" ) );
			txWailingFlush = BaseTexture.LoadFromPath( CSkin.Path( @"Graphics\7_WailingFlush.png" ) );
			txWailingFire = BaseTexture.LoadFromPath( CSkin.Path( @"Graphics\7_WailingFire.png" ) );
			base.OnManagedCreateResources();
		}
	}

	// Other

	#region [ private ]
	//-----------------
	protected CCounter[,] ct進行用 = new CCounter[ 3, 4 ];
	protected CCounter[,] ctWailing炎 = new CCounter[ 3, 4 ];
	protected BaseTexture txWailingBonus;
	protected BaseTexture txWailingFlush;
	protected BaseTexture txWailingFire;
	//-----------------
	#endregion
}