using System.Diagnostics;
using FDK;
using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.Core.Video;
using DTXMania.UI;
using DTXMania.UI.Animation;
using DTXMania.UI.Drawable;
using DTXMania.UI.Skin;
using DTXMania.UI.DynamicElements;
using Newtonsoft.Json.Linq;

namespace DTXMania;

internal class CStageTitle : CStage
{
	// コンストラクタ

	private bool exitRequested;

	public CStageTitle()
	{
		eStageID = EStage.Title_2;
		bActivated = false;
	}


	// CStage 実装

	public override void RegisterBindings()
	{
	}

	public override void BuildDefaultLayout()
	{
		var text = ui.AddChild(new UIText("", 15));
		text.name = "VersionText";
		text.bindings.Add(new UIBinding("text", "Game.VersionDisplay"));

		//ambient looping background video, part of the layout so its render order is skinnable
		ui.AddChild(new UINewVideoRenderer
		{
			video = SkinResource.System(@"Graphics\2_background.mp4"),
			renderOrder = -100,
			name = "BackgroundVideo"
		});


		ui.AddChild(new UIImage
		{
			imageSource = ImageSource.File,
			image = SkinResource.System(@"Graphics\2_background.png"),
			renderOrder = -99,
			position = Vector3.Zero,
			name = "Background"
		});

		UIMenu menu = ui.AddChild(new UIMenu("TitleMenu"));
		menu.position = new Vector3(MENU_X, MENU_Y, 0);
		menu.itemOffset = new Vector3(0, MENU_H, 0);
		menu.itemComponent = @"Components/TitleMenuItem.json";
		menu.renderOrder = 10;
		menu.wrapSelection = false;
		menu.selectionSpeed = 30.0f;

		BuildCursor(menu);
	}

	//the highlight bar plus a flash that pulses over it, both rows of the menu sheet
	private static void BuildCursor(UIMenu menu)
	{
		UIGroup cursor = menu.AddChild(new UIGroup("Cursor"));
		cursor.bindings.Add(new UIBinding("position.Y", "Selection.Y"));

		cursor.AddChild(new UIImage
		{
			name = "Bar",
			imageSource = ImageSource.File,
			image = SkinResource.System(MenuSheet),
			size = new Vector2(MENU_W, MENU_H),
			clipRect = new RectangleF(0, MENU_H * 4, MENU_W, MENU_H)
		});

		cursor.AddChild(new UIImage
		{
			name = "Flash",
			imageSource = ImageSource.File,
			image = SkinResource.System(MenuSheet),
			size = new Vector2(MENU_W, MENU_H),
			clipRect = new RectangleF(0, MENU_H * 5, MENU_W, MENU_H),
			anchor = new Vector2(0.5f, 0.5f),
			position = new Vector3(MENU_W / 2.0f, MENU_H / 2.0f, 0)
		});

		AnimationClip flash = new() { name = "flash", duration = 3.5f, loop = true };
		flash.tracks.Add(FlashTrack("Flash/scale.X", 1.0f, 1.5f));
		flash.tracks.Add(FlashTrack("Flash/scale.Y", 1.0f, 1.5f));
		flash.tracks.Add(FlashTrack("Flash/color.Alpha", 1.0f, 0.0f));

		cursor.animator = new Animator { autoPlayClip = flash.name };
		cursor.animator.Add(flash);
	}

	//the pulse runs over the first half second of the clip and the rest of it is the gap before the next
	private static AnimationTrack FlashTrack(string path, float from, float to)
	{
		AnimationTrack track = new() { path = path };
		track.keyframes.Add(new Keyframe { time = 0.0f, rawValue = new JValue(from) });
		track.keyframes.Add(new Keyframe { time = 0.5f, rawValue = new JValue(to) });
		return track;
	}

