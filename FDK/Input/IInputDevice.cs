namespace FDK;

public interface IInputDevice : IDisposable
{
	EInputDeviceType eInputDeviceType
	{
		get;
	}
	string GUID 
	{
		get; 
	}
	int ID 
	{
		get;
	}
	List<STInputEvent> listInputEvent
	{
		get;
	}
	string strDeviceName
	{
		get;
	}

	// Method interfaces

	void tPolling( bool isWindowActive, bool useBufferedInput );  // tポーリング
	bool bKeyPressed(int nKey);  // bキーが押された
	bool bKeyPressing(int nKey);  // bキーが押されている
	bool bKeyReleased( int nKey );  // bキーが離された
	bool bKeyReleasing( int nKey );  // bキーが離されている
}