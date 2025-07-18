using System.Runtime.InteropServices;
using DTXMania.Core;

namespace DTXMania;

[Serializable]
public class CScore
{
    // プロパティ

    public STScoreIniInformation ScoreIniInformation;
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct STScoreIniInformation
    {
        public DateTime LastModified;
        public long FileSize;

        public STScoreIniInformation(DateTime 最終更新日時, long ファイルサイズ)
        {
            LastModified = 最終更新日時;
            FileSize = ファイルサイズ;
        }
    }

    public STFileInformation FileInformation;
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct STFileInformation
    {
        public string AbsoluteFilePath;
        public string AbsoluteFolderPath;
        public DateTime LastModified;
        public long FileSize;

        public STFileInformation(string AbsoluteFilePath, string AbsoluteFolderPath, DateTime LastModified, long FileSize)
        {
            this.AbsoluteFilePath = AbsoluteFilePath;
            this.AbsoluteFolderPath = AbsoluteFolderPath;
            this.LastModified = LastModified;
            this.FileSize = FileSize;
        }
    }

    public STMusicInformation SongInformation;
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct STMusicInformation
    {
        public string Title;
        public string ArtistName;
        public string Comment;
        public string Genre;
        public string Preimage;
        public string Premovie;
        public string Presound;
        public string Backgound;
        public STDGBVALUE<int> Level;
        public STDGBVALUE<int> LevelDec;
        public STRANK BestRank;
        public STSKILL HighSkill;
        public STSKILL HighSongSkill;
        public STDGBVALUE<bool> FullCombo;
        public STDGBVALUE<int> NbPerformances;
        public STHISTORY PerformanceHistory;
        public bool bHiddenLevel;
        public CDTX.EType SongType;
        public double Bpm;
        public int Duration;
        public STDGBVALUE<bool> b完全にCLASSIC譜面である;
        public STDGBVALUE<bool> bScoreExists;
        //
        //public STDGBVALUE<EUseLanes> 使用レーン数;
        public STDGBVALUE<int> chipCountByInstrument;
        public Dictionary<ELane, int> chipCountByLane;
        public STDGBVALUE<string> progress;

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct STHISTORY
        {
            public string 行1;
            public string 行2;
            public string 行3;
            public string 行4;
            public string 行5;
            public string this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return 行1;

                        case 1:
                            return 行2;

                        case 2:
                            return 行3;

                        case 3:
                            return 行4;

                        case 4:
                            return 行5;
                    }
                    throw new IndexOutOfRangeException();
                }
                set
                {
                    switch (index)
                    {
                        case 0:
                            行1 = value;
                            return;

                        case 1:
                            行2 = value;
                            return;

                        case 2:
                            行3 = value;
                            return;

                        case 3:
                            行4 = value;
                            return;

                        case 4:
                            行5 = value;
                            return;
                    }
                    throw new IndexOutOfRangeException();
                }
            }
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct STRANK
        {
            public int Drums;
            public int Guitar;
            public int Bass;
            public int this[int index]
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
                    }
                    throw new IndexOutOfRangeException();
                }
                set
                {
                    if ((value < (int)CScoreIni.ERANK.SS) || ((value != (int)CScoreIni.ERANK.UNKNOWN) && (value > (int)CScoreIni.ERANK.E)))
                    {
                        throw new ArgumentOutOfRangeException();
                    }
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
                    }
                    throw new IndexOutOfRangeException();
                }
            }
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct STSKILL
        {
            public double Drums;
            public double Guitar;
            public double Bass;
            public double this[int index]
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
                    }
                    throw new IndexOutOfRangeException();
                }
                set
                {
                    if ((value < 0.0) || (value > 200.0))
                    {
                        throw new ArgumentOutOfRangeException();
                    }
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
                    }
                    throw new IndexOutOfRangeException();
                }
            }
        }
    }

    public bool bHadACacheInSongDB;
    public bool bIsScoreValid => (((SongInformation.Level[0] + SongInformation.Level[1]) + SongInformation.Level[2]) != 0);


    // Constructor

    public CScore()
    {
        ScoreIniInformation = new STScoreIniInformation(DateTime.MinValue, 0L);
        bHadACacheInSongDB = false;
        FileInformation = new STFileInformation("", "", DateTime.MinValue, 0L);
        SongInformation = new STMusicInformation();
        SongInformation.Title = "";
        SongInformation.ArtistName = "";
        SongInformation.Comment = "";
        SongInformation.Genre = "";
        SongInformation.Preimage = "";
        SongInformation.Premovie = "";
        SongInformation.Presound = "";
        SongInformation.Backgound = "";
        SongInformation.Level = new STDGBVALUE<int>();
        SongInformation.LevelDec = new STDGBVALUE<int>();
        SongInformation.BestRank = new STMusicInformation.STRANK();
        SongInformation.BestRank.Drums = (int)CScoreIni.ERANK.UNKNOWN;
        SongInformation.BestRank.Guitar = (int)CScoreIni.ERANK.UNKNOWN;
        SongInformation.BestRank.Bass = (int)CScoreIni.ERANK.UNKNOWN;
        SongInformation.FullCombo = new STDGBVALUE<bool>();
        SongInformation.NbPerformances = new STDGBVALUE<int>();
        SongInformation.PerformanceHistory = new STMusicInformation.STHISTORY();
        SongInformation.PerformanceHistory.行1 = "";
        SongInformation.PerformanceHistory.行2 = "";
        SongInformation.PerformanceHistory.行3 = "";
        SongInformation.PerformanceHistory.行4 = "";
        SongInformation.PerformanceHistory.行5 = "";
        SongInformation.bHiddenLevel = false;
        SongInformation.HighSkill = new STMusicInformation.STSKILL();
        SongInformation.HighSongSkill = new STMusicInformation.STSKILL();
        SongInformation.SongType = CDTX.EType.DTX;
        SongInformation.Bpm = 120.0;
        SongInformation.Duration = 0;
        SongInformation.b完全にCLASSIC譜面である.Drums = false;
        SongInformation.b完全にCLASSIC譜面である.Guitar = false;
        SongInformation.b完全にCLASSIC譜面である.Bass = false;
        SongInformation.bScoreExists.Drums = false;
        SongInformation.bScoreExists.Guitar = false;
        SongInformation.bScoreExists.Bass = false;
        //
        SongInformation.chipCountByInstrument = default(STDGBVALUE<int>);
        SongInformation.chipCountByLane = new Dictionary<ELane, int>();
        SongInformation.progress = new STDGBVALUE<string>()
        {
            Drums = "",
            Guitar = "",
            Bass = ""
        };

        for (ELane eLane = ELane.LC; eLane < ELane.BGM; eLane++)
        {
            SongInformation.chipCountByLane[eLane] = 0;
        }


    }
}