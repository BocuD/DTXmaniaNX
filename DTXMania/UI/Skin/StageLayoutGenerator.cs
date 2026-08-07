using System.Diagnostics;
using DTXMania.Core;

namespace DTXMania.UI.Skin;

/// <summary>
/// Dev tool that captures a stage's code-built UI tree as a layout json in the active skin, so the
/// existing initialization code can stay the source of truth while stages move over to loading from
/// json. dontSerialize elements are omitted; they stay in code and are re-added when the stage rebuilds.
/// </summary>
public static class StageLayoutGenerator
{
    public static void GenerateForCurrentStage()
    {
        CStage? stage = CDTXMania.StageManager.rCurrentStage;
        if (stage?.ui == null)
        {
            Trace.TraceWarning("StageLayoutGenerator: no current stage / ui to generate from.");
            return;
        }

        CDTXMania.SkinManager.SaveStageLayout(stage.eStageID, stage.ui);
        Trace.TraceInformation($"StageLayoutGenerator: wrote layout for {stage.eStageID} from code.");
    }
}
