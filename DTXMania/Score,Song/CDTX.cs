using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Globalization;
using DTXMania.Core;
using FDK;

namespace DTXMania;

public class CDTX : CActivity
{
    // 定数

    public enum EType
    {
        DTX,
        GDA,
        G2D,
        BMS,
        BME,
        SMF
    }

    public enum Eレーンビットパターン
    {
        OPEN = 0,
        xxB = 1,
        xGx = 2,
        xGB = 3,
        Rxx = 4,
        RxB = 5,
        RGx = 6,
        RGB = 7,
        xxxYx = 10,
        xxBYx,
        xGxYx,
        xGBYx,
        RxxYx,
        RxBYx,
        RGxYx,
        RGBYx,
        xxxxP = 20,
        xxBxP,
        xGxxP,
        xGBxP,
        RxxxP,
        RxBxP,
        RGxxP,
        RGBxP,
        xxxYP = 30,
        xxBYP,
        xGxYP,
        xGBYP,
        RxxYP,
        RxBYP,
        RGxYP,
        RGBYP
    };


    // クラス
    public class CAVIPAN
    {
        public int nAVI番号;
        public int n移動時間ct;
        public int n番号;
        public Point pt動画側開始位置 = new(0, 0);
        public Point pt動画側終了位置 = new(0, 0);
        public Point pt表示側開始位置 = new(0, 0);
        public Point pt表示側終了位置 = new(0, 0);
        public Size sz開始サイズ = new(0, 0);
        public Size sz終了サイズ = new(0, 0);

        public override string ToString()
        {
            return string.Format(
                "CAVIPAN{0}: AVI:{14}, 開始サイズ:{1}x{2}, 終了サイズ:{3}x{4}, 動画側開始位置:{5}x{6}, 動画側終了位置:{7}x{8}, 表示側開始位置:{9}x{10}, 表示側終了位置:{11}x{12}, 移動時間:{13}ct",
                Base36ToString(n番号),
                sz開始サイズ.Width, sz開始サイズ.Height,
                sz終了サイズ.Width, sz終了サイズ.Height,
                pt動画側開始位置.X, pt動画側開始位置.Y,
                pt動画側終了位置.X, pt動画側終了位置.Y,
                pt表示側開始位置.X, pt表示側開始位置.Y,
                pt表示側終了位置.X, pt表示側終了位置.Y,
                n移動時間ct,
                Base36ToString(nAVI番号));
        }
    }

    public class CBGA
    {
        public int nBMP番号;
        public int n番号;
        public Point pt画像側右下座標 = new(0, 0);
        public Point pt画像側左上座標 = new(0, 0);
        public Point pt表示座標 = new(0, 0);

        public override string ToString()
        {
            return string.Format("CBGA{0}, BMP:{1}, 画像側左上座標:{2}x{3}, 画像側右下座標:{4}x{5}, 表示座標:{6}x{7}",
                Base36ToString(n番号),
                Base36ToString(nBMP番号),
                pt画像側左上座標.X, pt画像側左上座標.Y,
                pt画像側右下座標.X, pt画像側右下座標.Y,
                pt表示座標.X, pt表示座標.Y);
        }
    }

    public class CBGAPAN
    {
        public int nBMP番号;
        public int n移動時間ct;
        public int n番号;
        public Point pt画像側開始位置 = new(0, 0);
        public Point pt画像側終了位置 = new(0, 0);
        public Point pt表示側開始位置 = new(0, 0);
        public Point pt表示側終了位置 = new(0, 0);
        public Size sz開始サイズ = new(0, 0);
        public Size sz終了サイズ = new(0, 0);

        public override string ToString()
        {
            return string.Format(
                "CBGAPAN{0}: BMP:{14}, 開始サイズ:{1}x{2}, 終了サイズ:{3}x{4}, 画像側開始位置:{5}x{6}, 画像側終了位置:{7}x{8}, 表示側開始位置:{9}x{10}, 表示側終了位置:{11}x{12}, 移動時間:{13}ct",
                Base36ToString(nBMP番号),
                sz開始サイズ.Width, sz開始サイズ.Height,
                sz終了サイズ.Width, sz終了サイズ.Height,
                pt画像側開始位置.X, pt画像側開始位置.Y,
                pt画像側終了位置.X, pt画像側終了位置.Y,
                pt表示側開始位置.X, pt表示側開始位置.Y,
                pt表示側終了位置.X, pt表示側終了位置.Y,
                n移動時間ct,
                Base36ToString(nBMP番号));
        }
    }

    public class CBMP : CBMPbase, IDisposable
    {
        public CBMP()
        {
            b黒を透過する = true; // BMPでは、黒を透過色とする
        }

        public override void PutLog(string strテクスチャファイル名)
        {
            Trace.TraceInformation("テクスチャを生成しました。({0})({1})({2}x{3})", strコメント文, strテクスチャファイル名, n幅, n高さ);
        }

        public override string ToString()
        {
            return string.Format("CBMP{0}: File:{1}, Comment:{2}", Base36ToString(n番号), strファイル名, strコメント文);
        }
    }

    public class CBMPTEX : CBMPbase, IDisposable
    {
        public CBMPTEX()
        {
            b黒を透過する = false; // BMPTEXでは、透過色はαで表現する
        }

        public override void PutLog(string strテクスチャファイル名)
        {
            Trace.TraceInformation("テクスチャを生成しました。({0})({1})(Gr:{2}x{3})(Tx:{4}x{5})", strコメント文, strテクスチャファイル名,
                tx画像.szImageSize.Width, tx画像.szImageSize.Height, tx画像.szTextureSize.Width,
                tx画像.szTextureSize.Height);
        }

        public override string ToString()
        {
            return string.Format("CBMPTEX{0}: File:{1}, Comment:{2}", Base36ToString(n番号), strファイル名, strコメント文);
        }
    }

    public class CBMPbase : IDisposable
    {
        public bool bUse;
        public int n番号;
        public string strコメント文 = "";
        public string strファイル名 = "";
        public CTexture tx画像;
        public int n高さ => tx画像.szImageSize.Height;

        public int n幅 => tx画像.szImageSize.Width;
        public bool b黒を透過する;
        public Bitmap bitmap;

        public string GetFullPathname
        {
            get
            {
                if (CDTXMania.DTX != null)
                {
                    if (!string.IsNullOrEmpty(CDTXMania.DTX.PATH_WAV))
                        return CDTXMania.DTX.PATH_WAV + strファイル名;
                    else
                        return CDTXMania.DTX.strFolderName + CDTXMania.DTX.PATH + strファイル名;
                }

                return "";
            }
        }

        public void OnDeviceCreated()
        {
            #region [ strテクスチャファイル名 を作成。]

            string strテクスチャファイル名 = GetFullPathname;

            #endregion

            if (!File.Exists(strテクスチャファイル名))
            {
                Trace.TraceWarning("ファイルが存在しません。({0})({1})", strコメント文, strテクスチャファイル名);
                tx画像 = null;
                return;
            }

            // テクスチャを作成。
            byte[] txData = File.ReadAllBytes(strテクスチャファイル名);
            tx画像 = CDTXMania.tGenerateTexture(txData, b黒を透過する);

            if (tx画像 != null)
            {
                // 作成成功。
                if (CDTXMania.ConfigIni.bLog作成解放ログ出力)
                    PutLog(strテクスチャファイル名);
                txData = null;
                bUse = true;
            }
            else
            {
                // 作成失敗。
                Trace.TraceError("テクスチャの生成に失敗しました。({0})({1})", strコメント文, strテクスチャファイル名);
                tx画像 = null;
            }
        }

        /// <summary>
        /// BGA画像のデコードをTexture()に渡す前に行う、OnDeviceCreate()
        /// </summary>
        /// <param name="bitmap">テクスチャ画像</param>
        /// <param name="strテクスチャファイル名">ファイル名</param>
        public void OnDeviceCreated(Bitmap bitmap, string strテクスチャファイル名)
        {
            if (bitmap != null && b黒を透過する)
            {
                bitmap.MakeTransparent(Color.Black); // 黒を透過色にする
            }

            tx画像 = CDTXMania.tGenerateTexture(bitmap, b黒を透過する);

            if (tx画像 != null)
            {
                // 作成成功。
                if (CDTXMania.ConfigIni.bLog作成解放ログ出力)
                    PutLog(strテクスチャファイル名);
                bUse = true;
            }
            else
            {
                // 作成失敗。
                Trace.TraceError("テクスチャの生成に失敗しました。({0})({1})", strコメント文, strテクスチャファイル名);
                tx画像 = null;
            }

            if (bitmap != null)
            {
                bitmap.Dispose();
            }
        }

        public virtual void PutLog(string strテクスチャファイル名)
        {
        }

        #region [ IDisposable 実装 ]

        //-----------------
        public void Dispose()
        {
            if (bDisposed済み)
                return;

            if (tx画像 != null)
            {
                #region [ strテクスチャファイル名 を作成。]

                //-----------------
                string strテクスチャファイル名 = GetFullPathname;
                //if( !string.IsNullOrEmpty( CDTXMania.DTX.PATH_WAV ) )
                //    strテクスチャファイル名 = CDTXMania.DTX.PATH_WAV + this.strFilename;
                //else
                //    strテクスチャファイル名 = CDTXMania.DTX.strFolderName + this.strFilename;
                //-----------------

                #endregion

                CDTXMania.tReleaseTexture(ref tx画像);

                if (CDTXMania.ConfigIni.bLog作成解放ログ出力)
                    Trace.TraceInformation("テクスチャを解放しました。({0})({1})", strコメント文, strテクスチャファイル名);
            }

            bUse = false;

            bDisposed済み = true;
        }

        #endregion

        #region [ private ]

        //-----------------
        private bool bDisposed済み;
        //-----------------

        #endregion
    }

    public class CBPM
    {
        public double dbBPM値;
        public int n内部番号;
        public int n表記上の番号;

        public override string ToString()
        {
            StringBuilder builder = new(0x80);
            if (n内部番号 != n表記上の番号)
            {
                builder.Append(string.Format("CBPM{0}(内部{1})", Base36ToString(n表記上の番号), n内部番号));
            }
            else
            {
                builder.Append(string.Format("CBPM{0}", Base36ToString(n表記上の番号)));
            }

            builder.Append(string.Format(", BPM:{0}", dbBPM値));
            return builder.ToString();
        }
    }

    public class CWAV : IDisposable
    {
        public bool bBGMとして使う;
        public List<EChannel> listこのWAVを使用するチャンネル番号の集合 = new(16);
        public int nChipSize = 100;
        public int nPosition;
        public long[] nPauseTime = new long[CDTXMania.ConfigIni.nPoliphonicSounds]; // 4
        public int nVolume = 100;
        public int n現在再生中のサウンド番号;
        public long[] nPlayStartTime = new long[CDTXMania.ConfigIni.nPoliphonicSounds]; // 4
        public int n内部番号;
        public int n表記上の番号;
        public CSound[] rSound = new CSound[CDTXMania.ConfigIni.nPoliphonicSounds]; // 4
        public string strコメント文 = "";
        public string strFileName = "";

        public bool bBGMとして使わない
        {
            get => !bBGMとして使う;
            set => bBGMとして使う = !value;
        }

        public bool bIsBassSound = false;
        public bool bIsGuitarSound = false;
        public bool bIsDrumsSound = false;
        public bool bIsSESound = false;
        public bool bIsBGMSound = false;

        public override string ToString()
        {
            var sb = new StringBuilder(128);

            if (n表記上の番号 == n内部番号)
            {
                sb.Append(string.Format("CWAV{0}: ", Base36ToString(n表記上の番号)));
            }
            else
            {
                sb.Append(string.Format("CWAV{0}(内部{1}): ", Base36ToString(n表記上の番号), n内部番号));
            }

            sb.Append(string.Format("音量:{0}, 位置:{1}, サイズ:{2}, BGM:{3}, File:{4}, Comment:{5}", nVolume, nPosition, nChipSize,
                bBGMとして使う ? 'Y' : 'N', strFileName, strコメント文));

            return sb.ToString();
        }

        #region [ Dispose-Finalize パターン実装 ]

        //-----------------
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool bManagedリソースの解放も行う)
        {
            if (bDisposed済み)
                return;

            if (bManagedリソースの解放も行う)
            {
                for (int i = 0; i < CDTXMania.ConfigIni.nPoliphonicSounds; i++) // 4
                {
                    if (rSound[i] != null)
                        CDTXMania.SoundManager.tDiscard(rSound[i]);
                    rSound[i] = null;

                    if ((i == 0) && CDTXMania.ConfigIni.bLog作成解放ログ出力)
                        Trace.TraceInformation("サウンドを解放しました。({0})({1})", strコメント文, strFileName);
                }
            }

            bDisposed済み = true;
        }

        ~CWAV()
        {
            Dispose(false);
        }

        //-----------------

        #endregion

        #region [ private ]

        //-----------------
        private bool bDisposed済み;
        //-----------------

        #endregion
    }


    // 構造体

    public struct STLANEINT
    {
        private int HH;
        private int SD;
        private int BD;
        private int HT;
        private int LT;
        private int CY;
        private int FT;
        private int HHO;
        private int RD;
        private int LC;
        private int LP;
        private int LBD;

        //Guitar
        private int GuitarR;
        private int GuitarG;
        private int GuitarB;
        private int GuitarY;
        private int GuitarP;

        private int GuitarOpen;

        //Bass
        private int BassR;
        private int BassG;
        private int BassB;
        private int BassY;
        private int BassP;
        private int BassOpen;

        public int Drums => HH + SD + BD + HT + LT + CY + FT + HHO + RD + LC + LP + LBD;
        public int Guitar;

        public int Bass;

        public int this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return HH;

                    case 1:
                        return SD;

                    case 2:
                        return BD;

                    case 3:
                        return HT;

                    case 4:
                        return LT;

                    case 5:
                        return CY;

                    case 6:
                        return FT;

                    case 7:
                        return HHO;

                    case 8:
                        return RD;

                    case 9:
                        return LC;

                    case 10:
                        return LP;

                    case 11:
                        return LBD;
                    case 12:
                        return Guitar;
                    case 13:
                        return Bass;
                }

                throw new IndexOutOfRangeException();
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                switch (index)
                {
                    case 0:
                        HH = value;
                        return;

                    case 1:
                        SD = value;
                        return;

                    case 2:
                        BD = value;
                        return;

                    case 3:
                        HT = value;
                        return;

                    case 4:
                        LT = value;
                        return;

                    case 5:
                        CY = value;
                        return;

                    case 6:
                        FT = value;
                        return;

                    case 7:
                        HHO = value;
                        return;

                    case 8:
                        RD = value;
                        return;

                    case 9:
                        LC = value;
                        return;

                    case 10:
                        LP = value;
                        return;

                    case 11:
                        LBD = value;
                        return;
                    case 12:
                        Guitar = value;
                        return;
                    case 13:
                        Bass = value;
                        return;
                }

                throw new IndexOutOfRangeException();
            }
        }

        public void incrementChipCount(EChannel channelNum)
        {
            switch (channelNum)
            {
                //Guitar Channels
                case EChannel.Guitar_Open:
                    GuitarOpen++;
                    break;
                case EChannel.Guitar_xxBxx:
                    GuitarB++;
                    break;
                case EChannel.Guitar_xGxxx:
                    GuitarG++;
                    break;
                case EChannel.Guitar_xGBxx:
                    GuitarG++;
                    GuitarB++;
                    break;
                case EChannel.Guitar_Rxxxx:
                    GuitarR++;
                    break;
                case EChannel.Guitar_RxBxx:
                    GuitarR++;
                    GuitarB++;
                    break;
                case EChannel.Guitar_RGxxx:
                    GuitarR++;
                    GuitarG++;
                    break;
                case EChannel.Guitar_RGBxx:
                    GuitarR++;
                    GuitarG++;
                    GuitarB++;
                    break;
                case EChannel.Guitar_Wailing:
                    //Guitar Wail
                    break;
                case EChannel.Guitar_xxxYx:
                    GuitarY++;
                    break;
                case EChannel.Guitar_xxBYx:
                    GuitarB++;
                    GuitarY++;
                    break;
                case EChannel.Guitar_xGxYx:
                    GuitarG++;
                    GuitarY++;
                    break;
                case EChannel.Guitar_xGBYx:
                    GuitarG++;
                    GuitarB++;
                    GuitarY++;
                    break;
                case EChannel.Guitar_RxxYx:
                    GuitarR++;
                    GuitarY++;
                    break;
                case EChannel.Guitar_RxBYx:
                    GuitarR++;
                    GuitarB++;
                    GuitarY++;
                    break;
                case EChannel.Guitar_RGxYx:
                    GuitarR++;
                    GuitarG++;
                    GuitarY++;
                    break;
                case EChannel.Guitar_RGBYx:
                    GuitarR++;
                    GuitarG++;
                    GuitarB++;
                    GuitarY++;
                    break;
                case EChannel.Guitar_xxxxP:
                    GuitarP++;
                    break;
                case EChannel.Guitar_xxBxP:
                    GuitarB++;
                    GuitarP++;
                    break;
                case EChannel.Guitar_xGxxP:
                    GuitarG++;
                    GuitarP++;
                    break;
                case EChannel.Guitar_xGBxP:
                    GuitarG++;
                    GuitarB++;
                    GuitarP++;
                    break;
                case EChannel.Guitar_RxxxP:
                    GuitarR++;
                    GuitarP++;
                    break;
                case EChannel.Guitar_RxBxP:
                    GuitarR++;
                    GuitarB++;
                    GuitarP++;
                    break;
                case EChannel.Guitar_RGxxP:
                    GuitarR++;
                    GuitarG++;
                    GuitarP++;
                    break;
                case EChannel.Guitar_RGBxP:
                    GuitarR++;
                    GuitarG++;
                    GuitarB++;
                    GuitarP++;
                    break;
                case EChannel.Guitar_xxxYP:
                    GuitarY++;
                    GuitarP++;
                    break;
                case EChannel.Guitar_xxBYP:
                    GuitarB++;
                    GuitarY++;
                    GuitarP++;
                    break;
                case EChannel.Guitar_xGxYP:
                    GuitarG++;
                    GuitarY++;
                    GuitarP++;
                    break;
                case EChannel.Guitar_xGBYP:
                    GuitarG++;
                    GuitarB++;
                    GuitarY++;
                    GuitarP++;
                    break;
                case EChannel.Guitar_RxxYP:
                    GuitarR++;
                    GuitarY++;
                    GuitarP++;
                    break;
                case EChannel.Guitar_RxBYP:
                    GuitarR++;
                    GuitarB++;
                    GuitarY++;
                    GuitarP++;
                    break;
                case EChannel.Guitar_RGxYP:
                    GuitarR++;
                    GuitarG++;
                    GuitarY++;
                    GuitarP++;
                    break;
                case EChannel.Guitar_RGBYP:
                    GuitarR++;
                    GuitarG++;
                    GuitarB++;
                    GuitarY++;
                    GuitarP++;
                    break;
                //Bass
                case EChannel.Bass_Open:
                    BassOpen++;
                    break;
                case EChannel.Bass_xxBxx:
                    BassB++;
                    break;
                case EChannel.Bass_xGxxx:
                    BassG++;
                    break;
                case EChannel.Bass_xGBxx:
                    BassG++;
                    BassB++;
                    break;
                case EChannel.Bass_Rxxxx:
                    BassR++;
                    break;
                case EChannel.Bass_RxBxx:
                    BassR++;
                    BassB++;
                    break;
                case EChannel.Bass_RGxxx:
                    BassR++;
                    BassG++;
                    break;
                case EChannel.Bass_RGBxx:
                    BassR++;
                    BassG++;
                    BassB++;
                    break;
                case EChannel.Bass_Wailing:
                    //Bass Wail
                    break;
                case EChannel.Bass_xxxYx:
                    BassY++;
                    break;
                case EChannel.Bass_xxBYx:
                    BassB++;
                    BassY++;
                    break;
                case EChannel.Bass_xGxYx:
                    BassG++;
                    BassY++;
                    break;
                case EChannel.Bass_xGBYx:
                    BassG++;
                    BassB++;
                    BassY++;
                    break;
                case EChannel.Bass_RxxYx:
                    BassR++;
                    BassY++;
                    break;
                case EChannel.Bass_RxBYx:
                    BassR++;
                    BassB++;
                    BassY++;
                    break;
                case EChannel.Bass_RGxYx:
                    BassR++;
                    BassG++;
                    BassY++;
                    break;
                case EChannel.Bass_RGBYx:
                    BassR++;
                    BassG++;
                    BassB++;
                    BassY++;
                    break;
                case EChannel.Bass_xxxxP:
                    BassP++;
                    break;
                case EChannel.Bass_xxBxP:
                    BassB++;
                    BassP++;
                    break;
                case EChannel.Bass_xGxxP:
                    BassG++;
                    BassP++;
                    break;
                case EChannel.Bass_xGBxP:
                    BassG++;
                    BassB++;
                    BassP++;
                    break;
                case EChannel.Bass_RxxxP:
                    BassR++;
                    BassP++;
                    break;
                case EChannel.Bass_RxBxP:
                    BassR++;
                    BassB++;
                    BassP++;
                    break;
                case EChannel.Bass_RGxxP:
                    BassR++;
                    BassG++;
                    BassP++;
                    break;
                case EChannel.Bass_RGBxP:
                    BassR++;
                    BassG++;
                    BassB++;
                    BassP++;
                    break;
                case EChannel.Bass_xxxYP:
                    BassY++;
                    BassP++;
                    break;
                case EChannel.Bass_xxBYP:
                    BassB++;
                    BassY++;
                    BassP++;
                    break;
                case EChannel.Bass_xGxYP:
                    BassG++;
                    BassY++;
                    BassP++;
                    break;
                case EChannel.Bass_xGBYP:
                    BassG++;
                    BassB++;
                    BassY++;
                    BassP++;
                    break;
                case EChannel.Bass_RxxYP:
                    BassR++;
                    BassY++;
                    BassP++;
                    break;
                case EChannel.Bass_RxBYP:
                    BassR++;
                    BassB++;
                    BassY++;
                    BassP++;
                    break;
                case EChannel.Bass_RGxYP:
                    BassR++;
                    BassG++;
                    BassY++;
                    BassP++;
                    break;
                case EChannel.Bass_RGBYP:
                    BassR++;
                    BassG++;
                    BassB++;
                    BassY++;
                    BassP++;
                    break;
            }
        }

        public void swapGuitarBassLaneChipCounters()
        {
            int temp_R = GuitarR;
            int temp_G = GuitarG;
            int temp_B = GuitarB;
            int temp_Y = GuitarY;
            int temp_P = GuitarP;
            int temp_Open = GuitarOpen;

            GuitarR = BassR;
            GuitarG = BassG;
            GuitarB = BassB;
            GuitarY = BassY;
            GuitarP = BassP;
            GuitarOpen = BassOpen;

            BassR = temp_R;
            BassG = temp_G;
            BassB = temp_B;
            BassY = temp_Y;
            BassP = temp_P;
            BassOpen = temp_Open;
        }

        public int chipCountInLane(ELane eLane)
        {
            switch (eLane)
            {
                case ELane.LC:
                    return LC;
                case ELane.HH:
                    return HH + HHO;
                case ELane.SD:
                    return SD;
                case ELane.LP:
                    return LP + LBD;
                //case ELane.LBD:
                //	return this.LBD;
                case ELane.HT:
                    return HT;
                case ELane.BD:
                    return BD;
                case ELane.LT:
                    return LT;
                case ELane.FT:
                    return FT;
                case ELane.CY:
                    return CY + RD;
                //case ELane.RD:
                //	return this.RD;
                case ELane.GtR:
                    return GuitarR;
                case ELane.GtG:
                    return GuitarG;
                case ELane.GtB:
                    return GuitarB;
                case ELane.GtY:
                    return GuitarY;
                case ELane.GtP:
                    return GuitarP;
                case ELane.GtPick:
                    return GuitarOpen;
                case ELane.BsR:
                    return BassR;
                case ELane.BsG:
                    return BassG;
                case ELane.BsB:
                    return BassB;
                case ELane.BsY:
                    return BassY;
                case ELane.BsP:
                    return BassP;
                case ELane.BsPick:
                    return BassOpen;
            }

            throw new IndexOutOfRangeException();
        }
    }

    public struct STRESULT
    {
        public string SS;
        public string S;
        public string A;
        public string B;
        public string C;
        public string D;
        public string E;

        public string this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return SS;

                    case 1:
                        return S;

                    case 2:
                        return A;

                    case 3:
                        return B;

                    case 4:
                        return C;

                    case 5:
                        return D;

                    case 6:
                        return E;
                }

                throw new IndexOutOfRangeException();
            }
            set
            {
                switch (index)
                {
                    case 0:
                        SS = value;
                        return;

                    case 1:
                        S = value;
                        return;

                    case 2:
                        A = value;
                        return;

                    case 3:
                        B = value;
                        return;

                    case 4:
                        C = value;
                        return;

                    case 5:
                        D = value;
                        return;

                    case 6:
                        E = value;
                        return;
                }

                throw new IndexOutOfRangeException();
            }
        }
    }

    public struct STHASCHIPS
    {
        public bool Drums;
        public bool Guitar;
        public bool Bass;

        public bool HHOpen;
        public bool LP;
        public bool LBD;
        public bool FT;
        public bool Ride;
        public bool LeftCymbal;
        public bool OpenGuitar;
        public bool OpenBass;
        public bool YPGuitar;
        public bool YPBass;
        public bool AVI;


        public bool this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return Drums;

                    case 1:
                        return Guitar;

                    case 2:
                        return Bass;

                    case 3:
                        return HHOpen;

                    case 4:
                        return LP;

                    case 5:
                        return LBD;

                    case 6:
                        return FT;

                    case 7:
                        return Ride;

                    case 8:
                        return LeftCymbal;

                    case 9:
                        return OpenGuitar;

                    case 10:
                        return OpenBass;

                    case 11:
                        return YPGuitar;

                    case 12:
                        return YPBass;

                    case 13:
                        return AVI;
                }

                throw new IndexOutOfRangeException();
            }
            set
            {
                switch (index)
                {
                    case 0:
                        Drums = value;
                        return;

                    case 1:
                        Guitar = value;
                        return;

                    case 2:
                        Bass = value;
                        return;

                    case 3:
                        HHOpen = value;
                        return;

                    case 4:
                        LP = value;
                        return;

                    case 5:
                        LBD = value;
                        return;

                    case 6:
                        FT = value;
                        return;

                    case 7:
                        Ride = value;
                        return;

                    case 8:
                        LeftCymbal = value;
                        return;

                    case 9:
                        OpenGuitar = value;
                        return;

                    case 10:
                        OpenBass = value;
                        return;

                    case 11:
                        YPGuitar = value;
                        return;

                    case 12:
                        YPBass = value;
                        return;

                    case 13:
                        AVI = value;
                        return;
                }

                throw new IndexOutOfRangeException();
            }
        }
    }


    // プロパティ

    public int nBGMAdjust { get; private set; }
    public string ARTIST;
    public string BACKGROUND;
    public string BACKGROUND_GR;
    public double BASEBPM;
    public bool BLACKCOLORKEY;
    public double BPM;
    public STHASCHIPS bHasChips;
    public string COMMENT;
    public double db再生速度;
    public EType eFileType;
    public string GENRE;
    public bool HIDDENLEVEL;
    public STDGBVALUE<int> LEVEL;
    public STDGBVALUE<int> LEVELDEC;

    public Dictionary<int, CAVI> listAVI;

    //public Dictionary<int, CDirectShow> listDS;
    public Dictionary<int, CAVIPAN> listAVIPAN;
    public Dictionary<int, CBGA> listBGA;
    public Dictionary<int, CBGAPAN> listBGAPAN;
    public Dictionary<int, CBMP> listBMP;
    public Dictionary<int, CBMPTEX> listBMPTEX;
    public Dictionary<int, CBPM> listBPM;
    public List<CChip> listChip;
    public Dictionary<int, CWAV> listWAV;
    public string MIDIFILE;
    public bool MIDINOTE;
    public int MIDIレベル;
    public STLANEINT nVisibleChipsCount;
    public int nボーナスチップ数;
    public const int n最大音数 = 4;
    public const int n小節の解像度 = 384;
    public string PANEL;
    public string PATH_WAV;
    public string PATH;
    public string PREIMAGE;
    public string PREMOVIE;
    public string PREVIEW;
    public STRESULT RESULTIMAGE;
    public STRESULT RESULTMOVIE;
    public STRESULT RESULTSOUND;
    public string SOUND_AUDIENCE;
    public string SOUND_FULLCOMBO;
    public string SOUND_NOWLOADING;
    public string SOUND_STAGEFAILED;
    public string STAGEFILE;
    public string strDTXFileHash;
    public string strFileName;
    public string strFileNameFullPath;
    public string strFolderName;
    public string TITLE;
    public bool bForceXGChart;
    public bool bVol137to100;
    public double dbDTXVPlaySpeed;
#if TEST_NOTEOFFMODE
		public STLANEVALUE<bool> b演奏で直前の音を消音する;
