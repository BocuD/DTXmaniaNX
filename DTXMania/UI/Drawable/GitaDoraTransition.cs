using DTXMania.Core;
using DTXMania.UI.Inspector;
using Hexa.NET.ImGui;
using SharpDX;

namespace DTXMania.UI.Drawable;

public class GitaDoraTransition : UIGroup
{
    [AddChildMenu]
    public new static GitaDoraTransition Create()
    {
        return new GitaDoraTransition();
    }
    
    public GitaDoraTransition() : base("GITADORA Transition")
    {
        size = new Vector2(1280, 720);
        position = new Vector3(640, 360, 0);
        anchor = new Vector2(0.5f, 0.5f);
        
        top = AddChild(new UIImage(DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\black.png"))));
        top.size = new Vector2(3000, 1000);
        top.anchor = new Vector2(0.5f, 1.0f);
        top.position = new Vector3(640, 0, 0);
        
        bottom = AddChild(new UIImage(DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\black.png"))));
        bottom.size = new Vector2(3000, 1000);
        bottom.anchor = new Vector2(0.5f, 0.0f);
        bottom.position = new Vector3(640, 720, 0);
    }
    
    long lastDrawTime = 0;
    private float animationProgress = 1.5f;
    public bool animate = false;

    private float verticalOffset = 600;
    private float targetRotation = MathF.PI * 2;

    private float animationSpeed = 3.4f;
    private float animationDirection = 1.0f;
    private float animationTarget;
    public override void Draw(Matrix parentMatrix)
    {
        float delta = (CDTXMania.Timer.nCurrentTime - lastDrawTime) / 1000.0f;
        lastDrawTime = CDTXMania.Timer.nCurrentTime;

        float t = animationProgress;
        
        //clamp
        t = Math.Clamp(t, 0f, 2.0f);
        float rotationDegrees = 52.08f * t + 87.88f * MathF.Pow(t, 2) - 2.72f;
        rotation.Z = MathF.PI * 2 * (rotationDegrees / 360.0f) + 0.047F;
        
        //distance = 142.74 * t + 1717.83 * t² - 614.51 * t³ - 0.64
        float distance = 142.74f * t + 1717.83f * MathF.Pow(t, 2) - 614.51f * MathF.Pow(t, 3) - 0.64f;
        top.position.Y = 360 - distance / 2;
        bottom.position.Y = 360 + distance / 2;
        
        if (animate)
        {
            animationProgress += delta * animationSpeed * animationDirection;
            if (animationProgress > 2.0f && animationTarget >= 1.5f)
            {
                animationProgress = animationTarget;
                animate = false;
                if (onComplete != null)
                {
                    onComplete.Invoke();
                    onComplete = null;
                    closed = false;
                }
            }
            if (animationProgress <= 0.0f && animationTarget <= 0.5f)
            {
                animationProgress = animationTarget;
                animate = false;
                if (onComplete != null)
                {
                    onComplete.Invoke();
                    onComplete = null;
                    closed = true;
                }
            }
        }

        base.Draw(parentMatrix);
    }

    private Action? onComplete;

    public void Close(Action? action = null)
    {
        animate = true;
        animationProgress = 2.0f;
        animationTarget = 0.0f;
        animationDirection = -1.0f;
        onComplete = action;
        lastDrawTime = CDTXMania.Timer.nCurrentTime;
    }

    public void Open(Action? action = null)
    {
        animate = true;
        animationProgress = 0.0f;
        animationTarget = 2.0f;
        animationDirection = 1.0f;
        onComplete = action;
        lastDrawTime = CDTXMania.Timer.nCurrentTime;
    }

    private UIImage top;
    private UIImage bottom;
    public bool closed = false;

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (ImGui.CollapsingHeader("GITADORA Transition"))
        {
            ImGui.Checkbox("Animate progress", ref animate);
            ImGui.SliderFloat("Animation Progress", ref animationProgress, 0.0f, 1.0f);
            ImGui.InputFloat("Animation Speed", ref animationSpeed, 0.1f, 10.0f);
            
            ImGui.InputFloat("Vertical Multiplier", ref verticalOffset, 1.0f, 100.0f);
            ImGui.InputFloat("Target Rotation", ref targetRotation, 0.1f, 10.0f);
            
            if (ImGui.Button("Open Transition"))
            {
                Open();
            }
            
            if (ImGui.Button("Close Transition"))
            {
                Close();
            }
        }
    }
}