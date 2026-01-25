using DTXMania.Core;
using DTXMania.UI;
using DTXMania.UI.Drawable;

namespace DTXMania._07.ResultNew;

public class CStageResultNew : CStage
{
    public CStageResultNew()
    {
        eStageID = EStage.Result_7;
    }

    public CScoreIni.ERANK rankResult;
    public STDGBVALUE<CScoreIni.CPerformanceEntry> playResult;

    public override void OnActivate()
    {
        rankResult = (CScoreIni.ERANK) CScoreIni.tCalculateOverallRankValue(playResult.Drums, playResult.Guitar, playResult.Bass);
    }

    public override void InitializeBaseUI()
    {
        
    }
    
    public override void InitializeDefaultUI()
    {
        var txBackground = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\8_background.jpg"));
        
        switch (rankResult)
        {
            case CScoreIni.ERANK.SS:
                if (File.Exists(CSkin.Path(@"Graphics\8_background rankSS.png")))
                {
                    txBackground = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\8_background rankSS.png"));
                }
                break;
            case CScoreIni.ERANK.S:
                if (File.Exists(CSkin.Path(@"Graphics\8_background rankS.png")))
                {
                    txBackground = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\8_background rankS.png"));
                }
                break;
            case CScoreIni.ERANK.A:
                if (File.Exists(CSkin.Path(@"Graphics\8_background rankA.png")))
                {
                    txBackground = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\8_background rankA.png"));
                }
                break;
            case CScoreIni.ERANK.B:
                if (File.Exists(CSkin.Path(@"Graphics\8_background rankB.png")))
                {
                    txBackground = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\8_background rankB.png"));
                }
                break;
            case CScoreIni.ERANK.C:
                if (File.Exists(CSkin.Path(@"Graphics\8_background rankC.png")))
                {
                    txBackground = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\8_background rankC.png"));
                }
                break;
            case CScoreIni.ERANK.D:
                if (File.Exists(CSkin.Path(@"Graphics\8_background rankD.png")))
                {
                    txBackground = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\8_background rankD.png"));
                }
                break;
            case CScoreIni.ERANK.E:
            case CScoreIni.ERANK.UNKNOWN:
                if (File.Exists(CSkin.Path(@"Graphics\8_background rankE.png")))
                {
                    txBackground = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\8_background rankE.png"));
                }
                break;
        }

        DTXTexture dtxTex = new(txBackground);
        var background = ui.AddChild(new UIImage(dtxTex));
        background.renderOrder = -100;
    }
}
