using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using DTXMania.Core.OpenGL;
using DTXMania.UI;

namespace DTXMania.Core;

internal class Program
{
	//-----------------------------
	private static Mutex concurrencyMutex;
	private static bool missingDll = false;

	private static void tCheckIfDllExists(string strDllPath, string strDllNotFoundErrorJp, string strDllNotFoundErrorEn,
		bool bLoadDllCheck = false)
	{
		string errorString = CDTXMania.isJapanese ? strDllNotFoundErrorJp : strDllNotFoundErrorEn;
		if (bLoadDllCheck)
		{
			IntPtr hModule = LoadLibrary(strDllPath); // 実際にLoadDll()してチェックする
			// if (hModule == IntPtr.Zero)
			{
				MessageBox.Show(errorString, "DTXMania runtime error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				missingDll = true;
			}

			FreeLibrary(hModule);
		}
		else
		{
			// 単純にファイルの存在有無をチェックするだけ (プロジェクトで「参照」していたり、アンマネージドなDLLが暗黙リンクされるものはこちら)
			string path = Path.Combine(Directory.GetCurrentDirectory(), strDllPath);
			if (!File.Exists(path))
			{
				MessageBox.Show(errorString, "DTXMania runtime error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				missingDll = true;
			}
		}
	}

	#region [DllImport]
	[DllImport( "kernel32", CharSet = CharSet.Unicode, SetLastError = true )]
	internal static extern void FreeLibrary( IntPtr hModule );

	[DllImport( "kernel32", CharSet = CharSet.Unicode, SetLastError = true )]
	internal static extern IntPtr LoadLibrary( string lpFileName );

	[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
	internal static extern bool SetDllDirectory(string lpPathName);
	#endregion

	[STAThread]
	private static void Main()
	{
		//prevents two instances from DTXMania from running at the same time
		concurrencyMutex = new Mutex(false, "DTXManiaMutex");

		CDTXMania.SetLanguage(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ja");

		if (!concurrencyMutex.WaitOne(0, false))
		{
			//display message that another instance is already running, and ask the user if they want to terminate it to start a new one
			var result = MessageBox.Show(
				CDTXMania.isJapanese
					? "DTXMania は既に起動しています。\n新しく起動しますか？"
					: "DTXMania is already running.\nDo you want to start a new instance?",
				"DTXMania",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Question
			);

			if (result == DialogResult.Yes)
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

		string newLine = Environment.NewLine;

		Trace.WriteLine("Current Directory: " + Environment.CurrentDirectory);
		Trace.WriteLine("EXEのあるフォルダ: " + Path.GetDirectoryName(Application.ExecutablePath));

		tCheckIfDllExists("dll\\FDK.dll",
			"FDK.dll またはその依存するdllが存在しません。" + newLine + "DTXManiaをダウンロードしなおしてください。",
			"FDK.dll, or its depended DLL, is not found." + newLine + "Please download DTXMania again.");

		tCheckIfDllExists("dll\\bass.dll",
			"bass.dll が存在しません。" + newLine + "DTXManiaをダウンロードしなおしてください。",
			"baas.dll is not found." + newLine + "Please download DTXMania again.");

		tCheckIfDllExists("dll\\Bass.Net.dll",
			"Bass.Net.dll が存在しません。" + newLine + "DTXManiaをダウンロードしなおしてください。",
			"Bass.Net.dll is not found." + newLine + "Please download DTXMania again.");

		tCheckIfDllExists("dll\\bassmix.dll",
			"bassmix.dll を読み込めません。bassmix.dll か bass.dll が存在しません。" + newLine + "DTXManiaをダウンロードしなおしてください。",
			"bassmix.dll is not loaded. bassmix.dll or bass.dll must not exist." + newLine +
			"Please download DTXMania again.");

		tCheckIfDllExists("dll\\bassasio.dll",
			"bassasio.dll を読み込めません。bassasio.dll か bass.dll が存在しません。" + newLine + "DTXManiaをダウンロードしなおしてください。",
			"bassasio.dll is not loaded. bassasio.dll or bass.dll must not exist." + newLine +
			"Please download DTXMania again.");

		tCheckIfDllExists("dll\\basswasapi.dll",
			"basswasapi.dll を読み込めません。basswasapi.dll か bass.dll が存在しません。" + newLine +
			"DTXManiaをダウンロードしなおしてください。",
			"basswasapi.dll is not loaded. basswasapi.dll or bass.dll must not exist." + newLine +
			"Please download DTXMania again.");

		tCheckIfDllExists("dll\\bass_fx.dll",
			"bass_fx.dll を読み込めません。bass_fx.dll か bass.dll が存在しません。" + newLine + "DTXManiaをダウンロードしなおしてください。",
			"bass_fx.dll is not loaded. bass_fx.dll or bass.dll must not exist." + newLine +
			"Please download DTXMania again.");

		tCheckIfDllExists("dll\\DirectShowLib-2005.dll",
			"DirectShowLib-2005.dll が存在しません。" + newLine + "DTXManiaをダウンロードしなおしてください。",
			"DirectShowLib-2005.dll is not found." + newLine + "Please download DTXMania again.");

		if (missingDll)
		{
			//show messagebox and ask if the user still wants to continue
			var result = MessageBox.Show(
				CDTXMania.isJapanese
					? "必要なDLLが見つかりませんでした。\nDTXManiaを起動しますか？"
					: "Some required DLLs are missing.\nDo you want to start DTXMania?",
				"DTXMania",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Question
			);

			if (result != DialogResult.Yes)
			{
				return;
			}
		}

		if (!CDTXMania.isJapanese)
		{
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
		}

		string path = Path.GetDirectoryName(Application.ExecutablePath);
		/* For future 64bit migration
		SetDllDirectory(null);
		if (Environment.Is64BitProcess)
		{
			SetDllDirectory(Path.Combine(path, @"dll\x64"));
		}
		else */
		{
			SetDllDirectory(Path.Combine(path, @"dll"));
		}

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
			DisplayControlsWindow.host = host;
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
			MessageBox.Show( e.ToString(), "DTXMania Error", MessageBoxButtons.OK, MessageBoxIcon.Error );	// #23670 2011.2.28 yyagi to show error dialog
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