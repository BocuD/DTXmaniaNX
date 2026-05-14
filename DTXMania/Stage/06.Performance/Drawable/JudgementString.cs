using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Drawable;
using DTXMania.UI.Inspector;
using Hexa.NET.ImGui;

namespace DTXMania.Drawable;

public class JudgementString : UIGroup
{
    private static BaseTexture[] stringTextures;

    private static void CacheTextures()
    {
        stringTextures = new BaseTexture[Enum.GetValues(typeof(EJudgement)).Length];
        
        foreach (EJudgement judgement in Enum.GetValues(typeof(EJudgement)))
        {
            BaseTexture stringTexture;
            
            switch (judgement)
            {
                case EJudgement.Miss:
                    stringTexture = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Judge\judge_miss.png"));
                    break;
                case EJudgement.Auto:
                    stringTexture = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Judge\judge_auto.png"));
                    break;
                default:
                    stringTexture = BaseTexture.LoadFromPath(CSkin.Path($"Graphics\\Judge\\judge_{(int)judgement}.png"));
                    break;
            }

            stringTextures[(int)judgement] = stringTexture;
        }
    }

    private EJudgement previewType = EJudgement.Perfect;
    
    [AddChildMenu]
    public static JudgementString Create()
    {
        return new JudgementString();
    }

    private UIImage baseString;
    private UIImage highlightString;
    
    public JudgementString()
    {
        if (stringTextures == null) CacheTextures();
        
        name = $"JudgementString";
        
        size = new Vector2(stringTextures[0].Width, stringTextures[0].Height);
        
        baseString = AddChild(new UIImage(stringTextures[(int)previewType]));
        highlightString = AddChild(new UIImage(stringTextures[(int)previewType]));
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        baseString.SetTexture(stringTextures[(int)previewType], false, false);
        highlightString.SetTexture(stringTextures[(int)previewType], false, false);
        
        base.Draw(parentMatrix);
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (ImGui.CollapsingHeader("Judgement String"))
        {
            Inspector.Inspect("Preview Type", ref previewType);
        }
    }
}