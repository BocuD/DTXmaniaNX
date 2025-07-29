using DiscordRPC;
using DTXMania.Core;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using FDK;
using SharpDX;

namespace DTXMania;

public abstract class CStage : CActivity
{
	/// <summary>
	/// The presence used to indicate the user's activity within this stage, or <see langword="null"/> if there is none.
	/// </summary>
	protected virtual RichPresence Presence => new CDTXRichPresence
	{
		State = "In Menu",
		Details = "Idle",
	};

	internal EStage eStageID;
	public enum EStage
	{
		DoNothing_0,
		Startup_1,
		Title_2,
		Config_3,
		SongSelection_4,
		SongLoading_5,
		Performance_6,
		Result_7,
		End_8,
		ChangeSkin_9						// #28195 2011.5.4 yyagi
	}
		
	internal EPhase ePhaseID;
	public enum EPhase
	{
		Common_DefaultState,
		Common_FadeIn,
		Common_FadeOut,
		Common_EndStatus,
		起動0_システムサウンドを構築,
		起動00_songlistから曲リストを作成する,
		起動1_SongsDBからスコアキャッシュを構築,
		起動2_曲を検索してリストを作成する,
		起動3_スコアキャッシュをリストに反映する,
		起動4_スコアキャッシュになかった曲をファイルから読み込んで反映する,
		起動5_曲リストへ後処理を適用する,
		起動6_スコアキャッシュをSongsDBに出力する,
		起動7_完了,
		タイトル_起動画面からのフェードイン,
		選曲_結果画面からのフェードイン,
		選曲_NowLoading画面へのフェードアウト,
		NOWLOADING_DTX_FILE_READING,
		NOWLOADING_WAV_FILE_READING,
		NOWLOADING_BMP_FILE_READING,
		NOWLOADING_WAIT_BGM_SOUND_COMPLETION,
		PERFORMANCE_STAGE_FAILED,
		PERFORMANCE_STAGE_FAILED_FADEOUT,
		PERFORMANCE_STAGE_CLEAR,
		PERFORMANCE_STAGE_CLEAR_FadeOut,
		PERFORMANCE_STAGE_RESTART
	}

	public Dictionary<string, DynamicStringSource> dynamicStringSources = new();
	
	public void LoadUI()
	{
		//remove old ui
		if (ui != null)
		{
			ui.Dispose();
		}
		
		//try to get the skin for this stage
		UIGroup? stageUI = CDTXMania.SkinManager.LoadStageSkin(eStageID);
		
		if (stageUI == null)
		{
			ui = new UIGroup(GetType().ToString());
			InitializeBaseUI();
			InitializeDefaultUI();
		}
		else
		{
			ui = stageUI;
			InitializeBaseUI();
		}
	}

	public abstract void InitializeBaseUI();
	public abstract void InitializeDefaultUI();

	public virtual void FirstUpdate()
	{
		
	}
	
	public override int OnUpdateAndDraw()
	{
		if (bJustStartedUpdate)
		{
			FirstUpdate();
			bJustStartedUpdate = false;
		}
		
		//scale by CDTXMania.renderScale;
		ui.scale.X = CDTXMania.renderScale;
		ui.scale.Y = CDTXMania.renderScale;
		ui.Draw(Matrix.Identity);
		
		return base.OnUpdateAndDraw();
	}
	
	public override void OnManagedCreateResources()
	{
		if (bActivated)
		{
			LoadUI();
		}
		
		base.OnManagedCreateResources();
	}

	public override void OnManagedReleaseResources()
	{
		if (bActivated)
		{
			ui.Dispose();
		}
		
		base.OnManagedReleaseResources();
	}

	public override void OnActivate()
	{
		base.OnActivate();
		tDisplayPresence();
	}

	public UIGroup ui;

	/// <summary>
	/// Display the current <see cref="Presence"/> of this stage.
	/// </summary>
	protected void tDisplayPresence()
	{
		if (Presence is var presence && presence != null)
			CDTXMania.DiscordRichPresence?.tSetPresence(presence);
	}
}