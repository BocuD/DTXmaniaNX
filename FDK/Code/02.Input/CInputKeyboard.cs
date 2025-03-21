﻿using System.Diagnostics;
using SharpDX.DirectInput;

using SlimDXKey = SlimDX.DirectInput.Key;
using SharpDXKey = SharpDX.DirectInput.Key;

namespace FDK;

public class CInputKeyboard : IInputDevice, IDisposable
{
	// コンストラクタ

	public CInputKeyboard(IntPtr hWnd, DirectInput directInput)
	{
		eInputDeviceType = EInputDeviceType.Keyboard;
		GUID = "";
		ID = 0;
		try
		{
			devKeyboard = new Keyboard(directInput);
			devKeyboard.SetCooperativeLevel(hWnd, CooperativeLevel.NoWinKey | CooperativeLevel.Foreground | CooperativeLevel.NonExclusive);
			devKeyboard.Properties.BufferSize = 32;
			Trace.TraceInformation(devKeyboard.Information.ProductName.Trim(new char[] { '\0' }) + " を生成しました。");    // なぜか0x00のゴミが出るので削除
			strDeviceName = devKeyboard.Information.ProductName.Trim(new char[] { '\0' });
		}
		catch
		{
			if (devKeyboard != null)
			{
				devKeyboard.Dispose();
				devKeyboard = null;
			}
			Trace.TraceWarning("Keyboard デバイスの生成に失敗しました。");
			throw;
		}
		try
		{
			devKeyboard.Acquire();
		}
		catch
		{
		}

		for (int i = 0; i < bKeyState.Length; i++)
			bKeyState[i] = false;

		//this.timer = new CTimer( CTimer.E種別.MultiMedia );
		listInputEvent = new List<STInputEvent>(32);
		// this.ct = new CTimer( CTimer.E種別.PerformanceCounter );
	}


	// メソッド

	#region [ IInputDevice 実装 ]
	//-----------------
	public EInputDeviceType eInputDeviceType { get; private set; }  // e入力デバイス種別
	public string GUID { get; private set; }
	public int ID { get; private set; }
	public List<STInputEvent> listInputEvent { get; private set; }
	public string strDeviceName { get; set; }

