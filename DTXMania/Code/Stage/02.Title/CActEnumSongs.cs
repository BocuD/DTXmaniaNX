using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms.VisualStyles;
using DTXUIRenderer;
using SharpDX;
using FDK;

namespace DTXMania
{
    internal class CActEnumSongs : CActivity
    {
        public bool bコマンドでの曲データ取得;


        /// <summary>
        /// Constructor
        /// </summary>
        public CActEnumSongs()
        {
            Init(false);
        }
        
        private void Init(bool _bコマンドでの曲データ取得)
        {
            bNotActivated = true;
            bコマンドでの曲データ取得 = _bコマンドでの曲データ取得;
        }

        // CActivity 実装

        public override void OnActivate()
        {
            if (bActivated)
                return;
            base.OnActivate();
            
            ctNowEnumeratingSongs = new CCounter();	// 0, 1000, 17, CDTXMania.Timer );
            ctNowEnumeratingSongs.tStart(0, 100, 17, CDTXMania.Timer);
        }
        public override void OnDeactivate()
        {
            if (bNotActivated)
                return;
            base.OnDeactivate();
            ctNowEnumeratingSongs = null;
        }
        public override void OnManagedCreateResources()
        {
            if (bNotActivated)
                return;

            //create font
            CPrivateFastFont font = new(new FontFamily(CDTXMania.ConfigIni.str選曲リストフォント), 12);
            
            string[] strMessage = 
            [
                "曲データの一覧を\n取得しています。\nしばらくお待ちください。",
                "Now enumerating songs.\nPlease wait..."
            ];
            
            //create ui
            ui = new UIGroup();
            ui.size = new Vector2(1280, 720);
            UIText message = ui.AddChild(new UIText(font, CDTXMania.isJapanese ? strMessage[0] : strMessage[1]));
            message.anchor = new Vector2(0.0f, 0.0f);
            
            text = ui.AddChild(new UIText(font, "Progress: 0/100"));
            text.position = new Vector3(0, 50, 0);
            text.anchor = new Vector2(0.0f, 1.0f);

            base.OnManagedCreateResources();
        }
        public override void OnManagedReleaseResources()
        {
            if (bNotActivated)
                return;
            
            ui.Dispose();
            
            base.OnManagedReleaseResources();
        }

        private UIGroup ui;
        private UIText text;

        public override int OnUpdateAndDraw()
        {
            if (bNotActivated)
            {
                return 0;
            }
            ctNowEnumeratingSongs.tUpdateLoop();
            
            //update ui
            text.SetText($"Progress: {text.Texture.transparency:F3}/100");
            
            double fade = Math.Sin(2 * Math.PI * ctNowEnumeratingSongs.nCurrentValue * 2 / 100.0);
            
            //convert to 0-1 range
            text.Texture.transparency = (float)(fade + 1) / 2;
            ui.Draw(Matrix.Identity);

            return 0;
        }
        
        private CCounter ctNowEnumeratingSongs;
    }
}
