using System.Diagnostics;
using DTXMania.Core;
using FDK;

namespace DTXMania;

public class CAVI : IDisposable
    {
        public CAviDS avi;
        private bool bDispose済み;
        public int n番号;
        public string strコメント文 = "";
        public string strファイル名 = "";
        public double dbPlaySpeed = 1.0;

        public CAVI(int number, string filename, string comment, double playSpeed)
        {
            n番号 = number;
            strファイル名 = filename;
            strコメント文 = comment;
            dbPlaySpeed = playSpeed;
            //taskLoad = null;
        }

        public void OnDeviceCreated()
        {
            #region [ strAVIファイル名の作成。]

            //-----------------
            string strAVIFileName;
            //strAVIファイル名 = CSkin.Path(@"Graphics\7_Movie.avi");

            if (CDTXMania.DTX == null || Path.IsPathRooted(strファイル名))
            {
                strAVIFileName = strファイル名;
            }
            else
            {
                if (!string.IsNullOrEmpty(CDTXMania.DTX.PATH_WAV))
                    strAVIFileName = CDTXMania.DTX.PATH_WAV + strファイル名;
                else
                    strAVIFileName = CDTXMania.DTX.strFolderName + CDTXMania.DTX.PATH + strファイル名;
            }

            //-----------------

            #endregion

            if (!File.Exists(strAVIFileName))
            {
                //Trace.TraceWarning( "File doesn't exist!({0})({1})", this.strコメント文, strAVIファイル名 );
                Trace.TraceWarning("File does not exist. ({0})({1})", strコメント文, strAVIFileName);
                avi = null;
                return;
            }

            // AVI の生成。

            try
            {
                avi = new CAviDS(strAVIFileName, dbPlaySpeed);
                Trace.TraceInformation("動画を生成しました。({0})({1})({2}msec)", strコメント文, strAVIFileName, avi.GetDuration());
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);
                Trace.TraceError("動画の生成に失敗しました。({0})({1})", strコメント文, strAVIFileName);
                avi = null;
            }
        }

        public override string ToString()
        {
            return string.Format("CAVI{0}: File:{1}, Comment:{2}", CDTX.Base36ToString(n番号), strファイル名, strコメント文);
        }

        #region [ IDisposable 実装 ]

        //-----------------
        public void Dispose()
        {
            if (bDispose済み)
                return;

            if (avi != null)
            {
                avi.Dispose();
                avi = null;

                Trace.TraceInformation("動画を解放しました。({0})({1})", strコメント文, strファイル名);
            }

            bDispose済み = true;
        }

        //-----------------

        #endregion
    }