using FDK;

namespace DTXMania;

internal class CActPerfDrumsFillingEffect : CActivity
{

    public CActPerfDrumsFillingEffect()
    {
        bActivated = false;
    }

    public override void OnManagedCreateResources()
    {
        if (bActivated)
        {
        }
    }
    public override void OnManagedReleaseResources()
    {
        if (bActivated)
        {
            base.OnManagedReleaseResources();
        }
    }
    public override void OnDeactivate()
    {
    }
    public override int OnUpdateAndDraw()
    {
        return 0;
    }


    // Other

    #region [ private ]
    //-----------------

    //-----------------
    #endregion
}