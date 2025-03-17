using System.Diagnostics;

namespace FDK
{
	public class CTraceLogListener : TraceListener
	{
		public CTraceLogListener( StreamWriter stream )
		{
			streamWriter = stream;
		}

		public override void Flush()
		{
			if( streamWriter != null )
			{
				try
				{
					streamWriter.Flush();
				}
				catch( ObjectDisposedException )
				{
				}
			}
		}
		public override void TraceEvent( TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message )
		{
			if( streamWriter != null )
			{
				try
				{
					tイベント種別を出力する( eventType );
					tインデントを出力する();
					streamWriter.WriteLine( message );
				}
				catch( ObjectDisposedException )
				{
				}
			}
		}
		public override void TraceEvent( TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args )
		{
			if( streamWriter != null )
			{
				try
				{
					tイベント種別を出力する( eventType );
					tインデントを出力する();
					streamWriter.WriteLine( string.Format( format, args ) );
				}
				catch( ObjectDisposedException )
				{
				}
			}
		}
		public override void Write( string message )
		{
			if( streamWriter != null )
			{
				try
				{
					streamWriter.Write( message );
				}
				catch( ObjectDisposedException )
				{
				}
			}
		}
		public override void WriteLine( string message )
		{
			if( streamWriter != null )
			{
				try
				{
					streamWriter.WriteLine( message );
				}
				catch( ObjectDisposedException )
				{
				}
			}
		}

		protected override void Dispose( bool disposing )
		{
			if( streamWriter != null )
			{
				try
				{
					streamWriter.Close();
				}
				catch
				{
				}
				streamWriter = null;
			}
			base.Dispose( disposing );
		}

		#region [ private ]
		//-----------------
		private StreamWriter streamWriter;

		private void tイベント種別を出力する( TraceEventType eventType )
		{
			if( streamWriter != null )
			{
				try
				{
					var now = DateTime.Now;
					streamWriter.Write( string.Format( "{0:D4}/{1:D2}/{2:D2} {3:D2}:{4:D2}:{5:D2}.{6:D3} ", new object[] { now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, now.Millisecond } ) );
					switch( eventType )
					{
						case TraceEventType.Error:
							streamWriter.Write( "[ERROR] " );
							return;

						case ( TraceEventType.Error | TraceEventType.Critical ):
							return;

						case TraceEventType.Warning:
							streamWriter.Write( "[WARNING] " );
							return;

						case TraceEventType.Information:
							break;

						default:
							return;
					}
					streamWriter.Write( "[INFO] " );
				}
				catch( ObjectDisposedException )
				{
				}
			}
		}
		private void tインデントを出力する()
		{
			if( ( streamWriter != null ) && ( IndentLevel > 0 ) )
			{
				try
				{
					for( int i = 0; i < IndentLevel; i++ )
						streamWriter.Write( "    " );
				}
				catch( ObjectDisposedException )
				{
				}
			}
		}
		//-----------------
		#endregion
	}
}
