using DiscordRPC;
using DTXMania.Core;
using DTXUIRenderer;
using FDK;
using SharpDX;

namespace DTXMania;

public abstract class CStage : CActivity
{
	// プロパティ

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
		PERFORMANCE_STAGE_FAILED_フェードアウト,
		PERFORMANCE_STAGE_CLEAR,
		PERFORMANCE_STAGE_CLEAR_FadeOut,
		PERFORMANCE_STAGE_RESTART
	}
	
	public void LoadUI()
	{
		//try to get the skin for this stage
		UIGroup stageUI = null; //CDTXMania.SkinManager.GetStageSkin(eStageID);

		if (stageUI == null)
		{
			ui = new UIGroup(GetType().ToString());
			InitializeBaseUI();
		}
		else
		{
			ui = stageUI;
		}
	}

	public abstract void InitializeBaseUI();

	public override int OnUpdateAndDraw()
	{
		ui.Draw(Matrix.Identity);
		
		return base.OnUpdateAndDraw();
	}

	public override void OnManagedReleaseResources()
	{
		if (!bNotActivated)
		{
			ui.Dispose();
		}
		
		base.OnManagedReleaseResources();
	}

	public override void OnActivate()
	{
		LoadUI();
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