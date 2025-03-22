using System.Diagnostics;
using SharpDX.DirectInput;
using Key = SlimDX.DirectInput.Key;

namespace FDK;

public class CInputMouse : IInputDevice, IDisposable
{
	// 定数

	public const int nマウスの最大ボタン数 = 8;


	// コンストラクタ

	public CInputMouse(IntPtr hWnd, DirectInput directInput)
	{
		eInputDeviceType = EInputDeviceType.Mouse;
		GUID = "";
		ID = 0;
		try
		{
			devMouse = new Mouse(directInput);
			devMouse.SetCooperativeLevel(hWnd, CooperativeLevel.Foreground | CooperativeLevel.NonExclusive);
			devMouse.Properties.BufferSize = 0x20;
			Trace.TraceInformation(devMouse.Information.ProductName.Trim(new char[] { '\0' }) + " を生成しました。");  // なぜか0x00のゴミが出るので削除
			strDeviceName = devMouse.Information.ProductName.Trim(new char[] { '\0' });
		}
		catch
		{
			if (devMouse != null)
			{
				devMouse.Dispose();
				devMouse = null;
			}
			Trace.TraceWarning("Mouse デバイスの生成に失敗しました。");
			throw;
		}
		try
		{
			devMouse.Acquire();
		}
		catch
		{
		}

		for (int i = 0; i < bMouseState.Length; i++)
			bMouseState[i] = false;

		//this.timer = new CTimer( CTimer.E種別.MultiMedia );
		listInputEvent = new List<STInputEvent>(32);
	}


	// メソッド

	#region [ IInputDevice 実装 ]
	//-----------------
	public EInputDeviceType eInputDeviceType { get; private set; }  // e入力デバイス種別 
	public string GUID { get; private set; }
	public int ID { get; private set; }
	public List<STInputEvent> listInputEvent { get; private set; }  // list入力イベント
	public string strDeviceName { get; set; }

	public void tPolling(bool bWindowがアクティブ中, bool bバッファ入力を使用する)  // tポーリング
	{
		for (int i = 0; i < 8; i++)
		{
			bMousePushDown[i] = false;
			bMousePullUp[i] = false;
		}

		if (bWindowがアクティブ中 && (devMouse != null))
		{
			devMouse.Acquire();
			devMouse.Poll();

			// this.list入力イベント = new List<STInputEvent>( 32 );
			listInputEvent.Clear();            // #xxxxx 2012.6.11 yyagi; To optimize, I removed new();

			if (bバッファ入力を使用する)
			{
				#region [ a.バッファ入力 ]
				//-----------------------------
				var bufferedData = devMouse.GetBufferedData();
				//if( Result.Last.IsSuccess && bufferedData != null )
				{
					foreach (MouseUpdate data in bufferedData)
					{
						var mouseButton = new[] {
							MouseOffset.Buttons0,
							MouseOffset.Buttons1,
							MouseOffset.Buttons2,
							MouseOffset.Buttons3,
							MouseOffset.Buttons4,
							MouseOffset.Buttons5,
							MouseOffset.Buttons6,
							MouseOffset.Buttons7,
						};

						for (int k = 0; k < 8; k++)
						{
							//if( data.IsPressed( k ) )
							if (data.Offset == mouseButton[k] && ((data.Value & 0x80) != 0))
							{
								STInputEvent item = new STInputEvent()
								{
									nKey = k,
									b押された = true,
									b離された = false,
									nTimeStamp = CSoundManager.rcPerformanceTimer.nサウンドタイマーのシステム時刻msへの変換(data.Timestamp),
									nVelocity = CInputManager.n通常音量
								};
								listInputEvent.Add(item);

								bMouseState[k] = true;
								bMousePushDown[k] = true;
							}
							else if (data.Offset == mouseButton[k] && bMouseState[k] == true && ((data.Value & 0x80) == 0))
								//else if( data.IsReleased( k ) )
							{
								STInputEvent item = new STInputEvent()
								{
									nKey = k,
									b押された = false,
									b離された = true,
									nTimeStamp = CSoundManager.rcPerformanceTimer.nサウンドタイマーのシステム時刻msへの変換(data.Timestamp),
									nVelocity = CInputManager.n通常音量
								};
								listInputEvent.Add(item);

								bMouseState[k] = false;
								bMousePullUp[k] = true;
							}
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
				MouseState currentState = devMouse.GetCurrentState();
				//if( Result.Last.IsSuccess && currentState != null )
				{
					bool[] buttons = currentState.Buttons;

					for (int j = 0; (j < buttons.Length) && (j < 8); j++)
					{
						if (bMouseState[j] == false && buttons[j] == true)
						{
							var ev = new STInputEvent()
							{
								nKey = j,
								b押された = true,
								b離された = false,
								nTimeStamp = CSoundManager.rcPerformanceTimer.nシステム時刻, // 演奏用タイマと同じタイマを使うことで、BGMと譜面、入力ずれを防ぐ。
								nVelocity = CInputManager.n通常音量,
							};
							listInputEvent.Add(ev);

							bMouseState[j] = true;
							bMousePushDown[j] = true;
						}
						else if (bMouseState[j] == true && buttons[j] == false)
						{
							var ev = new STInputEvent()
							{
								nKey = j,
								b押された = false,
								b離された = true,
								nTimeStamp = CSoundManager.rcPerformanceTimer.nシステム時刻, // 演奏用タイマと同じタイマを使うことで、BGMと譜面、入力ずれを防ぐ。
								nVelocity = CInputManager.n通常音量,
							};
							listInputEvent.Add(ev);

							bMouseState[j] = false;
							bMousePullUp[j] = true;
						}
					}
				}
				//-----------------------------
				#endregion
			}
		}
	}
	public bool bKeyPressed(int nButton)  // bキーが押された
	{
		return (((0 <= nButton) && (nButton < 8)) && bMousePushDown[nButton]);
	}
	public bool bKeyPressing(int nButton)  // bキーが押されている
	{
		return (((0 <= nButton) && (nButton < 8)) && bMouseState[nButton]);
	}
	public bool bKeyReleased(int nButton)  // bキーが離された
	{
		return (((0 <= nButton) && (nButton < 8)) && bMousePullUp[nButton]);
	}
	public bool bKeyReleasing(int nButton)  // bキーが離されている
	{
		return (((0 <= nButton) && (nButton < 8)) && !bMouseState[nButton]);
	}
	//-----------------
	#endregion

	#region [ IDisposable 実装 ]
	//-----------------
	public void Dispose()
	{
		if (!bDispose完了済み)
		{
			if (devMouse != null)
			{
				devMouse.Dispose();
				devMouse = null;
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
	private bool[] bMousePullUp = new bool[8];
	private bool[] bMousePushDown = new bool[8];
	private bool[] bMouseState = new bool[8];
	private Mouse devMouse;
	//private CTimer timer;
	//-----------------
	#endregion
}