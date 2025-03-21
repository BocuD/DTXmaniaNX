﻿using System.Drawing;
using DTXMania.Core;

namespace DTXMania;

internal class CActPerfDrumsRGB : CActPerfCommonRGB
{
	// CActivity 実装（共通クラスからの差分のみ）

	public override int OnUpdateAndDraw()
	{
		if( !bNotActivated )
		{
			if( !CDTXMania.ConfigIni.bGuitarEnabled )
			{
				return 0;
			}
			if( CDTXMania.DTX.bHasChips.Guitar )
			{
				for( int j = 0; j < 3; j++ )
				{
					int index = CDTXMania.ConfigIni.bLeft.Guitar ? ( 2 - j ) : j;
					Rectangle rectangle = new Rectangle( index * 0x18, 0, 0x18, 0x20 );
					if( bPressedState[ index ] )
					{
						rectangle.Y += 0x20;
					}
					if( txRGB != null )
					{
						txRGB.tDraw2D( CDTXMania.app.Device, 0x1fd + ( j * 0x1a ), 0x39, rectangle );
					}
				}
			}
			if( CDTXMania.DTX.bHasChips.Bass )
			{
				for( int k = 0; k < 3; k++ )
				{
					int num4 = CDTXMania.ConfigIni.bLeft.Bass ? ( 2 - k ) : k;
					Rectangle rectangle2 = new Rectangle( num4 * 0x18, 0, 0x18, 0x20 );
					if( bPressedState[ num4 + 3 ] )
					{
						rectangle2.Y += 0x20;
					}
					if( txRGB != null )
					{
						txRGB.tDraw2D( CDTXMania.app.Device, 400 + ( k * 0x1a ), 0x39, rectangle2 );
					}
				}
			}
			for( int i = 0; i < 6; i++ )
			{
				bPressedState[ i ] = false;
			}
		}
		return 0;
	}
}