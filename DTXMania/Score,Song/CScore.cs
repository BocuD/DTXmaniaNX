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

        public STFileInformation(string AbsoluteFilePath, string AbsoluteFolderPath, DateTime LastModified,
            long FileSize)
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

        public bool TitleHasJapanese;
        public bool ArtistNameHasJapanese;

        public string TitleKana;
        public string ArtistNameKana;
        
        public string TitleRoman;
        public string ArtistNameRoman;
        
        public string Comment;
        public string Genre;
        public string Preimage;
        public string Premovie;
        public string Presound;
        public string Backgound;
        public STDGBVALUE<int> Level;
        public STDGBVALUE<int> LevelDec;
        public STRANK BestRank;
        public STSKILL HighCompletionRate;
        public STSKILL HighSongSkill;
        public STDGBVALUE<bool> FullCombo;
        public STDGBVALUE<int> NbPerformances;
        public STHISTORY PerformanceHistory;
        public bool bHiddenLevel;
        public CDTX.EType SongType;
        public double Bpm;
        public int Duration;
        public STDGBVALUE<bool> bIsClassicChart;

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
                    if ((value < (int)CScoreIni.ERANK.SS) ||
                        ((value != (int)CScoreIni.ERANK.UNKNOWN) && (value > (int)CScoreIni.ERANK.E)))
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

        public double GetLevel(int instrument)
        {
            double dbLevel = Level[instrument];
            int levelDec = LevelDec[instrument];
            if (dbLevel >= 100)
            {
                dbLevel /= 100.0;
            }
            else if (dbLevel < 100)
            {
                dbLevel = dbLevel / 10.0 + levelDec / 100.0;
            }

            return dbLevel;
        }

        public double GetMaxSkill(int instrument)
        {
            return GetLevel(instrument) * 20;
        }
    }

    public bool bHadACacheInSongDB;
    public bool countSkill = false;


    // Constructor

    public CScore()
    {
        ScoreIniInformation = new STScoreIniInformation(DateTime.MinValue, 0L);
        bHadACacheInSongDB = false;
        FileInformation = new STFileInformation("", "", DateTime.MinValue, 0L);
        SongInformation = new STMusicInformation
        {
            Title = "",
            ArtistName = "",
            Comment = "",
            Genre = "",
            Preimage = "",
            Premovie = "",
            Presound = "",
            Backgound = "",
            Level = new STDGBVALUE<int>(),
            LevelDec = new STDGBVALUE<int>(),
            BestRank = new STMusicInformation.STRANK
            {
                Drums = (int)CScoreIni.ERANK.UNKNOWN,
                Guitar = (int)CScoreIni.ERANK.UNKNOWN,
                Bass = (int)CScoreIni.ERANK.UNKNOWN
            },
            FullCombo = new STDGBVALUE<bool>(),
            NbPerformances = new STDGBVALUE<int>(),
            PerformanceHistory = new STMusicInformation.STHISTORY
            {
                行1 = "",
                行2 = "",
                行3 = "",
                行4 = "",
                行5 = ""
            },
            bHiddenLevel = false,
            HighCompletionRate = new STMusicInformation.STSKILL(),
            HighSongSkill = new STMusicInformation.STSKILL(),
            SongType = CDTX.EType.DTX,
            Bpm = 120.0,
            Duration = 0,
            bIsClassicChart = new STDGBVALUE<bool>()
            {
                Drums = false,
                Guitar = false,
                Bass = false
            },
            bScoreExists = new STDGBVALUE<bool>()
            {
                Drums = false,
                Guitar = false,
                Bass = false
            },
            chipCountByInstrument = default,
            chipCountByLane = new Dictionary<ELane, int>(),
            progress = new STDGBVALUE<string>()
            {
                Drums = "",
                Guitar = "",
                Bass = ""
            }
        };

        for (ELane eLane = ELane.LC; eLane < ELane.BGM; eLane++)
        {
            SongInformation.chipCountByLane[eLane] = 0;
        }
    }

    public bool HasChartForCurrentMode()
    {
        bool bScoreExistForMode = CDTXMania.ConfigIni.bDrumsEnabled && SongInformation.bScoreExists.Drums;
        if (!bScoreExistForMode)
        {
            bScoreExistForMode = CDTXMania.ConfigIni.bGuitarEnabled &&
                                 (SongInformation.bScoreExists.Guitar || SongInformation.bScoreExists.Bass);
        }
        return bScoreExistForMode;
    }
}