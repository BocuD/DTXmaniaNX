using System.Diagnostics;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Inspector;

public class LogWindow
{
    private bool autoScroll = true;
    public RuntimeLogListener? logSource;

    public void DrawWindow()
    {
        ImGui.Begin("Log Window", ImGuiWindowFlags.NoFocusOnAppearing);

        if (ImGui.Button("Clear"))
        {
            logSource?.Clear();
        }

        ImGui.SameLine();
        ImGui.Checkbox("Auto-scroll", ref autoScroll);

        ImGui.BeginChild("LogRegion", new Vector2(0, 0), ImGuiWindowFlags.HorizontalScrollbar);

        // Determine bottom state before adding this frame's content.
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
                            Vector4 color = GetColorForLevel(line.Level);

                            ImGui.PushStyleColor(ImGuiCol.Text, color);
                            ImGui.TextUnformatted(line.Text);
                            ImGui.PopStyleColor();
                        }
                    }

                    clipper.End();
                }
            }
        }

        if (autoScroll && wasNearBottom)
        {
            ImGui.SetScrollHereY(1.0f);
        }

        ImGui.EndChild();
        ImGui.End();
    }

    public static Vector4 GetColorForLevel(TraceEventType level) => level switch
    {
        TraceEventType.Critical => new Vector4(1f, 0.2f, 0.2f, 1f),
        TraceEventType.Error => new Vector4(1f, 0.4f, 0.4f, 1f),
        TraceEventType.Warning => new Vector4(1f, 1f, 0.4f, 1f),
        TraceEventType.Verbose => new Vector4(0.6f, 0.6f, 0.6f, 1f),
        TraceEventType.Start or TraceEventType.Stop => new Vector4(0.6f, 1f, 0.6f, 1f),
        TraceEventType.Suspend or TraceEventType.Resume => new Vector4(0.6f, 0.6f, 1f, 1f),
        TraceEventType.Transfer => new Vector4(1f, 0.6f, 1f, 1f),
        _ => new Vector4(1f, 1f, 1f, 1f),
    };
}