using System.Diagnostics;
using DTXMania.Core;
using FDK;

namespace DTXMania;

internal class CStageEnd : CStage
{
	// Constructor

	public CStageEnd()
	{
		eStageID = EStage.End_8;
		ePhaseID = EPhase.Common_DefaultState;
		bActivated = false;
	}


	// CStage 実装

	public override void InitializeBaseUI()
	{
		
	}

	public override void InitializeDefaultUI()
	{
		
	}
	
	public override void OnActivate()
	{
		Trace.TraceInformation( "終了ステージを活性化します。" );
		Trace.Indent();
		try
		{
			ct時間稼ぎ = new CCounter();
			base.OnActivate();
		}
		finally
		{
			Trace.TraceInformation( "終了ステージの活性化を完了しました。" );
			Trace.Unindent();
		}
	}
	public override void OnDeactivate()
	{
		Trace.TraceInformation( "終了ステージを非活性化します。" );
		Trace.Indent();
		try
		{
			base.OnDeactivate();
		}
		finally
		{
			Trace.TraceInformation( "終了ステージの非活性化を完了しました。" );
			Trace.Unindent();
		}
	}
	public override void OnManagedCreateResources()
	{
		if( bActivated )
		{
			txBackground = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\9_background.jpg" ), false );
			base.OnManagedCreateResources();
		}
	}
	public override void OnManagedReleaseResources()
	{
		if( bActivated )
		{
			CDTXMania.tReleaseTexture( ref txBackground );
			base.OnManagedReleaseResources();
		}
	}

	public override void FirstUpdate()
	{
		CDTXMania.Skin.soundGameEnd.tPlay();
		ct時間稼ぎ.tStart( 0, 1, 0x3e8, CDTXMania.Timer );
	}

	public override int OnUpdateAndDraw()
	{
		if (!bActivated) return 0;
		
		base.OnUpdateAndDraw();
		
		ct時間稼ぎ.tUpdate();
		if( ct時間稼ぎ.bReachedEndValue && !CDTXMania.Skin.soundGameEnd.b再生中 )
		{
			return 1;
		}
		if( txBackground != null )
		{
			txBackground.tDraw2D( CDTXMania.app.Device, 0, 0 );
		}
		return 0;
	}

	#region [ private ]
	//-----------------
	private CCounter ct時間稼ぎ;
	private CTexture txBackground;
	//-----------------
	#endregion
}