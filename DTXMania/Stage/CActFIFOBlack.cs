using DTXMania.Core;
using FDK;

namespace DTXMania;

internal class CActFIFOBlack : CActivity
{
	// メソッド

	public void tStartFadeOut()
	{
		mode = EFIFOMode.FadeOut;
		counter = new CCounter( 0, 100, 5, CDTXMania.Timer );
	}
	public void tフェードイン開始()
	{
		mode = EFIFOMode.FadeIn;
		counter = new CCounter( 0, 100, 5, CDTXMania.Timer );
	}
	public void tフェードイン完了()		// #25406 2011.6.9 yyagi
	{
		counter.nCurrentValue = counter.nEndValue;
	}

		
	// CActivity 実装

	public override void OnDeactivate()
	{
		if( !bNotActivated )
		{
			CDTXMania.tReleaseTexture( ref tx黒タイル64x64 );
			base.OnDeactivate();
		}
	}
	public override void OnManagedCreateResources()
	{
		if( !bNotActivated )
		{
			tx黒タイル64x64 = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\Tile black 64x64.png" ), false );
			base.OnManagedCreateResources();
		}
	}
	public override int OnUpdateAndDraw()
	{
		if( bNotActivated || ( counter == null ) )
		{
			return 0;
		}
		counter.tUpdate();
		// Size clientSize = CDTXMania.app.Window.ClientSize;	// #23510 2010.10.31 yyagi: delete as of no one use this any longer.
		if (tx黒タイル64x64 != null)
		{
			tx黒タイル64x64.nTransparency = ( mode == EFIFOMode.FadeIn ) ? ( ( ( 100 - counter.nCurrentValue ) * 0xff ) / 100 ) : ( ( counter.nCurrentValue * 0xff ) / 100 );
			for (int i = 0; i <= (SampleFramework.GameFramebufferSize.Width / 64); i++)		// #23510 2010.10.31 yyagi: change "clientSize.Width" to "640" to fix FIFO drawing size
			{
				for (int j = 0; j <= (SampleFramework.GameFramebufferSize.Height / 64); j++)	// #23510 2010.10.31 yyagi: change "clientSize.Height" to "480" to fix FIFO drawing size
				{
					tx黒タイル64x64.tDraw2D( CDTXMania.app.Device, i * 64, j * 64 );
				}
			}
		}
		if( counter.nCurrentValue != 100 )
		{
			return 0;
		}
		return 1;
	}


	// Other

	#region [ private ]
	//-----------------
	private CCounter counter;
	private EFIFOMode mode;
	private CTexture tx黒タイル64x64;
	//-----------------
	#endregion
}