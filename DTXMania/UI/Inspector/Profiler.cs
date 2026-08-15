using System.Numerics;
using DTXMania.Core.Framework;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Inspector;

/// <summary>
/// Where the frame's time and allocation go. Sections and probes share one table, indented by how deep
/// they sit, so a child reads as part of what is above it rather than as another entry in a list.
/// </summary>
public static class Profiler
{
    private const int BufferSize = 200;
    private static readonly float[] frametimes = new float[BufferSize];
    private static int index;

    private static float rollingSum;
    private static int filledSamples;
    private static float smoothedMax = 0.01f;

    private static readonly Vector4 Busy = new(0.9f, 0.65f, 0.3f, 1.0f);
    private static readonly Vector4 Plain = new(1f, 1f, 1f, 1f);

    public static void UpdatePerformanceGraph(float deltaTime)
    {
        float old = frametimes[index];
        frametimes[index] = deltaTime;

        if (filledSamples < BufferSize)
        {
            rollingSum += deltaTime;
            filledSamples++;
        }
        else
        {
            rollingSum += deltaTime - old;
        }

        if (!(deltaTime > smoothedMax))
        {
            const float decayHalfLife = 1.0f;
            float decayFactor = MathF.Pow(0.5f, deltaTime / decayHalfLife);
            smoothedMax = MathF.Max(smoothedMax * decayFactor, 0.001f);
        }

        smoothedMax = MathF.Min(deltaTime, 1000);

        index = (index + 1) % BufferSize;
    }

    public static void Draw()
    {
        ImGui.Begin("Profiler", ImGuiWindowFlags.NoFocusOnAppearing);

        //measuring costs a timestamp per section per frame whether or not anything reads it, so it is off
        //until asked for and stays on once it is, window open or not
        bool enabled = FrameProfiler.Enabled;
        if (ImGui.Checkbox("Enable profiling", ref enabled))
        {
            FrameProfiler.Enabled = enabled;
        }


        DrawFrameGraph();
        DrawFrameTable();
        DrawOffThread();
        DrawTrace();

        ImGui.End();
    }

    private static void DrawFrameGraph()
    {
        float currentMs = frametimes[(index - 1 + BufferSize) % BufferSize] * 1000.0f;
        float average = filledSamples > 0 ? rollingSum / filledSamples : 0.016f;
        float scaleMax = MathF.Max(smoothedMax, average * 2.0f);

        ImGui.Text($"Frame: {currentMs:F2} ms ({1000.0f / currentMs:F0} fps)"
                   + $"   average {average * 1000:F2} ms ({1 / average:F0} fps)");

        var renderer = OpenGL.OpenGlRenderer.Instance;
        if (renderer != null)
        {
            ImGui.TextDisabled($"{renderer.lastFrameQuads} quads in {renderer.lastFrameDrawCalls} draw calls"
                               + $"   GPU {FrameProfiler.GpuFrameMs:F2} ms");
        }

        ImGui.BeginGroup();
        ImGui.Text($"{scaleMax * 1000:F1} ms");
        ImGui.Dummy(new Vector2(0, 60));
        ImGui.Text("0 ms");
        ImGui.EndGroup();

        ImGui.SameLine();

        unsafe
        {
            fixed (float* values = frametimes)
            {
                ImGui.PlotLines(
                    label: "##Plot",
                    values: values,
                    valuesCount: BufferSize,
                    valuesOffset: index,
                    overlayText: (ReadOnlySpan<byte>)null,
                    scaleMin: 0.0f,
                    scaleMax: scaleMax,
                    graphSize: new Vector2(300, 100));
            }
        }
    }

