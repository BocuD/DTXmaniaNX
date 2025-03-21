using System.Runtime.InteropServices;

namespace FDK;

public static class CSendMessage
{

	[DllImport( "USER32.dll" )]
	static extern uint SendMessage( IntPtr window, int msg, IntPtr wParam, ref SampleFramework.COPYDATASTRUCT lParam );
	
		
	public static uint sendmessage( IntPtr MainWindowHandle, IntPtr FromWindowHandle, string arg)
	{
		uint len = (uint)arg.Length;

		SampleFramework.COPYDATASTRUCT cds;
		cds.dwData = IntPtr.Zero;       // 使用しない
		cds.lpData = Marshal.StringToHGlobalUni(arg);           // テキストのポインターをセット
		cds.cbData = (len + 1) * 2; // 長さをセット

		//文字列を送る
		uint result = SendMessage(MainWindowHandle, SampleFramework.WindowConstants.WM_COPYDATA, FromWindowHandle, ref cds);

		Marshal.FreeHGlobal(cds.lpData);

		return result;
	}
}