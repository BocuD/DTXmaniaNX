using System.Diagnostics;
using DTXMania.Core;
using DTXMania.Core.Video;
using FDK;

namespace DTXMania;

//todo: this class should be removed fully
public class CAVI : IDisposable
    {
        //public CAviDS avi;
        private bool bDispose済み;
        public int n番号;
        public string strComment = "";
        public string strFileName = "";
        public double dbPlaySpeed = 1.0;

        public bool fileExists = false;
        public TimeSpan duration = TimeSpan.Zero;

        public CAVI(int number, string filename, string comment, double playSpeed)
        {
            n番号 = number;
            strFileName = filename;
            strComment = comment;
            dbPlaySpeed = playSpeed;
        }

        public void OnDeviceCreated()
        {
            #region [ strAVIファイル名の作成。]

            //-----------------
            string strAVIFileName;
            //strAVIファイル名 = CSkin.Path(@"Graphics\7_Movie.avi");

            if (CDTXMania.DTX == null || Path.IsPathRooted(strFileName))
            {
                strAVIFileName = strFileName;
            }
            else
            {
                if (!string.IsNullOrEmpty(CDTXMania.DTX.PATH_WAV))
                    strAVIFileName = CDTXMania.DTX.PATH_WAV + strFileName;
                else
                    strAVIFileName = CDTXMania.DTX.strFolderName + CDTXMania.DTX.PATH + strFileName;
            }

            //-----------------

            #endregion

            if (!File.Exists(strAVIFileName))
            {
                //Trace.TraceWarning( "File doesn't exist!({0})({1})", this.strコメント文, strAVIファイル名 );
                Trace.TraceWarning("File does not exist. ({0})({1})", strComment, strAVIFileName);
                fileExists = false;
                return;
            }

            // AVI の生成。

            try
            {
                SoftwareVideoPlayer player = new();
                if (player.Open(strAVIFileName))
                {
                    duration = player.Duration;
                    fileExists = true;
                }
                
                player.Dispose();
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);
                Trace.TraceError("動画の生成に失敗しました。({0})({1})", strComment, strAVIFileName);
            }
        }

        public override string ToString()
        {
            return string.Format("CAVI{0}: File:{1}, Comment:{2}", CDTX.Base36ToString(n番号), strFileName, strComment);
        }

        #region [ IDisposable 実装 ]

        //-----------------
        public void Dispose()
        {
            if (bDispose済み)
                return;

            // if (avi != null)
            // {
            //     avi.Dispose();
            //     avi = null;
            //
            //     Trace.TraceInformation("動画を解放しました。({0})({1})", strComment, strFileName);
            // }

            bDispose済み = true;
        }

        //-----------------

        #endregion
    }