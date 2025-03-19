using FDK;

namespace DTXMania;

internal class CActPerfScrollSpeed : CActivity
{
	// プロパティ

	public STDGBVALUE<double> db現在の譜面スクロール速度;


	// コンストラクタ

	public CActPerfScrollSpeed()
	{
		bNotActivated = true;
	}


	// CActivity 実装

	public override void OnActivate()
	{
		for( int i = 0; i < 3; i++ )
		{
			db譜面スクロール速度[ i ] = db現在の譜面スクロール速度[ i ] = (double) CDTXMania.ConfigIni.nScrollSpeed[ i ];
			n速度変更制御タイマ[ i ] = -1;
		}
		base.OnActivate();
	}
	public override unsafe int OnUpdateAndDraw()
	{
		if( !bNotActivated )
		{
			if( bJustStartedUpdate )
			{
				n速度変更制御タイマ.Drums = n速度変更制御タイマ.Guitar = n速度変更制御タイマ.Bass = CSoundManager.rcPerformanceTimer.nシステム時刻;
				bJustStartedUpdate = false;
			}
			long num = CSoundManager.rcPerformanceTimer.nCurrentTime;
			for( int i = 0; i < 3; i++ )
			{
				double num3 = (double) CDTXMania.ConfigIni.nScrollSpeed[ i ];
				if( num < n速度変更制御タイマ[ i ] )
				{
					n速度変更制御タイマ[ i ] = num;
				}
				while( ( num - n速度変更制御タイマ[ i ] ) >= 2 )
				{
					if( db譜面スクロール速度[ i ] < num3 )
					{
						db現在の譜面スクロール速度[ i ] += 0.012;

						if( db現在の譜面スクロール速度[ i ] > num3 )
						{
							db現在の譜面スクロール速度[ i ] = num3;
							db譜面スクロール速度[ i ] = num3;
						}
					}
					else if( db譜面スクロール速度[ i ] > num3 )
					{
						db現在の譜面スクロール速度[ i ] -= 0.012;

						if( db現在の譜面スクロール速度[ i ] < num3 )
						{
							db現在の譜面スクロール速度[ i ] = num3;
							db譜面スクロール速度[ i ] = num3;
						}
					}
					//this.db現在の譜面スクロール速度[ i ] = this.db譜面スクロール速度[ i ];
					n速度変更制御タイマ[ i ] += 2;
				}
			}
		}
		return 0;
	}


	// Other

	#region [ private ]
	//-----------------
	private STDGBVALUE<double> db譜面スクロール速度;
	private STDGBVALUE<long> n速度変更制御タイマ;
	//-----------------
	#endregion
}