	public void tPolling(bool bWindowがアクティブ中, bool bバッファ入力を使用する)  // tポーリング
	{
		for (int i = 0; i < 256; i++)
		{
			bKeyPushDown[i] = false;
			bKeyPullUp[i] = false;
		}

		if (bWindowがアクティブ中 && (devKeyboard != null))
		{
			devKeyboard.Acquire();
			devKeyboard.Poll();

			//this.list入力イベント = new List<STInputEvent>( 32 );
			listInputEvent.Clear();            // #xxxxx 2012.6.11 yyagi; To optimize, I removed new();
			int posEnter = -1;
			//string d = DateTime.Now.ToString( "yyyy/MM/dd HH:mm:ss.ffff" );

			if (bバッファ入力を使用する)
			{
				#region [ a.バッファ入力 ]
				//-----------------------------
				var bufferedData = devKeyboard.GetBufferedData();
				//if ( Result.Last.IsSuccess && bufferedData != null )
				{
					foreach (KeyboardUpdate data in bufferedData)
					{
						// #xxxxx: 2017.5.7: from: DIK (SharpDX.DirectInput.Key) を SlimDX.DirectInput.Key に変換。
						var key = DeviceConstantConverter.DIKtoKey(data.Key);
						if (SlimDXKey.Unknown == key)
							continue;   // 未対応キーは無視。

						//foreach ( Key key in data.PressedKeys )
						if (data.IsPressed)
						{
							// #23708 2016.3.19 yyagi; Even if we remove ALT+ENTER key input by SuppressKeyPress = true in Form,
							// it doesn't affect to DirectInput (ALT+ENTER does not remove)
							// So we ignore ENTER input in ALT+ENTER combination here.
							// Note: ENTER will be alived if you keyup ALT after ALT+ENTER.
							if (key != SlimDXKey.Return || (bKeyState[(int)SlimDXKey.LeftAlt] == false && bKeyState[(int)SlimDXKey.RightAlt] == false))
							{
								STInputEvent item = new STInputEvent()
								{
									nKey = (int)key,
									b押された = true,
									b離された = false,
									nTimeStamp = CSoundManager.rcPerformanceTimer.nサウンドタイマーのシステム時刻msへの変換(data.Timestamp),
									nVelocity = CInputManager.n通常音量
								};
								listInputEvent.Add(item);

								bKeyState[(int)key] = true;
								bKeyPushDown[(int)key] = true;
							}
							//if ( item.nKey == (int) SlimDXKey.Space )
							//{
							//    Trace.TraceInformation( "FDK(buffered): SPACE key registered. " + ct.nシステム時刻 );
							//}
						}
						//foreach ( Key key in data.ReleasedKeys )
						if (data.IsReleased)
						{
							STInputEvent item = new STInputEvent()
							{
								nKey = (int)key,
								b押された = false,
								b離された = true,
								nTimeStamp = CSoundManager.rcPerformanceTimer.nサウンドタイマーのシステム時刻msへの変換(data.Timestamp),
								nVelocity = CInputManager.n通常音量
							};
							listInputEvent.Add(item);

							bKeyState[(int)key] = false;
							bKeyPullUp[(int)key] = true;
						}
					}
				}
				//-----------------------------
				#endregion
			}
			else
			{
				#region [ b.状態入力 ]
				//-----------------------------
				KeyboardState currentState = devKeyboard.GetCurrentState();
				//if ( Result.Last.IsSuccess && currentState != null )
				{
					foreach (SharpDXKey dik in currentState.PressedKeys)
					{
						// #xxxxx: 2017.5.7: from: DIK (SharpDX.DirectInput.Key) を SlimDX.DirectInput.Key に変換。
						var key = DeviceConstantConverter.DIKtoKey(dik);
						if (SlimDXKey.Unknown == key)
							continue;   // 未対応キーは無視。

						if (bKeyState[(int)key] == false)
						{
							if (key != SlimDXKey.Return || (bKeyState[(int)SlimDXKey.LeftAlt] == false && bKeyState[(int)SlimDXKey.RightAlt] == false))    // #23708 2016.3.19 yyagi
							{
								var ev = new STInputEvent()
								{
									nKey = (int)key,
									b押された = true,
									b離された = false,
									nTimeStamp = CSoundManager.rcPerformanceTimer.nシステム時刻, // 演奏用タイマと同じタイマを使うことで、BGMと譜面、入力ずれを防ぐ。
									nVelocity = CInputManager.n通常音量,
								};
								listInputEvent.Add(ev);

								bKeyState[(int)key] = true;
								bKeyPushDown[(int)key] = true;
							}

							//if ( (int) key == (int) SlimDXKey.Space )
							//{
							//    Trace.TraceInformation( "FDK(direct): SPACE key registered. " + ct.nシステム時刻 );
							//}
						}
					}
					//foreach ( Key key in currentState.ReleasedKeys )
					foreach (SharpDXKey dik in currentState.AllKeys)
					{
						// #xxxxx: 2017.5.7: from: DIK (SharpDX.DirectInput.Key) を SlimDX.DirectInput.Key に変換。
						var key = DeviceConstantConverter.DIKtoKey(dik);
						if (SlimDXKey.Unknown == key)
							continue;   // 未対応キーは無視。

						if (bKeyState[(int)key] == true && !currentState.IsPressed(dik)) // 前回は押されているのに今回は押されていない → 離された
						{
							var ev = new STInputEvent()
							{
								nKey = (int)key,
								b押された = false,
								b離された = true,
								nTimeStamp = CSoundManager.rcPerformanceTimer.nシステム時刻, // 演奏用タイマと同じタイマを使うことで、BGMと譜面、入力ずれを防ぐ。
								nVelocity = CInputManager.n通常音量,
							};
							listInputEvent.Add(ev);

							bKeyState[(int)key] = false;
							bKeyPullUp[(int)key] = true;
						}
					}
				}
				//-----------------------------
				#endregion
			}
			#region [#23708 2011.4.8 yyagi Altが押されているときは、Enter押下情報を削除する -> 副作用が見つかり削除]
			//if ( this.bKeyState[ (int) SlimDXKey.RightAlt ] ||
			//     this.bKeyState[ (int) SlimDXKey.LeftAlt ] )
			//{
			//    int cr = (int) SlimDXKey.Return;
			//    this.bKeyPushDown[ cr ] = false;
			//    this.bKeyPullUp[ cr ] = false;
			//    this.bKeyState[ cr ] = false;
			//}
			#endregion
		}
	}

