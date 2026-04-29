using System.Diagnostics;
using System.Text;
using System.Numerics;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Inspector;

public class LogWindow : TraceListener
{
    private const int MaxLogLines = 200_000;

    private readonly RingBuffer<LogLine> logLines = new(MaxLogLines);
    private readonly object logLock = new();

    private bool autoScroll = true;
    private readonly StringBuilder messageBuffer = new();

    private struct LogLine
    {
        public string Text;
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

            AppendMessageLocked(fullMessage, TraceEventType.Information);
        }
    }

    public override void TraceEvent(TraceEventCache? eventCache, string? source, TraceEventType eventType, int id, string? message)
    {
        if (message == null) return;

        lock (logLock)
        {
            AppendMessageLocked(message, eventType);
        }
    }

    public override void TraceEvent(TraceEventCache? eventCache, string? source, TraceEventType eventType, int id, string? format, params object?[]? args)
    {
        if (format == null) return;

        lock (logLock)
        {
            string message = args == null ? format : string.Format(format, args);
            AppendMessageLocked(message, eventType);
        }
    }

    public override void TraceData(TraceEventCache? eventCache, string? source, TraceEventType eventType, int id, object? data)
    {
        if (data == null) return;

        lock (logLock)
        {
            AppendMessageLocked(data.ToString() ?? string.Empty, eventType);
        }
    }

    public override void TraceData(TraceEventCache? eventCache, string? source, TraceEventType eventType, int id, params object?[]? data)
    {
        if (data == null) return;

        lock (logLock)
        {
            StringBuilder sb = new();
            for (int i = 0; i < data.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(data[i]?.ToString());
            }
            AppendMessageLocked(sb.ToString(), eventType);
        }
    }

    private void AppendMessageLocked(string message, TraceEventType level)
    {
        string prefix = $"[{DateTime.Now:HH:mm:ss.fff}] [{level}] ";

        if (string.IsNullOrEmpty(message))
        {
            logLines.Add(new LogLine { Text = prefix, Level = level });
            return;
        }

        string[] lines = message.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        foreach (string line in lines)
        {
            logLines.Add(new LogLine { Text = prefix + line, Level = level });
        }
    }
    
    public void DrawWindow()
    {
        ImGui.Begin("Log Window", ImGuiWindowFlags.NoFocusOnAppearing);

        if (ImGui.Button("Clear"))
        {
            lock (logLock)
            {
                logLines.Clear();
                messageBuffer.Clear();
            }
        }

        ImGui.SameLine();
        ImGui.Checkbox("Auto-scroll", ref autoScroll);

        ImGui.BeginChild("LogRegion", new Vector2(0, 0), ImGuiWindowFlags.HorizontalScrollbar);

        // Determine bottom state before adding this frame's content.
        bool wasNearBottom = ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - 10f;

        lock (logLock)
        {
            int count = logLines.Count;
            if (count > 0)
            {
                var clipper = new ImGuiListClipper();
                clipper.Begin(count);

                while (clipper.Step())
                {
                    for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                    {
                        LogLine line = logLines[i];
                        Vector4 color = GetColorForLevel(line.Level);

                        ImGui.PushStyleColor(ImGuiCol.Text, color);
                        ImGui.TextUnformatted(line.Text);
                        ImGui.PopStyleColor();
                    }
                }

                clipper.End();
            }
        }

        if (autoScroll && wasNearBottom)
        {
            ImGui.SetScrollHereY(1.0f);
        }

        ImGui.EndChild();
        ImGui.End();
    }

    private sealed class RingBuffer<T>
    {
        private readonly T[] buffer;
        private int start;
        private int count;

        public RingBuffer(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            buffer = new T[capacity];
        }

        public int Count => count;

        public T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return buffer[(start + index) % buffer.Length];
            }
        }

        public void Add(T item)
        {
            if (count < buffer.Length)
            {
                buffer[(start + count) % buffer.Length] = item;
                count++;
                return;
            }

            buffer[start] = item;
            start = (start + 1) % buffer.Length;
        }

        public void Clear()
        {
            start = 0;
            count = 0;
        }
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