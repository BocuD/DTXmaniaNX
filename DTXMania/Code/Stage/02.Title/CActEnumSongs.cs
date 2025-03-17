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

            try
            {
                ctNowEnumeratingSongs = new CCounter();	// 0, 1000, 17, CDTXMania.Timer );
                ctNowEnumeratingSongs.tStart(0, 100, 17, CDTXMania.Timer);
            }
            finally
            {
            }
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
            string pathNowEnumeratingSongs = CSkin.Path(@"Graphics\ScreenTitle NowEnumeratingSongs.png");
            if (File.Exists(pathNowEnumeratingSongs))
            {
                txNowEnumeratingSongs = CDTXMania.tGenerateTexture(pathNowEnumeratingSongs, false);
            }
            else
            {
                txNowEnumeratingSongs = null;
            }
            string pathDialogNowEnumeratingSongs = CSkin.Path(@"Graphics\ScreenConfig NowEnumeratingSongs.png");
            if (File.Exists(pathDialogNowEnumeratingSongs))
            {
                txDialogNowEnumeratingSongs = CDTXMania.tGenerateTexture(pathDialogNowEnumeratingSongs, false);
            }
            else
            {
                txDialogNowEnumeratingSongs = null;
            }

            try
            {
                Font ftMessage = new Font("MS PGothic", 60f, FontStyle.Bold, GraphicsUnit.Pixel);
                string[] strMessage = 
				{
					"     曲データの一覧を\n       取得しています。\n   しばらくお待ちください。",
					" Now enumerating songs.\n         Please wait..."
				};
                int ci = (CDTXMania.isJapanese) ? 0 : 1;
                if ((strMessage != null) && (strMessage.Length > 0))
                {
                    Bitmap image = new Bitmap(1, 1);
                    Graphics graphics = Graphics.FromImage(image);
                    SizeF ef = graphics.MeasureString(strMessage[ci], ftMessage);
                    Size size = new Size((int)Math.Ceiling((double)ef.Width), (int)Math.Ceiling((double)ef.Height));
                    graphics.Dispose();
                    image.Dispose();
                    image = new Bitmap(size.Width, size.Height);
                    graphics = Graphics.FromImage(image);
                    graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    graphics.DrawString(strMessage[ci], ftMessage, Brushes.White, (float)0f, (float)0f);
                    graphics.Dispose();
                    txMessage = new CTexture(CDTXMania.app.Device, image, CDTXMania.TextureFormat);
                    txMessage.vcScaleRatio = new Vector3(0.5f, 0.5f, 1f);
                    image.Dispose();
                    CDTXMania.tDisposeSafely(ref ftMessage);
                }
                else
                {
                    txMessage = null;
                }
            }
            catch (CTextureCreateFailedException)
            {
                Trace.TraceError("テクスチャの生成に失敗しました。(txMessage)");
                txMessage = null;
            }
            
            //create font
            var font = new CPrivateFastFont(new FontFamily(CDTXMania.ConfigIni.str選曲リストフォント), 18);
            
            //create ui
            ui = new UIGroup();
            ui.size = new Vector2(1280, 720);
            text = ui.AddChild(new UIText(font, "Progress: 0/0"));
            text.position = new Vector3(640, 360, 0);
            text.anchor = new Vector2(0.0f, 1.0f);

            base.OnManagedCreateResources();
        }
        public override void OnManagedReleaseResources()
        {
            if (bNotActivated)
                return;

            CDTXMania.tDisposeSafely(ref txDialogNowEnumeratingSongs);
            CDTXMania.tDisposeSafely(ref txNowEnumeratingSongs);
            CDTXMania.tDisposeSafely(ref txMessage);
            base.OnManagedReleaseResources();
        }
        
        protected UIGroup ui;
        private UIText text;

        public override int OnUpdateAndDraw()
        {
            if (bNotActivated)
            {
                return 0;
            }
            ctNowEnumeratingSongs.tUpdateLoop();
            if (txNowEnumeratingSongs != null)
            {
                txNowEnumeratingSongs.nTransparency = (int)(176.0 + 80.0 * Math.Sin((double)(2 * Math.PI * ctNowEnumeratingSongs.nCurrentValue * 2 / 100.0)));
                txNowEnumeratingSongs.tDraw2D(CDTXMania.app.Device, 18, 7);
                
                //update ui
                text.SetText("Progress: " + ctNowEnumeratingSongs.nCurrentValue + "/100");
                
                ui.Draw(Matrix.Identity);
            }
            if (bコマンドでの曲データ取得 && txDialogNowEnumeratingSongs != null)
            {
                txDialogNowEnumeratingSongs.tDraw2D(CDTXMania.app.Device, 360, 177);
                txMessage.tDraw2D(CDTXMania.app.Device, 450, 240);
            }

            return 0;
        }


        private CCounter ctNowEnumeratingSongs;
        private CTexture txNowEnumeratingSongs = null;
        private CTexture txDialogNowEnumeratingSongs = null;
        private CTexture txMessage;
    }
}