	//what one entry looks like: its own row of the menu sheet
	private static UIGroup BuildEntryDefault()
	{
		UIGroup root = new("TitleMenuItem");

		UIImage art = root.AddChild(new UIImage
		{
			name = "Art",
			imageSource = ImageSource.File,
			image = SkinResource.System(MenuSheet),
			size = new Vector2(MENU_W, MENU_H),
			clipRect = new RectangleF(0, 0, MENU_W, MENU_H)
		});

		art.bindings.Add(new UIBinding("clipRect.Y", "Item.ClipY"));

		return root;
	}

	public override void OnLayoutReady()
	{
		titleMenu = ui.children.OfType<UIMenu>().FirstOrDefault();
		if (titleMenu == null)
		{
			return;
		}

		titleMenu.itemDefault = BuildEntryDefault;
		titleMenu.onDecide = ChooseEntry;
		titleMenu.onCancel = () => exitRequested = true;
		focusTarget = titleMenu;

		//the sheet has an unused "Options" row between Start and Config
		titleMenu.SetEntries([
			new UIMenuItem("Start", string.Empty) { ClipY = 0, Sound = GameStartSound },
			new UIMenuItem("Config", string.Empty) { ClipY = MENU_H * 2 },
			new UIMenuItem("Quit", string.Empty) { ClipY = MENU_H * 3 }
		]);
	}

	public override void OnActivate()
	{
		Trace.TraceInformation( "タイトルステージを活性化します。" );
		Trace.Indent();
		try
		{
			base.OnActivate();
		}
		finally
		{
			Trace.TraceInformation( "タイトルステージの活性化を完了しました。" );
			Trace.Unindent();
		}
	}
	public override void OnDeactivate()
	{
		Trace.TraceInformation( "タイトルステージを非活性化します。" );
		Trace.Indent();
		try
		{
		}
		finally
		{
			Trace.TraceInformation( "タイトルステージの非活性化を完了しました。" );
			Trace.Unindent();
		}
		base.OnDeactivate();
	}
	public override void FirstUpdate()
	{
		CDTXMania.Skin.soundTitle.tPlay();
		ePhaseID = EPhase.Common_DefaultState;
		exitRequested = false;
	}

	public override int OnUpdateAndDraw()
	{
		if (!bActivated) return 0;

		base.OnUpdateAndDraw();

		if ( exitRequested )
		{
			return (int) EReturnResult.EXIT;
		}

		if ( ePhaseID != EPhase.Common_FadeOut || GitaDoraTransition.isAnimating )
		{
			return 0;
		}

		ePhaseID = EPhase.Common_EndStatus;

		if ( SelectedEntry == EReturnResult.GAMESTART )
		{
			//reset stage count when we start playing
			CDTXMania.nStageNumber = 0;
		}

		return (int) SelectedEntry;
	}

	//a skin without a game-start sound falls back to the usual decide sound
	private static CSystemSound? GameStartSound
		=> CDTXMania.Skin.soundGameStart.loadSucceeded ? CDTXMania.Skin.soundGameStart : null;

	private void ChooseEntry(UIMenuItem entry)
	{
		//so a second press during the fade cannot pick something again
		UIFocus.Pop( titleMenu! );

		if ( SelectedEntry == EReturnResult.EXIT )
		{
			exitRequested = true;
			return;
		}

		GitaDoraTransition.Close();
		CDTXMania.Skin.soundTitle.tStop();
		ePhaseID = EPhase.Common_FadeOut;
	}

	//entries run in the same order as the results they lead to
	private EReturnResult SelectedEntry => (EReturnResult)( ( titleMenu?.SelectedItem ?? 0 ) + 1 );


	public enum EReturnResult
	{
		CONTINUE = 0,
		GAMESTART,
		CONFIG,
		EXIT
	}


	// Other

	#region [ private ]
	//-----------------
	private const string MenuSheet = @"Graphics\2_menu.png";
	private const int MENU_H = 0x27;
	private const int MENU_W = 0xe3;
	private const int MENU_X = 0x1fa;
	private const int MENU_Y = 0x201;

	private UIMenu? titleMenu;
	//-----------------
	#endregion
}