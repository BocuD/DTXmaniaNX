using System.Runtime.InteropServices;

namespace FDK;

public class CTimer : CTimerBase
{
	public enum EType  // E種別
	{
		Unknown = -1,
		PerformanceCounter = 0,
		MultiMedia = 1,
		GetTickCount = 2,
	}
	public EType eタイマ種別
	{
		get;
		protected set;
	}


	public override long nSystemTimeMs  // nシステム時刻ms
	{
		get
		{
			switch (eタイマ種別)
			{
				case EType.PerformanceCounter:
				{
					double num = 0.0;
					if (n現在の周波数 != 0L)
					{
						long x = 0L;
						QueryPerformanceCounter(ref x);
						num = ((double)x) / (((double)n現在の周波数) / 1000.0);
					}
					return (long)num;
				}
				case EType.MultiMedia:
					return (long)timeGetTime();

				case EType.GetTickCount:
					return (long)Environment.TickCount;
			}
			return 0;
		}
	}

	public CTimer(EType eタイマ種別)
		: base()
	{
		this.eタイマ種別 = eタイマ種別;

		if (n参照カウント[(int)this.eタイマ種別] == 0)
		{
			switch (this.eタイマ種別)
			{
				case EType.PerformanceCounter:
					if (!b確認と設定_PerformanceCounter() && !b確認と設定_MultiMedia())
						b確認と設定_GetTickCount();
					break;

				case EType.MultiMedia:
					if (!b確認と設定_MultiMedia() && !b確認と設定_PerformanceCounter())
						b確認と設定_GetTickCount();
					break;

				case EType.GetTickCount:
					b確認と設定_GetTickCount();
					break;

				default:
					throw new ArgumentException(string.Format("未知のタイマ種別です。[{0}]", this.eタイマ種別));
			}
		}

		tReset();

		n参照カウント[(int)this.eタイマ種別]++;
	}

	public override void Dispose()
	{
		if (eタイマ種別 == EType.Unknown)
			return;

		int type = (int)eタイマ種別;

		n参照カウント[type] = Math.Max(n参照カウント[type] - 1, 0);

		if (n参照カウント[type] == 0)
		{
			if (eタイマ種別 == EType.MultiMedia)
				timeEndPeriod(timeCaps.wPeriodMin);
		}

		eタイマ種別 = EType.Unknown;
	}

	#region [ protected ]
	//-----------------
	protected long n現在の周波数;
	protected static int[] n参照カウント = new int[3];
	protected TimeCaps timeCaps;

	protected bool b確認と設定_GetTickCount()
	{
		eタイマ種別 = EType.GetTickCount;
		return true;
	}
	protected bool b確認と設定_MultiMedia()
	{
		timeCaps = new TimeCaps();
		if ((timeGetDevCaps(out timeCaps, (uint)Marshal.SizeOf(typeof(TimeCaps))) == 0) && (timeCaps.wPeriodMin < 10))
		{
			eタイマ種別 = EType.MultiMedia;
			timeBeginPeriod(timeCaps.wPeriodMin);
			return true;
		}
		return false;
	}
	protected bool b確認と設定_PerformanceCounter()
	{
		if (QueryPerformanceFrequency(ref n現在の周波数) != 0)
		{
			eタイマ種別 = EType.PerformanceCounter;
			return true;
		}
		return false;
	}
	//-----------------
	#endregion

	#region [ DllImport ]
	//-----------------
	[DllImport("kernel32.dll")]
	protected static extern short QueryPerformanceCounter(ref long x);
	[DllImport("kernel32.dll")]
	protected static extern short QueryPerformanceFrequency(ref long x);
	[DllImport("winmm.dll")]
	protected static extern void timeBeginPeriod(uint x);
	[DllImport("winmm.dll")]
	protected static extern void timeEndPeriod(uint x);
	[DllImport("winmm.dll")]
	protected static extern uint timeGetDevCaps(out TimeCaps timeCaps, uint size);
	[DllImport("winmm.dll")]
	protected static extern uint timeGetTime();

	[StructLayout(LayoutKind.Sequential)]
	protected struct TimeCaps
	{
		public uint wPeriodMin;
		public uint wPeriodMax;
	}
	//-----------------
	#endregion
}