    private static void DrawFrameTable()
    {
        ImGui.Separator();

        if (!ImGui.BeginTable("##Frame", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            return;
        }

        ImGui.TableSetupColumn("Scope");
        ImGui.TableSetupColumn("Last (ms)");
        ImGui.TableSetupColumn("Avg (ms)");
        ImGui.TableSetupColumn("Max (ms)");
        ImGui.TableSetupColumn("Alloc (B)");
        ImGui.TableHeadersRow();

        for (int i = 0; i < FrameProfiler.Sections.Length; i++)
        {
            FrameSection section = FrameProfiler.Sections[i];

            Row(FrameProfiler.SectionNames[i], FrameProfiler.DepthOf(section));
            Milliseconds(FrameProfiler.GetLastMs(section));
            Milliseconds(FrameProfiler.GetAverageMs(section));
            Milliseconds(FrameProfiler.GetMaxMs(section));
            Bytes(FrameProfiler.GetAverageBytes(section), 1024);

            //the probes measure pieces of the stage's own draw, so they belong inside it rather than
            //after the frame. VideoUpload is the last of its children, so they go here
            if (section == FrameSection.VideoUpload)
            {
                DrawProbes(FrameProfiler.DepthOf(section));
            }
        }

        ImGui.EndTable();

        ImGui.TextDisabled($"{AllocationProbe.Frames} frames");
        ImGui.SameLine();

        if (ImGui.SmallButton("Reset"))
        {
            AllocationProbe.Reset();
            OffThreadStats.ResetAll();
        }
    }

    private static void DrawProbes(int baseDepth)
    {
        for (int i = 0; i < AllocationProbe.Count; i++)
        {
            Row(AllocationProbe.NameOf(i), baseDepth + AllocationProbe.DepthOf(i));

            //a probe counts bytes and nothing else, so its timing cells stay empty rather than read zero
            for (int column = 0; column < 3; column++)
            {
                ImGui.TableNextColumn();
                ImGui.TextDisabled("-");
            }

            Bytes(AllocationProbe.BytesPerFrame(i), 512);
        }
    }

    private static void Row(string name, int depth)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        if (depth > 0)
        {
            ImGui.Indent(depth * 12f);
        }

        ImGui.Text(name);

        if (depth > 0)
        {
            ImGui.Unindent(depth * 12f);
        }
    }

    private static void Milliseconds(float value)
    {
        ImGui.TableNextColumn();

        if (value > 0.005f)
        {
            ImGui.Text($"{value:F2}");
        }
        else
        {
            ImGui.TextDisabled("0");
        }
    }

    private static void Bytes(long value, long busyAbove)
    {
        ImGui.TableNextColumn();

        if (value > 0)
        {
            ImGui.TextColored(value > busyAbove ? Busy : Plain, value.ToString());
        }
        else
        {
            ImGui.TextDisabled("0");
        }
    }

    private static void DrawOffThread()
    {
        if (OffThreadStats.All.Count == 0)
        {
            return;
        }

        ImGui.Separator();
        ImGui.Text("Other threads");

        if (!ImGui.BeginTable("##OffThread", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            return;
        }

        ImGui.TableSetupColumn("Worker");
        ImGui.TableSetupColumn("Last (ms)");
        ImGui.TableSetupColumn("Avg (ms)");
        ImGui.TableSetupColumn("Queued");
        ImGui.TableSetupColumn("Alloc (B)");
        ImGui.TableHeadersRow();

        for (int i = 0; i < OffThreadStats.All.Count; i++)
        {
            OffThreadStats stats = OffThreadStats.All[i];

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text($"{stats.Name} ({stats.Items})");
            ImGui.TableNextColumn();
            ImGui.Text($"{stats.LastMs:F2}");
            ImGui.TableNextColumn();
            ImGui.Text($"{stats.AverageMs:F2}");
            ImGui.TableNextColumn();
            ImGui.Text(stats.Queued.ToString());
            Bytes(stats.Bytes, long.MaxValue);
        }

        ImGui.EndTable();
    }

    private static string lastTracePath = string.Empty;

    private static void DrawTrace()
    {
        ImGui.Separator();

        if (FrameTrace.Recording)
        {
            if (ImGui.Button("Stop recording"))
            {
                FrameTrace.Stop();
            }

            ImGui.SameLine();
            ImGui.TextColored(Busy, $"recording, {FrameTrace.Frames} frames");
        }
        else
        {
            if (ImGui.Button("Record frame trace"))
            {
                FrameTrace.Start();
            }

            ImGui.SameLine();
            ImGui.TextDisabled(FrameTrace.Frames > 0
                ? $"{FrameTrace.Frames} frames held{(FrameTrace.Full ? ", oldest being overwritten" : "")}"
                : "not recording");
        }

        ImGui.BeginDisabled(FrameTrace.Frames == 0);

        if (ImGui.Button("Export CSV"))
        {
            lastTracePath = FrameTrace.Export();
        }

        ImGui.EndDisabled();

        ImGui.SameLine();
        ImGui.BeginDisabled(FrameTrace.HeldBytes == 0);

        //the buffer is tens of megabytes and outlives the recording, so there is a way to hand it back
        if (ImGui.Button("Free buffer"))
        {
            FrameTrace.Release();
            lastTracePath = string.Empty;
        }

        ImGui.EndDisabled();

        ImGui.SameLine();
        ImGui.TextDisabled(FrameTrace.HeldBytes > 0
            ? $"{FrameTrace.HeldBytes / (1024.0 * 1024.0):F1} MB held"
            : "no buffer");

        if (lastTracePath.Length > 0)
        {
            ImGui.TextDisabled(lastTracePath);
        }

        ImGui.TextDisabled($"Late audio callbacks: {Core.Audio.AudioUnderruns.Count}"
                           + $"   worst gap {Core.Audio.AudioUnderruns.WorstGapMs:F1}ms");
    }
}
