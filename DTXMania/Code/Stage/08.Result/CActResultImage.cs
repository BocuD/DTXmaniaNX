using System.Diagnostics;
using System.Drawing;
using SharpDX;
using FDK;

using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;

namespace DTXMania
{
    internal class CActResultImage : CActivity
    {
        // コンストラクタ

        public CActResultImage()
        {
            bNotActivated = true;
        }


        // メソッド

        public void tアニメを完了させる()
        {
            ct登場用.nCurrentValue = ct登場用.nEndValue;
        }


        // CActivity 実装

        public override void OnActivate()
        {
            n本体X = 0x1d5;
            n本体Y = 0x11b;

            base.OnActivate();

        }
        public override void OnDeactivate()
        {
            if (ct登場用 != null)
            {
                ct登場用 = null;
            }            
            base.OnDeactivate();
        }
        public override void OnManagedCreateResources()
        {
            if (!bNotActivated)
            {
                ftSongDifficultyFont = new Font("Impact", 15f, FontStyle.Regular);
                iDrumSpeed = Image.FromFile(CSkin.Path(@"Graphics\7_panel_icons.jpg"));
                txジャケットパネル = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\7_JacketPanel.png"));

                txリザルト画像がないときの画像 = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\5_preimage default.png"));
                if (CDTXMania.ConfigIni.bストイックモード)
                {
                    txリザルト画像 = txリザルト画像がないときの画像;
                }
                else if (((!tリザルト画像の指定があれば構築する()) && (!tプレビュー画像の指定があれば構築する())))
                {
                    txリザルト画像 = txリザルト画像がないときの画像;
                }

                #region[ Generation of song title, artist name and disclaimer textures ]
                if (string.IsNullOrEmpty(CDTXMania.DTX.TITLE) || (!CDTXMania.bCompactMode && CDTXMania.ConfigIni.b曲名表示をdefのものにする))
                    strSongName = CDTXMania.stageSongSelection.r現在選択中の曲.strタイトル;
                else
                    strSongName = CDTXMania.DTX.TITLE;

                CPrivateFastFont pfTitle = new CPrivateFastFont(new FontFamily(CDTXMania.ConfigIni.str選曲リストフォント), 20, FontStyle.Regular);
                Bitmap bmpSongName = pfTitle.DrawPrivateFont(strSongName, CPrivateFont.DrawMode.Edge, Color.Black, Color.Black, clGITADORAgradationTopColor, clGITADORAgradationBottomColor, true);
                txSongName = CDTXMania.tGenerateTexture(bmpSongName, false);
                bmpSongName.Dispose();
                pfTitle.Dispose();

                CPrivateFastFont pfArtist = new CPrivateFastFont(new FontFamily(CDTXMania.ConfigIni.str選曲リストフォント), 15, FontStyle.Regular);
                Bitmap bmpArtistName = pfArtist.DrawPrivateFont(CDTXMania.DTX.ARTIST, CPrivateFont.DrawMode.Edge, Color.Black, Color.Black, clGITADORAgradationTopColor, clGITADORAgradationBottomColor, true);
                txArtistName = CDTXMania.tGenerateTexture(bmpArtistName, false);
                bmpArtistName.Dispose();
                pfArtist.Dispose();

                if (CDTXMania.ConfigIni.nPlaySpeed != 20)
                {
                    double d = (double)(CDTXMania.ConfigIni.nPlaySpeed / 20.0);
                    String strModifiedPlaySpeed = "Play Speed: x" + d.ToString("0.000");
                    CPrivateFastFont pfModifiedPlaySpeed = new CPrivateFastFont(new FontFamily(CDTXMania.ConfigIni.str選曲リストフォント), 18, FontStyle.Regular);
                    Bitmap bmpModifiedPlaySpeed = pfModifiedPlaySpeed.DrawPrivateFont(strModifiedPlaySpeed, CPrivateFont.DrawMode.Edge, Color.White, Color.White, Color.Black, Color.Red, true);
                    txModifiedPlaySpeed = CDTXMania.tGenerateTexture(bmpModifiedPlaySpeed, false);
                    bmpModifiedPlaySpeed.Dispose();
                    pfModifiedPlaySpeed.Dispose();
                }

                if (CDTXMania.stageResult.bIsTrainingMode)
                {
                    String strResultsNotSavedTraining = "Training feature used";
                    CPrivateFastFont pfResultsNotSavedTraining = new CPrivateFastFont(new FontFamily(CDTXMania.ConfigIni.str選曲リストフォント), 18, FontStyle.Regular);
                    Bitmap bmpResultsNotSavedTraining = pfResultsNotSavedTraining.DrawPrivateFont(strResultsNotSavedTraining, CPrivateFont.DrawMode.Edge, Color.White, Color.White, Color.Black, Color.Red, true);
                    txTrainingMode = CDTXMania.tGenerateTexture(bmpResultsNotSavedTraining, false);
                    bmpResultsNotSavedTraining.Dispose();
                    pfResultsNotSavedTraining.Dispose();
                }

                String strResultsNotSaved = "Score will not be saved";
                CPrivateFastFont pfResultsNotSaved = new CPrivateFastFont(new FontFamily(CDTXMania.ConfigIni.str選曲リストフォント), 18, FontStyle.Regular);
                Bitmap bmpResultsNotSaved = pfResultsNotSaved.DrawPrivateFont(strResultsNotSaved, CPrivateFont.DrawMode.Edge, Color.White, Color.White, Color.Black, Color.Red, true);
                txResultsNotSaved = CDTXMania.tGenerateTexture(bmpResultsNotSaved, false);
                bmpResultsNotSaved.Dispose();
                pfResultsNotSaved.Dispose();
                #endregion

                Bitmap bitmap2 = new Bitmap(0x3a, 0x12);
                Graphics graphics = Graphics.FromImage(bitmap2);

                graphics.Dispose();
                txSongDifficulty = new CTexture(CDTXMania.app.Device, bitmap2, CDTXMania.TextureFormat, false);
                bitmap2.Dispose();
                Bitmap bitmap3 = new Bitmap(100, 100);
                graphics = Graphics.FromImage(bitmap3);
                float num;
                //If Skill Mode is CLASSIC, always display lvl as Classic Style
                if (CDTXMania.ConfigIni.nSkillMode == 0 || (CDTXMania.ConfigIni.bCLASSIC譜面判別を有効にする && 
                    (CDTXMania.DTX.bHasChips.LeftCymbal == false) && 
                    (CDTXMania.DTX.bHasChips.LP == false) && 
                    (CDTXMania.DTX.bHasChips.LBD == false) && 
                    (CDTXMania.DTX.bHasChips.FT == false) && 
                    (CDTXMania.DTX.bHasChips.Ride == false)))
                {
                    num = ((float)CDTXMania.stageSongSelection.rChosenScore.SongInformation.Level.Drums);
                }
                else
                {
                    if (CDTXMania.stageSongSelection.rChosenScore.SongInformation.Level.Drums > 100)
                    {
                        num = ((float)CDTXMania.stageSongSelection.rChosenScore.SongInformation.Level.Drums);
                    }
                    else
                    {
                        num = ((float)CDTXMania.stageSongSelection.rChosenScore.SongInformation.Level.Drums) / 10f;
                    }
                }
                //If Skill Mode is CLASSIC, always display lvl as Classic Style
                if (CDTXMania.ConfigIni.nSkillMode == 0 || (CDTXMania.ConfigIni.bCLASSIC譜面判別を有効にする && 
                    (CDTXMania.DTX.bHasChips.LeftCymbal == false) && 
                    (CDTXMania.DTX.bHasChips.LP == false) && 
                    (CDTXMania.DTX.bHasChips.LBD == false) && 
                    (CDTXMania.DTX.bHasChips.FT == false) && 
                    (CDTXMania.DTX.bHasChips.Ride == false) &&
                    (CDTXMania.DTX.bForceXGChart == false)))
                {
                    graphics.DrawString(string.Format("{0:00}", num), ftSongDifficultyFont, new SolidBrush(Color.FromArgb(0xba, 0xba, 0xba)), (float)0f, (float)-4f);
                }
                else
                {
                    graphics.DrawString(string.Format("{0:0.00}", num), ftSongDifficultyFont, new SolidBrush(Color.FromArgb(0xba, 0xba, 0xba)), (float)0f, (float)-4f);
                }
                txSongLevel = new CTexture(CDTXMania.app.Device, bitmap3, CDTXMania.TextureFormat, false);
                graphics.Dispose();
                bitmap3.Dispose();
                Bitmap bitmap4 = new Bitmap(0x2a, 0x30);
                graphics = Graphics.FromImage(bitmap4);
                int speedTexturePosY = CDTXMania.ConfigIni.nScrollSpeed.Drums * 48 > 20 * 48 ? 20 * 48 : CDTXMania.ConfigIni.nScrollSpeed.Drums * 48;
                graphics.DrawImage(iDrumSpeed, new Rectangle(0, 0, 0x2a, 0x30), new Rectangle(0, speedTexturePosY, 0x2a, 0x30), GraphicsUnit.Pixel);
                txDrumSpeed = new CTexture(CDTXMania.app.Device, bitmap4, CDTXMania.TextureFormat, false);
                graphics.Dispose();
                //graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
                bitmap4.Dispose();
                base.OnManagedCreateResources();
            }
        }
        public override void OnManagedReleaseResources()
        {
            if (!bNotActivated)
            {
                CDTXMania.tDisposeSafely(ref ftSongDifficultyFont);
                CDTXMania.tDisposeSafely(ref iDrumSpeed);
                CDTXMania.tReleaseTexture(ref txジャケットパネル);
                CDTXMania.tReleaseTexture(ref txリザルト画像);
                CDTXMania.tReleaseTexture(ref txリザルト画像がないときの画像);
                CDTXMania.tReleaseTexture(ref txSongName);
                CDTXMania.tReleaseTexture(ref txArtistName);
                CDTXMania.tReleaseTexture(ref txModifiedPlaySpeed);
                CDTXMania.tReleaseTexture(ref txTrainingMode);
                CDTXMania.tReleaseTexture(ref txResultsNotSaved);
                CDTXMania.tReleaseTexture(ref r表示するリザルト画像);
                CDTXMania.tReleaseTexture(ref txSongLevel);
                CDTXMania.tReleaseTexture(ref txSongDifficulty);
                CDTXMania.tReleaseTexture(ref txDrumSpeed);

                base.OnManagedReleaseResources();
            }
        }
        public override unsafe int OnUpdateAndDraw()
        {
            if (bNotActivated)
            {
                return 0;
            }
            if (bJustStartedUpdate)
            {
                ct登場用 = new CCounter(0, 100, 5, CDTXMania.Timer);
                bJustStartedUpdate = false;
            }
            ct登場用.tUpdate();
            int x = n本体X;
            int y = n本体Y;
            txジャケットパネル.tDraw2D(CDTXMania.app.Device, 467, 287);
            if (txリザルト画像 != null)
            {
                Matrix mat = Matrix.Identity;
                float fScalingFactor;
                float jacketOnScreenSize = 245.0f;
                //Maintain aspect ratio by scaling only to the smaller scalingFactor
                if (jacketOnScreenSize / txリザルト画像.szImageSize.Width > jacketOnScreenSize / txリザルト画像.szImageSize.Height)
                {
                    fScalingFactor = jacketOnScreenSize / txリザルト画像.szImageSize.Height;
                }
                else
                {
                    fScalingFactor = jacketOnScreenSize / txリザルト画像.szImageSize.Width;
                }
                mat *= Matrix.Scaling(fScalingFactor, fScalingFactor, 1f);
                mat *= Matrix.Translation(-28f, -94.5f, 0f);
                mat *= Matrix.RotationZ(0.3f);

                txリザルト画像.tDraw3D(CDTXMania.app.Device, mat);
            }

            if (txSongName.szImageSize.Width > 320)
                txSongName.vcScaleRatio.X = 320f / txSongName.szImageSize.Width;

            if (txArtistName.szImageSize.Width > 320)
                txArtistName.vcScaleRatio.X = 320f / txArtistName.szImageSize.Width;

            txSongName.tDraw2D(CDTXMania.app.Device, 500, 630);
            txArtistName.tDraw2D(CDTXMania.app.Device, 500, 665);

            int nDisclaimerY = 360;
            if (CDTXMania.ConfigIni.nPlaySpeed != 20)
            {
                txModifiedPlaySpeed.tDraw2D(CDTXMania.app.Device, 840, nDisclaimerY);
                nDisclaimerY += 25;
            }
            if (CDTXMania.stageResult.bIsTrainingMode)
            {
                txTrainingMode.tDraw2D(CDTXMania.app.Device, 840, nDisclaimerY);
                nDisclaimerY += 25;
            }
            if (CDTXMania.stageResult.bIsTrainingMode || ((CDTXMania.ConfigIni.nPlaySpeed != 20) && !CDTXMania.ConfigIni.bSaveScoreIfModifiedPlaySpeed))
            {
                txResultsNotSaved.tDraw2D(CDTXMania.app.Device, 840, nDisclaimerY);
            }

            if (!ct登場用.bReachedEndValue)
            {
                return 0;
            }
            return 1;
        }