//		public bool bHH演奏で直前のHHを消音する;
//		public bool bGUITAR演奏で直前のGUITARを消音する;
//		public bool bBASS演奏で直前のBASSを消音する;
#endif
    // コンストラクタ

    public CDTX()
    {
        TITLE = "";
        ARTIST = "";
        COMMENT = "";
        PANEL = "";
        GENRE = "";
        PREVIEW = "";
        PREIMAGE = "";
        PREMOVIE = "";
        STAGEFILE = "";
        BACKGROUND = "";
        BACKGROUND_GR = "";
        PATH_WAV = "";
        PATH = "";
        MIDIFILE = "";
        SOUND_STAGEFAILED = "";
        SOUND_FULLCOMBO = "";
        SOUND_NOWLOADING = "";
        SOUND_AUDIENCE = "";
        BPM = 120.0;
        BLACKCOLORKEY = true;
        STDGBVALUE<int> stdgbvalue = new()
        {
            Drums = 0,
            Guitar = 0,
            Bass = 0
        };
        LEVEL = stdgbvalue;
        LEVELDEC = stdgbvalue;
        for (int i = 0; i < 7; i++)
        {
            RESULTIMAGE[i] = "";
            RESULTMOVIE[i] = "";
            RESULTSOUND[i] = "";
        }

        db再生速度 = 1.0;
        strDTXFileHash = "";
        bHasChips = new STHASCHIPS
        {
            Drums = false,
            Guitar = false,
            Bass = false,
            HHOpen = false,
            FT = false,
            LP = false,
            LBD = false,
            Ride = false,
            LeftCymbal = false,
            OpenGuitar = false,
            OpenBass = false,
            YPGuitar = false,
            YPBass = false,
            AVI = false
        };
        strFileName = "";
        strFolderName = "";
        strFileNameFullPath = "";
        n無限管理WAV = new int[36 * 36];
        n無限管理BPM = new int[36 * 36];
        n無限管理VOL = new int[36 * 36];
        n無限管理PAN = new int[36 * 36];
        n無限管理SIZE = new int[36 * 36];
        nRESULTIMAGE用優先順位 = new int[7];
        nRESULTMOVIE用優先順位 = new int[7];
        nRESULTSOUND用優先順位 = new int[7];
        bForceXGChart = false;
        bVol137to100 = false;


        #region [ 2011.1.1 yyagi GDA->DTX変換テーブル リファクタ後 ]

        STGDAPARAM[] stgdaparamArray = new STGDAPARAM[]
        {
            // GDA->DTX conversion table
            new("TC", EChannel.BPM), new("BL", EChannel.BarLength), new("GS", EChannel.flowspeed_gt_nouse),
            new("DS", EChannel.flowspeed_dr_nouse), new("FI", EChannel.FillIn), new("HH", EChannel.HiHatClose),
            new("SD", EChannel.Snare), new("BD", EChannel.BassDrum), new("HT", EChannel.HighTom),
            new("LT", EChannel.LowTom), new("CY", EChannel.Cymbal), new("G1", EChannel.Guitar_xxBxx),
            new("G2", EChannel.Guitar_xGxxx), new("G3", EChannel.Guitar_xGBxx), new("G4", EChannel.Guitar_Rxxxx),
            new("G5", EChannel.Guitar_RxBxx), new("G6", EChannel.Guitar_RGxxx), new("G7", EChannel.Guitar_RGBxx),
            new("GW", EChannel.Guitar_Wailing), new("01", EChannel.SE01), new("02", EChannel.SE02),
            new("03", EChannel.SE03), new("04", EChannel.SE04), new("05", EChannel.SE05),
            new("06", EChannel.SE06), new("07", EChannel.SE07), new("08", EChannel.SE08),
            new("09", EChannel.SE09), new("0A", EChannel.SE10), new("0B", EChannel.SE11),
            new("0C", EChannel.SE12), new("0D", EChannel.SE13), new("0E", EChannel.SE14),
            new("0F", EChannel.SE15), new("10", EChannel.SE16), new("11", EChannel.SE17),
            new("12", EChannel.SE18), new("13", EChannel.SE19), new("14", EChannel.SE20),
            new("15", EChannel.SE21), new("16", EChannel.SE22), new("17", EChannel.SE23),
            new("18", EChannel.SE24), new("19", EChannel.SE25), new("1A", EChannel.SE26),
            new("1B", EChannel.SE27), new("1C", EChannel.SE28), new("1D", EChannel.SE29),
            new("1E", EChannel.SE30), new("1F", EChannel.SE31), new("20", EChannel.SE32),
            new("B1", EChannel.Bass_xxBxx), new("B2", EChannel.Bass_xGxxx), new("B3", EChannel.Bass_xGBxx),
            new("B4", EChannel.Bass_Rxxxx), new("B5", EChannel.Bass_RxBxx), new("B6", EChannel.Bass_RGxxx),
            new("B7", EChannel.Bass_RGBxx), new("BW", EChannel.Bass_Wailing), new("G0", EChannel.Guitar_Open),
            new("B0", EChannel.Bass_Open)
        };
        stGDAParam = stgdaparamArray;

        #endregion

        nBGMAdjust = 0;
        nPolyphonicSounds = CDTXMania.ConfigIni.nPoliphonicSounds;
        dbDTXVPlaySpeed = 1.0f;
#if TEST_NOTEOFFMODE
			this.bHH演奏で直前のHHを消音する = true;
			this.bGUITAR演奏で直前のGUITARを消音する = true;
			this.bBASS演奏で直前のBASSを消音する = true;
#endif
    }

    public CDTX(string strFileName, bool bHeaderOnly)
        : this()
    {
        OnActivate();
        tRead(strFileName, bHeaderOnly);
    }

    public CDTX(string strFileName, bool bHeaderOnly, double db再生速度, int nBGMAdjust)
        : this()
    {
        OnActivate();
        tRead(strFileName, bHeaderOnly, db再生速度, nBGMAdjust);
    }


    // メソッド

    public int nモニタを考慮した音量(EInstrumentPart part)
    {
        CConfigIni configIni = CDTXMania.ConfigIni;
        switch (part)
        {
            case EInstrumentPart.DRUMS:
                if (configIni.b演奏音を強調する.Drums)
                {
                    return configIni.n自動再生音量;
                }

                return configIni.n手動再生音量;

            case EInstrumentPart.GUITAR:
                if (configIni.b演奏音を強調する.Guitar)
                {
                    return configIni.n自動再生音量;
                }

                return configIni.n手動再生音量;

            case EInstrumentPart.BASS:
                if (configIni.b演奏音を強調する.Bass)
                {
                    return configIni.n自動再生音量;
                }

                return configIni.n手動再生音量;
        }

        if ((!configIni.b演奏音を強調する.Drums && !configIni.b演奏音を強調する.Guitar) && !configIni.b演奏音を強調する.Bass)
        {
            return configIni.n手動再生音量;
        }

        return configIni.n自動再生音量;
    }

    public void tLoadAVI()
    {
        if (listAVI != null)
        {
            foreach (CAVI cavi in listAVI.Values)
            {
                cavi.OnDeviceCreated();
            }
        }

        //if( this.listDS != null && CDTXMania.ConfigIni.bDirectShowMode == true)
        //{
        //    foreach( CDirectShow cds in this.listDS.Values)
        //    {
        //        cds.OnDeviceCreated();
        //    }
        //}
        if (!bHeaderOnly && b動画読み込み)
        {
            foreach (CChip chip in listChip)
            {
                if (chip.nChannelNumber == EChannel.Movie || chip.nChannelNumber == EChannel.MovieFull)
                {
                    chip.eAVI種別 = EAVIType.Unknown;
                    chip.rAVI = null;
                    //chip.rDShow = null;
                    chip.rAVIPan = null;
                    if (listAVIPAN.ContainsKey(chip.nIntegerValue))
                    {
                        CAVIPAN cavipan = listAVIPAN[chip.nIntegerValue];
                        if (listAVI.ContainsKey(cavipan.nAVI番号) && (listAVI[cavipan.nAVI番号].avi != null))
                        {
                            chip.eAVI種別 = EAVIType.AVIPAN;
                            chip.rAVI = listAVI[cavipan.nAVI番号];
                            //if(CDTXMania.ConfigIni.bDirectShowMode == true)
                            //    chip.rDShow = this.listDS[ cavipan.nAVI番号 ];
                            chip.rAVIPan = cavipan;
                            continue;
                        }
                    }

                    if (listAVI.ContainsKey(chip.nIntegerValue) &&
                        (listAVI[chip.nIntegerValue].avi !=
                         null) /*|| ( this.listDS.ContainsKey( chip.nIntegerValue ) && ( this.listDS[ chip.nIntegerValue ].dshow != null ) )*/
                       )
                    {
                        chip.eAVI種別 = EAVIType.AVI;
                        chip.rAVI = listAVI[chip.nIntegerValue];
                        //if( CDTXMania.ConfigIni.bDirectShowMode == true )
                        //    chip.rDShow = this.listDS[ chip.nIntegerValue ];
                    }
                }
            }
        }
    }

    #region [ BMP/BMPTEXの並列読み込み_デコード用メソッド ]

    private static void LoadTexture(CBMPbase cbmp) // バックグラウンドスレッドで動作する、ファイル読み込み部
    {
        string filename = cbmp.GetFullPathname;
        if (!File.Exists(filename))
        {
            Trace.TraceWarning("ファイルが存在しません。({0})", filename);
            cbmp.bitmap = null;
            return;
        }

        cbmp.bitmap = new Bitmap(filename);
    }

    private static void BMPLoadAll(Dictionary<int, CBMP> listB) // バックグラウンドスレッドで、テクスチャファイルをひたすら読み込んではキューに追加する
    {
        //Trace.TraceInformation( "Back: ThreadID(BMPLoad)=" + Thread.CurrentThread.ManagedThreadId + ", listCount=" + listB.Count  );
        foreach (CBMP cbmp in listB.Values)
        {
            LoadTexture(cbmp);
            lock (lockQueue)
            {
                queueCBMPbaseDone.Enqueue(cbmp);
                //  Trace.TraceInformation( "Back: Enqueued(" + queueCBMPbaseDone.Count + "): " + cbmp.strFilename );
            }

            if (queueCBMPbaseDone.Count > 8)
            {
                Thread.Sleep(10);
            }
        }
    }

    private static void BMPTEXLoadAll(CDTX cdtx) // ダサい実装だが、Dictionary<>の中には手を出せず、妥協した
    {
        var listB = cdtx.listBMPTEX;
        
        //Trace.TraceInformation( "Back: ThreadID(BMPLoad)=" + Thread.CurrentThread.ManagedThreadId + ", listCount=" + listB.Count  );
        foreach (CBMPTEX cbmp in listB.Values)
        {
            LoadTexture(cbmp);
            lock (lockQueue)
            {
                queueCBMPbaseDone.Enqueue(cbmp);
                //  Trace.TraceInformation( "Back: Enqueued(" + queueCBMPbaseDone.Count + "): " + cbmp.strFilename );
            }

            if (queueCBMPbaseDone.Count > 8)
            {
                Thread.Sleep(10);
            }
        }
    }

    private static Queue<CBMPbase> queueCBMPbaseDone = new();
    private static object lockQueue = new();
    private static int nLoadDone;

    #endregion

    public void tLoadBMP_BMPTEX()
    {
        int nCPUCores = Environment.ProcessorCount;

        #region [ Read BMPs ]

        if (listBMP != null)
        {
            if (nCPUCores <= 1)
            {
                foreach (CBMP cbmp in listBMP.Values)
                {
                    cbmp.OnDeviceCreated();
                }
            }
            else
            {
                //Initialize textures on main thread, load and decode on background thread

                //Trace.TraceInformation( "Main: ThreadID(Main)=" + Thread.CurrentThread.ManagedThreadId + ", listCount=" + this.listBMP.Count );
                nLoadDone = 0;
                Task.Run(() => BMPLoadAll(listBMP));

                // t.Priority = ThreadPriority.Lowest;
                // t.Start( listBMP );
                int c = listBMP.Count;
                while (nLoadDone < c)
                {
                    if (queueCBMPbaseDone.Count > 0)
                    {
                        CBMP cbmp;
                        //Trace.TraceInformation( "Main: Lock Begin for dequeue1." );
                        lock (lockQueue)
                        {
                            cbmp = (CBMP)queueCBMPbaseDone.Dequeue();
                            //  Trace.TraceInformation( "Main: Dequeued(" + queueCBMPbaseDone.Count + "): " + cbmp.strFilename );
                        }

                        cbmp.OnDeviceCreated(cbmp.bitmap, cbmp.GetFullPathname);
                        nLoadDone++;
                        //Trace.TraceInformation( "Main: OnDeviceCreated: " + cbmp.strFilename );
                    }
                    else
                    {
                        //Trace.TraceInformation( "Main: Sleeped.");
                        Thread.Sleep(5); // WaitOneのイベント待ちにすると、メインスレッド処理中に2個以上イベント完了したときにそれを正しく検出できなくなるので、
                    } // ポーリングに逃げてしまいました。
                }
            }
        }

        #endregion

        #region [ Read BMPTEXs ]

        if (listBMPTEX != null)
        {
            if (nCPUCores <= 1)
            {
                #region [ シングルスレッドで逐次読み出し_デコード_テクスチャ定義 ]

                foreach (CBMPTEX cbmptex in listBMPTEX.Values)
                {
                    cbmptex.OnDeviceCreated();
                }

                #endregion
            }
            else
            {
                #region [ メインスレッド(テクスチャ定義)とバックグラウンドスレッド(読み出し_デコード)を並列動作させ高速化 ]

                //Trace.TraceInformation( "Main: ThreadID(Main)=" + Thread.CurrentThread.ManagedThreadId + ", listCount=" + this.listBMP.Count );
                nLoadDone = 0;
                Task.Run(() => BMPTEXLoadAll(this));
                int c = listBMPTEX.Count;
                while (nLoadDone < c)
                {
                    if (queueCBMPbaseDone.Count > 0)
                    {
                        CBMPTEX cbmptex;
                        //Trace.TraceInformation( "Main: Lock Begin for dequeue1." );
                        lock (lockQueue)
                        {
                            cbmptex = (CBMPTEX)queueCBMPbaseDone.Dequeue();
                            //  Trace.TraceInformation( "Main: Dequeued(" + queueCBMPbaseDone.Count + "): " + cbmp.strFilename );
                        }

                        cbmptex.OnDeviceCreated(cbmptex.bitmap, cbmptex.GetFullPathname);
                        nLoadDone++;
                        //Trace.TraceInformation( "Main: OnDeviceCreated: " + cbmp.strFilename );
                    }
                    else
                    {
                        //Trace.TraceInformation( "Main: Sleeped.");
                        Thread.Sleep(5); // WaitOneのイベント待ちにすると、メインスレッド処理中に2個以上イベント完了したときにそれを正しく検出できなくなるので、
                    } // ポーリングに逃げてしまいました。
                }

                #endregion
            }
        }

        #endregion

        if (!bHeaderOnly)
        {
            foreach (CChip chip in listChip)
            {
                #region [ BGAPAN/BGA/BMPTEX/BMP ]

                if ((((chip.nChannelNumber == EChannel.BGALayer1) || (chip.nChannelNumber == EChannel.BGALayer2)) ||
                     ((chip.nChannelNumber >= EChannel.BGALayer3) &&
                      (chip.nChannelNumber <= EChannel.BGALayer7))) || (chip.nChannelNumber == EChannel.BGALayer8))
                {
                    chip.eBGA種別 = EBGAType.Unknown;
                    chip.rBMP = null;
                    chip.rBMPTEX = null;
                    chip.rBGA = null;
                    chip.rBGAPan = null;

                    #region [ BGAPAN ]

                    if (listBGAPAN.ContainsKey(chip.nIntegerValue))
                    {
                        CBGAPAN cbgapan = listBGAPAN[chip.nIntegerValue];
                        if (listBMPTEX.ContainsKey(cbgapan.nBMP番号) && listBMPTEX[cbgapan.nBMP番号].bUse)
                        {
                            chip.eBGA種別 = EBGAType.BGAPAN;
                            chip.rBMPTEX = listBMPTEX[cbgapan.nBMP番号];
                            chip.rBGAPan = cbgapan;
                            continue;
                        }

                        if (listBMP.ContainsKey(cbgapan.nBMP番号) && listBMP[cbgapan.nBMP番号].bUse)
                        {
                            chip.eBGA種別 = EBGAType.BGAPAN;
                            chip.rBMP = listBMP[cbgapan.nBMP番号];
                            chip.rBGAPan = cbgapan;
                            continue;
                        }
                    }

                    #endregion

                    #region [ BGA ]

                    if (listBGA.ContainsKey(chip.nIntegerValue))
                    {
                        CBGA cbga = listBGA[chip.nIntegerValue];
                        if (listBMPTEX.ContainsKey(cbga.nBMP番号) && listBMPTEX[cbga.nBMP番号].bUse)
                        {
                            chip.eBGA種別 = EBGAType.BGA;
                            chip.rBMPTEX = listBMPTEX[cbga.nBMP番号];
                            chip.rBGA = cbga;
                            continue;
                        }

                        if (listBMP.ContainsKey(cbga.nBMP番号) && listBMP[cbga.nBMP番号].bUse)
                        {
                            chip.eBGA種別 = EBGAType.BGA;
                            chip.rBMP = listBMP[cbga.nBMP番号];
                            chip.rBGA = cbga;
                            continue;
                        }
                    }

                    #endregion

                    #region [ BMPTEX ]

                    if (listBMPTEX.ContainsKey(chip.nIntegerValue) && listBMPTEX[chip.nIntegerValue].bUse)
                    {
                        chip.eBGA種別 = EBGAType.BMPTEX;
                        chip.rBMPTEX = listBMPTEX[chip.nIntegerValue];
                        continue;
                    }

                    #endregion

                    #region [ BMP ]

                    if (listBMP.ContainsKey(chip.nIntegerValue) && listBMP[chip.nIntegerValue].bUse)
                    {
                        chip.eBGA種別 = EBGAType.BMP;
                        chip.rBMP = listBMP[chip.nIntegerValue];
                        continue;
                    }

                    #endregion
                }

                #endregion

                #region [ BGA入れ替え ]

                if ((((chip.nChannelNumber == EChannel.BGALayer1_Swap) ||
                      (chip.nChannelNumber == EChannel.BGALayer2_Swap)) ||
                     ((chip.nChannelNumber >= EChannel.BGALayer3_Swap) &&
                      (chip.nChannelNumber <= EChannel.BGALayer7_Swap))) ||
                    (chip.nChannelNumber == EChannel.BGALayer8_Swap))
                {
                    chip.eBGA種別 = EBGAType.Unknown;
                    chip.rBMP = null;
                    chip.rBMPTEX = null;
                    chip.rBGA = null;
                    chip.rBGAPan = null;
                    if (listBMPTEX.ContainsKey(chip.nIntegerValue) && listBMPTEX[chip.nIntegerValue].bUse)
                    {
                        chip.eBGA種別 = EBGAType.BMPTEX;
                        chip.rBMPTEX = listBMPTEX[chip.nIntegerValue];
                    }
                    else if (listBMP.ContainsKey(chip.nIntegerValue) && listBMP[chip.nIntegerValue].bUse)
                    {
                        chip.eBGA種別 = EBGAType.BMP;
                        chip.rBMP = listBMP[chip.nIntegerValue];
                    }
                }

                #endregion
            }
        }
    }

    public void t旧仕様のドコドコチップを振り分ける(EInstrumentPart part, bool bAssignToLBD)
    {
        if (part == EInstrumentPart.DRUMS && bAssignToLBD)
        {
            bool flag = false;
            foreach (CChip current in listChip)
            {
                if (part == EInstrumentPart.DRUMS && (current.nChannelNumber == EChannel.LeftPedal ||
                                                      current.nChannelNumber == EChannel.LeftBassDrum))
                {
                    flag = true;
                    break;
                }
            }

            if (!flag)
            {
                int num = 0;
                bool flag2 = false;
                foreach (CChip current2 in listChip)
                {
                    if (part == EInstrumentPart.DRUMS && current2.nChannelNumber == EChannel.BassDrum)
                    {
                        if (!flag2 && current2.nPlaybackTimeMs - num < 150)
                        {
                            current2.nChannelNumber = EChannel.LeftBassDrum;
                        }

                        num = current2.nPlaybackTimeMs;
                        flag2 = (current2.nChannelNumber == EChannel.LeftBassDrum);
                    }
                }
            }
        }
    }

    public void tドコドコ仕様変更(EInstrumentPart part, Core.EType eDkdkType)
    {
        if ((part == EInstrumentPart.DRUMS) && (eDkdkType != Core.EType.A))
        {
            if (eDkdkType == Core.EType.B)
            {
                int num = 0;
                int index = 0;
                int[] numArray = new int[1000];
                int num3 = 0;
                int num4 = 0;
                int num5 = -1;
                EChannel num6 = 0;
                EChannel num7 = 0;
                bool flag = false;
                for (int i = 0; i < 1000; i++)
                {
                    numArray[i] = 0;
                }

                foreach (CChip chip in listChip)
                {
                    bool flag2 = false;
                    EChannel num9 = chip.nChannelNumber;
                    if ((part == EInstrumentPart.DRUMS) &&
                        ((num9 == EChannel.BassDrum) || (num9 == EChannel.LeftBassDrum)))
                    {
                        num++;
                        if (((num6 == EChannel.BassDrum) && (chip.nPlaybackPosition == num3)) ||
                            ((num6 == EChannel.LeftBassDrum) && (chip.nPlaybackPosition == num4)))
                        {
                            chip.nChannelNumber = (num7 == EChannel.BassDrum)
                                ? EChannel.LeftBassDrum
                                : EChannel.BassDrum;
                            flag2 = true;
                        }
                        else if (num9 == EChannel.LeftBassDrum)
                        {
                            if ((num6 == EChannel.BassDrum) && ((chip.nPlaybackPosition - num3) <= 0x60))
                            {
                                if (!flag)
                                {
                                    if (!flag2)
                                    {
                                        numArray[index++] = num - 1;
                                    }

                                    flag = true;
                                    chip.nChannelNumber = EChannel.BassDrum;
                                }
                                else
                                {
                                    chip.nChannelNumber = EChannel.BassDrum;
                                }

                                num5 = chip.nPlaybackPosition - num3;
                            }
                            else
                            {
                                flag = false;
                                num5 = -1;
                            }

                            flag2 = false;
                        }
                        else
                        {
                            if ((num6 == EChannel.LeftBassDrum) && ((chip.nPlaybackPosition - num4) <= 0x60))
                            {
                                if (!flag)
                                {
                                    if ((((chip.nPlaybackPosition - num3) - num5) < 0x10) || (num5 == -1))
                                    {
                                        numArray[index++] = num - 1;
                                        flag = true;
                                        chip.nChannelNumber = EChannel.LeftBassDrum;
                                    }
                                    else
                                    {
                                        flag = false;
                                    }
                                }
                                else if (((chip.nPlaybackPosition - num4) - num5) >= 0x10)
                                {
                                    flag = false;
                                }
                                else
                                {
                                    chip.nChannelNumber = EChannel.LeftBassDrum;
                                }

                                num5 = chip.nPlaybackPosition - num4;
                            }
                            else
                            {
                                flag = false;
                                num5 = -1;
                            }

                            flag2 = false;
                        }

                        num6 = num9;
                        num7 = chip.nChannelNumber;
                        if (num9 == EChannel.BassDrum)
                        {
                            num3 = chip.nPlaybackPosition;
                        }
                        else
                        {
                            num4 = chip.nPlaybackPosition;
                        }
                    }
                }

                num = 0;
                index = 0;
                foreach (CChip chip2 in listChip)
                {
                    EChannel num10 = chip2.nChannelNumber;
                    if ((part == EInstrumentPart.DRUMS) &&
                        ((num10 == EChannel.BassDrum) || (num10 == EChannel.LeftBassDrum)))
                    {
                        num++;
                        if (num == numArray[index])
                        {
                            chip2.nChannelNumber = (num10 == EChannel.BassDrum)
                                ? EChannel.LeftBassDrum
                                : EChannel.BassDrum;
                            index++;
                        }
                    }
                }
            }
            else if (eDkdkType == Core.EType.C)
            {
                int num11 = 0;
                foreach (CChip chip3 in listChip)
                {
                    EChannel num12 = chip3.nChannelNumber;
                    if ((part == EInstrumentPart.DRUMS) &&
                        ((num12 == EChannel.BassDrum) || (num12 == EChannel.LeftBassDrum)))
                    {
                        if (num11 == chip3.nPlaybackPosition)
                        {
                            chip3.nChannelNumber = EChannel.SE16;
                        }
                        else if (num12 == EChannel.LeftBassDrum)
                        {
                            chip3.nChannelNumber = EChannel.BassDrum;
                        }

                        num11 = chip3.nPlaybackPosition;
                    }
                }
            }
        }
    }


    public void t譜面仕様変更(EInstrumentPart part, Core.EType eNumOfLanes)
    {
        if ((part == EInstrumentPart.DRUMS) && (eNumOfLanes != Core.EType.A))
        {
            int num = 0;
            if (eNumOfLanes == Core.EType.B)
            {
                foreach (CChip chip in listChip)
                {
                    EChannel nチャンネル番号 = chip.nChannelNumber;
                    if ((part == EInstrumentPart.DRUMS) &&
                        ((nチャンネル番号 == EChannel.RideCymbal) || (nチャンネル番号 == EChannel.Cymbal)))
                    {
                        if (num == chip.nPlaybackPosition)
                        {
                            chip.nChannelNumber = EChannel.LeftCymbal;
                        }
                        else if (nチャンネル番号 == EChannel.RideCymbal)
                        {
                            chip.nChannelNumber = EChannel.Cymbal;
                        }

                        num = chip.nPlaybackPosition;
                    }
                }
            }
            else if (eNumOfLanes == Core.EType.C)
            {
                bool flag = false;
                bool flag2 = false;
                EChannel num3 = 0;
                EChannel num4 = 0;
                int num5 = 0;
                int num6 = 0;
                foreach (CChip chip in listChip)
                {
                    EChannel num7 = chip.nChannelNumber;
                    if ((part == EInstrumentPart.DRUMS) &&
                        ((num7 >= EChannel.HiHatClose) || (num7 <= EChannel.LeftBassDrum)))
                    {
                        switch (num7)
                        {
                            case EChannel.HiHatClose:
                            case EChannel.Cymbal:
                            case EChannel.HiHatOpen:
                                if (num6 == chip.nPlaybackPosition)
                                {
                                    chip.nChannelNumber = (num4 == EChannel.Cymbal)
                                        ? EChannel.HiHatClose
                                        : EChannel.Cymbal;
                                }

                                flag2 = num7 == EChannel.Cymbal;
                                num4 = chip.nChannelNumber;
                                num6 = chip.nPlaybackPosition;
                                continue;


                            case EChannel.Snare:
                            case EChannel.BassDrum:
                            {
                                continue;
                            }
                            case EChannel.HighTom:
                            {
                                chip.nChannelNumber =
                                    ((num5 == chip.nPlaybackPosition) && (num3 == EChannel.HighTom))
                                        ? EChannel.LowTom
                                        : EChannel.HighTom;
                                flag = false;
                                num3 = chip.nChannelNumber;
                                num5 = chip.nPlaybackPosition;
                                continue;
                            }
                            case EChannel.LowTom:
                                if (num5 != chip.nPlaybackPosition)
                                {
                                    if (flag)
                                    {
                                        chip.nChannelNumber = EChannel.HighTom;
                                    }
                                }

                                num3 = chip.nChannelNumber;
                                num5 = chip.nPlaybackPosition;
                                continue;

                            case EChannel.FloorTom:
                            {
                                chip.nChannelNumber =
                                    ((num5 == chip.nPlaybackPosition) && (num3 == EChannel.LowTom))
                                        ? EChannel.HighTom
                                        : EChannel.LowTom;
                                flag = true;
                                num3 = chip.nChannelNumber;
                                num5 = chip.nPlaybackPosition;
                                continue;
                            }
                            case EChannel.RideCymbal:
                            {
                                chip.nChannelNumber =
                                    ((num6 == chip.nPlaybackPosition) && (num4 == EChannel.Cymbal))
                                        ? EChannel.HiHatClose
                                        : EChannel.Cymbal;
                                flag2 = true;
                                num4 = chip.nChannelNumber;
                                num6 = chip.nPlaybackPosition;
                                continue;
                            }
                            case EChannel.LeftCymbal:
                                if (num6 != chip.nPlaybackPosition)
                                {
                                    chip.nChannelNumber = (flag2 && ((chip.nPlaybackPosition - num6) <= 0xc0))
                                        ? EChannel.HiHatClose
                                        : EChannel.Cymbal;
                                }

                                chip.nChannelNumber =
                                    (num4 == EChannel.Cymbal) ? EChannel.HiHatClose : EChannel.Cymbal;
                                num4 = chip.nChannelNumber;
                                num6 = chip.nPlaybackPosition;
                                continue;

                            case EChannel.LeftPedal:
                            case EChannel.LeftBassDrum:
                            {
                                chip.nChannelNumber = EChannel.SE16;
                                continue;
                            }
                        }
                    }

                    continue;
                }
            }
        }
    }

    private static void tミラーチップのチャンネルを指定する(CChip chip, EChannel nミラー化前チャンネル番号)
    {
        switch (nミラー化前チャンネル番号)
        {
            case EChannel.HiHatClose:
            case EChannel.HiHatOpen:
                if (CDTXMania.ConfigIni.eNumOfLanes.Drums != Core.EType.B)
                {
                    chip.nChannelNumber = ((CDTXMania.ConfigIni.eNumOfLanes.Drums == Core.EType.A)
                        ? EChannel.RideCymbal
                        : EChannel.Cymbal);
                    return;
                }

                break;
            case EChannel.Snare:
                chip.nChannelNumber = ((CDTXMania.ConfigIni.eNumOfLanes.Drums == Core.EType.C)
                    ? EChannel.LowTom
                    : EChannel.FloorTom);
                return;
            case EChannel.BassDrum:
                if (CDTXMania.ConfigIni.eNumOfLanes.Drums != Core.EType.C)
                {
                    chip.nChannelNumber = EChannel.LeftBassDrum;
                    return;
                }

                break;
            case EChannel.HighTom:
                if (CDTXMania.ConfigIni.eNumOfLanes.Drums != Core.EType.C)
                {
                    chip.nChannelNumber = EChannel.LowTom;
                    return;
                }

                break;
            case EChannel.LowTom:
                chip.nChannelNumber = ((CDTXMania.ConfigIni.eNumOfLanes.Drums == Core.EType.C)
                    ? EChannel.Snare
                    : EChannel.HighTom);
                return;
            case EChannel.Cymbal:
                chip.nChannelNumber = ((CDTXMania.ConfigIni.eNumOfLanes.Drums == Core.EType.C)
                    ? EChannel.HiHatClose
                    : EChannel.LeftCymbal);
                return;
            case EChannel.FloorTom:
                chip.nChannelNumber = EChannel.Snare;
                return;
            case EChannel.RideCymbal:
                chip.nChannelNumber = EChannel.HiHatClose;
                return;
            case EChannel.LeftCymbal:
                chip.nChannelNumber = EChannel.Cymbal;
                return;
            case EChannel.LeftPedal:
            case EChannel.LeftBassDrum:
                chip.nChannelNumber = EChannel.BassDrum;
                break;
            default:
                return;
        }
    }

    public void tドラムのランダム化(EInstrumentPart part, ERandomMode eRandom)
    {
        if (part == EInstrumentPart.DRUMS && eRandom == ERandomMode.MIRROR)
        {
            foreach (CChip current in listChip)
            {
                EChannel nチャンネル番号 = current.nChannelNumber;
                if (part == EInstrumentPart.DRUMS && EChannel.HiHatClose <= nチャンネル番号 &&
                    nチャンネル番号 != EChannel.BassDrum && nチャンネル番号 <= EChannel.LeftCymbal)
                {
                    tミラーチップのチャンネルを指定する(current, nチャンネル番号);
                }
            }
        }
        else if (part == EInstrumentPart.DRUMS && eRandom != ERandomMode.OFF)
        {
            if (CDTXMania.ConfigIni.eNumOfLanes.Drums != Core.EType.C)
            {
                EChannel num = 0;
                int num2 = 0;
                int num3 = 0;
                int num4 = -10000;
                int[] array;
                int[] array2;
                tグループランダム分配作業(out array, out array2);
                using (List<CChip>.Enumerator enumerator = listChip.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        CChip current = enumerator.Current;
                        EChannel nチャンネル番号 = current.nChannelNumber;
                        if (part == EInstrumentPart.DRUMS && EChannel.HiHatClose <= nチャンネル番号 &&
                            nチャンネル番号 <= EChannel.LeftCymbal && nチャンネル番号 != EChannel.BassDrum)
                        {
                            switch (eRandom)
                            {
                                case ERandomMode.RANDOM:
                                    t乱数を各チャンネルに指定する(array, array2, current, nチャンネル番号);
                                    break;
                                case ERandomMode.SUPERRANDOM:
                                    if (current.nPlaybackPosition / 384 != num4)
                                    {
                                        num4 = current.nPlaybackPosition / 384;
                                        tグループランダム分配作業(out array, out array2);
                                    }

                                    t乱数を各チャンネルに指定する(array, array2, current, nチャンネル番号);
                                    break;
                                case ERandomMode.HYPERRANDOM:
                                    if (current.nPlaybackPosition / 96 != num4)
                                    {
                                        num4 = current.nPlaybackPosition / 96;
                                        tグループランダム分配作業(out array, out array2);
                                    }

                                    t乱数を各チャンネルに指定する(array, array2, current, nチャンネル番号);
                                    break;
                                case ERandomMode.MASTERRANDOM:
                                    if (nチャンネル番号 == EChannel.Snare || nチャンネル番号 == EChannel.HighTom ||
                                        nチャンネル番号 == EChannel.LowTom || nチャンネル番号 == EChannel.FloorTom)
                                    {
                                        if (num3 != current.nPlaybackPosition || num2 != 2)
                                        {
                                            current.nChannelNumber = array2[CDTXMania.Random.Next(4)] +
                                                                     EChannel.HiHatClose;
                                        }
                                        else
                                        {
                                            int num5 = 0;
                                            while (num5 == 0)
                                            {
                                                current.nChannelNumber = array2[CDTXMania.Random.Next(4)] +
                                                                         EChannel.HiHatClose;
                                                num5 = 1;
                                                if (current.nChannelNumber == num)
                                                {
                                                    num5 = 0;
                                                }
                                            }
                                        }

                                        num2 = 2;
                                    }
                                    else
                                    {
                                        if (num3 != current.nPlaybackPosition || num2 != 1)
                                        {
                                            current.nChannelNumber =
                                                array[CDTXMania.Random.Next(4)] + EChannel.HiHatClose;
                                        }
                                        else
                                        {
                                            int num6 = 0;
                                            while (num6 == 0)
                                            {
                                                current.nChannelNumber = array[CDTXMania.Random.Next(4)] +
                                                                         EChannel.HiHatClose;
                                                num6 = 1;
                                                if (current.nChannelNumber == num)
                                                {
                                                    num6 = 0;
                                                }
                                            }
                                        }

                                        num2 = 1;
                                    }

                                    num3 = current.nPlaybackPosition;
                                    num = current.nChannelNumber;
                                    break;
                                case ERandomMode.ANOTHERRANDOM:
                                    if (nチャンネル番号 == EChannel.HiHatClose || nチャンネル番号 == EChannel.HiHatOpen ||
                                        nチャンネル番号 == EChannel.LeftCymbal)
                                    {
                                        if (num2 != 1 || num3 != current.nPlaybackPosition)
                                        {
                                            if (CDTXMania.Random.Next(4) == 0)
                                            {
                                                current.nChannelNumber = ((nチャンネル番号 == EChannel.LeftCymbal)
                                                    ? EChannel.HiHatClose
                                                    : EChannel.LeftCymbal);
                                            }
                                            else
                                            {
                                                if (nチャンネル番号 == EChannel.HiHatOpen)
                                                {
                                                    current.nChannelNumber = EChannel.HiHatClose;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            current.nChannelNumber =
                                                ((num == EChannel.HiHatClose || num == EChannel.HiHatOpen)
                                                    ? EChannel.LeftCymbal
                                                    : EChannel.HiHatClose);
                                        }

                                        num = current.nChannelNumber;
                                        num2 = 1;
                                        num3 = current.nPlaybackPosition;
                                    }
                                    else
                                    {
                                        if (nチャンネル番号 == EChannel.Snare || nチャンネル番号 == EChannel.HighTom)
                                        {
                                            if (num2 != 2 || num3 != current.nPlaybackPosition)
                                            {
                                                if (CDTXMania.Random.Next(4) == 0)
                                                {
                                                    current.nChannelNumber = ((nチャンネル番号 == EChannel.Snare)
                                                        ? EChannel.HighTom
                                                        : EChannel.Snare);
                                                }
                                            }
                                            else
                                            {
                                                current.nChannelNumber = ((num == EChannel.Snare)
                                                    ? EChannel.HighTom
                                                    : EChannel.Snare);
                                            }

                                            num = current.nChannelNumber;
                                            num2 = 2;
                                            num3 = current.nPlaybackPosition;
                                        }
                                        else
                                        {
                                            if (nチャンネル番号 == EChannel.LowTom || nチャンネル番号 == EChannel.FloorTom)
                                            {
                                                if (num2 != 5 || num3 != current.nPlaybackPosition)
                                                {
                                                    if (CDTXMania.Random.Next(4) == 0)
                                                    {
                                                        current.nChannelNumber = ((nチャンネル番号 == EChannel.LowTom)
                                                            ? EChannel.FloorTom
                                                            : EChannel.LowTom);
                                                    }
                                                }
                                                else
                                                {
                                                    current.nChannelNumber = ((num == EChannel.LowTom)
                                                        ? EChannel.FloorTom
                                                        : EChannel.LowTom);
                                                }

                                                num = current.nChannelNumber;
                                                num2 = 5;
                                                num3 = current.nPlaybackPosition;
                                            }
                                            else
                                            {
                                                if (CDTXMania.ConfigIni.eNumOfLanes.Drums == Core.EType.A &&
                                                    (nチャンネル番号 == EChannel.Cymbal ||
                                                     nチャンネル番号 == EChannel.RideCymbal))
                                                {
                                                    if (num2 != 6 || num3 != current.nPlaybackPosition)
                                                    {
                                                        if (CDTXMania.Random.Next(4) == 0)
                                                        {
                                                            current.nChannelNumber = ((nチャンネル番号 == EChannel.Cymbal)
                                                                ? EChannel.RideCymbal
                                                                : EChannel.Cymbal);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        current.nChannelNumber = ((num == EChannel.Cymbal)
                                                            ? EChannel.RideCymbal
                                                            : EChannel.Cymbal);
                                                    }

                                                    num = current.nChannelNumber;
                                                    num2 = 6;
                                                    num3 = current.nPlaybackPosition;
                                                }
                                            }
                                        }
                                    }

                                    break;
                            }
                        }
                    }

                    return;
                }
            }

            EChannel num7 = 0;
            int num8 = 0;
            int num9 = -10000;
            int[] n乱数排列数列;
            t乱数排列数列生成作業_クラシック(out n乱数排列数列);
            foreach (CChip current2 in listChip)
            {
                EChannel nチャンネル番号2 = current2.nChannelNumber;
                if (part == EInstrumentPart.DRUMS && EChannel.HiHatClose <= nチャンネル番号2 &&
                    nチャンネル番号2 <= EChannel.HiHatOpen && nチャンネル番号2 != EChannel.BassDrum)
                {
                    switch (eRandom)
                    {
                        case ERandomMode.RANDOM:
                            t乱数を各チャンネルに指定する_クラシック(n乱数排列数列, current2, nチャンネル番号2);
                            break;
                        case ERandomMode.SUPERRANDOM:
                            if (current2.nPlaybackPosition / 384 != num9)
                            {
                                num9 = current2.nPlaybackPosition / 384;
                                t乱数排列数列生成作業_クラシック(out n乱数排列数列);
                            }

                            t乱数を各チャンネルに指定する_クラシック(n乱数排列数列, current2, nチャンネル番号2);
                            break;
                        case ERandomMode.HYPERRANDOM:
                            if (current2.nPlaybackPosition / 96 != num9)
                            {
                                num9 = current2.nPlaybackPosition / 96;
                                t乱数排列数列生成作業_クラシック(out n乱数排列数列);
                            }

                            t乱数を各チャンネルに指定する_クラシック(n乱数排列数列, current2, nチャンネル番号2);
                            break;
                        case ERandomMode.MASTERRANDOM:
                            do
                            {
                                current2.nChannelNumber = ((CDTXMania.Random.Next(5) >= 2)
                                    ? (CDTXMania.Random.Next(3) + EChannel.HighTom)
                                    : (CDTXMania.Random.Next(2) + EChannel.HiHatClose));
                            } while (num8 == current2.nPlaybackPosition && current2.nChannelNumber == num7);

                            num7 = current2.nChannelNumber;
                            num8 = current2.nPlaybackPosition;
                            break;
                        case ERandomMode.ANOTHERRANDOM:
                            switch (nチャンネル番号2)
                            {
                                case EChannel.HiHatClose:
                                case EChannel.HiHatOpen:
                                    if (CDTXMania.Random.Next(4) == 0)
                                    {
                                        current2.nChannelNumber = EChannel.Snare;
                                    }
                                    else
                                    {
                                        if (nチャンネル番号2 == EChannel.HiHatOpen)
                                        {
                                            current2.nChannelNumber = EChannel.HiHatClose;
                                        }
                                    }

                                    if (num8 == current2.nPlaybackPosition && current2.nChannelNumber == num7)
                                    {
                                        current2.nChannelNumber = ((num7 == EChannel.HiHatClose)
                                            ? EChannel.Snare
                                            : EChannel.HiHatClose);
                                    }

                                    num7 = current2.nChannelNumber;
                                    num8 = current2.nPlaybackPosition;
                                    break;
                                case EChannel.Snare:
                                    if (CDTXMania.Random.Next(4) == 0)
                                    {
                                        current2.nChannelNumber = ((CDTXMania.Random.Next(2) == 0)
                                            ? EChannel.HiHatClose
                                            : EChannel.HighTom);
                                    }

                                    if (num8 == current2.nPlaybackPosition && current2.nChannelNumber == num7)
                                    {
                                        if (num7 == EChannel.HiHatClose)
                                        {
                                            current2.nChannelNumber = ((CDTXMania.Random.Next(2) == 0)
                                                ? EChannel.Snare
                                                : EChannel.HighTom);
                                        }
                                        else
                                        {
                                            if (num7 == EChannel.Snare)
                                            {
                                                current2.nChannelNumber = ((CDTXMania.Random.Next(2) == 0)
                                                    ? EChannel.HiHatClose
                                                    : EChannel.HighTom);
                                            }
                                            else
                                            {
                                                current2.nChannelNumber = ((CDTXMania.Random.Next(2) == 0)
                                                    ? EChannel.HiHatClose
                                                    : EChannel.Snare);
                                            }
                                        }
                                    }

                                    num7 = current2.nChannelNumber;
                                    num8 = current2.nPlaybackPosition;
                                    break;
                                case EChannel.HighTom:
                                    if (CDTXMania.Random.Next(4) == 0)
                                    {
                                        current2.nChannelNumber = ((CDTXMania.Random.Next(2) == 0)
                                            ? EChannel.Snare
                                            : EChannel.LowTom);
                                    }

                                    if (num8 == current2.nPlaybackPosition && current2.nChannelNumber == num7)
                                    {
                                        if (num7 == EChannel.Snare)
                                        {
                                            current2.nChannelNumber = ((CDTXMania.Random.Next(2) == 0)
                                                ? EChannel.HighTom
                                                : EChannel.LowTom);
                                        }
                                        else
                                        {
                                            if (num7 == EChannel.HighTom)
                                            {
                                                current2.nChannelNumber = ((CDTXMania.Random.Next(2) == 0)
                                                    ? EChannel.Snare
                                                    : EChannel.LowTom);
                                            }
                                            else
                                            {
                                                current2.nChannelNumber = ((CDTXMania.Random.Next(2) == 0)
                                                    ? EChannel.Snare
                                                    : EChannel.HighTom);
                                            }
                                        }
                                    }

                                    num7 = current2.nChannelNumber;
                                    num8 = current2.nPlaybackPosition;
                                    break;
                                case EChannel.LowTom:
                                    if (CDTXMania.Random.Next(4) == 0)
                                    {
                                        current2.nChannelNumber = ((CDTXMania.Random.Next(2) == 0)
                                            ? EChannel.HighTom
                                            : EChannel.Cymbal);
                                    }

                                    if (num8 == current2.nPlaybackPosition && current2.nChannelNumber == num7)
                                    {
                                        if (num7 == EChannel.HighTom)
                                        {
                                            current2.nChannelNumber = ((CDTXMania.Random.Next(2) == 0)
                                                ? EChannel.LowTom
                                                : EChannel.Cymbal);
                                        }
                                        else
                                        {
                                            if (num7 == EChannel.LowTom)
                                            {
                                                current2.nChannelNumber = ((CDTXMania.Random.Next(2) == 0)
                                                    ? EChannel.HighTom
                                                    : EChannel.Cymbal);
                                            }
                                            else
                                            {
                                                current2.nChannelNumber = ((CDTXMania.Random.Next(2) == 0)
                                                    ? EChannel.HighTom
                                                    : EChannel.LowTom);
                                            }
                                        }
                                    }

                                    num7 = current2.nChannelNumber;
                                    num8 = current2.nPlaybackPosition;
                                    break;
                                case EChannel.Cymbal:
                                    if (CDTXMania.Random.Next(4) == 0)
                                    {
                                        current2.nChannelNumber = EChannel.LowTom;
                                    }

                                    if (num8 == current2.nPlaybackPosition && current2.nChannelNumber == num7)
                                    {
                                        current2.nChannelNumber = ((num7 == EChannel.Cymbal)
                                            ? EChannel.LowTom
                                            : EChannel.Cymbal);
                                    }

                                    num7 = current2.nChannelNumber;
                                    num8 = current2.nPlaybackPosition;
                                    break;
                            }

                            break;
                    }
                }
            }
        }
    }

    public void tRandomizeDrumPedal(EInstrumentPart part, ERandomMode eRandomPedal)
    {
        if (part == EInstrumentPart.DRUMS && eRandomPedal == ERandomMode.MIRROR)
        {
            foreach (CChip current in listChip)
            {
                EChannel nチャンネル番号 = current.nChannelNumber;
                if (part == EInstrumentPart.DRUMS && (nチャンネル番号 == EChannel.BassDrum ||
                                                      nチャンネル番号 == EChannel.LeftPedal ||
                                                      nチャンネル番号 == EChannel.LeftBassDrum))
                {
                    tミラーチップのチャンネルを指定する(current, nチャンネル番号);
                }
            }
        }
        else if (part == EInstrumentPart.DRUMS && eRandomPedal != ERandomMode.OFF &&
                 CDTXMania.ConfigIni.eNumOfLanes.Drums != Core.EType.C)
        {
            int num = CDTXMania.Random.Next(2);
            EChannel num2 = 0;
            int num3 = 0;
            int num4 = -10000;
            foreach (CChip current in listChip)
            {
                EChannel num5 = current.nChannelNumber;
                if (part == EInstrumentPart.DRUMS && (num5 == EChannel.BassDrum || num5 == EChannel.LeftPedal ||
                                                      num5 == EChannel.LeftBassDrum))
                {
                    if (num5 == EChannel.LeftBassDrum)
                    {
                        num5 = EChannel.LeftPedal;
                    }

                    switch (eRandomPedal)
                    {
                        case ERandomMode.RANDOM:
                            if (num5 == EChannel.BassDrum)
                            {
                                current.nChannelNumber = ((num == 0) ? EChannel.BassDrum : EChannel.LeftPedal);
                            }
                            else
                            {
                                current.nChannelNumber = ((num == 1) ? EChannel.BassDrum : EChannel.LeftPedal);
                            }

                            break;
                        case ERandomMode.SUPERRANDOM:
                            if (current.nPlaybackPosition / 384 != num4)
                            {
                                num4 = current.nPlaybackPosition / 384;
                                num = CDTXMania.Random.Next(2);
                            }

                            if (num5 == EChannel.BassDrum)
                            {
                                current.nChannelNumber = ((num == 0) ? EChannel.BassDrum : EChannel.LeftPedal);
                            }
                            else
                            {
                                current.nChannelNumber = ((num == 1) ? EChannel.BassDrum : EChannel.LeftPedal);
                            }

                            break;
                        case ERandomMode.HYPERRANDOM:
                            if (current.nPlaybackPosition / 96 != num4)
                            {
                                num4 = current.nPlaybackPosition / 96;
                                num = CDTXMania.Random.Next(2);
                            }

                            if (num5 == EChannel.BassDrum)
                            {
                                current.nChannelNumber = ((num == 0) ? EChannel.BassDrum : EChannel.LeftPedal);
                            }
                            else
                            {
                                current.nChannelNumber = ((num == 1) ? EChannel.BassDrum : EChannel.LeftPedal);
                            }

                            break;
                        case ERandomMode.MASTERRANDOM:
                            if (num3 != current.nPlaybackPosition)
                            {
                                num = CDTXMania.Random.Next(2);
                                if (num5 == EChannel.BassDrum)
                                {
                                    current.nChannelNumber = ((num == 0) ? EChannel.BassDrum : EChannel.LeftPedal);
                                }
                                else
                                {
                                    current.nChannelNumber = ((num == 1) ? EChannel.BassDrum : EChannel.LeftPedal);
                                }
                            }
                            else
                            {
                                current.nChannelNumber = ((num2 == EChannel.BassDrum)
                                    ? EChannel.LeftPedal
                                    : EChannel.BassDrum);
                            }

                            num3 = current.nPlaybackPosition;
                            num2 = current.nChannelNumber;
                            break;
                        case ERandomMode.ANOTHERRANDOM:
                            if (num3 != current.nPlaybackPosition)
                            {
                                if (CDTXMania.Random.Next(4) == 0)
                                {
                                    current.nChannelNumber = ((num5 == EChannel.BassDrum)
                                        ? EChannel.LeftPedal
                                        : EChannel.BassDrum);
                                }
                                else
                                {
                                    if (current.nChannelNumber == EChannel.LeftBassDrum)
                                    {
                                        current.nChannelNumber = EChannel.LeftPedal;
                                    }
                                }
                            }
                            else
                            {
                                current.nChannelNumber = ((num2 == EChannel.BassDrum)
                                    ? EChannel.LeftPedal
                                    : EChannel.BassDrum);
                            }

                            num2 = current.nChannelNumber;
                            num3 = current.nPlaybackPosition;
                            break;
                    }
                }
            }
        }
    }

    private static void t乱数を各チャンネルに指定する(int[] nシンバルグループ, int[] nタムグループ, CChip chip, EChannel nランダム化前チャンネル番号)
    {
        switch (nランダム化前チャンネル番号)
        {
            case EChannel.HiHatClose:
            case EChannel.HiHatOpen:
                chip.nChannelNumber = nシンバルグループ[0] + EChannel.HiHatClose;
                return;
            case EChannel.Snare:
                chip.nChannelNumber = nタムグループ[0] + EChannel.HiHatClose;
                return;
            case EChannel.BassDrum:
                break;
            case EChannel.HighTom:
                chip.nChannelNumber = nタムグループ[1] + EChannel.HiHatClose;
                return;
            case EChannel.LowTom:
                chip.nChannelNumber = nタムグループ[2] + EChannel.HiHatClose;
                return;
            case EChannel.Cymbal:
                chip.nChannelNumber = nシンバルグループ[1] + EChannel.HiHatClose;
                return;
            case EChannel.FloorTom:
                chip.nChannelNumber = nタムグループ[3] + EChannel.HiHatClose;
                return;
            case EChannel.RideCymbal:
                chip.nChannelNumber = nシンバルグループ[2] + EChannel.HiHatClose;
                return;
            case EChannel.LeftCymbal:
                chip.nChannelNumber = nシンバルグループ[3] + EChannel.HiHatClose;
                break;
            default:
                return;
        }
    }

    private static void tグループランダム分配作業(out int[] nシンバルグループ, out int[] nタムグループ)
    {
        nシンバルグループ = new int[4];
        nタムグループ = new int[4];
        int[] array = new int[8];
        int num = 0;
        int num2 = 0;
        bool[] array2 = new bool[8];
        bool[] array3 = array2;
        for (int i = 0; i < 8; i++)
        {
            int num3;
            do
            {
                num3 = CDTXMania.Random.Next(8);
            } while (array3[num3]);

            if (i < 2)
            {
                array[num3] = i;
            }
            else
            {
                array[num3] = ((i >= 6) ? (i + 2) : (i + 1));
            }

            array3[num3] = true;
        }

        for (int j = 0; j < 8; j++)
        {
            if (array[j] == 0 || array[j] == 5 || array[j] == 8 || array[j] == 9)
            {
                nシンバルグループ[num++] = array[j];
            }
            else
            {
                nタムグループ[num2++] = array[j];
            }
        }
    }

    private static void t乱数を各チャンネルに指定する_クラシック(int[] n乱数排列数列, CChip chip, EChannel nランダム化前チャンネル番号)
    {
        switch (nランダム化前チャンネル番号)
        {
            case EChannel.HiHatClose:
            case EChannel.HiHatOpen:
                chip.nChannelNumber = n乱数排列数列[0] + EChannel.HiHatClose;
                return;
            case EChannel.Snare:
                chip.nChannelNumber = n乱数排列数列[1] + EChannel.HiHatClose;
                return;
            case EChannel.BassDrum:
            case EChannel.FloorTom:
                break;
            case EChannel.HighTom:
                chip.nChannelNumber = n乱数排列数列[2] + EChannel.HiHatClose;
                return;
            case EChannel.LowTom:
                chip.nChannelNumber = n乱数排列数列[3] + EChannel.HiHatClose;
                return;
            case EChannel.Cymbal:
                chip.nChannelNumber = n乱数排列数列[4] + EChannel.HiHatClose;
                break;
            default:
                return;
        }
    }

    private static void t乱数排列数列生成作業_クラシック(out int[] n乱数排列数列)
    {
        n乱数排列数列 = new int[5];
        bool[] array = new bool[5];
        bool[] array2 = array;
        for (int i = 0; i < 5; i++)
        {
            int num;
            do
            {
                num = CDTXMania.Random.Next(5);
            } while (array2[num]);

            n乱数排列数列[num] = ((i >= 2) ? (i + 1) : i);
            array2[num] = true;
        }
    }

    private void t指定された発声位置と同じ位置の指定したチップにボーナスフラグを立てる(int n発声位置, int nLane)
    {
        //ボーナスチップの内部番号→チャンネル番号変換
        //初期値は0で問題無いはず。
        EChannel n変換後のレーン番号 = 0;
        EChannel n変換後のレーン番号2 = 0; //HH、LP用
        switch (nLane)
        {
            case 1:
                n変換後のレーン番号 = EChannel.LeftCymbal;
                break;
            case 2:
                n変換後のレーン番号 = EChannel.HiHatClose;
                n変換後のレーン番号2 = EChannel.HiHatOpen;
                break;
            case 3:
                n変換後のレーン番号 = EChannel.LeftPedal;
                n変換後のレーン番号2 = EChannel.LeftBassDrum;
                break;
            case 4:
                n変換後のレーン番号 = EChannel.Snare;
                break;
            case 5:
                n変換後のレーン番号 = EChannel.HighTom;
                break;
            case 6:
                n変換後のレーン番号 = EChannel.BassDrum;
                break;
            case 7:
                n変換後のレーン番号 = EChannel.LowTom;
                break;
            case 8:
                n変換後のレーン番号 = EChannel.FloorTom;
                break;
            case 9:
                n変換後のレーン番号 = EChannel.Cymbal;
                break;
            case 10:
                n変換後のレーン番号 = EChannel.RideCymbal;
                break;
        }

        //本当はfor文検索はよろしくないんだろうけど、僕の技術ではこれが限界なんだ...
        for (int i = 0; i < listChip.Count; i++)
        {
            if (listChip[i].nPlaybackPosition == n発声位置)
            {
                if (listChip[i].nChannelNumber == n変換後のレーン番号 || listChip[i].nChannelNumber == n変換後のレーン番号2)
                {
                    listChip[i].bBonusChip = true;
                }
            }
        }
    }

    public void tチップの再生(CChip pChip, long n再生開始システム時刻ms, int nVol, bool bBad = false)
    {
        if (pChip.nIntegerValue_InternalNumber >= 0)
        {
            if (listWAV.TryGetValue(pChip.nIntegerValue_InternalNumber, out CWAV? wc))
            {
                int index = wc.n現在再生中のサウンド番号 = (wc.n現在再生中のサウンド番号 + 1) % nPolyphonicSounds;
                if ((wc.rSound[0] != null) &&
                    (wc.rSound[0].bストリーム再生する || wc.rSound[index] == null))
                {
                    index = wc.n現在再生中のサウンド番号 = 0;
                }

                CSound sound = wc.rSound[index];
                if (sound != null)
                {
                    if (bBad)
                    {
                        sound.db周波数倍率 =
                            (100 + (((CDTXMania.Random.Next(3) + 1) * 7) * (1 - (CDTXMania.Random.Next(2) * 2)))) /
                            100f;
                    }
                    else
                    {
                        sound.db周波数倍率 = 1.0;
                    }

                    sound.dbPlaySpeed = CDTXMania.ConfigIni.nPlaySpeed / 20.0;
                    
                    // 再生速度によって、WASAPI/ASIOで使う使用mixerが決まるため、付随情報の設定(音量/PAN)は、再生速度の設定後に行う
                    sound.nVolume = (int)(nVol * wc.nVolume / 100.0);
                    sound.nPosition = wc.nPosition;
                    sound.tStartPlaying();
                }

                wc.nPlayStartTime[wc.n現在再生中のサウンド番号] = n再生開始システム時刻ms;
                tAutoCorrectWavPlaybackPosition(wc);
            }
        }
    }

    public void tAutoCorrectWavPlaybackPosition() // tWave再生位置自動補正
    {
        foreach (CWAV cwav in listWAV.Values)
        {
            tAutoCorrectWavPlaybackPosition(cwav);
        }
    }

    public void tAutoCorrectWavPlaybackPosition(CWAV wc) // tWave再生位置自動補正
    {
        if (wc.rSound[0] != null && wc.rSound[0].nTotalPlayTimeMs >= 5000)
        {
            for (int i = 0; i < nPolyphonicSounds; i++)
            {
                if ((wc.rSound[i] != null) && (wc.rSound[i].b再生中))
                {
                    long nCurrentTime = CSoundManager.rcPerformanceTimer.nSystemTimeMs;
                    if (nCurrentTime > wc.nPlayStartTime[i])
                    {
                        long nAbsTimeFromStartPlaying = nCurrentTime - wc.nPlayStartTime[i];
                        //Trace.TraceInformation( "再生位置自動補正: {0}, seek先={1}ms, 全音長={2}ms",
                        //    Path.GetFileName( wc.rSound[ 0 ].strFilename ),
                        //    nAbsTimeFromStartPlaying,
                        //    wc.rSound[ 0 ].nTotalPlayTimeMs
                        //);
                        // wc.rSound[ i ].tChangePlaybackPosition( wc.rSound[ i ].t時刻から位置を返す( nAbsTimeFromStartPlaying ) );
                        wc.rSound[i].tChangePlaybackPosition(nAbsTimeFromStartPlaying); // WASAPI/ASIO用
                    }
                }
            }
        }
    }

    public void tStopPlayingWav(int nWaveの内部番号)
    {
        if (!listWAV.TryGetValue(nWaveの内部番号, out CWAV? cwav)) return;
        
        for (int i = 0; i < nPolyphonicSounds; i++)
        {
            if (cwav.rSound[i] != null && cwav.rSound[i].b再生中)
            {
                cwav.rSound[i].tStopPlayback();
            }
        }
    }

    public void tLoadWAV(CWAV cwav)
    {
//			Trace.TraceInformation("WAV files={0}", this.listWAV.Count);
//			int count = 0;
//			foreach (CWAV cwav in this.listWAV.Values)
        {
//				string strCount = count.ToString() + " / " + this.listWAV.Count.ToString();
//				Debug.WriteLine(strCount);
//				CDTXMania.act文字コンソール.tPrint(0, 0, CCharacterConsole.Eフォント種別.白, strCount);
//				count++;

            string str = string.IsNullOrEmpty(PATH_WAV) ? strFolderName : PATH_WAV;
            str = str + PATH + cwav.strFileName;
            _ = (CDTXMania.SoundManager.GetCurrentSoundDeviceType() == "DirectSound");
            try
            {
                //try
                //{
                //    cwav.rSound[ 0 ] = CDTXMania.SoundManager.tGenerateSound( str );
                //    cwav.rSound[ 0 ].nVolume = 100;
                //    if ( CDTXMania.ConfigIni.bLog作成解放ログ出力 )
                //    {
                //        Trace.TraceInformation( "サウンドを作成しました。({3})({0})({1})({2}bytes)", cwav.strコメント文, str, cwav.rSound[ 0 ].nサウンドバッファサイズ, cwav.rSound[ 0 ].bストリーム再生する ? "Stream" : "OnMemory" );
                //    }
                //}
                //catch
                //{
                //    cwav.rSound[ 0 ] = null;
                //    Trace.TraceError( "サウンドの作成に失敗しました。({0})({1})", cwav.strコメント文, str );
                //}
                //if ( cwav.rSound[ 0 ] == null )	// #xxxxx 2012.5.3 yyagi rSound[1-3]もClone()するようにし、これらのストリーム再生がおかしくなる問題を修正
                //{
                //    for ( int j = 1; j < nPolyphonicSounds; j++ )
                //    {
                //        cwav.rSound[ j ] = null;
                //    }
                //}
                //else
                //{
                //    for ( int j = 1; j < nPolyphonicSounds; j++ )
                //    {
                //        cwav.rSound[ j ] = (CSound) cwav.rSound[ 0 ].Clone();	// #24007 2011.9.5 yyagi add: to accelerate loading chip sounds
                //        CDTXMania.SoundManager.tサウンドを登録する( cwav.rSound[ j ] );
                //    }
                //}

                // まず1つめを登録する
                try
                {
                    cwav.rSound[0] = CDTXMania.SoundManager.tGenerateSound(str);
                    cwav.rSound[0].nVolume = 100;
                    if (CDTXMania.ConfigIni.bLog作成解放ログ出力)
                    {
                        Trace.TraceInformation("サウンドを作成しました。({3})({0})({1})({2}bytes)", cwav.strコメント文, str,
                            cwav.rSound[0].nサウンドバッファサイズ, cwav.rSound[0].bストリーム再生する ? "Stream" : "OnMemory");
                    }
                }
                catch (Exception e)
                {
                    cwav.rSound[0] = null;
                    Trace.TraceError("サウンドの作成に失敗しました。({0})({1})", cwav.strコメント文, str);
                    Trace.TraceError("例外: " + e.Message);
                }

                #region [ 同時発音数を、チャンネルによって変える ]

                int nPoly = nPolyphonicSounds;
                if (CDTXMania.SoundManager.GetCurrentSoundDeviceType() !=
                    "DirectSound") // DShowでの再生の場合はミキシング負荷が高くないため、
                {
                    // チップのライフタイム管理を行わない
                    if (cwav.bIsBassSound) nPoly = (nPolyphonicSounds >= 2) ? 2 : 1;
                    else if (cwav.bIsGuitarSound) nPoly = (nPolyphonicSounds >= 2) ? 2 : 1;
                    else if (cwav.bIsSESound) nPoly = 1;
                    else if (cwav.bIsBGMSound) nPoly = 1;
                }

                if (cwav.bIsBGMSound) nPoly = 1;

                #endregion

                // 残りはClone等で登録する
                //if ( bIsDirectSound )	// DShowでの再生の場合はCloneする
                //{
                //    for ( int i = 1; i < nPoly; i++ )
                //    {
                //        cwav.rSound[ i ] = (CSound) cwav.rSound[ 0 ].Clone();	// #24007 2011.9.5 yyagi add: to accelerate loading chip sounds
                //        // CDTXMania.SoundManager.tサウンドを登録する( cwav.rSound[ j ] );
                //    }
                //    for ( int i = nPoly; i < nPolyphonicSounds; i++ )
                //    {
                //        cwav.rSound[ i ] = null;
                //    }
                //}
                //else															// WASAPI/ASIO時は通常通り登録
                {
                    for (int i = 1; i < nPoly; i++)
                    {
                        try
                        {
                            cwav.rSound[i] = CDTXMania.SoundManager.tGenerateSound(str);
                            cwav.rSound[i].nVolume = 100;
                            if (CDTXMania.ConfigIni.bLog作成解放ログ出力)
                            {
                                Trace.TraceInformation("サウンドを作成しました。({3})({0})({1})({2}bytes)", cwav.strコメント文, str,
                                    cwav.rSound[0].nサウンドバッファサイズ, cwav.rSound[0].bストリーム再生する ? "Stream" : "OnMemory");
                            }
                        }
                        catch (Exception e)
                        {
                            cwav.rSound[i] = null;
                            Trace.TraceError("サウンドの作成に失敗しました。({0})({1})", cwav.strコメント文, str);
                            Trace.TraceError("例外: " + e.Message);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Trace.TraceError("サウンドの生成に失敗しました。({0})({1})({2})", exception.Message, cwav.strコメント文, str);
                for (int j = 0; j < nPolyphonicSounds; j++)
                {
                    cwav.rSound[j] = null;
                }
                //continue;
            }
        }
    }

    public static string Base36ToString(int n)
    {
        if (n < 0 || n >= 36 * 36)
            return "!!"; // Over or underflow

        // n を36進数2桁の文字列にして返す。

        string str = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return new string(new char[] { str[n / 36], str[n % 36] });
    }

    public void tRandomizeGuitarAndBass(EInstrumentPart part, ERandomMode eRandom)
    {
        if (((part == EInstrumentPart.GUITAR) || (part == EInstrumentPart.BASS)) && (eRandom != ERandomMode.OFF))
        {
            int[,] nランダムレーン候補 = new int[,]
            {
                { 0, 1, 2, 3, 4, 5, 6, 7 }, { 0, 2, 1, 3, 4, 6, 5, 7 }, { 0, 1, 4, 5, 2, 3, 6, 7 },
                { 0, 2, 4, 6, 1, 3, 5, 7 }, { 0, 4, 1, 5, 2, 6, 3, 7 }, { 0, 4, 2, 6, 1, 5, 3, 7 }
            };
            int n小節番号 = -10000;
            int n小節内乱数6通り = 0;
            // int GOTO_END = 0;	// gotoの飛び先のダミーコードで使うダミー変数
            foreach (CChip chip in listChip)
            {
                int nRGBレーンビットパターン;
                int n新RGBレーンビットパターン = 0; // 「未割り当てのローカル変数」ビルドエラー回避のために0を初期値に設定
                bool flag;
                if ((chip.nPlaybackPosition / 384) != n小節番号) // 小節が変化したら
                {
                    n小節番号 = chip.nPlaybackPosition / 384;
                    n小節内乱数6通り = CDTXMania.Random.Next(6);
                }

                EChannel nランダム化前チャンネル番号 = chip.nChannelNumber;
                if ((((part != EInstrumentPart.GUITAR) || (EChannel.Guitar_Open > nランダム化前チャンネル番号)) ||
                     (nランダム化前チャンネル番号 > EChannel.Guitar_RGBxx))
                    && (((part != EInstrumentPart.BASS) || (EChannel.Bass_Open > nランダム化前チャンネル番号)) ||
                        (nランダム化前チャンネル番号 > EChannel.Bass_RGBxx))
                   )
                {
                    continue;
                }

                switch (eRandom)
                {
                    case ERandomMode.RANDOM: // 1小節単位でレーンのR/G/Bがランダムに入れ替わる
                        chip.nChannelNumber = (EChannel)(((int)nランダム化前チャンネル番号 & 0xF0) |
                                                         nランダムレーン候補[n小節内乱数6通り, (int)nランダム化前チャンネル番号 & 0x07]);
                        continue; // goto Label_02C4;

                    case ERandomMode.SUPERRANDOM: // チップごとにR/G/Bがランダムで入れ替わる(レーンの本数までは変わらない)。
                        chip.nChannelNumber = (EChannel)(((int)nランダム化前チャンネル番号 & 0xF0) |
                                                         nランダムレーン候補[CDTXMania.Random.Next(6),
                                                             (int)nランダム化前チャンネル番号 & 0x07]);
                        continue; // goto Label_02C4;

                    case ERandomMode.HYPERRANDOM: // レーンの本数も変わる
                        nRGBレーンビットパターン = (int)nランダム化前チャンネル番号 & 7;
                        // n新RGBレーンビットパターン = (int)Eレーンビットパターン.OPEN;	// この値は結局未使用なので削除
                        flag = ((part == EInstrumentPart.GUITAR) && bHasChips.OpenGuitar) ||
                               ((part == EInstrumentPart.BASS) &&
                                bHasChips.OpenBass); // #23546 2010.10.28 yyagi fixed (bチップがある.Bass→bチップがある.OpenBass)
                        //New: Set flag to false (disable Open) when chip has long note
                        if (chip.bロングノートである)
                        {
                            flag = false;
                        }

                        if (((nRGBレーンビットパターン != (int)Eレーンビットパターン.xxB) &&
                             (nRGBレーンビットパターン != (int)Eレーンビットパターン.xGx)) &&
                            (nRGBレーンビットパターン != (int)Eレーンビットパターン.Rxx)) // xxB, xGx, Rxx レーン1本相当
                        {
                            break; // レーン1本相当でなければ、とりあえず先に進む
                        }

                        n新RGBレーンビットパターン = CDTXMania.Random.Next(6) + 1; // レーン1本相当なら、レーン1本か2本(1～6)に変化して終了
                        goto Label_02B2;

                    default:
                        continue; // goto Label_02C4;
                }

                switch (nRGBレーンビットパターン)
                {
                    case (int)Eレーンビットパターン.xGB: // xGB	レーン2本相当
                    case (int)Eレーンビットパターン.RxB: // RxB
                    case (int)Eレーンビットパターン.RGx: // RGx
                        n新RGBレーンビットパターン =
                            flag
                                ? CDTXMania.Random.Next(8)
                                : (CDTXMania.Random.Next(7) + 1); // OPENあり譜面ならOPENを含むランダム, OPENなし譜面ならOPENを含まないランダム
                        break; // goto Label_02B2;

                    default:
                        if (nRGBレーンビットパターン == (int)Eレーンビットパターン.RGB) // RGB レーン3本相当
                        {
                            if (flag) // OPENあり譜面の場合
                            {
                                int n乱数パーセント = CDTXMania.Random.Next(100);
                                if (n乱数パーセント < 30)
                                {
                                    n新RGBレーンビットパターン = (int)Eレーンビットパターン.OPEN;
                                }
                                else if (n乱数パーセント < 60)
                                {
                                    n新RGBレーンビットパターン = (int)Eレーンビットパターン.RGB;
                                }
                                else if (n乱数パーセント < 85)
                                {
                                    switch (CDTXMania.Random.Next(3))
                                    {
                                        case 0:
                                            n新RGBレーンビットパターン = (int)Eレーンビットパターン.xGB;
                                            break; // goto Label_02B2;

                                        case 1:
                                            n新RGBレーンビットパターン = (int)Eレーンビットパターン.RxB;
                                            break; // goto Label_02B2;
                                    }

                                    n新RGBレーンビットパターン = (int)Eレーンビットパターン.RGx;
                                }
                                else // OPENでない場合
                                {
                                    switch (CDTXMania.Random.Next(3))
                                    {
                                        case 0:
                                            n新RGBレーンビットパターン = (int)Eレーンビットパターン.xxB;
                                            break; // goto Label_02B2;

                                        case 1:
                                            n新RGBレーンビットパターン = (int)Eレーンビットパターン.xGx;
                                            break; // goto Label_02B2;
                                    }

                                    n新RGBレーンビットパターン = (int)Eレーンビットパターン.Rxx;
                                }
                            }
                            else // OPENなし譜面の場合
                            {
                                int n乱数パーセント = CDTXMania.Random.Next(100);
                                if (n乱数パーセント < 60)
                                {
                                    n新RGBレーンビットパターン = (int)Eレーンビットパターン.RGB;
                                }
                                else if (n乱数パーセント < 85)
                                {
                                    switch (CDTXMania.Random.Next(3))
                                    {
                                        case 0:
                                            n新RGBレーンビットパターン = (int)Eレーンビットパターン.xGB;
                                            break; // goto Label_02B2;

                                        case 1:
                                            n新RGBレーンビットパターン = (int)Eレーンビットパターン.RxB;
                                            break; // goto Label_02B2;
                                    }

                                    n新RGBレーンビットパターン = (int)Eレーンビットパターン.RGx;
                                }
                                else
                                {
                                    switch (CDTXMania.Random.Next(3))
                                    {
                                        case 0:
                                            n新RGBレーンビットパターン = (int)Eレーンビットパターン.xxB;
                                            break; // goto Label_02B2;

                                        case 1:
                                            n新RGBレーンビットパターン = (int)Eレーンビットパターン.xGx;
                                            break; // goto Label_02B2;
                                    }

                                    n新RGBレーンビットパターン = (int)Eレーンビットパターン.Rxx;
                                }
                            }
                        }

                        break; // goto Label_02B2;
                }

                Label_02B2:
                chip.nChannelNumber = (EChannel)(((int)nランダム化前チャンネル番号 & 0xF0) | n新RGBレーンビットパターン);
//				Label_02C4:
//					GOTO_END++;		// goto用のダミーコード
            }
        }
    }

    #region [ チップの再生と停止 ]

    public void tPlayChip(CChip rChip, long n再生開始システム時刻ms, int nLane)
    {
        tPlayChip(rChip, n再生開始システム時刻ms, nLane, CDTXMania.ConfigIni.n自動再生音量, false, false);
    }

    public void tPlayChip(CChip rChip, long n再生開始システム時刻ms, int nLane, int nVol)
    {
        tPlayChip(rChip, n再生開始システム時刻ms, nLane, nVol, false, false);
    }

    public void tPlayChip(CChip rChip, long n再生開始システム時刻ms, int nLane, int nVol, bool bMIDIMonitor)
    {
        tPlayChip(rChip, n再生開始システム時刻ms, nLane, nVol, bMIDIMonitor, false);
    }

    public void tPlayChip(CChip pChip, long n再生開始システム時刻ms, int nLane, int nVol, bool bMIDIMonitor, bool bBad)
    {
        if (pChip.nIntegerValue_InternalNumber >= 0)
        {
            if ((nLane < (int)ELane.LC) || ((int)ELane.BGM < nLane))
            {
                throw new ArgumentOutOfRangeException();
            }

            if (listWAV.ContainsKey(pChip.nIntegerValue_InternalNumber))
            {
                CWAV wc = listWAV[pChip.nIntegerValue_InternalNumber];
                int index = wc.n現在再生中のサウンド番号 = (wc.n現在再生中のサウンド番号 + 1) % nPolyphonicSounds;
                if ((wc.rSound[0] != null) &&
                    (wc.rSound[0].bストリーム再生する || wc.rSound[index] == null))
                {
                    index = wc.n現在再生中のサウンド番号 = 0;
                }

                CSound sound = wc.rSound[index];
                if (sound != null)
                {
                    if (bBad)
                    {
                        sound.db周波数倍率 =
                            (100 + (((CDTXMania.Random.Next(3) + 1) * 7) * (1 - (CDTXMania.Random.Next(2) * 2)))) /
                            100f;
                    }
                    else
                    {
                        sound.db周波数倍率 = 1.0;
                    }

                    sound.dbPlaySpeed = CDTXMania.ConfigIni.nPlaySpeed / 20.0;
                    // 再生速度によって、WASAPI/ASIOで使う使用mixerが決まるため、付随情報の設定(音量/PAN)は、再生速度の設定後に行う
                    sound.nVolume = (int)(nVol * wc.nVolume / 100.0);
                    sound.nPosition = wc.nPosition;
                    sound.tStartPlaying();
                }

                wc.nPlayStartTime[wc.n現在再生中のサウンド番号] = n再生開始システム時刻ms;
                tAutoCorrectWavPlaybackPosition(wc);
            }
        }
    }

    public void t各自動再生音チップの再生時刻を変更する(int nBGMAdjustの増減値)
    {
        t各自動再生音チップの再生時刻を変更する(nBGMAdjustの増減値, true, false);
    }

    public void t各自動再生音チップの再生時刻を変更する(int nBGMAdjustの増減値, bool bScoreIni保存, bool bConfig保存)
    {
        if (bScoreIni保存)
            nBGMAdjust += nBGMAdjustの増減値;
        if (bConfig保存)
            CDTXMania.ConfigIni.nCommonBGMAdjustMs = nBGMAdjustの増減値;

        for (int i = 0; i < listChip.Count; i++)
        {
            EChannel nChannelNumber = listChip[i].nChannelNumber;
            if (((
                     (nChannelNumber == EChannel.BGM) ||
                     ((EChannel.SE01 <= nChannelNumber) && (nChannelNumber <= EChannel.SE09))
                 ) ||
                 ((EChannel.SE10 <= nChannelNumber) && (nChannelNumber <= EChannel.SE19))
                ) ||
                (((EChannel.SE20 <= nChannelNumber) && (nChannelNumber <= EChannel.SE29)) ||
                 ((EChannel.SE30 <= nChannelNumber) && (nChannelNumber <= EChannel.SE32)))
               )
            {
                listChip[i].nPlaybackTimeMs += nBGMAdjustの増減値;
            }
        }

        foreach (CWAV cwav in listWAV.Values)
        {
            for (int j = 0; j < nPolyphonicSounds; j++)
            {
                if ((cwav.rSound[j] != null) && cwav.rSound[j].b再生中)
                {
                    cwav.nPlayStartTime[j] += nBGMAdjustの増減値;
                }
            }
        }
    }

    public void tPausePlaybackForAllChips()
    {
        foreach (CWAV cwav in listWAV.Values)
        {
            for (int i = 0; i < nPolyphonicSounds; i++)
            {
                if ((cwav.rSound[i] != null) && cwav.rSound[i].b再生中)
                {
                    cwav.rSound[i].tPausePlayback();
                    cwav.nPauseTime[i] = CSoundManager.rcPerformanceTimer.nSystemTimeMs;
                }
            }
        }
    }

    public void tResumePlaybackForAllChips()
    {
        foreach (CWAV cwav in listWAV.Values)
        {
            for (int i = 0; i < nPolyphonicSounds; i++)
            {
                if ((cwav.rSound[i] != null) && cwav.rSound[i].b一時停止中)
                {
                    //long num1 = cwav.nPauseTime[ i ];
                    //long num2 = cwav.nPlayStartTime[ i ];
                    cwav.rSound[i].tResumePlayback(cwav.nPauseTime[i] - cwav.nPlayStartTime[i]);
                    cwav.nPlayStartTime[i] += CSoundManager.rcPerformanceTimer.nSystemTimeMs - cwav.nPauseTime[i];
                }
            }
        }
    }

    public void tStopPlayingAllChips() // t全チップの再生停止
    {
        foreach (CWAV cwav in listWAV.Values)
        {
            tStopPlayingWav(cwav.n内部番号);
        }
    }

    #endregion

    private void tRead(string strFileName, bool bHeaderOnly, double dbReplaySpeed = 1.0, int nBgmAdjust = 0)
    {
        Console.WriteLine("Reading DTX file: " + strFileName);
        
        this.bHeaderOnly = bHeaderOnly;
        b動画読み込み = (CDTXMania.StageManager.rCurrentStage.eStageID == CStage.EStage.SongLoading_5);
        strFileNameFullPath = Path.GetFullPath(strFileName);
        this.strFileName = Path.GetFileName(strFileNameFullPath);
        strFolderName = Path.GetDirectoryName(strFileNameFullPath) + @"\";
        string ext = Path.GetExtension(this.strFileName).ToLower();
        if (!string.IsNullOrEmpty(ext))
        {
            if (ext != ".dtx")
            {
                eFileType = ext switch
                {
                    ".gda" => EType.GDA,
                    ".g2d" => EType.G2D,
                    ".bms" => EType.BMS,
                    ".bme" => EType.BME,
                    ".mid" => EType.SMF,
                    _ => eFileType
                };
            }
            else
            {
                eFileType = EType.DTX;
            }
        }

        if (eFileType != EType.SMF)
        {
            try
            {
                this.db再生速度 = dbReplaySpeed;

                StreamReader reader = new(strFileName, Encoding.GetEncoding("shift-jis"));
                    
                //Stopwatch sw = new();
                //sw.Start();
                tRead_FromStream(reader);
                //sw.Stop();
                //TimeSpan span = sw.Elapsed;
                //Console.WriteLine("{0} tRead_FromStream {1:0.00} ms", strFileName, span.TotalMilliseconds);
                reader.Close();
                    
                // sw.Reset();
                // sw.Start();
                tProcessChartData(nBgmAdjust);
                // sw.Stop();
                // span = sw.Elapsed;
                //Console.WriteLine("{0} tProcessChartData {1:0.00} ms", strFileName, span.TotalMilliseconds);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);
                Trace.TraceError(e.StackTrace);
            }
        }
        else
        {
            Trace.TraceWarning("SMF の演奏は未対応です。（検討中）");
        }
    }


    private bool t入力_コメントをスキップする( ref CharEnumerator ce )
    {
        // 改行が現れるまでをコメントと見なしてスキップする。

        while( ce.Current != '\n' )
        {
            if( !ce.MoveNext() )
                return false;	// 文字が尽きた
        }

        // 改行の次の文字へ移動した結果を返す。

        return ce.MoveNext();
    }
        
    private void tRead_FromString(string inputString)
    { 
        if (string.IsNullOrEmpty(inputString)) return;

        #region [ 改行カット ]

        inputString = inputString.Replace(Environment.NewLine, "\n");
        inputString = inputString.Replace('\t', ' ');
        inputString += "\n";

        #endregion

        #region [ Initialization ]

        for (int j = 0; j < 36 * 36; j++)
        {
            n無限管理WAV[j] = -j;
            n無限管理BPM[j] = -j;
            n無限管理VOL[j] = -j;
            n無限管理PAN[j] = -10000 - j;
            n無限管理SIZE[j] = -j;
        }

        n内部番号WAV1to = 1;
        n内部番号BPM1to = 1;
        bstackIFからENDIFをスキップする = new Stack<bool>();
        bstackIFからENDIFをスキップする.Push(false);
        nCurrentRandomNumber = 0;
        for (int k = 0; k < 7; k++)
        {
            nRESULTIMAGE用優先順位[k] = 0;
            nRESULTMOVIE用優先順位[k] = 0;
            nRESULTSOUND用優先順位[k] = 0;
        }

        #endregion

        #region [ 入力/行解析 ]

        CharEnumerator ce = inputString.GetEnumerator();
        if (!ce.MoveNext()) return;
            
        lineNumber = 1;
        do
        {
            if (!tSkipWhiteSpaceAndNewLines(ref ce))
            {
                break;
            }

            if (ce.Current != '#') continue;
            if (!ce.MoveNext()) break;

            StringBuilder builder = new(0x20);
            if (!ExtractCommand(ref ce, ref builder)) break;

            StringBuilder builder2 = new(0x400);
            if (!ExtractParameter(ref ce, ref builder2)) break;

            StringBuilder builder3 = new(0x400);
            if (!ExtractComment(ref ce, ref builder3)) break;

            string command = builder.ToString();
            string parameter = builder2.ToString();
            string comment = builder3.ToString();
                
            ParseLine(ref command, ref parameter, ref comment);
            lineNumber++;
        } 
        while (t入力_コメントをスキップする(ref ce));

        #endregion
    }
        
    private void tRead_FromStream(StreamReader reader)
    {
        reader.BaseStream.Seek(0, SeekOrigin.Begin);

        #region [ Initialization ]

        for (int j = 0; j < 36 * 36; j++)
        {
            n無限管理WAV[j] = -j;
            n無限管理BPM[j] = -j;
            n無限管理VOL[j] = -j;
            n無限管理PAN[j] = -10000 - j;
            n無限管理SIZE[j] = -j;
        }

        n内部番号WAV1to = 1;
        n内部番号BPM1to = 1;
        bstackIFからENDIFをスキップする = new Stack<bool>();
        bstackIFからENDIFをスキップする.Push(false);
        nCurrentRandomNumber = 0;
        for (int k = 0; k < 7; k++)
        {
            nRESULTIMAGE用優先順位[k] = 0;
            nRESULTMOVIE用優先順位[k] = 0;
            nRESULTSOUND用優先順位[k] = 0;
        }

        #endregion
            
        lineNumber = 1;
            
        //iterate
        while (!reader.EndOfStream)
        {
            string? line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) continue;

            //we use offset to index the current position in the line to avoid string allocations
            int offset = 1;
            if (line[0] != '#') continue;
            string cmd = SplitLineString(ref line, [':', ';', ' ', '\n'], ref offset);
                
            string param;
            if (offset < line.Length)
            {
                offset += char.IsWhiteSpace(line[offset]) ? 1 : 0;
                param = SplitLineString(ref line, [';', '\n', '\t'], ref offset);
            }
            else
            {
                param = string.Empty;
            }
                
            string comment;
            if (offset < line.Length)
            {
                offset += char.IsWhiteSpace(line[offset]) ? 1 : 0;
                comment = SplitLineString(ref line, ['\n'], ref offset);
            }
            else
            {
                comment = string.Empty;
            }

            ParseLine(ref cmd, ref param, ref comment);
                
            lineNumber++;
        }
    }

    private static string SplitLineString(ref string input, char[] splitCharacters, ref int offset)
    {
        int length = input.Length;
        int startOffset = offset;
        while (offset < length)
        {
            if (splitCharacters.Contains(input[offset]))
            {
                break;
            }

            offset++;
        }

        string result = input.Substring(startOffset, offset - startOffset);
            
        if (offset == length)
        {
            return result;
        }

        //update the offset to skip the split character
        offset += 1;
        return result;
    }

    private void tProcessChartData(int nBGMAdjust)
    {
        //For DTXVMode, always overwrite Config PlaySpeed with DTXVPlaySpeed
        if (CDTXMania.DTXVmode.Enabled)
        {
            Trace.TraceInformation("DTXVMode Enabled. Set PlaySpeed to {0}", dbDTXVPlaySpeed);
            CDTXMania.ConfigIni.nPlaySpeed = (int)(dbDTXVPlaySpeed * 20.0);
        }

        n無限管理WAV = null;
        n無限管理BPM = null;
        n無限管理VOL = null;
        n無限管理PAN = null;
        n無限管理SIZE = null;

        if (!bHeaderOnly)
        {
            #region [ BPM Initialization ]

            CBPM? cbpm = null;
            foreach (CBPM cbpm2 in listBPM.Values)
            {
                if (cbpm2.n表記上の番号 == 0)
                {
                    cbpm = cbpm2;
                    break;
                }
            }

            if (cbpm == null)
            {
                cbpm = new CBPM
                {
                    n内部番号 = n内部番号BPM1to++,
                    n表記上の番号 = 0,
                    dbBPM値 = 120.0
                };

                listBPM.Add(cbpm.n内部番号, cbpm);
                CChip chip = new()
                {
                    nPlaybackPosition = 0,
                    nChannelNumber = EChannel.BPMEx, // 拡張BPM
                    nIntegerValue = 0,
                    nIntegerValue_InternalNumber = cbpm.n内部番号
                };
                listChip.Insert(0, chip);
            }
            else
            {
                CChip chip = new()
                {
                    nPlaybackPosition = 0,
                    nChannelNumber = EChannel.BPMEx, // 拡張BPM
                    nIntegerValue = 0,
                    nIntegerValue_InternalNumber = cbpm.n内部番号
                };
                listChip.Insert(0, chip);
            }

            if (listBMP.ContainsKey(0))
            {
                CChip chip = new()
                {
                    nPlaybackPosition = 0,
                    nChannelNumber = EChannel.BGALayer1, // BGA (レイヤBGA1)
                    nIntegerValue = 0,
                    nIntegerValue_InternalNumber = 0
                };
                listChip.Insert(0, chip);
            }

            #endregion
            #region [ CWAV初期化 ]

            foreach (CWAV cwav in listWAV.Values)
            {
                if (cwav.nChipSize < 0)
                {
                    cwav.nChipSize = 100;
                }

                if (cwav.nPosition <= -10000)
                {
                    cwav.nPosition = 0;
                }

                if (cwav.nVolume < 0)
                {
                    cwav.nVolume = 100;
                }
            }

            #endregion
            #region [ チップ倍率設定 ] // #28145 2012.4.22 yyagi 二重ループを1重ループに変更して高速化)

            foreach (CChip chip in listChip)
            {
                if (listWAV.TryGetValue(chip.nIntegerValue_InternalNumber, out CWAV? cwav))
                {
                    chip.dbChipSizeRatio = cwav.nChipSize / 100.0;
                }
            }

            #endregion
            #region [ 拍子_拍線の挿入 ]

            if (listChip.Count > 0)
            {
                listChip.Sort(); // 高速化のためにはこれを削りたいが、listChipの最後がn発声位置の終端である必要があるので、
                // 保守性確保を優先してここでのソートは残しておく
                // なお、093時点では、このソートを削除しても動作するようにはしてある。
                // (ここまでの一部チップ登録を、listChip.Add(c)から同Insert(0,c)に変更してある)
                // これにより、数ms程度ながらここでのソートも高速化されている。
                double barLength = 1.0;
                int nEndOfSong = (listChip[listChip.Count - 1].nPlaybackPosition + 384) -
                                 (listChip[listChip.Count - 1].nPlaybackPosition % 384);
                for (int tick384 = 0;
                     tick384 <= nEndOfSong;
                     tick384 += 384) // 小節線の挿入　(後に出てくる拍子線とループをまとめようとするなら、forループの終了条件の微妙な違いに注意が必要)
                {
                    CChip chip = new()
                    {
                        nPlaybackPosition = tick384,
                        nChannelNumber = EChannel.BarLine, // 小節線
                        nIntegerValue = 36 * 36 - 1
                    };
                    listChip.Add(chip);
                }

                //this.listChip.Sort();				// ここでのソートは不要。ただし最後にソートすること
                int nChipNo_BarLength = 0;
                int nChipNo_C1 = 0;
                for (int tick384 = 0; tick384 < nEndOfSong; tick384 += 384)
                {
                    int n発声位置_C1_同一小節内 = 0;
                    while ((nChipNo_C1 < listChip.Count) &&
                           (listChip[nChipNo_C1].nPlaybackPosition < (tick384 + 384)))
                    {
                        if (listChip[nChipNo_C1].nChannelNumber == EChannel.BeatLineShift) // 拍線シフトの検出
                        {
                            n発声位置_C1_同一小節内 = listChip[nChipNo_C1].nPlaybackPosition - tick384;
                        }

                        nChipNo_C1++;
                    }

                    if ((eFileType == EType.BMS) || (eFileType == EType.BME))
                    {
                        barLength = 1.0;
                    }

                    while ((nChipNo_BarLength < listChip.Count) &&
                           (listChip[nChipNo_BarLength].nPlaybackPosition <= tick384))
                    {
                        if (listChip[nChipNo_BarLength].nChannelNumber == EChannel.BarLength) // bar lengthの検出
                        {
                            barLength = listChip[nChipNo_BarLength].db実数値;
                        }

                        nChipNo_BarLength++;
                    }

                    for (int i = 0; i < 100; i++) // 拍線の挿入
                    {
                        int tickBeat = (int)(384 * i / (4.0 * barLength));
                        if ((tickBeat + n発声位置_C1_同一小節内) >= 384)
                        {
                            break;
                        }

                        if (((tickBeat + n発声位置_C1_同一小節内) % 384) != 0)
                        {
                            CChip chip = new()
                            {
                                nPlaybackPosition = tick384 + (tickBeat + n発声位置_C1_同一小節内),
                                nChannelNumber = EChannel.BeatLine, // beat line 拍線
                                nIntegerValue = 36 * 36 - 1
                            };
                            listChip.Add(chip);
                        }
                    }
                }

                listChip.Sort();
            }

            #endregion
            #region [ C2 [拍線_小節線表示指定] の処理 ] // #28145 2012.4.21 yyagi; 2重ループをほぼ1重にして高速化

            bool bShowBeatBarLine = true;
            for (int i = 0; i < listChip.Count; i++)
            {
                bool bChangedBeatBarStatus = false;
                if ((listChip[i].nChannelNumber == EChannel.BeatLineDisplay))
                {
                    if (listChip[i].nIntegerValue == 1) // BAR/BEAT LINE = ON
                    {
                        bShowBeatBarLine = true;
                        bChangedBeatBarStatus = true;
                    }
                    else if (listChip[i].nIntegerValue == 2) // BAR/BEAT LINE = OFF
                    {
                        bShowBeatBarLine = false;
                        bChangedBeatBarStatus = true;
                    }
                }

                int startIndex = i;
                if (bChangedBeatBarStatus) // C2チップの前に50/51チップが来ている可能性に配慮
                {
                    while (startIndex > 0 &&
                           listChip[startIndex].nPlaybackPosition == listChip[i].nPlaybackPosition)
                    {
                        startIndex--;
                    }

                    startIndex++; // 1つ小さく過ぎているので、戻す
                }

                for (int j = startIndex; j <= i; j++)
                {
                    if (((listChip[j].nChannelNumber == EChannel.BarLine) ||
                         (listChip[j].nChannelNumber == EChannel.BeatLine)) &&
                        (listChip[j].nIntegerValue == (36 * 36 - 1)))
                    {
                        listChip[j].bVisible = bShowBeatBarLine;
                    }
                }
            }

            #endregion
            #region [ 発声時刻の計算 ]

            double bpm = 120.0;
            double dbBarLength = 1.0;
            int n発声位置 = 0;
            //int ms = 0;
            double currTimeMs = 0.0; // 2024.2.18 fisyher Fix Time Drift issue due to int truncation
            int nBar = 0;
            //
            STDGBVALUE<CChip> cCandidateStartHold = default;

            cCandidateStartHold.Drums = null;
            cCandidateStartHold.Guitar = null;
            cCandidateStartHold.Bass = null;
            foreach (CChip chip in listChip)
            {
                double currChipPlaybackTimeMs =
                    tComputeChipPlayTimeMs(currTimeMs, chip.nPlaybackPosition - n発声位置, dbBarLength, bpm);
                chip.nPlaybackTimeMs = tConvertFromDoubleToIntBasedOnComputeMode(currChipPlaybackTimeMs);

                if (((eFileType == EType.BMS) || (eFileType == EType.BME)) &&
                    ((dbBarLength != 1.0) && ((chip.nPlaybackPosition / 384) != nBar)))
                {
                    n発声位置 = chip.nPlaybackPosition;
                    currTimeMs = currChipPlaybackTimeMs;
                    //ms = chip.nPlaybackTimeMs;
                    dbBarLength = 1.0;
                }

                nBar = chip.nPlaybackPosition / 384;
                EChannel ch = chip.nChannelNumber;
                switch (ch)
                {
                    case EChannel.BarLength: // BarLength
                    {
                        n発声位置 = chip.nPlaybackPosition;
                        currTimeMs = currChipPlaybackTimeMs;
                        //ms = chip.nPlaybackTimeMs;
                        dbBarLength = chip.db実数値;
                        continue;
                    }
                    case EChannel.BPM: // BPM
                    {
                        n発声位置 = chip.nPlaybackPosition;
                        currTimeMs = currChipPlaybackTimeMs;
                        //ms = chip.nPlaybackTimeMs;
                        bpm = BASEBPM + chip.nIntegerValue;
                        continue;
                    }
                    case EChannel.BGALayer1: // BGA (レイヤBGA1)
                    case EChannel.BGALayer2: // レイヤBGA2
                    case EChannel.BGALayer3: // レイヤBGA3
                    case EChannel.BGALayer4: // レイヤBGA4
                    case EChannel.BGALayer5: // レイヤBGA5
                    case EChannel.BGALayer6: // レイヤBGA6
                    case EChannel.BGALayer7: // レイヤBGA7
                    case EChannel.BGALayer8: // レイヤBGA8
                        break;

                    case EChannel.ExObj_nouse: // Extended Object (非対応)
                    case EChannel.MissAnimation_nouse: // Missアニメ (非対応)
                    //case 0x5A:	// 未定義
                    case EChannel.nouse_5b: // 未定義
                    case EChannel.nouse_5c: // 未定義
                    case EChannel.nouse_5d: // 未定義
                    case EChannel.nouse_5e: // 未定義
                    case EChannel.nouse_5f: // 未定義
                    {
                        continue;
                    }
                    case EChannel.BPMEx: // 拡張BPM
                    {
                        n発声位置 = chip.nPlaybackPosition;
                        currTimeMs = currChipPlaybackTimeMs;
                        //ms = chip.nPlaybackTimeMs;
                        if (listBPM.ContainsKey(chip.nIntegerValue_InternalNumber))
                        {
                            bpm = ((listBPM[chip.nIntegerValue_InternalNumber].n表記上の番号 == 0) ? 0.0 : BASEBPM) +
                                  listBPM[chip.nIntegerValue_InternalNumber].dbBPM値;
                        }

                        continue;
                    }
                    case EChannel.Movie: // 動画再生
                    {
                        if (listAVIPAN.ContainsKey(chip.nIntegerValue))
                        {
                            //int num21 = ms + ( (int) ( ( ( 0x271 * ( chip.nPlaybackPosition - n発声位置 ) ) * dbBarLength ) / bpm ) );
                            //int num22 = ms + ( (int) ( ( ( 0x271 * ( ( chip.nPlaybackPosition + this.listAVIPAN[ chip.nIntegerValue ].n移動時間ct ) - n発声位置 ) ) * dbBarLength ) / bpm ) );
                            double num21 = tComputeChipPlayTimeMs(currTimeMs, chip.nPlaybackPosition - n発声位置,
                                dbBarLength, bpm);
                            double num22 = tComputeChipPlayTimeMs(currTimeMs,
                                (chip.nPlaybackPosition + listAVIPAN[chip.nIntegerValue].n移動時間ct) - n発声位置,
                                dbBarLength, bpm);
                            chip.n総移動時間 = (int)Math.Round(num22 - num21);
                        }

                        continue;
                    }
                    case EChannel.MovieFull: // 動画再生 (Full)
                    {
                        if (listAVIPAN.ContainsKey(chip.nIntegerValue))
                        {
                            //int num21 = ms + ((int)(((0x271 * (chip.nPlaybackPosition - n発声位置)) * dbBarLength) / bpm));
                            //int num22 = ms + ((int)(((0x271 * ((chip.nPlaybackPosition + this.listAVIPAN[chip.nIntegerValue].n移動時間ct) - n発声位置)) * dbBarLength) / bpm));
                            double num21 = tComputeChipPlayTimeMs(currTimeMs, chip.nPlaybackPosition - n発声位置,
                                dbBarLength, bpm);
                            double num22 = tComputeChipPlayTimeMs(currTimeMs,
                                (chip.nPlaybackPosition + listAVIPAN[chip.nIntegerValue].n移動時間ct) - n発声位置,
                                dbBarLength, bpm);
                            chip.n総移動時間 = (int)Math.Round(num22 - num21);
                        }

                        continue;
                    }
                    default:
                    {
                        switch (chip.nChannelNumber)
                        {
                            case >= EChannel.BonusEffect_Min and <= EChannel.BonusEffect:
                                #region [ TEST ]
                                t指定された発声位置と同じ位置の指定したチップにボーナスフラグを立てる(chip.nPlaybackPosition, chip.nIntegerValue);
                                #endregion
                                break;
                            
                            //Process Long Notes for Guitar and Bass
                            case EChannel.Guitar_LongNote:
                            case EChannel.Bass_LongNote:
                            {
                                #region [Long Note Processing]

                                EInstrumentPart eChipPart = (chip.nChannelNumber == EChannel.Guitar_LongNote)
                                    ? EInstrumentPart.GUITAR
                                    : EInstrumentPart.BASS;
                                //Check if this chip coincide with a KeyPress if currently no candidate start hold 
                                if (cCandidateStartHold[(int)eChipPart] == null)
                                {
                                    foreach (CChip chip2 in listChip.Where(chip2 => chip2.nPlaybackPosition == chip.nPlaybackPosition &&
                                                 eChipPart == chip2.eInstrumentPart && chip2 is { bChannelWithVisibleChip: true, bChipIsOpenNote: false }))
                                    {
                                        cCandidateStartHold[(int)eChipPart] = chip2;
                                        break;
                                    }
                                }
                                //Check for EndHold note rule violation
                                else
                                {
                                    if (listChip.Any(chip2 => chip2.eInstrumentPart == eChipPart && chip2.bChannelWithVisibleChip &&
                                                              chip2.nPlaybackPosition >
                                                              cCandidateStartHold[(int)eChipPart].nPlaybackPosition &&
                                                              chip2.nPlaybackPosition <= chip.nPlaybackPosition))
                                    {
                                        cCandidateStartHold[(int)eChipPart] = null;
                                    }

                                    //If candidate start hold survives
                                    if (cCandidateStartHold[(int)eChipPart] != null)
                                    {
                                        cCandidateStartHold[(int)eChipPart].chipロングノート終端 = chip;
                                        //Reset
                                        cCandidateStartHold[(int)eChipPart] = null;
                                    }
                                }

                                #endregion

                                break;
                            }
                        }

                        continue;
                    }
                }

                if (listBGAPAN.ContainsKey(chip.nIntegerValue))
                {
                    //int num19 = ms + ( (int) ( ( ( 0x271 * ( chip.nPlaybackPosition - n発声位置 ) ) * dbBarLength ) / bpm ) );
                    //int num20 = ms + ( (int) ( ( ( 0x271 * ( ( chip.nPlaybackPosition + this.listBGAPAN[ chip.nIntegerValue ].n移動時間ct ) - n発声位置 ) ) * dbBarLength ) / bpm ) );
                    double num19 = tComputeChipPlayTimeMs(currTimeMs, chip.nPlaybackPosition - n発声位置, dbBarLength,
                        bpm);
                    double num20 = tComputeChipPlayTimeMs(currTimeMs,
                        (chip.nPlaybackPosition + listBGAPAN[chip.nIntegerValue].n移動時間ct) - n発声位置, dbBarLength,
                        bpm);
                    chip.n総移動時間 = (int)Math.Round(num20 - num19);
                }
            }

            if (this.db再生速度 > 0.0)
            {
                foreach (CChip chip in listChip)
                {
                    //chip.nPlaybackTimeMs = (int) ( ( (double) chip.nPlaybackTimeMs ) / this.db再生速度 );
                    chip.nPlaybackTimeMs =
                        tConvertFromDoubleToIntBasedOnComputeMode(chip.nPlaybackTimeMs / this.db再生速度);
                }
            }

            #endregion
                
            this.nBGMAdjust = 0;
            t各自動再生音チップの再生時刻を変更する(nBGMAdjust);
            if (CDTXMania.ConfigIni.nCommonBGMAdjustMs != 0)
                t各自動再生音チップの再生時刻を変更する(CDTXMania.ConfigIni.nCommonBGMAdjustMs, false, true);

            #region [ 可視チップ数カウント ]

            for (int n = 0; n < 14; n++)
            {
                nVisibleChipsCount[n] = 0;
            }

            foreach (CChip chip in listChip)
            {
                EChannel c = chip.nChannelNumber;
                if ((EChannel.HiHatClose <= c) && (c <= EChannel.LeftBassDrum))
                {
                    nVisibleChipsCount[c - EChannel.HiHatClose]++;
                }

                if ((EChannel.Guitar_Open <= c) && (c <= EChannel.Guitar_RGBxx) ||
                    (EChannel.Guitar_xxxYx <= c) && (c <= EChannel.Guitar_RxxxP) ||
                    (EChannel.Guitar_RxBxP <= c) && (c <= EChannel.Guitar_xGBYP) ||
                    (EChannel.Guitar_RxxYP <= c) && (c <= EChannel.Guitar_RGBYP))
                {
                    nVisibleChipsCount.Guitar++;
                    nVisibleChipsCount.incrementChipCount(c);
                }

                if ((EChannel.Bass_Open <= c) && (c <= EChannel.Bass_RGBxx) ||
                    (EChannel.Bass_xxxYx <= c) && (c <= EChannel.Bass_xxBYx) ||
                    (EChannel.Bass_xGxYx <= c) && (c <= EChannel.Bass_xxBxP) ||
                    (EChannel.Bass_xGxxP <= c) && (c <= EChannel.Bass_RGBxP) ||
                    (EChannel.Bass_xxxYP <= c) && (c <= EChannel.Bass_RGBYP))
                {
                    nVisibleChipsCount.Bass++;
                    nVisibleChipsCount.incrementChipCount(c);
                }

                if ((c >= EChannel.BonusEffect_Min) && (c <= EChannel.BonusEffect))
                {
                    nボーナスチップ数++;
                }
            }

            #endregion
            #region [ チップの種類を分類し、対応するフラグを立てる ]

            foreach (CChip chip in listChip)
            {
                if ((chip.bWAVを使うチャンネルである && listWAV.ContainsKey(chip.nIntegerValue_InternalNumber)) &&
                    !listWAV[chip.nIntegerValue_InternalNumber].listこのWAVを使用するチャンネル番号の集合
                        .Contains(chip.nChannelNumber))
                {
                    listWAV[chip.nIntegerValue_InternalNumber].listこのWAVを使用するチャンネル番号の集合.Add(chip.nChannelNumber);

                    int c = (int)chip.nChannelNumber >> 4;
                    switch (c)
                    {
                        case 0x01:
                            listWAV[chip.nIntegerValue_InternalNumber].bIsDrumsSound = true; break;
                        case 0x02:
                            listWAV[chip.nIntegerValue_InternalNumber].bIsGuitarSound = true; break;
                        case 0x0A:
                            listWAV[chip.nIntegerValue_InternalNumber].bIsBassSound = true; break;
                        case 0x06:
                        case 0x07:
                        case 0x08:
                        case 0x09:
                            listWAV[chip.nIntegerValue_InternalNumber].bIsSESound = true; break;
                        case 0x00:
                            if (chip.nChannelNumber == EChannel.BGM)
                            {
                                listWAV[chip.nIntegerValue_InternalNumber].bIsBGMSound = true;
                                break;
                            }

                            break;
                    }
                }
            }

            #endregion
            #region [ hash値計算 ]

            byte[] buffer = null;
            try
            {
                FileStream stream = new(strFileNameFullPath, FileMode.Open, FileAccess.Read);
                buffer = new byte[stream.Length];
                stream.Read(buffer, 0, (int)stream.Length);
                stream.Close();
            }
            catch (Exception exception)
            {
                Trace.TraceError(exception.Message);
                Trace.TraceError("DTXのハッシュの計算に失敗しました。({0})", strFileNameFullPath);
            }

            if (buffer != null)
            {
                byte[] buffer2 = MD5.HashData(buffer);
                StringBuilder sb = new();
                foreach (byte b in buffer2)
                {
                    sb.Append(b.ToString("x2"));
                }

                strDTXFileHash = sb.ToString();
            }
            else
            {
                strDTXFileHash = "00000000000000000000000000000000";
            }

            #endregion
            #region [ bLogDTX詳細ログ出力 ]

            if (CDTXMania.ConfigIni.bLogDTX詳細ログ出力)
            {
                foreach (CWAV cwav in listWAV.Values)
                {
                    Trace.TraceInformation(cwav.ToString());
                }

                foreach (CAVI cavi in listAVI.Values)
                {
                    Trace.TraceInformation(cavi.ToString());
                }

                //foreach ( CDirectShow cds in this.listDS.Values)
                //{
                //    Trace.TraceInformation( cds.ToString());
                //}
                foreach (CAVIPAN cavipan in listAVIPAN.Values)
                {
                    Trace.TraceInformation(cavipan.ToString());
                }

                foreach (CBGA cbga in listBGA.Values)
                {
                    Trace.TraceInformation(cbga.ToString());
                }

                foreach (CBGAPAN cbgapan in listBGAPAN.Values)
                {
                    Trace.TraceInformation(cbgapan.ToString());
                }

                foreach (CBMP cbmp in listBMP.Values)
                {
                    Trace.TraceInformation(cbmp.ToString());
                }

                foreach (CBMPTEX cbmptex in listBMPTEX.Values)
                {
                    Trace.TraceInformation(cbmptex.ToString());
                }

                foreach (CBPM cbpm3 in listBPM.Values)
                {
                    Trace.TraceInformation(cbpm3.ToString());
                }

                foreach (CChip chip in listChip)
                {
                    Trace.TraceInformation(chip.ToString());
                }
            }

            #endregion
        }
    }

    /// <summary>
    /// サウンドミキサーにサウンドを登録_削除する時刻を事前に算出する
    /// </summary>
    public void PlanToAddMixerChannel()
    {
        if (CDTXMania.SoundManager.GetCurrentSoundDeviceType() == "DirectSound") // DShowでの再生の場合はミキシング負荷が高くないため、
        {
            // チップのライフタイム管理を行わない
            return;
        }

        List<CChip> listAddMixerChannel = new(128);
        ;
        List<CChip> listRemoveMixerChannel = new(128);
        List<CChip> listRemoveTiming = new(128);

        foreach (CChip pChip in listChip)
        {
            switch (pChip.nChannelNumber)
            {
                // BGM, 演奏チャネル, 不可視サウンド, フィルインサウンド, 空打ち音はミキサー管理の対象
                // BGM:
                case EChannel.BGM:
                // Dr playing channels
                case EChannel.HiHatClose:
                case EChannel.Snare:
                case EChannel.BassDrum:
                case EChannel.HighTom:
                case EChannel.LowTom:
                case EChannel.Cymbal:
                case EChannel.FloorTom:
                case EChannel.HiHatOpen:
                case EChannel.RideCymbal:
                case EChannel.LeftCymbal:
                case EChannel.LeftPedal:
                case EChannel.LeftBassDrum:
                // Gt playing channels
                case EChannel.Guitar_Open:
                case EChannel.Guitar_xxBxx:
                case EChannel.Guitar_xGxxx:
                case EChannel.Guitar_xGBxx:
                case EChannel.Guitar_Rxxxx:
                case EChannel.Guitar_RxBxx:
                case EChannel.Guitar_RGxxx:
                case EChannel.Guitar_RGBxx:
                case EChannel.Guitar_Wailing:
                case EChannel.Guitar_xxxYx:
                case EChannel.Guitar_xxBYx:
                case EChannel.Guitar_xGxYx:
                case EChannel.Guitar_xGBYx:
                case EChannel.Guitar_RxxYx:
                case EChannel.Guitar_RxBYx:
                case EChannel.Guitar_RGxYx:
                case EChannel.Guitar_RGBYx:
                case EChannel.Guitar_xxxxP:
                case EChannel.Guitar_xxBxP:
                case EChannel.Guitar_xGxxP:
                case EChannel.Guitar_xGBxP:
                case EChannel.Guitar_RxxxP:
                case EChannel.Guitar_RxBxP:
                case EChannel.Guitar_RGxxP:
                case EChannel.Guitar_RGBxP:
                case EChannel.Guitar_xxxYP:
                case EChannel.Guitar_xxBYP:
                case EChannel.Guitar_xGxYP:
                case EChannel.Guitar_xGBYP:
                case EChannel.Guitar_RxxYP:
                case EChannel.Guitar_RxBYP:
                case EChannel.Guitar_RGxYP:
                case EChannel.Guitar_RGBYP:
                // Bs playing channels
                case EChannel.Bass_Open:
                case EChannel.Bass_xxBxx:
                case EChannel.Bass_xGxxx:
                case EChannel.Bass_xGBxx:
                case EChannel.Bass_Rxxxx:
                case EChannel.Bass_RxBxx:
                case EChannel.Bass_RGxxx:
                case EChannel.Bass_RGBxx:
                case EChannel.Bass_Wailing:
                case EChannel.Bass_xxxYx:
                case EChannel.Bass_xxBYx:
                case EChannel.Bass_xGxYx:
                case EChannel.Bass_xGBYx:
                case EChannel.Bass_RxxYx:
                case EChannel.Bass_RxBYx:
                case EChannel.Bass_RGxYx:
                case EChannel.Bass_RGBYx:
                case EChannel.Bass_xxxxP:
                case EChannel.Bass_xxBxP:
                case EChannel.Bass_xGxxP:
                case EChannel.Bass_xGBxP:
                case EChannel.Bass_RxxxP:
                case EChannel.Bass_RxBxP:
                case EChannel.Bass_RGxxP:
                case EChannel.Bass_RGBxP:
                case EChannel.Bass_xxxYP:
                case EChannel.Bass_xxBYP:
                case EChannel.Bass_xGxYP:
                case EChannel.Bass_xGBYP:
                case EChannel.Bass_RxxYP:
                case EChannel.Bass_RxBYP:
                case EChannel.Bass_RGxYP:
                case EChannel.Bass_RGBYP:
                // Dr invisible chips
                case EChannel.HiHatClose_Hidden:
                case EChannel.Snare_Hidden:
                case EChannel.BassDrum_Hidden:
                case EChannel.HighTom_Hidden:
                case EChannel.LowTom_Hidden:
                case EChannel.Cymbal_Hidden:
                case EChannel.FloorTom_Hidden:
                case EChannel.HiHatOpen_Hidden:
                case EChannel.RideCymbal_Hidden:
                case EChannel.LeftCymbal_Hidden:
                case EChannel.LeftPedal_Hidden:
                case EChannel.LeftBassDrum_Hidden:
                // Dr/Gt/Bs blank hits
                case EChannel.HiHatClose_NoChip:
                case EChannel.Snare_NoChip:
                case EChannel.BassDrum_NoChip:
                case EChannel.HighTom_NoChip:
                case EChannel.LowTom_NoChip:
                case EChannel.Cymbal_NoChip:
                case EChannel.FloorTom_NoChip:
                case EChannel.HiHatOpen_NoChip:
                case EChannel.RideCymbal_NoChip:
                case EChannel.Guitar_NoChip:
                case EChannel.Bass_NoChip:
                case EChannel.LeftCymbal_NoChip:
                case EChannel.LeftPedal_NoChip:
                case EChannel.LeftBassDrum_NoChip:
                // Fill-in sounds
                case EChannel.DrumsFillin:
                case EChannel.Guitar_WailingSound: //case 0xAF:
                // Auto-playing chips
                case EChannel.SE01:
                case EChannel.SE02:
                case EChannel.SE03:
                case EChannel.SE04:
                case EChannel.SE05:
                case EChannel.SE06:
                case EChannel.SE07:
                case EChannel.SE08:
                case EChannel.SE09:
                case EChannel.SE10:
                case EChannel.SE11:
                case EChannel.SE12:
                case EChannel.SE13:
                case EChannel.SE14:
                case EChannel.SE15:
                case EChannel.SE16:
                case EChannel.SE17:
                case EChannel.SE18:
                case EChannel.SE19:
                case EChannel.SE20:
                case EChannel.SE21:
                case EChannel.SE22:
                case EChannel.SE23:
                case EChannel.SE24:
                case EChannel.SE25:
                case EChannel.SE26:
                case EChannel.SE27:
                case EChannel.SE28:
                case EChannel.SE29:
                case EChannel.SE30:
                case EChannel.SE31:
                case EChannel.SE32:

                    #region [ 発音1秒前のタイミングを記録 ]

                    int n発音前余裕ms = 1000, n発音後余裕ms = 800;
                {
                    int ch = (int)pChip.nChannelNumber >> 4;
                    if (ch == 0x02 || ch == 0x0A)
                    {
                        n発音前余裕ms = 500;
                    }

                    if (ch == 0x06 || ch == 0x07 || ch == 0x08 || ch == 0x09)
                    {
                        n発音前余裕ms = 500;
                    }
                }
                    if (pChip.nChannelNumber == EChannel.BGM) // BGMチップは即ミキサーに追加
                    {
                        if (listWAV.ContainsKey(pChip.nIntegerValue_InternalNumber))
                        {
                            CWAV wc = CDTXMania.DTX.listWAV[pChip.nIntegerValue_InternalNumber];
                            if (wc.rSound[0] != null)
                            {
                                CDTXMania.SoundManager
                                    .AddMixer(wc.rSound[0]); // BGMは多重再生しない仕様としているので、1個目だけミキサーに登録すればよい
                            }
                        }
                    }

                    int nAddMixer時刻ms, nAddMixer位置 = 0;
//Debug.WriteLine("==================================================================");
//Debug.WriteLine( "Start: ch=" + pChip.nChannelNumber.ToString("x2") + ", nWAV番号=" + pChip.nIntegerValue + ", time=" + pChip.nPlaybackTimeMs + ", lasttime=" + listChip[ listChip.Count - 1 ].nPlaybackTimeMs );
                    t発声時刻msと発声位置を取得する(pChip.nPlaybackTimeMs - n発音前余裕ms, out nAddMixer時刻ms, out nAddMixer位置);
//Debug.WriteLine( "nAddMixer時刻ms=" + nAddMixer時刻ms + ",nAddMixer位置=" + nAddMixer位置 );

                    CChip c_AddMixer = new()
                    {
                        nChannelNumber = EChannel.MixChannel1_unc,
                        nIntegerValue = pChip.nIntegerValue,
                        nIntegerValue_InternalNumber = pChip.nIntegerValue_InternalNumber,
                        nPlaybackTimeMs = nAddMixer時刻ms,
                        nPlaybackPosition = nAddMixer位置,
                        bChipKeepsPlayingAfterPerfEnds = false
                    };
                    listAddMixerChannel.Add(c_AddMixer);
//Debug.WriteLine("listAddMixerChannel:" );
//DebugOut_CChipList( listAddMixerChannel );

                    #endregion

                    int duration = 0;
                    if (listWAV.ContainsKey(pChip.nIntegerValue_InternalNumber))
                    {
                        CWAV wc = CDTXMania.DTX.listWAV[pChip.nIntegerValue_InternalNumber];
                        duration = (wc.rSound[0] == null)
                            ? 0
                            : (int)(wc.rSound[0].nTotalPlayTimeMs /
                                    db再生速度); // #23664 durationに再生速度が加味されておらず、低速再生でBGMが途切れる問題を修正 (発声時刻msは、DTX読み込み時に再生速度加味済)
                    }

//Debug.WriteLine("duration=" + duration );
                    t発声時刻msと発声位置を取得する(pChip.nPlaybackTimeMs + duration + n発音後余裕ms, out int n新RemoveMixer時刻ms,
                        out int n新RemoveMixer位置);
//Debug.WriteLine( "n新RemoveMixer時刻ms=" + n新RemoveMixer時刻ms + ",n新RemoveMixer位置=" + n新RemoveMixer位置 );
                    if (n新RemoveMixer時刻ms < pChip.nPlaybackTimeMs + duration) // 曲の最後でサウンドが切れるような場合は
                    {
                        CChip c_AddMixer_noremove = c_AddMixer;
                        c_AddMixer_noremove.bChipKeepsPlayingAfterPerfEnds = true;
                        listAddMixerChannel[listAddMixerChannel.Count - 1] = c_AddMixer_noremove;
                        //continue;                 // 発声位置の計算ができないので、Mixer削除をあきらめる___のではなく
                        // #32248 2013.10.15 yyagi 演奏終了後も再生を続けるチップであるというフラグをpChip内に立てる
                        break;
                    }

                    #region [ 未使用コード ]

                    //if ( n新RemoveMixer時刻ms < pChip.nPlaybackTimeMs + duration )	// 曲の最後でサウンドが切れるような場合
                    //{
                    //    n新RemoveMixer時刻ms = pChip.nPlaybackTimeMs + duration;
                    //    // 「位置」は比例計算で求めてお茶を濁す...このやり方だと誤動作したため対応中止
                    //    n新RemoveMixer位置 = listChip[ listChip.Count - 1 ].nPlaybackPosition * n新RemoveMixer時刻ms / listChip[ listChip.Count - 1 ].nPlaybackTimeMs;
                    //}

                    #endregion

                    #region [ 発音終了2秒後にmixerから削除するが、その前に再発音することになるのかを確認(再発音ならmixer削除タイミングを延期) ]

                    int n整数値 = pChip.nIntegerValue;
                    int index = listRemoveTiming.FindIndex(
                        delegate(CChip cchip) { return cchip.nIntegerValue == n整数値; }
                    );
//Debug.WriteLine( "index=" + index );
                    if (index >= 0) // 過去に同じチップで発音中のものが見つかった場合
                    {
                        // 過去の発音のmixer削除を確定させるか、延期するかの2択。
                        int n旧RemoveMixer時刻ms = listRemoveTiming[index].nPlaybackTimeMs;

                        //Debug.WriteLine( "n旧RemoveMixer時刻ms=" + n旧RemoveMixer時刻ms + ",n旧RemoveMixer位置=" + n旧RemoveMixer位置 );
                        if (pChip.nPlaybackTimeMs - n発音前余裕ms <= n旧RemoveMixer時刻ms) // mixer削除前に、同じ音の再発音がある場合は、
                        {
                            // mixer削除時刻を遅延させる(if-else後に行う)
//Debug.WriteLine( "remove TAIL of listAddMixerChannel. TAIL INDEX=" + listAddMixerChannel.Count );
//DebugOut_CChipList( listAddMixerChannel );
                            listAddMixerChannel.RemoveAt(listAddMixerChannel.Count -
                                                         1); // また、同じチップ音の「mixerへの再追加」は削除する
//Debug.WriteLine( "removed result:" );
//DebugOut_CChipList( listAddMixerChannel );
                        }
                        else // 逆に、時間軸上、mixer削除後に再発音するような流れの場合は
                        {
//Debug.WriteLine( "Publish the value(listRemoveTiming[index] to listRemoveMixerChannel." );
                            listRemoveMixerChannel.Add(listRemoveTiming[index]); // mixer削除を確定させる
//Debug.WriteLine( "listRemoveMixerChannel:" );
//DebugOut_CChipList( listRemoveMixerChannel );
                            //listRemoveTiming.RemoveAt( index );
                        }

                        CChip c = new() // mixer削除時刻を更新(遅延)する
                        {
                            nChannelNumber = EChannel.MixChannel2_unc,
                            nIntegerValue = listRemoveTiming[index].nIntegerValue,
                            nIntegerValue_InternalNumber = listRemoveTiming[index].nIntegerValue_InternalNumber,
                            nPlaybackTimeMs = n新RemoveMixer時刻ms,
                            nPlaybackPosition = n新RemoveMixer位置
                        };
                        listRemoveTiming[index] = c;
                        //listRemoveTiming[ index ].nPlaybackTimeMs = n新RemoveMixer時刻ms;	// mixer削除時刻を更新(遅延)する
                        //listRemoveTiming[ index ].nPlaybackPosition = n新RemoveMixer位置;
//Debug.WriteLine( "listRemoveTiming: modified" );
//DebugOut_CChipList( listRemoveTiming );
                    }
                    else // 過去に同じチップを発音していないor
                    {
                        // 発音していたが既にmixer削除確定していたなら
                        CChip c = new() // 新しくmixer削除候補として追加する
                        {
                            nChannelNumber = EChannel.MixChannel2_unc,
                            nIntegerValue = pChip.nIntegerValue,
                            nIntegerValue_InternalNumber = pChip.nIntegerValue_InternalNumber,
                            nPlaybackTimeMs = n新RemoveMixer時刻ms,
                            nPlaybackPosition = n新RemoveMixer位置
                        };
//Debug.WriteLine( "Add new chip to listRemoveMixerTiming: " );
//Debug.WriteLine( "ch=" + c.nChannelNumber.ToString( "x2" ) + ", nWAV番号=" + c.nIntegerValue + ", time=" + c.nPlaybackTimeMs + ", lasttime=" + listChip[ listChip.Count - 1 ].nPlaybackTimeMs );
                        listRemoveTiming.Add(c);
//Debug.WriteLine( "listRemoveTiming:" );
//DebugOut_CChipList( listRemoveTiming );
                    }

                    #endregion

                    break;
            }
        }
//Debug.WriteLine("==================================================================");
//Debug.WriteLine( "Result:" );
//Debug.WriteLine( "listAddMixerChannel:" );
//DebugOut_CChipList( listAddMixerChannel );
//Debug.WriteLine( "listRemoveMixerChannel:" );
//DebugOut_CChipList( listRemoveMixerChannel );
//Debug.WriteLine( "listRemoveTiming:" );
//DebugOut_CChipList( listRemoveTiming );
//Debug.WriteLine( "==================================================================" );

        listChip.AddRange(listAddMixerChannel);
        listChip.AddRange(listRemoveMixerChannel);
        listChip.AddRange(listRemoveTiming);
        listChip.Sort();
    }

    private void DebugOut_CChipList(List<CChip> c)
    {
//Debug.WriteLine( "Count=" + c.Count );
        for (int i = 0; i < c.Count; i++)
        {
            Debug.WriteLine(i + ": ch=" + c[i].nChannelNumber.ToString("x2") + ", WAV番号=" + c[i].nIntegerValue +
                            ", time=" + c[i].nPlaybackTimeMs);
        }
    }

    private bool t発声時刻msと発声位置を取得する(int n希望発声時刻ms, out int n新発声時刻ms, out int n新発声位置)
    {
        // 発声時刻msから発声位置を逆算することはできないため、近似計算する。
        // 具体的には、希望発声位置前後の2つのチップの発声位置の中間を取る。

        if (n希望発声時刻ms < 0)
        {
            n希望発声時刻ms = 0;
        }
        //else if ( n希望発声時刻ms > listChip[ listChip.Count - 1 ].nPlaybackTimeMs )		// BGMの最後の余韻を殺してしまうので、この条件は外す
        //{
        //    n希望発声時刻ms = listChip[ listChip.Count - 1 ].nPlaybackTimeMs;
        //}

        int index_min = -1, index_max = -1;
        for (int i = 0; i < listChip.Count; i++) // 希望発声位置前後の「前」の方のチップを検索
        {
            if (listChip[i].nPlaybackTimeMs >= n希望発声時刻ms)
            {
                index_min = i;
                break;
            }
        }

        if (index_min < 0) // 希望発声時刻に至らずに曲が終了してしまう場合
        {
            // listの最終項目の時刻をそのまま使用する
            //___のではダメ。BGMが尻切れになる。
            // そこで、listの最終項目の発声時刻msと発生位置から、希望発声時刻に相当する希望発声位置を比例計算して求める。
            //n新発声時刻ms = n希望発声時刻ms;
            //n新発声位置 = listChip[ listChip.Count - 1 ].nPlaybackPosition * n希望発声時刻ms / listChip[ listChip.Count - 1 ].nPlaybackTimeMs;
            n新発声時刻ms = listChip[listChip.Count - 1].nPlaybackTimeMs;
            n新発声位置 = listChip[listChip.Count - 1].nPlaybackPosition;
            return false;
        }

        index_max = index_min + 1;
        if (index_max >= listChip.Count)
        {
            index_max = index_min;
        }

        n新発声時刻ms = (listChip[index_max].nPlaybackTimeMs + listChip[index_min].nPlaybackTimeMs) / 2;
        n新発声位置 = (listChip[index_max].nPlaybackPosition + listChip[index_min].nPlaybackPosition) / 2;

        return true;
    }

    /// <summary>
    /// Swap infos between Guitar and Bass (notes, level, nVisibleChipsCount, bチップがある)
    /// </summary>
    public void SwapGuitarBassInfos() // #24063 2011.1.24 yyagi ギターとベースの譜面情報入替
    {
        for (int i = listChip.Count - 1; i >= 0; i--)
        {
            if (listChip[i].eInstrumentPart == EInstrumentPart.BASS)
            {
                listChip[i].eInstrumentPart = EInstrumentPart.GUITAR;
                if (listChip[i].nChannelNumber >= EChannel.Bass_Open &&
                    listChip[i].nChannelNumber <= EChannel.Bass_Wailing)
                    listChip[i].nChannelNumber -= (EChannel.Bass_Open - EChannel.Guitar_Open);
                else if (listChip[i].nChannelNumber == EChannel.Bass_xxxYx ||
                         listChip[i].nChannelNumber == EChannel.Bass_xxBYx)
                    listChip[i].nChannelNumber -= 0x32;
                else if (listChip[i].nChannelNumber >= EChannel.Bass_xGxYx &&
                         listChip[i].nChannelNumber <= EChannel.Bass_xxBxP)
                    listChip[i].nChannelNumber -= 0x33;
                else if (listChip[i].nChannelNumber >= EChannel.Bass_xGxxP &&
                         listChip[i].nChannelNumber <= EChannel.Bass_RxxxP)
                    listChip[i].nChannelNumber -= 0x3D;
                else if (listChip[i].nChannelNumber >= EChannel.Bass_RxBxP &&
                         listChip[i].nChannelNumber <= EChannel.Bass_RGBxP)
                    listChip[i].nChannelNumber -= 0x34;
                else if (listChip[i].nChannelNumber >= EChannel.Bass_xxxYP &&
                         listChip[i].nChannelNumber <= EChannel.Bass_RGBYP)
                    listChip[i].nChannelNumber -= 0x35;
            }
            else if (listChip[i].eInstrumentPart == EInstrumentPart.GUITAR)
            {
                listChip[i].eInstrumentPart = EInstrumentPart.BASS;

                if (listChip[i].nChannelNumber >= EChannel.Guitar_Open &&
                    listChip[i].nChannelNumber <= EChannel.Guitar_Wailing)
                    listChip[i].nChannelNumber += (EChannel.Bass_Open - EChannel.Guitar_Open);
                else if (listChip[i].nChannelNumber == EChannel.Guitar_xxxYx ||
                         listChip[i].nChannelNumber == EChannel.Guitar_xxBYx)
                    listChip[i].nChannelNumber += 0x32;
                else if (listChip[i].nChannelNumber >= EChannel.Guitar_xGxYx &&
                         listChip[i].nChannelNumber <= EChannel.Guitar_xxBxP)
                    listChip[i].nChannelNumber += 0x33;
                else if (listChip[i].nChannelNumber >= EChannel.Guitar_xGxxP &&
                         listChip[i].nChannelNumber <= EChannel.Guitar_RxxxP)
                    listChip[i].nChannelNumber += 0x3D;
                else if (listChip[i].nChannelNumber >= EChannel.Guitar_RxBxP &&
                         listChip[i].nChannelNumber <= EChannel.Guitar_RGBxP)
                    listChip[i].nChannelNumber += 0x34;
                else if (listChip[i].nChannelNumber >= EChannel.Guitar_xxxYP &&
                         listChip[i].nChannelNumber <= EChannel.Guitar_RGBYP)
                    listChip[i].nChannelNumber += 0x35;
            }
            //Long Notes
            else if (listChip[i].nChannelNumber == EChannel.Guitar_LongNote)
            {
                listChip[i].nChannelNumber = EChannel.Bass_LongNote;
            }
            else if (listChip[i].nChannelNumber == EChannel.Bass_LongNote)
            {
                listChip[i].nChannelNumber = EChannel.Guitar_LongNote;
            }
            //Wailing
            else if
                (listChip[i].nChannelNumber ==
                 EChannel.Guitar_Wailing) // #25215 2011.5.21 yyagi wailingはE楽器パート.UNKNOWNが割り当てられているので個別に対応
            {
                listChip[i].nChannelNumber += (EChannel.Bass_Open - EChannel.Guitar_Open);
            }
            else if
                (listChip[i].nChannelNumber ==
                 EChannel.Bass_Wailing) // #25215 2011.5.21 yyagi wailingはE楽器パート.UNKNOWNが割り当てられているので個別に対応
            {
                listChip[i].nChannelNumber -= (EChannel.Bass_Open - EChannel.Guitar_Open);
            }
        }

        int t = LEVEL.Bass;
        LEVEL.Bass = LEVEL.Guitar;
        LEVEL.Guitar = t;

        t = LEVELDEC.Bass;
        LEVELDEC.Bass = LEVELDEC.Guitar;
        LEVELDEC.Guitar = t;

        t = nVisibleChipsCount.Bass;
        nVisibleChipsCount.Bass = nVisibleChipsCount.Guitar;
        nVisibleChipsCount.Guitar = t;

        //Swap Guitar Bass Lane chip info
        nVisibleChipsCount.swapGuitarBassLaneChipCounters();

        bool ts = bHasChips.Bass;
        bHasChips.Bass = bHasChips.Guitar;
        bHasChips.Guitar = ts;

//			SwapGuitarBassInfos_AutoFlags();
    }

    // CActivity 実装

    public override void OnActivate()
    {
        listWAV = new Dictionary<int, CWAV>();
        listBMP = new Dictionary<int, CBMP>();
        listBMPTEX = new Dictionary<int, CBMPTEX>();
        listBPM = new Dictionary<int, CBPM>();
        listBGAPAN = new Dictionary<int, CBGAPAN>();
        listBGA = new Dictionary<int, CBGA>();
        listAVIPAN = new Dictionary<int, CAVIPAN>();
        listAVI = new Dictionary<int, CAVI>();
        //this.listDS = new Dictionary<int, CDirectShow>();
        listChip = new List<CChip>();
        base.OnActivate();
    }

    public override void OnDeactivate()
    {
        if (listWAV != null)
        {
            foreach (CWAV cwav in listWAV.Values)
            {
                cwav.Dispose();
            }

            listWAV = null;
        }

        if (listBMP != null)
        {
            foreach (CBMP cbmp in listBMP.Values)
            {
                cbmp.Dispose();
            }

            listBMP = null;
        }

        if (listBMPTEX != null)
        {
            foreach (CBMPTEX cbmptex in listBMPTEX.Values)
            {
                cbmptex.Dispose();
            }

            listBMPTEX = null;
        }

        if (listAVI != null)
        {
            foreach (CAVI cavi in listAVI.Values)
            {
                cavi.Dispose();
            }

            listAVI = null;
        }

        //if (this.listDS != null)
        //{
        //    foreach (CDirectShow cds in this.listDS.Values)
        //    {
        //        cds.Dispose();
        //    }
        //    this.listDS= null;
        //}
        if (listBPM != null)
        {
            listBPM.Clear();
            listBPM = null;
        }

        if (listBGAPAN != null)
        {
            listBGAPAN.Clear();
            listBGAPAN = null;
        }

        if (listBGA != null)
        {
            listBGA.Clear();
            listBGA = null;
        }

        if (listAVIPAN != null)
        {
            listAVIPAN.Clear();
            listAVIPAN = null;
        }

        if (listChip != null)
        {
            listChip.Clear();
        }

        base.OnDeactivate();
    }

    public override void OnManagedCreateResources()
    {
        if (bActivated)
        {
            tLoadBMP_BMPTEX();
            tLoadAVI();
            base.OnManagedCreateResources();
        }
    }

    public override void OnManagedReleaseResources()
    {
        if (bActivated)
        {
            if (listBMP != null)
            {
                foreach (CBMP cbmp in listBMP.Values)
                {
                    cbmp.Dispose();
                }
            }

            if (listBMPTEX != null)
            {
                foreach (CBMPTEX cbmptex in listBMPTEX.Values)
                {
                    cbmptex.Dispose();
                }
            }

            if (listAVI != null)
            {
                foreach (CAVI cavi in listAVI.Values)
                {
                    cavi.Dispose();
                }
            }

            //if (this.listDS != null)
            //{
            //    foreach (CDirectShow cds in this.listDS.Values)
            //    {
            //        cds.Dispose();
            //    }
            //}
            base.OnManagedReleaseResources();
        }
    }


    // Other

    #region [ private ]

    //-----------------
    /// <summary>
    /// <para>GDAチャンネル番号に対応するDTXチャンネル番号。</para>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct STGDAPARAM
    {
        public string strGDAのチャンネル文字列;
        public EChannel nDTXのチャンネル番号;

        public STGDAPARAM(string strGDAのチャンネル文字列, EChannel nDTXのチャンネル番号) // 2011.1.1 yyagi 構造体のコンストラクタ追加(初期化簡易化のため)
        {
            this.strGDAのチャンネル文字列 = strGDAのチャンネル文字列;
            this.nDTXのチャンネル番号 = nDTXのチャンネル番号;
        }
    }

    private readonly STGDAPARAM[] stGDAParam;
    private bool bHeaderOnly;
    private bool b動画読み込み;
    private Stack<bool> bstackIFからENDIFをスキップする;

    private int lineNumber;
    private int nCurrentRandomNumber;

    private int nPolyphonicSounds = 4; // #28228 2012.5.1 yyagi

    private int n内部番号BPM1to;
    private int n内部番号WAV1to;
    private int[] n無限管理BPM;
    private int[] n無限管理PAN;
    private int[] n無限管理SIZE;
    private int[] n無限管理VOL;
    private int[] n無限管理WAV;
    private int[] nRESULTIMAGE用優先順位;
    private int[] nRESULTMOVIE用優先順位;
    private int[] nRESULTSOUND用優先順位;

    private static bool tSkipWhitespace(ref CharEnumerator ce)
    {
        while (char.IsWhiteSpace(ce.Current))
        {
            if (!ce.MoveNext()) return false;
        }

        return true;
    }

    private static bool ExtractCommand(ref CharEnumerator ce, ref StringBuilder output)
    {
        if (!tSkipWhitespace(ref ce)) return false;

        #region [ Treat the characters until the command terminator (':'), space, comment start (';'), or newline appears as the command string, and copy it to the sb string. ]

        while (ce.Current is not (':' or ' ' or ';' or '\n'))
        {
            output.Append(ce.Current);
            if (!ce.MoveNext()) return false;
        }

        #endregion

        if (ce.Current != ':') return true;

        return ce.MoveNext() && tSkipWhitespace(ref ce);
    }

    private static bool ExtractComment(ref CharEnumerator ce, ref StringBuilder output)
    {
        //If the current character is not the comment start character (';'), return normally
        if (ce.Current != ';') return true;
            
        //If the character string ends at the character after ';', exit
        if( !ce.MoveNext()) return false;

        //Treat the characters from the character after ';' to the one before '\n' as the comment string, and copy it to the sb string.
        while( ce.Current != '\n' )
        {
            output.Append( ce.Current );

            if(!ce.MoveNext())
                return false;
        }
        return true;
    }

    private static void t入力_パラメータ食い込みチェック(string strCommandName, ref string strCommand, ref string strParameter)
    {
        if ((strCommand.Length > strCommandName.Length) &&
            strCommand.StartsWith(strCommandName, StringComparison.OrdinalIgnoreCase))
        {
            strParameter = strCommand.Substring(strCommandName.Length).Trim();
            strCommand = strCommand.Substring(0, strCommandName.Length);
        }
    }

    private static bool ExtractParameter(ref CharEnumerator ce, ref StringBuilder output)
    {
        if (!tSkipWhitespace(ref ce)) return false;

        //Treat the characters until a newline or the comment start (';') appears as the parameter string, and copy it to the sb string.
        while (ce.Current != '\n' && ce.Current != ';')
        {
            output.Append(ce.Current);
            if (!ce.MoveNext()) return false;
        }
            
        return true;
    }

    private bool tSkipWhiteSpaceAndNewLines(ref CharEnumerator ce)
    {
        while (ce.Current is ' ' or '\n')
        {
            if (ce.Current == '\n')
                lineNumber++; // 改行文字では行番号が増える。

            if (!ce.MoveNext())
                return false; // 文字が尽きた
        }

        return true;
    }

    private static bool IsChipLocation(ReadOnlySpan<char> strCommand)
    {
        //check measure
        char c1 = strCommand[0], c2 = strCommand[1], c3 = strCommand[2];

        if (c1 is < '0' or > '9') return false;
        if (c2 is < '0' or > '9') return false;
        if (c3 is (< '0' or > '9') and (< 'A' or > 'Z')) return false;

        //get channel characters
        char ch1 = strCommand[3], ch2 = strCommand[4];

        return IsHexDigit(ch1) && IsHexDigit(ch2);
    }

    private static bool IsHexDigit(char c) =>
        (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F');

    bool lastLineWasChipLocation = false;

    private static readonly Dictionary<string, Action<CDTX, string>> commandHandlers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "PATH_WAV", (dtx, param) => dtx.PATH_WAV = param },
        { "PATH", (dtx, param) => dtx.PATH = (param != "PATH_WAV") ? param : "" },
        { "TITLE", (dtx, param) => dtx.TITLE = param },
        { "ARTIST", (dtx, param) => dtx.ARTIST = param },
        { "COMMENT", (dtx, param) => dtx.COMMENT = param },
        { "DLEVEL", (dtx, param) => ProcessLevel(dtx, "DLEVEL", param)},
        { "PLAYLEVEL", (dtx, param) => ProcessLevel(dtx, "PLAYLEVEL", param)},
        { "GLEVEL", (dtx, param) => ProcessLevel(dtx, "GLEVEL", param)},
        { "BLEVEL", (dtx, param) => ProcessLevel(dtx, "BLEVEL", param)},
        { "DLVDEC", (dtx, param) => ProcessLevel(dtx, "DLVDEC", param)},
        { "GLVDEC", (dtx, param) => ProcessLevel(dtx, "GLVDEC", param)},
        { "BLVDEC", (dtx, param) => ProcessLevel(dtx, "BLVDEC", param)},
        { "GENRE", (dtx, param) => dtx.GENRE = param },
        { "HIDDENLEVEL", (dtx, param) => dtx.HIDDENLEVEL = param.ToLower().Equals("on") },
        { "STAGEFILE", (dtx, param) => dtx.STAGEFILE = param },
        { "PREVIEW", (dtx, param) => dtx.PREVIEW = param },
        { "PREIMAGE", (dtx, param) => dtx.PREIMAGE = param },
        { "PREMOVIE", (dtx, param) => dtx.PREMOVIE = param },
        { "BACKGROUND_GR", (dtx, param) => dtx.BACKGROUND_GR = param },
        { "BACKGROUND", (dtx, param) => dtx.BACKGROUND = param },
        { "WALL", (dtx, param) => dtx.BACKGROUND = param },
        { "RANDOM", (dtx, param) =>
            {
                if (!int.TryParse(param, out int nMaxValue))
                    nMaxValue = 1;

                dtx.nCurrentRandomNumber = CDTXMania.Random.Next(nMaxValue) + 1; // 1～数値 までの乱数を生成。
            }
        },
        { "SOUND_NOWLOADING", (dtx, param) => dtx.SOUND_NOWLOADING = param },
        { "FORCINGXG", (dtx, param) => dtx.bForceXGChart = param.ToLower().Equals("on") },
        { "VOL7FTO64", (dtx, param) => dtx.bVol137to100 = param.ToLower().Equals("on") },
        { "DTXVPLAYSPEED", (dtx, param) =>
        {
            if (double.TryParse(param, out double dtxvplayspeed) && dtxvplayspeed > 0.0)
            {
                dtx.dbDTXVPlaySpeed = dtxvplayspeed;
            }
        }}
    };

    private static void ProcessLevel(CDTX dtx, string command, string param)
    {
        int level;
        int levelDec = 0;
        if (int.TryParse(param, out level))
        {
            level = Math.Min(Math.Max(level, 0), 1000);
            if (level >= 100)
            {
                int levelTemp = level;
                level = (int)(level / 10.0f);
                levelDec = levelTemp - level * 10;
            }
                
            switch (command)
            {
                case "DLEVEL":
                case "PLAYLEVEL":
                    dtx.LEVEL.Drums = level;
                    dtx.LEVELDEC.Drums = levelDec;
                    break;
                    
                case "GLEVEL":
                    dtx.LEVEL.Guitar = level;
                    break;
                    
                case "BLEVEL":
                    dtx.LEVEL.Bass = level;
                    break;
                    
                case "DLVDEC":
                    if (int.TryParse(param, out levelDec))
                    {
                        dtx.LEVELDEC.Drums = Math.Min(Math.Max(levelDec, 0), 10);
                    }
                    break;
                    
                case "GLVDEC":
                    if (int.TryParse(param, out levelDec))
                    {
                        dtx.LEVELDEC.Guitar = Math.Min(Math.Max(levelDec, 0), 10);
                    }
                    break;
                    
                case "BLVDEC":
                    if (int.TryParse(param, out levelDec))
                    {
                        dtx.LEVELDEC.Bass = Math.Min(Math.Max(levelDec, 0), 10);
                    }
                    break;
            }
        }
    }
        
    private void ParseLine(ref string strCommand, ref string strParameter, ref string comment)
    {
        //early out in case this is a chip location line
        if (lastLineWasChipLocation && strCommand.Length == 5 && IsChipLocation(strCommand))
        {
            //parse chip location
            if (tInput_LineAnalysis_ChipLocation(ref strCommand, ref strParameter))
            {
                return;
            }
        }
        lastLineWasChipLocation = false;

        // 行頭コマンドの処理

        #region [ IF ]

        //-----------------
        if (strCommand.StartsWith("IF", StringComparison.OrdinalIgnoreCase))
        {
            t入力_パラメータ食い込みチェック("IF", ref strCommand, ref strParameter);

            if (bstackIFからENDIFをスキップする.Count == 255)
            {
                Trace.TraceWarning("#IF の入れ子の数が 255 を超えました。この #IF を無視します。[{0}: {1}行]", strFileNameFullPath,
                    lineNumber);
            }
            else if (bstackIFからENDIFをスキップする.Peek())
            {
                bstackIFからENDIFをスキップする.Push(true); // 親が true ならその入れ子も問答無用で true 。
            }
            else // 親が false なら入れ子はパラメータと乱数を比較して結果を判断する。
            {
                if (!int.TryParse(strParameter, out int n数値))
                    n数値 = 1;

                bstackIFからENDIFをスキップする.Push(n数値 != nCurrentRandomNumber); // 乱数と数値が一致したら true 。
            }
        }
        //-----------------

        #endregion
        #region [ ENDIF ]

        //-----------------
        else if (strCommand.StartsWith("ENDIF", StringComparison.OrdinalIgnoreCase))
        {
            t入力_パラメータ食い込みチェック("ENDIF", ref strCommand, ref strParameter);

            if (bstackIFからENDIFをスキップする.Count > 1)
            {
                bstackIFからENDIFをスキップする.Pop(); // 入れ子を１つ脱出。
            }
            else
            {
                Trace.TraceWarning("#ENDIF に対応する #IF がありません。この #ENDIF を無視します。[{0}: {1}行]", strFileNameFullPath,
                    lineNumber);
            }
        }
        //-----------------

        #endregion
        else if (!bstackIFからENDIFをスキップする.Peek()) // IF～ENDIF をスキップするなら以下はすべて無視。
        {
            if (commandHandlers.TryGetValue(strCommand, out Action<CDTX, string>? handler))
            {
                handler(this, strParameter);
                return;
            }

            if (bHeaderOnly) return; // ヘッダのみの解析の場合、以下は無視。
            
            #region [ PANEL ]

            //-----------------
            if (strCommand.StartsWith("PANEL", StringComparison.OrdinalIgnoreCase))
            {
                t入力_パラメータ食い込みチェック("PANEL", ref strCommand, ref strParameter);

                if (!int.TryParse(strParameter, out int _))
                {
                    // 数値じゃないならPANELとみなす
                    PANEL = strParameter; //
                } // 数値ならPAN ELとみなす
            }
            //-----------------

            #endregion

            #region [ MIDIFILE ]

            //-----------------
            else if (strCommand.StartsWith("MIDIFILE", StringComparison.OrdinalIgnoreCase))
            {
                t入力_パラメータ食い込みチェック("MIDIFILE", ref strCommand, ref strParameter);
                MIDIFILE = strParameter;
            }
            //-----------------

            #endregion

            #region [ MIDINOTE ]

            //-----------------
            else if (strCommand.StartsWith("MIDINOTE", StringComparison.OrdinalIgnoreCase))
            {
                t入力_パラメータ食い込みチェック("MIDINOTE", ref strCommand, ref strParameter);
                MIDINOTE = strParameter.ToLower().Equals("on");
            }
            //-----------------

            #endregion

            #region [ BLACKCOLORKEY ]

            //-----------------
            else if (strCommand.StartsWith("BLACKCOLORKEY", StringComparison.OrdinalIgnoreCase))
            {
                t入力_パラメータ食い込みチェック("BLACKCOLORKEY", ref strCommand, ref strParameter);
                BLACKCOLORKEY = strParameter.ToLower().Equals("on");
            }
            //-----------------

            #endregion

            #region [ BASEBPM ]

            //-----------------
            else if (strCommand.StartsWith("BASEBPM", StringComparison.OrdinalIgnoreCase))
            {
                t入力_パラメータ食い込みチェック("BASEBPM", ref strCommand, ref strParameter);

                double basebpm = 0.0;
                //if( double.TryParse( str2, out num6 ) && ( num6 > 0.0 ) )
                if (TryParse(strParameter, out basebpm) &&
                    basebpm >
                    0.0) // #23880 2010.12.30 yyagi: alternative TryParse to permit both '.' and ',' for decimal point
                {
                    // #24204 2011.01.21 yyagi: Fix the condition correctly
                    BASEBPM = basebpm;
                }
            }
            //-----------------
            #endregion
            
            #region [ SOUND_STAGEFAILED ]

            //-----------------
            else if (strCommand.StartsWith("SOUND_STAGEFAILED", StringComparison.OrdinalIgnoreCase))
            {
                t入力_パラメータ食い込みチェック("SOUND_STAGEFAILED", ref strCommand, ref strParameter);
                SOUND_STAGEFAILED = strParameter;
            }
            //-----------------

            #endregion

            #region [ SOUND_FULLCOMBO ]

            //-----------------
            else if (strCommand.StartsWith("SOUND_FULLCOMBO", StringComparison.OrdinalIgnoreCase))
            {
                t入力_パラメータ食い込みチェック("SOUND_FULLCOMBO", ref strCommand, ref strParameter);
                SOUND_FULLCOMBO = strParameter;
            }
            //-----------------

            #endregion

            #region [ SOUND_AUDIENCE ]

            //-----------------
            else if (strCommand.StartsWith("SOUND_AUDIENCE", StringComparison.OrdinalIgnoreCase))
            {
                t入力_パラメータ食い込みチェック("SOUND_AUDIENCE", ref strCommand, ref strParameter);
                SOUND_AUDIENCE = strParameter;
            }
            //-----------------

            #endregion

            // オブジェクト記述コマンドの処理。
            else if (!t入力_行解析_WAVVOL_VOLUME(strCommand, strParameter) &&
                     !t入力_行解析_WAVPAN_PAN(strCommand, strParameter) &&
                     !t入力_行解析_WAV(strCommand, strParameter, comment) &&
                     !t入力_行解析_BMPTEX(strCommand, strParameter, comment) &&
                     !t入力_行解析_BMP(strCommand, strParameter, comment) &&
                     !t入力_行解析_BGAPAN(strCommand, strParameter) &&
                     !t入力_行解析_BGA(strCommand, strParameter) &&
                     !t入力_行解析_AVIPAN(strCommand, strParameter) &&
                     !t入力_行解析_AVI_VIDEO(strCommand, strParameter, comment) &&
                     !t入力_行解析_RESULTIMAGE(strCommand, strParameter) &&
                     !t入力_行解析_RESULTMOVIE(strCommand, strParameter) &&
                     !t入力_行解析_RESULTSOUND(strCommand, strParameter) &&
                     !t入力_行解析_SIZE(strCommand, strParameter) &&
                     !tAnalyzeLine_BPM_BPMzz(strCommand, strParameter))
            {
                tInput_LineAnalysis_ChipLocation(ref strCommand, ref strParameter);
                lastLineWasChipLocation = true;
            }
        }
    }
        
    private bool t入力_行解析_AVI_VIDEO(string strCommand, string strParameter, string strComment)
    {
        // (1) コマンドを処理。

        #region [ "AVI" or "VIDEO" で始まらないコマンドは無効。]

        //-----------------
        if (strCommand.StartsWith("AVI", StringComparison.OrdinalIgnoreCase))
            strCommand = strCommand.Substring(3); // strコマンド から先頭の"AVI"文字を除去。

        else if (strCommand.StartsWith("VIDEO", StringComparison.OrdinalIgnoreCase))
            strCommand = strCommand.Substring(5); // strコマンド から先頭の"VIDEO"文字を除去。

        else
            return false;
        //-----------------

        #endregion

        // (2) パラメータを処理。

        if (strCommand.Length < 2)
            return false; // AVI番号 zz がないなら無効。

        #region [ AVI番号 zz を取得する。]

        //-----------------
        int zz = CConversion.nConvert2DigitBase36StringToNumber(strCommand);
        if (zz < 0 || zz >= 36 * 36)
        {
            Trace.TraceError("AVI(VIDEO)番号に 00～ZZ 以外の値または不正な文字列が指定されました。[{0}: {1}行]", strFileNameFullPath,
                lineNumber);
            return false;
        }

        //-----------------

        #endregion

        #region [ AVIリストに {zz, avi} の組を登録する。 ]

        //-----------------
        var avi = new CAVI(zz, strParameter, strComment, CDTXMania.ConfigIni.nPlaySpeed);
        //{
        //	n番号 = zz,
        //	strファイル名 = strパラメータ,
        //	strコメント文 = strコメント,
        //};

        if (listAVI.ContainsKey(zz)) // 既にリスト中に存在しているなら削除。後のものが有効。
            listAVI.Remove(zz);

        listAVI.Add(zz, avi);

        //var ds = new CDirectShow()
        //{
        //    n番号 = zz,
        //    strファイル名 = strパラメータ,
        //    strコメント文 = strコメント,
        //};

        //if (this.listDS.ContainsKey(zz))	// 既にリスト中に存在しているなら削除。後のものが有効。
        //    this.listDS.Remove(zz);

        //this.listDS.Add(zz, ds);
        //-----------------

        #endregion

        return true;
    }

    private bool t入力_行解析_AVIPAN(string strCommand, string strParameter)
    {
        // (1) コマンドを処理。

        #region [ "AVIPAN" で始まらないコマンドは無効。]

        //-----------------
        if (!strCommand.StartsWith("AVIPAN", StringComparison.OrdinalIgnoreCase))
            return false;

        strCommand = strCommand.Substring(6); // strコマンド から先頭の"AVIPAN"文字を除去。
        //-----------------

        #endregion

        // (2) パラメータを処理。

        if (strCommand.Length < 2)
            return false; // AVIPAN番号 zz がないなら無効。

        #region [ AVIPAN番号 zz を取得する。]

        //-----------------
        int zz = CConversion.nConvert2DigitBase36StringToNumber(strCommand);
        if (zz < 0 || zz >= 36 * 36)
        {
            Trace.TraceError("AVIPAN番号に 00～ZZ 以外の値または不正な文字列が指定されました。[{0}: {1}行]", strFileNameFullPath, lineNumber);
            return false;
        }

        //-----------------

        #endregion

        var avipan = new CAVIPAN()
        {
            n番号 = zz,
        };

        // パラメータ引数（14個）を取得し、avipan に登録していく。

        string[] strParams = strParameter.Split(new char[] { ' ', ',', '(', ')', '[', ']', 'x', '|' },
            StringSplitOptions.RemoveEmptyEntries);

        #region [ パラメータ引数は全14個ないと無効。]

        //-----------------
        if (strParams.Length < 14)
        {
            Trace.TraceError("AVIPAN: 引数が足りません。[{0}: {1}行]", strFileNameFullPath, lineNumber);
            return false;
        }

        //-----------------

        #endregion

        int i = 0;
        int n値 = 0;

        #region [ 1. AVI番号 ]

        //-----------------
        if (string.IsNullOrEmpty(strParams[i]) || strParams[i].Length > 2)
        {
            Trace.TraceError("AVIPAN: {2}番目の数（AVI番号）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber, i + 1);
            return false;
        }

        avipan.nAVI番号 = CConversion.nConvert2DigitBase36StringToNumber(strParams[i]);
        if (avipan.nAVI番号 < 1 || avipan.nAVI番号 >= 36 * 36)
        {
            Trace.TraceError("AVIPAN: {2}番目の数（AVI番号）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber, i + 1);
            return false;
        }

        i++;
        //-----------------

        #endregion

        #region [ 2. 開始転送サイズ_幅 ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("AVIPAN: {2}番目の引数（開始転送サイズ_幅）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber,
                i + 1);
            return false;
        }

        avipan.sz開始サイズ.Width = n値;
        i++;
        //-----------------

        #endregion

        #region [ 3. 転送サイズ_高さ ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("AVIPAN: {2}番目の引数（開始転送サイズ_高さ）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber,
                i + 1);
            return false;
        }

        avipan.sz開始サイズ.Height = n値;
        i++;
        //-----------------

        #endregion

        #region [ 4. 終了転送サイズ_幅 ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("AVIPAN: {2}番目の引数（終了転送サイズ_幅）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber,
                i + 1);
            return false;
        }

        avipan.sz終了サイズ.Width = n値;
        i++;
        //-----------------

        #endregion

        #region [ 5. 終了転送サイズ_高さ ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("AVIPAN: {2}番目の引数（終了転送サイズ_高さ）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber,
                i + 1);
            return false;
        }

        avipan.sz終了サイズ.Height = n値;
        i++;
        //-----------------

        #endregion

        #region [ 6. 動画側開始位置_X ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("AVIPAN: {2}番目の引数（動画側開始位置_X）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber,
                i + 1);
            return false;
        }

        avipan.pt動画側開始位置.X = n値;
        i++;
        //-----------------

        #endregion

        #region [ 7. 動画側開始位置_Y ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("AVIPAN: {2}番目の引数（動画側開始位置_Y）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber,
                i + 1);
            return false;
        }

        avipan.pt動画側開始位置.Y = n値;
        i++;
        //-----------------

        #endregion

        #region [ 8. 動画側終了位置_X ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("AVIPAN: {2}番目の引数（動画側終了位置_X）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber,
                i + 1);
            return false;
        }

        avipan.pt動画側終了位置.X = n値;
        i++;
        //-----------------

        #endregion

        #region [ 9. 動画側終了位置_Y ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("AVIPAN: {2}番目の引数（動画側終了位置_Y）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber,
                i + 1);
            return false;
        }

        avipan.pt動画側終了位置.Y = n値;
        i++;
        //-----------------

        #endregion

        #region [ 10.表示側開始位置_X ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("AVIPAN: {2}番目の引数（表示側開始位置_X）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber,
                i + 1);
            return false;
        }

        avipan.pt表示側開始位置.X = n値;
        i++;
        //-----------------

        #endregion

        #region [ 11.表示側開始位置_Y ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("AVIPAN: {2}番目の引数（表示側開始位置_Y）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber,
                i + 1);
            return false;
        }

        avipan.pt表示側開始位置.Y = n値;
        i++;
        //-----------------

        #endregion

        #region [ 12.表示側終了位置_X ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("AVIPAN: {2}番目の引数（表示側終了位置_X）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber,
                i + 1);
            return false;
        }

        avipan.pt表示側終了位置.X = n値;
        i++;
        //-----------------

        #endregion

        #region [ 13.表示側終了位置_Y ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("AVIPAN: {2}番目の引数（表示側終了位置_Y）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber,
                i + 1);
            return false;
        }

        avipan.pt表示側終了位置.Y = n値;
        i++;
        //-----------------

        #endregion

        #region [ 14.移動時間 ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("AVIPAN: {2}番目の引数（移動時間）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber, i + 1);
            return false;
        }

        if (n値 < 0)
            n値 = 0;

        avipan.n移動時間ct = n値;
        i++;
        //-----------------

        #endregion

        #region [ AVIPANリストに {zz, avipan} の組を登録する。]

        //-----------------
        if (listAVIPAN.ContainsKey(zz)) // 既にリスト中に存在しているなら削除。後のものが有効。
            listAVIPAN.Remove(zz);

        listAVIPAN.Add(zz, avipan);
        //-----------------

        #endregion

        return true;
    }

    private bool t入力_行解析_BGA(string strCommand, string strParameter)
    {
        // (1) コマンドを処理。

        #region [ "BGA" で始まらないコマンドは無効。]

        //-----------------
        if (!strCommand.StartsWith("BGA", StringComparison.OrdinalIgnoreCase))
            return false;

        strCommand = strCommand.Substring(3); // strコマンド から先頭の"BGA"文字を除去。
        //-----------------

        #endregion

        // (2) パラメータを処理。

        if (strCommand.Length < 2)
            return false; // BGA番号 zz がないなら無効。

        #region [ BGA番号 zz を取得する。]

        //-----------------
        int zz = CConversion.nConvert2DigitBase36StringToNumber(strCommand);
        if (zz < 0 || zz >= 36 * 36)
        {
            Trace.TraceError("BGA番号に 00～ZZ 以外の値または不正な文字列が指定されました。[{0}: {1}行]", strFileNameFullPath, lineNumber);
            return false;
        }

        //-----------------

        #endregion

        var bga = new CBGA()
        {
            n番号 = zz,
        };

        // パラメータ引数（7個）を取得し、bga に登録していく。

        string[] strParams = strParameter.Split(new char[] { ' ', ',', '(', ')', '[', ']', 'x', '|' },
            StringSplitOptions.RemoveEmptyEntries);

        #region [ パラメータ引数は全7個ないと無効。]

        //-----------------
        if (strParams.Length < 7)
        {
            Trace.TraceError("BGA: 引数が足りません。[{0}: {1}行]", strFileNameFullPath, lineNumber);
            return false;
        }

        //-----------------

        #endregion

        int i = 0;
        int n値 = 0;

        #region [ 1.BMP番号 ]

        //-----------------
        if (string.IsNullOrEmpty(strParams[i]) || strParams[i].Length > 2)
        {
            Trace.TraceError("BGA: {2}番目の数（BMP番号）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber, i + 1);
            return false;
        }

        bga.nBMP番号 = CConversion.nConvert2DigitBase36StringToNumber(strParams[i]);
        if (bga.nBMP番号 < 1 || bga.nBMP番号 >= 36 * 36)
        {
            Trace.TraceError("BGA: {2}番目の数（BMP番号）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber, i + 1);
            return false;
        }

        i++;
        //-----------------

        #endregion

        #region [ 2.画像側位置１_X ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("BGA: {2}番目の引数（画像側位置１_X）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber, i + 1);
            return false;
        }

        bga.pt画像側左上座標.X = n値;
        i++;
        //-----------------

        #endregion

        #region [ 3.画像側位置１_Y ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("BGA: {2}番目の引数（画像側位置１_Y）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber, i + 1);
            return false;
        }

        bga.pt画像側左上座標.Y = n値;
        i++;
        //-----------------

        #endregion

        #region [ 4.画像側位置２_X ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("BGA: {2}番目の引数（画像側位置２_X）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber, i + 1);
            return false;
        }

        bga.pt画像側右下座標.X = n値;
        i++;
        //-----------------

        #endregion

        #region [ 5.画像側位置２_Y ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("BGA: {2}番目の引数（画像側座標２_Y）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber, i + 1);
            return false;
        }

        bga.pt画像側右下座標.Y = n値;
        i++;
        //-----------------

        #endregion

        #region [ 6.表示位置_X ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("BGA: {2}番目の引数（表示位置_X）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber, i + 1);
            return false;
        }

        bga.pt表示座標.X = n値;
        i++;
        //-----------------

        #endregion

        #region [ 7.表示位置_Y ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("BGA: {2}番目の引数（表示位置_Y）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber, i + 1);
            return false;
        }

        bga.pt表示座標.Y = n値;
        i++;
        //-----------------

        #endregion

        #region [ 画像側座標の正規化とクリッピング。]

        //-----------------
        if (bga.pt画像側左上座標.X > bga.pt画像側右下座標.X)
        {
            n値 = bga.pt画像側左上座標.X;
            bga.pt画像側左上座標.X = bga.pt画像側右下座標.X;
            bga.pt画像側右下座標.X = n値;
        }

        if (bga.pt画像側左上座標.Y > bga.pt画像側右下座標.Y)
        {
            n値 = bga.pt画像側左上座標.Y;
            bga.pt画像側左上座標.Y = bga.pt画像側右下座標.Y;
            bga.pt画像側右下座標.Y = n値;
        }

        //-----------------

        #endregion

        #region [ BGAリストに {zz, bga} の組を登録する。]

        //-----------------
        if (listBGA.ContainsKey(zz)) // 既にリスト中に存在しているなら削除。後のものが有効。
            listBGA.Remove(zz);

        listBGA.Add(zz, bga);
        //-----------------

        #endregion

        return true;
    }

    private bool t入力_行解析_BGAPAN(string strCommand, string strParameter)
    {
        // (1) コマンドを処理。

        #region [ "BGAPAN" で始まらないコマンドは無効。]

        //-----------------
        if (!strCommand.StartsWith("BGAPAN", StringComparison.OrdinalIgnoreCase))
            return false;

        strCommand = strCommand.Substring(6); // strコマンド から先頭の"BGAPAN"文字を除去。
        //-----------------

        #endregion

        // (2) パラメータを処理。

        if (strCommand.Length < 2)
            return false; // BGAPAN番号 zz がないなら無効。

        #region [ BGAPAN番号 zz を取得する。]

        //-----------------
        int zz = CConversion.nConvert2DigitBase36StringToNumber(strCommand);
        if (zz < 0 || zz >= 36 * 36)
        {
            Trace.TraceError("BGAPAN番号に 00～ZZ 以外の値または不正な文字列が指定されました。[{0}: {1}行]", strFileNameFullPath, lineNumber);
            return false;
        }

        //-----------------

        #endregion

        var bgapan = new CBGAPAN()
        {
            n番号 = zz,
        };

        // パラメータ引数（14個）を取得し、bgapan に登録していく。

        string[] strParams = strParameter.Split(new char[] { ' ', ',', '(', ')', '[', ']', 'x', '|' },
            StringSplitOptions.RemoveEmptyEntries);

        #region [ パラメータ引数は全14個ないと無効。]

        //-----------------
        if (strParams.Length < 14)
        {
            Trace.TraceError("BGAPAN: 引数が足りません。[{0}: {1}行]", strFileNameFullPath, lineNumber);
            return false;
        }

        //-----------------

        #endregion

        int i = 0;
        int n値 = 0;

        #region [ 1. BMP番号 ]

        //-----------------
        if (string.IsNullOrEmpty(strParams[i]) || strParams[i].Length > 2)
        {
            Trace.TraceError("BGAPAN: {2}番目の数（BMP番号）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber, i + 1);
            return false;
        }

        bgapan.nBMP番号 = CConversion.nConvert2DigitBase36StringToNumber(strParams[i]);
        if (bgapan.nBMP番号 < 1 || bgapan.nBMP番号 >= 36 * 36)
        {
            Trace.TraceError("BGAPAN: {2}番目の数（BMP番号）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber, i + 1);
            return false;
        }

        i++;
        //-----------------

        #endregion

        #region [ 2. 開始転送サイズ_幅 ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("BGAPAN: {2}番目の引数（開始転送サイズ_幅）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber,
                i + 1);
            return false;
        }

        bgapan.sz開始サイズ.Width = n値;
        i++;
        //-----------------

        #endregion

        #region [ 3. 開始転送サイズ_高さ ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("BGAPAN: {2}番目の引数（開始転送サイズ_高さ）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber,
                i + 1);
            return false;
        }

        bgapan.sz開始サイズ.Height = n値;
        i++;
        //-----------------

        #endregion

        #region [ 4. 終了転送サイズ_幅 ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("BGAPAN: {2}番目の引数（終了転送サイズ_幅）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber,
                i + 1);
            return false;
        }

        bgapan.sz終了サイズ.Width = n値;
        i++;
        //-----------------

        #endregion

        #region [ 5. 終了転送サイズ_高さ ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("BGAPAN: {2}番目の引数（終了転送サイズ_高さ）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber,
                i + 1);
            return false;
        }

        bgapan.sz終了サイズ.Height = n値;
        i++;
        //-----------------

        #endregion

        #region [ 6. 画像側開始位置_X ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("BGAPAN: {2}番目の引数（画像側開始位置_X）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber,
                i + 1);
            return false;
        }

        bgapan.pt画像側開始位置.X = n値;
        i++;
        //-----------------

        #endregion

        #region [ 7. 画像側開始位置_Y ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("BGAPAN: {2}番目の引数（画像側開始位置_Y）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber,
                i + 1);
            return false;
        }

        bgapan.pt画像側開始位置.Y = n値;
        i++;
        //-----------------

        #endregion

        #region [ 8. 画像側終了位置_X ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("BGAPAN: {2}番目の引数（画像側終了位置_X）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber,
                i + 1);
            return false;
        }

        bgapan.pt画像側終了位置.X = n値;
        i++;
        //-----------------

        #endregion

        #region [ 9. 画像側終了位置_Y ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("BGAPAN: {2}番目の引数（画像側終了位置_Y）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber,
                i + 1);
            return false;
        }

        bgapan.pt画像側終了位置.Y = n値;
        i++;
        //-----------------

        #endregion

        #region [ 10.表示側開始位置_X ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("BGAPAN: {2}番目の引数（表示側開始位置_X）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber,
                i + 1);
            return false;
        }

        bgapan.pt表示側開始位置.X = n値;
        i++;
        //-----------------

        #endregion

        #region [ 11.表示側開始位置_Y ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("BGAPAN: {2}番目の引数（表示側開始位置_Y）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber,
                i + 1);
            return false;
        }

        bgapan.pt表示側開始位置.Y = n値;
        i++;
        //-----------------

        #endregion

        #region [ 12.表示側終了位置_X ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("BGAPAN: {2}番目の引数（表示側終了位置_X）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber,
                i + 1);
            return false;
        }

        bgapan.pt表示側終了位置.X = n値;
        i++;
        //-----------------

        #endregion

        #region [ 13.表示側終了位置_Y ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("BGAPAN: {2}番目の引数（表示側終了位置_Y）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber,
                i + 1);
            return false;
        }

        bgapan.pt表示側終了位置.Y = n値;
        i++;
        //-----------------

        #endregion

        #region [ 14.移動時間 ]

        //-----------------
        n値 = 0;
        if (!int.TryParse(strParams[i], out n値))
        {
            Trace.TraceError("BGAPAN: {2}番目の引数（移動時間）が異常です。[{0}: {1}行]", strFileNameFullPath, lineNumber, i + 1);
            return false;
        }

        if (n値 < 0)
            n値 = 0;

        bgapan.n移動時間ct = n値;
        i++;
        //-----------------

        #endregion

        #region [ BGAPANリストに {zz, bgapan} の組を登録する。]

        //-----------------
        if (listBGAPAN.ContainsKey(zz)) // 既にリスト中に存在しているなら削除。後のものが有効。
            listBGAPAN.Remove(zz);

        listBGAPAN.Add(zz, bgapan);
        //-----------------

        #endregion

        return true;
    }

    private bool t入力_行解析_BMP(string strCommand, string strParameter, string strComment)
    {
        // (1) コマンドを処理。

        #region [ "BMP" で始まらないコマンドは無効。]

        //-----------------
        if (!strCommand.StartsWith("BMP", StringComparison.OrdinalIgnoreCase))
            return false;

        strCommand = strCommand.Substring(3); // strコマンド から先頭の"BMP"文字を除去。
        //-----------------

        #endregion

        // (2) パラメータを処理。

        int zz = 0;

        #region [ BMP番号 zz を取得する。]

        //-----------------
        if (strCommand.Length < 2)
        {
            #region [ (A) "#BMP:" の場合 → zz = 00 ]

            //-----------------
            zz = 0;
            //-----------------

            #endregion
        }
        else
        {
            #region [ (B) "#BMPzz:" の場合 → zz = 00 ～ ZZ ]

            //-----------------
            zz = CConversion.nConvert2DigitBase36StringToNumber(strCommand);
            if (zz < 0 || zz >= 36 * 36)
            {
                Trace.TraceError("BMP番号に 00～ZZ 以外の値または不正な文字列が指定されました。[{0}: {1}行]", strFileNameFullPath, lineNumber);
                return false;
            }

            //-----------------

            #endregion
        }

        //-----------------

        #endregion


        var bmp = new CBMP()
        {
            n番号 = zz,
            strファイル名 = strParameter,
            strコメント文 = strComment,
        };

        #region [ BMPリストに {zz, bmp} の組を登録。]

        //-----------------
        if (listBMP.ContainsKey(zz)) // 既にリスト中に存在しているなら削除。後のものが有効。
            listBMP.Remove(zz);

        listBMP.Add(zz, bmp);
        //-----------------

        #endregion

        return true;
    }

    private bool t入力_行解析_BMPTEX(string strCommand, string strParameter, string strComment)
    {
        // (1) コマンドを処理。

        #region [ "BMPTEX" で始まらないコマンドは無効。]

        //-----------------
        if (!strCommand.StartsWith("BMPTEX", StringComparison.OrdinalIgnoreCase))
            return false;

        strCommand = strCommand.Substring(6); // strコマンド から先頭の"BMPTEX"文字を除去。
        //-----------------

        #endregion

        // (2) パラメータを処理。

        if (strCommand.Length < 2)
            return false; // BMPTEX番号 zz がないなら無効。

        #region [ BMPTEX番号 zz を取得する。]

        //-----------------
        int zz = CConversion.nConvert2DigitBase36StringToNumber(strCommand);
        if (zz < 0 || zz >= 36 * 36)
        {
            Trace.TraceError("BMPTEX番号に 00～ZZ 以外の値または不正な文字列が指定されました。[{0}: {1}]", strFileNameFullPath, lineNumber);
            return false;
        }

        //-----------------

        #endregion

        var bmptex = new CBMPTEX()
        {
            n番号 = zz,
            strファイル名 = strParameter,
            strコメント文 = strComment,
        };

        #region [ BMPTEXリストに {zz, bmptex} の組を登録する。]

        //-----------------
        if (listBMPTEX.ContainsKey(zz)) // 既にリスト中に存在しているなら削除。後のものが有効。
            listBMPTEX.Remove(zz);

        listBMPTEX.Add(zz, bmptex);
        //-----------------

        #endregion

        return true;
    }

    private bool tAnalyzeLine_BPM_BPMzz(string strCommand, string strParameter)
    {
        // (1) コマンドを処理。

        #region [ "BPM" で始まらないコマンドは無効。]

        //-----------------
        if (!strCommand.StartsWith("BPM", StringComparison.OrdinalIgnoreCase))
            return false;

        strCommand = strCommand.Substring(3); // strコマンド から先頭の"BPM"文字を除去。
        //-----------------

        #endregion

        // (2) パラメータを処理。

        int zz = 0;

        #region [ BPM番号 zz を取得する。]

        //-----------------
        if (strCommand.Length < 2)
        {
            #region [ (A) "#BPM:" の場合 → zz = 00 ]

            //-----------------
            zz = 0;
            //-----------------

            #endregion
        }
        else
        {
            #region [ (B) "#BPMzz:" の場合 → zz = 00 ～ ZZ ]

            //-----------------
            zz = CConversion.nConvert2DigitBase36StringToNumber(strCommand);
            if (zz < 0 || zz >= 36 * 36)
            {
                Trace.TraceError("BPM番号に 00～ZZ 以外の値または不正な文字列が指定されました。[{0}: {1}行]", strFileNameFullPath, lineNumber);
                return false;
            }

            //-----------------

            #endregion
        }

        //-----------------

        #endregion

        double dbBPM = 0.0;

        #region [ BPM値を取得する。]

        //-----------------
        //if( !double.TryParse( strパラメータ, out result ) )
        if (!TryParse(strParameter,
                out dbBPM)) // #23880 2010.12.30 yyagi: alternative TryParse to permit both '.' and ',' for decimal point
            return false;

        if (dbBPM <= 0.0)
            return false;
        //-----------------

        #endregion

        if (zz == 0) // "#BPM00:" と "#BPM:" は等価。
            BPM = dbBPM; // この曲の代表 BPM に格納する。

        #region [ BPMリストに {内部番号, zz, dbBPM} の組を登録。]

        //-----------------
        listBPM.Add(
            n内部番号BPM1to,
            new CBPM()
            {
                n内部番号 = n内部番号BPM1to,
                n表記上の番号 = zz,
                dbBPM値 = dbBPM,
            });
        //-----------------

        #endregion

        #region [ BPM番号が zz であるBPM未設定のBPMチップがあれば、そのサイズを変更する。無限管理に対応。]

        //-----------------
        if (n無限管理BPM[zz] == -zz) // 初期状態では n無限管理BPM[zz] = -zz である。この場合、#BPMzz がまだ出現していないことを意味する。
        {
            for (int i = 0;
                 i < listChip.Count;
                 i++) // これまでに出てきたチップのうち、該当する（BPM値が未設定の）BPMチップの値を変更する（仕組み上、必ず後方参照となる）。
            {
                var chip = listChip[i];

                if (chip.bBPMチップである &&
                    chip.nIntegerValue_InternalNumber ==
                    -zz) // #BPMzz 行より前の行に出現した #BPMzz では、整数値_内部番号は -zz に初期化されている。
                    chip.nIntegerValue_InternalNumber = n内部番号BPM1to;
            }
        }

        n無限管理BPM[zz] = n内部番号BPM1to; // 次にこの BPM番号 zz を使うBPMチップが現れたら、このBPM値が格納されることになる。
        n内部番号BPM1to++; // 内部番号は単純増加連番。
        //-----------------

        #endregion

        return true;
    }

    private bool t入力_行解析_RESULTIMAGE(string strCommand, string strParameter)
    {
        // (1) コマンドを処理。

        #region [ "RESULTIMAGE" で始まらないコマンドは無効。]

        //-----------------
        if (!strCommand.StartsWith("RESULTIMAGE", StringComparison.OrdinalIgnoreCase))
            return false;

        strCommand = strCommand.Substring(11); // strコマンド から先頭の"RESULTIMAGE"文字を除去。
        //-----------------

        #endregion

        // (2) パラメータを処理。
        //     コマンドには "#RESULTIMAGE:" と "#RESULTIMAGE_SS～E" の2種類があり、パラメータの処理はそれぞれ異なる。

        if (strCommand.Length < 2)
        {
            #region [ (A) ランク指定がない場合("#RESULTIMAGE:") → 優先順位が設定されていないすべてのランクで同じパラメータを使用する。]

            //-----------------
            for (int i = 0; i < 7; i++)
            {
                if (nRESULTIMAGE用優先順位[i] == 0)
                    RESULTIMAGE[i] = strParameter.Trim();
            }

            //-----------------

            #endregion
        }
        else
        {
            #region [ (B) ランク指定がある場合("#RESULTIMAGE_SS～E:") → 優先順位に従ってパラメータを記録する。]

            //-----------------
            switch (strCommand.ToUpper())
            {
                case "_SS":
                    t入力_行解析_RESULTIMAGE_ファイルを設定する(0, strParameter);
                    break;

                case "_S":
                    t入力_行解析_RESULTIMAGE_ファイルを設定する(1, strParameter);
                    break;

                case "_A":
                    t入力_行解析_RESULTIMAGE_ファイルを設定する(2, strParameter);
                    break;

                case "_B":
                    t入力_行解析_RESULTIMAGE_ファイルを設定する(3, strParameter);
                    break;

                case "_C":
                    t入力_行解析_RESULTIMAGE_ファイルを設定する(4, strParameter);
                    break;

                case "_D":
                    t入力_行解析_RESULTIMAGE_ファイルを設定する(5, strParameter);
                    break;

                case "_E":
                    t入力_行解析_RESULTIMAGE_ファイルを設定する(6, strParameter);
                    break;
            }

            //-----------------

            #endregion
        }

        return true;
    }

    private void t入力_行解析_RESULTIMAGE_ファイルを設定する(int nランク0to6, string strファイル名)
    {
        if (nランク0to6 < 0 || nランク0to6 > 6) // 値域チェック。
            return;

        // 指定されたランクから上位のすべてのランクについて、ファイル名を更新する。

        for (int i = nランク0to6; i >= 0; i--)
        {
            int n優先順位 = 7 - nランク0to6;

            // 現状より優先順位の低い RESULTIMAGE[] に限り、ファイル名を更新できる。
            //（例：#RESULTMOVIE_D が #RESULTIMAGE_A より後に出現しても、#RESULTIMAGE_A で指定されたファイル名を上書きすることはできない。しかしその逆は可能。）

            if (nRESULTIMAGE用優先順位[i] < n優先順位)
            {
                nRESULTIMAGE用優先順位[i] = n優先順位;
                RESULTIMAGE[i] = strファイル名;
            }
        }
    }

    private bool t入力_行解析_RESULTMOVIE(string strCommand, string strParameter)
    {
        // (1) コマンドを処理。

        #region [ "RESULTMOVIE" で始まらないコマンドは無効。]

        //-----------------
        if (!strCommand.StartsWith("RESULTMOVIE", StringComparison.OrdinalIgnoreCase))
            return false;

        strCommand = strCommand.Substring(11); // strコマンド から先頭の"RESULTMOVIE"文字を除去。
        //-----------------

        #endregion

        // (2) パラメータを処理。
        //     コマンドには "#RESULTMOVIE:" と "#RESULTMOVIE_SS～E" の2種類があり、パラメータの処理はそれぞれ異なる。

        if (strCommand.Length < 2)
        {
            #region [ (A) ランク指定がない場合("#RESULTMOVIE:") → 優先順位が設定されていないすべてのランクで同じパラメータを使用する。]

            //-----------------
            for (int i = 0; i < 7; i++)
            {
                if (nRESULTMOVIE用優先順位[i] == 0)
                    RESULTMOVIE[i] = strParameter.Trim();
            }

            //-----------------

            #endregion
        }
        else
        {
            #region [ (B) ランク指定がある場合("#RESULTMOVIE_SS～E:") → 優先順位に従ってパラメータを記録する。]

            //-----------------
            switch (strCommand.ToUpper())
            {
                case "_SS":
                    t入力_行解析_RESULTMOVIE_ファイルを設定する(0, strParameter);
                    break;

                case "_S":
                    t入力_行解析_RESULTMOVIE_ファイルを設定する(1, strParameter);
                    break;

                case "_A":
                    t入力_行解析_RESULTMOVIE_ファイルを設定する(2, strParameter);
                    break;

                case "_B":
                    t入力_行解析_RESULTMOVIE_ファイルを設定する(3, strParameter);
                    break;

                case "_C":
                    t入力_行解析_RESULTMOVIE_ファイルを設定する(4, strParameter);
                    break;

                case "_D":
                    t入力_行解析_RESULTMOVIE_ファイルを設定する(5, strParameter);
                    break;

                case "_E":
                    t入力_行解析_RESULTMOVIE_ファイルを設定する(6, strParameter);
                    break;
            }

            //-----------------

            #endregion
        }

        return true;
    }

    private void t入力_行解析_RESULTMOVIE_ファイルを設定する(int nランク0to6, string strファイル名)
    {
        if (nランク0to6 < 0 || nランク0to6 > 6) // 値域チェック。
            return;

        // 指定されたランクから上位のすべてのランクについて、ファイル名を更新する。

        for (int i = nランク0to6; i >= 0; i--)
        {
            int n優先順位 = 7 - nランク0to6;

            // 現状より優先順位の低い RESULTMOVIE[] に限り、ファイル名を更新できる。
            //（例：#RESULTMOVIE_D が #RESULTMOVIE_A より後に出現しても、#RESULTMOVIE_A で指定されたファイル名を上書きすることはできない。しかしその逆は可能。）

            if (nRESULTMOVIE用優先順位[i] < n優先順位)
            {
                nRESULTMOVIE用優先順位[i] = n優先順位;
                RESULTMOVIE[i] = strファイル名;
            }
        }
    }

    private bool t入力_行解析_RESULTSOUND(string strCommand, string strParameter)
    {
        // (1) コマンドを処理。

        #region [ "RESULTSOUND" で始まらないコマンドは無効。]

        //-----------------
        if (!strCommand.StartsWith("RESULTSOUND", StringComparison.OrdinalIgnoreCase))
            return false;

        strCommand = strCommand.Substring(11); // strコマンド から先頭の"RESULTSOUND"文字を除去。
        //-----------------

        #endregion

        // (2) パラメータを処理。
        //     コマンドには "#RESULTSOUND:" と "#RESULTSOUND_SS～E" の2種類があり、パラメータの処理はそれぞれ異なる。

        if (strCommand.Length < 2)
        {
            #region [ (A) ランク指定がない場合("#RESULTSOUND:") → 優先順位が設定されていないすべてのランクで同じパラメータを使用する。]

            //-----------------
            for (int i = 0; i < 7; i++)
            {
                if (nRESULTSOUND用優先順位[i] == 0)
                    RESULTSOUND[i] = strParameter.Trim();
            }

            //-----------------

            #endregion
        }
        else
        {
            #region [ (B) ランク指定がある場合("#RESULTSOUND_SS～E:") → 優先順位に従ってパラメータを記録する。]

            //-----------------
            switch (strCommand.ToUpper())
            {
                case "_SS":
                    t入力_行解析_RESULTSOUND_ファイルを設定する(0, strParameter);
                    break;

                case "_S":
                    t入力_行解析_RESULTSOUND_ファイルを設定する(1, strParameter);
                    break;

                case "_A":
                    t入力_行解析_RESULTSOUND_ファイルを設定する(2, strParameter);
                    break;

                case "_B":
                    t入力_行解析_RESULTSOUND_ファイルを設定する(3, strParameter);
                    break;

                case "_C":
                    t入力_行解析_RESULTSOUND_ファイルを設定する(4, strParameter);
                    break;

                case "_D":
                    t入力_行解析_RESULTSOUND_ファイルを設定する(5, strParameter);
                    break;

                case "_E":
                    t入力_行解析_RESULTSOUND_ファイルを設定する(6, strParameter);
                    break;
            }

            //-----------------

            #endregion
        }

        return true;
    }

    private void t入力_行解析_RESULTSOUND_ファイルを設定する(int nランク0to6, string strファイル名)
    {
        if (nランク0to6 < 0 || nランク0to6 > 6) // 値域チェック。
            return;

        // 指定されたランクから上位のすべてのランクについて、ファイル名を更新する。

        for (int i = nランク0to6; i >= 0; i--)
        {
            int n優先順位 = 7 - nランク0to6;

            // 現状より優先順位の低い RESULTSOUND[] に限り、ファイル名を更新できる。
            //（例：#RESULTSOUND_D が #RESULTSOUND_A より後に出現しても、#RESULTSOUND_A で指定されたファイル名を上書きすることはできない。しかしその逆は可能。）

            if (nRESULTSOUND用優先順位[i] < n優先順位)
            {
                nRESULTSOUND用優先順位[i] = n優先順位;
                RESULTSOUND[i] = strファイル名;
            }
        }
    }

    private bool t入力_行解析_SIZE(string strCommand, string strParameter)
    {
        // (1) コマンドを処理。

        #region [ "SIZE" で始まらないコマンドや、その後ろに2文字（番号）が付随してないコマンドは無効。]

        //-----------------
        if (!strCommand.StartsWith("SIZE", StringComparison.OrdinalIgnoreCase))
            return false;

        strCommand = strCommand.Substring(4); // strコマンド から先頭の"SIZE"文字を除去。

        if (strCommand.Length < 2) // サイズ番号の指定がない場合は無効。
            return false;
        //-----------------

        #endregion

        #region [ nWAV番号（36進数2桁）を取得。]

        //-----------------
        int nWAV番号 = CConversion.nConvert2DigitBase36StringToNumber(strCommand);

        if (nWAV番号 < 0 || nWAV番号 >= 36 * 36)
        {
            Trace.TraceError("SIZEのWAV番号に 00～ZZ 以外の値または不正な文字列が指定されました。[{0}: {1}行]", strFileNameFullPath,
                lineNumber);
            return false;
        }

        //-----------------

        #endregion


        // (2) パラメータを処理。

        #region [ nサイズ値 を取得する。値は 0～100 に収める。]

        //-----------------
        int nサイズ値;

        if (!int.TryParse(strParameter, out nサイズ値))
            return true; // int変換に失敗しても、この行自体の処理は終えたのでtrueを返す。

        nサイズ値 = Math.Min(Math.Max(nサイズ値, 0), 100); // 0未満は0、100超えは100に強制変換。
        //-----------------

        #endregion

        #region [ nWAV番号で示されるサイズ未設定のWAVチップがあれば、そのサイズを変更する。無限管理に対応。]

        //-----------------
        if (n無限管理SIZE[nWAV番号] == -nWAV番号) // 初期状態では n無限管理SIZE[xx] = -xx である。この場合、#SIZExx がまだ出現していないことを意味する。
        {
            foreach (CWAV wav in listWAV.Values) // これまでに出てきたWAVチップのうち、該当する（サイズが未設定の）チップのサイズを変更する（仕組み上、必ず後方参照となる）。
            {
                if (wav.nChipSize == -nWAV番号) // #SIZExx 行より前の行に出現した #WAVxx では、チップサイズは -xx に初期化されている。
                    wav.nChipSize = nサイズ値;
            }
        }

        n無限管理SIZE[nWAV番号] = nサイズ値; // 次にこの nWAV番号を使うWAVチップが現れたら、負数の代わりに、このサイズ値が格納されることになる。
        //-----------------

        #endregion

        return true;
    }

    private bool t入力_行解析_WAV(string strCommand, string strParameter, string strComment)
    {
        // (1) コマンドを処理。

        #region [ "WAV" で始まらないコマンドは無効。]

        //-----------------
        if (!strCommand.StartsWith("WAV", StringComparison.OrdinalIgnoreCase))
            return false;

        strCommand = strCommand.Substring(3); // strコマンド から先頭の"WAV"文字を除去。
        //-----------------

        #endregion

        // (2) パラメータを処理。

        if (strCommand.Length < 2)
            return false; // WAV番号 zz がないなら無効。

        #region [ WAV番号 zz を取得する。]

        //-----------------
        int zz = CConversion.nConvert2DigitBase36StringToNumber(strCommand);
        if (zz < 0 || zz >= 36 * 36)
        {
            Trace.TraceError("WAV番号に 00～ZZ 以外の値または不正な文字列が指定されました。[{0}: {1}行]", strFileNameFullPath, lineNumber);
            return false;
        }

        //-----------------

        #endregion

        CWAV wav = new()
        {
            n内部番号 = n内部番号WAV1to,
            n表記上の番号 = zz,
            nChipSize = n無限管理SIZE[zz],
            nPosition = n無限管理PAN[zz],
            nVolume = n無限管理VOL[zz],
            strFileName = strParameter,
            strコメント文 = strComment,
        };

        #region [ WAVリストに {内部番号, wav} の組を登録。]

        //-----------------
        listWAV.Add(n内部番号WAV1to, wav);
        //-----------------

        #endregion

        #region [ WAV番号が zz である内部番号未設定のWAVチップがあれば、その内部番号を変更する。無限管理対応。]

        //-----------------
        if (n無限管理WAV[zz] == -zz) // 初期状態では n無限管理WAV[zz] = -zz である。この場合、#WAVzz がまだ出現していないことを意味する。
        {
            for (int i = 0;
                 i < listChip.Count;
                 i++) // これまでに出てきたチップのうち、該当する（内部番号が未設定の）WAVチップの値を変更する（仕組み上、必ず後方参照となる）。
            {
                var chip = listChip[i];

                if (chip.bWAVを使うチャンネルである &&
                    (chip.nIntegerValue_InternalNumber ==
                     -zz)) // この #WAVzz 行より前の行に出現した #WAVzz では、整数値_内部番号は -zz に初期化されている。
                    chip.nIntegerValue_InternalNumber = n内部番号WAV1to;
            }
        }

        n無限管理WAV[zz] = n内部番号WAV1to; // 次にこの WAV番号 zz を使うWAVチップが現れたら、この内部番号が格納されることになる。
        n内部番号WAV1to++; // 内部番号は単純増加連番。
        //-----------------

        #endregion

        return true;
    }

    private bool t入力_行解析_WAVPAN_PAN(string strCommand, string strParameter)
    {
        // (1) コマンドを処理。

        #region [ "WAVPAN" or "PAN" で始まらないコマンドは無効。]

        //-----------------
        if (strCommand.StartsWith("WAVPAN", StringComparison.OrdinalIgnoreCase))
            strCommand = strCommand.Substring(6); // strコマンド から先頭の"WAVPAN"文字を除去。

        else if (strCommand.StartsWith("PAN", StringComparison.OrdinalIgnoreCase))
            strCommand = strCommand.Substring(3); // strコマンド から先頭の"PAN"文字を除去。

        else
            return false;
        //-----------------

        #endregion

        // (2) パラメータを処理。

        if (strCommand.Length < 2)
            return false; // WAV番号 zz がないなら無効。

        #region [ WAV番号 zz を取得する。]

        //-----------------
        int zz = CConversion.nConvert2DigitBase36StringToNumber(strCommand);
        if (zz is < 0 or >= 36 * 36)
        {
            Trace.TraceError("WAVPAN(PAN)のWAV番号に 00～ZZ 以外の値または不正な文字列が指定されました。[{0}: {1}行]", strFileNameFullPath,
                lineNumber);
            return false;
        }

        //-----------------

        #endregion

        #region [ WAV番号 zz を持つWAVチップの位置を変更する。無限定義対応。]

        //-----------------
        int n位置;
        if (int.TryParse(strParameter, out n位置))
        {
            n位置 = Math.Min(Math.Max(n位置, -100), 100); // -100～+100 に丸める

            if (n無限管理PAN[zz] ==
                (-10000 - zz)) // 初期状態では n無限管理PAN[zz] = -10000 - zz である。この場合、#WAVPANzz, #PANzz がまだ出現していないことを意味する。
            {
                foreach (CWAV wav in listWAV.Values) // これまでに出てきたチップのうち、該当する（位置が未設定の）WAVチップの値を変更する（仕組み上、必ず後方参照となる）。
                {
                    if (wav.nPosition == (-10000 -
                                          zz)) // #WAVPANzz, #PANzz 行より前の行に出現した #WAVzz では、位置は -10000-zz に初期化されている。
                        wav.nPosition = n位置;
                }
            }

            n無限管理PAN[zz] = n位置; // 次にこの WAV番号 zz を使うWAVチップが現れたら、この位置が格納されることになる。
        }

        //-----------------

        #endregion

        return true;
    }

    private bool t入力_行解析_WAVVOL_VOLUME(string strCommand, string strParameter)
    {
        // (1) コマンドを処理。

        #region [ "WAVCOL" or "VOLUME" で始まらないコマンドは無効。]

        //-----------------
        if (strCommand.StartsWith("WAVVOL", StringComparison.OrdinalIgnoreCase))
            strCommand = strCommand.Substring(6); // strコマンド から先頭の"WAVVOL"文字を除去。

        else if (strCommand.StartsWith("VOLUME", StringComparison.OrdinalIgnoreCase))
            strCommand = strCommand.Substring(6); // strコマンド から先頭の"VOLUME"文字を除去。

        else
            return false;
        //-----------------

        #endregion

        // (2) パラメータを処理。

        if (strCommand.Length < 2)
            return false; // WAV番号 zz がないなら無効。

        #region [ WAV番号 zz を取得する。]

        //-----------------
        int zz = CConversion.nConvert2DigitBase36StringToNumber(strCommand);
        if (zz < 0 || zz >= 36 * 36)
        {
            Trace.TraceError("WAV番号に 00～ZZ 以外の値または不正な文字列が指定されました。[{0}: {1}行]", strFileNameFullPath, lineNumber);
            return false;
        }

        //-----------------

        #endregion

        #region [ WAV番号 zz を持つWAVチップの音量を変更する。無限定義対応。]

        //-----------------
        int n音量;
        if (int.TryParse(strParameter, out n音量))
        {
            if (!bVol137to100)
                n音量 = Math.Min(Math.Max(n音量, 0), 100); // 0～100に丸める。

            if (n無限管理VOL[zz] == -zz) // 初期状態では n無限管理VOL[zz] = - zz である。この場合、#WAVVOLzz, #VOLUMEzz がまだ出現していないことを意味する。
            {
                foreach (CWAV wav in listWAV.Values) // これまでに出てきたチップのうち、該当する（音量が未設定の）WAVチップの値を変更する（仕組み上、必ず後方参照となる）。
                {
                    if (wav.nVolume == -zz) // #WAVVOLzz, #VOLUMEzz 行より前の行に出現した #WAVzz では、音量は -zz に初期化されている。
                        wav.nVolume = n音量;
                }
            }

            n無限管理VOL[zz] = n音量; // 次にこの WAV番号 zz を使うWAVチップが現れたら、この音量が格納されることになる。
        }

        //-----------------

        #endregion

        return true;
    }

    private int tWAVVolMax137toMax100(int nMax137Vol)
    {
        int nMax100Vol;

        nMax100Vol = (int)(100.0 / nMax137Vol);

        return nMax100Vol;
    }

    private double tComputeChipPlayTimeMs(double startTimePosition, int positionDelta, double currBarLength,
        double currBPM)
    {
        //Emulate Original method by using Math.Floor
        if (CDTXMania.ConfigIni.nChipPlayTimeComputeMode == 0)
        {
            return Math.Floor(startTimePosition + ((int)(((0x271 * (positionDelta)) * currBarLength) / currBPM)));
        }
        //Accurate Method returning double
        else
        {
            return startTimePosition + (0x271 * (positionDelta) * currBarLength / currBPM);
        }
    }

    private int tConvertFromDoubleToIntBasedOnComputeMode(double input)
    {
        //Original method truncate by casting to int
        if (CDTXMania.ConfigIni.nChipPlayTimeComputeMode == 0)
        {
            return (int)input;
        }
        //Accurate Method using Math.Round
        else
        {
            return (int)Math.Round(input);
        }
    }

    private bool tInput_LineAnalysis_ChipLocation(ref string strCommand, ref string strParameter)
    {
        // (1) Process command
        if (strCommand.Length != 5)
            return false;

        //Extract measure number (first three characters)
        int measureNumber = CConversion.nConvert3DigitMeasureNumberToNumber(strCommand);
        if (measureNumber < 0)
            return false;

        measureNumber++; // 先頭に空の1小節を設ける。
            
        #region [ Determine channel number ]
        EChannel nChannelNumber = EChannel.Invalid;

        //Process depends on file format
        if (eFileType is EType.GDA or EType.G2D)
        {
            //(A) GDA, G2D files: Replace channel strings with DTX channel numbers (conversion)
            string strChannelString = strCommand.Substring(3, 2);

            foreach (STGDAPARAM param in stGDAParam)
            {
                if (strChannelString.Equals(param.strGDAのチャンネル文字列, StringComparison.OrdinalIgnoreCase))
                {
                    nChannelNumber = param.nDTXのチャンネル番号;
                    break;
                }
            }
        }
        else
        {
            //(B) In other cases: Channel is two hexadecimal digits
            nChannelNumber = (EChannel) CConversion.nConvert2DigitHexadecimalStringToNumber(strCommand, 3);
        }
        #endregion
            
        #region [ bHasChips ]
        switch (nChannelNumber) //determine if we have chips per instrument
        {
            case < 0:
                return false; //channel number is invalid
                
            case >= EChannel.HiHatClose and <= EChannel.LeftBassDrum:
                bHasChips.Drums = true;
                break;
                
            case >= EChannel.Guitar_Open and <= EChannel.Guitar_RGBxx:
            case >= EChannel.Guitar_xxxYx and <= EChannel.Guitar_RxxxP:
            case >= EChannel.Guitar_RGxxP and <= EChannel.Guitar_xGBYP:
            case >= EChannel.Guitar_RxxYP and <= EChannel.Guitar_RGBYP:
                bHasChips.Guitar = true;
                break;
                
            case >= EChannel.Bass_Open and <= EChannel.Bass_RGBxx:
            case EChannel.Bass_xxxYx:
            case EChannel.Bass_xxBYx:
            case >= EChannel.Bass_xGxYx and <= EChannel.Bass_xxBxP:
            case >= EChannel.Bass_xGxxP and <= EChannel.Bass_RGBxP:
            case >= EChannel.Bass_xxxYP and <= EChannel.Bass_RGBYP:
                bHasChips.Bass = true;
                break;
        }
            
        //determine if we have chips per instrument input
        switch (nChannelNumber)
        {
            case EChannel.FloorTom:
                bHasChips.FT = true;
                break;

            case EChannel.HiHatOpen:
                bHasChips.HHOpen = true;
                break;

            case EChannel.RideCymbal:
                bHasChips.Ride = true;
                break;

            case EChannel.LeftCymbal:
                bHasChips.LeftCymbal = true;
                break;

            case EChannel.LeftPedal:
                bHasChips.LP = true;
                break;

            case EChannel.LeftBassDrum:
                bHasChips.LBD = true;
                break;

            case EChannel.Guitar_Open:
                bHasChips.OpenGuitar = true;
                break;

            case EChannel.Movie:
            case EChannel.MovieFull:
                bHasChips.AVI = true;
                break;

            case EChannel.Guitar_xxxYx:
            case EChannel.Guitar_xxBYx:
            case EChannel.Guitar_xGxYx:
            case EChannel.Guitar_xGBYx:
            case EChannel.Guitar_RxxYx:
            case EChannel.Guitar_RxBYx:
            case EChannel.Guitar_RGxYx:
            case EChannel.Guitar_RGBYx:
            case EChannel.Guitar_xxxxP:
            case EChannel.Guitar_xxBxP:
            case EChannel.Guitar_xGxxP:
            case EChannel.Guitar_xGBxP:
            case EChannel.Guitar_RxxxP:
            case EChannel.Guitar_RxBxP:
            case EChannel.Guitar_RGxxP:
            case EChannel.Guitar_RGBxP:
            case EChannel.Guitar_xxxYP:
            case EChannel.Guitar_xxBYP:
            case EChannel.Guitar_xGxYP:
            case EChannel.Guitar_xGBYP:
            case EChannel.Guitar_RxxYP:
            case EChannel.Guitar_RxBYP:
            case EChannel.Guitar_RGxYP:
            case EChannel.Guitar_RGBYP:
                bHasChips.YPGuitar = true;
                break;

            case EChannel.Bass_xxxYx:
            case EChannel.Bass_xxBYx:
            case EChannel.Bass_xGxYx:
            case EChannel.Bass_xGBYx:
            case EChannel.Bass_RxxYx:
            case EChannel.Bass_RxBYx:
            case EChannel.Bass_RGxYx:
            case EChannel.Bass_RGBYx:
            case EChannel.Bass_xxxxP:
            case EChannel.Bass_xxBxP:
            case EChannel.Bass_xGxxP:
            case EChannel.Bass_xGBxP:
            case EChannel.Bass_RxxxP:
            case EChannel.Bass_RxBxP:
            case EChannel.Bass_RGxxP:
            case EChannel.Bass_RGBxP:
            case EChannel.Bass_xxxYP:
            case EChannel.Bass_xxBYP:
            case EChannel.Bass_xGxYP:
            case EChannel.Bass_xGBYP:
            case EChannel.Bass_RxxYP:
            case EChannel.Bass_RxBYP:
            case EChannel.Bass_RGxYP:
            case EChannel.Bass_RGBYP:
                bHasChips.YPBass = true;
                break;

            case EChannel.Bass_Open:
                bHasChips.OpenBass = true;
                break;
        }
        #endregion
            
        // (2) Process Ch. 02
        #region [ 小節長変更(Ch.02)は他のチャンネルとはパラメータが特殊なので、先にとっとと終わらせる。 ]

        //-----------------
        if (nChannelNumber == EChannel.BarLength)
        {
            // 小節長倍率を取得する。

            //if( !double.TryParse( strパラメータ, out result ) )
            if (!TryParse(strParameter, out double db小節長倍率)) // #23880 2010.12.30 yyagi: alternative TryParse to permit both '.' and ',' for decimal point
            {
                Trace.TraceError("小節長倍率に不正な値を指定しました。[{0}: {1}行]", strFileNameFullPath, lineNumber);
                return false;
            }

            // 小節長倍率チップを配置する。

            listChip.Insert(0,
                new CChip
                {
                    nChannelNumber = nChannelNumber,
                    db実数値 = db小節長倍率,
                    nPlaybackPosition = measureNumber * 384,
                });

            return true; // 配置終了。
        }

        //-----------------

        #endregion


        // (3) Process Parameter

        if (string.IsNullOrEmpty(strParameter)) // パラメータはnullまたは空文字列ではないこと。
            return false;

        #region [ strパラメータ にオブジェクト記述を格納し、その n文字数 をカウントする。]

        //-----------------
        int n文字数 = 0;

        StringBuilder sb = new(strParameter.Length);

        // strパラメータを先頭から1文字ずつ見ながら正規化（無効文字('_')を飛ばしたり不正な文字でエラーを出したり）し、sb へ格納する。

        CharEnumerator ce = strParameter.GetEnumerator();
        while (ce.MoveNext())
        {
            if (ce.Current == '_') // '_' は無視。
                continue;

            if (CConversion.strBase36Characters.IndexOf(ce.Current) < 0) // オブジェクト記述は36進数文字であること。
            {
                Trace.TraceError("不正なオブジェクト指定があります。[{0}: {1}行]", strFileNameFullPath, lineNumber);
                return false;
            }

            sb.Append(ce.Current);
            n文字数++;
        }

        strParameter = sb.ToString(); // 正規化された文字列になりました。
        ce.Dispose();

        if ((n文字数 % 2) != 0) // パラメータの文字数が奇数の場合、最後の1文字を無視する。
            n文字数--;
        //-----------------

        #endregion


        // (4) パラメータをオブジェクト数値に分解して配置する。

        for (int i = 0; i < (n文字数 / 2); i++) // 2文字で1オブジェクト数値
        {
            #region [ nオブジェクト数値 を１つ取得する。'00' なら無視。]

            //-----------------
            int nオブジェクト数値 = 0;

            if (nChannelNumber == EChannel.BPM)
            {
                // Ch.03 のみ 16進数2桁。
                nオブジェクト数値 = CConversion.nConvert2DigitHexadecimalStringToNumber(strParameter, i * 2);
            }
            else
            {
                // その他のチャンネルは36進数2桁。
                nオブジェクト数値 = CConversion.nConvert2DigitBase36StringToNumber(strParameter, i * 2);
            }

            if (nオブジェクト数値 == 0x00)
                continue;
            //-----------------

            #endregion

            // オブジェクト数値に対応するチップを生成。

            var chip = new CChip
            {
                nChannelNumber = nChannelNumber,
                nPlaybackPosition = (measureNumber * 384) + ((384 * i) / (n文字数 / 2)),
                nIntegerValue = nオブジェクト数値,
                nIntegerValue_InternalNumber = nオブジェクト数値
            };

            #region [ Assign instrument part based on channel number ]
            switch (nChannelNumber)
            {
                case >= EChannel.HiHatClose and <= EChannel.LeftBassDrum:
                    chip.eInstrumentPart = EInstrumentPart.DRUMS;
                    break;
                case >= EChannel.Guitar_Open and <= EChannel.Guitar_RGBxx:
                case >= EChannel.Guitar_xxxYx and <= EChannel.Guitar_RxxxP:
                case >= EChannel.Guitar_RxBxP and <= EChannel.Guitar_xGBYP:
                case >= EChannel.Guitar_RxxYP and <= EChannel.Guitar_RGBYP:
                    chip.eInstrumentPart = EInstrumentPart.GUITAR;
                    break;
                case >= EChannel.Bass_Open and <= EChannel.Bass_RGBxx:
                case EChannel.Bass_xxxYx:
                case EChannel.Bass_xxBYx:
                case >= EChannel.Bass_xGxYx and <= EChannel.Bass_xxBxP:
                case >= EChannel.Bass_xGxxP and <= EChannel.Bass_RGBxP:
                case >= EChannel.Bass_xxxYP and <= EChannel.Bass_RGBYP:
                    chip.eInstrumentPart = EInstrumentPart.BASS;
                    break;
            }
            #endregion

            #region [ 無限定義への対応 → 内部番号の取得。]

            //-----------------
            // 2019.06.30 kairera0467
            if (n無限管理WAV.Length < nオブジェクト数値)
                break;

            if (chip.bWAVを使うチャンネルである)
            {
                chip.nIntegerValue_InternalNumber =
                    n無限管理WAV[nオブジェクト数値]; // これが本当に一意なWAV番号となる。（無限定義の場合、chip.nIntegerValue は一意である保証がない。）
            }
            else if (chip.bBPMチップである)
            {
                chip.nIntegerValue_InternalNumber = n無限管理BPM[nオブジェクト数値]; // これが本当に一意なBPM番号となる。（同上。）
            }

            //-----------------

            #endregion

            #region [ フィルインON/OFFチャンネル(Ch.53)の場合、発声位置を少し前後にずらす。]

            //-----------------
            if (nChannelNumber == EChannel.FillIn)
            {
                // ずらすのは、フィルインONチップと同じ位置にいるチップでも確実にフィルインが発動し、
                // 同様に、フィルインOFFチップと同じ位置にいるチップでも確実にフィルインが終了するようにするため。

                // XG化の都合上、フィルインを使っているように見せかけるので、最後はずらさない。
                // ついでに言えば微妙に曖昧だったような気もしなくない。 by.kairera0467

                if ((nオブジェクト数値 > 0) && (nオブジェクト数値 < 2))
                {
                    chip.nPlaybackPosition -= 32; // 384÷32＝12 ということで、フィルインONチップは12分音符ほど前へ移動。
                }
                else if (nオブジェクト数値 == 2)
                {
                    chip.nPlaybackPosition += 32; // 同じく、フィルインOFFチップは12分音符ほど後ろへ移動。
                }
            }

            //-----------------

            #endregion

            // チップを配置。

            listChip.Add(chip);
        }

        return true;
    }

    #region [#23880 2010.12.30 yyagi: コンマとスペースの両方を小数点として扱うTryParse]

    /// <summary>
    /// 小数点としてコンマとピリオドの両方を受け付けるTryParse()
    /// </summary>
    /// <param name="s">strings convert to double</param>
    /// <param name="result">parsed double value</param>
    /// <returns>s が正常に変換された場合は true。それ以外の場合は false。</returns>
    /// <exception cref="ArgumentException">style が NumberStyles 値でないか、style に NumberStyles.AllowHexSpecifier 値が含まれている</exception>
    private bool TryParse(string s, out double result)
    {
        // #23880 2010.12.30 yyagi: alternative TryParse to permit both '.' and ',' for decimal point
        // EU諸国での #BPM 123,45 のような記述に対応するため、
        // 小数点の最終位置を検出して、それをlocaleにあった
        // 文字に置き換えてからTryParse()する
        // 桁区切りの文字はスキップする

        const string DecimalSeparators = ".,"; // 小数点文字
        const string GroupSeparators = ".,' "; // 桁区切り文字
        const string NumberSymbols = "0123456789"; // 数値文字

        int len = s.Length; // 文字列長
        int decimalPosition = len; // 真の小数点の位置 最初は文字列終端位置に仮置きする

        for (int i = 0; i < len; i++)
        {
            // まず、真の小数点(一番最後に現れる小数点)の位置を求める
            char c = s[i];
            if (NumberSymbols.IndexOf(c) >= 0)
            {
                // 数値だったらスキップ
                continue;
            }
            else if (DecimalSeparators.IndexOf(c) >= 0)
            {
                // 小数点文字だったら、その都度位置を上書き記憶
                decimalPosition = i;
            }
            else if (GroupSeparators.IndexOf(c) >= 0)
            {
                // 桁区切り文字の場合もスキップ
                continue;
            }
            else
            {
                // 数値_小数点_区切り文字以外がきたらループ終了
                break;
            }
        }

        StringBuilder decimalStr = new(16);
        for (int i = 0; i < len; i++)
        {
            // 次に、localeにあった数値文字列を生成する
            char c = s[i];
            if (NumberSymbols.IndexOf(c) >= 0)
            {
                // 数値だったら
                decimalStr.Append(c); // そのままコピー
            }
            else if (DecimalSeparators.IndexOf(c) >= 0)
            {
                // 小数点文字だったら
                if (i == decimalPosition)
                {
                    // 最後に出現した小数点文字なら、localeに合った小数点を出力する
                    decimalStr.Append(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
                }
            }
            else if (GroupSeparators.IndexOf(c) >= 0)
            {
                // 桁区切り文字だったら
                continue; // DoNothing(スキップ)
            }
            else
            {
                break;
            }
        }

        return double.TryParse(decimalStr.ToString(), out result); // 最後に、自分のlocale向けの文字列に対してTryParse実行
    }

    #endregion

    //-----------------

    #endregion
}