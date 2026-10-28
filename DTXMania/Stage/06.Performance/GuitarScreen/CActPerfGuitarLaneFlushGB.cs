using System.Numerics;
using System.Drawing;
using DTXMania.Core;
using DTXMania.UI.Drawable;

namespace DTXMania;

internal class CActPerfGuitarLaneFlushGB : CActPerfCommonLaneFlushGB
{
	// コンストラクタ

	public CActPerfGuitarLaneFlushGB()
	{
		bActivated = false;
	}
	// 2013.02.22 kairera0467
	// ギターのレーンフラッシュの幅は37。

	// CActivity 実装（共通クラスからの差分のみ）

	public override void OnManagedCreateResources()
	{ 
		txLaneFlush = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_guitar line.png"));
		
		base.OnManagedCreateResources();
	}

	public void Draw(Matrix4x4 origin, EInstrumentPart part)
	{
		if ( bActivated )
		{
			int first = part == EInstrumentPart.BASS ? 5 : 0;
			for ( int i = first; i < first + 5; i++ )
			{
				if ( !ctUpdate[ i ].bStopped )
				{
					EInstrumentPart e楽器パート = part;
					BaseTexture texture = CDTXMania.ConfigIni.bReverse[ (int) e楽器パート ] ? txFlush[ ( i % 5 ) + 5 ] : txFlush[ i % 5 ];
					int num2 = CDTXMania.ConfigIni.bLeft[ (int) e楽器パート ] ? 1 : 0;
					int x = (88 + nRGBのX座標[num2, i] + ( ( 19 * ctUpdate[ i ].nCurrentValue ) / 70 ));
					int y = CDTXMania.ConfigIni.bReverse[ (int) e楽器パート ] ? 414 : 100;
					if ( texture != null && CDTXMania.ConfigIni.bLaneFlush[ (int) e楽器パート ] )
					{
						texture.tDraw2D(origin, x, y, new RectangleF( 37, 0, ( 37 * ( 70 - ctUpdate[ i ].nCurrentValue)) / 70, 256 ) );
					}
					ctUpdate[ i ].tUpdate();
					if ( ctUpdate[ i ].bReachedEndValue )
					{
						ctUpdate[ i ].tStop();
					}
				}
			}
			//ここの分岐文はbase.ctUpdate[ n ]のものを使わないと、停止中にレーンフラッシュが消えてしまう。
                
			if ( part == EInstrumentPart.GUITAR && !ctUpdate[ 0 ].bStopped && CDTXMania.ConfigIni.bLaneFlush.Guitar )
			{
				txLaneFlush.tDraw2D(origin, ( CDTXMania.ConfigIni.bLeft.Guitar ? 242 : 86 ), 104, new RectangleF(0, 0, 41, 566));
			}
			if ( part == EInstrumentPart.GUITAR && !ctUpdate[ 1 ].bStopped && CDTXMania.ConfigIni.bLaneFlush.Guitar )
			{
				txLaneFlush.tDraw2D(origin, ( CDTXMania.ConfigIni.bLeft.Guitar ? 203 : 125 ), 104, new RectangleF(39, 0, 41, 566));
			}
			if ( part == EInstrumentPart.GUITAR && !ctUpdate[ 2 ].bStopped && CDTXMania.ConfigIni.bLaneFlush.Guitar )
			{
				txLaneFlush.tDraw2D(origin, 164, 104, new RectangleF(78, 0, 41, 566));
			}
			if ( part == EInstrumentPart.GUITAR && !ctUpdate[ 3 ].bStopped && CDTXMania.ConfigIni.bLaneFlush.Guitar )
			{
				txLaneFlush.tDraw2D(origin, ( CDTXMania.ConfigIni.bLeft.Guitar ? 125 : 203 ), 104, new RectangleF(117, 0, 41, 566));
			}
			if ( part == EInstrumentPart.GUITAR && !ctUpdate[ 4 ].bStopped && CDTXMania.ConfigIni.bLaneFlush.Guitar )
			{
				txLaneFlush.tDraw2D(origin, ( CDTXMania.ConfigIni.bLeft.Guitar ? 86 : 242 ), 104, new RectangleF(156, 0, 41, 566));
			}
			if ( part == EInstrumentPart.BASS && !ctUpdate[ 5 ].bStopped && CDTXMania.ConfigIni.bLaneFlush.Bass )
			{
				txLaneFlush.tDraw2D(origin, ( CDTXMania.ConfigIni.bLeft.Bass ? 242 : 86 ), 104, new RectangleF(0, 0, 41, 566));
			}
			if ( part == EInstrumentPart.BASS && !ctUpdate[ 6 ].bStopped && CDTXMania.ConfigIni.bLaneFlush.Bass )
			{
				txLaneFlush.tDraw2D(origin, ( CDTXMania.ConfigIni.bLeft.Bass ? 203 : 125 ), 104, new RectangleF(39, 0, 41, 566));
			}
			if ( part == EInstrumentPart.BASS && !ctUpdate[ 7 ].bStopped && CDTXMania.ConfigIni.bLaneFlush.Bass )
			{
				txLaneFlush.tDraw2D(origin, 164, 104, new RectangleF(78, 0, 41, 566));
			}
			if ( part == EInstrumentPart.BASS && !ctUpdate[ 8 ].bStopped && CDTXMania.ConfigIni.bLaneFlush.Bass )
			{
				txLaneFlush.tDraw2D(origin, ( CDTXMania.ConfigIni.bLeft.Bass ? 125 : 203 ), 104, new RectangleF(117, 0, 41, 566));
			}
			if ( part == EInstrumentPart.BASS && !ctUpdate[ 9 ].bStopped && CDTXMania.ConfigIni.bLaneFlush.Bass )
			{
				txLaneFlush.tDraw2D(origin, ( CDTXMania.ConfigIni.bLeft.Bass ? 86 : 242 ), 104, new RectangleF(156, 0, 41, 566));
			}

		}
	}
		

	// Other

	#region [ private ]
	//-----------------
	private readonly int[,] nRGBのX座標 = new int[ , ] { { 0, 39, 78, 117, 156, 0, 39, 78, 117, 156 }, { 156, 117, 78, 39, 0, 156, 117, 78, 39, 0 } };

	private BaseTexture txLaneFlush;
	//-----------------
	#endregion
}