using System.Diagnostics;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Inspector;

namespace DTXMania;

internal class CStageStartup : CStage
{
	private const int LogFontSize = 12;
	private const float LogLineHeight = 17f;
	private const float LogBoxHeight = 700f;

	private static int LogRows => (int)(LogBoxHeight / LogLineHeight);

	private RuntimeLogListener? logSource;

	//one element per row, reused. A new line overwrites the oldest row and every row moves up by one,
	//so only the row that changed is rasterized
	private UIText[] logRows = [];
	private int oldestRow;
	private int linesTaken;

	public CStageStartup()
	{
		eStageID = EStage.Startup_1;
		bActivated = false;
	}

	protected override StageRoot CreateRoot() => new() { canvasFit = UiCanvasFit.Fill };

	public override void RegisterBindings()
	{

	}

	public override void BuildDefaultLayout()
	{
		UICoverGroup background = ui.AddChild(new UICoverGroup("Background"));
		background.renderOrder = -100;
		background.AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\1_background.png")))
		{
			name = "Image",
			size = new Vector2(1280, 720)
		});

		UIImage logo = ui.AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\logo.png"))));
		logo.name = "Logo";
		logo.parentAnchor = new Vector2(1.0f, 1.0f);
		logo.pivot = new Vector2(1.0f, 1.0f);
		logo.position = new Vector3(-10, -15, 0);
		logo.scale = new Vector3(0.18f, 0.18f, 1.0f);

		var text = ui.AddChild(new UIText("", 15));
		text.name = "VersionText";
		text.bindings.Add(new UIBinding("text", "Game.VersionDisplay"));

		//the rows are laid out from the top of this, so moving the log means moving the group
		UIGroup lines = ui.AddChild(new UIGroup("LogLines"));
		lines.position = new Vector3(0, 20, 0);
		lines.size = new Vector2(1240, LogBoxHeight);

		logRows = new UIText[LogRows];
		oldestRow = 0;
		linesTaken = 0;

		for (int row = 0; row < logRows.Length; row++)
		{
			UIText line = lines.AddChild(new UIText("", LogFontSize));
			line.name = $"LogLine{row}";
			line.position = new Vector3(0, row * LogLineHeight, 0);
			line.outlineWidth = 0;
			line.fillColor = Color4.White;
			logRows[row] = line;
		}
	}

	public override int OnUpdateAndDraw()
	{
		//before the base call, so a line added this frame is drawn this frame
		UpdateLog();

		base.OnUpdateAndDraw();

		return 1;
	}

	private void UpdateLog()
	{
		logSource ??= CDTXMania.app.maniaGl.host.RuntimeLogListener;

		if (logSource == null || logRows.Length == 0)
		{
			return;
		}

		bool scrolled = false;

		lock (logSource.logLock)
		{
			//everything older than the rows can hold has already scrolled off
			int first = Math.Max(linesTaken, logSource.logLines.Count - logRows.Length);

			for (int i = first; i < logSource.logLines.Count; i++)
			{
				RuntimeLogListener.LogLine line = logSource.logLines[i];
				Vector4 color = LogWindow.GetColorForLevel(line.Level);

				UIText row = logRows[oldestRow];
				row.fillColor = new Color4(color.X, color.Y, color.Z, color.W);
				row.SetText(line.Text);

				//the async render is thrown away whenever the text changes again, and during startup it
				//changes most frames, so nothing would ever finish
				row.RenderTexture();

				oldestRow = (oldestRow + 1) % logRows.Length;
				scrolled = true;
			}

			linesTaken = logSource.logLines.Count;
		}

		if (scrolled)
		{
			PlaceRows();
		}
	}

	//the ring head is the oldest row, so it goes at the top of the group and the rest follow it round
	private void PlaceRows()
	{
		for (int offset = 0; offset < logRows.Length; offset++)
		{
			UIText row = logRows[(oldestRow + offset) % logRows.Length];
			row.position.Y = offset * LogLineHeight;
		}
	}
}
