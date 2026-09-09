using System.Drawing;
using System.Numerics;
using DTXMania.UI.Animation;
using DTXMania.UI.Drawable;
using DTXMania.UI.Skin;
using Newtonsoft.Json.Linq;

namespace DTXMania;

public class StageFailedCover : UICoverGroup
{
    private const string CloseClip = "close";

    private const float Half = 640.0f;
    private const float Height = 720.0f;

    public StageFailedCover() : base("StageFailedCover")
    {
        size = new Vector2(Half * 2.0f, Height);
        isVisible = false;

        MakeComponent("StageFailedCover", CoverDefault);
    }

    public void Close()
    {
        EnsureContent();

        isVisible = true;
        animator?.Play(CloseClip, false);
    }

    public void Hide() => isVisible = false;

    private static UIGroup CoverDefault()
    {
        UIGroup root = new("StageFailedCover");

        Panel(root, "Left", 0.0f);
        Panel(root, "Right", Half);

        root.animator = new Animator();
        root.animator.Add(CloseAnimation());

        return root;
    }

    private static void Panel(UIGroup root, string name, float clipLeft)
    {
        root.AddChild(new UIImage
        {
            name = name,
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\7_stage_failed.jpg"),
            size = new Vector2(Half, Height),
            position = new Vector3(clipLeft, 0.0f, 0.0f),
            clipRect = new RectangleF(clipLeft, 0.0f, Half, Height)
        });
    }

    private static AnimationClip CloseAnimation()
    {
        AnimationClip clip = new() { name = CloseClip, duration = 0.2f };

        clip.tracks.Add(Slide("Left/position.X", -Half, 0.0f));
        clip.tracks.Add(Slide("Right/position.X", Half * 2.0f, Half));

        return clip;
    }

    private static AnimationTrack Slide(string path, float from, float to)
    {
        AnimationTrack track = new() { path = path };
        track.keyframes.Add(new Keyframe { time = 0.0f, rawValue = new JValue(from) });
        track.keyframes.Add(new Keyframe { time = 0.2f, rawValue = new JValue(to) });

        return track;
    }
}
