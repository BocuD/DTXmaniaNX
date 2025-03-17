using FDK;

namespace DTXMania
{
    internal class CActPerfDrumsFillingEffect : CActivity
    {

        public CActPerfDrumsFillingEffect()
		{
			base.bNotActivated = true;
		}

        public override void OnManagedCreateResources()
        {
            if (!base.bNotActivated)
            {
            }
        }
        public override void OnManagedReleaseResources()
        {
            if (!base.bNotActivated)
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