        // Other

        #region [ private ]
        //-----------------
        private CCounter ct登場用;
        private Font ftSongDifficultyFont;        
        private Image iDrumSpeed;
        private int n本体X;
        private int n本体Y;
        private CTexture r表示するリザルト画像;
        private string strSongName;
        private CTexture txDrumSpeed;
        private CTexture txSongDifficulty;
        private CTexture txSongLevel;
        private CTexture txリザルト画像;
        private CTexture txリザルト画像がないときの画像;
        private CTexture txジャケットパネル;

        private CTexture txSongName;
        private CTexture txArtistName;
        private CTexture txModifiedPlaySpeed;
        private CTexture txTrainingMode;
        private CTexture txResultsNotSaved;

        //2014.04.05.kairera0467 GITADORAグラデーションの色。
        //本当は共通のクラスに設置してそれを参照する形にしたかったが、なかなかいいメソッドが無いため、とりあえず個別に設置。
        //private Color clGITADORAgradationTopColor = Color.FromArgb(0, 220, 200);
        //private Color clGITADORAgradationBottomColor = Color.FromArgb(255, 250, 40);
        private Color clGITADORAgradationTopColor = Color.FromArgb(255, 255, 255);
        private Color clGITADORAgradationBottomColor = Color.FromArgb(255, 255, 255);

