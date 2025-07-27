using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Numerics;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Inspector;

public class LogWindow : TraceListener
{
    private readonly List<LogEntry> logMessages = new();
    private readonly object logLock = new();

    private bool autoScroll = true;
    private readonly StringBuilder messageBuffer = new();

    private struct LogEntry
    {
        public string Message;
        public DateTime Timestamp;
        public TraceEventType Level;
    }

    public override void Write(string? message)
    {
        if (message == null) return;

        lock (logLock)
        {
            messageBuffer.Append(message);
        }
    }

    public override void WriteLine(string? message)
    {
        if (message == null) return;

        lock (logLock)
        {
            messageBuffer.AppendLine(message);
            string fullMessage = messageBuffer.ToString().TrimEnd('\n', '\r');
            messageBuffer.Clear();

            logMessages.Add(new LogEntry
            {
                Message = fullMessage,
                Timestamp = DateTime.Now,
                Level = TraceEventType.Information // Fallback
            });
        }
    }

    public override void TraceEvent(TraceEventCache? eventCache, string? source, TraceEventType eventType, int id, string? message)
    {
        if (message == null) return;

        lock (logLock)
        {
            logMessages.Add(new LogEntry
            {
                Message = message,
                Timestamp = DateTime.Now,
                Level = eventType
            });
        }
    }

    public void DrawWindow()
    {
        ImGui.Begin("Log Window");

        if (ImGui.Button("Clear"))
        {
            lock (logLock)
            {
                logMessages.Clear();
            }
        }

        ImGui.SameLine();
        ImGui.Checkbox("Auto-scroll", ref autoScroll);

        ImGui.BeginChild("LogRegion", new Vector2(0, 0), ImGuiWindowFlags.HorizontalScrollbar);

        lock (logLock)
        {
            int count = logMessages.Count;
            if (count > 0)
            {
                var clipper = new ImGuiListClipper();
                clipper.Begin(count);

                while (clipper.Step())
                {
                    for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                    {
                        var entry = logMessages[i];
                        var timestamp = entry.Timestamp.ToString("HH:mm:ss.fff");
                        var color = GetColorForLevel(entry.Level);

                        ImGui.PushStyleColor(ImGuiCol.Text, color);
                        ImGui.TextUnformatted($"[{timestamp}] [{entry.Level}] {entry.Message}");
                        ImGui.PopStyleColor();
                    }
                }
            }

            if (autoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - 10)
            {
                ImGui.SetScrollHereY(1.0f);
            }
        }

        ImGui.EndChild();
        ImGui.End();
    }

    private static Vector4 GetColorForLevel(TraceEventType level) => level switch
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