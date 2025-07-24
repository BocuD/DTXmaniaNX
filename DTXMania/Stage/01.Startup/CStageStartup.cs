using System.Diagnostics;
using DTXMania.Core;
using DTXMania.UI.DynamicElements;
using FDK;

namespace DTXMania;

internal class CStageStartup : CStage
{
	// コンストラクタ

	public CStageStartup()
	{
		eStageID = EStage.Startup_1;
		bActivated = false;
	}

	public List<string> startupScreenConsole;

	// CStage 実装

	public override void InitializeBaseUI()
	{
		
	}
	
	public override void InitializeDefaultUI()
	{
		
	}

	public override void OnActivate()
	{
		Trace.TraceInformation( "起動ステージを活性化します。" );
		Trace.Indent();
		try
		{
			startupScreenConsole = new List<string>();
			ePhaseID = EPhase.Common_DefaultState;
			
			dynamicStringSources["Version"] = new DynamicStringSource(() => CDTXMania.VERSION_DISPLAY);

			base.OnActivate();
			Trace.TraceInformation( "起動ステージの活性化を完了しました。" );
		}
		finally
		{
			Trace.Unindent();
		}
	}
	public override void OnDeactivate()
	{
		Trace.TraceInformation( "起動ステージを非活性化します。" );
		Trace.Indent();
		try
		{
			startupScreenConsole = null;
			base.OnDeactivate();
			Trace.TraceInformation( "起動ステージの非活性化を完了しました。" );
		}
		finally
		{
			Trace.Unindent();
		}
	}
	public override void OnManagedCreateResources()
	{
		if(bActivated)
		{
			txBackground = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\1_background.jpg"), false);
			base.OnManagedCreateResources();
		}
	}
	public override void OnManagedReleaseResources()
	{
		if(bActivated)
		{
			CDTXMania.tReleaseTexture(ref txBackground);
			base.OnManagedReleaseResources();
		}
	}

	public override void FirstUpdate()
	{
		startupScreenConsole.Add("DTXMania powered by YAMAHA Silent Session Drums\n");
		startupScreenConsole.Add("Release: " + CDTXMania.VERSION + " [" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version + "]");

		ePhaseID = EPhase.起動0_システムサウンドを構築;

		Trace.TraceInformation("0) システムサウンドを構築します。");
		Trace.Indent();

		try
		{
			CDTXMania.Skin.bgmTitleScreen.tPlay();
                
			CDTXMania.Skin.ReloadSkin();
                
			lock (startupScreenConsole)
			{
				startupScreenConsole.Add("Loading system sounds ... OK ");
			}
		}
		finally
		{
			Trace.Unindent();
		}
	}

	public override int OnUpdateAndDraw()
	{
		if (!bActivated) return 0;

		base.OnUpdateAndDraw();
		
		txBackground?.tDraw2D( CDTXMania.app.Device, 0, 0 );

		#region [ this.str現在進行中 の決定 ]
		//-----------------
		switch( ePhaseID )
		{
			case EPhase.起動0_システムサウンドを構築:
				str現在進行中 = "Loading system sounds ... ";
				break;

			case EPhase.起動7_完了:
				str現在進行中 = "Setup done.";
				break;
		}
		//-----------------
		#endregion
		#region [ this.list進行文字列＋this.現在進行中 の表示 ]
		//-----------------
		lock( startupScreenConsole )
		{
			int x = 0;
			int y = 0;
			foreach( string str in startupScreenConsole )
			{
				CDTXMania.actDisplayString.tPrint( x, y, CCharacterConsole.EFontType.AshThin, str );
				y += 14;
			}
			CDTXMania.actDisplayString.tPrint( x, y, CCharacterConsole.EFontType.AshThin, str現在進行中 );
		}
		//-----------------
		#endregion
		
		return 1;
	}


	// Other

	#region [ private ]
	//-----------------
	private string str現在進行中 = "";
	private CTexture? txBackground;
	#endregion
}