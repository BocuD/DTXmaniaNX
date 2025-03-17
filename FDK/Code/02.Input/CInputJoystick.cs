using System.Diagnostics;
using SharpDX.DirectInput;

namespace FDK
{
	public class CInputJoystick : IInputDevice, IDisposable
	{
		// コンストラクタ

		public CInputJoystick(IntPtr hWnd, DeviceInstance di, DirectInput directInput)
		{
			eInputDeviceType = EInputDeviceType.Joystick;
			GUID = di.InstanceGuid.ToString();
			ID = 0;
			try
			{
				devJoystick = new Joystick(directInput, di.InstanceGuid);
				devJoystick.SetCooperativeLevel(hWnd, CooperativeLevel.Foreground | CooperativeLevel.Exclusive);
				devJoystick.Properties.BufferSize = 32;
				Trace.TraceInformation(devJoystick.Information.InstanceName + "を生成しました。");
				strDeviceName = devJoystick.Information.InstanceName;
			}
			catch
			{
				if (devJoystick != null)
				{
					devJoystick.Dispose();
					devJoystick = null;
				}
				Trace.TraceError(devJoystick.Information.InstanceName, new object[] { " の生成に失敗しました。" });
				throw;
			}
			foreach (DeviceObjectInstance instance in devJoystick.GetObjects())
			{
				if ((instance.ObjectId.Flags & DeviceObjectTypeFlags.Axis) != DeviceObjectTypeFlags.All)
				{
					devJoystick.GetObjectPropertiesById(instance.ObjectId).Range = new InputRange(-1000, 1000);
					devJoystick.GetObjectPropertiesById(instance.ObjectId).DeadZone = 5000;        // 50%をデッドゾーンに設定
																										// 軸をON/OFFの2値で使うならこれで十分
				}
			}
			try
			{
				devJoystick.Acquire();
			}
			catch
			{
			}

			for (int i = 0; i < bButtonState.Length; i++)
				bButtonState[i] = false;
			for (int i = 0; i < nPovState.Length; i++)
				nPovState[i] = -1;

			//this.timer = new CTimer( CTimer.E種別.MultiMedia );

			listInputEvent = new List<STInputEvent>(32);
		}


		// メソッド

		public void SetID(int nID)
		{
			ID = nID;
		}

		#region [ IInputDevice 実装 ]
		//-----------------
		public EInputDeviceType eInputDeviceType
		{
			get;
			private set;
		}
		public string GUID
		{
			get;
			private set;
		}
		public int ID
		{
			get;
			private set;
		}
		public List<STInputEvent> listInputEvent
		{
			get;
			private set;
		}
		public string strDeviceName
		{
			get;
			set;
		}

