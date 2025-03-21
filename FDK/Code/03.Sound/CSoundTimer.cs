﻿using Timer = System.Threading.Timer;

namespace FDK;

public class CSoundTimer : CTimerBase
{
	public override long nSystemTimeMs
	{
		get
		{
			if( Device.e出力デバイス == ESoundDeviceType.ExclusiveWASAPI || 
			    Device.e出力デバイス == ESoundDeviceType.SharedWASAPI ||
			    Device.e出力デバイス == ESoundDeviceType.ASIO )
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
			else if( Device.e出力デバイス == ESoundDeviceType.DirectSound )
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

		if ( Device.e出力デバイス != ESoundDeviceType.DirectSound )
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
		if ( Device.e出力デバイス != ESoundDeviceType.DirectSound )
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

#if false
		// キーボードイベント(keybd_eventの引数と同様のデータ)
		[StructLayout( LayoutKind.Sequential )]
		private struct KEYBDINPUT
		{
			public ushort wVk;
			public ushort wScan;
			public uint dwFlags;
			public uint time;
			public IntPtr dwExtraInfo;
		};
		// 各種イベント(SendInputの引数データ)
		[StructLayout( LayoutKind.Sequential )]
		private struct INPUT
		{
			public int type;
			public KEYBDINPUT ki;
		};
		// キー操作、マウス操作をシミュレート(擬似的に操作する)
		[DllImport( "user32.dll" )]
		private extern static void SendInput(
			int nInputs, ref INPUT pInputs, int cbsize );

		// 仮想キーコードをスキャンコードに変換
		[DllImport( "user32.dll", EntryPoint = "MapVirtualKeyA" )]
		private extern static int MapVirtualKey(
			int wCode, int wMapType );
		
		[DllImport( "user32.dll" )]
		static extern IntPtr GetMessageExtraInfo();

		private const int INPUT_MOUSE = 0;                  // マウスイベント
		private const int INPUT_KEYBOARD = 1;               // キーボードイベント
		private const int INPUT_HARDWARE = 2;               // ハードウェアイベント
		private const int KEYEVENTF_KEYDOWN = 0x0;          // キーを押す
		private const int KEYEVENTF_KEYUP = 0x2;            // キーを離す
		private const int KEYEVENTF_EXTENDEDKEY = 0x1;      // 拡張コード
		private const int KEYEVENTF_SCANCODE = 0x8;
		private const int KEYEVENTF_UNIOCODE = 0x4;
		private const int VK_SHIFT = 0x10;                  // SHIFTキー

		private void pollingSendInput()
		{
//			INPUT[] inp = new INPUT[ 2 ];
			INPUT inp = new INPUT();
			while ( true )
			{
				// (2)キーボード(A)を押す
				//inp[0].type = INPUT_KEYBOARD;
				//inp[ 0 ].ki.wVk = ( ushort ) Key.B;
				//inp[ 0 ].ki.wScan = ( ushort ) MapVirtualKey( inp[ 0 ].ki.wVk, 0 );
				//inp[ 0 ].ki.dwFlags = KEYEVENTF_KEYDOWN;
				//inp[ 0 ].ki.dwExtraInfo = IntPtr.Zero;
				//inp[ 0 ].ki.time = 0;
				inp.type = INPUT_KEYBOARD;
				inp.ki.wVk = ( ushort ) Key.B;
				inp.ki.wScan = ( ushort ) MapVirtualKey( inp.ki.wVk, 0 );
				inp.ki.dwFlags = KEYEVENTF_SCANCODE | KEYEVENTF_KEYDOWN;
				inp.ki.dwExtraInfo = GetMessageExtraInfo();
				inp.ki.time = 0;

				//// (3)キーボード(A)を離す
				//inp[ 1 ].type = INPUT_KEYBOARD;
				//inp[ 1 ].ki.wVk = ( short ) Key.B;
				//inp[ 1 ].ki.wScan = ( short ) MapVirtualKey( inp[ 1 ].ki.wVk, 0 );
				//inp[ 1 ].ki.dwFlags = KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP;
				//inp[ 1 ].ki.dwExtraInfo = 0;
				//inp[ 1 ].ki.time = 0;

				// キーボード操作実行
				SendInput( 1, ref inp, Marshal.SizeOf( inp ) );
Debug.WriteLine( "B" );
				Thread.Sleep( 1000 );
			}
		}
#endif
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