using FDK;

namespace DTXMania
{
    internal class CActPerfDrumsFillingEffect : CActivity
    {

        public CActPerfDrumsFillingEffect()
		{
			bNotActivated = true;
		}

        public override void OnManagedCreateResources()
        {
            if (!bNotActivated)
            {
            }
        }
        public override void OnManagedReleaseResources()
        {
            if (!bNotActivated)
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
}
