using System.Diagnostics;
using DTXMania.Core;


namespace DTXMania;

/// <summary>
/// box.defによるスキン変更時に一時的に遷移する、スキン画像の一切無いステージ。
/// </summary>
internal class CStageChangeSkin : CStage
{
	// コンストラクタ

	public CStageChangeSkin()
	{
		eStageID = EStage.ChangeSkin_9;
		bNotActivated = true;
	}


	// CStage 実装

	public override void InitializeBaseUI()
	{
		
	}

	public override void OnActivate()
	{
		Trace.TraceInformation( "スキン変更ステージを活性化します。" );
		Trace.Indent();
		try
		{
			base.OnActivate();
			Trace.TraceInformation( "スキン変更ステージの活性化を完了しました。" );
		}
		finally
		{
			Trace.Unindent();
		}
	}
	public override void OnDeactivate()
	{
		Trace.TraceInformation( "スキン変更ステージを非活性化します。" );
		Trace.Indent();
		try
		{
			base.OnDeactivate();
			Trace.TraceInformation( "スキン変更ステージの非活性化を完了しました。" );
		}
		finally
		{
			Trace.Unindent();
		}
	}
	public override void OnManagedCreateResources()
	{
		if( !bNotActivated )
		{
			base.OnManagedCreateResources();
		}
	}
	public override void OnManagedReleaseResources()
	{
		if( !bNotActivated )
		{
			base.OnManagedReleaseResources();
		}
	}
	public override int OnUpdateAndDraw()
	{
		if (bNotActivated) return 0;

		base.OnUpdateAndDraw();

		if ( bJustStartedUpdate )
		{
			bJustStartedUpdate = false;
			return 0;
		}

		//スキン変更処理
		tChangeSkinMain();
		return 1;
	}
	public void tChangeSkinMain()
	{
		Trace.TraceInformation( "スキン変更:" + CDTXMania.Skin.GetCurrentSkinSubfolderFullName( false ) );

		CDTXMania.actDisplayString.OnDeactivate();

		CDTXMania.Skin.PrepareReloadSkin();
		CDTXMania.Skin.ReloadSkin();

		CDTXMania.actDisplayString.OnActivate();
	}
}