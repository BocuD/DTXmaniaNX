using System.Runtime.InteropServices;

namespace FDK;

public class CCommon
{
	// 解放

	public static void tDispose<T>( ref T obj )
	{
		if( obj == null )
			return;

		var d = obj as IDisposable;

		if( d != null )
		{
			d.Dispose();
			obj = default( T );
		}
	}
	public static void tDispose<T>( T obj)  // tDisposeする
	{
		if( obj == null )
			return;

		var d = obj as IDisposable;

		if( d != null )
			d.Dispose();
	}
	public static void tReleaseComObject<T>( ref T obj )
	{
		if( obj != null )
		{
			try
			{
				Marshal.ReleaseComObject( obj );
			}
			catch
			{
				// COMがマネージドコードで書かれている場合、ReleaseComObject は例外を発生させる。
				// http://www.infoq.com/jp/news/2010/03/ReleaseComObject-Dangerous
			}

			obj = default( T );
		}
	}

	public static void tRunGarbageCollector()
	{
		GC.Collect();					// アクセス不可能なオブジェクトを除去し、ファイナライぜーション実施。
		GC.WaitForPendingFinalizers();	// ファイナライゼーションが終わるまでスレッドを待機。
		GC.Collect();					// ファイナライズされたばかりのオブジェクトに関連するメモリを開放。

		// 出展: http://msdn.microsoft.com/ja-jp/library/ms998547.aspx#scalenetchapt05_topic10
	}
}