        private bool tプレビュー画像の指定があれば構築する()
        {
            if (string.IsNullOrEmpty(CDTXMania.DTX.PREIMAGE))
            {
                return false;
            }
            CDTXMania.tReleaseTexture(ref txリザルト画像);
            r表示するリザルト画像 = null;
            string path = CDTXMania.DTX.strFolderName + CDTXMania.DTX.PREIMAGE;
            if (!File.Exists(path))
            {
                Trace.TraceWarning("ファイルが存在しません。({0})", new object[] { path });
                return false;
            }
            txリザルト画像 = CDTXMania.tGenerateTexture(path);
            r表示するリザルト画像 = txリザルト画像;
            return (r表示するリザルト画像 != null);
        }
        private bool tリザルト画像の指定があれば構築する()
        {
            int rank = CScoreIni.tCalculateOverallRankValue(CDTXMania.stageResult.stPerformanceEntry.Drums, CDTXMania.stageResult.stPerformanceEntry.Guitar, CDTXMania.stageResult.stPerformanceEntry.Bass);
            if (rank == 99)	// #23534 2010.10.28 yyagi: 演奏チップが0個のときは、rankEと見なす
            {
                rank = 6;
            }
            if (string.IsNullOrEmpty(CDTXMania.DTX.RESULTIMAGE[rank]))
            {
                return false;
            }
            CDTXMania.tReleaseTexture(ref txリザルト画像);
            r表示するリザルト画像 = null;
            string path = CDTXMania.DTX.strFolderName + CDTXMania.DTX.RESULTIMAGE[rank];
            if (!File.Exists(path))
            {
                Trace.TraceWarning("ファイルが存在しません。({0})", new object[] { path });
                return false;
            }
            txリザルト画像 = CDTXMania.tGenerateTexture(path);
            r表示するリザルト画像 = txリザルト画像;
            return (r表示するリザルト画像 != null);
        }
        //-----------------
        #endregion
    }
}
