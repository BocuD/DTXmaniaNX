namespace DTXMania.UI.Drawable;

public class StageRoot : UIGroup
{
    //a clip in this root's own animator, played once as the stage opens; empty for none
    [Themable] public string openClip = string.Empty;

    public StageRoot() : base("StageRoot")
    {
    }

    public StageRoot(string name) : base(name)
    {
    }

    /// <summary>Runs once the stage's tree is built, before it is first drawn.</summary>
    public virtual void OnStageOpened()
    {
        if (openClip.Length > 0)
        {
            animator?.Play(openClip);
        }
    }
}
