using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Animation;
using DTXMania.UI.Drawable;
using DTXMania.UI.Inspector;
using Hexa.NET.ImGui;

namespace DTXMania.Drawable;

public class JudgementString : UIGroup
{
    private static BaseTexture[] stringTextures;
    private static BaseTexture barTexture;
    private static BaseTexture autoBarTexture;

    private static void CacheTextures()
    {
        barTexture = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Note\Judge\judge_bar.png"));
        autoBarTexture = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Note\Judge\judge_bar_auto.png"));
        stringTextures = new BaseTexture[Enum.GetValues(typeof(EJudgement)).Length];
        
        foreach (EJudgement judgement in Enum.GetValues(typeof(EJudgement)))
        {
            BaseTexture stringTexture;
            
            switch (judgement)
            {
                case EJudgement.Miss:
                    stringTexture = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Note\Judge\judge_miss.png"));
                    break;
                case EJudgement.Auto:
                    stringTexture = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Note\Judge\judge_auto.png"));
                    break;
                default:
                    stringTexture = BaseTexture.LoadFromPath(CSkin.Path($"Graphics\\Note\\Judge\\judge_{(int)judgement}.png"));
                    break;
            }

            stringTextures[(int)judgement] = stringTexture;
        }
    }

    private EJudgement judgement = EJudgement.Perfect;
    
    [AddChildMenu]
    public static JudgementString Create()
    {
        return new JudgementString();
    }

    private UIImage baseString;
    private UIImage highlightString;
    private UIImage bar;
    
    public JudgementString()
    {
        if (barTexture == null || !barTexture.IsValid()) CacheTextures();
        
        name = $"JudgementString";
        
        bar = AddChild(new UIImage(barTexture));
        bar.anchor = new Vector2(0.5f, 0.5f);
        bar.name = "bar";
        bar.renderOrder = -1;
        
        baseString = AddChild(new UIImage(stringTextures[(int)judgement]));
        baseString.name = "judge1";
        baseString.anchor = new Vector2(0.5f, 0.5f);
        highlightString = AddChild(new UIImage(stringTextures[(int)judgement]));
        highlightString.name = "judge2";
        highlightString.anchor = new Vector2(0.5f, 0.5f);

        animator = new Animator();
        AnimationClip? loaded = AnimationClipIO.LoadFromFile(CSkin.Path(@"Graphics\Note\Judge\hit.json"));
        if (loaded != null)
        {
            animator.clips.Add(loaded);
        }

        isVisible = false;
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (ImGui.CollapsingHeader("Judgement"))
        {
            Inspector.Inspect("Preview Type", ref judgement);
            
            if (ImGui.Button("Play")) Play(judgement);
        }
    }

    public void Play(EJudgement judge)
    {
        isVisible = true;
        judgement = judge;

        switch (judgement)
        {
            case EJudgement.Perfect:
                bar.isVisible = true;
                bar.SetTexture(barTexture, false, false);
                break;
            
            case EJudgement.Auto:
                bar.isVisible = true;
                bar.SetTexture(autoBarTexture, false, false);
                break;
            
            default:
                bar.isVisible = false;
                break;
        }
        
        baseString.SetTexture(stringTextures[(int)judgement], false, false);
        highlightString.SetTexture(stringTextures[(int)judgement], false, false);
        
        //fast forward one frame
        animator.time = 1 / 60.0f;
        animator.Play("hit");
    }
}