	public bool bKeyPressed(int nKey)
	{
		//block keyboard input if ImGui is overriding it
		if (preventKeyboardInput)
			return false;
		
		return bKeyPushDown[nKey];
	}

	public bool bKeyPressing(int nKey)
	{
		//block keyboard input if ImGui is overriding it
		if (preventKeyboardInput)
			return false;
		
		return bKeyState[nKey];
	}

	public bool bKeyReleased(int nKey)
	{
		if (preventKeyboardInput)
			return false;
		
		return bKeyPullUp[nKey];
	}

	public bool bKeyReleasing(int nKey)
	{
		if (preventKeyboardInput)
			return false;
		
		return !bKeyState[nKey];
	}

	///  
	/// <param name="nKey">
	///     調べる SlimDX.DirectInput.Key を int にキャストした値。（SharpDX.DirectInput.Key ではないので注意。）
	/// </param>
	public bool bKeyPressed(SlimDXKey nKey)  // bキーが押された
	{
		//block keyboard input if ImGui is overriding it
		if (preventKeyboardInput)
			return false;
		
		return bKeyPushDown[(int)nKey];
	}

	///  
	/// <param name="nKey">
	///     調べる SlimDX.DirectInput.Key を int にキャストした値。（SharpDX.DirectInput.Key ではないので注意。）
	/// </param>
	public bool bKeyPressing(SlimDXKey nKey)  // bキーが押されている
	{
		//block keyboard input if ImGui is overriding it
		if (preventKeyboardInput)
			return false;
		
		return bKeyState[(int)nKey];
	}

	/// <param name="nKey">
	///		調べる SlimDX.DirectInput.Key を int にキャストした値。（SharpDX.DirectInput.Key ではないので注意。）
	/// </param>
	public bool bKeyReleased(SlimDXKey nKey)  // bキーが離された
	{
		//block keyboard input if ImGui is overriding it
		if (preventKeyboardInput)
			return false;
		
		return bKeyPullUp[(int)nKey];
	}
	
	/// <param name="nKey">
	///		調べる SlimDX.DirectInput.Key を int にキャストした値。（SharpDX.DirectInput.Key ではないので注意。）
	/// </param>
	public bool bKeyReleasing(SlimDXKey nKey)  // bキーが離されている
	{
		//block keyboard input if ImGui is overriding it
		if (preventKeyboardInput)
			return false;
		
		return !bKeyState[(int)nKey];
	}
	//-----------------
	#endregion

	#region [ IDisposable 実装 ]
	//-----------------
	public void Dispose()
	{
		if (!bDispose完了済み)
		{
			if (devKeyboard != null)
			{
				devKeyboard.Dispose();
				devKeyboard = null;
			}
			//if( this.timer != null )
			//{
			//    this.timer.Dispose();
			//    this.timer = null;
			//}
			if (listInputEvent != null)
			{
				listInputEvent = null;
			}
			bDispose完了済み = true;
		}
	}
	//-----------------
	#endregion


	// その他

	#region [ private ]
	//-----------------
	private bool bDispose完了済み;
	private bool[] bKeyPullUp = new bool[256];
	private bool[] bKeyPushDown = new bool[256];
	private bool[] bKeyState = new bool[256];
	private Keyboard devKeyboard;

	public bool preventKeyboardInput = false;
	//private CTimer timer;
	//private CTimer ct;
	//-----------------

	#endregion
}