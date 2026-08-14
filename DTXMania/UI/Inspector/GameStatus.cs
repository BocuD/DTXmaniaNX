using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Skin;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Inspector;

public class GameStatus
{
    private static bool demoWindowShown = false;
    public static bool preventGameKeyboardInput = false;

    //fps graph
    private const int BufferSize = 200;
    private static readonly float[] frametimes = new float[BufferSize];
    private static int index = 0;
    
    //average
    private static float rollingSum = 0.0f;
    private static int filledSamples = 0;

    private static float smoothedMax = 0.01f;
    
    public static void UpdatePerformanceGraph(float deltaTime)
    {
        float old = frametimes[index];

        frametimes[index] = deltaTime;

        //maintain sum for rolling average
        if (filledSamples < BufferSize)
        {
            rollingSum += deltaTime;
            filledSamples++;
        }
        else
        {
            rollingSum += deltaTime - old;
        }

        //smoothed max
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
        ImGuiIOPtr io = ImGui.GetIO();
        
        ImGui.Begin("Game State", ImGuiWindowFlags.NoFocusOnAppearing);

        ImGui.Text("Capturing input: " + (io.WantCaptureMouse ? "Mouse " : "") + (io.WantCaptureKeyboard ? "Keyboard" : ""));

        if (ImGui.CollapsingHeader("Game State"))
        {
            ImGui.Text("Current Stage: " + CDTXMania.StageManager.rCurrentStage.GetType());
            
            ImGui.Checkbox("Prevent game keyboard input", ref preventGameKeyboardInput);
            
            ImGui.Checkbox("Prevent stage transitions", ref StageManager.preventStageChanges);
        }

        if (ImGui.CollapsingHeader("Other"))
        {
            if (ImGui.Button("Toggle Demo Window"))
            {
                demoWindowShown = !demoWindowShown;
            }
        }

        DrawFPSGraph();

        DrawFrameProfiler();

        ImGui.End();
        
        if (demoWindowShown)
        {
            ImGui.ShowDemoWindow(ref demoWindowShown);
        }
    }

    private static void DrawFPSGraph()
    {
        //calculate dynamic max for autoscaling
        float maxInBuffer = 0.001f; //start from something tiny to avoid 0
        for (int i = 0; i < BufferSize; i++)
        {
            if (frametimes[i] > maxInBuffer)
                maxInBuffer = frametimes[i];
        }

        //show current frame time
        float currentMs = frametimes[(index - 1 + BufferSize) % BufferSize] * 1000.0f;
        float avgFrametime = (filledSamples > 0) ? rollingSum / filledSamples : 0.016f; // fallback to ~60 FPS
        float scaleMax = MathF.Max(smoothedMax, avgFrametime * 2.0f);
        
        ImGui.Text($"Current Frame Time: {currentMs:F2} ms ({1000.0f / currentMs:F1} FPS)");
        ImGui.Text($"Average Frame Time: {avgFrametime * 1000:F2} ms ({1 / avgFrametime:F1} FPS)");

        //draw label column next to graph
        ImGui.BeginGroup();
        ImGui.Text($"{scaleMax * 1000:F1} ms");
        ImGui.Dummy(new Vector2(0, 60));
        ImGui.Text("0 ms");
        ImGui.EndGroup();

        ImGui.SameLine();

        //draw graph
        unsafe
        {
            fixed (float* dataPtr = frametimes)
            {
                ImGui.PlotLines(
                    label: "##Plot",
                    values: dataPtr,
                    valuesCount: BufferSize,
                    valuesOffset: index,
                    overlayText: (ReadOnlySpan<byte>)null,
                    scaleMin: 0.0f,
                    scaleMax: scaleMax,
                    graphSize: new Vector2(300, 100)
                );
            }
        }
    }

    private static void DrawFrameProfiler()
    {
        if (!ImGui.CollapsingHeader("Frame Profiler", ImGuiTreeNodeFlags.DefaultOpen))
        {
            return;
        }

        var renderer = OpenGL.OpenGlRenderer.Instance;
        if (renderer != null)
        {
            ImGui.Text($"Quads last frame: {renderer.lastFrameQuads} in {renderer.lastFrameDrawCalls} GL draw calls");
        }

        ImGui.Text($"GPU frame time: {FrameProfiler.GpuFrameMs:F2} ms");
        ImGui.TextDisabled("CPU time per section; GPU back-pressure can surface as stalls inside StageDraw");

        if (!ImGui.BeginTable("##FrameProfiler", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            return;
        }

        ImGui.TableSetupColumn("Section");
        ImGui.TableSetupColumn("Last (ms)");
        ImGui.TableSetupColumn("Avg (ms)");
        ImGui.TableSetupColumn("Max (ms)");
        ImGui.TableHeadersRow();

        for (int i = 0; i < FrameProfiler.Sections.Length; i++)
        {
            FrameSection section = FrameProfiler.Sections[i];
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(FrameProfiler.SectionNames[i]);
            ImGui.TableNextColumn();
            ImGui.Text($"{FrameProfiler.GetLastMs(section):F2}");
            ImGui.TableNextColumn();
            ImGui.Text($"{FrameProfiler.GetAverageMs(section):F2}");
            ImGui.TableNextColumn();
            ImGui.Text($"{FrameProfiler.GetMaxMs(section):F2}");
        }

        ImGui.EndTable();

        DrawFrameTrace();
    }

    //where the profiler above answers "what is slow now", this answers "what happened during that song"
    private static string lastTracePath = string.Empty;

    private static void DrawFrameTrace()
    {
        ImGui.Separator();

        if (FrameTrace.Recording)
        {
            if (ImGui.Button("Stop recording"))
            {
                FrameTrace.Stop();
            }

            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.9f, 0.65f, 0.3f, 1.0f), $"recording, {FrameTrace.Frames} frames");
        }
        else
        {
            if (ImGui.Button("Record frame trace"))
            {
                FrameTrace.Start();
            }

            ImGui.SameLine();
            ImGui.TextDisabled(FrameTrace.Frames > 0
                ? $"{FrameTrace.Frames} frames held{(FrameTrace.Full ? ", buffer full" : "")}"
                : "not recording");
        }

        ImGui.BeginDisabled(FrameTrace.Frames == 0);

        if (ImGui.Button("Export CSV"))
        {
            lastTracePath = FrameTrace.Export();
        }

        ImGui.EndDisabled();

        if (lastTracePath.Length > 0)
        {
            ImGui.TextDisabled(lastTracePath);
        }


        ImGui.TextDisabled($"Audio underruns so far: {DTXMania.Core.Audio.AudioUnderruns.Count}");
    }
}