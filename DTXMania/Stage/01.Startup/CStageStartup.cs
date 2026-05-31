using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using DTXMania.UI.Inspector;
using Hexa.NET.ImGui;

namespace DTXMania;

internal class CStageStartup : CStage
{
	private RuntimeLogListener? logSource;
	
	public CStageStartup()
	{
		eStageID = EStage.Startup_1;
		bActivated = false;
	}

	public override void InitializeBaseUI()
	{
		
	}
	
	public override void InitializeDefaultUI()
	{
		var background = ui.AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\1_background.jpg"))));
		background.size = new Vector2(1280, 720);
		
		var text = ui.AddChild(new UIText(CDTXMania.VERSION_DISPLAY, 15));
		text.name = "VersionText";
	}
	
	public override int OnUpdateAndDraw()
	{
		base.OnUpdateAndDraw();

		DrawLogArea();
		
		return 1;
	}

	public override void FirstUpdate()
	{
		logSource = CDTXMania.app.maniaGl.host.RuntimeLogListener;
	}
	
	private void DrawLogArea()
	{
		ImGui.SetNextWindowPos(new Vector2(0, 50 * CDTXMania.renderScale));
		Vector2 appSize = CDTXMania.app.maniaGl.windowSize;
		ImGui.SetNextWindowSize(new Vector2(appSize.X, appSize.Y - (100 * CDTXMania.renderScale)));
		ImGui.Begin("Log Window", 
			ImGuiWindowFlags.NoDecoration 
			| ImGuiWindowFlags.NoBackground
			| ImGuiWindowFlags.NoMove
			| ImGuiWindowFlags.NoResize
			| ImGuiWindowFlags.NoScrollbar);

		Vector2 available = ImGui.GetContentRegionAvail();
		ImGui.BeginChild("LogRegion", available, ImGuiWindowFlags.NoScrollbar);
		ImGui.PushFont(ImFontPtr.Null, 18.0f * CDTXMania.renderScale);
		
		bool wasNearBottom = ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - 10f;
		
		if (logSource != null)
		{
			lock (logSource.logLock)
			{
				int count = logSource.logLines.Count;
				if (count > 0)
				{
					ImGuiListClipper clipper = new();
					clipper.Begin(count);

					while (clipper.Step())
					{
						for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
						{
							RuntimeLogListener.LogLine line = logSource.logLines[i];
							Vector4 color = LogWindow.GetColorForLevel(line.Level);

							ImGui.PushStyleColor(ImGuiCol.Text, color);
							ImGui.TextUnformatted(line.Text);
							ImGui.PopStyleColor();
						}
					}

					clipper.End();
				}
			}
		}

		if (wasNearBottom)
		{
			ImGui.SetScrollHereY(1.0f);
		}

		ImGui.PopFont();
		ImGui.EndChild();
		ImGui.End();
	}
}