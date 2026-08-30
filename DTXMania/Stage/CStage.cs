using System.Numerics;
using DiscordRPC;
using DTXMania.Core;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using FDK;

namespace DTXMania;

public abstract class CStage : CActivity, IUIInputHandler
{
	/// <summary>
	/// The presence used to indicate the user's activity within this stage, or <see langword="null"/> if there is none.
	/// </summary>
	protected virtual RichPresence Presence => new CDTXRichPresence
	{
		State = "In Menu",
		Details = "Idle",
	};

	public virtual bool NeedsImGui => false;

	public static bool previewMode;

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
		ChangeSkin_9,						// #28195 2011.5.4 yyagi
		UITest_10							//dev only json layout test bed
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
		PERFORMANCE_STAGE_RESTART
	}

	//behaviour a layout can bind to by key. Values come from data contexts instead, so that one element
	//can be reused with different data; see IUIDataContext
	public Dictionary<string, Action> dynamicActions = new();
	public Dictionary<string, Action> drawSources = new();

	public void LoadUI(bool loadSkin = true)
	{
		//whatever this stage handed its input to is part of the tree about to be replaced
		UIFocus.PopOverlays(this);
		focusTarget = null;

		ui?.Dispose();

		//a skin layout defines the serializable UI outright; there is no merge with the code default
		UIGroup? layout = loadSkin ? CDTXMania.SkinManager.LoadStageLayout(eStageID) : null;

		StageRoot root = CreateRoot();

		//a layout saved with this stage's own root type carries the stage's settings, so it is adopted
		//whole. Anything else — a plain group, or a root from before this stage declared its own type —
		//contributes only its children, or the stage would lose the sounds and settings that live on it
		if (layout != null && layout.GetType() == root.GetType())
		{
			root.Dispose();
			root = (StageRoot)layout;
		}

		root.name = GetType().ToString();

		ui = root;
		RegisterBindings();

		if (layout == null)
		{
			BuildDefaultLayout();
		}
		else if (!ReferenceEquals(layout, root))
		{
			foreach (UIDrawable child in layout.children.ToArray())
			{
				ui.AddChild(child);
			}
		}

		//before OnStageOpened, so a stage that plays something as it opens has it in memory by then
		root.LoadSounds();

		OnLayoutReady();
		root.OnStageOpened();
	}

	/// <summary>The root type this stage uses, for a stage that declares settings of its own.</summary>
	protected virtual StageRoot CreateRoot() => new();

	/// <summary>Register the data and behaviour the layout binds to by key. No visuals here.</summary>
	public abstract void RegisterBindings();

	/// <summary>
	/// Build the skinnable UI tree in code, using only elements that round-trip through json. This is the
	/// code "default skin"; a custom skin's layout json replaces it wholesale.
	/// </summary>
	public abstract void BuildDefaultLayout();

	/// <summary>
	/// Runs once the whole tree exists, from json or code. Resolve references to elements by name here,
	/// and add anything that has no serializable form (marked <c>dontSerialize</c>). Must be idempotent:
	/// it also runs on a mid-stage rebuild from a skin save/reload.
	/// </summary>
	public virtual void OnLayoutReady()
	{
	}

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

		UICanvas.Place(ui);
		ui.Draw(Matrix4x4.Identity);
		
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
			ui = null;
		}
		
		base.OnManagedReleaseResources();
	}

	public override void OnActivate()
	{
		base.OnActivate();

		//one stage reads input at a time, even where the previous one is deliberately left running
		//underneath this one
		foreach (IUIInputHandler handler in UIFocus.Stack.ToArray())
		{
			if (handler is CStage stage && stage != this)
			{
				UIFocus.Remove(stage);
			}
		}

		UIFocus.Push(this);
		tDisplayPresence();
	}

	public override void OnDeactivate()
	{
		UIFocus.Pop(this);
		base.OnDeactivate();
	}

	public virtual string FocusName => GetType().Name;

	public NavigationRepeat? Navigation => navigation;

	protected readonly NavigationRepeat navigation = NavigationRepeat.Vertical();

	/// <summary>
	/// What this stage hands its input to — usually its menu. Set it and there is nothing else to do; a
	/// stage that reads input itself overrides <see cref="HandleInput"/> instead.
	/// </summary>
	protected IUIInputHandler? focusTarget;

	//a stage mid-transition reads nothing, so no stage has to remember to check its own phase
	void IUIInputHandler.HandleInput()
	{
		if (ePhaseID == EPhase.Common_DefaultState)
		{
			HandleInput();
		}
	}

	/// <summary>
	/// Reads the frame's input, if this stage still holds focus — an overlay that pushed itself is polled
	/// instead. Never called mid-transition, so an implementation does not have to check the phase.
	/// </summary>
	public virtual void HandleInput()
	{
		if (focusTarget != null)
		{
			UIFocus.Push(focusTarget);
		}
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