		public void tPolling(bool bWindowがアクティブ中, bool bバッファ入力を使用する)  // tポーリング
		{
			#region [ bButtonフラグ初期化 ]
			for (int i = 0; i < 256; i++)
			{
				bButtonPushDown[i] = false;
				bButtonPullUp[i] = false;
			}
			#endregion

			if (bWindowがアクティブ中)
			{
				devJoystick.Acquire();
				devJoystick.Poll();

				// this.list入力イベント = new List<STInputEvent>( 32 );
				listInputEvent.Clear();                        // #xxxxx 2012.6.11 yyagi; To optimize, I removed new();


				if (bバッファ入力を使用する)
				{
					#region [ a.バッファ入力 ]
					//-----------------------------
					var bufferedData = devJoystick.GetBufferedData();
					//if( Result.Last.IsSuccess && bufferedData != null )
					{
						foreach (JoystickUpdate data in bufferedData)
						{
#if false
//if ( 0 < data.X && data.X < 128 && 0 < data.Y && data.Y < 128 && 0 < data.Z && data.Z < 128 )
{
Trace.TraceInformation( "TS={0}: offset={4}, X={1},Y={2},Z={3}", data.TimeStamp, data.X, data.Y, data.Z, data.JoystickDeviceType);
if ( data.JoystickDeviceType == (int) JoystickDeviceType.POV0 ||
	 data.JoystickDeviceType == (int) JoystickDeviceType.POV1 ||
	 data.JoystickDeviceType == (int) JoystickDeviceType.POV2 ||
	 data.JoystickDeviceType == (int) JoystickDeviceType.POV3) {

//if ( data.JoystickDeviceType== (int)JoystickDeviceType.POV0 )
//{
	 Debug.WriteLine( "POV0です!!" );
}
//Trace.TraceInformation( "TS={0}: X={1},Y={2},Z={3}", data.TimeStamp, data.X, data.Y, data.Z );
string pp = "";
int[] pp0 = data.GetPointOfViewControllers();
for ( int ii = 0; ii < pp0.Length; ii++ )
{
pp += pp0[ ii ];
}
Trace.TraceInformation( "TS={0}: povs={1}", data.TimeStamp, pp );
string pp2 = "", pp3 = "";
for ( int ii = 0; ii < 32; ii++ )
{
pp2 += ( data.IsPressed( ii ) ) ? "1" : "0";
pp3 += ( data.IsReleased( ii ) ) ? "1" : "0";
}
Trace.TraceInformation( "TS={0}: IsPressed={1}, IsReleased={2}", data.TimeStamp, pp2, pp3 );
}
#endif
							switch (data.Offset)
							{
								case JoystickOffset.X:
									#region [ X軸－ ]
									//-----------------------------
									bButtonUpDown(data, data.Value, 0, 1);
									//-----------------------------
									#endregion
									#region [ X軸＋ ]
									//-----------------------------
									bButtonUpDown(data, data.Value, 1, 0);
									//-----------------------------
									#endregion
									break;
								case JoystickOffset.Y:
									#region [ Y軸－ ]
									//-----------------------------
									bButtonUpDown(data, data.Value, 2, 3);
									//-----------------------------
									#endregion
									#region [ Y軸＋ ]
									//-----------------------------
									bButtonUpDown(data, data.Value, 3, 2);
									//-----------------------------
									#endregion
									break;
								case JoystickOffset.Z:
									#region [ Z軸－ ]
									//-----------------------------
									bButtonUpDown(data, data.Value, 4, 5);
									//-----------------------------
									#endregion
									#region [ Z軸＋ ]
									//-----------------------------
									bButtonUpDown(data, data.Value, 5, 4);
									//-----------------------------
									#endregion
									break;
								// #24341 2011.3.12 yyagi: POV support
								// #26880 2011.12.6 yyagi: improve to support "pullup" of POV buttons
								case JoystickOffset.PointOfViewControllers0:
									#region [ POV HAT 4/8way ]
									POVの処理(0, data.Value);
									#endregion
									break;
								case JoystickOffset.PointOfViewControllers1:
									#region [ POV HAT 4/8way ]
									POVの処理(1, data.Value);
									#endregion
									break;
								case JoystickOffset.PointOfViewControllers2:
									#region [ POV HAT 4/8way ]
									POVの処理(2, data.Value);
									#endregion
									break;
								case JoystickOffset.PointOfViewControllers3:
									#region [ POV HAT 4/8way ]
									POVの処理(3, data.Value);
									#endregion
									break;
								default:
									#region [ ボタン ]
									//-----------------------------

									//for ( int i = 0; i < 32; i++ )
									if (data.Offset >= JoystickOffset.Buttons0 && data.Offset <= JoystickOffset.Buttons31)
									{
										int i = data.Offset - JoystickOffset.Buttons0;

										if ((data.Value & 0x80) != 0)
										{
											STInputEvent e = new STInputEvent()
											{
												nKey = 6 + i,
												b押された = true,
												b離された = false,
												nTimeStamp = CSoundManager.rcPerformanceTimer.nサウンドタイマーのシステム時刻msへの変換(data.Timestamp),
												nVelocity = CInputManager.n通常音量
											};
											listInputEvent.Add(e);

											bButtonState[6 + i] = true;
											bButtonPushDown[6 + i] = true;
										}
										else //if ( ( data.Value & 0x80 ) == 0 )
										{
											var ev = new STInputEvent()
											{
												nKey = 6 + i,
												b押された = false,
												b離された = true,
												nTimeStamp = CSoundManager.rcPerformanceTimer.nサウンドタイマーのシステム時刻msへの変換(data.Timestamp),
												nVelocity = CInputManager.n通常音量,
											};
											listInputEvent.Add(ev);

											bButtonState[6 + i] = false;
											bButtonPullUp[6 + i] = true;
										}
									}
									//-----------------------------
									#endregion
									break;
							}

							#region [ ローカル関数 ]
							void POVの処理(int p, int nPovDegree)
							{
								STInputEvent e = new STInputEvent();
								int nWay = (nPovDegree + 2250) / 4500;
								if (nWay == 8) nWay = 0;
								//Debug.WriteLine( "POVS:" + povs[ 0 ].ToString( CultureInfo.CurrentCulture ) + ", " +stevent.nKey );
								//Debug.WriteLine( "nPovDegree=" + nPovDegree );
								if (nPovDegree == -1)
								{
									e.nKey = 6 + 128 + nPovState[p];
									nPovState[p] = -1;
									//Debug.WriteLine( "POVS離された" + data.TimeStamp + " " + e.nKey );
									e.b押された = false;
									e.nVelocity = 0;
									bButtonState[e.nKey] = false;
									bButtonPullUp[e.nKey] = true;
								}
								else
								{
									nPovState[p] = nWay;
									e.nKey = 6 + 128 + nWay;
									e.b押された = true;
									e.nVelocity = CInputManager.n通常音量;
									bButtonState[e.nKey] = true;
									bButtonPushDown[e.nKey] = true;
									//Debug.WriteLine( "POVS押された" + data.TimeStamp + " " + e.nKey );
								}
								//e.nTimeStamp = data.TimeStamp;
								e.nTimeStamp = CSoundManager.rcPerformanceTimer.nサウンドタイマーのシステム時刻msへの変換(data.Timestamp);
								listInputEvent.Add(e);
							}
							#endregion
						}
					}
					//-----------------------------
					#endregion
				}
				else
				{
					#region [ b.状態入力 ]
					//-----------------------------
					JoystickState currentState = devJoystick.GetCurrentState();
					//if( Result.Last.IsSuccess && currentState != null )
					{
						#region [ X軸－ ]
						//-----------------------------
						if (currentState.X < -500)
						{
							if (bButtonState[0] == false)
							{
								STInputEvent ev = new STInputEvent()
								{
									nKey = 0,
									b押された = true,
									nTimeStamp = CSoundManager.rcPerformanceTimer.nシステム時刻, // 演奏用タイマと同じタイマを使うことで、BGMと譜面、入力ずれを防ぐ。
									nVelocity = CInputManager.n通常音量
								};
								listInputEvent.Add(ev);

								bButtonState[0] = true;
								bButtonPushDown[0] = true;
							}
						}
						else
						{
							if (bButtonState[0] == true)
							{
								STInputEvent ev = new STInputEvent()
								{
									nKey = 0,
									b押された = false,
									nTimeStamp = CSoundManager.rcPerformanceTimer.nシステム時刻, // 演奏用タイマと同じタイマを使うことで、BGMと譜面、入力ずれを防ぐ。
									nVelocity = CInputManager.n通常音量
								};
								listInputEvent.Add(ev);

								bButtonState[0] = false;
								bButtonPullUp[0] = true;
							}
						}
						//-----------------------------
						#endregion
						#region [ X軸＋ ]
						//-----------------------------
						if (currentState.X > 500)
						{
							if (bButtonState[1] == false)
							{
								STInputEvent ev = new STInputEvent()
								{
									nKey = 1,
									b押された = true,
									nTimeStamp = CSoundManager.rcPerformanceTimer.nシステム時刻, // 演奏用タイマと同じタイマを使うことで、BGMと譜面、入力ずれを防ぐ。
									nVelocity = CInputManager.n通常音量
								};
								listInputEvent.Add(ev);

								bButtonState[1] = true;
								bButtonPushDown[1] = true;
							}
						}
						else
						{
							if (bButtonState[1] == true)
							{
								STInputEvent event7 = new STInputEvent()
								{
									nKey = 1,
									b押された = false,
									nTimeStamp = CSoundManager.rcPerformanceTimer.nシステム時刻, // 演奏用タイマと同じタイマを使うことで、BGMと譜面、入力ずれを防ぐ。
									nVelocity = CInputManager.n通常音量
								};
								listInputEvent.Add(event7);

								bButtonState[1] = false;
								bButtonPullUp[1] = true;
							}
						}
						//-----------------------------
						#endregion
						#region [ Y軸－ ]
						//-----------------------------
						if (currentState.Y < -500)
						{
							if (bButtonState[2] == false)
							{
								STInputEvent ev = new STInputEvent()
								{
									nKey = 2,
									b押された = true,
									nTimeStamp = CSoundManager.rcPerformanceTimer.nシステム時刻, // 演奏用タイマと同じタイマを使うことで、BGMと譜面、入力ずれを防ぐ。
									nVelocity = CInputManager.n通常音量
								};
								listInputEvent.Add(ev);

								bButtonState[2] = true;
								bButtonPushDown[2] = true;
							}
						}
						else
						{
							if (bButtonState[2] == true)
							{
								STInputEvent ev = new STInputEvent()
								{
									nKey = 2,
									b押された = false,
									nTimeStamp = CSoundManager.rcPerformanceTimer.nシステム時刻, // 演奏用タイマと同じタイマを使うことで、BGMと譜面、入力ずれを防ぐ。
									nVelocity = CInputManager.n通常音量
								};
								listInputEvent.Add(ev);

								bButtonState[2] = false;
								bButtonPullUp[2] = true;
							}
						}
						//-----------------------------
						#endregion
						#region [ Y軸＋ ]
						//-----------------------------
						if (currentState.Y > 500)
						{
							if (bButtonState[3] == false)
							{
								STInputEvent ev = new STInputEvent()
								{
									nKey = 3,
									b押された = true,
									nTimeStamp = CSoundManager.rcPerformanceTimer.nシステム時刻, // 演奏用タイマと同じタイマを使うことで、BGMと譜面、入力ずれを防ぐ。
									nVelocity = CInputManager.n通常音量
								};
								listInputEvent.Add(ev);

								bButtonState[3] = true;
								bButtonPushDown[3] = true;
							}
						}
						else
						{
							if (bButtonState[3] == true)
							{
								STInputEvent ev = new STInputEvent()
								{
									nKey = 3,
									b押された = false,
									nTimeStamp = CSoundManager.rcPerformanceTimer.nシステム時刻, // 演奏用タイマと同じタイマを使うことで、BGMと譜面、入力ずれを防ぐ。
									nVelocity = CInputManager.n通常音量
								};
								listInputEvent.Add(ev);

								bButtonState[3] = false;
								bButtonPullUp[3] = true;
							}
						}
						//-----------------------------
						#endregion
						#region [ Z軸－ ]
						//-----------------------------
						if (currentState.Z < -500)
						{
							if (bButtonState[4] == false)
							{
								STInputEvent ev = new STInputEvent()
								{
									nKey = 4,
									b押された = true,
									nTimeStamp = CSoundManager.rcPerformanceTimer.nシステム時刻, // 演奏用タイマと同じタイマを使うことで、BGMと譜面、入力ずれを防ぐ。
									nVelocity = CInputManager.n通常音量
								};
								listInputEvent.Add(ev);

								bButtonState[4] = true;
								bButtonPushDown[4] = true;
							}
						}
						else
						{
							if (bButtonState[4] == true)
							{
								STInputEvent ev = new STInputEvent()
								{
									nKey = 4,
									b押された = false,
									nTimeStamp = CSoundManager.rcPerformanceTimer.nシステム時刻, // 演奏用タイマと同じタイマを使うことで、BGMと譜面、入力ずれを防ぐ。
									nVelocity = CInputManager.n通常音量
								};
								listInputEvent.Add(ev);

								bButtonState[4] = false;
								bButtonPullUp[4] = true;
							}
						}
						//-----------------------------
						#endregion
						#region [ Z軸＋ ]
						//-----------------------------
						if (currentState.Z > 500)
						{
							if (bButtonState[5] == false)
							{
								STInputEvent ev = new STInputEvent()
								{
									nKey = 5,
									b押された = true,
									nTimeStamp = CSoundManager.rcPerformanceTimer.nシステム時刻, // 演奏用タイマと同じタイマを使うことで、BGMと譜面、入力ずれを防ぐ。
									nVelocity = CInputManager.n通常音量
								};
								listInputEvent.Add(ev);

								bButtonState[5] = true;
								bButtonPushDown[5] = true;
							}
						}
						else
						{
							if (bButtonState[5] == true)
							{
								STInputEvent event15 = new STInputEvent()
								{
									nKey = 5,
									b押された = false,
									nTimeStamp = CSoundManager.rcPerformanceTimer.nシステム時刻, // 演奏用タイマと同じタイマを使うことで、BGMと譜面、入力ずれを防ぐ。
									nVelocity = CInputManager.n通常音量
								};
								listInputEvent.Add(event15);

								bButtonState[5] = false;
								bButtonPullUp[5] = true;
							}
						}
						//-----------------------------
						#endregion
						#region [ ボタン ]
						//-----------------------------
						bool bIsButtonPressedReleased = false;
						bool[] buttons = currentState.Buttons;
						for (int j = 0; (j < buttons.Length) && (j < 128); j++)
						{
							if (bButtonState[6 + j] == false && buttons[j])
							{
								STInputEvent item = new STInputEvent()
								{
									nKey = 6 + j,
									b押された = true,
									nTimeStamp = CSoundManager.rcPerformanceTimer.nシステム時刻, // 演奏用タイマと同じタイマを使うことで、BGMと譜面、入力ずれを防ぐ。
									nVelocity = CInputManager.n通常音量
								};
								listInputEvent.Add(item);

								bButtonState[6 + j] = true;
								bButtonPushDown[6 + j] = true;
								bIsButtonPressedReleased = true;
							}
							else if (bButtonState[6 + j] == true && !buttons[j])
							{
								STInputEvent item = new STInputEvent()
								{
									nKey = 6 + j,
									b押された = false,
									nTimeStamp = CSoundManager.rcPerformanceTimer.nシステム時刻, // 演奏用タイマと同じタイマを使うことで、BGMと譜面、入力ずれを防ぐ。
									nVelocity = CInputManager.n通常音量
								};
								listInputEvent.Add(item);

								bButtonState[6 + j] = false;
								bButtonPullUp[6 + j] = true;
								bIsButtonPressedReleased = true;
							}
						}
						//-----------------------------
						#endregion
						// #24341 2011.3.12 yyagi: POV support
						#region [ POV HAT 4/8way (only single POV switch is supported)]
						int[] povs = currentState.PointOfViewControllers;
						if (povs != null)
						{
							if (povs[0] >= 0)
							{
								int nPovDegree = povs[0];
								int nWay = (nPovDegree + 2250) / 4500;
								if (nWay == 8) nWay = 0;

								if (bButtonState[6 + 128 + nWay] == false)
								{
									STInputEvent stevent = new STInputEvent()
									{
										nKey = 6 + 128 + nWay,
										//Debug.WriteLine( "POVS:" + povs[ 0 ].ToString( CultureInfo.CurrentCulture ) + ", " +stevent.nKey );
										b押された = true,
										nTimeStamp = CSoundManager.rcPerformanceTimer.nシステム時刻, // 演奏用タイマと同じタイマを使うことで、BGMと譜面、入力ずれを防ぐ。
										nVelocity = CInputManager.n通常音量
									};
									listInputEvent.Add(stevent);

									bButtonState[stevent.nKey] = true;
									bButtonPushDown[stevent.nKey] = true;
								}
							}
							else if (bIsButtonPressedReleased == false) // #xxxxx 2011.12.3 yyagi 他のボタンが何も押され/離されてない＝POVが離された
							{
								int nWay = 0;
								for (int i = 6 + 0x80; i < 6 + 0x80 + 8; i++)
								{                                           // 離されたボタンを調べるために、元々押されていたボタンを探す。
									if (bButtonState[i] == true)   // DirectInputを直接いじるならこんなことしなくて良いのに、あぁ面倒。
									{                                       // この処理が必要なために、POVを1個しかサポートできない。無念。
										nWay = i;
										break;
									}
								}
								if (nWay != 0)
								{
									STInputEvent stevent = new STInputEvent()
									{
										nKey = nWay,
										b押された = false,
										nTimeStamp = CSoundManager.rcPerformanceTimer.nシステム時刻, // 演奏用タイマと同じタイマを使うことで、BGMと譜面、入力ずれを防ぐ。
										nVelocity = 0
									};
									listInputEvent.Add(stevent);

									bButtonState[nWay] = false;
									bButtonPullUp[nWay] = true;
								}
							}
						}
						#endregion
					}
					//-----------------------------
					#endregion
				}
			}
		}

