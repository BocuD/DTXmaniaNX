using DTXMania.Core;
using FDK;

namespace DTXMania;

internal class CActPerfGuitarDanger : CActPerfCommonDanger
{

	public override void OnManagedCreateResources()
	{
		if ( !bNotActivated )
		{
			txDANGER = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\ScreenPlayGuitar danger.png" ) );
			base.OnManagedCreateResources();
		}
	}
	public override void OnManagedReleaseResources()
	{
		if ( !bNotActivated )
		{
			CDTXMania.tReleaseTexture( ref txDANGER );
			base.OnManagedReleaseResources();
		}
	}
	public override int OnUpdateAndDraw()
	{
		throw new InvalidOperationException( "tUpdateAndDraw(bool)のほうを使用してください。" );
	}
	/// <summary>
	/// DANGER表示(Guitar/Bass)
	/// </summary>
	/// <param name="bIsDangerDrums">DrumsがDangerか否か(未使用)</param>
	/// <param name="bIsDangerGuitar">GuitarがDangerか否か</param>
	/// <param name="bIsDangerBass">BassがDangerか否か</param>
	/// <returns></returns>
	public override int tUpdateAndDraw( bool bIsDangerDrums, bool bIsDangerGuitar, bool bIsDangerBass )
	{
		bool[] bIsDanger = { bIsDangerDrums, bIsDangerGuitar, bIsDangerBass };

		if ( !bNotActivated )
		{
			if ( ct透明度用 == null )
			{
				//this.ct移動用 = new CCounter( 0, 0x7f, 7, CDTXMania.Timer );
				ct透明度用 = new CCounter( 0, n波長, 8, CDTXMania.Timer );
			}
			if ( ct透明度用 != null )
			{
				//this.ct移動用.tUpdateLoop();
				ct透明度用.tUpdateLoop();
			}
			for ( int nPart = (int) EInstrumentPart.GUITAR; nPart <= (int) EInstrumentPart.BASS; nPart++ )
			{
				//	this.bDanger中[nPart] = bIsDanger[nPart];
				if ( bIsDanger[ nPart ] )
				{
					if ( txDANGER != null )
					{
						int d = ct透明度用.nCurrentValue;
						txDANGER.nTransparency = n透明度MIN + ( ( d < n波長 / 2 ) ? ( n透明度MAX - n透明度MIN ) * d / ( n波長 / 2 ) : ( n透明度MAX - n透明度MIN ) * ( n波長 - d ) / ( n波長 / 2 ) );		// 60-200
						txDANGER.tDraw2D( CDTXMania.app.Device, nGaugeX[ nPart ], 0 );
					}
				}
			}
		}
		return 0;
	}


	// Other

	#region [ private ]
	//-----------------
	private const int n波長 = 40;
	private const int n透明度MAX = 180;
	private const int n透明度MIN = 20;
	private readonly int[] nGaugeX = { 0, 168, 328 };
//		private readonly Rectangle[] rc領域 = new Rectangle[] { new Rectangle( 0, 0, 0x20, 0x40 ), new Rectangle( 0x20, 0, 0x20, 0x40 ) };
	private CTexture txDANGER;
	//-----------------
	#endregion
}