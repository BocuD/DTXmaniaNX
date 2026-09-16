using System.Diagnostics;
using System.Globalization;
using System.Text;
using DTXMania.Core.Audio;
using DTXMania.Core.OpenGL;
using DTXMania.Core.Video;
using NativeFileDialog.Extended;

namespace DTXMania.Core;

internal class Program
{
	//-----------------------------
	private static Mutex concurrencyMutex;

	[STAThread]
	private static void Main()
	{
		Framework.PerformanceRun.ReadCommandLine();

		//prevents two instances from DTXMania from running at the same time
		concurrencyMutex = new Mutex(false, "DTXManiaMutex");

		CDTXMania.SetLanguage(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ja");

		if (!concurrencyMutex.WaitOne(0, false))
		{
			//display message that another instance is already running, and ask the user if they want to terminate it to start a new one
			bool startNew = NativeMessageBox.Ask("DTXMania",
				CDTXMania.isJapanese
					? "DTXMania は既に起動しています。\n新しく起動しますか？"
					: "DTXMania is already running.\nDo you want to start a new instance?");

			if (startNew)
			{
				//terminate all other instances
				Process currentProcess = Process.GetCurrentProcess();
				Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);
				foreach (Process process in processes)
				{
					if (process.Id != currentProcess.Id)
					{
						process.Kill();
					}
				}
			}
			else
			{
				return;
			}
		}

		Trace.WriteLine("Current Directory: " + Environment.CurrentDirectory);
		Trace.WriteLine("Executable directory: " + AppContext.BaseDirectory);

		string[] missingLibraries = BassRuntime.LibraryFiles.Concat(FFmpegCore.LibraryFiles)
			.Where(file => !File.Exists(file))
			.Select(file => Path.GetRelativePath(AppContext.BaseDirectory, file))
			.ToArray();

		if (missingLibraries.Length > 0)
		{
			string list = string.Join(Environment.NewLine, missingLibraries);
			Trace.TraceError("Missing libraries:" + Environment.NewLine + list);

			//show messagebox and ask if the user still wants to continue
			bool start = NativeMessageBox.Ask("DTXMania runtime error",
				CDTXMania.isJapanese
					? $"必要なライブラリが見つかりませんでした。DTXManiaをダウンロードしなおしてください。\n\n{list}\n\nDTXManiaを起動しますか？"
					: $"Some required libraries are missing. Please download DTXMania again.\n\n{list}\n\nDo you want to start DTXMania?",
				MessageBoxIcon.Error);

			if (!start)
			{
				return;
			}
		}

		if (!CDTXMania.isJapanese)
		{
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
		}

		BassRuntime.ResolveLibraries();

		//NuGet's nfd is arm64 only on macOS
		NativeLibraries.ResolveFrom(typeof(NFD).Assembly, "nfd");

		//set up support for shift-jis
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

#if !DEBUG
		try
#endif
		{
			// using (CDTXMania mania = new())
			//  	mania.Run();
			
			DTXManiaGL game = new();
			//CubeRenderer game = new();
			GlfwOpenGlHost host = new(game);
			host.Run();

			Trace.WriteLine("");
			Trace.WriteLine("遊んでくれてありがとう！");
		}
#if !DEBUG
		catch( Exception e )
		{
			Trace.WriteLine( "" );
			Trace.Write( e.ToString() );
			Trace.WriteLine( "" );
			Trace.WriteLine( "エラーだゴメン！（涙" );
			NativeMessageBox.Show("DTXMania Error", e.ToString());	// #23670 2011.2.28 yyagi to show error dialog
		}
#endif
		// END #24606 2011.03.08 from
		// END #23670 2010.11.13 from

		if (Trace.Listeners.Count > 1)
			Trace.Listeners.RemoveAt(1);

		concurrencyMutex.ReleaseMutex();
		concurrencyMutex = null;
	}
}