		public bool bKeyPressed(int nButton)
		{
			return bButtonPushDown[nButton];
		}
		public bool bKeyPressing(int nButton)
		{
			return bButtonState[nButton];
		}
		public bool bKeyReleased(int nButton)
		{
			return bButtonPullUp[nButton];
		}
		public bool bKeyReleasing(int nButton)
		{
			return !bButtonState[nButton];
		}
		//-----------------
		#endregion

		#region [ IDisposable 実装 ]
		//-----------------
		public void Dispose()
		{
			if (!bDispose完了済み)
			{
				if (devJoystick != null)
				{
					devJoystick.Dispose();
					devJoystick = null;
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
		private bool[] bButtonPullUp = new bool[0x100];
		private bool[] bButtonPushDown = new bool[0x100];
		private bool[] bButtonState = new bool[0x100];      // 0-5: XYZ, 6 - 0x128+5: buttons, 0x128+6 - 0x128+6+8: POV/HAT
		private int[] nPovState = new int[4];                   // POVの現在値を保持
		private bool bDispose完了済み;
		private Joystick devJoystick;
		//private CTimer timer;

		private void bButtonUpDown(JoystickUpdate data, int axisdata, int target, int contrary) // #26871 2011.12.3 軸の反転に対応するためにリファクタ
		{
			int targetsign = (target < contrary) ? -1 : 1;
			if (Math.Abs(axisdata) > 500 && (targetsign == Math.Sign(axisdata)))            // 軸の最大値の半分を超えていて、かつ
			{
				if (bDoUpDownCore(target, data, false))                                         // 直前までは超えていなければ、今回ON
				{
					//Debug.WriteLine( "X-ON " + data.TimeStamp + " " + axisdata );
				}
				else
				{
					//Debug.WriteLine( "X-ONx " + data.TimeStamp + " " + axisdata );
				}
				bDoUpDownCore(contrary, data, true);                                                // X軸+ == ON から X軸-のONレンジに来たら、X軸+はOFF
			}
			else if ((axisdata <= 0 && targetsign <= 0) || (axisdata >= 0 && targetsign >= 0))  // 軸の最大値の半分を超えておらず、かつ  
			{
				//Debug.WriteLine( "X-OFF? " + data.TimeStamp + " " + axisdata );
				if (bDoUpDownCore(target, data, true))                                          // 直前までは超えていたのならば、今回OFF
				{
					//Debug.WriteLine( "X-OFF " + data.TimeStamp + " " + axisdata );
				}
				else if (bDoUpDownCore(contrary, data, true))                                   // X軸+ == ON から X軸-のOFFレンジにきたら、X軸+はOFF
				{
					//Debug.WriteLine( "X-OFFx " + data.TimeStamp + " " + axisdata );
				}
			}
		}

		/// <summary>
		/// 必要に応じて軸ボタンの上げ下げイベントを発生する
		/// </summary>
		/// <param name="target">軸ボタン番号 0=-X 1=+X ... 5=+Z</param>
		/// <param name="data"></param>
		/// <param name="currentMode">直前のボタン状態 true=押されていた</param>
		/// <returns>上げ下げイベント発生時true</returns>
		private bool bDoUpDownCore(int target, JoystickUpdate data, bool lastMode)
		{
			if (bButtonState[target] == lastMode)
			{
				STInputEvent e = new STInputEvent()
				{
					nKey = target,
					b押された = !lastMode,
					nTimeStamp = CSoundManager.rcPerformanceTimer.nサウンドタイマーのシステム時刻msへの変換(data.Timestamp),
					nVelocity = (lastMode) ? 0 : CInputManager.n通常音量
				};
				listInputEvent.Add(e);

				bButtonState[target] = !lastMode;
				if (lastMode)
				{
					bButtonPullUp[target] = true;
				}
				else
				{
					bButtonPushDown[target] = true;
				}
				return true;
			}
			return false;
		}
		//-----------------
		#endregion
	}
}
