using Timer = System.Threading.Timer;

namespace FDK;

public class CSoundTimer : CTimerBase
{
	public override long nSystemTimeMs
	{
		get
		{
			if( Device.eOutputDevice == ESoundDeviceType.ExclusiveWASAPI || 
			    Device.eOutputDevice == ESoundDeviceType.SharedWASAPI ||
			    Device.eOutputDevice == ESoundDeviceType.ASIO )
			{
				// BASS 系の ISoundDevice.n経過時間ms はオーディオバッファの更新間隔ずつでしか更新されないため、単にこれを返すだけではとびとびの値になる。
				// そこで、更新間隔の最中に呼ばれた場合は、システムタイマを使って補間する。
				// この場合の経過時間との誤差は更新間隔以内に収まるので問題ないと判断する。
				// ただし、ASIOの場合は、転送byte数から時間算出しているため、ASIOの音声合成処理の負荷が大きすぎる場合(処理時間が実時間を超えている場合)は
				// 動作がおかしくなる。(具体的には、ここで返すタイマー値の逆行が発生し、スクロールが巻き戻る)
				// この場合の対策は、ASIOのバッファ量を増やして、ASIOの音声合成処理の負荷を下げること。

				return Device.n経過時間ms
				       + ( Device.tmシステムタイマ.nSystemTimeMs - Device.n経過時間を更新したシステム時刻ms );
			}
			else if( Device.eOutputDevice == ESoundDeviceType.DirectSound )
			{
				//return this.Device.n経過時間ms;		// #24820 2013.2.3 yyagi TESTCODE DirectSoundでスクロールが滑らかにならないため、
				return ct.nSystemTimeMs;				// 仮にCSoundTimerをCTimer相当の動作にしてみた
			}
			return nUnused;
		}
	}

	public CSoundTimer( ISoundDevice device )
	{
		Device = device;

		if ( Device.eOutputDevice != ESoundDeviceType.DirectSound )
		{
			TimerCallback timerDelegate = new TimerCallback( SnapTimers );	// CSoundTimerをシステム時刻に変換するために、
			timer = new Timer( timerDelegate, null, 0, 1000 );				// CSoundTimerとCTimerを両方とも走らせておき、
			ctDInputTimer = new CTimer( CTimer.EType.MultiMedia );			// 1秒に1回時差を測定するようにしておく
		}
		else																// TESTCODE DirectSound時のみ、CSoundTimerでなくCTimerを使う
		{
			ct = new CTimer( CTimer.EType.MultiMedia );
		}
	}
	
	private void SnapTimers(object o)	// 1秒に1回呼び出され、2つのタイマー間の現在値をそれぞれ保持する。
	{
		if ( Device.eOutputDevice != ESoundDeviceType.DirectSound )
		{
			nDInputTimerCounter = ctDInputTimer.nSystemTimeMs;
			nSoundTimerCounter = nSystemTimeMs;
			//Debug.WriteLine( "BaseCounter: " + nDInputTimerCounter + ", " + nSoundTimerCounter );
		}
	}
	public long nサウンドタイマーのシステム時刻msへの変換( long nDInputのタイムスタンプ )
	{
		return nDInputのタイムスタンプ - nDInputTimerCounter + nSoundTimerCounter;	// Timer違いによる時差を補正する
	}
	
	public override void Dispose()
	{
		// 特になし； ISoundDevice の解放は呼び出し元で行うこと。

		//sendinputスレッド削除
		if ( timer != null )
		{
			timer.Change( Timeout.Infinite, Timeout.Infinite );
			timer.Dispose();
			timer = null;
		}
		if ( ct != null )
		{
			ct.tPause();
			ct.Dispose();
			ct = null;
		}
	}

	protected ISoundDevice Device = null;
	//protected Thread thSendInput = null;
	protected Thread thSnapTimers = null;
	private CTimer ctDInputTimer = null;
	private long nDInputTimerCounter = 0;
	private long nSoundTimerCounter = 0;
	Timer timer = null;

	private CTimer ct = null;								// TESTCODE
}