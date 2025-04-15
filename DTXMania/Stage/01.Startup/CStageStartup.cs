using System.Diagnostics;
using DTXMania.Core;
using DTXMania.UI.DynamicElements;
using DTXUIRenderer;
using FDK;

namespace DTXMania;

internal class CStageStartup : CStage
{
	// コンストラクタ

	public CStageStartup()
	{
		eStageID = EStage.Startup_1;
		bNotActivated = true;
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
			if ( es != null )
			{
				if ( es.thDTXFileEnumerate is { IsAlive: true } )
				{
					Trace.TraceWarning( "リスト構築スレッドを強制停止します。" );
					es.thDTXFileEnumerate.Abort();
					es.thDTXFileEnumerate.Join();
				}
			}
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
		if(!bNotActivated)
		{
			txBackground = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\1_background.jpg"), false);
			base.OnManagedCreateResources();
		}
	}
	public override void OnManagedReleaseResources()
	{
		if(!bNotActivated)
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
			CDTXMania.Skin.bgm起動画面.tPlay();
                
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
					
		es = new CEnumSongs();
		if (!CDTXMania.bCompactMode)
		{
			es.StartEnumFromCacheStartup(this);
		}
	}

	public override int OnUpdateAndDraw()
	{
		if (bNotActivated) return 0;

		base.OnUpdateAndDraw();
		
		txBackground?.tDraw2D( CDTXMania.app.Device, 0, 0 );

		#region [ this.str現在進行中 の決定 ]
		//-----------------
		switch( ePhaseID )
		{
			case EPhase.起動0_システムサウンドを構築:
				str現在進行中 = "Loading system sounds ... ";
				break;

			case EPhase.起動00_songlistから曲リストを作成する:
				str現在進行中 = "Loading songlist.db ... ";
				break;

			case EPhase.起動1_SongsDBからスコアキャッシュを構築:
				str現在進行中 = "Loading songs.db ... ";
				break;

			case EPhase.起動2_曲を検索してリストを作成する:
				str現在進行中 = $"Enumerating songs ... {es.SongManager.nNbScoresFound}";
				break;

			case EPhase.起動3_スコアキャッシュをリストに反映する:
				str現在進行中 = $"Loading score properties from songs.db ... {es.SongManager.nNbScoresFromScoreCache}/{es.SongManager.nNbScoresFound}";
				break;

			case EPhase.起動4_スコアキャッシュになかった曲をファイルから読み込んで反映する:
				str現在進行中 = $"Loading score properties from files ... {es.SongManager.nNbScoresFromFile}/{es.SongManager.nNbScoresFound - es.SongManager.nNbScoresFromScoreCache}";
				break;

			case EPhase.起動5_曲リストへ後処理を適用する:
				str現在進行中 = "Building songlists ... ";
				break;

			case EPhase.起動6_スコアキャッシュをSongsDBに出力する:
				str現在進行中 = "Saving songs.db ... ";
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

		if( es is { IsSongListEnumCompletelyDone: true } )
		{
			CDTXMania.SongManager = es.SongManager;
			return 1;
		}
		return 0;
	}


	// Other

	#region [ private ]
	//-----------------
	private string str現在進行中 = "";
	private CTexture? txBackground;
	private CEnumSongs es;
		
	#endregion
}