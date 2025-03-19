namespace FDK;

public class CInputMIDI : IInputDevice, IDisposable
{
	// プロパティ

	public IntPtr hMidiIn;
	public List<STInputEvent> listEventBuffer;


	// コンストラクタ

	public CInputMIDI( uint nID )
	{
		hMidiIn = IntPtr.Zero;
		listEventBuffer = new List<STInputEvent>( 32 );
		listInputEvent = new List<STInputEvent>( 32 );
		eInputDeviceType = EInputDeviceType.MidiIn;
		GUID = "";
		ID = (int) nID;
		strDeviceName = "";    // CInput管理で初期化する
	}


	// メソッド

	public void tメッセージからMIDI信号のみ受信( uint wMsg, int dwInstance, int dwParam1, int dwParam2, long n受信システム時刻 )
	{
		if( wMsg == CWin32.MIM_DATA )
		{
			int nMIDIevent = dwParam1 & 0xF0;
			int nPara1 = ( dwParam1 >> 8 ) & 0xFF;
			int nPara2 = ( dwParam1 >> 16 ) & 0xFF;

// Trace.TraceInformation( "MIDIevent={0:X2} para1={1:X2} para2={2:X2}", nMIDIevent, nPara1, nPara2 );
			
			if( ( nMIDIevent == 0x90 ) && ( nPara2 != 0 ) )
			{
				STInputEvent item = new STInputEvent();
				item.nKey = nPara1;
				item.b押された = true;
				item.nTimeStamp = n受信システム時刻;
				item.nVelocity = nPara2;
				listEventBuffer.Add( item );
			}
		}
	}

	#region [ IInputDevice 実装 ]
	//-----------------
	public EInputDeviceType eInputDeviceType { get; private set; }
	public string GUID { get; private set; }
	public int ID { get; private set; }
	public List<STInputEvent> listInputEvent { get; private set; }
	public string strDeviceName { get; set; }

	public void tPolling( bool bWindowがアクティブ中, bool bバッファ入力を使用する )
	{
		// this.listInputEvent = new List<STInputEvent>( 32 );
		listInputEvent.Clear();								// #xxxxx 2012.6.11 yyagi; To optimize, I removed new();

		for( int i = 0; i < listEventBuffer.Count; i++ )
			listInputEvent.Add( listEventBuffer[ i ] );

		listEventBuffer.Clear();
	}
	public bool bKeyPressed( int nKey )
	{
		foreach( STInputEvent event2 in listInputEvent )
		{
			if( ( event2.nKey == nKey ) && event2.b押された )
			{
				return true;
			}
		}
		return false;
	}
	public bool bKeyPressing( int nKey )
	{
		return false;
	}
	public bool bKeyReleased( int nKey )
	{
		return false;
	}
	public bool bKeyReleasing( int nKey )
	{
		return false;
	}
	//-----------------
	#endregion

	#region [ IDisposable 実装 ]
	//-----------------
	public void Dispose()
	{
		if ( listEventBuffer != null )
		{
			listEventBuffer = null;
		}
		if ( listInputEvent != null )
		{
			listInputEvent = null;
		}
	}
	//-----------------
	#endregion
}