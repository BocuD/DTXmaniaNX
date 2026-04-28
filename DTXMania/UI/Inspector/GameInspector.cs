using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Skin;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Inspector;

public class GameStatus
{
    private static bool demoWindowShown = false;
    private static bool preventGameKeyboardInput = false;
   
    //todo: move this somewhere else, maybe a debug class in the core
    public static bool logThemeApplyDetails = false;
    
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

        CDTXMania.InputManager.Keyboard.preventKeyboardInput =
            io.WantCaptureKeyboard || preventGameKeyboardInput;
        
        InspectorManager.hierarchyWindow.target = CDTXMania.StageManager.rCurrentStage.ui;

        ImGui.Begin("Game State", ImGuiWindowFlags.NoFocusOnAppearing);

        ImGui.Text("Capturing input: " + (io.WantCaptureMouse ? "Mouse " : "") + (io.WantCaptureKeyboard ? "Keyboard" : ""));

        if (ImGui.CollapsingHeader("Game State"))
        {
            ImGui.Text("Current Stage: " + CDTXMania.StageManager.rCurrentStage.GetType());
            
            ImGui.Checkbox("Prevent game keyboard input", ref preventGameKeyboardInput);
            
            ImGui.Checkbox("Prevent stage transitions", ref StageManager.preventStageChanges);
        }

        if (ImGui.CollapsingHeader("Skin"))
        {
            DrawSkinInspector();
        }

        if (ImGui.CollapsingHeader("Other"))
        {
            if (ImGui.Button("Toggle Demo Window"))
            {
                demoWindowShown = !demoWindowShown;
            }
        }

        DrawFPSGraph();

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

    private static string newSkinName = "";
    private static string newSkinAuthor = "";
    private static void DrawSkinInspector()
    {
        var currentSkin = CDTXMania.SkinManager.currentSkin;

        ImGui.Checkbox("Debug Theme Serializer", ref logThemeApplyDetails);

        string skinName = currentSkin?.name ?? "No skin selected";

        if (ImGui.TreeNode("Currently loaded skin: " + skinName))
        {
            if (currentSkin != null)
            {
                currentSkin.DrawInspector();
                
                if (ImGui.Button("Save Skin Changes"))
                {
                    currentSkin.Save();
                    currentSkin.SaveCurrentStageChanges();
                    
                    //run gc
                    CDTXMania.tRunGarbageCollector();
                
                    //load the skin again
                    CDTXMania.StageManager.rCurrentStage.LoadUI(true);
                }

                if (ImGui.Button("Unload Skin"))
                {
                    CDTXMania.SkinManager.ChangeSkin(null);
                    
                    //run gc
                    CDTXMania.tRunGarbageCollector();
                }

                if (ImGui.Button("Reset Current Stage"))
                {
                    CDTXMania.StageManager.rCurrentStage.LoadUI(false);
                }
            }
            ImGui.TreePop();
        }

        ImGui.Spacing();

        if (ImGui.TreeNode("Available Skins"))
        {
            //display list of skins
            foreach (SkinDescriptor skin in CDTXMania.SkinManager.skins)
            {
                ImGui.Text(skin.name);

                ImGui.SameLine();

                int hash = skin.GetHashCode();
                if (ImGui.Button("Load##" + hash))
                {
                    CDTXMania.SkinManager.ChangeSkin(skin);
                }
            }
            ImGui.TreePop();
        }

        if (ImGui.Button("Scan skin directory"))
        {
            CDTXMania.SkinManager.ScanSkinDirectory();
        }

        ImGui.SameLine();
        
        if (ImGui.Button("Create new skin"))
        {
            //create modal
            ImGui.OpenPopup("Create new skin");
        }
            
        if (ImGui.BeginPopupModal("Create new skin"))
        {
            ImGui.Text("Skin Options");
            ImGui.InputText("Name", ref newSkinName, 100);
            ImGui.InputText("Author", ref newSkinAuthor, 100);
            if (ImGui.Button("Create"))
            {
                CDTXMania.SkinManager.CreateNewSkin(newSkinName, newSkinAuthor);
                ImGui.CloseCurrentPopup();
            }
            
            ImGui.SameLine();
            
            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
    }
}