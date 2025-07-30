using System.Diagnostics;
using System.Drawing;
using DTXMania.Core;
using DTXMania.UI.Inspector;
using FDK;
using Hexa.NET.ImGui;
using SharpDX;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;

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
        //create a black texture
        Bitmap bitmap = new(32, 32);
        Graphics graphics = Graphics.FromImage(bitmap);
        Color black = Color.FromArgb(255, Color.Black);
        graphics.FillRectangle(new SolidBrush(black), new Rectangle(0, 0, bitmap.Width, bitmap.Height));
        graphics.Dispose();
        CTexture blackTexture = new(CDTXMania.app.Device, bitmap, CDTXMania.TextureFormat, false);
        bitmap.Dispose();
        
        //create childContainer
        childContainer = AddChild(new UIGroup("covers"));
        childContainer.size = new Vector2(1280, 720);
        childContainer.position = new Vector3(640, 360, 0);
        childContainer.anchor = new Vector2(0.5f, 0.5f);
        
        top = childContainer.AddChild(new UIImage(new DTXTexture(blackTexture)));
        top.size = new Vector2(3000, 1000);
        top.anchor = new Vector2(0.5f, 1.0f);
        top.position = new Vector3(640, 0, 0);
        
        bottom = childContainer.AddChild(new UIImage(new DTXTexture(blackTexture)));
        bottom.size = new Vector2(3000, 1000);
        bottom.anchor = new Vector2(0.5f, 0.0f);
        bottom.position = new Vector3(640, 720, 0);

        logo = AddChild(new UIImage(DTXTexture.LoadFromPath(CSkin.Path("Graphics/logo_small.png"))));
        logo.position = new Vector3(870, 572, 0);
        logo.size = new Vector2(412, 71);
    }
    
    //these need to be static since the UIDrawable itself might get destroyed
    struct GitaDoraTransitionState
    {
        public long lastDrawTime = 0;
        public float animationProgress = 1.5f;
        public bool animate = false;
        public float targetRotation = MathF.PI * 2.0f;
        public float animationSpeed = 3.0f;
        public float animationDirection = 1.0f;
        public float animationTarget = 2.0f;
        public Action? onComplete = null;
        public bool closed = false;

        public bool finishOnNextFrame = false;

        public GitaDoraTransitionState()
        {
        }
    }

    private static GitaDoraTransitionState state = new();

    public static bool isClosed => state.closed;
    public static bool isAnimating => state.animate;
    
    private UIImage top;
    private UIImage bottom;
    private UIGroup childContainer;
    private UIImage logo;

    private const float logoStartX = 635;
    private const float logoFinalX = 815;
    
    public override void Draw(Matrix parentMatrix)
    {
        //cursed way to ensure we don't stop animating when the animation isn't completed yet
        if (state.finishOnNextFrame)
        {
            state.animate = false;
            if (state.onComplete != null)
            {
                state.onComplete.Invoke();
                state.onComplete = null;
            }
            state.finishOnNextFrame = false;
        }
        
        float delta = (CDTXMania.Timer.nCurrentTime - state.lastDrawTime) / 1000.0f;
        state.lastDrawTime = CDTXMania.Timer.nCurrentTime;
        
        delta = Math.Clamp(delta, 0.0f, 0.1f); //clamp delta to prevent too fast animations

        float t = state.animationProgress;
        
        //clamp
        t = Math.Clamp(t, 0f, 2.0f);
        float rotationDegrees = 52.08f * t + 87.88f * MathF.Pow(t, 2) - 2.72f;
        childContainer.rotation.Z = MathF.PI * 2 * (rotationDegrees / 360.0f) + 0.047F;
        
        //distance = 142.74 * t + 1717.83 * t² - 614.51 * t³ - 0.64
        float clampedT = Math.Clamp(t, 0.0f, 2.5f);
        float distance = 142.74f * clampedT + 1717.83f
            * MathF.Pow(clampedT, 2) - 614.51f
            * MathF.Pow(clampedT, 3) - 0.64f;
        top.position.Y = 360 - distance / 2;
        bottom.position.Y = 360 + distance / 2;
        
        float remappedT = Remap(state.animationProgress, 0.63f, -1.0f, 0.0f, 1.0f);
        Trace.TraceInformation($"Remapped T: {remappedT:F3}");
        float tClamped = Math.Clamp(remappedT, 0.0f, 1.0f);
        float easedT = 1 - MathF.Pow(1 - tClamped, 5);
        Trace.TraceInformation($"Eased T: {easedT:F3}");
        
        float t_logo = Remap(easedT, 0.0f, 1.0f, logoStartX, logoFinalX);
        logo.position.X = t_logo;
        
        float alpha_logo = Remap(t, 0.5f, 0.0f, 0, 1);
        logo.position.X = t_logo;
        logo.Texture.transparency = alpha_logo;
        
        if (state.animate)
        {
            Trace.TraceInformation($"Animating GITADORA transition: {state.animationProgress} -> {state.animationTarget} (direction: {state.animationDirection})");
            state.animationProgress += delta * state.animationSpeed * state.animationDirection;
            if (state.animationProgress > 2.0f && state.animationTarget >= 1.5f)
            {
                state.animationProgress = state.animationTarget;
                state.finishOnNextFrame = true;
                state.closed = false;
            }

            if (state.animationProgress <= -1.0f && state.animationTarget <= -0.5f)
            {
                state.animationProgress = state.animationTarget;
                state.finishOnNextFrame = true;
                state.closed = true;
            }
        }

        base.Draw(parentMatrix);
    }
    
    private static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        return toMin + (value - fromMin) * (toMax - toMin) / (fromMax - fromMin);
    }
    
    public static void Close(Action? action = null)
    {
        state.animate = true;
        state.animationProgress = 2.0f;
        state.animationTarget = -1.0f;
        state.animationDirection = -1.0f;
        state.onComplete = action;
        state.lastDrawTime = CDTXMania.Timer.nCurrentTime;
    }

    public static void Open(Action? action = null)
    {
        state.animate = true;
        state.animationProgress = 0.0f;
        state.animationTarget = 2.0f;
        state.animationDirection = 1.0f;
        state.onComplete = action;
        state.lastDrawTime = CDTXMania.Timer.nCurrentTime;
    }
    
    public override void DrawInspector()
    {
        base.DrawInspector();

        if (ImGui.CollapsingHeader("GITADORA Transition"))
        {
            ImGui.Checkbox("Animate progress", ref state.animate);
            ImGui.SliderFloat("Animation Progress", ref state.animationProgress, -1.0f, 1.0f);
            ImGui.InputFloat("Animation Speed", ref state.animationSpeed, 0.1f, 10.0f);
            
            ImGui.InputFloat("Target Rotation", ref state.targetRotation, 0.1f, 10.0f);
            
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