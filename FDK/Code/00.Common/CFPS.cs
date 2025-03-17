namespace FDK
{
	public class CFPS
	{
		// プロパティ

		public int n現在のFPS
		{
			get;
			private set;
		}
		public bool bFPSの値が変化した
		{
			get;
			private set;
		}


		// コンストラクタ

		public CFPS()
		{
			n現在のFPS = 0;
			timer = new CTimer( CTimer.EType.MultiMedia );
			基点時刻ms = timer.nCurrentTime;
			内部FPS = 0;
			bFPSの値が変化した = false;
		}


		// メソッド

		public void tカウンタ更新()
		{
			timer.tUpdate();
			bFPSの値が変化した = false;

			const long INTERVAL = 1000;
			while( ( timer.nCurrentTime - 基点時刻ms ) >= INTERVAL )
			{
				n現在のFPS = 内部FPS;
				内部FPS = 0;
				bFPSの値が変化した = true;
				基点時刻ms += INTERVAL;
			}
			内部FPS++;
		}


		// その他

		#region [ private ]
		//-----------------
		private CTimer	timer;
		private long	基点時刻ms;
		private int		内部FPS;
		//-----------------
		#endregion
	}
}
